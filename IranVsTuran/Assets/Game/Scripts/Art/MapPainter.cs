using System.Collections.Generic;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Art
{
    /// <summary>
    /// Paints a battle's background: themed ground, the roads, scenery that keeps clear of roads
    /// and tower plots, the enemy camp where roads enter and Iran's gate where they leave.
    /// Covers 24 × 14.4 world units so any phone or tablet aspect ratio is filled.
    /// </summary>
    public static class MapPainter
    {
        public const float HalfWidth = 12f;
        public const float HalfHeight = 7.2f;
        public const float RoadHalfWidth = 0.55f;
        public const float ViewHalfWidth = 9.6f;
        public const float ViewHalfHeight = 5.4f;

        static readonly Color Ink = Raster.Hex(0x2A1E14);

        class Palette
        {
            public Color ground;
            public Color ground2;
            public Color road;
            public Color roadEdge;
        }

        static Palette For(Theme theme)
        {
            switch (theme)
            {
                case Theme.Forest: return P(0x4F7D3A, 0x3D6A2E, 0xA88A5A, 0x6E5636);
                case Theme.Cave: return P(0x5E544A, 0x4A4139, 0x8F806C, 0x5C5044);
                case Theme.Desert: return P(0xE3C98E, 0xD1B077, 0xBF9866, 0x9C7A4C);
                case Theme.Snow: return P(0xE9EFF3, 0xD2DDE6, 0xBDAE94, 0x8F806A);
                case Theme.Dark: return P(0x5F5E4C, 0x4B4A3D, 0x8E7E5E, 0x5E5240);
                case Theme.Volcanic: return P(0x6C5C52, 0x564840, 0x9E8572, 0x6A5444);
                default: return P(0x93B65A, 0x7FA34C, 0xDDBD82, 0xA88A55);
            }
        }

        static Palette P(uint g, uint g2, uint road, uint edge)
        {
            return new Palette { ground = Raster.Hex(g), ground2 = Raster.Hex(g2), road = Raster.Hex(road), roadEdge = Raster.Hex(edge) };
        }

        public static Raster Paint(LevelDef level, float ppu)
        {
            var width = Mathf.RoundToInt(HalfWidth * 2f * ppu);
            var height = Mathf.RoundToInt(HalfHeight * 2f * ppu);
            var r = new Raster(width, height, -HalfWidth, -HalfHeight, ppu);
            var palette = For(level.theme);
            var noise = new ValueNoise(level.index * 7919 + 17);
            var random = new System.Random(level.index * 104729 + 3);

            // ground
            for (var py = 0; py < height; py++)
                for (var px = 0; px < width; px++)
                {
                    var x = r.X(px);
                    var y = r.Y(py);
                    var n = noise.Fractal(x * 0.45f + 50f, y * 0.45f + 50f);
                    var c = Color.Lerp(palette.ground2, palette.ground, Mathf.SmoothStep(0.25f, 0.75f, n));
                    var speck = noise.Sample(x * 6f, y * 6f);
                    c = Raster.Shade(c, (speck - 0.5f) * 0.08f);
                    r.Set(px, py, c);
                }

            if (level.id == "l1")
                River(r, random);
            if (level.theme == Theme.Desert)
                for (var i = 0; i < 9; i++)
                    r.Ellipse(Rand(random, -11f, 11f), Rand(random, -6.5f, 6.5f), Rand(random, 2f, 3.5f), Rand(random, 0.5f, 0.9f),
                        new Color(1f, 0.95f, 0.8f, 0.25f));

            var roadDistance = RoadDistance(r, level);
            PaintRoad(r, roadDistance, palette, noise);

            var slots = new List<Vector2>();
            foreach (var slot in level.slots)
                slots.Add(new Vector2(slot.x, slot.y));
            Scenery(r, level, random, roadDistance, slots);
            Camps(r, level);
            Vignette(r);
            return r;
        }

        static float Rand(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        /// <summary>Distance from each pixel to the nearest road centre line (capped at 1.2 units).</summary>
        static float[] RoadDistance(Raster r, LevelDef level)
        {
            const float reach = 1.2f;
            var field = new float[r.Width * r.Height];
            for (var i = 0; i < field.Length; i++)
                field[i] = reach;

            for (var p = 0; p < level.PathCount; p++)
            {
                var path = level.Path(p);
                for (var s = 0; s < path.Length - 1; s++)
                {
                    var a = path[s];
                    var b = path[s + 1];
                    var px0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - reach - r.MinX) * r.Ppu));
                    var px1 = Mathf.Min(r.Width - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + reach - r.MinX) * r.Ppu));
                    var py0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - reach - r.MinY) * r.Ppu));
                    var py1 = Mathf.Min(r.Height - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + reach - r.MinY) * r.Ppu));
                    for (var py = py0; py <= py1; py++)
                        for (var px = px0; px <= px1; px++)
                        {
                            var d = Raster.SegmentDistance(r.X(px), r.Y(py), a.x, a.y, b.x, b.y);
                            var i = py * r.Width + px;
                            if (d < field[i])
                                field[i] = d;
                        }
                }
            }
            return field;
        }

        static void PaintRoad(Raster r, float[] distance, Palette palette, ValueNoise noise)
        {
            for (var py = 0; py < r.Height; py++)
                for (var px = 0; px < r.Width; px++)
                {
                    var d = distance[py * r.Width + px];
                    if (d >= 1.0f)
                        continue;
                    var x = r.X(px);
                    var y = r.Y(py);
                    var c = r.Get(px, py);
                    if (d < RoadHalfWidth + 0.15f)
                    {
                        var shadow = Mathf.Clamp01(1f - (d - RoadHalfWidth) / 0.15f);
                        c = Color.Lerp(c, Raster.Shade(c, -0.25f), shadow * 0.6f);
                    }
                    if (d < RoadHalfWidth + 0.05f)
                    {
                        var dirt = Raster.Shade(palette.road, (noise.Sample(x * 3f + 9f, y * 3f) - 0.5f) * 0.18f);
                        var t = Mathf.Clamp01((d - (RoadHalfWidth - 0.12f)) / 0.12f);
                        var roadColor = Color.Lerp(dirt, palette.roadEdge, t);
                        var coverage = Mathf.Clamp01((RoadHalfWidth + 0.05f - d) * r.Ppu);
                        c = Color.Lerp(c, roadColor, coverage);
                        // ruts and pebbles
                        if (d < RoadHalfWidth - 0.1f && noise.Sample(x * 9f, y * 9f) > 0.82f)
                            c = Raster.Shade(c, -0.12f);
                    }
                    r.Set(px, py, c);
                }
        }

        static void River(Raster r, System.Random random)
        {
            var water = Raster.Hex(0x4A90B8);
            var foam = Raster.Hex(0xA8D8EA);
            for (var y = -7.5f; y < 7.5f; y += 0.08f)
            {
                var x = -7.6f + Mathf.Sin(y * 0.6f) * 0.5f;
                r.Circle(x, y, 1.05f, Raster.Hex(0xC8B68A));
            }
            for (var y = -7.5f; y < 7.5f; y += 0.08f)
            {
                var x = -7.6f + Mathf.Sin(y * 0.6f) * 0.5f;
                r.Circle(x, y, 0.85f, water);
            }
            for (var i = 0; i < 40; i++)
            {
                var y = Rand(random, -7f, 7f);
                var x = -7.6f + Mathf.Sin(y * 0.6f) * 0.5f + Rand(random, -0.5f, 0.5f);
                r.Capsule(x, y, x + 0.05f, y + 0.35f, 0.025f, new Color(foam.r, foam.g, foam.b, 0.6f));
            }
        }

        // ------------------------------------------------------------ scenery

        static void Scenery(Raster r, LevelDef level, System.Random random, float[] roadDistance, List<Vector2> slots)
        {
            var placed = new List<Vector3>();
            var attempts = 0;
            while (placed.Count < 85 && attempts++ < 1500)
            {
                var x = Rand(random, -HalfWidth + 0.3f, HalfWidth - 0.3f);
                var y = Rand(random, -HalfHeight + 0.3f, HalfHeight - 0.3f);
                var px = Mathf.Clamp(Mathf.RoundToInt((x - r.MinX) * r.Ppu), 0, r.Width - 1);
                var py = Mathf.Clamp(Mathf.RoundToInt((y - r.MinY) * r.Ppu), 0, r.Height - 1);
                if (roadDistance[py * r.Width + px] < 1.0f)
                    continue;
                if (level.id == "l1" && Mathf.Abs(x - (-7.6f + Mathf.Sin(y * 0.6f) * 0.5f)) < 1.3f)
                    continue;
                var clear = true;
                foreach (var slot in slots)
                    if ((slot - new Vector2(x, y)).sqrMagnitude < 0.85f * 0.85f)
                        clear = false;
                foreach (var other in placed)
                    if ((new Vector2(other.x, other.y) - new Vector2(x, y)).sqrMagnitude < 0.55f * 0.55f)
                        clear = false;
                // keep the playable middle a little emptier than the edges
                var inView = Mathf.Abs(x) < ViewHalfWidth - 0.5f && Mathf.Abs(y) < ViewHalfHeight - 0.5f;
                if (!clear || (inView && random.NextDouble() < 0.35))
                    continue;
                placed.Add(new Vector3(x, y, (float)random.NextDouble()));
            }

            // draw back to front
            placed.Sort((a, b) => b.y.CompareTo(a.y));
            foreach (var p in placed)
                Prop(r, level.theme, p.x, p.y, p.z, random);
        }

        static void Prop(Raster r, Theme theme, float x, float y, float pick, System.Random random)
        {
            var scale = 0.8f + pick * 0.5f;
            switch (theme)
            {
                case Theme.Steppe:
                    if (pick < 0.35f) Tree(r, x, y, scale, Raster.Hex(0x5E8C3A));
                    else if (pick < 0.6f) Bush(r, x, y, scale, Raster.Hex(0x6E9C40));
                    else if (pick < 0.8f) Rock(r, x, y, scale * 0.8f, Raster.Hex(0xA9A08E));
                    else Flowers(r, x, y, random);
                    break;
                case Theme.Forest:
                    if (pick < 0.5f) Pine(r, x, y, scale * 1.1f, Raster.Hex(0x2E5A2A), false);
                    else if (pick < 0.75f) Tree(r, x, y, scale * 1.1f, Raster.Hex(0x3A6A2E));
                    else if (pick < 0.9f) Bush(r, x, y, scale, Raster.Hex(0x4A7A34));
                    else Mushroom(r, x, y);
                    break;
                case Theme.Cave:
                    if (pick < 0.45f) Rock(r, x, y, scale, Raster.Hex(0x7A6E62));
                    else if (pick < 0.75f) Stalagmite(r, x, y, scale);
                    else if (pick < 0.9f) Crystal(r, x, y, scale);
                    else Bones(r, x, y);
                    break;
                case Theme.Desert:
                    if (pick < 0.3f) Palm(r, x, y, scale);
                    else if (pick < 0.65f) Rock(r, x, y, scale * 0.8f, Raster.Hex(0xC2A070));
                    else if (pick < 0.85f) Bush(r, x, y, scale * 0.7f, Raster.Hex(0x9AA055));
                    else Pillar(r, x, y, scale);
                    break;
                case Theme.Snow:
                    if (pick < 0.55f) Pine(r, x, y, scale * 1.1f, Raster.Hex(0x2E5A4A), true);
                    else Rock(r, x, y, scale, Raster.Hex(0x8A8E96), true);
                    break;
                case Theme.Dark:
                    if (pick < 0.4f) DeadTree(r, x, y, scale);
                    else if (pick < 0.7f) Rock(r, x, y, scale, Raster.Hex(0x6A665A));
                    else if (pick < 0.85f) Spikes(r, x, y);
                    else Pillar(r, x, y, scale);
                    break;
                case Theme.Volcanic:
                    if (pick < 0.45f) Rock(r, x, y, scale, Raster.Hex(0x4A403A));
                    else if (pick < 0.75f) Lava(r, x, y, random);
                    else DeadTree(r, x, y, scale);
                    break;
            }
        }

        static void Shadow(Raster r, float x, float y, float w)
        {
            r.Ellipse(x, y - 0.02f, w, w * 0.35f, new Color(0f, 0f, 0f, 0.22f));
        }

        static void Tree(Raster r, float x, float y, float s, Color leaf)
        {
            Shadow(r, x, y, 0.45f * s);
            r.Capsule(x, y, x, y + 0.45f * s, 0.07f * s, Raster.Hex(0x6A4426), 0.035f, Ink);
            r.Circle(x - 0.2f * s, y + 0.55f * s, 0.26f * s, Raster.Shade(leaf, -0.1f), 0.035f, Ink);
            r.Circle(x + 0.2f * s, y + 0.55f * s, 0.26f * s, Raster.Shade(leaf, -0.05f), 0.035f, Ink);
            r.Circle(x, y + 0.78f * s, 0.32f * s, leaf, 0.035f, Ink);
            r.Circle(x - 0.08f * s, y + 0.86f * s, 0.12f * s, Raster.Shade(leaf, 0.25f));
        }

        static void Pine(Raster r, float x, float y, float s, Color leaf, bool snow)
        {
            Shadow(r, x, y, 0.38f * s);
            r.Capsule(x, y, x, y + 0.3f * s, 0.06f * s, Raster.Hex(0x5A3A20), 0.03f, Ink);
            for (var i = 0; i < 3; i++)
            {
                var by = y + (0.2f + i * 0.28f) * s;
                var w = (0.42f - i * 0.1f) * s;
                var c = Raster.Shade(leaf, i * 0.08f);
                r.PolygonOutlined(c, 0.035f, Ink, x - w, by, x + w, by, x, by + 0.5f * s);
                if (snow)
                    r.Polygon(Color.white, x - w * 0.35f, by + 0.33f * s, x + w * 0.35f, by + 0.33f * s, x, by + 0.5f * s);
            }
        }

        static void Bush(Raster r, float x, float y, float s, Color leaf)
        {
            Shadow(r, x, y, 0.35f * s);
            r.Circle(x - 0.15f * s, y + 0.12f * s, 0.16f * s, Raster.Shade(leaf, -0.1f), 0.03f, Ink);
            r.Circle(x + 0.15f * s, y + 0.12f * s, 0.16f * s, leaf, 0.03f, Ink);
            r.Circle(x, y + 0.22f * s, 0.18f * s, Raster.Shade(leaf, 0.1f), 0.03f, Ink);
        }

        static void Rock(Raster r, float x, float y, float s, Color stone, bool snow = false)
        {
            Shadow(r, x, y, 0.36f * s);
            r.Ellipse(x, y + 0.12f * s, 0.32f * s, 0.2f * s, stone, 0.035f, Ink);
            r.Ellipse(x - 0.08f * s, y + 0.18f * s, 0.14f * s, 0.07f * s, Raster.Shade(stone, 0.2f));
            if (snow)
                r.Ellipse(x, y + 0.26f * s, 0.24f * s, 0.07f * s, Color.white);
        }

        static void Flowers(Raster r, float x, float y, System.Random random)
        {
            for (var i = 0; i < 5; i++)
            {
                var fx = x + Rand(random, -0.3f, 0.3f);
                var fy = y + Rand(random, -0.15f, 0.15f);
                var color = i % 3 == 0 ? Raster.Hex(0xE04848) : i % 3 == 1 ? Raster.Hex(0xF2D04A) : Color.white;
                r.Capsule(fx, fy, fx, fy + 0.12f, 0.012f, Raster.Hex(0x3E6A2A));
                r.Circle(fx, fy + 0.14f, 0.045f, color);
            }
        }

        static void Mushroom(Raster r, float x, float y)
        {
            r.Capsule(x, y, x, y + 0.14f, 0.04f, Raster.Hex(0xEDE6D6), 0.025f, Ink);
            r.Ellipse(x, y + 0.16f, 0.14f, 0.08f, Raster.Hex(0xC0392B), 0.025f, Ink);
            r.Circle(x - 0.04f, y + 0.18f, 0.02f, Color.white);
        }

        static void Stalagmite(Raster r, float x, float y, float s)
        {
            Shadow(r, x, y, 0.25f * s);
            r.PolygonOutlined(Raster.Hex(0x8A7E70), 0.035f, Ink, x - 0.18f * s, y, x + 0.18f * s, y, x + 0.02f * s, y + 0.75f * s);
            r.Polygon(Raster.Hex(0xA89C8C), x - 0.08f * s, y + 0.05f, x, y + 0.05f, x + 0.01f * s, y + 0.6f * s);
        }

        static void Crystal(Raster r, float x, float y, float s)
        {
            r.Glow(x, y + 0.25f * s, 0.5f * s, new Color(0.6f, 0.4f, 1f, 0.5f));
            r.PolygonOutlined(Raster.Hex(0x9A6AE0), 0.03f, Ink, x - 0.1f * s, y, x + 0.1f * s, y, x + 0.12f * s, y + 0.35f * s, x, y + 0.5f * s, x - 0.12f * s, y + 0.35f * s);
            r.PolygonOutlined(Raster.Hex(0x7A4AC0), 0.03f, Ink, x + 0.08f * s, y, x + 0.24f * s, y, x + 0.26f * s, y + 0.22f * s, x + 0.16f * s, y + 0.32f * s);
        }

        static void Bones(Raster r, float x, float y)
        {
            var bone = Raster.Hex(0xE8E0D0);
            r.Capsule(x - 0.2f, y + 0.05f, x + 0.2f, y + 0.12f, 0.03f, bone, 0.02f, Ink);
            r.Circle(x + 0.25f, y + 0.12f, 0.09f, bone, 0.02f, Ink);
            r.Circle(x + 0.28f, y + 0.12f, 0.02f, Ink);
        }

        static void Palm(Raster r, float x, float y, float s)
        {
            Shadow(r, x, y, 0.4f * s);
            r.Taper(x, y, x + 0.12f * s, y + 0.9f * s, 0.08f * s, 0.05f * s, Raster.Hex(0x8A6A3A), 0.03f, Ink);
            var top = new Vector2(x + 0.12f * s, y + 0.92f * s);
            for (var i = 0; i < 6; i++)
            {
                var a = (i / 6f) * Mathf.PI * 2f;
                var end = top + new Vector2(Mathf.Cos(a) * 0.42f * s, Mathf.Sin(a) * 0.2f * s - 0.1f * s);
                r.Taper(top.x, top.y, end.x, end.y, 0.07f * s, 0.02f * s, Raster.Hex(0x4E8A3A), 0.025f, Ink);
            }
            r.Circle(top.x, top.y - 0.05f, 0.05f * s, Raster.Hex(0x7A4A1A));
        }

        static void Pillar(Raster r, float x, float y, float s)
        {
            Shadow(r, x, y, 0.3f * s);
            r.Box(x, y + 0.35f * s, 0.1f * s, 0.35f * s, 0.02f, Raster.Hex(0xD8CCB0), 0f, 0.03f, Ink);
            r.Box(x, y + 0.72f * s, 0.16f * s, 0.05f * s, 0.02f, Raster.Hex(0xC8BC9E), 0f, 0.03f, Ink);
            r.Box(x, y + 0.03f, 0.15f * s, 0.04f * s, 0.02f, Raster.Hex(0xC8BC9E), 0f, 0.03f, Ink);
        }

        static void DeadTree(Raster r, float x, float y, float s)
        {
            Shadow(r, x, y, 0.3f * s);
            var bark = Raster.Hex(0x4A3A2E);
            r.Taper(x, y, x, y + 0.75f * s, 0.07f * s, 0.03f * s, bark, 0.03f, Ink);
            r.Taper(x, y + 0.45f * s, x - 0.28f * s, y + 0.75f * s, 0.035f * s, 0.01f, bark, 0.025f, Ink);
            r.Taper(x, y + 0.55f * s, x + 0.25f * s, y + 0.85f * s, 0.035f * s, 0.01f, bark, 0.025f, Ink);
        }

        static void Spikes(Raster r, float x, float y)
        {
            for (var i = 0; i < 3; i++)
            {
                var sx = x - 0.2f + i * 0.2f;
                r.Taper(sx, y, sx + 0.06f, y + 0.4f, 0.04f, 0.01f, Raster.Hex(0x6A4A2E), 0.025f, Ink);
            }
        }

        static void Lava(Raster r, float x, float y, System.Random random)
        {
            var px = x;
            var py = y;
            r.Glow(x, y, 0.7f, new Color(1f, 0.4f, 0.05f, 0.35f));
            for (var i = 0; i < 4; i++)
            {
                var nx = px + Rand(random, -0.35f, 0.35f);
                var ny = py + Rand(random, -0.2f, 0.2f);
                r.Capsule(px, py, nx, ny, 0.04f, Raster.Hex(0xF07A2A));
                r.Capsule(px, py, nx, ny, 0.015f, Raster.Hex(0xFFE08A));
                px = nx;
                py = ny;
            }
        }

        // ------------------------------------------------------------ camps

        /// <summary>Finds where a path crosses into (entry) or out of (exit) the visible area.</summary>
        public static Vector2 EdgePoint(P[] path, bool entry)
        {
            var points = new List<P>(path);
            if (!entry)
                points.Reverse();
            for (var i = 0; i < points.Count; i++)
                if (Mathf.Abs(points[i].x) < ViewHalfWidth - 0.6f && Mathf.Abs(points[i].y) < ViewHalfHeight - 0.6f)
                    return new Vector2(points[i].x, points[i].y);
            return new Vector2(points[0].x, points[0].y);
        }

        static void Camps(Raster r, LevelDef level)
        {
            var exits = new List<Vector2>();
            for (var p = 0; p < level.PathCount; p++)
            {
                var path = level.Path(p);
                var entry = EdgePoint(path, true);
                TuranBanner(r, entry + Perpendicular(path, 0) * 0.95f);
                var last = path[path.Length - 1];
                var exit = new Vector2(last.x, last.y);
                if (!exits.Exists(e => (e - exit).sqrMagnitude < 1f))
                    exits.Add(exit);
            }
            foreach (var exit in exits)
                IranGate(r, Vector2.MoveTowards(exit, Vector2.zero, 1.6f));
        }

        static Vector2 Perpendicular(P[] path, int index)
        {
            var a = path[Mathf.Min(index, path.Length - 2)];
            var b = path[Mathf.Min(index + 6, path.Length - 1)];
            var d = new Vector2(b.x - a.x, b.y - a.y).normalized;
            return new Vector2(-d.y, d.x);
        }

        static void TuranBanner(Raster r, Vector2 at)
        {
            var x = Mathf.Clamp(at.x, -ViewHalfWidth + 0.5f, ViewHalfWidth - 0.5f);
            var y = Mathf.Clamp(at.y, -ViewHalfHeight + 0.4f, ViewHalfHeight - 1.2f);
            Shadow(r, x, y, 0.3f);
            r.Capsule(x, y, x, y + 1.1f, 0.04f, Raster.Hex(0x3A2A1A), 0.03f, Ink);
            r.PolygonOutlined(Raster.Hex(0x1E1E1E), 0.03f, Ink, x, y + 1.1f, x + 0.55f, y + 0.95f, x + 0.4f, y + 0.8f, x + 0.55f, y + 0.62f, x, y + 0.6f);
            r.Circle(x + 0.25f, y + 0.85f, 0.1f, Raster.Hex(0xB0302A));
        }

        static void IranGate(Raster r, Vector2 at)
        {
            var x = at.x;
            var y = at.y;
            var stone = Raster.Hex(0xD8CCB0);
            for (var side = -1; side <= 1; side += 2)
            {
                var px = x + side * 0.85f;
                Shadow(r, px, y - 0.1f, 0.35f);
                r.Box(px, y + 0.35f, 0.22f, 0.5f, 0.03f, stone, 0f, 0.035f, Ink);
                r.Box(px, y + 0.88f, 0.28f, 0.07f, 0.02f, Raster.Hex(0x1FA2A6), 0f, 0.035f, Ink);
                r.Capsule(px, y + 0.95f, px, y + 1.6f, 0.03f, Raster.Hex(0x5E3A1C), 0.025f, Ink);
                r.PolygonOutlined(Raster.Hex(0x6A2A7A), 0.03f, Ink, px, y + 1.6f, px + 0.42f, y + 1.48f, px, y + 1.32f);
                r.Star(px + 0.15f, y + 1.47f, 0.07f, 0.03f, 4, Raster.Hex(0xE2B13C));
            }
        }

        static void Vignette(Raster r)
        {
            for (var py = 0; py < r.Height; py++)
                for (var px = 0; px < r.Width; px++)
                {
                    var x = Mathf.Abs(r.X(px)) / HalfWidth;
                    var y = Mathf.Abs(r.Y(py)) / HalfHeight;
                    var edge = Mathf.Clamp01(Mathf.Max(x, y) * 1.25f - 0.85f);
                    if (edge > 0f)
                        r.Set(px, py, Color.Lerp(r.Get(px, py), Color.black, edge * 0.5f));
                }
        }
    }
}
