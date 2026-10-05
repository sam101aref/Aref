using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arash.Art;
using Arash.Combat;
using Arash.Editor.Setup;
using Arash.Levels;
using Arash.Story;
using UnityEditor;
using UnityEngine;

namespace Arash.Editor.Content
{
    /// <summary>
    /// The game's story content as data. Enemy types (F-27, F-28, F-63) are defined here; the
    /// chapters, levels, waves, dialogue and illustrated cutscenes (F-60 to F-62) come from
    /// StoryScript.json next to this file. Assets are rewritten from the script on every setup,
    /// so the script is the single source of truth (asset GUIDs stay the same). Text lives in
    /// Resources/Localization/Story.json, generated from the same script by tools/story.
    /// </summary>
    static class StoryContent
    {
        const string LevelFolder = ProjectSetup.ProjectRoot + "/Data/Levels";
        const string EnemyFolder = ProjectSetup.ProjectRoot + "/Data/Enemies";
        const string StoryFolder = ProjectSetup.ProjectRoot + "/Data/Story";
        const string CatalogPath = ProjectSetup.ProjectRoot + "/Resources/" + LevelCatalog.ResourcePath + ".asset";
        const string ScriptPath = ProjectSetup.ProjectRoot + "/Editor/Content/StoryScript.json";

        public class Projectiles
        {
            public Arrow Spear;
            public Arrow FireArrow;
            public Arrow Stone;
            public Arrow Boulder;
        }

#pragma warning disable 0649 // filled in by JsonUtility
        [Serializable] class Text { public string en; public string fa; }
        [Serializable] class ActorData { public string look; public float x; public float y; public string face; public string pose; public float scale = 1f; public float dx; }
        [Serializable] class PropData { public string sprite; public float x; public float y; public float scale = 1f; public bool flip; public bool front; public float dx; }
        [Serializable] class CameraData { public float x0; public float y0 = 2.6f; public float z0 = 4.4f; public float x1; public float y1 = 2.6f; public float z1 = 4f; }
        [Serializable] class PanelData { public string biome; public string time; public string speaker; public Text text; public List<ActorData> actors; public List<PropData> props; public CameraData camera; public float duration = 6f; }
        [Serializable] class CutsceneData { public Text title; public List<PanelData> panels; }
        [Serializable] class LineData { public string speaker; public string en; public string fa; }
        [Serializable] class SpawnData { public string type; public float x; public float h; }
        [Serializable] class WaveData { public List<SpawnData> enemies; public List<LineData> dialogue; }
        [Serializable] class CoverData { public float x; public float width = 0.6f; public float height = 1.6f; public bool flammable = true; }
        [Serializable] class WindData { public float strength; public float variance; }
        [Serializable] class TargetData { public string kind; public float x; public float y; public float mx; public float my; }
        [Serializable] class FlightData { public float length = 900f; public float speed = 14f; public float density = 0.55f; public int seed = 1; }
        [Serializable]
        class LevelData
        {
            public string id;
            public string mode;
            public string biome;
            public string time;
            public Text hint;
            public int coins = 15;
            public int gems;
            public int threeStars = 6;
            public float health2 = 0.5f;
            public WindData wind;
            public string companion;
            public List<CoverData> covers;
            public List<WaveData> waves;
            public List<TargetData> targets;
            public int arrows = 5;
            public FlightData flight;
            public List<LineData> introDialogue;
            public List<LineData> outroDialogue;
            public CutsceneData cutscene;
            public CutsceneData outro;
        }
        [Serializable] class ChapterData { public int number; public int starsToUnlock; public CutsceneData intro; public List<LevelData> levels; }
        [Serializable] class ScriptData { public List<ChapterData> chapters; public CutsceneData ending; }
#pragma warning restore 0649

        static Dictionary<string, EnemyDefinition> s_Enemies;

        /// <summary>Creates or updates the catalog from the story script. Returns null on failure.</summary>
        public static LevelCatalog EnsureCatalog(Projectiles projectiles)
        {
            foreach (var folder in new[] { LevelFolder, EnemyFolder, StoryFolder, Path.GetDirectoryName(CatalogPath) })
                Directory.CreateDirectory(folder);

            if (!File.Exists(ScriptPath))
            {
                Debug.LogError("[Arash Setup] Story script not found at " + ScriptPath);
                return null;
            }
            var script = JsonUtility.FromJson<ScriptData>(File.ReadAllText(ScriptPath));
            if (script == null || script.chapters == null || script.chapters.Count == 0)
            {
                Debug.LogError("[Arash Setup] Story script has no chapters.");
                return null;
            }

            s_Enemies = EnsureEnemies(projectiles);

            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.chapters = new List<LevelCatalog.Chapter>();
            foreach (var chapter in script.chapters.OrderBy(c => c.number))
            {
                catalog.chapters.Add(new LevelCatalog.Chapter
                {
                    titleKey = "chapter." + chapter.number + ".title",
                    starsToUnlock = chapter.starsToUnlock,
                    introCutscene = Cutscene(chapter.number == 0 ? "prologue" : "ch" + chapter.number, chapter.intro),
                    levels = (chapter.levels ?? new List<LevelData>()).Select(Level).ToList(),
                });
            }
            catalog.endingCutscene = Cutscene("ending", script.ending);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Arash Setup] Story: {catalog.chapters.Count} chapters, {catalog.AllLevels().Count} levels, {catalog.AllCutscenes().Count} cutscenes.");
            return catalog;
        }

