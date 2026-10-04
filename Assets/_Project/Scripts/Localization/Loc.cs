using System;
using System.Collections.Generic;
using Arash.Core;
using UnityEngine;

namespace Arash.Localization
{
    public enum Language
    {
        English = 0,
        Persian = 1,
    }

    /// <summary>
    /// Bilingual strings (F-15). Strings live in Resources/Localization/Strings.json as
    /// { key, en, fa } entries. <see cref="T(string)"/> returns text ready to display: Persian is
    /// shaped and reordered by <see cref="PersianShaper"/> and numbers use Persian digits.
    /// </summary>
    public static class Loc
    {
        const string TablePath = "Localization/Strings";
        const string RegularFontPath = "Fonts/Vazirmatn-Regular";
        const string BoldFontPath = "Fonts/Vazirmatn-Bold";

#pragma warning disable 0649 // filled in by JsonUtility
        [Serializable]
        class Entry
        {
            public string key;
            public string en;
            public string fa;
        }

        [Serializable]
        class Table
        {
            public List<Entry> entries = new List<Entry>();
        }
#pragma warning restore 0649

        /// <summary>Raised after the language changes; screens rebuild their text.</summary>
        public static event Action Changed;

        static Dictionary<string, Entry> entries;
        static Language? current;
        static Font regularFont;
        static Font boldFont;

        public static Language Current
        {
            get
            {
                if (!current.HasValue)
                    current = GameSettings.Language;
                return current.Value;
            }
        }

        public static bool IsRtl { get { return Current == Language.Persian; } }

        public static Font Font { get { return regularFont != null ? regularFont : (regularFont = LoadFont(RegularFontPath)); } }
        public static Font BoldFont { get { return boldFont != null ? boldFont : (boldFont = LoadFont(BoldFontPath)); } }

        /// <summary>Called by <see cref="GameSettings"/>; use GameSettings.Language to change and save.</summary>
        internal static void Apply(Language language)
        {
            var changed = current.HasValue && current.Value != language;
            current = language;
            if (changed && Changed != null)
                Changed();
        }

        /// <summary>The raw string in the current language (falls back to English, then to the key).</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            if (entries == null)
                LoadTable();

            Entry entry;
            if (!entries.TryGetValue(key, out entry))
            {
                Debug.LogWarning("[Loc] Missing key: " + key);
                return key;
            }
            var text = Current == Language.Persian ? entry.fa : entry.en;
            return string.IsNullOrEmpty(text) ? entry.en : text;
        }

        /// <summary>Display-ready text for a key.</summary>
        public static string T(string key)
        {
            return Display(Get(key));
        }

        /// <summary>Display-ready text for a key with {0}-style arguments; numbers follow the language.</summary>
        public static string T(string key, params object[] args)
        {
            var localized = new object[args.Length];
            for (var i = 0; i < args.Length; i++)
                localized[i] = args[i] is int ? Number((int)args[i]) : args[i];
            return Display(string.Format(Get(key), localized));
        }

        /// <summary>Prepares any string for display (shapes Persian, leaves other text alone).</summary>
        public static string Display(string text)
        {
            return PersianShaper.Shape(text);
        }

        public static string Number(int value)
        {
            var text = value.ToString();
            return Current == Language.Persian ? PersianShaper.ToPersianDigits(text) : text;
        }

        /// <summary>Mirrors left/right alignment for right-to-left languages.</summary>
        public static TextAnchor Align(TextAnchor anchor)
        {
            if (!IsRtl)
                return anchor;
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAnchor.UpperRight;
                case TextAnchor.UpperRight: return TextAnchor.UpperLeft;
                case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight;
                case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft;
                case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
                case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
                default: return anchor;
            }
        }

        /// <summary>Guesses the device language; Persian devices start in Persian.</summary>
        public static Language DetectDeviceLanguage()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var locale = new AndroidJavaClass("java.util.Locale"))
                using (var defaultLocale = locale.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    var code = defaultLocale.Call<string>("getLanguage");
                    if (code == "fa" || code == "per")
                        return Language.Persian;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Loc] Could not read the device language: " + e.Message);
            }
#endif
            return Language.English;
        }

        static void LoadTable()
        {
            entries = new Dictionary<string, Entry>();
            var asset = Resources.Load<TextAsset>(TablePath);
            if (asset == null)
            {
                Debug.LogError("[Loc] String table not found at Resources/" + TablePath);
                return;
            }

            var table = JsonUtility.FromJson<Table>(asset.text);
            foreach (var entry in table.entries)
                entries[entry.key] = entry;
        }

        static Font LoadFont(string path)
        {
            var font = Resources.Load<Font>(path);
            if (font == null)
            {
                Debug.LogWarning("[Loc] Font missing at Resources/" + path + "; Persian text will not render.");
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return font;
        }
    }
}
