using System;
using System.Collections.Generic;
using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.Story;
using UnityEngine;

namespace Siavosh.Play
{
    public enum StageKind
    {
        /// <summary>On foot: running, jumping, sword and bow.</summary>
        Walk,
        /// <summary>On Shabrang: an endless gallop, jumping and ducking.</summary>
        Ride,
    }

    public enum Thing
    {
        Ground,     // solid ground from X to X+W
        Water,      // decorative water filling a gap from X to X+W
        Ledge,      // one-way rock platform, top at Y
        Prop,       // scenery behind the action (Name = props/<name>)
        PropFront,  // scenery in front of the action
        Leaf,       // collectible page of the Shahnameh (Index 0..2)
        Coin,
        Heal,       // pomegranate
        Tip,        // tutorial hint shown when the hero reaches X
        Gate,       // the stage exit
        Npc,        // a figure standing by (Name = chars/<name>)
        Dummy,
        Target,
        SwingTarget,
        Trainee,
        Spearman,
        Archer,
        Rostam,
        Lamb,       // kindness: return the lost lamb
        HurtBird,   // kindness: tend the wounded hoopoe
        Log,        // riding: jump
        Branch,     // riding: duck
        Stone,      // riding: jump
        Onager,     // riding hunt: shoot
        Foal,       // riding hunt: spare
    }

    public class Placement
    {
        public Thing What;
        public float X;
        public float Y;
        public float W;
        public string Name;
        public int Index;
        public LocText Text;
        public bool Goal;
    }

    /// <summary>A stage's contents, written with the methods below in world units (ground is y = 0).</summary>
    public class StageLayout
    {
        public readonly List<Placement> Items = new List<Placement>();
        public float Length;

        Placement Add(Thing what, float x, float y = 0f)
        {
            var p = new Placement { What = what, X = x, Y = y };
            Items.Add(p);
            return p;
        }

        public StageLayout Ground(float from, float to) { Add(Thing.Ground, from).W = to - from; return this; }
        public StageLayout Water(float from, float to) { Add(Thing.Water, from).W = to - from; return this; }
        public StageLayout Ledge(float x, float y, float width) { Add(Thing.Ledge, x, y).W = width; return this; }
        public StageLayout Prop(string name, float x, float y = 0f) { Add(Thing.Prop, x, y).Name = name; return this; }
        public StageLayout Front(string name, float x, float y = 0f) { Add(Thing.PropFront, x, y).Name = name; return this; }
        public StageLayout Leaf(int index, float x, float y) { Add(Thing.Leaf, x, y).Index = index; return this; }
        public StageLayout Heal(float x, float y) { Add(Thing.Heal, x, y); return this; }
        public StageLayout Gate(float x) { Add(Thing.Gate, x); return this; }
        public StageLayout Npc(string figure, float x, bool faceLeft = true) { var p = Add(Thing.Npc, x); p.Name = figure; p.Index = faceLeft ? 1 : 0; return this; }

        public StageLayout Coins(float x, float y, int count, float spacing = 0.8f, float arc = 0f)
        {
            for (var i = 0; i < count; i++)
            {
                var t = count > 1 ? i / (float)(count - 1) : 0.5f;
                Add(Thing.Coin, x + i * spacing, y + arc * Mathf.Sin(t * Mathf.PI));
            }
            return this;
        }

        public StageLayout Tip(float x, string fa, string en) { Add(Thing.Tip, x).Text = new LocText(fa, en); return this; }

        public StageLayout Enemy(Thing what, float x, float y = 0f, bool goal = true)
        {
            var p = Add(what, x, y);
            p.Goal = goal;
            return this;
        }

        public StageLayout Kindness(Thing what, float x, string flag) { Add(what, x).Name = flag; return this; }
    }

    public enum GoalKind
    {
        /// <summary>Walk to the gate.</summary>
        ReachGate,
        /// <summary>The gate opens once every goal-marked thing is dealt with.</summary>
        ClearGoals,
        /// <summary>Ride to the end.</summary>
        RideToEnd,
        /// <summary>Ride to the end having brought down enough goal-marked game.</summary>
        Hunt,
        /// <summary>Land clean blows on Rostam.</summary>
        Duel,
    }

    public class StageDef
    {
        public string Id;
        public int Chapter;
        public int Number;
        public LocText Name;
        public StageKind Kind;
        public string Backdrop;       // Art/bg/<name>
        public string GroundTile;     // Art/tiles/<name>
        public LocText Goal;          // may contain {0} done and {1} needed
        public GoalKind GoalKind;
        public int GoalCount;
        public int XpReward;
        public int DinarReward;
        public bool Boss;
        public bool BowAllowed = true;
        public Cutscene Intro;        // dialogue over the stage when it starts
        public Cutscene Outro;        // played after the result screen
        public string GrantsSkill;
        public string GrantsFarr;
        public Action<StageLayout> Build;

