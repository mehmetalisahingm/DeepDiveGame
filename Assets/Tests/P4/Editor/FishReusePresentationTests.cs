using DeepDive.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class FishReusePresentationTests
    {
        [Test]
        public void LivingNightFishReusesOneDisabledLightAcrossImmediateDiveRespawns()
        {
            var root = new GameObject("living-fish-reuse");
            try
            {
                var fish = root.AddComponent<FishActor>();
                var serialized = new SerializedObject(fish);
                serialized.FindProperty("species").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(
                    "Assets/DeepDive/World/Fish/Species/SeaBass.asset");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                fish.OnNetworkSpawn();
                var glow = root.GetComponent<Light>();
                Assert.NotNull(glow);
                for (var dive = 0; dive < 3; dive++)
                {
                    glow.enabled = true;
                    fish.OnNetworkDespawn();
                    Assert.IsFalse(glow.enabled, "a retained fish must not glow while despawned");
                    fish.OnNetworkSpawn();
                    Assert.AreSame(glow, root.GetComponent<Light>());
                    Assert.AreEqual(1, root.GetComponents<Light>().Length);
                    Assert.IsFalse(glow.enabled, "night state must be resolved again by the host");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

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
