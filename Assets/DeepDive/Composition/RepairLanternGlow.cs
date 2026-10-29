using UnityEngine;

namespace DeepDive.Composition
{
    // The repair worksite's lanterns light the crew's work at dusk and night (#188 item 5). It reads the scene's own ambient
    // (CoastalAtmosphere scales it with the shared daylight), so it needs no clock, network or save state: bright day -> off.
    [RequireComponent(typeof(Light))]
    public sealed class RepairLanternGlow : MonoBehaviour
    {
        public float FullIntensity = 3f;
        // Ambient brightness (grayscale) at which the light starts and is full: day is ~0.68, night ~0.17.
        public const float OffAbove = .5f, FullBelow = .25f;
        private Light lamp;

        public static float Darkness(Color ambient) => Mathf.InverseLerp(OffAbove, FullBelow, ambient.grayscale);

        public void Refresh()
        {
            if (lamp == null) lamp = GetComponent<Light>();
            var dark = Darkness(RenderSettings.ambientLight);
            lamp.intensity = FullIntensity * dark;
            lamp.enabled = dark > .01f;
        }

        private void Update() => Refresh();
    }
}
