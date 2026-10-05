using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace IranVsTuran.Editor.Setup
{
    /// <summary>
    /// Entry points for the cloud build (.github/workflows/android.yml), called with -executeMethod.
    /// The build runs Unity twice, because newly installed packages only compile after a restart:
    ///   1. <see cref="InstallPackages"/>     — adds any missing packages, then Unity quits.
    ///   2. <see cref="ConfigureAndBuild"/>   — runs the EditMode tests, applies <see cref="ProjectSetup"/> and builds the APK.
    /// Any exception makes Unity exit with a non-zero code, which fails the workflow step.
    /// </summary>
    public static class CIBuild
    {
        /// <summary>Set by the CI test assembly once the Test Framework is installed; returns true if all tests pass.</summary>
        internal static Func<bool> RunTests;

        const string DefaultOutputPath = "build/Android/IranVsTuran.apk";
        static readonly TimeSpan PackageTimeout = TimeSpan.FromMinutes(15);

        public static void InstallPackages()
        {
            // Switching to the Input System only takes effect after a restart; doing it here means the
            // build pass starts with editor and player compiled alike (otherwise Unity refuses to build:
            // "script class layout is incompatible between the editor and the player").
            if (ProjectSetup.ConfigureInputHandling())
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[IVT CI] Switched Active Input Handling to the Input System package.");
            }

            var missing = MissingPackages();
            if (missing.Length == 0)
            {
                Debug.Log("[IVT CI] All required packages are installed.");
                return;
            }

            Debug.Log("[IVT CI] Installing: " + string.Join(", ", missing));
            var request = Wait(Client.AddAndRemove(missing));
            if (request.Status != StatusCode.Success)
                throw new Exception($"[IVT CI] Package installation failed: {request.Error?.message}");

            Debug.Log("[IVT CI] Packages installed.");
        }

        public static void ConfigureAndBuild()
        {
            var missing = MissingPackages();
            if (missing.Length > 0)
                throw new Exception("[IVT CI] Packages missing (run InstallPackages first): " + string.Join(", ", missing));

            if (RunTests == null)
                throw new Exception("[IVT CI] Test runner not available (Test Framework package or CI assembly missing).");
            if (!RunTests())
                throw new Exception("[IVT CI] Tests failed, see the log above.");

            if (!ProjectSetup.Configure())
                throw new Exception("[IVT CI] Project setup failed, see the log above.");

            BuildAndroid();
        }

        public static void BuildAndroid()
        {
            // Release builds (F-39) are Android App Bundles signed with the upload key.
            var appBundle = GetArgument("-androidExportType") == "androidAppBundle";
            var extension = appBundle ? ".aab" : ".apk";
            var outputPath = GetArgument("-customBuildPath") ?? DefaultOutputPath;
            outputPath = Path.HasExtension(outputPath)
                ? Path.ChangeExtension(outputPath, extension)
                : Path.Combine(outputPath, "IranVsTuran" + extension);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));

            // GitHub's run number always increases, which is what Android expects from the version code.
            if (int.TryParse(Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER"), out var runNumber))
                PlayerSettings.Android.bundleVersionCode = runNumber;

            var version = GetArgument("-buildVersion");
            if (!string.IsNullOrEmpty(version) && version != "none")
                PlayerSettings.bundleVersion = version.TrimStart('v');

            EditorUserBuildSettings.buildAppBundle = appBundle;
            ConfigureSigning();

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new Exception("[IVT CI] No scenes in the build settings.");

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
                throw new Exception($"[IVT CI] Build {summary.result} with {summary.totalErrors} error(s).");

            Debug.Log($"[IVT CI] Built {outputPath} ({summary.totalSize / (1024 * 1024)} MB) in {summary.totalTime}.");
        }

        /// <summary>Uses the upload keystore passed by the release workflow; debug signing otherwise.</summary>
        static void ConfigureSigning()
        {
            var keystore = GetArgument("-androidKeystoreName");
            if (string.IsNullOrEmpty(keystore) || !File.Exists(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                return;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
            PlayerSettings.Android.keystorePass = GetArgument("-androidKeystorePass");
            PlayerSettings.Android.keyaliasName = GetArgument("-androidKeyaliasName");
            PlayerSettings.Android.keyaliasPass = GetArgument("-androidKeyaliasPass");
            Debug.Log("[IVT CI] Signing with " + Path.GetFileName(keystore) + ".");
        }

        static string[] MissingPackages()
        {
            var request = Wait(Client.List(offlineMode: true, includeIndirectDependencies: true));
            if (request.Status != StatusCode.Success)
                throw new Exception($"[IVT CI] Could not list packages: {request.Error?.message}");

            var installed = request.Result.Select(p => p.name).ToList();
            return ProjectSetup.RequiredPackages.Where(p => !installed.Contains(p)).ToArray();
        }

        /// <summary>Blocks until a Package Manager request finishes; there is no editor loop in batch mode.</summary>
        static T Wait<T>(T request) where T : Request
        {
            var deadline = DateTime.UtcNow + PackageTimeout;
            while (!request.IsCompleted)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("[IVT CI] Package Manager request timed out.");
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
