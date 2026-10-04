using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Arash.Editor.Setup
{
    /// <summary>
    /// Entry points for the cloud build (.github/workflows/android.yml), called with -executeMethod.
    /// The build runs Unity twice, because newly installed packages only compile after a restart:
    ///   1. <see cref="InstallPackages"/>     — adds any missing packages, then Unity quits.
    ///   2. <see cref="ConfigureAndBuild"/>   — applies <see cref="ProjectSetup"/> and builds the APK.
    /// Any exception makes Unity exit with a non-zero code, which fails the workflow step.
    /// </summary>
    public static class CIBuild
    {
        const string DefaultOutputPath = "build/Android/ArashTheArcher.apk";
        static readonly TimeSpan PackageTimeout = TimeSpan.FromMinutes(15);

        public static void InstallPackages()
        {
            var missing = MissingPackages();
            if (missing.Length == 0)
            {
                Debug.Log("[Arash CI] All required packages are installed.");
                return;
            }

            Debug.Log("[Arash CI] Installing: " + string.Join(", ", missing));
            var request = Wait(Client.AddAndRemove(missing));
            if (request.Status != StatusCode.Success)
                throw new Exception($"[Arash CI] Package installation failed: {request.Error?.message}");

            Debug.Log("[Arash CI] Packages installed.");
        }

        public static void ConfigureAndBuild()
        {
            var missing = MissingPackages();
            if (missing.Length > 0)
                throw new Exception("[Arash CI] Packages missing (run InstallPackages first): " + string.Join(", ", missing));

            if (!ProjectSetup.Configure())
                throw new Exception("[Arash CI] Project setup failed, see the log above.");

            BuildAndroid();
        }

        public static void BuildAndroid()
        {
            var outputPath = GetArgument("-customBuildPath") ?? DefaultOutputPath;
            if (!outputPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                outputPath = Path.Combine(outputPath, "ArashTheArcher.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));

            // GitHub's run number always increases, which is what Android expects from the version code.
            if (int.TryParse(Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER"), out var runNumber))
                PlayerSettings.Android.bundleVersionCode = runNumber;

            EditorUserBuildSettings.buildAppBundle = false;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new Exception("[Arash CI] No scenes in the build settings.");

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
                throw new Exception($"[Arash CI] Build {summary.result} with {summary.totalErrors} error(s).");

            Debug.Log($"[Arash CI] Built {outputPath} ({summary.totalSize / (1024 * 1024)} MB) in {summary.totalTime}.");
        }

        static string[] MissingPackages()
        {
            var request = Wait(Client.List(offlineMode: true, includeIndirectDependencies: true));
            if (request.Status != StatusCode.Success)
                throw new Exception($"[Arash CI] Could not list packages: {request.Error?.message}");

            var installed = request.Result.Select(p => p.name).ToList();
            return PackageInstaller.RequiredPackages.Where(p => !installed.Contains(p)).ToArray();
        }

        /// <summary>Blocks until a Package Manager request finishes; there is no editor loop in batch mode.</summary>
        static T Wait<T>(T request) where T : Request
        {
            var deadline = DateTime.UtcNow + PackageTimeout;
            while (!request.IsCompleted)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("[Arash CI] Package Manager request timed out.");
                Thread.Sleep(200);
            }
            return request;
        }

        static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
