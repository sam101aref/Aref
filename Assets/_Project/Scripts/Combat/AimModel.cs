using System;
using UnityEngine;

namespace Arash.Combat
{
    [Serializable]
    public class AimSettings
    {
        [Tooltip("Drag length, as a fraction of the screen height, that gives full power.")]
        [Range(0.05f, 1f)] public float fullPowerDrag = 0.3f;

        [Tooltip("Drags shorter than this (fraction of the screen height) cancel the shot.")]
        [Range(0f, 0.2f)] public float cancelDrag = 0.04f;

        [Tooltip("Lowest and highest shot angle in degrees, measured from the facing direction.")]
        public float minAngle = -30f;
        public float maxAngle = 85f;
    }

    /// <summary>The result of reading a drag gesture: where the arrow goes and how hard.</summary>
    public readonly struct AimState
    {
        public static readonly AimState Cancelled = new AimState(false, 0f, 0f, 1f);

        public readonly bool IsValid;
        /// <summary>Angle in degrees from the facing direction; positive is up.</summary>
        public readonly float Angle;
        /// <summary>Draw strength from 0 to 1.</summary>
        public readonly float Power;
        /// <summary>+1 when shooting to the right, -1 when shooting to the left.</summary>
        public readonly float Facing;

        public AimState(bool isValid, float angle, float power, float facing)
        {
            IsValid = isValid;
            Angle = angle;
            Power = power;
            Facing = facing;
        }

        /// <summary>World-space unit vector of the shot.</summary>
        public Vector2 Direction
        {
            get
            {
                var radians = Angle * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(radians) * Facing, Mathf.Sin(radians));
            }
        }
    }

    /// <summary>
    /// Turns a pull-back drag into an aim, like drawing a bow: the arrow flies opposite to the drag.
    /// Lengths are measured relative to the screen height so the feel is the same on every device.
    /// </summary>
    public static class AimModel
    {
        public static AimState Evaluate(Vector2 dragStart, Vector2 dragCurrent, float screenHeight,
            AimSettings settings, bool facingRight = true)
        {
            var facing = facingRight ? 1f : -1f;
            var pull = dragStart - dragCurrent;
            pull.x *= facing;

            var length = pull.magnitude / Mathf.Max(1f, screenHeight);
            if (length < settings.cancelDrag)
                return AimState.Cancelled;

            var angle = Mathf.Clamp(Mathf.Atan2(pull.y, pull.x) * Mathf.Rad2Deg, settings.minAngle, settings.maxAngle);
            var range = Mathf.Max(0.0001f, settings.fullPowerDrag - settings.cancelDrag);
            var power = Mathf.Clamp01((length - settings.cancelDrag) / range);

            return new AimState(true, angle, power, facing);
        }
    }
}
