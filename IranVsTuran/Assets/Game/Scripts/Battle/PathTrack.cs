using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Battle
{
    /// <summary>A smoothed enemy path with arc-length lookup.</summary>
    public class PathTrack
    {
        readonly Vector2[] points;
        readonly float[] cumulative;

        public float Length { get; private set; }

        public PathTrack(P[] path)
        {
            points = new Vector2[path.Length];
            cumulative = new float[path.Length];
            for (var i = 0; i < path.Length; i++)
            {
                points[i] = new Vector2(path[i].x, path[i].y);
                if (i > 0)
                    cumulative[i] = cumulative[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            }
            Length = cumulative[cumulative.Length - 1];
        }

        /// <summary>Position at a distance along the path, and the walking direction there.</summary>
        public Vector2 At(float distance, out Vector2 direction)
        {
            distance = Mathf.Clamp(distance, 0f, Length);
            var lo = 0;
            var hi = cumulative.Length - 1;
            while (hi - lo > 1)
            {
                var mid = (lo + hi) / 2;
                if (cumulative[mid] <= distance)
                    lo = mid;
                else
                    hi = mid;
            }
            var segment = cumulative[hi] - cumulative[lo];
            var t = segment > 0f ? (distance - cumulative[lo]) / segment : 0f;
            direction = (points[hi] - points[lo]).normalized;
            return Vector2.Lerp(points[lo], points[hi], t);
        }

        public Vector2 At(float distance)
        {
            Vector2 direction;
            return At(distance, out direction);
        }

        /// <summary>Distance along the path of the point nearest to <paramref name="p"/>, and how far away it is.</summary>
        public float Project(Vector2 p, out float offPath)
        {
            var best = float.MaxValue;
            var bestDistance = 0f;
            for (var i = 0; i < points.Length - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var ab = b - a;
                var lengthSq = ab.sqrMagnitude;
                var t = lengthSq > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
                var d = Vector2.Distance(p, a + ab * t);
                if (d < best)
                {
                    best = d;
                    bestDistance = cumulative[i] + Mathf.Sqrt(lengthSq) * t;
                }
            }
            offPath = best;
            return bestDistance;
        }

        /// <summary>The first point of the path inside the visible battlefield (where the call-wave horn goes).</summary>
        public Vector2 EntryInView(float margin)
        {
            for (var d = 0f; d < Length; d += 0.1f)
            {
                var p = At(d);
                if (Mathf.Abs(p.x) < 9.6f - margin && Mathf.Abs(p.y) < 5.4f - margin)
                    return p;
            }
            return At(0f);
        }
    }

    /// <summary>Depth sorting and small visual helpers shared by battle objects.</summary>
    public static class Depth
    {
        public const int Background = -30000;
        public const int Ground = -20000;
        public const int Projectiles = 20000;
        public const int Effects = 22000;
        public const int Bars = 25000;
        public const int Overlay = 28000;

        /// <summary>Objects lower on screen are drawn in front.</summary>
        public static int Of(float y)
        {
            return Mathf.RoundToInt(-y * 100f) * 10;
        }
    }

    /// <summary>A small health bar that appears once a unit has been hurt.</summary>
    public class HealthBar
    {
        readonly Transform root;
        readonly Transform fill;
        readonly SpriteRenderer fillRenderer;
        readonly float width;

        public HealthBar(Transform parent, float y, float width)
        {
            this.width = width;
            var pixel = Art.ArtLibrary.Pixel();
            root = new GameObject("HealthBar").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, y, 0f);

            var back = new GameObject("Back").AddComponent<SpriteRenderer>();
            back.sprite = pixel;
            back.color = new Color(0.15f, 0.05f, 0.05f, 0.9f);
            back.sortingOrder = Depth.Bars;
            back.transform.SetParent(root, false);
            back.transform.localScale = new Vector3(width + 0.04f, 0.09f, 1f);

            fillRenderer = new GameObject("Fill").AddComponent<SpriteRenderer>();
            fillRenderer.sprite = pixel;
            fillRenderer.color = new Color(0.35f, 0.85f, 0.3f);
            fillRenderer.sortingOrder = Depth.Bars + 1;
            fill = fillRenderer.transform;
            fill.SetParent(root, false);
            Set(1f);
        }

        public void Set(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            root.gameObject.SetActive(fraction < 0.999f && fraction > 0f);
            fill.localScale = new Vector3(width * fraction, 0.06f, 1f);
            fill.localPosition = new Vector3(-width * (1f - fraction) / 2f, 0f, 0f);
            fillRenderer.color = fraction > 0.5f ? new Color(0.35f, 0.85f, 0.3f) : fraction > 0.25f ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.9f, 0.25f, 0.2f);
        }
    }
}
