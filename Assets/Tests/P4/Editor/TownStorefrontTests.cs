using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P4.Tests
{
    // P4.6-B (#139): the four service storefronts. They are presentation only (the rules stay in the economy and the host's range check), so these
    // pin what a customer can SEE and AIM at: a counter that resolves to its own service, a sign with the service's name, a customer spot in front,
    // a lantern that follows the shared day, nothing that overlaps the next shop or leaves the beach strip.
    public sealed class TownStorefrontTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        private static ServicePointAnchor MakeAnchor(string id, ServicePointType type)
        {
            var go = new GameObject("Npc_" + id);
            var anchor = go.AddComponent<ServicePointAnchor>();
            anchor.Configure(id, type, "anchor-" + id, 3f, "cat-" + id);
            return anchor;
        }

        [Test]
        public void EveryServiceTypeGetsACounterASignACustomerSpotAndALantern()
        {
            foreach (var definition in TownServiceCatalog.All)
            {
                var anchor = MakeAnchor(definition.ServiceId, definition.ServiceType);
                try
                {
                    var front = TownStorefront.Build(anchor);
                    Assert.IsNotNull(front, definition.ServiceId);
                    Assert.IsFalse(string.IsNullOrEmpty(front.Label), definition.ServiceId);
                    var sign = front.Root.Find("SignText").GetComponent<TextMesh>();
                    Assert.AreEqual(front.Label, sign.text);
                    Assert.IsNotNull(sign.GetComponent<MeshRenderer>().sharedMaterial, "the sign text needs its font material");
                    Assert.Greater(Vector3.Dot(-sign.transform.forward, anchor.transform.forward), 0.99f, "the sign must read from the customer side, not mirrored");

                    var counter = front.Root.Find("Counter").GetComponent<Collider>();
                    Assert.IsNotNull(counter, "the counter is what a customer aims at");
                    Assert.AreSame(anchor, counter.GetComponentInParent<ServicePointAnchor>(), "aiming at the counter resolves to the service");
                    Assert.AreEqual(1, front.Root.GetComponentsInChildren<Collider>().Length, "decoration must not block a walk or an aim ray");

                    Assert.AreEqual(TownStorefront.CustomerSpot, front.CustomerPoint.localPosition.z, 0.001f);
                    Assert.IsNotNull(front.Root.Find("CustomerMark"));
                    Assert.IsNotNull(front.Lamp);
                    Assert.AreEqual(LightType.Point, front.Lamp.type);
                }
                finally { Object.DestroyImmediate(anchor.gameObject); }
            }
        }

        [Test]
        public void TheFourStorefrontsReadApartByLabelAndByColour()
        {
            var labels = TownServiceCatalog.All.Select(d => TownStorefront.LabelFor(d.ServiceType)).ToArray();
            Assert.AreEqual(labels.Length, labels.Distinct().Count(), "one name per service");
            var roofs = new HashSet<Material>();
            foreach (var definition in TownServiceCatalog.All)
            {
                var anchor = MakeAnchor(definition.ServiceId, definition.ServiceType);
                try
                {
                    var roof = TownStorefront.Build(anchor).Root.Find("Roof").GetComponent<Renderer>();
                    Assert.IsNotNull(roof.sharedMaterial, definition.ServiceId);
                    StringAssert.StartsWith("Dev_", roof.sharedMaterial.name, "a real material asset, not the primitive default that turns magenta in a build");
                    roofs.Add(roof.sharedMaterial);
                }
                finally { Object.DestroyImmediate(anchor.gameObject); }
            }
            Assert.AreEqual(TownServiceCatalog.All.Count, roofs.Count, "each service has its own roof colour");
        }

        [Test]
        public void BuildingTwiceDoesNotDuplicateAndAnAuthoredBuildingIsLeftAlone()
        {
            var anchor = MakeAnchor("a", ServicePointType.FishBuyer);
            var authored = MakeAnchor("b", ServicePointType.EquipmentShop);
            try
            {
                var first = TownStorefront.Build(anchor);
                Assert.AreSame(first, TownStorefront.Build(anchor));
                Assert.AreEqual(1, anchor.GetComponentsInChildren<Transform>().Count(t => t.name == TownStorefront.RootName));

                new GameObject(TownStorefront.RootName).transform.SetParent(authored.transform, false);
                Assert.IsNull(TownStorefront.Build(authored), "World may place a real building under the anchor; the placeholder steps aside");
                Assert.IsNull(authored.GetComponent<TownStorefront>());
            }
            finally { Object.DestroyImmediate(anchor.gameObject); Object.DestroyImmediate(authored.gameObject); }
        }

        [Test]
        public void ANonServiceAnchorGetsNoStorefront()
        {
            var anchor = MakeAnchor("x", ServicePointType.None);
            try { Assert.IsNull(TownStorefront.Build(anchor)); }
            finally { Object.DestroyImmediate(anchor.gameObject); }
        }

        [Test]
        public void TheLanternIsOffByDayAndBrightestAtNight()
        {
            Assert.AreEqual(0f, TownStorefront.LampIntensity(0f));
            Assert.Greater(TownStorefront.LampIntensity(0.5f), 0f);
            Assert.Greater(TownStorefront.LampIntensity(1f), TownStorefront.LampIntensity(0.5f));
            Assert.AreEqual(TownStorefront.LampIntensity(1f), TownStorefront.LampIntensity(7f), "clamped");
            var anchor = MakeAnchor("a", ServicePointType.FishBuyer);
            try
            {
                var front = TownStorefront.Build(anchor);
                Assert.AreEqual(0f, front.Lamp.intensity, "no replicated day yet = day");
                Assert.IsFalse(front.Lamp.enabled);
            }
            finally { Object.DestroyImmediate(anchor.gameObject); }
        }

        [Test]
        public void OnTheRealBeachTheStorefrontsFitTheStripAndNeitherTouchNorBlockEachOther()
        {
            var previous = SceneManager.GetActiveScene();
            var already = SceneManager.GetSceneByPath(ScenePath);
            var opened = !already.IsValid() || !already.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : already;
            try
            {
                var roots = scene.GetRootGameObjects();
                var platform = roots.First(x => x.name == "Beach_Platform").GetComponent<Collider>().bounds;
                var anchors = roots.SelectMany(x => x.GetComponentsInChildren<ServicePointAnchor>(true)).ToArray();
                Assert.AreEqual(TownServiceCatalog.All.Count, anchors.Length);
                var fronts = anchors.Select(TownStorefront.Build).ToArray();
                Assert.IsTrue(fronts.All(f => f != null));
                Physics.SyncTransforms();

                var boxes = fronts.Select(f =>
                {
                    var renderers = f.Root.GetComponentsInChildren<Renderer>().Where(r => r.GetComponent<TextMesh>() == null).ToArray();
                    var b = renderers[0].bounds;
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    return b;
                }).ToArray();

                for (var i = 0; i < boxes.Length; i++)
                {
                    Assert.GreaterOrEqual(boxes[i].min.x, platform.min.x, fronts[i].Label + " hangs off the west end");
                    Assert.LessOrEqual(boxes[i].max.x, platform.max.x, fronts[i].Label + " hangs off the east end");
                    Assert.GreaterOrEqual(boxes[i].min.z, platform.min.z - 0.05f, fronts[i].Label + " hangs off the back edge");
                    Assert.Greater(boxes[i].min.x, -3.5f, fronts[i].Label + " must not narrow the wade lane");
                    for (var j = i + 1; j < boxes.Length; j++)
                        Assert.IsFalse(boxes[i].Intersects(boxes[j]), $"{fronts[i].Label} overlaps {fronts[j].Label}");
                }
                foreach (var f in fronts)
                {
                    var spot = f.CustomerPoint.position;
                    Assert.IsTrue(Physics.Raycast(spot + Vector3.up * 2f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore));
                    Assert.AreEqual(platform.max.y, hit.point.y, 0.01f, f.Label + ": the customer spot is on the strip, not on a prop");
                }
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
