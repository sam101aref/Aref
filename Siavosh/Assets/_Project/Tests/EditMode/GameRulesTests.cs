using System.Collections.Generic;
using NUnit.Framework;
using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.Play;
using Siavosh.Story;
using UnityEngine;

namespace Siavosh.Tests
{
    public class ProgressionTests
    {
        [Test]
        public void LevelsFollowTheExperienceTable()
        {
            Assert.AreEqual(1, Rules.LevelForXp(0));
            Assert.AreEqual(1, Rules.LevelForXp(99));
            Assert.AreEqual(2, Rules.LevelForXp(100));
            Assert.AreEqual(3, Rules.LevelForXp(250));
            Assert.AreEqual(Rules.MaxLevel, Rules.LevelForXp(1000000));
        }

        [Test]
        public void SkillPoints_OnePerLevel_StorySkillsAreFree()
        {
            Assert.AreEqual(0, Rules.SkillPoints(1, new List<string>()));
            Assert.AreEqual(2, Rules.SkillPoints(3, new List<string>()));
            Assert.AreEqual(1, Rules.SkillPoints(3, new List<string> { "p1" }));
            Assert.AreEqual(2, Rules.SkillPoints(3, new List<string> { "r1" }), "Taming is granted by the story");
        }

        [Test]
        public void Skills_NeedThePreviousTierAndAPoint()
        {
            var save = new SaveData { xp = 250 }; // level 3: two points
            Assert.IsFalse(Rules.TryLearn(save, "p2", 1), "tier 2 needs tier 1");
            Assert.IsTrue(Rules.TryLearn(save, "p1", 1));
            Assert.IsTrue(Rules.TryLearn(save, "p2", 1));
            Assert.IsFalse(Rules.TryLearn(save, "a1", 1), "no points left");
            Assert.IsFalse(Rules.TryLearn(save, "r1", 1), "story skill cannot be bought");
            Assert.IsFalse(Rules.TryLearn(save, "r4", 1), "later chapter");
        }

        [Test]
        public void Smith_SellsOnlyWhatIsAffordableAndUnlocked()
        {
            var save = new SaveData { dinars = 300 };
            Assert.IsFalse(Rules.TryBuy(save, "sword.siavoshi", 1), "chapter 6 item");
            Assert.IsTrue(Rules.TryBuy(save, "sword.kayani", 1));
            Assert.AreEqual(50, save.dinars);
            Assert.AreEqual("sword.kayani", save.sword, "a bought weapon is equipped");
            Assert.IsFalse(Rules.TryBuy(save, "bow.horn", 1), "not enough dinars");
            Rules.Equip(save, Rules.DefaultSword);
            Assert.AreEqual(Rules.DefaultSword, save.sword);
        }

        [Test]
        public void Stats_FollowSkillsGearAndDifficulty()
        {
            var save = new SaveData();
            var basic = HeroStats.From(save, Difficulty.Normal);
            Assert.AreEqual(2, basic.ComboLength);
            Assert.IsFalse(basic.CanParry);
            Assert.AreEqual(3, basic.HorseHearts);

            save.skills.AddRange(new[] { "p1", "p2", "p4", "a1", "r1", "r2" });
            save.owned.Add("armor.lamellar");
            save.armor = "armor.lamellar";
            var grown = HeroStats.From(save, Difficulty.Easy);
            Assert.AreEqual(3, grown.ComboLength);
            Assert.IsTrue(grown.CanParry);
            Assert.AreEqual(basic.MaxHealth + 60, grown.MaxHealth);
            Assert.Less(grown.DrawTime, basic.DrawTime);
            Assert.AreEqual(4, grown.HorseHearts);
            Assert.Less(grown.DamageTaken, basic.DamageTaken);
        }

        [Test]
        public void Save_RoundTripsThroughJson()
        {
            var save = new SaveData { xp = 321, dinars = 77, honor = 2 };
            save.skills.Add("p1");
            save.Stage("1-1").done = true;
            save.Stage("1-1").leaves = 5;
            save.MarkSeen("prologue");
            save.Set("kind.1-1.lamb");
            var copy = SaveSystem.FromJson(SaveSystem.ToJson(save));
            Assert.AreEqual(321, copy.xp);
            Assert.AreEqual(77, copy.dinars);
            Assert.IsTrue(copy.IsDone("1-1"));
            Assert.AreEqual(2, copy.LeavesFound("1-1"));
            Assert.IsTrue(copy.HasSeen("prologue"));
            Assert.IsTrue(copy.Has("kind.1-1.lamb"));
            Assert.AreEqual(-1, SaveSystem.FromJson("{}").settings.language, "a new save has no language yet");
        }
    }

