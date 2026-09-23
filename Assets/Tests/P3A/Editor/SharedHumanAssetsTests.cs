using System.Linq;
using DeepDive.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P3A.Tests
{
    public sealed class SharedHumanAssetsTests
    {
        [Test]
        public void CleanCheckoutHasAnimatedHumanAndSkinnedHandsWithUrpMaterials()
        {
            var catalog = Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);
            Assert.NotNull(catalog);
            foreach (var prefab in new[] { catalog.ThirdPersonRigPrefab, catalog.FirstPersonArmsPrefab })
            {
                Assert.NotNull(prefab, "Shared build must not depend on a developer's private FBX import");
                var animator = prefab.GetComponentInChildren<Animator>();
                Assert.NotNull(animator);
                Assert.IsTrue(animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman);
                Assert.IsFalse(animator.applyRootMotion, "NetworkPlayer alone moves the player");
                Assert.NotNull(animator.runtimeAnimatorController);
                Assert.IsTrue(animator.runtimeAnimatorController.animationClips.Any(c => c.name.EndsWith("Walk")));
                var meshes = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                Assert.IsNotEmpty(meshes);
                foreach (var mesh in meshes)
                {
                    Assert.NotNull(mesh.sharedMesh);
                    Assert.Greater(mesh.sharedMesh.triangles.Length, 30);
                    foreach (var material in mesh.sharedMaterials)
                    {
                        Assert.NotNull(material);
                        Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name);
                    }
                }
            }
            Assert.AreEqual(1, catalog.FirstPersonArmsPrefab.GetComponentsInChildren<SkinnedMeshRenderer>().Length);
            var socket = catalog.ThirdPersonRigPrefab.GetComponentsInChildren<Transform>()
                .FirstOrDefault(t => t.name == "Socket_RightHand_Equipment");
            Assert.NotNull(socket);
            Assert.Less(Vector3.Distance(Vector3.one, socket.lossyScale), .001f,
                "FBX bone scale must not make the equipped camera a hundred times larger");
            Assert.IsTrue(AssetDatabase.GetAssetPath(catalog.ThirdPersonRigPrefab).EndsWith("CoastalDiver.prefab"));
        }
    }
}
