using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arash.Core
{
    [Serializable]
    public class LevelRecord
    {
        public string id;
        public int stars;
    }

    [Serializable]
    public class SettingsData
    {
        /// <summary>-1 until the player (or device language detection) picks one.</summary>
        public int language = -1;
        public bool sound = true;
        public bool music = true;
        public bool vibration = true;
        public int difficulty = 1;
    }

    [Serializable]
    public class UpgradeRecord
    {
        public string id;
        public int level;
    }

    /// <summary>Everything that is saved: progress, coins, settings, armory and purchases (F-14).</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public int coins;
        /// <summary>The rare currency (F-59): first three-star wins and bosses.</summary>
        public int gems;
        /// <summary>Levels whose first three-star win has paid out gems.</summary>
        public List<string> gemRewards = new List<string>();
        public List<LevelRecord> levels = new List<LevelRecord>();
        public SettingsData settings = new SettingsData();
        public List<string> seenCutscenes = new List<string>();

        // Shop and equipment (F-55 to F-58)
        public List<string> owned = new List<string>();
        public List<string> equippedBows = new List<string> { Armory.DefaultBow };
        public string equippedArmor;
        public string equippedHelmet;
        public string equippedShield;
        public string equippedOutfit = Armory.DefaultOutfit;

        // Version 1 armory, kept only so old saves can be migrated
        public List<UpgradeRecord> upgrades = new List<UpgradeRecord>();
        public string equippedBow;
        public string selectedSpecial;

        // Store purchases (F-38)
        public List<string> entitlements = new List<string>();

        public bool Owns(string itemId)
        {
            return owned.Contains(itemId);
        }

        public int UpgradeLevel(string upgradeId)
        {
            var record = upgrades.Find(u => u.id == upgradeId);
            return record != null ? record.level : 0;
        }

        public void SetUpgradeLevel(string upgradeId, int level)
        {
            var record = upgrades.Find(u => u.id == upgradeId);
            if (record == null)
                upgrades.Add(new UpgradeRecord { id = upgradeId, level = level });
            else
                record.level = level;
        }

        /// <summary>Pays out a one-time gem reward; false if it was already claimed.</summary>
        public bool ClaimGems(string rewardId, int amount)
        {
            if (string.IsNullOrEmpty(rewardId) || gemRewards.Contains(rewardId))
                return false;
            gemRewards.Add(rewardId);
            gems += amount;
            return true;
        }

        public bool HasSeen(string cutsceneId)
        {
            return seenCutscenes.Contains(cutsceneId);
        }

        public void MarkSeen(string cutsceneId)
        {
            if (!string.IsNullOrEmpty(cutsceneId) && !seenCutscenes.Contains(cutsceneId))
                seenCutscenes.Add(cutsceneId);
        }

        public int GetStars(string levelId)
        {
            var record = Find(levelId);
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

        /// <summary>Records a win; keeps the best star count.</summary>
        public void RecordWin(string levelId, int stars)
        {
            stars = Mathf.Clamp(stars, 1, 3);
            var record = Find(levelId);
            if (record == null)
                levels.Add(new LevelRecord { id = levelId, stars = stars });
            else
                record.stars = Mathf.Max(record.stars, stars);
        }

        LevelRecord Find(string levelId)
        {
            return levels.Find(r => r.id == levelId);
        }
    }

    /// <summary>
    /// Loads and saves <see cref="SaveData"/> as JSON in the app's persistent data folder.
    /// Writes go to a temporary file first so a crash mid-write cannot corrupt the save.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";

        static SaveData data;

        public static SaveData Data
        {
            get
            {
                if (data == null)
                    data = Load();
                return data;
            }
        }

        static string FilePath { get { return Path.Combine(Application.persistentDataPath, FileName); } }

        public static void Save()
        {
            try
            {
                var temp = FilePath + ".tmp";
                File.WriteAllText(temp, ToJson(Data));
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Could not save: " + e.Message);
            }
        }

        /// <summary>Deletes all progress and settings.</summary>
        public static void Reset()
        {
            data = new SaveData();
            Save();
        }

        public static string ToJson(SaveData save)
        {
            return JsonUtility.ToJson(save, true);
        }

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new SaveData();
            var save = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            // A save without a version field is older than versioning: treat it as version 1.
            var hasVersion = json.Contains("\"version\"");
            if (save.levels == null)
                save.levels = new List<LevelRecord>();
            if (save.settings == null)
                save.settings = new SettingsData();
            if (save.seenCutscenes == null)
                save.seenCutscenes = new List<string>();
            if (save.owned == null)
                save.owned = new List<string>();
            if (save.upgrades == null)
                save.upgrades = new List<UpgradeRecord>();
            if (save.entitlements == null)
                save.entitlements = new List<string>();
            if (save.gemRewards == null)
                save.gemRewards = new List<string>();
            if (save.equippedBows == null)
                save.equippedBows = new List<string>();
            if (string.IsNullOrEmpty(save.equippedOutfit))
                save.equippedOutfit = Armory.DefaultOutfit;
            if (!hasVersion || save.version < 2)
            {
                Armory.MigrateFromVersion1(save);
                save.version = SaveData.CurrentVersion;
            }
            if (save.equippedBows.Count == 0)
                save.equippedBows.Add(Armory.DefaultBow);
            return save;
        }

        static SaveData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return FromJson(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Could not load, starting fresh: " + e.Message);
            }
            return new SaveData();
        }
    }
}
