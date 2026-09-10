using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Network
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(CharacterController))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float swimSpeed = 3f;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Renderer bodyRenderer;
        public readonly NetworkVariable<bool> Swimming = new NetworkVariable<bool>();
        public bool ReadKeyboard { get; set; } = true;
        private readonly ServerInputBuffer input = new ServerInputBuffer();
        private CharacterController controller;
        private NetworkTransform networkTransform;
        private NetworkSession session;
        private Material bodyMaterial;
        private uint sequence;
        private float yaw;
        private float pitch;
        private float gravityVelocity;
        private double nextInputTime;

        public override void OnNetworkSpawn()
        {
            controller = GetComponent<CharacterController>();
            networkTransform = GetComponent<NetworkTransform>();
            session = NetworkManager.GetComponent<NetworkSession>();
            controller.enabled = IsServer;
            NetworkObject.DestroyWithScene = false;
            yaw = transform.eulerAngles.y;
            if (viewCamera != null)
            {
                viewCamera.enabled = IsOwner;
                var listener = viewCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = IsOwner;
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
            if (!IsSpawned || !IsOwner || !ReadKeyboard) return;
            if (Input.GetKeyDown(KeyCode.F1)) SetLookCaptured(Cursor.lockState != CursorLockMode.Locked);
            if (Input.GetKeyDown(KeyCode.Escape)) SetLookCaptured(false);
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxisRaw("Mouse X") * 2f;
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 2f, -85f, 85f);
            }
            if (Time.unscaledTimeAsDouble < nextInputTime) return;
            nextInputTime = Time.unscaledTimeAsDouble + 1.0 / 30;
            var move = Vector3.zero;
            if (Cursor.lockState == CursorLockMode.Locked && Application.isFocused)
            {
                move.x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
                move.z = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
                move.y = (Input.GetKey(KeyCode.Space) ? 1 : 0) - (Input.GetKey(KeyCode.LeftControl) ? 1 : 0);
            }
            SubmitLocalInput(move, yaw);
        }

        public void SetLookCaptured(bool captured)
        {
            if (!IsOwner) return;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        // Used by local controls. Ownership is checked again by NGO at the RPC boundary.
        public void SubmitLocalInput(Vector3 move, float lookYaw)
        {
            if (!IsSpawned || !IsOwner || !NetworkManager.IsConnectedClient) return;
            var frame = new PlayerInputFrame { Sequence = ++sequence, Move = move, Yaw = lookYaw };
            if (IsServer) input.Accept(frame, Time.realtimeSinceStartupAsDouble);
            else MoveRpc(frame);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        private void MoveRpc(PlayerInputFrame frame)
        { input.Accept(frame, Time.realtimeSinceStartupAsDouble); }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || session == null || session.IsSceneLoading) return;
            var frame = input.Read(Time.realtimeSinceStartupAsDouble);
            transform.rotation = Quaternion.Euler(0, frame.Yaw, 0);
            Swimming.Value = SwimVolume.Contains(transform.position + Vector3.up * 0.9f);
            var move = frame.Move;
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
            if (IsOwner) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (bodyMaterial != null) Destroy(bodyMaterial);
        }
    }
}
