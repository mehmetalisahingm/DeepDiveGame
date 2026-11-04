using System;
using System.Collections.Generic;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #219: the reef kit's placement on the natural sea bed, the same on every process. Heights come from
    // SeabedProfile, and the solid pieces get SeabedProfile's boxes as colliders, so what a diver bumps into is what
    // the host's fish bounds steer around. Everything else is decor a diver and a fish pass through. No rewards, no
    // network objects; the prefabs themselves stay collider-free.
    public static class ReefHabitatVisuals
    {
        public const string RootName = "P411_ReefHabitatVisuals";
        public const string SolidPrefix = "ReefSolid_";

        // How far a piece sits into the bed: the 1 m terrain mesh runs a little off the profile round the outcrops.
        public const float BedOverlap = 0.15f;

        public const string ArchId = "reef-rock-arch";
        public const float ArchX = -7f, ArchZ = 30f, ArchScale = 1.5f;

        public readonly struct Spot
        {
            public readonly string PrefabId;
            public readonly Vector3 Position;
            public readonly float Yaw, Scale;

            public Spot(string prefabId, float x, float z, float yaw, float scale = 1f)
            {
                PrefabId = prefabId;
                Position = new Vector3(x, SeabedProfile.FloorHeight(x, z) - BedOverlap, z);
                Yaw = yaw;
                Scale = scale;
            }
        }

        private const string Fan = "reef-coral-fan", Mound = "reef-coral-mound", Seagrass = "reef-plant-seagrass";

        // No collider. Low pieces only over the eastern shelf, under the snappers' wander set.
        private static readonly Spot[] Decor =
        {
            new Spot(Seagrass, 8.5f, 5f, 10f), new Spot(Seagrass, 12f, 10f, 80f), new Spot(Seagrass, -13.5f, 13.5f, 200f),
            new Spot(Fan, -9.5f, 22.6f, 95f), new Spot(Mound, -5.5f, 23f, 70f), new Spot(Seagrass, -4f, 24.5f, 30f),
            new Spot(Fan, -13f, 26.5f, 35f), new Spot(Mound, -8f, 25.8f, 140f),
            new Spot(Seagrass, -8.6f, 32f, 10f), new Spot(Seagrass, -11.2f, 29f, 260f), new Spot(Mound, -2.9f, 30.8f, 20f),
            new Spot(Fan, -5f, 32.2f, 60f), new Spot(Mound, -6.5f, 34.5f, 310f), new Spot(Seagrass, -2f, 36f, 150f),
            new Spot(Fan, -3.6f, 36.2f, 15f), new Spot(Fan, -4.2f, 37.6f, 110f),
            new Spot(Mound, 6f, 27f, 45f), new Spot(Mound, 9f, 33f, 200f), new Spot(Mound, 3f, 34f, 120f),
            new Spot(Seagrass, 13f, 27f, 75f), new Spot(Fan, 10f, 20.5f, 130f), new Spot(Mound, 9f, 23f, 250f),
            new Spot(Fan, 11f, 54f, 60f), new Spot(Mound, 11.5f, 58f, 10f), new Spot(Seagrass, 10f, 62f, 80f)
        };

        public static readonly IReadOnlyList<Spot> Spots = Array.AsReadOnly(BuildSpots());

        private static Spot[] BuildSpots()
        {
            var spots = new List<Spot>(Decor);
            spots.Add(new Spot(ArchId, ArchX, ArchZ, 0f, ArchScale));
            for (var i = 0; i < SeabedProfile.Solids.Count; i++)
            {
                var solid = SeabedProfile.Solids[i];
                // The arch's three boxes share the one visual above. A small turn keeps the rocks from reading as a
                // row, and stays small so the box still covers the piece.
                if (solid.KitId != ArchId) spots.Add(new Spot(solid.KitId, solid.X, solid.Z, i * 37 % 30 - 15, solid.Scale));
            }
            return spots.ToArray();
        }

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
                visual.transform.localScale = Vector3.one * spot.Scale;
            }

            for (var i = 0; i < SeabedProfile.Solids.Count; i++)
            {
                var box = SeabedProfile.Solids[i].Box;
                var solid = new GameObject(SolidPrefix + i.ToString("00")) { layer = 0 };
                solid.transform.SetParent(root.transform, false);
                solid.transform.position = box.center;
                solid.AddComponent<BoxCollider>().size = box.size;
            }

            Debug.Log("P411_REEF_VISUALS_READY count=" + Spots.Count + " solids=" + SeabedProfile.Solids.Count);
        }
    }
}
