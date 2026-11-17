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
    // #298 (Utku): read-only measurement of the existing reef rock arch (#219) as a swim-through gate on the real scene.
    // Opening size, whether a diver swimming out from the beach entries can see it and at what distance (underwater fog
    // formula), whether the real controller swims through it both ways, and which fish homes are near it. Nothing moves.
    public static class P298ReefGateAudit
    {
        public const string OutputPath = "docs/reports/evidence/p298-reef-gate/reef-gate.json";
        private const float Dt = 0.02f, NearFog = 0.03f, DeepFog = 0.065f, FogDepth = 8f;

        [Serializable] public sealed class Sight { public string from; public Vector3 eye; public float distance, visibility01; public bool lineClear; public string blocker; public float firstReadableAtMetres; }
        [Serializable] public sealed class Swim { public string id; public bool through, stuck; public float metres, seconds; public int blockedFrames; }
        [Serializable] public sealed class Home { public string species; public Vector3 home; public float distance; }
        [Serializable] public sealed class Audit
        {
            public string note = "Real scene, real controller values, closed unsaved. Geometric line of sight with the underwater fog formula; no light, contrast or model.";
            public float openingWidth, openingHeight, gateDepth, floorUnderGate;
            public Vector3 gateCentre;
            public List<Sight> sights = new List<Sight>();
            public List<Swim> swims = new List<Swim>();
            public List<Home> nearbyFish = new List<Home>();
            // Floor along the gate's axis (x = gate centre): what a diver approaching the opening has to clear.
            public List<Vector2> floorAlongAxis = new List<Vector2>();
            public float openingTopY;
            // Each leg of the shore-to-arch trail (#298): from a diver's eye over one piece to the next piece, and on
            // to the gate from the last.
            public List<Sight> trailLegs = new List<Sight>();
            // How far a diver drops over the last 4 m into the south mouth, and the height between floor and the
            // opening's top along that approach (a standing diver needs 1.96 m).
            public float lastFourMetresDrop;
            public List<Vector2> roomUnderTopSouth = new List<Vector2>();
        }

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P4RouteAudit.DiverPath);
            var diver = prefab.GetComponent<CharacterController>();
            var speed = new SerializedObject(prefab.GetComponent<NetworkPlayer>()).FindProperty("swimSpeed").floatValue;
            var audit = new Audit();
            var arch = SeabedProfile.Solids.Where(s => s.KitId == "reef-rock-arch").ToArray();
            var top = arch.Single(s => s.Lift > 0f).Box;
            var feet = arch.Where(s => s.Lift == 0f).Select(s => s.Box).OrderBy(b => b.center.x).ToArray();
            var floor = SeabedProfile.FloorHeight(top.center.x, top.center.z);
            audit.openingWidth = feet[1].min.x - feet[0].max.x;
            audit.openingHeight = top.min.y - floor;
            audit.gateDepth = top.size.z;
            audit.floorUnderGate = floor;
            audit.gateCentre = new Vector3((feet[0].max.x + feet[1].min.x) * 0.5f, floor + audit.openingHeight * 0.5f, top.center.z);
            audit.openingTopY = top.min.y;
            for (var z = top.center.z - 10f; z <= top.center.z + 10.01f; z += 1f)
                audit.floorAlongAxis.Add(new Vector2(z, SeabedProfile.FloorHeight(audit.gateCentre.x, z)));
            {
                // The last 4 m of floor before the gate's south face, and the room left under the opening's top there.
                var south = top.min.z;
                audit.lastFourMetresDrop = SeabedProfile.FloorHeight(audit.gateCentre.x, south - 4f) - SeabedProfile.FloorHeight(audit.gateCentre.x, south);
                for (var z = south - 5f; z <= south + 0.01f; z += 1f)
                    audit.roomUnderTopSouth.Add(new Vector2(z, top.min.y - SeabedProfile.FloorHeight(audit.gateCentre.x, z)));
            }

            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene);
                ReefHabitatVisuals.Build(scene);
                Physics.SyncTransforms();
                foreach (var moving in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                             .Where(c => c.GetComponentInParent<Unity.Netcode.NetworkObject>() != null))
                    moving.enabled = false;
                Physics.SyncTransforms();
                var surface = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaterField>(true)).Single();
                surface.RebuildVolumes();
                var surfaceY = surface.Bodies[0].SurfaceY;

                foreach (var (id, eye) in new[]
                         {
                             ("wade-entry", new Vector3(-6.5f, surfaceY - 0.5f, -3.5f)),
                             ("sea-bass-habitat", new Vector3(-4f, 4f, 8f)),
                             ("reef-shelf-edge", new Vector3(-6f, 0f, 18f))
                         })
                    audit.sights.Add(Look(id, eye, audit.gateCentre, surfaceY));

                var trail = ReefHabitatVisuals.ArchTrail;
                for (var i = 0; i < trail.Length; i++)
                {
                    var eye = trail[i].Position + Vector3.up * 1.5f;
                    var next = i + 1 < trail.Length ? trail[i + 1].Position + Vector3.up * 0.4f : audit.gateCentre;
                    audit.trailLegs.Add(Look("trail-" + i + (i + 1 < trail.Length ? "-to-" + (i + 1) : "-to-gate"), eye, next, surfaceY));
                }

                var y = audit.gateCentre.y - diver.height * 0.5f;
                var x = audit.gateCentre.x;
                audit.swims.Add(SwimThrough("north-level", diver, speed, new Vector3(x, y, top.center.z - 6f), new Vector3(x, y, top.center.z + 6f)));
                audit.swims.Add(SwimThrough("south-level", diver, speed, new Vector3(x, y, top.center.z + 6f), new Vector3(x, y, top.center.z - 6f)));
                // The way a diver really takes it: from over the south mound, down into the mouth, through at the
                // opening's floor, then up over the north mound. Feet heights keep 0.3 m off the floor at each stop.
                var z0 = top.center.z;
                float Feet(float z, float lift) => SeabedProfile.FloorHeight(x, z) + lift;
                var path = new[]
                {
                    new Vector3(x, Feet(z0 - 6f, 0.6f), z0 - 6f), new Vector3(x, Feet(z0 - 2.5f, 0.3f), z0 - 2.5f),
                    new Vector3(x, floor + 0.15f, z0 - 1.2f), new Vector3(x, floor + 0.15f, z0 + 1.2f),
                    new Vector3(x, Feet(z0 + 3f, 0.3f), z0 + 3f), new Vector3(x, Feet(z0 + 5f, 0.6f), z0 + 5f),
                    new Vector3(x, Feet(z0 + 7f, 0.6f), z0 + 7f)
                };
                audit.swims.Add(SwimPath("north-dip", diver, speed, path));
                audit.swims.Add(SwimPath("south-dip", diver, speed, path.Reverse().ToArray()));

                for (var i = 0; i < P411FiveSpeciesRoster.Homes.Length; i++)
                {
                    var home = P411FiveSpeciesRoster.Homes[i];
                    var d = Vector3.Distance(home, audit.gateCentre);
                    if (d <= 12f) audit.nearbyFish.Add(new Home { species = P411FiveSpeciesRoster.SpeciesForSlot(i), home = home, distance = d });
                }

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P298_REEF_GATE_WRITTEN " + OutputPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Sight Look(string id, Vector3 eye, Vector3 target, float surfaceY)
        {
            var line = target - eye;
            var sight = new Sight { from = id, eye = eye, distance = line.magnitude, firstReadableAtMetres = -1f };
            var density = Mathf.Lerp(NearFog, DeepFog, Mathf.Max(0f, surfaceY - eye.y) / FogDepth);
            sight.visibility01 = Mathf.Exp(-Mathf.Pow(sight.distance * density, 2f));
            if (Physics.Raycast(eye, line.normalized, out var hit, line.magnitude - 0.5f, ~0, QueryTriggerInteraction.Ignore))
            { sight.lineClear = false; sight.blocker = hit.collider.name; }
            else sight.lineClear = true;
            // Half-visibility distance at this eye depth: how close one must come before the gate reads.
            sight.firstReadableAtMetres = Mathf.Sqrt(Mathf.Log(2f)) / density;
            return sight;
        }

        private static Swim SwimPath(string id, CharacterController template, float speed, Vector3[] stops)
        {
            var total = new Swim { id = id, through = true };
            for (var i = 1; i < stops.Length && total.through; i++)
            {
                var leg = SwimThrough(id, template, speed, stops[i - 1], stops[i]);
                total.metres += leg.metres; total.seconds += leg.seconds; total.blockedFrames += leg.blockedFrames;
                if (!leg.through) { total.through = false; total.stuck = true; total.id = id + " (stuck on leg " + i + ")"; }
            }
            return total;
        }

        private static Swim SwimThrough(string id, CharacterController template, float speed, Vector3 from, Vector3 to)
        {
            var swim = new Swim { id = id };
            var go = new GameObject("Audit_P298_Diver");
            var cc = go.AddComponent<CharacterController>();
            cc.radius = template.radius; cc.height = template.height; cc.center = template.center;
            cc.skinWidth = template.skinWidth; cc.stepOffset = template.stepOffset; cc.slopeLimit = template.slopeLimit;
            go.transform.position = from;
            Physics.SyncTransforms();
            try
            {
                var frames = 0;
                var guard = Mathf.CeilToInt(Vector3.Distance(from, to) / (speed * Dt)) * 3 + 100;
                while (Vector3.Distance(go.transform.position, to) > 0.15f)
                {
                    if (--guard < 0) { swim.stuck = true; break; }
                    var before = go.transform.position;
                    cc.Move(Vector3.ClampMagnitude(to - before, speed * Dt));
                    var moved = Vector3.Distance(before, go.transform.position);
                    if (moved < speed * Dt * 0.5f) swim.blockedFrames++;
                    swim.metres += moved;
                    frames++;
                }
                swim.seconds = frames * Dt;
                swim.through = !swim.stuck;
            }
            finally { Object.DestroyImmediate(go); Physics.SyncTransforms(); }
            return swim;
        }
    }
}
