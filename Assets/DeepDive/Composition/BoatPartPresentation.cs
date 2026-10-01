using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Composition
{
    // Visual projection only. Completion and duplicate claims are decided by the host economy.
    [RequireComponent(typeof(BoatPartAnchor))]
    public sealed class BoatPartPresentation : MonoBehaviour
    {
        private Renderer[] visuals;
        private Collider[] colliders;
        private int bit;
        private float nextRefresh;
        private void Awake()
        {
            visuals = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            var id = GetComponent<BoatPartAnchor>().PartId;
            for (var i = 0; i < BoatRepairParts.All.Count; i++)
                if (id == BoatRepairParts.All[i]) bit = 1 << i;
        }
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            var manager = NetworkManager.Singleton;
            var player = manager != null ? manager.LocalClient?.PlayerObject : null;
            var progress = player != null ? player.GetComponent<EconomyPlayerSync>() : null;
            if (progress == null || !progress.IsSpawned) return;
            var visible = (progress.BoatPartsMask.Value & bit) == 0;
            foreach (var visual in visuals) if (visual != null) visual.enabled = visible;
            foreach (var collider in colliders) if (collider != null) collider.enabled = visible;
        }
    }
}
