using System;
using UnityEngine;

namespace Siavosh.Localization
{
    public enum Language
    {
        English = 0,
        Persian = 1,
    }

    /// <summary>
    /// A piece of text in both languages. All of the game's words — interface and story alike — are
    /// written as <see cref="LocText"/> values next to the code that uses them, so a missing
    /// translation is impossible and the script reads like a script.
    /// </summary>
    [Serializable]
    public class LocText
    {
        public readonly string Fa;
        public readonly string En;

        public LocText(string fa, string en)
        {
            Fa = fa;
            En = en;
        }

        /// <summary>The raw (logical-order) text in the current language.</summary>
        public string Raw { get { return Loc.Current == Language.Persian ? Fa : En; } }

        /// <summary>Display-ready text (Persian shaped and reordered).</summary>
        public override string ToString()
        {
            return Loc.Display(Raw);
        }

        /// <summary>{0}-style formatting; numbers follow the language.</summary>
        public string Format(params object[] args)
        {
            var localized = new object[args.Length];
            for (var i = 0; i < args.Length; i++)
                localized[i] = args[i] is int ? Loc.Number((int)args[i]) : args[i];
            return Loc.Display(string.Format(Raw, localized));
        }

        public string RawFormat(params object[] args)
        {
            var localized = new object[args.Length];
            for (var i = 0; i < args.Length; i++)
                localized[i] = args[i] is int ? Loc.Number((int)args[i]) : args[i];
            return string.Format(Raw, localized);
        }
    }

    /// <summary>
    /// The current language, fonts and display helpers. Persian text is shaped and reordered by
    /// <see cref="PersianShaper"/> because Unity's text renderer neither joins letters nor lays out
    /// right to left.
    /// </summary>
    public static class Loc
    {
        const string RegularFontPath = "Fonts/Vazirmatn-Regular";
        const string BoldFontPath = "Fonts/Vazirmatn-Bold";

        /// <summary>Raised after the language changes; screens rebuild their text.</summary>
        public static event Action Changed;

        static Language current = Language.Persian;
        static Font regularFont;
        static Font boldFont;

        public static Language Current { get { return current; } }
        public static bool IsRtl { get { return current == Language.Persian; } }
        public static bool IsPersian { get { return current == Language.Persian; } }

        public static Font Font { get { return regularFont != null ? regularFont : (regularFont = LoadFont(RegularFontPath)); } }
        public static Font BoldFont { get { return boldFont != null ? boldFont : (boldFont = LoadFont(BoldFontPath)); } }

        public static void Apply(Language language)
        {
            var changed = current != language;
            current = language;
            if (changed && Changed != null)
                Changed();
        }

        /// <summary>Prepares any string for display (shapes Persian, leaves other text alone).</summary>
        public static string Display(string text)
        {
            return PersianShaper.Shape(text);
        }

        public static string Number(int value)
        {
            var text = value.ToString();
            return IsPersian ? PersianShaper.ToPersianDigits(text) : text;
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

        /// <summary>The device language, used to highlight a choice on the language screen.</summary>
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
            return Language.English;
#endif
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
