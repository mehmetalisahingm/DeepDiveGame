using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.Tests
{
    public sealed class BoatHullPresentationTests
    {
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
                    Assert.AreEqual(size.Length, selected.Find("Deck").localScale.z);
                    Assert.AreEqual(kind == BoatHullKind.ResearchVessel, selected.Find("Cabin") != null);
                    Assert.AreEqual(kind == BoatHullKind.Motorboat, selected.Find("OutboardMotor") != null);
                }
                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Assert.IsFalse(collider.enabled);
                view.ApplyHullPresentation((BoatHullKind)255);
                foreach (Transform model in root.transform) Assert.IsFalse(model.gameObject.activeSelf);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
