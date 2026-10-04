using System;
using System.Collections.Generic;
using Arash.Combat;
using UnityEngine;

namespace Arash.Levels
{
    [Serializable]
    public class EnemySpawn
    {
        [Tooltip("World X of the enemy. The player stands at -6.")]
        public float x = 16f;
        [Tooltip("Height above the ground; a rock platform is placed underneath.")]
        public float height;
        public float maxHealth = 100f;
        public AiAccuracy accuracy = new AiAccuracy();
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

        [Header("Battle")]
        public List<EnemySpawn> enemies = new List<EnemySpawn>();
        public StarRules stars = new StarRules();
        [Min(0)] public int coinsPerStar = 10;
    }
}
