using System.Collections.Generic;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Core
{
    public enum SkillBranch
    {
        Champion,
        Archery,
        Riding,
    }

    public enum GearSlot
    {
        Sword,
        Bow,
        Armor,
    }

    /// <summary>A skill on the tree (GDD 3.1). Each tier needs the one before it.</summary>
    public class SkillDef
    {
        public string Id;
        public SkillBranch Branch;
        public int Tier;
        public string Icon;
        public LocText Name;
        public LocText Description;
        /// <summary>Granted by the story instead of bought with a skill point.</summary>
        public bool StoryGranted;
        /// <summary>0 = available from the start; otherwise the chapter that teaches it.</summary>
        public int Chapter;
    }

    public class GearDef
    {
        public string Id;
        public GearSlot Slot;
        public LocText Name;
        public int Price;
        public float Damage;
        public float DrawTime = 1f;
        public int Health;
        /// <summary>Chapter in which the smith can make it.</summary>
        public int Chapter;
        public string Color;
    }

    public class FarrDef
    {
        public string Id;
        public string Icon;
        public LocText Name;
        public LocText Description;
        public int Chapter;
    }

    /// <summary>
    /// The numbers behind the hero's growth: experience levels, skill points, the skill tree, the
    /// smith's wares and the resulting combat stats. Pure logic, covered by EditMode tests.
    /// </summary>
    public static class Rules
    {
        public const string DefaultSword = "sword.zabuli";
        public const string DefaultBow = "bow.wood";
        public const string DefaultArmor = "armor.felt";

        public const string FarrRostam = "farr.rostam";

        /// <summary>Total experience needed to reach level index+1.</summary>
        static readonly int[] LevelXp = { 0, 100, 250, 450, 700, 1000, 1400, 1900, 2500, 3200, 4000, 5000, 6200, 7600, 9200 };

        public static int MaxLevel { get { return LevelXp.Length; } }

        public static int LevelForXp(int xp)
        {
            var level = 1;
            for (var i = 1; i < LevelXp.Length; i++)
                if (xp >= LevelXp[i])
                    level = i + 1;
            return level;
        }

        /// <summary>Experience at the start of a level and at the start of the next (for progress bars).</summary>
        public static void LevelRange(int level, out int from, out int to)
        {
            level = Mathf.Clamp(level, 1, LevelXp.Length);
            from = LevelXp[level - 1];
            to = level < LevelXp.Length ? LevelXp[level] : from;
        }

        /// <summary>One point per level after the first, minus the skills already bought.</summary>
        public static int SkillPoints(int level, List<string> learned)
        {
            var spent = 0;
            foreach (var id in learned)
            {
                var skill = Skill(id);
                if (skill != null && !skill.StoryGranted)
                    spent++;
            }
            return Mathf.Max(0, level - 1 - spent);
        }

        public static readonly List<SkillDef> Skills = new List<SkillDef>
        {
            new SkillDef { Id = "p1", Branch = SkillBranch.Champion, Tier = 1, Icon = "sword",
                Name = new LocText("کمبوی سه‌ضربه", "Three-strike combo"),
                Description = new LocText("ضربهٔ سوم پشت سر دو ضربهٔ اول؛ دشمن را به عقب پرت می‌کند.", "A third blow after the first two that throws the enemy back.") },
            new SkillDef { Id = "p2", Branch = SkillBranch.Champion, Tier = 2, Icon = "shield",
                Name = new LocText("دفع ضربه", "Parry"),
                Description = new LocText("سپر را درست در لحظهٔ ضربهٔ دشمن بالا ببر تا ضربه دفع شود و دشمن یک لحظه گیج بماند.", "Raise your shield just as a blow lands to turn it aside and leave the foe stunned.") },
            new SkillDef { Id = "p3", Branch = SkillBranch.Champion, Tier = 3, Icon = "heavy",
                Name = new LocText("ضربهٔ سنگین", "Heavy blow"),
                Description = new LocText("دکمهٔ ضربه را نگه دار و رها کن: ضربه‌ای که از سپر هم می‌گذرد.", "Hold and release the strike button for a blow that breaks through shields.") },
            new SkillDef { Id = "p4", Branch = SkillBranch.Champion, Tier = 4, Icon = "heart",
                Name = new LocText("تن رویین", "Iron body"),
                Description = new LocText("سلامت بیشتر: سی امتیاز به سلامت سیاوش افزوده می‌شود.", "Thirty more points of health.") },
            new SkillDef { Id = "a1", Branch = SkillBranch.Archery, Tier = 1, Icon = "bow",
                Name = new LocText("کشیدن تند", "Quick draw"),
                Description = new LocText("کمان را بسیار تندتر می‌کشی.", "You draw the bow much faster.") },
            new SkillDef { Id = "a2", Branch = SkillBranch.Archery, Tier = 2, Icon = "twin",
                Name = new LocText("دو تیر", "Twin arrows"),
                Description = new LocText("با هر کشیدن کمان، دو تیر با هم رها می‌شود.", "Every draw looses two arrows at once.") },
            new SkillDef { Id = "a3", Branch = SkillBranch.Archery, Tier = 3, Icon = "eye",
                Name = new LocText("چشم عقاب", "Eagle's eye"),
                Description = new LocText("راهنمای مسیر تیر بسیار بلندتر می‌شود.", "The arrow's path guide reaches much farther.") },
            new SkillDef { Id = "a4", Branch = SkillBranch.Archery, Tier = 4, Icon = "mounted",
                Name = new LocText("تیر از زین", "Shot from the saddle"),
                Description = new LocText("تیرهایی که از روی اسب می‌اندازی دو برابر کارگرند.", "Arrows loosed from horseback strike twice as hard.") },
            new SkillDef { Id = "r1", Branch = SkillBranch.Riding, Tier = 1, Icon = "shoe", StoryGranted = true, Chapter = 1,
                Name = new LocText("رام کردن", "Taming"),
                Description = new LocText("شبرنگ تو را پذیرفته است. (با داستان به دست می‌آید.)", "Shabrang has accepted you. (Earned in the story.)") },
            new SkillDef { Id = "r2", Branch = SkillBranch.Riding, Tier = 2, Icon = "wind",
                Name = new LocText("تاخت", "Gallop"),
                Description = new LocText("شبرنگ یک ضربهٔ بیشتر تاب می‌آورد.", "Shabrang can take one more knock.") },
            new SkillDef { Id = "r3", Branch = SkillBranch.Riding, Tier = 3, Icon = "leap",
                Name = new LocText("جهش دوبل", "Double leap"),
                Description = new LocText("شبرنگ در هوا یک بار دیگر می‌جهد.", "Shabrang can leap again in mid-air.") },
            new SkillDef { Id = "r4", Branch = SkillBranch.Riding, Tier = 4, Icon = "call", Chapter = 4,
                Name = new LocText("فراخواندن شبرنگ", "Call Shabrang"),
                Description = new LocText("در نبرد شبرنگ را فرا بخوان تا دشمنان را پراکنده کند. (از فصل ۴)", "Call Shabrang into battle to scatter your foes. (From chapter 4.)") },
        };

        public static readonly List<GearDef> Gear = new List<GearDef>
        {
            new GearDef { Id = DefaultSword, Slot = GearSlot.Sword, Name = new LocText("شمشیر زابلی", "Zabuli sword"), Damage = 10f, Color = "#6F7A84" },
            new GearDef { Id = "sword.kayani", Slot = GearSlot.Sword, Name = new LocText("شمشیر کیانی", "Kayanian sword"), Price = 250, Damage = 15f, Chapter = 1, Color = "#C99A2E" },
            new GearDef { Id = "sword.siavoshi", Slot = GearSlot.Sword, Name = new LocText("شمشیر سیاوشی", "Sword of Siavosh"), Price = 600, Damage = 22f, Chapter = 6, Color = "#1F3C88" },
            new GearDef { Id = DefaultBow, Slot = GearSlot.Bow, Name = new LocText("کمان چوبی", "Wooden bow"), Damage = 8f, Color = "#9C6B3E" },
            new GearDef { Id = "bow.horn", Slot = GearSlot.Bow, Name = new LocText("کمان شاخی", "Horn bow"), Price = 220, Damage = 12f, DrawTime = 0.8f, Chapter = 1, Color = "#E3A42B" },
            new GearDef { Id = "bow.royal", Slot = GearSlot.Bow, Name = new LocText("کمان شاهی", "Royal bow"), Price = 520, Damage = 17f, DrawTime = 0.7f, Chapter = 4, Color = "#C2412D" },
            new GearDef { Id = DefaultArmor, Slot = GearSlot.Armor, Name = new LocText("جامهٔ نمدی", "Felt coat"), Color = "#6B4626" },
            new GearDef { Id = "armor.lamellar", Slot = GearSlot.Armor, Name = new LocText("زره پولکی", "Lamellar armour"), Price = 300, Health = 30, Chapter = 1, Color = "#AEB7BF" },
            new GearDef { Id = "armor.kayani", Slot = GearSlot.Armor, Name = new LocText("زره کیانی", "Kayanian mail"), Price = 650, Health = 60, Chapter = 5, Color = "#C99A2E" },
        };

        public static readonly List<FarrDef> Farr = new List<FarrDef>
        {
            new FarrDef { Id = FarrRostam, Icon = "palm", Chapter = 1,
                Name = new LocText("پنجهٔ رستمی", "Rostam's Fist"),
                Description = new LocText("ضربه‌ای که زمین را می‌لرزاند و دشمنان دور و بر را پس می‌راند.", "A blow that shakes the ground and throws back every foe around you.") },
            new FarrDef { Id = "farr.fire", Icon = "flame", Chapter = 3,
                Name = new LocText("جامهٔ آتش", "Robe of Fire"),
                Description = new LocText("چند دم در برابر آتش و تیر بی‌آسیب می‌مانی.", "For a few breaths, neither fire nor arrow can harm you.") },
            new FarrDef { Id = "farr.oath", Icon = "oath", Chapter = 4,
                Name = new LocText("سوگند", "The Oath"),
                Description = new LocText("سپری از نور یارانت را در بر می‌گیرد.", "A shield of light surrounds your companions.") },
            new FarrDef { Id = "farr.light", Icon = "sun", Chapter = 6,
                Name = new LocText("نور سیاوش‌گرد", "Light of Siavoshgerd"),
                Description = new LocText("سلامت تو و یارانت را باز می‌گرداند.", "Restores your health and your companions'.") },
        };

        public static SkillDef Skill(string id)
        {
            return Skills.Find(s => s.Id == id);
        }

        public static GearDef GearItem(string id)
        {
            return Gear.Find(g => g.Id == id);
        }

        public static bool IsDefaultGear(string id)
        {
            return id == DefaultSword || id == DefaultBow || id == DefaultArmor;
        }

        public enum SkillState
        {
            Learned,
            Available,
            NeedsPrevious,
            NeedsPoint,
            StoryLocked,
        }

        public static SkillState StateOf(SkillDef skill, SaveData save, int unlockedChapter)
        {
            if (save.Knows(skill.Id))
                return SkillState.Learned;
            if (skill.StoryGranted || (skill.Chapter > unlockedChapter))
                return SkillState.StoryLocked;
            var previous = Skills.Find(s => s.Branch == skill.Branch && s.Tier == skill.Tier - 1);
            if (previous != null && !save.Knows(previous.Id))
                return SkillState.NeedsPrevious;
            if (save.SkillPoints <= 0)
                return SkillState.NeedsPoint;
            return SkillState.Available;
        }

        public static bool TryLearn(SaveData save, string skillId, int unlockedChapter)
        {
            var skill = Skill(skillId);
            if (skill == null || StateOf(skill, save, unlockedChapter) != SkillState.Available)
                return false;
            save.skills.Add(skill.Id);
            return true;
        }

        public static bool TryBuy(SaveData save, string gearId, int unlockedChapter)
        {
            var item = GearItem(gearId);
            if (item == null || save.Owns(gearId) || item.Chapter > unlockedChapter || save.dinars < item.Price)
                return false;
            save.dinars -= item.Price;
            save.owned.Add(gearId);
            Equip(save, gearId);
            return true;
        }

        public static void Equip(SaveData save, string gearId)
        {
            var item = GearItem(gearId);
            if (item == null || !save.Owns(gearId))
                return;
            switch (item.Slot)
            {
                case GearSlot.Sword: save.sword = gearId; break;
                case GearSlot.Bow: save.bow = gearId; break;
                default: save.armor = gearId; break;
            }
        }

        public static int CountBits(int mask)
        {
            var count = 0;
            while (mask != 0)
            {
                count += mask & 1;
                mask >>= 1;
            }
            return count;
        }
    }

    /// <summary>The hero's combat numbers, derived from level, skills, gear and difficulty.</summary>
    public struct HeroStats
    {
        public int MaxHealth;
        public float SwordDamage;
        public float BowDamage;
        public float DrawTime;
        public int ComboLength;
        public bool CanParry;
        public bool CanHeavy;
        public bool TwinArrows;
        public float PreviewSeconds;
        public float MountedShotMultiplier;
        public int HorseHearts;
        public bool DoubleLeap;
        public float DamageTaken;
        public bool HasFarr;

        public static HeroStats From(SaveData save, Difficulty difficulty)
        {
            var level = save.Level;
            var sword = Rules.GearItem(save.sword) ?? Rules.GearItem(Rules.DefaultSword);
            var bow = Rules.GearItem(save.bow) ?? Rules.GearItem(Rules.DefaultBow);
            var armor = Rules.GearItem(save.armor) ?? Rules.GearItem(Rules.DefaultArmor);
            var growth = 1f + (level - 1) * 0.05f;
            return new HeroStats
            {
                MaxHealth = 100 + armor.Health + (level - 1) * 6 + (save.Knows("p4") ? 30 : 0),
                SwordDamage = sword.Damage * growth,
                BowDamage = bow.Damage * growth,
                DrawTime = 0.65f * bow.DrawTime * (save.Knows("a1") ? 0.55f : 1f),
                ComboLength = save.Knows("p1") ? 3 : 2,
                CanParry = save.Knows("p2"),
                CanHeavy = save.Knows("p3"),
                TwinArrows = save.Knows("a2"),
                PreviewSeconds = save.Knows("a3") ? 0.9f : 0.35f,
                MountedShotMultiplier = save.Knows("a4") ? 2f : 1f,
                HorseHearts = 3 + (save.Knows("r2") ? 1 : 0),
                DoubleLeap = save.Knows("r3"),
                DamageTaken = difficulty == Difficulty.Easy ? 0.6f : difficulty == Difficulty.Hard ? 1.4f : 1f,
                HasFarr = save.HasFarr(Rules.FarrRostam),
            };
        }
    }
}
