using System;
using System.Collections.Generic;
using System.Linq;
using Arash.Combat;
using Arash.Core;
using Arash.Localization;
using Arash.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Arash.Levels
{
    /// <summary>
    /// Builds the Battle scene from the current <see cref="LevelDefinition"/> (F-11) and runs it:
    /// look, covers, wind (F-26), enemies with their types (F-27, F-28) or the real-time modes
    /// (waves, escort, trial — F-23 to F-25), intro and outro dialogue (F-19). When the battle ends
    /// it rates it (F-12), saves the result (F-14) and shows the end screen, playing the level's
    /// outro cutscene (F-20) on the way out the first time.
    /// </summary>
    public class LevelRunner : MonoBehaviour
    {
        [SerializeField] TurnManager turnManager;
        [SerializeField] RealtimeBattle realtimeBattle;
        [SerializeField] BattleHud hud;

        [Header("Player")]
        [SerializeField] Combatant player;
        [SerializeField] AimController playerAim;
        [SerializeField] TrajectoryPreview playerPreview;

        [Header("Prefabs")]
        [SerializeField] Combatant enemyPrefab;
        [SerializeField, Tooltip("Projectile used by the player's fire special arrow.")]
        Arrow fireArrowPrefab;
        [SerializeField, Tooltip("Unarmed companion: envoys to protect, villagers in trials.")]
        Combatant companionPrefab;

        [Header("World")]
        [SerializeField] SpriteRenderer ground;
        [SerializeField] Camera sceneCamera;
        [SerializeField] float groundY = -3f;
        [SerializeField] Sprite squareSprite;
        [SerializeField] Sprite circleSprite;
        [SerializeField] Material spriteMaterial;

        static readonly Color RockColor = new Color(0.42f, 0.38f, 0.34f);
        static readonly Color WoodColor = new Color(0.5f, 0.33f, 0.18f);
        static readonly Color ShieldColor = new Color(0.55f, 0.45f, 0.3f);

        LevelCatalog catalog;
        LevelDefinition level;
        IBattleMode battle;
        int playerShots;
        PlayerArsenal arsenal;
        float startTime;

        public LevelDefinition Level { get { return level; } }

        void Awake()
        {
            catalog = LevelCatalog.Load();
            level = SceneFlow.CurrentLevel != null ? SceneFlow.CurrentLevel : (catalog != null ? catalog.First() : null);
            if (level == null)
            {
                Debug.LogWarning("[LevelRunner] No level catalog found; using a default duel.");
                level = ScriptableObject.CreateInstance<LevelDefinition>();
                level.enemies.Add(new EnemySpawn());
            }

            if (sceneCamera != null)
                sceneCamera.backgroundColor = level.skyColor;
            if (ground != null)
                ground.color = level.groundColor;
            BattleEnvironment.Wind = level.wind.strength;
            foreach (var cover in level.covers)
                CreateCover(cover);
            ApplyLoadout();

            switch (level.mode)
            {
                case LevelMode.Waves:
                    SetUpWaves();
                    break;
                case LevelMode.Escort:
                    SetUpEscort();
                    break;
                case LevelMode.Trial:
                    SetUpTrial();
                    break;
                default:
                    SetUpDuel();
                    break;
            }
        }

        /// <summary>Equipment from the armory (F-33, F-34): bow, upgrades, outfit and special arrows.</summary>
        void ApplyLoadout()
        {
            var loadout = Armory.CurrentLoadout(SaveSystem.Data);
            player.Health.SetMax(100f * loadout.HealthMultiplier);

            var bow = player.GetComponentInChildren<Bow>();
            if (bow != null)
                bow.SetProjectile(null, 8f, loadout.MaxSpeed, 1f, 1f);

            // Hard difficulty has no aim guide at all (GDD 4.2); otherwise the bow and upgrades extend it.
            if (playerPreview != null)
            {
                var basePreview = GameSettings.PreviewDuration;
                playerPreview.Duration = basePreview > 0f ? basePreview + loadout.PreviewBonus : 0f;
            }

            var torso = player.BodyTarget.GetComponent<SpriteRenderer>();
            if (torso != null)
                torso.color = loadout.Tunic;

            arsenal = player.gameObject.AddComponent<PlayerArsenal>();
            arsenal.Configure(bow, playerAim, player.Health, fireArrowPrefab, LivingEnemies, loadout);
        }

        static IEnumerable<Combatant> LivingEnemies()
        {
            return FindObjectsByType<Combatant>(FindObjectsSortMode.None).Where(c => c.Team == Team.Enemy && c.IsAlive);
        }

        void SetUpDuel()
        {
            var enemies = new List<Combatant>();
            foreach (var spawn in level.enemies)
                enemies.Add(SpawnEnemy(spawn));
            if (enemies.Count == 0)
                enemies.Add(SpawnEnemy(new EnemySpawn()));

            turnManager.Configure(player, enemies, true);
            turnManager.TurnStarted += OnTurnStarted;
            battle = turnManager;
            if (realtimeBattle != null)
                realtimeBattle.enabled = false;
        }

        void SetUpWaves()
        {
            var palisadeX = player.transform.position.x + 3f;
            CreateCover(new CoverSpawn { x = palisadeX, width = 0.5f, height = 1.4f, flammable = false });
            realtimeBattle.ConfigureWaves(player, playerAim, level.waves, level.palisadeLives,
                wave => SpawnWalker(wave, palisadeX + 0.6f));
            UseRealtime(() => Loc.T("status.waves", realtimeBattle.Lives, realtimeBattle.Wave, realtimeBattle.WaveCount));
        }

        void SetUpEscort()
        {
            var archers = new List<Combatant>();
            foreach (var spawn in level.enemies)
                archers.Add(SpawnEnemy(spawn));
            // The companion stands behind Arash, so only enemy arrows threaten it.
            var companion = SpawnCompanion(player.transform.position.x - 2.2f, new Color(0.95f, 0.9f, 0.75f));
            realtimeBattle.ConfigureEscort(player, playerAim, companion, archers, level.enemyFireInterval, level.surviveSeconds);
            if (level.surviveSeconds > 0f)
                UseRealtime(() => Loc.T("status.escort", Mathf.CeilToInt(realtimeBattle.TimeLeft)));
            else
                UseRealtime(() => Loc.T("status.escort_all"));
        }

        void SetUpTrial()
        {
            var targets = new List<TrialTarget>();
            var bystanders = new List<Health>();
            foreach (var spawn in level.targets)
                targets.Add(CreateTarget(spawn, bystanders));
            realtimeBattle.ConfigureTrial(player, playerAim, targets, level.arrowLimit, bystanders);
            UseRealtime(() => Loc.T("status.trial", realtimeBattle.TargetsLeft, realtimeBattle.ArrowsLeft));
        }

        void UseRealtime(Func<string> status)
        {
            battle = realtimeBattle;
            turnManager.enabled = false;
            realtimeBattle.StatusChanged += () =>
            {
                if (hud != null)
                    hud.RefreshStatus();
            };
            if (hud != null)
                hud.SetStatus(status);
        }

        void OnEnable()
        {
            if (playerAim != null)
                playerAim.Shot += OnPlayerShot;
        }

        void OnDisable()
        {
            if (playerAim != null)
                playerAim.Shot -= OnPlayerShot;
            BattleEnvironment.Wind = 0f;
        }

        void Start()
        {
            battle.BattleEnded += OnBattleEnded;
            if (hud == null)
            {
                battle.Begin();
                return;
            }

            hud.SetLevel(level.titleKey, level.hintKey);
            hud.SetArsenal(arsenal);
            startTime = Time.time;
            Telemetry.Event("level_start", "level", level.id, "mode", level.mode.ToString());
            if (level.wind.strength != 0f || level.wind.variance > 0f)
                hud.SetWind(BattleEnvironment.Wind);
            hud.PlayDialogue(level.introDialogue, () =>
            {
                hud.ShowIntro(level.introKey);
                battle.Begin();
            });
        }

        void OnTurnStarted(Combatant actor)
        {
            if (level.wind.variance <= 0f)
                return;
            BattleEnvironment.Wind = BattleEnvironment.RollWind(level.wind.strength, level.wind.variance, Random.Range(-1f, 1f));
            if (hud != null)
                hud.SetWind(BattleEnvironment.Wind);
        }

        void OnPlayerShot(Arrow arrow)
        {
            playerShots++;
            if (hud != null)
                hud.HideHint();
        }

        void OnBattleEnded(bool won)
        {
            var arrowsUsed = battle is RealtimeBattle ? realtimeBattle.ArrowsUsed : playerShots;
            var stars = ProgressRules.Stars(won, battle.PlayerCondition, arrowsUsed, level.stars);
            var coins = won ? stars * level.coinsPerStar : 0;

            Telemetry.Event("level_end", "level", level.id, "won", won, "stars", stars,
                "arrows", arrowsUsed, "seconds", Mathf.RoundToInt(Time.time - startTime));

            var save = SaveSystem.Data;
            if (won && !string.IsNullOrEmpty(level.id))
            {
                save.RecordWin(level.id, stars);
                save.coins += coins;
                SaveSystem.Save();
            }

            Action next = null;
            var nextLevel = catalog != null ? catalog.Next(level) : null;
            if (won && nextLevel != null && catalog.IsUnlocked(nextLevel, save))
                next = () => SceneFlow.Play(nextLevel);

            // The outro cutscene plays on the way out, whichever way the player leaves.
            Action map = SceneFlow.ToWorldMap;
            if (won)
            {
                if (next != null)
                    next = SceneFlow.WithCutscene(level.outroCutscene, next);
                map = SceneFlow.WithCutscene(level.outroCutscene, map);
            }

            if (hud == null)
                return;
            hud.SetStatus(null);
            hud.SetWind(null);
            hud.SetArsenal(null);
            hud.PlayDialogue(won ? level.outroDialogue : null, () => hud.ShowResult(won, stars, coins, next, map));
        }

        // ------------------------------------------------------------------ spawning

        Combatant SpawnEnemy(EnemySpawn spawn)
        {
            if (spawn.height > 0.05f)
                CreateBlock("Rock Platform", new Vector2(spawn.x, groundY + spawn.height * 0.5f), new Vector2(2.4f, spawn.height), RockColor, 11);

            var enemy = Instantiate(enemyPrefab, new Vector3(spawn.x, groundY + spawn.height, 0f), Quaternion.identity);
            var ai = enemy.GetComponent<EnemyArcherAI>();
            var type = spawn.type;
            if (type == null)
            {
                enemy.Health.SetMax(spawn.maxHealth);
                if (ai != null && spawn.accuracy != null)
                    ai.SetAccuracy(spawn.accuracy.Scaled(GameSettings.EnemyErrorMultiplier));
                return enemy;
            }

            enemy.name = type.name;
            enemy.transform.localScale = Vector3.one * type.scale;
            enemy.Health.SetMax(type.maxHealth);
            Tint(enemy, type.tint);
            if (ai != null)
            {
                ai.SetAccuracy(type.accuracy.Scaled(GameSettings.EnemyErrorMultiplier));
                ai.SetTactics(type.tactics.Clone());
                ai.PhaseChanged += OnBossPhase;
            }
            var bow = enemy.GetComponentInChildren<Bow>();
            if (bow != null)
                bow.SetProjectile(type.projectile, type.minSpeed, type.maxSpeed, type.gravityScale, type.windScale);
            if (type.shield)
                AddShield(enemy);
            if (type.patrolRange > 0f)
            {
                var patrol = enemy.gameObject.AddComponent<Patrol>();
                patrol.range = type.patrolRange;
                patrol.speed = type.patrolSpeed;
                enemy.GetComponent<Ragdoll2D>().DisableOnDeath(patrol);
            }
            return enemy;
        }

        void OnBossPhase(EnemyArcherAI ai, BossPhase phase)
        {
            if (hud != null && !string.IsNullOrEmpty(phase.announceKey))
                hud.ShowPopup(Loc.T(phase.announceKey));
        }

        Walker SpawnWalker(WaveSpawn wave, float goalX)
        {
            // Raiders appear just beyond the right edge of the wide real-time view.
            var raider = Instantiate(enemyPrefab, new Vector3(player.transform.position.x + 26f, groundY, 0f), Quaternion.identity);
            raider.Health.SetMax(wave.health * (wave.mounted ? 1.5f : 1f));
            var ai = raider.GetComponent<EnemyArcherAI>();
            if (ai != null)
                ai.enabled = false; // raiders charge instead of shooting
            if (wave.mounted)
                Tint(raider, new Color(0.75f, 0.6f, 0.45f));

            var walker = raider.gameObject.AddComponent<Walker>();
            walker.speed = wave.speed * (wave.mounted ? 1.8f : 1f);
            walker.goalX = goalX;
            raider.GetComponent<Ragdoll2D>().DisableOnDeath(walker);
            return walker;
        }

        Combatant SpawnCompanion(float x, Color tint)
        {
            var companion = Instantiate(companionPrefab, new Vector3(x, groundY, 0f), Quaternion.identity);
            Tint(companion, tint);
            return companion;
        }

        TrialTarget CreateTarget(TrialTargetSpawn spawn, List<Health> bystanders)
        {
            var position = new Vector2(spawn.position.x, groundY + spawn.position.y);
            GameObject target;
            switch (spawn.kind)
            {
                case TrialTargetKind.Apple:
                    var villager = SpawnCompanion(spawn.position.x, new Color(0.85f, 0.75f, 0.55f));
                    bystanders.Add(villager.Health);
                    target = CreateDisc("Apple", villager.HeadTarget.position + new Vector3(0f, 0.42f, 0f), 0.32f, new Color(0.8f, 0.1f, 0.1f), 40);
                    target.transform.SetParent(villager.transform, true);
                    break;
                case TrialTargetKind.Lantern:
                    target = CreateDisc("Lantern", position, 0.6f, new Color(1f, 0.78f, 0.25f), 20);
                    break;
                default:
                    CreateBlock("Post", new Vector2(position.x, groundY + spawn.position.y * 0.5f), new Vector2(0.15f, Mathf.Max(0.1f, spawn.position.y)), WoodColor, 19);
                    target = CreateBlock("Board", position + new Vector2(0f, 0.5f), new Vector2(0.25f, 1f), new Color(0.9f, 0.8f, 0.55f), 20);
                    break;
            }

            var trialTarget = target.AddComponent<TrialTarget>();
            trialTarget.kind = spawn.kind;
            if (spawn.movement != Vector2.zero)
            {
                var oscillator = target.AddComponent<Oscillator>();
                oscillator.amplitude = spawn.movement;
                oscillator.period = spawn.period;
            }
            return trialTarget;
        }

        void CreateCover(CoverSpawn cover)
        {
            var block = CreateBlock(cover.flammable ? "Wooden Cover" : "Palisade", new Vector2(cover.x, groundY + cover.height * 0.5f),
                new Vector2(cover.width, cover.height), WoodColor, 12);
            if (cover.flammable)
                block.AddComponent<Flammable>();
        }

        void AddShield(Combatant enemy)
        {
            var torso = enemy.BodyTarget;
            var facing = enemy.FacingRight ? 1f : -1f;
            var shield = new GameObject("Shield");
            shield.transform.SetParent(torso, false);
            shield.transform.localPosition = new Vector3(0.42f * facing, 0f, 0f);
            var renderer = AddRenderer(shield, squareSprite, ShieldColor, 35);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.18f, 1.05f);
            shield.AddComponent<BoxCollider2D>().size = new Vector2(0.18f, 1.05f);
            shield.AddComponent<HitZone>().SetZone(HitZoneType.Armor);
        }

        static void Tint(Combatant combatant, Color tint)
        {
            if (tint == Color.white)
                return;
            var bar = combatant.GetComponentInChildren<HealthBar>();
            foreach (var renderer in combatant.GetComponentsInChildren<SpriteRenderer>(true))
                if (bar == null || !renderer.transform.IsChildOf(bar.transform))
                    renderer.color *= tint;
        }

        GameObject CreateBlock(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var block = new GameObject(name);
            block.transform.position = position;
            var renderer = AddRenderer(block, squareSprite, color, order);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            block.AddComponent<BoxCollider2D>().size = size;
            return block;
        }

        GameObject CreateDisc(string name, Vector2 position, float diameter, Color color, int order)
        {
            var disc = new GameObject(name);
            disc.transform.position = position;
            disc.transform.localScale = new Vector3(diameter, diameter, 1f);
            AddRenderer(disc, circleSprite, color, order);
            disc.AddComponent<CircleCollider2D>().radius = 0.5f;
            return disc;
        }

        SpriteRenderer AddRenderer(GameObject go, Sprite sprite, Color color, int order)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            if (spriteMaterial != null)
                renderer.sharedMaterial = spriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
