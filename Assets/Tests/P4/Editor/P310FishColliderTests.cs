using System.Linq;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #310 (Mehmet, 21 Nov): every fish's harpoon capsule wraps its drawn body with 15-25 % headroom, along the body's
    // length and across its width, instead of the 1.1 m capsule authored round a 0.25 m model.
    public sealed class P310FishColliderTests
    {
        [Test]
        public void EachFishCapsuleWrapsItsDrawnBodyWithFifteenToTwentyFivePercentHeadroom()
        {
            Assert.That(FishActor.BodyColliderTolerance, Is.InRange(1.15f, 1.25f));
            ContinuousTownScene.With(scene =>
            {
                var fishes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FishActor>(true)).ToArray();
                Assert.AreEqual(15, fishes.Length);
                foreach (var fish in fishes)
                {
                    var capsule = fish.GetComponent<CapsuleCollider>();
                    Assert.IsNotNull(capsule, fish.name);
                    fish.FitColliderToBody();
                    var body = Body(fish);
                    var length = body.size[capsule.direction];
                    Assert.AreEqual(Mathf.Max(body.size.x, body.size.y, body.size.z), length, 1e-4f, fish.name + " along its longest side");
                    Assert.That(capsule.height / length, Is.InRange(1.15f, 1.25f), fish.name + " length");
                    var across = (body.size[(capsule.direction + 1) % 3] + body.size[(capsule.direction + 2) % 3]) / 2f;
                    Assert.That(capsule.radius * 2f / across, Is.InRange(1.15f, 1.25f), fish.name + " width");
                    Assert.Less(Vector3.Distance(capsule.center, body.center), 0.01f, fish.name + " centred on the body");
                    // Far from the old 1.1 m capsule round a ~0.25 m model.
                    Assert.Less(capsule.height, 0.6f, fish.name);
                }
            }, false);
        }

        // The drawn body's box in the fish's own space.
        private static Bounds Body(FishActor fish)
        {
            var toRoot = fish.transform.worldToLocalMatrix;
            var any = false;
            var box = new Bounds();
            foreach (var r in fish.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                var m = toRoot * r.transform.localToWorldMatrix;
                foreach (var x in new[] { -1f, 1f })
                foreach (var y in new[] { -1f, 1f })
                foreach (var z in new[] { -1f, 1f })
                {
                    var p = m.MultiplyPoint3x4(mesh.bounds.center + Vector3.Scale(mesh.bounds.extents, new Vector3(x, y, z)));
                    if (!any) { box = new Bounds(p, Vector3.zero); any = true; } else box.Encapsulate(p);
                }
            }
            return box;
        }
    }
}
