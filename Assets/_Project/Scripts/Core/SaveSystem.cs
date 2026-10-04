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

    /// <summary>Everything that is saved: progress, coins and settings (F-14).</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public List<LevelRecord> levels = new List<LevelRecord>();
        public SettingsData settings = new SettingsData();

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
