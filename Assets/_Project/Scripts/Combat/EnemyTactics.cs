using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>A boss phase (F-28): kicks in once health drops below a fraction.</summary>
    [Serializable]
    public class BossPhase
    {
        [Range(0f, 1f)] public float healthBelow = 0.5f;
        [Tooltip("Multiplier on the aiming error in this phase (lower is deadlier).")]
        public float errorMultiplier = 0.7f;
        [Min(1)] public int shotsPerTurn = 1;
        [Tooltip("Moves this far (either way) before each shot; 0 stays put.")]
        public float repositionRange;
        [Tooltip("Optional line shown when the phase starts.")]
        public string announceKey;
    }

    /// <summary>How an enemy fights beyond plain aiming (F-27, F-28).</summary>
    [Serializable]
    public class EnemyTactics
    {
        [Tooltip("Lob shots on a high arc (slingers, catapults).")]
        public bool highArc;
        [Min(1), Tooltip("Projectiles per turn, fanned out by the volley spread.")]
        public int shotsPerTurn = 1;
        public float volleySpread = 4f;
        [Tooltip("Moves this far (either way) before each shot, e.g. mountain assassins.")]
        public float repositionRange;
        public List<BossPhase> phases = new List<BossPhase>();

        public EnemyTactics Clone()
        {
            var copy = (EnemyTactics)MemberwiseClone();
            copy.phases = new List<BossPhase>(phases);
            return copy;
        }

        /// <summary>The deepest phase reached at this health fraction, or null.</summary>
        public BossPhase PhaseFor(float healthFraction)
        {
            BossPhase current = null;
            foreach (var phase in phases)
                if (healthFraction < phase.healthBelow && (current == null || phase.healthBelow < current.healthBelow))
                    current = phase;
            return current;
        }
    }
}
