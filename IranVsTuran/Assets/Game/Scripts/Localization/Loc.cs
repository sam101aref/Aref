using System;
using System.Collections.Generic;
using UnityEngine;

namespace IranVsTuran.Localization
{
    public enum Language
    {
        English = 0,
        Persian = 1,
    }

    /// <summary>
    /// Bilingual strings. Every entry lives in one of the <see cref="Strings"/> tables as
    /// { key, English, Persian }. <see cref="T(string)"/> returns text ready for a uGUI Text:
    /// Persian is shaped and reordered by <see cref="PersianShaper"/> and numbers use Persian digits.
    /// </summary>
    public static class Loc
    {
        const string RegularFontPath = "Fonts/Vazirmatn-Regular";
        const string BoldFontPath = "Fonts/Vazirmatn-Bold";

        /// <summary>Raised after the language changes; open screens rebuild their text.</summary>
        public static event Action Changed;

        static Dictionary<string, string[]> table;
        static Language current = Language.Persian;
        static Font regularFont;
        static Font boldFont;

        public static Language Current { get { return current; } }
        public static bool IsRtl { get { return current == Language.Persian; } }

        public static Font Font { get { return regularFont != null ? regularFont : (regularFont = LoadFont(RegularFontPath)); } }
        public static Font BoldFont { get { return boldFont != null ? boldFont : (boldFont = LoadFont(BoldFontPath)); } }

        public static void SetLanguage(Language language)
        {
            if (current == language)
                return;
            current = language;
            if (Changed != null)
                Changed();
        }

        public static bool Has(string key)
        {
            return Table.ContainsKey(key);
        }

        /// <summary>The raw string in the current language (falls back to English, then to the key).</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            string[] entry;
            if (!Table.TryGetValue(key, out entry))
            {
                Debug.LogWarning("[Loc] Missing key: " + key);
                return key;
            }
            var text = current == Language.Persian ? entry[2] : entry[1];
            return string.IsNullOrEmpty(text) ? entry[1] : text;
        }

        /// <summary>Display-ready text for a key.</summary>
        public static string T(string key)
        {
            return Display(Get(key));
        }

        /// <summary>Display-ready text for a key with {0}-style arguments; numbers follow the language.</summary>
        public static string T(string key, params object[] args)
        {
            return Display(Format(Get(key), args));
        }

        /// <summary>Formats logical (unshaped) text; integers become Persian digits in Persian.</summary>
        public static string Format(string format, params object[] args)
        {
            var localized = new object[args.Length];
            for (var i = 0; i < args.Length; i++)
                localized[i] = args[i] is int ? Number((int)args[i]) : args[i];
            return string.Format(format, localized);
        }

        /// <summary>Prepares any string for display (shapes Persian, leaves other text alone).</summary>
        public static string Display(string text)
        {
            return PersianShaper.Shape(text);
        }

        public static string Number(int value)
        {
            var text = value.ToString();
            return current == Language.Persian ? PersianShaper.ToPersianDigits(text) : text;
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

        /// <summary>Persian-language devices start in Persian; everything else in English.</summary>
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
            return Language.English;
#else
            return Application.systemLanguage == SystemLanguage.English ? Language.English : Language.Persian;
#endif
        }

        /// <summary>All keys, for tests.</summary>
        public static IEnumerable<string> Keys { get { return Table.Keys; } }

        static Dictionary<string, string[]> Table
        {
            get
            {
                if (table == null)
                    table = Build();
                return table;
            }
        }

        static Dictionary<string, string[]> Build()
        {
            var result = new Dictionary<string, string[]>();
            foreach (var source in Strings.All)
                foreach (var entry in source)
                {
                    if (entry.Length != 3)
                    {
                        Debug.LogError("[Loc] Malformed entry: " + (entry.Length > 0 ? entry[0] : "?"));
                        continue;
                    }
                    if (result.ContainsKey(entry[0]))
                        Debug.LogWarning("[Loc] Duplicate key: " + entry[0]);
                    result[entry[0]] = entry;
                }
            return result;
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
