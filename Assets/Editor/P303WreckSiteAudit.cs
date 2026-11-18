using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.Editor
{
    // #303 (Utku): read-only site choice for a small wreck in DiveTestArea's shallows. Scores every whole-metre spot of
    // shallow sand by how clear it is of what is already there (boat keels, fish homes, reef and trail pieces, walls),
    // how deep the water is and whether a diver entering from the beach or the dock can see it through the underwater
    // fog. Also reports the size of the approved damaged-rowboat model it would reuse. Nothing is placed or saved.
    public static class P303WreckSiteAudit
    {
        public const string OutputPath = "docs/reports/evidence/p303-wreck-site/wreck-site.json";
        private const float NearFog = 0.03f, DeepFog = 0.065f, FogDepth = 8f;

        // The five boat route legs the keel test in P219SeabedTests follows (x, z).
        private static readonly (Vector2 From, Vector2 To)[] Keels =
        {
            (new Vector2(9f, 3.5f), new Vector2(9f, 8.5f)), (new Vector2(9f, 3.5f), new Vector2(6f, 14f)),
            (new Vector2(6f, 14f), new Vector2(3f, 30f)), (new Vector2(6f, 14f), new Vector2(3f, 36f)),
            (new Vector2(3f, 36f), new Vector2(0f, 56f))
        };

        // Where a diver gets into the water: the west wade and the end of the town dock.
        private static readonly (string Id, Vector3 Eye)[] Entries =
        {
            ("wade-west", new Vector3(-6.5f, 7.3f, -3.5f)), ("dock-end", new Vector3(9f, 7.3f, -2.5f))
        };

        [Serializable] public sealed class Candidate
        {
            public Vector2 xz; public float floorY, waterDepth, keel, fishHome3D, reefPiece, wall, score;
            public string nearestHome;
            public List<string> sights = new List<string>();
        }

        [Serializable] public sealed class Audit
        {
            public string note = "Real scene opened additively and closed unsaved. Clearances are flat (x,z) metres unless named 3D.";
            public Vector3 hullSize;
            public string hullSource = BoatRepairSetArt.DamagedHull;
            public float minKeel = 3.5f, minFishHome3D = 4f, minReefPiece = 3f, minWall = 2.5f, maxFloorZ = 15f;
            public int scanned, eligible;
            public List<Candidate> best = new List<Candidate>();
        }

        public static void Run()
        {
            var audit = new Audit();
            var hull = Resources.Load<GameObject>(BoatRepairSetArt.DamagedHull);
            if (hull == null) throw new InvalidOperationException("P303_NO_HULL " + BoatRepairSetArt.DamagedHull);
            var renderers = hull.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            audit.hullSize = bounds.size;

            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene);
                ReefHabitatVisuals.Build(scene);
                BossTrailVisuals.Build(scene);
                Physics.SyncTransforms();
                foreach (var moving in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                             .Where(c => c.GetComponentInParent<Unity.Netcode.NetworkObject>() != null))
                    moving.enabled = false;
                Physics.SyncTransforms();
                var surface = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaterField>(true)).Single();
                surface.RebuildVolumes();
                var surfaceY = surface.Bodies[0].SurfaceY;

                var pieces = ReefHabitatVisuals.Spots.Select(s => new Vector2(s.Position.x, s.Position.z)).ToList();
                for (var i = 0; i < BossTrailVisuals.PieceIds.Length; i++)
                {
                    var p = BossTrailVisuals.PositionOf(i);
                    pieces.Add(new Vector2(p.x, p.z));
                }

                var all = new List<Candidate>();
                for (var x = -12; x <= 12; x++)
                for (var z = 2; z <= 22; z++)
                {
                    audit.scanned++;
                    var at = new Vector2(x, z);
                    var floor = SeabedProfile.FloorHeight(x, z);
                    var c = new Candidate
                    {
                        xz = at, floorY = floor, waterDepth = surfaceY - floor,
                        keel = Keels.Min(k => DistanceToSegment(at, k.From, k.To)),
                        reefPiece = pieces.Min(p => Vector2.Distance(p, at)),
                        wall = Mathf.Min(14.75f - Mathf.Abs(x), z + 14.75f)
                    };
                    var bed = new Vector3(x, floor + 0.6f, z);
                    var homes = P411FiveSpeciesRoster.Homes;
                    var nearest = 0;
                    for (var i = 1; i < homes.Length; i++)
                        if (Vector3.Distance(homes[i], bed) < Vector3.Distance(homes[nearest], bed)) nearest = i;
                    c.fishHome3D = Vector3.Distance(homes[nearest], bed);
                    c.nearestHome = P411FiveSpeciesRoster.SpeciesForSlot(nearest) + "@" + homes[nearest];
                    if (z > audit.maxFloorZ || c.keel < audit.minKeel || c.fishHome3D < audit.minFishHome3D ||
                        c.reefPiece < audit.minReefPiece || c.wall < audit.minWall) continue;
                    audit.eligible++;
                    // Room first, then the shorter swim from either entry.
                    var swim = Entries.Min(e => Vector2.Distance(new Vector2(e.Eye.x, e.Eye.z), at));
                    c.score = Mathf.Min(c.keel, c.fishHome3D, c.reefPiece, c.wall) - swim * 0.1f;
                    foreach (var (id, eye) in Entries) c.sights.Add(Look(id, eye, new Vector3(x, floor + 0.8f, z), surfaceY));
                    all.Add(c);
                }

                audit.best = all.OrderByDescending(c => c.score).Take(12).ToList();
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P303_WRECK_SITE_WRITTEN " + OutputPath + " eligible=" + audit.eligible + "/" + audit.scanned);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public const string PlacedPath = "docs/reports/evidence/p303-wreck-site/wreck-placed.json";
        private const float Dt = 0.02f;

        [Serializable] public sealed class Swim { public string id; public bool through; public float metres, seconds; public int blockedFrames; public string stuckOnLeg; }
        [Serializable] public sealed class Placed
        {
            public string note = "Real scene with the wreck built as at runtime, real diver controller values, closed unsaved.";
            public Vector3 hullBoxMin, hullBoxMax, hullVisualMin, hullVisualMax;
            public float keelClearance, nearestFishHome3D, nearestReefPiece, roomOverHullToSurface;
            public List<Swim> swims = new List<Swim>();
            public List<string> sights = new List<string>();
        }

        // After placement: the real controller swims round both sides and over the top from the two entries, and a
        // level line straight through the hull must be stopped by its box.
        public static void RunPlaced()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P4RouteAudit.DiverPath);
            var diver = prefab.GetComponent<CharacterController>();
            var speed = new SerializedObject(prefab.GetComponent<DeepDive.Network.NetworkPlayer>()).FindProperty("swimSpeed").floatValue;
            var placed = new Placed();
            var box = SeabedProfile.WreckSolids[0].Box;
            placed.hullBoxMin = box.min; placed.hullBoxMax = box.max;
            var at = new Vector2(SeabedProfile.WreckX, SeabedProfile.WreckZ);
            placed.keelClearance = Keels.Min(k => DistanceToSegment(at, k.From, k.To)) - box.extents.x;
            placed.nearestFishHome3D = P411FiveSpeciesRoster.Homes.Min(h => Vector3.Distance(h, box.ClosestPoint(h)));
            placed.nearestReefPiece = ReefHabitatVisuals.Spots.Min(sp => Vector3.Distance(sp.Position, box.ClosestPoint(sp.Position)));

            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene);
                ReefHabitatVisuals.Build(scene);
                ShallowWreckVisuals.Build(scene);
                Physics.SyncTransforms();
                foreach (var moving in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                             .Where(c => c.GetComponentInParent<Unity.Netcode.NetworkObject>() != null))
                    moving.enabled = false;
                Physics.SyncTransforms();
                var surface = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaterField>(true)).Single();
                surface.RebuildVolumes();
                var surfaceY = surface.Bodies[0].SurfaceY;
                placed.roomOverHullToSurface = surfaceY - box.max.y;
                var hull = scene.GetRootGameObjects().Single(r => r.name == ShallowWreckVisuals.RootName).transform.Find(ShallowWreckVisuals.HullName);
                var rs = hull.GetComponentsInChildren<Renderer>();
                var vb = rs[0].bounds; foreach (var r in rs) vb.Encapsulate(r.bounds);
                placed.hullVisualMin = vb.min; placed.hullVisualMax = vb.max;

                Vector3 Low(float x, float z) => new Vector3(x, SeabedProfile.FloorHeight(x, z) + 0.4f, z);
                float sx = box.min.x - 1.2f, ex = box.max.x + 1.2f, sz = box.min.z - 1.2f, nz = box.max.z + 1.2f;
                var x0 = SeabedProfile.WreckX;
                placed.swims.Add(SwimPath("wade-round-south", diver, speed, new[] { new Vector3(-6f, 5f, -1f), Low(sx, sz), Low(ex, sz), new Vector3(6f, 4f, 6f) }));
                placed.swims.Add(SwimPath("dock-round-north", diver, speed, new[] { new Vector3(8f, 5f, 0f), Low(ex, nz), Low(sx, nz), new Vector3(-5f, 4f, 6f) }));
                placed.swims.Add(SwimPath("wade-over-top", diver, speed, new[] { new Vector3(x0, 5f, -1f), new Vector3(x0, box.max.y + 0.3f, box.center.z), new Vector3(x0, 4f, 8f) }));
                placed.swims.Add(SwimPath("dock-over-top", diver, speed, new[] { new Vector3(x0, 4f, 8f), new Vector3(x0, box.max.y + 0.3f, box.center.z), new Vector3(x0, 5f, -1f) }));
                var through = SwimPath("level-through-hull (expected blocked)", diver, speed, new[] { new Vector3(box.min.x - 2f, box.min.y + 0.35f, box.center.z), new Vector3(box.max.x + 2f, box.min.y + 0.35f, box.center.z) });
                placed.swims.Add(through);
                foreach (var (id, eye) in Entries) placed.sights.Add(Look(id, eye, box.center, surfaceY));

                Directory.CreateDirectory(Path.GetDirectoryName(PlacedPath));
                File.WriteAllText(PlacedPath, JsonUtility.ToJson(placed, true));
                Debug.Log("P303_WRECK_PLACED_WRITTEN " + PlacedPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Swim SwimPath(string id, CharacterController template, float speed, Vector3[] stops)
        {
            var total = new Swim { id = id, through = true };
            var go = new GameObject("Audit_P303_Diver");
            var cc = go.AddComponent<CharacterController>();
            cc.radius = template.radius; cc.height = template.height; cc.center = template.center;
            cc.skinWidth = template.skinWidth; cc.stepOffset = template.stepOffset; cc.slopeLimit = template.slopeLimit;
            go.transform.position = stops[0];
            Physics.SyncTransforms();
            try
            {
                for (var leg = 1; leg < stops.Length && total.through; leg++)
                {
                    var to = stops[leg];
                    var guard = Mathf.CeilToInt(Vector3.Distance(go.transform.position, to) / (speed * Dt)) * 3 + 100;
                    while (Vector3.Distance(go.transform.position, to) > 0.15f)
                    {
                        if (--guard < 0) { total.through = false; total.stuckOnLeg = leg + " at " + go.transform.position; break; }
                        var before = go.transform.position;
                        cc.Move(Vector3.ClampMagnitude(to - before, speed * Dt));
                        var moved = Vector3.Distance(before, go.transform.position);
                        if (moved < speed * Dt * 0.5f) total.blockedFrames++;
                        total.metres += moved;
                        total.seconds += Dt;
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); Physics.SyncTransforms(); }
            return total;
        }

        private static string Look(string id, Vector3 eye, Vector3 target, float surfaceY)
        {
            var line = target - eye;
            var density = Mathf.Lerp(NearFog, DeepFog, Mathf.Clamp01((surfaceY - Mathf.Min(eye.y, target.y)) / FogDepth));
            var visibility = Mathf.Exp(-Mathf.Pow(line.magnitude * density, 2f));
            var blocked = Physics.Raycast(eye, line.normalized, out var hit, line.magnitude - 0.5f, ~0, QueryTriggerInteraction.Ignore);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}: {1:F1} m, visibility {2:F2}, {3}",
                id, line.magnitude, visibility, blocked ? "blocked by " + hit.collider.name : "clear");
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
