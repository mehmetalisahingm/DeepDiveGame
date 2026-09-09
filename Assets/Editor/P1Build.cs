using System;
using System.IO;
using DeepDive.Network;
using DeepDive.P1.Lab;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Editor
{
    public static class P1Build
    {
        private const string Root = "Assets/Tests/P1/Fixtures";
        public const string Lab = Root + "/P1NetworkLab.unity";
        public const string WaterLab = Root + "/P1NetworkWaterLab.unity";

        [MenuItem("DeepDive/P1/Generate network test fixtures")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/LabMaterial.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.22f, 0.63f, 0.68f) };
                AssetDatabase.CreateAsset(material, Root + "/LabMaterial.mat");
            }
            var playerObject = new GameObject("NetworkDiver");
            playerObject.AddComponent<NetworkObject>();
            var sync = playerObject.AddComponent<NetworkTransform>();
            sync.SyncRotAngleX = false; sync.SyncRotAngleZ = false;
            sync.SyncScaleX = false; sync.SyncScaleY = false; sync.SyncScaleZ = false;
            var controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.35f; controller.center = Vector3.up * 0.9f;
            var player = playerObject.AddComponent<NetworkPlayer>();
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "DiverBody"; body.transform.SetParent(playerObject.transform, false);
            body.transform.localPosition = Vector3.up * 0.9f; body.transform.localScale = new Vector3(0.65f, 0.9f, 0.65f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = material;
            var cameraObject = new GameObject("OwnerCamera"); cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.55f;
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = 0.05f; camera.enabled = false;
            cameraObject.AddComponent<AudioListener>().enabled = false;
            var serialized = new SerializedObject(player);
            serialized.FindProperty("viewCamera").objectReferenceValue = camera;
            serialized.FindProperty("bodyRenderer").objectReferenceValue = body.GetComponent<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(playerObject, Root + "/NetworkDiver.prefab");
            UnityEngine.Object.DestroyImmediate(playerObject);
            var prefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(Root + "/NetworkPrefabs.asset");
            if (prefabs == null)
            {
                prefabs = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                prefabs.Add(new NetworkPrefab { Prefab = prefab });
                AssetDatabase.CreateAsset(prefabs, Root + "/NetworkPrefabs.asset");
            }
            CreateScene(false, material, prefab, prefabs);
            CreateScene(true, material, prefab, prefabs);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Lab);
            Debug.Log("P1_FIXTURES_READY");
        }

        private static void CreateScene(bool water, Material material, GameObject prefab, NetworkPrefabsList prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.35f, 0.5f, 0.55f);
            RenderSettings.fog = water; RenderSettings.fogColor = new Color(0.03f, 0.2f, 0.3f); RenderSettings.fogDensity = 0.025f;
            var light = new GameObject("LabLight").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            Cube("LabFloor", new Vector3(0, water ? -10.5f : -0.5f, 0), new Vector3(60, 1, 60), material);
            Cube("CollisionWall", new Vector3(0, water ? -7 : 1.5f, 12), new Vector3(60, 6, 1), material);
            for (int slot = 0; slot < 4; slot++)
            {
                var spawn = new GameObject("Spawn" + slot).AddComponent<PlayerSpawnPoint>();
                spawn.Slot = slot; spawn.transform.position = new Vector3(-6 + slot * 4, water ? -5 : 0.05f, 0);
            }
            if (water)
            {
                var volume = new GameObject("LabWater");
                volume.transform.position = new Vector3(0, -3, 0);
                volume.AddComponent<BoxCollider>().size = new Vector3(60, 14, 60);
                volume.AddComponent<SwimVolume>();
            }
            else
            {
                var bootstrap = new GameObject("NetworkBootstrap");
                var transport = bootstrap.AddComponent<UnityTransport>();
                var manager = bootstrap.AddComponent<NetworkManager>();
                manager.NetworkConfig.NetworkTransport = transport;
                manager.NetworkConfig.PlayerPrefab = prefab;
                manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
                manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabs);
                manager.NetworkConfig.ConnectionApproval = true;
                manager.NetworkConfig.EnableSceneManagement = true;
                var session = bootstrap.AddComponent<NetworkSession>();
                var sessionSettings = new SerializedObject(session);
                sessionSettings.FindProperty("offlineScene").stringValue = "P1NetworkLab";
                sessionSettings.ApplyModifiedPropertiesWithoutUndo();
                var console = bootstrap.AddComponent<NetworkLabConsole>();
                bootstrap.AddComponent<NetworkSmokeDriver>();
                var overview = new GameObject("OfflineCamera"); overview.transform.SetParent(bootstrap.transform, false);
                overview.transform.position = new Vector3(0, 12, -18); overview.transform.rotation = Quaternion.Euler(25, 0, 0);
                console.OfflineCamera = overview.AddComponent<Camera>();
                overview.AddComponent<AudioListener>();
            }
            EditorSceneManager.SaveScene(scene, water ? WaterLab : Lab);
        }

        private static void Cube(string name, Vector3 position, Vector3 size, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name;
            cube.transform.position = position; cube.transform.localScale = size;
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }

        [MenuItem("DeepDive/P1/Build network test Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(Lab) || !File.Exists(WaterLab)) throw new InvalidOperationException("Generate P1 test fixtures first.");
            var path = "Builds/P1-NetworkLab/DeepDiveGame-P1.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Lab, WaterLab }, locationPathName = path,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("P1 build failed: " + report.summary.result);
            Debug.Log("P1_BUILD_SUCCEEDED");
        }

        public static void GenerateAndBuild() { Generate(); BuildWindows(); }
    }
}
