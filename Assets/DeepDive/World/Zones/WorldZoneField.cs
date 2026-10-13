using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World
{
    // Scene shell for PlayerZoneRules (#138): the zone volumes under this object plus the scene's
    // one WaterField. It answers queries and holds no per-player state; it is not a water authority
    // either - without a WaterField nothing resolves to Sea rather than guessing one.
    [DisallowMultipleComponent]
    public sealed class WorldZoneField : MonoBehaviour, IPlayerZoneSource
    {
        private static readonly IReadOnlyList<WaterBody> NoWater = new WaterBody[0];

        [SerializeField] private WaterField water;

        private readonly List<ZoneVolumeEntry> entries = new List<ZoneVolumeEntry>();
        private bool built;

        public WaterField Water => water;

        public IReadOnlyList<ZoneVolumeEntry> Volumes
        {
            get
            {
                EnsureBuilt();
                return entries;
            }
        }

        public void Configure(WaterField sceneWater)
        {
            water = sceneWater;
            built = false;
        }

        // For a shell that adds, removes or moves volumes after the first query.
        public void RebuildVolumes()
        {
            built = false;
            EnsureBuilt();
        }

        public PlayerZone ResolveZone(Vector3 feetPosition)
        {
            EnsureBuilt();
            return PlayerZoneRules.Resolve(feetPosition, water != null ? water.Bodies : NoWater, entries);
        }

        // Exactly one active field across the loaded scenes, or nothing.
        public static bool TryFind(out IPlayerZoneSource source)
        {
            source = null;
            WorldZoneField only = null;
            for (var s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var field in root.GetComponentsInChildren<WorldZoneField>(false))
                {
                    if (!field.gameObject.activeInHierarchy) continue;
                    if (only != null) return false;
                    only = field;
                }
            }
            source = only;
            return only != null;
        }

        private void EnsureBuilt()
        {
            if (built) return;
            built = true;
            entries.Clear();
            foreach (var volume in GetComponentsInChildren<WorldZoneVolume>(false))
                if (volume.TryGetEntry(out var entry)) entries.Add(entry);
        }
    }
}
