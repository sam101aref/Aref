using System;
using System.Collections.Generic;

namespace IranVsTuran.Defs
{
    public enum Theme
    {
        Steppe,
        Forest,
        Cave,
        Desert,
        Snow,
        Dark,
        Volcanic,
    }

    public class SpawnGroup
    {
        public string enemy;
        public int count;
        /// <summary>Seconds between two enemies of the group.</summary>
        public float interval;
        /// <summary>Seconds after the wave starts.</summary>
        public float delay;
        /// <summary>Path index, or -1 to alternate between all paths.</summary>
        public int path;
    }

    public class WaveDef
    {
        public List<SpawnGroup> groups = new List<SpawnGroup>();

        public int EnemyCount
        {
            get
            {
                var total = 0;
                foreach (var group in groups)
                    total += group.count;
                return total;
            }
        }

        /// <summary>Seconds from the wave's start until its last enemy has appeared.</summary>
        public float Duration
        {
            get
            {
                var end = 0f;
                foreach (var group in groups)
                    end = Math.Max(end, group.delay + group.interval * Math.Max(0, group.count - 1));
                return end;
            }
        }
    }

    /// <summary>An enemy type and the wave in which it first appears.</summary>
    public struct RosterEntry
    {
        public string enemy;
        public int firstWave;

        public RosterEntry(string enemy, int firstWave)
        {
            this.enemy = enemy;
            this.firstWave = firstWave;
        }
    }

    public class LevelDef
    {
        public string id;
        public int chapter;
        public int index;
        public Theme theme;
        /// <summary>Raw control points; enemies walk the smoothed version (see <see cref="Path"/>).</summary>
        public P[][] paths;
        public P[] slots;
        public int startCoins;
        public int lives = 20;
        public WaveDef[] waves;
        public string boss;
        /// <summary>Position on the world map, 0–1 in both axes (x east, y north).</summary>
        public P mapPosition;

        public string NameKey { get { return "level." + id; } }
        public string DescKey { get { return "level." + id + ".desc"; } }

        P[][] smoothed;

        /// <summary>The path enemies follow: the control points smoothed with two Chaikin passes.</summary>
        public P[] Path(int index)
        {
            if (smoothed == null)
            {
                smoothed = new P[paths.Length][];
                for (var i = 0; i < paths.Length; i++)
                    smoothed[i] = Geometry.Chaikin(paths[i], 2);
            }
            return smoothed[index];
        }

        public int PathCount { get { return paths.Length; } }
    }

    public class ChapterDef
    {
        public string id;
        public string[] levels;
        /// <summary>Cutscene shown before the chapter's first level.</summary>
        public string intro;
        /// <summary>Cutscene shown after the chapter's last level is first won.</summary>
        public string outro;
        public bool bonus;
        public string NameKey { get { return "chapter." + id; } }
    }

    public static class Geometry
    {
        public static P[] Chaikin(P[] points, int iterations)
        {
            var current = points;
            for (var it = 0; it < iterations; it++)
            {
                var result = new List<P> { current[0] };
                for (var i = 0; i < current.Length - 1; i++)
                {
                    var a = current[i];
                    var b = current[i + 1];
                    result.Add(new P(0.75f * a.x + 0.25f * b.x, 0.75f * a.y + 0.25f * b.y));
                    result.Add(new P(0.25f * a.x + 0.75f * b.x, 0.25f * a.y + 0.75f * b.y));
                }
                result.Add(current[current.Length - 1]);
                current = result.ToArray();
            }
            return current;
        }

        public static float SegmentDistance(P p, P a, P b)
        {
            var dx = b.x - a.x;
            var dy = b.y - a.y;
            var lengthSq = dx * dx + dy * dy;
            var t = lengthSq <= 0f ? 0f : Math.Max(0f, Math.Min(1f, ((p.x - a.x) * dx + (p.y - a.y) * dy) / lengthSq));
            return P.Distance(p, new P(a.x + t * dx, a.y + t * dy));
        }

