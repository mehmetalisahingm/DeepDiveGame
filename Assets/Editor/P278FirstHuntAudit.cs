using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using DeepDive.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // #278 (Utku): what a new player sees and can reach on the first dive. From each real water entry: which fish homes
    // are in view on entering the water, how long a diver swimming straight out to sea takes to see a fish and to have
    // one in harpoon range, and how long the real controller takes to reach each species' habitat on one tank.
    // Read-only: the scene is opened additively and closed unsaved; fish colliders are off so only scenery occludes.
    public static class P278FirstHuntAudit
    {
        public const string OutputPath = "docs/reports/evidence/p278-first-hunt/first-hunt.json";

        private const float Dt = 0.02f;
        // CoastalAtmosphere underwater: exponential-squared fog, density from 0.03 at the line to 0.065 by 8 m depth.
        private const float NearFog = 0.03f, DeepFog = 0.065f, FogDepth = 8f;
        private const float Readable = 0.5f;          // a fish counts as seen at half visibility or better
        private const float OpenSeaZ = 60f;

        [Serializable] public sealed class Fish { public string species; public Vector3 home; }
        [Serializable] public sealed class Seen
        {
            public string species; public Vector3 home; public float distance, visibility01; public bool lineClear, inView;
        }
        // How large each species' real scene model is, and how many pixels of a 720p / 1080p frame its length takes at the
        // first-sight distances (pixel density from the vertical field of view).
        [Serializable] public sealed class Size
        {
            public string species; public float lengthMetres; public float px720At12, px720At17, px720At22, px1080At12, px1080At22;
        }
        [Serializable] public sealed class Habitat { public string species; public float metres, seconds, tankFraction; public bool stuck; }
        [Serializable] public sealed class Entry
        {
            public string id; public Vector3 eye;
            public List<Seen> atEntry = new List<Seen>();
            public int readableAtEntry;
            public float firstSightSeconds = -1f, firstInRangeSeconds = -1f;
            public string firstSightSpecies = "", firstInRangeSpecies = "";
            public List<Habitat> habitats = new List<Habitat>();
        }
        [Serializable] public sealed class Audit
        {
            public string note = "Static fish homes (fish wander around them), geometric line of sight with the underwater fog formula; no light, contrast or model.";
            public float swimSpeed, maxOxygen, harpoonRange, horizontalFov, verticalFov, surfaceY;
            public List<Fish> fish = new List<Fish>();
            public List<Entry> entries = new List<Entry>();
            public List<Size> sizes = new List<Size>();
        }

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P4RouteAudit.DiverPath);
            var diver = prefab.GetComponent<CharacterController>();
            var cam = prefab.GetComponentInChildren<Camera>(true);
            var player = new SerializedObject(prefab.GetComponent<NetworkPlayer>());
            var audit = new Audit
            {
                swimSpeed = player.FindProperty("swimSpeed").floatValue,
                maxOxygen = player.FindProperty("maxOxygen").floatValue,
                harpoonRange = player.FindProperty("harpoonRange").floatValue,
                verticalFov = cam.fieldOfView,
                horizontalFov = 2f * Mathf.Atan(Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 16f / 9f) * Mathf.Rad2Deg
            };
            var eyeHeight = prefab.transform.InverseTransformPoint(cam.transform.position).y;
            for (var i = 0; i < P411FiveSpeciesRoster.Homes.Length; i++)
                audit.fish.Add(new Fish { species = P411FiveSpeciesRoster.SpeciesForSlot(i), home = P411FiveSpeciesRoster.Homes[i] });

            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene);
                Physics.SyncTransforms();
                foreach (var moving in All<Collider>(scene).Where(c => c.GetComponentInParent<Unity.Netcode.NetworkObject>() != null))
                    moving.enabled = false;
                Physics.SyncTransforms();
                var field = All<WaterField>(scene).Single();
                field.RebuildVolumes();
                audit.surfaceY = field.Bodies[0].SurfaceY;

                // The diver has just ducked under: eye half a metre below the line at the three places a player enters
                // the sea from the beach. (From above the line nothing under it is readable.)
                var feetY = audit.surfaceY - 0.5f - eyeHeight;
                var starts = new (string Id, Vector3 Feet)[]
                {
                    ("wade-west", new Vector3(-6.5f, feetY, -3.5f)),
                    ("ramp-east", new Vector3(4.5f, feetY, -6.5f)),
                    ("dock-end", new Vector3(9f, feetY, -2.5f))
                };
                foreach (var (id, feet) in starts)
                    audit.entries.Add(MeasureEntry(id, feet, eyeHeight, diver, audit, field));
                MeasureSizes(scene, audit);

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P278_FIRST_HUNT_WRITTEN " + OutputPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Entry MeasureEntry(string id, Vector3 feet, float eyeHeight, CharacterController template,
            Audit audit, WaterField field)
        {
            var entry = new Entry { id = id, eye = feet + Vector3.up * eyeHeight };
            var forward = Vector3.forward;
            foreach (var fish in audit.fish)
            {
                var seen = Look(entry.eye, forward, fish, audit);
                entry.atEntry.Add(seen);
                if (seen.inView && seen.lineClear && seen.visibility01 >= Readable) entry.readableAtEntry++;
            }

            // A first-time player swims straight out to sea at eye level, looking ahead.
            var steps = Mathf.CeilToInt((OpenSeaZ - feet.z) / (audit.swimSpeed * Dt));
            for (var i = 0; i <= steps && entry.firstInRangeSeconds < 0f; i++)
            {
                var eye = entry.eye + forward * (audit.swimSpeed * Dt * i);
                foreach (var fish in audit.fish)
                {
                    var seen = Look(eye, forward, fish, audit);
                    if (!seen.inView || !seen.lineClear || seen.visibility01 < Readable) continue;
                    if (entry.firstSightSeconds < 0f) { entry.firstSightSeconds = i * Dt; entry.firstSightSpecies = fish.species; }
                    if (seen.distance <= audit.harpoonRange)
                    { entry.firstInRangeSeconds = i * Dt; entry.firstInRangeSpecies = fish.species; break; }
                }
            }

            // The real controller to the nearest home of each species, then to a point 4 m short of it.
            foreach (var species in audit.fish.Select(f => f.species).Distinct())
            {
                var home = audit.fish.Where(f => f.species == species).OrderBy(f => Vector3.Distance(f.home, feet)).First().home;
                entry.habitats.Add(Swim(template, audit, species, feet, home));
            }
            return entry;
        }

        private static void MeasureSizes(Scene scene, Audit audit)
        {
            float Px(float length, float distance, float height) =>
                2f * Mathf.Atan(length * 0.5f / distance) * Mathf.Rad2Deg / audit.verticalFov * height;
            foreach (var group in All<FishActor>(scene).Where(f => f.Species != null).GroupBy(f => f.Species.SpeciesId))
            {
                var renderers = group.First().GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                var length = Mathf.Max(bounds.size.x, bounds.size.z);
                audit.sizes.Add(new Size
                {
                    species = group.Key, lengthMetres = length,
                    px720At12 = Px(length, 12f, 720f), px720At17 = Px(length, 17f, 720f), px720At22 = Px(length, 22f, 720f),
                    px1080At12 = Px(length, 12f, 1080f), px1080At22 = Px(length, 22f, 1080f)
                });
            }
        }

        private static Seen Look(Vector3 eye, Vector3 forward, Fish fish, Audit audit)
        {
            var line = fish.home - eye;
            var seen = new Seen { species = fish.species, home = fish.home, distance = line.magnitude };
            var depth = Mathf.Max(0f, audit.surfaceY - eye.y);
            var density = Mathf.Lerp(NearFog, DeepFog, depth / FogDepth);
            seen.visibility01 = Mathf.Exp(-Mathf.Pow(seen.distance * density, 2f));
            seen.lineClear = !Physics.Raycast(eye, line.normalized, line.magnitude, ~0, QueryTriggerInteraction.Ignore);
            var flat = new Vector3(line.x, 0f, line.z);
            var yaw = Vector3.Angle(forward, flat);
            var pitch = Mathf.Atan2(line.y, flat.magnitude) * Mathf.Rad2Deg;
            seen.inView = yaw <= audit.horizontalFov * 0.5f && Mathf.Abs(pitch) <= audit.verticalFov * 0.5f;
            return seen;
        }

        private static Habitat Swim(CharacterController template, Audit audit, string species, Vector3 feet, Vector3 home)
        {
            var habitat = new Habitat { species = species };
            var go = new GameObject("Audit_P278_Diver");
            var cc = go.AddComponent<CharacterController>();
            cc.radius = template.radius; cc.height = template.height; cc.center = template.center;
            cc.skinWidth = template.skinWidth; cc.stepOffset = template.stepOffset; cc.slopeLimit = template.slopeLimit;
            go.transform.position = feet;
            Physics.SyncTransforms();
            try
            {
                var target = home - (home - feet).normalized * 4f - template.center;
                var frames = 0;
                var guard = Mathf.CeilToInt(Vector3.Distance(feet, target) / (audit.swimSpeed * Dt)) * 3 + 100;
                while (Vector3.Distance(go.transform.position, target) > 0.15f)
                {
                    if (--guard < 0) { habitat.stuck = true; break; }
                    var before = go.transform.position;
                    cc.Move(Vector3.ClampMagnitude(target - before, audit.swimSpeed * Dt));
                    habitat.metres += Vector3.Distance(before, go.transform.position);
                    frames++;
                }
                habitat.seconds = frames * Dt;
                habitat.tankFraction = habitat.seconds / audit.maxOxygen;
            }
            finally
            {
                Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }
            return habitat;
        }

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }
}
