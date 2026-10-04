using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arash.Combat;
using Arash.Editor.Setup;
using Arash.Levels;
using Arash.Story;
using UnityEditor;
using UnityEngine;

namespace Arash.Editor.Content
{
    /// <summary>
    /// The game's story content as data: enemy types (F-27, F-28), cutscenes (F-20), and the chapters
    /// and levels (F-17, F-29, F-30) with their dialogue (F-19). Creates the assets once; existing
    /// assets are left alone so designers can tune them. Text lives in Strings.json under the keys
    /// built here (level.{id}.title, dlg.{id}.{n}, cut.{id}.{n}, …).
    /// </summary>
    static class StoryContent
    {
        const string LevelFolder = ProjectSetup.ProjectRoot + "/Data/Levels";
        const string EnemyFolder = ProjectSetup.ProjectRoot + "/Data/Enemies";
        const string StoryFolder = ProjectSetup.ProjectRoot + "/Data/Story";
        const string CatalogPath = ProjectSetup.ProjectRoot + "/Resources/" + LevelCatalog.ResourcePath + ".asset";

        public class Projectiles
        {
            public Arrow Spear;
            public Arrow FireArrow;
            public Arrow Stone;
            public Arrow Boulder;
        }

        static readonly Color VillageSky = new Color(0.98f, 0.82f, 0.55f);
        static readonly Color VillageDusk = new Color(0.95f, 0.66f, 0.45f);
        static readonly Color Earth = new Color(0.55f, 0.43f, 0.28f);
        static readonly Color BorderSky = new Color(0.97f, 0.75f, 0.5f);
        static readonly Color BorderGround = new Color(0.6f, 0.5f, 0.32f);
        static readonly Color ForestSky = new Color(0.62f, 0.75f, 0.68f);
        static readonly Color ForestNight = new Color(0.25f, 0.32f, 0.42f);
        static readonly Color ForestGround = new Color(0.28f, 0.36f, 0.22f);
        static readonly Color ValleySky = new Color(0.85f, 0.7f, 0.6f);
        static readonly Color ValleyGround = new Color(0.5f, 0.4f, 0.33f);
        static readonly Color MountainSky = new Color(0.72f, 0.8f, 0.9f);
        static readonly Color Snow = new Color(0.82f, 0.84f, 0.88f);
        static readonly Color Noon = new Color(0.45f, 0.7f, 0.95f);

        static Dictionary<string, EnemyDefinition> s_Enemies;

