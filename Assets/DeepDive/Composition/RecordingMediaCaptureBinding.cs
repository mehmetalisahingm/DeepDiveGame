using System;
using System.Collections.Generic;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Media;
using DeepDive.Network;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.2-A (#100): turns P3's already host-verified recording lifecycle into a real local clip payload.
    // No client can mint a ClipManifest: frames are sampled from the server's authoritative recorder pose,
    // the World layer decides which take is payable/best, and only a safely-returned RecordingResult receives
    // a persistent recordingId and reaches ClipArchive. A rejected/low-quality/lost take leaves no archive row.
    [DisallowMultipleComponent]
    public sealed class RecordingMediaCaptureBinding : MonoBehaviour
    {
        private const int CaptureWidth = 320;
        private const int CaptureHeight = 180;
        private const int JpegQuality = 58;
        private const double FrameInterval = 0.2; // 5 fps: enough for the prototype while keeping co-op payloads small.
        private const int MaxCapturedFrames = 300;

        private sealed class ActiveCapture
        {
            public PlayerId Player;
            public string DiveId;
            public double StartedAt;
            public double NextFrameAt;
            public readonly List<ClipFramePacket> Frames = new List<ClipFramePacket>();
        }

        private sealed class PendingCapture
        {
            public RecordingTake Take;
            public int DayNumber;
            public float MediaDuration;
            public List<ClipFramePacket> Frames;
            public byte[] Encoded;
            public string Hash;
        }

        private sealed class PlaybackState
        {
            public string ClipId;
            public ClipFileData Data;
            public Texture2D Texture;
            public double StartedAt;
            public int FrameIndex = -1;
        }

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private RecordingWorldBinding world;
        private RecordingDiveBinding observedBinding;
        private DayNetworkBinding day;
        private readonly Dictionary<ulong, ActiveCapture> active = new Dictionary<ulong, ActiveCapture>();
        private readonly Dictionary<string, PendingCapture> pending = new Dictionary<string, PendingCapture>(StringComparer.Ordinal);
        private readonly Dictionary<string, RecordingResult> waitingResults = new Dictionary<string, RecordingResult>(StringComparer.Ordinal);
        private readonly HashSet<string> submittedRecordingIds = new HashSet<string>(StringComparer.Ordinal);

        private GameObject captureCameraObject;
        private Camera captureCamera;
        private RenderTexture captureTarget;
        private Texture2D readback;
        private PlaybackState playback;
        private Func<string, bool> canPlayDelegate;
        private Func<string, bool> playDelegate;
        private double nextSubmitRetry;
        private bool warnedNoGraphics;

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
            canPlayDelegate = CanPlay;
            playDelegate = TryPlay;
            ClipPlayback.Bind(canPlayDelegate, playDelegate);
            EnsureHooks();
        }

        private void Update()
        {
            EnsureHooks();
            UpdatePlayback();
            if (adapter == null || manager == null || !manager.IsListening)
            {
                active.Clear();
                return;
            }

            if (!adapter.IsAuthority || !manager.IsServer)
            {
                active.Clear();
                return;
            }

            PollAuthoritativeRecordingState();
            RetryArchiveSubmissions();
        }

        private void EnsureHooks()
        {
            if (world == null) world = GetComponent<RecordingWorldBinding>();
            if (day == null) day = GetComponent<DayNetworkBinding>();
            var next = world != null ? world.Binding : null;
            if (ReferenceEquals(next, observedBinding)) return;

            if (observedBinding != null)
            {
                observedBinding.TakeRegistered -= OnTakeRegistered;
                observedBinding.RecordingQueued -= OnRecordingQueued;
            }
            observedBinding = next;
            if (observedBinding != null)
            {
                observedBinding.TakeRegistered += OnTakeRegistered;
                observedBinding.RecordingQueued += OnRecordingQueued;
            }
        }

        private void PollAuthoritativeRecordingState()
        {
            var live = new HashSet<ulong>();
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            var now = Time.realtimeSinceStartupAsDouble;
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (!player.IsSpawned || !player.IsServer) continue;
                var id = player.OwnerClientId;
                live.Add(id);

                if (player.RecordingPresentation.Value && adapter.Session.State.Phase == SessionPhase.Dive &&
                    !string.IsNullOrWhiteSpace(adapter.Session.State.DiveId))
                {
                    if (!active.TryGetValue(id, out var capture))
                    {
                        capture = new ActiveCapture
                        {
                            Player = new PlayerId(id),
                            DiveId = adapter.Session.State.DiveId,
                            StartedAt = now,
                            NextFrameAt = now
                        };
                        active[id] = capture;
                    }
                    if (capture.DiveId != adapter.Session.State.DiveId)
                    {
                        active.Remove(id);
                        continue;
                    }
                    if (capture.Frames.Count < MaxCapturedFrames && now >= capture.NextFrameAt)
                    {
                        CaptureFrame(capture, player, now, false);
                        capture.NextFrameAt = now + FrameInterval;
                    }
                }
                else if (active.ContainsKey(id))
                {
                    // If World did not raise TakeRegistered, the stop was rejected/non-payable. Drop it.
                    active.Remove(id);
                }
            }

            foreach (var id in new List<ulong>(active.Keys))
                if (!live.Contains(id)) active.Remove(id);

            if (adapter.Session.State.Phase == SessionPhase.Dive && !string.IsNullOrWhiteSpace(adapter.Session.State.DiveId))
            {
                var currentDive = adapter.Session.State.DiveId;
                foreach (var key in new List<string>(pending.Keys))
                    if (!key.StartsWith(currentDive + "|", StringComparison.Ordinal) &&
                        !HasWaitingResultFor(key)) pending.Remove(key);
            }
        }

        private bool HasWaitingResultFor(string pendingKey)
        {
            foreach (var result in waitingResults.Values)
                if (PendingKey(result.DiveId, result.PlayerId, result.SubjectId) == pendingKey) return true;
            return false;
        }

        private void OnTakeRegistered(RecordingTake take)
        {
            if (adapter == null || !adapter.IsAuthority || string.IsNullOrWhiteSpace(take.DiveId)) return;
            if (!active.TryGetValue(take.PlayerId.Value, out var capture) ||
                !string.Equals(capture.DiveId, take.DiveId, StringComparison.Ordinal)) return;

            var player = FindPlayer(take.PlayerId);
            var now = Time.realtimeSinceStartupAsDouble;
            if (player != null && capture.Frames.Count < MaxCapturedFrames)
                CaptureFrame(capture, player, now, true);

            active.Remove(take.PlayerId.Value);
            if (capture.Frames.Count == 0) return; // graphics failure/headless: never persist a fake or empty clip.

            var duration = Mathf.Max(0.05f, (float)(now - capture.StartedAt));
            pending[PendingKey(take.DiveId, take.PlayerId, take.SubjectId)] = new PendingCapture
            {
                Take = take,
                DayNumber = CurrentDayNumber(),
                MediaDuration = duration,
                Frames = capture.Frames
            };
        }

        private void OnRecordingQueued(RecordingResult result)
        {
            if (adapter == null || !adapter.IsAuthority || string.IsNullOrWhiteSpace(result.RecordingId)) return;
            if (submittedRecordingIds.Contains(result.RecordingId)) return;
            waitingResults[result.RecordingId] = result;
            TrySubmit(result);
        }

        private void RetryArchiveSubmissions()
        {
            if (waitingResults.Count == 0 || ClipArchive.SubmitHandler == null) return;
            var now = Time.realtimeSinceStartupAsDouble;
            if (now < nextSubmitRetry) return;
            nextSubmitRetry = now + 0.5;
            foreach (var result in new List<RecordingResult>(waitingResults.Values)) TrySubmit(result);
        }

        private void TrySubmit(RecordingResult result)
        {
            if (submittedRecordingIds.Contains(result.RecordingId))
            {
                waitingResults.Remove(result.RecordingId);
                return;
            }

            var key = PendingKey(result.DiveId, result.PlayerId, result.SubjectId);
            if (!pending.TryGetValue(key, out var media) || media.Frames == null || media.Frames.Count == 0) return;

            try
            {
                if (media.Encoded == null)
                {
                    media.Encoded = RecordingClipFile.Encode(CaptureWidth, CaptureHeight, media.MediaDuration, media.Frames);
                    media.Hash = RecordingClipFile.Sha256Hex(media.Encoded);
                }
                if (!RecordingClipFile.TryBuildManifest(result, media.DayNumber, media.MediaDuration,
                        media.Hash, media.Encoded.LongLength, out var manifest))
                    return;

                var path = PathFor(manifest.ClipId);
                if (string.IsNullOrEmpty(path)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
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
                    submittedRecordingIds.Add(result.RecordingId);
                    waitingResults.Remove(result.RecordingId);
                    pending.Remove(key);
                    Debug.Log($"P4_MEDIA_CLIP archived clip={manifest.ClipId} recording={result.RecordingId} frames={media.Frames.Count} bytes={media.Encoded.Length}");
                }
                else if (outcome == ClipArchiveOutcome.ArchiveFull)
                {
                    waitingResults.Remove(result.RecordingId);
                    pending.Remove(key);
                    TryDelete(path);
                }
            }
            catch (IOException exception)
            {
                Debug.LogWarning("P4 media clip write failed: " + exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning("P4 media clip access failed: " + exception.Message);
            }
        }

        private bool CaptureFrame(ActiveCapture capture, NetworkPlayer player, double now, bool force)
        {
            if (!force && capture.Frames.Count >= MaxCapturedFrames) return false;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                if (!warnedNoGraphics)
                {
                    warnedNoGraphics = true;
                    Debug.Log("P4_MEDIA_CAPTURE graphics device is Null; clip pixels are disabled on this headless process.");
                }
                return false;
            }
            if (!EnsureCaptureResources()) return false;

            var forward = player.RecordingForwardServer;
            if (forward.sqrMagnitude < 0.001f) return false;
            var source = FirstCamera(player);
            try
            {
                if (source != null) captureCamera.CopyFrom(source);
                captureCamera.enabled = false;
                captureCamera.targetTexture = captureTarget;
                captureCamera.fieldOfView = Mathf.Clamp(player.RecordingFieldOfView, 20f, 120f);
                captureCamera.aspect = CaptureWidth / (float)CaptureHeight;
                captureCamera.transform.SetPositionAndRotation(player.RecordingEyePosition,
                    Quaternion.LookRotation(forward.normalized, Vector3.up));

                var prior = RenderTexture.active;
                captureCamera.Render();
                RenderTexture.active = captureTarget;
                readback.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0, false);
                readback.Apply(false, false);
                var jpeg = readback.EncodeToJPG(JpegQuality);
                RenderTexture.active = prior;
                if (jpeg == null || jpeg.Length == 0) return false;
                capture.Frames.Add(new ClipFramePacket((float)(now - capture.StartedAt), jpeg));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("P4 media frame capture failed: " + exception.Message);
                return false;
            }
        }

        private bool EnsureCaptureResources()
        {
            if (captureCamera == null)
            {
                captureCameraObject = new GameObject("P4HostClipCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
                captureCamera = captureCameraObject.AddComponent<Camera>();
                captureCamera.enabled = false;
            }
            if (captureTarget == null)
            {
                captureTarget = new RenderTexture(CaptureWidth, CaptureHeight, 16, RenderTextureFormat.ARGB32)
                {
                    name = "P4ClipCaptureRT",
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (!captureTarget.Create()) return false;
            }
            if (readback == null)
            {
                readback = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false)
                {
                    name = "P4ClipReadback",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            return true;
        }

        private static Camera FirstCamera(NetworkPlayer player)
        {
            var cameras = player.GetComponentsInChildren<Camera>(true);
            return cameras.Length > 0 ? cameras[0] : null;
        }

        private static string PendingKey(string diveId, PlayerId player, string subjectId) =>
            (diveId ?? string.Empty) + "|" + player.Value + "|" + (subjectId ?? string.Empty);

        private NetworkPlayer FindPlayer(PlayerId playerId)
        {
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
                if (players[i].IsSpawned && players[i].OwnerClientId == playerId.Value) return players[i];
            return null;
        }

        private int CurrentDayNumber()
        {
            if (day == null) day = GetComponent<DayNetworkBinding>();
            var engine = day != null ? day.Engine : null;
            return engine != null ? Mathf.Max(1, engine.State.DayNumber) : 1;
        }

        private static string ClipRoot => Path.Combine(Application.persistentDataPath, "DeepDiveClips");

        private static string PathFor(string clipId)
        {
            if (string.IsNullOrWhiteSpace(clipId) || clipId != Path.GetFileName(clipId) || clipId.Length > 80)
                return string.Empty;
            return Path.Combine(ClipRoot, clipId + ".ddclip");
        }

        private bool CanPlay(string clipId)
        {
            var path = PathFor(clipId);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                var info = new FileInfo(path);
                foreach (var clip in MediaNetworkBinding.Mirrored.Clips)
                    if (clip.ClipId == clipId && clip.SizeBytes > 0 && info.Length != clip.SizeBytes) return false;
                return info.Length > 0;
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
                foreach (var clip in MediaNetworkBinding.Mirrored.Clips)
                {
                    if (clip.ClipId != clipId) continue;
                    if (clip.SizeBytes > 0 && clip.SizeBytes != bytes.LongLength) return false;
                    if (!string.IsNullOrWhiteSpace(clip.ContentHash) &&
                        !string.Equals(clip.ContentHash, RecordingClipFile.Sha256Hex(bytes), StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;
                }

                StopPlayback();
                playback = new PlaybackState
                {
                    ClipId = clipId,
                    Data = data,
                    Texture = new Texture2D(data.Width, data.Height, TextureFormat.RGB24, false),
                    StartedAt = Time.unscaledTimeAsDouble
                };
                LoadPlaybackFrame(0);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private void UpdatePlayback()
        {
            if (playback == null || playback.Data == null || playback.Data.Frames.Count == 0) return;
            var elapsed = (float)(Time.unscaledTimeAsDouble - playback.StartedAt);
            if (elapsed > playback.Data.DurationSeconds + 0.25f)
            {
                StopPlayback();
                return;
            }
            var next = playback.FrameIndex;
            for (var i = Mathf.Max(0, playback.FrameIndex); i < playback.Data.Frames.Count; i++)
            {
                if (playback.Data.Frames[i].TimeSeconds > elapsed) break;
                next = i;
            }
            if (next >= 0 && next != playback.FrameIndex) LoadPlaybackFrame(next);
        }

        private void LoadPlaybackFrame(int index)
        {
            if (playback == null || index < 0 || index >= playback.Data.Frames.Count) return;
            if (!playback.Texture.LoadImage(playback.Data.Frames[index].JpegBytes, false)) return;
            playback.FrameIndex = index;
        }

        private void OnGUI()
        {
            if (playback == null || playback.Texture == null) return;
            var width = Mathf.Min(760f, Screen.width * 0.72f);
            var height = width * CaptureHeight / CaptureWidth;
            var frame = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(new Rect(frame.x - 8, frame.y - 34, frame.width + 16, frame.height + 70), "KAYIT OYNATMA  " + playback.ClipId);
            GUI.DrawTexture(frame, playback.Texture, ScaleMode.ScaleToFit, false);
            if (GUI.Button(new Rect(frame.xMax - 90, frame.yMax + 8, 90, 24), "KAPAT")) StopPlayback();
        }

        private void StopPlayback()
        {
            if (playback?.Texture != null) Destroy(playback.Texture);
            playback = null;
        }

        private static void TryDelete(string path)
        {
            try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private void ReleaseCaptureResources()
        {
            if (readback != null) Destroy(readback);
            readback = null;
            if (captureTarget != null)
            {
                captureTarget.Release();
                Destroy(captureTarget);
            }
            captureTarget = null;
            if (captureCameraObject != null) Destroy(captureCameraObject);
            captureCameraObject = null;
            captureCamera = null;
        }

        private void OnDisable()
        {
            if (observedBinding != null)
            {
                observedBinding.TakeRegistered -= OnTakeRegistered;
                observedBinding.RecordingQueued -= OnRecordingQueued;
                observedBinding = null;
            }
            ClipPlayback.Unbind(canPlayDelegate);
            active.Clear();
            waitingResults.Clear();
            pending.Clear();
            StopPlayback();
            ReleaseCaptureResources();
        }
    }
}
