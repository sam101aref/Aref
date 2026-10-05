using System;
using System.Collections.Generic;

namespace IranVsTuran.Core
{
    [Serializable]
    public class LevelRecord
    {
        public string id;
        public int stars;
        public int wins;
    }

    [Serializable]
    public class CountRecord
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class HeroRecord
    {
        public string id;
        public int level = 1;
        public string skin = string.Empty;
    }

    [Serializable]
    public class SettingsData
    {
        /// <summary>-1 until the device language has been detected.</summary>
        public int language = -1;
        public bool sound = true;
        public bool music = true;
        /// <summary>0 casual, 1 normal, 2 veteran.</summary>
        public int difficulty = 1;
    }

    [Serializable]
    public class PassState
    {
        public int season;
        public int xp;
        public bool premium;
        public List<int> claimedFree = new List<int>();
        public List<int> claimedPremium = new List<int>();
    }

    [Serializable]
    public class DailyState
    {
        /// <summary>UTC day number of the last claim; -1 = never.</summary>
        public int lastDay = -1;
        /// <summary>Position in the seven-day reward cycle of the next claim.</summary>
        public int index;
    }

    [Serializable]
    public class AdState
    {
        public int day = -1;
        public List<CountRecord> watched = new List<CountRecord>();
    }

    /// <summary>Everything that is saved between sessions.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int gold;
        public int gems;
        public List<LevelRecord> levels = new List<LevelRecord>();
        public List<string> seen = new List<string>();
        public List<HeroRecord> heroes = new List<HeroRecord>();
        public string selectedHero = string.Empty;
        public List<string> skins = new List<string>();
        public List<CountRecord> upgrades = new List<CountRecord>();
        public List<CountRecord> items = new List<CountRecord>();
        public List<string> entitlements = new List<string>();
        public PassState pass = new PassState();
        public DailyState daily = new DailyState();
        public AdState ads = new AdState();
        public SettingsData settings = new SettingsData();
        public int kills;

        // ---- levels ----

        public int GetStars(string levelId)
        {
            var record = levels.Find(r => r.id == levelId);
            return record != null ? record.stars : 0;
        }

        public bool IsCompleted(string levelId)
        {
            return GetStars(levelId) > 0;
        }

        public int TotalStars
        {
            get
            {
                var total = 0;
                foreach (var record in levels)
                    total += record.stars;
                return total;
            }
        }

        /// <summary>Records a win and returns how many stars are new (above the previous best).</summary>
        public int RecordWin(string levelId, int stars)
        {
            stars = Math.Max(1, Math.Min(3, stars));
            var record = levels.Find(r => r.id == levelId);
            if (record == null)
            {
                levels.Add(new LevelRecord { id = levelId, stars = stars, wins = 1 });
                return stars;
            }
            var gained = Math.Max(0, stars - record.stars);
            record.stars = Math.Max(record.stars, stars);
            record.wins++;
            return gained;
        }

        public int Wins(string levelId)
        {
            var record = levels.Find(r => r.id == levelId);
            return record != null ? record.wins : 0;
        }

        // ---- story ----

        public bool HasSeen(string id)
        {
            return seen.Contains(id);
        }

        public void MarkSeen(string id)
        {
            if (!string.IsNullOrEmpty(id) && !seen.Contains(id))
                seen.Add(id);
        }

        // ---- heroes ----

        public HeroRecord Hero(string heroId)
        {
            return heroes.Find(h => h.id == heroId);
        }

        public bool OwnsHero(string heroId)
        {
            return Hero(heroId) != null;
        }

        public void AddHero(string heroId)
        {
            if (!OwnsHero(heroId))
                heroes.Add(new HeroRecord { id = heroId });
        }

        // ---- counters ----

        public int UpgradeRank(string id)
        {
            return Count(upgrades, id);
        }

        public void SetUpgradeRank(string id, int rank)
        {
            SetCount(upgrades, id, rank);
        }

        public int ItemCount(string id)
        {
            return Count(items, id);
        }

        public void AddItem(string id, int amount)
        {
            SetCount(items, id, Math.Max(0, Count(items, id) + amount));
        }

        internal static int Count(List<CountRecord> list, string id)
        {
            var record = list.Find(r => r.id == id);
            return record != null ? record.count : 0;
        }

        internal static void SetCount(List<CountRecord> list, string id, int count)
        {
            var record = list.Find(r => r.id == id);
            if (record == null)
                list.Add(new CountRecord { id = id, count = count });
            else
                record.count = count;
        }

        /// <summary>Fills fields that older saves (or a corrupted file) may lack.</summary>
        public void Repair()
        {
            if (levels == null) levels = new List<LevelRecord>();
            if (seen == null) seen = new List<string>();
            if (heroes == null) heroes = new List<HeroRecord>();
            if (skins == null) skins = new List<string>();
            if (upgrades == null) upgrades = new List<CountRecord>();
            if (items == null) items = new List<CountRecord>();
            if (entitlements == null) entitlements = new List<string>();
            if (pass == null) pass = new PassState();
            if (pass.claimedFree == null) pass.claimedFree = new List<int>();
            if (pass.claimedPremium == null) pass.claimedPremium = new List<int>();
            if (daily == null) daily = new DailyState();
            if (ads == null) ads = new AdState();
            if (ads.watched == null) ads.watched = new List<CountRecord>();
            if (settings == null) settings = new SettingsData();
            if (selectedHero == null) selectedHero = string.Empty;
            gold = Math.Max(0, gold);
            gems = Math.Max(0, gems);
            version = CurrentVersion;
        }
    }
}
