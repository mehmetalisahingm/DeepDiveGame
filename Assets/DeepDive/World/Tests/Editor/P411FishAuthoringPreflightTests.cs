using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DeepDive.World.Tests
{
    // Regression contract for the non-destructive P4.11 five-species scene authoring.
    // Inspect the current scene; never call the authoring menu or mutate/save it.
    public class P411FishAuthoringPreflightTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        [Test]
        public void EveryExistingFishHasOneExpectedSlotAndOneRootNetworkIdentity()
        {
            var already = SceneManager.GetSceneByPath(ScenePath);
            var opened = !already.IsValid() || !already.isLoaded;
            var scene = opened
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : already;
            try
            {
                var fish = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FishActor>(true))
                    .ToArray();
                Assert.AreEqual(15, fish.Length, "P4.11 authoring must not add or delete NGO fish");
                for (var i = 0; i < 15; i++)
                {
                    var name = i == 0 ? "Fish_SeaBass" : $"Fish_SeaBass_{i + 1:00}";
                    var matched = fish.Where(actor => actor.name == name).ToArray();
                    Assert.AreEqual(1, matched.Length, name + " must identify exactly one authored fish");
                    var actor = matched[0];
                    Assert.NotNull(actor.GetComponent<NetworkObject>(),
                        name + " must keep its original root NetworkObject");
                    Assert.AreEqual(1, actor.GetComponentsInChildren<NetworkObject>(true).Length,
                        name + " must not hide a second NGO identity inside the visual subtree");
                }
            }
            finally
            {
                if (opened && scene.IsValid())
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
