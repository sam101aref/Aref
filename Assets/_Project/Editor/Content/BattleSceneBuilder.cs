using System.IO;
using System.Linq;
using Arash.Combat;
using Arash.Core;
using Arash.Editor.Setup;
using Arash.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arash.Editor.Content
{
    /// <summary>
    /// Builds the placeholder duel used to test combat (F-02 – F-10): placeholder sprites, the Arrow
    /// prefab, the Arash and Turanian archer prefabs (ragdoll body parts with hit zones), and the
    /// contents of the Battle and Boot scenes. Runs as part of project setup and skips anything that
    /// is already up to date, so hand edits are kept. Placeholder art is replaced once real art arrives.
    /// </summary>
    [InitializeOnLoad]
    static class BattleSceneBuilder
    {
        const float GroundY = -3f;
        const float PlayerX = -6f;
        const float EnemyX = 16f;

        const string PlaceholderFolder = ProjectSetup.ProjectRoot + "/Art/Placeholder";
        const string SquarePath = PlaceholderFolder + "/Square.png";
        const string CirclePath = PlaceholderFolder + "/Circle.png";
        const string PrefabFolder = ProjectSetup.ProjectRoot + "/Prefabs/Combat";
        const string ArrowPrefabPath = PrefabFolder + "/Arrow.prefab";
        const string ArashPrefabPath = PrefabFolder + "/Arash.prefab";
        const string EnemyPrefabPath = PrefabFolder + "/TuranianArcher.prefab";
        const string BattleScenePath = ProjectSetup.ScenesFolder + "/Battle.unity";
        const string BootScenePath = ProjectSetup.ScenesFolder + "/Boot.unity";
        const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        static readonly Color PersianTeal = new Color(0.10f, 0.42f, 0.55f);
        static readonly Color TuranianRed = new Color(0.55f, 0.15f, 0.12f);
        static readonly Color Skin = new Color(0.87f, 0.68f, 0.52f);
        static readonly Color Wood = new Color(0.45f, 0.29f, 0.15f);
        static readonly Color Trousers = new Color(0.28f, 0.22f, 0.18f);
        static readonly Color Steel = new Color(0.75f, 0.75f, 0.78f);
        static readonly Color Feather = new Color(0.72f, 0.16f, 0.12f);
        static readonly Color Earth = new Color(0.55f, 0.43f, 0.28f);
        static readonly Color BarBackground = new Color(0.1f, 0.08f, 0.06f, 0.8f);
        static readonly Color BarFill = new Color(0.35f, 0.8f, 0.3f);

        static Sprite s_Square;
        static Sprite s_Circle;
        static Material s_Material;

        static BattleSceneBuilder()
        {
            ProjectSetup.ContentSteps.Add(Build);
        }

        [MenuItem("Arash/Setup/Rebuild Battle Prefabs", priority = 20)]
        static void RebuildPrefabs()
        {
            AssetDatabase.DeleteAsset(ArrowPrefabPath);
            AssetDatabase.DeleteAsset(ArashPrefabPath);
            AssetDatabase.DeleteAsset(EnemyPrefabPath);
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
            var arrow = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            if (arrow == null)
                arrow = CreateArrowPrefab();

            var arash = LoadArcher(ArashPrefabPath);
            if (arash == null)
                arash = CreateArcherPrefab("Arash", ArashPrefabPath, true, PersianTeal, arrow);
            var enemy = LoadArcher(EnemyPrefabPath);
            if (enemy == null)
                enemy = CreateArcherPrefab("Turanian Archer", EnemyPrefabPath, false, TuranianRed, arrow);

            BuildBattleScene(arash, enemy);
            BuildBootScene();
            return true;
        }

        /// <summary>Loads an archer prefab, discarding ones made by an older builder (no Combatant).</summary>
        static GameObject LoadArcher(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<Combatant>() == null)
            {
                AssetDatabase.DeleteAsset(path);
                return null;
            }
            return prefab;
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

        /// <summary>
        /// Builds an archer whose body parts are kinematic rigidbodies joined to the torso by hinges
        /// (they become a ragdoll on death). Parts are siblings under the root so physics never fights
        /// the hierarchy. A left-facing archer is built mirrored rather than with a negative scale.
        /// </summary>
        static GameObject CreateArcherPrefab(string name, string path, bool isPlayer, Color tunic, GameObject arrowPrefab)
        {
            var facing = isPlayer ? 1f : -1f;
            var root = new GameObject(name);
            root.AddComponent<Health>();

            var torso = Box("Torso", root.transform, new Vector2(0f, 1.25f), new Vector2(0.6f, 0.9f), tunic, 31);
            var torsoBody = AddBody(torso, 2f);
            torso.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.9f);
            AddZone(torso, HitZoneType.Torso);

            var legs = Box("Legs", root.transform, new Vector2(0f, 0.4f), new Vector2(0.45f, 0.8f), Trousers, 30);
            AddBody(legs, 1f);
            legs.AddComponent<BoxCollider2D>().size = new Vector2(0.45f, 0.8f);
            AddZone(legs, HitZoneType.Limb);
            AddHinge(legs, torsoBody, new Vector2(0f, 0.4f), 25f);

            var head = Disc("Head", root.transform, new Vector2(0f, 1.95f), 0.5f, Skin, 32);
            AddBody(head, 0.6f);
            head.AddComponent<CircleCollider2D>().radius = 0.5f;
            AddZone(head, HitZoneType.Head);
            AddHinge(head, torsoBody, new Vector2(0f, -0.5f), 30f);

            var pivot = new GameObject("AimPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector2(0.1f * facing, 1.55f);
            AddBody(pivot, 0.5f);
            AddHinge(pivot, torsoBody, Vector2.zero, 0f);
            var arm = Box("Arm", pivot.transform, new Vector2(0.3f, 0f), new Vector2(0.6f, 0.12f), Skin, 33);
            arm.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.12f);
            AddZone(arm, HitZoneType.Limb);
            var bowObject = Box("Bow", pivot.transform, new Vector2(0.62f, 0f), new Vector2(0.1f, 1.1f), Wood, 34);
            var launchPoint = new GameObject("LaunchPoint").transform;
            launchPoint.SetParent(pivot.transform, false);
            launchPoint.localPosition = new Vector2(0.72f, 0f);

            var bar = new GameObject("HealthBar");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector2(0f, 2.75f);
            Box("Background", bar.transform, Vector2.zero, new Vector2(1.24f, 0.2f), BarBackground, 60);
            var fill = Box("Fill", bar.transform, Vector2.zero, new Vector2(1.16f, 0.12f), BarFill, 61);
            var healthBar = bar.AddComponent<HealthBar>();
            Wire(healthBar, "fill", fill.GetComponent<SpriteRenderer>());

            var bow = bowObject.AddComponent<Bow>();
            Wire(bow, "arrowPrefab", arrowPrefab.GetComponent<Arrow>());
            Wire(bow, "launchPoint", launchPoint);
            Wire(bow, "owner", root.transform);

            var rig = root.AddComponent<ArcherRig>();
            Wire(rig, "aimPivot", pivot.transform);
            WireBool(rig, "facingRight", isPlayer);

            var combatant = root.AddComponent<Combatant>();
            WireEnum(combatant, "team", (int)(isPlayer ? Team.Player : Team.Enemy));
            WireBool(combatant, "facingRight", isPlayer);
            Wire(combatant, "bodyTarget", torso.transform);
            Wire(combatant, "headTarget", head.transform);

            Behaviour controller;
            if (isPlayer)
            {
                var previewObject = new GameObject("TrajectoryPreview");
                previewObject.transform.SetParent(root.transform, false);
                var preview = previewObject.AddComponent<TrajectoryPreview>();
                Wire(preview, "dotSprite", s_Circle);

                var aim = root.AddComponent<AimController>();
                Wire(aim, "bow", bow);
                Wire(aim, "preview", preview);
                Wire(aim, "rig", rig);
                WireBool(aim, "facingRight", true);
                controller = aim;
            }
            else
            {
                var ai = root.AddComponent<EnemyArcherAI>();
                Wire(ai, "bow", bow);
                Wire(ai, "rig", rig);
                controller = ai;
            }

            var ragdoll = root.AddComponent<Ragdoll2D>();
            WireArray(ragdoll, "disableOnDeath", new Object[] { rig, controller });

            return SavePrefab(root, path);
        }

        static Rigidbody2D AddBody(GameObject go, float mass)
        {
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.mass = mass;
            return body;
        }

        static void AddZone(GameObject go, HitZoneType zone)
        {
            WireEnum(go.AddComponent<HitZone>(), "zone", (int)zone);
        }

        static void AddHinge(GameObject go, Rigidbody2D connectedTo, Vector2 anchor, float limit)
        {
            var hinge = go.AddComponent<HingeJoint2D>();
            hinge.connectedBody = connectedTo;
            hinge.autoConfigureConnectedAnchor = true;
            hinge.anchor = anchor;
            hinge.enableCollision = false;
            if (limit > 0f)
            {
                hinge.useLimits = true;
                hinge.limits = new JointAngleLimits2D { min = -limit, max = limit };
            }
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

        static void WireBool(Object component, string field, bool value)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Arash Setup] {component.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireEnum(Object component, string field, int value)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Arash Setup] {component.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireArray(Object component, string field, Object[] values)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(field);
            if (property == null || !property.isArray)
            {
                Debug.LogError($"[Arash Setup] {component.GetType().Name} has no serialized array '{field}'.");
                return;
            }
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- scenes

        static void BuildBattleScene(GameObject arashPrefab, GameObject enemyPrefab)
        {
            if (!File.Exists(BattleScenePath))
                return;

            var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            if (roots.Any(go => go.GetComponent<TurnManager>() != null))
                return;

            // Replace anything an older builder generated; keep the camera and the global light.
            foreach (var go in roots)
                if (go.GetComponent<Camera>() == null && !go.name.Contains("Light"))
                    Object.DestroyImmediate(go);

            var ground = Box("Ground", null, new Vector2(90f, GroundY - 5f), new Vector2(240f, 10f), Earth, 10);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(240f, 10f);

            var arash = (GameObject)PrefabUtility.InstantiatePrefab(arashPrefab, scene);
            arash.transform.position = new Vector2(PlayerX, GroundY);
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
            enemy.transform.position = new Vector2(EnemyX, GroundY);

            var camera = roots.Select(go => go != null ? go.GetComponent<Camera>() : null).FirstOrDefault(c => c != null);
            BattleCamera battleCamera = null;
            if (camera != null)
            {
                battleCamera = camera.GetComponent<BattleCamera>();
                if (battleCamera == null)
                    battleCamera = camera.gameObject.AddComponent<BattleCamera>();
                Wire(battleCamera, "home", arash.transform);
            }
            else
            {
                Debug.LogWarning("[Arash Setup] Battle scene has no camera; the BattleCamera was not added.");
            }

            var battle = new GameObject("Battle");
            var turns = battle.AddComponent<TurnManager>();
            Wire(turns, "player", arash.GetComponent<Combatant>());
            WireArray(turns, "enemies", new Object[] { enemy.GetComponent<Combatant>() });
            Wire(turns, "battleCamera", battleCamera);

            var hud = battle.AddComponent<BattleHud>();
            Wire(hud, "turnManager", turns);

            var feedback = battle.AddComponent<CombatFeedback>();
            Wire(feedback, "battleCamera", battleCamera);
            Wire(feedback, "hud", hud);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Arash Setup] Duel added to the Battle scene.");
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
