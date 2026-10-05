using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Art
{
    /// <summary>
    /// Paints towers and build plots. Canvas: x −1…1, y −0.35…1.85 design units; the tower
    /// stands on (0, 0). Archers and fire are separate sprites drawn on top by the battle code.
    /// </summary>
    public static class TowerArt
    {
        public const float MinX = -1f;
        public const float MinY = -0.35f;
        public const float Size = 2.2f;
        /// <summary>Where the archer or the flame sits, in design units.</summary>
        public const float TopY = 1.05f;

        static readonly Color Ink = Raster.Hex(0x2A1E14);
        static readonly Color Stone = Raster.Hex(0xB9AE98);
        static readonly Color StoneDark = Raster.Hex(0x8C8270);
        static readonly Color Brick = Raster.Hex(0xC98E5A);
        static readonly Color Wood = Raster.Hex(0x8A5A2E);
        static readonly Color WoodDark = Raster.Hex(0x5E3A1C);
        static readonly Color Turquoise = Raster.Hex(0x1FA2A6);
        static readonly Color Gold = Raster.Hex(0xE2B13C);
        static readonly Color Red = Raster.Hex(0xB0302A);
        static readonly Color Purple = Raster.Hex(0x6A2A7A);
        static readonly Color Cream = Raster.Hex(0xF2E6C8);
        const float O = 0.04f;

        public static Raster Paint(TowerDef def, int pixels)
        {
            var r = new Raster(pixels, pixels, MinX, MinY, pixels / Size);
            Base(r);
            switch (def.family)
            {
                case TowerFamily.Archer: Archer(r, def); break;
                case TowerFamily.Barracks: Barracks(r, def); break;
                case TowerFamily.Mage: Mage(r, def); break;
                default: Artillery(r, def); break;
            }
            return r;
        }

        /// <summary>The empty plot where a tower can be built.</summary>
        public static Raster PaintSlot(int pixels)
        {
            var r = new Raster(pixels, pixels, MinX, MinY, pixels / Size);
            r.Ellipse(0f, 0.02f, 0.72f, 0.36f, new Color(0f, 0f, 0f, 0.25f));
            r.Ellipse(0f, 0.06f, 0.66f, 0.32f, Raster.Hex(0x9B7B50), O, Ink);
            r.Ellipse(0f, 0.1f, 0.5f, 0.22f, Raster.Hex(0xB59466));
            for (var i = 0; i < 9; i++)
            {
                var a = i / 9f * Mathf.PI * 2f;
                r.Ellipse(Mathf.Cos(a) * 0.62f, 0.06f + Mathf.Sin(a) * 0.3f, 0.09f, 0.06f, Stone, O * 0.6f, Ink);
            }
            // a little banner pole marks it as buildable
            r.Capsule(0.38f, 0.1f, 0.38f, 0.75f, 0.025f, WoodDark, O * 0.5f, Ink);
            r.PolygonOutlined(Red, O * 0.5f, Ink, 0.4f, 0.75f, 0.68f, 0.66f, 0.4f, 0.55f);
            return r;
        }

        /// <summary>The small archer who stands on archer towers (facing right).</summary>
        public static Look ArcherLook(int tier)
        {
            return new Look
            {
                main = tier >= 4 ? 0x1E4A7Au : 0x2E6B3Au,
                accent = 0xC9A227,
                head = tier >= 3 ? Headgear.Helmet : Headgear.Cap,
                weapon = Weapon.Bow,
            };
        }

        static void Base(Raster r)
        {
            r.Ellipse(0f, 0.0f, 0.8f, 0.3f, new Color(0f, 0f, 0f, 0.3f));
            r.Ellipse(0f, 0.06f, 0.74f, 0.28f, StoneDark, O, Ink);
            r.Ellipse(0f, 0.1f, 0.66f, 0.22f, Stone);
        }

        static void Crenellations(Raster r, float cx, float y, float halfWidth, int count, Color color)
        {
            var step = halfWidth * 2f / count;
            for (var i = 0; i < count; i++)
            {
                var x = cx - halfWidth + step * (i + 0.5f);
                r.Box(x, y, step * 0.32f, 0.08f, 0.01f, color, 0f, O * 0.6f, Ink);
            }
        }

        static void Flag(Raster r, float x, float y, Color color, float size = 1f)
        {
            r.Capsule(x, y, x, y + 0.55f * size, 0.022f, WoodDark, O * 0.5f, Ink);
            r.PolygonOutlined(color, O * 0.5f, Ink, x, y + 0.55f * size, x + 0.36f * size, y + 0.46f * size, x, y + 0.34f * size);
        }

        static void Archer(Raster r, TowerDef def)
        {
            if (def.tier == 1)
            {
                r.Capsule(-0.4f, 0.12f, -0.34f, 0.9f, 0.05f, WoodDark, O, Ink);
                r.Capsule(0.4f, 0.12f, 0.34f, 0.9f, 0.05f, WoodDark, O, Ink);
                r.Capsule(-0.36f, 0.3f, 0.36f, 0.7f, 0.025f, Wood);
                r.Capsule(0.36f, 0.3f, -0.36f, 0.7f, 0.025f, Wood);
                r.Box(0f, 0.92f, 0.5f, 0.07f, 0.02f, Wood, 0f, O, Ink);
                for (var i = 0; i < 5; i++)
                    r.Capsule(-0.44f + i * 0.22f, 0.98f, -0.44f + i * 0.22f, 1.12f, 0.035f, Wood, O * 0.5f, Ink);
                return;
            }

            var top = def.tier == 2 ? 0.95f : 1.0f;
            var width = def.tier == 2 ? 0.42f : 0.48f;
            var wall = def.tier == 2 ? Brick : Stone;
            if (def.id == "archer4a")
            {
                wall = Cream;
                width = 0.38f;
            }
            r.PolygonOutlined(wall, O, Ink, -width - 0.08f, 0.12f, width + 0.08f, 0.12f, width, top, -width, top);
            for (var row = 1; row < 4; row++)
                r.Capsule(-width + 0.02f, 0.12f + row * (top - 0.12f) / 4f, width - 0.02f, 0.12f + row * (top - 0.12f) / 4f, 0.01f, Raster.Shade(wall, -0.15f));
            r.Box(0f, 0.45f, 0.06f, 0.14f, 0.05f, Ink);
            r.Box(0f, top, width + 0.06f, 0.06f, 0.02f, Raster.Shade(wall, -0.1f), 0f, O, Ink);
            if (def.tier >= 3)
                r.Box(0f, top - 0.14f, width, 0.04f, 0.01f, def.id == "archer4b" ? Red : Turquoise);
            Crenellations(r, 0f, top + 0.12f, width + 0.04f, 4, Raster.Shade(wall, -0.05f));
            if (def.tier >= 3)
                Flag(r, -width + 0.02f, top + 0.16f, def.id == "archer4a" ? Turquoise : Red, def.tier >= 4 ? 1.15f : 1f);
            if (def.id == "archer4a")
                r.Star(0f, 0.72f, 0.1f, 0.045f, 8, Gold, 90f, O * 0.5f, Ink);
        }

        static void Barracks(Raster r, TowerDef def)
        {
            if (def.tier == 1)
            {
                // tent
                r.PolygonOutlined(Cream, O, Ink, -0.6f, 0.15f, 0.6f, 0.15f, 0f, 1.05f);
                r.PolygonOutlined(Red, 0f, Ink, -0.2f, 0.15f, 0.2f, 0.15f, 0f, 1.05f);
                r.PolygonOutlined(Raster.Hex(0x3A2418), 0f, Ink, -0.12f, 0.15f, 0.12f, 0.15f, 0f, 0.55f);
                Flag(r, 0f, 1.02f, Red, 0.8f);
                return;
            }

            var wall = def.tier == 2 ? Wood : Stone;
            var width = 0.58f;
            var top = 0.82f;
            if (def.tier == 2)
            {
                for (var i = 0; i < 7; i++)
                {
                    var x = -width + i * (width * 2f / 6f);
                    r.Capsule(x, 0.14f, x, top, 0.075f, Wood, O * 0.8f, Ink);
                    r.PolygonOutlined(Wood, O * 0.6f, Ink, x - 0.075f, top, x + 0.075f, top, x, top + 0.12f);
                }
            }
            else
            {
                r.Box(0f, (0.14f + top) / 2f, width, (top - 0.14f) / 2f, 0.03f, wall, 0f, O, Ink);
                Crenellations(r, 0f, top + 0.08f, width, 5, wall);
                r.Box(0f, top - 0.1f, width, 0.04f, 0.01f, def.id == "barracks4a" ? Raster.Hex(0x2E7A3A) : Turquoise);
            }
            // gate
            r.Box(0f, 0.32f, 0.2f, 0.2f, 0.18f, Raster.Hex(0x3A2418), 0f, O * 0.7f, Ink);
            r.Box(0f, 0.24f, 0.2f, 0.12f, 0.01f, Raster.Hex(0x3A2418));

            if (def.id == "barracks4b")
                KavianiBanner(r, 0.42f, top + 0.15f);
            else
                Flag(r, 0.44f, top + 0.1f, def.id == "barracks4a" ? Raster.Hex(0x2E7A3A) : Red, def.tier >= 3 ? 1.1f : 0.9f);
        }

        /// <summary>The Derafsh Kaviani: Kaveh's apron raised as Iran's banner.</summary>
        static void KavianiBanner(Raster r, float x, float y)
        {
            r.Capsule(x, y - 0.1f, x, y + 0.85f, 0.025f, WoodDark, O * 0.5f, Ink);
            r.Star(x, y + 0.92f, 0.08f, 0.035f, 4, Gold, 90f);
            r.Box(x - 0.24f, y + 0.55f, 0.22f, 0.22f, 0.02f, Purple, 0f, O * 0.6f, Ink);
            r.Star(x - 0.24f, y + 0.55f, 0.15f, 0.06f, 4, Gold, 45f);
            r.Circle(x - 0.24f, y + 0.55f, 0.04f, Red);
            for (var i = 0; i < 3; i++)
                r.Capsule(x - 0.38f + i * 0.14f, y + 0.33f, x - 0.38f + i * 0.14f, y + 0.12f, 0.025f, i == 1 ? Gold : Red);
        }

        static void Mage(Raster r, TowerDef def)
        {
            if (def.tier == 1)
            {
                r.PolygonOutlined(Stone, O, Ink, -0.3f, 0.14f, 0.3f, 0.14f, 0.2f, 0.7f, -0.2f, 0.7f);
                r.Box(0f, 0.75f, 0.36f, 0.07f, 0.03f, StoneDark, 0f, O, Ink);
                r.Box(0f, 0.88f, 0.28f, 0.08f, 0.06f, Raster.Hex(0x5A4A3A), 0f, O, Ink);
                return;
            }

            // Chahar-taq: four arches under a dome, the classic Zoroastrian fire temple
            var marble = def.id == "mage4b" ? Raster.Hex(0xEDEDF2) : Brick;
            var width = def.tier == 2 ? 0.45f : 0.55f;
            var top = def.tier == 2 ? 0.72f : 0.8f;
            r.Box(0f, (0.14f + top) / 2f, width, (top - 0.14f) / 2f, 0.02f, marble, 0f, O, Ink);
            r.Box(0f, 0.36f, width * 0.45f, 0.22f, width * 0.44f, Raster.Hex(0x2A1A10));
            r.Box(0f, 0.25f, width * 0.45f, 0.11f, 0.01f, Raster.Hex(0x2A1A10));
            r.Glow(0f, 0.3f, 0.3f, new Color(1f, 0.6f, 0.15f, 0.6f));
            var domeColor = def.id == "mage4a" ? Gold : def.tier >= 3 ? Turquoise : Raster.Shade(marble, -0.1f);
            r.Box(0f, top + 0.04f, width + 0.04f, 0.05f, 0.02f, Raster.Shade(marble, -0.15f), 0f, O, Ink);
            r.Ellipse(0f, top + 0.08f, width * 0.75f, 0.36f, domeColor, O, Ink);
            r.Box(0f, top + 0.0f, width * 0.8f, 0.06f, 0.01f, Raster.Shade(marble, -0.15f));
            if (def.tier >= 3)
                r.Box(0f, 0.18f, width, 0.04f, 0.01f, Turquoise);
            // the fire burns on top of the dome (separate animated sprite); give it a bowl
            r.Box(0f, TopY - 0.02f, 0.16f, 0.05f, 0.04f, Raster.Shade(Gold, -0.2f), 0f, O * 0.6f, Ink);
        }

        static void Artillery(Raster r, TowerDef def)
        {
            if (def.tier == 1)
            {
                // sandbag wall of the slingers
                for (var i = 0; i < 4; i++)
                    r.Ellipse(-0.42f + i * 0.28f, 0.28f, 0.16f, 0.1f, Raster.Hex(0xC2A27A), O * 0.7f, Ink);
                for (var i = 0; i < 3; i++)
                    r.Ellipse(-0.28f + i * 0.28f, 0.44f, 0.16f, 0.1f, Raster.Hex(0xB89670), O * 0.7f, Ink);
                r.Circle(-0.3f, 0.62f, 0.08f, Raster.Hex(0x8A8A8A), O * 0.6f, Ink);
                r.Circle(-0.12f, 0.6f, 0.07f, Raster.Hex(0x7A7A7A), O * 0.6f, Ink);
                return;
            }

            var big = def.tier >= 3;
            var frame = def.tier >= 3 ? WoodDark : Wood;
            if (big)
                r.Box(0f, 0.24f, 0.62f, 0.12f, 0.03f, Stone, 0f, O, Ink);
            var baseY = big ? 0.36f : 0.2f;
            r.Box(0f, baseY + 0.06f, 0.55f, 0.06f, 0.02f, frame, 0f, O, Ink);
            r.Circle(-0.38f, baseY, 0.1f, WoodDark, O, Ink);
            r.Circle(0.38f, baseY, 0.1f, WoodDark, O, Ink);
            r.PolygonOutlined(frame, O, Ink, -0.15f, baseY + 0.1f, 0.15f, baseY + 0.1f, 0.05f, baseY + 0.62f, -0.05f, baseY + 0.62f);
            // throwing arm at rest, cup up at the back
            r.Capsule(0.45f, baseY + 0.2f, -0.5f, baseY + 0.75f, 0.04f, Raster.Shade(frame, 0.15f), O, Ink);
            r.Ellipse(-0.52f, baseY + 0.8f, 0.13f, 0.08f, frame, O * 0.7f, Ink);
            if (def.id == "artillery4b")
            {
                r.Circle(-0.52f, baseY + 0.9f, 0.09f, Raster.Hex(0x6B4A2A), O * 0.6f, Ink);
                r.Glow(-0.52f, baseY + 1.0f, 0.16f, new Color(1f, 0.5f, 0.1f, 0.8f));
                for (var i = 0; i < 3; i++)
                    r.Ellipse(0.25f + i * 0.14f, baseY + 0.22f, 0.07f, 0.1f, Raster.Hex(0x7A5230), O * 0.5f, Ink);
            }
            else
            {
                r.Circle(-0.52f, baseY + 0.9f, def.id == "artillery4a" ? 0.14f : 0.1f, Raster.Hex(0x8A8A8A), O * 0.6f, Ink);
            }
            if (def.tier >= 4)
                Flag(r, 0.55f, baseY + 0.1f, def.id == "artillery4a" ? Turquoise : Red);
        }
    }
}
