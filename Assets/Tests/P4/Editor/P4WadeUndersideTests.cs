using System;
using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // World check after #185/#194, measured by DeepDive.Editor.P4WadeUndersideAudit (docs/reports/P4-WADE-UNDERSIDE.md):
    // the water under Beach_Wade / Shore_Ramp / Shore_Ledge (closed by DiveTestAreaShoreFillSetup), the 15 coast fish
    // against the coast scenery, and the supplied home plant against the east side corridor. EditMode geometry and
    // host-rule replays, not playtest proof.
    public sealed class P4WadeUndersideTests
    {
        private const float Radius = 0.35f;
        private const float Height = 1.8f;
        private const float Skin = 0.08f;
        private const float SwimSpeed = 3f;
        private const float Dt = 0.02f;
        private const float EscapeSeconds = 10f;

        private static readonly string[] Slabs = { "Beach_Wade", "Shore_Ramp", "Shore_Ledge" };
        private static readonly Vector3 ProductDayStuck = new Vector3(-7.6f, 3.3f, -5.0f);

        // DiveTestAreaShoreFillSetup's pieces (DeepDive.World.Editor, which this assembly does not reference).
        private const string WadeFill = "Beach_Wade_Fill", WadeToe = "Beach_Wade_Toe";
        private const string RampFill = "Shore_Ramp_Fill", RampToe = "Shore_Ramp_Toe";
        private static readonly string[] Fills = { WadeFill, WadeToe, RampFill, RampToe, "Shore_Ledge_Fill" };

        // ---- divers under the slabs ----------------------------------------------------------------------

        // No upright diver fits anywhere with a coast slab overhead. Red before the fill: 39 / 25 / 15 m2.
        [Test]
        public void NoDiverFitsUnderACoastSlab()
        {
            WithScenery(scene =>
            {
                var failures = new List<string>();
                foreach (var name in Slabs)
                {
                    var slab = Slab(scene, name);
                    var columns = PocketColumns(scene, slab).ToArray();
                    if (columns.Length > 0)
                        failures.Add($"{name}: {columns.Length * 0.0625f:0.00} m2 fits, e.g. feet {columns[0]}");
                }
                Assert.IsEmpty(failures, string.Join("\n", failures));
            });
        }

        // Every start a diver could reach before the fill (the old pocket on a 1 m lattice, jammed and mid gap, and
        // the product-day point) is now solid, and every start in the open water along a slab's edges surfaces within
        // 10 s holding "up", "up + nearest open water" or "up + toward the shore". Red before the fill: 133/139 and
        // 97/139 old starts stayed trapped holding "up" and "up + toward the shore".
        [Test]
        public void DiverByACoastSlabSurfacesWithEveryEscapeInput()
        {
            WithScenery(scene =>
            {
                var field = All<WaterField>(scene).Single();
                var surfaceY = field.Bodies[0].SurfaceY;
                var old = OldPocketStarts(scene);
                Assert.Greater(old.Count, 100, "the pre-fill pocket lattice should be measurable with the fills off");

                var starts = new List<(string plate, Vector3 feet)>();
                foreach (var start in old)
                    if (Fits(start.feet)) starts.Add(start);
                foreach (var name in Slabs) starts.AddRange(EdgeStarts(Slab(scene, name), surfaceY).Select(f => (name, f)));
                Assert.Greater(starts.Count, 30, "there is open water along the slab edges to test");

                var failures = new List<string>();
                foreach (var (plate, feet) in starts)
                {
                    var open = NearestOpenWater(feet, surfaceY);
                    var shore = plate == "Shore_Ramp" ? Vector3.right : Vector3.back;
                    foreach (var (strategy, input) in new[]
                             {
                                 ("up", Vector3.up),
                                 ("up+nearest-open-water", Vector3.ClampMagnitude(open + Vector3.up, 1f)),
                                 ("up+toward-shore", Vector3.ClampMagnitude(shore + Vector3.up, 1f))
                             })
                        if (!SurfacesWithin(scene, field, feet, input, EscapeSeconds, out var end))
                            failures.Add($"{plate} {strategy} from {feet} stuck at {end}");
                }
                Assert.IsEmpty(failures, $"{failures.Count} trapped of {starts.Count * 3}\n" + string.Join("\n", failures.Take(20)));
            });
        }

        // The fills sit strictly under their slab: straight down onto every slab's top face the slab is still the
        // first thing hit, nothing reaches past a slope's open-water foot edge, and they are solid scenery.
        [Test]
        public void ShoreFillsStayUnderTheSlabsAndAreScenery()
        {
            WithScenery(scene =>
            {
                foreach (var name in Fills)
                {
                    var piece = All<BoxCollider>(scene).SingleOrDefault(c => c.name == name);
                    Assert.IsNotNull(piece, name + " missing: run DeepDive/P4/Dive test area: close the coast slab undersides");
                    Assert.IsFalse(piece.isTrigger, name + " must be solid");
                    Assert.IsNull(piece.GetComponentInParent<NetworkObject>(), name + " must be scenery");
                }
                foreach (var name in Slabs)
                {
                    var slab = Slab(scene, name);
                    for (var u = -0.45f; u <= 0.45f; u += 0.05f)
                    for (var v = -0.45f; v <= 0.45f; v += 0.05f)
                    {
                        var top = slab.transform.TransformPoint(new Vector3(u, 0.5f, v));
                        Assert.IsTrue(Physics.Raycast(top + Vector3.up * 2f, Vector3.down, out var hit, 2.05f, ~0, QueryTriggerInteraction.Ignore));
                        Assert.IsFalse(Fills.Contains(hit.collider.name),
                            $"{hit.collider.name} rises above {name}'s top face at {hit.point}");
                    }
                }
                var wadeFoot = Slab(scene, "Beach_Wade").bounds.max.z;
                var rampFoot = Slab(scene, "Shore_Ramp").bounds.min.x;
                foreach (var name in new[] { WadeFill, WadeToe })
                    Assert.LessOrEqual(All<Collider>(scene).Single(c => c.name == name).bounds.max.z, wadeFoot + 1e-3f, name + " passes the wade foot");
                foreach (var name in new[] { RampFill, RampToe })
                    Assert.GreaterOrEqual(All<Collider>(scene).Single(c => c.name == name).bounds.min.x, rampFoot - 1e-3f, name + " passes the ramp foot");
            });
        }

        // ---- fish against the scenery --------------------------------------------------------------------

        // Every point FishMotion.PickWanderTarget can choose (home + offset * WanderRadius, |offset| <= 1,
        // |offset.y| <= 0.5, inside the SwimVolume box) keeps 0.2 m from static scenery, checked on a 0.5 m grid
        // with a 0.45 m sphere so the gaps between samples are covered. The set is convex and contains home, so
        // every straight wander leg stays in it. Red before: the z -6 row (Fish_SeaBass_02..08) reached into
        // Beach_Wade, Shore_Ramp and Beach_Platform.
        [Test]
        public void CoastFishWanderEnvelopeStaysClearOfScenery()
        {
            WithScenery(scene =>
            {
                var swim = All<SwimVolume>(scene).Single().GetComponent<BoxCollider>();
                var fish = All<FishActor>(scene).OrderBy(f => f.name, StringComparer.Ordinal).ToArray();
                Assert.AreEqual(15, fish.Length);
                var failures = new List<string>();
                foreach (var actor in fish)
                {
                    var home = actor.transform.position;
                    var r = actor.Species.Swim.WanderRadius;
                    var hits = new HashSet<string>();
                    for (var x = -r; x <= r + 1e-3f; x += 0.5f)
                    for (var y = -r * 0.5f; y <= r * 0.5f + 1e-3f; y += 0.5f)
                    for (var z = -r; z <= r + 1e-3f; z += 0.5f)
                    {
                        if (x * x + y * y + z * z > r * r + 1e-3f) continue;
                        var p = home + new Vector3(x, y, z);
                        if (!InBox(swim, p)) continue;
                        foreach (var c in Physics.OverlapSphere(p, 0.45f, ~0, QueryTriggerInteraction.Ignore)) hits.Add(c.name);
                    }
                    if (hits.Count > 0) failures.Add($"{actor.name} home {home}: envelope touches {string.Join(",", hits.OrderBy(n => n))}");
                }
                Assert.IsEmpty(failures, string.Join("\n", failures));
            });
        }

        // The runtime wander rule as the host runs it (FishMotion with the water box as its only bound, which is
        // what SwimVolumeBounds is), three seeds, 120 s each: the fish body never touches static scenery.
        [Test]
        public void CoastFishNeverWanderIntoScenery()
        {
            WithScenery(scene =>
            {
                var water = new BoxWater(All<SwimVolume>(scene).Single().GetComponent<BoxCollider>());
                var failures = new List<string>();
                foreach (var actor in All<FishActor>(scene).OrderBy(f => f.name, StringComparer.Ordinal))
                for (var seed = 1; seed <= 3; seed++)
                {
                    var motion = new FishMotion(actor.Species.Swim, actor.transform.position, new System.Random(seed));
                    var pos = actor.transform.position;
                    for (var f = 0; f < 6000; f++)
                    {
                        pos = motion.Step(pos, Array.Empty<Vector3>(), Dt, water);
                        var inside = Physics.OverlapSphere(pos, 0.2f, ~0, QueryTriggerInteraction.Ignore);
                        if (inside.Length == 0) continue;
                        failures.Add($"{actor.name} seed {seed} t={f * Dt:0.00}s at {pos} touches {inside[0].name}");
                        break;
                    }
                }
                Assert.IsEmpty(failures, string.Join("\n", failures));
            });
        }

        // Moving homes must not touch the population itself: 15 fish, same names and species mix, unique NGO
        // scene identities.
        [Test]
        public void CoastFishPopulationKeepsCountSpeciesAndIdentity()
        {
            WithScenery(scene =>
            {
                var fish = All<FishActor>(scene).OrderBy(f => f.name, StringComparer.Ordinal).ToArray();
                CollectionAssert.AreEqual(
                    new[] { "Fish_SeaBass" }.Concat(Enumerable.Range(2, 14).Select(i => $"Fish_SeaBass_{i:00}")).ToArray(),
                    fish.Select(f => f.name).ToArray());
                var species = fish.GroupBy(f => f.Species.SpeciesId).ToDictionary(g => g.Key, g => g.Count());
                Assert.AreEqual(5, species["sea_bass"]);
                Assert.AreEqual(5, species["tuna"]);
                Assert.AreEqual(5, species["red_snapper"]);
                var hashes = fish.Select(f => new UnityEditor.SerializedObject(f.GetComponent<NetworkObject>()).FindProperty("GlobalObjectIdHash").uintValue).ToArray();
                Assert.IsTrue(hashes.All(h => h != 0), "every fish has an NGO identity");
                Assert.AreEqual(15, hashes.Distinct().Count(), "every fish has its own NGO identity");
            });
        }

        // ---- the home plant ------------------------------------------------------------------------------

        // The east side corridor (between bed 3 and HomeRight, from the PC stand to the door) is the walk route
        // to the PC and bed 3 (#172 route c). Red before the fix: the plant stood at local (5.3, 0, 4.8), in it.
        [Test]
        public void HomePlantStandsOutsideTheEastSideCorridorAndInsideTheRoom()
        {
            var root = new GameObject("Test_P4Wade_Home");
            try
            {
                SuppliedTownArt.Home(root.transform);
                var plant = root.transform.Find("SuppliedHomeDecor/Supplied_Plant");
                Assert.IsNotNull(plant, "SuppliedKit/Plant did not load");
                var renderers = plant.GetComponentsInChildren<Renderer>(true);
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);

                var minX = ContinuousTownLayout.HomeRig.Bed(3).x + ContinuousTownLayout.HomeRig.BedSize.x * 0.5f;
                var maxX = ContinuousTownLayout.FindBlock("HomeRight").Min.x;
                var pcStandZ = ContinuousTownLayout.HomeRig.Pc.z + ContinuousTownLayout.HomeRig.PcSize.z * 0.5f + 0.8f;
                var minZ = pcStandZ - (Radius + Skin);
                var maxZ = ContinuousTownLayout.HomeRig.Door.z;
                var inCorridor = b.max.x > minX && b.min.x < maxX && b.max.z > minZ && b.min.z < maxZ;
                Assert.IsFalse(inCorridor, $"plant {b.min}..{b.max} overlaps the east corridor x {minX}..{maxX} z {minZ}..{maxZ}");

                Assert.LessOrEqual(b.max.x, maxX + 1e-3f, "plant crosses HomeRight");
                Assert.GreaterOrEqual(b.min.z, ContinuousTownLayout.FindBlock("HomeBack").Max.z - 1e-3f, "plant crosses HomeBack");
                Assert.IsFalse(Overlaps(b, ContinuousTownLayout.HomeRig.Pc, ContinuousTownLayout.HomeRig.PcSize), "plant overlaps the PC");
                for (var i = 0; i < ContinuousTownLayout.HomeRig.BedXs.Length; i++)
                    Assert.IsFalse(Overlaps(b, ContinuousTownLayout.HomeRig.Bed(i), ContinuousTownLayout.HomeRig.BedSize), "plant overlaps bed " + i);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---- helpers -------------------------------------------------------------------------------------

        // The town scene with every NetworkObject collider (fish, special-event hull) off: the questions are
        // about authored scenery, and those move or appear at run time.
        private static void WithScenery(Action<Scene> assertions)
        {
            ContinuousTownScene.With(scene =>
            {
                var dynamic = All<Collider>(scene).Where(c => c.enabled && c.GetComponentInParent<NetworkObject>() != null).ToArray();
                try
                {
                    foreach (var c in dynamic) c.enabled = false;
                    Physics.SyncTransforms();
                    assertions(scene);
                }
                finally
                {
                    foreach (var c in dynamic) if (c != null) c.enabled = true;
                    Physics.SyncTransforms();
                }
            });
        }

        private static Collider Slab(Scene scene, string name) => All<Collider>(scene).Single(c => c.name == name);

        // Feet positions on a 0.25 m column grid over the slab's footprint where the upright capsule fits and the
        // first thing above the head is the slab (or its fill): a diver there has the slab for a ceiling.
        private static IEnumerable<Vector3> PocketColumns(Scene scene, Collider slab)
        {
            var b = slab.bounds;
            for (var x = b.min.x + 0.125f; x < b.max.x; x += 0.25f)
            for (var z = b.min.z + 0.125f; z < b.max.z; z += 0.25f)
            for (var y = 0.08f; y + Height < b.max.y; y += 0.1f)
            {
                var feet = new Vector3(x, y, z);
                if (!Fits(feet)) continue;
                if (!Physics.Raycast(feet + Vector3.up * Height, Vector3.up, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (hit.collider != slab && !hit.collider.name.StartsWith(slab.name + "_", StringComparison.Ordinal)) continue;
                yield return feet;
                break;
            }
        }

        // The audit's pre-fill start set, rebuilt with the fill colliders switched off: for every 1 m lattice
        // column under a slab, the capsule jammed against the underside and at mid gap, plus the product-day point.
        private static List<(string plate, Vector3 feet)> OldPocketStarts(Scene scene)
        {
            var fills = All<Collider>(scene).Where(c => Fills.Contains(c.name) && c.enabled).ToArray();
            var starts = new List<(string, Vector3)> { ("Beach_Wade", ProductDayStuck) };
            try
            {
                foreach (var c in fills) c.enabled = false;
                Physics.SyncTransforms();
                foreach (var name in Slabs)
                {
                    var slab = Slab(scene, name);
                    var b = slab.bounds;
                    for (var x = b.min.x + 0.125f; x < b.max.x; x += 1f)
                    for (var z = b.min.z + 0.125f; z < b.max.z; z += 1f)
                    {
                        var origin = new Vector3(x, 0.05f, z);
                        if (Physics.CheckSphere(origin, 0.01f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        if (!Physics.Raycast(origin, Vector3.up, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore) || hit.collider != slab) continue;
                        float? low = null, high = null;
                        for (var y = 0.08f; y + Height <= hit.point.y; y += 0.1f)
                        {
                            if (!Fits(new Vector3(x, y, z))) continue;
                            low ??= y;
                            high = y;
                        }
                        if (!high.HasValue) continue;
                        starts.Add((name, new Vector3(x, high.Value, z)));
                        starts.Add((name, new Vector3(x, (low.Value + high.Value) * 0.5f, z)));
                    }
                }
            }
            finally
            {
                foreach (var c in fills) c.enabled = true;
                Physics.SyncTransforms();
            }
            return starts;
        }

        // Free, underwater, open-sky water 0.5..1.5 m outside the slab footprint, every 1 m along it, at three
        // depths: where a diver who swam along the closed slab ends up.
        private static IEnumerable<Vector3> EdgeStarts(Collider slab, float surfaceY)
        {
            var b = slab.bounds;
            for (var d = 0.5f; d <= 1.5f; d += 1f)
            for (var t = 0f; t <= 1f; t += 0.125f)
            foreach (var y in new[] { 0.5f, 2.5f, 4.5f })
            foreach (var feet in new[]
                     {
                         new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), y, b.max.z + d),
                         new Vector3(b.min.x - d, y, Mathf.Lerp(b.min.z, b.max.z, t)),
                         new Vector3(b.max.x + d, y, Mathf.Lerp(b.min.z, b.max.z, t))
                     })
            {
                if (feet.y + Height > surfaceY - 0.5f || !Fits(feet)) continue;
                if (Physics.Raycast(feet + Vector3.up * Height, Vector3.up, surfaceY + 1f - feet.y, ~0, QueryTriggerInteraction.Ignore)) continue;
                yield return feet;
            }
        }

        private static bool Fits(Vector3 feet) =>
            !Physics.CheckCapsule(feet + Vector3.up * Radius, feet + Vector3.up * (Height - Radius), Radius, ~0, QueryTriggerInteraction.Ignore);

        private static Vector3 NearestOpenWater(Vector3 feet, float surfaceY)
        {
            var best = Vector3.forward;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < 8; i++)
            {
                var d = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                for (var s = 0.1f; s < 15f && s < bestDistance; s += 0.1f)
                {
                    var q = feet + d * s;
                    if (!Fits(q)) break;
                    if (Physics.Raycast(q + Vector3.up * Height * 0.5f, Vector3.up, surfaceY + 1f - q.y, ~0, QueryTriggerInteraction.Ignore)) continue;
                    bestDistance = s;
                    best = d;
                    break;
                }
            }
            return best;
        }

        // NetworkPlayer.FixedUpdate for an Underwater diver (move * swimSpeed, no gravity) classified each step by
        // the scene's WaterField tracker, as PlayerWaterLocomotionBinding does on the host.
        private static bool SurfacesWithin(Scene scene, WaterField field, Vector3 feet, Vector3 input, float limit, out Vector3 end)
        {
            var go = new GameObject("Test_P4Wade_Diver");
            SceneManager.MoveGameObjectToScene(go, scene);
            try
            {
                var cc = go.AddComponent<CharacterController>();
                cc.height = Height; cc.radius = Radius; cc.center = new Vector3(0f, Height * 0.5f, 0f);
                cc.slopeLimit = 45f; cc.stepOffset = 0.3f; cc.skinWidth = Skin;
                go.transform.position = feet;
                Physics.SyncTransforms();
                var tracker = field.CreateTracker();
                for (var f = 0; f * Dt < limit; f++)
                {
                    var t = go.transform;
                    var env = tracker.Classify(new WaterProbe(t.position, t.TransformVector(cc.center), Radius, Height, Vector3.up));
                    if (env != EnvironmentLocomotion.Underwater) { end = t.position; return true; }
                    cc.Move(input * SwimSpeed * Dt);
                }
                end = go.transform.position;
                return false;
            }
            finally
            {
                Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }
        }

        // SwimVolumeBounds in EditMode: SwimVolume's registry only fills in OnEnable at play time, so the box is
        // read directly. Same rule: the water box and nothing else.
        private sealed class BoxWater : IWaterBounds
        {
            private readonly BoxCollider box;
            public BoxWater(BoxCollider box) => this.box = box;
            public bool Contains(Vector3 point) => InBox(box, point);
        }

        private static bool InBox(BoxCollider box, Vector3 point)
        {
            var local = box.transform.InverseTransformPoint(point) - box.center;
            var half = box.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private static bool Overlaps(Bounds b, Vector3 center, Vector3 size) =>
            b.max.x > center.x - size.x * 0.5f && b.min.x < center.x + size.x * 0.5f &&
            b.max.z > center.z - size.z * 0.5f && b.min.z < center.z + size.z * 0.5f;

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
    }
}
