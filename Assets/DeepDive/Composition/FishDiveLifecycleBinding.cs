using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // Restores the previous per-trip scene lifecycle without reloading/teleporting the world.
    // Reuses only authored fish, not a new species/spawn authority or a test stock provider.
    public sealed class FishDiveLifecycleBinding : MonoBehaviour
    {
        private readonly Dictionary<FishActor, Pose> authored = new Dictionary<FishActor, Pose>();
        private SessionNetworkAdapter adapter;
        private string lastDive = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= AddBinding;
            SceneManager.sceneLoaded += AddBinding;
        }

        private static void AddBinding(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<FishDiveLifecycleBinding>() == null)
                session.gameObject.AddComponent<FishDiveLifecycleBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            adapter.Session.OnSessionStateChanged += StateChanged;
            SceneManager.sceneLoaded += Capture;
            Capture(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void Capture(Scene scene, LoadSceneMode mode)
        {
            authored.Clear();
            lastDive = "";
            if (scene.name != ContinuousTownLayout.SceneName) return;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var fish in root.GetComponentsInChildren<FishActor>(true))
                authored.Add(fish, new Pose(fish.transform.position, fish.transform.rotation));
        }

        private void StateChanged(SessionState state)
        {
            if (state.Revision == 0) { lastDive = ""; return; }
            if (!adapter.IsAuthority || adapter.Connection.IsSceneLoading || state.Phase != SessionPhase.Dive ||
                string.IsNullOrEmpty(state.DiveId) || state.DiveId == lastDive) return;
            var first = string.IsNullOrEmpty(lastDive);
            lastDive = state.DiveId;
            if (first) return; // The scene's initial NGO spawn is already the first population.
            foreach (var pair in authored)
            {
                var fish = pair.Key;
                if (fish == null) continue;
                var net = fish.NetworkObject;
                if (net == null || net.IsSceneObject != true) continue;
                if (net.IsSpawned) net.Despawn(false);
                fish.transform.SetPositionAndRotation(pair.Value.position, pair.Value.rotation);
                net.Spawn(true);
                Debug.Log($"P46_FISH_NEXT_DIVE id={net.NetworkObjectId} dive={state.DiveId}");
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= Capture;
            if (adapter != null && adapter.Session != null) adapter.Session.OnSessionStateChanged -= StateChanged;
        }
    }
}
