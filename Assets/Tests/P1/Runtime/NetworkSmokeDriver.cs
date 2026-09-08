using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P1.Lab
{
    // Compiles only in Editor/Development builds; inputs pass through the real owner RPC.
    public sealed class NetworkSmokeDriver : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string mode;
            public bool passed;
            public bool connected;
            public bool roster;
            public bool walk;
            public bool swim;
            public bool returned;
            public bool stopped;
            public bool cameras;
            public string reason;
            public int maxPlayers;
            public List<string> errors = new List<string>();
        }

        private readonly Result result = new Result();
        private NetworkSession session;
        private NetworkManager manager;
        private string reportPath;
        private bool active;
        private double nextInput;
        private float sceneStarted;
        private string observedScene;
        private readonly HashSet<ulong> walked = new HashSet<ulong>();
        private readonly HashSet<ulong> swam = new HashSet<ulong>();

        private string Arg(string name, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        private IEnumerator Start()
        {
            result.mode = Arg("-p1-smoke");
            if (result.mode.Length == 0) yield break;
            reportPath = Arg("-p1-report");
            active = true;
            Application.logMessageReceived += Log;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            session = GetComponent<NetworkSession>();
            manager = GetComponent<NetworkManager>();
            yield return null;
            var port = ushort.Parse(Arg("-p1-port", "17777"));
            var duration = float.Parse(Arg("-p1-duration", "32"), System.Globalization.CultureInfo.InvariantCulture);
            var expected = int.Parse(Arg("-p1-count", "4"));
            var host = result.mode == "host" || result.mode == "solo";
            var expectReject = result.mode == "reject";
            var expectedReason = Arg("-p1-reason", "RoomFull");
            if (host) session.StartHost(port, "127.0.0.1"); else session.Join("127.0.0.1", port);
            var started = Time.realtimeSinceStartup;
            var diveRequested = false;
            var returnRequested = false;
            var leaveRequested = false;
            var sceneResults = new HashSet<string>();
            session.SceneLoaded += r => { if (r.Succeeded) sceneResults.Add(r.SceneName); else result.errors.Add(r.Error); };
            while (Time.realtimeSinceStartup - started < duration)
            {
                var elapsed = Time.realtimeSinceStartup - started;
                if (expectReject)
                {
                    if (session.Status == ConnectionStatus.Offline && session.LastError.Length > 0)
                    { result.reason = session.LastError; result.passed = result.reason == expectedReason; Finish(); yield break; }
                    yield return null; continue;
                }
                result.connected |= session.Status == ConnectionStatus.Connected;
                result.maxPlayers = Math.Max(result.maxPlayers, session.Players.Count);
                result.roster |= session.Players.Count == expected;
                if (session.Status == ConnectionStatus.Connected && !session.IsSceneLoading) Probe(expected);
                if (host && elapsed > 10 && !diveRequested)
                {
                    diveRequested = true;
                    if (!session.TryLoadScene("P1NetworkWaterLab", 1)) result.errors.Add("Dive request: " + session.LastError);
                    // Duplicate request must not launch another scene load.
                    if (!session.TryLoadScene("P1NetworkWaterLab", 1)) result.errors.Add("Duplicate request rejected");
                }
                if (host && elapsed > 19 && !returnRequested)
                {
                    returnRequested = true;
                    if (!session.TryLoadScene("P1NetworkLab", 2)) result.errors.Add("Return request: " + session.LastError);
                }
                if (host && sceneResults.Contains("P1NetworkLab")) session.SetJoinAllowed(true);
                if (result.swim && SceneManager.GetActiveScene().name == "P1NetworkLab" && !session.IsSceneLoading) result.returned = true;
                if (host && elapsed > duration - 3 && !leaveRequested) { leaveRequested = true; session.Leave(); }
                if (result.connected && session.Status == ConnectionStatus.Offline) result.stopped = true;
                yield return null;
            }
            result.reason = session.LastError;
            result.passed = result.connected && result.roster && result.walk && result.swim && result.returned && result.stopped && result.cameras && result.errors.Count == 0;
            Finish();
        }

        private void Probe(int expected)
        {
            var scene = SceneManager.GetActiveScene().name;
            if (observedScene != scene)
            { observedScene = scene; sceneStarted = Time.realtimeSinceStartup; }
            var all = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).ToArray();
            var local = all.FirstOrDefault(p => p.IsOwner);
            foreach (var player in all) player.ReadKeyboard = false;
            if (local != null && Time.realtimeSinceStartupAsDouble >= nextInput)
            {
                nextInput = Time.realtimeSinceStartupAsDouble + 1.0 / 30;
                var sceneElapsed = Time.realtimeSinceStartup - sceneStarted;
                var move = sceneElapsed > 1 && sceneElapsed < 3 ?
                    (scene == "P1NetworkWaterLab" ? Vector3.up : Vector3.forward) : Vector3.zero;
                local.SubmitLocalInput(move, 0);
            }
            foreach (var player in all)
            {
                if (scene == "P1NetworkLab" && player.transform.position.z > 2f) walked.Add(player.OwnerClientId);
                if (scene == "P1NetworkWaterLab" && player.Swimming.Value && player.transform.position.y > -3f) swam.Add(player.OwnerClientId);
            }
            result.walk |= walked.Count >= expected;
            result.swim |= swam.Count >= expected;
            if (all.Length == expected && local != null)
            {
                result.cameras = Camera.allCamerasCount == 1 && all.All(p =>
                    p.GetComponentInChildren<Camera>(true).enabled == p.IsOwner &&
                    p.GetComponentInChildren<AudioListener>(true).enabled == p.IsOwner);
            }
        }

        private void Log(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && result.errors.Count < 12)
                result.errors.Add(message);
        }

        private void Finish()
        {
            active = false;
            Application.logMessageReceived -= Log;
            File.WriteAllText(reportPath, JsonUtility.ToJson(result, true));
            Debug.Log("P1_SMOKE_RESULT " + JsonUtility.ToJson(result));
            Application.Quit(result.passed ? 0 : 2);
        }
        private void OnDestroy() { if (active) Application.logMessageReceived -= Log; }
    }
}
