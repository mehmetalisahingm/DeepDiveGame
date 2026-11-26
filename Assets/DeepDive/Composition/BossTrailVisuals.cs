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
        // Reuse the existing shared daylight; six small, shadowless beacons
        // illuminate wreck surfaces, not the progression/harpoon targets.
        public const float BeaconRange = 2.5f, BeaconLift = .65f, BeaconNightIntensity = .8f;
        public static float BeaconIntensity(float night) =>
            BeaconNightIntensity * Mathf.Clamp01((night - .3f) / .5f);
        public static Vector3 BeaconPosition(int index) => PositionOf(index) + Vector3.up * BeaconLift;
        public static void ApplyBeacon(Light light, float night)
        {
            if (light == null) return;
            light.intensity = BeaconIntensity(night);
            light.enabled = light.intensity > .02f;
        }

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
            var lights = new Light[PieceIds.Length];
            for (var i = 0; i < PieceIds.Length; i++)
            {
                var piece = Object.Instantiate(prefabs[i], root.transform, false);
                piece.name = "BossTrail_" + i.ToString("00") + "_" + PieceIds[i];
                piece.transform.SetPositionAndRotation(PositionOf(i), Quaternion.Euler(0f, Yaws[i], 0f));
                var beacon = new GameObject("WreckGlow_" + i.ToString("00"));
                beacon.transform.SetParent(piece.transform, false);
                beacon.transform.position = BeaconPosition(i);
                var light = beacon.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = BeaconRange;
                light.color = new Color(.45f, .78f, .9f);
                light.shadows = LightShadows.None;
                ApplyBeacon(light, 0f);
                lights[i] = light;
            }
            root.AddComponent<BeaconDriver>().Lights = lights;
            Debug.Log("P258_BOSS_TRAIL_READY count=" + PieceIds.Length);
        }

        private sealed class BeaconDriver : MonoBehaviour
        {
            public Light[] Lights;
            private float next;
            private void Update()
            {
                if (Time.unscaledTime < next || Lights == null) return;
                next = Time.unscaledTime + .5f;
                var night = SharedDaylight.Night();
                foreach (var light in Lights) ApplyBeacon(light, night);
            }
            private void OnDisable()
            {
                if (Lights == null) return;
                foreach (var light in Lights) ApplyBeacon(light, 0f);
                next = 0f;
            }
        }
    }
}