    public class StoryAndStageTests
    {
        static IEnumerable<Cutscene> AllCutscenes()
        {
            foreach (var c in Script.Book)
                yield return c;
            foreach (var stage in Catalog.AllStages())
            {
                if (stage.Intro != null) yield return stage.Intro;
                if (stage.Outro != null) yield return stage.Outro;
            }
        }

        [Test]
        public void EveryLineIsWrittenInBothLanguages()
        {
            foreach (var cutscene in AllCutscenes())
            {
                Assert.IsFalse(string.IsNullOrEmpty(cutscene.Title.Fa) || string.IsNullOrEmpty(cutscene.Title.En), cutscene.Id);
                foreach (var beat in cutscene.Beats)
                {
                    if (beat.Kind == BeatKind.Line)
                    {
                        Assert.IsNotNull(beat.Speaker, cutscene.Id);
                        Assert.IsTrue(PersianShaper.ContainsRtl(beat.Text.Fa), cutscene.Id + ": " + beat.Text.En);
                        Assert.IsFalse(PersianShaper.ContainsRtl(beat.Text.En), cutscene.Id + ": " + beat.Text.Fa);
                    }
                    if (beat.Kind == BeatKind.Choice)
                    {
                        Assert.AreEqual(1, beat.Options.FindAll(o => o.Faithful).Count, cutscene.Id + ": exactly one faithful answer");
                        foreach (var o in beat.Options)
                            if (!o.Faithful)
                                Assert.IsNotNull(o.Rewind, cutscene.Id + ": the storyteller must answer an unfaithful choice");
                    }
                }
            }
        }

        [Test]
        public void ThePrologueBeginsInTheCoffeehouseUnderTheCloth()
        {
            var first = Script.Prologue.Beats[0];
            Assert.AreEqual(BeatKind.Scene, first.Kind);
            Assert.AreEqual(StageMode.Coffeehouse, first.Mode);
            Assert.IsTrue(first.Covered);
            Assert.IsTrue(Script.Prologue.Beats.Exists(b => b.Kind == BeatKind.Reveal));
        }

        [Test]
        public void ChapterOneHasFiveStagesAndRostamsTrial()
        {
            var stages = Catalog.Chapters[1].Stages;
            Assert.AreEqual(6, stages.Count);
            Assert.IsTrue(stages[5].Boss);
            Assert.AreEqual(Rules.FarrRostam, stages[5].GrantsFarr);
            Assert.AreEqual("r1", stages[3].GrantsSkill);
            Assert.AreSame(stages[0], Catalog.FirstStage);
        }

        [Test]
        public void StagesUnlockInOrder()
        {
            var save = new SaveData();
            var stages = Catalog.Chapters[1].Stages;
            Assert.IsTrue(Catalog.IsUnlocked(stages[0], save));
            Assert.IsFalse(Catalog.IsUnlocked(stages[1], save));
            save.Stage(stages[0].Id).done = true;
            Assert.IsTrue(Catalog.IsUnlocked(stages[1], save));
            Assert.AreSame(stages[1], Catalog.NextStage(save));
        }

        [Test]
        public void EveryStageLayoutIsSound()
        {
            foreach (var stage in Catalog.AllStages())
            {
                var layout = stage.Layout();
                Assert.Greater(layout.Length, 10f, stage.Id);
                var leaves = layout.Items.FindAll(p => p.What == Thing.Leaf);
                Assert.AreEqual(3, leaves.Count, stage.Id + " has three leaves");
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, leaves.ConvertAll(p => p.Index), stage.Id);
                Assert.IsTrue(layout.Items.Exists(p => p.What == Thing.Ground && p.X <= 0f), stage.Id + " has ground under the start");
                if (stage.Kind == StageKind.Walk && !stage.Boss)
                    Assert.IsTrue(layout.Items.Exists(p => p.What == Thing.Gate), stage.Id + " has a gate");
                if (stage.Goal.Fa.Contains("{0}"))
                    Assert.IsTrue(stage.Goal.En.Contains("{0}"), stage.Id);
                if (stage.GoalKind == GoalKind.ClearGoals)
                    Assert.IsTrue(layout.Items.Exists(p => p.Goal), stage.Id + " has something to clear");
                if (stage.GoalKind == GoalKind.Hunt)
                    Assert.GreaterOrEqual(layout.Items.FindAll(p => p.What == Thing.Onager && p.Goal).Count, stage.GoalCount + 2, stage.Id + " leaves room for misses");
            }
        }

