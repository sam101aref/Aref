using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arash.Art;
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
    /// the biome backdrop (F-50), covers, wind (F-26), Arash's equipment (F-55 to F-58), and the
    /// waves of enemies that walk, ride or jump in and fight in real time (F-51, F-63). Dialogue
    /// plays before the battle, before waves (F-61) and after it. When the battle ends it rates it
    /// (F-12), pays coins and gems (F-59), saves (F-14) and shows the end screen, playing the
    /// level's outro cutscene on the way out the first time.
    /// </summary>
    public class LevelRunner : MonoBehaviour
    {
        const float HorseHeight = 1.05f;
        const float EntryDistance = 9f;
        const float WindChangeSeconds = 9f;

        [SerializeField] RealtimeBattle realtimeBattle;
        [SerializeField] BattleCamera battleCamera;
        [SerializeField] BattleHud hud;

        [Header("Player")]
        [SerializeField] Combatant player;
        [SerializeField] AimController playerAim;
        [SerializeField] TrajectoryPreview playerPreview;

        [Header("Prefabs")]
        [SerializeField] Combatant enemyPrefab;
        [SerializeField] Arrow arrowPrefab;
        [SerializeField, Tooltip("Projectile of the fire bow.")]
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

        LevelCatalog catalog;
        LevelDefinition level;
        IBattleMode battle;
        PlayerArsenal arsenal;
        Barrier playerShield;
        Combatant companion;
        float startTime;

        public LevelDefinition Level { get { return level; } }

        void Awake()
        {
            ArtLibrary.SpriteMaterial = spriteMaterial;
            catalog = LevelCatalog.Load();
            level = SceneFlow.CurrentLevel != null ? SceneFlow.CurrentLevel : (catalog != null ? catalog.First() : null);
            if (level == null)
            {
                Debug.LogWarning("[LevelRunner] No level catalog found; using a default battle.");
                level = ScriptableObject.CreateInstance<LevelDefinition>();
                level.waves.Add(new EnemyWave { enemies = { new EnemySpawn() } });
            }

            Backdrop.Build(level.biome, level.time, sceneCamera, groundY, 8f, 110f);
            if (ground != null)
                ground.enabled = false; // the backdrop draws the ground; the collider stays

            BattleEnvironment.Wind = level.wind.strength;
            foreach (var cover in level.covers)
                CreateCover(cover);
            foreach (var wave in level.waves)
                foreach (var spawn in wave.enemies)
                    if (spawn.height > 0.05f)
                        CreatePlatform(spawn.x, spawn.height);

            ApplyLoadout();

            if (level.mode == LevelMode.Trial)
                SetUpTrial();
            else
                SetUpBattle();
        }

        // ------------------------------------------------------------------ player

        /// <summary>Equipment from the shop (F-55 to F-58): bows, armour, helmet, shield and outfit.</summary>
        void ApplyLoadout()
        {
            var loadout = Armory.CurrentLoadout(SaveSystem.Data);
            player.Health.SetMax(100f * loadout.HealthMultiplier);
            player.Health.SetResistance(loadout.ResistHead, loadout.ResistBody, loadout.ResistLimb);

            var skin = player.GetComponent<CharacterSkin>();
            if (skin != null)
            {
                skin.Apply(CharacterLook.Arash);
                var outfit = loadout.Outfit;
                if (outfit != null)
                    skin.SetColors(outfit.Tunic, outfit.Cape);
                if (loadout.Helmet != null)
                    skin.SetHat(ArtLibrary.Get(loadout.Helmet.Sprite), Color.white);
                else if (outfit != null)
                    skin.SetHat(null, outfit.Cap);
                if (loadout.Armor != null)
                    skin.SetArmor(ArtLibrary.Get(loadout.Armor.Sprite));
            }

            if (loadout.Shield != null)
                playerShield = CreatePavise(loadout.Shield);

            var bow = player.GetComponentInChildren<Bow>();
            arsenal = player.gameObject.AddComponent<PlayerArsenal>();
            arsenal.Configure(bow, playerAim, playerPreview, player.Health, skin, arrowPrefab, fireArrowPrefab,
                () => realtimeBattle.LivingEnemies, loadout, GameSettings.PreviewDuration);
        }

        Barrier CreatePavise(ShopItem item)
        {
            var go = new GameObject("Shield");
            go.transform.SetParent(player.transform, false);
            go.transform.localPosition = new Vector3(1.3f, 0f, 0f);
            ArtLibrary.Renderer(go.transform, "Art", ArtLibrary.Prop(item.Sprite), 38);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.5f, 1.3f);
            box.offset = new Vector2(0f, 0.62f);
            var barrier = go.AddComponent<Barrier>();
            barrier.Configure(item.Durability, true);
            return barrier;
        }

        // ------------------------------------------------------------------ modes

        void SetUpBattle()
        {
            if (level.mode == LevelMode.Escort)
            {
                companion = SpawnCompanion(player.transform.position.x - 2.2f, level.companion);
                companion.Health.SetMax(80f);
            }
            realtimeBattle.ConfigureBattle(player, playerAim, arsenal, level.waves.Count, SpawnWave, BeforeWave, companion);
            realtimeBattle.Resupplied += added =>
            {
                if (hud != null)
                    hud.ShowPopup(Loc.T("battle.resupplied", added));
            };
            UseRealtime(() => Loc.T("status.wave", realtimeBattle.Wave, realtimeBattle.WaveCount));
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
            realtimeBattle.StatusChanged += () =>
            {
                if (hud != null)
                    hud.RefreshStatus();
            };
            if (hud != null)
                hud.SetStatus(status);
        }

        void BeforeWave(int index, Action proceed)
        {
            var lines = index < level.waves.Count ? level.waves[index].dialogue : null;
            if (index > 0 && hud != null)
                hud.ShowPopup(Loc.T("battle.wave", index + 1));
            if (hud == null || lines == null || lines.Count == 0)
            {
                proceed();
                return;
            }
            hud.PlayDialogue(lines, proceed);
        }

        List<Combatant> SpawnWave(int index)
        {
            var spawned = new List<Combatant>();
            if (index >= level.waves.Count)
                return spawned;
            var order = 0;
            foreach (var spawn in level.waves[index].enemies)
                spawned.Add(SpawnEnemy(spawn, order++));
            return spawned;
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
            if (level.wind.variance > 0f)
                StartCoroutine(ChangeWind());
            if (hud == null)
            {
                battle.Begin();
                return;
            }

            hud.SetLevel(level.titleKey, level.hintKey);
            hud.SetArsenal(arsenal, () => arsenal.CastRain(groundY));
            if (playerShield != null)
                hud.SetShield(playerShield);
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

        IEnumerator ChangeWind()
        {
            while (true)
            {
                yield return new WaitForSeconds(WindChangeSeconds);
                BattleEnvironment.Wind = BattleEnvironment.RollWind(level.wind.strength, level.wind.variance, Random.Range(-1f, 1f));
                if (hud != null && realtimeBattle.IsRunning)
                    hud.SetWind(BattleEnvironment.Wind);
            }
        }

        void OnPlayerShot(Arrow arrow)
        {
            if (hud != null)
                hud.HideHint();
        }

        void OnBattleEnded(bool won)
        {
            var arrowsUsed = realtimeBattle.ArrowsUsed;
            var stars = ProgressRules.Stars(won, battle.PlayerCondition, arrowsUsed, level.stars);
            var coins = won ? stars * level.coinsPerStar : 0;
            var gems = 0;

            Telemetry.Event("level_end", "level", level.id, "won", won, "stars", stars, "arrows", arrowsUsed,
                "reason", realtimeBattle.Defeat.ToString(), "seconds", Mathf.RoundToInt(Time.time - startTime));

            var save = SaveSystem.Data;
            if (won && !string.IsNullOrEmpty(level.id))
            {
                save.RecordWin(level.id, stars);
                save.coins += coins;
                if (stars >= 3 && save.ClaimGems("stars3." + level.id, ProgressRules.GemsForThreeStars))
                    gems += ProgressRules.GemsForThreeStars;
                if (level.firstWinGems > 0 && save.ClaimGems("win." + level.id, level.firstWinGems))
                    gems += level.firstWinGems;
                Armory.GrantStoryItems(save);
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
            hud.SetArsenal(null, null);
            hud.SetShield(null);
            var defeatKey = won ? null : "defeat." + realtimeBattle.Defeat.ToString().ToLowerInvariant();
            hud.PlayDialogue(won ? level.outroDialogue : null, () => hud.ShowResult(won, stars, coins, gems, defeatKey, next, map));
        }

        // ------------------------------------------------------------------ enemies

        static EnemyDefinition defaultType;

        static EnemyDefinition DefaultType
        {
            get
            {
                if (defaultType == null)
                {
                    defaultType = ScriptableObject.CreateInstance<EnemyDefinition>();
                    defaultType.name = "Turanian Archer";
                }
                return defaultType;
            }
        }

        Combatant SpawnEnemy(EnemySpawn spawn, int order)
        {
            var type = spawn.type != null ? spawn.type : DefaultType;
            var standHeight = spawn.height + (type.mounted ? HorseHeight : 0f);
            var stand = new Vector3(spawn.x, groundY + standHeight, 0f);
            var enemy = Instantiate(enemyPrefab, stand, Quaternion.identity);
            enemy.name = type.name;
            enemy.transform.localScale = Vector3.one * type.scale;
            enemy.Health.SetMax(type.maxHealth);

            var skin = enemy.GetComponent<CharacterSkin>();
            if (skin != null)
            {
                skin.Apply(type.look);
                if (type.tint != Color.white)
                    skin.Tint(type.tint);
            }

            var ai = enemy.GetComponent<EnemyArcherAI>();
            if (ai != null)
            {
                ai.SetAccuracy(type.accuracy.Scaled(GameSettings.EnemyErrorMultiplier));
                ai.SetTactics(type.tactics.Clone());
                ai.ProjectileDamage = type.damage * GameSettings.EnemyDamageMultiplier;
                ai.PhaseChanged += OnBossPhase;
            }
            var bow = enemy.GetComponentInChildren<Bow>();
            if (bow != null)
                bow.SetProjectile(type.projectile, type.minSpeed, type.maxSpeed, type.gravityScale, type.windScale);
            if (type.shield)
                AddShield(enemy);
            if (type.mounted)
                AddHorse(enemy);

            var ragdoll = enemy.GetComponent<Ragdoll2D>();
            enemy.Health.Died += info => StartCoroutine(RemoveBody(enemy));

            switch (type.role)
            {
                case EnemyRole.Raider:
                    if (ai != null)
                        ai.enabled = false;
                    var raider = enemy.gameObject.AddComponent<Raider>();
                    raider.speed = type.runSpeed;
                    raider.damage = type.meleeDamage * GameSettings.EnemyDamageMultiplier;
                    raider.goalX = player.transform.position.x + (playerShield != null ? 2.2f : 1.5f) + order * 0.4f;
                    raider.Target = player.Health;
                    raider.Shield = () => playerShield;
                    ragdoll.DisableOnDeath(raider);
                    // Raiders run in from beyond the edge of the view.
                    enemy.transform.position = stand + Vector3.right * EntryDistance;
                    return enemy;

                case EnemyRole.Shaman:
                    if (ai != null)
                        ai.enabled = false;
                    var shaman = enemy.gameObject.AddComponent<ShamanCaster>();
                    shaman.interval = type.castInterval;
                    shaman.shieldStrength = type.shieldStrength;
                    shaman.Allies = () => realtimeBattle.LivingEnemies.Where(e => e != enemy);
                    shaman.enabled = false;
                    ragdoll.DisableOnDeath(shaman);
                    StartCoroutine(Enter(enemy, stand, spawn.height > 0.05f, order, () => shaman.enabled = true));
                    return enemy;

                default:
                    var brain = enemy.gameObject.AddComponent<EnemyBrain>();
                    var cooldown = type.cooldown * GameSettings.EnemyCooldownMultiplier;
                    brain.minCooldown = cooldown.x;
                    brain.maxCooldown = Mathf.Max(cooldown.x, cooldown.y);
                    brain.ChooseTarget = ChooseTarget;
                    ragdoll.DisableOnDeath(brain);
                    StartCoroutine(Enter(enemy, stand, spawn.height > 0.05f, order, () =>
                    {
                        if (type.patrolRange > 0f)
                        {
                            var patrol = enemy.gameObject.AddComponent<Patrol>();
                            patrol.range = type.patrolRange;
                            patrol.speed = type.patrolSpeed;
                            ragdoll.DisableOnDeath(patrol);
                        }
                        brain.Run();
                    }));
                    return enemy;
            }
        }

        /// <summary>In escort levels most arrows fly at the companion.</summary>
        Combatant ChooseTarget()
        {
            if (companion != null && companion.IsAlive && Random.value < 0.6f)
                return companion;
            return player;
        }

        /// <summary>Walks (or rides) in from the right, or drops onto a rock from above.</summary>
        IEnumerator Enter(Combatant enemy, Vector3 stand, bool onPlatform, int order, Action arrived)
        {
            var from = onPlatform ? stand + Vector3.up * 7f : stand + Vector3.right * (EntryDistance + order * 1.5f);
            enemy.transform.position = from;
            var duration = onPlatform ? 0.5f + order * 0.15f : 1.6f + order * 0.3f;
            for (var t = 0f; t < duration && enemy != null && enemy.IsAlive; t += Time.deltaTime)
            {
                var k = t / duration;
                var p = onPlatform ? Vector3.Lerp(from, stand, k * k) : Vector3.Lerp(from, stand, Mathf.SmoothStep(0f, 1f, k));
                if (!onPlatform)
                    p.y += Mathf.Abs(Mathf.Sin(k * 14f)) * 0.12f;
                enemy.transform.position = p;
                yield return null;
            }
            if (enemy == null || !enemy.IsAlive)
                yield break;
            enemy.transform.position = stand;
            arrived();
        }

        IEnumerator RemoveBody(Combatant enemy)
        {
            yield return new WaitForSeconds(4f);
            if (enemy == null)
                yield break;
            var skin = enemy.GetComponent<CharacterSkin>();
            if (skin != null)
                yield return skin.FadeOut(0.8f);
            if (enemy != null)
                Destroy(enemy.gameObject);
        }

        void OnBossPhase(EnemyArcherAI ai, BossPhase phase)
        {
            if (hud != null && !string.IsNullOrEmpty(phase.announceKey))
                hud.ShowPopup(Loc.T(phase.announceKey));
        }

        Combatant SpawnCompanion(float x, CharacterLook look)
        {
            var spawned = Instantiate(companionPrefab, new Vector3(x, groundY, 0f), Quaternion.identity);
            var skin = spawned.GetComponent<CharacterSkin>();
            if (skin != null)
                skin.Apply(look);
            return spawned;
        }

        void AddShield(Combatant enemy)
        {
            var torso = enemy.BodyTarget;
            var facing = enemy.FacingRight ? 1f : -1f;
            var shield = new GameObject("Shield");
            shield.transform.SetParent(torso, false);
            shield.transform.localPosition = new Vector3(0.42f * facing, -0.05f, 0f);
            ArtLibrary.Renderer(shield.transform, "Art", ArtLibrary.Prop("turan_shield"), 38);
            shield.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 1.1f);
            shield.AddComponent<HitZone>().SetZone(HitZoneType.Armor);
        }

        void AddHorse(Combatant enemy)
        {
            var horse = new GameObject("Horse");
            horse.transform.SetParent(enemy.transform, false);
            horse.transform.localPosition = new Vector3(0.2f, -HorseHeight, 0f);
            var renderer = ArtLibrary.Renderer(horse.transform, "Art", ArtLibrary.Prop("horse"), 29);
            renderer.flipX = !enemy.FacingRight;
            var box = horse.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2.2f, 0.7f);
            box.offset = new Vector2(0f, 1.0f);
            horse.AddComponent<HitZone>().SetZone(HitZoneType.Limb);
            enemy.Health.Died += info =>
            {
                box.enabled = false;
                horse.transform.SetParent(null, true);
                horse.AddComponent<RunAway>();
            };
        }

        TrialTarget CreateTarget(TrialTargetSpawn spawn, List<Health> bystanders)
        {
            var position = new Vector2(spawn.position.x, groundY + spawn.position.y);
            GameObject target;
            switch (spawn.kind)
            {
                case TrialTargetKind.Apple:
                    var villager = SpawnCompanion(spawn.position.x, CharacterLook.Villager);
                    bystanders.Add(villager.Health);
                    target = PropObject("Apple", "apple", villager.HeadTarget.position + new Vector3(0f, 0.42f, 0f), 40);
                    target.AddComponent<CircleCollider2D>().radius = 0.17f;
                    target.transform.SetParent(villager.transform, true);
                    break;
                case TrialTargetKind.Lantern:
                    target = PropObject("Lantern", "lantern", position, 20);
                    target.AddComponent<CircleCollider2D>().radius = 0.25f;
                    break;
                default:
                    target = PropObject("Target", "target", position + new Vector2(0f, 0.44f), 20);
                    target.AddComponent<CircleCollider2D>().radius = 0.26f;
                    if (spawn.position.y > 0.3f)
                        Sliced("Post", "cover_wood", new Vector2(position.x, groundY + spawn.position.y * 0.5f), new Vector2(0.15f, spawn.position.y), 19);
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

        // ------------------------------------------------------------------ scenery

        void CreateCover(CoverSpawn cover)
        {
            var block = Sliced(cover.flammable ? "Wooden Cover" : "Palisade", cover.flammable ? "cover_wood" : "palisade",
                new Vector2(cover.x, groundY + cover.height * 0.5f), new Vector2(cover.width, cover.height), 12);
            block.AddComponent<BoxCollider2D>().size = new Vector2(cover.width, cover.height);
            if (cover.flammable)
                block.AddComponent<Flammable>();
        }

        void CreatePlatform(float x, float height)
        {
            var block = Sliced("Rock Platform", "rock_platform", new Vector2(x, groundY + height * 0.5f), new Vector2(2.4f, height + 0.15f), 11);
            block.AddComponent<BoxCollider2D>().size = new Vector2(2.4f, height);
        }

        GameObject Sliced(string name, string prop, Vector2 position, Vector2 size, int order)
        {
            var sprite = ArtLibrary.Prop(prop) ?? squareSprite;
            var renderer = ArtLibrary.Renderer(null, name, sprite, order, position);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            return renderer.gameObject;
        }

        GameObject PropObject(string name, string prop, Vector2 position, int order)
        {
            return ArtLibrary.Renderer(null, name, ArtLibrary.Prop(prop) ?? circleSprite, order, position).gameObject;
        }
    }

    /// <summary>A riderless horse gallops off and fades away.</summary>
    public class RunAway : MonoBehaviour
    {
        float t;

        void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.right * 5f * Time.deltaTime;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                var c = renderer.color;
                c.a = 1f - t / 1.5f;
                renderer.color = c;
            }
            if (t > 1.5f)
                Destroy(gameObject);
        }
    }
}
