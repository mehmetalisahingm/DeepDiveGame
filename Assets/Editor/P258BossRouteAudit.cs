using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // #258 (P4-B): read-only measurement of today's boss trail on the #219 floor. Where do the three traces, the arena
    // and the boss sit against the bed and the depth bands, can a real CharacterController swim shore -> traces ->
    // arena -> surface on one tank, and can each stop be seen from the one before it through the underwater fog.
    // Same method as P411SeabedAudit: the scene is opened additively and closed unsaved; nothing is moved.
    public static class P258BossRouteAudit
    {
        public const string OutputPath = "docs/reports/evidence/p258-boss-route/boss-route.json";

        private const float Dt = 0.02f;
        // CoastalAtmosphere below 8 m: exponential-squared fog at this density.
        private const float DeepFogDensity = 0.065f;

        [Serializable] public sealed class Stop
        {
            public string id; public Vector3 position; public float bedY, clearance, depth; public string band, bedPiece;
            public bool insideSolid, insideSwimVolume;
        }
        [Serializable] public sealed class Leg
        {
            public string from, to; public float straight, swum, seconds; public bool stuck; public int blockedFrames;
            public bool lineBlocked; public string blocker; public float fogVisibility01;
        }
        [Serializable] public sealed class Audit
        {
            public string note = "Scene opened additively and closed unsaved. Geometric line of sight with the deep fog formula; no light, contrast or model.";
            public float swimSpeed, maxOxygen, radius, height, surfaceY, deepFogDensity = DeepFogDensity;
            public float fogHalfVisibleMetres, fogTenPercentMetres;
            public List<Stop> stops = new List<Stop>();
            public List<Leg> legs = new List<Leg>();
            // #258 trail: reef edge -> debris -> trace -> ... -> arena, seen from a diver's eye 1.5 m over each stop.
            public List<Leg> trail = new List<Leg>();
            public float outSeconds, backSeconds, roundTripSeconds, oxygenMarginSeconds;
            public float bossBottomY, bossBedY;
        }

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P4RouteAudit.DiverPath);
            var diver = prefab.GetComponent<CharacterController>();
            var player = new SerializedObject(prefab.GetComponent<NetworkPlayer>());
            var audit = new Audit
            {
                swimSpeed = player.FindProperty("swimSpeed").floatValue,
                maxOxygen = player.FindProperty("maxOxygen").floatValue,
                radius = diver.radius, height = diver.height,
                fogHalfVisibleMetres = Mathf.Sqrt(Mathf.Log(2f)) / DeepFogDensity,
                fogTenPercentMetres = Mathf.Sqrt(Mathf.Log(10f)) / DeepFogDensity
            };

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
                var swim = All<SwimVolume>(scene).Single().GetComponent<BoxCollider>();
                audit.surfaceY = field.Bodies[0].SurfaceY;

                var entry = new Vector3(9f, audit.surfaceY - 0.6f, -9f); // DiveExit, just under the line
                var named = new List<(string Id, Vector3 At)> { ("dive-exit", entry) };
                for (var i = 0; i < DeepEncounterWorld.TraceIds.Length; i++)
                    named.Add((DeepEncounterWorld.TraceIds[i], DeepEncounterWorld.TracePositions[i]));
                named.Add((DeepEncounterWorld.ArenaId, DeepEncounterWorld.ArenaPosition));
                named.Add(("boss", DeepEncounterWorld.BossPosition));

                foreach (var (id, at) in named)
                {
                    var bed = BedAt(at, audit.surfaceY, out var piece);
                    WaterDepth.TryClassify(field.Bodies, at, out var band);
                    audit.stops.Add(new Stop
                    {
                        id = id, position = at, bedY = bed, bedPiece = piece, clearance = at.y - bed,
                        depth = audit.surfaceY - at.y, band = band,
                        insideSolid = Physics.CheckSphere(at, diver.radius, ~0, QueryTriggerInteraction.Ignore),
                        insideSwimVolume = swim.bounds.Contains(at)
                    });
                }

                // The boss capsule primitive (height 2, radius 0.5) scaled by DeepEncounterWorld's (1.7, 2.4, 1.7).
                audit.bossBottomY = DeepEncounterWorld.BossPosition.y - 2.4f;
                audit.bossBedY = BedAt(DeepEncounterWorld.BossPosition, audit.surfaceY, out _);

                // Out: exit -> trace 1 -> 2 -> 3 -> arena. Back: straight up from the arena, then along the surface.
                var outPath = named.Take(5).ToList();
                for (var i = 1; i < outPath.Count; i++)
                    audit.legs.Add(Swim(diver, audit.swimSpeed, outPath[i - 1], outPath[i]));
                var ascent = (DeepEncounterWorld.ArenaId + " ascent", new Vector3(DeepEncounterWorld.ArenaPosition.x, audit.surfaceY - 0.6f, DeepEncounterWorld.ArenaPosition.z));
                audit.legs.Add(Swim(diver, audit.swimSpeed, outPath[4], ascent));
                audit.legs.Add(Swim(diver, audit.swimSpeed, ascent, named[0]));

                var trailStops = DeepEncounterWorld.TrailWithStops();
                for (var i = 1; i < trailStops.Count; i++)
                {
                    var eye = Vector3.up * 1.5f;
                    var a = trailStops[i - 1]; var b = trailStops[i];
                    // A debris point is on the sand; the eye floats over it. Traces and the arena are already lifted.
                    var from = a + (a.y <= SeabedProfile.FloorHeight(a.x, a.z) + 0.01f ? eye : Vector3.zero);
                    var to = b + (b.y <= SeabedProfile.FloorHeight(b.x, b.z) + 0.01f ? Vector3.up * 0.5f : Vector3.zero);
                    var sight = new Leg { from = "trail-" + (i - 1), to = "trail-" + i, straight = Vector3.Distance(from, to) };
                    var dir = to - from;
                    if (Physics.Raycast(from, dir.normalized, out var block, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    { sight.lineBlocked = true; sight.blocker = block.collider.name; }
                    sight.fogVisibility01 = Mathf.Exp(-Mathf.Pow(sight.straight * DeepFogDensity, 2f));
                    audit.trail.Add(sight);
                }

                audit.outSeconds = audit.legs.Take(4).Sum(l => l.seconds);
                audit.backSeconds = audit.legs.Skip(4).Take(1).Sum(l => l.seconds); // oxygen refills at the surface
                audit.roundTripSeconds = audit.legs.Sum(l => l.seconds);
                audit.oxygenMarginSeconds = audit.maxOxygen - audit.outSeconds - audit.backSeconds;

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P258_BOSS_ROUTE_WRITTEN " + OutputPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Straight swim toward the next stop, no gravity, sliding along whatever is in the way. The line of sight
        // is the eye-free geometric line between the two stops, the same line the swimmer would look along.
        private static Leg Swim(CharacterController template, float speed, (string Id, Vector3 At) from, (string Id, Vector3 At) to)
        {
            var leg = new Leg { from = from.Id, to = to.Id, straight = Vector3.Distance(from.At, to.At) };
            var line = to.At - from.At;
            if (Physics.Raycast(from.At, line.normalized, out var hit, line.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                leg.lineBlocked = true;
                leg.blocker = hit.collider.name;
            }
            leg.fogVisibility01 = Mathf.Exp(-Mathf.Pow(leg.straight * DeepFogDensity, 2f));

            var go = new GameObject("Audit_P258_Diver");
            var cc = go.AddComponent<CharacterController>();
            cc.radius = template.radius; cc.height = template.height; cc.center = template.center;
            cc.skinWidth = template.skinWidth; cc.stepOffset = template.stepOffset; cc.slopeLimit = template.slopeLimit;
            var centre = template.center;
            go.transform.position = from.At - centre;
            Physics.SyncTransforms();
            try
            {
                var frames = 0;
                var guard = Mathf.CeilToInt(leg.straight / (speed * Dt)) * 3 + 100;
                while (Vector3.Distance(go.transform.position + centre, to.At) > 0.1f)
                {
                    if (--guard < 0) { leg.stuck = true; break; }
                    var before = go.transform.position;
                    cc.Move(Vector3.ClampMagnitude(to.At - (before + centre), speed * Dt));
                    var moved = Vector3.Distance(before, go.transform.position);
                    if (moved < speed * Dt * 0.5f) leg.blockedFrames++;
                    leg.swum += moved;
                    frames++;
                }
                leg.seconds = frames * Dt;
            }
            finally
            {
                Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }
            return leg;
        }

        private static float BedAt(Vector3 at, float surfaceY, out string piece)
        {
            piece = "(open)";
            var origin = new Vector3(at.x, surfaceY - 0.05f, at.z);
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore)) return float.NaN;
            piece = hit.collider.name;
            return hit.point.y;
        }

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }
}
