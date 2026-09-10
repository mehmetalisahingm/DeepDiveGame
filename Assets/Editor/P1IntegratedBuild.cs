using System;
using System.IO;
using DeepDive.Composition;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.Session.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.Editor
{
    public static class P1IntegratedBuild
    {
        public const string Prep = "Assets/DeepDive/World/Scenes/PrepArea.unity";
        public const string Dive = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string Prefabs = "Assets/DeepDive/Network/Prefabs";

        [MenuItem("DeepDive/P1/Connect team scenes and build Windows")]
        public static void GenerateAndBuild()
        {
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            var material = AssetDatabase.LoadAssetAtPath<Material>(Prefabs + "/DiverMaterial.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.15f, 0.55f, 0.65f) };
                AssetDatabase.CreateAsset(material, Prefabs + "/DiverMaterial.mat");
            }
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/NetworkDiver.prefab");
            if (player == null)
            {
                var root = new GameObject("NetworkDiver");
                root.AddComponent<NetworkObject>();
                var sync = root.AddComponent<NetworkTransform>();
                sync.SyncRotAngleX = sync.SyncRotAngleZ = false;
                sync.SyncScaleX = sync.SyncScaleY = sync.SyncScaleZ = false;
                var controller = root.AddComponent<CharacterController>();
                controller.height = 1.8f; controller.radius = 0.35f; controller.center = Vector3.up * 0.9f;
                var movement = root.AddComponent<NetworkPlayer>();
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "DiverBody"; body.transform.SetParent(root.transform, false);
                body.transform.localPosition = Vector3.up * 0.9f;
                body.transform.localScale = new Vector3(0.65f, 0.9f, 0.65f);
                UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
                body.GetComponent<Renderer>().sharedMaterial = material;
                var view = new GameObject("OwnerCamera"); view.transform.SetParent(root.transform, false);
                view.transform.localPosition = Vector3.up * 1.55f;
                var camera = view.AddComponent<Camera>(); camera.nearClipPlane = 0.05f; camera.enabled = false;
                view.AddComponent<AudioListener>().enabled = false;
                var settings = new SerializedObject(movement);
                settings.FindProperty("viewCamera").objectReferenceValue = camera;
                settings.FindProperty("bodyRenderer").objectReferenceValue = body.GetComponent<Renderer>();
                settings.ApplyModifiedPropertiesWithoutUndo();
                player = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/NetworkDiver.prefab");
                UnityEngine.Object.DestroyImmediate(root);
            }
            var prefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(Prefabs + "/NetworkPrefabs.asset");
            if (prefabs == null)
            {
                prefabs = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                prefabs.Add(new NetworkPrefab { Prefab = player });
                AssetDatabase.CreateAsset(prefabs, Prefabs + "/NetworkPrefabs.asset");
            }
            ConnectScene(Prep, player, prefabs);
            ConnectScene(Dive, player, prefabs);
            // Editor Play and the Windows player must use the same network scene table.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Prep, true), new EditorBuildSettingsScene(Dive, true),
                new EditorBuildSettingsScene("Assets/P0/Scenes/P0Example.unity", false) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Prep);
            BuildWindows();
        }

        private static void ConnectScene(string path, GameObject player, NetworkPrefabsList prefabs)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var old = GameObject.Find("P1NetworkSession");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.enabled = false;
                if (camera.TryGetComponent<AudioListener>(out var listener)) listener.enabled = false;
            }
            for (var i = 0; i < 4; i++)
            {
                var point = GameObject.Find("Spawn_" + i);
                if (point == null) throw new InvalidOperationException(path + ": missing Spawn_" + i);
                var spawn = point.GetComponent<PlayerSpawnPoint>() ?? point.AddComponent<PlayerSpawnPoint>();
                spawn.Slot = i;
            }
            var underwater = path == Dive;
            var portal = GameObject.Find(underwater ? "DiveExit" : "DiveEntry");
            if (portal == null) throw new InvalidOperationException("Missing entry/exit in " + path);
            var portalControl = portal.GetComponent<SessionPortal>() ?? portal.AddComponent<SessionPortal>();
            portalControl.RequiredPhase = underwater ? SessionPhase.Dive : SessionPhase.Prep;
            if (underwater)
            {
                var water = GameObject.Find("SwimVolume");
                if (water == null) throw new InvalidOperationException("Missing water volume");
                if (water.GetComponent<SwimVolume>() == null) water.AddComponent<SwimVolume>();
            }
            else
            {
                var root = new GameObject("P1NetworkSession");
                var transport = root.AddComponent<UnityTransport>();
                var manager = root.AddComponent<NetworkManager>();
                manager.NetworkConfig.NetworkTransport = transport;
                manager.NetworkConfig.PlayerPrefab = player;
                manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
                manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabs);
                manager.NetworkConfig.ConnectionApproval = true; manager.NetworkConfig.EnableSceneManagement = true;
                var session = root.AddComponent<NetworkSession>();
                var settings = new SerializedObject(session);
                settings.FindProperty("offlineScene").stringValue = "PrepArea";
                settings.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<SessionManager>(); root.AddComponent<SessionRoomUI>();
                var adapter = root.AddComponent<SessionNetworkAdapter>();
                var view = new GameObject("OfflineCamera"); view.transform.SetParent(root.transform, false);
                view.transform.position = new Vector3(0, 8, -8); view.transform.rotation = Quaternion.Euler(40, 0, 0);
                adapter.OfflineCamera = view.AddComponent<Camera>(); view.AddComponent<AudioListener>();
            }
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("DeepDive/P1/Build integrated Windows")]
        public static void BuildWindows()
        {
            var path = "Builds/P1-Integrated/DeepDiveGame-P1.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Prep, Dive },
                locationPathName = path, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Integrated P1 build failed.");
            Debug.Log("P1_BUILD_SUCCEEDED integrated=true");
        }
    }
}
