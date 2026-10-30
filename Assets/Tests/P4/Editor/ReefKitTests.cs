using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.P4.Tests
{
    // #217 P4.11-C: the reef art kit is isolated presentation with a bounded cost: stable ids, metre scale, ground pivot, two LODs,
    // few materials, one measured emission, one cosmetic motion and no gameplay components. Placement stays Utku's call.
    public sealed class ReefKitTests
    {
        private static IEnumerable<string> Ids => ReefKitPiece.AllIds;

        private static GameObject Load(string id)
        {
            var prefab = ReefKitPiece.Load(id);
            Assert.IsNotNull(prefab, id);
            return prefab;
        }

        private static int Tris(Mesh mesh)
        {
            var n = 0;
            for (var s = 0; s < mesh.subMeshCount; s++) n += (int)mesh.GetIndexCount(s) / 3;
            return n;
        }

        [Test]
        public void SixPiecesWithStableIdsAndNoGameplayComponents()
        {
            Assert.AreEqual(6, ReefKitPiece.AllIds.Length);
            Assert.AreEqual(3, Ids.Count(i => Load(i).GetComponent<ReefKitPiece>().Kind == ReefKitKind.Coral), "three coral silhouettes");
            Assert.AreEqual(2, Ids.Count(i => Load(i).GetComponent<ReefKitPiece>().Kind == ReefKitKind.Rock), "two rocks");
            Assert.AreEqual(1, Ids.Count(i => Load(i).GetComponent<ReefKitPiece>().Kind == ReefKitKind.Plant), "one plant group");
            foreach (var id in Ids)
            {
                var prefab = Load(id);
                Assert.AreEqual(id, prefab.name); Assert.AreEqual(id, prefab.GetComponent<ReefKitPiece>().Id);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), id + " carries no collider (placement decides)");
                Assert.IsEmpty(prefab.GetComponentsInChildren<Rigidbody>(true), id);
                Assert.IsFalse(prefab.GetComponentsInChildren<Component>(true).Any(c => c != null && c.GetType().Name == "NetworkObject"), id + " is not networked");
                Assert.IsFalse(prefab.GetComponentsInChildren<Renderer>(true).Any(r => r.shadowCastingMode != ShadowCastingMode.Off), id + " casts no shadows");
            }
        }

        [Test]
        public void TriangleAndRendererBudgetWithASimplifiedFarLod()
        {
            foreach (var id in Ids)
            {
                var prefab = Load(id); var piece = prefab.GetComponent<ReefKitPiece>();
                var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers) Assert.LessOrEqual(r.sharedMaterials.Length, 2, id + " materials per renderer");
                if (piece.Kind == ReefKitKind.Plant)
                {
                    Assert.AreEqual(3, renderers.Length, "three clumps");
                    Assert.LessOrEqual(renderers.Sum(r => Tris(r.GetComponent<MeshFilter>().sharedMesh)), 400, id);
                    continue;
                }
                var lod = prefab.GetComponent<LODGroup>();
                Assert.IsNotNull(lod, id);
                var lods = lod.GetLODs();
                Assert.AreEqual(2, lods.Length, id);
                var near = Tris(lods[0].renderers[0].GetComponent<MeshFilter>().sharedMesh);
                var far = Tris(lods[1].renderers[0].GetComponent<MeshFilter>().sharedMesh);
                Assert.LessOrEqual(near, 1300, id + " near LOD triangles");
                Assert.LessOrEqual(far, near * .45f, id + " far LOD is simplified");
                Assert.AreEqual(1, lods[0].renderers.Length);
                Assert.AreEqual(1, lods[1].renderers.Length, "one renderer per LOD");
            }
        }

        [Test]
        public void MetreScaleGroundPivotAndDeclaredSizeAgree()
        {
            var ranges = new Dictionary<string, (Vector2 w, Vector2 h)>
            {
                ["reef-coral-branching"] = (new Vector2(.8f, 1.6f), new Vector2(.7f, 1.4f)),
                ["reef-coral-fan"] = (new Vector2(1f, 1.8f), new Vector2(.9f, 1.6f)),
                ["reef-coral-mound"] = (new Vector2(.8f, 1.6f), new Vector2(.25f, .7f)),
                ["reef-rock-boulder"] = (new Vector2(1.5f, 3f), new Vector2(.5f, 1.4f)),
                ["reef-rock-arch"] = (new Vector2(3f, 5f), new Vector2(1.8f, 2.6f)),
                ["reef-plant-seagrass"] = (new Vector2(.7f, 1.6f), new Vector2(.7f, 1.6f))
            };
            foreach (var id in Ids)
            {
                var prefab = Load(id); var piece = prefab.GetComponent<ReefKitPiece>();
                var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name != "LOD1").ToArray();
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                Assert.AreEqual(0f, b.min.y, .03f, id + " sits on the ground (pivot at the base)");
                Assert.LessOrEqual(Mathf.Abs(b.center.x), .45f, id + " centred");
                Assert.LessOrEqual(Mathf.Abs(b.center.z), .45f, id);
                Assert.That(b.size.x, Is.InRange(ranges[id].w.x, ranges[id].w.y), id + " width m");
                Assert.That(b.size.y, Is.InRange(ranges[id].h.x, ranges[id].h.y), id + " height m");
                Assert.AreEqual(b.size.x, piece.Size.x, .03f, id + " declared width");
                Assert.AreEqual(b.size.y, piece.Size.y, .03f, id + " declared height");
            }
        }

        [Test]
        public void EveryPieceHasItsOwnMeshAndSilhouetteNotAColourCopy()
        {
            var near = Ids.Select(id => Load(id).GetComponentsInChildren<MeshFilter>(true).First().sharedMesh).ToArray();
            Assert.AreEqual(near.Length, near.Select(m => m.GetInstanceID()).Distinct().Count());
            Assert.AreEqual(near.Length, near.Select(m => (m.vertexCount, Mathf.RoundToInt(m.bounds.size.x * 10), Mathf.RoundToInt(m.bounds.size.y * 10))).Distinct().Count(), "no two pieces share shape and size");
            var aspects = Ids.Select(Load).Select(p => p.GetComponent<ReefKitPiece>()).Where(p => p.Kind == ReefKitKind.Coral).Select(p => p.Size.y / p.Size.x).OrderBy(a => a).ToArray();
            Assert.GreaterOrEqual(aspects[1] - aspects[0], .15f, "corals differ in proportion: tall tree, fan, low mound");
            Assert.GreaterOrEqual(aspects[2] - aspects[1], .15f);
        }

        [Test]
        public void MaterialsAreFewUrpAndOnlyTheBranchingCoralTipsGlow()
        {
            var materials = Ids.SelectMany(id => Load(id).GetComponentsInChildren<MeshRenderer>(true)).SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            Assert.LessOrEqual(materials.Length, 10);
            foreach (var m in materials)
            {
                Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, m.name);
                var glows = m.IsKeywordEnabled("_EMISSION") && m.GetColor("_EmissionColor").maxColorComponent > 0f;
                Assert.AreEqual(m.name == "RK_CoralBranchTip", glows, m.name + " emission");
                if (glows) Assert.LessOrEqual(m.GetColor("_EmissionColor").maxColorComponent, 1f, "measured glow, not a lamp");
            }
        }

        [Test]
        public void OnlySeagrassMovesAndItsLeanIsBounded()
        {
            foreach (var id in Ids)
                Assert.AreEqual(id == "reef-plant-seagrass", Load(id).GetComponentsInChildren<ReefPlantSway>(true).Length == 1, id);
            var sway = Load("reef-plant-seagrass").GetComponent<ReefPlantSway>();
            Assert.LessOrEqual(sway.AmplitudeDegrees, 8f);
            var peak = 0f;
            for (var t = 0f; t < 30f; t += .05f)
            {
                var l = ReefPlantSway.Lean(t, sway.PeriodSeconds, sway.AmplitudeDegrees, 1.3f);
                peak = Mathf.Max(peak, Mathf.Abs(l.x), Mathf.Abs(l.y));
            }
            Assert.LessOrEqual(peak, sway.AmplitudeDegrees + .001f);
            Assert.Greater(peak, sway.AmplitudeDegrees * .8f, "it does move");
        }

        [Test]
        public void ArchOpeningLetsADiverSwimThrough()
        {
            var arch = (GameObject)Object.Instantiate(Load("reef-rock-arch"));
            try
            {
                foreach (var filter in arch.GetComponentsInChildren<MeshFilter>().Where(f => f.name == "LOD0")) filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                Physics.SyncTransforms();
                Assert.IsTrue(arch.GetComponent<ReefKitPiece>().WalkThrough);
                // A prone diver is about 0.7 m wide and 0.5 m deep; the opening must pass 1.0 m x 1.0 m (y 0.4-1.4) through the whole thickness.
                var blocked = Physics.OverlapBox(new Vector3(0f, .9f, 0f), new Vector3(.5f, .5f, 1.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide).Select(c => c.name).ToArray();
                Assert.IsEmpty(blocked, "opening clear");
                Assert.IsNotEmpty(Physics.OverlapBox(new Vector3(0f, 1.95f, 0f), new Vector3(.3f, .7f, .8f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide), "a box straddling the span meets its surface (proves the query sees the mesh)");
            }
            finally { Object.DestroyImmediate(arch); }
        }

        [Test]
        public void BudgetEvidenceListsEveryPiece()
        {
            var lines = File.ReadAllLines("docs/reports/evidence/p411-reef-kit/budget.csv");
            Assert.AreEqual(7, lines.Length);
            foreach (var id in Ids) Assert.IsTrue(lines.Any(l => l.StartsWith(id + ",")), id);
        }
    }
}