        public StageLayout Layout()
        {
            var layout = new StageLayout();
            Build(layout);
            return layout;
        }
    }

    public class ChapterDef
    {
        public int Number;
        public string TitleKey;       // titles/<key>_fa|_en
        public LocText Name;
        public LocText Place;
        public Cutscene Intro;
        public Cutscene Outro;
        public readonly List<StageDef> Stages = new List<StageDef>();
        public bool Available { get { return Stages.Count > 0; } }
    }

    /// <summary>What the hero brought out of a stage.</summary>
    public struct StageOutcome
    {
        public int Xp;
        public int Dinars;
        public int LeavesMask;
        public int Honor;
        public LocText HonorNote;
        /// <summary>Kindnesses done this run (saved so they are rewarded only once).</summary>
        public List<string> Flags;
    }

    public class StageResult
    {
        public StageDef Stage;
        public int Xp;
        public int Dinars;
        public int Honor;
        public int LeavesMask;
        public int NewLeaves;
        public bool LevelUp;
        public LocText HonorNote;
    }

    /// <summary>All chapters and stages. Chapters 2–7 and the epilogue are listed for the map and arrive later.</summary>
    public static class Catalog
    {
        public static readonly List<ChapterDef> Chapters = new List<ChapterDef>();
        public static StageDef FirstStage { get { return Chapters[1].Stages[0]; } }

        static Catalog()
        {
            Chapters.Add(new ChapterDef { Number = 0, TitleKey = "ch0", Name = new LocText("پیش‌درآمد", "Prologue"), Place = new LocText("زادن سیاوش", "The birth of Siavosh") });
            var ch1 = new ChapterDef
            {
                Number = 1, TitleKey = "ch1",
                Name = new LocText("شاگرد رستم", "Rostam's Pupil"), Place = new LocText("زابلستان", "Zabulistan"),
                Intro = Script.Chapter1Intro, Outro = Script.Chapter1Outro,
            };
            ch1.Stages.AddRange(Chapter1.Stages());
            Chapters.Add(ch1);
            Chapters.Add(new ChapterDef { Number = 2, TitleKey = "ch2", Name = new LocText("بازگشت به پایتخت", "Return to the Capital"), Place = new LocText("کاخ کاووس", "The palace of Kavus") });
            Chapters.Add(new ChapterDef { Number = 3, TitleKey = "ch3", Name = new LocText("گذر از آتش", "Through the Fire"), Place = new LocText("دو کوه آتش", "Two mountains of fire") });
            Chapters.Add(new ChapterDef { Number = 4, TitleKey = "ch4", Name = new LocText("جنگ بلخ", "The War at Balkh"), Place = new LocText("بلخ", "Balkh") });
            Chapters.Add(new ChapterDef { Number = 5, TitleKey = "ch5", Name = new LocText("مهمان توران", "Guest of Turan"), Place = new LocText("دربار افراسیاب", "The court of Afrasiab") });
            Chapters.Add(new ChapterDef { Number = 6, TitleKey = "ch6", Name = new LocText("سیاوش‌گرد", "Siavoshgerd"), Place = new LocText("شهر سیاوش", "The city of Siavosh") });
            Chapters.Add(new ChapterDef { Number = 7, TitleKey = "ch7", Name = new LocText("نیرنگ", "The Deceit"), Place = new LocText("نیرنگ گرسیوز", "The lies of Garsivaz") });
            Chapters.Add(new ChapterDef { Number = 8, TitleKey = "ch8", Name = new LocText("کیخسرو", "Kay Khosrow"), Place = new LocText("پس‌گفتار", "Epilogue") });
        }

        public static IEnumerable<StageDef> AllStages()
        {
            foreach (var chapter in Chapters)
                foreach (var stage in chapter.Stages)
                    yield return stage;
        }

        public static ChapterDef ChapterOf(StageDef stage)
        {
            return stage == null ? null : Chapters.Find(c => c.Stages.Contains(stage));
        }

        /// <summary>The first stage not yet finished, or null when all are done.</summary>
        public static StageDef NextStage(SaveData save)
        {
            foreach (var stage in AllStages())
                if (!save.IsDone(stage.Id))
                    return stage;
            return null;
        }

        /// <summary>A stage can be played once the one before it is done.</summary>
        public static bool IsUnlocked(StageDef stage, SaveData save)
        {
            StageDef previous = null;
            foreach (var s in AllStages())
            {
                if (s == stage)
                    return previous == null || save.IsDone(previous.Id);
                previous = s;
            }
            return false;
        }

        /// <summary>The highest chapter the hero has reached (for the smith and skill tree).</summary>
        public static int UnlockedChapter(SaveData save)
        {
            var next = NextStage(save);
            return next != null ? next.Chapter : Chapters[Chapters.Count - 1].Number;
        }
    }
}
