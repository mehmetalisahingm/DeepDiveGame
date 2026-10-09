using System.Collections.Generic;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Composition
{
    // P4.5-C (#132) the daily goals board, the development list and the free role choice, as one IMGUI panel (key B). Everything shown is the
    // host's mirrored state; every action is a request the HOST decides (it must see the sender at the home PC outside a dive). Contract
    // progress cannot be clicked: only a settled fish hand-in or an accepted publication moves it.
    [DisallowMultipleComponent]
    public sealed class LivingBoardView : MonoBehaviour
    {
        public const KeyCode ToggleKey = KeyCode.B;
        private const float Width = 420f;

        private bool visible;
        private string status = "";
        private float statusUntil;
        private ulong awaiting;

        public static bool PanelVisible { get; private set; }

        public void SetVisible(bool value) { visible = value; PanelVisible = value; }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey)) visible = !visible;
            PanelVisible = visible;

            // The host's answer to this process's last request.
            if (awaiting != 0 && LivingWorldNetworkBinding.LastResultRequest == awaiting)
            {
                status = LivingWorldNetworkBinding.LastResultAccepted ? "TAMAM" : Friendly(LivingWorldNetworkBinding.LastResultReason);
                statusUntil = Time.unscaledTime + 3f;
                awaiting = 0;
            }
        }

        private void OnGUI()
        {
            if (!visible || !LivingWorldNetworkBinding.HasMirror) return;
            var board = LivingWorldNetworkBinding.Board;
            var development = LivingWorldNetworkBinding.Development;
            var manager = NetworkManager.Singleton;
            var me = manager != null ? manager.LocalClientId : 0UL;
            var atPc = HomePcView.PanelOpen;

            var rect = new Rect(Screen.width - Width - 20f, Mathf.Max(10f, Mathf.Min(290f, Screen.height - 400f)), Width, 390f);
            GUI.Box(rect, $"GUNLUK HEDEFLER [{ToggleKey}]   gun {board.Day}");
            var x = rect.x + 10f;
            var y = rect.y + 24f;

            y = DrawContract(x, y, "SIPARIS", board.Order);
            y = DrawContract(x, y, "SPONSOR", board.Sponsor);

            GUI.Label(new Rect(x, y, Width - 20f, 20f), atPc ? "GELISIM (PC'de):" : "GELISIM (satin almak icin PC'ye git):");
            y += 20f;
            for (var i = 0; i < DevelopmentCatalog.All.Count; i++)
            {
                var d = DevelopmentCatalog.All[i];
                var owned = development.Owns(d.Id);
                GUI.Label(new Rect(x, y, Width - 134f, 18f), $"{d.Title}: {d.Effect}");
                if (owned) GUI.Label(new Rect(x + Width - 124f, y, 100f, 18f), "SAHIPSIN");
                else if (GUI.Button(new Rect(x + Width - 124f, y, 100f, 18f), d.Price.ToString())) awaiting = LivingWorldNetworkBinding.RequestBuild(d.Id);
                y += 20f;
            }

            y += 4f;
            GUI.Label(new Rect(x, y, Width - 20f, 18f), $"ROL (ucretsiz, PC'de): {CrewRoleLabels.Label(LivingWorldNetworkBinding.RoleOfClient(me))}");
            y += 20f;
            var bx = x;
            foreach (var role in new[] { CrewRole.CameraOperator, CrewRole.Hunter, CrewRole.Explorer, CrewRole.Carrier })
            {
                if (GUI.Button(new Rect(bx, y, 74f, 20f), CrewRoleLabels.Label(role))) awaiting = LivingWorldNetworkBinding.RequestRole(role);
                bx += 78f;
            }
            y += 24f;
            if (Time.unscaledTime < statusUntil && status.Length > 0) GUI.Label(new Rect(x, y, Width - 20f, 20f), status);
        }

        private static float DrawContract(float x, float y, string label, in ContractState state)
        {
            if (!state.HasContract || !ContractCatalog.TryGet(state.TemplateId, out var template))
            {
                GUI.Label(new Rect(x, y, Width - 20f, 18f), $"{label}: bugun yok");
                return y + 22f;
            }
            var done = state.Status == ContractStatus.Completed;
            GUI.Label(new Rect(x, y, Width - 20f, 18f), $"{label}: {template.Title}  (+{template.Reward})  {(done ? "TAMAM" : "acik")}");
            var hintStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUI.Label(new Rect(x + 8f, y + 18f, Width - 28f, 36f), $"{template.Hint}  [{Format(template, state.Progress)}/{Format(template, template.Target)}]", hintStyle);
            return y + 58f;
        }

        private static string Format(in ContractTemplate template, int value) =>
            template.Measure == ContractMeasure.GramsSold ? $"{value / 1000f:0.0} kg" : value.ToString();

        private static string Friendly(string reason) => reason switch
        {
            "NotAtPc" => "PC'DEN UZAKSIN",
            "DayClosing" => "GUN KAPANIYOR",
            "WrongPhase" => "DALISTA DEGISMEZ",
            "InsufficientFunds" => "YETERSIZ PARA",
            "AlreadyProcessed" => "ZATEN VAR",
            "PlayerInactive" => "OYUNCU AKTIF DEGIL",
            "SaveFailed" => "KAYIT HATASI",
            "InvalidTarget" => "GECERSIZ SECIM",
            _ => string.IsNullOrWhiteSpace(reason) ? "ISLEM REDDEDILDI" : reason
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<LivingBoardView>() != null) return;
            var go = new GameObject("LivingBoardView");
            DontDestroyOnLoad(go);
            go.AddComponent<LivingBoardView>();
        }
    }
}
