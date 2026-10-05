using UnityEngine;

namespace Siavosh.Core
{
    /// <summary>The manuscript palette (same values as Siavosh/Design/tools/palette.py).</summary>
    public static class Palette
    {
        public static readonly Color Ink = Hex(0x2B1B12);
        public static readonly Color Paper = Hex(0xF4E8CC);
        public static readonly Color PaperDark = Hex(0xE6D3A8);
        public static readonly Color Gold = Hex(0xC99A2E);
        public static readonly Color GoldLight = Hex(0xE8C65A);
        public static readonly Color GoldDark = Hex(0x8E6A1C);
        public static readonly Color Lapis = Hex(0x1F3C88);
        public static readonly Color LapisDark = Hex(0x14275E);
        public static readonly Color LapisNight = Hex(0x0E1A3F);
        public static readonly Color Turquoise = Hex(0x2E9C95);
        public static readonly Color TurquoiseLight = Hex(0x6CC5B8);
        public static readonly Color Vermilion = Hex(0xC2412D);
        public static readonly Color Crimson = Hex(0x8E1F2A);
        public static readonly Color Saffron = Hex(0xE3A42B);
        public static readonly Color Leaf = Hex(0x4F7A34);
        public static readonly Color Plum = Hex(0x5A2E5E);
        public static readonly Color Rose = Hex(0xD88E86);
        public static readonly Color White = Hex(0xFBF7EC);
        public static readonly Color Earth = Hex(0x9C6B3E);
        public static readonly Color EarthDark = Hex(0x6B4626);
        public static readonly Color Steel = Hex(0xAEB7BF);
        public static readonly Color Smoke = Hex(0x5B4A44);
        public static readonly Color Shade = new Color(0.08f, 0.05f, 0.03f, 0.82f);
        public static readonly Color Locked = Hex(0xB8AC92);

        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        public static Color Parse(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.white;
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
