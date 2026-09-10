using System.Text;
using DeepDive.Core.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepDive.Inventory.UI
{
    // "Av alindi / canta dolu / guvenle dondun" feedback for issue P2-C (#22). Builds a
    // minimal uGUI panel at runtime (same approach as DeepDive.Session.UI.SessionRoomUI) and
    // drives it from InventoryManager events. LocalPlayer is set by composition once the real
    // pickup-request transport (Mehmet's P2-A) exists; until then it can be set for local
    // testing directly.
    [RequireComponent(typeof(InventoryManager))]
    public class InventoryUI : MonoBehaviour
    {
        public PlayerId? LocalPlayer;

        private InventoryManager _inventory;
        private Text _bagLabel;
        private Text _statusLabel;

        private void Start()
        {
            _inventory = GetComponent<InventoryManager>();
            BuildUi();
            _inventory.OnBagChanged += HandleBagChanged;
            _inventory.OnDiveSummaryReady += HandleDiveSummary;
            RefreshBag();
        }

        private void OnDestroy()
        {
            if (_inventory == null)
                return;
            _inventory.OnBagChanged -= HandleBagChanged;
            _inventory.OnDiveSummaryReady -= HandleDiveSummary;
        }

        private void HandleBagChanged(PlayerId player)
        {
            if (LocalPlayer.HasValue && player.Equals(LocalPlayer.Value))
                RefreshBag();
        }

        private void RefreshBag()
        {
            if (_bagLabel == null)
                return;
            if (!LocalPlayer.HasValue || !_inventory.Bags.TryGetValue(LocalPlayer.Value, out var bag))
            {
                _bagLabel.text = "Canta: -";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Canta: {bag.WeightGrams}g / {InventoryManager.CapacityGrams}g");
            sb.AppendLine(bag.SafelyReturned ? "Durum: guvenle dondun" : "Durum: dalista");
            foreach (var item in bag.Items)
                sb.AppendLine($"  - {item.SpeciesId} ({item.WeightGrams}g)");
            _bagLabel.text = sb.ToString();
        }

        private void HandleDiveSummary(DiveSummary summary)
        {
            SetStatus($"Dalis ozeti: guvenle donen {summary.SafelyReturned.Count}, " +
                      $"korunan av {summary.PreservedCaptureIds.Count}, kayip {summary.LostCaptureIds.Count}");
        }

        // Feedback for a single pickup attempt; composition calls this after TryAddCatch.
        public void ShowPickupResult(InventoryActionResult result)
        {
            switch (result)
            {
                case InventoryActionResult.Ok: SetStatus("Av alindi"); break;
                case InventoryActionResult.InventoryFull: SetStatus("Canta dolu"); break;
                case InventoryActionResult.AlreadyClaimed: SetStatus("Av zaten alindi"); break;
                default: SetStatus(result.ToString()); break;
            }
            RefreshBag();
        }

        private void SetStatus(string message)
        {
            if (_statusLabel != null)
                _statusLabel.text = message;
            Debug.Log("[P2Inventory] " + message);
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

            var canvasGo = new GameObject("P2InventoryCanvas");
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
            panelRect.anchorMin = new Vector2(1, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 0.5f);
            panelRect.sizeDelta = new Vector2(360, 0);
            panelRect.anchoredPosition = new Vector2(-20, 0);
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

            _bagLabel = AddLabel(panel.transform, "BagLabel", "Canta: -");
            _statusLabel = AddLabel(panel.transform, "StatusLabel", string.Empty);
        }

        private static Text AddLabel(Transform parent, string name, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 20;
            return label;
        }
    }
}
