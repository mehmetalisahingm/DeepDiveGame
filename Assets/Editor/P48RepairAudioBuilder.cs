using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeepDive.EditorTools
{
    // #189 P4.8-C: original synthesised wood sounds for lifting, seating and dropping the repair material, next to Mehmet's
    // OakNailImpact. Deterministic (fixed noise seeds), 22050 Hz mono 16-bit, no recording, no downloaded sample. The clips are
    // bound in the diver catalog; RepairCarryView plays them on host-published state changes only.
    public static class P48RepairAudioBuilder
    {
        private const string Root = "Assets/DeepDive/Art/RepairTools";
        private const string CatalogPath = "Assets/DeepDive/Network/Resources/DiverPresentationCatalog.asset";
        private const int Rate = 22050;

        // Wood-on-wood lift: a short dry knock as the plank leaves the rack, then a fading scrape.
        public static float Lift(float t, float noise) =>
            Mathf.Sin(2 * Mathf.PI * 190 * t) * Mathf.Exp(-t * 34) * .28f +
            noise * Mathf.Clamp01(t / .03f) * Mathf.Exp(-t * 13) * .2f +
            Mathf.Sin(2 * Mathf.PI * 880 * t) * Mathf.Exp(-t * 60) * .05f;

        // Heavy drop: low body thump, short noise burst and a softer rebound.
        public static float Drop(float t, float noise)
        {
            var main = Mathf.Sin(2 * Mathf.PI * 92 * t) * Mathf.Exp(-t * 15) * .5f + Mathf.Sin(2 * Mathf.PI * 150 * t) * Mathf.Exp(-t * 22) * .22f +
                       noise * Mathf.Exp(-t * 70) * .3f;
            var u = t - .12f;
            var rebound = u > 0 ? Mathf.Sin(2 * Mathf.PI * 118 * u) * Mathf.Exp(-u * 26) * .22f : 0f;
            return main + rebound;
        }

        // Seating: a hollow, higher knock (board meeting its frame), clearly not the nail impact (420 Hz + noise burst).
        public static float Seat(float t, float noise) =>
            Mathf.Sin(2 * Mathf.PI * 310 * t) * Mathf.Exp(-t * 40) * .38f + Mathf.Sin(2 * Mathf.PI * 640 * t) * Mathf.Exp(-t * 55) * .16f +
            noise * Mathf.Exp(-t * 110) * .16f;

        [MenuItem("DeepDive/P4.8/Generate repair carry sounds")]
        public static void Generate()
        {
            Write("OakPlankLift.wav", .26f, 311, Lift);
            Write("OakPlankDrop.wav", .36f, 523, Drop);
            Write("OakPlankSeat.wav", .2f, 719, Seat);
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Missing diver catalog");
            var serialized = new SerializedObject(catalog);
            Bind(serialized, "repairLiftClip", "OakPlankLift.wav");
            Bind(serialized, "repairDropClip", "OakPlankDrop.wav");
            Bind(serialized, "repairSeatClip", "OakPlankSeat.wav");
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("P48_REPAIR_AUDIO_DONE");
        }

        public static void GenerateBatch() => Generate();

        private static void Bind(SerializedObject catalog, string field, string file) =>
            catalog.FindProperty(field).objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root + "/" + file);

        private static void Write(string file, float seconds, uint seed, Func<float, float, float> voice)
        {
            var count = Mathf.RoundToInt(seconds * Rate);
            using (var bytes = new MemoryStream())
            using (var writer = new BinaryWriter(bytes))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(Rate); writer.Write(Rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                var state = seed;
                for (var i = 0; i < count; i++)
                {
                    state = state * 1664525 + 1013904223;
                    var noise = ((state >> 8) & 65535) / 32768f - 1f;
                    var fade = Mathf.Clamp01((count - i) / 200f); // no click at the end
                    writer.Write((short)(Mathf.Clamp(voice(i / (float)Rate, noise) * fade, -1f, 1f) * short.MaxValue));
                }
                var path = Root + "/" + file; var output = bytes.ToArray();
                if (!File.Exists(path) || Convert.ToBase64String(File.ReadAllBytes(path)) != Convert.ToBase64String(output)) File.WriteAllBytes(path, output);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}
