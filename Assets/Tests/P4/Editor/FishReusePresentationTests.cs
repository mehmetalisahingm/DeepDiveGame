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

        [TestCase("SeaBass")]
        [TestCase("Tuna")]
        [TestCase("RedSnapper")]
        [TestCase("Grouper")]
        [TestCase("Lanternfish")]
        public void DeadFishStopsAuthoredSwimClipsAndRespawnRestoresThem(string visual)
        {
            var prefab = Resources.Load<GameObject>("SuppliedKit/" + visual);
            Assert.NotNull(prefab);
            var root = Object.Instantiate(prefab);
            try
            {
                var actor = root.AddComponent<FishActor>();
                var clips = root.GetComponentsInChildren<Animation>(true);
                Assert.IsNotEmpty(clips);
                var enabled = System.Array.ConvertAll(clips, clip => clip.enabled);
                var hidden = new GameObject("Authored disabled clip");
                hidden.transform.SetParent(root.transform, false);
                var disabledClip = hidden.AddComponent<Animation>();
                disabledClip.enabled = false;
                for (var dive = 0; dive < 3; dive++)
                {
                    actor.SetSwimPresentation(false);
                    foreach (var clip in clips) Assert.IsFalse(clip.enabled);
                    actor.SetSwimPresentation(true);
                    for (var i = 0; i < clips.Length; i++) Assert.AreEqual(enabled[i], clips[i].enabled);
                    Assert.IsFalse(disabledClip.enabled, "restore must preserve authored disabled visuals");
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
