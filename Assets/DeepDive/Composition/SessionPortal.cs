using DeepDive.Network;
using DeepDive.Session;
using UnityEngine;

namespace DeepDive.Composition
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SessionPortal : MonoBehaviour
    {
        public SessionPhase RequiredPhase;
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.E)) return;
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session == null || !session.IsAuthority || session.Session.State.Phase != RequiredPhase) return;
            var box = GetComponent<BoxCollider>();
            foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            {
                if (!player.IsSpawned || !player.IsOwner) continue;
                var point = transform.InverseTransformPoint(player.transform.position + Vector3.up * 0.9f) - box.center;
                var half = box.size * 0.5f;
                if (Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.y) <= half.y && Mathf.Abs(point.z) <= half.z)
                    session.AdvancePhase();
            }
        }
    }
}
