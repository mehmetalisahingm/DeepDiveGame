using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    // P4/#278: small owner-only guidance for the first playable loop.
    // It only reads already replicated host-confirmed state, never grants
    // rewards, publishes progress, or changes input/network/save authority.
    public static class FirstSessionObjectiveRules
    {
        public static string Next(bool passive, bool selectedProduct, bool recording,
            bool swimming, bool confirmedPickup)
        {
            if (passive) return "DALIS BITTI - Ekibinle guvenli donusu tamamla.";
            if (selectedProduct) return "URUN SECILDI - Kasaya gotur ve F ile ode.";
            if (recording) return "KAYIT SURUYOR - Klibi bitir, guvenle eve don.";
            if (swimming) return "DALIS - 1: ZIPKIN  2: KAMERA  E: TOPLA";
            if (confirmedPickup) return "ILK TOPLAMA TAMAM - Satisi ve ev PC yayini kontrol et.";
            return "ILK HEDEF - Hazirlan, sahile git ve dalisa basla.";
        }
    }

    [DisallowMultipleComponent]
    public sealed class FirstSessionObjectiveView : MonoBehaviour
    {
        private const float FirstMinutesSeconds = 150f;
        private NetworkPlayer player;
        private float visibleUntil;
        private float successUntil;
        private ulong lastActionRequest;
        private bool initialized;
        private bool enabledByPlayer = true;
        private bool confirmedPickup;

        private void Awake()
        {
            player = GetComponent<NetworkPlayer>();
            visibleUntil = Time.unscaledTime + FirstMinutesSeconds;
        }

        private void Update()
        {
            if (player == null || !player.IsSpawned || !player.IsOwner) return;
            if (Input.GetKeyDown(KeyCode.F2)) enabledByPlayer = !enabledByPlayer;
            if (!initialized)
            {
                initialized = true;
                lastActionRequest = player.LastActionRequestId.Value;
                return;
            }

            var request = player.LastActionRequestId.Value;
            if (request == 0 || request == lastActionRequest) return;
            lastActionRequest = request;
            var feedback = ActionFeedbackRules.Resolve(
                (PlayerActionKind)player.LastActionKind.Value,
                (PlayerActionResult)player.LastActionResult.Value);
            // This is a first-session milestone, not a celebration for
            // every future catch. Do not resurrect onboarding after it expires.
            if (confirmedPickup || feedback.Cue != PlayerFeedbackCue.PickupAccepted) return;
            confirmedPickup = true;
            successUntil = Time.unscaledTime + 6f;
        }

        private void OnGUI()
        {
            if (player == null || !player.IsSpawned || !player.IsOwner || !enabledByPlayer ||
                (Time.unscaledTime > visibleUntil && Time.unscaledTime > successUntil)) return;

            var width = Mathf.Min(380f, Screen.width - 24f);
            if (width <= 180f) return;
            var panel = new Rect(Screen.width - width - 12f, 20f, width, 74f);
            var previous = GUI.color;
            GUI.color = new Color(0.07f, 0.16f, 0.20f, 0.87f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Time.unscaledTime < successUntil
                ? new Color(1f, 0.84f, 0.49f)
                : new Color(0.65f, 0.90f, 0.94f);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 7f, width - 24f, 20f),
                Time.unscaledTime < successUntil ? "BASARDIN! ILK TOPLAMA" : "SIRADAKI ADIM   [F2: GIZLE]");
            GUI.color = Color.white;
            var message = FirstSessionObjectiveRules.Next(
                player.Passive.Value,
                !string.IsNullOrEmpty(player.SelectedShopItem.Value.ToString()),
                player.RecordingPresentation.Value,
                player.Swimming.Value,
                confirmedPickup);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 36f, width - 24f, 26f), message);
            GUI.color = previous;
        }
    }
}
