using UnityEngine;

namespace IranVsTuran.Art
{
    public enum Icon
    {
        Coin,
        Gem,
        Heart,
        Skull,
        Star,
        StarEmpty,
        Lock,
        Play,
        Pause,
        Fast,
        Gear,
        Close,
        Back,
        Check,
        Sword,
        Shield,
        Flame,
        Arrow,
        Boulder,
        Crown,
        Scroll,
        Book,
        Chest,
        Ad,
        Plus,
        Horn,
        Flag,
        Sell,
        Upgrade,
        Bow,
        Helmet,
        Hourglass,
        Sound,
        Music,
        Globe,
        Elixir,
        Feather,
        Jar,
        Treasure,
    }

    /// <summary>
    /// Paints UI icons and effect sprites. Icons live in a −1…1 square; they are drawn in full
    /// colour (not tinted), with a dark outline so they read on any background.
    /// </summary>
    public static class IconArt
    {
        static readonly Color Ink = Raster.Hex(0x2A1E14);
        static readonly Color Gold = Raster.Hex(0xF2C14E);
        static readonly Color GoldDark = Raster.Hex(0xB07F1A);
        static readonly Color White = Color.white;
        const float O = 0.07f;

        public static Raster Paint(Icon icon, int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            switch (icon)
            {
                case Icon.Coin:
                    r.Circle(0f, 0f, 0.78f, GoldDark, O, Ink);
                    r.Circle(0f, 0.05f, 0.66f, Gold);
                    r.Ring(0f, 0.05f, 0.48f, 0.05f, GoldDark);
                    r.Star(0f, 0.05f, 0.32f, 0.14f, 8, GoldDark);
                    break;
                case Icon.Gem:
                    var gem = Raster.Hex(0x3FC6C9);
                    r.PolygonOutlined(gem, O, Ink, -0.75f, 0.25f, -0.4f, 0.7f, 0.4f, 0.7f, 0.75f, 0.25f, 0f, -0.8f);
                    r.Polygon(Raster.Shade(gem, 0.45f), -0.4f, 0.7f, 0.4f, 0.7f, 0.2f, 0.25f, -0.2f, 0.25f);
                    r.Polygon(Raster.Shade(gem, -0.25f), 0.2f, 0.25f, 0.75f, 0.25f, 0f, -0.8f);
                    r.Polygon(Raster.Shade(gem, 0.15f), -0.75f, 0.25f, -0.2f, 0.25f, 0f, -0.8f);
                    break;
                case Icon.Heart:
                    var red = Raster.Hex(0xE04848);
                    r.Circle(-0.36f, 0.25f, 0.42f, red, O, Ink);
                    r.Circle(0.36f, 0.25f, 0.42f, red, O, Ink);
                    r.PolygonOutlined(red, O, Ink, -0.76f, 0.12f, 0.76f, 0.12f, 0f, -0.8f);
                    r.Circle(-0.36f, 0.25f, 0.42f, red);
                    r.Circle(0.36f, 0.25f, 0.42f, red);
                    r.Polygon(red, -0.76f, 0.15f, 0.76f, 0.15f, 0f, -0.78f);
                    r.Circle(-0.4f, 0.38f, 0.14f, Raster.Shade(red, 0.5f));
                    break;
                case Icon.Skull:
                    var bone = Raster.Hex(0xEDE6D6);
                    r.Circle(0f, 0.15f, 0.62f, bone, O, Ink);
                    r.Box(0f, -0.5f, 0.36f, 0.25f, 0.08f, bone, 0f, O, Ink);
                    r.Circle(0f, 0.15f, 0.6f, bone);
                    r.Circle(-0.25f, 0.1f, 0.17f, Ink);
                    r.Circle(0.25f, 0.1f, 0.17f, Ink);
                    r.Polygon(Ink, -0.07f, -0.2f, 0.07f, -0.2f, 0f, -0.05f);
                    break;
                case Icon.Star:
                    r.Star(0f, -0.05f, 0.9f, 0.4f, 5, Gold, 90f, O, Ink);
                    r.Star(-0.1f, 0.05f, 0.35f, 0.16f, 5, Raster.Shade(Gold, 0.45f), 90f);
                    break;
                case Icon.StarEmpty:
                    r.Star(0f, -0.05f, 0.9f, 0.4f, 5, new Color(0f, 0f, 0f, 0.45f), 90f, O, new Color(0f, 0f, 0f, 0.6f));
                    break;
                case Icon.Lock:
                    r.Arc(0f, 0.2f, 0.36f, 0.1f, 0f, 180f, Raster.Hex(0x8A8A8A));
                    r.Box(0f, -0.3f, 0.6f, 0.45f, 0.1f, Gold, 0f, O, Ink);
                    r.Circle(0f, -0.22f, 0.11f, Ink);
                    r.Box(0f, -0.4f, 0.04f, 0.12f, 0.02f, Ink);
                    break;
                case Icon.Play:
                    r.PolygonOutlined(White, O, Ink, -0.45f, 0.7f, 0.7f, 0f, -0.45f, -0.7f);
                    break;
                case Icon.Pause:
                    r.Box(-0.32f, 0f, 0.18f, 0.65f, 0.06f, White, 0f, O, Ink);
                    r.Box(0.32f, 0f, 0.18f, 0.65f, 0.06f, White, 0f, O, Ink);
                    break;
                case Icon.Fast:
                    r.PolygonOutlined(White, O, Ink, -0.85f, 0.6f, 0.05f, 0f, -0.85f, -0.6f);
                    r.PolygonOutlined(White, O, Ink, -0.05f, 0.6f, 0.85f, 0f, -0.05f, -0.6f);
                    break;
                case Icon.Gear:
                    r.Star(0f, 0f, 0.82f, 0.62f, 8, White, 0f, O, Ink);
                    r.Circle(0f, 0f, 0.55f, White);
                    r.Circle(0f, 0f, 0.24f, Ink);
                    break;
                case Icon.Close:
                    r.Capsule(-0.55f, -0.55f, 0.55f, 0.55f, 0.15f, White, O, Ink);
                    r.Capsule(-0.55f, 0.55f, 0.55f, -0.55f, 0.15f, White, O, Ink);
                    r.Capsule(-0.55f, -0.55f, 0.55f, 0.55f, 0.15f, White);
                    break;
                case Icon.Back:
                    r.Capsule(-0.5f, 0f, 0.6f, 0f, 0.15f, White, O, Ink);
                    r.PolygonOutlined(White, O, Ink, -0.75f, 0f, -0.15f, 0.6f, -0.15f, -0.6f);
                    r.Capsule(-0.3f, 0f, 0.6f, 0f, 0.15f, White);
                    break;
                case Icon.Check:
                    r.Capsule(-0.6f, 0f, -0.15f, -0.5f, 0.15f, White, O, Ink);
                    r.Capsule(-0.15f, -0.5f, 0.65f, 0.55f, 0.15f, White, O, Ink);
                    r.Capsule(-0.6f, 0f, -0.15f, -0.5f, 0.15f, White);
                    break;
                case Icon.Sword:
                    r.Capsule(-0.55f, -0.55f, 0.6f, 0.6f, 0.1f, Raster.Hex(0xC9D0D8), O, Ink);
                    r.Capsule(-0.55f, -0.15f, -0.15f, -0.55f, 0.08f, Gold, O, Ink);
                    r.Capsule(-0.65f, -0.65f, -0.45f, -0.45f, 0.08f, Raster.Hex(0x7A4E26), O, Ink);
                    break;
                case Icon.Shield:
                    var blue = Raster.Hex(0x2E5A8B);
                    r.PolygonOutlined(blue, O, Ink, -0.7f, 0.7f, 0.7f, 0.7f, 0.65f, -0.1f, 0f, -0.85f, -0.65f, -0.1f);
                    r.PolygonOutlined(Gold, 0f, Ink, -0.15f, 0.7f, 0.15f, 0.7f, 0.15f, -0.6f, 0f, -0.75f, -0.15f, -0.6f);
                    break;
                case Icon.Flame:
                    Flame(r, 0f, -0.8f, 1.5f);
                    break;
                case Icon.Arrow:
                case Icon.Bow:
                    r.Arc(-0.1f, 0f, 0.75f, 0.09f, -70f, 70f, Raster.Hex(0x8A5A2E));
                    r.Capsule(0.15f, -0.7f, 0.15f, 0.7f, 0.025f, White);
                    r.Capsule(-0.7f, 0f, 0.65f, 0f, 0.05f, Raster.Hex(0xC9A227), O * 0.6f, Ink);
                    r.PolygonOutlined(Raster.Hex(0xC9D0D8), O * 0.6f, Ink, 0.6f, 0.15f, 0.9f, 0f, 0.6f, -0.15f);
                    break;
                case Icon.Boulder:
                    r.Circle(0f, 0f, 0.7f, Raster.Hex(0x8A8A8A), O, Ink);
                    r.Circle(-0.2f, 0.2f, 0.18f, Raster.Hex(0xA8A8A8));
                    r.Circle(0.25f, -0.25f, 0.12f, Raster.Hex(0x6A6A6A));
                    break;
                case Icon.Crown:
                    r.PolygonOutlined(Gold, O, Ink, -0.75f, -0.5f, 0.75f, -0.5f, 0.85f, 0.55f, 0.4f, 0.05f, 0f, 0.7f, -0.4f, 0.05f, -0.85f, 0.55f);
                    r.Circle(0f, -0.15f, 0.13f, Raster.Hex(0xC0392B));
                    r.Circle(-0.45f, -0.25f, 0.09f, Raster.Hex(0x3FC6C9));
                    r.Circle(0.45f, -0.25f, 0.09f, Raster.Hex(0x3FC6C9));
                    break;
                case Icon.Scroll:
                    var paper = Raster.Hex(0xF2E2B8);
                    r.Box(0f, 0f, 0.55f, 0.7f, 0.05f, paper, 0f, O, Ink);
                    r.Capsule(-0.7f, 0.7f, 0.7f, 0.7f, 0.12f, Raster.Hex(0xD8C090), O, Ink);
                    r.Capsule(-0.7f, -0.7f, 0.7f, -0.7f, 0.12f, Raster.Hex(0xD8C090), O, Ink);
                    for (var i = 0; i < 4; i++)
                        r.Capsule(-0.35f, 0.35f - i * 0.22f, 0.35f, 0.35f - i * 0.22f, 0.03f, Raster.Hex(0x8A6A3A));
                    break;
                case Icon.Book:
                    r.Box(0f, 0f, 0.7f, 0.75f, 0.08f, Raster.Hex(0x7A2A2A), 0f, O, Ink);
                    r.Box(0.08f, 0f, 0.55f, 0.65f, 0.04f, Raster.Hex(0xF2E2B8));
                    r.Box(-0.08f, 0f, 0.55f, 0.65f, 0.04f, Raster.Hex(0x9A3A3A));
                    r.Star(-0.08f, 0.05f, 0.28f, 0.12f, 8, Gold);
                    break;
                case Icon.Chest:
                case Icon.Treasure:
                    r.Box(0f, -0.25f, 0.8f, 0.45f, 0.06f, Raster.Hex(0x8A5A2E), 0f, O, Ink);
                    r.Box(0f, 0.3f, 0.8f, 0.25f, 0.2f, Raster.Hex(0x9C6A36), 0f, O, Ink);
                    r.Box(0f, 0.05f, 0.82f, 0.06f, 0.02f, Gold);
                    r.Box(0f, -0.05f, 0.12f, 0.16f, 0.03f, Gold, 0f, O * 0.6f, Ink);
                    if (icon == Icon.Treasure)
                    {
                        r.Circle(-0.35f, 0.55f, 0.18f, Gold, O * 0.6f, Ink);
                        r.Circle(0.1f, 0.62f, 0.18f, Gold, O * 0.6f, Ink);
                    }
                    break;
                case Icon.Ad:
                    r.Box(0f, 0.05f, 0.85f, 0.6f, 0.15f, Raster.Hex(0x3A3A4A), 0f, O, Ink);
                    r.Box(0f, 0.05f, 0.7f, 0.45f, 0.08f, Raster.Hex(0x6FCF97));
                    r.PolygonOutlined(White, O * 0.6f, Ink, -0.18f, 0.3f, 0.28f, 0.05f, -0.18f, -0.2f);
                    r.Capsule(-0.3f, -0.72f, 0.3f, -0.72f, 0.07f, Raster.Hex(0x3A3A4A));
                    break;
                case Icon.Plus:
                    r.Capsule(0f, -0.6f, 0f, 0.6f, 0.17f, White, O, Ink);
                    r.Capsule(-0.6f, 0f, 0.6f, 0f, 0.17f, White, O, Ink);
                    r.Capsule(0f, -0.6f, 0f, 0.6f, 0.17f, White);
                    break;
                case Icon.Horn:
                    r.Taper(-0.7f, -0.35f, 0.5f, 0.35f, 0.08f, 0.32f, Raster.Hex(0xE8D8B0), O, Ink);
                    r.Ellipse(0.55f, 0.38f, 0.15f, 0.33f, Raster.Hex(0x8A6A3A), O * 0.6f, Ink);
                    r.Capsule(-0.4f, -0.15f, 0.2f, 0.2f, 0.03f, GoldDark);
                    break;
                case Icon.Flag:
                    r.Capsule(-0.5f, -0.85f, -0.5f, 0.8f, 0.07f, Raster.Hex(0x5E3A1C), O, Ink);
                    r.PolygonOutlined(Raster.Hex(0xB0302A), O, Ink, -0.45f, 0.8f, 0.7f, 0.45f, -0.45f, 0.05f);
                    break;
                case Icon.Sell:
                    r.Circle(0f, 0f, 0.75f, GoldDark, O, Ink);
                    r.Circle(0f, 0.04f, 0.62f, Gold);
                    r.Capsule(-0.3f, 0f, 0.3f, 0f, 0.1f, Ink);
                    break;
                case Icon.Upgrade:
                    r.PolygonOutlined(Raster.Hex(0x6FCF97), O, Ink, 0f, 0.85f, 0.7f, 0.1f, 0.3f, 0.1f, 0.3f, -0.75f, -0.3f, -0.75f, -0.3f, 0.1f, -0.7f, 0.1f);
                    break;
                case Icon.Helmet:
                    r.Arc(0f, -0.1f, 0.55f, 0.18f, 0f, 180f, Raster.Hex(0x9AA3AD));
                    r.Circle(0f, -0.1f, 0.5f, Raster.Hex(0x9AA3AD), O, Ink);
                    r.Box(0f, -0.5f, 0.6f, 0.1f, 0.03f, Raster.Hex(0x7A838D), 0f, O, Ink);
                    r.Taper(0f, 0.35f, -0.5f, 0.75f, 0.12f, 0.05f, Raster.Hex(0xB0302A), O * 0.6f, Ink);
                    break;
                case Icon.Hourglass:
                    r.PolygonOutlined(Raster.Hex(0xF2E2B8), O, Ink, -0.5f, 0.7f, 0.5f, 0.7f, 0.08f, 0f, 0.5f, -0.7f, -0.5f, -0.7f, -0.08f, 0f);
                    r.Polygon(Gold, -0.3f, -0.6f, 0.3f, -0.6f, 0f, -0.25f);
                    r.Capsule(-0.6f, 0.75f, 0.6f, 0.75f, 0.08f, Raster.Hex(0x8A5A2E));
                    r.Capsule(-0.6f, -0.75f, 0.6f, -0.75f, 0.08f, Raster.Hex(0x8A5A2E));
                    break;
                case Icon.Sound:
                    r.PolygonOutlined(White, O, Ink, -0.75f, 0.25f, -0.35f, 0.25f, 0.15f, 0.65f, 0.15f, -0.65f, -0.35f, -0.25f, -0.75f, -0.25f);
                    r.Arc(0.2f, 0f, 0.35f, 0.06f, -50f, 50f, White);
                    r.Arc(0.2f, 0f, 0.6f, 0.06f, -50f, 50f, White);
                    break;
                case Icon.Music:
                    r.Capsule(-0.2f, -0.45f, -0.2f, 0.6f, 0.07f, White, O * 0.6f, Ink);
                    r.Capsule(0.55f, -0.3f, 0.55f, 0.75f, 0.07f, White, O * 0.6f, Ink);
                    r.Capsule(-0.2f, 0.6f, 0.55f, 0.75f, 0.12f, White, O * 0.6f, Ink);
                    r.Ellipse(-0.38f, -0.5f, 0.25f, 0.18f, White, O, Ink);
                    r.Ellipse(0.37f, -0.35f, 0.25f, 0.18f, White, O, Ink);
                    break;
                case Icon.Globe:
                    r.Circle(0f, 0f, 0.78f, Raster.Hex(0x3A8ACF), O, Ink);
                    r.Capsule(0f, -0.74f, 0f, 0.74f, 0.04f, White);
                    r.Arc(-0.9f, 0f, 1.05f, 0.04f, -40f, 40f, White);
                    r.Arc(0.9f, 0f, 1.05f, 0.04f, 140f, 220f, White);
                    r.Capsule(-0.75f, 0f, 0.75f, 0f, 0.04f, White);
                    r.Arc(0f, -0.9f, 1.15f, 0.04f, 50f, 130f, White);
                    break;
                case Icon.Elixir:
                    r.Circle(0f, -0.25f, 0.55f, Raster.Hex(0xE0F0F0), O, Ink);
                    r.Box(0f, 0.45f, 0.18f, 0.25f, 0.05f, Raster.Hex(0xE0F0F0), 0f, O, Ink);
                    r.Circle(0f, -0.3f, 0.45f, Raster.Hex(0xE04888));
                    r.Box(0f, 0.75f, 0.24f, 0.1f, 0.04f, Raster.Hex(0x8A5A2E), 0f, O * 0.6f, Ink);
                    r.Circle(-0.15f, -0.15f, 0.12f, Raster.Shade(Raster.Hex(0xE04888), 0.5f));
                    break;
                case Icon.Feather:
                    r.Ellipse(0.05f, 0.05f, 0.3f, 0.8f, Raster.Hex(0x3FA89A), O, Ink);
                    r.Ellipse(0.05f, 0.15f, 0.18f, 0.55f, Raster.Hex(0xF2C14E));
                    r.Ellipse(0.05f, 0.3f, 0.08f, 0.25f, Raster.Hex(0x7A2A9A));
                    r.Capsule(0.05f, -0.9f, 0.05f, 0.6f, 0.03f, Ink);
                    break;
                case Icon.Jar:
                    r.Ellipse(0f, -0.2f, 0.55f, 0.58f, Raster.Hex(0x9C6A36), O, Ink);
                    r.Box(0f, 0.45f, 0.22f, 0.15f, 0.05f, Raster.Hex(0x9C6A36), 0f, O, Ink);
                    r.Box(0f, -0.2f, 0.56f, 0.07f, 0.02f, Raster.Hex(0x6A3A1A));
                    Flame(r, 0f, 0.45f, 0.5f);
                    break;
            }
            return r;
        }

