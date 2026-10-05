using System;
using Arash.Localization;

namespace Arash.Core
{
    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
    }

    /// <summary>Player settings (F-16), stored in the save file. Changes are saved immediately.</summary>
    public static class GameSettings
    {
        public static event Action Changed;

        static SettingsData Data { get { return SaveSystem.Data.settings; } }

        public static Language Language
        {
            get
            {
                if (Data.language < 0)
                    Data.language = (int)Loc.DetectDeviceLanguage();
                return (Language)Data.language;
            }
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
            get { return (Difficulty)Data.difficulty; }
            set { Data.difficulty = (int)value; SaveAndNotify(); }
        }

        /// <summary>Seconds of arrow flight the aim preview shows (GDD 4.2: none on Hard).</summary>
        public static float PreviewDuration
        {
            get
            {
                switch (Difficulty)
                {
                    case Difficulty.Easy: return 0.6f;
                    case Difficulty.Hard: return 0f;
                    default: return 0.35f;
                }
            }
        }

        /// <summary>Multiplier on enemy aiming error.</summary>
        public static float EnemyErrorMultiplier
        {
            get
            {
                switch (Difficulty)
                {
                    case Difficulty.Easy: return 1.6f;
                    case Difficulty.Hard: return 0.6f;
                    default: return 1f;
                }
            }
        }

        /// <summary>Enemy projectile damage (real-time battles, F-51).</summary>
        public static float EnemyDamageMultiplier
        {
            get
            {
                switch (Difficulty)
                {
                    case Difficulty.Easy: return 0.65f;
                    case Difficulty.Hard: return 1.3f;
                    default: return 1f;
                }
            }
        }

        /// <summary>Time between enemy shots (real-time battles, F-51).</summary>
        public static float EnemyCooldownMultiplier
        {
            get
            {
                switch (Difficulty)
                {
                    case Difficulty.Easy: return 1.3f;
                    case Difficulty.Hard: return 0.8f;
                    default: return 1f;
                }
            }
        }

        static void SaveAndNotify()
        {
            SaveSystem.Save();
            if (Changed != null)
                Changed();
        }
    }
}
