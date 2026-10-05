using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IranVsTuran.Editor.Setup
{
    /// <summary>
    /// One-time project configuration: Android player settings, the Input System, and the single
    /// Boot scene (the game builds everything else in code at runtime, see GameRoot).
    /// Runs automatically when the project is opened; re-run from Iran vs Turan ▸ Configure Project.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const int SetupVersion = 1;
        const string MarkerPath = "ProjectSettings/IranVsTuranSetup.json";

        // The application ID is permanent once the game is published.
        public const string CompanyName = "sam101aref";
        public const string ProductName = "Iran vs Turan";
        public const string ApplicationId = "com.sam101aref.iranvsturan";
        public const string Version = "0.1.0";
        const int MinAndroidApiLevel = 24;

        public const string ScenePath = "Assets/Game/Scenes/Boot.unity";

        /// <summary>Packages the game needs; all are listed in Packages/manifest.json.</summary>
        internal static readonly string[] RequiredPackages =
        {
            "com.unity.inputsystem",
            "com.unity.ugui",
            "com.unity.test-framework",
        };

        [Serializable]
        class Marker
        {
            public int version;
        }

        static ProjectSetup()
        {
            if (Application.isBatchMode)
                return;
            EditorApplication.delayCall += () =>
            {
                if (ReadMarkerVersion() < SetupVersion)
                    Configure();
            };
        }

        [MenuItem("Iran vs Turan/Configure Project")]
        static void ConfigureFromMenu()
        {
            Configure();
        }

        public static bool Configure()
        {
            Debug.Log("[IVT Setup] Configuring project…");
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSettings.serializationMode = SerializationMode.ForceText;

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            if ((int)PlayerSettings.Android.minSdkVersion < MinAndroidApiLevel)
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinAndroidApiLevel;

            var inputChanged = ConfigureInputHandling();
            if (!CreateScene())
                return false;

            AssetDatabase.SaveAssets();
            File.WriteAllText(MarkerPath, JsonUtility.ToJson(new Marker { version = SetupVersion }, true));
            SwitchToAndroid();
            Debug.Log("[IVT Setup] Done.");

            if (inputChanged && !Application.isBatchMode &&
                EditorUtility.DisplayDialog(ProductName, "The project now uses the Input System. Unity must restart.", "Restart now", "Later"))
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            return true;
        }

        /// <summary>Switches Active Input Handling to the Input System package. Returns true if it changed.</summary>
        internal static bool ConfigureInputHandling()
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
            EditorUtility.SetDirty(assets[0]);
            return true;
        }

        /// <summary>The Boot scene only holds a camera; GameRoot creates the rest when the game starts.</summary>
        static bool CreateScene()
        {
            if (!File.Exists(ScenePath))
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return false;
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                go.transform.position = new Vector3(0f, 0f, -10f);
                var camera = go.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5.4f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.07f, 0.09f, 0.15f);
                go.AddComponent<AudioListener>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            return true;
        }

        static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                return;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogWarning("[IVT Setup] Android Build Support is not installed (Unity Hub ▸ Installs ▸ Add modules).");
                return;
            }
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        static int ReadMarkerVersion()
        {
            try
            {
                return File.Exists(MarkerPath) ? JsonUtility.FromJson<Marker>(File.ReadAllText(MarkerPath)).version : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
