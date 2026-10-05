using System;
using System.Collections.Generic;
using Arash.Art;
using Arash.Combat;
using Arash.Story;
using UnityEngine;

namespace Arash.Levels
{
    public enum LevelMode
    {
        /// <summary>Waves of enemies; both sides shoot at once (F-51).</summary>
        Battle,
        /// <summary>A battle in which a companion behind Arash must survive too (F-24).</summary>
        Escort,
        /// <summary>Hit every target with limited arrows (F-25).</summary>
        Trial,
        /// <summary>Guide the great arrow from Damavand to the Oxus (F-30).</summary>
        Flight,
    }

    [Serializable]
    public class EnemySpawn
    {
        [Tooltip("Enemy type; empty means a plain Turanian archer.")]
        public EnemyDefinition type;
        [Tooltip("World X of the enemy. The player stands at -6.")]
        public float x = 14f;
        [Tooltip("Height above the ground; a rock platform is placed underneath.")]
        public float height;
    }

    /// <summary>One wave (F-51): its enemies arrive together, after an optional story moment.</summary>
    [Serializable]
    public class EnemyWave
    {
        public List<EnemySpawn> enemies = new List<EnemySpawn>();
        [Tooltip("Lines shown before the wave arrives (F-61); the battle waits for them.")]
        public List<DialogueLine> dialogue = new List<DialogueLine>();
    }

    [Serializable]
    public class CoverSpawn
    {
        public float x = 4f;
        public float width = 0.6f;
        public float height = 1.6f;
        [Tooltip("Wooden covers burn when hit by fire arrows.")]
        public bool flammable = true;
    }

    [Serializable]
    public class WindSettings
    {
        [Tooltip("Base wind in units/s²; positive blows towards the enemy.")]
        public float strength;
        [Tooltip("Every few seconds the wind changes randomly by up to this much.")]
        public float variance;
    }

    [Serializable]
    public class TrialTargetSpawn
    {
        public TrialTargetKind kind;
        public Vector2 position = new Vector2(10f, 0f);
        [Tooltip("Movement range; zero for a still target.")]
        public Vector2 movement;
        public float period = 2.5f;
    }

    [Serializable]
    public class FlightSettings
    {
        [Tooltip("Distance from Damavand to the Oxus in world units.")]
        public float length = 900f;
        public float speed = 14f;
        [Range(0f, 1f)] public float obstacleDensity = 0.5f;
        public int seed = 1;
    }

    [Serializable]
    public class StarRules
    {
        [Range(0f, 1f), Tooltip("Second star: finish with at least this much health.")]
        public float healthForSecondStar = 0.5f;
        [Min(1), Tooltip("Third star: win using at most this many arrows.")]
        public int maxArrowsForThirdStar = 3;
    }

    /// <summary>One level (F-11): story, look, enemy waves and star rules. Add levels without code.</summary>
    [CreateAssetMenu(menuName = "Arash/Level", fileName = "Level")]
    public class LevelDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by the save file. Never change it after release.")]
        public string id;
        public string titleKey;
        public string introKey;
        [Tooltip("Optional hint shown until the first shot (tutorial levels).")]
        public string hintKey;

        [Header("Look")]
        public Biome biome = Biome.Village;
        public TimeOfDay time = TimeOfDay.Day;

        [Header("Story")]
        [Tooltip("Played before the battle the first time (F-60).")]
        public CutsceneDefinition introCutscene;
        public List<DialogueLine> introDialogue = new List<DialogueLine>();
        public List<DialogueLine> outroDialogue = new List<DialogueLine>();
        [Tooltip("Played after the first win.")]
        public CutsceneDefinition outroCutscene;

        [Header("Battle")]
        public LevelMode mode = LevelMode.Battle;
        public List<EnemyWave> waves = new List<EnemyWave>();
        public List<CoverSpawn> covers = new List<CoverSpawn>();
        public WindSettings wind = new WindSettings();

        [Header("Escort")]
        [Tooltip("Who stands behind Arash and must survive.")]
        public CharacterLook companion = CharacterLook.Envoy;

        [Header("Trial")]
        public List<TrialTargetSpawn> targets = new List<TrialTargetSpawn>();
        [Min(1)] public int arrowLimit = 5;

        [Header("Flight")]
        public FlightSettings flight = new FlightSettings();

        [Header("Rewards")]
        public StarRules stars = new StarRules();
        [Min(0)] public int coinsPerStar = 15;
        [Min(0), Tooltip("Gems for the first win (bosses).")]
        public int firstWinGems;

        public int EnemyCount
        {
            get
            {
                var count = 0;
                foreach (var wave in waves)
                    count += wave.enemies.Count;
                return count;
            }
        }
    }
}
