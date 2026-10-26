using System.Collections.Generic;
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
        private Transform equipmentSocket, leftIndex, rightIndex;
        private Transform[] fingerBones;
        private PlayerPresentationState state;
        private float speed, cycle;
        private struct PalmFrame
        {public Transform Hand;public Vector3 Center,Normal;public Quaternion BasisInverse;}
        private PalmFrame leftPalm,rightPalm;
        private bool repairFingersApplied;
        private readonly Dictionary<Transform,Quaternion> fingerBind=new Dictionary<Transform,Quaternion>();
        public Vector3 RepairRightPalmPosition=>rightPalm.Hand!=null?rightPalm.Hand.TransformPoint(rightPalm.Center):transform.position;
        public Vector3 RepairLeftPalmPosition=>leftPalm.Hand!=null?leftPalm.Hand.TransformPoint(leftPalm.Center):transform.position;
        private void Awake()
        {
            leftArm = Bone("UpperArm.L"); leftElbow = Bone("LowerArm.L"); leftHand = Bone("Hand.L");
            rightArm = Bone("UpperArm.R"); rightElbow = Bone("LowerArm.R"); rightHand = Bone("Hand.R");
            leftLeg = Bone("UpperLeg.L"); leftKnee = Bone("LowerLeg.L"); leftFoot = Bone("Foot.L");
            rightLeg = Bone("UpperLeg.R"); rightKnee = Bone("LowerLeg.R"); rightFoot = Bone("Foot.R");
            head = Bone("Head");
            equipmentSocket = Bone("Socket_RightHand_Equipment");
            leftIndex = Bone("Index2.L"); rightIndex = Bone("Index2.R");
            fingerBones = GetComponentsInChildren<Transform>(true);
            leftPalm=Palm(leftHand,leftIndex,Bone("Pinky2.L"),false);
            rightPalm=Palm(rightHand,rightIndex,Bone("Pinky2.R"),true);
            fingerBind.Clear();
            foreach(var finger in fingerBones)
                if(IsFinger(finger)&&finger.childCount>0)fingerBind[finger]=finger.localRotation;
        }
        private float equipmentLowering;
        public void Present(PlayerPresentationState value, float normalizedSpeed,float lowering=0)
        { state = value; speed = Mathf.Clamp01(normalizedSpeed);equipmentLowering=Mathf.Clamp01(lowering); }

        public void PresentRepair(Vector3 left, Vector3 right, Quaternion rotation)
        {PresentRepair(left,right,rotation,rotation,true);}

        public void PresentRepair(Vector3 left,Vector3 right,Quaternion leftRotation,Quaternion rightRotation,bool gripLeft)
        {
            if (Model == null) return;
            Grip(leftPalm,leftArm,leftElbow,left,leftRotation,-1);
            Grip(rightPalm,rightArm,rightElbow,right,rightRotation,1);
            Curl(leftPalm,".L",gripLeft);Curl(rightPalm,".R",true);
            repairFingersApplied=true;
        }

        private static bool IsFinger(Transform bone)=>bone.name.StartsWith("Index")||bone.name.StartsWith("Middle")||
            bone.name.StartsWith("Ring")||bone.name.StartsWith("Pinky")||bone.name.StartsWith("Thumb");
        private PalmFrame Palm(Transform hand,Transform index,Transform pinky,bool right)
        {
            if(hand==null||index==null||pinky==null)return new PalmFrame{Hand=hand,BasisInverse=Quaternion.identity};
            var middle=(index.position+pinky.position)*.5f;
            var across=hand.InverseTransformDirection(index.position-pinky.position).normalized;
            var forward=Vector3.ProjectOnPlane(hand.InverseTransformDirection(middle-hand.position),across).normalized;
            return new PalmFrame{Hand=hand,Center=hand.InverseTransformPoint(middle)*.62f,
                Normal=(right?Vector3.Cross(forward,across):Vector3.Cross(across,forward)).normalized,
                BasisInverse=Quaternion.Inverse(Quaternion.LookRotation(forward,across))};
        }
        private void Grip(PalmFrame palm,Transform upper,Transform lower,Vector3 target,Quaternion rotation,float side)
        {
            if(palm.Hand==null)return;
            var orientation=rotation*palm.BasisInverse;palm.Hand.rotation=orientation;
            var wrist=target-palm.Hand.TransformVector(palm.Center);
            HoldWorld(upper,lower,palm.Hand,wrist,side);palm.Hand.rotation=orientation;
        }
        private void Curl(PalmFrame palm,string side,bool gripping)
        {
            if(palm.Hand==null)return;
            foreach(var pair in fingerBind)if(pair.Key.name.EndsWith(side))pair.Key.localRotation=pair.Value;
            var normal=palm.Hand.TransformDirection(palm.Normal);
            foreach(var finger in fingerBones)
            {
                if(!fingerBind.ContainsKey(finger)||!finger.name.EndsWith(side))continue;
                var direction=finger.GetChild(0).position-finger.position;
                bool thumb=finger.name.StartsWith("Thumb");
                var toward=thumb?(palm.Hand.TransformPoint(palm.Center)-finger.position+normal*.025f).normalized:normal;
                var axis=Vector3.Cross(direction,toward);
                if(axis.sqrMagnitude<.000001f)continue;
                float angle=thumb?(finger.name.Contains("2.")?40:28):(finger.name.Contains("2.")?55:finger.name.Contains("3.")?65:35);
                if(!gripping)angle*=.18f;
                finger.rotation=Quaternion.AngleAxis(angle,axis.normalized)*finger.rotation;
            }
        }

        private void LateUpdate()
        {
            if (Model == null || leftArm == null) return;
            if(repairFingersApplied)
            {
                foreach(var pair in fingerBind)pair.Key.localRotation=pair.Value;
                repairFingersApplied=false;
            }
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
                var plank = state.HeldEquipment == HeldEquipmentMode.RepairPart;
                var y = FirstPerson ? 1.39f : 1.16f;
                if (plank) y -= .15f;
                y-=(FirstPerson ? .34f : .25f)*equipmentLowering;
                var zOffset=-.10f*equipmentLowering;
                Hold(rightArm, rightElbow, rightHand, new Vector3(plank ? .35f : camera ? 0.18f : 0.24f, y, (plank ? .57f : .44f)+zOffset), 1);
                Hold(leftArm, leftElbow, leftHand, new Vector3(plank ? -.35f : camera ? -0.18f : -0.12f, y - (camera || plank ? 0 : 0.10f), (plank ? .57f : .43f)+zOffset), -1);
                Aim(rightHand, rightIndex, camera ? Vector3.up : Vector3.forward);
                Aim(leftHand, leftIndex, camera ? Vector3.up : Vector3.forward);
                foreach (var finger in fingerBones)
                {
                    if (finger.childCount == 0 || !(finger.name.StartsWith("Index") || finger.name.StartsWith("Middle") ||
                        finger.name.StartsWith("Ring") || finger.name.StartsWith("Pinky"))) continue;
                    var curl = camera
                        ? finger.name.Contains("2.") ? Vector3.forward : finger.name.Contains("3.") ? Vector3.down : Vector3.back
                        : finger.name.Contains("2.") ? Vector3.down : finger.name.Contains("3.") ? Vector3.back : Vector3.up;
                    Aim(finger, finger.GetChild(0), curl);
                }
                if (equipmentSocket != null)
                {
                    equipmentSocket.position = rightHand.position + Model.TransformDirection(new Vector3(0, .085f, .015f));
                    equipmentSocket.rotation = Model.rotation*Quaternion.Euler(25*equipmentLowering,0,0);
                }
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
        { HoldWorld(upper, lower, hand, Model.TransformPoint(localTarget), side); }

        private void HoldWorld(Transform upper, Transform lower, Transform hand, Vector3 target, float side)
        {
            if (upper == null || lower == null || hand == null) return;
            var a = Vector3.Distance(upper.position, lower.position);
            var b = Vector3.Distance(lower.position, hand.position);
            var delta = target - upper.position;
            var distance = Mathf.Clamp(delta.magnitude, 0.02f, a + b - 0.001f);
            var direction = delta.normalized;
            target = upper.position + direction * distance;
            var along = (a * a - b * b + distance * distance) / (2 * distance);
            var bend = Vector3.ProjectOnPlane(Model.TransformDirection(new Vector3(side, -1, 0)), direction).normalized;
            var elbow = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
        }
    }
}
