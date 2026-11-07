using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // #259: the shared house's baked dressing (Resources/CityKit/SharedHouse, built by DeepDive > P4.10 > Build shared house dressing). It must be
    // a pure look: no collider, body or network part, nothing standing in a walking lane or on a bed/storage/PC target, one blanket colour per
    // bed, day/night switching, and it is placed once, on the home origin, by the real rig.
    public sealed class P259SharedHouseTests
    {
        private static GameObject Load()
        {
            var prefab = Resources.Load<GameObject>(HomeDressing.Resource);
            Assert.IsNotNull(prefab, "Resources/" + HomeDressing.Resource + " (run DeepDive > P4.10 > Build shared house dressing)");
            return prefab;
        }

        [Test]
        public void IsOnlyLookNoColliderBodyOrNetworkPart()
        {
            var house = Object.Instantiate(Load());
            try
            {
                Assert.IsEmpty(house.GetComponentsInChildren<Collider>(true), "colliders");
                Assert.IsEmpty(house.GetComponentsInChildren<Rigidbody>(true), "bodies");
                Assert.IsEmpty(house.GetComponentsInChildren<CharacterController>(true), "controllers");
                Assert.IsEmpty(house.GetComponentsInChildren<NetworkObject>(true), "network objects");
                Assert.IsEmpty(house.GetComponentsInChildren<HomeInteractionAnchor>(true), "interaction anchors: the targets stay the rig's");
            }
            finally { Object.DestroyImmediate(house); }
        }

        [Test]
        public void IsFurnishedButWithinABudget()
        {
            var house = Object.Instantiate(Load());
            try
            {
                var renderers = house.GetComponentsInChildren<Renderer>(true).Length;
                var lights = house.GetComponentsInChildren<Light>(true).Length;
                Assert.GreaterOrEqual(renderers, 200, "furnished: wall finish, windows, curtains, lamps, decor, bed dress, PC corner");
                Assert.LessOrEqual(renderers, 900, "renderer budget");
                Assert.LessOrEqual(lights, 6, "light budget (3 pendants + 2 porch lanterns)");
            }
            finally { Object.DestroyImmediate(house); }
        }

        [Test]
        public void StaysOnThePlot()
        {
            var house = Object.Instantiate(Load());
            try
            {
                foreach (var r in house.GetComponentsInChildren<Renderer>(true))
                {
                    var b = r.bounds;
                    Assert.GreaterOrEqual(b.min.x, -8.2f, r.name + " x");
                    Assert.LessOrEqual(b.max.x, 8.2f, r.name + " x");
                    Assert.GreaterOrEqual(b.min.z, -7.6f, r.name + " z");
                    Assert.LessOrEqual(b.max.z, 7.1f, r.name + " z");
                    Assert.GreaterOrEqual(b.min.y, -0.05f, r.name + " y");
                    Assert.LessOrEqual(b.max.y, 7.5f, r.name + " y");
                }
            }
            finally { Object.DestroyImmediate(house); }
        }

        // Lanes the people walk (house frame, metres): the middle of the room where the four spawn, the strip behind storage and PC, the aisle
        // behind the beds' heads and the door line, the gaps between the beds and the corridors at the side walls, and the standing places at the
        // PC and the storage. A look that stands in one of them would be walked through. Flat things (rugs) and wall pieces thinner than the
        // corridor margin are fine.
        private static IEnumerable<(string name, Bounds lane)> Lanes()
        {
            Bounds Box(float x0, float x1, float z0, float z1) => new Bounds(new Vector3((x0 + x1) / 2f, 1.0f, (z0 + z1) / 2f), new Vector3(x1 - x0, 1.78f, z1 - z0));
            yield return ("middle of the room", Box(-3.0f, 3.0f, -3.0f, 2.2f));
            yield return ("strip behind storage and PC", Box(-3.1f, 3.1f, -4.4f, -3.0f));
            yield return ("aisle behind the beds and the door line", Box(-4.9f, 4.9f, 4.55f, 6.0f));
            var xs = ContinuousTownLayout.HomeRig.BedXs;
            for (var i = 0; i < xs.Length - 1; i++) yield return ("gap between beds " + i + "/" + (i + 1), Box(xs[i] + 0.65f, xs[i + 1] - 0.65f, 2.3f, 4.6f));
            yield return ("east corridor", Box(xs[3] + 0.625f, 5.55f, 2.3f, 4.6f));
            yield return ("west corridor", Box(-5.55f, xs[0] - 0.625f, 2.3f, 4.6f));
            yield return ("PC standing place", Box(3.0f, 4.8f, -3.0f, -1.9f));
            yield return ("storage standing place", Box(-4.8f, -3.0f, -3.0f, -1.9f));
        }

        [Test]
        public void NothingStandsInAWalkingLane()
        {
            var house = Object.Instantiate(Load());
            try
            {
                foreach (var (laneName, lane) in Lanes())
                    foreach (var r in house.GetComponentsInChildren<Renderer>(true))
                    {
                        var b = r.bounds;
                        if (b.max.y <= 0.12f || b.min.y >= 1.9f) continue;       // flat on the floor (rugs) or above head height
                        Assert.IsFalse(b.Intersects(lane), $"{r.transform.parent?.name}/{r.name} (x {b.min.x:0.00}..{b.max.x:0.00}, z {b.min.z:0.00}..{b.max.z:0.00}) stands in the {laneName}");
                    }
            }
            finally { Object.DestroyImmediate(house); }
        }

        [Test]
        public void EachBedHasItsOwnBlanketColourOnItsBed()
        {
            var house = Object.Instantiate(Load());
            try
            {
                var beds = house.transform.Find("Beds").Cast<Transform>().Where(t => t.name.StartsWith("BedDress_")).ToArray();
                Assert.AreEqual(DayIds.BedCount, beds.Length, "one dressed bed per bed");
                var colours = new HashSet<Color>();
                for (var i = 0; i < beds.Length; i++)
                {
                    var bed = beds[i];
                    Assert.AreEqual(ContinuousTownLayout.HomeRig.BedXs[i], bed.localPosition.x, 1e-3f, bed.name + " x");
                    Assert.AreEqual(ContinuousTownLayout.HomeRig.BedLocalZ, bed.localPosition.z, 1e-3f, bed.name + " z");
                    var blanket = bed.Find("Blanket").GetComponent<Renderer>();
                    Assert.IsTrue(colours.Add(blanket.sharedMaterial.GetColor("_BaseColor")), bed.name + " has its own colour");
                    // on top of the bed (its top is at 0.475) and inside its 1.25 x 2.05 footprint
                    Assert.GreaterOrEqual(blanket.bounds.min.y, 0.47f, bed.name + " rests on the mattress");
                    Assert.LessOrEqual(blanket.bounds.size.x, 1.25f, bed.name + " width");
                    Assert.LessOrEqual(blanket.bounds.size.z, 2.05f, bed.name + " length");
                }
            }
            finally { Object.DestroyImmediate(house); }
        }

        [Test]
        public void PcCornerHasAMonitorAndAChannelBoardThatLightWithTheNight()
        {
            var house = Object.Instantiate(Load());
            try
            {
                var corner = house.transform.Find("PcCorner");
                Assert.IsNotNull(corner.Find("MonitorScreen"));
                var board = corner.Find("ChannelBoard");
                Assert.IsNotNull(board);
                Assert.AreEqual("KANAL", board.GetComponentInChildren<TextMesh>().text);
                var lighting = house.GetComponent<DressingLighting>();
                Assert.AreEqual(2, lighting.Screens.Length, "monitor and board");
                Assert.AreEqual(0, lighting.Screens.Count(s => s == null));
                // The signs read from where people stand: the porch sign from the street, the board from inside the room (a TextMesh reads along +Z)
                foreach (var text in house.GetComponentsInChildren<TextMesh>(true))
                    Assert.Less(Quaternion.Angle(text.transform.rotation, Quaternion.Euler(0f, 180f, 0f)), 1f, text.text + " is turned to its readers");
            }
            finally { Object.DestroyImmediate(house); }
        }

        [Test]
        public void DayAndNightSwitchWindowsLampsAndScreens()
        {
            var house = Object.Instantiate(Load());
            try
            {
                var l = house.GetComponent<DressingLighting>();
                Assert.GreaterOrEqual(l.Panes.Length, 12, "6 inside + 6 outside windows");
                Assert.AreEqual(5, l.Lamps.Length, "3 pendants + 2 lanterns");
                l.Apply(0f);
                Assert.IsTrue(l.Panes.All(p => p.sharedMaterial == l.PaneDay), "bright windows by day");
                Assert.IsTrue(l.Lamps.All(x => !x.enabled), "lamps off by day");
                Assert.IsTrue(l.Bulbs.All(b => b.sharedMaterial == l.BulbOff));
                l.Apply(1f);
                Assert.IsTrue(l.Panes.All(p => p.sharedMaterial == l.PaneNight), "dark windows at night");
                Assert.IsTrue(l.Lamps.All(x => x.enabled && x.intensity > 0.5f), "lamps on at night");
                Assert.IsTrue(l.Bulbs.All(b => b.sharedMaterial == l.BulbOn));
                Assert.IsTrue(l.Screens.All(s => s.sharedMaterial == l.ScreenNight));
                l.Apply(0.4f);   // dusk: lamps already glowing, windows still pale
                Assert.IsTrue(l.Panes.All(p => p.sharedMaterial == l.PaneDay));
                Assert.IsTrue(l.Lamps.All(x => x.enabled && x.intensity > 0f && x.intensity < l.LampIntensity));
                l.Apply(0f);
                Assert.IsTrue(l.Lamps.All(x => !x.enabled), "and back to day");
            }
            finally { Object.DestroyImmediate(house); }
        }

        [Test]
        public void TheRigPlacesItOnceOnTheHomeOriginAndTheTargetsStayTheRigs()
        {
            ContinuousTownScene.With(scene =>
            {
                HomeSceneSetup.EnsureRig(scene);
                HomeSceneSetup.EnsureRig(scene);   // idempotent
                var roots = scene.GetRootGameObjects().Where(r => r.name == HomeDressing.RootName).ToArray();
                Assert.AreEqual(1, roots.Length, "one dressing");
                Assert.AreEqual(ContinuousTownLayout.HomeOrigin.x, roots[0].transform.position.x, 1e-3f);
                Assert.AreEqual(ContinuousTownLayout.HomeOrigin.y, roots[0].transform.position.y, 1e-3f);
                Assert.AreEqual(ContinuousTownLayout.HomeOrigin.z, roots[0].transform.position.z, 1e-3f);
                Assert.IsEmpty(roots[0].GetComponentsInChildren<Collider>(true));
                var rig = scene.GetRootGameObjects().Single(r => r.name == HomeSceneSetup.RootName);
                Assert.AreEqual(DayIds.BedCount, rig.GetComponentsInChildren<HomeInteractionAnchor>(true).Count(a => a.Kind == HomeInteractionKind.Bed), "four beds");
                Assert.AreEqual(1, rig.GetComponentsInChildren<HomeInteractionAnchor>(true).Count(a => a.Kind == HomeInteractionKind.Storage), "one storage");
            });
        }

        [Test]
        public void DevelopmentVisualsStandInTheRealStoreAndOnTheRealHouseWall()
        {
            ContinuousTownScene.With(scene =>
            {
                var store = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EquipmentStoreLayout>(true)).Single();
                // The shop upgrade rack: 2.2 x 1.9 x 0.45 m against the street wall, west of the door, clear of every collider of the store.
                var yaw = Quaternion.Euler(0f, ContinuousTownLayout.EquipmentStoreYaw, 0f);
                var origin = ContinuousTownLayout.StorefrontPositions[0] + ContinuousTownLayout.EquipmentStoreOffset;
                var centre = origin + yaw * (DevelopmentVisual.StoreRackLocal + new Vector3(0f, 0.95f, 0f));
                Physics.SyncTransforms();
                var hits = Physics.OverlapBox(centre, new Vector3(1.05f, 0.9f, 0.2f), yaw, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsEmpty(hits.Select(h => h.name), "the shop upgrade rack does not stand in the store's walls or shelves");
                var inside = store.transform.InverseTransformPoint(centre);
                Assert.Less(Mathf.Abs(inside.x + 2.8f), 0.05f, "rack is where the data says (store frame)");
                Assert.Greater(Mathf.Abs(inside.x) - 1.1f, 1.3f, "and stays out of the 2.6 m door line");
                // The home upgrade: two wall shelves (x +-1.3) on the back wall above the couch, between the two back windows (their curtains reach x +-1.8).
                var shelf = ContinuousTownLayout.HomeOrigin + DevelopmentVisual.TownHomeShelfLocal;
                Assert.AreEqual(ContinuousTownLayout.HomeOrigin.z - 5.7f, shelf.z, 1e-3f);
            });
        }
    }
}
