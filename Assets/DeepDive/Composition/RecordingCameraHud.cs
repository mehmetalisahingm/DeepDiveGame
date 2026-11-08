using DeepDive.Core.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace DeepDive.Composition
{
    // P4/#253: countdown comes from the exact tier limit used by the host.
    // This is display-only; the server remains the sole authority for stopping and archiving.
    public readonly struct RecordingCountdownState
    {
        public readonly int RemainingSeconds;
        public readonly float RemainingRatio;
        public readonly bool WarnLastSeconds;
        public readonly bool ReachedLimit;
        public readonly int LimitSeconds;

        public RecordingCountdownState(int remainingSeconds, float remainingRatio, bool warnLastSeconds,
            bool reachedLimit, int limitSeconds)
        {
            RemainingSeconds = remainingSeconds;
            RemainingRatio = remainingRatio;
            WarnLastSeconds = warnLastSeconds;
            ReachedLimit = reachedLimit;
            LimitSeconds = limitSeconds;
        }
    }

    public static class RecordingCountdownRules
    {
        public static RecordingCountdownState Evaluate(CameraTier tier, bool recording, float elapsed)
        {
            var limit = Mathf.Max(1f, CameraRecordingLimits.MaxSeconds(tier));
            var safeElapsed = float.IsNaN(elapsed) || float.IsInfinity(elapsed)
                ? 0f : Mathf.Max(0f, elapsed);
            var remaining = recording ? Mathf.CeilToInt(Mathf.Max(0f, limit - safeElapsed)) : Mathf.CeilToInt(limit);
            var ratio = recording ? Mathf.Clamp01((limit - safeElapsed) / limit) : 1f;
            return new RecordingCountdownState(remaining, ratio,
                recording && remaining > 0 && remaining <= 5,
                recording && remaining == 0, Mathf.CeilToInt(limit));
        }
    }

    // Owner-camera UI only. Layer 5 is excluded from the separate archive capture camera.
    internal sealed class RecordingCameraHud : MonoBehaviour
    {
        private GameObject root;
        private Canvas canvas;
        private RectTransform frame;
        private Text tally, timer, tierLabel, controls, status;
        private Image dot, countdownFill;
        private Texture2D dotTexture;
        private Sprite dotSprite;
        private readonly Color red = new Color(1f, .18f, .22f);

        public void Hide() { if (root != null) root.SetActive(false); }
        public void Show(Camera camera, bool recording, float elapsed, CameraTier tier, string message)
        {
            if (camera == null) { Hide(); return; }
            if (root == null) Build();
            root.SetActive(true);
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + .01f;
            // Existing oxygen/weather warnings use screen pixels; keep the tally below them.
            frame.offsetMax = new Vector2(-30f, -Mathf.Max(132f, 142f / Mathf.Max(.1f, canvas.scaleFactor)));
            tally.text = recording ? "REC" : "STBY";
            tally.color = recording ? red : Color.white;
            dot.color = recording ? new Color(red.r, red.g, red.b, .65f + .35f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * 5)))
                : new Color(.6f, .7f, .73f);
            var clock = RecordingCountdownRules.Evaluate(tier, recording, elapsed);
            timer.text = recording ? $"{clock.RemainingSeconds:00} sn KALDI" :
                $"LIMIT {clock.LimitSeconds:00} sn";
            // A Unity UI Image with no sprite does not implement Filled geometry.
            // Simple Image + physical RectTransform width renders an actual shrinking bar.
            countdownFill.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, 206f * clock.RemainingRatio);
            countdownFill.color = clock.WarnLastSeconds || clock.ReachedLimit ? red :
                recording ? new Color(.42f, .94f, .78f) : new Color(.60f, .75f, .81f);
            tierLabel.text = "CAM / " + (tier == CameraTier.Professional ? "PRO" : tier == CameraTier.Advanced ? "ADVANCED" : "BASIC");
            controls.text = recording ? "R  KAYDI DURDUR" : "R  KAYDI BASLAT     C  KAMERAYI KAPAT";
            status.text = !string.IsNullOrEmpty(message) ? message :
                clock.ReachedLimit ? "SURE DOLDU - SUNUCU KAYDI SONLANDIRIYOR" :
                clock.WarnLastSeconds ? "SON 5 SANIYE - KAYIT OTOMATIK BITECEK" : "";
        }
        private void Build()
        {
            root = new GameObject("OwnerCameraViewfinder", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.layer = 5; root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.sortingOrder = 90;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            frame = CreateRect(root.transform, "OpenViewfinder", 0, 0, 0, 0);
            frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
            frame.offsetMin = new Vector2(30, 108); frame.offsetMax = new Vector2(-30, -142);
            var ink = new Color(.91f, .96f, 1f, .8f);
            Corners(frame, 28, ink);
            var chip = CreateRect(frame, "TallyPanel", 14, 14, 228, 42);
            Panel(chip, new Color(.02f, .035f, .045f, .78f));
            var pixels = new Color[32 * 32];
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
                pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Clamp01(15.5f - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f))));
            dotTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "CameraTallyLight" };
            dotTexture.SetPixels(pixels); dotTexture.Apply();
            dotSprite = Sprite.Create(dotTexture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f));
            dot = Panel(CreateRect(chip, "RecordingLight", 13, 14, 13, 13), red); dot.sprite = dotSprite;
            tally = Label(CreateRect(chip, "RecordingState", 37, 2, 76, 38), 22, FontStyle.Bold);
            timer = Label(CreateRect(chip, "ElapsedTime", 116, 2, 110, 38), 17, FontStyle.Bold);
            var countdownTrack = CreateRect(chip, "TimeRemainingTrack", 12, 36, 206, 4);
            Panel(countdownTrack, new Color(.20f, .29f, .33f, 1f));
            var countdownBar = CreateRect(countdownTrack, "TimeRemainingFill", 0, 0, 206, 4);
            countdownFill = Panel(countdownBar, new Color(.42f, .94f, .78f));
            countdownFill.type = Image.Type.Simple;
            var tier = CreateRect(frame, "CameraModel", -165, 22, 150, 26);
            tier.anchorMin = tier.anchorMax = new Vector2(1, 1);
            tierLabel = Label(tier, 14);
            var focus = CreateRect(root.transform, "FocusBrackets", 0, 0, 48, 48);
            focus.anchorMin = focus.anchorMax = new Vector2(.5f, .5f); focus.pivot = new Vector2(.5f, .5f);
            Corners(focus, 11, new Color(.6f, .94f, .8f, .85f));
            var footer = CreateRect(frame, "ControlsPanel", 14, 40, 370, 28);
            footer.anchorMin = footer.anchorMax = Vector2.zero; footer.anchoredPosition = new Vector2(14, 40);
            Panel(footer, new Color(.02f, .035f, .045f, .72f));
            controls = Label(CreateRect(footer, "Controls", 10, 0, 355, 28), 14);
            var notice = CreateRect(frame, "CameraStatus", -180, 74, 360, 28);
            notice.anchorMin = notice.anchorMax = new Vector2(.5f, 0); notice.anchoredPosition = new Vector2(-180, 74);
            status = Label(notice, 14); status.alignment = TextAnchor.MiddleCenter;
        }
        private static RectTransform CreateRect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        private static Image Panel(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        private static Text Label(RectTransform rect, int size, FontStyle style = FontStyle.Normal)
        {
            var text = rect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.fontStyle = style; text.color = Color.white; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.raycastTarget = false; return text;
        }
        private static void Corners(RectTransform parent, float length, Color color)
        {
            foreach (var right in new[] { false, true }) foreach (var top in new[] { false, true })
            {
                var a = new Vector2(right ? 1 : 0, top ? 1 : 0);
                var h = CreateRect(parent, "ViewfinderHorizontal", 0, 0, length, 2);
                h.anchorMin = h.anchorMax = h.pivot = a; h.anchoredPosition = Vector2.zero; Panel(h, color);
                var v = CreateRect(parent, "ViewfinderVertical", 0, 0, 2, length);
                v.anchorMin = v.anchorMax = v.pivot = a; v.anchoredPosition = Vector2.zero; Panel(v, color);
            }
        }
        private void OnDestroy()
        { if (root != null) Destroy(root); if (dotSprite != null) Destroy(dotSprite); if (dotTexture != null) Destroy(dotTexture); }
    }
}
