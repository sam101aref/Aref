using System.Collections.Generic;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Shows the first part of the arrow's path as a row of shrinking, fading dots.
    /// Only a short stretch is shown on purpose: judging the rest of the arc is the player's skill.
    /// </summary>
    public class TrajectoryPreview : MonoBehaviour
    {
        const int MaxDots = 64;

        [SerializeField] Sprite dotSprite;
        [SerializeField] Color color = new Color(1f, 0.95f, 0.8f, 0.9f);
        [SerializeField] int sortingOrder = 50;

        [Header("Length")]
        [SerializeField, Min(0f), Tooltip("Seconds of flight shown. Difficulty and upgrades change this.")]
        float duration = 0.35f;
        [SerializeField, Min(1), Tooltip("Physics steps between dots.")]
        int stepsPerDot = 2;

        [Header("Look")]
        [SerializeField] float firstDotSize = 0.22f;
        [SerializeField] float lastDotSize = 0.08f;

        readonly List<SpriteRenderer> dots = new List<SpriteRenderer>();
        readonly Vector2[] points = new Vector2[MaxDots + 1];

        /// <summary>Seconds of flight the preview covers.</summary>
        public float Duration
        {
            get { return duration; }
            set { duration = Mathf.Max(0f, value); }
        }

        public void Show(Vector2 origin, Vector2 velocity, Vector2 acceleration)
        {
            var dt = Time.fixedDeltaTime;
            var count = Mathf.Clamp(Mathf.CeilToInt(duration / (dt * stepsPerDot)), 0, MaxDots);

            // Point 0 is the bow itself, so it is skipped.
            Ballistics.SamplePath(origin, velocity, acceleration, dt, stepsPerDot, points, count + 1);

            for (var i = 0; i < count; i++)
            {
                var dot = GetDot(i);
                var t = count > 1 ? i / (float)(count - 1) : 0f;
                var size = Mathf.Lerp(firstDotSize, lastDotSize, t);
                var dotColor = color;
                dotColor.a *= 1f - t * 0.8f;

                dot.transform.position = points[i + 1];
                dot.transform.localScale = new Vector3(size, size, 1f);
                dot.color = dotColor;
                dot.enabled = true;
            }

            for (var i = count; i < dots.Count; i++)
                dots[i].enabled = false;
        }

        public void Hide()
        {
            for (var i = 0; i < dots.Count; i++)
                dots[i].enabled = false;
        }

        SpriteRenderer GetDot(int index)
        {
            while (dots.Count <= index)
            {
                var go = new GameObject("Dot " + dots.Count);
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = dotSprite;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;
                dots.Add(renderer);
            }
            return dots[index];
        }
    }
}
