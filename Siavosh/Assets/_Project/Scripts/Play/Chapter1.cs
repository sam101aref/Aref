using System.Collections.Generic;
using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.Story;

namespace Siavosh.Play
{
    /// <summary>
    /// Chapter one, "Rostam's Pupil" (Zabulistan): five stages and Rostam's trial. Each stage teaches
    /// one thing — moving, the sword, the bow, riding, hunting from the saddle — and the trial asks
    /// for all of it. Distances are in world units; the hero runs 6.5 units a second and jumps about
    /// 2.6 units high and 4.5 units far.
    /// </summary>
    public static class Chapter1
    {
        public static IEnumerable<StageDef> Stages()
        {
            yield return FirstSteps();
            yield return WoodenSword();
            yield return ReedsOfHelmand();
            yield return BlackColt();
            yield return OnagerHunt();
            yield return RostamsTrial();
        }

        static StageDef FirstSteps()
        {
            return new StageDef
            {
                Id = "1-1", Chapter = 1, Number = 1, Kind = StageKind.Walk,
                Name = new LocText("نخستین گام", "First Steps"),
                Backdrop = "zabul_far", GroundTile = "ground_meadow",
                Goal = new LocText("از باغ رستم بگذر و به دروازه برس", "Cross Rostam's garden and reach the gate"),
                GoalKind = GoalKind.ReachGate,
                XpReward = 100, DinarReward = 40, BowAllowed = false,
                Intro = Script.Stage1Intro,
                Build = s =>
                {
                    s.Length = 82f;
                    s.Ground(-6f, 18f).Water(18f, 21.5f).Ground(21.5f, 43f).Water(43f, 46.5f).Ground(46.5f, 90f);
                    s.Prop("cypress", -2f).Prop("tree_blossom", 7f).Prop("reeds", 17f).Prop("reeds", 22f)
                     .Prop("rock_lilac", 31f).Prop("cypress", 37f).Prop("tree_rose", 50f).Prop("rock_turq", 63f)
                     .Prop("cypress", 69f).Prop("fence", 72f).Front("reeds", 45f);
                    s.Npc("rostam", 4f, faceLeft: false);
                    s.Tip(1f, "با جوی‌استیک سمت چپ راه برو.", "Walk with the stick on the left.");
                    s.Tip(13.5f, "دکمهٔ پرش را بزن و از جوی بپر.", "Press jump to leap over the stream.");
                    s.Coins(8f, 1.0f, 4).Leaf(0, 12f, 2.9f);
                    s.Ledge(26f, 1.7f, 2.6f).Ledge(29.5f, 3.3f, 2.6f).Leaf(1, 30.8f, 4.9f).Coins(26.3f, 2.4f, 3, 0.8f);
                    s.Tip(24.5f, "از صخره‌ها بالا برو. برگ‌های شاهنامه را جمع کن.", "Climb the rocks. Gather the leaves of the Shahnameh.");
                    s.Kindness(Thing.Lamb, 38.5f, "kind.1-1.lamb");
                    s.Coins(43.4f, 2.4f, 4, 0.8f, 1.2f);
                    s.Ledge(53f, 1.8f, 2.4f).Ledge(56.5f, 3.4f, 2.4f).Ledge(60f, 5.0f, 2.6f).Leaf(2, 61.2f, 6.6f);
                    s.Heal(66f, 0.6f).Coins(68f, 0.9f, 3);
                    s.Gate(78f).Npc("rostam", 81.5f);
                },
            };
        }

        static StageDef WoodenSword()
        {
            return new StageDef
            {
                Id = "1-2", Chapter = 1, Number = 2, Kind = StageKind.Walk,
                Name = new LocText("شمشیر چوبی", "The Wooden Sword"),
                Backdrop = "zabul_far", GroundTile = "ground_earth",
                Goal = new LocText("نوآموزان را شکست بده · {0} از {1}", "Defeat the trainees · {0} of {1}"),
                GoalKind = GoalKind.ClearGoals,
                XpReward = 120, DinarReward = 50, BowAllowed = false,
                Intro = Script.Stage2Intro,
                Build = s =>
                {
                    s.Length = 72f;
                    s.Ground(-6f, 80f);
                    s.Prop("banner", 2f).Prop("fence", 6f).Prop("fence", 16f).Prop("banner", 30f).Prop("tree_blossom", 38f)
                     .Prop("rock_rose", 52f).Prop("banner", 60f).Prop("cypress", 66f);
                    s.Npc("rostam", 3.5f, faceLeft: false);
                    s.Tip(5f, "دکمهٔ شمشیر را بزن. پشت سر هم بزن تا ضربه‌ها زنجیره شوند.", "Press the sword button. Press again quickly to chain blows.");
                    s.Enemy(Thing.Dummy, 9f, goal: false).Enemy(Thing.Dummy, 12f, goal: false).Enemy(Thing.Dummy, 15f, goal: false);
                    s.Ledge(14f, 2.4f, 2.4f).Leaf(0, 15.2f, 4.0f);
                    s.Tip(19f, "دکمهٔ سپر را نگه دار تا ضربه را بگیری. با دکمهٔ جاخالی بغلت.", "Hold the shield button to catch a blow. Roll away with the dodge button.");
                    s.Enemy(Thing.Trainee, 26f).Kindness(Thing.Trainee, 26f, "kind.1-2.help");
                    s.Coins(30f, 0.9f, 4).Heal(34f, 0.6f);
                    s.Enemy(Thing.Trainee, 42f).Enemy(Thing.Trainee, 46f);
                    s.Ledge(50f, 1.8f, 2.4f).Ledge(53.5f, 3.4f, 2.4f).Leaf(1, 54.7f, 5.0f);
                    s.Coins(57f, 1.0f, 3).Leaf(2, 63f, 0.9f);
                    s.Gate(68f).Npc("rostam", 71.5f);
                },
            };
        }

