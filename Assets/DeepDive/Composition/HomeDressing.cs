using DeepDive.Economy;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #259: the shared house's art. The home rig (HomeSceneSetup) keeps the physical targets (beds, storage, PC) and the town data keeps the walls;
    // this places the baked, collider-free dressing prefab (Resources/CityKit/SharedHouse) over them: plaster and tile outside, a pitched roof,
    // a porch with a sign and lanterns, a wood floor, wainscot, windows with curtains, pendant lamps, rug, a pennant/gear wall, a lifebuoy, the PC
    // corner with its monitor and one coloured blanket per bed. Visual only: it never adds a collider, a NetworkObject or an interaction, so no
    // route, bed, storage or PC behaviour can change. Idempotent, and the same on host and guest (it is scene art, not state).
    public static class HomeDressing
    {
        public const string Resource = "CityKit/SharedHouse";
        public const string RootName = "SharedHouseDressing";
        public const string MissingCode = "P259_HOME_DRESSING_MISSING";

        public static GameObject Place(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            var existing = HomeSceneSetup.FindRoot(scene, RootName);
            if (existing != null) return existing;
            var prefab = Resources.Load<GameObject>(Resource);
            if (prefab == null)
            {
                Debug.LogError(MissingCode + " " + Resource);
                return null;
            }
            var house = Object.Instantiate(prefab);
            house.name = RootName;
            SceneManager.MoveGameObjectToScene(house, scene);
            house.transform.SetPositionAndRotation(ContinuousTownLayout.HomeOrigin, Quaternion.identity);
            return house;
        }
    }

    // The shared night (0 day .. 1 night) from the already replicated day, the same source the town lanterns and the coast sky read.
    public static class SharedDaylight
    {
        private static EconomyPlayerSync source;

        public static float Night()
        {
            if (source == null || !source.IsSpawned)
            {
                source = null;
                foreach (var sync in Object.FindObjectsByType<EconomyPlayerSync>(FindObjectsSortMode.None))
                {
                    if (!sync.IsSpawned) continue;
                    if (source == null) source = sync;
                    if (sync.IsOwner) { source = sync; break; }
                }
            }
            if (source == null) return 0f;
            return DaylightModel.Evaluate(source.DayClockMinute.Value, (DayPhase)source.DayPhaseValue.Value, source.DayWeatherSeed.Value).NightFactor;
        }
    }
}