        // ----------------------------------------------------------------- levels

        static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        static LevelDefinition Level(LevelData data)
        {
            var id = data.id;
            var level = Load<LevelDefinition>(LevelFolder + "/Level_" + id + ".asset");
            level.id = "ch" + id;
            level.titleKey = "level." + id + ".title";
            level.introKey = "level." + id + ".intro";
            level.hintKey = data.hint != null && !string.IsNullOrEmpty(data.hint.en) ? "level." + id + ".hint" : null;
            level.mode = Parse(data.mode, LevelMode.Battle);
            level.biome = Parse(data.biome, Biome.Village);
            level.time = Parse(data.time, TimeOfDay.Day);
            level.companion = Parse(data.companion, CharacterLook.Envoy);

            level.introDialogue = Lines("dlg." + id, data.introDialogue);
            level.outroDialogue = Lines("dlg." + id + "o", data.outroDialogue);
            level.introCutscene = Cutscene("lv" + id, data.cutscene);
            level.outroCutscene = Cutscene("lv" + id + "o", data.outro);

            level.waves = new List<EnemyWave>();
            var waves = data.waves ?? new List<WaveData>();
            for (var w = 0; w < waves.Count; w++)
            {
                level.waves.Add(new EnemyWave
                {
                    enemies = (waves[w].enemies ?? new List<SpawnData>()).Select(e => new EnemySpawn { type = Enemy(e.type), x = e.x, height = e.h }).ToList(),
                    dialogue = Lines("dlg." + id + "w" + (w + 1), waves[w].dialogue),
                });
            }
            level.covers = (data.covers ?? new List<CoverData>())
                .Select(c => new CoverSpawn { x = c.x, width = c.width, height = c.height, flammable = c.flammable }).ToList();
            level.wind = data.wind != null ? new WindSettings { strength = data.wind.strength, variance = data.wind.variance } : new WindSettings();
            level.targets = (data.targets ?? new List<TargetData>()).Select(t => new TrialTargetSpawn
            {
                kind = Parse(t.kind, TrialTargetKind.Board),
                position = new Vector2(t.x, t.y),
                movement = new Vector2(t.mx, t.my),
                period = 2.5f,
            }).ToList();
            level.arrowLimit = Mathf.Max(1, data.arrows);
            var flight = data.flight ?? new FlightData();
            level.flight = new FlightSettings { length = flight.length, speed = flight.speed, obstacleDensity = flight.density, seed = flight.seed };
            level.stars = new StarRules { healthForSecondStar = data.health2 > 0f ? data.health2 : 0.5f, maxArrowsForThirdStar = Mathf.Max(1, data.threeStars) };
            level.coinsPerStar = data.coins;
            level.firstWinGems = data.gems;
            EditorUtility.SetDirty(level);
            return level;
        }

        static List<DialogueLine> Lines(string prefix, List<LineData> lines)
        {
            var result = new List<DialogueLine>();
            if (lines == null)
                return result;
            for (var i = 0; i < lines.Count; i++)
                result.Add(new DialogueLine { speaker = Parse(lines[i].speaker, Speaker.Narrator), textKey = prefix + "." + (i + 1) });
            return result;
        }