        public static void Flame(Raster r, float x, float y, float size)
        {
            r.PolygonOutlined(Raster.Hex(0xE8562A), 0.05f * size, Ink,
                x - 0.4f * size, y + 0.35f * size, x - 0.25f * size, y + 0.75f * size, x - 0.08f * size, y + 0.6f * size,
                x, y + 1.05f * size, x + 0.15f * size, y + 0.65f * size, x + 0.28f * size, y + 0.85f * size,
                x + 0.4f * size, y + 0.35f * size, x + 0.2f * size, y + 0.02f * size, x - 0.2f * size, y + 0.02f * size);
            r.Ellipse(x, y + 0.32f * size, 0.22f * size, 0.3f * size, Raster.Hex(0xF5B53A));
            r.Ellipse(x, y + 0.24f * size, 0.1f * size, 0.15f * size, Raster.Hex(0xFFF0A0));
        }

        // ---- effects (white sprites are tinted at runtime) ----

        public static Raster SolidCircle(int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Circle(0f, 0f, 0.96f, White);
            return r;
        }

        public static Raster SoftCircle(int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Glow(0f, 0f, 1f, White);
            return r;
        }

        public static Raster RingSprite(int pixels, float halfWidth)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Circle(0f, 0f, 0.97f, new Color(1f, 1f, 1f, 0.18f));
            r.Ring(0f, 0f, 0.97f - halfWidth, halfWidth, White);
            return r;
        }

