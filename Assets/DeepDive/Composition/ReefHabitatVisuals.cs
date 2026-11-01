using System;
using System.Collections.Generic;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #219: deterministic, purely visual first pass over the authored reef shelf and
    // deep basin. The scene's existing slopes, physics, fish and boat paths remain
    // authoritative; no terrain, colliders, rewards or network objects are created.
    public static class ReefHabitatVisuals
    {
        public const string RootName = "P411_ReefHabitatVisuals";

        public readonly struct Spot
        {
            public readonly string PrefabId;
            public readonly Vector3 Position;
            public readonly float Yaw;

            public Spot(string prefabId, Vector3 position, float yaw)
            {
                PrefabId = prefabId;
                Position = position;
                Yaw = yaw;
            }
        }

        // DiveTestAreaReefDeepSetup: reef shelf top = -8 (z 19..40), deep
        // basin top = -26 (z 46..65); wall interior x -14.75..14.75.
        // Western reef positions avoid the snapper's eastern spawn/wander row
        // (x 3.5..8); eastern deep positions avoid the tuna's western row
        // (x -7..-1) and the research boat column centered at x=0,z=56.
        // These are visual placements, not proven player-path clearances.
        public static readonly IReadOnlyList<Spot> Spots = Array.AsReadOnly(new[]
        {
            new Spot("reef-coral-branching", new Vector3(-11.5f, -8f, 27.5f), 35f),
            new Spot("reef-coral-fan",       new Vector3(-12f, -8f, 31f), 95f),
            new Spot("reef-coral-mound",     new Vector3(-11.5f, -8f, 34f), 70f),
            new Spot("reef-plant-seagrass",  new Vector3(-9.2f, -8f, 30f), 10f),
            new Spot("reef-rock-boulder",   new Vector3(-12f, -8f, 38f), 55f),
            new Spot("reef-rock-arch",      new Vector3(-9.8f, -8f, 38f), 110f),
            new Spot("reef-rock-boulder",   new Vector3(10f, -26f, 50f), 140f),
            new Spot("reef-coral-fan",      new Vector3(11f, -26f, 54f), 60f),
            new Spot("reef-coral-mound",    new Vector3(11.5f, -26f, 58f), 10f),
            new Spot("reef-plant-seagrass", new Vector3(10f, -26f, 62f), 80f)
        });

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Build(scene);
        }

        public static void Build(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != ContinuousTownLayout.SceneName) return;
            foreach (var existing in scene.GetRootGameObjects())
                if (existing.name == RootName) return;

            // Resolve and validate the entire kit before creating a partial world.
            var prefabs = new GameObject[Spots.Count];
            for (var i = 0; i < Spots.Count; i++)
            {
                prefabs[i] = ReefKitPiece.Load(Spots[i].PrefabId);
                if (prefabs[i] == null || prefabs[i].GetComponent<ReefKitPiece>() == null ||
                    prefabs[i].GetComponentInChildren<Collider>(true) != null ||
                    prefabs[i].GetComponentInChildren<NetworkObject>(true) != null)
                {
                    Debug.LogError("P411_REEF_PREVIEW_INVALID_PREFAB index=" + i + " id=" + Spots[i].PrefabId);
                    return;
                }
            }

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            for (var i = 0; i < Spots.Count; i++)
            {
                var spot = Spots[i];
                var visual = UnityEngine.Object.Instantiate(prefabs[i], root.transform, false);
                visual.name = "ReefVisual_" + i.ToString("00") + "_" + spot.PrefabId;
                visual.transform.SetPositionAndRotation(spot.Position, Quaternion.Euler(0f, spot.Yaw, 0f));
            }
            Debug.Log("P411_REEF_VISUALS_READY count=" + Spots.Count + " reef=6 deep=4");
        }
    }
}
