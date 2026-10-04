using UnityEngine;

namespace Arash.Combat
{
    /// <summary>Scene-wide battle conditions. Wind (F-26) is a horizontal acceleration on projectiles.</summary>
    public static class BattleEnvironment
    {
        /// <summary>Horizontal wind acceleration in units/s²; positive blows to the right.</summary>
        public static float Wind { get; set; }

        public static Vector2 WindAcceleration { get { return new Vector2(Wind, 0f); } }

        /// <summary>A wind for one turn: the base strength plus a random gust within the variance.</summary>
        public static float RollWind(float strength, float variance, float noise)
        {
            return strength + variance * Mathf.Clamp(noise, -1f, 1f);
        }
    }
}
