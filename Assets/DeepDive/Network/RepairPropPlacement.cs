using UnityEngine;

namespace DeepDive.Network
{
    public static class RepairPropPlacement
    {
        public static void AimToolContact(Transform tool,Transform contact,Transform grip,
            Vector3 target,Vector3 restingGrip,Quaternion facing)
        {
            var lever=tool.InverseTransformDirection(contact.position-grip.position);
            var direction=target-restingGrip;
            if(lever.sqrMagnitude<.000001f||direction.sqrMagnitude<.000001f)return;
            var orientation=Quaternion.FromToRotation(facing*lever,direction)*facing;
            var relative=Quaternion.Inverse(tool.rotation)*contact.rotation;
            AlignGrip(tool,contact,target,orientation*relative);
        }
        // Supports nested markers and imported prefab scale; preserve the source hierarchy.
        public static void AlignGrip(Transform prop, Transform grip, Vector3 position, Quaternion rotation)
        {
            if (prop == null) return;
            if (grip == null) { prop.SetPositionAndRotation(position, rotation); return; }
            prop.rotation = rotation * Quaternion.Inverse(grip.rotation) * prop.rotation;
            prop.position += position - grip.position;
        }

        public static Transform Marker(Transform root, string name)
        {
            foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == name) return candidate;
            return null;
        }
    }
}
