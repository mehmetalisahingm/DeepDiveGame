using System;
using UnityEngine;

namespace DeepDive.World
{
    // The scene's one region: where the map's 0..1 square sits in the world. The arithmetic is
    // DiveRegionBounds'; this is only the authored extents plus the lookup that finds them, the
    // way DiveDepthBandSet carries DiveDepthBands' numbers rather than a second copy of the maths.
    //
    // One mount, one owner (docs/plan/CONTRACTS.md "Harita ve kalici kesif": the world<->map
    // transform is one person's and is never repeated in the UI). A second DiveRegionField in a
    // scene would be a second answer to "where is this on the map", so TryFind refuses rather
    // than picking one.
    //
    // regionId is a label for logs and the inspector only. It is deliberately NOT in Core: there
    // is exactly one region in P3.3, nothing persists it, and adding an id to a contract three
    // people share for the sake of a debug string would be a change nobody needs yet.
    [DisallowMultipleComponent]
    public sealed class DiveRegionField : MonoBehaviour
    {
        [SerializeField] private string regionId = "region-near-1";

        // DiveTestArea's arena. SwimVolume's footprint is exactly this, the walls' inner faces
        // sit at +-14.75 and the beach ends at +-14.75, so every reachable position is inside and
        // a refusal always means a real mistake. The scene script authors these and the scene
        // test pins them; they are serialized rather than hard-coded so a second region (P4.3)
        // needs no code change.
        [SerializeField] private float minX = -15f;
        [SerializeField] private float maxX = 15f;
        [SerializeField] private float minZ = -15f;
        [SerializeField] private float maxZ = 15f;

        public string RegionId => regionId;

        public DiveRegionBounds Bounds => new DiveRegionBounds(minX, maxX, minZ, maxZ);

        public bool TryWorldToMap(Vector3 world, out Vector2 map) => Bounds.TryWorldToMap(world, out map);

        public bool TryMapToWorld(Vector2 map, out Vector2 worldXZ) => Bounds.TryMapToWorld(map, out worldXZ);

        // The loud form, for the scene script and its tests: authoring a dock outside its own
        // region is a mistake to stop on, not a false to ignore. Runtime callers use the bool
        // form above - a throw inside a per-frame map update would be worse than a missing icon.
        public Vector2 WorldToMapChecked(Vector3 world)
        {
            if (TryWorldToMap(world, out var map)) return map;
            throw new InvalidOperationException(
                $"P3_ROUTE_OUT_OF_REGION region={regionId} world={world} " +
                $"bounds=x[{minX},{maxX}] z[{minZ},{maxZ}]");
        }

        // Editor setup only, like BoatPartAnchor.Configure.
        public void Configure(string id, float regionMinX, float regionMaxX, float regionMinZ, float regionMaxZ)
        {
            regionId = id ?? string.Empty;
            minX = regionMinX;
            maxX = regionMaxX;
            minZ = regionMinZ;
            maxZ = regionMaxZ;
        }

        public static bool TryFind(out DiveRegionField region)
        {
            region = null;

            var found = FindObjectsByType<DiveRegionField>(FindObjectsSortMode.None);
            for (var i = 0; i < found.Length; i++)
            {
                if (region != null)
                {
                    Debug.LogError(
                        $"P3_ROUTE_DUPLICATE_REGION first={region.name} second={found[i].name} " +
                        "reason=a scene has exactly one region");
                    region = null;
                    return false;
                }

                region = found[i];
            }

            return region != null;
        }

        // Inverted or zero-sized extents would refuse every conversion at runtime, which reads as
        // "everything is outside the world" - a failure worth catching while the scene is open
        // rather than in a map that silently draws nothing.
        private void OnValidate()
        {
            if (Bounds.IsValid) return;
            Debug.LogError(
                $"P3_ROUTE_REGION_INVALID object={name} bounds=x[{minX},{maxX}] z[{minZ},{maxZ}] " +
                "reason=max must be greater than min on both axes", this);
        }
    }
}
