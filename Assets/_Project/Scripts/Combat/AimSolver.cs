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
        /// Lowest angle (degrees above the facing direction) that passes through <paramref name="offset"/>
        /// at the given speed. <paramref name="offset"/> and <paramref name="acceleration"/> are in facing
        /// space: +X points the way the archer faces.
        /// </summary>
        public static bool TrySolveAngle(Vector2 offset, float speed, Vector2 acceleration, float deltaTime,
            float minAngle, float maxAngle, out float angle)
        {
            angle = 0f;
            if (offset.x <= 0f)
                return false;

            float previousAngle = minAngle;
            float previousError;
            var previousValid = Error(minAngle, offset, speed, acceleration, deltaTime, out previousError);
            if (previousValid && previousError >= 0f)
                return false; // even the flattest allowed shot goes over the target

            for (var a = minAngle + ScanStep; a <= maxAngle + 0.001f; a += ScanStep)
            {
                float error;
                var valid = Error(a, offset, speed, acceleration, deltaTime, out error);
                if (previousValid && valid && previousError < 0f && error >= 0f)
                {
                    angle = Bisect(previousAngle, a, offset, speed, acceleration, deltaTime);
                    return true;
                }
                previousAngle = a;
                previousError = error;
                previousValid = valid;
            }
            return false;
        }

        static float Bisect(float low, float high, Vector2 offset, float speed, Vector2 acceleration, float deltaTime)
        {
            for (var i = 0; i < BisectIterations; i++)
            {
                var mid = (low + high) * 0.5f;
                float error;
                if (Error(mid, offset, speed, acceleration, deltaTime, out error) && error >= 0f)
                    high = mid;
                else
                    low = mid;
            }
            return (low + high) * 0.5f;
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
