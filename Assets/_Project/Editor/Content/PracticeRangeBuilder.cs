using System.IO;
using System.Linq;
using Arash.Combat;
using Arash.Core;
using Arash.Editor.Setup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arash.Editor.Content
{
    /// <summary>
    /// Builds the placeholder practice range used to test aiming, arrow flight and the camera
    /// (F-02 – F-05): placeholder sprites, the Arrow and Arash prefabs, and the contents of the
    /// Battle and Boot scenes. Runs as part of project setup and skips anything that already exists,
    /// so hand edits are kept. Placeholder art is replaced once real art arrives.
    /// </summary>
    [InitializeOnLoad]
    static class PracticeRangeBuilder
    {
        const float GroundY = -3f;

        const string PlaceholderFolder = ProjectSetup.ProjectRoot + "/Art/Placeholder";
        const string SquarePath = PlaceholderFolder + "/Square.png";
        const string CirclePath = PlaceholderFolder + "/Circle.png";
        const string PrefabFolder = ProjectSetup.ProjectRoot + "/Prefabs/Combat";
        const string ArrowPrefabPath = PrefabFolder + "/Arrow.prefab";
        const string ArashPrefabPath = PrefabFolder + "/Arash.prefab";
        const string BattleScenePath = ProjectSetup.ScenesFolder + "/Battle.unity";
        const string BootScenePath = ProjectSetup.ScenesFolder + "/Boot.unity";
        const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        static readonly Color Tunic = new Color(0.10f, 0.42f, 0.55f);    // Persian teal
        static readonly Color Skin = new Color(0.87f, 0.68f, 0.52f);
        static readonly Color Wood = new Color(0.45f, 0.29f, 0.15f);
        static readonly Color Steel = new Color(0.75f, 0.75f, 0.78f);
        static readonly Color Feather = new Color(0.72f, 0.16f, 0.12f);
        static readonly Color Straw = new Color(0.86f, 0.73f, 0.40f);
        static readonly Color Earth = new Color(0.55f, 0.43f, 0.28f);
        static readonly Color Lantern = new Color(1.00f, 0.78f, 0.25f);

        static Sprite s_Square;
        static Sprite s_Circle;
        static Material s_Material;

        static PracticeRangeBuilder()
        {
            ProjectSetup.ContentSteps.Add(Build);
        }

        [MenuItem("Arash/Setup/Rebuild Practice Range Prefabs", priority = 20)]
        static void RebuildPrefabs()
        {
            AssetDatabase.DeleteAsset(ArrowPrefabPath);
            AssetDatabase.DeleteAsset(ArashPrefabPath);
            Build();
        }

        static bool Build()
        {
            s_Square = EnsureSprite(SquarePath, 32, circle: false);
            s_Circle = EnsureSprite(CirclePath, 64, circle: true);
            s_Material = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (s_Square == null || s_Circle == null)
            {
                Debug.LogError("[Arash Setup] Could not create placeholder sprites.");
                return false;
            }

            Directory.CreateDirectory(PrefabFolder);
            var arrow = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath) ?? CreateArrowPrefab();
            var arash = AssetDatabase.LoadAssetAtPath<GameObject>(ArashPrefabPath) ?? CreateArashPrefab(arrow);

            BuildBattleScene(arash);
            BuildBootScene();
            return true;
        }

        // ---------------------------------------------------------------- sprites

        static Sprite EnsureSprite(string path, int size, bool circle)
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                var radius = size * 0.5f;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    byte alpha = 255;
                    if (circle)
                    {
                        var distance = new Vector2(x + 0.5f - radius, y + 0.5f - radius).magnitude;
                        alpha = (byte)(Mathf.Clamp01(radius - distance) * 255f);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size; // one unit across
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; // needed for sliced drawing
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static GameObject Box(string name, Transform parent, Vector2 localPosition, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var renderer = AddRenderer(go, s_Square, color, order);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            return go;
        }

        static GameObject Disc(string name, Transform parent, Vector2 localPosition, float diameter, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            AddRenderer(go, s_Circle, color, order);
            return go;
        }

        static SpriteRenderer AddRenderer(GameObject go, Sprite sprite, Color color, int order)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (s_Material != null)
                renderer.sharedMaterial = s_Material;
            return renderer;
        }

        // ---------------------------------------------------------------- prefabs

        static GameObject CreateArrowPrefab()
        {
            // The pivot is the arrow tip: that is the point the arrow simulates and line-casts.
            var root = new GameObject("Arrow");
            root.AddComponent<Arrow>();
            Box("Shaft", root.transform, new Vector2(-0.45f, 0f), new Vector2(0.9f, 0.05f), Wood, 40);
            var head = Box("Head", root.transform, new Vector2(-0.05f, 0f), new Vector2(0.11f, 0.11f), Steel, 41);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Box("Fletching", root.transform, new Vector2(-0.84f, 0f), new Vector2(0.18f, 0.12f), Feather, 41);
            return SavePrefab(root, ArrowPrefabPath);
        }

        static GameObject CreateArashPrefab(GameObject arrowPrefab)
        {
            var root = new GameObject("Arash");
            var collider = root.AddComponent<BoxCollider2D>();
            collider.offset = new Vector2(0f, 1.1f);
            collider.size = new Vector2(0.6f, 2.2f);

            Box("Legs", root.transform, new Vector2(0f, 0.4f), new Vector2(0.45f, 0.8f), Wood, 30);
            Box("Body", root.transform, new Vector2(0f, 1.25f), new Vector2(0.6f, 0.9f), Tunic, 31);
            Disc("Head", root.transform, new Vector2(0f, 1.95f), 0.5f, Skin, 32);

            var pivot = new GameObject("AimPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector2(0.1f, 1.55f);
            Box("Arm", pivot, new Vector2(0.3f, 0f), new Vector2(0.6f, 0.12f), Skin, 33);
            var bowObject = Box("Bow", pivot, new Vector2(0.62f, 0f), new Vector2(0.1f, 1.1f), Wood, 34);
            var launchPoint = new GameObject("LaunchPoint").transform;
            launchPoint.SetParent(pivot, false);
            launchPoint.localPosition = new Vector2(0.72f, 0f);

            var bow = bowObject.AddComponent<Bow>();
            Wire(bow, "arrowPrefab", arrowPrefab.GetComponent<Arrow>());
            Wire(bow, "launchPoint", launchPoint);
            Wire(bow, "owner", root.transform);

            var rig = root.AddComponent<ArcherRig>();
            Wire(rig, "aimPivot", pivot);

            var previewObject = new GameObject("TrajectoryPreview");
            previewObject.transform.SetParent(root.transform, false);
            var preview = previewObject.AddComponent<TrajectoryPreview>();
            Wire(preview, "dotSprite", s_Circle);

            var aim = root.AddComponent<AimController>();
            Wire(aim, "bow", bow);
            Wire(aim, "preview", preview);
            Wire(aim, "rig", rig);

            return SavePrefab(root, ArashPrefabPath);
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void Wire(Object component, string field, Object value)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Arash Setup] {component.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- scenes

        static void BuildBattleScene(GameObject arashPrefab)
        {
            if (!File.Exists(BattleScenePath))
                return;

            var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            if (scene.GetRootGameObjects().Any(go => go.GetComponent<PracticeRange>() != null))
                return;

            var ground = Box("Ground", null, new Vector2(90f, GroundY - 5f), new Vector2(240f, 10f), Earth, 10);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(240f, 10f);

            var arash = (GameObject)PrefabUtility.InstantiatePrefab(arashPrefab, scene);
            arash.transform.position = new Vector2(-6f, GroundY);

            var targets = new GameObject("Targets").transform;
            CreateDummy(targets, 8f);
            CreateDummy(targets, 15f);
            CreateDummy(targets, 25f);
            var lantern = Disc("Lantern", targets, new Vector2(12f, 4f), 0.6f, Lantern, 20);
            lantern.AddComponent<CircleCollider2D>().radius = 0.5f;

            var camera = scene.GetRootGameObjects().Select(go => go.GetComponent<Camera>()).FirstOrDefault(c => c != null);
            BattleCamera battleCamera = null;
            if (camera != null)
            {
                battleCamera = camera.gameObject.AddComponent<BattleCamera>();
                Wire(battleCamera, "home", arash.transform);
            }
            else
            {
                Debug.LogWarning("[Arash Setup] Battle scene has no camera; the BattleCamera was not added.");
            }

            var range = new GameObject("PracticeRange").AddComponent<PracticeRange>();
            Wire(range, "aim", arash.GetComponent<AimController>());
            Wire(range, "battleCamera", battleCamera);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Arash Setup] Practice range added to the Battle scene.");
        }

        static void CreateDummy(Transform parent, float x)
        {
            var root = new GameObject("Target Dummy").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector2(x, GroundY);

            Box("Post", root, new Vector2(0f, 0.5f), new Vector2(0.15f, 1f), Wood, 20);
            var body = Box("Body", root, new Vector2(0f, 1.4f), new Vector2(0.7f, 1f), Straw, 21);
            body.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1f);
            var head = Disc("Head", root, new Vector2(0f, 2.15f), 0.5f, Straw, 21);
            head.AddComponent<CircleCollider2D>().radius = 0.5f;
        }

        static void BuildBootScene()
        {
            if (!File.Exists(BootScenePath))
                return;

            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            if (scene.GetRootGameObjects().Any(go => go.GetComponent<Bootstrap>() != null))
                return;

            new GameObject("Bootstrap").AddComponent<Bootstrap>();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
