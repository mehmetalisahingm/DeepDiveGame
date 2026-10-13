using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Session;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace DeepDive.Composition
{
    // #156: the boat-repair line under the existing "SANDAL ONARIM n/3" box. While the sandal is not repaired it names each missing part and
    // points at it (compass + metres from the local player). Reads the host's mirrored mask and Utku's scene anchors; it never decides anything.
    [DisallowMultipleComponent]
    public sealed class BoatRepairHintView : MonoBehaviour
    {
        private const float Interval = 0.25f;
        private readonly List<string> lines = new List<string>();
        private readonly Dictionary<string, BoatPartAnchor> anchors = new Dictionary<string, BoatPartAnchor>();
        private SessionNetworkAdapter adapter;
        private float next, nextAnchors;
        private string note = "";

        public IReadOnlyList<string> Lines => lines;
        public string Note => note;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() =>
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
            {
                var session = FindFirstObjectByType<SessionNetworkAdapter>();
                if (session != null && session.GetComponent<BoatRepairHintView>() == null) session.gameObject.AddComponent<BoatRepairHintView>();
            };

        private void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Interval;
            Refresh();
        }

        private void Refresh()
        {
            lines.Clear(); note = "";
            var manager = NetworkManager.Singleton;
            var player = manager != null ? manager.LocalClient?.PlayerObject : null;
            var progress = player != null ? player.GetComponent<EconomyPlayerSync>() : null;
            if (progress == null || !progress.IsSpawned) return;
            var missing = BoatRepairGuide.Missing(progress.BoatPartsMask.Value);
            if (missing.Count == 0) return;
            if (adapter == null) adapter = GetComponent<SessionNetworkAdapter>();
            var phase = adapter != null ? adapter.Session.State.Phase : SessionPhase.Lobby;
            if (phase == SessionPhase.Lobby) return;   // the coast is not loaded in the home scene

            if (Time.unscaledTime >= nextAnchors)
            {
                nextAnchors = Time.unscaledTime + 1f;
                anchors.Clear();
                foreach (var anchor in FindObjectsByType<BoatPartAnchor>(FindObjectsSortMode.None)) anchors[anchor.PartId] = anchor;
            }
            var at = player.transform.position;
            foreach (var part in missing)
            {
                if (anchors.TryGetValue(part, out var anchor) && anchor != null)
                    lines.Add(BoatRepairGuide.Line(part, at.x, at.z, anchor.WorldPosition.x, anchor.WorldPosition.z));
                else lines.Add(BoatRepairGuide.PartName(part) + ": kiyida");
            }
            note = phase == SessionPhase.Dive ? BoatRepairGuide.DiveNote : BoatRepairGuide.OutsideDiveNote;
        }

        // A real Canvas (not IMGUI): it is drawn by the camera like the other P4 panels, so a captured frame shows what the player sees.
        private GameObject panel;
        private Text text;

        public bool PanelVisible => panel != null && panel.activeSelf;

        private void LateUpdate()
        {
            var show = lines.Count > 0;
            if (show && panel == null) BuildPanel();
            if (panel == null) return;
            if (panel.activeSelf != show) panel.SetActive(show);
            if (!show) return;
            text.text = "SANDAL: EKSIK PARCALAR\n" + string.Join("\n", lines) + "\n" + note;
        }

        private void BuildPanel()
        {
            var canvasObject = new GameObject("BoatRepairHintCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -306f);
            rect.sizeDelta = new Vector2(330f, 108f);
            var background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(panel.transform, false);
            text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 6f); textRect.offsetMax = new Vector2(-10f, -6f);
            panel.SetActive(false);
        }
    }
}
