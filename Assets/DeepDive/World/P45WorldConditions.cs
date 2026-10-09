using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // World-owned source consumed by CrewEnvironmentBinding on the host. No network writes,
    // player transforms, day advances or duplicate route state live here.
    [DisallowMultipleComponent]
    public sealed class P45WorldConditions : MonoBehaviour, ICrewCurrentSource, ICrewWeatherAdvisorySource
    {
        private DiveRegionField region;
        private DiveDepthBandSet depth;
        private GameObject buoy;
        private Material markerMaterial;
        private float nextLookup;

        public static P45WeatherKind TodaysWeather
        {
            get
            {
                var day = DayLock.StateProvider;
                if (day == null) return P45WeatherKind.Calm;
                var state = day();
                return P45WorldRules.Weather(state.DayNumber, state.WeatherSeed);
            }
        }

        public static bool CanDepart(string routeId) =>
            P45WorldRules.CanDepart(routeId, TodaysWeather);

        public CrewEnvironmentAdvisory CurrentAdvisory => P45WorldRules.Advisory(TodaysWeather);

        public bool TrySampleCurrent(Vector3 worldPosition, out CrewCurrentSample sample)
        {
            sample = default;
            if (region == null || depth == null) return false;
            var underwater = depth.DepthAt(worldPosition.y) > 0.3f;
            return P45WorldRules.TryCurrent(region.Bounds, worldPosition, underwater, TodaysWeather, out sample);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextLookup) return;
            nextLookup = Time.unscaledTime + 1f;

            if (!DiveRegionField.TryFind(out var found))
            {
                region = null;
                depth = null;
                ClearMarker();
                return;
            }
            if (region != found)
            {
                region = found;
                depth = FindFirstObjectByType<DiveDepthBandSet>();
                ClearMarker();
            }
            if (depth == null) depth = FindFirstObjectByType<DiveDepthBandSet>();
            if (region == null || depth == null) return;
            if (buoy == null) CreateMarker();

            if (region.TryMapToWorld(P45WorldRules.CurrentMapPosition, out var xz) && buoy != null)
                buoy.transform.position = new Vector3(xz.x, depth.SurfaceY + 0.22f, xz.y);
        }

        private void CreateMarker()
        {
            // A small visible yellow buoy at the current: both players see the same
            // authored map coordinate without revealing any living creature position.
            buoy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buoy.name = "P45MarkedCurrentBuoy";
            buoy.transform.SetParent(transform, false);
            buoy.transform.localScale = new Vector3(0.65f, 0.12f, 0.65f);
            var collider = buoy.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            markerMaterial = new Material(shader) { name = "P45CurrentMarkerMaterial", color = new Color(1f, 0.75f, 0.12f) };
            var renderer = buoy.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = markerMaterial;
        }

        private void ClearMarker()
        {
            if (buoy != null) Destroy(buoy);
            if (markerMaterial != null) Destroy(markerMaterial);
            buoy = null;
            markerMaterial = null;
        }

        private void OnDisable() => ClearMarker();
        private void OnDestroy() => ClearMarker();
    }
}
