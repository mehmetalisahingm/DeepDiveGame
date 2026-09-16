using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Network
{
    [DisallowMultipleComponent]
    public sealed class PlayerPresentationView : MonoBehaviour
    {
        [Header("Optional P3.1 rig bindings")]
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer[] firstPersonRenderers;
        [SerializeField] private Renderer[] thirdPersonRenderers;
        [SerializeField] private GameObject firstPersonHarpoon;
        [SerializeField] private GameObject firstPersonCamera;
        [SerializeField] private GameObject thirdPersonHarpoon;
        [SerializeField] private GameObject thirdPersonCamera;

        private readonly HashSet<int> parameters = new HashSet<int>();
        private Renderer fallbackBody;
        private bool initialized;

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

            SetRendererGroup(firstPersonRenderers, isOwner, false);
            SetRendererGroup(thirdPersonRenderers, true, isOwner);
            if ((thirdPersonRenderers == null || thirdPersonRenderers.Length == 0) && fallbackBody != null)
            {
                fallbackBody.enabled = true;
                fallbackBody.shadowCastingMode = isOwner ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }

            var showEquipment = state.Locomotion != LocomotionMode.Passive;
            SetActive(firstPersonHarpoon, isOwner && showEquipment && state.HeldEquipment == HeldEquipmentMode.Harpoon);
            SetActive(firstPersonCamera, isOwner && showEquipment && state.HeldEquipment == HeldEquipmentMode.Camera);
            SetActive(thirdPersonHarpoon, !isOwner && showEquipment && state.HeldEquipment == HeldEquipmentMode.Harpoon);
            SetActive(thirdPersonCamera, !isOwner && showEquipment && state.HeldEquipment == HeldEquipmentMode.Camera);

            if (animator == null) return;
            if (parameters.Contains(LocomotionParam)) animator.SetInteger(LocomotionParam, (int)state.Locomotion);
            if (parameters.Contains(SpeedParam)) animator.SetFloat(SpeedParam, Mathf.Clamp01(normalizedSpeed));
            if (parameters.Contains(EquipmentParam)) animator.SetInteger(EquipmentParam, (int)state.HeldEquipment);
            if (parameters.Contains(RecordingParam)) animator.SetBool(RecordingParam, state.Recording);
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null) return;
            foreach (var parameter in animator.parameters)
                parameters.Add(parameter.nameHash);
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
