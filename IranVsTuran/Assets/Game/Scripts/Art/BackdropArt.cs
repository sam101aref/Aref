using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Art
{
    /// <summary>
    /// Paints the cutscene backdrops in a flat, layered "storybook" style.
    /// Design space: x 0…16, y 0…9.
    /// </summary>
    public static class BackdropArt
    {
        const float W = 16f;
        const float H = 9f;
        static readonly Color Ink = Raster.Hex(0x2A1E14);

        public static Raster Paint(Backdrop backdrop, int width, int height)
        {
            var r = new Raster(width, height, 0f, 0f, width / W);
            var random = new System.Random((int)backdrop * 31 + 7);
            switch (backdrop)
            {
                case Backdrop.Palace: Palace(r); break;
                case Backdrop.River: River(r, random); break;
                case Backdrop.Steppe: Steppe(r, random); break;
                case Backdrop.Battlefield: Battlefield(r, random); break;
                case Backdrop.Forest: Forest(r, random); break;
                case Backdrop.Cave: Cave(r, random); break;
                case Backdrop.Castle: Castle(r, random); break;
                case Backdrop.Camp: Camp(r, random); break;
                case Backdrop.Fortress: Fortress(r, random); break;
                case Backdrop.Mountain: Mountain(r, random); break;
                default: Damavand(r, random); break;
            }
            return r;
        }

        static void Sky(Raster r, uint bottom, uint top)
        {
            r.VerticalGradient(Raster.Hex(bottom), Raster.Hex(top));
        }

        /// <summary>A rolling silhouette from the left edge to the right edge.</summary>
        static void Hills(Raster r, float baseY, float amplitude, float frequency, Color color, int seed)
        {
            var noise = new ValueNoise(seed, 32);
            const int steps = 48;
            var xy = new float[(steps + 3) * 2];
            for (var i = 0; i <= steps; i++)
            {
                var x = W * i / steps;
                xy[2 * i] = x;
                xy[2 * i + 1] = baseY + (noise.Fractal(x * frequency, 3.3f) - 0.5f) * 2f * amplitude;
            }
            xy[2 * (steps + 1)] = W;
            xy[2 * (steps + 1) + 1] = -1f;
            xy[2 * (steps + 2)] = 0f;
            xy[2 * (steps + 2) + 1] = -1f;
            r.Polygon(color, xy);
        }

        static void Peak(Raster r, float x, float baseY, float halfWidth, float height, Color rock, bool snow)
        {
            r.Polygon(rock, x - halfWidth, baseY, x + halfWidth, baseY, x, baseY + height);
            r.Polygon(Raster.Shade(rock, -0.15f), x, baseY, x + halfWidth, baseY, x, baseY + height);
            if (snow)
                r.Polygon(Color.white, x - halfWidth * 0.3f, baseY + height * 0.7f, x + halfWidth * 0.3f, baseY + height * 0.7f, x, baseY + height);
        }

        static void Sun(Raster r, float x, float y, float radius, uint color)
        {
            r.Glow(x, y, radius * 3f, Raster.Hex(color, 0.5f));
            r.Circle(x, y, radius, Raster.Hex(color));
        }

        static void Palace(Raster r)
        {
            Sky(r, 0xF6D8A0, 0xE59A5A);
            Sun(r, 12.5f, 6.8f, 0.8f, 0xFFF0C0);
            r.Box(8f, 1.2f, 8.2f, 1.4f, 0f, Raster.Hex(0xC8B08A));
            // stairs
            for (var i = 0; i < 4; i++)
                r.Box(8f, 0.35f + i * 0.3f, 6f - i * 0.6f, 0.15f, 0f, Raster.Hex(0xB89C74), 0f, 0.03f, Ink);
            // Persepolis columns with bull capitals
            for (var i = 0; i < 7; i++)
            {
                var x = 1.6f + i * 2.13f;
                r.Box(x, 4.4f, 0.28f, 2.4f, 0.04f, Raster.Hex(0xE6D5B5), 0f, 0.05f, Ink);
                for (var f = -1; f <= 1; f++)
                    r.Capsule(x + f * 0.13f, 2.2f, x + f * 0.13f, 6.6f, 0.02f, Raster.Hex(0xC8B490));
                r.Box(x, 6.95f, 0.6f, 0.18f, 0.05f, Raster.Hex(0xD8C49E), 0f, 0.05f, Ink);
                r.Circle(x - 0.45f, 7.2f, 0.22f, Raster.Hex(0xD8C49E), 0.05f, Ink);
                r.Circle(x + 0.45f, 7.2f, 0.22f, Raster.Hex(0xD8C49E), 0.05f, Ink);
            }
            r.Box(8f, 7.55f, 8.2f, 0.25f, 0f, Raster.Hex(0xC8B08A), 0f, 0.05f, Ink);
            // royal banners
            Banner(r, 4.8f, 2.4f, 0x6A2A7A);
            Banner(r, 11.2f, 2.4f, 0x6A2A7A);
        }

        static void Banner(Raster r, float x, float y, uint color)
        {
            r.Capsule(x, y, x, y + 3.2f, 0.05f, Raster.Hex(0x5E3A1C), 0.04f, Ink);
            r.Box(x + 0.55f, y + 2.5f, 0.5f, 0.6f, 0.03f, Raster.Hex(color), 0f, 0.04f, Ink);
            r.Star(x + 0.55f, y + 2.55f, 0.32f, 0.13f, 4, Raster.Hex(0xE2B13C), 45f);
            r.Circle(x + 0.55f, y + 2.55f, 0.09f, Raster.Hex(0xB0302A));
            r.Star(x, y + 3.35f, 0.15f, 0.06f, 4, Raster.Hex(0xE2B13C));
        }

        static void River(Raster r, System.Random random)
        {
            Sky(r, 0xCFE8F0, 0x7FB8D8);
            Sun(r, 3f, 7.2f, 0.6f, 0xFFF6D8);
            Hills(r, 5.2f, 0.4f, 0.4f, Raster.Hex(0x8AA67A), 11);
            // the far (Turanian) bank with dark tents
            r.Box(8f, 4.6f, 8f, 0.35f, 0f, Raster.Hex(0x9A9E6A));
            for (var i = 0; i < 6; i++)
            {
                var x = 2f + i * 2.5f + (float)random.NextDouble();
                r.Polygon(Raster.Hex(0x3A3030), x - 0.35f, 4.7f, x + 0.35f, 4.7f, x, 5.3f);
                r.Capsule(x, 5.3f, x, 5.7f, 0.02f, Raster.Hex(0x2A2020));
                r.Polygon(Raster.Hex(0xB0302A), x, 5.7f, x + 0.25f, 5.6f, x, 5.5f);
            }
            // the Jeyhun
            r.Box(8f, 3.3f, 8f, 1.05f, 0f, Raster.Hex(0x4A90B8));
            for (var i = 0; i < 30; i++)
            {
                var x = (float)random.NextDouble() * W;
                var y = 2.5f + (float)random.NextDouble() * 1.7f;
                r.Capsule(x, y, x + 0.5f, y, 0.025f, Raster.Hex(0xA8D8EA, 0.8f));
            }
            Hills(r, 1.8f, 0.3f, 0.5f, Raster.Hex(0x7FA34C), 12);
            for (var i = 0; i < 25; i++)
            {
                var x = (float)random.NextDouble() * W;
                r.Taper(x, 1.6f, x + 0.1f, 2.4f + (float)random.NextDouble() * 0.5f, 0.03f, 0.01f, Raster.Hex(0x5E7A3A));
            }
        }

        static void Steppe(Raster r, System.Random random)
        {
            Sky(r, 0xF8E2B0, 0x8FC0E0);
            Sun(r, 12f, 7f, 0.7f, 0xFFF4D0);
            Peak(r, 3f, 4f, 2.6f, 2.6f, Raster.Hex(0x8A8EA8), true);
            Peak(r, 6.5f, 4f, 2.2f, 2f, Raster.Hex(0x9A9EB4), true);
            Hills(r, 4f, 0.5f, 0.3f, Raster.Hex(0xB8C27A), 21);
            Hills(r, 2.8f, 0.5f, 0.4f, Raster.Hex(0xC9B86A), 22);
            Hills(r, 1.4f, 0.4f, 0.6f, Raster.Hex(0xD8B85A), 23);
            for (var i = 0; i < 40; i++)
            {
                var x = (float)random.NextDouble() * W;
                var y = (float)random.NextDouble() * 1.2f;
                r.Taper(x, y, x - 0.1f, y + 0.4f, 0.03f, 0.005f, Raster.Hex(0xB89A4A));
            }
        }

        static void Battlefield(Raster r, System.Random random)
        {
            Sky(r, 0xE8A070, 0x6A3A4A);
            Sun(r, 8f, 4.6f, 1.1f, 0xFFD090);
            Hills(r, 3.6f, 0.3f, 0.3f, Raster.Hex(0x6A4A4A), 31);
            for (var i = 0; i < 40; i++)
            {
                var x = (float)random.NextDouble() * W;
                var h = 0.8f + (float)random.NextDouble() * 1.2f;
                r.Capsule(x, 3.2f, x + 0.15f, 3.2f + h, 0.025f, Raster.Hex(0x3A2A2A));
            }
            Hills(r, 2.4f, 0.35f, 0.5f, Raster.Hex(0x4A3434), 32);
            for (var i = 0; i < 6; i++)
                r.Glow((float)random.NextDouble() * W, 2.5f + (float)random.NextDouble() * 2f, 1.5f, new Color(0.5f, 0.45f, 0.45f, 0.35f));
            Hills(r, 1.0f, 0.25f, 0.6f, Raster.Hex(0x3A2828), 33);
            // two fallen banners
            r.Capsule(3f, 0.4f, 4.5f, 2.2f, 0.06f, Raster.Hex(0x3A2418), 0.04f, Ink);
            r.Polygon(Raster.Hex(0x6A2A7A), 4.5f, 2.2f, 5.3f, 2.1f, 4.8f, 1.6f);
            r.Capsule(12.5f, 0.4f, 11.6f, 2.4f, 0.06f, Raster.Hex(0x3A2418), 0.04f, Ink);
            r.Polygon(Raster.Hex(0x1E1E1E), 11.6f, 2.4f, 10.9f, 2.3f, 11.3f, 1.8f);
        }

        static void Forest(Raster r, System.Random random)
        {
            Sky(r, 0x5A7A5A, 0x1E2E2A);
            r.Circle(12.5f, 7.3f, 0.6f, Raster.Hex(0xE8F0D0));
            for (var layer = 0; layer < 3; layer++)
            {
                var color = Raster.Shade(Raster.Hex(0x2E4A2E), -0.2f + layer * 0.15f);
                var baseY = 4.5f - layer * 1.6f;
                for (var i = 0; i < 14; i++)
                {
                    var x = (float)random.NextDouble() * W;
                    var h = 2.2f + (float)random.NextDouble() * 1.5f;
                    r.Polygon(color, x - 0.8f, baseY, x + 0.8f, baseY, x, baseY + h);
                }
                r.Box(8f, baseY - 1f, 8f, 1f, 0f, color);
                r.Glow(8f, baseY + 0.2f, 9f, new Color(0.7f, 0.8f, 0.7f, 0.12f));
            }
        }

        static void Cave(Raster r, System.Random random)
        {
            r.Clear(Raster.Hex(0x2A2420));
            r.Ellipse(8f, 2.8f, 5.5f, 4.2f, Raster.Hex(0x14100E));
            r.Glow(8f, 1.6f, 4f, new Color(1f, 0.45f, 0.15f, 0.35f));
            for (var i = 0; i < 16; i++)
            {
                var x = (float)random.NextDouble() * W;
                var len = 0.8f + (float)random.NextDouble() * 1.8f;
                r.Polygon(Raster.Hex(0x4A3E36), x - 0.3f, 9.1f, x + 0.3f, 9.1f, x, 9.1f - len);
            }
            for (var i = 0; i < 10; i++)
            {
                var x = (float)random.NextDouble() * W;
                var len = 0.5f + (float)random.NextDouble() * 1.2f;
                r.Polygon(Raster.Hex(0x3A302A), x - 0.35f, 0f, x + 0.35f, 0f, x, len);
            }
            // eyes in the dark
            r.Glow(6.5f, 3.6f, 0.3f, new Color(1f, 0.2f, 0.1f, 0.9f));
            r.Glow(7.3f, 3.6f, 0.3f, new Color(1f, 0.2f, 0.1f, 0.9f));
            r.Box(8f, 0.3f, 8f, 0.4f, 0f, Raster.Hex(0x3A302A));
        }

        static void Castle(Raster r, System.Random random)
        {
            Sky(r, 0xD8ECF4, 0x6AA6D2);
            Sun(r, 3f, 7.4f, 0.55f, 0xFFF6D8);
            Hills(r, 2.6f, 0.5f, 0.3f, Raster.Hex(0xA8B86A), 41);
            // the White Castle on its hill
            r.Ellipse(9.5f, 2.2f, 4.8f, 1.8f, Raster.Hex(0x9AA85A));
            var white = Raster.Hex(0xF2EEE4);
            r.Box(9.5f, 4.4f, 3f, 1.1f, 0.05f, white, 0f, 0.06f, Ink);
            for (var i = 0; i < 4; i++)
            {
                var x = 6.7f + i * 1.87f;
                r.Box(x, 5.2f, 0.45f, 2f, 0.05f, white, 0f, 0.06f, Ink);
                for (var c = -1; c <= 1; c++)
                    r.Box(x + c * 0.3f, 7.3f, 0.1f, 0.14f, 0.02f, white, 0f, 0.04f, Ink);
                r.Box(x, 5.8f, 0.1f, 0.25f, 0.08f, Raster.Hex(0x3A3A4A));
            }
            r.Box(9.5f, 3.75f, 0.45f, 0.45f, 0.4f, Raster.Hex(0x5A3A22), 0f, 0.05f, Ink);
            r.Capsule(10.43f, 7.4f, 10.43f, 8.6f, 0.04f, Raster.Hex(0x5E3A1C));
            r.Polygon(Raster.Hex(0x6A2A7A), 10.43f, 8.6f, 11.3f, 8.35f, 10.43f, 8.1f);
            Hills(r, 1.0f, 0.3f, 0.6f, Raster.Hex(0x8A9A4A), 42);
        }

        static void Camp(Raster r, System.Random random)
        {
            Sky(r, 0x3A3A5A, 0x0E1022);
            for (var i = 0; i < 60; i++)
                r.Circle((float)random.NextDouble() * W, 4f + (float)random.NextDouble() * 5f, 0.025f + (float)random.NextDouble() * 0.03f, Color.white);
            r.Circle(13f, 7.5f, 0.55f, Raster.Hex(0xF2EED8));
            Hills(r, 3f, 0.4f, 0.3f, Raster.Hex(0x22223A), 51);
            for (var i = 0; i < 7; i++)
            {
                var x = 1.2f + i * 2.3f;
                var y = 2.2f + (i % 2) * 0.5f;
                r.Polygon(Raster.Hex(0x4A3A3A), x - 0.9f, y, x + 0.9f, y, x, y + 1.3f);
                r.Polygon(Raster.Hex(0x2A2020), x - 0.2f, y, x + 0.2f, y, x, y + 0.6f);
            }
            r.Box(8f, 0.9f, 8f, 1.1f, 0f, Raster.Hex(0x1A1A2A));
            r.Glow(8f, 1.6f, 3f, new Color(1f, 0.55f, 0.15f, 0.55f));
            IconArt.Flame(r, 8f, 1.2f, 0.9f);
        }

        static void Fortress(Raster r, System.Random random)
        {
            Sky(r, 0x5A3A3A, 0x1A1418);
            for (var i = 0; i < 5; i++)
                r.Glow((float)random.NextDouble() * W, 6f + (float)random.NextDouble() * 3f, 3f, new Color(0.25f, 0.2f, 0.22f, 0.6f));
            Hills(r, 2.4f, 0.6f, 0.3f, Raster.Hex(0x2A2224), 61);
            var stone = Raster.Hex(0x1E1A1C);
            r.Box(8f, 4f, 4.5f, 1.8f, 0f, stone);
            for (var i = 0; i < 5; i++)
            {
                var x = 4f + i * 2f;
                var h = i == 2 ? 3.6f : 2.7f;
                r.Box(x, 2.2f + h, 0.55f, h, 0f, stone);
                r.Polygon(stone, x - 0.7f, 2.2f + 2f * h, x + 0.7f, 2.2f + 2f * h, x, 2.2f + 2f * h + 0.9f);
                r.Glow(x, 3.5f + h * 0.8f, 0.35f, new Color(1f, 0.3f, 0.1f, 0.8f));
                r.Box(x, 3.5f + h * 0.8f, 0.08f, 0.15f, 0.04f, Raster.Hex(0xFF7A3A));
            }
            Hills(r, 1f, 0.3f, 0.5f, Raster.Hex(0x14100E), 62);
        }

        static void Mountain(Raster r, System.Random random)
        {
            Sky(r, 0xE8F2F8, 0x7AAAD0);
            Peak(r, 4f, 2.5f, 4f, 5.2f, Raster.Hex(0x7A8098), true);
            Peak(r, 11f, 2.5f, 4.5f, 4.4f, Raster.Hex(0x8A90A8), true);
            Peak(r, 7.5f, 2.5f, 3f, 3.2f, Raster.Hex(0x9AA0B8), true);
            Hills(r, 2.4f, 0.4f, 0.4f, Raster.Hex(0xE4ECF2), 71);
            for (var i = 0; i < 12; i++)
            {
                var x = (float)random.NextDouble() * W;
                var y = 0.5f + (float)random.NextDouble() * 1.4f;
                r.Polygon(Raster.Hex(0x2E5A4A), x - 0.35f, y, x + 0.35f, y, x, y + 1.1f);
            }
        }

        static void Damavand(Raster r, System.Random random)
        {
            Sky(r, 0xF2B07A, 0x5A4A7A);
            Sun(r, 2.5f, 7.6f, 0.5f, 0xFFE8C0);
            // the great cone with its snow cap and a thread of smoke
            r.Polygon(Raster.Hex(0x6A5A62), 1.5f, 1.8f, 14.5f, 1.8f, 9.2f, 7.4f, 6.8f, 7.4f);
            r.Polygon(Raster.Hex(0x5A4A52), 8f, 1.8f, 14.5f, 1.8f, 9.2f, 7.4f, 8f, 7.4f);
            r.Polygon(Color.white, 5.4f, 6.1f, 10.6f, 6.1f, 9.2f, 7.4f, 6.8f, 7.4f);
            for (var i = 0; i < 5; i++)
                r.Glow(8f + i * 0.4f, 7.8f + i * 0.35f, 0.6f + i * 0.15f, new Color(0.8f, 0.8f, 0.85f, 0.4f));
            Hills(r, 1.6f, 0.4f, 0.5f, Raster.Hex(0x4A3A3A), 81);
            for (var i = 0; i < 6; i++)
            {
                var x = (float)random.NextDouble() * W;
                r.Glow(x, 0.8f, 0.6f, new Color(1f, 0.4f, 0.05f, 0.4f));
            }
        }
    }
}
