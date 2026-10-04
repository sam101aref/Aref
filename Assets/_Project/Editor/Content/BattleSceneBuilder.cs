using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arash.Combat;
using Arash.Core;
using Arash.Editor.Setup;
using Arash.Flight;
using Arash.Levels;
using Arash.Story;
using Arash.UI;
using UnityEditor.Build;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arash.Editor.Content
{
    /// <summary>
    /// Builds the placeholder game content: sprites and app icon, the Arrow and archer prefabs
    /// (ragdoll body parts with hit zones), the prologue levels and level catalog (F-11, F-17), and
    /// the contents of the Boot, MainMenu, WorldMap and Battle scenes. Runs as part of project setup
    /// and skips anything that is already up to date, so hand edits are kept. Placeholder art is
    /// replaced once real art arrives.
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
        const string CompanionPrefabPath = PrefabFolder + "/Companion.prefab";
        const string SpearPrefabPath = PrefabFolder + "/Spear.prefab";
        const string FireArrowPrefabPath = PrefabFolder + "/FireArrow.prefab";
        const string StonePrefabPath = PrefabFolder + "/Stone.prefab";
        const string BoulderPrefabPath = PrefabFolder + "/Boulder.prefab";
        const string CutsceneScenePath = ProjectSetup.ScenesFolder + "/Cutscene.unity";
        const string FlightScenePath = ProjectSetup.ScenesFolder + "/FinalFlight.unity";
        const string UICirclePath = ProjectSetup.ProjectRoot + "/Resources/UI/Circle.png";
        const string BattleScenePath = ProjectSetup.ScenesFolder + "/Battle.unity";
        const string BootScenePath = ProjectSetup.ScenesFolder + "/Boot.unity";
        const string MainMenuScenePath = ProjectSetup.ScenesFolder + "/MainMenu.unity";
        const string WorldMapScenePath = ProjectSetup.ScenesFolder + "/WorldMap.unity";
        const string StarPath = ProjectSetup.ProjectRoot + "/Resources/UI/Star.png";
        const string IconPath = ProjectSetup.ProjectRoot + "/Art/Placeholder/Icon.png";
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
        static readonly Color Lapis = new Color(0.07f, 0.12f, 0.25f);

        static Sprite s_Square;
        static Sprite s_Circle;
        static Material s_Material;

        static BattleSceneBuilder()
        {
            ProjectSetup.ContentSteps.Add(Build);
        }

        static readonly string[] AllPrefabPaths =
        {
            ArrowPrefabPath, ArashPrefabPath, EnemyPrefabPath, CompanionPrefabPath,
            SpearPrefabPath, FireArrowPrefabPath, StonePrefabPath, BoulderPrefabPath,
        };

        enum Role
        {
            Player,
            Enemy,
            Companion,
        }

        [MenuItem("Arash/Setup/Rebuild Battle Prefabs", priority = 20)]
        static void RebuildPrefabs()
        {
            foreach (var path in AllPrefabPaths)
                AssetDatabase.DeleteAsset(path);
            Build();
        }

        static bool Build()
        {
            s_Square = EnsureSprite(SquarePath, 32, (u, v) => 1f);
            s_Circle = EnsureSprite(CirclePath, 64, Disc);
            EnsureSprite(StarPath, 128, StarShape);
            EnsureSprite(UICirclePath, 128, Disc);
            EnsureIcon();
            s_Material = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (s_Square == null || s_Circle == null)
            {
                Debug.LogError("[Arash Setup] Could not create placeholder sprites.");
                return false;
            }

            Directory.CreateDirectory(PrefabFolder);
            DiscardOutdatedPrefabs();
            var arrow = LoadOrCreate(ArrowPrefabPath, () => CreateArrowPrefab());
            var projectiles = new StoryContent.Projectiles
            {
                Spear = LoadOrCreate(SpearPrefabPath, () => CreateSpearPrefab()).GetComponent<Arrow>(),
                FireArrow = LoadOrCreate(FireArrowPrefabPath, () => CreateFireArrowPrefab()).GetComponent<Arrow>(),
                Stone = LoadOrCreate(StonePrefabPath, () => CreateStonePrefab(StonePrefabPath, "Stone", 0.28f, new Color(0.5f, 0.48f, 0.45f), 120f)).GetComponent<Arrow>(),
                Boulder = LoadOrCreate(BoulderPrefabPath, () => CreateStonePrefab(BoulderPrefabPath, "Boulder", 0.7f, new Color(0.6f, 0.62f, 0.66f), 200f)).GetComponent<Arrow>(),
            };

            var arash = LoadOrCreate(ArashPrefabPath, () => CreateArcherPrefab("Arash", ArashPrefabPath, Role.Player, PersianTeal, arrow));
            var enemy = LoadOrCreate(EnemyPrefabPath, () => CreateArcherPrefab("Turanian Archer", EnemyPrefabPath, Role.Enemy, TuranianRed, arrow));
            var companion = LoadOrCreate(CompanionPrefabPath, () => CreateArcherPrefab("Companion", CompanionPrefabPath, Role.Companion, Color.white, arrow));

            var catalog = StoryContent.EnsureCatalog(projectiles);
            BuildBattleScene(arash, enemy, companion);
            BuildCutsceneScene();
            BuildFlightScene();
            BuildMenuScene<MainMenuScreen>(MainMenuScenePath, "Main Menu");
            BuildMenuScene<WorldMapScreen>(WorldMapScenePath, "World Map");
            BuildBootScene();
            return catalog != null;
        }

        /// <summary>
        /// Prefabs from an older builder are rebuilt: the arrow without an intercept collider (before
        /// escort levels) or archers without a Combatant (before the duel). Archers reference the arrow,
        /// so everything is rebuilt together.
        /// </summary>
        static void DiscardOutdatedPrefabs()
        {
            var arrow = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            var archer = AssetDatabase.LoadAssetAtPath<GameObject>(ArashPrefabPath);
            var outdated = (arrow != null && arrow.GetComponent<Collider2D>() == null) ||
                           (archer != null && archer.GetComponent<Combatant>() == null);
            if (!outdated)
                return;
            foreach (var path in AllPrefabPaths)
                AssetDatabase.DeleteAsset(path);
        }

        static GameObject LoadOrCreate(string path, Func<GameObject> create)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab : create();
        }

        // ---------------------------------------------------------------- sprites

        /// <summary>
        /// Creates a white sprite whose alpha is <paramref name="coverage"/>(u, v) for pixel centres in
        /// 0–1 space (4× supersampled), unless the file already exists.
        /// </summary>
        static Sprite EnsureSprite(string path, int size, Func<float, float, float> coverage)
        {
            if (!File.Exists(path))
            {
                WritePng(path, size, (u, v) => new Color(1f, 1f, 1f, Supersample(size, u, v, coverage)));
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

        static void WritePng(string path, int size, Func<float, float, Color> pixel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                pixels[y * size + x] = pixel((x + 0.5f) / size, (y + 0.5f) / size);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        static float Supersample(int size, float u, float v, Func<float, float, float> coverage)
        {
            var step = 1f / (size * 4f);
            var sum = 0f;
            for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
                sum += coverage(u + (i - 1.5f) * step, v + (j - 1.5f) * step);
            return Mathf.Clamp01(sum / 16f);
        }

        static float Disc(float u, float v)
        {
            return new Vector2(u - 0.5f, v - 0.5f).magnitude <= 0.5f ? 1f : 0f;
        }

        /// <summary>Five-pointed star, point up.</summary>
        static float StarShape(float u, float v)
        {
            var p = new Vector2(u - 0.5f, v - 0.47f);
            var corners = new Vector2[10];
            for (var i = 0; i < 10; i++)
            {
                var angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                var radius = i % 2 == 0 ? 0.5f : 0.2f;
                corners[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            var inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                if ((corners[i].y > p.y) != (corners[j].y > p.y) &&
                    p.x < (corners[j].x - corners[i].x) * (p.y - corners[i].y) / (corners[j].y - corners[i].y) + corners[i].x)
                    inside = !inside;
            }
            return inside ? 1f : 0f;
        }

        /// <summary>Placeholder app icon (F-18): a golden arrow on lapis, set as the default icon.</summary>
        static void EnsureIcon()
        {
            if (!File.Exists(IconPath))
            {
                const int size = 512;
                WritePng(IconPath, size, (u, v) =>
                {
                    var p = new Vector2(u - 0.5f, v - 0.5f);
                    var ring = Mathf.Abs(p.magnitude - 0.36f) < 0.03f;
                    // Arrow along the diagonal: shaft, head and fletching.
                    var along = (p.x + p.y) * 0.7071f;
                    var across = Mathf.Abs(p.x - p.y) * 0.7071f;
                    var shaft = across < 0.018f && along > -0.3f && along < 0.2f;
                    var head = along >= 0.2f && along < 0.32f && across < (0.32f - along) * 0.6f;
                    var fletch = along > -0.32f && along < -0.2f && across < 0.06f && across > 0.02f;
                    return ring || shaft || head || fletch ? UIFactoryGold : Lapis;
                });
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        static readonly Color UIFactoryGold = new Color(0.90f, 0.72f, 0.32f);

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
            var root = ProjectileRoot("Arrow", DamageRules.StandardArrowDamage, false);
            Box("Shaft", root.transform, new Vector2(-0.45f, 0f), new Vector2(0.9f, 0.05f), Wood, 40);
            var head = Box("Head", root.transform, new Vector2(-0.05f, 0f), new Vector2(0.11f, 0.11f), Steel, 41);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Box("Fletching", root.transform, new Vector2(-0.84f, 0f), new Vector2(0.18f, 0.12f), Feather, 41);
            return SavePrefab(root, ArrowPrefabPath);
        }

        static GameObject CreateSpearPrefab()
        {
            var root = ProjectileRoot("Spear", 150f, false);
            Box("Shaft", root.transform, new Vector2(-0.8f, 0f), new Vector2(1.6f, 0.07f), Wood, 40);
            var head = Box("Head", root.transform, new Vector2(-0.08f, 0f), new Vector2(0.18f, 0.18f), Steel, 41);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return SavePrefab(root, SpearPrefabPath);
        }

        static GameObject CreateFireArrowPrefab()
        {
            var root = ProjectileRoot("Fire Arrow", DamageRules.StandardArrowDamage, true);
            Box("Shaft", root.transform, new Vector2(-0.45f, 0f), new Vector2(0.9f, 0.05f), Wood, 40);
            Disc("Flame", root.transform, new Vector2(-0.12f, 0f), 0.3f, new Color(1f, 0.55f, 0.1f), 42);
            Box("Fletching", root.transform, new Vector2(-0.84f, 0f), new Vector2(0.18f, 0.12f), new Color(0.2f, 0.15f, 0.1f), 41);
            return SavePrefab(root, FireArrowPrefabPath);
        }

        static GameObject CreateStonePrefab(string path, string name, float diameter, Color color, float damage)
        {
            var root = ProjectileRoot(name, damage, false);
            Disc("Stone", root.transform, new Vector2(-diameter * 0.5f, 0f), diameter, color, 40);
            return SavePrefab(root, path);
        }

        static GameObject ProjectileRoot(string name, float damage, bool ignites)
        {
            var root = new GameObject(name);
            var arrow = root.AddComponent<Arrow>();
            var intercept = root.AddComponent<CircleCollider2D>();
            intercept.isTrigger = true;
            intercept.radius = 0.35f; // generous, so shooting arrows out of the air is fair
            WireFloat(arrow, "damage", damage);
            WireBool(arrow, "ignites", ignites);
            Wire(arrow, "interceptCollider", intercept);
            return root;
        }

        /// <summary>
        /// Builds an archer whose body parts are kinematic rigidbodies joined to the torso by hinges
        /// (they become a ragdoll on death). Parts are siblings under the root so physics never fights
        /// the hierarchy. A left-facing archer is built mirrored rather than with a negative scale.
        /// </summary>
        static GameObject CreateArcherPrefab(string name, string path, Role role, Color tunic, GameObject arrowPrefab)
        {
            var isPlayer = role == Role.Player;
            var armed = role != Role.Companion;
            var facingRight = role != Role.Enemy;
            var facing = facingRight ? 1f : -1f;
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
            GameObject bowObject = null;
            Transform launchPoint = null;
            if (armed)
            {
                bowObject = Box("Bow", pivot.transform, new Vector2(0.62f, 0f), new Vector2(0.1f, 1.1f), Wood, 34);
                launchPoint = new GameObject("LaunchPoint").transform;
                launchPoint.SetParent(pivot.transform, false);
                launchPoint.localPosition = new Vector2(0.72f, 0f);
            }

            var bar = new GameObject("HealthBar");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector2(0f, 2.75f);
            Box("Background", bar.transform, Vector2.zero, new Vector2(1.24f, 0.2f), BarBackground, 60);
            var fill = Box("Fill", bar.transform, Vector2.zero, new Vector2(1.16f, 0.12f), BarFill, 61);
            var healthBar = bar.AddComponent<HealthBar>();
            Wire(healthBar, "fill", fill.GetComponent<SpriteRenderer>());

            Bow bow = null;
            if (armed)
            {
                bow = bowObject.AddComponent<Bow>();
                Wire(bow, "arrowPrefab", arrowPrefab.GetComponent<Arrow>());
                Wire(bow, "launchPoint", launchPoint);
                Wire(bow, "owner", root.transform);
            }

            var rig = root.AddComponent<ArcherRig>();
            Wire(rig, "aimPivot", pivot.transform);
            WireBool(rig, "facingRight", facingRight);

            var combatant = root.AddComponent<Combatant>();
            WireEnum(combatant, "team", (int)(role == Role.Enemy ? Team.Enemy : Team.Player));
            WireBool(combatant, "facingRight", facingRight);
            Wire(combatant, "bodyTarget", torso.transform);
            Wire(combatant, "headTarget", head.transform);

            Behaviour controller = null;
            if (!armed)
            {
                // Companions do not fight.
            }
            else if (isPlayer)
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
            WireArray(ragdoll, "disableOnDeath", controller != null ? new Object[] { rig, controller } : new Object[] { rig });

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

        static void WireFloat(Object component, string field, float value)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Arash Setup] {component.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            property.floatValue = value;
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

        static void WireString(Object component, string field, string value)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Arash Setup] {component.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildBattleScene(GameObject arashPrefab, GameObject enemyPrefab, GameObject companionPrefab)
        {
            if (!File.Exists(BattleScenePath))
                return;

            var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            if (roots.Any(go => go.GetComponent<RealtimeBattle>() != null))
                return;

            // Replace anything an older builder generated; keep the camera and the global light.
            foreach (var go in roots)
                if (go.GetComponent<Camera>() == null && !go.name.Contains("Light"))
                    Object.DestroyImmediate(go);

            var ground = Box("Ground", null, new Vector2(90f, GroundY - 5f), new Vector2(240f, 10f), Earth, 10);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(240f, 10f);

            var arash = (GameObject)PrefabUtility.InstantiatePrefab(arashPrefab, scene);
            arash.transform.position = new Vector2(PlayerX, GroundY);

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
            Wire(turns, "battleCamera", battleCamera);

            var realtime = battle.AddComponent<RealtimeBattle>();
            Wire(realtime, "battleCamera", battleCamera);

            var hud = battle.AddComponent<BattleHud>();

            var feedback = battle.AddComponent<CombatFeedback>();
            Wire(feedback, "battleCamera", battleCamera);
            Wire(feedback, "hud", hud);

            var runner = battle.AddComponent<LevelRunner>();
            Wire(runner, "turnManager", turns);
            Wire(runner, "realtimeBattle", realtime);
            Wire(runner, "companionPrefab", companionPrefab.GetComponent<Combatant>());
            Wire(runner, "hud", hud);
            Wire(runner, "player", arash.GetComponent<Combatant>());
            Wire(runner, "playerAim", arash.GetComponent<AimController>());
            Wire(runner, "playerPreview", arash.GetComponentInChildren<TrajectoryPreview>());
            Wire(runner, "enemyPrefab", enemyPrefab.GetComponent<Combatant>());
            Wire(runner, "ground", ground.GetComponent<SpriteRenderer>());
            Wire(runner, "sceneCamera", camera);
            Wire(runner, "squareSprite", s_Square);
            Wire(runner, "circleSprite", s_Circle);
            Wire(runner, "spriteMaterial", s_Material);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Arash Setup] Battle scene built.");
        }

        static void BuildCutsceneScene()
        {
            if (!File.Exists(CutsceneScenePath))
                return;
            var scene = EditorSceneManager.OpenScene(CutsceneScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            if (roots.Any(go => go.GetComponent<CutscenePlayer>() != null))
                return;

            var player = new GameObject("Cutscene Player").AddComponent<CutscenePlayer>();
            Wire(player, "square", s_Square);
            Wire(player, "circle", s_Circle);
            Wire(player, "spriteMaterial", s_Material);
            Wire(player, "sceneCamera", roots.Select(go => go.GetComponent<Camera>()).FirstOrDefault(c => c != null));
            EditorSceneManager.SaveScene(scene);
        }

        static void BuildFlightScene()
        {
            if (!File.Exists(FlightScenePath))
                return;
            var scene = EditorSceneManager.OpenScene(FlightScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            if (roots.Any(go => go.GetComponent<ArrowFlightController>() != null))
                return;

            var flight = new GameObject("Final Flight");
            var hud = flight.AddComponent<BattleHud>();
            var controller = flight.AddComponent<ArrowFlightController>();
            Wire(controller, "sceneCamera", roots.Select(go => go.GetComponent<Camera>()).FirstOrDefault(c => c != null));
            Wire(controller, "hud", hud);
            Wire(controller, "square", s_Square);
            Wire(controller, "circle", s_Circle);
            Wire(controller, "spriteMaterial", s_Material);
            EditorSceneManager.SaveScene(scene);
        }

        static void BuildMenuScene<T>(string path, string name) where T : MonoBehaviour
        {
            if (!File.Exists(path))
                return;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            if (roots.Any(go => go.GetComponent<T>() != null))
                return;

            var camera = roots.Select(go => go.GetComponent<Camera>()).FirstOrDefault(c => c != null);
            if (camera != null)
                camera.backgroundColor = Lapis;

            new GameObject(name).AddComponent<T>();
            EditorSceneManager.SaveScene(scene);
        }

        static void BuildBootScene()
        {
            if (!File.Exists(BootScenePath))
                return;

            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            var bootstrap = scene.GetRootGameObjects().Select(go => go.GetComponent<Bootstrap>()).FirstOrDefault(b => b != null);
            if (bootstrap == null)
                bootstrap = new GameObject("Bootstrap").AddComponent<Bootstrap>();
            WireString(bootstrap, "firstScene", SceneFlow.MainMenuScene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
