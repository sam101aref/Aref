using UnityEngine;

namespace Arash.Combat
{
    public enum HitZoneType
    {
        Head,
        Torso,
        Limb,
        Armor,
    }

    /// <summary>Everything known about one arrow hit.</summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly HitZoneType Zone;
        public readonly Vector2 Point;
        /// <summary>Arrow velocity at impact; its direction is used to push ragdolls.</summary>
        public readonly Vector2 Velocity;
        public readonly Collider2D Collider;
        /// <summary>Root transform of whoever shot the arrow (may be null).</summary>
        public readonly Transform Source;

        public DamageInfo(float amount, HitZoneType zone, Vector2 point, Vector2 velocity, Collider2D collider, Transform source)
        {
            Amount = amount;
            Zone = zone;
            Point = point;
            Velocity = velocity;
            Collider = collider;
            Source = source;
        }

        public bool IsHeadshot { get { return Zone == HitZoneType.Head; } }
    }

    /// <summary>Damage multipliers per body zone (GDD 4.4). A standard arrow does 100 damage.</summary>
    public static class DamageRules
    {
        public const float StandardArrowDamage = 100f;

        public static float Multiplier(HitZoneType zone)
        {
            switch (zone)
            {
                case HitZoneType.Head: return 1f;
                case HitZoneType.Torso: return 0.4f;
                case HitZoneType.Limb: return 0.25f;
                default: return 0f;
            }
        }

        public static float Compute(float arrowDamage, HitZoneType zone)
        {
            return Mathf.Max(0f, arrowDamage) * Multiplier(zone);
        }
    }
}