        static StageDef ReedsOfHelmand()
        {
            return new StageDef
            {
                Id = "1-3", Chapter = 1, Number = 3, Kind = StageKind.Walk,
                Name = new LocText("نیزارهای هیرمند", "Reeds of the Helmand"),
                Backdrop = "zabul_far", GroundTile = "ground_meadow",
                Goal = new LocText("نشانه‌ها را بزن و پیشروان را برانید · {0} از {1}", "Hit the targets and drive off the scouts · {0} of {1}"),
                GoalKind = GoalKind.ClearGoals,
                XpReward = 150, DinarReward = 60,
                Intro = Script.Stage3Intro,
                Build = s =>
                {
                    s.Length = 92f;
                    s.Ground(-6f, 33f).Water(33f, 37f).Ground(37f, 100f);
                    s.Prop("reeds", 3f).Prop("reeds", 9f).Prop("tree_blossom", 16f).Prop("reeds", 31f).Prop("reeds", 38f)
                     .Prop("rock_lilac", 47f).Prop("cypress", 58f).Prop("reeds", 66f).Prop("rock_turq", 80f).Prop("reeds", 88f)
                     .Front("reeds", 34.5f).Front("reeds", 71f);
                    s.Npc("rostam", 2f, faceLeft: false);
                    s.Tip(4f, "دکمهٔ کمان را نگه دار تا کمان کشیده شود؛ با جوی‌استیک بالا و پایین نشانه بگیر و رها کن.", "Hold the bow button to draw; aim up or down with the stick, then let go.");
                    s.Enemy(Thing.Target, 13f);
                    s.Ledge(17f, 2.2f, 2.6f).Enemy(Thing.Target, 18.3f, 2.2f);
                    s.Ledge(21f, 3.8f, 2.4f).Leaf(0, 22.2f, 5.4f);
                    s.Enemy(Thing.SwingTarget, 27f, 5.5f);
                    s.Enemy(Thing.Target, 41f);
                    s.Enemy(Thing.SwingTarget, 45f, 5.8f);
                    s.Tip(30f, "نشانه‌ای که تاب می‌خورد را پیش از رسیدن به آن بزن.", "Shoot the swinging target before you reach it.");
                    s.Kindness(Thing.HurtBird, 50f, "kind.1-3.hoopoe");
                    s.Coins(52f, 1.0f, 4).Ledge(55f, 2.2f, 2.6f).Leaf(1, 56.3f, 3.8f);
                    s.Tip(60f, "پیشروان تورانی! با شمشیر و کمان آن‌ها را برانید.", "Turanian scouts! Drive them off with sword and bow.");
                    s.Enemy(Thing.Spearman, 66f);
                    s.Ledge(71f, 2.2f, 3.2f).Enemy(Thing.Archer, 72.6f, 2.2f);
                    s.Enemy(Thing.Spearman, 77f);
                    s.Heal(74f, 0.6f).Leaf(2, 84f, 2.8f).Coins(80f, 1.0f, 3);
                    s.Gate(88f).Npc("rostam", 91.5f);
                },
            };
        }

