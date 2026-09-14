using System;
using UnityEditor;
using UnityEngine;

namespace DeepDive.World.Editor
{
    // Authors the bioluminescence event's two definition assets: its own quality table and the
    // RecordingEventDefinition that points at it. Nothing about the scene is touched here - the
    // event object is a later slice, and DiveTestAreaSetup (P2-B) and DiveTestAreaRecordingSetup
    // (P3-B scene half) stay untouched.
    //
    // A script rather than hand-written YAML for the same reason as the other setup scripts:
    // Unity owns asset GUIDs and serialization, and re-running this is the repair path for a bad
    // merge. Idempotent - twice over updates the same two assets, never creates a third.
    //
    // Every design number below is Mehmet's 14 September 2026 decision (a small plankton cluster,
    // 45 s after the dive starts, open 20 s, once per dive). The framing numbers are mine and are
    // first-pass tuning, worked out against this scene's camera the way the fish table was.
    public static class RecordingEventSetup
    {
        private const string EventsRoot = "Assets/DeepDive/World/Events";
        private const string DefinitionFolder = EventsRoot + "/Definitions";
        private const string DefinitionPath = DefinitionFolder + "/Bioluminescence.asset";

        // Quality tables live together under Recording/Quality, next to DefaultRecordingQuality:
        // they are the same asset type and the same shared tuning surface for Mert and Mehmet.
        private const string QualityFolder = "Assets/DeepDive/World/Recording/Quality";
        private const string QualityPath = QualityFolder + "/BioluminescenceRecordingQuality.asset";

        // The id that goes out as RecordingResult.SubjectId and that Mert prices per quality
        // tier. Stable forever: changing it orphans every price row and every saved recording.
        public const string EventId = "event_bioluminescence";

        [MenuItem("DeepDive/P3/Recording event: bioluminescence")]
        public static void Apply()
        {
            var quality = EnsureQualityTable();
            var definition = EnsureDefinition(quality);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"P3_EVENT_DEFINITION_READY id={definition.EventId} " +
                      $"delay={definition.TriggerDelaySeconds:0}s window={definition.WindowSeconds:0}s " +
                      $"radius={definition.SubjectRadiusMetres:0.00} tiers={quality.TierCount} " +
                      $"asset={DefinitionPath}");
        }

        // The plankton cluster is much bigger than the fish and it does not flee, so the band it
        // is filmed in is different from DefaultRecordingQuality's.
        //
        // Frame fill is radius / (distance * tan(halfFov)); at the diver camera's 60 degree
        // vertical FOV that is radius / (0.577 * distance). With a 1.5 m radius:
        //   3 m -> 0.87 (overfills the frame, hence minDistance 3)
        //   6 m -> 0.43 (about idealFrameFill, the shot this event is designed around)
        //  16 m -> 0.16 (still well above minFrameFill; the fog is what ends it, not the size)
        //
        // maxDistance 16 rather than the fish's 14 because the cluster glows and reads farther
        // through the water. The tier ladder keeps the agreed four rungs and the same thresholds
        // as the fish table (docs/plan/CONTRACTS.md: quality is a 1-4 tier, and Mert prices each
        // rung): a stationary subject at 6 m with steady centring scores about 0.85, so Platinum
        // is reachable inside the 20 second window but still needs 8 seconds of held framing.
        // First-pass numbers - the P3 playtest retunes them.
        private static RecordingQualityTable EnsureQualityTable()
        {
            EnsureFolder("Assets/DeepDive/World/Recording", "Quality");

            var table = AssetDatabase.LoadAssetAtPath<RecordingQualityTable>(QualityPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<RecordingQualityTable>();
                AssetDatabase.CreateAsset(table, QualityPath);
            }

            var serialized = new SerializedObject(table);
            serialized.FindProperty("minDistanceMetres").floatValue = 3f;
            serialized.FindProperty("maxDistanceMetres").floatValue = 16f;
            serialized.FindProperty("maxOffAxisDegrees").floatValue = 22f;
            serialized.FindProperty("minFrameFill").floatValue = 0.08f;
            serialized.FindProperty("idealFrameFill").floatValue = 0.45f;
            serialized.FindProperty("centeringWeight").floatValue = 0.4f;

            SetTiers(serialized.FindProperty("tiers"),
                ("Bronze", 0.25f, 2f),
                ("Silver", 0.45f, 4f),
                ("Gold", 0.60f, 6f),
                ("Platinum", 0.80f, 8f));

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);

            if (!table.IsValid(out var error))
                throw new InvalidOperationException($"P3_EVENT_QUALITY_INVALID asset={QualityPath} reason={error}");
            return table;
        }

        private static RecordingEventDefinition EnsureDefinition(RecordingQualityTable quality)
        {
            EnsureFolder(EventsRoot, "Definitions");

            var definition = AssetDatabase.LoadAssetAtPath<RecordingEventDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RecordingEventDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("eventId").stringValue = EventId;
            serialized.FindProperty("displayName").stringValue = "Biyolüminesans";
            serialized.FindProperty("triggerDelaySeconds").floatValue = 45f;
            serialized.FindProperty("windowSeconds").floatValue = 20f;
            serialized.FindProperty("quality").objectReferenceValue = quality;
            serialized.FindProperty("subjectRadiusMetres").floatValue = 1.5f;

            // Blue/turquoise, per Mehmet. The clip stays empty until Mert delivers it; a silent
            // event is a working event, so this is left alone rather than pointed at a stand-in.
            serialized.FindProperty("signalColor").colorValue = new Color(0.25f, 0.85f, 0.95f, 1f);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);

            if (!definition.IsValid(out var error))
                throw new InvalidOperationException($"P3_EVENT_DEFINITION_INVALID asset={DefinitionPath} reason={error}");
            return definition;
        }

        private static void SetTiers(SerializedProperty tiers,
            params (string Name, float MinScore01, float MinValidSeconds)[] ladder)
        {
            tiers.arraySize = ladder.Length;
            for (var i = 0; i < ladder.Length; i++)
            {
                var tier = tiers.GetArrayElementAtIndex(i);
                tier.FindPropertyRelative("name").stringValue = ladder[i].Name;
                tier.FindPropertyRelative("minScore01").floatValue = ladder[i].MinScore01;
                tier.FindPropertyRelative("minValidSeconds").floatValue = ladder[i].MinValidSeconds;
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