        /// <summary>Rounded rectangle for 9-sliced UI; white so Image.color tints it.</summary>
        public static Raster Panel(int pixels, float radius, float border)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            var rad = radius * 2f / pixels;
            var b = border * 2f / pixels;
            r.Box(0f, 0f, 1f - 1f / pixels, 1f - 1f / pixels, rad, new Color(0.72f, 0.72f, 0.72f, 1f));
            r.Box(0f, b * 0.6f, 1f - b, 1f - b * 1.6f, Mathf.Max(0f, rad - b), White);
            return r;
        }

        public static Raster ArrowProjectile(int pixels)
        {
            var r = new Raster(pixels, pixels / 4, -1f, -0.25f, pixels / 2f);
            r.Capsule(-0.8f, 0f, 0.6f, 0f, 0.035f, Raster.Hex(0x8A5A2E));
            r.PolygonOutlined(Raster.Hex(0xC9D0D8), 0.02f, Ink, 0.55f, 0.1f, 0.92f, 0f, 0.55f, -0.1f);
            r.Polygon(Raster.Hex(0xEDE6D6), -0.85f, 0f, -0.6f, 0.12f, -0.5f, 0f);
            r.Polygon(Raster.Hex(0xEDE6D6), -0.85f, 0f, -0.6f, -0.12f, -0.5f, 0f);
            return r;
        }

        public static Raster Fireball(int pixels, uint core, uint glow)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Glow(0f, 0f, 1f, Raster.Hex(glow, 0.9f));
            r.Circle(0f, 0f, 0.45f, Raster.Hex(glow));
            r.Circle(0.08f, 0.08f, 0.28f, Raster.Hex(core));
            return r;
        }

        public static Raster BoulderSprite(int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Circle(0f, 0f, 0.8f, Raster.Hex(0x8A8A8A), 0.1f, Ink);
            r.Circle(-0.25f, 0.25f, 0.2f, Raster.Hex(0xA8A8A8));
            r.Circle(0.3f, -0.2f, 0.15f, Raster.Hex(0x6A6A6A));
            return r;
        }

        public static Raster JarSprite(int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Glow(0f, 0.2f, 1f, new Color(1f, 0.5f, 0.1f, 0.7f));
            r.Ellipse(0f, -0.15f, 0.55f, 0.6f, Raster.Hex(0x9C6A36), 0.1f, Ink);
            Flame(r, 0f, 0.25f, 0.6f);
            return r;
        }

        public static Raster FlameSprite(int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Glow(0f, -0.1f, 0.9f, new Color(1f, 0.55f, 0.1f, 0.6f));
            Flame(r, 0f, -0.85f, 1.5f);
            return r;
        }

        public static Raster Spark(int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Glow(0f, 0f, 0.8f, White);
            r.Star(0f, 0f, 0.95f, 0.18f, 4, White, 45f);
            return r;
        }

        public static Raster Smoke(int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -1f, pixels / 2f);
            r.Glow(-0.3f, -0.1f, 0.7f, new Color(1f, 1f, 1f, 0.8f));
            r.Glow(0.3f, 0f, 0.7f, new Color(1f, 1f, 1f, 0.8f));
            r.Glow(0f, 0.3f, 0.7f, new Color(1f, 1f, 1f, 0.8f));
            return r;
        }

        public static Raster RallyFlag(int pixels)
        {
            var r = new Raster(pixels, pixels, -1f, -0.2f, pixels / 2f);
            r.Ellipse(-0.4f, -0.02f, 0.3f, 0.1f, new Color(0f, 0f, 0f, 0.3f));
            r.Capsule(-0.4f, 0f, -0.4f, 1.6f, 0.06f, Raster.Hex(0x5E3A1C), 0.06f, Ink);
            r.PolygonOutlined(Raster.Hex(0x6A2A7A), 0.06f, Ink, -0.35f, 1.6f, 0.7f, 1.3f, -0.35f, 0.95f);
            r.Star(0f, 1.3f, 0.18f, 0.08f, 4, Gold);
            return r;
        }
    }
}