        [Test]
        public void LocText_FollowsTheLanguage()
        {
            var text = new LocText("سلام", "Hello");
            Loc.Apply(Language.English);
            Assert.AreEqual("Hello", text.Raw);
            Assert.AreEqual("Hello", text.ToString());
            Loc.Apply(Language.Persian);
            Assert.AreEqual("سلام", text.Raw);
            Assert.AreEqual(PersianShaper.Shape("سلام"), text.ToString());
            Assert.AreEqual("۳", new LocText("{0}", "{0}").Format(3));
            Loc.Apply(Language.English);
        }
    }

    public class PhysicsTests
    {
        static List<Solid> Ground(float from, float to)
        {
            return new List<Solid> { new Solid { Left = from, Right = to, Top = 0f, Bottom = -30f } };
        }

        static void Run(Body body, List<Solid> solids, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f)
                body.Step(1f / 60f, solids);
        }

        [Test]
        public void ABodyLandsOnTheGround()
        {
            var body = new Body { Position = new Vector2(0f, 3f) };
            Run(body, Ground(-5f, 5f), 1f);
            Assert.IsTrue(body.Grounded);
            Assert.AreEqual(0f, body.Position.y, 0.001f);
        }

        [Test]
        public void ABodyFallsIntoAGap()
        {
            var solids = Ground(-5f, 0f);
            solids.Add(new Solid { Left = 3f, Right = 8f, Top = 0f, Bottom = -30f });
            var body = new Body { Position = new Vector2(1.5f, 0.5f) };
            Run(body, solids, 1f);
            Assert.Less(body.Position.y, -2f);
            Assert.Greater(body.Position.x, 0.2f);
            Assert.Less(body.Position.x, 2.8f, "the sides of the pit hold the body in");
        }

        [Test]
        public void LedgesAreOneWay()
        {
            var solids = Ground(-5f, 5f);
            solids.Add(new Solid { Left = -1f, Right = 1f, Top = 2f, Bottom = 1.4f, OneWay = true });
            var body = new Body { Position = new Vector2(0f, 0f), Velocity = new Vector2(0f, 14f) };
            Run(body, solids, 0.6f);
            Assert.Greater(body.Position.y, 1.9f, "jumped up through the ledge");
            Run(body, solids, 1f);
            Assert.AreEqual(2f, body.Position.y, 0.001f, "and landed on top of it");
        }

        [Test]
        public void TheHeroCanClearTheStagesGaps()
        {
            // Jump speed 14.2, gravity 38, run speed 6.5: about 4.9 units of air time at full run.
            const float jump = 14.2f, gravity = 38f, run = 6.5f;
            var airTime = 2f * jump / gravity;
            var reach = airTime * run;
            foreach (var stage in Catalog.AllStages())
                foreach (var p in stage.Layout().Items)
                    if (p.What == Thing.Water)
                        Assert.Less(p.W, reach - 0.5f, stage.Id + " gap at " + p.X);
        }

        [Test]
        public void ArrowAimHitsItsTarget()
        {
            var from = new Vector2(0f, 1.7f);
            var target = new Vector2(9f, 1.2f);
            var v = Ballistics.Aim(from, target, 13f);
            Assert.IsTrue(v.HasValue);
            var t = 9f / v.Value.x;
            var hit = Ballistics.PointAt(from, v.Value, t);
            Assert.AreEqual(target.y, hit.y, 0.05f);
            Assert.IsFalse(Ballistics.Aim(from, new Vector2(60f, 0f), 5f).HasValue, "out of range");
        }
    }
}
