using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Arrow flight integration. The flying arrow and the aim preview both step through this,
    /// with the same fixed time step, so the preview matches the real flight exactly.
    /// </summary>
    public static class Ballistics
    {
        /// <summary>Advances one step with semi-implicit Euler integration.</summary>
        public static void Step(ref Vector2 position, ref Vector2 velocity, Vector2 acceleration, float deltaTime)
        {
            velocity += acceleration * deltaTime;
            position += velocity * deltaTime;
        }

        /// <summary>
        /// Writes <paramref name="count"/> points into <paramref name="points"/>, starting at
        /// <paramref name="origin"/> and taking <paramref name="stepsPerPoint"/> steps between points.
        /// </summary>
        public static void SamplePath(Vector2 origin, Vector2 velocity, Vector2 acceleration,
            float deltaTime, int stepsPerPoint, Vector2[] points, int count)
        {
            var position = origin;
            for (var i = 0; i < count; i++)
            {
                points[i] = position;
                for (var s = 0; s < stepsPerPoint; s++)
                    Step(ref position, ref velocity, acceleration, deltaTime);
            }
        }
    }
}
