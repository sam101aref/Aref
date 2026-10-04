using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Arash.Editor.Setup
{
    /// <summary>
    /// Installs the Unity packages the game depends on. Packages are added by name only, so the
    /// Package Manager picks the version that matches the installed Unity 6 editor.
    /// When everything is installed it hands over to <see cref="ProjectSetup"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class PackageInstaller
    {
        internal static readonly string[] RequiredPackages =
        {
            "com.unity.render-pipelines.universal", // URP with the 2D renderer
            "com.unity.inputsystem",
            "com.unity.cinemachine",
            "com.unity.2d.sprite",
            "com.unity.2d.animation",                // skeletal 2D animation, needed for ragdolls
            "com.unity.2d.psdimporter",             // layered character art straight from Photoshop/Krita
            "com.unity.timeline",                   // cutscenes
            "com.unity.test-framework",
            "com.unity.ide.visualstudio",
            "com.unity.ide.rider",
        };

        const string DeclinedKey = "Arash.Setup.PackagesDeclined";

        static ListRequest s_ListRequest;
        static AddAndRemoveRequest s_AddRequest;
        static bool s_Forced;

        static PackageInstaller()
        {
            if (Application.isBatchMode)
                return;
            EditorApplication.delayCall += () => CheckPackages(forced: false);
        }

        [MenuItem("Arash/Setup/Install Required Packages", priority = 0)]
        static void InstallFromMenu() => CheckPackages(forced: true);

        static void CheckPackages(bool forced)
        {
            if (s_ListRequest != null || s_AddRequest != null)
                return;
            if (!forced && SessionState.GetBool(DeclinedKey, false))
                return;

            s_Forced = forced;
            s_ListRequest = Client.List(offlineMode: true, includeIndirectDependencies: true);
            EditorApplication.update += WaitForList;
        }

        static void WaitForList()
        {
            if (!s_ListRequest.IsCompleted)
                return;

            EditorApplication.update -= WaitForList;
            var request = s_ListRequest;
            s_ListRequest = null;

            if (request.Status != StatusCode.Success)
            {
                Debug.LogError($"[Arash Setup] Could not list packages: {request.Error?.message}");
                return;
            }

            var installed = new HashSet<string>(request.Result.Select(p => p.name));
            var missing = RequiredPackages.Where(p => !installed.Contains(p)).ToArray();

            if (missing.Length == 0)
            {
                if (s_Forced)
                    Debug.Log("[Arash Setup] All required packages are already installed.");
                ProjectSetup.RunIfNeeded();
                return;
            }

            var accepted = EditorUtility.DisplayDialog(
                "Arash The Archer — Setup",
                "This project needs the following Unity packages:\n\n" + string.Join("\n", missing) +
                "\n\nInstall them now? Unity will recompile afterwards.",
                "Install", "Later");

            if (!accepted)
            {
                SessionState.SetBool(DeclinedKey, true);
                Debug.LogWarning("[Arash Setup] Packages not installed. Use Arash ▸ Setup ▸ Install Required Packages.");
                return;
            }

            s_AddRequest = Client.AddAndRemove(missing);
            EditorApplication.update += WaitForAdd;
        }

        static void WaitForAdd()
        {
            if (!s_AddRequest.IsCompleted)
                return;

            EditorApplication.update -= WaitForAdd;
            var request = s_AddRequest;
            s_AddRequest = null;

            if (request.Status == StatusCode.Success)
                Debug.Log("[Arash Setup] Packages installed. Project setup continues after recompilation.");
            else
                Debug.LogError($"[Arash Setup] Package installation failed: {request.Error?.message}");
        }
    }
}