        public static float PathDistance(P p, P[] path)
        {
            var best = float.MaxValue;
            for (var i = 0; i < path.Length - 1; i++)
                best = Math.Min(best, SegmentDistance(p, path[i], path[i + 1]));
            return best;
        }
    }

    /// <summary>
    /// Thirteen battles in five chapters, following the Shahnameh: Afrasiab's invasion, Rostam's
    /// seven labours in Mazandaran, Sohrab and the White Castle, the revenge for Siavash, and a
    /// bonus battle against Zahhak on Mount Damavand. Paths and tower slots were laid out with a
    /// design tool that keeps every slot 1.05–1.85 units from the road.
    /// </summary>
    public static class LevelDefs
    {
        public static readonly List<ChapterDef> Chapters = new List<ChapterDef>
        {
            new ChapterDef { id = "invasion", levels = new[] { "l1", "l2", "l3" }, intro = "prologue", outro = "invasion_end" },
            new ChapterDef { id = "haftkhan", levels = new[] { "l4", "l5", "l6" }, intro = "haftkhan", outro = "haftkhan_end" },
            new ChapterDef { id = "sohrab", levels = new[] { "l7", "l8", "l9" }, intro = "sohrab", outro = "sohrab_end" },
            new ChapterDef { id = "siavash", levels = new[] { "l10", "l11", "l12" }, intro = "siavash", outro = "ending" },
            new ChapterDef { id = "damavand", levels = new[] { "l13" }, intro = "damavand", outro = "damavand_end", bonus = true },
        };

        public static readonly List<LevelDef> All = Build();

        public static LevelDef Get(string id)
        {
            return All.Find(l => l.id == id);
        }

        public static ChapterDef ChapterOf(LevelDef level)
        {
            return Chapters[level.chapter];
        }

        /// <summary>The level after <paramref name="level"/> in campaign order, or null.</summary>
        public static LevelDef Next(LevelDef level)
        {
            var index = All.IndexOf(level);
            return index >= 0 && index + 1 < All.Count ? All[index + 1] : null;
        }

