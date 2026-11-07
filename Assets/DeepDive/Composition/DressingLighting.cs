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
            if (Mathf.Abs(night - shown) < 0.01f) return;
            shown = night;
            var dark = night > 0.5f;
            Swap(Panes, dark ? PaneNight : PaneDay);
            Swap(Bulbs, night > 0.3f ? BulbOn : BulbOff);
            Swap(Screens, dark ? ScreenNight : ScreenDay);
            foreach (var lamp in Lamps)
            {
                if (lamp == null) continue;
                lamp.intensity = LampIntensity * night;
                lamp.enabled = night > 0.02f;
            }
        }

        private static void Swap(Renderer[] renderers, Material material)
        {
            if (material == null) return;
            foreach (var renderer in renderers) if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
