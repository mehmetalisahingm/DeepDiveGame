using DeepDive.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #312: at night the shore-to-arch trail (#298) is the way out and the way home, but the pieces vanish in the dark
    // water. Each trail piece and the arch's two feet get a faint blue-green glow, as bioluminescent plankton gathers on
    // coral, so from one glow the next one shows. The light follows the shared replicated day like the town lanterns:
    // off by day, fading in at dusk. Pure presentation on every process; no collider, no shadow, nothing to collect, and
    // the water's fog, shader and weather are untouched.
    public static class ReefNightGlow
    {
        public const string RootName = "P312_ReefNightGlow";
        public const float Range = 3.5f, NightIntensity = 1.6f, Lift = 0.6f;
        public static readonly Color Glow = new Color(0.35f, 0.95f, 0.85f);

        // Trail pieces in order from the wade, then the arch's two feet (the mouth).
        public static Vector3[] Points()
        {
            var trail = ReefHabitatVisuals.ArchTrail;
            var points = new Vector3[trail.Length + 2];
            for (var i = 0; i < trail.Length; i++) points[i] = trail[i].Position + Vector3.up * Lift;
            var feet = 0;
            foreach (var solid in SeabedProfile.Solids)
            {
                if (solid.KitId != ReefHabitatVisuals.ArchId || solid.Lift > 0f || feet >= 2) continue;
                var box = solid.Box;
                // On the mouth side of each foot, at the opening's floor.
                points[trail.Length + feet++] = new Vector3(box.center.x + (box.center.x < ReefHabitatVisuals.ArchX ? 1.2f : -1.2f),
                    SeabedProfile.FloorHeight(box.center.x, box.min.z) + Lift, box.min.z - 0.4f);
            }
            return points;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode) => Build(scene);

        public static void Build(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != ContinuousTownLayout.SceneName) return;
            foreach (var existing in scene.GetRootGameObjects())
                if (existing.name == RootName) return;
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            var points = Points();
            var lights = new Light[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var go = new GameObject("NightGlow_" + i.ToString("00"));
                go.transform.SetParent(root.transform, false);
                go.transform.position = points[i];
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Glow;
                light.range = Range;
                light.shadows = LightShadows.None;
                light.intensity = 0f;
                light.enabled = false;
                lights[i] = light;
            }
            root.AddComponent<Driver>().Lights = lights;
            Debug.Log("P312_REEF_NIGHT_GLOW_READY points=" + points.Length);
        }

        public static float IntensityFor(float night01) => NightIntensity * Mathf.Clamp01((night01 - 0.3f) / 0.5f);

        public static bool NeedsLightUpdate(float previous, float intensity) =>
            previous < 0f || (previous > .02f) != (intensity > .02f) ||
            Mathf.Abs(intensity - previous) >= .01f;

        private sealed class Driver : MonoBehaviour
        {
            public Light[] Lights;
            private float next, current = -1f;

            private void Update()
            {
                if (Time.unscaledTime < next) return;
                next = Time.unscaledTime + 0.5f;
                var intensity = IntensityFor(SharedDaylight.Night());
                if (!NeedsLightUpdate(current, intensity)) return;
                current = intensity;
                foreach (var light in Lights)
                {
                    if (light == null) continue;
                    light.intensity = intensity;
                    light.enabled = intensity > 0.02f;
                }
            }

            private void OnDisable()
            {
                if (Lights != null)
                    foreach (var light in Lights) if (light != null) light.enabled = false;
                current = -1f;
                next = 0f;
            }
        }
    }
}
