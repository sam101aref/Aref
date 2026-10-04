using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Marks a collider as part of a character's body. Arrows that stick into it damage the
    /// <see cref="Health"/> above it in the hierarchy, scaled by the zone (head, torso, limb, armor).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HitZone : MonoBehaviour
    {
        [SerializeField] HitZoneType zone = HitZoneType.Torso;
        [SerializeField, Tooltip("Overrides the zone's default multiplier when 0 or more.")]
        float multiplierOverride = -1f;

        Health health;

        public HitZoneType Zone { get { return zone; } }

        public void SetZone(HitZoneType newZone)
        {
            zone = newZone;
        }
        public Health Health
        {
            get
            {
                if (health == null)
                    health = GetComponentInParent<Health>();
                return health;
            }
        }

        public void ReceiveHit(Arrow arrow, ArrowHit hit)
        {
            var target = Health;
            if (target == null)
                return;

            var amount = multiplierOverride >= 0f
                ? arrow.Damage * multiplierOverride
                : DamageRules.Compute(arrow.Damage, zone);
            target.ApplyDamage(new DamageInfo(amount, zone, hit.Point, hit.Velocity, hit.Collider, arrow.Shooter));
        }
    }
}
