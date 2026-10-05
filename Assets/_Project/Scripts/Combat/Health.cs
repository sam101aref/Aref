using System;
using UnityEngine;

namespace Arash.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxHealth = 100f;

        /// <summary>Raised for every hit on this character; the bool is true when the hit killed it.</summary>
        public event Action<DamageInfo, bool> Damaged;
        public event Action<DamageInfo> Died;
        /// <summary>Raised whenever current or maximum health changes.</summary>
        public event Action Changed;

        /// <summary>Raised for hits on any character, for scene-wide effects (slow motion, shake…).</summary>
        public static event Action<Health, DamageInfo, bool> AnyDamaged;

        float current = -1f;
        float headResistance;
        float bodyResistance;
        float limbResistance;

        public float Max { get { return maxHealth; } }
        public float Current
        {
            get
            {
                if (current < 0f)
                    current = maxHealth;
                return current;
            }
        }
        public float Fraction { get { return Current / maxHealth; } }
        public bool IsDead { get { return Current <= 0f; } }

        /// <summary>Sets the maximum and refills to full (used when spawning from level data).</summary>
        public void SetMax(float max)
        {
            maxHealth = Mathf.Max(1f, max);
            current = maxHealth;
            if (Changed != null)
                Changed();
        }

        /// <summary>
        /// Armour (F-57): the fraction of damage blocked on the head, the torso and the limbs.
        /// </summary>
        public void SetResistance(float head, float body, float limb)
        {
            headResistance = Mathf.Clamp01(head);
            bodyResistance = Mathf.Clamp01(body);
            limbResistance = Mathf.Clamp01(limb);
        }

        /// <summary>How much of a hit on this zone gets through the armour.</summary>
        public float DamageTaken(HitZoneType zone)
        {
            switch (zone)
            {
                case HitZoneType.Head: return 1f - headResistance;
                case HitZoneType.Torso: return 1f - bodyResistance;
                case HitZoneType.Limb: return 1f - limbResistance;
                default: return 1f;
            }
        }

        /// <summary>Restores health (haoma arrows); the dead stay dead.</summary>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
                return;
            current = Mathf.Min(maxHealth, Current + amount);
            if (Changed != null)
                Changed();
        }

        public void ApplyDamage(DamageInfo info)
        {
            if (IsDead)
                return;

            current = Mathf.Max(0f, Current - info.Amount);
            var killed = current <= 0f;

            if (Changed != null)
                Changed();
            if (Damaged != null)
                Damaged(info, killed);
            if (AnyDamaged != null)
                AnyDamaged(this, info, killed);
            if (killed && Died != null)
                Died(info);
        }
    }
}
