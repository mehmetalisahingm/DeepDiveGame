using System;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.World.Editor
{
    public static class DiveTestAreaEventSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string EventName = "BioluminescenceEvent";
        private const string DefinitionPath = "Assets/DeepDive/World/Events/Definitions/Bioluminescence.asset";
        private const string ParticleMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";

        [MenuItem("DeepDive/P3/Dive test area: special event")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var definition = AssetDatabase.LoadAssetAtPath<RecordingEventDefinition>(DefinitionPath);
            if (definition == null || !definition.IsValid(out _)) throw new InvalidOperationException("Invalid event definition");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab");
            if (prefab == null) throw new InvalidOperationException("Missing diver prefab");
            var matches = scene.GetRootGameObjects().Where(x => x.name == EventName).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("Duplicate event roots");
            var root = matches.Length == 1 ? matches[0] : new GameObject(EventName);
            root.transform.SetPositionAndRotation(new Vector3(-9, 3.5f, -2), Quaternion.identity);
            root.transform.localScale = Vector3.one;
            root.layer = 0;
            Ensure<NetworkObject>(root); // Unity authors the scene hash; never hand-write it.
            var subject = Ensure<RecordingSubject>(root);
            var runner = Ensure<SpecialEventRunner>(root);
            var presenter = Ensure<SpecialEventPresenter>(root);
            var hull = Ensure<SphereCollider>(root);
            hull.radius = definition.SubjectRadiusMetres;
            hull.center = Vector3.zero;
            hull.isTrigger = false;
            hull.excludeLayers = 1 << prefab.layer;

            var data = new SerializedObject(subject);
            data.FindProperty("subjectId").stringValue = definition.EventId;
            data.FindProperty("quality").objectReferenceValue = definition.Quality;
            data.FindProperty("subjectRadiusMetres").floatValue = definition.SubjectRadiusMetres;
            data.FindProperty("framingAnchor").objectReferenceValue = root.transform;
            data.FindProperty("occluderLayers").intValue = 1;
            data.ApplyModifiedPropertiesWithoutUndo();
            data = new SerializedObject(runner);
            data.FindProperty("definition").objectReferenceValue = definition;
            data.ApplyModifiedPropertiesWithoutUndo();

            // Only empty slots get placeholders. Never retune Mert's assigned components.
            data = new SerializedObject(presenter);
            if (data.FindProperty("glowParticles").objectReferenceValue == null)
            {
                var child = root.transform.Find("PlaceholderGlow");
                var particles = child != null ? child.GetComponent<ParticleSystem>() : null;
                if (particles == null)
                {
                    var glow = new GameObject("PlaceholderGlow");
                    glow.transform.SetParent(root.transform, false);
                    particles = glow.AddComponent<ParticleSystem>();
                    var main = particles.main;
                    main.playOnAwake = false; main.loop = true;
                    main.startLifetime = 1.5f; main.startSpeed = 0.05f; main.startSize = 0.16f;
                    main.startColor = definition.SignalColor;
                    var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = definition.SubjectRadiusMetres;
                    var emission = particles.emission; emission.rateOverTime = 35;
                    var material = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterial);
                    if (material == null) throw new InvalidOperationException("URP particle material missing");
                    particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                data.FindProperty("glowParticles").objectReferenceValue = particles;
            }
            if (data.FindProperty("glowLight").objectReferenceValue == null)
            {
                var child = root.transform.Find("PlaceholderLight");
                var light = child != null ? child.GetComponent<Light>() : null;
                if (light == null)
                {
                    var glow = new GameObject("PlaceholderLight");
                    glow.transform.SetParent(root.transform, false);
                    light = glow.AddComponent<Light>();
                    light.type = LightType.Point; light.color = definition.SignalColor;
                    light.range = 6; light.intensity = 3; light.enabled = false;
                }
                data.FindProperty("glowLight").objectReferenceValue = light;
            }
            // startAudio and the definition's clip are deliberately untouched.
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Event scene save failed");
            AssetDatabase.SaveAssets();
            Debug.Log("P3_EVENT_SCENE_READY event_bioluminescence position=(-9,3.5,-2)");
        }

        public static void ApplyAndVerify()
        {
            Apply();
            var bytes = File.ReadAllBytes(ScenePath);
            Apply();
            if (!bytes.SequenceEqual(File.ReadAllBytes(ScenePath))) throw new InvalidOperationException("Event setup is not byte-idempotent");
            Debug.Log("P3_EVENT_SETUP_IDEMPOTENT");
        }

        private static T Ensure<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
    }
}
