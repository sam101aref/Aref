using System;
using UnityEngine;

namespace IranVsTuran.Art
{
    /// <summary>
    /// A small software painter for placeholder art. Shapes are signed distance functions in
    /// "design units" (y up); each fill touches only the shape's bounding box, anti-aliases its
    /// edge and alpha-blends over what is already there. The result becomes a Texture2D/Sprite.
    /// </summary>
    public class Raster
    {
        public delegate float Sdf(float x, float y);

        public readonly int Width;
        public readonly int Height;
        readonly Color[] pixels;
        readonly float minX;
        readonly float minY;
        /// <summary>Pixels per design unit.</summary>
        public readonly float Ppu;

        /// <summary>A canvas covering design coordinates [minX, minX + width/ppu] × [minY, …].</summary>
        public Raster(int width, int height, float minX, float minY, float ppu)
        {
            Width = width;
            Height = height;
            this.minX = minX;
            this.minY = minY;
            Ppu = ppu;
            pixels = new Color[width * height];
        }

        public float MinX { get { return minX; } }
        public float MinY { get { return minY; } }
        public float MaxX { get { return minX + Width / Ppu; } }
        public float MaxY { get { return minY + Height / Ppu; } }

        public void Clear(Color color)
        {
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;
        }

        /// <summary>Design-space coordinates of a pixel centre.</summary>
        public float X(int px) { return minX + (px + 0.5f) / Ppu; }
        public float Y(int py) { return minY + (py + 0.5f) / Ppu; }

        public Color Get(int px, int py) { return pixels[py * Width + px]; }
        public void Set(int px, int py, Color c) { pixels[py * Width + px] = c; }

        public void Blend(int px, int py, Color c, float alpha)
        {
            if (px < 0 || py < 0 || px >= Width || py >= Height || alpha <= 0f)
                return;
            var i = py * Width + px;
            var a = c.a * alpha;
            var dst = pixels[i];
            var outA = a + dst.a * (1f - a);
            if (outA <= 0f)
                return;
            pixels[i] = new Color(
                (c.r * a + dst.r * dst.a * (1f - a)) / outA,
                (c.g * a + dst.g * dst.a * (1f - a)) / outA,
                (c.b * a + dst.b * dst.a * (1f - a)) / outA,
                outA);
        }

        /// <summary>Fills where <paramref name="sdf"/> &lt; <paramref name="grow"/> inside the given bounds.</summary>
        public void Fill(Sdf sdf, float x0, float y0, float x1, float y1, Color color, float grow = 0f)
        {
            var pad = grow + 2f / Ppu;
            var px0 = Mathf.Max(0, Mathf.FloorToInt((x0 - pad - minX) * Ppu));
            var py0 = Mathf.Max(0, Mathf.FloorToInt((y0 - pad - minY) * Ppu));
            var px1 = Mathf.Min(Width - 1, Mathf.CeilToInt((x1 + pad - minX) * Ppu));
            var py1 = Mathf.Min(Height - 1, Mathf.CeilToInt((y1 + pad - minY) * Ppu));
            for (var py = py0; py <= py1; py++)
            {
                var y = Y(py);
                for (var px = px0; px <= px1; px++)
                {
                    var d = sdf(X(px), y) - grow;
                    var coverage = Mathf.Clamp01(0.5f - d * Ppu);
                    if (coverage > 0f)
                        Blend(px, py, color, coverage);
                }
            }
        }

        // ---- shapes; every shape can be drawn with an outline first ----

