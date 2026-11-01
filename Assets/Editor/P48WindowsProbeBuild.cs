using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeepDive.Editor
{
    // Builds the DiveTestArea scene alone as a Windows player for DeepDive.Composition.P48WindowsProbe
    // (run it with `-p48-probe <outDir>`). A measuring build, not the integrated game build: it does not touch
    // EditorBuildSettings and writes to Builds/P48-Probe/ (ignored output).
    public static class P48WindowsProbeBuild
    {
        public const string Scene = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string Exe = "Builds/P48-Probe/P48Probe.exe";

        // -executeMethod DeepDive.Editor.P48WindowsProbeBuild.Build
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Exe));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene }, locationPathName = Exe, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Debug.Log("P48_PROBE_BUILD " + report.summary.result + " errors=" + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("probe build failed: " + report.summary.result);
        }
    }
}
