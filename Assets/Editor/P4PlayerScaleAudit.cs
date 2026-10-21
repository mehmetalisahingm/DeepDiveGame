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
            public EquipmentFrame[] equipmentFrames;
            public KitBounds[] suppliedKit;
        }

        [Serializable] private sealed class EquipmentFrame
        {
            public string equipment, tier, presentationRoot;
            public bool owner, crossesNearPlane, nearPlaneBoundsTouchViewport, projectedBoundsContainReticle;
            public int visibleRenderers;
            public float nearestDepth, farthestDepth;
            public Vector2 viewportMin, viewportMax;
        }

        [Serializable] private sealed class KitBounds
        {
            public string resource;
            public Vector3 centre, size;
            public int renderers, colliders;
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
                audit.equipmentFrames = MeasureEquipment(prefab, scene);
                audit.suppliedKit = new[] { "Rowboat", "ShopBuilding", "TownBuilding", "Bed", "Storage" }
                    .Select(name => MeasureKit(name, scene)).ToArray();
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

        // Exercise the existing presentation binding in a disposable scene. This is a
        // static bounds audit; animation/contact, comfort and visual acceptance still
        // need the real player and observer cameras in a running Windows build.
        private static EquipmentFrame[] MeasureEquipment(GameObject prefab, Scene scene)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var observerObject = new GameObject("P173_DisposableObserver");
            SceneManager.MoveGameObjectToScene(observerObject, scene);
            try
            {
                var camera = instance.GetComponentInChildren<Camera>(true);
                camera.aspect = 16f / 9f;
                var observer = observerObject.AddComponent<Camera>();
                observer.enabled = false;
                observer.fieldOfView = camera.fieldOfView; observer.aspect = camera.aspect;
                observer.nearClipPlane = camera.nearClipPlane;
                observer.transform.position = instance.transform.position + new Vector3(0f, 1.55f, 3f);
                observer.transform.LookAt(instance.transform.position + Vector3.up * 1.2f);
                var view = instance.GetComponent<PlayerPresentationView>() ?? instance.AddComponent<PlayerPresentationView>();
                var frames = new System.Collections.Generic.List<EquipmentFrame>();
                foreach (var owner in new[] { true, false })
                foreach (var held in new[] { HeldEquipmentMode.Harpoon, HeldEquipmentMode.Camera })
                foreach (var tier in held == HeldEquipmentMode.Camera
                    ? new[] { CameraTier.Basic, CameraTier.Advanced, CameraTier.Professional }
                    : new[] { CameraTier.Basic })
                {
                    view.Apply(owner, new PlayerPresentationState(LocomotionMode.Land, held, false), 0f, tier);
                    var suffix = held == HeldEquipmentMode.Harpoon ? "Harpoon" : "Camera";
                    var prefix = owner ? "FirstPerson" : "ThirdPerson";
                    var roots = instance.GetComponentsInChildren<Transform>(true)
                        .Where(t => t.name.Contains(prefix + suffix) && t.gameObject.activeInHierarchy).ToArray();
                    var renderers = roots.SelectMany(t => t.GetComponentsInChildren<Renderer>())
                        .Where(r => r.enabled).Distinct().ToArray();
                    var frame = new EquipmentFrame { owner = owner, equipment = held.ToString(), tier = tier.ToString(),
                        presentationRoot = string.Join(",", roots.Select(t => t.name)), visibleRenderers = renderers.Length,
                        nearestDepth = float.MaxValue, farthestDepth = float.MinValue,
                        viewportMin = Vector2.one * float.MaxValue, viewportMax = Vector2.one * float.MinValue };
                    var projection = owner ? camera : observer;
                    foreach (var renderer in renderers)
                    {
                        var bounds = renderer.bounds;
                        for (var corner = 0; corner < 8; corner++)
                        {
                            var world = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                                (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                            var p = projection.WorldToViewportPoint(world);
                            frame.nearestDepth = Mathf.Min(frame.nearestDepth, p.z);
                            frame.farthestDepth = Mathf.Max(frame.farthestDepth, p.z);
                            frame.viewportMin = Vector2.Min(frame.viewportMin, new Vector2(p.x, p.y));
                            frame.viewportMax = Vector2.Max(frame.viewportMax, new Vector2(p.x, p.y));
                        }
                        // A weapon tail behind the near plane but outside the screen
                        // is not evidence of visible clipping. Intersect its bounds
                        // with the whole frustum before reporting a visible risk.
                        frame.nearPlaneBoundsTouchViewport |= GeometryUtility.TestPlanesAABB(
                            GeometryUtility.CalculateFrustumPlanes(projection), bounds) &&
                            BoundsCrossNearPlane(projection, bounds);
                    }
                    if (renderers.Length == 0)
                    {
                        frame.nearestDepth = frame.farthestDepth = 0;
                        frame.viewportMin = frame.viewportMax = Vector2.zero;
                    }
                    else
                    {
                        frame.crossesNearPlane = frame.nearestDepth <= projection.nearClipPlane;
                        // Conservative projected AABB only, not a claim of pixel occlusion.
                        frame.projectedBoundsContainReticle = frame.farthestDepth > 0 &&
                            frame.viewportMin.x <= .5f && frame.viewportMax.x >= .5f &&
                            frame.viewportMin.y <= .5f && frame.viewportMax.y >= .5f;
                    }
                    frames.Add(frame);
                }
                return frames.ToArray();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(observerObject);
            }
        }

        private static bool BoundsCrossNearPlane(Camera camera, Bounds bounds)
        {
            var near = camera.nearClipPlane;
            var centre = camera.transform.InverseTransformPoint(bounds.center).z;
            var f = camera.transform.forward;
            var extent = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(f.x), Mathf.Abs(f.y), Mathf.Abs(f.z)));
            if (centre - extent > near || centre + extent < near) return false;
            // Conservative AABB slice at the near plane. The current owner view is
            // axis aligned in this static audit; animated/rotated contact needs video.
            var closest = bounds.ClosestPoint(camera.transform.position + f * near);
            var p = camera.WorldToViewportPoint(closest);
            return p.z > 0f && p.x >= 0f && p.x <= 1f && p.y >= 0f && p.y <= 1f;
        }

        private static KitBounds MeasureKit(string name, Scene scene)
        {
            var resource = "SuppliedKit/" + name;
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) throw new InvalidOperationException("Missing current supplied kit: " + resource);
            var instance = UnityEngine.Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(instance, scene);
            try
            {
                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new InvalidOperationException("No visual geometry: " + resource);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                return new KitBounds { resource = resource, centre = bounds.center, size = bounds.size,
                    renderers = renderers.Length, colliders = instance.GetComponentsInChildren<Collider>(true).Length };
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
