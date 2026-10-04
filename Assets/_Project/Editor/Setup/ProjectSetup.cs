using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arash.Editor.Setup
{
    /// <summary>
    /// One-time project configuration (feature F-01): editor defaults, Android player settings,
    /// input handling, render pipeline and the base scenes.
    /// Runs automatically once all packages are installed; can be re-run from Arash ▸ Setup.
    /// A marker file in ProjectSettings records which setup version has been applied, so other
    /// machines that clone the configured project skip it.
    /// </summary>
    public static class ProjectSetup
    {
        // Bump when a new setup step is added so existing checkouts pick it up.
        const int SetupVersion = 5;
        const string MarkerPath = "ProjectSettings/ArashProjectSetup.json";

        // The application ID is permanent once the game is published on Google Play.
        public const string CompanyName = "sam101aref";
        public const string ProductName = "Arash The Archer";
        public const string ApplicationId = "com.sam101aref.arashthearcher";
        public const string Version = "0.1.0";
        const int MinAndroidApiLevel = 24; // Android 7.0

        public const string ProjectRoot = "Assets/_Project";
        public const string ScenesFolder = ProjectRoot + "/Scenes";
        public const string SettingsFolder = ProjectRoot + "/Settings";

        // Order matters: the first scene is the one the game boots into.
        static readonly string[] SceneNames = { "Boot", "MainMenu", "WorldMap", "Battle", "Cutscene", "FinalFlight" };

        static readonly Color SkyColor = new Color(0.98f, 0.82f, 0.55f); // warm dawn over Damavand

        /// <summary>Set by the URP setup assembly, which only compiles once URP is installed.</summary>
        internal static Func<bool> ConfigureRenderPipeline;

        /// <summary>Set by the URP setup assembly to add a global 2D light to new scenes.</summary>
        internal static Action AddGlobalLight;

        /// <summary>
        /// Steps that build game content (prefabs, scene contents) once the base scenes exist.
        /// Registered by assemblies that depend on the game code. Each returns false on failure.
        /// </summary>
        internal static readonly List<Func<bool>> ContentSteps = new List<Func<bool>>();

        [Serializable]
        class Marker
        {
            public int version;
        }

        internal static void RunIfNeeded()
        {
            if (ReadMarkerVersion() >= SetupVersion)
                return;
            Configure();
        }

        [MenuItem("Arash/Setup/Configure Project", priority = 1)]
        static void ConfigureFromMenu() => Configure();

        /// <summary>Applies the whole setup. Returns false if a step could not be completed.</summary>
        public static bool Configure()
        {
            Debug.Log("[Arash Setup] Configuring project…");

            ConfigureEditor();
            ConfigurePlayer();
            ConfigureAndroid();
            var inputChanged = ConfigureInputHandling();

            var pipelineReady = ConfigureRenderPipeline != null && ConfigureRenderPipeline();
            if (!pipelineReady)
                Debug.LogWarning("[Arash Setup] URP is not configured yet. Install the packages, then run Arash ▸ Setup ▸ Configure Project again.");

            if (!CreateScenes())
                return false;

            var contentReady = ContentSteps.Count > 0;
            foreach (var step in ContentSteps)
                contentReady &= step();
            if (!contentReady)
                Debug.LogWarning("[Arash Setup] Game content was not built (game code not compiled yet, or a step failed).");

            AssetDatabase.SaveAssets();
            var complete = pipelineReady && contentReady;
            if (complete)
                WriteMarker(); // otherwise setup retries on the next reload

            SwitchToAndroid();

            Debug.Log("[Arash Setup] Done.");

            if (inputChanged && !Application.isBatchMode &&
                EditorUtility.DisplayDialog(
                    "Arash The Archer — Setup",
                    "The project now uses the new Input System. Unity must restart for this to take effect.",
                    "Restart now", "Later"))
            {
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            }

            return complete;
        }

        static void ConfigureEditor()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // The whole game is played in landscape.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        static void ConfigureAndroid()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // Never lower the minimum below what this Unity version already requires.
            if ((int)PlayerSettings.Android.minSdkVersion < MinAndroidApiLevel)
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinAndroidApiLevel;
        }

        /// <summary>Switches Active Input Handling to the Input System package. Returns true if it changed.</summary>
        static bool ConfigureInputHandling()
        {
            const int inputSystemOnly = 1;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
                return false;

            var settings = new SerializedObject(assets[0]);
            var handler = settings.FindProperty("activeInputHandler");
            if (handler == null || handler.intValue == inputSystemOnly)
                return false;

            handler.intValue = inputSystemOnly;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static bool CreateScenes()
        {
            var paths = SceneNames.Select(n => $"{ScenesFolder}/{n}.unity").ToArray();
            var missing = paths.Where(p => !File.Exists(p)).ToArray();

            if (missing.Length > 0)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[Arash Setup] Scene creation cancelled.");
                    return false;
                }

                Directory.CreateDirectory(ScenesFolder);
                foreach (var path in missing)
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    CreateCamera();
                    AddGlobalLight?.Invoke();
                    EditorSceneManager.SaveScene(scene, path);
                }

                EditorSceneManager.OpenScene(paths[0]);
            }

            EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            return true;
        }

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f; // 1080px tall at 100 pixels per unit
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;

            go.AddComponent<AudioListener>();
        }

        static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                return;

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogWarning("[Arash Setup] Android Build Support is not installed. Add it from Unity Hub ▸ Installs ▸ Add modules, then switch platform in File ▸ Build Profiles.");
                return;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        static int ReadMarkerVersion()
        {
            if (!File.Exists(MarkerPath))
                return 0;
            try
            {
                return JsonUtility.FromJson<Marker>(File.ReadAllText(MarkerPath)).version;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Arash Setup] Could not read {MarkerPath}: {e.Message}");
                return 0;
            }
        }

        static void WriteMarker()
        {
            File.WriteAllText(MarkerPath, JsonUtility.ToJson(new Marker { version = SetupVersion }, true));
        }
    }
}
