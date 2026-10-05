using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Art
{
    /// <summary>
    /// Paints a character from its <see cref="Look"/>, facing right, feet at design y = 0.
    /// Canvas: x −1.2…1.2, y −0.1…2.5 design units. Humans are about two units tall.
    /// </summary>
    public static class CharacterArt
    {
        public const float MinX = -1.2f;
        public const float MinY = -0.1f;
        public const float Size = 2.6f;

        static readonly Color Ink = Raster.Hex(0x2A1E14);
        static readonly Color Metal = Raster.Hex(0x9AA3AD);
        static readonly Color Wood = Raster.Hex(0x7A4E26);
        static readonly Color Ivory = Raster.Hex(0xF0E6C8);
        static readonly Color Horse = Raster.Hex(0x6B4423);
        static readonly Color Gold = Raster.Hex(0xE2B13C);

        const float O = 0.045f; // outline width

        public static Raster Paint(Look look, int pixels)
        {
            var raster = new Raster(pixels, pixels, MinX, MinY, pixels / Size);
            switch (look.body)
            {
                case Body.Div:
                    Div(raster, look, 0f);
                    break;
                case Body.Beast:
                    Beast(raster, look);
                    break;
                case Body.Winged:
                    Winged(raster, look);
                    break;
                case Body.Dragon:
                    Dragon(raster, look);
                    break;
                case Body.Snake:
                    Snake(raster, look);
                    break;
                case Body.Elephant:
                    Elephant(raster, look);
                    break;
                default:
                    if (look.mounted)
                    {
                        HorseBody(raster, look);
                        Human(raster, look, 0.5f, true);
                    }
                    else
                    {
                        Human(raster, look, 0f, false);
                    }
                    break;
            }
            return raster;
        }

        // ---------------------------------------------------------------- humans

        static void Human(Raster r, Look look, float dy, bool riding)
        {
            var main = Raster.Hex(look.main);
            var accent = Raster.Hex(look.accent);
            var skin = Raster.Hex(look.skin);
            var dark = Raster.Shade(main, -0.4f);
            var wolf = look.body == Body.Wolfman;

            if (riding)
            {
                r.Capsule(0.04f, 0.95f + dy - 0.5f, 0.12f, 0.62f, 0.085f, dark, O, Ink);
            }
            else
            {
                r.Capsule(-0.13f, 0.45f + dy, -0.17f, 0.08f + dy, 0.085f, dark, O, Ink);
                r.Capsule(0.12f, 0.45f + dy, 0.17f, 0.08f + dy, 0.085f, dark, O, Ink);
                r.Ellipse(-0.12f, 0.06f + dy, 0.13f, 0.065f, Raster.Hex(0x3A2A1A), O * 0.7f, Ink);
                r.Ellipse(0.22f, 0.06f + dy, 0.13f, 0.065f, Raster.Hex(0x3A2A1A), O * 0.7f, Ink);
            }

            if (look.cape != 0)
            {
                var cape = Raster.Hex(look.cape);
                r.PolygonOutlined(cape, O, Ink, -0.18f, 1.05f + dy, 0.12f, 1.05f + dy, -0.12f, 0.3f + dy, -0.42f, 0.24f + dy, -0.5f, 0.4f + dy);
                r.Capsule(-0.3f, 0.95f + dy, -0.4f, 0.4f + dy, 0.03f, Raster.Shade(cape, -0.2f));
            }

            if (look.weapon == Weapon.Spear || look.weapon == Weapon.Staff)
                LongWeapon(r, look, dy);

            // tunic
            r.PolygonOutlined(Raster.Shade(main, -0.12f), O, Ink,
                -0.3f, 0.4f + dy, 0.3f, 0.4f + dy, 0.25f, 0.62f + dy, -0.25f, 0.62f + dy);
            r.Box(0f, 0.76f + dy, 0.26f, 0.3f, 0.12f, main, 0f, O, Ink);
            r.Box(0f, 0.57f + dy, 0.27f, 0.05f, 0.02f, accent);
            r.Box(0f, 0.86f + dy, 0.05f, 0.18f, 0.02f, Raster.Shade(main, 0.15f));

            if (look.shield)
            {
                r.Circle(-0.26f, 0.74f + dy, 0.25f, accent, O, Ink);
                r.Ring(-0.26f, 0.74f + dy, 0.19f, 0.025f, Raster.Shade(accent, 0.35f));
                r.Circle(-0.26f, 0.74f + dy, 0.06f, Metal);
            }

            if (look.snakes)
            {
                Serpent(r, -0.14f, 1.0f + dy, -0.5f, 1.62f + dy, Raster.Hex(look.accent));
                Serpent(r, 0.14f, 1.0f + dy, 0.5f, 1.66f + dy, Raster.Hex(look.accent));
            }

            // head
            if (look.hair != 0)
            {
                var hair = Raster.Hex(look.hair);
                r.Capsule(-0.12f, 1.2f + dy, -0.24f, 0.78f + dy, 0.075f, hair, O, Ink);
                r.Circle(-0.02f, 1.27f + dy, 0.27f, hair, O, Ink);
            }
            if (look.head == Headgear.Hood)
                r.Circle(0.0f, 1.27f + dy, 0.31f, Raster.Shade(main, -0.25f), O, Ink);
            if (wolf)
                WolfHead(r, look, dy);
            else
            {
                r.Circle(0.05f, 1.25f + dy, look.head == Headgear.Hood ? 0.21f : 0.24f, skin, O, Ink);
                r.Circle(0.16f, 1.28f + dy, 0.035f, Ink);
                if (look.beard != 0)
                {
                    var beard = Raster.Hex(look.beard);
                    r.PolygonOutlined(beard, O * 0.6f, Ink, -0.08f, 1.2f + dy, 0.28f, 1.16f + dy, 0.24f, 0.98f + dy, 0.1f, 0.86f + dy, -0.02f, 0.98f + dy);
                    r.Capsule(0.12f, 1.15f + dy, 0.28f, 1.13f + dy, 0.035f, Raster.Shade(beard, 0.15f));
                }
                else if (look.head != Headgear.Hood && look.head != Headgear.None && look.hair == 0)
                    r.PolygonOutlined(Raster.Hex(0x3A2418), 0f, Ink, 0.02f, 1.12f + dy, 0.24f, 1.14f + dy, 0.2f, 1.02f + dy, 0.06f, 1.0f + dy);
            }
            DrawHeadgear(r, look, dy);

            // arm and short weapons
            r.Capsule(0.1f, 0.95f + dy, 0.3f, 0.66f + dy, 0.075f, Raster.Shade(main, -0.2f), O, Ink);
            ShortWeapon(r, look, dy);
            r.Circle(0.3f, 0.66f + dy, 0.075f, wolf ? main : skin, O * 0.8f, Ink);
        }

        static void Serpent(Raster r, float ax, float ay, float bx, float by, Color color)
        {
            const int segments = 9;
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var x = Mathf.Lerp(ax, bx, t) + Mathf.Sin(t * Mathf.PI * 2f) * 0.08f;
                var y = Mathf.Lerp(ay, by, t);
                r.Circle(x, y, Mathf.Lerp(0.07f, 0.05f, t), color, O * 0.6f, Ink);
            }
            r.Ellipse(bx + (bx > ax ? 0.06f : -0.06f), by + 0.04f, 0.1f, 0.07f, color, O * 0.6f, Ink);
            r.Circle(bx + (bx > ax ? 0.09f : -0.09f), by + 0.07f, 0.02f, Raster.Hex(0xF2D04A));
        }

        static void WolfHead(Raster r, Look look, float dy)
        {
            var fur = Raster.Hex(look.main);
            r.Polygon(Raster.Shade(fur, -0.2f), -0.1f, 1.38f + dy, -0.04f, 1.62f + dy, 0.06f, 1.42f + dy);
            r.Polygon(Raster.Shade(fur, -0.2f), 0.08f, 1.42f + dy, 0.16f, 1.64f + dy, 0.22f, 1.4f + dy);
            r.Ellipse(0.03f, 1.25f + dy, 0.23f, 0.24f, fur, O, Ink);
            r.Box(0.27f, 1.18f + dy, 0.16f, 0.08f, 0.07f, fur, -10f, O, Ink);
            r.Circle(0.42f, 1.2f + dy, 0.04f, Ink);
            r.Circle(0.14f, 1.3f + dy, 0.04f, Raster.Hex(0xF2D04A));
        }

        static void DrawHeadgear(Raster r, Look look, float dy)
        {
            var accent = Raster.Hex(look.accent);
            var main = Raster.Hex(look.main);
            var cx = 0.05f;
            var cy = 1.29f + dy;
            switch (look.head)
            {
                case Defs.Headgear.Helmet:
                case Defs.Headgear.SpikeHelm:
                case Defs.Headgear.Plume:
                    if (look.head == Defs.Headgear.Plume)
                        r.Taper(cx, cy + 0.2f, cx - 0.36f, cy + 0.42f, 0.08f, 0.03f, accent, O, Ink);
                    Dome(r, cx, cy, 0.27f, Metal);
                    r.Box(cx, cy, 0.29f, 0.04f, 0.02f, Raster.Shade(Metal, -0.2f));
                    r.Capsule(cx + 0.15f, cy, cx + 0.15f, cy - 0.15f, 0.022f, Raster.Shade(Metal, -0.2f));
                    if (look.head == Defs.Headgear.SpikeHelm)
                        r.PolygonOutlined(Metal, O * 0.7f, Ink, cx - 0.06f, cy + 0.24f, cx + 0.06f, cy + 0.24f, cx, cy + 0.46f);
                    break;
                case Defs.Headgear.Cap:
                    r.Taper(cx - 0.1f, cy + 0.08f, cx - 0.32f, cy - 0.15f, 0.07f, 0.03f, Raster.Shade(accent, -0.1f));
                    Dome(r, cx, cy - 0.02f, 0.25f, accent);
                    break;
                case Defs.Headgear.Horns:
                    r.Taper(cx - 0.12f, cy + 0.12f, cx - 0.34f, cy + 0.44f, 0.07f, 0.015f, Ivory, O * 0.7f, Ink);
                    r.Taper(cx + 0.14f, cy + 0.12f, cx + 0.3f, cy + 0.46f, 0.07f, 0.015f, Ivory, O * 0.7f, Ink);
                    break;
                case Defs.Headgear.Crown:
                    r.PolygonOutlined(Gold, O * 0.7f, Ink,
                        cx - 0.22f, cy + 0.08f, cx + 0.24f, cy + 0.08f, cx + 0.26f, cy + 0.36f, cx + 0.13f, cy + 0.22f,
                        cx + 0.01f, cy + 0.4f, cx - 0.11f, cy + 0.22f, cx - 0.24f, cy + 0.36f);
                    r.Circle(cx + 0.01f, cy + 0.17f, 0.04f, Raster.Hex(0xC0392B));
                    break;
                case Defs.Headgear.DivHelm:
                    var bone = Raster.Hex(0xEDEDED);
                    r.Taper(cx - 0.14f, cy + 0.16f, cx - 0.3f, cy + 0.4f, 0.06f, 0.015f, Ivory, O * 0.7f, Ink);
                    r.Taper(cx + 0.16f, cy + 0.16f, cx + 0.3f, cy + 0.42f, 0.06f, 0.015f, Ivory, O * 0.7f, Ink);
                    Dome(r, cx, cy, 0.29f, bone);
                    r.Circle(cx + 0.06f, cy + 0.14f, 0.04f, Raster.Hex(0x2A1A10));
                    r.Circle(cx + 0.19f, cy + 0.14f, 0.04f, Raster.Hex(0x2A1A10));
                    r.Box(cx, cy, 0.3f, 0.04f, 0.02f, Raster.Shade(bone, -0.25f));
                    break;
                case Defs.Headgear.Tiger:
                    r.Circle(cx - 0.15f, cy + 0.2f, 0.07f, main, O * 0.7f, Ink);
                    r.Circle(cx + 0.2f, cy + 0.2f, 0.07f, main, O * 0.7f, Ink);
                    Dome(r, cx, cy, 0.28f, main);
                    for (var i = 0; i < 3; i++)
                        r.Capsule(cx - 0.15f + i * 0.13f, cy + 0.04f, cx - 0.1f + i * 0.13f, cy + 0.2f, 0.022f, Raster.Hex(0x2A1A10));
                    break;
            }
        }

        static void Dome(Raster r, float cx, float cy, float radius, Color color)
        {
            const int steps = 12;
            var xy = new float[(steps + 1) * 2];
            for (var i = 0; i <= steps; i++)
            {
                var a = Mathf.PI * i / steps;
                xy[2 * i] = cx + Mathf.Cos(a) * radius;
                xy[2 * i + 1] = cy + Mathf.Sin(a) * radius;
            }
            r.PolygonOutlined(color, O, Ink, xy);
            r.Capsule(cx - radius * 0.5f, cy + radius * 0.55f, cx + radius * 0.2f, cy + radius * 0.8f, 0.03f, Raster.Shade(color, 0.35f));
        }

        static void LongWeapon(Raster r, Look look, float dy)
        {
            if (look.weapon == Weapon.Spear)
            {
                r.Capsule(0.14f, 0.15f + dy, 0.5f, 1.7f + dy, 0.03f, Wood, O * 0.6f, Ink);
                r.PolygonOutlined(Metal, O * 0.6f, Ink, 0.45f, 1.66f + dy, 0.57f, 1.69f + dy, 0.56f, 1.92f + dy);
            }
            else
            {
                r.Capsule(0.32f, 0.2f + dy, 0.38f, 1.55f + dy, 0.035f, Wood, O * 0.6f, Ink);
                var orb = Raster.Hex(look.accent);
                r.Glow(0.38f, 1.62f + dy, 0.22f, new Color(orb.r, orb.g, orb.b, 0.6f));
                r.Circle(0.38f, 1.62f + dy, 0.08f, orb, O * 0.6f, Ink);
            }
        }

        static void ShortWeapon(Raster r, Look look, float dy)
        {
            var hx = 0.3f;
            var hy = 0.66f + dy;
            switch (look.weapon)
            {
                case Weapon.Sword:
                    r.Capsule(hx, hy, hx + 0.28f, hy + 0.58f, 0.04f, Metal, O * 0.7f, Ink);
                    r.Capsule(hx - 0.08f, hy + 0.06f, hx + 0.1f, hy - 0.04f, 0.03f, Raster.Hex(look.accent), O * 0.5f, Ink);
                    break;
                case Weapon.Bow:
                    r.Arc(hx + 0.02f, hy + 0.12f, 0.42f, 0.035f, -65f, 65f, Wood);
                    var top = new Vector2(hx + 0.02f + Mathf.Cos(65f * Mathf.Deg2Rad) * 0.42f, hy + 0.12f + Mathf.Sin(65f * Mathf.Deg2Rad) * 0.42f);
                    var bottom = new Vector2(top.x, hy + 0.12f - Mathf.Sin(65f * Mathf.Deg2Rad) * 0.42f);
                    r.Capsule(top.x, top.y, bottom.x, bottom.y, 0.01f, Ivory);
                    break;
                case Weapon.Club:
                    r.Taper(hx, hy, hx + 0.26f, hy + 0.66f, 0.045f, 0.12f, Raster.Shade(Wood, -0.1f), O, Ink);
                    break;
                case Weapon.Mace:
                    r.Capsule(hx, hy, hx + 0.18f, hy + 0.48f, 0.03f, Wood, O * 0.6f, Ink);
                    r.Star(hx + 0.2f, hy + 0.54f, 0.15f, 0.1f, 8, Metal, 90f, O * 0.6f, Ink);
                    r.Circle(hx + 0.2f, hy + 0.54f, 0.08f, Raster.Shade(Metal, 0.2f));
                    break;
                case Weapon.Axe:
                    r.Capsule(hx, hy, hx + 0.2f, hy + 0.56f, 0.03f, Wood, O * 0.6f, Ink);
                    r.PolygonOutlined(Metal, O * 0.6f, Ink, hx + 0.16f, hy + 0.46f, hx + 0.42f, hy + 0.62f, hx + 0.38f, hy + 0.3f);
                    break;
            }
        }

        static void HorseBody(Raster r, Look look)
        {
            var horse = look.main == 0x1E1E1E ? Raster.Hex(0x1A1A1A) : Horse;
            var dark = Raster.Shade(horse, -0.3f);
            r.Taper(-0.58f, 0.72f, -0.86f, 0.32f, 0.08f, 0.03f, dark, O, Ink);
            r.Capsule(-0.42f, 0.5f, -0.46f, 0.05f, 0.06f, dark, O, Ink);
            r.Capsule(0.36f, 0.5f, 0.44f, 0.05f, 0.06f, dark, O, Ink);
            r.Ellipse(0f, 0.64f, 0.6f, 0.25f, horse, O, Ink);
            r.Capsule(-0.24f, 0.5f, -0.2f, 0.05f, 0.06f, horse, O, Ink);
            r.Capsule(0.22f, 0.5f, 0.26f, 0.05f, 0.06f, horse, O, Ink);
            r.Taper(0.42f, 0.75f, 0.68f, 1.08f, 0.15f, 0.1f, horse, O, Ink);
            r.Box(0.82f, 1.02f, 0.2f, 0.09f, 0.08f, horse, -32f, O, Ink);
            r.Circle(0.78f, 1.1f, 0.025f, Ink);
            r.Taper(0.5f, 0.95f, 0.66f, 1.2f, 0.05f, 0.03f, dark);
            r.Box(-0.02f, 0.88f, 0.24f, 0.07f, 0.03f, Raster.Hex(look.accent), 0f, O * 0.6f, Ink);
        }

        // ---------------------------------------------------------------- creatures

        static void Div(Raster r, Look look, float dy)
        {
            var skin = Raster.Hex(look.skin);
            var dark = Raster.Shade(skin, -0.3f);
            var accent = Raster.Hex(look.accent);
            r.Capsule(-0.22f, 0.55f + dy, -0.26f, 0.08f + dy, 0.13f, dark, O, Ink);
            r.Capsule(0.22f, 0.55f + dy, 0.26f, 0.08f + dy, 0.13f, dark, O, Ink);
            r.Capsule(-0.3f, 1.15f + dy, -0.52f, 0.8f + dy, 0.1f, dark, O, Ink);
            r.Ellipse(0f, 0.95f + dy, 0.5f, 0.5f, skin, O, Ink);
            r.Ellipse(0.08f, 0.85f + dy, 0.3f, 0.3f, Raster.Shade(skin, 0.18f));
            r.PolygonOutlined(accent, O * 0.7f, Ink, -0.42f, 0.62f + dy, 0.42f, 0.62f + dy, 0.3f, 0.32f + dy, -0.3f, 0.32f + dy);
            // head with horns, red eyes and fangs
            r.Taper(0.05f, 1.62f + dy, -0.2f, 2.0f + dy, 0.08f, 0.02f, Ivory, O * 0.7f, Ink);
            r.Taper(0.3f, 1.64f + dy, 0.48f, 2.02f + dy, 0.08f, 0.02f, Ivory, O * 0.7f, Ink);
            r.Circle(0.18f, 1.5f + dy, 0.27f, skin, O, Ink);
            r.Glow(0.3f, 1.55f + dy, 0.12f, new Color(1f, 0.3f, 0.1f, 0.8f));
            r.Circle(0.3f, 1.55f + dy, 0.045f, Raster.Hex(0xFF3A1A));
            r.Polygon(Ivory, 0.26f, 1.36f + dy, 0.32f, 1.36f + dy, 0.29f, 1.28f + dy);
            r.Polygon(Ivory, 0.36f, 1.37f + dy, 0.41f, 1.37f + dy, 0.39f, 1.3f + dy);
            // arm with club
            r.Capsule(0.3f, 1.12f + dy, 0.56f, 0.78f + dy, 0.1f, skin, O, Ink);
            if (look.weapon == Weapon.Club)
            {
                r.Taper(0.56f, 0.72f + dy, 0.86f, 1.55f + dy, 0.05f, 0.15f, Raster.Shade(Wood, -0.15f), O, Ink);
                r.Polygon(Ivory, 0.72f, 1.3f + dy, 0.66f, 1.34f + dy, 0.74f, 1.38f + dy);
                r.Polygon(Ivory, 0.9f, 1.4f + dy, 0.98f, 1.42f + dy, 0.9f, 1.47f + dy);
            }
            r.Circle(0.56f, 0.78f + dy, 0.1f, skin, O * 0.8f, Ink);
        }

        static void Beast(Raster r, Look look)
        {
            var fur = Raster.Hex(look.main);
            var mane = Raster.Hex(look.accent);
            var dark = Raster.Shade(fur, -0.25f);
            r.Taper(-0.6f, 0.75f, -0.95f, 1.0f, 0.05f, 0.03f, fur, O, Ink);
            r.Circle(-0.97f, 1.02f, 0.07f, mane, O * 0.7f, Ink);
            r.Capsule(-0.42f, 0.5f, -0.46f, 0.06f, 0.08f, dark, O, Ink);
            r.Capsule(0.32f, 0.5f, 0.38f, 0.06f, 0.08f, dark, O, Ink);
            r.Ellipse(-0.05f, 0.66f, 0.6f, 0.28f, fur, O, Ink);
            r.Capsule(-0.22f, 0.5f, -0.2f, 0.06f, 0.08f, fur, O, Ink);
            r.Capsule(0.18f, 0.5f, 0.22f, 0.06f, 0.08f, fur, O, Ink);
            r.Circle(0.52f, 0.92f, 0.38f, mane, O, Ink);
            r.Circle(0.6f, 0.9f, 0.23f, fur, O, Ink);
            r.Ellipse(0.78f, 0.84f, 0.12f, 0.09f, Raster.Shade(fur, 0.25f), O * 0.6f, Ink);
            r.Circle(0.88f, 0.87f, 0.035f, Ink);
            r.Circle(0.66f, 0.98f, 0.035f, Ink);
        }

        static void Winged(Raster r, Look look)
        {
            const float dy = 0.55f;
            var skin = Raster.Hex(look.skin);
            var wing = Raster.Hex(look.accent);
            Wing(r, -0.05f, 1.05f + dy, Raster.Shade(wing, -0.2f), 1.0f);
            r.Taper(-0.2f, 0.8f + dy, -0.6f, 0.45f + dy, 0.08f, 0.02f, skin, O, Ink);
            r.Ellipse(0.05f, 0.88f + dy, 0.26f, 0.34f, Raster.Hex(look.main), O, Ink);
            r.Capsule(0f, 0.6f + dy, -0.05f, 0.38f + dy, 0.06f, skin, O, Ink);
            r.Capsule(0.14f, 0.6f + dy, 0.18f, 0.38f + dy, 0.06f, skin, O, Ink);
            r.Taper(0.12f, 1.38f + dy, 0.0f, 1.62f + dy, 0.05f, 0.015f, Ivory, O * 0.6f, Ink);
            r.Taper(0.3f, 1.38f + dy, 0.4f, 1.62f + dy, 0.05f, 0.015f, Ivory, O * 0.6f, Ink);
            r.Circle(0.2f, 1.25f + dy, 0.19f, skin, O, Ink);
            r.Circle(0.29f, 1.28f + dy, 0.035f, Raster.Hex(0xFF3A1A));
            Wing(r, 0.05f, 1.05f + dy, wing, 0.9f);
        }

        static void Wing(Raster r, float x, float y, Color color, float scale)
        {
            r.PolygonOutlined(color, O, Ink,
                x, y + 0.05f * scale, x - 0.95f * scale, y + 0.6f * scale, x - 0.8f * scale, y + 0.15f * scale,
                x - 1.0f * scale, y - 0.05f * scale, x - 0.62f * scale, y - 0.12f * scale, x - 0.7f * scale, y - 0.38f * scale,
                x, y - 0.2f * scale);
        }

        static void Dragon(Raster r, Look look)
        {
            var scale = Raster.Hex(look.main);
            var belly = Raster.Shade(scale, 0.35f);
            var accent = Raster.Hex(look.accent);
            r.Taper(-1.15f, 0.55f, -0.55f, 0.5f, 0.03f, 0.16f, scale, O, Ink);
            Wing(r, -0.05f, 1.0f, Raster.Shade(accent, -0.2f), 0.8f);
            r.Capsule(-0.35f, 0.45f, -0.4f, 0.05f, 0.09f, Raster.Shade(scale, -0.25f), O, Ink);
            r.Capsule(0.25f, 0.45f, 0.3f, 0.05f, 0.09f, Raster.Shade(scale, -0.25f), O, Ink);
            r.Ellipse(-0.08f, 0.62f, 0.55f, 0.3f, scale, O, Ink);
            r.Ellipse(-0.02f, 0.52f, 0.38f, 0.14f, belly);
            for (var i = 0; i < 5; i++)
            {
                var sx = -0.55f + i * 0.22f;
                r.PolygonOutlined(accent, O * 0.5f, Ink, sx - 0.07f, 0.86f, sx + 0.07f, 0.86f, sx, 1.02f);
            }
            r.Taper(0.32f, 0.72f, 0.66f, 1.25f, 0.2f, 0.13f, scale, O, Ink);
            r.Box(0.9f, 1.32f, 0.26f, 0.13f, 0.08f, scale, -12f, O, Ink);
            r.Box(0.92f, 1.18f, 0.22f, 0.06f, 0.04f, belly, -20f, O * 0.6f, Ink);
            r.Taper(0.76f, 1.42f, 0.6f, 1.68f, 0.05f, 0.015f, Ivory, O * 0.6f, Ink);
            r.Circle(0.92f, 1.38f, 0.045f, Raster.Hex(0xF2D04A));
            r.Glow(1.12f, 1.22f, 0.18f, new Color(1f, 0.55f, 0.1f, 0.7f));
            r.Capsule(-0.05f, 0.45f, -0.02f, 0.05f, 0.09f, scale, O, Ink);
            r.Capsule(0.42f, 0.5f, 0.48f, 0.05f, 0.09f, scale, O, Ink);
        }

        static void Snake(Raster r, Look look)
        {
            var body = Raster.Hex(look.main);
            var accent = Raster.Hex(look.accent);
            const int segments = 18;
            for (var i = 0; i < segments; i++)
            {
                var t = i / (segments - 1f);
                var x = -0.9f + 1.45f * t;
                var y = 0.2f + 0.14f * Mathf.Sin(t * Mathf.PI * 2.4f) + Mathf.Max(0f, t - 0.75f) * 2.2f;
                var radius = Mathf.Lerp(0.05f, 0.16f, Mathf.Min(1f, t * 1.4f));
                r.Circle(x, y, radius, body, O, Ink);
                if (i % 3 == 1)
                    r.Circle(x, y + radius * 0.2f, radius * 0.45f, accent);
            }
            r.Ellipse(0.72f, 0.82f, 0.2f, 0.14f, body, O, Ink);
            r.Circle(0.78f, 0.88f, 0.035f, Raster.Hex(0xF2D04A));
            r.Capsule(0.9f, 0.78f, 1.02f, 0.76f, 0.012f, Raster.Hex(0xC0392B));
        }

        static void Elephant(Raster r, Look look)
        {
            var skin = Raster.Hex(look.main);
            var dark = Raster.Shade(skin, -0.25f);
            var cloth = Raster.Hex(look.accent);
            r.Taper(-0.78f, 1.0f, -0.92f, 0.6f, 0.04f, 0.02f, dark, O, Ink);
            r.Capsule(-0.5f, 0.6f, -0.52f, 0.06f, 0.12f, dark, O, Ink);
            r.Capsule(0.32f, 0.6f, 0.34f, 0.06f, 0.12f, dark, O, Ink);
            r.Ellipse(-0.1f, 0.96f, 0.7f, 0.48f, skin, O, Ink);
            r.Capsule(-0.24f, 0.6f, -0.24f, 0.06f, 0.12f, skin, O, Ink);
            r.Capsule(0.12f, 0.6f, 0.12f, 0.06f, 0.12f, skin, O, Ink);
            r.Box(-0.12f, 1.0f, 0.5f, 0.28f, 0.06f, cloth, 0f, O, Ink);
            r.Box(-0.12f, 0.78f, 0.5f, 0.04f, 0.02f, Gold);
            r.Box(-0.15f, 1.52f, 0.36f, 0.16f, 0.05f, Raster.Shade(cloth, -0.15f), 0f, O, Ink);
            r.PolygonOutlined(Gold, O * 0.6f, Ink, -0.55f, 1.66f, 0.25f, 1.66f, -0.15f, 1.95f);
            r.Circle(0.6f, 1.18f, 0.32f, skin, O, Ink);
            r.Ellipse(0.42f, 1.2f, 0.18f, 0.27f, dark, O, Ink);
            r.Taper(0.82f, 1.08f, 0.96f, 0.36f, 0.11f, 0.055f, skin, O, Ink);
            r.Taper(0.76f, 0.95f, 1.06f, 0.82f, 0.05f, 0.02f, Ivory, O * 0.6f, Ink);
            r.Circle(0.72f, 1.28f, 0.035f, Ink);
        }
    }
}
