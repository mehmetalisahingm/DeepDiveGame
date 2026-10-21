using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.World.Editor
{
    // P4: closes the water under the three coast slabs (docs/reports/P4-WADE-UNDERSIDE.md).
    //
    // Beach_Wade and Shore_Ramp are 0.4 m slopes and Shore_Ledge a 1 m slab, all hung over open water
    // down to SeaFloor. A diver fits under each one (39 / 25 / 15 m2) and, holding plain "up" or
    // "up + toward the shore", stays jammed against the underside (133/139 and 97/139 starts trapped
    // for 10 s). The product-day smoke diver jammed exactly there, at (-7.6, 3.3, -5.0).
    //
    // The slabs themselves are NOT touched: their transforms, top faces, angles, feet and the
    // water/land classification all stay what DiveTestAreaWaterSetup (P3.1) and
    // DiveTestAreaBeachSetup (P3.2) author, so every walk, wade and ramp route is unchanged. What
    // is added sits strictly under them:
    //
    //   <slope>_Fill  the slope's own box, same rotation and footprint, extended down along the
    //                 face normal until even its high end is under the sea bed. Its top face is the
    //                 slab's underside (FillOverlap inside it, so there is no hairline gap).
    //   <slope>_Toe   the wedge the Fill's tilted foot cap leaves open: a plain box from the sea bed
    //                 up to the walkable foot edge's height, between where the cap meets the sea bed
    //                 and straight under that edge. Its open-water face is a flat wall that meets the
    //                 slope at the foot edge, so no overhang is left for a diver to wedge under.
    //   Shore_Ledge_Fill  the ledge footprint from the sea bed to the ledge's underside.
    //
    // Nothing reaches past a slab's open-water edge (north for the wade and ledge, west for the
    // ramp) and nothing rises above a slab's underside, so no new walkable surface exists. Parts
    // that run into Beach_Platform, Shore_Ledge or under the sea bed are hidden inside them.
    //
    // Same rules as the P3 setups: never destroys anything, creates a missing piece and retunes an
    // existing one in place, scenery only (no NetworkObject), byte-idempotent (ApplyAndVerify).
    public static class DiveTestAreaShoreFillSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string ShoreMaterialPath = "Assets/DeepDive/World/Art/ShoreMaterial.mat";

        public const string WadeFillName = DiveTestAreaBeachSetup.WadeName + "_Fill";
        public const string WadeToeName = DiveTestAreaBeachSetup.WadeName + "_Toe";
        public const string RampFillName = DiveTestAreaWaterSetup.RampName + "_Fill";
        public const string RampToeName = DiveTestAreaWaterSetup.RampName + "_Toe";
        public const string LedgeFillName = DiveTestAreaWaterSetup.LedgeName + "_Fill";

        public static readonly string[] PieceNames = { WadeFillName, WadeToeName, RampFillName, RampToeName, LedgeFillName };

        // How far each fill reaches into the slab above it. Small against the slab (0.4 m) and far
        // below anything a CharacterController skin (0.08) could slip through.
        public const float FillOverlap = 0.02f;

        // How far the fills go under SeaFloor's plane, so their bottom edge never shows as a seam.
        public const float BelowSeaBed = 0.5f;

        [MenuItem("DeepDive/P4/Dive test area: close the coast slab undersides")]
        public static void Apply()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ShoreMaterialPath);
            if (material == null)
                throw new InvalidOperationException(
                    $"P4_SHOREFILL_NO_SHORE_MATERIAL path={ShoreMaterialPath} reason=run DeepDive/P3.1/Dive test area first");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wade = RequireSlab(scene, DiveTestAreaBeachSetup.WadeName);
            var ramp = RequireSlab(scene, DiveTestAreaWaterSetup.RampName);
            var ledge = RequireSlab(scene, DiveTestAreaWaterSetup.LedgeName);
            var before = PieceSnapshot(wade, ramp, ledge);

            // Beach_Wade descends along local +z (rotated about X); Shore_Ramp along local -x (about Z).
            PlaceSlopeFill(Ensure(scene, WadeFillName, material), Ensure(scene, WadeToeName, material), wade, Vector3.forward);
            PlaceSlopeFill(Ensure(scene, RampFillName, material), Ensure(scene, RampToeName, material), ramp, Vector3.left);
            PlaceLedgeFill(Ensure(scene, LedgeFillName, material), ledge);
            Physics.SyncTransforms();

            if (before != PieceSnapshot(wade, ramp, ledge))
                throw new InvalidOperationException("P4_SHOREFILL_SLAB_MOVED reason=the slabs are P3.1/P3.2 geometry and must not change");
            foreach (var (slab, foot) in new[] { (wade, Vector3.forward), (ramp, Vector3.left) })
                VerifyUnderSlab(scene, slab, foot);
            VerifyUnderSlab(scene, ledge, Vector3.zero);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"P4_SHOREFILL_SAVE_FAILED scene={ScenePath}");
            AssetDatabase.SaveAssets();
            Debug.Log("P4_SHOREFILL_READY pieces=" + string.Join(",", PieceNames));
        }

        public static void ApplyAndVerify()
        {
            Apply();
            var bytes = File.ReadAllBytes(ScenePath);
            Apply();
            if (!bytes.SequenceEqual(File.ReadAllBytes(ScenePath)))
                throw new InvalidOperationException("P4_SHOREFILL_SETUP_NOT_IDEMPOTENT");
            Debug.Log("P4_SHOREFILL_SETUP_IDEMPOTENT");
        }

        // --- Placement -----------------------------------------------------------------------

        // `descent` is the slab-local axis along which its top face goes down toward the water.
        private static void PlaceSlopeFill(GameObject fill, GameObject toe, Transform slab, Vector3 descent)
        {
            var scale = slab.localScale;
            var normal = slab.up;
            var bottomCentre = slab.position - normal * (scale.y * 0.5f);

            // Deep enough that the high (dry) end's underside corner is below the sea bed too.
            var highCorner = slab.TransformPoint(new Vector3(0f, -0.5f, 0f) - descent * 0.5f);
            var depth = (highCorner.y - DiveTestAreaBeachSetup.SeaBedY + BelowSeaBed) / normal.y + FillOverlap;
            fill.transform.SetPositionAndRotation(bottomCentre - normal * (depth * 0.5f - FillOverlap), slab.rotation);
            fill.transform.localScale = new Vector3(scale.x, depth, scale.z);

            // Where the fill's tilted foot cap (through the underside's foot edge) meets the sea bed, and the
            // slab's walkable foot edge. The toe runs from the first to straight under the second and up to
            // its height: stopping at the underside's foot edge instead left a 0.17 m overhang (the slab's
            // own end cap) whose crease with the toe face held a diver swimming up + toward the shore.
            var underFoot = slab.TransformPoint(new Vector3(0f, -0.5f, 0f) + descent * 0.5f);
            var topFoot = slab.TransformPoint(new Vector3(0f, 0.5f, 0f) + descent * 0.5f);
            var capAtSeaBed = underFoot - normal * ((underFoot.y - DiveTestAreaBeachSetup.SeaBedY) / normal.y);
            var outward = slab.TransformDirection(descent);
            outward.y = 0f;
            outward.Normalize();
            var span = Vector3.Dot(topFoot - capAtSeaBed, outward);
            var across = Vector3.Cross(Vector3.up, outward).normalized;
            var width = Mathf.Abs(Vector3.Dot(slab.TransformVector(Vector3.Cross(descent, Vector3.up)), across));

            var bottom = DiveTestAreaBeachSetup.SeaBedY - BelowSeaBed;
            var top = topFoot.y;
            var centre = topFoot - outward * (span * 0.5f);
            centre.y = (top + bottom) * 0.5f;
            toe.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(outward, Vector3.up));
            toe.transform.localScale = new Vector3(width, top - bottom, span);
        }

        private static void PlaceLedgeFill(GameObject fill, Transform ledge)
        {
            var bottom = DiveTestAreaBeachSetup.SeaBedY - BelowSeaBed;
            var top = ledge.position.y - ledge.localScale.y * 0.5f + FillOverlap;
            fill.transform.SetPositionAndRotation(new Vector3(ledge.position.x, (top + bottom) * 0.5f, ledge.position.z), Quaternion.identity);
            fill.transform.localScale = new Vector3(ledge.localScale.x, top - bottom, ledge.localScale.z);
        }

        // Creates the cube only when it is missing; an existing one is retuned in place.
        private static GameObject Ensure(UnityEngine.SceneManagement.Scene scene, string name, Material material)
        {
            var piece = FindRoot(scene, name);
            if (piece == null)
            {
                piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = name;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(piece, scene);
            }

            piece.layer = 0;
            var box = piece.GetComponent<BoxCollider>() ?? piece.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = Vector3.zero;
            box.size = Vector3.one;
            var renderer = piece.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            if (piece.GetComponent<NetworkObject>() != null)
                throw new InvalidOperationException($"P4_SHOREFILL_NETWORKED object={name}");
            return piece;
        }

        // --- Author-time guards ----------------------------------------------------------------

        // Every fill is under the slab: (1) straight down onto the slab's top face, the slab is the
        // first thing hit at the same height it was before (no fill above the walkable face), and
        // (2) no fill crosses the slab's open-water edge.
        private static void VerifyUnderSlab(UnityEngine.SceneManagement.Scene scene, Transform slab, Vector3 descent)
        {
            var collider = slab.GetComponent<Collider>();
            for (var u = -0.45f; u <= 0.45f; u += 0.15f)
            for (var v = -0.45f; v <= 0.45f; v += 0.15f)
            {
                var top = slab.TransformPoint(new Vector3(u, 0.5f, v));
                if (!Physics.Raycast(top + Vector3.up * 2f, Vector3.down, out var hit, 2.05f, ~0, QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException($"P4_SHOREFILL_TOP_LOST slab={slab.name} at={top}");
                if (PieceNames.Contains(hit.collider.name))
                    throw new InvalidOperationException($"P4_SHOREFILL_ABOVE_SLAB slab={slab.name} piece={hit.collider.name} at={hit.point}");
            }

            if (descent == Vector3.zero) return;
            var outward = slab.TransformDirection(descent);
            outward.y = 0f;
            outward.Normalize();
            var edge = Vector3.Dot(slab.TransformPoint(descent * 0.5f + Vector3.up * 0.5f), outward);
            foreach (var name in PieceNames)
            {
                var piece = FindRoot(scene, name).transform;
                if (!collider.bounds.Intersects(piece.GetComponent<Collider>().bounds)) continue;
                for (var i = 0; i < 8; i++)
                {
                    var vertex = piece.TransformPoint(new Vector3((i & 1) - 0.5f, ((i >> 1) & 1) - 0.5f, ((i >> 2) & 1) - 0.5f));
                    if (Vector3.Dot(vertex, outward) > edge + 1e-3f)
                        throw new InvalidOperationException($"P4_SHOREFILL_PAST_EDGE slab={slab.name} piece={name} at={vertex}");
                }
            }
        }

        private static string PieceSnapshot(params Transform[] slabs) =>
            string.Join(";", slabs.Select(s => $"{s.name}:{s.position:F5}:{s.rotation:F5}:{s.localScale:F5}"));

        private static Transform RequireSlab(UnityEngine.SceneManagement.Scene scene, string name)
        {
            var root = FindRoot(scene, name);
            if (root == null || root.GetComponent<BoxCollider>() == null)
                throw new InvalidOperationException($"P4_SHOREFILL_NO_SLAB name={name} reason=run the P3.1/P3.2 dive test area setups first");
            return root.transform;
        }

        private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(x => x.name == name).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException($"P4_SHOREFILL_DUPLICATE_ROOT name={name} count={matches.Length}");
            return matches.Length == 1 ? matches[0] : null;
        }
    }
}
