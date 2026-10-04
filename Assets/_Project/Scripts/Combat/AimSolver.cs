using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Finds the shot angle that lands on a target, by simulating the same stepped flight the arrow
    /// uses (so there is no drift between the solution and the real arrow). Used by the enemy AI.
    /// </summary>
    public static class AimSolver
    {
        const float ScanStep = 2f;
        const int BisectIterations = 14;
        const float MaxFlightTime = 10f;

        /// <summary>
        /// Height of the path, relative to the launch point, where it crosses <paramref name="distance"/>
        /// (measured along the shot direction). False if the arrow never gets that far.
        /// </summary>
        public static bool HeightAtDistance(Vector2 launchVelocity, Vector2 acceleration, float deltaTime,
            float distance, out float height)
        {
            var position = Vector2.zero;
            var velocity = launchVelocity;
            for (var time = 0f; time < MaxFlightTime; time += deltaTime)
            {
                var previous = position;
                Ballistics.Step(ref position, ref velocity, acceleration, deltaTime);
                if (position.x >= distance)
                {
                    var t = (distance - previous.x) / Mathf.Max(0.0001f, position.x - previous.x);
                    height = Mathf.Lerp(previous.y, position.y, t);
                    return true;
                }
                if (velocity.x <= 0f)
                    break;
            }
            height = 0f;
            return false;
        }

        /// <summary>
        /// Angle (degrees above the facing direction) whose path passes through <paramref name="offset"/>
        /// at the given speed: the lowest such angle, or the highest when <paramref name="highArc"/> is set
        /// (lobbed shots, e.g. slingers). <paramref name="offset"/> and <paramref name="acceleration"/> are
        /// in facing space: +X points the way the archer faces.
        /// </summary>
        public static bool TrySolveAngle(Vector2 offset, float speed, Vector2 acceleration, float deltaTime,
            float minAngle, float maxAngle, out float angle, bool highArc = false)
        {
            angle = 0f;
            if (offset.x <= 0f)
                return false;

            // Walk from the flat end (low arc) or the steep end (high arc) towards the other, and stop
            // at the first angle where the path goes from passing below the target to above it.
            var step = highArc ? -ScanStep : ScanStep;
            var previousAngle = highArc ? maxAngle : minAngle;
            var previousAbove = IsAbove(previousAngle, offset, speed, acceleration, deltaTime);
            if (previousAbove)
                return false; // the first angle already overshoots: no arc of this kind

            for (var a = previousAngle + step; highArc ? a >= minAngle - 0.001f : a <= maxAngle + 0.001f; a += step)
            {
                var above = IsAbove(a, offset, speed, acceleration, deltaTime);
                if (above)
                {
                    angle = Bisect(previousAngle, a, offset, speed, acceleration, deltaTime);
                    return true;
                }
                previousAngle = a;
            }
            return false;
        }

        /// <summary>Bisects between an angle passing below the target and one passing above it.</summary>
        static float Bisect(float below, float above, Vector2 offset, float speed, Vector2 acceleration, float deltaTime)
        {
            for (var i = 0; i < BisectIterations; i++)
            {
                var mid = (below + above) * 0.5f;
                if (IsAbove(mid, offset, speed, acceleration, deltaTime))
                    above = mid;
                else
                    below = mid;
            }
            return (below + above) * 0.5f;
        }

        static bool IsAbove(float angle, Vector2 offset, float speed, Vector2 acceleration, float deltaTime)
        {
            float error;
            return Error(angle, offset, speed, acceleration, deltaTime, out error) && error >= 0f;
        }

        /// <summary>Path height minus target height at the target's distance (positive = above).</summary>
        static bool Error(float angle, Vector2 offset, float speed, Vector2 acceleration, float deltaTime, out float error)
        {
            var radians = angle * Mathf.Deg2Rad;
            var velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * speed;
            float height;
            var reached = HeightAtDistance(velocity, acceleration, deltaTime, offset.x, out height);
            error = reached ? height - offset.y : float.NegativeInfinity;
            return reached;
        }
    }
}
