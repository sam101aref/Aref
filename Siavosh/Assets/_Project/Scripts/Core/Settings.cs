using System;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Core
{
    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
    }

    /// <summary>Player settings, stored in the save file. Changes are saved immediately.</summary>
    public static class Settings
    {
        public static event Action Changed;

        static SettingsData Data { get { return SaveSystem.Data.settings; } }

        /// <summary>False until the player has chosen a language on the first screen.</summary>
        public static bool LanguageChosen { get { return Data.language >= 0; } }

        public static Language Language
        {
            get { return Data.language < 0 ? Loc.DetectDeviceLanguage() : (Language)Data.language; }
            set
            {
                Data.language = (int)value;
                Loc.Apply(value);
                SaveAndNotify();
            }
        }

        public static bool Sound
        {
            get { return Data.sound; }
            set { Data.sound = value; SaveAndNotify(); }
        }

        public static bool Music
        {
            get { return Data.music; }
            set { Data.music = value; SaveAndNotify(); }
        }

        public static bool Vibration
        {
            get { return Data.vibration; }
            set { Data.vibration = value; SaveAndNotify(); }
        }

        public static Difficulty Difficulty
        {
            get { return (Difficulty)Mathf.Clamp(Data.difficulty, 0, 2); }
            set { Data.difficulty = (int)value; SaveAndNotify(); }
        }

        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Vibration)
                Handheld.Vibrate();
#endif
        }

        static void SaveAndNotify()
        {
            SaveSystem.Save();
            if (Changed != null)
                Changed();
        }
    }

    /// <summary>Pauses gameplay by stopping time.</summary>
    public static class Pause
    {
        public static bool IsPaused { get; private set; }

        public static void Set(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }
    }
}