        static List<LevelDef> Build()
        {
            var levels = new List<LevelDef>
            {
                new LevelDef
                {
                    id = "l1", theme = Theme.Steppe, startCoins = 260, mapPosition = new P(0.63f, 0.42f),
                    paths = new[] { Path(-11f, 2.4f, -6f, 2.4f, -4f, 1f, -4f, -1.5f, -1.5f, -2.6f, 1.5f, -2.4f, 3f, -0.5f, 3.2f, 1.6f, 5.5f, 2.6f, 7.8f, 1.6f, 8.2f, -0.6f, 11f, -0.8f) },
                    slots = Points(5.3f, 0.7f, 0.8f, -0.8f, -6.2f, 0.7f, -1.7f, -1.3f, 6.8f, 0.7f, -2.95f, -0.3f, -5.2f, -0.55f, 4.05f, -0.3f),
                    waves = WaveGen.Build(101, 6, 30, 150, 1, null, R("soldier", 1), R("raider", 3), R("heavy", 6)),
                },
                new LevelDef
                {
                    id = "l2", theme = Theme.Steppe, startCoins = 300, mapPosition = new P(0.55f, 0.34f),
                    paths = new[] { Path(-11f, 3f, -6f, 3f, -3.2f, 1.4f, -0.5f, 0f, 2.5f, 0f, 4.5f, 1.5f, 7f, 1.5f, 8f, -1f, 11f, -1.5f), Path(-11f, -2.6f, -6.5f, -2.6f, -3.2f, -1.4f, -0.5f, 0f, 2.5f, 0f, 4.5f, 1.5f, 7f, 1.5f, 8f, -1f, 11f, -1.5f) },
                    slots = Points(5.3f, -0.3f, 1.55f, 1.2f, 6.8f, -0.8f, -0.2f, 1.2f, 8.3f, 1.2f, 3.3f, 1.95f, 3.8f, -0.55f, -2.95f, -0.05f, -0.45f, -1.3f, 1.55f, -1.05f),
                    waves = WaveGen.Build(202, 8, 45, 240, 2, null, R("soldier", 1), R("raider", 1), R("archer", 2), R("heavy", 4)),
                },
                new LevelDef
                {
                    id = "l3", theme = Theme.Steppe, startCoins = 350, mapPosition = new P(0.46f, 0.26f), boss = "garsivaz",
                    paths = new[] { Path(-6.5f, 6.2f, -6.5f, 2f, -3f, 2.6f, -1.2f, 0.6f, -3.5f, -1.6f, -1f, -3f, 3f, -2.6f, 4f, 0f, 2.2f, 2.2f, 5f, 3.2f, 7.5f, 2f, 8f, -0.8f, 11f, -1.2f) },
                    slots = Points(5.05f, 0.7f, -0.45f, -1.55f, -3.95f, -0.05f, 1.55f, -1.05f, -4.45f, 3.45f, 2.05f, 0.45f, 6.55f, 0.7f, 8.55f, 1.7f, -1.2f, -3.8f, -4.95f, 1.2f, 1.8f, -3.8f, -0.7f, -0.05f),
                    waves = WaveGen.Build(303, 10, 60, 330, 1, "garsivaz", R("soldier", 1), R("archer", 1), R("raider", 2), R("heavy", 3), R("cavalry", 4), R("shaman", 6)),
                },
                new LevelDef
                {
                    id = "l4", chapter = 1, theme = Theme.Forest, startCoins = 400, mapPosition = new P(0.30f, 0.60f),
                    paths = new[] { Path(11f, 0.5f, 6.5f, 0.5f, 4.5f, 2.5f, 0.5f, 2.6f, -2f, 0.5f, -5f, 0.4f, -11f, 0.4f), Path(11f, 0.5f, 6.5f, 0.5f, 4.5f, -1.8f, 0.5f, -2.4f, -2f, 0.5f, -5f, 0.4f, -11f, 0.4f) },
                    slots = Points(-6.95f, -0.8f, -3.95f, -0.8f, -5.45f, -0.8f, -6.95f, 1.7f, -4.7f, 1.7f, -3.2f, 1.7f, 0.05f, 0.2f, 4.8f, 0.2f, -2.45f, -1.05f, 7.05f, 1.95f),
                    waves = WaveGen.Build(404, 10, 80, 420, 2, null, R("imp", 1), R("lion", 1), R("gorgsar", 2), R("div", 4), R("witch", 5)),
                },
                new LevelDef
                {
                    id = "l5", chapter = 1, theme = Theme.Forest, startCoins = 450, mapPosition = new P(0.21f, 0.68f), boss = "dragon",
                    paths = new[] { Path(7f, 6.2f, 7f, 2.2f, 2f, 2.8f, -1f, 1.6f, 2f, -0.2f, 5.5f, -0.8f, 4f, -3.2f, -1f, -2.8f, -4f, -1f, -6.5f, 0.6f, -11f, 0.6f) },
                    slots = Points(2.3f, -1.55f, 2.3f, 1.2f, 3.8f, 1.2f, 0.8f, -1.55f, -0.45f, -0.55f, 4.8f, 3.7f, 5.05f, 0.2f, -1.95f, -0.3f, -6.7f, -0.8f, -4.7f, -1.8f, -3.45f, -0.05f),
                    waves = WaveGen.Build(505, 11, 100, 480, 1, "dragon", R("imp", 1), R("gorgsar", 1), R("lion", 2), R("winged", 3), R("witch", 4), R("div", 5)),
                },
                new LevelDef
                {
                    id = "l6", chapter = 1, theme = Theme.Cave, startCoins = 500, mapPosition = new P(0.12f, 0.78f), boss = "divsepid",
                    paths = new[] { Path(11f, 3f, 5f, 3f, 2.5f, 1f, -1f, 1.2f, -3.5f, 2.6f, -7f, 2f, -7.5f, -0.5f, -11f, -0.5f), Path(11f, -2.4f, 6f, -2.6f, 3f, -0.8f, -1f, -1.2f, -4f, -2.6f, -7f, -1.8f, -7.5f, -0.5f, -11f, -0.5f) },
                    slots = Points(-6.45f, -0.3f, -0.2f, -0.05f, 2.55f, 0.2f, -1.7f, -0.05f, 4.05f, 0.2f, -3.2f, -0.3f, -7.7f, -2.3f, -8.45f, 0.7f, -4.95f, -0.55f, -4.7f, 0.95f, 1.3f, 2.2f, 2.3f, -2.05f),
                    waves = WaveGen.Build(606, 12, 120, 560, 2, "divsepid", R("imp", 1), R("gorgsar", 1), R("lion", 1), R("winged", 2), R("witch", 3), R("div", 3)),
                },
                new LevelDef
                {
                    id = "l7", chapter = 2, theme = Theme.Desert, startCoins = 550, mapPosition = new P(0.58f, 0.60f),
                    paths = new[] { Path(-11f, -0.5f, -6.5f, -0.5f, -5f, 2.2f, -1.5f, 2.6f, 0f, 0f, -1.5f, -2.6f, 2.5f, -3f, 4.5f, -0.6f, 6f, 1.8f, 8f, 1f, 11f, 1f) },
                    slots = Points(1.3f, -0.8f, -2.45f, 0.7f, 6.05f, -0.3f, -4.2f, 0.95f, -7.2f, 0.95f, 2.8f, -0.8f, -1.7f, -0.8f, 7.55f, -0.3f, 3.8f, 0.45f, 5.05f, -1.55f, -5.2f, -0.3f),
                    waves = WaveGen.Build(707, 12, 140, 650, 1, null, R("soldier", 1), R("archer", 1), R("raider", 1), R("heavy", 2), R("cavalry", 3), R("shaman", 5)),
                },
                new LevelDef
                {
                    id = "l8", chapter = 2, theme = Theme.Steppe, startCoins = 600, mapPosition = new P(0.67f, 0.69f),
                    paths = new[] { Path(-3f, 6.2f, -3f, 3f, -0.5f, 1.6f, 1f, -0.2f, 4f, -0.6f, 6.5f, 1.4f, 11f, 1.4f), Path(-11f, -2f, -6f, -2f, -3.5f, -0.8f, -1f, -1.8f, 1f, -0.2f, 4f, -0.6f, 6.5f, 1.4f, 11f, 1.4f) },
                    slots = Points(2.8f, 0.7f, 6.55f, -0.05f, 4.3f, 1.2f, -0.7f, -0.05f, 5.05f, -1.05f, 5.8f, 1.95f, 1.8f, -1.55f, 1.3f, 1.2f, 3.3f, -1.55f, -2.2f, 0.2f, 8.05f, 0.2f, 7.3f, 2.45f),
                    waves = WaveGen.Build(808, 12, 160, 720, 2, null, R("soldier", 1), R("raider", 1), R("archer", 1), R("heavy", 2), R("cavalry", 3), R("shaman", 4), R("gorgsar", 5)),
                },
                new LevelDef
                {
                    id = "l9", chapter = 2, theme = Theme.Steppe, startCoins = 650, mapPosition = new P(0.74f, 0.58f), boss = "houman",
                    paths = new[] { Path(-11f, 2.5f, -4f, 2.8f, 0f, 1.4f, 3f, 2.6f, 6f, 2f, 7f, 0f, 11f, 0f), Path(-11f, -2.5f, -4f, -2.8f, 0f, -1.4f, 3f, -2.6f, 6f, -2f, 7f, 0f, 11f, 0f) },
                    slots = Points(5.3f, -0.05f, 7.3f, -1.8f, 7.55f, 1.45f, 0.05f, -0.05f, -1.45f, -0.05f, 1.55f, -0.05f, 3.8f, -1.3f, 4.05f, 0.95f, -0.45f, -2.8f, -0.7f, 2.95f, -4.7f, -1.55f, -3.95f, 1.45f),
                    waves = WaveGen.Build(909, 13, 180, 800, 2, "houman", R("raider", 1), R("archer", 1), R("heavy", 1), R("cavalry", 2), R("shaman", 3), R("gorgsar", 4)),
                },
                new LevelDef
                {
                    id = "l10", chapter = 3, theme = Theme.Snow, startCoins = 700, mapPosition = new P(0.80f, 0.42f), boss = "ashkbus",
                    paths = new[] { Path(0.5f, 6.2f, 0.5f, 3f, -4f, 2.5f, -6.5f, 0.5f, -4.5f, -1.5f, -0.5f, -0.5f, 2.5f, 0.8f, 5.5f, 0.5f, 6.5f, -2f, 2.5f, -3.2f, -1f, -3.4f, -1.5f, -6.2f) },
                    slots = Points(-3.45f, 0.95f, 3.55f, -1.55f, 1.8f, -1.55f, -1.95f, 0.95f, -0.2f, -2.05f, -0.45f, 1.2f, -1.7f, -2.55f, 1.05f, 1.7f, -2.7f, 3.7f, -3.2f, -2.8f, -4.95f, 2.95f, 4.3f, -3.8f),
                    waves = WaveGen.Build(1010, 14, 200, 900, 1, "ashkbus", R("soldier", 1), R("archer", 1), R("heavy", 1), R("cavalry", 2), R("shaman", 3), R("elephant", 5)),
                },
                new LevelDef
                {
                    id = "l11", chapter = 3, theme = Theme.Desert, startCoins = 750, mapPosition = new P(0.87f, 0.60f), boss = "akvan",
                    paths = new[] { Path(-11f, 3f, -5f, 2.6f, 0f, -1.5f, 5f, -2.6f, 11f, -2.4f), Path(-11f, -2.8f, -5f, -2.6f, 0f, 1.5f, 5f, 2.6f, 11f, 2.4f) },
                    slots = Points(0.3f, -0.05f, -3.7f, -0.05f, -1.7f, 1.45f, -1.95f, -1.55f, 1.8f, -0.05f, -5.2f, 0.2f, -0.45f, -2.3f, 3.3f, 0.45f, -2.95f, 2.45f, -5.95f, -1.3f, -6.45f, 1.45f, 6.3f, 1.45f),
                    waves = WaveGen.Build(1111, 14, 220, 980, 2, "akvan", R("imp", 1), R("gorgsar", 1), R("winged", 1), R("witch", 2), R("div", 3), R("lion", 4)),
                },
                new LevelDef
                {
                    id = "l12", chapter = 3, theme = Theme.Dark, startCoins = 800, mapPosition = new P(0.92f, 0.80f), boss = "afrasiab",
                    paths = new[] { Path(11f, 3f, 6f, 3.2f, 3f, 1.5f, 0f, 2.4f, -3f, 1f, -6f, 1.2f, -11f, 0f), Path(11f, 0f, 6.5f, 0f, 3f, -1.2f, 0f, -0.4f, -3f, 1f, -6f, 1.2f, -11f, 0f), Path(11f, -3f, 6f, -3.2f, 2f, -2.8f, -2f, -2f, -5f, -1.2f, -8f, -0.4f, -11f, 0f) },
                    slots = Points(-5.2f, -0.05f, -3.7f, -0.3f, -2.2f, -0.55f, -7.2f, 1.95f, 0.3f, 0.7f, 4.8f, -1.8f, -3.95f, 2.2f, 6.3f, -1.8f, -5.45f, 2.2f, 1.8f, 0.45f, 6.8f, 1.2f, -7.95f, -1.55f, 3.55f, 0.45f),
                    waves = WaveGen.Build(1212, 15, 250, 1100, 3, "afrasiab", R("heavy", 1), R("cavalry", 1), R("archer", 1), R("shaman", 2), R("elephant", 3), R("div", 5), R("winged", 6)),
                },
                new LevelDef
                {
                    id = "l13", chapter = 4, theme = Theme.Volcanic, startCoins = 850, mapPosition = new P(0.37f, 0.80f), boss = "zahhak",
                    paths = new[] { Path(-11f, 1f, -6.5f, 1f, -4.5f, -1.6f, -1f, -2.6f, 2.5f, -1.2f, 5f, -2.6f, 11f, -2.6f), Path(-1.5f, 6.2f, -1.5f, 2.8f, 1.5f, 1.4f, 0.5f, -0.8f, 2.5f, -1.2f, 5f, -2.6f, 11f, -2.6f) },
                    slots = Points(3.05f, -0.3f, 5.3f, -1.3f, 2.8f, -2.8f, 6.8f, -1.3f, -0.45f, -0.55f, 4.3f, -3.3f, 1.3f, -3.05f, 8.3f, -1.55f, -0.2f, 0.95f, 2.3f, 1.2f, 0.3f, 3.2f, -3.2f, -0.8f),
                    waves = WaveGen.Build(1313, 15, 260, 1200, 2, "zahhak", R("snake", 1), R("imp", 1), R("winged", 2), R("div", 3), R("witch", 4), R("gorgsar", 5)),
                },
            };

            for (var i = 0; i < levels.Count; i++)
                levels[i].index = i;
            return levels;
        }

