using System;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Editor
{
    // Read-only preparation for #173. Generated scene objects are discarded, never saved.
    public static class P4PlayerScaleAudit
    {
        [Serializable] private sealed class Audit
        {
            public float playerHeight, radius, skin, stepOffset, slopeLimit, walkSpeed, cameraHeight, fov, nearClip;
            public float effectiveWidth, twoPersonWidth, fourPersonWidth, openFacadeWidth, nearestBedExitToSpawn, nearestSpawnPair;
            public Vector3 dockSize;
            public bool hasAuthoredDoor;
            public string cameraPrefab = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
            public string scene = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
            public string[] furniture;
        }

        public static void Run()
        {
            var audit = new Audit();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(audit.cameraPrefab);
            var controller = prefab.GetComponent<CharacterController>();
            var camera = prefab.GetComponentInChildren<Camera>(true);
            audit.playerHeight = controller.height; audit.radius = controller.radius; audit.skin = controller.skinWidth;
            audit.stepOffset = controller.stepOffset; audit.slopeLimit = controller.slopeLimit;
            audit.cameraHeight = prefab.transform.InverseTransformPoint(camera.transform.position).y;
            audit.fov = camera.fieldOfView; audit.nearClip = camera.nearClipPlane;
            audit.walkSpeed = new SerializedObject(prefab.GetComponent<NetworkPlayer>()).FindProperty("walkSpeed").floatValue;
            audit.effectiveWidth = 2f * (audit.radius + audit.skin);
            audit.twoPersonWidth = 2f * audit.effectiveWidth; audit.fourPersonWidth = 4f * audit.effectiveWidth;
            var prior = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(audit.scene, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene); HomeSceneSetup.EnsureRig(scene);
                foreach (var root in scene.GetRootGameObjects())
                foreach (var service in root.GetComponentsInChildren<ServicePointAnchor>(true)) TownStorefront.Build(service);
                Physics.SyncTransforms();
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                var left = all.Single(t => t.name == "HomeLeft").GetComponent<Collider>().bounds;
                var right = all.Single(t => t.name == "HomeRight").GetComponent<Collider>().bounds;
                audit.openFacadeWidth = right.min.x - left.max.x;
                audit.hasAuthoredDoor = all.Any(t => t.name == "HomeDoor" && t.GetComponent<Collider>() != null);
                audit.dockSize = all.Single(t => t.name == "Dock_Town").GetComponent<Collider>().bounds.size;
                audit.furniture = all.Where(t => t.name == "Counter" || t.name == "CounterTop").Select(t =>
                    $"{t.parent.parent.name}/{t.name}: center={t.GetComponent<Renderer>().bounds.center}, size={t.GetComponent<Renderer>().bounds.size}").ToArray();
                var beds = all.Select(t => t.GetComponent<HomeInteractionAnchor>()).Where(b => b != null && b.Kind == HomeInteractionKind.Bed).ToArray();
                audit.nearestBedExitToSpawn = float.MaxValue; audit.nearestSpawnPair = float.MaxValue;
                for (var i = 0; i < 4; i++)
                {
                    var spawn = ContinuousTownLayout.HomeRig.Spawn(i);
                    foreach (var bed in beds)
                        audit.nearestBedExitToSpawn = Mathf.Min(audit.nearestBedExitToSpawn, FlatDistance(spawn, bed.ExitPose.position));
                    for (var j = i + 1; j < 4; j++)
                        audit.nearestSpawnPair = Mathf.Min(audit.nearestSpawnPair, FlatDistance(spawn, ContinuousTownLayout.HomeRig.Spawn(j)));
                }
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/P173-player-scale.json", JsonUtility.ToJson(audit, true));
                Debug.Log("P173_SCALE_AUDIT_WRITTEN");
            }
            finally
            {
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
