using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace DeepDive.P1.Lab
{
    // #327 explicit development measurement (-p327-perf 1): rides along with any lab driver in a real player process and
    // records every frame's time with where the local player stands (PlayerZone from the world's zone field) and whether
    // it is night (SharedDaylight), plus GC collections, managed allocation and memory. Writes "<report>.perf.json" next
    // to the driver's -p1-report: per zone and day/night p50/p95/p99/max frame ms, frames over 33 and 50 ms, collections
    // and allocation per frame, the first hitches with what was happening, and the machine/build/settings. The first
    // 5 s after a scene load and the load frames themselves are kept apart as "loading". With -p327-uncap 1 the process
    // turns VSync off and uncaps the frame rate (a LABELLED measurement fixture, so the numbers show headroom rather
    // than the monitor's refresh). Changes nothing else.
    public sealed class WorldPerfSampler : MonoBehaviour
    {
        private const int MaxFramesPerKey = 40000;
        private const float SettleSeconds = 5f;

        [Serializable] public sealed class Bucket
        {
            public string key;
            public int frames, over33, over50, gcCollections;
            public float p50, p95, p99, max, mean, allocKbPerFrame;
            // From the development player's profiler counters, means over the bucket's frames (0 when unavailable).
            public float gcAllocKbPerFrame, mainThreadMs, renderThreadMs, gpuMs;
        }
        [Serializable] public sealed class Hitch { public float time, ms; public string key, position; public bool gc; }
        [Serializable] public sealed class Report
        {
            public string build, unity, device, cpu, gpu, graphicsApi, resolution, quality;
            public int cpuCores, systemMemoryMb, gpuMemoryMb, vSyncCount, targetFrameRate, players;
            public bool uncapped, developmentBuild, counterGcAlloc, counterMainThread, counterRenderThread, counterGpu;
            public float seconds, loadingSeconds, monoUsedMb, totalAllocatedMb, totalReservedMb;
            public List<Bucket> buckets = new List<Bucket>();
            public List<Hitch> hitches = new List<Hitch>();
        }

        private readonly Dictionary<string, List<float>> frames = new Dictionary<string, List<float>>();
        private readonly Dictionary<string, int> gcByKey = new Dictionary<string, int>();
        private readonly Dictionary<string, long> allocByKey = new Dictionary<string, long>();
        private readonly Dictionary<string, double[]> countersByKey = new Dictionary<string, double[]>();
        private ProfilerRecorder gcAlloc, mainThread, renderThread, gpu;
        private readonly List<Hitch> hitches = new List<Hitch>();
        private string output;
        private float settledAt, loadingSeconds, nextWrite;
        private int lastGc;
        private long lastMono;
        private bool uncapped;
        private float nextUncap;
        private IPlayerZoneSource zones;

        private static string Arg(string key, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p327-perf") != "1" || Arg("-p1-report").Length == 0) return;
            var go = new GameObject("P327_WorldPerfSampler");
            DontDestroyOnLoad(go);
            go.AddComponent<WorldPerfSampler>();
        }

        private void Awake()
        {
            output = Arg("-p1-report") + ".perf.json";
            uncapped = Arg("-p327-uncap") == "1";
            if (uncapped) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; }
            SceneManager.sceneLoaded += (_, __) => { settledAt = Time.realtimeSinceStartup + SettleSeconds; zones = null; };
            settledAt = Time.realtimeSinceStartup + SettleSeconds;
            lastGc = GC.CollectionCount(0);
            lastMono = Profiler.GetMonoUsedSizeLong();
            gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
            renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Render Thread Frame Time");
            gpu = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GPU Frame Time");
        }

        private void OnDestroy() { gcAlloc.Dispose(); mainThread.Dispose(); renderThread.Dispose(); gpu.Dispose(); }

        private void Update()
        {
            var ms = Time.unscaledDeltaTime * 1000f;
            var gc = GC.CollectionCount(0);
            var collected = gc != lastGc;
            lastGc = gc;
            var mono = Profiler.GetMonoUsedSizeLong();
            var alloc = Math.Max(0L, mono - lastMono);
            lastMono = mono;
            // The game sets its own frame cap after start-up; the labelled fixture keeps it off.
            if (uncapped && Time.realtimeSinceStartup >= nextUncap)
            {
                nextUncap = Time.realtimeSinceStartup + 1f;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }
            if (Time.realtimeSinceStartup < settledAt) { loadingSeconds += Time.unscaledDeltaTime; return; }

            var key = Key(out var feet);
            if (!frames.TryGetValue(key, out var list))
            { frames[key] = list = new List<float>(); gcByKey[key] = 0; allocByKey[key] = 0; countersByKey[key] = new double[4]; }
            var counters = countersByKey[key];
            // A counter with no sample this frame can read back garbage; only plausible values count (under 1 s / 64 MB).
            static double Ok(ProfilerRecorder r, double cap) => r.Valid && r.LastValue >= 0 && r.LastValue < cap ? r.LastValue : 0;
            counters[0] += Ok(gcAlloc, 64.0 * 1048576);
            counters[1] += Ok(mainThread, 1e9);
            counters[2] += Ok(renderThread, 1e9);
            counters[3] += Ok(gpu, 1e9);
            if (list.Count < MaxFramesPerKey) list.Add(ms);
            if (collected) gcByKey[key]++;
            allocByKey[key] += alloc;
            if (ms > 50f && hitches.Count < 60)
                hitches.Add(new Hitch { time = Time.realtimeSinceStartup, ms = ms, key = key, position = feet.ToString("0.0"), gc = collected });
            if (Time.realtimeSinceStartup >= nextWrite) { nextWrite = Time.realtimeSinceStartup + 30f; Write(); }
        }

        private void OnApplicationQuit() => Write();

        // Cached per zone and day/night so a frame allocates nothing here.
        private static readonly Dictionary<(PlayerZone, bool), string> Keys = new Dictionary<(PlayerZone, bool), string>();

        private string Key(out Vector3 feet)
        {
            feet = Vector3.zero;
            var local = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClient?.PlayerObject : null;
            if (local == null) return "noPlayer";
            feet = local.transform.position;
            if (zones == null) WorldZoneField.TryFind(out zones);
            var zone = zones != null ? zones.ResolveZone(feet) : PlayerZone.Unknown;
            var night = SharedDaylight.Night() > 0.5f;
            if (!Keys.TryGetValue((zone, night), out var key)) Keys[(zone, night)] = key = zone + (night ? "/night" : "/day");
            return key;
        }

        private void Write()
        {
            try
            {
                var report = new Report
                {
                    build = Application.version, unity = Application.unityVersion, device = SystemInfo.deviceModel,
                    cpu = SystemInfo.processorType, cpuCores = SystemInfo.processorCount, systemMemoryMb = SystemInfo.systemMemorySize,
                    gpu = SystemInfo.graphicsDeviceName, gpuMemoryMb = SystemInfo.graphicsMemorySize,
                    graphicsApi = SystemInfo.graphicsDeviceType.ToString(), resolution = Screen.width + "x" + Screen.height,
                    quality = QualitySettings.names[QualitySettings.GetQualityLevel()], vSyncCount = QualitySettings.vSyncCount,
                    targetFrameRate = Application.targetFrameRate, uncapped = uncapped, developmentBuild = Debug.isDebugBuild,
                    counterGcAlloc = gcAlloc.Valid, counterMainThread = mainThread.Valid, counterRenderThread = renderThread.Valid, counterGpu = gpu.Valid,
                    players = FindObjectsByType<DeepDive.Network.NetworkPlayer>(FindObjectsSortMode.None).Count(p => p.IsSpawned),
                    seconds = Time.realtimeSinceStartup, loadingSeconds = loadingSeconds,
                    monoUsedMb = Profiler.GetMonoUsedSizeLong() / 1048576f,
                    totalAllocatedMb = Profiler.GetTotalAllocatedMemoryLong() / 1048576f,
                    totalReservedMb = Profiler.GetTotalReservedMemoryLong() / 1048576f,
                    hitches = hitches
                };
                foreach (var pair in frames.OrderBy(p => p.Key))
                {
                    var sorted = pair.Value.OrderBy(v => v).ToArray();
                    if (sorted.Length == 0) continue;
                    float At(float q) => sorted[Mathf.Clamp(Mathf.CeilToInt(q * sorted.Length) - 1, 0, sorted.Length - 1)];
                    report.buckets.Add(new Bucket
                    {
                        key = pair.Key, frames = sorted.Length, p50 = At(0.5f), p95 = At(0.95f), p99 = At(0.99f),
                        max = sorted[sorted.Length - 1], mean = sorted.Average(), over33 = sorted.Count(v => v > 33.3f),
                        over50 = sorted.Count(v => v > 50f), gcCollections = gcByKey[pair.Key],
                        allocKbPerFrame = allocByKey[pair.Key] / 1024f / sorted.Length,
                        gcAllocKbPerFrame = (float)(countersByKey[pair.Key][0] / 1024.0 / sorted.Length),
                        mainThreadMs = (float)(countersByKey[pair.Key][1] / 1e6 / sorted.Length),
                        renderThreadMs = (float)(countersByKey[pair.Key][2] / 1e6 / sorted.Length),
                        gpuMs = (float)(countersByKey[pair.Key][3] / 1e6 / sorted.Length)
                    });
                }
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            catch (Exception e) { Debug.LogWarning("P327_PERF_WRITE_FAILED " + e.Message); }
        }
    }
}
