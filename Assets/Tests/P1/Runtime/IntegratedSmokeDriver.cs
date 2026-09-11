using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using DeepDive.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace DeepDive.P1.Lab
{
    // Opt-in automation only, never a scene component or a replacement game provider.
    public sealed class IntegratedSmokeDriver : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string mode, reason;
            public bool passed, connected, roster, ready, prep, dive, returned, readyReset;
            public bool walk, swim, collisions, cameras, stopped, clientRejoined;
            public bool fishMoved, catchObserved, catchDespawned, inventoryAdded;
            public int maxPlayers;
            public List<string> errors = new List<string>();
        }
        private readonly Result result = new Result();
        private SessionNetworkAdapter adapter;
        private string path, observedScene;
        private float sceneStarted;
        private double nextInput;
        private float nextAction;
        private Vector3? firstFishPosition;
        private bool Hunt => Arg("-p2-hunt") == "1";
        private readonly Dictionary<ulong, Vector3> starts = new Dictionary<ulong, Vector3>();
        private readonly HashSet<ulong> walked = new HashSet<ulong>(), swam = new HashSet<ulong>();

        private static string Arg(string key, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p1-integrated").Length == 0) return;
            var go = new GameObject("IntegratedSmokeDriver"); DontDestroyOnLoad(go);
            go.AddComponent<IntegratedSmokeDriver>();
        }
        private IEnumerator Start()
        {
            result.mode = Arg("-p1-integrated"); path = Arg("-p1-report");
            var expected = int.Parse(Arg("-p1-count", "4"));
            var port = ushort.Parse(Arg("-p1-port", "17777"));
            var host = result.mode == "host";
            var reject = result.mode == "reject";
            var rejoin = result.mode == "rejoin";
            Application.logMessageReceived += Log;
            Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            yield return null; yield return null;
            adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            if (adapter == null) { result.errors.Add("No integrated adapter in real scene"); Finish(); yield break; }
            GameObject.Find("Port").GetComponent<InputField>().text = port.ToString();
            GameObject.Find(host ? "HostButton" : "JoinButton").GetComponent<Button>().onClick.Invoke();
            var started = Time.realtimeSinceStartup;
            bool prepSent = false, diveSent = false, returnSent = false, lobbySent = false, leaveSent = false;
            bool rejoinLeft = false, reconnectSent = false, sawOffline = false, lobbyLogged = false, unauthorizedSent = false, screenshot = false;
            var leaveAt = 0f;
            var duration = Hunt ? 58f : 46f;
            while (Time.realtimeSinceStartup - started < duration)
            {
                var elapsed = Time.realtimeSinceStartup - started;
                var connection = adapter.Connection;
                var state = adapter.Session.State;
                if (host && !screenshot && elapsed > 5 && Arg("-p1-screenshot").Length > 0)
                { screenshot = true; CaptureRoom(); }
                if (reject)
                {
                    if (connection.Status == ConnectionStatus.Offline && connection.LastError.Length > 0)
                    {
                        result.reason = connection.LastError;
                        result.passed = result.reason == Arg("-p1-reason") && result.errors.Count == 0;
                        Finish(); yield break;
                    }
                    yield return null; continue;
                }
                result.connected |= connection.Status == ConnectionStatus.Connected;
                result.maxPlayers = Math.Max(result.maxPlayers, adapter.Session.Roster.Count);
                result.roster |= adapter.Session.Roster.Count == expected && connection.Players.Count == expected;
                var allReady = adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(value => value);
                result.ready |= allReady;
                if (host && !lobbyLogged && result.roster)
                { lobbyLogged = true; Debug.Log("P1_INTEGRATED_LOBBY_READY"); }
                if (connection.Status == ConnectionStatus.Connected && !connection.IsSceneLoading)
                {
                    Probe(expected);
                    if (state.Phase == SessionPhase.Lobby && connection.LocalPlayerId.HasValue &&
                        adapter.Session.Roster.TryGetValue(connection.LocalPlayerId.Value, out var ready) && !ready && state.Revision == 0)
                        GameObject.Find("ReadyButton").GetComponent<Button>().onClick.Invoke();
                    // Exercise the server rejection as well as the disabled client UI.
                    if (!host && !unauthorizedSent && state.Phase == SessionPhase.Lobby && elapsed > 4)
                    { unauthorizedSent = true; adapter.AdvancePhase(); }
                }
                if (rejoin && elapsed > 8 && !rejoinLeft)
                { rejoinLeft = true; leaveAt = elapsed; adapter.LeaveRoom(); }
                if (rejoinLeft && connection.Status == ConnectionStatus.Offline) sawOffline = true;
                if (rejoinLeft && !reconnectSent && sawOffline && elapsed > leaveAt + 2)
                { reconnectSent = true; adapter.JoinRoom("127.0.0.1", port); }
                if (reconnectSent && connection.Status == ConnectionStatus.Connected && connection.Players.Count == expected)
                    result.clientRejoined = true;
                if (host && elapsed > 19 && !prepSent && allReady)
                { prepSent = true; GameObject.Find("BeginPrepButton").GetComponent<Button>().onClick.Invoke(); }
                result.prep |= state.Phase == SessionPhase.Prep && !connection.IsSceneLoading;
                if (host && elapsed > 22 && !diveSent && state.Phase == SessionPhase.Prep)
                { diveSent = true; GameObject.Find("BeginDiveButton").GetComponent<Button>().onClick.Invoke(); }
                result.dive |= state.Phase == SessionPhase.Dive && SceneManager.GetActiveScene().name == SessionNetworkAdapter.DiveScene;
                if (host && elapsed > (Hunt ? 44 : 33) && !returnSent && state.Phase == SessionPhase.Dive && !connection.IsSceneLoading)
                { returnSent = true; GameObject.Find("BeginReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (host && elapsed > (Hunt ? 48 : 36) && !lobbySent && state.Phase == SessionPhase.Return && !connection.IsSceneLoading)
                { lobbySent = true; GameObject.Find("CompleteReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (result.dive && state.Phase == SessionPhase.Lobby && state.Revision >= 4 && !connection.IsSceneLoading)
                {
                    result.returned = SceneManager.GetActiveScene().name == SessionNetworkAdapter.PrepScene;
                    result.readyReset |= adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(value => !value) &&
                        string.IsNullOrEmpty(state.DiveId);
                }
                if (host && elapsed > (Hunt ? 54 : 41) && !leaveSent) { leaveSent = true; adapter.LeaveRoom(); }
                if (result.returned && connection.Status == ConnectionStatus.Offline)
                    result.stopped = adapter.Session.Roster.Count == 0 && connection.Players.Count == 0;
                yield return null;
            }
            result.reason = adapter.Connection.LastError;
            result.passed = result.connected && result.roster && result.ready && result.prep && result.dive && result.returned &&
                result.readyReset && result.walk && result.swim && result.collisions && result.cameras && result.stopped &&
                (!rejoin || result.clientRejoined) && result.errors.Count == 0;
            if (Hunt) result.passed &= result.fishMoved && result.catchObserved && result.catchDespawned && (!host || result.inventoryAdded);
            Finish();
        }

        private void Probe(int expected)
        {
            var scene = SceneManager.GetActiveScene().name;
            if (observedScene != scene)
            { observedScene = scene; sceneStarted = Time.realtimeSinceStartup; starts.Clear(); }
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).ToArray();
            foreach (var player in players)
            {
                player.ReadKeyboard = false;
                if (!starts.ContainsKey(player.OwnerClientId)) starts[player.OwnerClientId] = player.transform.position;
            }
            var local = players.FirstOrDefault(p => p.IsOwner);
            var elapsed = Time.realtimeSinceStartup - sceneStarted;
            if (local != null && Time.realtimeSinceStartupAsDouble >= nextInput)
            {
                nextInput = Time.realtimeSinceStartupAsDouble + 1.0 / 30;
                var move = scene == SessionNetworkAdapter.DiveScene ?
                    (elapsed > 1 && elapsed < 2.5f ? Vector3.up : Vector3.zero) :
                    (elapsed > 1 && elapsed < 5 ? Vector3.forward : Vector3.zero);
                if (Hunt && scene == SessionNetworkAdapter.DiveScene && elapsed > 3) ProbeHunt(local, players);
                else local.SubmitLocalInput(move, 0);
            }
            foreach (var player in players)
            {
                var delta = player.transform.position - starts[player.OwnerClientId];
                if (scene == SessionNetworkAdapter.PrepScene && delta.z > 0.5f) walked.Add(player.OwnerClientId);
                if (scene == SessionNetworkAdapter.DiveScene && player.Swimming.Value && delta.y > 0.7f) swam.Add(player.OwnerClientId);
            }
            result.walk |= walked.Count >= expected;
            result.swim |= swam.Count >= expected;
            if (scene == SessionNetworkAdapter.PrepScene && elapsed > 6 && players.Length == expected)
                result.collisions = players.All(p => Mathf.Abs(p.transform.position.x) < 4.9f && Mathf.Abs(p.transform.position.z) < 4.9f && p.transform.position.y > -0.5f);
            if (players.Length == expected && local != null && elapsed > 1)
                result.cameras = Camera.allCamerasCount == 1 && players.All(p =>
                    p.GetComponentInChildren<Camera>(true).enabled == p.IsOwner && p.GetComponentInChildren<AudioListener>(true).enabled == p.IsOwner);
        }

        private void ProbeHunt(NetworkPlayer local, NetworkPlayer[] players)
        {
            // Real owner inputs/RPCs, live fish AI and real inventory; no injected catch.
            var shooter = players.Length == 1 ? local.OwnerClientId : players.Where(p => p.OwnerClientId != 0).Min(p => p.OwnerClientId);
            if (adapter.IsAuthority && adapter.GetComponent<InventoryManager>().Bags.TryGetValue(new PlayerId(shooter), out var bag))
                result.inventoryAdded |= bag.Items.Count == 1;
            var fish = FindFirstObjectByType<FishActor>();
            if (fish == null || !fish.IsSpawned)
            {
                result.catchDespawned |= result.catchObserved;
                local.SubmitLocalInput(Vector3.zero, 0); return;
            }
            if (!firstFishPosition.HasValue) firstFishPosition = fish.transform.position;
            result.fishMoved |= Vector3.Distance(firstFishPosition.Value, fish.transform.position) > 0.15f;
            var available = fish.GetComponent<CatchObject>().IsAvailable;
            result.catchObserved |= available;
            if (local.OwnerClientId != shooter) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            var delta = fish.GetComponent<Collider>().bounds.center - local.GetComponentInChildren<Camera>(true).transform.position;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            var move = available && delta.magnitude > 1.7f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * delta.normalized : Vector3.zero;
            local.SubmitLocalInput(move, yaw, pitch);
            if (nextAction == 0) nextAction = Time.realtimeSinceStartup + 0.2f;
            if (Time.realtimeSinceStartup < nextAction) return;
            nextAction = Time.realtimeSinceStartup + 0.75f;
            if (available) { if (delta.magnitude < 2.2f) local.SubmitPickupLocal(); }
            else local.SubmitHarpoonLocal();
        }

        private void CaptureRoom()
        {
            // A hidden Windows player can skip presenting its backbuffer. Render explicitly
            // into a texture so the capture still contains the real camera and uGUI layout.
            var camera = Camera.allCameras.First(c => c.enabled);
            var canvas = adapter.GetComponentInChildren<Canvas>();
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousCameraTarget = camera.targetTexture;
            var previousPlane = canvas.planeDistance;
            var previousTarget = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                target.Create();
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 0.01f;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(Arg("-p1-screenshot"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                camera.targetTexture = previousCameraTarget;
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousPlane;
                target.Release(); Destroy(target); Destroy(pixels);
            }
        }
        private void Log(string message, string stack, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) && result.errors.Count < 15)
                result.errors.Add(message);
        }
        private void Finish()
        {
            Application.logMessageReceived -= Log;
            File.WriteAllText(path, JsonUtility.ToJson(result, true));
            Application.Quit(result.passed ? 0 : 2);
        }
    }
}
