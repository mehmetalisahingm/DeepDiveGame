using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeepDive.Editor
{
    // Builds the DiveTestArea scene alone as a Windows player for DeepDive.Composition.TownLookProbe (run it with `-town-look <outDir>`).
    // A look-and-measure build, not the integrated game: it does not touch EditorBuildSettings and writes to Builds/TownLook/ (ignored output).
    public static class TownLookProbeBuild
    {
        public const string Exe = "Builds/TownLook/TownLook.exe";

        // -executeMethod DeepDive.Editor.TownLookProbeBuild.Build
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Exe));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { P48WindowsProbeBuild.Scene }, locationPathName = Exe, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Debug.Log("TOWNLOOK_BUILD " + report.summary.result + " errors=" + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("town look build failed: " + report.summary.result);
        }
    }
}