        public void Circle(float cx, float cy, float r, Color color, float outline = 0f, Color outlineColor = default(Color))
        {
            Sdf sdf = (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
            Draw(sdf, cx - r, cy - r, cx + r, cy + r, color, outline, outlineColor);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color color, float outline = 0f, Color outlineColor = default(Color))
        {
            var k = Mathf.Min(rx, ry);
            Sdf sdf = (x, y) =>
            {
                var nx = (x - cx) / rx;
                var ny = (y - cy) / ry;
                return (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * k;
            };
            Draw(sdf, cx - rx, cy - ry, cx + rx, cy + ry, color, outline, outlineColor);
        }

        /// <summary>A rounded rectangle centred on (cx, cy), optionally rotated (degrees).</summary>
        public void Box(float cx, float cy, float hw, float hh, float round, Color color, float angle = 0f,
            float outline = 0f, Color outlineColor = default(Color))
        {
            var rad = -angle * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            round = Mathf.Min(round, Mathf.Min(hw, hh));
            Sdf sdf = (x, y) =>
            {
                var lx = (x - cx) * cos - (y - cy) * sin;
                var ly = (x - cx) * sin + (y - cy) * cos;
                var qx = Mathf.Abs(lx) - hw + round;
                var qy = Mathf.Abs(ly) - hh + round;
                var outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - round;
            };
            var extent = Mathf.Sqrt(hw * hw + hh * hh);
            Draw(sdf, cx - extent, cy - extent, cx + extent, cy + extent, color, outline, outlineColor);
        }

        /// <summary>A line with round ends; r is half its thickness.</summary>
        public void Capsule(float ax, float ay, float bx, float by, float r, Color color, float outline = 0f,
            Color outlineColor = default(Color))
        {
            Sdf sdf = (x, y) => SegmentDistance(x, y, ax, ay, bx, by) - r;
            Draw(sdf, Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r,
                color, outline, outlineColor);
        }

        /// <summary>A tapered line: radius ra at A, rb at B.</summary>
        public void Taper(float ax, float ay, float bx, float by, float ra, float rb, Color color, float outline = 0f,
            Color outlineColor = default(Color))
        {
            Sdf sdf = (x, y) =>
            {
                var dx = bx - ax;
                var dy = by - ay;
                var lengthSq = dx * dx + dy * dy;
                var t = lengthSq <= 0f ? 0f : Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / lengthSq);
                var cx = ax + t * dx - x;
                var cy = ay + t * dy - y;
                return Mathf.Sqrt(cx * cx + cy * cy) - Mathf.Lerp(ra, rb, t);
            };
            var r = Mathf.Max(ra, rb);
            Draw(sdf, Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r,
                color, outline, outlineColor);
        }

        public void Ring(float cx, float cy, float r, float halfWidth, Color color)
        {
            Sdf sdf = (x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - halfWidth;
            Draw(sdf, cx - r - halfWidth, cy - r - halfWidth, cx + r + halfWidth, cy + r + halfWidth, color, 0f, default(Color));
        }

        /// <summary>An arc of a ring between two angles (degrees, counter-clockwise from +x).</summary>
        public void Arc(float cx, float cy, float r, float halfWidth, float fromDeg, float toDeg, Color color)
        {
            var steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(toDeg - fromDeg) / 8f));
            for (var i = 0; i < steps; i++)
            {
                var a0 = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                var a1 = Mathf.Lerp(fromDeg, toDeg, (i + 1) / (float)steps) * Mathf.Deg2Rad;
                Capsule(cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r, cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r, halfWidth, color);
            }
        }

        /// <summary>A filled polygon from x,y pairs (any winding).</summary>
        public void Polygon(Color color, params float[] xy)
        {
            PolygonOutlined(color, 0f, default(Color), xy);
        }

        public void PolygonOutlined(Color color, float outline, Color outlineColor, params float[] xy)
        {
            var n = xy.Length / 2;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (var i = 0; i < n; i++)
            {
                x0 = Mathf.Min(x0, xy[2 * i]);
                x1 = Mathf.Max(x1, xy[2 * i]);
                y0 = Mathf.Min(y0, xy[2 * i + 1]);
                y1 = Mathf.Max(y1, xy[2 * i + 1]);
            }
            Sdf sdf = (x, y) => PolygonDistance(xy, x, y);
            Draw(sdf, x0, y0, x1, y1, color, outline, outlineColor);
        }

        /// <summary>A regular star with <paramref name="points"/> points.</summary>
        public void Star(float cx, float cy, float outer, float inner, int points, Color color, float rotation = 90f,
            float outline = 0f, Color outlineColor = default(Color))
        {
            var xy = new float[points * 4];
            for (var i = 0; i < points * 2; i++)
            {
                var r = i % 2 == 0 ? outer : inner;
                var a = (rotation + i * 180f / points) * Mathf.Deg2Rad;
                xy[2 * i] = cx + Mathf.Cos(a) * r;
                xy[2 * i + 1] = cy + Mathf.Sin(a) * r;
            }
            PolygonOutlined(color, outline, outlineColor, xy);
        }

        /// <summary>A soft radial glow: full colour at the centre fading to transparent at r.</summary>
        public void Glow(float cx, float cy, float r, Color color)
        {
            var px0 = Mathf.Max(0, Mathf.FloorToInt((cx - r - minX) * Ppu));
            var py0 = Mathf.Max(0, Mathf.FloorToInt((cy - r - minY) * Ppu));
            var px1 = Mathf.Min(Width - 1, Mathf.CeilToInt((cx + r - minX) * Ppu));
            var py1 = Mathf.Min(Height - 1, Mathf.CeilToInt((cy + r - minY) * Ppu));
            for (var py = py0; py <= py1; py++)
                for (var px = px0; px <= px1; px++)
                {
                    var dx = X(px) - cx;
                    var dy = Y(py) - cy;
                    var t = 1f - Mathf.Sqrt(dx * dx + dy * dy) / r;
                    if (t > 0f)
                        Blend(px, py, color, t * t);
                }
        }

