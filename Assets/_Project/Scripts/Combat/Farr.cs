using System;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>How much farr (divine glory, F-31) each kind of good shot earns.</summary>
    public static class FarrRules
    {
        public const float LimbHit = 0.12f;
        public const float BodyHit = 0.18f;
        public const float HeadHit = 0.4f;
        public const float KillBonus = 0.15f;
        public const float Intercept = 0.2f;

        public static float Gain(HitZoneType zone, bool killed)
        {
            float gain;
            switch (zone)
            {
                case HitZoneType.Head: gain = HeadHit; break;
                case HitZoneType.Torso: gain = BodyHit; break;
                case HitZoneType.Limb: gain = LimbHit; break;
                default: gain = 0f; break;
            }
            return gain + (killed ? KillBonus : 0f);
        }
    }

    /// <summary>The farr meter: fills with good shots, empties when a special arrow is fired.</summary>
    public class FarrMeter
    {
        public float Value { get; private set; }
        public float GainMultiplier = 1f;

        public bool IsFull { get { return Value >= 1f; } }

        public event Action Changed;

        public void Add(float amount)
        {
            if (amount <= 0f)
                return;
            Value = Mathf.Clamp01(Value + amount * GainMultiplier);
            if (Changed != null)
                Changed();
        }

        public bool TrySpend()
        {
            if (!IsFull)
                return false;
            Value = 0f;
            if (Changed != null)
                Changed();
            return true;
        }
    }
}
