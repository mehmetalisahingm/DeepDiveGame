using DeepDive.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class RepairAuthoredArtTests
    {
        private const string Root="Assets/DeepDive/Art/RepairTools/";
        [TestCase("OakRepairPlank",.7f,.9f)]
        [TestCase("CarpentersAdze",.5f,.8f)]
        [TestCase("ForgedBoatNail",.05f,.1f)]
        [TestCase("OakRepairPatch",.6f,.85f)]
        public void ShippedPropsHaveRealMeshesAndHumanScaleWithoutInteractionAuthority(string name,float min,float max)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+name+".prefab");
            Assert.That(prefab,Is.Not.Null,name);
            var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(filters.Length,Is.GreaterThan(0));
            int triangles=0;Bounds bounds=default;bool first=true;
            foreach(var filter in filters)
            {
                Assert.That(filter.sharedMesh,Is.Not.Null);
                triangles+=filter.sharedMesh.triangles.Length/3;
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    Assert.That(float.IsNaN(vertex.x)||float.IsInfinity(vertex.x)||float.IsNaN(vertex.y)||float.IsInfinity(vertex.y)||float.IsNaN(vertex.z)||float.IsInfinity(vertex.z),Is.False);
                    var world=filter.transform.TransformPoint(vertex);
                    if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);
                }
            }
            Assert.That(triangles,Is.InRange(100,10000));
            Assert.That(Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z),Is.InRange(min,max));
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
        }

        [Test]
        public void CatalogUsesAuthoredRepairAssetsAndARealImpactClip()
        {
            var catalog=Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);
            Assert.That(AssetDatabase.GetAssetPath(catalog.RepairPlankPrefab),Is.EqualTo(Root+"OakRepairPlank.prefab"));
            Assert.That(AssetDatabase.GetAssetPath(catalog.RepairHatchetPrefab),Is.EqualTo(Root+"CarpentersAdze.prefab"));
            Assert.That(AssetDatabase.GetAssetPath(catalog.RepairPlacedPlankPrefab),Is.EqualTo(Root+"OakRepairPatch.prefab"));
            Assert.That(AssetDatabase.GetAssetPath(catalog.RepairNailPrefab),Is.EqualTo(Root+"ForgedBoatNail.prefab"));
            Assert.That(catalog.RepairImpactClip,Is.Not.Null);
            Assert.That(catalog.RepairImpactClip.length,Is.InRange(.1f,.3f));
            Assert.That(RepairPropPlacement.Marker(catalog.RepairPlankPrefab.transform,"Grip_Left"),Is.Not.Null);
            Assert.That(RepairPropPlacement.Marker(catalog.RepairPlankPrefab.transform,"Grip_Right"),Is.Not.Null);
            Assert.That(RepairPropPlacement.Marker(catalog.RepairPlacedPlankPrefab.transform,"FastenerSurface"),Is.Not.Null);
        }

        [Test]
        public void AuthoredAdzeHitsExactNailPositionWithoutMovingPlayerRoot()
        {
            var player=new GameObject("UnmovedPlayer");
            var tool=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"CarpentersAdze.prefab"),player.transform);
            try
            {
                player.transform.SetPositionAndRotation(new Vector3(4,8,-12),Quaternion.Euler(0,75,0));
                var rootPosition=player.transform.position;var rootRotation=player.transform.rotation;
                var grip=RepairPropPlacement.Marker(tool.transform,"GripPoint");
                var contact=RepairPropPlacement.Marker(tool.transform,"ToolContact");
                Assert.That(grip,Is.Not.Null);Assert.That(contact,Is.Not.Null);
                var target=player.transform.TransformPoint(new Vector3(0,.98f,.85f));
                RepairPropPlacement.AimToolContact(tool.transform,contact,grip,target,
                    player.transform.TransformPoint(new Vector3(.22f,1.26f,.38f)),player.transform.rotation);
                Assert.That(Vector3.Distance(contact.position,target),Is.LessThan(.0001f));
                Assert.That(Vector3.Distance(grip.position,contact.position),Is.InRange(.45f,.55f));
                Assert.That(player.transform.position,Is.EqualTo(rootPosition));
                Assert.That(player.transform.rotation,Is.EqualTo(rootRotation));
            }
            finally{Object.DestroyImmediate(player);}
        }
    }
}
