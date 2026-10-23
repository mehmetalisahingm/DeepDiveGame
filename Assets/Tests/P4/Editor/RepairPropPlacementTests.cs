using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class RepairPropPlacementTests
    {
        [Test]
        public void ImportedNestedGripAlignsWithoutChangingScaleOrMarkerHierarchy()
        {
            var parent=new GameObject("ScaledImportParent");
            var prop=new GameObject("Hatchet");var nested=new GameObject("Pivot");var marker=new GameObject("GripPoint");
            try
            {
                parent.transform.localScale=Vector3.one*1.7f;
                parent.transform.rotation=Quaternion.Euler(10,70,4);
                prop.transform.SetParent(parent.transform,false);prop.transform.localScale=Vector3.one*.01f;
                nested.transform.SetParent(prop.transform,false);nested.transform.localRotation=Quaternion.Euler(20,40,50);
                marker.transform.SetParent(nested.transform,false);marker.transform.localPosition=new Vector3(3,12,2);
                var scale=prop.transform.localScale;var localMarker=marker.transform.localPosition;
                var target=new Vector3(4,2,-9);var rotation=Quaternion.Euler(-25,150,15);
                RepairPropPlacement.AlignGrip(prop.transform,marker.transform,target,rotation);
                Assert.That(Vector3.Distance(marker.transform.position,target),Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(marker.transform.rotation,rotation),Is.LessThan(.01f));
                Assert.That(prop.transform.localScale,Is.EqualTo(scale));
                Assert.That(marker.transform.localPosition,Is.EqualTo(localMarker));
                Assert.That(marker.transform.parent,Is.EqualTo(nested.transform));
            }
            finally {Object.DestroyImmediate(parent);}
        }

        [Test]
        public void UnassignedFinalArtIsExplicitAndSafe()
        {
            var catalog=ScriptableObject.CreateInstance<DiverPresentationCatalog>();
            try
            {
                Assert.That(catalog.RepairPlankPrefab,Is.Null);
                Assert.That(catalog.RepairHatchetPrefab,Is.Null);
                Assert.That(catalog.RepairImpactClip,Is.Null);
            }
            finally {Object.DestroyImmediate(catalog);}
        }
    }
}
