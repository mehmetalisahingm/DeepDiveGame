using System.Linq;
using System.Reflection;
using DeepDive.Composition;
using DeepDive.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #189 P4.8-C: pins what the authored repair worksite promises BoatRepairNetworkBinding and the crew: presentation only
    // (no colliders), a broken hull whose three bays stay open for the R01 parts, parts that keep OakRepairPatch's nail surface
    // and stay inside their own slot, and a bench that sits under the repair-drop-0 racks.
    public sealed class RepairSetArtTests
    {
        private static readonly string[] All = { BoatRepairSetArt.DamagedHull, BoatRepairSetArt.PartResources[0], BoatRepairSetArt.PartResources[1], BoatRepairSetArt.PartResources[2], BoatRepairSetArt.Trestle };
        // OakRepairPatch: three 0.215 m slats, 0.225 m pitch, 0.58 m wide, 0.06 m thick.
        private static readonly Vector3 PanelHalf = new Vector3(.28f, .33f, .025f);

        private static GameObject Load(string resource)
        {
            var prefab = Resources.Load<GameObject>(resource);
            Assert.IsNotNull(prefab, resource);
            return prefab;
        }

        private static void AddMeshColliders(GameObject go)
        {
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        [Test]
        public void EverythingIsPresentationWithRealMeshesAndKitMaterials()
        {
            foreach (var resource in All)
            {
                var prefab = Load(resource);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), resource + " must not carry colliders (interaction rays and walking are not its business)");
                Assert.IsEmpty(prefab.GetComponentsInChildren<Rigidbody>(true), resource);
                var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                Assert.IsNotEmpty(renderers, resource);
                foreach (var renderer in renderers)
                {
                    Assert.IsNotNull(renderer.GetComponent<MeshFilter>().sharedMesh, resource + "/" + renderer.name);
                    foreach (var material in renderer.sharedMaterials)
                    {
                        Assert.IsNotNull(material, resource + "/" + renderer.name + " empty material slot");
                        Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name, resource + "/" + material.name);
                    }
                }
            }
        }

        [Test]
        public void BrokenHullFitsTheCradleFootprintAndOnlyTheLanternStandsAbove()
        {
            BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size);
            var hull = (GameObject)Object.Instantiate(Load(BoatRepairSetArt.DamagedHull));
            try
            {
                var b = BoundsOf(hull);
                Assert.GreaterOrEqual(b.min.x, -size.Width * .5f - .05f); Assert.LessOrEqual(b.max.x, size.Width * .5f + .05f);
                Assert.GreaterOrEqual(b.min.z, -size.Length * .5f); Assert.LessOrEqual(b.max.z, size.Length * .5f);
                Assert.GreaterOrEqual(b.min.y, -0.01f, "stands on the ground");
                var lantern = hull.transform.Find("WorkLantern");
                Assert.IsNotNull(lantern);
                var withoutLantern = hull.GetComponentsInChildren<Renderer>().Where(r => !r.transform.IsChildOf(lantern)).ToArray();
                var hullOnly = withoutLantern[0].bounds; foreach (var r in withoutLantern) hullOnly.Encapsulate(r.bounds);
                Assert.Less(hullOnly.max.y, 1.45f, "stays below eye height so it never hides the bays from the crew");
            }
            finally { Object.DestroyImmediate(hull); }
        }

        [Test]
        public void ThreeBaysStayOpenSoTheRepairedPartsNeverClipTheBrokenHull()
        {
            var hull = (GameObject)Object.Instantiate(Load(BoatRepairSetArt.DamagedHull));
            try
            {
                AddMeshColliders(hull); Physics.SyncTransforms();
                for (var i = 0; i < 3; i++)
                {
                    var hits = Physics.OverlapBox(BoatRepairNetworkBinding.SlotLocal(i), PanelHalf, Quaternion.identity, ~0, QueryTriggerInteraction.Collide)
                        .Select(c => c.name).ToArray();
                    Assert.IsEmpty(hits, "bay " + i + " is blocked by " + string.Join(",", hits));
                }
            }
            finally { Object.DestroyImmediate(hull); }
        }

        [TestCase(0, "TarSeam", "SideCleat")]
        [TestCase(1, "Thwart", "KneeBrace")]
        [TestCase(2, "Oar", "Horn")]
        public void EachGroupHasItsOwnVisiblePermanentPartInsideItsSlot(int index, string markA, string markB)
        {
            var part = (GameObject)Object.Instantiate(Load(BoatRepairSetArt.PartResources[index]));
            try
            {
                var names = part.GetComponentsInChildren<Transform>().Select(t => t.name).ToArray();
                Assert.Contains(markA, names); Assert.Contains(markB, names);
                foreach (var signature in new[] { "TarSeam", "Thwart", "Oar" }.Where(n => n != markA))
                    CollectionAssert.DoesNotContain(names, signature, "part " + index + " must not look like another group");
                Assert.LessOrEqual(BoundsOf(part).size.x, .65f, "stays inside its 0.65 m slot pitch so neighbours never overlap");
            }
            finally { Object.DestroyImmediate(part); }
        }

        [Test]
        public void PartsKeepTheNailSurfaceOfTheCatalogPatchAndDoNotOverlapNeighbours()
        {
            var catalog = Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);
            var reference = RepairPropPlacement.Marker(catalog.RepairPlacedPlankPrefab.transform, "FastenerSurface");
            var parts = Enumerable.Range(0, 3).Select(i => (GameObject)Object.Instantiate(Load(BoatRepairSetArt.PartResources[i]))).ToArray();
            try
            {
                for (var i = 0; i < 3; i++)
                {
                    var marker = RepairPropPlacement.Marker(parts[i].transform, "FastenerSurface");
                    Assert.IsNotNull(marker, "part " + i);
                    Assert.AreEqual(catalog.RepairPlacedPlankPrefab.transform.InverseTransformPoint(reference.position).z, parts[i].transform.InverseTransformPoint(marker.position).z, 1e-4f,
                        "nail seating is computed from this surface (BoatRepairNetworkBinding.nailRootZ)");
                    parts[i].transform.position = BoatRepairNetworkBinding.SlotLocal(i);
                }
                foreach (var part in parts) AddMeshColliders(part);
                Physics.SyncTransforms();
                for (var i = 0; i < 3; i++)
                {
                    var hits = Physics.OverlapBox(BoatRepairNetworkBinding.SlotLocal(i), PanelHalf, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
                    Assert.IsNotEmpty(hits, "the query must see the part's own panel, otherwise the neighbour check proves nothing");
                    foreach (var collider in hits)
                        Assert.IsTrue(collider.transform.IsChildOf(parts[i].transform), "part " + collider.transform.root.name + "/" + collider.name + " reaches into slot " + i);
                }
            }
            finally { foreach (var part in parts) Object.DestroyImmediate(part); }
        }

        [Test]
        public void TrestleSitsJustUnderTheRepairDropRacksAndCarriesALitLantern()
        {
            var bench = (GameObject)Object.Instantiate(Load(BoatRepairSetArt.Trestle));
            try
            {
                var top = bench.transform.Find("BenchTop");
                Assert.IsNotNull(top);
                var topBounds = top.GetComponent<MeshRenderer>().bounds;
                // BoatRepairNetworkBinding.RackPosition: drop.TransformPoint((i - 1) * 0.8, 0.65, 0).
                Assert.That(topBounds.max.y, Is.InRange(.5f, .62f), "racks hang at 0.65 m");
                Assert.GreaterOrEqual(topBounds.size.x, 2.4f, "spans the three rack slots (-0.8, 0, 0.8)");
                var glass = bench.GetComponentsInChildren<Transform>().First(t => t.name == "Glass").GetComponent<MeshRenderer>().sharedMaterial;
                Assert.IsTrue(glass.IsKeywordEnabled("_EMISSION"), "the lantern must glow, it is the night cue");
                Assert.Greater(glass.GetColor("_EmissionColor").maxColorComponent, 1.5f);
            }
            finally { Object.DestroyImmediate(bench); }
        }

        [Test]
        public void BindingBuildsTheAuthoredWorksiteInsteadOfThePrimitiveStandIn()
        {
            var host = new GameObject("Test_RepairBinding");
            var binding = host.AddComponent<BoatRepairNetworkBinding>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(BoatRepairNetworkBinding).GetMethod("BuildSite", flags).Invoke(binding, null);
                var site = (GameObject)typeof(BoatRepairNetworkBinding).GetField("site", flags).GetValue(binding);
                var names = site.GetComponentsInChildren<Transform>().Select(t => t.name).ToArray();
                Assert.Contains("DamagedRowboat", names);
                CollectionAssert.DoesNotContain(names, "CrackedGunwale");
                CollectionAssert.DoesNotContain(names, "OldSeat");
                var boards = (GameObject[])typeof(BoatRepairNetworkBinding).GetField("boards", flags).GetValue(binding);
                Assert.IsTrue(boards[0].name.StartsWith("RepairedHullPatch"));
                Assert.IsTrue(boards[1].name.StartsWith("RepairedSeatSupport"));
                Assert.IsTrue(boards[2].name.StartsWith("RepairedOarlock"));
                for (var i = 0; i < 3; i++)
                    Assert.AreEqual(1, site.GetComponentsInChildren<BoatRepairSlotTarget>().Count(s => s.Index == i), "slot " + i + " keeps its single interaction target");
                Object.DestroyImmediate(site);
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
