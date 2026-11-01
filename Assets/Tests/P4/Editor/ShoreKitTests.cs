using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #171 P4.7-C: the shore kit (user-supplied Quaternius Pirate Kit, CC0) promises the shore placer stable ids, metre scale, one
    // URP atlas material, a bounded triangle budget and no gameplay components. Placement and colliders stay the placer's call.
    public sealed class ShoreKitTests
    {
        private const string Models = "Assets/DeepDive/Art/Supplied/PirateKit/Models";
        private static IEnumerable<string> Ids => ShoreKitPiece.AllIds;

        private static GameObject Load(string id)
        {
            var prefab = ShoreKitPiece.Load(id);
            Assert.IsNotNull(prefab, id);
            return prefab;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        [Test]
        public void SixteenPiecesWithStableIdsKindsAndNoGameplayComponents()
        {
            Assert.AreEqual(16, ShoreKitPiece.AllIds.Length);
            Assert.AreEqual(16, ShoreKitPiece.AllIds.Distinct().Count());
            var kinds = Ids.GroupBy(i => Load(i).GetComponent<ShoreKitPiece>().Kind).ToDictionary(g => g.Key, g => g.Count());
            Assert.AreEqual(3, kinds[ShoreKitKind.Dock]); Assert.AreEqual(5, kinds[ShoreKitKind.Rock]);
            Assert.AreEqual(3, kinds[ShoreKitKind.Tree]); Assert.AreEqual(5, kinds[ShoreKitKind.Prop]);
            foreach (var id in Ids)
            {
                var prefab = Load(id);
                Assert.AreEqual(id, prefab.name); Assert.AreEqual(id, prefab.GetComponent<ShoreKitPiece>().Id);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), id + " carries no collider (placement decides)");
                Assert.IsEmpty(prefab.GetComponentsInChildren<Rigidbody>(true), id);
                Assert.IsFalse(prefab.GetComponentsInChildren<Component>(true).Any(c => c != null && c.GetType().Name == "NetworkObject"), id + " is not networked");
            }
        }

        [Test]
        public void OnlyTheAtlasMaterialIsUsedAndItIsUrp()
        {
            var materials = Ids.SelectMany(id => Load(id).GetComponentsInChildren<Renderer>(true)).SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            Assert.AreEqual(1, materials.Length, "one shared atlas material");
            var material = materials[0];
            Assert.IsNotNull(material);
            Assert.AreEqual("MI_PirateAtlas", material.name);
            Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name);
            Assert.IsNotNull(material.GetTexture("_BaseMap"), "atlas bound");
            Assert.IsFalse(material.IsKeywordEnabled("_EMISSION") && material.GetColor("_EmissionColor").maxColorComponent > 0f, "no glow");
            StringAssert.StartsWith("Assets/DeepDive/Art/Supplied/PirateKit/Materials/", AssetDatabase.GetAssetPath(material));
        }

        [Test]
        public void TriangleBudgetAndOneRendererPerPiece()
        {
            foreach (var id in Ids)
            {
                var prefab = Load(id);
                var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
                Assert.AreEqual(1, filters.Length, id + " is one mesh");
                var tris = filters[0].sharedMesh.triangles.Length / 3;
                Assert.LessOrEqual(tris, 4000, id + " triangles");
                Assert.AreEqual(1, filters[0].GetComponent<Renderer>().sharedMaterials.Length, id);
            }
        }

        [Test]
        public void MetreScaleGroundPivotAndDeclaredSizeAgree()
        {
            // (width, height) ranges in metres; every prop must be human-scale next to a 1.8 m diver.
            var ranges = new Dictionary<string, (Vector2 w, Vector2 h)>
            {
                ["shore-dock"] = (new Vector2(2f, 2.8f), new Vector2(2.8f, 3.3f)),
                ["shore-dock-pole"] = (new Vector2(.25f, .6f), new Vector2(2.8f, 3.3f)),
                ["shore-dock-broken"] = (new Vector2(2f, 2.8f), new Vector2(2.8f, 3.3f)),
                ["shore-rock-1"] = (new Vector2(.5f, 1f), new Vector2(.4f, .8f)),
                ["shore-rock-2"] = (new Vector2(.5f, 1f), new Vector2(.8f, 1.3f)),
                ["shore-rock-3"] = (new Vector2(.5f, 1f), new Vector2(.4f, .9f)),
                ["shore-rock-4"] = (new Vector2(.5f, 1f), new Vector2(.5f, 1f)),
                ["shore-rock-5"] = (new Vector2(.4f, .9f), new Vector2(.4f, .9f)),
                ["shore-palm-1"] = (new Vector2(2f, 3.4f), new Vector2(3f, 4f)),
                ["shore-palm-2"] = (new Vector2(2f, 3.4f), new Vector2(2.3f, 3.2f)),
                ["shore-palm-3"] = (new Vector2(2f, 3.4f), new Vector2(2.3f, 3.2f)),
                ["shore-prop-barrel"] = (new Vector2(.5f, 1f), new Vector2(.5f, .9f)),
                ["shore-prop-bucket"] = (new Vector2(.5f, 1f), new Vector2(.5f, .9f)),
                ["shore-prop-bucket-fishes"] = (new Vector2(.6f, 1.3f), new Vector2(.5f, .9f)),
                ["shore-prop-anchor"] = (new Vector2(.6f, 1.2f), new Vector2(.05f, .2f)),
                ["shore-prop-chest"] = (new Vector2(1f, 1.8f), new Vector2(.6f, 1.1f))
            };
            foreach (var id in Ids)
            {
                var prefab = Load(id); var piece = prefab.GetComponent<ShoreKitPiece>(); var b = BoundsOf(prefab);
                Assert.That(Mathf.Max(b.size.x, b.size.z), Is.InRange(ranges[id].w.x, ranges[id].w.y), id + " width m");
                Assert.That(b.size.y, Is.InRange(ranges[id].h.x, ranges[id].h.y), id + " height m");
                Assert.AreEqual(b.size.x, piece.Size.x, .03f, id + " declared width"); Assert.AreEqual(b.size.y, piece.Size.y, .03f, id + " declared height");
                Assert.AreEqual(b.size.z, piece.Size.z, .03f, id + " declared depth");
                if (piece.Kind == ShoreKitKind.Dock) continue;                        // dock pieces stand on their posts: origin is not the base
                Assert.GreaterOrEqual(b.min.y, -.35f, id + " is at most half buried (rocks 1 and 5 are authored that way)");
                Assert.LessOrEqual(b.min.y, .05f, id + " does not float");
            }
        }

        [Test]
        public void DocksExposeTheirWalkingSurfaceAndOnlyDocksAreWalkable()
        {
            foreach (var id in Ids)
            {
                var piece = Load(id).GetComponent<ShoreKitPiece>();
                Assert.AreEqual(id == "shore-dock" || id == "shore-dock-broken", piece.Walkable, id);
                if (!piece.Walkable) { Assert.AreEqual(0f, piece.SurfaceY, id); continue; }
                var b = BoundsOf(Load(id));
                Assert.That(piece.SurfaceY, Is.InRange(b.min.y + .5f, b.max.y), id + " plank top lies inside the piece");
                Assert.AreEqual(.5f, piece.SurfaceY, .15f, id + " plank top as measured on the model (0.5 m above the origin)");
                Assert.Greater(piece.ColliderHint.x, 1.5f, id); Assert.Less(piece.ColliderHint.y, .5f, id + " thin walkway box");
            }
        }

        [Test]
        public void SourceFilesAreTheSelectedSixteenAndCarryTheirAttribution()
        {
            var fbx = Directory.GetFiles(Models, "*.fbx");
            Assert.AreEqual(16, fbx.Length);
            Assert.IsTrue(File.Exists("Assets/DeepDive/Art/Supplied/PirateKit/Textures/Atlas_Pirate.png"));
            var credits = File.ReadAllText("docs/assets/CREDITS.md");
            StringAssert.Contains("Pirate Kit", credits); StringAssert.Contains("CC0", credits);
            StringAssert.Contains("quaternius.com/packs/piratekit", credits);
        }
    }
}
