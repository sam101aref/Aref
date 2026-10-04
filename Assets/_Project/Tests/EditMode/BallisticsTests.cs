using Arash.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Arash.Tests
{
    public class BallisticsTests
    {
        static readonly Vector2 Gravity = new Vector2(0f, -9.81f);
        const float Dt = 0.02f;

        [Test]
        public void Step_ApproximatesProjectileMotion()
        {
            var position = Vector2.zero;
            var velocity = new Vector2(10f, 10f);
            for (var i = 0; i < 100; i++) // 2 seconds
                Ballistics.Step(ref position, ref velocity, Gravity, Dt);

            // Analytic: x = 20, y = 10*2 - 0.5*9.81*4 = 0.38
            Assert.AreEqual(20f, position.x, 0.001f);
            Assert.AreEqual(0.38f, position.y, 0.25f);
            Assert.AreEqual(10f - 9.81f * 2f, velocity.y, 0.001f);
        }

        [Test]
        public void SamplePath_MatchesSteppedFlight()
        {
            var origin = new Vector2(-6f, -1f);
            var launch = new Vector2(15f, 12f);
            var points = new Vector2[11];
            Ballistics.SamplePath(origin, launch, Gravity, Dt, 3, points, points.Length);

            // The arrow steps one fixed update at a time; every 3rd position must be a preview point.
            var position = origin;
            var velocity = launch;
            for (var i = 0; i < points.Length; i++)
            {
                Assert.AreEqual(points[i].x, position.x, 1e-5f, "x at point " + i);
                Assert.AreEqual(points[i].y, position.y, 1e-5f, "y at point " + i);
                for (var s = 0; s < 3; s++)
                    Ballistics.Step(ref position, ref velocity, Gravity, Dt);
            }
        }

        [Test]
        public void SamplePath_FirstPointIsOrigin()
        {
            var points = new Vector2[4];
            Ballistics.SamplePath(new Vector2(1f, 2f), Vector2.one, Gravity, Dt, 1, points, 4);

            Assert.AreEqual(new Vector2(1f, 2f), points[0]);
        }
    }
}
