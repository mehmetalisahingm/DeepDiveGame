using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World.Tests
{
    // Guards the P3.2 half of DiveTestArea: the south coastline, the shallow shelf, the three
    // free boat parts, the town-path marker and the depth band. Everything asserted here is
    // produced by DiveTestAreaBeachSetup.Apply; if this fails, running that menu item again is
    // the repair, not hand-editing the scene.
    //
    // Expectations are written out rather than read from the setup script, the same way the P2,
    // P3.1 and P3-B scene tests do it - and for the same reason: a guard that reads its
    // expectations from the script it is guarding would follow that script anywhere, including
    // into a mistake. DeepDive.World.Tests does not reference DeepDive.World.Editor and is not
    // given that reference here.
    //
    // Still EditMode: no rig, no play mode, no netcode. The two-player smoke that closes issue
    // #61 needs the diver rig and is not attempted here.
    public class DiveTestAreaBeachSceneTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string DiverPrefabPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";

        private const string PlatformName = "Beach_Platform";
        private const string WadeName = "Beach_Wade";
        private const string HullName = "BoatPart_Hull";
        private const string EngineName = "BoatPart_Engine";
        private const string FuelTankName = "BoatPart_FuelTank";
        private const string TownGateName = "Beach_TownGate";
        private const string DepthBandsName = "DepthBands";

        // --- The beach, as authored ----------------------------------------------------------

        private static readonly Vector3 PlatformCenter = new Vector3(0f, 4.2f, -12.875f);
        private static readonly Vector3 PlatformSize = new Vector3(29.5f, 8.4f, 3.75f);
        private const float PlatformTopY = 8.4f;
        private const float PlatformNorthZ = -11f;

        private static readonly Vector3 WadeCenter = new Vector3(-6.5f, 7.60437f, -8.21882f);
        private static readonly Vector3 WadeSize = new Vector3(6f, 0.4f, 5.7717f);
        private const float WadeAngleDegrees = 12f;
        private const float WadeMinX = -9.5f;
        private const float WadeMaxX = -3.5f;
        private const float WadeTopZ = -11f;
        private const float WadeFootZ = -5.354445f;
        private const float WadeFootY = 7.2f;

        private static readonly Vector3 HullPosition = new Vector3(-12f, 8.4f, -12.5f);
        private static readonly Vector3 EnginePosition = new Vector3(-6.5f, 7.762326f, -8f);
        private static readonly Vector3 FuelTankPosition = new Vector3(12.5f, 8.4f, -12.5f);
        private static readonly Vector3 TownGatePosition = new Vector3(0f, 8.4f, -13.5f);
        private const float AnchorSize = 0.6f;

        // --- The arena it has to live with ---------------------------------------------------

        private const float SurfaceY = 8f;
        private const float ArenaInnerExtent = 14.75f;

        // A depth in open water past the foot of the wade, reached by swimming rather than
        // walking. Matches the P3.1 shore test's OpenWaterY.
        private const float OpenWaterY = 5f;

        // DiveExit is now a dedicated dry return pad on Shore_Ledge rather than a whole-arena
        // y=6..8 volume. Copied as plain numbers because this assembly does not reference
        // Composition. SafeReturnZone tests the diver at position + up * 0.9.
        private static readonly Vector3 ExitCenter = new Vector3(9f, 9.3f, -9f);
        private static readonly Vector3 ExitSize = new Vector3(3.5f, 2f, 3.5f);
        private const float ExitZoneProbeHeight = 0.9f;

        // The spawn ring, the event and the fish wander box the beach has to stay clear of.
        private const float SpawnRingExtent = 3f;
        private static readonly Vector3 EventCenter = new Vector3(-9f, 3.5f, -2f);
        private const float EventRadius = 1.5f;
        private static readonly Vector3 FishHome = new Vector3(0f, 4f, 8f);
        private const float FishWanderRadius = 6f;
        private const float FishVerticalWanderFactor = 0.5f;

        // P3.1's shore and P2's arena, pinned so the beach cannot have moved them.
        private static readonly Vector3 LedgeCenter = new Vector3(9f, 7.9f, -9f);
        private static readonly Vector3 LedgeSize = new Vector3(4f, 1f, 4f);
        private static readonly Vector3 SwimCenter = new Vector3(0f, 4f, 0f);
        private static readonly Vector3 SwimSize = new Vector3(30f, 8f, 30f);

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

        private GameObject Require(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            Assert.Fail("DiveTestArea is missing " + name + "; run DeepDive/P3.2/Dive test area");
            return null;
        }

        private static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<T>(true));
            return found;
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

        // Height of the wade's walkable face at a z along it.
        private static float WadeSurfaceYAt(float z) =>
            Mathf.Lerp(PlatformTopY, WadeFootY, Mathf.InverseLerp(WadeTopZ, WadeFootZ, z));

        private static bool InsideExitZone(Vector3 stand)
        {
            var probe = stand + Vector3.up * ExitZoneProbeHeight;
            var local = probe - ExitCenter;
            var half = ExitSize * 0.5f;
            return Mathf.Abs(local.x) <= half.x
                   && Mathf.Abs(local.y) <= half.y
                   && Mathf.Abs(local.z) <= half.z;
        }

        // --- Geometry ------------------------------------------------------------------------

        [Test]
        public void BeachPlatformAndWadeSitAtTheDocumentedTransforms()
        {
            var platform = Require(PlatformName);
            Assert.AreEqual(PlatformCenter, platform.transform.position, "platform centre moved");
            Assert.AreEqual(PlatformSize, platform.transform.localScale, "platform size moved");
            Assert.AreEqual(Quaternion.identity, platform.transform.rotation, "platform must stay axis aligned");

            var wade = Require(WadeName);
            Assert.AreEqual(WadeCenter.x, wade.transform.position.x, 0.001f, "wade x");
            Assert.AreEqual(WadeCenter.y, wade.transform.position.y, 0.001f, "wade y");
            Assert.AreEqual(WadeCenter.z, wade.transform.position.z, 0.001f, "wade z");
            Assert.AreEqual(WadeSize.x, wade.transform.localScale.x, 0.001f, "wade width");
            Assert.AreEqual(WadeSize.y, wade.transform.localScale.y, 0.001f, "wade thickness");
            Assert.AreEqual(WadeSize.z, wade.transform.localScale.z, 0.001f, "wade slope length");

            // The slope the diver walks, measured from the face rather than from the Euler
            // angles: this is what pins the rotation's sign, not just its size.
            var slope = Vector3.Angle(wade.transform.up, Vector3.up);
            Assert.AreEqual(WadeAngleDegrees, slope, 0.01f, "wade slope moved");
            Assert.Less(slope, DiverController().slopeLimit, "wade must be inside the diver's slopeLimit");

            // Local +Z on a unit cube's top face is the downhill end; it has to be the north,
            // wet one. If the rotation flipped, this would be the high end instead.
            var high = wade.transform.TransformPoint(new Vector3(0f, 0.5f, -0.5f));
            var low = wade.transform.TransformPoint(new Vector3(0f, 0.5f, 0.5f));
            Assert.AreEqual(PlatformTopY, high.y, 0.01f, "the wade's high end must meet the platform top");
            Assert.AreEqual(WadeTopZ, high.z, 0.01f, "the wade's high end must sit at the platform's north face");
            Assert.AreEqual(WadeFootY, low.y, 0.01f, "the wade's foot must stop at 7.2");
            Assert.AreEqual(WadeFootZ, low.z, 0.01f, "the wade's foot must reach z -5.354");
        }

        [Test]
        public void TheBeachMeetsTheP31LedgeFlush()
        {
            var platform = Require(PlatformName);
            var ledge = Require("Shore_Ledge");
            var controller = DiverController();

            var platformNorth = platform.transform.position.z + platform.transform.localScale.z * 0.5f;
            var ledgeSouth = ledge.transform.position.z - ledge.transform.localScale.z * 0.5f;
            Assert.AreEqual(ledgeSouth, platformNorth, 0.001f, "the strip must meet the ledge's south face");

            var platformTop = platform.transform.position.y + platform.transform.localScale.y * 0.5f;
            var ledgeTop = ledge.transform.position.y + ledge.transform.localScale.y * 0.5f;
            Assert.LessOrEqual(Mathf.Abs(platformTop - ledgeTop), controller.stepOffset,
                "the step between beach and ledge is taller than stepOffset");
        }

        [Test]
        public void TheWadeMeetsThePlatformFlushAndItsFootStaysUnderTheWaterLine()
        {
            var controller = DiverController();
            var wade = Require(WadeName);
            var high = wade.transform.TransformPoint(new Vector3(0f, 0.5f, -0.5f));
            var low = wade.transform.TransformPoint(new Vector3(0f, 0.5f, 0.5f));

            Assert.LessOrEqual(Mathf.Abs(high.y - PlatformTopY), controller.stepOffset,
                "the step onto the wade is taller than stepOffset");

            // Under the water line, or the shelf leads nowhere...
            Assert.Less(low.y, Field().Bodies[0].SurfaceY, "the wade must reach under the surface");

            // ...and above the exit zone, or standing on it would pay the diver out.
            Assert.IsFalse(InsideExitZone(low), "the wade foot must stay clear of the dive exit zone");
        }

        [Test]
        public void TheBeachRunsWallToWallWithNoGapBehindIt()
        {
            var platform = Require(PlatformName);
            var half = platform.transform.localScale * 0.5f;
            var centre = platform.transform.position;

            Assert.AreEqual(-ArenaInnerExtent, centre.x - half.x, 0.001f, "west edge must reach the wall face");
            Assert.AreEqual(ArenaInnerExtent, centre.x + half.x, 0.001f, "east edge must reach the wall face");
            Assert.AreEqual(-ArenaInnerExtent, centre.z - half.z, 0.001f, "south edge must reach the wall face");
        }

        // --- The crossing --------------------------------------------------------------------

        // Walks a list of feet positions through one tracker and returns the states it settled
        // in, repeats collapsed.
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

        // Dry sand, down the wade to its foot, off into open water, and back the same way.
        // The wade alone never reaches Underwater: its foot stops at 7.2 to stay clear of the
        // exit zone, which leaves the diver's head well above the line, so the submerged leg is
        // a swim down from the foot.
        private IEnumerable<Vector3> BeachPath(float noise)
        {
            var dry = new Vector3(WadeCenter.x, PlatformTopY, -12.5f);
            var top = new Vector3(WadeCenter.x, PlatformTopY, WadeTopZ);
            var foot = new Vector3(WadeCenter.x, WadeFootY, WadeFootZ);
            var deep = new Vector3(WadeCenter.x, OpenWaterY, WadeFootZ);

            foreach (var step in Leg(dry, top, noise, 0)) yield return step;
            foreach (var step in Leg(top, foot, noise, 1)) yield return step;
            foreach (var step in Leg(foot, deep, noise, 2)) yield return step;
            foreach (var step in Leg(deep, foot, noise, 3)) yield return step;
            foreach (var step in Leg(foot, top, noise, 4)) yield return step;
            foreach (var step in Leg(top, dry, noise, 5)) yield return step;
        }

        private static IEnumerable<Vector3> Leg(Vector3 from, Vector3 to, float noise, int phase)
        {
            const int steps = 40;
            for (var i = 0; i <= steps; i++)
                yield return Jitter(Vector3.Lerp(from, to, i / (float)steps), phase * (steps + 1) + i, noise);
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
        public void WalkingFromTheBeachIntoTheShallowsAndBackCrossesExactlyFourTimes()
        {
            CollectionAssert.AreEqual(ExpectedRoundTrip, Walk(BeachPath(0f)),
                "walking in off the beach and back must cross exactly four times");
        }

        [Test]
        public void BeachRoundTripWithTickNoiseStillChangesExactlyFourTimes()
        {
            // +-0.05 m per tick is inside the 0.10 m band, so it must not add a crossing:
            // reversing a state needs 2 x deadband of real movement, which noise cannot fake.
            CollectionAssert.AreEqual(ExpectedRoundTrip, Walk(BeachPath(0.05f)),
                "tick noise added or removed a crossing on the beach");
        }

        [Test]
        public void SteppingOffTheBeachEdgeLandsInWaterNotTheVoid()
        {
            var field = Field();
            var body = field.Bodies[0];

            // Points along the platform's north face that are clear of both the P3.1 ledge
            // (x 7..11) and the wade (x -9.5..-3.5), so stepping north is a real drop.
            foreach (var x in new[] { -13f, -1f, 3f, 14f })
            {
                var step = new Vector3(x, PlatformTopY, PlatformNorthZ + 0.5f);
                Assert.GreaterOrEqual(body.SignedInset(step.x, step.z), 0f,
                    "stepping off at " + step + " leaves the water footprint");
                Assert.AreEqual(EnvironmentLocomotion.Land, field.CreateTracker().Classify(Diver(step)),
                    "the air beside the beach should read as Land");

                var landed = new Vector3(step.x, OpenWaterY, step.z);
                Assert.AreEqual(EnvironmentLocomotion.Underwater, field.CreateTracker().Classify(Diver(landed)),
                    "falling off at " + step + " must end underwater");
            }
        }

        [Test]
        public void NoBeachWalkableSurfaceSitsInTheSafeReturnZone()
        {
            // The dedicated return pad lives on the separate east ledge. The beach/wade route
            // must stay outside it so #61 can extend the wade deeper without false Returned.
            var samples = new List<Vector3>();

            var platform = Require(PlatformName);
            var half = platform.transform.localScale * 0.5f;
            var centre = platform.transform.position;
            for (var sx = -1; sx <= 1; sx++)
                for (var sz = -1; sz <= 1; sz++)
                    samples.Add(new Vector3(centre.x + sx * half.x, PlatformTopY, centre.z + sz * half.z));

            for (var i = 0; i <= 20; i++)
            {
                var z = Mathf.Lerp(WadeTopZ, WadeFootZ, i / 20f);
                samples.Add(new Vector3(WadeCenter.x, WadeSurfaceYAt(z), z));
            }

            foreach (var stand in samples)
                Assert.IsFalse(InsideExitZone(stand),
                    "standing at " + stand + " is tested at " + (stand + Vector3.up * ExitZoneProbeHeight) +
                    ", inside the dive exit zone");
        }

        // --- The measured x range --------------------------------------------------------------

        [Test]
        public void TheShallowStripOverlapsTheFishCorridorButNotTheSpawnRing()
        {
            var wade = Require(WadeName);
            var minX = wade.transform.position.x - wade.transform.localScale.x * 0.5f;
            var maxX = wade.transform.position.x + wade.transform.localScale.x * 0.5f;
            Assert.AreEqual(WadeMinX, minX, 0.001f, "the strip's west edge moved");
            Assert.AreEqual(WadeMaxX, maxX, 0.001f, "the strip's east edge moved");

            // In the fish's x corridor: the shelf has to lead somewhere worth swimming to.
            var overlap = Mathf.Min(maxX, FishHome.x + FishWanderRadius) -
                          Mathf.Max(minX, FishHome.x - FishWanderRadius);
            Assert.AreEqual(2.5f, overlap, 0.001f, "the strip's overlap with the fish corridor moved");

            // And out of the spawn ring: divers must not surface into the shelf.
            Assert.Less(maxX, -SpawnRingExtent, "the strip must stay west of the spawn ring");
            Assert.AreEqual(0.5f, -SpawnRingExtent - maxX, 0.001f, "spawn ring clearance moved");
        }

        [Test]
        public void TheBeachStaysClearOfEverySpawnPoint()
        {
            var spawns = FindAll<PlayerSpawnPoint>(scene);
            Assert.AreEqual(4, spawns.Count, "DiveTestArea must hold four spawn points");

            foreach (var name in new[] { PlatformName, WadeName })
            {
                var bounds = Require(name).GetComponent<Renderer>().bounds;
                foreach (var spawn in spawns)
                    Assert.IsFalse(bounds.Contains(spawn.transform.position),
                        name + " swallows " + spawn.name);
            }
        }

        [Test]
        public void TheBeachStaysOutOfTheFishWanderBox()
        {
            var reach = new Vector3(FishWanderRadius, FishWanderRadius * FishVerticalWanderFactor, FishWanderRadius);
            var wander = new Bounds(FishHome, reach * 2f);

            foreach (var name in new[] { PlatformName, WadeName })
            {
                var bounds = Require(name).GetComponent<Renderer>().bounds;
                Assert.IsFalse(bounds.Intersects(wander),
                    name + " overlaps the fish wander box " + wander + "; the fish would be pinned against it");
            }
        }

        [Test]
        public void TheBeachStaysClearOfTheSpecialEvent()
        {
            var eventBounds = new Bounds(EventCenter, Vector3.one * (EventRadius * 2f));
            foreach (var name in new[] { PlatformName, WadeName })
            {
                var bounds = Require(name).GetComponent<Renderer>().bounds;
                Assert.IsFalse(bounds.Intersects(eventBounds),
                    name + " overlaps the bioluminescence event");
            }
        }

        [Test]
        public void TheBeachIsSolidSceneryOnTheDefaultLayer()
        {
            foreach (var name in new[] { PlatformName, WadeName })
            {
                var piece = Require(name);
                Assert.AreEqual(0, piece.layer, name + " must stay on Default");

                var box = piece.GetComponent<BoxCollider>();
                Assert.IsNotNull(box, name + " needs a BoxCollider to stand on");
                Assert.IsFalse(box.isTrigger, name + " must be solid, not a trigger");

                Assert.IsNull(piece.GetComponent<NetworkObject>(), name + " must not be networked");
            }
        }

        // --- The three free boat parts ---------------------------------------------------------

        private List<BoatPartAnchor> Anchors() => FindAll<BoatPartAnchor>(scene);

        [Test]
        public void ThreeBoatPartAnchorsCarryExactlyTheContractIds()
        {
            var anchors = Anchors();
            Assert.AreEqual(3, anchors.Count, "the beach must hold exactly three boat part anchors");

            var ids = anchors.Select(a => a.PartId).ToArray();
            CollectionAssert.AreEquivalent(
                new[] { "boat-part-hull", "boat-part-engine", "boat-part-fuel-tank" }, ids,
                "the anchors must carry the three save-visible ids");
            CollectionAssert.AreEquivalent(BoatRepairParts.All, ids,
                "the anchors and the economy must agree on the ids");
            CollectionAssert.AllItemsAreUnique(ids, "two anchors claim the same part");
        }

        [Test]
        public void BoatPartAnchorsSitAtTheDocumentedPositions()
        {
            Assert.AreEqual(HullPosition, Require(HullName).transform.position, "hull moved");
            var engine = Require(EngineName).transform.position;
            Assert.AreEqual(EnginePosition.x, engine.x, 0.001f, "engine x");
            Assert.AreEqual(EnginePosition.y, engine.y, 0.001f, "engine y must sit on the wade face");
            Assert.AreEqual(EnginePosition.z, engine.z, 0.001f, "engine z");
            Assert.AreEqual(FuelTankPosition, Require(FuelTankName).transform.position, "fuel tank moved");

            foreach (var name in new[] { HullName, EngineName, FuelTankName })
                Assert.AreEqual(Vector3.one * AnchorSize, Require(name).transform.localScale, name + " scale");
        }

        [Test]
        public void TheEngineSitsOnTheWadeFaceNotInsideOrAboveIt()
        {
            var engine = Require(EngineName).transform.position;
            Assert.AreEqual(WadeSurfaceYAt(engine.z), engine.y, 0.01f,
                "the engine must rest on the slope, not float over it or sink into it");
            Assert.GreaterOrEqual(engine.x, WadeMinX, "the engine must sit on the shelf");
            Assert.LessOrEqual(engine.x, WadeMaxX, "the engine must sit on the shelf");
        }

        [Test]
        public void EveryBoatPartIsReachableWithoutSwimming()
        {
            // Free, solo and co-op: a diver can walk or wade to every part. None of them needs a
            // boat, a camera or deep water, so none may classify as Underwater.
            var field = Field();
            foreach (var anchor in Anchors())
            {
                var state = field.CreateTracker().Classify(Diver(anchor.WorldPosition));
                Assert.AreNotEqual(EnvironmentLocomotion.Underwater, state,
                    anchor.PartId + " would need a swim to reach");
            }
        }

        [Test]
        public void NoBoatPartSitsInTheSafeReturnZone()
        {
            // Walking up to a part must not be worth money on its own.
            foreach (var anchor in Anchors())
                Assert.IsFalse(InsideExitZone(anchor.WorldPosition),
                    anchor.PartId + " stands inside the dive exit zone");
        }

        [Test]
        public void BoatPartAnchorsAreSceneryWithNoNetworkIdentity()
        {
            foreach (var anchor in Anchors())
            {
                Assert.AreEqual(0, anchor.gameObject.layer, anchor.name + " must stay on Default");
                Assert.IsNull(anchor.GetComponent<NetworkObject>(), anchor.name + " must not be networked");

                // A marker volume for Mehmet's interaction, not something to trip over.
                var box = anchor.GetComponent<BoxCollider>();
                Assert.IsNotNull(box, anchor.name + " needs a collider for interaction to test against");
                Assert.IsTrue(box.isTrigger, anchor.name + " must not block the beach");
            }
        }

        // --- The town gate ---------------------------------------------------------------------

        [Test]
        public void TheTownGateIsABareMarker()
        {
            var gate = Require(TownGateName);
            Assert.AreEqual(TownGatePosition, gate.transform.position, "the town gate moved");

            // Nothing but a Transform. The gate documents where the town path is expected to
            // meet the sand; it makes no claim about what will stand there. The three NPC service
            // points (P3.2-C) stand on the beach strip beside it since PR #65 made DiveTestArea the
            // Prep/Dive/Return scene; TownSceneTests pins where and keeps them clear of the gate.
            Assert.AreEqual(1, gate.GetComponents<Component>().Length,
                "the town gate must stay a bare transform");
        }

        // --- The depth band is not a second water authority ---------------------------------------

        [Test]
        public void WaterFieldStillReadsExactlyOneSwimVolume()
        {
            var field = Field();
            var volumes = FindAll<SwimVolume>(scene);
            Assert.AreEqual(1, volumes.Count, "DiveTestArea must hold exactly one SwimVolume");
            Assert.AreEqual(1, field.VolumeCount, "the beach must not have added a water volume");
            Assert.AreEqual(1, field.Bodies.Count, "the field must still read exactly one body");

            var body = field.Bodies[0];
            Assert.AreEqual(SurfaceY, body.SurfaceY, 0.001f, "the water line moved");
            Assert.AreEqual(-15f, body.MinX);
            Assert.AreEqual(15f, body.MaxX);
            Assert.AreEqual(-15f, body.MinZ);
            Assert.AreEqual(15f, body.MaxZ);
        }

        [Test]
        public void TheDepthBandsWaterLineMatchesTheWaterField()
        {
            var bands = FindAll<DiveDepthBandSet>(scene);
            Assert.AreEqual(1, bands.Count, "DiveTestArea must hold exactly one DiveDepthBandSet");
            Assert.AreEqual(SurfaceY, bands[0].SurfaceY, 0.001f, "the band's water line moved");
            Assert.AreEqual(Field().Bodies[0].SurfaceY, bands[0].SurfaceY, 0.001f,
                "the band and the water field must read the same surface");

            // And the band is not something the locomotion seam could pick up.
            Assert.IsFalse(typeof(IWaterField).IsAssignableFrom(bands[0].GetType()));
        }

        [Test]
        public void TheWholeArenaColumnAndTheFishSitInsideTheShallowBand()
        {
            var bands = FindAll<DiveDepthBandSet>(scene)[0];

            for (var y = 0f; y <= SurfaceY; y += 0.5f)
                Assert.IsTrue(bands.TryClassify(y, out _), "y=" + y + " must be shallow");

            // The whole wander box, not just the home: a fish that roams out of the band would
            // be hunted in water the band says does not exist.
            foreach (var fish in FindAll<FishActor>(scene))
            {
                Assert.IsNotNull(fish.Species, fish.name + " has no species");
                var reach = fish.Species.Swim.WanderRadius * FishVerticalWanderFactor;
                var home = fish.transform.position.y;
                foreach (var y in new[] { home + reach, home, home - reach })
                    Assert.IsTrue(bands.TryClassify(y, out _),
                        fish.name + " wanders to y=" + y + ", outside the shallow band");
            }

            // Dry sand is not shallow water.
            Assert.IsFalse(bands.TryClassify(PlatformTopY, out _), "the beach top must not read as shallow water");
        }

        // The sample path the two proofs below both measure.
        private static IEnumerable<Vector3> ClassificationProbePath()
        {
            yield return new Vector3(WadeCenter.x, PlatformTopY, -12.5f);
            yield return new Vector3(WadeCenter.x, WadeFootY, WadeFootZ);
            yield return new Vector3(WadeCenter.x, OpenWaterY, WadeFootZ);
            yield return new Vector3(0f, 0.5f, 0f);
            yield return new Vector3(0f, 7.5f, 0f);
            yield return new Vector3(20f, 2f, 0f);
            yield return HullPosition;
            yield return EnginePosition;
            yield return FuelTankPosition;
        }

        private List<EnvironmentLocomotion> ClassifyProbePath()
        {
            var field = Field();
            return ClassificationProbePath()
                .Select(p => field.CreateTracker().Classify(Diver(p)))
                .ToList();
        }

        [Test]
        public void RemovingTheDepthBandsDoesNotChangeAnyWaterClassification()
        {
            // The proof behind "the band is not a second water authority". The arguments - it
            // does not implement IWaterField, it never names EnvironmentLocomotion, the binding
            // finds WaterField by type - are all structural. This deletes the object outright
            // and shows the water answers identically, so a hidden link would turn this red.
            //
            // The destruction happens in this fixture's own additively opened copy and the
            // scene is never saved; TearDown closes it with removeScene:true. opened is forced
            // true afterwards so the mutated copy is closed even when SetUp found the scene
            // already loaded, and cannot leak into the next test.
            var before = ClassifyProbePath();

            Object.DestroyImmediate(Require(DepthBandsName));
            opened = true;

            Assert.IsEmpty(FindAll<DiveDepthBandSet>(scene), "the band set should be gone for this measurement");
            CollectionAssert.AreEqual(before, ClassifyProbePath(),
                "removing the depth band changed a water classification, so it was feeding the locomotion seam");
        }

        [Test]
        public void WaterClassificationComesFromSwimVolumeAloneNotFromAnythingElseInTheScene()
        {
            // The same claim without touching the scene: build the water by hand from
            // SwimVolume's numbers and run the pure rules over it. If any object in the scene -
            // the depth band, the beach, an anchor - contributed to the classification, the
            // field's answer would diverge from this one.
            Assert.IsTrue(WaterBody.TryFromBox(SwimCenter, SwimSize, out var handBuilt));

            var field = Field();
            foreach (var probe in ClassificationProbePath())
            {
                var fromScene = field.CreateTracker().Classify(Diver(probe));
                var fromNumbers = WaterRules.ClassifyRaw(new[] { handBuilt }, Diver(probe));
                Assert.AreEqual(fromNumbers, fromScene,
                    "the scene classified " + probe + " differently from the one hand-built SwimVolume");
            }
        }

        // --- Nothing else moved -----------------------------------------------------------------

        [Test]
        public void TheP31ShoreAndTheP2ArenaAreUnchanged()
        {
            var ledge = Require("Shore_Ledge");
            Assert.AreEqual(LedgeCenter, ledge.transform.position, "Shore_Ledge moved");
            Assert.AreEqual(LedgeSize, ledge.transform.localScale, "Shore_Ledge resized");

            var ramp = Require("Shore_Ramp");
            Assert.AreEqual(25f, Vector3.Angle(ramp.transform.up, Vector3.up), 0.01f, "Shore_Ramp slope moved");

            var swim = Require("SwimVolume");
            Assert.AreEqual(SwimCenter, swim.transform.position, "SwimVolume moved");
            Assert.AreEqual(SwimSize, swim.transform.localScale, "SwimVolume resized");

            var exit = Require("DiveExit");
            Assert.AreEqual(ExitCenter, exit.transform.position, "DiveExit moved");
            var exitBox = exit.GetComponent<BoxCollider>();
            Assert.AreEqual(ExitSize, exitBox.size, "DiveExit resized");
            Assert.IsTrue(exitBox.isTrigger, "DiveExit must stay a trigger");

            foreach (var spawn in FindAll<PlayerSpawnPoint>(scene))
            {
                var p = spawn.transform.position;
                Assert.AreEqual(SpawnRingExtent, Mathf.Abs(p.x), 0.001f, spawn.name + " x");
                Assert.AreEqual(1f, p.y, 0.001f, spawn.name + " y");
                Assert.AreEqual(SpawnRingExtent, Mathf.Abs(p.z), 0.001f, spawn.name + " z");
            }
        }
    }
}
