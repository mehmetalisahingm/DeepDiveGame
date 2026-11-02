using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P1.Lab
{
    // #219 explicit development measurement (-SeabedFrames with -OceanVideo): at each band stop of the ocean survey,
    // frame time of the natural sea bed and reef kit against the old floor boxes, alternated in one binary, with the
    // real ocean, reflection and player camera. Renderers only, on this process only: no collider or authority
    // changes, and video frame capture is off because it would swamp the numbers.
    public sealed partial class ProductDaySmokeDriver
    {
        private const int SeabedWarmFrames = 20, SeabedSampleFrames = 180;
        private static readonly bool[] SeabedScenarios = { true, false, true, false };
        private static readonly string[] OldFloorNames =
            { "SeaFloor", "Reef_DropOff", "Reef_Shelf", "Reef_Head_0", "Reef_Head_1", "Reef_Head_2", "Deep_DropOff", "Deep_Basin" };

        private bool SeabedFrames => Arg("-p219-seabed-frames") == "1";
        private List<Renderer> seabedNew;
        private List<GameObject> seabedOld;
        private readonly List<float> seabedSamples = new List<float>();
        private int seabedScenario, seabedFrame;

        private bool SeabedFrameStop()
        {
            if (seabedNew == null) PrepareSeabedFrames();
            if (seabedFrame == 0) ApplySeabedScenario(SeabedScenarios[seabedScenario]);
            seabedFrame++;
            if (seabedFrame <= SeabedWarmFrames) return false;
            seabedSamples.Add(Time.unscaledDeltaTime * 1000f);
            if (seabedSamples.Count < SeabedSampleFrames) return false;

            seabedSamples.Sort();
            var ci = CultureInfo.InvariantCulture;
            File.AppendAllText(output + ".seabed-frames.csv", string.Format(ci, "{0},{1},{2},{3:F3},{4:F3},{5:F3},{6}\n",
                oceanWaypoint, SeabedScenarios[seabedScenario] ? "seabed" : "old-floor", seabedSamples.Count, seabedSamples.Average(),
                seabedSamples[(int)(seabedSamples.Count * .95f)], seabedSamples[seabedSamples.Count - 1], SystemInfo.graphicsDeviceName));
            seabedSamples.Clear();
            seabedFrame = 0;
            if (++seabedScenario < SeabedScenarios.Length) return false;
            seabedScenario = 0;
            ApplySeabedScenario(true);
            return true;
        }

        private void PrepareSeabedFrames()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            File.WriteAllText(output + ".seabed-frames.csv", "stop,scenario,frames,mean_ms,p95_ms,max_ms,gpu\n");
            var roots = SceneManager.GetSceneByName(ContinuousTownLayout.SceneName).GetRootGameObjects();
            seabedNew = roots.Where(r => r.name.StartsWith("Seabed_") || r.name == ReefHabitatVisuals.RootName)
                .SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToList();
            var floorMaterial = roots.First(r => r.name == "Seabed_Reef").GetComponent<Renderer>().sharedMaterial;
            seabedOld = roots.Where(r => OldFloorNames.Contains(r.name)).ToList();
            foreach (var piece in seabedOld)
            {
                foreach (var collider in piece.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                piece.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            }
        }

        private void ApplySeabedScenario(bool seabed)
        {
            foreach (var renderer in seabedNew) if (renderer != null) renderer.enabled = seabed;
            foreach (var piece in seabedOld) piece.SetActive(!seabed);
        }
    }
}
