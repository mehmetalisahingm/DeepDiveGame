using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #303: a small wreck on the shallow sand in front of the beach, the same on every process. Mert's approved damaged
    // rowboat lies keeled over and half buried, with its anchor, a chest and a barrel spilled round it from the shore
    // kit. The pieces are decor without colliders; the hull gets SeabedProfile's one low box, so a diver bumps into
    // what the host's fish bounds steer around. No rewards, no network objects, nothing to pick up.
    public static class ShallowWreckVisuals
    {
        public const string RootName = "P303_ShallowWreck";
        public const string HullName = "Wreck_Hull";
        public const string SolidPrefix = "WreckSolid_";

        // How far the hull settles into the sand, and how far it lists.
        public const float HullSink = 0.45f, HullYaw = 90f, HullRoll = 12f;
        public const float BedOverlap = 0.15f;

        public readonly struct Prop
        {
            public readonly string Id;
            public readonly float X, Z, Yaw, Tilt;

            public Prop(string id, float x, float z, float yaw, float tilt = 0f)
            {
                Id = id;
                X = x;
                Z = z;
                Yaw = yaw;
                Tilt = tilt;
            }

            public Vector3 Position => new Vector3(X, SeabedProfile.FloorHeight(X, Z) - BedOverlap, Z);
        }

        public static readonly Prop[] Props =
        {
            // The anchor line runs out from the bow, the cargo fell out over the low side.
            new Prop("shore-prop-anchor", SeabedProfile.WreckX - 3.6f, SeabedProfile.WreckZ + 1.6f, 30f, 20f),
            new Prop("shore-prop-chest", SeabedProfile.WreckX + 1.4f, SeabedProfile.WreckZ - 1.9f, 200f, 15f),
            new Prop("shore-prop-barrel", SeabedProfile.WreckX - 1.2f, SeabedProfile.WreckZ - 2.1f, 75f, 80f)
        };

        public static Vector3 HullPosition =>
            new Vector3(SeabedProfile.WreckX, SeabedProfile.FloorHeight(SeabedProfile.WreckX, SeabedProfile.WreckZ) - HullSink, SeabedProfile.WreckZ);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode) => Build(scene);

        public static void Build(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != ContinuousTownLayout.SceneName) return;
            foreach (var existing in scene.GetRootGameObjects())
                if (existing.name == RootName) return;

            // Resolve and validate the whole set before creating any of it.
            var hull = Resources.Load<GameObject>(BoatRepairSetArt.DamagedHull);
            if (!Safe(hull, BoatRepairSetArt.DamagedHull)) return;
            var props = new GameObject[Props.Length];
            for (var i = 0; i < Props.Length; i++)
            {
                props[i] = ShoreKitPiece.Load(Props[i].Id);
                if (!Safe(props[i], Props[i].Id)) return;
            }

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            var visual = Object.Instantiate(hull, root.transform, false);
            visual.name = HullName;
            visual.transform.SetPositionAndRotation(HullPosition, Quaternion.Euler(0f, HullYaw, HullRoll));
            for (var i = 0; i < Props.Length; i++)
            {
                var prop = Object.Instantiate(props[i], root.transform, false);
                prop.name = "WreckProp_" + i + "_" + Props[i].Id;
                prop.transform.SetPositionAndRotation(Props[i].Position, Quaternion.Euler(Props[i].Tilt, Props[i].Yaw, 0f));
            }

            for (var i = 0; i < SeabedProfile.WreckSolids.Count; i++)
            {
                var box = SeabedProfile.WreckSolids[i].Box;
                var solid = new GameObject(SolidPrefix + i) { layer = 0 };
                solid.transform.SetParent(root.transform, false);
                solid.transform.position = box.center;
                solid.AddComponent<BoxCollider>().size = box.size;
            }

            Debug.Log("P303_SHALLOW_WRECK_READY props=" + Props.Length + " solids=" + SeabedProfile.WreckSolids.Count);
        }

        private static bool Safe(GameObject prefab, string id)
        {
            if (prefab != null && prefab.GetComponentInChildren<Collider>(true) == null &&
                prefab.GetComponentInChildren<NetworkObject>(true) == null) return true;
            Debug.LogError("P303_SHALLOW_WRECK_INVALID_PREFAB id=" + id);
            return false;
        }
    }
}
