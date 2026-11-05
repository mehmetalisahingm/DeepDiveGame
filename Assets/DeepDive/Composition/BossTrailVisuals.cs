using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #258: the wreck debris trail from the reef's deep edge to the boss arena, the same on every process. Positions
    // are DeepEncounterWorld.TrailPoints on the #219 bed; the pieces are the accepted shore kit's broken planks and
    // cargo, collider-free, so the trail is a sight line and never a wall. No ids, no triggers, no rewards: the
    // traces and the arena stay the only progression evidence.
    public static class BossTrailVisuals
    {
        public const string RootName = "P258_BossTrailVisuals";

        // How far a piece sits into the sand, the same as the reef kit.
        public const float BedOverlap = 0.15f;

        // One kit piece per trail point, in TrailPoints order: broken dock planks near the reef, then cargo that
        // fell further down, then the anchor where the wreck's line points at the arena.
        public static readonly string[] PieceIds =
        {
            "shore-dock-broken", "shore-prop-barrel", "shore-prop-chest",
            "shore-prop-barrel", "shore-dock-broken", "shore-prop-anchor"
        };

        private static readonly float[] Yaws = { 25f, 80f, 140f, 200f, 300f, 15f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode) => Build(scene);

        public static Vector3 PositionOf(int index)
        {
            var point = DeepEncounterWorld.TrailPoints[index];
            return DeepEncounterWorld.OnBed(point.x, point.y, -BedOverlap);
        }

        public static void Build(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != ContinuousTownLayout.SceneName) return;
            foreach (var existing in scene.GetRootGameObjects())
                if (existing.name == RootName) return;

            // Resolve and validate the whole trail before creating any of it.
            var prefabs = new GameObject[PieceIds.Length];
            for (var i = 0; i < PieceIds.Length; i++)
            {
                prefabs[i] = ShoreKitPiece.Load(PieceIds[i]);
                if (prefabs[i] == null || prefabs[i].GetComponent<ShoreKitPiece>() == null ||
                    prefabs[i].GetComponentInChildren<Collider>(true) != null ||
                    prefabs[i].GetComponentInChildren<NetworkObject>(true) != null)
                {
                    Debug.LogError("P258_TRAIL_INVALID_PREFAB index=" + i + " id=" + PieceIds[i]);
                    return;
                }
            }

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            for (var i = 0; i < PieceIds.Length; i++)
            {
                var piece = Object.Instantiate(prefabs[i], root.transform, false);
                piece.name = "BossTrail_" + i.ToString("00") + "_" + PieceIds[i];
                piece.transform.SetPositionAndRotation(PositionOf(i), Quaternion.Euler(0f, Yaws[i], 0f));
            }
            Debug.Log("P258_BOSS_TRAIL_READY count=" + PieceIds.Length);
        }
    }
}
