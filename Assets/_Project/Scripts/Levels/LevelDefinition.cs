using System;
using System.Collections.Generic;
using Arash.Combat;
using Arash.Story;
using UnityEngine;

namespace Arash.Levels
{
    public enum LevelMode
    {
        /// <summary>Turn-based archery duel, one or more enemies (F-08, F-22).</summary>
        Duel,
        /// <summary>Raiders march on the palisade in waves (F-23).</summary>
        Waves,
        /// <summary>Protect a companion from enemy archers (F-24).</summary>
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
        public float x = 16f;
        [Tooltip("Height above the ground; a rock platform is placed underneath.")]
        public float height;
        [Tooltip("Used when no type is set.")]
        public float maxHealth = 100f;
        [Tooltip("Used when no type is set.")]
        public AiAccuracy accuracy = new AiAccuracy();
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
        [Tooltip("Each turn the wind changes randomly by up to this much.")]
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

    /// <summary>One level (F-11): story text keys, look, enemies and star rules. Add levels without code.</summary>
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
        public Color skyColor = new Color(0.98f, 0.82f, 0.55f);
        public Color groundColor = new Color(0.55f, 0.43f, 0.28f);

        [Header("Story")]
        public List<DialogueLine> introDialogue = new List<DialogueLine>();
        public List<DialogueLine> outroDialogue = new List<DialogueLine>();
        [Tooltip("Played after the first win.")]
        public CutsceneDefinition outroCutscene;

        [Header("Battle")]
        public LevelMode mode = LevelMode.Duel;
        public List<EnemySpawn> enemies = new List<EnemySpawn>();
        public List<CoverSpawn> covers = new List<CoverSpawn>();
        public WindSettings wind = new WindSettings();

        [Header("Waves")]
        public List<WaveSpawn> waves = new List<WaveSpawn>();
        [Min(1)] public int palisadeLives = 3;

        [Header("Escort")]
        [Tooltip("Seconds to hold out; 0 means defeat every archer.")]
        public float surviveSeconds = 30f;
        public float enemyFireInterval = 4f;

        [Header("Trial")]
        public List<TrialTargetSpawn> targets = new List<TrialTargetSpawn>();
        [Min(1)] public int arrowLimit = 5;

        [Header("Flight")]
        public FlightSettings flight = new FlightSettings();
        public StarRules stars = new StarRules();
        [Min(0)] public int coinsPerStar = 10;
    }
}
