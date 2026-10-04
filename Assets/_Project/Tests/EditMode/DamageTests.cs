using Arash.Combat;
using NUnit.Framework;

namespace Arash.Tests
{
    public class DamageTests
    {
        [Test]
        public void StandardArrow_HeadshotIsLethal()
        {
            Assert.AreEqual(100f, DamageRules.Compute(DamageRules.StandardArrowDamage, HitZoneType.Head), 0.001f);
        }

        [Test]
        public void ZoneMultipliers_MatchDesign()
        {
            Assert.AreEqual(40f, DamageRules.Compute(100f, HitZoneType.Torso), 0.001f);
            Assert.AreEqual(25f, DamageRules.Compute(100f, HitZoneType.Limb), 0.001f);
            Assert.AreEqual(0f, DamageRules.Compute(100f, HitZoneType.Armor), 0.001f);
        }

        [Test]
        public void NegativeDamage_IsIgnored()
        {
            Assert.AreEqual(0f, DamageRules.Compute(-50f, HitZoneType.Head), 0.001f);
        }

        [Test]
        public void AiError_ShrinksAfterMissesDownToMinimum()
        {
            var accuracy = new AiAccuracy { initialAngleError = 8f, minAngleError = 1f, decayPerMiss = 0.5f };

            var error = accuracy.initialAngleError;
            error = accuracy.NextError(error);
            Assert.AreEqual(4f, error, 0.001f);
            for (var i = 0; i < 10; i++)
                error = accuracy.NextError(error);
            Assert.AreEqual(1f, error, 0.001f);
        }

        [Test]
        public void AiError_IsAppliedAndClamped()
        {
            var accuracy = new AiAccuracy { powerErrorPerDegree = 0.02f };
            var perfect = new AimState(true, 30f, 0.5f, -1f);

            var shot = accuracy.ApplyError(perfect, 5f, 1f, -1f, -30f, 80f);
            Assert.AreEqual(35f, shot.Angle, 0.001f);
            Assert.AreEqual(0.4f, shot.Power, 0.001f);
            Assert.AreEqual(-1f, shot.Facing, 0.001f);

            var clamped = accuracy.ApplyError(new AimState(true, 78f, 1f, 1f), 5f, 1f, 1f, -30f, 80f);
            Assert.AreEqual(80f, clamped.Angle, 0.001f);
            Assert.AreEqual(1f, clamped.Power, 0.001f);
        }
    }
}
