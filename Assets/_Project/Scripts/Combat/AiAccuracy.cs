using System;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// How well an enemy shoots. Each shot gets a random error that shrinks after every miss, so
    /// an enemy that keeps missing "finds its range" and pressure builds (GDD 4.6).
    /// </summary>
    [Serializable]
    public class AiAccuracy
    {
        [Tooltip("Angle error in degrees on the first shot.")]
        public float initialAngleError = 7f;
        [Tooltip("Smallest angle error, reached after enough misses.")]
        public float minAngleError = 0.6f;
        [Tooltip("Error is multiplied by this after each miss.")]
        [Range(0.1f, 1f)] public float decayPerMiss = 0.6f;
        [Tooltip("Power error per degree of angle error.")]
        public float powerErrorPerDegree = 0.015f;
        [Tooltip("Chance of aiming at the head instead of the body.")]
        [Range(0f, 1f)] public float headshotChance = 0.25f;

        public float NextError(float currentError)
        {
            return Mathf.Max(minAngleError, currentError * decayPerMiss);
        }

        /// <summary>
        /// Adds error to a perfect aim. <paramref name="angleNoise"/> and <paramref name="powerNoise"/>
        /// are random numbers in [-1, 1].
        /// </summary>
        public AimState ApplyError(AimState perfect, float error, float angleNoise, float powerNoise,
            float minAngle, float maxAngle)
        {
            var angle = Mathf.Clamp(perfect.Angle + angleNoise * error, minAngle, maxAngle);
            var power = Mathf.Clamp01(perfect.Power + powerNoise * error * powerErrorPerDegree);
            return new AimState(perfect.IsValid, angle, power, perfect.Facing);
        }
    }
}
