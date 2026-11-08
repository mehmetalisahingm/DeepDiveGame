using UnityEngine;

namespace DeepDive.Composition
{
    // #171/#162: the town's baked, collider-free dressing (Resources/CityKit/TownDressing, built by DeepDive > P4.10 > Build town dressing in WORLD
    // coordinates from the layout's own constants): fence posts and rails on every edge block, cobble paving, street lamps with night lanterns, bunting,
    // palms, crates, barrels, benches and planters. Placed under the town root by ContinuousTownLayout.Build, so the town's cleanup removes it too.
    // The edge blocks keep their collider and size; only their colour changes (from the building plaster to weathered planks), so no walk,
    // fall-off or route measurement can change. Idempotent; a missing prefab is logged and the bare town still works.
    public static class TownDressing
    {
        public const string Resource = "CityKit/TownDressing";
        public const string Name = "TownDressing";
        public const string FenceMaterial = "CityKit/Materials/Home_FencePlanks";
        public const string MissingCode = "P171_TOWN_DRESSING_MISSING";

        public static GameObject Place(Transform townRoot)
        {
            if (townRoot == null) return null;
            var existing = townRoot.Find(Name);
            if (existing != null) return existing.gameObject;
            var prefab = Resources.Load<GameObject>(Resource);
            if (prefab == null)
            {
                Debug.LogError(MissingCode + " " + Resource);
                return null;
            }
            var dressing = Object.Instantiate(prefab, townRoot, false);
            dressing.name = Name;
            var planks = Resources.Load<Material>(FenceMaterial);
            if (planks != null)
                foreach (Transform child in townRoot)
                    if (child.name.StartsWith("Edge_") && child.TryGetComponent<Renderer>(out var renderer)) renderer.sharedMaterial = planks;
            return dressing;
        }
    }
}
