using System.Text;
using DeepDive.Core.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepDive.Session.UI
{
    // Mert's room UI uses the real connection/command bridge. No synthetic player is joined.
    [RequireComponent(typeof(SessionManager))]
    public class SessionRoomUI : MonoBehaviour
    {
        public ISessionControls Controls { get; set; }

        private SessionManager _session;
        private InputField _address, _port;
        private Button _hostButton, _joinButton, _leaveButton;
        private GameObject _canvas;

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
        }

        private void Start()
        {
            BuildUi();

            _session.OnSessionStateChanged += RefreshPhase;
            _session.OnRosterChanged += RefreshRoster;

            if (Controls != null) Controls.Changed += RefreshConnection;

            RefreshPhase(_session.State);
            RefreshRoster(_session.Roster);
            RefreshConnection();
        }

        private void OnDestroy()
        {
            if (_session == null)
                return;
            _session.OnSessionStateChanged -= RefreshPhase;
            _session.OnRosterChanged -= RefreshRoster;
            if (Controls != null) Controls.Changed -= RefreshConnection;
        }

        private void OnToggleReady()
        {
            Controls?.ToggleReady();
        }

        private void OnBeginPrep() => Controls?.AdvancePhase();
        private void OnBeginDive() => Controls?.AdvancePhase();
        private void OnBeginReturn() => Controls?.AdvancePhase();
        private void OnCompleteReturn() => Controls?.AdvancePhase();

        private void Connect(bool host)
        {
            if (Controls == null) { SetStatus("Ag baglantisi kurulmamıs."); return; }
            if (!ushort.TryParse(_port.text, out var port) || port == 0)
            { SetStatus("Port 1-65535 olmali."); return; }
            if (host) Controls.CreateRoom(port); else Controls.JoinRoom(_address.text.Trim(), port);
            RefreshConnection();
        }

        private void RefreshConnection()
        {
            var connection = Controls?.Connection;
            var offline = connection != null && connection.Status == ConnectionStatus.Offline;
            SetInteractable(_hostButton, offline); SetInteractable(_joinButton, offline);
            SetInteractable(_leaveButton, connection != null && !offline);
            if (_address != null) _address.interactable = offline;
            if (_port != null) _port.interactable = offline;
            if (_address != null) _address.gameObject.SetActive(offline);
            if (_port != null) _port.gameObject.SetActive(offline);
            if (_hostButton != null) _hostButton.gameObject.SetActive(offline);
            if (_joinButton != null) _joinButton.gameObject.SetActive(offline);
            if (_leaveButton != null) _leaveButton.gameObject.SetActive(!offline && connection != null);
            if (_statusLabel != null)
                _statusLabel.text = connection == null ? "Ag baglantisi yok; PrepArea sahnesini acin." :
                    $"{connection.Status}  {connection.LastError}\n{Controls.LastAction}";
            RefreshPhase(_session.State);
        }

        private void Update()
        {
            // F1 captures the game; Esc releases it and shows the room controls again.
            if (_canvas != null) _canvas.SetActive(Cursor.lockState != CursorLockMode.Locked);
        }

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

            var connection = Controls?.Connection;
            var connected = connection != null && connection.Status == ConnectionStatus.Connected && !connection.IsSceneLoading;
            var host = connected && connection.IsHost;
            var lobby = state.Phase == SessionPhase.Lobby;
            var allReady = _session.Roster.Count > 0;
            foreach (var ready in _session.Roster.Values) allReady &= ready;
            SetInteractable(_readyButton, connected && lobby);
            SetInteractable(_beginPrepButton, host && lobby && allReady);
            SetInteractable(_beginDiveButton, host && state.Phase == SessionPhase.Prep);
            SetInteractable(_beginReturnButton, host && state.Phase == SessionPhase.Dive);
            SetInteractable(_completeReturnButton, host && state.Phase == SessionPhase.Return);
            Show(_readyButton, connected && lobby);
            Show(_beginPrepButton, host && lobby);
            Show(_beginDiveButton, host && state.Phase == SessionPhase.Prep);
            Show(_beginReturnButton, host && state.Phase == SessionPhase.Dive);
            Show(_completeReturnButton, host && state.Phase == SessionPhase.Return);
        }

        private void RefreshRoster(System.Collections.Generic.IReadOnlyDictionary<PlayerId, bool> roster)
        {
            if (_rosterLabel == null)
                return;

            var sb = new StringBuilder("Oyuncular:\n");
            foreach (var entry in roster)
                sb.AppendLine($"  {entry.Key} - {(entry.Value ? "Hazir" : "Hazir degil")}");
            _rosterLabel.text = sb.ToString();
            RefreshPhase(_session.State);
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
                esGo.transform.SetParent(transform, false);
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("P1SessionCanvas");
            _canvas = canvasGo;
            canvasGo.transform.SetParent(transform, false);
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
            _address = AddInput(panel.transform, "HostAddress", "127.0.0.1");
            _port = AddInput(panel.transform, "Port", "7777");
            _hostButton = AddButton(panel.transform, "HostButton", "Oda olustur / Solo", () => Connect(true));
            _joinButton = AddButton(panel.transform, "JoinButton", "IP ile katil", () => Connect(false));
            _leaveButton = AddButton(panel.transform, "LeaveButton", "Odadan ayril", () => Controls?.LeaveRoom());
            _rosterLabel = AddLabel(panel.transform, "RosterLabel", "Oyuncular:");
            _readyButton = AddButton(panel.transform, "ReadyButton", "Hazir / Hazir degil", OnToggleReady);
            _beginPrepButton = AddButton(panel.transform, "BeginPrepButton", "Hazirliga basla (Lobby->Prep)", OnBeginPrep);
            _beginDiveButton = AddButton(panel.transform, "BeginDiveButton", "Dalisa basla (Prep->Dive)", OnBeginDive);
            _beginReturnButton = AddButton(panel.transform, "BeginReturnButton", "Donusu basla (Dive->Return)", OnBeginReturn);
            _completeReturnButton = AddButton(panel.transform, "CompleteReturnButton", "Odaya don (Return->Lobby)", OnCompleteReturn);
            _statusLabel = AddLabel(panel.transform, "StatusLabel", string.Empty);
            AddLabel(panel.transform, "ControlsLabel", "F1: oyna | Esc: menu\nWASD: hareket | Fare: bakis\nSpace / Ctrl: yuzme yukari / asagi\nE: host giris/cikis noktasini kullanir");
        }

        private static void Show(Button button, bool visible)
        { if (button != null) button.gameObject.SetActive(visible); }

        private static InputField AddInput(Transform parent, string name, string value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.1f, 0.17f, 0.22f);
            var field = go.GetComponent<InputField>();
            var label = AddLabel(go.transform, "Text", "");
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4); rect.offsetMax = new Vector2(-8, -4);
            field.textComponent = label; field.text = value; field.characterLimit = 45;
            go.AddComponent<LayoutElement>().minHeight = 34;
            return field;
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
