using UnityEngine;

namespace Siavosh.Play
{
    /// <summary>Arrow flight: where an arrow goes, and how to aim one at a point.</summary>
    public static class Ballistics
    {
        public const float Gravity = 16f;

        /// <summary>Launch velocity to hit <paramref name="target"/> at <paramref name="speed"/> (low arc), or null.</summary>
        public static Vector2? Aim(Vector2 from, Vector2 target, float speed)
        {
            var d = target - from;
            var x = Mathf.Abs(d.x);
            var v2 = speed * speed;
            var disc = v2 * v2 - Gravity * (Gravity * x * x + 2f * d.y * v2);
            if (x < 0.01f || disc < 0f)
                return null;
            var angle = Mathf.Atan((v2 - Mathf.Sqrt(disc)) / (Gravity * x));
            return new Vector2(Mathf.Cos(angle) * speed * Mathf.Sign(d.x), Mathf.Sin(angle) * speed);
        }

        public static Vector2 PointAt(Vector2 from, Vector2 velocity, float t)
        {
            return from + velocity * t + 0.5f * new Vector2(0f, -Gravity) * t * t;
        }
    }
}
