using Arash.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Arash.Tests
{
    public class AimSolverTests
    {
        static readonly Vector2 Gravity = new Vector2(0f, -9.81f);
        const float Dt = 0.02f;

        static float HeightAt(float angle, float speed, float distance)
        {
            var radians = angle * Mathf.Deg2Rad;
            var velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * speed;
            float height;
            Assert.IsTrue(AimSolver.HeightAtDistance(velocity, Gravity, Dt, distance, out height));
            return height;
        }

        [Test]
        public void SolvedShot_PassesThroughTarget()
        {
            var target = new Vector2(22f, 0.5f);
            float angle;
            Assert.IsTrue(AimSolver.TrySolveAngle(target, 20f, Gravity, Dt, -30f, 80f, out angle));

            Assert.AreEqual(target.y, HeightAt(angle, 20f, target.x), 0.05f);
        }

        [Test]
        public void SolvedShot_WorksForTargetsAboveAndBelow()
        {
            float up, down;
            Assert.IsTrue(AimSolver.TrySolveAngle(new Vector2(15f, 4f), 22f, Gravity, Dt, -30f, 80f, out up));
            Assert.IsTrue(AimSolver.TrySolveAngle(new Vector2(15f, -4f), 22f, Gravity, Dt, -30f, 80f, out down));

            Assert.AreEqual(4f, HeightAt(up, 22f, 15f), 0.05f);
            Assert.AreEqual(-4f, HeightAt(down, 22f, 15f), 0.05f);
            Assert.Greater(up, down);
        }

        [Test]
        public void SolverPicksTheLowArc()
        {
            float angle;
            Assert.IsTrue(AimSolver.TrySolveAngle(new Vector2(20f, 0f), 20f, Gravity, Dt, -30f, 80f, out angle));

            Assert.Less(angle, 45f);
        }

        [Test]
        public void TargetOutOfRange_HasNoSolution()
        {
            float angle;
            Assert.IsFalse(AimSolver.TrySolveAngle(new Vector2(200f, 0f), 15f, Gravity, Dt, -30f, 80f, out angle));
        }

        [Test]
        public void TargetBehind_HasNoSolution()
        {
            float angle;
            Assert.IsFalse(AimSolver.TrySolveAngle(new Vector2(-10f, 0f), 20f, Gravity, Dt, -30f, 80f, out angle));
        }
    }
}
