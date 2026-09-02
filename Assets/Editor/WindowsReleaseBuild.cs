#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SparkStrikers.Editor
{
    public static class WindowsReleaseBuild
    {
        [MenuItem("Build/Windows Release")]
        public static void Build()
        {
            string output = Environment.GetEnvironmentVariable("SPARK_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output))
                output = Path.GetFullPath(Path.Combine("Builds", "Windows", "SparkStrikers.exe"));

            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "Builds");
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes in Build Settings.");

            ApplyProductSettings();
            // ponytail: Mono ships with this editor; switch to IL2CPP when its Windows module is installed.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode | BuildOptions.CompressWithLz4HC
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}");

            string notices = Path.GetFullPath("ThirdPartyNotices.txt");
            if (!File.Exists(notices)) throw new FileNotFoundException("Missing third-party notices.", notices);
            File.Copy(notices, Path.Combine(Path.GetDirectoryName(output) ?? "Builds", "ThirdPartyNotices.txt"), true);
        }

        static void ApplyProductSettings()
        {
            const string iconPath = "Assets/Brand/SparkStrikersIcon.png";
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon == null) throw new InvalidOperationException("Missing product icon: " + iconPath);

            int iconCount = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application).Length;
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, Enumerable.Repeat(icon, iconCount).ToArray(), IconKind.Application);
            PlayerSettings.companyName = "Spark Strikers";
            PlayerSettings.productName = "Spark Strikers";
            string version = Environment.GetEnvironmentVariable("SPARK_BUILD_VERSION");
            if (!string.IsNullOrWhiteSpace(version)) PlayerSettings.bundleVersion = version.Trim();
        }
    }
}
#endif
