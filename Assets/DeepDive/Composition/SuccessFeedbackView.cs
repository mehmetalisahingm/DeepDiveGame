using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.Composition
{
    // #278 (Mert): the success card the acting player sees after a real turn-in: a title, the money before and after, what the next goal is, a
    // sound, and a short celebration for the first success of the session. Local to the owner (it sits on the owner's EconomyPlayerSync object), driven
    // only by replicated state the host already published, and it writes nothing: no credit, no save, no counter. It keys on the replicated request
    // id, so the same sale is never celebrated twice.
    [DisallowMultipleComponent]
    public sealed class SuccessFeedbackView : MonoBehaviour
    {
        public const float ShowSeconds = 6f, FirstShowSeconds = 9f;
        private static bool firstShown;   // per process: the first accepted turn-in after the game started

        private EconomyPlayerSync sync;
        private NetworkPlayer player;
        private ulong observed;
        private SuccessCard card;
        private float shownAt = -100f, until;
        private AudioSource source;
        private static GUIStyle title, body, number;

        public bool Visible => Time.unscaledTime < until;
        public SuccessCard Card => card;

        public static void ResetFirstForTests() => firstShown = false;

        private void Awake()
        {
            sync = GetComponent<EconomyPlayerSync>();
            player = GetComponent<NetworkPlayer>();
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f; source.playOnAwake = false; source.volume = 0.7f;
        }

        private void Update()
        {
            if (sync == null || !sync.IsSpawned || !sync.IsOwner) return;
            var id = sync.LastServiceRequestId.Value;
            if (id == 0 || id == observed) return;
            observed = id;
            var tier = player != null ? (int)player.EquippedCameraTier.Value : 0;
            if (!SuccessFeedbackRules.TryBuild((ServicePointType)sync.LastServiceType.Value, sync.LastServiceAccepted.Value, sync.LastServiceAmount.Value,
                    sync.LastServiceItems.Value, sync.SharedBalance.Value, tier, !firstShown, out var built)) return;
            Show(built);
        }

        public void Show(SuccessCard built)
        {
            card = built;
            if (built.First) firstShown = true;
            Debug.Log($"P278_SUCCESS_CARD title={built.Title} before={built.Before} after={built.After} first={built.First}");
            shownAt = Time.unscaledTime;
            until = shownAt + (built.First ? FirstShowSeconds : ShowSeconds);
            if (source != null && Application.isPlaying) source.PlayOneShot(SuccessFeedbackRules.CreateChime(built.First));
        }

        private static void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.9f, 0.45f) } };
            body = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, wordWrap = true, normal = { textColor = Color.white } };
            number = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        private void OnGUI()
        {
            if (!Visible || (sync != null && !sync.IsOwner)) return;   // (no sync: the look probe shows a card without a network)
            Styles();
            var age = Time.unscaledTime - shownAt;
            var fade = Mathf.Clamp01((until - Time.unscaledTime) / 0.6f);
            var pop = Mathf.SmoothStep(0.8f, 1f, Mathf.Clamp01(age / 0.25f));
            const float w = 520f, h = 210f;
            var rect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.56f, w, h);
            var previous = GUI.matrix; var color = GUI.color;
            GUIUtility.ScaleAroundPivot(Vector2.one * pop, rect.center);
            GUI.color = new Color(0.06f, 0.1f, 0.16f, 0.88f * fade);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(card.First ? 1f : 0.45f, card.First ? 0.82f : 0.85f, card.First ? 0.25f : 0.55f, fade);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 4f, rect.width, 4f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.Label(new Rect(rect.x, rect.y + 8f, w, 40f), card.Title, title);
            GUI.Label(new Rect(rect.x, rect.y + 48f, w, 22f), card.Detail, body);
            // Before -> after, the difference counted up for a moment so it is noticed
            var shownDelta = Mathf.RoundToInt(card.Delta * Mathf.Clamp01(age / 0.7f));
            GUI.Label(new Rect(rect.x, rect.y + 76f, w, 36f), $"PARA {card.Before}  >  {card.Before + shownDelta}   (+{shownDelta})", number);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 122f, w - 32f, 70f), card.NextGoal, body);
            GUI.matrix = previous;
            if (card.First) Confetti(rect, age, fade);
            GUI.color = color;
        }

        // Deterministic little squares falling over the card: no random state, the same each time, a few seconds long.
        private static void Confetti(Rect rect, float age, float fade)
        {
            var colours = new[] { new Color(1f, 0.82f, 0.25f), new Color(0.35f, 0.8f, 1f), new Color(1f, 0.45f, 0.5f), new Color(0.5f, 0.9f, 0.5f) };
            for (var i = 0; i < 48; i++)
            {
                var seed = (uint)i * 2654435761u;
                var x = rect.x - 30f + (seed % 1000u) / 1000f * (rect.width + 60f);
                var speed = 70f + (seed / 1000u % 100u);
                var y = rect.y - 60f + (age * speed + (seed / 100000u % 60u)) % (rect.height + 120f);
                var size = 6f + (seed / 7u % 5u);
                var c = colours[i % colours.Length];
                GUI.color = new Color(c.r, c.g, c.b, fade * 0.9f);
                GUI.DrawTexture(new Rect(x, y, size, size * 0.6f), Texture2D.whiteTexture);
            }
        }
    }
}
