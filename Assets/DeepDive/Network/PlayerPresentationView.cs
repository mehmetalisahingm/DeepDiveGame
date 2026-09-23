using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Network
{
    [DisallowMultipleComponent]
    public sealed class PlayerPresentationView : MonoBehaviour
    {
        [Header("Optional explicit P3.1 bindings")]
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer[] firstPersonRenderers;
        [SerializeField] private Renderer[] thirdPersonRenderers;
        [SerializeField] private GameObject firstPersonHarpoon;
        [SerializeField] private GameObject firstPersonCamera;
        [SerializeField] private GameObject thirdPersonHarpoon;
        [SerializeField] private GameObject thirdPersonCamera;

        [Header("P3.1 runtime presentation")]
        [SerializeField] private DiverPresentationCatalog presentationCatalog;
        [SerializeField] private Transform firstPersonMount;
        [SerializeField] private Transform thirdPersonMount;
        [SerializeField] private bool buildFirstPersonArms = true;

        private readonly HashSet<int> parameters = new HashSet<int>();
        private Renderer fallbackBody;
        private GameObject runtimeRig;
        private GameObject runtimeFirstPersonArms;
        private bool initialized;
        private int drivenAnimatorState;
        private CoastalDiverPose bodyPose, armsPose;

        private static readonly int LocomotionParam = Animator.StringToHash("LocomotionMode");
        private static readonly int SpeedParam = Animator.StringToHash("MoveSpeed");
        private static readonly int EquipmentParam = Animator.StringToHash("HeldEquipment");
        private static readonly int RecordingParam = Animator.StringToHash("Recording");

        public void BindFallback(Renderer renderer)
        {
            fallbackBody = renderer;
            EnsureInitialized();
        }

        public void Apply(bool isOwner, PlayerPresentationState state, float normalizedSpeed)
        {
            EnsureInitialized();

            var showEquipment = state.Locomotion != LocomotionMode.Passive;
            SetRendererGroup(firstPersonRenderers, isOwner && showEquipment, false);
            SetRendererGroup(thirdPersonRenderers, true, isOwner);

            var hasHumanRig = thirdPersonRenderers != null && thirdPersonRenderers.Length > 0;
            if (fallbackBody != null)
            {
                fallbackBody.enabled = !hasHumanRig;
                if (fallbackBody.enabled)
                    fallbackBody.shadowCastingMode = isOwner ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }

            SetActive(firstPersonHarpoon, isOwner && showEquipment && state.HeldEquipment == HeldEquipmentMode.Harpoon);
            SetActive(firstPersonCamera, isOwner && showEquipment && state.HeldEquipment == HeldEquipmentMode.Camera);
            SetActive(thirdPersonHarpoon, !isOwner && showEquipment && state.HeldEquipment == HeldEquipmentMode.Harpoon);
            SetActive(thirdPersonCamera, !isOwner && showEquipment && state.HeldEquipment == HeldEquipmentMode.Camera);

            ApplyAnimator(state, normalizedSpeed);
            bodyPose?.Present(state, normalizedSpeed);
            armsPose?.Present(state, normalizedSpeed);
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            if (presentationCatalog == null)
                presentationCatalog = Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);

            if (thirdPersonMount == null) thirdPersonMount = transform;
            if (firstPersonMount == null)
            {
                var ownerCamera = GetComponentInChildren<Camera>(true);
                if (ownerCamera != null) firstPersonMount = ownerCamera.transform;
            }

            BuildRuntimePresentation();

            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                foreach (var parameter in animator.parameters)
                    parameters.Add(parameter.nameHash);
            }

            SetRendererGroup(firstPersonRenderers, false, false);
            SetActive(firstPersonHarpoon, false);
            SetActive(firstPersonCamera, false);
            SetActive(thirdPersonHarpoon, false);
            SetActive(thirdPersonCamera, false);
        }

        private void BuildRuntimePresentation()
        {
            if (presentationCatalog == null) return;

            if (runtimeRig == null && presentationCatalog.ThirdPersonRigPrefab != null)
            {
                runtimeRig = Instantiate(presentationCatalog.ThirdPersonRigPrefab, thirdPersonMount, false);
                runtimeRig.name = "P3_ThirdPersonDiverRig";
                runtimeRig.transform.localPosition = Vector3.zero;
                runtimeRig.transform.localRotation = Quaternion.identity;
                runtimeRig.transform.localScale = Vector3.one;
                bodyPose = runtimeRig.GetComponent<CoastalDiverPose>();

                if (animator == null) animator = runtimeRig.GetComponentInChildren<Animator>(true);
                if (thirdPersonRenderers == null || thirdPersonRenderers.Length == 0)
                    thirdPersonRenderers = runtimeRig.GetComponentsInChildren<Renderer>(true);

                var handSocket = FindDescendant(runtimeRig.transform, "Socket_RightHand_Equipment");
                if (handSocket != null)
                {
                    if (thirdPersonHarpoon == null)
                        thirdPersonHarpoon = InstantiateGripAligned(presentationCatalog.HarpoonPropPrefab, handSocket,
                            "P3_ThirdPersonHarpoon");
                    if (thirdPersonCamera == null)
                        thirdPersonCamera = InstantiateGripAligned(presentationCatalog.CameraPropPrefab, handSocket,
                            "P3_ThirdPersonCamera");
                }
            }

            if (firstPersonMount == null) return;

            if (buildFirstPersonArms && (firstPersonRenderers == null || firstPersonRenderers.Length == 0))
            {
                if (presentationCatalog.FirstPersonArmsPrefab != null)
                {
                    runtimeFirstPersonArms = Instantiate(presentationCatalog.FirstPersonArmsPrefab, firstPersonMount, false);
                    runtimeFirstPersonArms.transform.localPosition = new Vector3(0, -1.65f, 0);
                    armsPose = runtimeFirstPersonArms.GetComponent<CoastalDiverPose>();
                    firstPersonRenderers = runtimeFirstPersonArms.GetComponentsInChildren<Renderer>(true);
                }
                else firstPersonRenderers = BuildFirstPersonArms(firstPersonMount);
            }

            if (firstPersonHarpoon == null && presentationCatalog.HarpoonPropPrefab != null)
            {
                firstPersonHarpoon = Instantiate(presentationCatalog.HarpoonPropPrefab, firstPersonMount, false);
                firstPersonHarpoon.name = "P3_FirstPersonHarpoon";
                firstPersonHarpoon.transform.localPosition = new Vector3(0.27f, -0.28f, 0.55f);
                firstPersonHarpoon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            if (firstPersonCamera == null && presentationCatalog.CameraPropPrefab != null)
            {
                firstPersonCamera = Instantiate(presentationCatalog.CameraPropPrefab, firstPersonMount, false);
                firstPersonCamera.name = "P3_FirstPersonCamera";
                firstPersonCamera.transform.localPosition = new Vector3(0f, -0.16f, 0.42f);
                firstPersonCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void ApplyAnimator(PlayerPresentationState state, float normalizedSpeed)
        {
            if (animator == null) return;

            normalizedSpeed = Mathf.Clamp01(normalizedSpeed);
            var hasSpeedParameter = parameters.Contains(SpeedParam);

            if (hasSpeedParameter)
            {
                if (parameters.Contains(LocomotionParam)) animator.SetInteger(LocomotionParam, (int)state.Locomotion);
                animator.SetFloat(SpeedParam, normalizedSpeed);
            }
            else
            {
                // Mert's first controller revision only had LocomotionMode and used same-value
                // transitions for idle/walk and surface/tread. Put that parameter outside the
                // transition range and drive the named state directly so it cannot ping-pong.
                if (parameters.Contains(LocomotionParam)) animator.SetInteger(LocomotionParam, 99);
                var stateName = PlayerPresentationRules.ResolveAnimatorStateName(state.Locomotion, normalizedSpeed);
                var stateHash = Animator.StringToHash("Base Layer." + stateName);
                if (stateHash != drivenAnimatorState && animator.HasState(0, stateHash))
                {
                    drivenAnimatorState = stateHash;
                    animator.CrossFade(stateHash, 0.12f, 0);
                }
            }

            if (parameters.Contains(EquipmentParam)) animator.SetInteger(EquipmentParam, (int)state.HeldEquipment);
            if (parameters.Contains(RecordingParam)) animator.SetBool(RecordingParam, state.Recording);
        }

        private Renderer[] BuildFirstPersonArms(Transform mount)
        {
            runtimeFirstPersonArms = new GameObject("P3_FirstPersonArms");
            runtimeFirstPersonArms.transform.SetParent(mount, false);

            CreatePrimitivePart(runtimeFirstPersonArms.transform, PrimitiveType.Capsule, "RightForearm",
                new Vector3(0.22f, -0.24f, 0.40f), Quaternion.Euler(72f, 0f, -12f),
                new Vector3(0.055f, 0.20f, 0.055f));
            CreatePrimitivePart(runtimeFirstPersonArms.transform, PrimitiveType.Sphere, "RightHand",
                new Vector3(0.24f, -0.20f, 0.62f), Quaternion.identity, new Vector3(0.09f, 0.09f, 0.11f));
            CreatePrimitivePart(runtimeFirstPersonArms.transform, PrimitiveType.Capsule, "LeftForearm",
                new Vector3(-0.22f, -0.24f, 0.40f), Quaternion.Euler(72f, 0f, 12f),
                new Vector3(0.055f, 0.20f, 0.055f));
            CreatePrimitivePart(runtimeFirstPersonArms.transform, PrimitiveType.Sphere, "LeftHand",
                new Vector3(-0.24f, -0.20f, 0.62f), Quaternion.identity, new Vector3(0.09f, 0.09f, 0.11f));

            return runtimeFirstPersonArms.GetComponentsInChildren<Renderer>(true);
        }

        private static void CreatePrimitivePart(Transform parent, PrimitiveType type, string name,
            Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            var collider = part.GetComponent<Collider>();
            if (collider == null) return;
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        private static GameObject InstantiateGripAligned(GameObject prefab, Transform mount, string instanceName)
        {
            if (prefab == null || mount == null) return null;
            var instance = Instantiate(prefab, mount, false);
            instance.name = instanceName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            var grip = FindDescendant(instance.transform, "GripPoint");
            if (grip != null && grip.parent == instance.transform)
            {
                instance.transform.localRotation = Quaternion.Inverse(grip.localRotation);
                instance.transform.localPosition = -(instance.transform.localRotation * grip.localPosition);
            }
            return instance;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static void SetRendererGroup(Renderer[] renderers, bool enabled, bool shadowsOnly)
        {
            if (renderers == null) return;
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.enabled = enabled;
                if (enabled)
                    renderer.shadowCastingMode = shadowsOnly ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}
