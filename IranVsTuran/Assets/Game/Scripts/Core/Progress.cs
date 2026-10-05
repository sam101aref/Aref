using System.Collections.Generic;
using IranVsTuran.Defs;
using IranVsTuran.Localization;

namespace IranVsTuran.Core
{
    /// <summary>What a won battle paid out, for the victory screen.</summary>
    public class VictoryRewards
    {
        public int stars;
        public int newStars;
        public int gold;
        public int gems;
        public int passXp;
        public bool firstWin;
        public string unlockedHero;
    }

    /// <summary>Campaign progress: which levels are open, and what winning pays.</summary>
    public static class Progress
    {
        public const int GemsPerNewStar = 5;

        public static bool IsUnlocked(LevelDef level)
        {
            if (level.index == 0)
                return true;
            var previous = LevelDefs.All[level.index - 1];
            return SaveSystem.Data.IsCompleted(previous.id);
        }

        public static bool IsChapterUnlocked(ChapterDef chapter)
        {
            return IsUnlocked(LevelDefs.Get(chapter.levels[0]));
        }

        /// <summary>Stars from lives left, the Kingdom Rush way: 18+ of 20 for three, 6+ for two.</summary>
        public static int StarsFor(int livesLeft, int startLives)
        {
            if (livesLeft <= 0)
                return 0;
            var fraction = livesLeft / (float)startLives;
            if (fraction >= 0.9f)
                return 3;
            if (fraction >= 0.3f)
                return 2;
            return 1;
        }

        /// <summary>Gold for winning <paramref name="level"/> with <paramref name="stars"/>; replays pay 40%.</summary>
        public static int GoldFor(LevelDef level, int stars, bool firstWin)
        {
            var gold = 120 + 40 * stars + 25 * level.index;
            return firstWin ? gold : gold * 2 / 5;
        }

        public static VictoryRewards RecordVictory(LevelDef level, int stars)
        {
            var save = SaveSystem.Data;
            var result = new VictoryRewards { stars = stars, firstWin = !save.IsCompleted(level.id) };
            result.newStars = save.RecordWin(level.id, stars);
            result.gold = GoldFor(level, stars, result.firstWin);
            result.gems = result.newStars * GemsPerNewStar;
            result.passXp = BattlePass.XpForWin(stars);

            foreach (var hero in HeroDefs.All)
                if (hero.unlockLevel == level.id && !save.OwnsHero(hero.id))
                {
                    save.AddHero(hero.id);
                    result.unlockedHero = hero.id;
                }

            save.gold += result.gold;
            save.gems += result.gems;
            BattlePass.AddXp(result.passXp); // also saves
            Economy.Commit();
            return result;
        }

        /// <summary>Total stars available in the campaign.</summary>
        public static int MaxStars { get { return LevelDefs.All.Count * 3; } }
    }

    /// <summary>Heroes: buying with gems, training with gold, choosing one and its skin.</summary>
    public static class Heroes
    {
        public static HeroDef Selected
        {
            get
            {
                var hero = HeroDefs.Get(SaveSystem.Data.selectedHero);
                return hero != null && SaveSystem.Data.OwnsHero(hero.id) ? hero : HeroDefs.Get(HeroIds.Rostam);
            }
        }

        public static int Level(string heroId)
        {
            var record = SaveSystem.Data.Hero(heroId);
            return record != null ? record.level : 1;
        }

        public static string Skin(string heroId)
        {
            var record = SaveSystem.Data.Hero(heroId);
            return record != null ? record.skin : string.Empty;
        }

        public static bool Buy(HeroDef hero)
        {
            if (hero.gemPrice <= 0 || SaveSystem.Data.OwnsHero(hero.id) || !Economy.TrySpendGems(hero.gemPrice))
                return false;
            SaveSystem.Data.AddHero(hero.id);
            Economy.Commit();
            return true;
        }

        public static bool Train(HeroDef hero)
        {
            var record = SaveSystem.Data.Hero(hero.id);
            if (record == null || record.level >= HeroDefs.MaxLevel || !Economy.TrySpendGold(HeroDefs.TrainCost(record.level)))
                return false;
            record.level++;
            Economy.Commit();
            return true;
        }

        public static void Select(HeroDef hero)
        {
            if (!SaveSystem.Data.OwnsHero(hero.id))
                return;
            SaveSystem.Data.selectedHero = hero.id;
            Economy.Commit();
        }

        public static bool OwnsSkin(SkinDef skin)
        {
            return SaveSystem.Data.skins.Contains(skin.id);
        }

        public static bool BuySkin(SkinDef skin)
        {
            if (skin.gemPrice <= 0 || OwnsSkin(skin) || !Economy.TrySpendGems(skin.gemPrice))
                return false;
            SaveSystem.Data.skins.Add(skin.id);
            Economy.Commit();
            return true;
        }

        /// <summary>Wears a skin (or the default look when <paramref name="skin"/> is null).</summary>
        public static void Wear(HeroDef hero, SkinDef skin)
        {
            var record = SaveSystem.Data.Hero(hero.id);
            if (record == null || (skin != null && !OwnsSkin(skin)))
                return;
            record.skin = skin != null ? skin.id : string.Empty;
            Economy.Commit();
        }

        public static List<SkinDef> SkinsOf(HeroDef hero)
        {
            return HeroDefs.Skins.FindAll(s => s.heroId == hero.id);
        }
    }

    /// <summary>Permanent upgrades bought with gold.</summary>
    public static class Upgrades
    {
        public static int Rank(string upgradeId)
        {
            return SaveSystem.Data.UpgradeRank(upgradeId);
        }

        /// <summary>The upgrade's total effect: rank × per-rank value (0 if not bought).</summary>
        public static float Bonus(string upgradeId)
        {
            var def = UpgradeDefs.Get(upgradeId);
            return def == null ? 0f : Rank(upgradeId) * def.perRank;
        }

        public static int NextCost(UpgradeDef def)
        {
            var rank = Rank(def.id);
            return rank < def.MaxRank ? def.costs[rank] : 0;
        }

        public static bool Buy(UpgradeDef def)
        {
            var rank = Rank(def.id);
            if (rank >= def.MaxRank || !Economy.TrySpendGold(def.costs[rank]))
                return false;
            SaveSystem.Data.SetUpgradeRank(def.id, rank + 1);
            Economy.Commit();
            return true;
        }
    }

    /// <summary>Sound, music and language, stored in the save.</summary>
    public static class GameSettings
    {
        public static bool Sound
        {
            get { return SaveSystem.Data.settings.sound; }
            set { SaveSystem.Data.settings.sound = value; SaveSystem.Save(); }
        }

        public static bool Music
        {
            get { return SaveSystem.Data.settings.music; }
            set { SaveSystem.Data.settings.music = value; SaveSystem.Save(); }
        }

        public static Language Language
        {
            get
            {
                var settings = SaveSystem.Data.settings;
                if (settings.language < 0)
                {
                    settings.language = (int)Loc.DetectDeviceLanguage();
                    SaveSystem.Save();
                }
                return (Language)settings.language;
            }
            set
            {
                SaveSystem.Data.settings.language = (int)value;
                SaveSystem.Save();
                Loc.SetLanguage(value);
            }
        }
    }
}
