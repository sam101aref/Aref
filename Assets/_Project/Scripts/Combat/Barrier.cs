using System;
using System.Collections;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Something that soaks up projectiles until it breaks (F-57, F-63): Arash's standing shield,
    /// or the magic shield a Turanian shaman puts around an ally. Arrows that hit it stick in it
    /// and wear it down by their damage.
    /// </summary>
    public class Barrier : MonoBehaviour
    {
        [SerializeField, Min(1f)] float durability = 150f;
        [SerializeField, Tooltip("Tips over when broken (shields); otherwise pops (magic).")]
        bool topple = true;

        float current = -1f;

        public event Action<Barrier> Broken;

        public float Max { get { return durability; } }
        public float Current { get { return current < 0f ? durability : current; } }
        public float Fraction { get { return Current / durability; } }
        public bool IsBroken { get; private set; }

        public void Configure(float maxDurability, bool toppleWhenBroken)
        {
            durability = Mathf.Max(1f, maxDurability);
            current = durability;
            topple = toppleWhenBroken;
        }

        public void Absorb(float damage)
        {
            if (IsBroken || damage <= 0f)
                return;
            current = Mathf.Max(0f, Current - damage);
            StartCoroutine(Shake());
            if (current <= 0f)
                Break();
        }

        public void Break()
        {
            if (IsBroken)
                return;
            IsBroken = true;
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            if (Broken != null)
                Broken(this);
            StartCoroutine(topple ? Topple() : Pop());
        }

        IEnumerator Shake()
        {
            var origin = transform.localPosition;
            for (var t = 0f; t < 0.12f && !IsBroken; t += Time.deltaTime)
            {
                transform.localPosition = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.04f);
                yield return null;
            }
            if (!IsBroken)
                transform.localPosition = origin;
        }

        IEnumerator Topple()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            var start = transform.localRotation;
            for (var t = 0f; t < 1f; t += Time.deltaTime)
            {
                transform.localRotation = start * Quaternion.Euler(0f, 0f, -80f * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 2f)));
                foreach (var renderer in renderers)
                {
                    var c = renderer.color;
                    c.a = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
                    renderer.color = c;
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        IEnumerator Pop()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            var scale = transform.localScale;
            for (var t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                transform.localScale = scale * (1f + t * 2f);
                foreach (var renderer in renderers)
                {
                    var c = renderer.color;
                    c.a = 1f - t / 0.25f;
                    renderer.color = c;
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
