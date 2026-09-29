using System;
using System.Collections.Generic;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Media;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.2-A (#100): P3's host-authoritative recording becomes a real local frame-sequence clip.
    // The client never mints metadata: World chooses the payable/best take and only a safely-returned
    // RecordingResult receives a persistent recordingId and can enter Mert's ClipArchive.
    [DisallowMultipleComponent]
    public sealed class RecordingMediaCaptureBinding : MonoBehaviour
    {
        private const int Width = 320, Height = 180, JpegQuality = 58, MaxFrames = 300;
        private const double FrameInterval = 0.2; // 5 fps prototype footage.

        private sealed class Active
        {
            public PlayerId Player;
            public string DiveId;
            public double StartedAt, NextFrameAt;
            public readonly List<ClipFramePacket> Frames = new List<ClipFramePacket>();
        }

        private sealed class Pending
        {
            public int DayNumber;
            public float Duration;
            public List<ClipFramePacket> Frames;
            public byte[] Encoded;
            public string Hash;
        }

        private sealed class Playing
        {
            public string ClipId;
            public ClipFileData Data;
            public Texture2D Texture;
            public double StartedAt;
            public int Frame = -1;
        }

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private RecordingWorldBinding world;
        private RecordingDiveBinding observed;
        private DayNetworkBinding day;
        private readonly Dictionary<ulong, Active> active = new Dictionary<ulong, Active>();
        private readonly Dictionary<string, Pending> pending = new Dictionary<string, Pending>(StringComparer.Ordinal);
        private readonly Dictionary<string, RecordingResult> waiting = new Dictionary<string, RecordingResult>(StringComparer.Ordinal);
        private readonly HashSet<string> submitted = new HashSet<string>(StringComparer.Ordinal);

        private GameObject captureCameraObject;
        private Camera captureCamera;
        private RenderTexture captureTarget;
        private Texture2D readback;
        private Playing playing;
        private Func<string, bool> canPlay;
        private Func<string, bool> play;
        private double nextRetry;
        private bool warnedHeadless;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<RecordingMediaCaptureBinding>() == null)
                session.gameObject.AddComponent<RecordingMediaCaptureBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            world = GetComponent<RecordingWorldBinding>();
            day = GetComponent<DayNetworkBinding>();
            if (adapter == null || manager == null) enabled = false;
        }

        private void OnEnable()
        {
            canPlay = CanPlay;
            play = TryPlay;
            ClipPlayback.Bind(canPlay, play);
            RefreshHooks();
        }

        private void Update()
        {
            RefreshHooks();
            TickPlayback();
            if (adapter == null || manager == null || !manager.IsListening || !adapter.IsAuthority || !manager.IsServer)
            {
                active.Clear();
                return;
            }

            TickCaptures();
            if (waiting.Count > 0 && ClipArchive.SubmitHandler != null && Time.realtimeSinceStartupAsDouble >= nextRetry)
            {
                nextRetry = Time.realtimeSinceStartupAsDouble + 0.5;
                foreach (var result in new List<RecordingResult>(waiting.Values)) TrySubmit(result);
            }
        }

        private void RefreshHooks()
        {
            if (world == null) world = GetComponent<RecordingWorldBinding>();
            if (day == null) day = GetComponent<DayNetworkBinding>();
            var next = world != null ? world.Binding : null;
            if (ReferenceEquals(next, observed)) return;
            if (observed != null)
            {
                observed.TakeRegistered -= OnTakeRegistered;
                observed.RecordingQueued -= OnRecordingQueued;
            }
            observed = next;
            if (observed != null)
            {
                observed.TakeRegistered += OnTakeRegistered;
                observed.RecordingQueued += OnRecordingQueued;
            }
        }

        private void TickCaptures()
        {
            var now = Time.realtimeSinceStartupAsDouble;
            var live = new HashSet<ulong>();
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (!player.IsSpawned || !player.IsServer) continue;
                var owner = player.OwnerClientId;
                live.Add(owner);

                var shouldRecord = player.RecordingPresentation.Value && adapter.Session.State.Phase == SessionPhase.Dive &&
                                   !string.IsNullOrWhiteSpace(adapter.Session.State.DiveId);
                if (!shouldRecord)
                {
                    // If TakeRegistered did not consume it synchronously during the stop, it was non-payable/rejected.
                    active.Remove(owner);
                    continue;
                }

                if (!active.TryGetValue(owner, out var capture))
                {
                    capture = new Active
                    {
                        Player = new PlayerId(owner), DiveId = adapter.Session.State.DiveId,
                        StartedAt = now, NextFrameAt = now
                    };
                    active[owner] = capture;
                }
                if (capture.DiveId != adapter.Session.State.DiveId) { active.Remove(owner); continue; }
                if (capture.Frames.Count < MaxFrames && now >= capture.NextFrameAt)
                {
                    CaptureFrame(capture, player, now);
                    capture.NextFrameAt = now + FrameInterval;
                }
            }
            foreach (var owner in new List<ulong>(active.Keys)) if (!live.Contains(owner)) active.Remove(owner);
        }

        private void OnTakeRegistered(RecordingTake take)
        {
            if (adapter == null || !adapter.IsAuthority || !active.TryGetValue(take.PlayerId.Value, out var capture) ||
                capture.DiveId != take.DiveId) return;

            var now = Time.realtimeSinceStartupAsDouble;
            var player = FindPlayer(take.PlayerId);
            if (player != null && capture.Frames.Count < MaxFrames) CaptureFrame(capture, player, now);
            active.Remove(take.PlayerId.Value);
            if (capture.Frames.Count == 0) return; // Headless/graphics failure: never persist fake footage.

            pending[Key(take.DiveId, take.PlayerId, take.SubjectId)] = new Pending
            {
                DayNumber = CurrentDay(),
                Duration = Mathf.Max(0.05f, (float)(now - capture.StartedAt)),
                Frames = capture.Frames
            };
        }

        private void OnRecordingQueued(RecordingResult result)
        {
            if (adapter == null || !adapter.IsAuthority || string.IsNullOrWhiteSpace(result.RecordingId) || submitted.Contains(result.RecordingId)) return;
            waiting[result.RecordingId] = result;
            TrySubmit(result);
        }

        private void TrySubmit(RecordingResult result)
        {
            if (submitted.Contains(result.RecordingId)) { waiting.Remove(result.RecordingId); return; }
            var key = Key(result.DiveId, result.PlayerId, result.SubjectId);
            if (!pending.TryGetValue(key, out var media) || media.Frames == null || media.Frames.Count == 0) return;

            try
            {
                media.Encoded ??= RecordingClipFile.Encode(Width, Height, media.Duration, media.Frames);
                media.Hash ??= RecordingClipFile.Sha256Hex(media.Encoded);
                if (!RecordingClipFile.TryBuildManifest(result, media.DayNumber, media.Duration, media.Hash,
                        media.Encoded.LongLength, out var manifest)) return;

                var path = PathFor(manifest.ClipId);
                if (string.IsNullOrEmpty(path)) return;
                Directory.CreateDirectory(ClipRoot);
                if (!File.Exists(path) || new FileInfo(path).Length != media.Encoded.LongLength)
                {
                    var temp = path + ".tmp";
                    File.WriteAllBytes(temp, media.Encoded);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(temp, path);
                }

                var outcome = ClipArchive.TrySubmit(manifest);
                if (outcome == ClipArchiveOutcome.Added || outcome == ClipArchiveOutcome.AlreadyArchived)
                {
                    submitted.Add(result.RecordingId);
                    waiting.Remove(result.RecordingId);
                    pending.Remove(key);
                    Debug.Log($"P4_MEDIA_CLIP archived clip={manifest.ClipId} recording={result.RecordingId} frames={media.Frames.Count} bytes={media.Encoded.Length}");
                }
                else if (outcome == ClipArchiveOutcome.ArchiveFull)
                {
                    waiting.Remove(result.RecordingId);
                    pending.Remove(key);
                    DeleteQuietly(path);
                }
            }
            catch (IOException e) { Debug.LogWarning("P4 media clip write failed: " + e.Message); }
            catch (UnauthorizedAccessException e) { Debug.LogWarning("P4 media clip access failed: " + e.Message); }
        }

        private bool CaptureFrame(Active capture, NetworkPlayer player, double now)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                if (!warnedHeadless) { warnedHeadless = true; Debug.Log("P4_MEDIA_CAPTURE disabled on Null graphics device."); }
                return false;
            }
            if (!EnsureCaptureObjects()) return false;
            var forward = player.RecordingForwardServer;
            if (forward.sqrMagnitude < 0.001f) return false;

            var sourceCameras = player.GetComponentsInChildren<Camera>(true);
            try
            {
                if (sourceCameras.Length > 0) captureCamera.CopyFrom(sourceCameras[0]);
                captureCamera.enabled = false;
                captureCamera.targetTexture = captureTarget;
                captureCamera.fieldOfView = Mathf.Clamp(player.RecordingFieldOfView, 20f, 120f);
                captureCamera.aspect = Width / (float)Height;
                captureCamera.transform.SetPositionAndRotation(player.RecordingEyePosition, Quaternion.LookRotation(forward.normalized, Vector3.up));

                var old = RenderTexture.active;
                captureCamera.Render();
                RenderTexture.active = captureTarget;
                readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                readback.Apply(false, false);
                var jpeg = readback.EncodeToJPG(JpegQuality);
                RenderTexture.active = old;
                if (jpeg == null || jpeg.Length == 0) return false;
                capture.Frames.Add(new ClipFramePacket((float)(now - capture.StartedAt), jpeg));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("P4 media frame capture failed: " + e.Message);
                return false;
            }
        }

        private bool EnsureCaptureObjects()
        {
            if (captureCamera == null)
            {
                captureCameraObject = new GameObject("P4HostClipCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
                captureCamera = captureCameraObject.AddComponent<Camera>();
                captureCamera.enabled = false;
            }
            if (captureTarget == null)
            {
                captureTarget = new RenderTexture(Width, Height, 16, RenderTextureFormat.ARGB32)
                    { name = "P4ClipCaptureRT", hideFlags = HideFlags.HideAndDontSave };
                if (!captureTarget.Create()) return false;
            }
            if (readback == null)
                readback = new Texture2D(Width, Height, TextureFormat.RGB24, false)
                    { name = "P4ClipReadback", hideFlags = HideFlags.HideAndDontSave };
            return true;
        }

        private NetworkPlayer FindPlayer(PlayerId id)
        {
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++) if (players[i].IsSpawned && players[i].OwnerClientId == id.Value) return players[i];
            return null;
        }

        private int CurrentDay()
        {
            if (day == null) day = GetComponent<DayNetworkBinding>();
            return day?.Engine != null ? Mathf.Max(1, day.Engine.State.DayNumber) : 1;
        }

        private static string Key(string dive, PlayerId player, string subject) => (dive ?? "") + "|" + player.Value + "|" + (subject ?? "");
        private static string ClipRoot => Path.Combine(Application.persistentDataPath, "DeepDiveClips");
        private static string PathFor(string clipId) => string.IsNullOrWhiteSpace(clipId) || clipId != Path.GetFileName(clipId) || clipId.Length > 80
            ? string.Empty : Path.Combine(ClipRoot, clipId + ".ddclip");

        private bool CanPlay(string clipId)
        {
            var path = PathFor(clipId);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                var size = new FileInfo(path).Length;
                foreach (var clip in MediaNetworkBinding.Mirrored.Clips ?? new List<ClipSave>())
                    if (clip.ClipId == clipId && clip.SizeBytes > 0 && clip.SizeBytes != size) return false;
                return size > 0;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private bool TryPlay(string clipId)
        {
            var path = PathFor(clipId);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (!RecordingClipFile.TryDecode(bytes, out var data)) return false;
                foreach (var clip in MediaNetworkBinding.Mirrored.Clips ?? new List<ClipSave>())
                {
                    if (clip.ClipId != clipId) continue;
                    if (clip.SizeBytes > 0 && clip.SizeBytes != bytes.LongLength) return false;
                    if (!string.IsNullOrWhiteSpace(clip.ContentHash) && !string.Equals(clip.ContentHash,
                            RecordingClipFile.Sha256Hex(bytes), StringComparison.OrdinalIgnoreCase)) return false;
                    break;
                }
                StopPlayback();
                playing = new Playing
                {
                    ClipId = clipId, Data = data,
                    Texture = new Texture2D(data.Width, data.Height, TextureFormat.RGB24, false),
                    StartedAt = Time.unscaledTimeAsDouble
                };
                LoadFrame(0);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private void TickPlayback()
        {
            if (playing == null || playing.Data.Frames.Count == 0) return;
            var elapsed = (float)(Time.unscaledTimeAsDouble - playing.StartedAt);
            if (elapsed > playing.Data.DurationSeconds + 0.25f) { StopPlayback(); return; }
            var index = playing.Frame;
            for (var i = Mathf.Max(0, playing.Frame); i < playing.Data.Frames.Count; i++)
            {
                if (playing.Data.Frames[i].TimeSeconds > elapsed) break;
                index = i;
            }
            if (index >= 0 && index != playing.Frame) LoadFrame(index);
        }

        private void LoadFrame(int index)
        {
            if (playing == null || index < 0 || index >= playing.Data.Frames.Count) return;
            if (playing.Texture.LoadImage(playing.Data.Frames[index].JpegBytes, false)) playing.Frame = index;
        }

        private void OnGUI()
        {
            if (playing?.Texture == null) return;
            var w = Mathf.Min(760f, Screen.width * 0.72f);
            var h = w * Height / Width;
            var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(new Rect(rect.x - 8, rect.y - 34, rect.width + 16, rect.height + 70), "KAYIT OYNATMA  " + playing.ClipId);
            GUI.DrawTexture(rect, playing.Texture, ScaleMode.ScaleToFit, false);
            if (GUI.Button(new Rect(rect.xMax - 90, rect.yMax + 8, 90, 24), "KAPAT")) StopPlayback();
        }

        private void StopPlayback()
        {
            if (playing?.Texture != null) Destroy(playing.Texture);
            playing = null;
        }

        private static void DeleteQuietly(string path)
        {
            try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private void OnDisable()
        {
            if (observed != null)
            {
                observed.TakeRegistered -= OnTakeRegistered;
                observed.RecordingQueued -= OnRecordingQueued;
                observed = null;
            }
            ClipPlayback.Unbind(canPlay);
            active.Clear(); pending.Clear(); waiting.Clear();
            StopPlayback();
            if (readback != null) Destroy(readback);
            if (captureTarget != null) { captureTarget.Release(); Destroy(captureTarget); }
            if (captureCameraObject != null) Destroy(captureCameraObject);
            readback = null; captureTarget = null; captureCamera = null; captureCameraObject = null;
        }
    }
}
