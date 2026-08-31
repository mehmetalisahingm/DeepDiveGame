using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DeepDive.Editor
{
    // Editor-only: builds the saved scene without regenerating or changing it.
    public static class P0Build
    {
        public const string ScenePath = "Assets/P0/Scenes/P0Example.unity";
        public const string OutputPath = "Builds/P0-Windows/DeepDiveGame-P0.exe";

        [MenuItem("DeepDive/Build P0 Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath))
                throw new InvalidOperationException("P0 example scene is missing: " + ScenePath);
            if (!(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
                throw new InvalidOperationException("P0 requires the shared URP asset in Graphics settings.");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("P0 build failed: " + report.summary.result);

            Debug.Log($"P0_BUILD_SUCCEEDED | Unity {Application.unityVersion} | " +
                      $"{report.summary.totalSize} bytes | {Path.GetFullPath(OutputPath)}");
        }
    }
}