        /// <summary>A cutscene asset from script data; null when the script has no panels for it.</summary>
        static CutsceneDefinition Cutscene(string id, CutsceneData data)
        {
            var path = StoryFolder + "/Cutscene_" + id + ".asset";
            if (data == null || data.panels == null || data.panels.Count == 0)
            {
                if (File.Exists(path))
                    AssetDatabase.DeleteAsset(path);
                return null;
            }

            var cutscene = Load<CutsceneDefinition>(path);
            cutscene.id = id;
            cutscene.titleKey = "cut." + id + ".title";
            cutscene.panels = new List<CutscenePanel>();
            for (var i = 0; i < data.panels.Count; i++)
            {
                var p = data.panels[i];
                var camera = p.camera ?? new CameraData();
                cutscene.panels.Add(new CutscenePanel
                {
                    biome = Parse(p.biome, Biome.Village),
                    time = Parse(p.time, TimeOfDay.Day),
                    speaker = Parse(p.speaker, Speaker.Narrator),
                    captionKey = "cut." + id + "." + (i + 1),
                    duration = p.duration > 1f ? p.duration : 6f,
                    cameraFrom = new Vector2(camera.x0, camera.y0),
                    cameraTo = new Vector2(camera.x1, camera.y1),
                    zoomFrom = camera.z0 > 0.5f ? camera.z0 : 4.4f,
                    zoomTo = camera.z1 > 0.5f ? camera.z1 : 4f,
                    actors = (p.actors ?? new List<ActorData>()).Select(a => new CutsceneActor
                    {
                        look = Parse(a.look, CharacterLook.Arash),
                        position = new Vector2(a.x, a.y),
                        facingRight = a.face != "left",
                        scale = a.scale > 0.1f ? a.scale : 1f,
                        pose = Parse(a.pose, ActorPose.Stand),
                        drift = new Vector2(a.dx, 0f),
                    }).ToList(),
                    props = (p.props ?? new List<PropData>()).Select(o => new CutsceneProp
                    {
                        sprite = o.sprite,
                        position = new Vector2(o.x, o.y),
                        scale = o.scale > 0.05f ? o.scale : 1f,
                        flip = o.flip,
                        front = o.front,
                        drift = new Vector2(o.dx, 0f),
                    }).ToList(),
                });
            }
            EditorUtility.SetDirty(cutscene);
            return cutscene;
        }

