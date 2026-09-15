using UnityEngine;

namespace DeepDive.Network
{
    public enum EnvironmentLocomotion : byte
    {
        Land = 0,
        Surface = 1,
        Underwater = 2
    }

    public enum LocomotionMode : byte
    {
        Land = 0,
        Surface = 1,
        Underwater = 2,
        Seated = 3,
        Passive = 4
    }

    public enum HeldEquipmentMode : byte
    {
        None = 0,
        Harpoon = 1,
        Camera = 2
    }

    public readonly struct PlayerPresentationState
    {
        public readonly LocomotionMode Locomotion;
        public readonly HeldEquipmentMode HeldEquipment;
        public readonly bool Recording;

        public PlayerPresentationState(LocomotionMode locomotion, HeldEquipmentMode heldEquipment, bool recording)
        {
            Locomotion = locomotion;
            HeldEquipment = heldEquipment;
            Recording = recording;
        }
    }

    public static class PlayerPresentationRules
    {
        public static LocomotionMode ResolveLocomotion(EnvironmentLocomotion environment, bool seated, bool passive)
        {
            if (passive) return LocomotionMode.Passive;
            if (seated) return LocomotionMode.Seated;
            return environment switch
            {
                EnvironmentLocomotion.Surface => LocomotionMode.Surface,
                EnvironmentLocomotion.Underwater => LocomotionMode.Underwater,
                _ => LocomotionMode.Land
            };
        }

        public static HeldEquipmentMode ResolveHeldEquipment(bool diveActive, bool passive,
            HeldEquipmentMode requested, bool recording)
        {
            if (!diveActive || passive) return HeldEquipmentMode.None;
            if (recording) return HeldEquipmentMode.Camera;
            return requested == HeldEquipmentMode.Camera ? HeldEquipmentMode.Camera : HeldEquipmentMode.Harpoon;
        }

        public static Vector3 FilterMoveInput(LocomotionMode locomotion, Vector3 input)
        {
            if (locomotion == LocomotionMode.Passive || locomotion == LocomotionMode.Seated)
                return Vector3.zero;

            if (locomotion == LocomotionMode.Land)
                input.y = 0f;
            else if (locomotion == LocomotionMode.Surface && input.y > 0f)
                input.y = 0f;

            return Vector3.ClampMagnitude(input, 1f);
        }

        public static bool IsSwimming(LocomotionMode locomotion) =>
            locomotion == LocomotionMode.Surface || locomotion == LocomotionMode.Underwater;

        public static bool DrainsOxygen(LocomotionMode locomotion) => locomotion == LocomotionMode.Underwater;

        public static bool CanUseHarpoon(bool diveActive, bool passive, LocomotionMode locomotion,
            HeldEquipmentMode equipment) =>
            diveActive && !passive && IsSwimming(locomotion) && equipment == HeldEquipmentMode.Harpoon;
    }
}
