using UnityEngine;

namespace DeepDive.Composition
{
    // #189 P4.8-C presentation of the starter-boat repair worksite: the broken hull with three open bays, one permanent
    // repaired part per R01 group (hull patch, seat/support, oar connection) and the bench at repair-drop-0.
    // Pure presentation: no colliders, no interaction, no state. The parts keep OakRepairPatch's nail surface, so the
    // host-driven nail seating stays in BoatRepairNetworkBinding. Every loader returns null/false when the art is
    // missing, so the binding falls back to its primitive worksite.
    public static class BoatRepairSetArt
    {
        public const string DamagedHull = "RepairSet/DamagedRowboat";
        public const string Trestle = "RepairSet/DropTrestle";
        // BoatRepairParts.All order: hull -> patch, engine -> seat/support, fuel-tank -> oar connection (R01 keeps the technical ids).
        public static readonly string[] PartResources =
        {
            "RepairSet/RepairedHullPatch", "RepairSet/RepairedSeatSupport", "RepairSet/RepairedOarlock"
        };

        public static bool AttachDamagedHull(Transform site)
        {
            var prefab = Resources.Load<GameObject>(DamagedHull);
            if (prefab == null) return false;
            Instantiate(prefab, site).name = "DamagedRowboat";
            return true;
        }

        // The permanent repaired part of group `index`, or null when the group art is missing.
        public static GameObject Part(int index, Transform site)
        {
            if (index < 0 || index >= PartResources.Length) return null;
            var prefab = Resources.Load<GameObject>(PartResources[index]);
            return prefab != null ? Instantiate(prefab, site) : null;
        }

        public static GameObject AttachTrestle(Transform drop)
        {
            var prefab = Resources.Load<GameObject>(Trestle);
            if (prefab == null || drop == null) return null;
            var bench = Instantiate(prefab, drop);
            bench.name = "DropTrestle";
            return bench;
        }

        private static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            var instance = Object.Instantiate(prefab, parent, false);
            instance.transform.localPosition = Vector3.zero; instance.transform.localRotation = Quaternion.identity;
            return instance;
        }
    }
}
