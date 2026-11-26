using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #297 option A: the beach platform's north edge drops onto the dock ramp (Shore_Ramp) as a wall that is 0.93 m at x 5
    // and 3.2 m at x 0 (P297ShoreDockAudit), so a diver walking off the beach towards the dock could only join the ramp
    // at its top. A side apron along the seam (x 4.8 .. 7) slopes down north from the platform's edge (8.4) and meets the
    // ramp's own surface, so the ramp can be joined from the beach along its upper third. The ramp, the platform and the
    // ledge keep their real colliders; nothing on the dock, the cradle or the boats moves. Built with the town on every
    // process from the scene's own ramp, so DiveTestArea.unity is not edited.
    public static class ShoreRampApron
    {
        public const string Name = "Shore_RampApron";
        public const float MinX = 4.8f, MaxX = 7.0f, SeamZ = -11f, PlatformTopY = 8.4f, Thickness = 0.4f;
        // Measured on the scene (#297): the ramp's top surface reaches PlatformTopY at x 7 and falls 25 degrees westwards.
        public const float RampTopX = 7f, RampAngleDegrees = 25f;

        public static float RampSurfaceY(float x) => PlatformTopY - Mathf.Tan(RampAngleDegrees * Mathf.Deg2Rad) * (RampTopX - x);

        // How far north the apron runs: until it is level with the ramp at its west end, at no more than SlopeLimit.
        public const float SlopeDegrees = 34.5f;
        public static float Run => (PlatformTopY - RampSurfaceY(MinX)) / Mathf.Tan(SlopeDegrees * Mathf.Deg2Rad);

        public static void Build(Scene scene)
        {
            if (!scene.IsValid()) return;
            foreach (var existing in scene.GetRootGameObjects())
                if (existing.name == Name) return;
            Material material = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Shore_Ramp" && root.TryGetComponent<Renderer>(out var ramp)) material = ramp.sharedMaterial;
            var tilt = Quaternion.Euler(SlopeDegrees, 0f, 0f);   // top face falls towards +z (north)
            var up = tilt * Vector3.up;
            var length = Run / Mathf.Cos(SlopeDegrees * Mathf.Deg2Rad);
            var topMid = new Vector3((MinX + MaxX) / 2f, PlatformTopY - (PlatformTopY - RampSurfaceY(MinX)) / 2f, SeamZ + Run / 2f);
            var apron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apron.name = Name;
            SceneManager.MoveGameObjectToScene(apron, scene);
            apron.transform.SetPositionAndRotation(topMid - up * (Thickness / 2f), tilt);
            apron.transform.localScale = new Vector3(MaxX - MinX, Thickness, length + 0.05f);
            if (material != null) apron.GetComponent<Renderer>().sharedMaterial = material;
            Debug.Log($"P297_RAMP_APRON_READY run={Run:0.00} slope={SlopeDegrees}");
        }
    }
}
