using System;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P1.Lab
{
    // Acceptance guard for -p4-explore. Unlike the legacy #96 smoke fixture this never constructs
    // an exploration authority and never calls AcceptSighting/AcceptRecording/AcceptCatch. It drives
    // a real owner input toward a real fish and requires the shipping ExplorationNetworkBinding to
    // discover a real cell and accept that host-verified sighting. Any failure is an Error, which the
    // existing IntegratedSmokeDriver already records and turns into a non-zero smoke result.
    public sealed class P4ExplorationProductProbe : MonoBehaviour
    {
        private SessionNetworkAdapter adapter;
        private float diveStartedAt;
        private bool done;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p1-integrated").Length == 0 || Arg("-p4-explore") != "1") return;
            if (FindFirstObjectByType<P4ExplorationProductProbe>() != null) return;
            var go = new GameObject("P4ExplorationProductProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<P4ExplorationProductProbe>();
        }

        private static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : string.Empty;
        }

        private void LateUpdate()
        {
            if (done) return;
            if (adapter == null) adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            if (adapter == null || adapter.Connection.IsSceneLoading) return;
            if (SceneManager.GetActiveScene().name != SessionNetworkAdapter.DiveScene ||
                adapter.Session.State.Phase != SessionPhase.Dive)
            {
                diveStartedAt = 0;
                return;
            }

            if (diveStartedAt == 0) diveStartedAt = Time.realtimeSinceStartup;

            if (!adapter.IsAuthority)
            {
                // The existing network smoke still owns the client-side mirror assertions. This guard's
                // job is specifically to make the host prove the product authority path instead of a fixture.
                return;
            }

            var binding = adapter.GetComponent<ExplorationNetworkBinding>();
            var local = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
                .FirstOrDefault(p => p.IsOwner && p.IsSpawned);
            var fish = FindObjectsByType<FishActor>(FindObjectsSortMode.None)
                .Where(f => f != null && f.IsSpawned && !f.IsDead && f.Species != null)
                .OrderBy(f => local == null ? float.MaxValue :
                    (f.transform.position - local.transform.position).sqrMagnitude)
                .FirstOrDefault();

            if (binding != null && binding.IsBound && binding.OwnsSavePersistence && local != null && fish != null)
            {
                DriveRealOwnerToward(local, fish);

                var discoveredCell = false;
                var snapshot = binding.Cells?.Snapshot();
                if (snapshot != null)
                    for (var i = 0; i < snapshot.Cells.Count; i++)
                        if (snapshot.Cells[i].Discovered) { discoveredCell = true; break; }

                if (discoveredCell && binding.Species != null &&
                    binding.Species.TryGetSpecies(fish.Species.SpeciesId, out var observation) && observation.Sighted)
                {
                    done = true;
                    Debug.Log($"P4_EXPLORE_PRODUCT_OK species={fish.Species.SpeciesId} cellCount={snapshot.Cells.Count}");
                    return;
                }
            }

            // The integrated Explore scenario returns at ~14 seconds of dive time. Fail early enough
            // that the error is guaranteed to be captured by IntegratedSmokeDriver before scene return.
            if (Time.realtimeSinceStartup - diveStartedAt > 11f)
            {
                done = true;
                Debug.LogError($"P4_EXPLORE_PRODUCT_FAIL bound={binding != null && binding.IsBound} " +
                    $"save={binding != null && binding.OwnsSavePersistence} player={local != null} fish={fish != null}");
            }
        }

        private static void DriveRealOwnerToward(NetworkPlayer local, FishActor fish)
        {
            var target = fish.GetComponent<Collider>() != null
                ? fish.GetComponent<Collider>().bounds.center
                : fish.transform.position;
            var delta = target - local.RecordingEyePosition;
            if (delta.sqrMagnitude <= 0.0001f) return;

            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            var move = delta.magnitude > 8f
                ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * delta.normalized
                : Vector3.zero;
            local.SubmitLocalInput(move, yaw, pitch);
        }
    }
}
