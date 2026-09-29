using System;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Media;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #100 acceptance guard. It creates no clip, manifest or recording fixture. The existing real -Record
    // smoke drives owner input through RecordingNetworkBridge. This probe only starts judging the media path
    // after RecordingDiveBinding emits the REAL safely-settled RecordingQueued event, so a slow CI return cannot
    // fail merely because settlement has not happened yet.
    public sealed class P4MediaProductProbe : MonoBehaviour
    {
        private SessionNetworkAdapter adapter;
        private RecordingDiveBinding observed;
        private bool sawRealRecordingPresentation;
        private bool finished;
        private string expectedRecordingId = "";
        private double queuedAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p1-integrated").Length == 0 || Arg("-p4-media-product") != "1") return;
            if (FindFirstObjectByType<P4MediaProductProbe>() != null) return;
            var go = new GameObject("P4MediaProductProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<P4MediaProductProbe>();
        }

        private static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : string.Empty;
        }

        private void LateUpdate()
        {
            if (finished) return;
            if (adapter == null) adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            if (adapter == null || !adapter.IsAuthority) return; // host proves the authority path.
            RefreshBinding();

            var players = FindObjectsByType<DeepDive.Network.NetworkPlayer>(FindObjectsSortMode.None);
            if (players.Any(p => p.IsSpawned && p.IsServer && p.RecordingPresentation.Value))
                sawRealRecordingPresentation = true;

            // If the real recording lifecycle never settles, IntegratedSmokeDriver's normal -Record assertions
            // (recordingSafe/recordingPaid) fail the run. This guard owns only the post-settlement media contract.
            if (string.IsNullOrWhiteSpace(expectedRecordingId)) return;

            var clipId = RecordingClipFile.ClipIdForRecording(expectedRecordingId);
            var data = MediaNetworkBinding.Mirrored;
            var real = data?.Clips?.FirstOrDefault(c => c != null && c.RecordingId == expectedRecordingId && c.ClipId == clipId);
            if (real != null)
            {
                var valid = sawRealRecordingPresentation && real.MediaReady && real.SafeReturned &&
                            !string.IsNullOrWhiteSpace(real.SubjectId) && real.Quality >= 1 && real.Quality <= 4 &&
                            real.DurationSeconds > 0f && real.SizeBytes > 0 && real.ContentHash != null &&
                            real.ContentHash.Length == 64 && ClipPlayback.CanPlay(real.ClipId);
                if (valid)
                {
                    finished = true;
                    Debug.Log($"P4_MEDIA_PRODUCT_OK clip={real.ClipId} recording={real.RecordingId} subject={real.SubjectId} q={real.Quality} bytes={real.SizeBytes}");
                    return;
                }
            }

            // RecordingQueued means the host has already selected a safe winner and minted the persistent id.
            // ClipArchive is synchronous once bound, with only short retry delay for binding order; several seconds
            // without a valid playable clip is therefore a real product failure rather than return timing noise.
            if (Time.realtimeSinceStartupAsDouble - queuedAt > 6.0)
            {
                finished = true;
                Debug.LogError($"P4_MEDIA_PRODUCT_FAIL queued={expectedRecordingId} presentation={sawRealRecordingPresentation} " +
                    $"clips={data?.Clips?.Count ?? 0} real={(real != null)} playable={(real != null && ClipPlayback.CanPlay(real.ClipId))}");
            }
        }

        private void RefreshBinding()
        {
            var world = adapter.GetComponent<RecordingWorldBinding>();
            var next = world != null ? world.Binding : null;
            if (ReferenceEquals(next, observed)) return;
            if (observed != null) observed.RecordingQueued -= OnRecordingQueued;
            observed = next;
            if (observed != null) observed.RecordingQueued += OnRecordingQueued;
        }

        private void OnRecordingQueued(RecordingResult result)
        {
            if (finished || string.IsNullOrWhiteSpace(result.RecordingId)) return;
            expectedRecordingId = result.RecordingId;
            queuedAt = Time.realtimeSinceStartupAsDouble;
            Debug.Log($"P4_MEDIA_PRODUCT_QUEUED recording={result.RecordingId} subject={result.SubjectId} q={result.Quality}");
        }

        private void OnDisable()
        {
            if (observed != null) observed.RecordingQueued -= OnRecordingQueued;
            observed = null;
        }
    }
}