        static P[] Path(params float[] xy)
        {
            return Points(xy);
        }

        static P[] Points(params float[] xy)
        {
            var points = new P[xy.Length / 2];
            for (var i = 0; i < points.Length; i++)
                points[i] = new P(xy[2 * i], xy[2 * i + 1]);
            return points;
        }

        static RosterEntry R(string enemy, int firstWave)
        {
            return new RosterEntry(enemy, firstWave);
        }
    }

    /// <summary>
    /// Builds a level's waves from its roster: each wave gets a threat budget (counted in bounty
    /// coins) that grows from the first wave to the last; newly introduced enemies lead their
    /// first wave so the player meets them alone, and the boss closes the final wave. The seed
    /// makes every level's waves the same on every device.
    /// </summary>
    public static class WaveGen
    {
        public static WaveDef[] Build(int seed, int count, int startBudget, int endBudget, int pathCount, string boss,
            params RosterEntry[] roster)
        {
            var random = new Random(seed);
            var waves = new WaveDef[count];
            for (var w = 1; w <= count; w++)
            {
                var progress = count > 1 ? (w - 1f) / (count - 1f) : 1f;
                var budget = startBudget + (endBudget - startBudget) * (float)Math.Pow(progress, 1.15);
                var wave = new WaveDef();
                var isBossWave = w == count && !string.IsNullOrEmpty(boss);
                var delay = 0f;

                if (isBossWave)
                {
                    wave.groups.Add(new SpawnGroup { enemy = boss, count = 1, interval = 1f, delay = 4f, path = 0 });
                    budget *= 0.55f;
                }

                var available = new List<string>();
                var newcomers = new List<string>();
                foreach (var entry in roster)
                {
                    if (entry.firstWave <= w)
                        available.Add(entry.enemy);
                    if (entry.firstWave == w && w > 1)
                        newcomers.Add(entry.enemy);
                }

                var groupCount = w <= 2 ? 1 : w <= count / 2 ? 2 : 3;
                var picks = new List<string>(newcomers);
                while (picks.Count < groupCount)
                    picks.Add(available[random.Next(available.Count)]);

                for (var g = 0; g < picks.Count; g++)
                {
                    var def = EnemyDefs.Get(picks[g]);
                    var share = budget / picks.Count;
                    var enemies = Math.Max(1, Math.Min(30, (int)Math.Round(share / Math.Max(1, def.bounty))));
                    var interval = Math.Max(0.35f, Math.Min(3f, 0.25f + def.bounty * 0.04f) / Math.Max(0.6f, def.speed));
                    var path = pathCount <= 1 ? 0 : enemies >= 6 ? -1 : (w + g) % pathCount;
                    wave.groups.Add(new SpawnGroup { enemy = def.id, count = enemies, interval = (float)Math.Round(interval, 2), delay = delay, path = path });
                    delay += Math.Min(8f, 2.5f + enemies * interval * 0.5f);
                }
                waves[w - 1] = wave;
            }
            return waves;
        }
    }
}
