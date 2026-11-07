using DeepDive.Economy;
using UnityEngine;

namespace DeepDive.Composition
{
    // #278: puts the success card on the local player. A hidden runner looks twice a second for the owner's spawned EconomyPlayerSync and adds
    // SuccessFeedbackView once; it carries no state of its own.
    public sealed class SuccessFeedbackBinding : MonoBehaviour
    {
        private float next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<SuccessFeedbackBinding>() != null) return;
            var host = new GameObject("SuccessFeedbackBinding") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(host);
            host.AddComponent<SuccessFeedbackBinding>();
        }

        private void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.5f;
            foreach (var sync in FindObjectsByType<EconomyPlayerSync>(FindObjectsSortMode.None))
                if (sync.IsSpawned && sync.IsOwner && sync.GetComponent<SuccessFeedbackView>() == null) sync.gameObject.AddComponent<SuccessFeedbackView>();
        }
    }
}
