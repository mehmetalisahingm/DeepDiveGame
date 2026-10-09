using DeepDive.Core.Contracts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace DeepDive.Composition
{
    // Real Canvas UI. Every purchase and role change remains host validated.
    [DisallowMultipleComponent]
    public sealed class LivingBoardView : MonoBehaviour
    {
        public const KeyCode ToggleKey = KeyCode.B;
        private bool visible;
        private string status = "";
        private float statusUntil, nextRefresh;
        private ulong awaiting;
        private GameObject panel;
        private Text header, orderTitle, orderHint, sponsorTitle, sponsorHint, developmentTitle, roleTitle, statusText;
        private readonly Text[] developmentRows = new Text[4];
        private readonly Button[] developmentButtons = new Button[4];
        private readonly Button[] roleButtons = new Button[4];
        private Font font;
        public static bool PanelVisible { get; private set; }
        public ulong LastRequestId => awaiting;
        public void SetVisible(bool value) { visible = value; PanelVisible = value; }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey)) SetVisible(!visible);
            if (panel == null) CreateUi();
            panel.SetActive(visible && LivingWorldNetworkBinding.HasMirror);
            if (!panel.activeSelf) return;
            if (awaiting != 0 && LivingWorldNetworkBinding.LastResultRequest == awaiting)
            {
                status = LivingWorldNetworkBinding.LastResultAccepted ? "TAMAM" : Friendly(LivingWorldNetworkBinding.LastResultReason);
                statusUntil = Time.unscaledTime + 3f; awaiting = 0;
            }
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.15f;
            var board = LivingWorldNetworkBinding.Board;
            var development = LivingWorldNetworkBinding.Development;
            var manager = NetworkManager.Singleton;
            var me = manager != null ? manager.LocalClientId : 0UL;
            var atPc = HomePcView.PanelOpen;
            header.text = $"GUNLUK HEDEFLER [{ToggleKey}]   Gun {board.Day}";
            DrawContract(orderTitle, orderHint, "SIPARIS", board.Order);
            DrawContract(sponsorTitle, sponsorHint, "SPONSOR", board.Sponsor);
            developmentTitle.text = atPc ? "GELISIM (PC'de)" : "GELISIM (satin almak icin PC'ye git)";
            for (var i = 0; i < DevelopmentCatalog.All.Count; i++)
            {
                var item = DevelopmentCatalog.All[i]; var owned = development.Owns(item.Id);
                developmentRows[i].text = $"{item.Title}: {item.Effect}";
                developmentButtons[i].GetComponentInChildren<Text>().text = owned ? "SAHIPSIN" : item.Price.ToString();
                developmentButtons[i].interactable = !owned && atPc;
            }
            roleTitle.text = $"ROL (ucretsiz, PC'de): {CrewRoleLabels.Label(LivingWorldNetworkBinding.RoleOfClient(me))}";
            foreach (var button in roleButtons) button.interactable = atPc;
            statusText.text = Time.unscaledTime < statusUntil ? status : "";
        }

        private void CreateUi()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("LivingBoardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
            panel = new GameObject("DailyBoardPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-20f, 20f); rect.sizeDelta = new Vector2(440f, 390f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.07f, 0.09f, 0.96f);
            header = Label("Header", 12, 12, 416, 26, 17);
            orderTitle = Label("OrderTitle", 12, 44, 416, 22, 15);
            orderHint = Label("OrderHint", 12, 68, 416, 38, 14);
            sponsorTitle = Label("SponsorTitle", 12, 108, 416, 22, 15);
            sponsorHint = Label("SponsorHint", 12, 132, 416, 38, 14);
            developmentTitle = Label("DevelopmentTitle", 12, 174, 416, 22, 14);
            for (var i = 0; i < DevelopmentCatalog.All.Count; i++)
            {
                var item = DevelopmentCatalog.All[i]; var id = item.Id;
                developmentRows[i] = Label("Development-" + id, 12, 198 + i * 25, 300, 23, 12);
                developmentButtons[i] = MakeButton(id, item.Price.ToString(), 322, 198 + i * 25, 104, () => awaiting = LivingWorldNetworkBinding.RequestBuild(id));
            }
            roleTitle = Label("RoleTitle", 12, 304, 416, 22, 14);
            var roles = new[] { CrewRole.CameraOperator, CrewRole.Hunter, CrewRole.Explorer, CrewRole.Carrier };
            for (var i = 0; i < roles.Length; i++)
            {
                var role = roles[i]; roleButtons[i] = MakeButton("Role-" + role, CrewRoleLabels.Label(role), 12 + i * 104, 330, 98,
                    () => awaiting = LivingWorldNetworkBinding.RequestRole(role));
            }
            statusText = Label("Status", 12, 359, 416, 22, 14);
            panel.SetActive(false);
        }

        private Text Label(string name, float x, float y, float width, float height, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(panel.transform, false);
            Place(go.GetComponent<RectTransform>(), x, y, width, height);
            var text = go.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = new Color(0.9f, 0.95f, 0.97f);
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Button MakeButton(string name, string title, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(panel.transform, false);
            Place(go.GetComponent<RectTransform>(), x, y, width, 23);
            go.GetComponent<Image>().color = new Color(0.12f, 0.4f, 0.46f);
            var text = Label(name + "-Label", 0, 0, width, 23, 13); text.transform.SetParent(go.transform, false);
            Place(text.rectTransform, 0, 0, width, 23); text.alignment = TextAnchor.MiddleCenter; text.text = title;
            var button = go.GetComponent<Button>(); button.onClick.AddListener(action); return button;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }

        private static void DrawContract(Text title, Text hint, string label, in ContractState state)
        {
            if (!state.HasContract || !ContractCatalog.TryGet(state.TemplateId, out var template))
            { title.text = label + ": bugun yok"; hint.text = ""; return; }
            title.text = $"{label}: {template.Title} (+{template.Reward}) {(state.Status == ContractStatus.Completed ? "TAMAM" : "acik")}";
            hint.text = $"{template.Hint} [{Format(template, state.Progress)}/{Format(template, template.Target)}]";
        }
        private static string Format(in ContractTemplate template, int value) => template.Measure == ContractMeasure.GramsSold ? $"{value / 1000f:0.0} kg" : value.ToString();
        private static string Friendly(string reason) => reason switch
        {
            "NotAtPc" => "PC'DEN UZAKSIN", "DayClosing" => "GUN KAPANIYOR", "WrongPhase" => "DALISTA DEGISMEZ",
            "InsufficientFunds" => "YETERSIZ PARA", "AlreadyProcessed" => "ZATEN VAR", "PlayerInactive" => "OYUNCU AKTIF DEGIL",
            "SaveFailed" => "KAYIT HATASI", "InvalidTarget" => "GECERSIZ SECIM", _ => string.IsNullOrWhiteSpace(reason) ? "ISLEM REDDEDILDI" : reason
        };
        private void OnDisable() { PanelVisible = false; if (panel != null) panel.SetActive(false); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<LivingBoardView>() != null) return;
            var go = new GameObject("LivingBoardView"); DontDestroyOnLoad(go); go.AddComponent<LivingBoardView>();
        }
    }
}
