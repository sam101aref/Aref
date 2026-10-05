using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Siavosh.Editor.Setup
{
    /// <summary>
    /// One-time project configuration: player and Android settings, input handling, the single game
    /// scene and the app icon. The game builds everything else in code at runtime, so this is all the
    /// editor has to prepare. Runs automatically after scripts compile; can be re-run from
    /// Siavosh ▸ Setup. A marker file in ProjectSettings records the applied setup version.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        // Bump when a setup step changes so existing checkouts pick it up.
        const int SetupVersion = 1;
        const string MarkerPath = "ProjectSettings/SiavoshProjectSetup.json";

        // The application ID is permanent once the game is published on Google Play.
        public const string CompanyName = "sam101aref";
        public const string ProductName = "Siavosh the Prince";
        public const string ApplicationId = "com.sam101aref.siavoshtheprince";
        public const string Version = "0.1.0";
        const int MinAndroidApiLevel = 24; // Android 7.0

        public const string ProjectRoot = "Assets/_Project";
        public const string ScenePath = ProjectRoot + "/Scenes/Main.unity";
        public const string IconPath = ProjectRoot + "/Art/Icon.png";

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

        [MenuItem("Siavosh/Setup/Configure Project")]
        static void ConfigureFromMenu() => Configure();

        /// <summary>Applies the whole setup. Returns false if a step could not be completed.</summary>
        public static bool Configure()
        {
            Debug.Log("[Siavosh Setup] Configuring project…");

            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";

            ConfigurePlayer();
            ConfigureAndroid();
            ConfigureInputHandling();

            // Sprites and uGUI only: the built-in pipeline keeps the build small and simple.
            GraphicsSettings.defaultRenderPipeline = null;

            if (!CreateScene())
                return false;
            SetIcon();

            AssetDatabase.SaveAssets();
            WriteMarker();
            SwitchToAndroid();
            Debug.Log("[Siavosh Setup] Done.");
            return true;
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            // Gamma keeps the manuscript palette exactly as painted (UI colours are not linearised).
            PlayerSettings.colorSpace = ColorSpace.Gamma;

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
            if ((int)PlayerSettings.Android.minSdkVersion < MinAndroidApiLevel)
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinAndroidApiLevel;

            var framePacing = typeof(PlayerSettings.Android).GetProperty("optimizedFramePacing");
            if (framePacing != null && framePacing.CanWrite)
                framePacing.SetValue(null, true);

            var currentQuality = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 0;
            }
            QualitySettings.SetQualityLevel(currentQuality, false);
        }

        /// <summary>
        /// The game reads touches through the classic Input Manager. Returns true if the setting
        /// changed, which only takes effect after Unity restarts (the CI does this in its first pass).
        /// </summary>
        internal static bool ConfigureInputHandling()
        {
            const int inputManager = 0;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
                return false;

            var settings = new SerializedObject(assets[0]);
            var handler = settings.FindProperty("activeInputHandler");
            if (handler == null || handler.intValue == inputManager)
                return false;

            handler.intValue = inputManager;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(assets[0]);
            return true;
        }

        /// <summary>An empty scene: the game object tree is created by Siavosh.Core.Game at startup.</summary>
        static bool CreateScene()
        {
            if (!File.Exists(ScenePath))
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[Siavosh Setup] Scene creation cancelled.");
                    return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            return true;
        }

        static void SetIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            else
                Debug.LogWarning("[Siavosh Setup] App icon not found at " + IconPath);
        }

        static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                return;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogWarning("[Siavosh Setup] Android Build Support is not installed (Unity Hub ▸ Installs ▸ Add modules).");
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
                Debug.LogWarning($"[Siavosh Setup] Could not read {MarkerPath}: {e.Message}");
                return 0;
            }
        }

        static void WriteMarker()
        {
            File.WriteAllText(MarkerPath, JsonUtility.ToJson(new Marker { version = SetupVersion }, true));
        }
    }
}
