using UnityEngine;

namespace DeepDive.Network
{
    // Local visual pose only. Movement, oxygen and equipment ownership stay on NetworkPlayer.
    public sealed class CoastalDiverPose : MonoBehaviour
    {
        public bool FirstPerson;
        public Transform Model;
        private Transform leftArm, leftElbow, leftHand, rightArm, rightElbow, rightHand;
        private Transform leftLeg, leftKnee, leftFoot, rightLeg, rightKnee, rightFoot, head;
        private PlayerPresentationState state;
        private float speed, cycle;
        private void Awake()
        {
            leftArm = Bone("UpperArm.L"); leftElbow = Bone("LowerArm.L"); leftHand = Bone("Hand.L");
            rightArm = Bone("UpperArm.R"); rightElbow = Bone("LowerArm.R"); rightHand = Bone("Hand.R");
            leftLeg = Bone("UpperLeg.L"); leftKnee = Bone("LowerLeg.L"); leftFoot = Bone("Foot.L");
            rightLeg = Bone("UpperLeg.R"); rightKnee = Bone("LowerLeg.R"); rightFoot = Bone("Foot.R");
            head = Bone("Head");
        }
        public void Present(PlayerPresentationState value, float normalizedSpeed)
        { state = value; speed = Mathf.Clamp01(normalizedSpeed); }

        private void LateUpdate()
        {
            if (Model == null || leftArm == null) return;
            cycle += Time.deltaTime * Mathf.Lerp(2.4f, 6.5f, speed);
            var underwater = state.Locomotion == LocomotionMode.Underwater && !FirstPerson;
            var swimming = PlayerPresentationRules.IsSwimming(state.Locomotion) && !FirstPerson;
            var rotation = Quaternion.Euler(underwater ? 78f : 0f, 0f, 0f);
            Model.localRotation = rotation;
            Model.localPosition = underwater ? new Vector3(0, 0.9f, 0) - rotation * new Vector3(0, 0.9f, 0) : Vector3.zero;
            if (swimming)
            {
                var kick = Mathf.Sin(cycle) * Mathf.Lerp(0.10f, 0.40f, speed);
                Aim(leftLeg, leftKnee, new Vector3(0, -1, kick));
                Aim(rightLeg, rightKnee, new Vector3(0, -1, -kick));
                Aim(leftKnee, leftFoot, new Vector3(0, -1, -Mathf.Max(0, kick) * 1.6f));
                Aim(rightKnee, rightFoot, new Vector3(0, -1, -Mathf.Max(0, -kick) * 1.6f));
                Aim(leftArm, leftElbow, new Vector3(-0.7f, -0.4f, 0.4f + kick));
                Aim(rightArm, rightElbow, new Vector3(0.7f, -0.4f, 0.4f - kick));
                Aim(leftElbow, leftHand, new Vector3(-0.2f, -0.25f, 0.8f));
                Aim(rightElbow, rightHand, new Vector3(0.2f, -0.25f, 0.8f));
                if (underwater && head != null) head.rotation = Quaternion.AngleAxis(-55, Model.right) * head.rotation;
            }
            if (FirstPerson || state.HeldEquipment != HeldEquipmentMode.None)
            {
                var camera = state.HeldEquipment == HeldEquipmentMode.Camera;
                var y = FirstPerson ? 1.46f : 1.24f;
                Hold(rightArm, rightElbow, rightHand, new Vector3(camera ? 0.18f : 0.24f, y, 0.44f), 1);
                Hold(leftArm, leftElbow, leftHand, new Vector3(camera ? -0.18f : -0.12f, y - (camera ? 0 : 0.10f), 0.43f), -1);
            }
        }

        private Transform Bone(string boneName)
        {
            foreach (var bone in GetComponentsInChildren<Transform>(true)) if (bone.name == boneName) return bone;
            return null;
        }
        private void Aim(Transform bone, Transform child, Vector3 direction)
        {
            if (bone == null || child == null) return;
            bone.rotation = Quaternion.FromToRotation(child.position - bone.position,
                Model.TransformDirection(direction)) * bone.rotation;
        }
        private void Hold(Transform upper, Transform lower, Transform hand, Vector3 localTarget, float side)
        {
            if (upper == null || lower == null || hand == null) return;
            var target = Model.TransformPoint(localTarget);
            var a = Vector3.Distance(upper.position, lower.position);
            var b = Vector3.Distance(lower.position, hand.position);
            var delta = target - upper.position;
            var distance = Mathf.Clamp(delta.magnitude, 0.02f, a + b - 0.001f);
            var direction = delta.normalized;
            var along = (a * a - b * b + distance * distance) / (2 * distance);
            var bend = Vector3.ProjectOnPlane(Model.TransformDirection(new Vector3(side, -1, 0)), direction).normalized;
            var elbow = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
        }
    }
}
