using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Siavosh.Core
{
    [Serializable]
    public class StageRecord
    {
        public string id;
        public bool done;
        /// <summary>Bit i set = leaf i of this stage found.</summary>
        public int leaves;
    }

    [Serializable]
    public class SettingsData
    {
        /// <summary>-1 until the player picks a language on the first screen.</summary>
        public int language = -1;
        public bool sound = true;
        public bool music = true;
        public bool vibration = true;
        public int difficulty = 1;
    }

    /// <summary>Everything that is saved: story progress, the hero's growth, purchases and settings.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public SettingsData settings = new SettingsData();

        // Growth
        public int xp;
        public int dinars;
        public int honor;
        public List<string> skills = new List<string>();
        public List<string> farr = new List<string>();

        // Gear
        public List<string> owned = new List<string>();
        public string sword = Rules.DefaultSword;
        public string bow = Rules.DefaultBow;
        public string armor = Rules.DefaultArmor;

        // Story
        public List<StageRecord> stages = new List<StageRecord>();
        public List<string> seen = new List<string>();
        /// <summary>One-time events, e.g. a kindness already rewarded.</summary>
        public List<string> flags = new List<string>();

        public bool Has(string flag) { return flags.Contains(flag); }

        public void Set(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag))
                flags.Add(flag);
        }

        public bool HasSeen(string cutsceneId) { return seen.Contains(cutsceneId); }

        public void MarkSeen(string cutsceneId)
        {
            if (!string.IsNullOrEmpty(cutsceneId) && !seen.Contains(cutsceneId))
                seen.Add(cutsceneId);
        }

        public bool Knows(string skillId) { return skills.Contains(skillId); }
        public bool HasFarr(string farrId) { return farr.Contains(farrId); }
        public bool Owns(string gearId) { return owned.Contains(gearId) || Rules.IsDefaultGear(gearId); }

        public StageRecord Stage(string id)
        {
            var record = stages.Find(s => s.id == id);
            if (record == null)
            {
                record = new StageRecord { id = id };
                stages.Add(record);
            }
            return record;
        }

        public bool IsDone(string stageId)
        {
            var record = stages.Find(s => s.id == stageId);
            return record != null && record.done;
        }

        public int LeavesFound(string stageId)
        {
            var record = stages.Find(s => s.id == stageId);
            return record == null ? 0 : Rules.CountBits(record.leaves);
        }

        public int TotalLeaves
        {
            get
            {
                var total = 0;
                foreach (var s in stages)
                    total += Rules.CountBits(s.leaves);
                return total;
            }
        }

        public int Level { get { return Rules.LevelForXp(xp); } }
        public int SkillPoints { get { return Rules.SkillPoints(Level, skills); } }
    }

    /// <summary>
    /// Loads and saves <see cref="SaveData"/> as JSON in the app's persistent data folder.
    /// Writes go to a temporary file first so a crash mid-write cannot corrupt the save.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "siavosh-save.json";
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

        /// <summary>Deletes all progress but keeps the settings.</summary>
        public static void ResetProgress()
        {
            var settings = Data.settings;
            data = new SaveData { settings = settings };
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
            if (save.settings == null) save.settings = new SettingsData();
            if (save.skills == null) save.skills = new List<string>();
            if (save.farr == null) save.farr = new List<string>();
            if (save.owned == null) save.owned = new List<string>();
            if (save.stages == null) save.stages = new List<StageRecord>();
            if (save.seen == null) save.seen = new List<string>();
            if (save.flags == null) save.flags = new List<string>();
            if (string.IsNullOrEmpty(save.sword)) save.sword = Rules.DefaultSword;
            if (string.IsNullOrEmpty(save.bow)) save.bow = Rules.DefaultBow;
            if (string.IsNullOrEmpty(save.armor)) save.armor = Rules.DefaultArmor;
            return save;
        }

        /// <summary>For tests: replaces the in-memory save without touching the disk.</summary>
        public static void UseInMemory(SaveData save)
        {
            data = save;
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
