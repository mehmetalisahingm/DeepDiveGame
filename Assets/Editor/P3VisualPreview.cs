using System.IO;
using DeepDive.Network;
using DeepDive.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Editor
{
    public static class P3VisualPreview
    {
        public static void CaptureAndBuild()
        {
            Capture();
            // Discard the preview actors and temporary camera presentation before building.
            EditorSceneManager.OpenScene(P1IntegratedBuild.Prep);
            P1IntegratedBuild.BuildWindows();
        }
        // Authoring evidence; multiplayer screenshots are produced separately by the smoke driver.
        public static void Capture()
        {
            P3RepairSetup.Apply();
            EditorSceneManager.OpenScene("Assets/DeepDive/World/Scenes/DiveTestArea.unity");
            Object.FindFirstObjectByType<CoastalAtmosphere>().SendMessage("OnEnable");
            var catalog = Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);
            var root = new GameObject("PreviewPlayer");
            root.transform.position = new Vector3(-2, 8.4f, -12.6f);
            var eye = new GameObject("PreviewEye"); eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(0, 1.55f, 0);
            var ownerCam = eye.AddComponent<Camera>(); ownerCam.enabled = false;
            var view = root.AddComponent<PlayerPresentationView>();
            var pose = new PlayerPresentationState(LocomotionMode.Land, HeldEquipmentMode.Camera, true);
            view.Apply(false, pose, 0);
            Sample(root, pose);
            Take("Logs/p3-human-preview.png", root.transform.position + new Vector3(2.4f, 1.7f, 3.0f), root.transform.position + Vector3.up);
            view.Apply(true, pose, 0); Sample(root, pose);
            Take("Logs/p3-hands-preview.png", eye.transform.position, eye.transform.position + Vector3.forward * 6);
            view.Apply(false, new PlayerPresentationState(LocomotionMode.Underwater, HeldEquipmentMode.None, false), 0.8f);
            root.transform.position = new Vector3(0, 3, 0);
            Sample(root, new PlayerPresentationState(LocomotionMode.Underwater, HeldEquipmentMode.None, false));
            Take("Logs/p3-swim-preview.png", new Vector3(3.6f, 4.6f, 3.6f), new Vector3(0, 3.9f, 0));
            Object.DestroyImmediate(root);
            Debug.Log("P3_VISUAL_PREVIEW_COMPLETE");
        }
        private static void Sample(GameObject root, PlayerPresentationState state)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>()) { animator.Rebind(); animator.Update(0.2f); }
            foreach (var pose in root.GetComponentsInChildren<CoastalDiverPose>())
            { pose.SendMessage("Awake"); pose.Present(state, 0.8f); pose.SendMessage("LateUpdate"); }
        }
        private static void Take(string path, Vector3 position, Vector3 target)
        {
            var go = new GameObject("PreviewCapture"); go.transform.position = position; go.transform.LookAt(target);
            var camera = go.AddComponent<Camera>(); camera.nearClipPlane = 0.04f; camera.farClipPlane = 120;
            var atmosphere = Object.FindFirstObjectByType<CoastalAtmosphere>();
            atmosphere.ApplyTo(camera);
            var rt = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                rt.Create(); camera.targetTexture = rt;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = rt });
                RenderTexture.active = rt; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; rt.Release();
                Object.DestroyImmediate(pixels); Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
            }
        }
    }
}
