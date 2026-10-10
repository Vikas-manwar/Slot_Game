using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlotGame.EditorTools
{
    public static class WebGLBuilder
    {
        [MenuItem(EditorPaths.MenuRoot + "Build WebGL", priority = 20)]
        public static void BuildFromMenu()
        {
            var report = Build();
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.Combine(EditorPaths.WebGLBuild, "index.html"));
        }

        public static void BuildFromCommandLine()
        {
            if (!File.Exists(EditorPaths.Scene)) SlotSceneBuilder.BuildAll();
            var report = Build();
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "SlotGameAssignment";
            PlayerSettings.productName = "Lucky Sevens";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
        }

        private static BuildReport Build()
        {
            if (!File.Exists(EditorPaths.Scene))
                throw new InvalidOperationException("Run 'Slot Game > Build Scene and Assets' first.");

            ApplyPlayerSettings();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            if (Directory.Exists(EditorPaths.WebGLBuild)) Directory.Delete(EditorPaths.WebGLBuild, true);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = EditorPaths.WebGLBuild,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"[Slot Game] WebGL build succeeded: {summary.totalSize / (1024f * 1024f):F1} MB at {EditorPaths.WebGLBuild}");
            else
                Debug.LogError($"[Slot Game] WebGL build {summary.result} with {summary.totalErrors} error(s).");
            return report;
        }
    }
}
