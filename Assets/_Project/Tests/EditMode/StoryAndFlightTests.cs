using System.Collections.Generic;
using Arash.Combat;
using Arash.Core;
using Arash.Flight;
using Arash.Story;
using NUnit.Framework;
using UnityEngine;

namespace Arash.Tests
{
    public class StoryAndFlightTests
    {
        [Test]
        public void Wrap_BreaksAtWordsWithinWidth()
        {
            // One unit of width per character.
            var lines = TextLayout.Wrap("the arrow flew from dawn until noon", s => s.Length, 15f);

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual("the arrow flew", lines[0]);
            Assert.AreEqual("from dawn until", lines[1]);
            Assert.AreEqual("noon", lines[2]);
        }

        [Test]
        public void Wrap_KeepsExplicitLineBreaksAndLongWords()
        {
            var lines = TextLayout.Wrap("short\nunbreakableword", s => s.Length, 5f);

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("short", lines[0]);
            Assert.AreEqual("unbreakableword", lines[1]);
        }

        [Test]
        public void Flight_HoldingClimbsAndReleasingSinks()
        {
            var state = new FlightState { Y = 0f };
            for (var i = 0; i < 30; i++)
                FlightRules.Step(state, true, 0.02f, 14f, 900f);
            var climbed = state.Y;
            // Letting go first slows the climb, then the arrow sinks.
            for (var i = 0; i < 150; i++)
                FlightRules.Step(state, false, 0.02f, 14f, 900f);

            Assert.Greater(climbed, 0f);
            Assert.Less(state.Y, climbed);
            Assert.AreEqual(180 * 0.02f * 14f, state.X, 0.01f);
        }

        [Test]
        public void Flight_ReachingTheEndFinishes_RunningDryFails()
        {
            var state = new FlightState();
            FlightRules.Step(state, false, 1f, 1000f, 900f);
            Assert.IsTrue(state.Finished);

            var tired = new FlightState { Energy = 0.5f };
            FlightRules.Step(tired, false, 1f, 1f, 900f);
            Assert.IsTrue(tired.Failed);
        }

        [Test]
        public void Flight_ObstaclesCostStrengthOnceThenGiveGrace()
        {
            var state = new FlightState();
            var cloud = new FlightObstacle { Kind = FlightObstacleKind.Cloud, X = 0f, Y = 0f, Size = 1f };

            Assert.IsFalse(FlightRules.Touch(state, cloud));
            Assert.IsFalse(FlightRules.Touch(state, cloud)); // still invulnerable
            Assert.AreEqual(FlightRules.MaxEnergy - FlightRules.HitCost, state.Energy, 0.001f);
            Assert.AreEqual(1, state.Hits);
        }

        [Test]
        public void Flight_FarrRestoresStrengthUpToTheMaximum()
        {
            var state = new FlightState { Energy = 50f };
            var farr = new FlightObstacle { Kind = FlightObstacleKind.Farr };

            Assert.IsTrue(FlightRules.Touch(state, farr));
            Assert.AreEqual(50f + FlightRules.FarrGain, state.Energy, 0.001f);
            state.Energy = FlightRules.MaxEnergy;
            FlightRules.Touch(state, farr);
            Assert.AreEqual(FlightRules.MaxEnergy, state.Energy, 0.001f);
        }

        [Test]
        public void Flight_PeakCollisionFollowsItsSlopes()
        {
            var peak = new FlightObstacle { Kind = FlightObstacleKind.Peak, X = 10f, Size = 4f };

            Assert.IsTrue(FlightRules.Overlaps(peak, 10f, FlightRules.MinY + 3f));
            Assert.IsFalse(FlightRules.Overlaps(peak, 10f, FlightRules.MinY + 5f));
            Assert.IsFalse(FlightRules.Overlaps(peak, 16f, FlightRules.MinY + 1f));
        }

        [Test]
        public void FlightCourse_IsDeterministicAndInBounds()
        {
            var a = FlightCourse.Generate(42, 900f, 0.5f);
            var b = FlightCourse.Generate(42, 900f, 0.5f);

            Assert.Greater(a.Count, 10f);
            Assert.AreEqual(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].X, b[i].X, 0.0001f);
                Assert.IsTrue(a[i].X < 900f);
                Assert.IsTrue(a[i].Y >= FlightRules.MinY && a[i].Y <= FlightRules.MaxY);
            }
        }

        [Test]
        public void HighArc_IsSteeperThanLowArcAndBothHit()
        {
            var gravity = new Vector2(0f, -9.81f);
            var target = new Vector2(18f, 0f);
            float low, high;
            Assert.IsTrue(AimSolver.TrySolveAngle(target, 20f, gravity, 0.02f, -30f, 80f, out low));
            Assert.IsTrue(AimSolver.TrySolveAngle(target, 20f, gravity, 0.02f, -30f, 80f, out high, true));

            Assert.Greater(high, 45f);
            Assert.Less(low, 45f);
        }

        [Test]
        public void Wind_RollStaysWithinVariance()
        {
            Assert.AreEqual(3f, BattleEnvironment.RollWind(2f, 1f, 1f), 0.0001f);
            Assert.AreEqual(1f, BattleEnvironment.RollWind(2f, 1f, -5f), 0.0001f);
        }

        [Test]
        public void BossPhase_DeepestReachedPhaseWins()
        {
            var tactics = new EnemyTactics
            {
                phases = new List<BossPhase>
                {
                    new BossPhase { healthBelow = 0.66f, shotsPerTurn = 2 },
                    new BossPhase { healthBelow = 0.33f, shotsPerTurn = 3 },
                },
            };

            Assert.IsNull(tactics.PhaseFor(0.9f));
            Assert.AreEqual(2, tactics.PhaseFor(0.5f).shotsPerTurn);
            Assert.AreEqual(3, tactics.PhaseFor(0.2f).shotsPerTurn);
        }

        [Test]
        public void SeenCutscenes_AreRecordedOnce()
        {
            var save = new SaveData();
            save.MarkSeen("prologue");
            save.MarkSeen("prologue");

            Assert.IsTrue(save.HasSeen("prologue"));
            Assert.IsFalse(save.HasSeen("ending"));
            Assert.AreEqual(1, save.seenCutscenes.Count);
        }
    }
}
