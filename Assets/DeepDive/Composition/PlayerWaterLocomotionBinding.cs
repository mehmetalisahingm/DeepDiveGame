using System.Collections.Generic;
using DeepDive.Network;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // Composition owns the seam between Utku's World water classification and Mehmet's
    // host-authoritative player mover. Running before NetworkPlayer.FixedUpdate guarantees
    // Classify -> SetEnvironmentLocomotionServer -> movement in the same simulation tick.
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class PlayerWaterLocomotionBinding : MonoBehaviour
    {
        private const float TeleportResetDistance = 2f;
        private const float TeleportResetDistanceSq = TeleportResetDistance * TeleportResetDistance;

        private sealed class PlayerBinding
        {
            public NetworkPlayer Player;
            public CharacterController Controller;
            public IWaterField Tracker;
            public Vector3 LastPosition;
        }

        private readonly Dictionary<ulong, PlayerBinding> bindings = new Dictionary<ulong, PlayerBinding>();
        private readonly HashSet<ulong> seen = new HashSet<ulong>();
        private NetworkManager manager;
        private WaterField field;
        private int sceneHandle = int.MinValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstalled()
        {
            var networkManager = NetworkManager.Singleton ?? UnityEngine.Object.FindFirstObjectByType<NetworkManager>();
            if (networkManager == null || networkManager.GetComponent<PlayerWaterLocomotionBinding>() != null) return;
            networkManager.gameObject.AddComponent<PlayerWaterLocomotionBinding>();
        }

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            if (manager == null) manager = NetworkManager.Singleton;
        }

        private void OnDisable()
        {
            ReleaseConnectedPlayers();
            bindings.Clear();
            seen.Clear();
            field = null;
            sceneHandle = int.MinValue;
        }

        private void FixedUpdate()
        {
            if (manager == null) manager = GetComponent<NetworkManager>() ?? NetworkManager.Singleton;
            if (manager == null || !manager.IsListening || !manager.IsServer)
            {
                bindings.Clear();
                seen.Clear();
                return;
            }

            RefreshField();
            if (field == null)
            {
                ReleaseConnectedPlayers();
                bindings.Clear();
                return;
            }

            seen.Clear();
            foreach (var pair in manager.ConnectedClients)
            {
                var playerObject = pair.Value.PlayerObject;
                if (playerObject == null) continue;
                var player = playerObject.GetComponent<NetworkPlayer>();
                if (player == null || !player.IsSpawned || !player.IsServer) continue;

                var key = player.NetworkObjectId;
                seen.Add(key);
                BindAndClassify(key, player);
            }

            if (bindings.Count == seen.Count) return;
            var stale = new List<ulong>();
            foreach (var key in bindings.Keys)
                if (!seen.Contains(key)) stale.Add(key);
            foreach (var key in stale) bindings.Remove(key);
        }

        private void RefreshField()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.handle != sceneHandle)
            {
                ReleaseConnectedPlayers();
                bindings.Clear();
                field = null;
                sceneHandle = activeScene.handle;
            }

            if (field != null) return;
            var next = FindFirstObjectByType<WaterField>();
            if (next == null) return;
            field = next;
            bindings.Clear();
        }

        private void BindAndClassify(ulong key, NetworkPlayer player)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller == null)
            {
                player.ReleaseEnvironmentLocomotionServer();
                bindings.Remove(key);
                return;
            }

            if (!bindings.TryGetValue(key, out var binding) || binding.Player != player ||
                binding.Controller != controller)
            {
                binding = NewBinding(player, controller);
                bindings[key] = binding;
            }
            else if ((player.transform.position - binding.LastPosition).sqrMagnitude > TeleportResetDistanceSq)
            {
                // Hysteresis memory belongs to one continuous diver trajectory. A teleport is a
                // new sample context, so do not carry the old shoreline state across it.
                binding.Tracker = field.CreateTracker();
            }

            binding.LastPosition = player.transform.position;
            var probe = BuildProbe(player.transform, controller);
            var environment = binding.Tracker.Classify(probe);
            player.SetEnvironmentLocomotionServer(environment);
        }

        private PlayerBinding NewBinding(NetworkPlayer player, CharacterController controller) =>
            new PlayerBinding
            {
                Player = player,
                Controller = controller,
                Tracker = field.CreateTracker(),
                LastPosition = player.transform.position
            };

        private void ReleaseConnectedPlayers()
        {
            if (manager == null || !manager.IsServer) return;
            foreach (var pair in manager.ConnectedClients)
            {
                var playerObject = pair.Value.PlayerObject;
                if (playerObject == null) continue;
                var player = playerObject.GetComponent<NetworkPlayer>();
                if (player != null && player.IsSpawned) player.ReleaseEnvironmentLocomotionServer();
            }
        }

        // CharacterController dimensions are local-space values. The World seam explicitly wants
        // a world-axis centre offset and world-space capsule dimensions, so Composition performs
        // that conversion rather than making World inspect the player prefab.
        public static WaterProbe BuildProbe(Transform playerTransform, CharacterController controller)
        {
            var scale = playerTransform.lossyScale;
            var radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var radius = Mathf.Max(0f, controller.radius * radiusScale);
            var height = Mathf.Max(radius * 2f, controller.height * Mathf.Abs(scale.y));
            var centerOffsetWorld = playerTransform.TransformVector(controller.center);
            var up = playerTransform.up.sqrMagnitude > 0f ? playerTransform.up.normalized : Vector3.up;
            return new WaterProbe(playerTransform.position, centerOffsetWorld, radius, height, up);
        }
    }
}
