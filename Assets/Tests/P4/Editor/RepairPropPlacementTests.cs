using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace DeepDive.P4.Tests
{
    public sealed class RepairPropPlacementTests
    {
        [TestCase("CoastalDiver")]
        [TestCase("CoastalArms")]
        public void ActualSharedRigReachesRepairGripsWithoutMovingItsRoot(string name)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeepDive/Player/Character/"+name+".prefab");
            Assert.That(prefab,Is.Not.Null);
            var instance=Object.Instantiate(prefab);
            try
            {
                var pose=instance.GetComponent<CoastalDiverPose>();
                // EditMode does not run MonoBehaviour messages. Initialize the real
                // rig's bone cache explicitly without asking Unity to dispatch Awake.
                typeof(CoastalDiverPose).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(pose,null);
                var upperLeft=RepairPropPlacement.Marker(instance.transform,"UpperArm.L");
                var upperRight=RepairPropPlacement.Marker(instance.transform,"UpperArm.R");
                var left=upperLeft.position+new Vector3(.06f,-.18f,.35f);
                var right=upperRight.position+new Vector3(-.06f,-.18f,.35f);
                var rootPosition=instance.transform.position;var rootRotation=instance.transform.rotation;
                pose.PresentRepair(left,right,Quaternion.identity);
                Assert.That(Vector3.Distance(pose.RepairLeftPalmPosition,left),Is.LessThan(.015f),"Left palm grip");
                Assert.That(Vector3.Distance(pose.RepairRightPalmPosition,right),Is.LessThan(.015f),"Right palm grip");
                Assert.That(instance.transform.position,Is.EqualTo(rootPosition));
                Assert.That(instance.transform.rotation,Is.EqualTo(rootRotation));
            }
            finally {Object.DestroyImmediate(instance);}
        }

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
        public void RepairHudExplainsPhysicalStepsWithoutInventingProgress()
        {
            var carry = RepairCarryView.RepairInstruction(false, 2);
            StringAssert.Contains("YUVAYA BAK", carry);
            StringAssert.Contains("YERLESTIR", carry);
            StringAssert.DoesNotContain("2/3", carry,
                "carrying a board is not proof that nails have been accepted");
            Assert.AreEqual("KESER: SOL TIK | ONAYLI CIVI 0/3 | Q: BIRAK",
                RepairCarryView.RepairInstruction(true, -10));
            Assert.AreEqual("KESER: SOL TIK | ONAYLI CIVI 2/3 | Q: BIRAK",
                RepairCarryView.RepairInstruction(true, 2));
            Assert.AreEqual("KESER: SOL TIK | ONAYLI CIVI 3/3 | Q: BIRAK",
                RepairCarryView.RepairInstruction(true, 99),
                "a cosmetic UI never displays more than the 3 physical nails");
        }

        [Test]
        public void RepairHudStaysWithinSmallAndWideScreenBounds()
        {
            foreach (var size in new[] { new Vector2(320f, 480f), new Vector2(800f, 600f),
                    new Vector2(1920f, 1080f) })
            {
                var hud = RepairCarryView.RepairHudRect(size.x, size.y);
                Assert.GreaterOrEqual(hud.xMin, 0f);
                Assert.GreaterOrEqual(hud.yMin, 0f);
                Assert.LessOrEqual(hud.xMax, size.x);
                Assert.LessOrEqual(hud.yMax, size.y);
                Assert.Greater(hud.width, 0f);
                Assert.Greater(hud.height, 0f);
            }
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
