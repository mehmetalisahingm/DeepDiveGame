using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using DeepDive.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // #297/#188 (Mehmet, 21 Nov): should the repair cradle move about 1 m south so the side apron is not cut off before the
    // boat is repaired? Read-only, on the real scene with the town and the apron built. For the cradle where it is and
    // moved 0.6, 0.95 and 1.0 m south it places the broken hull's real collider (BoatRepairNetworkBinding: 2.4 x 0.4 x 5 m,
    // yaw 90) and, for the crewed passes, a standing diver capsule on each of the four work points. Then real-size
    // CharacterControllers walk: one and four from the beach gate onto the apron and the ramp landing; four from the town
    // edge to the pier by the route audit's diagonal and by the corridors north and south of the hull; and each worker
    // from the part drop to its own work point with the other three standing. Also checks each work point is dry,
    // free and on the beach. Nothing is saved; the scene is closed unsaved.
    public static class P297CradleShiftAudit
    {
        public const string OutputPath = "docs/reports/evidence/p297-shore-dock/cradle-shift.json";
        private static readonly (string Id, float Z)[] Poses = { ("current", -12.6f), ("south-0.6", -13.2f), ("south-0.95", -13.55f), ("south-1.0", -13.6f) };
        private const float HullWidth = 2.4f, HullLength = 5f, HullHeight = 0.4f, PlatformMinZ = -14.75f, PlatformNorthZ = -11f;

        [Serializable] public sealed class WorkPoint { public int index; public Vector3 at; public bool dry, free; public string blockedBy, ground; }
        [Serializable] public sealed class Pose
        {
            // southOverhang: how far the hull's south rim reaches past Beach_Platform's south edge (z -14.75) onto the
            // town's back fill; DiveTestAreaRepairCradleSetup refuses any (> 0). South of the beach is walkable town ground.
            public string id; public Vector3 cradle; public float gapToApron, southOverhang;
            public List<string> hullClimb = new List<string>();
            public List<WorkPoint> work = new List<WorkPoint>();
            public List<P297ShoreDockAudit.Pass> passes = new List<P297ShoreDockAudit.Pass>();
        }
        [Serializable] public sealed class Audit
        {
            public string note = "Hull collider as the runtime binding builds it; crewed passes add a standing capsule on each work point. Walkers steer straight and never give way.";
            public List<Pose> poses = new List<Pose>();
        }

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P4RouteAudit.DiverPath);
            var diver = prefab.GetComponent<CharacterController>();
            var speed = new SerializedObject(prefab.GetComponent<NetworkPlayer>()).FindProperty("walkSpeed").floatValue;
            var audit = new Audit();
            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene);
                foreach (var moving in P297ShoreDockAudit.All<Collider>(scene).Where(c => c.GetComponentInParent<Unity.Netcode.NetworkObject>() != null))
                    moving.enabled = false;
                Physics.SyncTransforms();
                foreach (var (id, z) in Poses) audit.poses.Add(Measure(id, new Vector3(DiveTestAreaRepairCradleSetup.CradlePosition.x, DiveTestAreaRepairCradleSetup.CradlePosition.y, z), diver, speed));
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                foreach (var p in audit.poses)
                {
                    Debug.Log($"P297_CRADLE {p.id} hullClimb onHull={p.hullClimb.Count(c => c.Contains("onHull=True"))}/{p.hullClimb.Count}");
                    Debug.Log($"P297_CRADLE {p.id} cradle={p.cradle} gapToApron={p.gapToApron:0.00} southOverhang={p.southOverhang:0.00} " +
                              "work=" + string.Join(" ", p.work.Select(w => $"{w.index}:{(w.dry ? "dry" : "WET")}/{(w.free ? "free" : "BLOCKED " + w.blockedBy)}")));
                    foreach (var pass in p.passes)
                        Debug.Log($"P297_CRADLE {p.id} {pass.id} arrived={pass.walkers.Count(w => w.arrived)}/{pass.walkers.Count} seconds={pass.seconds:0.0} blocked={pass.walkers.Sum(w => w.blockedFrames)}");
                }
                Debug.Log("P297_CRADLE_WRITTEN " + OutputPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Pose Measure(string id, Vector3 cradle, CharacterController diver, float speed)
        {
            var pose = new Pose
            {
                id = id, cradle = cradle,
                gapToApron = PlatformNorthZ - (cradle.z + HullWidth / 2f),
                southOverhang = PlatformMinZ - (cradle.z - HullWidth / 2f)
            };
            var rotation = Quaternion.Euler(0f, DiveTestAreaRepairCradleSetup.CradleYaw, 0f);
            var hull = new GameObject("Audit_P297_Hull");
            var box = hull.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, HullHeight / 2f, 0f);
            box.size = new Vector3(HullWidth, HullHeight, HullLength);
            hull.transform.SetPositionAndRotation(cradle, rotation);
            var work = Enumerable.Range(0, DiveTestAreaRepairCradleSetup.WorkLocal.Length)
                .Select(i => cradle + rotation * DiveTestAreaRepairCradleSetup.WorkLocal[i]).ToArray();
            Physics.SyncTransforms();
            var crew = new List<GameObject>();
            try
            {
                for (var i = 0; i < work.Length; i++)
                {
                    var feet = work[i];
                    // Dry: standing ground at the beach's height under the feet (the beach or the town's back fill).
                    var dry = Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var ground, 1f, ~0, QueryTriggerInteraction.Ignore) &&
                              Mathf.Abs(ground.point.y - feet.y) < 0.02f;
                    var blockers = Physics.OverlapCapsule(feet + Vector3.up * 0.43f, feet + Vector3.up * 1.45f, 0.35f, ~0, QueryTriggerInteraction.Ignore)
                        .Where(c => c != box).Select(c => c.name).ToArray();
                    pose.work.Add(new WorkPoint { index = i, at = feet, dry = dry, free = blockers.Length == 0, blockedBy = string.Join(",", blockers),
                        ground = dry ? ground.collider.name : "(none)" });
                }

                var gate = new Vector2(0f, -13.5f);
                var north = (cradle.z + HullWidth / 2f + PlatformNorthZ) / 2f;   // middle of the corridor between hull and platform edge
                var southRim = cradle.z - HullWidth / 2f;
                var landing = new[] { new Vector2(8.3f, -9.6f), new Vector2(9.1f, -9.0f), new Vector2(8.0f, -8.4f), new Vector2(10.3f, -10.4f) };
                // Single file through the gate and along the north corridor, each to its own apron column and landing spot.
                Vector2[] Apron(int k) => new[]
                {
                    gate + new Vector2(-0.4f - 1.2f * k, 0f), new Vector2(-0.4f, north), new Vector2(5.0f + 0.5f * k, north),
                    new Vector2(5.0f + 0.5f * k, -10.0f), landing[k]
                };
                Vector2[] Pier(int k, string way)
                {
                    var start = new Vector2(-0.6f + 0.6f * k, -16f);
                    var spot = new Vector2(9f + (k % 2 == 0 ? -0.45f : 0.45f), -9f + (k / 2) * 1.0f);
                    switch (way)
                    {
                        // Single file along the north corridor.
                        case "north": return new[] { start + new Vector2(0f, -1.2f * k), new Vector2(-0.4f, north), new Vector2(7.6f + 0.3f * k, north), spot };
                        // Each on its own lane south of the hull (0.6 m off the rim, then 0.75 m apart), over the beach or the town's back fill.
                        case "south":
                            var lane = southRim - 0.6f - 0.75f * k;
                            return new[] { start + new Vector2(0f, -0.75f * k), new Vector2(start.x, lane), new Vector2(7.0f + 0.4f * k, lane), new Vector2(7.6f + 0.4f * k, -12.0f), spot };
                        default: return new[] { start, new Vector2(9f, -12.5f), spot };
                    }
                }

                foreach (var crewed in new[] { false, true })
                {
                    if (crewed)
                        foreach (var feet in work)
                        {
                            var man = new GameObject("Audit_P297_Crew");
                            var cap = man.AddComponent<CapsuleCollider>();
                            cap.radius = 0.35f; cap.height = 1.8f; cap.center = new Vector3(0f, 0.9f, 0f);
                            man.transform.position = feet;
                            crew.Add(man);
                        }
                    Physics.SyncTransforms();
                    var tag = crewed ? "crew" : "empty";
                    pose.passes.Add(P297ShoreDockAudit.Walk($"{tag}-apron-1", new List<(string, Vector2[])> { ("a", Apron(1)) }, diver, speed));
                    pose.passes.Add(P297ShoreDockAudit.Walk($"{tag}-apron-4-single-file",
                        Enumerable.Range(0, 4).Select(k => ("a" + k, Apron(k))).ToList(), diver, speed));
                    foreach (var way in new[] { "diagonal", "north", "south" })
                        pose.passes.Add(P297ShoreDockAudit.Walk($"{tag}-pier-4-{way}",
                            Enumerable.Range(0, 4).Select(k => ("p" + k, Pier(k, way))).ToList(), diver, speed));
                }
                // #188: does a walker pushing into the empty-handed hull end up standing on it? Twelve headings at the
                // hull centre from 2 m outside its rim, 3 s each; the highest the feet got.
                foreach (var man in crew) man.SetActive(false);
                Physics.SyncTransforms();
                for (var k = 0; k < 12; k++)
                {
                    var dir = Quaternion.Euler(0f, k * 30f, 0f) * Vector3.forward;
                    var reach = Mathf.Abs(dir.x) * HullLength / 2f + Mathf.Abs(dir.z) * HullWidth / 2f + 2f;   // hull long along x at yaw 90
                    var from = new Vector2(cradle.x - dir.x * reach, cradle.z - dir.z * reach);
                    var through = new Vector2(cradle.x + dir.x * reach, cradle.z + dir.z * reach);
                    var climb = P297ShoreDockAudit.Walk($"hull-climb-{k * 30}", new List<(string, Vector2[])> { ("c", new[] { from, through }) }, diver, speed);
                    var w = climb.walkers[0];
                    pose.hullClimb.Add($"heading={k * 30} maxFeetY={w.maxY:0.00} onHull={(w.maxY > cradle.y + 0.3f)} crossed={w.arrived}");
                }
                foreach (var man in crew) man.SetActive(true);
                Physics.SyncTransforms();
                // Each worker carries from the drop to its own point while the other three stand at theirs.
                var drop = new Vector2(DiveTestAreaRepairCradleSetup.DropPosition.x, DiveTestAreaRepairCradleSetup.DropPosition.z);
                for (var i = 0; i < work.Length; i++)
                {
                    crew[i].SetActive(false);
                    Physics.SyncTransforms();
                    var target = new Vector2(work[i].x, work[i].z);
                    // Two ways from the drop: round the hull's east end and along its south side, or up the east end and
                    // along its north side; stern points (0, 1) then turn round the stern.
                    var south = southRim - 0.6f;
                    var southWay = i < 2
                        ? new[] { drop, new Vector2(7.2f, Mathf.Min(south, drop.y)), new Vector2(-0.4f, Mathf.Min(south, drop.y)), new Vector2(-0.4f, target.y), target }
                        : new[] { drop, new Vector2(Mathf.Max(target.x, 6.6f), Mathf.Min(target.y, south)), target };
                    var northWay = new[] { drop, new Vector2(6.6f, -14.0f), new Vector2(6.6f, north), new Vector2(-0.2f, north), new Vector2(-0.2f, target.y), target };
                    pose.passes.Add(P297ShoreDockAudit.Walk($"carry-drop-to-work-{i}-south", new List<(string, Vector2[])> { ("w" + i, southWay) }, diver, speed));
                    pose.passes.Add(P297ShoreDockAudit.Walk($"carry-drop-to-work-{i}-north", new List<(string, Vector2[])> { ("w" + i, northWay) }, diver, speed));
                    crew[i].SetActive(true);
                }
            }
            finally
            {
                foreach (var man in crew) Object.DestroyImmediate(man);
                Object.DestroyImmediate(hull);
                Physics.SyncTransforms();
            }
            return pose;
        }
    }
}
