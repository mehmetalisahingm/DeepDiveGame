using DeepDive.Core.Contracts;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Network
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(CharacterController))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float swimSpeed = 3f;

        [Header("P2 Diver")]
        [SerializeField] private float maxOxygen = 120f;
        [SerializeField] private float oxygenDrainPerSecond = 1f;
        [SerializeField, Range(0.05f, 0.9f)] private float lowOxygenFraction = 0.2f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float harpoonRange = 22f;
        [SerializeField] private float harpoonDamage = 1f;
        [SerializeField] private float harpoonCooldown = 0.65f;
        [SerializeField] private float pickupRange = 2.5f;
        [SerializeField] private float pickupCooldown = 0.2f;
        [SerializeField] private AudioSource lowOxygenLoop;

        [Header("View")]
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Renderer bodyRenderer;

        public readonly NetworkVariable<bool> Swimming = new NetworkVariable<bool>();
        public readonly NetworkVariable<float> Oxygen = new NetworkVariable<float>();
        public readonly NetworkVariable<float> Health = new NetworkVariable<float>();
        public readonly NetworkVariable<bool> Passive = new NetworkVariable<bool>();
        public readonly NetworkVariable<ulong> LastActionRequestId = new NetworkVariable<ulong>();
        public readonly NetworkVariable<int> LastActionResult = new NetworkVariable<int>();

        public bool ReadKeyboard { get; set; } = true;

        private readonly ServerInputBuffer input = new ServerInputBuffer();
        private readonly ServerActionGate actionGate = new ServerActionGate();
        private CharacterController controller;
        private NetworkTransform networkTransform;
        private NetworkSession session;
        private DiverVitalsState vitals;
        private Material bodyMaterial;
        private uint sequence;
        private ulong actionSequence;
        private ulong observedActionRequestId;
        private float actionMessageUntil;
        private string actionMessage = "";
        private float yaw;
        private float pitch;
        private float gravityVelocity;
        private double nextInputTime;

        public override void OnNetworkSpawn()
        {
            controller = GetComponent<CharacterController>();
            networkTransform = GetComponent<NetworkTransform>();
            session = NetworkManager.GetComponent<NetworkSession>();
            vitals = new DiverVitalsState(maxOxygen, maxHealth, oxygenDrainPerSecond, lowOxygenFraction);
            controller.enabled = IsServer;
            NetworkObject.DestroyWithScene = false;
            yaw = transform.eulerAngles.y;

            if (IsServer)
            {
                actionGate.Reset();
                PublishVitals();
                LastActionRequestId.Value = 0;
                LastActionResult.Value = (int)PlayerActionResult.Accepted;
            }

            if (viewCamera != null)
            {
                viewCamera.enabled = IsOwner;
                var listener = viewCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = IsOwner;
            }
            if (lowOxygenLoop != null)
            {
                lowOxygenLoop.loop = true;
                if (!IsOwner && lowOxygenLoop.isPlaying) lowOxygenLoop.Stop();
            }
            if (bodyRenderer != null)
            {
                bodyMaterial = bodyRenderer.material;
                bodyMaterial.color = Color.HSVToRGB((OwnerClientId * 0.23f) % 1f, 0.65f, 0.95f);
                if (IsOwner) bodyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            session.Register(this);
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            UpdateLowOxygenFeedback();
            ObserveActionFeedback();
            if (!ReadKeyboard) return;

            if (Input.GetKeyDown(KeyCode.F1)) SetLookCaptured(Cursor.lockState != CursorLockMode.Locked);
            if (Input.GetKeyDown(KeyCode.Escape)) SetLookCaptured(false);
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxisRaw("Mouse X") * 2f;
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 2f, -85f, 85f);
                if (Application.isFocused && !Passive.Value)
                {
                    if (Input.GetMouseButtonDown(0)) SubmitHarpoonLocal();
                    if (Input.GetKeyDown(KeyCode.E)) SubmitPickupLocal();
                }
            }

            if (Time.unscaledTimeAsDouble < nextInputTime) return;
            nextInputTime = Time.unscaledTimeAsDouble + 1.0 / 30;
            var move = Vector3.zero;
            if (!Passive.Value && Cursor.lockState == CursorLockMode.Locked && Application.isFocused)
            {
                move.x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
                move.z = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
                move.y = (Input.GetKey(KeyCode.Space) ? 1 : 0) - (Input.GetKey(KeyCode.LeftControl) ? 1 : 0);
            }
            SubmitLocalInput(move, yaw, pitch);
        }

        public void SetLookCaptured(bool captured)
        {
            if (!IsOwner) return;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        // Kept for P1 callers/tests; P2 adds pitch so host can authoritatively raycast harpoon/pickup.
        public void SubmitLocalInput(Vector3 move, float lookYaw) => SubmitLocalInput(move, lookYaw, pitch);

        public void SubmitLocalInput(Vector3 move, float lookYaw, float lookPitch)
        {
            if (!IsSpawned || !IsOwner || !NetworkManager.IsConnectedClient) return;
            var frame = new PlayerInputFrame { Sequence = ++sequence, Move = move, Yaw = lookYaw, Pitch = lookPitch };
            if (IsServer) input.Accept(frame, Time.realtimeSinceStartupAsDouble);
            else MoveRpc(frame);
        }

        public void SubmitHarpoonLocal()
        {
            if (!IsSpawned || !IsOwner || !NetworkManager.IsConnectedClient) return;
            var requestId = ++actionSequence;
            if (IsServer) HandleHarpoon(requestId);
            else HarpoonRpc(requestId);
        }

        public void SubmitPickupLocal()
        {
            if (!IsSpawned || !IsOwner || !NetworkManager.IsConnectedClient) return;
            var requestId = ++actionSequence;
            if (IsServer) HandlePickup(requestId);
            else PickupRpc(requestId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        private void MoveRpc(PlayerInputFrame frame) => input.Accept(frame, Time.realtimeSinceStartupAsDouble);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void HarpoonRpc(ulong requestId) => HandleHarpoon(requestId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void PickupRpc(ulong requestId) => HandlePickup(requestId);

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || session == null || session.IsSceneLoading) return;
            var now = Time.realtimeSinceStartupAsDouble;
            var frame = input.Read(now);
            transform.rotation = Quaternion.Euler(0, frame.Yaw, 0);
            Swimming.Value = SwimVolume.Contains(transform.position + Vector3.up * 0.9f);

            vitals.Tick(Time.fixedDeltaTime, Swimming.Value);
            PublishVitals();

            var move = Passive.Value ? Vector3.zero : frame.Move;
            if (!Swimming.Value) move.y = 0;
            move = Quaternion.Euler(0, frame.Yaw, 0) * Vector3.ClampMagnitude(move, 1f);
            var velocity = move * (Swimming.Value ? swimSpeed : walkSpeed);
            if (Swimming.Value) gravityVelocity = 0;
            else
            {
                gravityVelocity = controller.isGrounded ? -2f : Mathf.Max(gravityVelocity - 9.81f * Time.fixedDeltaTime, -20f);
                velocity.y = gravityVelocity;
            }
            controller.Move(velocity * Time.fixedDeltaTime);
        }

        private void HandleHarpoon(ulong requestId)
        {
            if (!IsServer) return;
            var result = actionGate.TryAccept(requestId, PlayerActionKind.Harpoon,
                Time.realtimeSinceStartupAsDouble, harpoonCooldown);
            if (result != PlayerActionResult.Accepted)
            {
                PublishAction(requestId, result);
                return;
            }
            if (Passive.Value || !Swimming.Value)
            {
                PublishAction(requestId, PlayerActionResult.InvalidState);
                return;
            }

            var frame = input.Read(Time.realtimeSinceStartupAsDouble);
            var origin = transform.position + Vector3.up * 1.35f;
            var direction = Quaternion.Euler(frame.Pitch, frame.Yaw, 0f) * Vector3.forward;
            if (!Physics.Raycast(origin, direction, out var hit, Mathf.Max(0.1f, harpoonRange), ~0, QueryTriggerInteraction.Ignore))
            {
                PublishAction(requestId, PlayerActionResult.InvalidTarget);
                return;
            }

            var target = FindTarget<IHarpoonTarget>(hit.collider);
            result = target == null
                ? PlayerActionResult.InvalidTarget
                : target.TryApplyHarpoonHit(new HarpoonHit(new PlayerId(OwnerClientId), requestId, Mathf.Max(0f, harpoonDamage)));
            PublishAction(requestId, result);
            Debug.DrawRay(origin, direction * hit.distance, result == PlayerActionResult.Accepted ? Color.green : Color.yellow, 0.4f);
        }

        private void HandlePickup(ulong requestId)
        {
            if (!IsServer) return;
            var result = actionGate.TryAccept(requestId, PlayerActionKind.Pickup,
                Time.realtimeSinceStartupAsDouble, pickupCooldown);
            if (result != PlayerActionResult.Accepted)
            {
                PublishAction(requestId, result);
                return;
            }
            if (Passive.Value)
            {
                PublishAction(requestId, PlayerActionResult.InvalidState);
                return;
            }

            var frame = input.Read(Time.realtimeSinceStartupAsDouble);
            var origin = transform.position + Vector3.up * 1.35f;
            var direction = Quaternion.Euler(frame.Pitch, frame.Yaw, 0f) * Vector3.forward;
            if (!Physics.Raycast(origin, direction, out var hit, Mathf.Max(0.1f, pickupRange), ~0, QueryTriggerInteraction.Ignore))
            {
                PublishAction(requestId, PlayerActionResult.InvalidTarget);
                return;
            }

            var target = FindTarget<ICatchPickupTarget>(hit.collider);
            result = target == null ? PlayerActionResult.InvalidTarget : target.TryPickup(new PlayerId(OwnerClientId), requestId);
            PublishAction(requestId, result);
        }

        private static T FindTarget<T>(Collider collider) where T : class
        {
            if (collider == null) return null;
            var behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
                if (behaviour is T target) return target;
            return null;
        }

        private void PublishAction(ulong requestId, PlayerActionResult result)
        {
            if (requestId < LastActionRequestId.Value) return;
            LastActionResult.Value = (int)result;
            LastActionRequestId.Value = requestId;
        }

        public bool ApplyDamageServer(float amount)
        {
            if (!IsServer || vitals == null) return false;
            var changed = vitals.ApplyDamage(amount);
            PublishVitals();
            return changed;
        }

        public void ResetForDiveServer()
        {
            if (!IsServer || vitals == null) return;
            vitals.Reset();
            actionGate.Reset();
            PublishVitals();
        }

        private void PublishVitals()
        {
            if (!IsServer || vitals == null) return;
            Oxygen.Value = vitals.Oxygen;
            Health.Value = vitals.Health;
            Passive.Value = vitals.Passive;
            if (Passive.Value) input.ClearMotion();
        }

        private void UpdateLowOxygenFeedback()
        {
            if (!IsOwner || lowOxygenLoop == null || lowOxygenLoop.clip == null) return;
            var warning = !Passive.Value && Oxygen.Value > 0f && Oxygen.Value <= Mathf.Max(1f, maxOxygen) * lowOxygenFraction;
            if (warning && !lowOxygenLoop.isPlaying) lowOxygenLoop.Play();
            else if (!warning && lowOxygenLoop.isPlaying) lowOxygenLoop.Stop();
        }

        private void ObserveActionFeedback()
        {
            if (LastActionRequestId.Value == 0 || LastActionRequestId.Value == observedActionRequestId) return;
            observedActionRequestId = LastActionRequestId.Value;
            actionMessage = ((PlayerActionResult)LastActionResult.Value).ToString();
            actionMessageUntil = Time.unscaledTime + 0.9f;
        }

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner) return;
            var oxygenRatio = Mathf.Clamp01(Oxygen.Value / Mathf.Max(1f, maxOxygen));
            var healthRatio = Mathf.Clamp01(Health.Value / Mathf.Max(1f, maxHealth));
            GUI.Box(new Rect(20, 20, 230, 24), $"O2 {Oxygen.Value:0}/{maxOxygen:0} ({oxygenRatio * 100f:0}%)");
            GUI.Box(new Rect(20, 48, 230, 24), $"HEALTH {Health.Value:0}/{maxHealth:0} ({healthRatio * 100f:0}%)");
            GUI.Label(new Rect(Screen.width * 0.5f - 8, Screen.height * 0.5f - 12, 30, 30), "+");

            if (!Passive.Value && Oxygen.Value > 0f && Oxygen.Value <= Mathf.Max(1f, maxOxygen) * lowOxygenFraction)
                GUI.Box(new Rect(20, 78, 230, 28), "LOW OXYGEN - RETURN");
            if (Passive.Value)
                GUI.Box(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f + 45, 300, 40), "PASSIVE - DIVE ENDED FOR YOU");
            if (Time.unscaledTime < actionMessageUntil)
                GUI.Box(new Rect(Screen.width * 0.5f - 90, Screen.height * 0.5f + 90, 180, 28), actionMessage);
        }

        private void LateUpdate()
        {
            if (IsSpawned && IsOwner && viewCamera != null)
                viewCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }

        public void Teleport(Pose pose)
        {
            if (!IsServer) return;
            input.ClearMotion(); gravityVelocity = 0;
            controller.enabled = false;
            networkTransform.Teleport(pose.position, pose.rotation, Vector3.one);
            controller.enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            if (session != null) session.Unregister(this);
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (lowOxygenLoop != null && lowOxygenLoop.isPlaying) lowOxygenLoop.Stop();
            }
            if (bodyMaterial != null) Destroy(bodyMaterial);
        }
    }
}