        /// <summary>Creates or completes the catalog. Returns null on failure.</summary>
        public static LevelCatalog EnsureCatalog(Projectiles projectiles)
        {
            foreach (var folder in new[] { LevelFolder, EnemyFolder, StoryFolder, Path.GetDirectoryName(CatalogPath) })
                Directory.CreateDirectory(folder);

            s_Enemies = EnsureEnemies(projectiles);
            var cutscenes = EnsureCutscenes();

            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var chapters = new Func<LevelCatalog.Chapter>[] { Prologue, Chapter1, Chapter2, Chapter3, Chapter4, Chapter5 };
            var introIds = new[] { "prologue", "ch1", "ch2", "ch3", "ch4", "ch5" };
            for (var i = catalog.chapters.Count; i < chapters.Length; i++)
                catalog.chapters.Add(chapters[i]());
            for (var i = 0; i < catalog.chapters.Count && i < introIds.Length; i++)
                if (catalog.chapters[i].introCutscene == null)
                    catalog.chapters[i].introCutscene = cutscenes[introIds[i]];
            if (catalog.endingCutscene == null)
                catalog.endingCutscene = cutscenes["ending"];

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        // ----------------------------------------------------------------- chapters

        static LevelCatalog.Chapter Prologue()
        {
            return Chapter(0, 0,
                Duel("0_1", VillageSky, Earth, 2, hint: true, enemies: new[] { Archer(14f, 0f, 40f, 10f, 0f) })
                    .With(l => l.introDialogue = Lines("0_1", Speaker.Roshana, Speaker.Arash, Speaker.Roshana)),
                Duel("0_2", VillageSky, Earth, 3, hint: true, enemies: new[] { Archer(16f, 0f, 100f, 9f, 0.1f) }),
                Duel("0_3", VillageSky, Earth, 3, hint: true, enemies: new[] { Archer(26f, 0f, 100f, 8f, 0.15f) }),
                Duel("0_4", VillageDusk, Earth, 3, enemies: new[] { Archer(18f, 3f, 100f, 7f, 0.2f) }),
                Duel("0_5", VillageDusk, Earth, 4, enemies: new[] { Archer(15f, 0f, 70f, 8f, 0.15f), Archer(24f, 2f, 70f, 8f, 0.15f) })
                    .With(l => l.outroDialogue = Lines("0_5o", Speaker.Mobad, Speaker.Arash)));
        }

        static LevelCatalog.Chapter Chapter1()
        {
            return Chapter(1, 6,
                Duel("1_1", BorderSky, BorderGround, 3, enemies: new[] { Archer(16f, 0f, 100f, 7f, 0.2f) })
                    .With(l => l.introDialogue = Lines("1_1", Speaker.Manuchehr, Speaker.Arash)),
                Duel("1_2", BorderSky, BorderGround, 4, enemies: new[] { Typed("ShieldBearer", 17f) }, hint: true),
                Duel("1_3", BorderSky, BorderGround, 4, enemies: new[] { Typed("Rider", 20f) }, hint: true),
                Waves("1_4", BorderSky, BorderGround, 3, Wave(3, 3f, 1.3f, 40f), Wave(4, 2.6f, 1.4f, 40f), Wave(5, 2.2f, 1.5f, 40f)),
                Duel("1_5", VillageDusk, BorderGround, 5, enemies: new[] { Archer(15f, 0f, 100f, 7f, 0.2f), Typed("ShieldBearer", 24f, 2f) }),
                Duel("1_6", BorderSky, BorderGround, 4, enemies: new[] { Archer(22f, 1f, 100f, 6f, 0.2f) }, hint: true)
                    .With(l => l.wind = new WindSettings { strength = 2f, variance = 1.5f }),
                Trial("1_7", BorderSky, BorderGround, 7,
                    Target(TrialTargetKind.Board, 10f, 0.8f), Target(TrialTargetKind.Board, 16f, 1.4f),
                    Target(TrialTargetKind.Lantern, 13f, 4f), Target(TrialTargetKind.Board, 22f, 2f),
                    Target(TrialTargetKind.Lantern, 19f, 5.5f)),
                Duel("1_8", VillageDusk, BorderGround, 6, coins: 20,
                        enemies: new[] { Typed("VanguardCommander", 20f), Archer(27f, 2f, 80f, 7f, 0.2f) })
                    .With(l =>
                    {
                        l.introDialogue = Lines("1_8", Speaker.Turanian, Speaker.Arash);
                        l.outroDialogue = Lines("1_8o", Speaker.Manuchehr);
                    }));
        }

        static LevelCatalog.Chapter Chapter2()
        {
            return Chapter(2, 24,
                Duel("2_1", ForestSky, ForestGround, 4, enemies: new[] { Archer(18f, 0f, 100f, 6f, 0.2f) },
                        covers: new[] { Cover(11f, 0.6f, 1.8f) })
                    .With(l => l.introDialogue = Lines("2_1", Speaker.Roshana, Speaker.Arash)),
                Escort("2_2", ForestSky, ForestGround, 30f, 4.5f, Archer(18f, 0f, 80f, 8f, 0.1f), Archer(25f, 2f, 80f, 8f, 0.1f)),
                Duel("2_3", ForestSky, ForestGround, 4, enemies: new[] { Typed("Slinger", 22f, 3f) }, hint: true),
                Waves("2_4", ForestNight, ForestGround, 3, Wave(4, 2.5f, 1.5f, 50f), Wave(3, 2.5f, 1.4f, 50f, true), Wave(6, 1.8f, 1.6f, 50f)),
                Duel("2_5", ForestNight, ForestGround, 5, enemies: new[] { Typed("Spearman", 16f), Archer(24f, 0f, 100f, 6f, 0.2f) }, hint: true),
                Duel("2_6", ForestSky, ForestGround, 4, enemies: new[] { Typed("Assassin", 19f) }, covers: new[] { Cover(14f, 0.6f, 1.6f) }, hint: true),
                Trial("2_7", ForestSky, ForestGround, 6,
                    Target(TrialTargetKind.Board, 12f, 1f, new Vector2(0f, 1.2f)), Target(TrialTargetKind.Apple, 17f, 0f),
                    Target(TrialTargetKind.Board, 22f, 2f, new Vector2(0f, 1.6f)), Target(TrialTargetKind.Lantern, 15f, 5f, new Vector2(1.5f, 0f))),
                Duel("2_8", ForestNight, ForestGround, 6, coins: 20, enemies: new[] { Typed("Barman", 21f) })
                    .With(l =>
                    {
                        l.introDialogue = Lines("2_8", Speaker.Barman, Speaker.Arash);
                        l.outroDialogue = Lines("2_8o", Speaker.Roshana);
                    }));
        }

        static LevelCatalog.Chapter Chapter3()
        {
            return Chapter(3, 44,
                Escort("3_1", ValleySky, ValleyGround, 35f, 4f, Archer(17f, 0f, 90f, 7f, 0.15f), Archer(24f, 0f, 90f, 7f, 0.15f))
                    .With(l => l.introDialogue = Lines("3_1", Speaker.Manuchehr, Speaker.Afrasiab, Speaker.Arash)),
                Duel("3_2", ValleySky, ValleyGround, 4, enemies: new[] { Typed("FireArcher", 18f) }, covers: new[] { Cover(-3.5f, 0.6f, 1.7f) }, hint: true),
                Duel("3_3", ValleySky, ValleyGround, 5, enemies: new[] { Typed("Slinger", 24f, 3.5f), Archer(16f, 0f, 100f, 6f, 0.2f) })
                    .With(l => l.wind = new WindSettings { strength = -1.5f, variance = 1.5f }),
                Waves("3_4", VillageDusk, ValleyGround, 3, Wave(4, 2.2f, 1.6f, 60f), Wave(3, 2.5f, 1.5f, 60f, true), Wave(4, 2f, 1.6f, 60f, true), Wave(6, 1.6f, 1.7f, 60f)),
                Duel("3_5", ValleySky, ValleyGround, 5, enemies: new[] { Typed("Assassin", 17f), Typed("FireArcher", 25f, 2f) },
                    covers: new[] { Cover(12f, 0.6f, 1.6f) }),
                Duel("3_6", ValleySky, ValleyGround, 6, enemies: new[] { Typed("ShieldBearer", 15f), Typed("ShieldBearer", 21f), Typed("Spearman", 27f, 2f) }),
                Escort("3_7", VillageDusk, ValleyGround, 0f, 4f, Archer(16f, 0f, 70f, 7f, 0.2f), Archer(22f, 2f, 70f, 7f, 0.2f), Archer(28f, 0f, 70f, 7f, 0.2f)),
                Duel("3_8", VillageDusk, ValleyGround, 6, coins: 20, enemies: new[] { Typed("Garsivaz", 20f) }, covers: new[] { Cover(10f, 0.6f, 1.7f) })
                    .With(l =>
                    {
                        l.introDialogue = Lines("3_8", Speaker.Garsivaz, Speaker.Arash);
                        l.outroDialogue = Lines("3_8o", Speaker.Mobad);
                    }));
        }

        static LevelCatalog.Chapter Chapter4()
        {
            var wind = new WindSettings { strength = 1f, variance = 2f };
            return Chapter(4, 64,
                Duel("4_1", MountainSky, Snow, 5, enemies: new[] { Typed("Rider", 18f), Archer(26f, 2.5f, 100f, 6f, 0.2f) })
                    .With(l =>
                    {
                        l.wind = wind;
                        l.introDialogue = Lines("4_1", Speaker.Mobad, Speaker.Arash);
                    }),
                Duel("4_2", MountainSky, Snow, 5, enemies: new[] { Typed("Assassin", 16f), Typed("Assassin", 23f, 2f) }),
                Duel("4_3", MountainSky, Snow, 4, enemies: new[] { Typed("Div", 22f, 2f) }, hint: true),
                Duel("4_4", MountainSky, Snow, 5, enemies: new[] { Archer(18f, 0f, 100f, 5f, 0.25f), Typed("Slinger", 25f, 3f) })
                    .With(l => l.wind = new WindSettings { strength = 3f, variance = 2f }),
                Waves("4_5", MountainSky, Snow, 3, Wave(4, 2f, 1.7f, 60f, true), Wave(5, 1.8f, 1.7f, 70f), Wave(5, 1.6f, 1.8f, 70f, true), Wave(6, 1.5f, 1.9f, 70f)),
                Duel("4_6", MountainSky, Snow, 6, enemies: new[] { Typed("Div", 19f), Typed("Div", 27f, 2.5f) }),
                Trial("4_7", MountainSky, Snow, 6,
                    Target(TrialTargetKind.Lantern, 12f, 4f, new Vector2(0f, 1.5f)), Target(TrialTargetKind.Lantern, 17f, 5.5f, new Vector2(2f, 0f)),
                    Target(TrialTargetKind.Lantern, 22f, 3f, new Vector2(0f, 2f)), Target(TrialTargetKind.Lantern, 27f, 6f, new Vector2(1.5f, 1f)))
                    .With(l => l.wind = new WindSettings { strength = 1.5f }),
                Duel("4_8", MountainSky, Snow, 6, coins: 25, enemies: new[] { Typed("WhiteDiv", 21f) })
                    .With(l =>
                    {
                        l.wind = new WindSettings { strength = 1f, variance = 1f };
                        l.introDialogue = Lines("4_8", Speaker.WhiteDiv, Speaker.Arash);
                        l.outroDialogue = Lines("4_8o", Speaker.Mobad, Speaker.Arash);
                    }));
        }

        static LevelCatalog.Chapter Chapter5()
        {
            var flight = Make("5_1", LevelMode.Flight, VillageDusk, Earth, 1, 30, false);
            flight.flight = new FlightSettings { length = 900f, speed = 14f, obstacleDensity = 0.55f, seed = 1359 };
            flight.stars = new StarRules { healthForSecondStar = 0.5f, maxArrowsForThirdStar = 1 }; // "arrows" = hits taken
            flight.introDialogue = Lines("5_1", Speaker.Arash, Speaker.Mobad, Speaker.Arash);
            flight.outroDialogue = Lines("5_1o", Speaker.Narrator);
            EditorUtility.SetDirty(flight);
            return Chapter(5, 84, flight);
        }

        // ----------------------------------------------------------------- level helpers

        static LevelCatalog.Chapter Chapter(int number, int starsToUnlock, params LevelDefinition[] levels)
        {
            AssetDatabase.SaveAssets();
            return new LevelCatalog.Chapter
            {
                titleKey = "chapter." + number + ".title",
                starsToUnlock = starsToUnlock,
                levels = levels.ToList(),
            };
        }

        static LevelDefinition Make(string id, LevelMode mode, Color sky, Color ground, int threeStarArrows, int coins, bool hint)
        {
            var path = LevelFolder + "/Level_" + id + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (existing != null)
                return existing;

            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.id = "ch" + id;
            level.mode = mode;
            level.titleKey = "level." + id + ".title";
            level.introKey = "level." + id + ".intro";
            level.hintKey = hint ? "level." + id + ".hint" : null;
            level.skyColor = sky;
            level.groundColor = ground;
            level.stars = new StarRules { healthForSecondStar = 0.5f, maxArrowsForThirdStar = threeStarArrows };
            level.coinsPerStar = coins;
            AssetDatabase.CreateAsset(level, path);
            return level;
        }

        static LevelDefinition With(this LevelDefinition level, Action<LevelDefinition> configure)
        {
            configure(level);
            EditorUtility.SetDirty(level);
            return level;
        }

        static LevelDefinition Duel(string id, Color sky, Color ground, int threeStarArrows, EnemySpawn[] enemies,
            CoverSpawn[] covers = null, bool hint = false, int coins = 10)
        {
            return Make(id, LevelMode.Duel, sky, ground, threeStarArrows, coins, hint).With(l =>
            {
                l.enemies = enemies.ToList();
                l.covers = covers != null ? covers.ToList() : new List<CoverSpawn>();
            });
        }

        static LevelDefinition Waves(string id, Color sky, Color ground, int lives, params WaveSpawn[] waves)
        {
            var raiders = waves.Sum(w => w.count);
            return Make(id, LevelMode.Waves, sky, ground, raiders + 2, 10, true).With(l =>
            {
                l.waves = waves.ToList();
                l.palisadeLives = lives;
            });
        }

        static LevelDefinition Escort(string id, Color sky, Color ground, float surviveSeconds, float fireInterval, params EnemySpawn[] archers)
        {
            return Make(id, LevelMode.Escort, sky, ground, 8, 10, true).With(l =>
            {
                l.enemies = archers.ToList();
                l.surviveSeconds = surviveSeconds;
                l.enemyFireInterval = fireInterval;
            });
        }

        static LevelDefinition Trial(string id, Color sky, Color ground, int arrowLimit, params TrialTargetSpawn[] targets)
        {
            return Make(id, LevelMode.Trial, sky, ground, targets.Length, 10, true).With(l =>
            {
                l.targets = targets.ToList();
                l.arrowLimit = arrowLimit;
            });
        }

        static List<DialogueLine> Lines(string id, params Speaker[] speakers)
        {
            return speakers.Select((speaker, i) => new DialogueLine { speaker = speaker, textKey = "dlg." + id + "." + (i + 1) }).ToList();
        }

        static EnemySpawn Archer(float x, float height, float health, float initialError, float headshotChance)
        {
            return new EnemySpawn
            {
                x = x,
                height = height,
                maxHealth = health,
                accuracy = new AiAccuracy { initialAngleError = initialError, headshotChance = headshotChance },
            };
        }

        static EnemySpawn Typed(string type, float x, float height = 0f)
        {
            return new EnemySpawn { type = s_Enemies[type], x = x, height = height };
        }

        static CoverSpawn Cover(float x, float width, float height, bool flammable = true)
        {
            return new CoverSpawn { x = x, width = width, height = height, flammable = flammable };
        }

        static WaveSpawn Wave(int count, float interval, float speed, float health, bool mounted = false)
        {
            return new WaveSpawn { count = count, interval = interval, speed = speed, health = health, mounted = mounted, startDelay = 2f };
        }

        static TrialTargetSpawn Target(TrialTargetKind kind, float x, float y, Vector2 movement = default(Vector2))
        {
            return new TrialTargetSpawn { kind = kind, position = new Vector2(x, y), movement = movement, period = 2.5f };
        }

        // ----------------------------------------------------------------- enemy types

        static Dictionary<string, EnemyDefinition> EnsureEnemies(Projectiles p)
        {
            var enemies = new Dictionary<string, EnemyDefinition>();
            Action<string, Action<EnemyDefinition>> define = (name, configure) =>
            {
                var path = EnemyFolder + "/" + name + ".asset";
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (enemy == null)
                {
                    enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
                    configure(enemy);
                    AssetDatabase.CreateAsset(enemy, path);
                }
                enemies[name] = enemy;
            };

            define("ShieldBearer", e =>
            {
                e.shield = true;
                e.tint = new Color(0.8f, 0.8f, 0.85f);
                e.accuracy = new AiAccuracy { initialAngleError = 7f, headshotChance = 0.15f };
            });
            define("Spearman", e =>
            {
                e.projectile = p.Spear;
                e.minSpeed = 8f;
                e.maxSpeed = 21f;
                e.gravityScale = 1.3f;
                e.windScale = 0.5f;
                e.tint = new Color(0.85f, 0.75f, 0.6f);
                e.accuracy = new AiAccuracy { initialAngleError = 9f, headshotChance = 0.1f };
            });
            define("Rider", e =>
            {
                e.maxHealth = 80f;
                e.patrolRange = 3f;
                e.patrolSpeed = 3f;
                e.tint = new Color(0.75f, 0.6f, 0.45f);
                e.accuracy = new AiAccuracy { initialAngleError = 8f, headshotChance = 0.15f };
            });
            define("Slinger", e =>
            {
                e.maxHealth = 70f;
                e.projectile = p.Stone;
                e.minSpeed = 10f;
                e.maxSpeed = 24f;
                e.tactics = new EnemyTactics { highArc = true };
                e.tint = new Color(0.7f, 0.75f, 0.6f);
                e.accuracy = new AiAccuracy { initialAngleError = 6f, headshotChance = 0.3f };
            });
            define("FireArcher", e =>
            {
                e.projectile = p.FireArrow;
                e.tint = new Color(1f, 0.6f, 0.35f);
                e.accuracy = new AiAccuracy { initialAngleError = 6f, headshotChance = 0.2f };
            });
            define("Assassin", e =>
            {
                e.maxHealth = 60f;
                e.tactics = new EnemyTactics { repositionRange = 4f };
                e.tint = new Color(0.35f, 0.35f, 0.4f);
                e.accuracy = new AiAccuracy { initialAngleError = 5f, headshotChance = 0.4f };
            });
            define("Div", e =>
            {
                e.maxHealth = 300f;
                e.scale = 1.8f;
                e.projectile = p.Boulder;
                e.minSpeed = 10f;
                e.maxSpeed = 22f;
                e.gravityScale = 1.2f;
                e.windScale = 0.2f;
                e.tint = new Color(0.55f, 0.65f, 0.6f);
                e.accuracy = new AiAccuracy { initialAngleError = 8f, headshotChance = 0f };
            });
            define("VanguardCommander", e =>
            {
                e.maxHealth = 200f;
                e.shield = true;
                e.tint = new Color(0.9f, 0.75f, 0.45f);
                e.accuracy = new AiAccuracy { initialAngleError = 6f, headshotChance = 0.25f };
                e.tactics = new EnemyTactics
                {
                    phases = { new BossPhase { healthBelow = 0.5f, errorMultiplier = 0.6f, shotsPerTurn = 2, announceKey = "boss.1.phase" } },
                };
            });
            define("Barman", e =>
            {
                e.maxHealth = 250f;
                e.scale = 1.15f;
                e.tint = new Color(0.65f, 0.5f, 0.5f);
                e.accuracy = new AiAccuracy { initialAngleError = 6f, headshotChance = 0.3f };
                e.tactics = new EnemyTactics
                {
                    shotsPerTurn = 2,
                    volleySpread = 3f,
                    phases = { new BossPhase { healthBelow = 0.5f, errorMultiplier = 0.6f, shotsPerTurn = 3, announceKey = "boss.2.phase" } },
                };
            });
            define("Garsivaz", e =>
            {
                e.maxHealth = 220f;
                e.projectile = p.FireArrow;
                e.tint = new Color(0.5f, 0.35f, 0.55f);
                e.accuracy = new AiAccuracy { initialAngleError = 5f, headshotChance = 0.35f };
                e.tactics = new EnemyTactics
                {
                    repositionRange = 3f,
                    phases = { new BossPhase { healthBelow = 0.5f, errorMultiplier = 0.5f, shotsPerTurn = 2, repositionRange = 5f, announceKey = "boss.3.phase" } },
                };
            });
            define("WhiteDiv", e =>
            {
                e.maxHealth = 450f;
                e.scale = 2.2f;
                e.projectile = p.Boulder;
                e.minSpeed = 10f;
                e.maxSpeed = 24f;
                e.gravityScale = 1.2f;
                e.windScale = 0.2f;
                e.tint = new Color(0.95f, 0.97f, 1f);
                e.accuracy = new AiAccuracy { initialAngleError = 7f, headshotChance = 0f };
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

        // ----------------------------------------------------------------- cutscenes

        static Dictionary<string, CutsceneDefinition> EnsureCutscenes()
        {
            var cutscenes = new Dictionary<string, CutsceneDefinition>();
            Action<string, CutscenePanel[]> define = (id, panels) =>
            {
                var path = StoryFolder + "/Cutscene_" + id + ".asset";
                var cutscene = AssetDatabase.LoadAssetAtPath<CutsceneDefinition>(path);
                if (cutscene == null)
                {
                    cutscene = ScriptableObject.CreateInstance<CutsceneDefinition>();
                    cutscene.id = id;
                    cutscene.titleKey = "cut." + id + ".title";
                    for (var i = 0; i < panels.Length; i++)
                        panels[i].captionKey = "cut." + id + "." + (i + 1);
                    cutscene.panels = panels.ToList();
                    AssetDatabase.CreateAsset(cutscene, path);
                }
                cutscenes[id] = cutscene;
            };

            define("prologue", new[]
            {
                Panel(CutsceneMotif.Army, VillageDusk, Earth),
                Panel(CutsceneMotif.Camp, ForestNight, ForestGround),
                Panel(CutsceneMotif.Village, VillageSky, Earth),
                Panel(CutsceneMotif.Village, VillageDusk, Earth),
            });
            define("ch1", new[]
            {
                Panel(CutsceneMotif.Army, BorderSky, BorderGround),
                Panel(CutsceneMotif.Village, VillageDusk, BorderGround),
                Panel(CutsceneMotif.Mountains, BorderSky, BorderGround),
            });
            define("ch2", new[]
            {
                Panel(CutsceneMotif.Forest, ForestSky, ForestGround),
                Panel(CutsceneMotif.Forest, ForestNight, ForestGround),
                Panel(CutsceneMotif.Camp, ForestNight, ForestGround),
            });
            define("ch3", new[]
            {
                Panel(CutsceneMotif.Camp, ValleySky, ValleyGround),
                Panel(CutsceneMotif.Army, ValleySky, ValleyGround),
                Panel(CutsceneMotif.Camp, VillageDusk, ValleyGround),
            });
            define("ch4", new[]
            {
                Panel(CutsceneMotif.Camp, MountainSky, Snow),
                Panel(CutsceneMotif.Damavand, MountainSky, Snow),
                Panel(CutsceneMotif.Mountains, ForestNight, Snow),
            });
            define("ch5", new[]
            {
                Panel(CutsceneMotif.Damavand, VillageDusk, Snow),
                Panel(CutsceneMotif.Damavand, new Color(0.98f, 0.6f, 0.45f), Snow),
                Panel(CutsceneMotif.ArrowFlight, new Color(0.98f, 0.7f, 0.5f), Earth),
            });
            define("ending", new[]
            {
                Panel(CutsceneMotif.River, Noon, new Color(0.45f, 0.55f, 0.3f)),
                Panel(CutsceneMotif.Damavand, VillageDusk, Snow),
                Panel(CutsceneMotif.Army, VillageSky, Earth),
                Panel(CutsceneMotif.Celebration, ForestNight, Earth),
            });
            return cutscenes;
        }

        static CutscenePanel Panel(CutsceneMotif motif, Color sky, Color land)
        {
            return new CutscenePanel { motif = motif, sky = sky, land = land, duration = 6.5f };
        }
    }
}
