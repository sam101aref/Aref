using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Art
{
    /// <summary>
    /// Paints the campaign map: an old parchment map of Iran and Turan divided by the Jeyhun,
    /// with the Caspian, the Alborz and Damavand, Mazandaran's forests and the road that links
    /// every battle. Design space x 0…16, y 0…9; level positions are 0–1 fractions of it.
    /// </summary>
    public static class WorldMapArt
    {
        public const float W = 16f;
        public const float H = 9f;
        static readonly Color Ink = Raster.Hex(0x4A3220);

        public static Raster Paint(int width, int height)
        {
            var r = new Raster(width, height, 0f, 0f, width / W);
            var noise = new ValueNoise(1404);
            var random = new System.Random(77);

            // parchment, with Turan (east of the river) in a colder tint
            for (var py = 0; py < r.Height; py++)
                for (var px = 0; px < r.Width; px++)
                {
                    var x = r.X(px);
                    var y = r.Y(py);
                    var n = noise.Fractal(x * 0.8f, y * 0.8f);
                    var parchment = Color.Lerp(Raster.Hex(0xE2CB98), Raster.Hex(0xF0DFB4), n);
                    var turan = Mathf.Clamp01((x - RiverX(y)) * 2f);
                    var c = Color.Lerp(parchment, Raster.Hex(0xC9C0A8), turan * 0.7f);
                    r.Set(px, py, c);
                }

            // desert of central Iran
            for (var i = 0; i < 10; i++)
                r.Ellipse(3.5f + (float)random.NextDouble() * 5f, 0.8f + (float)random.NextDouble() * 3f, 1.2f, 0.45f, new Color(0.92f, 0.78f, 0.5f, 0.35f));

            // seas
            r.Ellipse(4.2f, 8.9f, 2.6f, 0.95f, Raster.Hex(0x7FB0C8), 0.06f, Ink);
            r.Ellipse(1.2f, 0.1f, 2.2f, 0.7f, Raster.Hex(0x7FB0C8), 0.06f, Ink);
            r.Ellipse(15.2f, 1.2f, 1.6f, 0.6f, Raster.Hex(0x8FB8C8), 0.06f, Ink);

            // the Jeyhun
            for (var y = -0.2f; y < H + 0.2f; y += 0.05f)
                r.Circle(RiverX(y), y, 0.11f, Raster.Hex(0x5A94B8));

            // Mazandaran's forests
            for (var i = 0; i < 45; i++)
            {
                var x = 0.6f + (float)random.NextDouble() * 4.2f;
                var y = 5.2f + (float)random.NextDouble() * 2.6f;
                r.Circle(x, y + 0.15f, 0.16f, Raster.Hex(0x4E7A3A), 0.03f, Ink);
            }

            // the Alborz, and Damavand above all
            for (var i = 0; i < 14; i++)
            {
                var x = 1.5f + i * 0.55f;
                var y = 7.2f + (i % 2) * 0.25f;
                Mountain(r, x, y, 0.35f, 0.55f);
            }
            Mountain(r, 5.9f, 7.55f, 0.6f, 1.0f);

            // Turan: steppe grass and Gang-Dezh far in the north-east
            for (var i = 0; i < 30; i++)
            {
                var x = 11f + (float)random.NextDouble() * 4.6f;
                var y = 0.6f + (float)random.NextDouble() * 6f;
                if (x > RiverX(y) + 0.4f)
                    r.Capsule(x, y, x + 0.08f, y + 0.18f, 0.02f, Raster.Hex(0x8A8A5A));
            }
            for (var i = 0; i < 6; i++)
                Mountain(r, 12.8f + i * 0.5f, 7.9f + (i % 2) * 0.2f, 0.3f, 0.45f);

            Road(r);
            Compass(r, 14.8f, 1.1f);

            // frame
            for (var i = 0; i < 2; i++)
            {
                var inset = 0.12f + i * 0.12f;
                r.Capsule(inset, inset, W - inset, inset, 0.03f, Ink);
                r.Capsule(inset, H - inset, W - inset, H - inset, 0.03f, Ink);
                r.Capsule(inset, inset, inset, H - inset, 0.03f, Ink);
                r.Capsule(W - inset, inset, W - inset, H - inset, 0.03f, Ink);
            }
            return r;
        }

        public static float RiverX(float y)
        {
            return 10.6f - y * 0.08f + Mathf.Sin(y * 1.3f) * 0.35f + (y < 4f ? (4f - y) * 0.12f : 0f);
        }

        static void Mountain(Raster r, float x, float y, float halfWidth, float height)
        {
            r.PolygonOutlined(Raster.Hex(0xB09A78), 0.03f, Ink, x - halfWidth, y, x + halfWidth, y, x, y + height);
            r.Polygon(Raster.Hex(0x8A7458), x, y, x + halfWidth, y, x, y + height);
            r.Polygon(Raster.Hex(0xF4F0E8), x - halfWidth * 0.3f, y + height * 0.7f, x + halfWidth * 0.3f, y + height * 0.7f, x, y + height);
        }

        /// <summary>A dotted road through the levels in campaign order.</summary>
        static void Road(Raster r)
        {
            for (var i = 0; i < LevelDefs.All.Count - 1; i++)
            {
                var a = Position(LevelDefs.All[i]);
                var b = Position(LevelDefs.All[i + 1]);
                var length = (b - a).magnitude;
                var dots = Mathf.Max(2, Mathf.RoundToInt(length / 0.22f));
                for (var d = 1; d < dots; d++)
                {
                    var p = a + (b - a) * (d / (float)dots);
                    r.Circle(p.x, p.y, 0.045f, new Color(0.45f, 0.2f, 0.15f, 0.8f));
                }
            }
        }

        public static Vector2 Position(LevelDef level)
        {
            return new Vector2(level.mapPosition.x * W, level.mapPosition.y * H);
        }

        static void Compass(Raster r, float x, float y)
        {
            r.Circle(x, y, 0.55f, new Color(0.95f, 0.88f, 0.7f, 1f), 0.04f, Ink);
            r.Star(x, y, 0.5f, 0.12f, 4, Raster.Hex(0x8A3A2A), 90f);
            r.Star(x, y, 0.32f, 0.08f, 4, Raster.Hex(0x4A3220), 45f);
            r.Circle(x, y, 0.06f, Raster.Hex(0xE2B13C));
        }
    }
}
