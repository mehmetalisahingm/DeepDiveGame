using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.Tests
{
    public sealed class BoatHullPresentationTests
    {
        [Test]
        public void SuppliedMotorboatPrefabAndTexturedUrpMaterialArePresent()
        {
            var prefab = Resources.Load<GameObject>("BoatArt/TripoMotorboat/MotorboatVisual");
            Assert.NotNull(prefab, "The supplied model must ship with the game");
            Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(true).Length);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(renderers.Length, 0);
            foreach (var renderer in renderers)
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name);
                    Assert.NotNull(material.GetTexture("_BaseMap"));
                    Assert.NotNull(material.GetTexture("_BumpMap"));
                }
        }

        [Test]
        public void SwitchingAndReinitializingKeepsOneDistinctHullAndNoVisualColliders()
        {
            var root = new GameObject("hull-test");
            try
            {
                var view = root.AddComponent<BoatHullPresentation>();
                view.Initialize(null);
                view.Initialize(null);
                Assert.AreEqual(3, root.transform.childCount);
                foreach (var kind in new[] { BoatHullKind.Motorboat, BoatHullKind.ResearchVessel, BoatHullKind.Rowboat })
                {
                    view.ApplyHullPresentation(kind);
                    foreach (Transform model in root.transform)
                        Assert.AreEqual(model.name == kind.ToString(), model.gameObject.activeSelf);
                    BoatHullSeatRules.TryGetDimensions(kind, out var size);
                    var selected = root.transform.Find(kind.ToString());
                    var imported = selected.Find(kind == BoatHullKind.Rowboat ? "Rowboat(Clone)" : "MotorboatVisual(Clone)");
                    if (kind == BoatHullKind.Motorboat || kind == BoatHullKind.Rowboat)
                    {
                        Assert.NotNull(imported, "Available motorboat art must replace the fallback");
                        var renderers = imported.GetComponentsInChildren<Renderer>(true);
                        Assert.Greater(renderers.Length, 0);
                        var bounds = renderers[0].bounds;
                        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        Assert.LessOrEqual(bounds.size.x, size.Width + 0.01f);
                        Assert.LessOrEqual(bounds.size.z, size.Length + 0.01f);
                        Assert.Greater(bounds.size.z, size.Length * 0.5f);
                        Assert.IsNull(selected.Find("Deck"), "Do not draw the blockout inside imported art");
                    }
                    else
                    {
                        Assert.AreEqual(size.Length, selected.Find("Deck").localScale.z);
                        Assert.AreEqual(kind == BoatHullKind.ResearchVessel, selected.Find("Cabin") != null);
                        Assert.AreEqual(kind == BoatHullKind.Motorboat, selected.Find("OutboardMotor") != null);
                    }
                }
                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Assert.IsFalse(collider.enabled);
                view.ApplyHullPresentation((BoatHullKind)255);
                foreach (Transform model in root.transform) Assert.IsFalse(model.gameObject.activeSelf);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
