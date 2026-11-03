using System.Reflection;
using DeepDive.Composition;
using DeepDive.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public class ShopCarryTests
    {
        [TestCase("CoastalDiver")] [TestCase("CoastalArms")]
        public void RealHandsReachTheShoppingTrayWithoutMovingThePlayer(string name)
        {
            var rig = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeepDive/Player/Character/" + name + ".prefab"));
            GameObject basket = null;
            try
            {
                var pose = rig.GetComponent<CoastalDiverPose>();
                typeof(CoastalDiverPose).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pose, null);
                rig.transform.position = new Vector3(100, 8.4f, -80); rig.transform.rotation = Quaternion.Euler(0, 37, 0);
                var before = rig.transform.position;
                basket = ShopCarryView.CreateBasket(null, "camera-basic");
                var held = ShopCarryView.HeldPose(pose.Model);
                basket.transform.SetPositionAndRotation(held.position, held.rotation);
                var left = basket.transform.Find("Tray/Grip_Left"); var right = basket.transform.Find("Tray/Grip_Right");
                pose.PresentRepair(left.position, right.position, left.rotation, right.rotation, true);
                Assert.Less(Vector3.Distance(pose.RepairLeftPalmPosition, left.position), .025f);
                Assert.Less(Vector3.Distance(pose.RepairRightPalmPosition, right.position), .025f);
                Assert.AreEqual(before, rig.transform.position);
            }
            finally { Object.DestroyImmediate(basket); Object.DestroyImmediate(rig); }
        }

        [Test] public void EveryCatalogSampleFitsTheTrayAndHasNoGameplayOrSolidCollider()
        {
            foreach (var id in ShopShelfAnchor.ItemIds)
            {
                var basket = ShopCarryView.CreateBasket(null, id);
                try
                {
                    Assert.IsNotNull(basket, id);
                    Assert.IsEmpty(basket.GetComponentsInChildren<Unity.Netcode.NetworkBehaviour>(), id);
                    Assert.IsEmpty(basket.GetComponentsInChildren<Rigidbody>(), id);
                    foreach (var collider in basket.GetComponentsInChildren<Collider>()) Assert.IsFalse(collider.enabled, id);
                    var product = basket.transform.Find("SelectedProduct");
                    var renderers = product.GetComponentsInChildren<Renderer>(); Assert.IsNotEmpty(renderers, id);
                    var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    Assert.LessOrEqual(bounds.size.x, .40f, id); Assert.LessOrEqual(bounds.size.z, .40f, id);
                    Assert.GreaterOrEqual(bounds.min.y, -.065f, id + " penetrates tray floor");
                }
                finally { Object.DestroyImmediate(basket); }
            }
            Assert.IsNull(ShopCarryView.CreateBasket(null, "not-a-product"));
        }
    }
}
