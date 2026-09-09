using System.Text;
using DeepDive.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepDive.Session.UI
{
    // Room/player list and ready screen for issue P1-C (#14). Builds a minimal uGUI layout at
    // runtime (no hand-authored prefab/scene UI hierarchy) and drives it from SessionManager.
    // Standalone/local-player demo only: this is the solo test harness described in the task,
    // not the final in-game HUD. The "Dalisa basla / Dalisi bitir / Donusu tamamla" buttons let
    // one person walk the full Lobby -> Prep -> Dive -> Return -> Lobby cycle without a network.
    [RequireComponent(typeof(SessionManager))]
    public class SessionRoomUI : MonoBehaviour
    {
        [SerializeField] private string localPlayerId = "local-mert";

        private SessionManager _session;
        private PlayerId _localPlayer;

        private Text _phaseLabel;
        private Text _rosterLabel;
        private Text _statusLabel;
        private Button _readyButton;
        private Button _beginPrepButton;
        private Button _beginDiveButton;
        private Button _beginReturnButton;
        private Button _completeReturnButton;

        private void Awake()
        {
            _session = GetComponent<SessionManager>();
            _localPlayer = new PlayerId(localPlayerId);
        }

        private void Start()
        {
            BuildUi();

            _session.OnSessionStateChanged += RefreshPhase;
            _session.OnRosterChanged += RefreshRoster;

            _session.Initialize(sessionId: "solo-test", regionId: "P1Session");
            _session.Join(_localPlayer);

            RefreshPhase(_session.State);
            RefreshRoster(_session.Roster);
        }

        private void OnDestroy()
        {
            if (_session == null)
                return;
            _session.OnSessionStateChanged -= RefreshPhase;
            _session.OnRosterChanged -= RefreshRoster;
        }

        private void OnToggleReady()
        {
            var currentlyReady = _session.Roster.TryGetValue(_localPlayer, out var ready) && ready;
            var result = _session.SetReady(_localPlayer, !currentlyReady);
            SetStatus($"SetReady -> {result}");
        }

        private void OnBeginPrep() => SetStatus($"BeginPrep -> {_session.BeginPrep()}");
        private void OnBeginDive() => SetStatus($"BeginDive -> {_session.BeginDive(diveId: "solo-dive-1")}");
        private void OnBeginReturn() => SetStatus($"BeginReturn -> {_session.BeginReturn()}");
        private void OnCompleteReturn() => SetStatus($"CompleteReturn -> {_session.CompleteReturn()}");

        private void SetStatus(string message)
        {
            if (_statusLabel != null)
                _statusLabel.text = message;
            Debug.Log("[P1Session] " + message);
        }

        private void RefreshPhase(SessionState state)
        {
            if (_phaseLabel != null)
                _phaseLabel.text = $"Asama: {state.Phase}  (session={state.SessionId} rev={state.Revision})";

            var lobby = state.Phase == SessionPhase.Lobby;
            SetInteractable(_readyButton, lobby);
            SetInteractable(_beginPrepButton, lobby);
            SetInteractable(_beginDiveButton, state.Phase == SessionPhase.Prep);
            SetInteractable(_beginReturnButton, state.Phase == SessionPhase.Dive);
            SetInteractable(_completeReturnButton, state.Phase == SessionPhase.Return);
        }

        private void RefreshRoster(System.Collections.Generic.IReadOnlyDictionary<PlayerId, bool> roster)
        {
            if (_rosterLabel == null)
                return;

            var sb = new StringBuilder("Oyuncular:\n");
            foreach (var entry in roster)
                sb.AppendLine($"  {entry.Key} - {(entry.Value ? "Hazir" : "Hazir degil")}");
            _rosterLabel.text = sb.ToString();
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private void BuildUi()
        {
            if (EventSystem.current == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("P1SessionCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 0.5f);
            panelRect.sizeDelta = new Vector2(420, 0);
            panelRect.anchoredPosition = new Vector2(20, 0);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.55f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 8;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _phaseLabel = AddLabel(panel.transform, "PhaseLabel", "Asama: -");
            _rosterLabel = AddLabel(panel.transform, "RosterLabel", "Oyuncular:");
            _readyButton = AddButton(panel.transform, "ReadyButton", "Hazir / Hazir degil", OnToggleReady);
            _beginPrepButton = AddButton(panel.transform, "BeginPrepButton", "Hazirliga basla (Lobby->Prep)", OnBeginPrep);
            _beginDiveButton = AddButton(panel.transform, "BeginDiveButton", "Dalisa basla (Prep->Dive)", OnBeginDive);
            _beginReturnButton = AddButton(panel.transform, "BeginReturnButton", "Donusu basla (Dive->Return)", OnBeginReturn);
            _completeReturnButton = AddButton(panel.transform, "CompleteReturnButton", "Odaya don (Return->Lobby)", OnCompleteReturn);
            _statusLabel = AddLabel(panel.transform, "StatusLabel", string.Empty);
        }

        private static Text AddLabel(Transform parent, string name, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 24;
            return label;
        }

        private static Button AddButton(Transform parent, string name, string text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.45f, 0.5f, 1f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 32;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
