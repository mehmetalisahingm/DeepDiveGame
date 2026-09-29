using System;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Media;
using DeepDive.Session;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #100 acceptance guard. It creates no clip, manifest or recording fixture. The existing real -Record
    // smoke drives owner input through RecordingNetworkBridge; this only requires the shipping capture binding
    // to turn the host-verified/safely-returned recording into bytes, archive metadata and playable local media.
    // A Debug.LogError is consumed by IntegratedSmokeDriver and makes the run fail.
    public sealed class P4MediaProductProbe : MonoBehaviour
    {
        private SessionNetworkAdapter adapter;
        private float firstDiveAt;
        private bool sawRealRecordingPresentation;
        private bool finished;

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

            var state = adapter.Session.State;
            if (state.Phase == SessionPhase.Dive)
            {
                if (firstDiveAt == 0) firstDiveAt = Time.realtimeSinceStartup;
                var players = FindObjectsByType<DeepDive.Network.NetworkPlayer>(FindObjectsSortMode.None);
                if (players.Any(p => p.IsSpawned && p.IsServer && p.RecordingPresentation.Value))
                    sawRealRecordingPresentation = true;
                return;
            }

            if (firstDiveAt == 0) return;
            var data = MediaNetworkBinding.Mirrored;
            var real = data?.Clips?.FirstOrDefault(c => c != null && !string.IsNullOrWhiteSpace(c.RecordingId) &&
                c.ClipId == RecordingClipFile.ClipIdForRecording(c.RecordingId));
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

            // -Record returns around 44s and exits around 60s. Fail while the result driver is still alive.
            if (Time.realtimeSinceStartup - firstDiveAt > 34f && state.Phase != SessionPhase.Dive)
            {
                finished = true;
                Debug.LogError($"P4_MEDIA_PRODUCT_FAIL presentation={sawRealRecordingPresentation} clips={data?.Clips?.Count ?? 0} " +
                    $"real={(real != null)} playable={(real != null && ClipPlayback.CanPlay(real.ClipId))}");
            }
        }
    }
}
