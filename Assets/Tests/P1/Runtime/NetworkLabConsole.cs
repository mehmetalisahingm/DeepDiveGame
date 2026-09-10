using DeepDive.Core.Contracts;
using DeepDive.Network;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // Explicit development-only controls. Not Mert's lobby or SessionState implementation.
    public sealed class NetworkLabConsole : MonoBehaviour
    {
        public Camera OfflineCamera;
        private NetworkSession session;
        private string address = "127.0.0.1";
        private string port = "7777";
        private ulong requestId;
        private bool reopenAfterLoad;
        private void Start()
        {
            session = GetComponent<NetworkSession>();
            session.SceneLoaded += Loaded;
            Application.runInBackground = true;
        }
        private void Loaded(SceneLoadResult result)
        {
            if (result.Succeeded && reopenAfterLoad) session.SetJoinAllowed(true);
            reopenAfterLoad = false;
        }
        private void Update()
        {
            if (OfflineCamera != null && session != null)
                OfflineCamera.gameObject.SetActive(session.LocalPlayerId == null);
        }
        private void OnGUI()
        {
            if (session == null || Application.isBatchMode) return;
            GUILayout.BeginArea(new Rect(16, 16, 360, 420), GUI.skin.box);
            GUILayout.Label("P1-A BAGLANTI TESTI / GELISTIRME BUILD'I");
            GUILayout.Label("Durum: " + session.Status + " | Oyuncu: " + session.Players.Count + "/4");
            if (session.Status == ConnectionStatus.Offline)
            {
                GUILayout.Label("Host IP"); address = GUILayout.TextField(address);
                GUILayout.Label("UDP port"); port = GUILayout.TextField(port);
                var valid = ushort.TryParse(port, out var parsed) && parsed > 0;
                GUI.enabled = valid;
                if (GUILayout.Button("Oda olustur / Solo")) session.StartHost(parsed);
                if (GUILayout.Button("Katil")) session.Join(address, parsed);
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("F1: kontrolu al | WASD: hareket");
                GUILayout.Label("Fare: bakis | Space/Ctrl: yuzme | Esc: imlec");
                foreach (var player in session.Players) GUILayout.Label("Oyuncu " + player);
                GUI.enabled = session.IsHost && !session.IsSceneLoading;
                if (GUILayout.Button("Test: sualti sahnesini yukle"))
                    session.TryLoadScene("P1NetworkWaterLab", ++requestId);
                if (GUILayout.Button("Test: hazirlik sahnesine don"))
                {
                    reopenAfterLoad = true;
                    if (!session.TryLoadScene("P1NetworkLab", ++requestId)) reopenAfterLoad = false;
                }
                GUI.enabled = true;
                if (GUILayout.Button("Ayril")) session.Leave();
            }
            if (!string.IsNullOrEmpty(session.LastError)) GUILayout.Label("Bilgi: " + session.LastError);
            GUILayout.Label("Bu alan/ekran yalniz ag testi icindir.");
            GUILayout.EndArea();
        }
        private void OnDestroy() { if (session != null) session.SceneLoaded -= Loaded; }
    }
}
