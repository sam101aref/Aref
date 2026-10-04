using System;
using System.Collections.Generic;
using Arash.Combat;
using Arash.Core;
using Arash.UI;
using UnityEngine;

namespace Arash.Levels
{
    /// <summary>
    /// Sets up the Battle scene from the current <see cref="LevelDefinition"/> (F-11): sky and ground
    /// colours, enemies (with rock platforms for raised ones), difficulty settings. When the battle
    /// ends it rates it (F-12), saves the result (F-14) and shows the end screen.
    /// </summary>
    public class LevelRunner : MonoBehaviour
    {
        [SerializeField] TurnManager turnManager;
        [SerializeField] BattleHud hud;

        [Header("Player")]
        [SerializeField] Combatant player;
        [SerializeField] AimController playerAim;
        [SerializeField] TrajectoryPreview playerPreview;

        [Header("World")]
        [SerializeField] Combatant enemyPrefab;
        [SerializeField] SpriteRenderer ground;
        [SerializeField] Camera sceneCamera;
        [SerializeField] float groundY = -3f;
        [SerializeField] Sprite platformSprite;
        [SerializeField] Material spriteMaterial;
        [SerializeField] Color platformColor = new Color(0.42f, 0.38f, 0.34f);

        LevelCatalog catalog;
        LevelDefinition level;
        int arrowsUsed;

        public LevelDefinition Level { get { return level; } }

        void Awake()
        {
            catalog = LevelCatalog.Load();
            level = SceneFlow.CurrentLevel != null ? SceneFlow.CurrentLevel : (catalog != null ? catalog.First() : null);

            var enemies = new List<Combatant>();
            if (level != null)
            {
                if (sceneCamera != null)
                    sceneCamera.backgroundColor = level.skyColor;
                if (ground != null)
                    ground.color = level.groundColor;
                foreach (var spawn in level.enemies)
                    enemies.Add(Spawn(spawn));
            }
            else
            {
                Debug.LogWarning("[LevelRunner] No level catalog found; using a default enemy.");
            }
            if (enemies.Count == 0)
                enemies.Add(Spawn(new EnemySpawn()));

            turnManager.Configure(player, enemies);
            if (playerPreview != null)
                playerPreview.Duration = GameSettings.PreviewDuration;
        }

        void OnEnable()
        {
            if (playerAim != null)
                playerAim.Shot += OnPlayerShot;
            if (turnManager != null)
                turnManager.BattleEnded += OnBattleEnded;
        }

        void OnDisable()
        {
            if (playerAim != null)
                playerAim.Shot -= OnPlayerShot;
            if (turnManager != null)
                turnManager.BattleEnded -= OnBattleEnded;
        }

        void Start()
        {
            if (hud == null)
                return;
            hud.SetLevel(level != null ? level.titleKey : null, level != null ? level.hintKey : null);
            if (level != null)
                hud.ShowIntro(level.introKey);
        }

        void OnPlayerShot(Arrow arrow)
        {
            arrowsUsed++;
            if (hud != null)
                hud.HideHint();
        }

        void OnBattleEnded(bool won)
        {
            var stars = level != null
                ? ProgressRules.Stars(won, player.Health.Fraction, arrowsUsed, level.stars)
                : (won ? 1 : 0);
            var coins = won && level != null ? stars * level.coinsPerStar : 0;

            var save = SaveSystem.Data;
            if (won && level != null)
            {
                save.RecordWin(level.id, stars);
                save.coins += coins;
                SaveSystem.Save();
            }

            Action next = null;
            var nextLevel = level != null && catalog != null ? catalog.Next(level) : null;
            if (won && nextLevel != null && catalog.IsUnlocked(nextLevel, save))
                next = () => SceneFlow.Play(nextLevel);

            if (hud != null)
                hud.ShowResult(won, stars, coins, next);
        }

        Combatant Spawn(EnemySpawn spawn)
        {
            if (spawn.height > 0.05f)
                CreatePlatform(spawn.x, spawn.height);

            var enemy = Instantiate(enemyPrefab, new Vector3(spawn.x, groundY + spawn.height, 0f), Quaternion.identity);
            enemy.Health.SetMax(spawn.maxHealth);
            var ai = enemy.GetComponent<EnemyArcherAI>();
            if (ai != null && spawn.accuracy != null)
                ai.SetAccuracy(spawn.accuracy.Scaled(GameSettings.EnemyErrorMultiplier));
            return enemy;
        }

        void CreatePlatform(float x, float height)
        {
            var platform = new GameObject("Rock Platform");
            platform.transform.position = new Vector3(x, groundY + height * 0.5f, 0f);
            var size = new Vector2(2.4f, height);

            var renderer = platform.AddComponent<SpriteRenderer>();
            renderer.sprite = platformSprite;
            if (spriteMaterial != null)
                renderer.sharedMaterial = spriteMaterial;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = platformColor;
            renderer.sortingOrder = 11;

            platform.AddComponent<BoxCollider2D>().size = size;
        }
    }
}
