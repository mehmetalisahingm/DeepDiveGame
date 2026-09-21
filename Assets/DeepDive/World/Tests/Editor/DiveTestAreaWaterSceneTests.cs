using System.Collections.Generic;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World.Tests
{
    // Guards the P3.1 half of DiveTestArea: the WaterField that reads SwimVolume, and the shore
    // the diver crosses the water line on. Everything asserted here is produced by
    // DiveTestAreaWaterSetup.Apply; if this fails, running that menu item again is the repair,
    // not hand-editing the scene.
    //
    // The round trips at the end drive a real WaterTracker along the real ramp and ledge. They
    // are still EditMode: no rig, no play mode, no netcode - just the scene's geometry fed
    // through the rules.
    public class DiveTestAreaWaterSceneTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string DiverPrefabPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";

        // FishMotion's wander box: SeaBass wanderRadius 6 around the fish home, flattened
        // vertically by FishMotion.VerticalWanderFactor. Mirrored here because the shore has to
        // stay out of it and the two constants live in different files.
        // Written out rather than imported from DiveTestAreaWaterSetup, the way the P2 and P3-B
        // scene tests are: a guard that reads its expectations from the script it is guarding
        // would follow that script anywhere, including into a mistake.
        private const string LedgeName = "Shore_Ledge";
        private const string RampName = "Shore_Ramp";

        // P3.2's south strip. Named here only because it changed what happens off the ledge's
        // south edge; everything else in this file stays about P3.1's own geometry.
        private const string BeachPlatformName = "Beach_Platform";
        private const float LedgeTopY = 8.4f;
        private const float RampAngleDegrees = 25f;

        // Foot of the ramp: the lowest surface a diver can stand on. Kept above the exit zone
        // (see NoShoreWalkableSurfaceSitsInTheSafeReturnZone) and below the water line.
        private const float RampBottomY = 7.2f;

        // A depth in open water past the foot of the ramp, where a diver ends up by swimming
        // or falling rather than walking.
        private const float OpenWaterY = 5f;

        // DiveExit's trigger, written out rather than read from Composition: the zone belongs
        // to Mehmet and this assembly does not reference his, so the numbers are copied the way
        // the rest of this file copies the scene's constants. SessionPortal tests the diver at
        // position + up * 0.9, so that is the point the shore has to keep out of the box.
        private const float ExitZoneMinY = 6f;
        private const float ExitZoneMaxY = 8f;
        private const float ExitZoneHalfXZ = 15f;
        private const float ExitZoneProbeHeight = 0.9f;

        private static readonly Vector3 FishHome = new Vector3(0f, 4f, 8f);
        private const float FishWanderRadius = 6f;
        private const float FishVerticalWanderFactor = 0.5f;

        private Scene scene;
        private Scene previousActive;
        private bool opened;

        [SetUp]
        public void OpenDiveScene()
        {
            previousActive = SceneManager.GetActiveScene();
            var already = SceneManager.GetSceneByPath(ScenePath);
            opened = !already.IsValid() || !already.isLoaded;
            scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : already;
            SceneManager.SetActiveScene(scene);
        }

        [TearDown]
        public void CloseDiveScene()
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        private static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<T>(true));
            return found;
        }

        private GameObject Require(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            Assert.Fail("DiveTestArea is missing " + name + "; run DeepDive/P3.1/Dive test area");
            return null;
        }

        private WaterField Field()
        {
            var fields = FindAll<WaterField>(scene);
            Assert.AreEqual(1, fields.Count, "DiveTestArea must hold exactly one WaterField");
            return fields[0];
        }

        private static CharacterController DiverController()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPrefabPath);
            Assert.IsNotNull(prefab, "Missing diver prefab");
            var controller = prefab.GetComponent<CharacterController>();
            Assert.IsNotNull(controller, "Diver prefab has no CharacterController");
            return controller;
        }

        // A probe shaped like the real diver, placed by the feet.
        private static WaterProbe Diver(Vector3 feet)
        {
            var controller = DiverController();
            return new WaterProbe(feet, controller.center, controller.radius, controller.height, Vector3.up);
        }

        [Test]
        public void DiverCapsuleMatchesThePlainTestValuesAndSkinIsInsideTheDeadband()
        {
            var controller = DiverController();
            Assert.AreEqual(1.8f, controller.height, "height");
            Assert.AreEqual(0.35f, controller.radius, "radius");
            Assert.AreEqual(new Vector3(0f, 0.9f, 0f), controller.center, "center");

            // The rules are fed the capsule, but the controller keeps the diver a skin width
            // off every surface. If the skin were wider than the band, standing still on the
            // shore could read as a crossing.
            Assert.Less(controller.skinWidth, Field().Deadband, "skinWidth must sit inside the deadband");
        }

        [Test]
        public void OneWaterFieldReadsEverySwimVolume()
        {
            var field = Field();
            var volumes = FindAll<SwimVolume>(scene);
            Assert.AreEqual(1, volumes.Count, "DiveTestArea must hold exactly one SwimVolume");
            Assert.AreEqual(volumes.Count, field.VolumeCount, "field must reference every SwimVolume");
            Assert.AreEqual(volumes.Count, field.Bodies.Count, "every referenced volume must read as water");

            // The canonical water: centre (0,4,0), size (30,8,30).
            var body = field.Bodies[0];
            Assert.AreEqual(8f, body.SurfaceY);
            Assert.AreEqual(-15f, body.MinX);
            Assert.AreEqual(15f, body.MaxX);
            Assert.AreEqual(-15f, body.MinZ);
            Assert.AreEqual(15f, body.MaxZ);
        }

        [Test]
        public void TheWaterLineAgreesWithTheVisualSurface()
        {
            // The rules use SwimVolume's top (y=8); the player sees the WaterSurface plane
            // (y=8.1). They are allowed to differ, but not by enough to read as a lie.
            var visual = Require("WaterSurface").transform.position.y;
            Assert.LessOrEqual(Mathf.Abs(Field().Bodies[0].SurfaceY - visual), 0.15f,
                "water line and visual surface drifted apart");
        }

        [Test]
        public void EveryDiveSpawnIsSubmerged()
        {
            var field = Field();
            var spawns = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!root.name.StartsWith("Spawn_")) continue;
                spawns++;
                Assert.AreEqual(EnvironmentLocomotion.Underwater,
                    field.CreateTracker().Classify(Diver(root.transform.position)),
                    root.name + " must start the dive underwater");
            }
            Assert.AreEqual(4, spawns, "DiveTestArea must hold four spawn points");
        }

        [Test]
        public void StandingOnTheShoreLedgeIsLand()
        {
            var ledge = Require(LedgeName);
            var top = ledge.transform.position.y + ledge.transform.localScale.y * 0.5f;
            Assert.AreEqual(LedgeTopY, top, 0.001f, "ledge top moved");

            // Dry land: the ledge top clears SwimVolume's surface, so standing on it is not a
            // rounding error away from being in the water.
            Assert.Greater(top, Field().Bodies[0].SurfaceY, "the ledge must stand out of the water");
            var feet = new Vector3(ledge.transform.position.x, top, ledge.transform.position.z);
            Assert.AreEqual(EnvironmentLocomotion.Land, Field().CreateTracker().Classify(Diver(feet)));
        }

        [Test]
        public void TheShoreRampIsWalkable()
        {
            var ramp = Require(RampName);
            var controller = DiverController();

            // The face the diver walks on is the ramp's local +Y, so its tilt from world up is
            // the slope the controller has to climb.
            var slope = Vector3.Angle(ramp.transform.up, Vector3.up);
            Assert.AreEqual(RampAngleDegrees, slope, 0.01f, "ramp slope moved");
            Assert.Less(slope, controller.slopeLimit, "ramp must be inside the diver's slopeLimit");

            // Where the ramp meets the ledge there must be no lip taller than stepOffset. The
            // cube is a unit mesh, so its top corners are local (+-0.5, 0.5, 0).
            var topEast = ramp.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f));
            Assert.LessOrEqual(Mathf.Abs(topEast.y - LedgeTopY), controller.stepOffset,
                "step between ramp and ledge is taller than stepOffset");

            // And the foot of the ramp has to end under the water, or the shore leads nowhere.
            var bottomWest = ramp.transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0f));
            Assert.Less(bottomWest.y, Field().Bodies[0].SurfaceY, "the ramp must reach under the surface");
        }

        [Test]
        public void WalkingOffTheShoreLandsInWaterNotTheVoid()
        {
            // Updated for P3.2's beach, and only for that.
            //
            // This walked off all four edges of the ledge and asserted that each fall ended
            // underwater. The south edge is not a fall any more: Beach_Platform now occupies
            // z -14.75..-11 with its top at the ledge's own height, so a step south lands on
            // sand at the same level.
            //
            // The old south sample kept passing, because WaterField answers from geometry and
            // never asks a collider whether there is ground in the way - it would have gone on
            // claiming a diver falls into water where they now stand on a beach. The sample is
            // not dropped and the assertion is not softened; it is moved to the claim that is
            // true there, which is the stronger one, since a junction that drifted out of
            // stepOffset or under the water line would now fail here.
            //
            // The other three edges are left exactly as they were.
            var field = Field();
            var body = field.Bodies[0];
            var ledge = Require(LedgeName);
            var half = ledge.transform.localScale * 0.5f;
            var centre = ledge.transform.position;

            // The three edges the beach did not touch.
            var steps = new[]
            {
                new Vector3(centre.x + half.x + 0.5f, LedgeTopY, centre.z),
                new Vector3(centre.x - half.x - 0.5f, LedgeTopY, centre.z),
                new Vector3(centre.x, LedgeTopY, centre.z + half.z + 0.5f)
            };

            foreach (var step in steps)
            {
                // Over water, not over the gap behind the arena geometry.
                Assert.GreaterOrEqual(body.SignedInset(step.x, step.z), 0f,
                    "stepping off at " + step + " leaves the water footprint");

                // Standing in mid air just past the edge is still Land...
                Assert.AreEqual(EnvironmentLocomotion.Land, field.CreateTracker().Classify(Diver(step)),
                    "the air beside the ledge should read as Land");

                // ...and the fall from there ends in the water.
                var landed = new Vector3(step.x, OpenWaterY, step.z);
                Assert.AreEqual(EnvironmentLocomotion.Underwater, field.CreateTracker().Classify(Diver(landed)),
                    "falling off at " + step + " must end underwater");
            }

            // The south edge: ground, at the same height, so the diver walks across instead of
            // falling. Still not the void, which is what this test is named for.
            var south = new Vector3(centre.x, LedgeTopY, centre.z - half.z - 0.5f);
            var beach = Require(BeachPlatformName);
            var beachTop = beach.transform.position.y + beach.transform.localScale.y * 0.5f;
            var beachHalf = beach.transform.localScale * 0.5f;

            // The original claim still holds and is still checked: this is inside the water
            // footprint, so if the beach ever stopped covering it the diver would fall into
            // water rather than into the gap behind the arena.
            Assert.GreaterOrEqual(body.SignedInset(south.x, south.z), 0f,
                "stepping off at " + south + " leaves the water footprint");

            Assert.LessOrEqual(Mathf.Abs(south.x - beach.transform.position.x), beachHalf.x,
                "the south step must land over the beach in x");
            Assert.LessOrEqual(Mathf.Abs(south.z - beach.transform.position.z), beachHalf.z,
                "the south step must land over the beach in z");
            Assert.LessOrEqual(Mathf.Abs(beachTop - LedgeTopY), DiverController().stepOffset,
                "the drop from the ledge onto the beach is taller than stepOffset");
            Assert.AreEqual(EnvironmentLocomotion.Land, field.CreateTracker().Classify(Diver(
                    new Vector3(south.x, beachTop, south.z))),
                "standing on the beach south of the ledge must read as Land");
        }

        [Test]
        public void TheShoreIsSolidSceneryOnTheDefaultLayer()
        {
            foreach (var name in new[] { LedgeName, RampName })
            {
                var piece = Require(name);
                Assert.AreEqual(0, piece.layer, name + " must stay on Default");

                var box = piece.GetComponent<BoxCollider>();
                Assert.IsNotNull(box, name + " needs a BoxCollider to stand on");
                Assert.IsFalse(box.isTrigger, name + " must be solid, not a trigger");

                // Scenery: a NetworkObject here would need a GlobalObjectIdHash and would drag
                // the shore into the netcode surface for nothing.
                Assert.IsNull(piece.GetComponent<NetworkObject>(), name + " must not be networked");
            }
        }

        [Test]
        public void NoShoreWalkableSurfaceSitsInTheSafeReturnZone()
        {
            // The shore must not hand the diver a place to stand inside the dive's exit volume.
            // DiveExit belongs to Composition and is not touched here; its box is copied as
            // plain numbers above, the same way this file copies every other scene constant.
            var samples = new List<Vector3>();

            // The ledge top: centre, edges and corners.
            var ledge = Require(LedgeName);
            var half = ledge.transform.localScale * 0.5f;
            var centre = ledge.transform.position;
            for (var sx = -1; sx <= 1; sx++)
                for (var sz = -1; sz <= 1; sz++)
                    samples.Add(new Vector3(centre.x + sx * half.x, LedgeTopY, centre.z + sz * half.z));

            // The ramp's walkable face, end to end - the foot is the lowest of them and the
            // one that made this a blocker.
            var ramp = Require(RampName);
            var top = ramp.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f));
            var foot = ramp.transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0f));
            for (var i = 0; i <= 20; i++) samples.Add(Vector3.Lerp(top, foot, i / 20f));

            foreach (var stand in samples)
            {
                var probe = stand + Vector3.up * ExitZoneProbeHeight;
                var inside = Mathf.Abs(probe.x) <= ExitZoneHalfXZ
                             && Mathf.Abs(probe.z) <= ExitZoneHalfXZ
                             && probe.y >= ExitZoneMinY
                             && probe.y <= ExitZoneMaxY;
                Assert.IsFalse(inside,
                    "standing at " + stand + " is tested at " + probe + ", inside the dive exit zone");
            }
        }

        [Test]
        public void ShoreStaysOutOfTheFishWanderBox()
        {
            var reach = new Vector3(FishWanderRadius, FishWanderRadius * FishVerticalWanderFactor, FishWanderRadius);
            var wander = new Bounds(FishHome, reach * 2f);

            foreach (var name in new[] { LedgeName, RampName })
            {
                var piece = Require(name);
                var shore = piece.GetComponent<Renderer>().bounds;
                Assert.IsFalse(shore.Intersects(wander),
                    name + " overlaps the fish wander box " + wander + "; the fish would be pinned against it");
            }
        }

        // --- Round trips: the tracker driven along the real shore ---------------------------
        //
        // Deferred from step 2, where there was no geometry to walk on yet. A crossing is only
        // meaningful against the ramp and ledge that actually exist in the scene.

        // Walks the list of feet positions through one tracker and returns the states it
        // settled in, with repeats collapsed - so [Land, Surface, Underwater, Surface, Land]
        // means exactly four changes, however many ticks it took.
        private List<EnvironmentLocomotion> Walk(IEnumerable<Vector3> path)
        {
            var tracker = Field().CreateTracker();
            var states = new List<EnvironmentLocomotion>();
            foreach (var feet in path)
            {
                var state = tracker.Classify(Diver(feet));
                if (states.Count == 0 || states[states.Count - 1] != state) states.Add(state);
            }
            return states;
        }

        private static readonly EnvironmentLocomotion[] ExpectedRoundTrip =
        {
            EnvironmentLocomotion.Land,
            EnvironmentLocomotion.Surface,
            EnvironmentLocomotion.Underwater,
            EnvironmentLocomotion.Surface,
            EnvironmentLocomotion.Land
        };

        // Down the ramp, off its foot into open water, and back the same way: the horizontal
        // crossing, where the diver walks the shore line rather than dropping through it.
        //
        // The ramp alone no longer reaches Underwater. Its foot stops at 7.2 so that a diver
        // standing there is clear of the dive exit zone, which leaves the head well above the
        // water line; the submerged leg is therefore a swim down from the foot into open water.
        private IEnumerable<Vector3> RampPath(float noise)
        {
            var ramp = Require(RampName);
            var top = ramp.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f));
            var foot = ramp.transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0f));
            var deep = new Vector3(foot.x, OpenWaterY, foot.z);

            foreach (var step in Leg(top, foot, noise, 0)) yield return step;
            foreach (var step in Leg(foot, deep, noise, 1)) yield return step;
            foreach (var step in Leg(deep, foot, noise, 2)) yield return step;
            foreach (var step in Leg(foot, top, noise, 3)) yield return step;
        }

        private static IEnumerable<Vector3> Leg(Vector3 from, Vector3 to, float noise, int phase)
        {
            const int steps = 40;
            for (var i = 0; i <= steps; i++)
                yield return Jitter(Vector3.Lerp(from, to, i / (float)steps), phase * (steps + 1) + i, noise);
        }

        // Off the side of the ledge and back up: the vertical crossing, where the diver falls
        // past the surface instead of walking through it.
        private IEnumerable<Vector3> LedgeDropPath(float noise)
        {
            var ledge = Require(LedgeName);
            var x = ledge.transform.position.x + ledge.transform.localScale.x * 0.5f + 0.5f;
            var z = ledge.transform.position.z;
            var top = new Vector3(x, LedgeTopY, z);
            var bottom = new Vector3(x, OpenWaterY, z);

            const int steps = 60;
            for (var i = 0; i <= steps; i++) yield return Jitter(Vector3.Lerp(top, bottom, i / (float)steps), i, noise);
            for (var i = steps; i >= 0; i--) yield return Jitter(Vector3.Lerp(top, bottom, i / (float)steps), i, noise);
        }

        // Deterministic per-tick wobble, the size of a physics jitter rather than a movement.
        private static Vector3 Jitter(Vector3 position, int tick, float noise)
        {
            if (noise <= 0f) return position;
            switch (tick & 3)
            {
                case 0: return position + new Vector3(noise, noise, 0f);
                case 1: return position - new Vector3(noise, noise, 0f);
                case 2: return position + new Vector3(0f, noise, noise);
                default: return position - new Vector3(0f, noise, noise);
            }
        }

        [Test]
        public void ShoreEdgeRoundTrip()
        {
            CollectionAssert.AreEqual(ExpectedRoundTrip, Walk(RampPath(0f)),
                "walking down the ramp and back must cross exactly four times");
        }

        [Test]
        public void LedgeAboveSurfaceRoundTrip()
        {
            CollectionAssert.AreEqual(ExpectedRoundTrip, Walk(LedgeDropPath(0f)),
                "dropping off the ledge and back must cross exactly four times");
        }

        [TestCase("ramp")]
        [TestCase("ledge")]
        public void RoundTripWithTickNoiseStillChangesExactlyFourTimes(string route)
        {
            // +-0.05 m per tick is inside the 0.10 m band, so it must not add a crossing:
            // reversing a state needs 2 x deadband of real movement, which noise cannot fake.
            var path = route == "ramp" ? RampPath(0.05f) : LedgeDropPath(0.05f);
            CollectionAssert.AreEqual(ExpectedRoundTrip, Walk(path),
                "tick noise added or removed a crossing on the " + route);
        }
    }
}
