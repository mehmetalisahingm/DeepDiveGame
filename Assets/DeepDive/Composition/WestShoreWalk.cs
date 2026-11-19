using DeepDive.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #302: a rocky walk off the west end of the beach, away from the dock and the repair cradle. Through an opening in
    // the beach's west fence a landing (level with the beach, 8.4) leads to a gentle ramp running north outside the
    // arena wall, up to a viewing terrace 1 m higher, west of the wade, looking out over the dive area. Railed on every
    // side; shore-kit rocks below make it read as a headland. A warm lantern on the terrace follows the shared day like
    // the town lamps. The same on every process; no network objects, services or rewards.
    public static class WestShoreWalk
    {
        public const string RootName = "P302_WestShoreWalk";
        public const float BeachY = ContinuousTownLayout.WalkSurfaceY, TerraceY = BeachY + 1f;
        public const float Thickness = 0.6f, RailHeight = 1.1f, RailThickness = 0.25f;

        // Opening in the beach's west fence (z), the landing, the ramp and the terrace (x/z ranges in world metres).
        public const float OpeningMinZ = -14.5f, OpeningMaxZ = -12f;
        public const float WalkMinX = -18f, WalkMaxX = -15.05f, BeachWestX = -14.75f;
        public const float RampMinZ = -12f, RampMaxZ = -4f;
        public const float TerraceMinX = -20f, TerraceMinZ = -4f, TerraceMaxZ = 2f;

        public static readonly Vector3 LandingPoint = new Vector3(-16.5f, BeachY, -13.25f);
        public static readonly Vector3 RampFoot = new Vector3(-16.5f, BeachY, RampMinZ + 0.6f);
        public static readonly Vector3 RampTop = new Vector3(-16.5f, TerraceY, RampMaxZ - 0.6f);
        public static readonly Vector3 TerraceMiddle = new Vector3(-17.5f, TerraceY, -1f);
        public static readonly Vector3 ViewPoint = new Vector3(-15.9f, TerraceY, 0.5f);

        public static float RampSlopeDegrees => Mathf.Atan2(TerraceY - BeachY, RampMaxZ - RampMinZ) * Mathf.Rad2Deg;

        // Built by ContinuousTownLayout.Build, right after the town whose beach fence it opens.
        public static void Build(Scene scene)
        {
            if (!scene.IsValid() || scene.name != ContinuousTownLayout.SceneName) return;
            foreach (var existing in scene.GetRootGameObjects())
                if (existing.name == RootName) return;
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Material stone = null, wood = null;
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name == "Beach_Platform" && r.TryGetComponent<Renderer>(out var sand)) stone = sand.sharedMaterial;
                if (r.name == "Wall_S" && r.TryGetComponent<Renderer>(out var wall)) wood = wall.sharedMaterial;
            }
            var t = root.transform;

            // Floor: landing at beach level (it reaches over the fence line onto the platform's edge), the ramp, the terrace.
            Box(t, "WestWalk_Landing", new Vector3((WalkMinX + BeachWestX) / 2f, BeachY - Thickness / 2f, (OpeningMinZ + OpeningMaxZ) / 2f),
                new Vector3(BeachWestX - WalkMinX, Thickness, OpeningMaxZ - OpeningMinZ), stone);
            var rise = TerraceY - BeachY;
            var run = RampMaxZ - RampMinZ;
            var length = Mathf.Sqrt(rise * rise + run * run);
            var tilt = Quaternion.Euler(-RampSlopeDegrees, 0f, 0f);
            var up = tilt * Vector3.up;
            var topMid = new Vector3((WalkMinX + WalkMaxX) / 2f, (BeachY + TerraceY) / 2f, (RampMinZ + RampMaxZ) / 2f);
            var ramp = Box(t, "WestWalk_Ramp", topMid - up * (Thickness / 2f), new Vector3(WalkMaxX - WalkMinX, Thickness, length + 0.3f), stone);
            ramp.transform.rotation = tilt;
            Box(t, "WestWalk_Terrace", new Vector3((TerraceMinX + WalkMaxX) / 2f, TerraceY - Thickness / 2f, (TerraceMinZ + TerraceMaxZ) / 2f),
                new Vector3(WalkMaxX - TerraceMinX, Thickness, TerraceMaxZ - TerraceMinZ), stone);

            // Rails. Landing: its open south and north sides west of the fence line. Ramp: both sides, following the slope.
            // Terrace: all round except where the ramp arrives.
            Rail(t, "WestWalk_RailLandingSouth", WalkMinX, BeachWestX - 0.3f, OpeningMinZ - RailThickness, OpeningMinZ, BeachY, wood);
            Rail(t, "WestWalk_RailLandingWest", WalkMinX - RailThickness, WalkMinX, OpeningMinZ - RailThickness, RampMinZ, BeachY, wood);
            foreach (var (name, x) in new[] { ("WestWalk_RailRampWest", WalkMinX - RailThickness / 2f), ("WestWalk_RailRampEast", WalkMaxX + RailThickness / 2f) })
            {
                var rail = Box(t, name, new Vector3(x, topMid.y, topMid.z) + up * (RailHeight / 2f),
                    new Vector3(RailThickness, RailHeight, length + 0.3f), wood);
                rail.transform.rotation = tilt;
            }
            Rail(t, "WestWalk_RailTerraceSouthWest", TerraceMinX, WalkMinX - RailThickness, TerraceMinZ - RailThickness, TerraceMinZ, TerraceY, wood);
            Rail(t, "WestWalk_RailTerraceWest", TerraceMinX - RailThickness, TerraceMinX, TerraceMinZ - RailThickness, TerraceMaxZ + RailThickness, TerraceY, wood);
            Rail(t, "WestWalk_RailTerraceNorth", TerraceMinX - RailThickness, WalkMaxX + RailThickness, TerraceMaxZ, TerraceMaxZ + RailThickness, TerraceY, wood);
            Rail(t, "WestWalk_RailTerraceEast", WalkMaxX, WalkMaxX + RailThickness, TerraceMinZ, TerraceMaxZ + RailThickness, TerraceY, wood);

            // Collider-free dressing: the headland's rocks, a bench facing the sea and a lantern.
            var dressing = new GameObject("WestWalk_Dressing").transform;
            dressing.SetParent(t, false);
            foreach (var (id, pos, yaw, scale) in new[]
                     {
                         ("shore-rock-1", new Vector3(-19.2f, BeachY - 1.6f, -13.6f), 20f, 2.6f),
                         ("shore-rock-3", new Vector3(-19.4f, BeachY - 1.2f, -8.5f), 110f, 2.8f),
                         ("shore-rock-5", new Vector3(-20.6f, TerraceY - 1.8f, -2.5f), 200f, 3.2f),
                         ("shore-rock-1", new Vector3(-20.8f, TerraceY - 1.6f, 1.6f), 290f, 2.4f),
                         ("shore-rock-3", new Vector3(-17.0f, TerraceY - 2.4f, 2.8f), 60f, 2.6f),
                         ("shore-palm-2", new Vector3(-19.3f, TerraceY, 1.3f), 30f, 1f)
                     })
            {
                var prefab = ShoreKitPiece.Load(id);
                if (prefab == null) { Debug.LogWarning("P302_WEST_WALK_MISSING_PIECE " + id); continue; }
                var piece = Object.Instantiate(prefab, dressing, false);
                piece.name = "WestWalk_" + id;
                piece.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                piece.transform.localScale = Vector3.one * scale;
                foreach (var c in piece.GetComponentsInChildren<Collider>(true)) Discard(c);
            }
            Decor(dressing, "WestWalk_BenchSeat", new Vector3(-18.6f, TerraceY + 0.45f, -0.6f), new Vector3(0.5f, 0.08f, 1.8f), wood);
            Decor(dressing, "WestWalk_BenchBack", new Vector3(-18.85f, TerraceY + 0.8f, -0.6f), new Vector3(0.06f, 0.45f, 1.8f), wood);
            Decor(dressing, "WestWalk_LanternPost", new Vector3(-19.6f, TerraceY + 1.3f, -3.5f), new Vector3(0.14f, 2.6f, 0.14f), wood);
            var lantern = new GameObject("WestWalk_Lantern").AddComponent<Light>();
            lantern.transform.SetParent(dressing, false);
            lantern.transform.position = new Vector3(-19.6f, TerraceY + 2.7f, -3.5f);
            lantern.type = LightType.Point;
            lantern.color = new Color(1f, 0.85f, 0.6f);
            lantern.range = 7f;
            lantern.shadows = LightShadows.None;
            lantern.enabled = false;
            root.AddComponent<LanternDriver>().Lantern = lantern;
            Debug.Log("P302_WEST_WALK_READY slope=" + RampSlopeDegrees.ToString("0.0"));
        }

        public static float LanternIntensity(float night01) => 2f * Mathf.Clamp01((night01 - 0.2f) / 0.5f);

        // Also called by edit-mode audits and tests, where Destroy is not allowed.
        private static void Discard(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        private static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static void Rail(Transform parent, string name, float minX, float maxX, float minZ, float maxZ, float floorY, Material material) =>
            Box(parent, name, new Vector3((minX + maxX) / 2f, floorY + RailHeight / 2f, (minZ + maxZ) / 2f),
                new Vector3(maxX - minX, RailHeight, maxZ - minZ), material);

        private static void Decor(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            var go = Box(parent, name, center, size, material);
            Discard(go.GetComponent<Collider>());
        }

        private sealed class LanternDriver : MonoBehaviour
        {
            public Light Lantern;
            private float next, current = -1f;

            private void Update()
            {
                if (Lantern == null || Time.unscaledTime < next) return;
                next = Time.unscaledTime + 0.5f;
                var intensity = LanternIntensity(SharedDaylight.Night());
                if (Mathf.Abs(intensity - current) < 0.01f) return;
                current = intensity;
                Lantern.intensity = intensity;
                Lantern.enabled = intensity > 0.02f;
            }
        }
    }
}
