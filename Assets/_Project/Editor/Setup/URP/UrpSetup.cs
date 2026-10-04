using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arash.Editor.Setup
{
    /// <summary>
    /// Creates a URP asset with the 2D renderer and makes it the project's render pipeline.
    /// This assembly only compiles once the URP package is installed (ARASH_URP define constraint).
    /// The 2D renderer and Light2D types are looked up by name, because their assembly layout
    /// differs between URP versions.
    /// </summary>
    [InitializeOnLoad]
    static class UrpSetup
    {
        const string PipelineAssetPath = ProjectSetup.SettingsFolder + "/URP-2D.asset";
        const string RendererAssetPath = ProjectSetup.SettingsFolder + "/URP-2D-Renderer.asset";
        const string UrpPackagePath = "Packages/com.unity.render-pipelines.universal";
        const string DefaultPostProcessDataPath = UrpPackagePath + "/Runtime/Data/PostProcessData.asset";

        static UrpSetup()
        {
            ProjectSetup.ConfigureRenderPipeline = Configure;
            ProjectSetup.AddGlobalLight = AddGlobalLight;
        }

        static bool Configure()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
                return true;

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                pipeline = CreatePipelineAsset();
                if (pipeline == null)
                    return false;
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            Debug.Log($"[Arash Setup] Render pipeline set to {PipelineAssetPath}.");
            return true;
        }

        static UniversalRenderPipelineAsset CreatePipelineAsset()
        {
            var rendererType = TypeCache.GetTypesDerivedFrom<ScriptableRendererData>()
                .FirstOrDefault(t => t.Name == "Renderer2DData");
            if (rendererType == null)
            {
                Debug.LogError("[Arash Setup] URP 2D renderer (Renderer2DData) not found.");
                return null;
            }

            System.IO.Directory.CreateDirectory(ProjectSetup.SettingsFolder);

            var renderer = (ScriptableRendererData)ScriptableObject.CreateInstance(rendererType);
            AssetDatabase.CreateAsset(renderer, RendererAssetPath);
            ReloadNullResources(renderer);
            AssignDefaultPostProcessData(renderer);

            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        /// <summary>Mirrors what URP's own "Create ▸ URP Asset (with 2D Renderer)" menu does.</summary>
        static void ReloadNullResources(ScriptableObject asset)
        {
            var reloader = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEngine.Rendering.ResourceReloader"))
                .FirstOrDefault(t => t != null);
            var method = reloader?.GetMethod("TryReloadAllNullIn", new[] { typeof(object), typeof(string) });
            if (method == null)
                return;

            try
            {
                method.Invoke(null, new object[] { asset, UrpPackagePath });
                EditorUtility.SetDirty(asset);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Arash Setup] Could not reload renderer resources: {e.Message}");
            }
        }

        static void AssignDefaultPostProcessData(ScriptableObject renderer)
        {
            var serialized = new SerializedObject(renderer);
            var property = serialized.FindProperty("m_PostProcessData");
            if (property == null || property.objectReferenceValue != null)
                return;

            var data = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath);
            if (data == null)
                return;

            property.objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddGlobalLight()
        {
            var lightType = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .FirstOrDefault(t => t.Name == "Light2D" && t.Namespace == "UnityEngine.Rendering.Universal");
            if (lightType == null)
                return;

            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent(lightType);

            var typeProperty = lightType.GetProperty("lightType");
            if (typeProperty != null && typeProperty.CanWrite && typeProperty.PropertyType.IsEnum &&
                Enum.IsDefined(typeProperty.PropertyType, "Global"))
            {
                typeProperty.SetValue(light, Enum.Parse(typeProperty.PropertyType, "Global"));
            }
        }
    }
}