        /// <summary>Vertical gradient over the whole canvas (bottom to top).</summary>
        public void VerticalGradient(Color bottom, Color top)
        {
            for (var py = 0; py < Height; py++)
            {
                var c = Color.Lerp(bottom, top, py / (float)(Height - 1));
                for (var px = 0; px < Width; px++)
                    pixels[py * Width + px] = c;
            }
        }

        void Draw(Sdf sdf, float x0, float y0, float x1, float y1, Color color, float outline, Color outlineColor)
        {
            if (outline > 0f)
                Fill(sdf, x0, y0, x1, y1, outlineColor, outline);
            Fill(sdf, x0, y0, x1, y1, color);
        }

        public static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
        {
            var dx = bx - ax;
            var dy = by - ay;
            var lengthSq = dx * dx + dy * dy;
            var t = lengthSq <= 0f ? 0f : Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / lengthSq);
            var cx = ax + t * dx - px;
            var cy = ay + t * dy - py;
            return Mathf.Sqrt(cx * cx + cy * cy);
        }

        static float PolygonDistance(float[] xy, float px, float py)
        {
            var n = xy.Length / 2;
            var best = float.MaxValue;
            var inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = xy[2 * i], yi = xy[2 * i + 1], xj = xy[2 * j], yj = xy[2 * j + 1];
                best = Mathf.Min(best, SegmentDistance(px, py, xi, yi, xj, yj));
                if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi)
                    inside = !inside;
            }
            return inside ? -best : best;
        }

#if !ART_PREVIEW
        // ---- output ----

        public Texture2D ToTexture(string name, FilterMode filter = FilterMode.Bilinear)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
            };
            // Premultiplied edges would darken; store straight alpha but bleed colour into
            // transparent pixels so bilinear filtering does not show dark fringes.
            var output = new Color32[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
                output[i] = pixels[i];
            BleedEdges(output);
            texture.SetPixels32(output);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>A sprite whose pivot is the design-space point (pivotX, pivotY).</summary>
        public Sprite ToSprite(string name, float pivotX, float pivotY, float pixelsPerWorldUnit, Vector4 border = default(Vector4))
        {
            var texture = ToTexture(name);
            var pivot = new Vector2((pivotX - minX) * Ppu / Width, (pivotY - minY) * Ppu / Height);
            var sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), pivot, pixelsPerWorldUnit, 0,
                SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }

        void BleedEdges(Color32[] output)
        {
            for (var py = 0; py < Height; py++)
                for (var px = 0; px < Width; px++)
                {
                    var i = py * Width + px;
                    if (output[i].a != 0)
                        continue;
                    // copy the colour of an opaque neighbour, keep alpha 0
                    for (var k = 0; k < 4; k++)
                    {
                        var nx = px + (k == 0 ? 1 : k == 1 ? -1 : 0);
                        var ny = py + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
                            continue;
                        var n = pixels[ny * Width + nx];
                        if (n.a > 0.05f)
                        {
                            output[i] = new Color32((byte)(n.r * 255), (byte)(n.g * 255), (byte)(n.b * 255), 0);
                            break;
                        }
                    }
                }
        }

#endif

        public Color[] Pixels { get { return pixels; } }

        public static Color Hex(uint rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        public static Color Shade(Color c, float amount)
        {
            return amount >= 0f
                ? Color.Lerp(c, Color.white, amount)
                : Color.Lerp(c, Color.black, -amount);
        }
    }

    /// <summary>Deterministic 2D value noise for terrain colour variation.</summary>
    public class ValueNoise
    {
        readonly float[] grid;
        readonly int size;

        public ValueNoise(int seed, int size = 64)
        {
            this.size = size;
            grid = new float[size * size];
            var random = new System.Random(seed);
            for (var i = 0; i < grid.Length; i++)
                grid[i] = (float)random.NextDouble();
        }

        public float Sample(float x, float y)
        {
            var xi = Mathf.FloorToInt(x);
            var yi = Mathf.FloorToInt(y);
            var fx = x - xi;
            var fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            var a = At(xi, yi);
            var b = At(xi + 1, yi);
            var c = At(xi, yi + 1);
            var d = At(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Two octaves, 0–1.</summary>
        public float Fractal(float x, float y)
        {
            return Sample(x, y) * 0.65f + Sample(x * 2.7f + 13.1f, y * 2.7f + 7.3f) * 0.35f;
        }

        float At(int x, int y)
        {
            x = ((x % size) + size) % size;
            y = ((y % size) + size) % size;
            return grid[y * size + x];
        }
    }
}