        static T Parse<T>(string value, T fallback) where T : struct
        {
            T result;
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value.Replace("_", string.Empty), true, out result))
                return result;
            if (!string.IsNullOrEmpty(value))
                Debug.LogWarning($"[Arash Setup] Unknown {typeof(T).Name} '{value}' in the story script; using {fallback}.");
            return fallback;
        }

        static EnemyDefinition Enemy(string type)
        {
            EnemyDefinition enemy;
            if (!string.IsNullOrEmpty(type) && s_Enemies.TryGetValue(type.ToLowerInvariant(), out enemy))
                return enemy;
            Debug.LogWarning("[Arash Setup] Unknown enemy type '" + type + "' in the story script; using an archer.");
            return s_Enemies["archer"];
        }

        // ----------------------------------------------------------------- enemy types

        static Dictionary<string, EnemyDefinition> EnsureEnemies(Projectiles p)
        {
            var enemies = new Dictionary<string, EnemyDefinition>();
            Action<string, string, Action<EnemyDefinition>> define = (key, file, configure) =>
            {
                var enemy = Load<EnemyDefinition>(EnemyFolder + "/" + file + ".asset");
                // Start from defaults so removed settings do not linger.
                var fresh = ScriptableObject.CreateInstance<EnemyDefinition>();
                EditorUtility.CopySerialized(fresh, enemy);
                UnityEngine.Object.DestroyImmediate(fresh);
                enemy.nameKey = "enemy." + key;
                configure(enemy);
                EditorUtility.SetDirty(enemy);
                enemies[key] = enemy;
            };
            Func<float, float, float, AiAccuracy> aim = (error, minimum, headshot) =>
                new AiAccuracy { initialAngleError = error, minAngleError = minimum, decayPerMiss = 0.75f, headshotChance = headshot };

            define("recruit", "Recruit", e =>
            {
                e.look = CharacterLook.Turanian;
                e.tint = new Color(1f, 0.92f, 0.85f);
                e.maxHealth = 40f;
                e.damage = 14f;
                e.cooldown = new Vector2(4.5f, 6.5f);
                e.accuracy = aim(12f, 4f, 0f);
            });
            define("archer", "TuranianArcher", e =>
            {
                e.look = CharacterLook.Turanian;
                e.accuracy = aim(8f, 1.8f, 0.2f);
            });
            define("shieldbearer", "ShieldBearer", e =>
            {
                e.look = CharacterLook.ShieldBearer;
                e.shield = true;
                e.cooldown = new Vector2(3.2f, 5f);
                e.accuracy = aim(8f, 2f, 0.15f);
            });
            define("spearman", "Spearman", e =>
            {
                e.look = CharacterLook.Spearman;
                e.projectile = p.Spear;
                e.damage = 35f;
                e.minSpeed = 8f;
                e.maxSpeed = 21f;
                e.gravityScale = 1.3f;
                e.windScale = 0.5f;
                e.cooldown = new Vector2(3.5f, 5.5f);
                e.accuracy = aim(9f, 2.2f, 0.1f);
            });
            define("rider", "Rider", e =>
            {
                e.look = CharacterLook.Rider;
                e.mounted = true;
                e.maxHealth = 80f;
                e.patrolRange = 2.5f;
                e.patrolSpeed = 2.5f;
                e.cooldown = new Vector2(3f, 4.8f);
                e.accuracy = aim(8.5f, 2.2f, 0.15f);
            });
            define("slinger", "Slinger", e =>
            {
                e.look = CharacterLook.Slinger;
                e.maxHealth = 70f;
                e.projectile = p.Stone;
                e.damage = 30f;
                e.minSpeed = 10f;
                e.maxSpeed = 24f;
                e.tactics = new EnemyTactics { highArc = true };
                e.cooldown = new Vector2(3f, 5f);
                e.accuracy = aim(7f, 1.8f, 0.3f);
            });
            define("firearcher", "FireArcher", e =>
            {
                e.look = CharacterLook.FireArcher;
                e.projectile = p.FireArrow;
                e.accuracy = aim(7f, 1.8f, 0.2f);
            });
            define("assassin", "Assassin", e =>
            {
                e.look = CharacterLook.Assassin;
                e.maxHealth = 60f;
                e.tactics = new EnemyTactics { repositionRange = 4f };
                e.cooldown = new Vector2(2.5f, 4f);
                e.accuracy = aim(6f, 1.4f, 0.45f);
            });
            define("raider", "Raider", e =>
            {
                e.look = CharacterLook.Raider;
                e.role = EnemyRole.Raider;
                e.maxHealth = 50f;
                e.runSpeed = 2.4f;
                e.meleeDamage = 12f;
            });
            define("shaman", "Shaman", e =>
            {
                e.look = CharacterLook.Shaman;
                e.role = EnemyRole.Shaman;
                e.maxHealth = 60f;
                e.castInterval = 6f;
                e.shieldStrength = 80f;
            });
            define("div", "Div", e =>
            {
                e.look = CharacterLook.Div;
                e.maxHealth = 300f;
                e.scale = 1.8f;
                e.projectile = p.Boulder;
                e.damage = 45f;
                e.minSpeed = 10f;
                e.maxSpeed = 22f;
                e.gravityScale = 1.2f;
                e.windScale = 0.2f;
                e.cooldown = new Vector2(4f, 6f);
                e.accuracy = aim(8f, 2.2f, 0f);
            });
            define("commander", "VanguardCommander", e =>
            {
                e.look = CharacterLook.Commander;
                e.boss = true;
                e.maxHealth = 220f;
                e.shield = true;
                e.cooldown = new Vector2(2.6f, 4f);
                e.accuracy = aim(6.5f, 1.5f, 0.25f);
                e.tactics = new EnemyTactics
                {
                    phases = { new BossPhase { healthBelow = 0.5f, errorMultiplier = 0.6f, shotsPerTurn = 2, announceKey = "boss.1.phase" } },
                };
            });
            define("barman", "Barman", e =>
            {
                e.look = CharacterLook.Barman;
                e.boss = true;
                e.maxHealth = 280f;
                e.scale = 1.15f;
                e.damage = 28f;
                e.cooldown = new Vector2(3f, 4.5f);
                e.accuracy = aim(6f, 1.5f, 0.3f);
                e.tactics = new EnemyTactics
                {
                    shotsPerTurn = 2,
                    volleySpread = 3f,
                    phases = { new BossPhase { healthBelow = 0.5f, errorMultiplier = 0.6f, shotsPerTurn = 3, announceKey = "boss.2.phase" } },
                };
            });
            define("garsivaz", "Garsivaz", e =>
            {
                e.look = CharacterLook.Garsivaz;
                e.boss = true;
                e.maxHealth = 240f;
                e.projectile = p.FireArrow;
                e.cooldown = new Vector2(2.6f, 4f);
                e.accuracy = aim(5.5f, 1.3f, 0.35f);
                e.tactics = new EnemyTactics
                {
                    repositionRange = 3f,
                    phases = { new BossPhase { healthBelow = 0.5f, errorMultiplier = 0.5f, shotsPerTurn = 2, repositionRange = 5f, announceKey = "boss.3.phase" } },
                };
            });
            define("whitediv", "WhiteDiv", e =>
            {
                e.look = CharacterLook.WhiteDiv;
                e.boss = true;
                e.maxHealth = 500f;
                e.scale = 2.2f;
                e.projectile = p.Boulder;
                e.damage = 50f;
                e.minSpeed = 10f;
                e.maxSpeed = 24f;
                e.gravityScale = 1.2f;
                e.windScale = 0.2f;
                e.cooldown = new Vector2(3.6f, 5.2f);
                e.accuracy = aim(7f, 1.8f, 0f);
                e.tactics = new EnemyTactics
                {
                    phases =
                    {
                        new BossPhase { healthBelow = 0.66f, errorMultiplier = 0.8f, shotsPerTurn = 2, announceKey = "boss.4.phase1" },
                        new BossPhase { healthBelow = 0.33f, errorMultiplier = 0.5f, shotsPerTurn = 3, announceKey = "boss.4.phase2" },
                    },
                };
            });
            return enemies;
        }
    }
}
