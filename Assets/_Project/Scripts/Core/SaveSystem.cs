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
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public List<LevelRecord> levels = new List<LevelRecord>();
        public SettingsData settings = new SettingsData();
        public List<string> seenCutscenes = new List<string>();

        // Armory (F-33, F-34)
        public List<string> owned = new List<string>();
        public List<UpgradeRecord> upgrades = new List<UpgradeRecord>();
        public string equippedBow = Armory.DefaultBow;
        public string equippedOutfit = Armory.DefaultOutfit;
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
            if (string.IsNullOrEmpty(save.equippedBow))
                save.equippedBow = Armory.DefaultBow;
            if (string.IsNullOrEmpty(save.equippedOutfit))
                save.equippedOutfit = Armory.DefaultOutfit;
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
