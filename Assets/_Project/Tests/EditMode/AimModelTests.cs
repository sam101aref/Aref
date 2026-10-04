using Arash.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Arash.Tests
{
    public class AimModelTests
    {
        const float ScreenHeight = 1000f;
        AimSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = new AimSettings { fullPowerDrag = 0.3f, cancelDrag = 0.04f, minAngle = -30f, maxAngle = 85f };
        }

        [Test]
        public void PullingBackAndDown_ShootsForwardAndUp()
        {
            var aim = AimModel.Evaluate(new Vector2(500, 500), new Vector2(400, 400), ScreenHeight, settings);

            Assert.IsTrue(aim.IsValid);
            Assert.AreEqual(45f, aim.Angle, 0.01f);
            Assert.Greater(aim.Direction.x, 0f);
            Assert.Greater(aim.Direction.y, 0f);
        }

        [Test]
        public void ShortDrag_IsCancelled()
        {
            var aim = AimModel.Evaluate(new Vector2(500, 500), new Vector2(490, 495), ScreenHeight, settings);

            Assert.IsFalse(aim.IsValid);
        }

        [Test]
        public void Power_GrowsWithDragAndCapsAtOne()
        {
            var start = new Vector2(500, 500);
            var half = AimModel.Evaluate(start, start - new Vector2(170, 0), ScreenHeight, settings);
            var full = AimModel.Evaluate(start, start - new Vector2(300, 0), ScreenHeight, settings);
            var beyond = AimModel.Evaluate(start, start - new Vector2(900, 0), ScreenHeight, settings);

            Assert.AreEqual(0.5f, half.Power, 0.01f);
            Assert.AreEqual(1f, full.Power, 0.001f);
            Assert.AreEqual(1f, beyond.Power, 0.001f);
        }

        [Test]
        public void Power_IsIndependentOfScreenResolution()
        {
            var small = AimModel.Evaluate(Vector2.zero, new Vector2(-72, 0), 720f, settings);
            var large = AimModel.Evaluate(Vector2.zero, new Vector2(-144, 0), 1440f, settings);

            Assert.AreEqual(small.Power, large.Power, 0.0001f);
        }

        [Test]
        public void Angle_IsClampedToSettings()
        {
            var tooHigh = AimModel.Evaluate(new Vector2(500, 500), new Vector2(510, 200), ScreenHeight, settings);
            var tooLow = AimModel.Evaluate(new Vector2(500, 500), new Vector2(300, 700), ScreenHeight, settings);

            Assert.AreEqual(85f, tooHigh.Angle, 0.001f);
            Assert.AreEqual(-30f, tooLow.Angle, 0.001f);
        }

        [Test]
        public void FacingLeft_MirrorsTheShot()
        {
            var aim = AimModel.Evaluate(new Vector2(500, 500), new Vector2(600, 400), ScreenHeight, settings, facingRight: false);

            Assert.IsTrue(aim.IsValid);
            Assert.AreEqual(45f, aim.Angle, 0.01f);
            Assert.Less(aim.Direction.x, 0f);
            Assert.Greater(aim.Direction.y, 0f);
        }
    }
}
