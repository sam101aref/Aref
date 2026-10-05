using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Siavosh.Editor.Setup
{
    /// <summary>
    /// Entry points for the cloud build (.github/workflows/siavosh-android.yml), called with -executeMethod.
    /// Unity runs twice, because a change of input handling only applies after a restart:
    ///   1. <see cref="Prepare"/>            — switches input handling, then Unity quits.
    ///   2. <see cref="ConfigureAndBuild"/>  — runs the EditMode tests, applies <see cref="ProjectSetup"/>, builds the APK.
    /// Any exception makes Unity exit with a non-zero code, which fails the workflow step.
    /// </summary>
    public static class CIBuild
    {
        /// <summary>Set by the CI test assembly once the Test Framework is installed; returns true if all tests pass.</summary>
        internal static Func<bool> RunTests;

        const string DefaultOutputPath = "build/Android/SiavoshThePrince.apk";

        public static void Prepare()
        {
            if (ProjectSetup.ConfigureInputHandling())
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[Siavosh CI] Switched Active Input Handling to the Input Manager.");
            }
            AssetDatabase.Refresh();
        }

        public static void ConfigureAndBuild()
        {
            if (RunTests == null)
                throw new Exception("[Siavosh CI] Test runner not available (Test Framework package or CI assembly missing).");
            if (!RunTests())
                throw new Exception("[Siavosh CI] Tests failed, see the log above.");

            if (!ProjectSetup.Configure())
                throw new Exception("[Siavosh CI] Project setup failed, see the log above.");

            BuildAndroid();
        }

        public static void BuildAndroid()
        {
            var appBundle = GetArgument("-androidExportType") == "androidAppBundle";
            var extension = appBundle ? ".aab" : ".apk";
            var outputPath = GetArgument("-customBuildPath") ?? DefaultOutputPath;
            outputPath = Path.HasExtension(outputPath)
                ? Path.ChangeExtension(outputPath, extension)
                : Path.Combine(outputPath, "SiavoshThePrince" + extension);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));

            // GitHub's run number always increases, which is what Android expects from the version code.
            if (int.TryParse(Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER"), out var runNumber))
                PlayerSettings.Android.bundleVersionCode = runNumber;

            EditorUserBuildSettings.buildAppBundle = appBundle;
            PlayerSettings.Android.useCustomKeystore = false;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new Exception("[Siavosh CI] No scenes in the build settings.");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"[Siavosh CI] Build {summary.result} with {summary.totalErrors} error(s).");

            Debug.Log($"[Siavosh CI] Built {outputPath} ({summary.totalSize / (1024 * 1024)} MB) in {summary.totalTime}.");
        }

        static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
