using System;
using System.IO;
using UnityEngine;

namespace IranVsTuran.Core
{
    /// <summary>
    /// Loads and saves <see cref="SaveData"/> as JSON in the app's persistent data folder.
    /// Writes go to a temporary file first so a crash mid-write cannot corrupt the save.
    /// Tests call <see cref="UseInMemory"/> so they never touch the real file.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "iran-vs-turan.json";

        static SaveData data;
        static bool inMemory;

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

        /// <summary>Replaces the save with an in-memory one that is never written to disk.</summary>
        public static SaveData UseInMemory(SaveData save = null)
        {
            inMemory = true;
            data = save ?? NewGame();
            data.Repair();
            return data;
        }

        public static void Save()
        {
            if (inMemory || data == null)
                return;
            try
            {
                var temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Could not save: " + e.Message);
            }
        }

        /// <summary>Erases all progress (Settings ▸ Reset).</summary>
        public static void ResetProgress()
        {
            var settings = Data.settings;
            data = NewGame();
            data.settings = settings;
            Save();
        }

        static SaveData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        loaded.Repair();
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Could not load, starting fresh: " + e.Message);
            }
            return NewGame();
        }

        /// <summary>A brand-new player: Rostam, some gold and gems, and a couple of each item.</summary>
        public static SaveData NewGame()
        {
            var save = new SaveData { gold = 300, gems = 50 };
            save.AddHero(Defs.HeroIds.Rostam);
            save.selectedHero = Defs.HeroIds.Rostam;
            save.AddItem(Defs.ItemIds.Nushdaru, 1);
            save.AddItem(Defs.ItemIds.Naphtha, 1);
            save.Repair();
            return save;
        }
    }
}
