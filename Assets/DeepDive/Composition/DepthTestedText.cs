using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.Composition
{
    // A TextMesh that does not show through walls. The built-in font material draws a TextMesh over everything in a 3D scene, so "EKIPMAN" on the
    // store, "ORTAK EV" on the house and every stall label were readable through any building in front of them. This swaps the TextMesh's renderer
    // to a copy of the font material on a depth-tested shader (Resources/CityKit/DepthText) and keeps its atlas texture in step when the dynamic
    // font grows. Only the TextMesh it sits on changes: the font's own material, and so every IMGUI text, is left alone. If the shader is missing
    // the TextMesh keeps the original material (readable, merely not depth-tested).
    [DisallowMultipleComponent, RequireComponent(typeof(TextMesh))]
    public sealed class DepthTestedText : MonoBehaviour
    {
        public const string ShaderResource = "CityKit/DepthText";
        private static readonly Dictionary<Font, Material> Materials = new Dictionary<Font, Material>();
        private static bool subscribed;

        private void Awake() => Apply(GetComponent<TextMesh>());

        public static bool Apply(TextMesh mesh)
        {
            if (mesh == null) return false;
            var font = mesh.font != null ? mesh.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return false;
            mesh.font = font;
            if (!Materials.TryGetValue(font, out var material) || material == null)
            {
                var shader = Resources.Load<Shader>(ShaderResource);
                if (shader == null) return false;
                material = new Material(shader) { name = "DepthText_" + font.name, hideFlags = HideFlags.HideAndDontSave };
                Materials[font] = material;
            }
            material.mainTexture = font.material.mainTexture;
            if (!subscribed)
            {
                subscribed = true;
                Font.textureRebuilt += Rebuilt;
            }
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (renderer == null) return false;
            renderer.sharedMaterial = material;
            return true;
        }

        private static void Rebuilt(Font font)
        {
            if (font != null && Materials.TryGetValue(font, out var material) && material != null) material.mainTexture = font.material.mainTexture;
        }
    }
}