        static StageDef BlackColt()
        {
            return new StageDef
            {
                Id = "1-4", Chapter = 1, Number = 4, Kind = StageKind.Ride,
                Name = new LocText("کرّهٔ سیاه", "The Black Colt"),
                Backdrop = "zabul_far", GroundTile = "ground_meadow",
                Goal = new LocText("بر کرّهٔ سیاه بمان تا رام شود", "Stay on the black colt until he is tamed"),
                GoalKind = GoalKind.RideToEnd,
                XpReward = 120, DinarReward = 60, BowAllowed = false,
                Intro = Script.Stage4Intro, Outro = Script.Stage4Outro,
                GrantsSkill = "r1",
                Build = s =>
                {
                    s.Length = 330f;
                    s.Ground(-10f, 360f);
                    s.Tip(4f, "برای پریدن، دکمهٔ پرش را بزن.", "Tap jump to leap.");
                    s.Enemy(Thing.Log, 26f, goal: false).Coins(24f, 2.6f, 4, 0.9f, 0.8f);
                    s.Enemy(Thing.Stone, 44f, goal: false);
                    s.Tip(54f, "شاخه‌ها! دکمهٔ خم شدن را نگه دار.", "Branches! Hold duck.");
                    s.Enemy(Thing.Branch, 66f, goal: false).Coins(64f, 0.8f, 4, 0.9f);
                    s.Enemy(Thing.Log, 84f, goal: false).Leaf(0, 84f, 3.6f);
                    s.Enemy(Thing.Branch, 102f, goal: false).Enemy(Thing.Log, 118f, goal: false);
                    s.Enemy(Thing.Stone, 132f, goal: false).Coins(140f, 1.0f, 5, 0.9f);
                    s.Enemy(Thing.Log, 152f, goal: false).Enemy(Thing.Branch, 166f, goal: false);
                    s.Enemy(Thing.Log, 180f, goal: false).Leaf(1, 180f, 3.7f);
                    s.Enemy(Thing.Stone, 192f, goal: false).Enemy(Thing.Branch, 206f, goal: false);
                    s.Enemy(Thing.Log, 220f, goal: false).Enemy(Thing.Log, 232f, goal: false);
                    s.Coins(240f, 1.0f, 5, 0.9f).Enemy(Thing.Branch, 252f, goal: false);
                    s.Enemy(Thing.Stone, 264f, goal: false).Enemy(Thing.Log, 276f, goal: false).Leaf(2, 276f, 3.7f);
                    s.Enemy(Thing.Branch, 290f, goal: false).Enemy(Thing.Log, 304f, goal: false);
                    for (var x = 0f; x < 340f; x += 23f)
                        s.Prop(x % 46f < 1f ? "cypress" : "tree_blossom", x + 9f);
                    for (var x = 5f; x < 340f; x += 31f)
                        s.Prop("rock_rose", x);
                },
            };
        }

        static StageDef OnagerHunt()
        {
            return new StageDef
            {
                Id = "1-5", Chapter = 1, Number = 5, Kind = StageKind.Ride,
                Name = new LocText("شکار گور", "The Onager Hunt"),
                Backdrop = "dusk_far", GroundTile = "ground_meadow",
                Goal = new LocText("سه گور شکار کن · {0} از {1}", "Bring down three onagers · {0} of {1}"),
                GoalKind = GoalKind.Hunt, GoalCount = 3,
                XpReward = 150, DinarReward = 80,
                Intro = Script.Stage5Intro,
                Build = s =>
                {
                    s.Length = 360f;
                    s.Ground(-10f, 390f);
                    s.Tip(4f, "دکمهٔ کمان را بزن تا به سوی نزدیک‌ترین گور تیر بیندازی.", "Tap the bow button to shoot at the nearest onager.");
                    s.Enemy(Thing.Onager, 40f).Enemy(Thing.Onager, 80f).Enemy(Thing.Foal, 100f, goal: false);
                    s.Enemy(Thing.Onager, 140f).Enemy(Thing.Onager, 200f).Enemy(Thing.Onager, 250f).Enemy(Thing.Onager, 300f);
                    s.Enemy(Thing.Log, 60f, goal: false).Enemy(Thing.Branch, 96f, goal: false).Enemy(Thing.Stone, 128f, goal: false);
                    s.Enemy(Thing.Log, 170f, goal: false).Enemy(Thing.Branch, 214f, goal: false).Enemy(Thing.Log, 262f, goal: false);
                    s.Enemy(Thing.Stone, 296f, goal: false).Enemy(Thing.Branch, 326f, goal: false);
                    s.Leaf(0, 60f, 3.6f).Leaf(1, 170f, 3.7f).Leaf(2, 262f, 3.7f);
                    s.Coins(110f, 1.0f, 5, 0.9f).Coins(230f, 1.0f, 5, 0.9f);
                    for (var x = 0f; x < 380f; x += 27f)
                        s.Prop(x % 54f < 1f ? "cypress" : "rock_lilac", x + 12f);
                },
            };
        }

        static StageDef RostamsTrial()
        {
            return new StageDef
            {
                Id = "1-6", Chapter = 1, Number = 6, Kind = StageKind.Walk, Boss = true,
                Name = new LocText("آزمون رستم", "Rostam's Trial"),
                Backdrop = "dusk_far", GroundTile = "ground_earth",
                Goal = new LocText("سه ضربهٔ پاک به رستم بزن · {0} از {1}", "Land three clean blows on Rostam · {0} of {1}"),
                GoalKind = GoalKind.Duel, GoalCount = 3,
                XpReward = 220, DinarReward = 120,
                Intro = Script.Stage6Intro,
                GrantsFarr = Rules.FarrRostam,
                Build = s =>
                {
                    s.Length = 26f;
                    s.Ground(-14f, 40f);
                    s.Prop("banner", -1f).Prop("fence", 3f).Prop("tree_rose", 12f).Prop("fence", 20f).Prop("banner", 26f);
                    s.Enemy(Thing.Rostam, 19f);
                    s.Leaf(0, 1f, 3.2f).Leaf(1, 13f, 4.6f).Leaf(2, 25f, 3.2f);
                    s.Ledge(11.5f, 2.2f, 3.0f);
                },
            };
        }
    }
}
