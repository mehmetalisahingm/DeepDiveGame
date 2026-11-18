using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P1.Lab
{
    // #303 explicit development measurement (-WreckFrames with -OceanVideo):
    // - frame time at each wreck stop with the wreck's renderers shown and hidden, alternated in one binary, the same
    //   way #219 measured the sea bed. Renderers only, this process only; colliders and authority are untouched.
    // - with -WreckNight, a LABELLED clock fixture: the host moves the day clock to 22:00 for the survey and puts the
    //   minute back afterwards, so the frames show the real night lighting. Fish AI, replication and lights stay real.
    public sealed partial class ProductDaySmokeDriver
    {
        private const int WreckWarmFrames = 20, WreckSampleFrames = 180;
        private static readonly bool[] WreckScenarios = { true, false, true, false };

        private bool WreckNight => Arg("-p303-wreck-night") == "1";
        private List<Renderer> wreckRenderers;
        private readonly List<float> wreckSamples = new List<float>();
        private int wreckScenario, wreckFrame, wreckSavedMinute = -1;

        private void WreckNightFixture(bool on)
        {
            if (!Host || !WreckNight) return;
            var engine = adapter.GetComponent<DayNetworkBinding>()?.Engine;
            if (engine == null) return;
            var saved = engine.ExportDay();
            if (on && wreckSavedMinute < 0) { wreckSavedMinute = saved.ClockMinute; saved.ClockMinute = 22 * 60; engine.RestoreDay(saved); }
            else if (!on && wreckSavedMinute >= 0) { saved.ClockMinute = wreckSavedMinute; wreckSavedMinute = -1; engine.RestoreDay(saved); }
        }

        private bool WreckFrameStop()
        {
            if (wreckRenderers == null)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                File.WriteAllText(output + ".wreck-frames.csv", "stop,scenario,frames,mean_ms,p95_ms,max_ms,night,gpu\n");
                wreckRenderers = SceneManager.GetSceneByName(ContinuousTownLayout.SceneName).GetRootGameObjects()
                    .Where(r => r.name == ShallowWreckVisuals.RootName).SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToList();
            }
            if (wreckRenderers.Count == 0) return true; // a build without the wreck: frames only, nothing to alternate
            if (wreckFrame == 0) foreach (var r in wreckRenderers) if (r != null) r.enabled = WreckScenarios[wreckScenario];
            wreckFrame++;
            if (wreckFrame <= WreckWarmFrames) return false;
            wreckSamples.Add(Time.unscaledDeltaTime * 1000f);
            if (wreckSamples.Count < WreckSampleFrames) return false;

            wreckSamples.Sort();
            File.AppendAllText(output + ".wreck-frames.csv", string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3:F3},{4:F3},{5:F3},{6},{7}\n", oceanWaypoint, WreckScenarios[wreckScenario] ? "wreck" : "hidden",
                wreckSamples.Count, wreckSamples.Average(), wreckSamples[(int)(wreckSamples.Count * .95f)],
                wreckSamples[wreckSamples.Count - 1], WreckNight ? 1 : 0, SystemInfo.graphicsDeviceName));
            wreckSamples.Clear();
            wreckFrame = 0;
            if (++wreckScenario < WreckScenarios.Length) return false;
            wreckScenario = 0;
            foreach (var r in wreckRenderers) if (r != null) r.enabled = true;
            return true;
        }
    }
}
