using Arash.Art;
using Arash.Combat;
using UnityEngine;

namespace Arash.Levels
{
    public enum EnemyRole
    {
        /// <summary>Shoots from where it stands (archers, spearmen, slingers, divs…).</summary>
        Archer,
        /// <summary>Runs at Arash and strikes at close range (F-63).</summary>
        Raider,
        /// <summary>Shields allies with magic and does not shoot (F-63).</summary>
        Shaman,
    }

    /// <summary>
    /// An enemy type (F-27, F-28, F-63): look, stats, projectile, timing, defence, movement and
    /// tactics applied on top of the basic Turanian archer when it is spawned.
    /// </summary>
    [CreateAssetMenu(menuName = "Arash/Enemy Type", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        public string nameKey;
        public CharacterLook look = CharacterLook.Turanian;
        public EnemyRole role = EnemyRole.Archer;
        public float maxHealth = 100f;
        [Tooltip("Gives gems the first time it is defeated.")]
        public bool boss;

        [Header("Look")]
        [Tooltip("Multiplied over the look's own colours; white keeps them.")]
        public Color tint = Color.white;
        [Min(0.5f)] public float scale = 1f;

        [Header("Shooting")]
        public AiAccuracy accuracy = new AiAccuracy();
        public EnemyTactics tactics = new EnemyTactics();
        [Tooltip("Seconds between shots (random in this range).")]
        public Vector2 cooldown = new Vector2(2.8f, 4.6f);

        [Header("Projectile")]
        [Tooltip("Leave empty for normal arrows.")]
        public Arrow projectile;
        [Tooltip("Damage of each projectile before the zone multiplier.")]
        public float damage = 25f;
        public float minSpeed = 8f;
        public float maxSpeed = 28f;
        public float gravityScale = 1f;
        public float windScale = 1f;

        [Header("Defence and movement")]
        [Tooltip("Carries a shield that blocks body shots.")]
        public bool shield;
        [Tooltip("Rides a horse; sits higher.")]
        public bool mounted;
        [Tooltip("Rides or walks back and forth this far; 0 stands still.")]
        public float patrolRange;
        public float patrolSpeed = 2.5f;

        [Header("Raider")]
        public float runSpeed = 2.2f;
        public float meleeDamage = 12f;

        [Header("Shaman")]
        public float castInterval = 6f;
        public float shieldStrength = 80f;
    }
}
