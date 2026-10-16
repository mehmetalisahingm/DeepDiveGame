using System.Linq;
using DeepDive.Composition;
using DeepDive.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #173: repeatable checks for the *existing* prefab and authored dock/home.
    // These are technical geometry baselines, not new-rig, first-person clipping,
    // human-playtest, or P4.7 visual acceptance.
    public sealed class P4PlayerScaleBaselineTests
    {
        private const string DiverPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";

        [Test]
        public void ExistingCameraMaintainsHumanEyeHeightAndUsefulNearClip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPath);
            Assert.IsNotNull(prefab, "real NetworkDiver prefab required");
            var controller = prefab.GetComponent<CharacterController>();
            var view = prefab.GetComponentInChildren<Camera>(true);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(view);
            var eye = prefab.transform.InverseTransformPoint(view.transform.position).y;

            Assert.That(controller.height, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(eye, Is.GreaterThan(controller.height * 0.75f), "eye should not clip into the upper chest");
            Assert.That(eye, Is.LessThan(controller.height), "eye should remain within current capsule");
            Assert.IsFalse(view.orthographic, "player camera must use perspective");
            Assert.That(view.fieldOfView, Is.InRange(55f, 75f), "re-audit framing after a lens change");
            Assert.That(view.nearClipPlane, Is.InRange(0.02f, 0.1f), "re-audit close held-object clipping if changed");
        }

        [Test]
        public void CurrentHomeAndDockPermitSequentialFourPlayerMovementWithoutClaimingFourAbreast()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPath);
            Assert.IsNotNull(prefab);
            var cc = prefab.GetComponent<CharacterController>();
            Assert.IsNotNull(cc);
            var capsuleDiameter = cc.radius * 2f;
            var paddedDiameter = (cc.radius + cc.skinWidth) * 2f;
            ContinuousTownScene.With(scene =>
            {
                HomeSceneSetup.EnsureRig(scene);
                var roots = scene.GetRootGameObjects();
                var dock = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == "Dock_Town").GetComponent<Collider>();
                Assert.IsNotNull(dock);
                var width = dock.bounds.size.x;
                Assert.That(width, Is.GreaterThanOrEqualTo(2f * paddedDiameter),
                    "current dock must pass at least two padded capsules");
                Assert.That(width, Is.LessThan(4f * paddedDiameter),
                    "four players must queue, not all stand abreast on this dock");

                var spawns = Enumerable.Range(0, 4).Select(ContinuousTownLayout.HomeRig.Spawn).ToArray();
                for (var i = 0; i < spawns.Length; i++)
                for (var j = i + 1; j < spawns.Length; j++)
                {
                    var a = new Vector2(spawns[i].x, spawns[i].z);
                    var b = new Vector2(spawns[j].x, spawns[j].z);
                    Assert.That(Vector2.Distance(a, b), Is.GreaterThanOrEqualTo(capsuleDiameter),
                        "overlapping four-player home spawns: " + i + "/" + j);
                }
            }, furnitureProxies: true);
        }
    }
}
