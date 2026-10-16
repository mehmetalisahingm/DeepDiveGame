using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class FishReusePresentationTests
    {
        [Test]
        public void RetainedFishHidesWhileDespawnedAndRestoresOnlyAuthoredEnabledParts()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var detail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            detail.transform.SetParent(root.transform);
            detail.GetComponent<Renderer>().enabled = false;
            detail.GetComponent<Collider>().enabled = false;
            try
            {
                var fish = root.AddComponent<FishActor>();
                fish.OnNetworkDespawn();
                Assert.IsFalse(root.GetComponent<Renderer>().enabled);
                Assert.IsFalse(root.GetComponent<Collider>().enabled);
                fish.OnNetworkSpawn();
                Assert.IsTrue(root.GetComponent<Renderer>().enabled);
                Assert.IsTrue(root.GetComponent<Collider>().enabled);
                Assert.IsFalse(detail.GetComponent<Renderer>().enabled);
                Assert.IsFalse(detail.GetComponent<Collider>().enabled);
                fish.OnNetworkDespawn();
                fish.OnNetworkSpawn();
                Assert.IsTrue(root.GetComponent<Collider>().enabled, "second reuse must remain interactive");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
