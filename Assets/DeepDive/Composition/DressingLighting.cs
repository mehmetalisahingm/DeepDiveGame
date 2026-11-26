using UnityEngine;

namespace DeepDive.Composition
{
    // Day and night for a baked dressing prefab (the shared house, the town): the windows go from bright sky to dark blue, lamps and lanterns light up, and so does
    // the PC screen. No clock of its own; Apply is public so a test can drive both ends.
    public sealed class DressingLighting : MonoBehaviour
    {
        public const float Poll = 0.5f;
        public Renderer[] Panes = new Renderer[0];
        public Renderer[] Bulbs = new Renderer[0];
        public Renderer[] Screens = new Renderer[0];
        public Light[] Lamps = new Light[0];
        public Material PaneDay, PaneNight, BulbOn, BulbOff, ScreenDay, ScreenNight;
        public float LampIntensity = 1.5f;

        private float next;
        private float shown = -1f;

        public float Night => shown;

        private void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Poll;
            Apply(SharedDaylight.Night());
        }

        public void Apply(float night)
        {
            night = Mathf.Clamp01(night);
            var first = shown < 0f;
            var dark = night > .5f;
            var bulb = night > .3f;
            var lit = night > .02f;
            var darkChanged = first || dark != (shown > .5f);
            var bulbChanged = first || bulb != (shown > .3f);
            var lampChanged = first || lit != (shown > .02f);
            // Do not miss a threshold when the clock changes by less than
            // the normal intensity update tolerance.
            if (!darkChanged && !bulbChanged && !lampChanged && Mathf.Abs(night - shown) < .01f) return;
            shown = night;
            if (darkChanged)
            {
                Swap(Panes, dark ? PaneNight : PaneDay);
                Swap(Screens, dark ? ScreenNight : ScreenDay);
            }
            if (bulbChanged) Swap(Bulbs, bulb ? BulbOn : BulbOff);
            foreach (var lamp in Lamps)
            {
                if (lamp == null) continue;
                lamp.intensity = LampIntensity * night;
                lamp.enabled = lit;
            }
        }

        private void OnDisable()
        {
            // A disabled driver must not leave active lights behind in the scene.
            foreach (var lamp in Lamps)
            {
                if (lamp == null) continue;
                lamp.enabled = false;
            }
            shown = -1f;
            next = 0f;
        }

        private static void Swap(Renderer[] renderers, Material material)
        {
            if (material == null) return;
            foreach (var renderer in renderers) if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
