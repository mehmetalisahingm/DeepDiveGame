using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.Network
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SwimVolume : MonoBehaviour
    {
        private static readonly HashSet<SwimVolume> Active = new HashSet<SwimVolume>();
        private BoxCollider volume;
        private void Awake() { volume = GetComponent<BoxCollider>(); volume.isTrigger = true; }
        private void OnEnable() { volume = GetComponent<BoxCollider>(); Active.Add(this); }
        private void OnDisable() => Active.Remove(this);

        public static bool Contains(Vector3 worldPosition)
        {
            foreach (var water in Active)
            {
                if (water == null || water.volume == null || !water.volume.enabled) continue;
                var point = water.transform.InverseTransformPoint(worldPosition) - water.volume.center;
                var half = water.volume.size * 0.5f;
                if (Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.y) <= half.y && Mathf.Abs(point.z) <= half.z)
                    return true;
            }
            return false;
        }
    }
}
