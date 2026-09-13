using System;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.World.Editor
{
    // Authors the P3-B half of DiveTestArea: the quality table asset and the RecordingSubject on
    // the fish that already lives there (docs/plan/PHASES.md P3-B: "Canli/olay tanima,
    // gorus/mesafe/sure kontrolu, cekim kalitesi ...").
    //
    // Separate from DiveTestAreaSetup on purpose. That script is P2-B's: its menu path, its log
    // tags and its comments all say so, and phases are tracked file by file
    // (docs/plan/WORKFLOW.md: "Faz acikken bir kisi gelecekteki faza ait kod, sahne veya icerik
    // eklemez"). The two do not fight: DiveTestAreaSetup only ever adds components and rebuilds
    // the fish's children, so a P2 re-run leaves the RecordingSubject on the root alone.
    //
    // Run this after DiveTestAreaSetup.Apply, which is what authors the fish in the first place.
    // Order matters only the first time; afterwards either may be re-run on its own.
    //
    // Why a script rather than hand-written scene YAML: the same reason as P2-B. No new
    // NetworkObject is created here - the subject rides the fish's existing one, so no
    // GlobalObjectIdHash is regenerated - but DiveTestArea is shared ground and re-running this
    // is the repair path for a bad merge, so it is idempotent: twice over produces one subject
    // with the same settings, never a second one.
    public static class DiveTestAreaRecordingSetup
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string RecordingRoot = "Assets/DeepDive/World/Recording";
        private const string QualityFolder = RecordingRoot + "/Quality";
        private const string QualityPath = QualityFolder + "/DefaultRecordingQuality.asset";

        // Everything solid in DiveTestArea sits on Default (the project also defines Water and
        // UI, unused by the dive). Named explicitly rather than left at ~0 so adding a layer
        // later cannot silently turn it into an obstacle; the subject's own hierarchy is skipped
        // by RecordingSubject itself, and SwimVolume/DiveExit are triggers it already ignores.
        // A new layer for divers or geometry is a project-wide change and needs coordinating
        // (docs/plan/WORKFLOW.md: "katman ve render ayarlari birlikte koordine edilir").
        private const int OccluderLayers = 1 << 0;

        [MenuItem("DeepDive/P3/Dive test area: recording subject")]
        public static void Apply()
        {
            var quality = EnsureQualityTable();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var fish = GameObject.Find(DiveTestAreaSetup.FishName);
            if (fish == null)
                throw new InvalidOperationException(
                    $"P3_DIVEAREA_NO_FISH scene={ScenePath} name={DiveTestAreaSetup.FishName} " +
                    "reason=run DeepDive/P2/Dive test area first");

            var subject = EnsureSubject(fish, quality);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"P3_DIVEAREA_RECORDING_SAVE_FAILED scene={ScenePath}");
            AssetDatabase.SaveAssets();

            Debug.Log($"P3_DIVEAREA_RECORDING_READY subject={subject.ExpectedSubjectId} " +
                      $"radius={SubjectRadius(fish):0.00} tiers={quality.TierCount} " +
                      $"occluders={OccluderLayers} table={QualityPath}");
        }

        // The thresholds half of P3-B as a static definition asset, the same shape as
        // SeaBass.asset. Framing gates stay at the tuning defaults; the tier ladder is set here
        // because the numbers were chosen against this scene's fish and camera, and the shape of
        // the ladder is Mert's pricing input (docs/plan/CONTRACTS.md: "Kalite esikleri ve fiyat
        // katsayilari Utku/Mert'in ortak veri tablosunda tutulur. Utku kaliteyi, Mert krediyi
        // belirler").
        private static RecordingQualityTable EnsureQualityTable()
        {
            EnsureFolder(RecordingRoot, "Quality");
            var table = AssetDatabase.LoadAssetAtPath<RecordingQualityTable>(QualityPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<RecordingQualityTable>();
                AssetDatabase.CreateAsset(table, QualityPath);
            }

            var serialized = new SerializedObject(table);
            serialized.FindProperty("minDistanceMetres").floatValue = 1.5f;
            serialized.FindProperty("maxDistanceMetres").floatValue = 14f;
            serialized.FindProperty("maxOffAxisDegrees").floatValue = 22f;
            serialized.FindProperty("minFrameFill").floatValue = 0.08f;
            serialized.FindProperty("idealFrameFill").floatValue = 0.45f;
            serialized.FindProperty("centeringWeight").floatValue = 0.4f;

            // Tuned against the real numbers rather than picked round: a 0.55 m subject radius
            // at the diver camera's 60 degree vertical FOV gives a frame fill of 0.19 at 5 m and
            // 0.10 at 10 m, and minFrameFill cuts the shot off entirely past ~11.9 m. The fish
            // flees from 5 m (SeaBass.fleeRadius) at 3.2 m/s against a diver's 3, so a steady
            // shot lives in the 5-12 m band and scores about 0.4-0.65 there.
            //
            // Hence Gold at 0.60 and Platinum at 0.80 rather than the class defaults of 0.70 and
            // 0.85: at those, Gold needed near-perfect centring inside 4 m and Platinum was not
            // reachable at all in this scene. First-pass numbers - the P3 playtest retunes them.
            SetTiers(serialized.FindProperty("tiers"),
                ("Bronze", 0.25f, 2f),
                ("Silver", 0.45f, 4f),
                ("Gold", 0.60f, 6f),
                ("Platinum", 0.80f, 8f));

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);

            if (!table.IsValid(out var error))
                throw new InvalidOperationException($"P3_QUALITY_TABLE_INVALID asset={QualityPath} reason={error}");
            return table;
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

        private static RecordingSubject EnsureSubject(GameObject fish, RecordingQualityTable quality)
        {
            // The bridge resolves a target by walking the aimed-at NetworkObject, and the
            // subject reads the species off a FishActor beside it, so both have to be there
            // before this means anything. Checked rather than assumed: the fish is authored by
            // another script and the two files can drift.
            if (!fish.TryGetComponent<NetworkObject>(out _))
                throw new InvalidOperationException($"P3_DIVEAREA_NO_NETWORK_OBJECT object={fish.name}");
            if (!fish.TryGetComponent<FishActor>(out var actor) || actor.Species == null)
                throw new InvalidOperationException($"P3_DIVEAREA_NO_SPECIES object={fish.name}");

            var subject = fish.GetComponent<RecordingSubject>() ?? fish.AddComponent<RecordingSubject>();

            var serialized = new SerializedObject(subject);
            // Left empty deliberately: the id then falls back to FishActor's speciesId, so the
            // species asset stays the single source of truth for what this subject is filmed as.
            // Written rather than skipped so a hand-edited leftover cannot survive a re-run.
            serialized.FindProperty("subjectId").stringValue = "";
            serialized.FindProperty("quality").objectReferenceValue = quality;
            serialized.FindProperty("subjectRadiusMetres").floatValue = SubjectRadius(fish);
            // The fish root is already the body centre, so there is nothing better to aim at.
            serialized.FindProperty("framingAnchor").objectReferenceValue = null;
            serialized.FindProperty("occluderLayers").intValue = OccluderLayers;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (string.IsNullOrWhiteSpace(subject.ExpectedSubjectId))
                throw new InvalidOperationException($"P3_DIVEAREA_NO_SUBJECT_ID object={fish.name}");
            return subject;
        }

        // RecordingFraming.FrameFill measures the subject's radius against the frame half
        // height, so for a fish filmed side-on the useful figure is its half length, not its
        // girth. Derived from the collider the harpoon already uses instead of being typed in,
        // so re-shaping the fish in DiveTestAreaSetup cannot quietly leave the camera judging
        // the old size. The root scale is one (DiveTestAreaSetup pins it), so no scale maths.
        private static float SubjectRadius(GameObject fish)
        {
            if (!fish.TryGetComponent<CapsuleCollider>(out var hull))
                throw new InvalidOperationException($"P3_DIVEAREA_NO_HULL object={fish.name}");
            return Mathf.Max(hull.height * 0.5f, hull.radius);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
