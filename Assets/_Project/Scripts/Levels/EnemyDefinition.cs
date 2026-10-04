using Arash.Combat;
using UnityEngine;

namespace Arash.Levels
{
    /// <summary>
    /// An enemy type (F-27, F-28): stats, projectile, shield, movement and tactics applied on top of
    /// the basic Turanian archer when it is spawned.
    /// </summary>
    [CreateAssetMenu(menuName = "Arash/Enemy Type", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        public string nameKey;
        public float maxHealth = 100f;
        public AiAccuracy accuracy = new AiAccuracy();
        public EnemyTactics tactics = new EnemyTactics();

        [Header("Look")]
        public Color tint = Color.white;
        [Min(0.5f)] public float scale = 1f;

        [Header("Projectile")]
        [Tooltip("Leave empty for normal arrows.")]
        public Arrow projectile;
        public float minSpeed = 8f;
        public float maxSpeed = 28f;
        public float gravityScale = 1f;
        public float windScale = 1f;

        [Header("Defence and movement")]
        [Tooltip("Carries a shield that blocks body shots.")]
        public bool shield;
        [Tooltip("Rides back and forth this far (mounted enemies); 0 stands still.")]
        public float patrolRange;
        public float patrolSpeed = 2.5f;
    }
}
