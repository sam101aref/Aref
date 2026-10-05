using System.Collections.Generic;
using IranVsTuran.Art;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Battle
{
    /// <summary>Short-lived visual effects: rings, bursts, smoke, sparkles and coins.</summary>
    public class Effects
    {
        class Fx
        {
            public SpriteRenderer renderer;
            public float age;
            public float duration;
            public Vector3 startScale;
            public Vector3 endScale;
            public Vector3 velocity;
            public Color color;
        }

        readonly Transform parent;
        readonly List<Fx> active = new List<Fx>();

        public Effects(Transform parent)
        {
            this.parent = parent;
        }

        Fx Spawn(Sprite sprite, Vector2 position, Color color, float duration, float startSize, float endSize, int order)
        {
            var renderer = new GameObject("Fx").AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            renderer.transform.SetParent(parent, false);
            renderer.transform.localPosition = new Vector3(position.x, position.y, 0f);
            var fx = new Fx
            {
                renderer = renderer,
                duration = duration,
                startScale = Vector3.one * startSize,
                endScale = Vector3.one * endSize,
                color = color,
            };
            renderer.transform.localScale = fx.startScale;
            active.Add(fx);
            return fx;
        }

        /// <summary>A flat ring that grows to <paramref name="radius"/> (shock waves, stomps).</summary>
        public void Ring(Vector2 position, float radius, Color color, float duration)
        {
            var fx = Spawn(ArtLibrary.Ring(), position, color, duration, radius * 0.4f, radius, Depth.Ground + 20);
            fx.startScale = new Vector3(radius * 0.4f, radius * 0.24f, 1f);
            fx.endScale = new Vector3(radius, radius * 0.6f, 1f);
        }

        public void Burst(Vector2 position, float radius, Color color)
        {
            Spawn(ArtLibrary.SoftCircle(), position, color, 0.35f, radius * 0.5f, radius * 1.2f, Depth.Effects);
        }

        public void Smoke(Vector2 position, float size)
        {
            var fx = Spawn(ArtLibrary.Smoke(), position + new Vector2(0f, 0.2f), new Color(0.85f, 0.82f, 0.78f, 0.75f), 0.7f,
                size * 0.4f, size, Depth.Effects);
            fx.velocity = new Vector3(0f, 0.6f, 0f);
        }

        public void Sparkle(Vector2 position, Color color)
        {
            Spawn(ArtLibrary.Spark(), position, color, 0.4f, 0.3f, 0.9f, Depth.Effects + 1);
        }

        public void Coin(Vector2 position)
        {
            var fx = Spawn(ArtLibrary.Icon(Icon.Coin), position + new Vector2(0f, 0.4f), Color.white, 0.6f, 0.28f, 0.22f, Depth.Effects + 2);
            fx.velocity = new Vector3(0f, 1.1f, 0f);
        }

        /// <summary>A flame that stays on the ground for a while (naphtha).</summary>
        public void GroundFlame(Vector2 position, float duration)
        {
            Spawn(ArtLibrary.Flame(), position, Color.white, duration, 0.45f, 0.25f, Depth.Of(position.y) + 5);
        }

        public void Update(float dt)
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var fx = active[i];
                fx.age += dt;
                var t = fx.age / fx.duration;
                if (t >= 1f || fx.renderer == null)
                {
                    if (fx.renderer != null)
                        Object.Destroy(fx.renderer.gameObject);
                    active.RemoveAt(i);
                    continue;
                }
                var transform = fx.renderer.transform;
                transform.localScale = Vector3.Lerp(fx.startScale, fx.endScale, Mathf.Sqrt(t));
                transform.localPosition += fx.velocity * dt;
                var color = fx.color;
                color.a *= 1f - t * t;
                fx.renderer.color = color;
            }
        }
    }

    /// <summary>Burning ground left by naphtha: damages ground enemies standing in it.</summary>
    public class GroundFire
    {
        public Vector2 Position;
        public float Radius;
        public float Dps;
        public float TimeLeft;
        float flameTimer;

        public void Update(Battlefield field, float dt)
        {
            TimeLeft -= dt;
            foreach (var enemy in field.Enemies)
                if (enemy.Alive && !enemy.Flying && (enemy.Position - Position).sqrMagnitude < Radius * Radius)
                    enemy.TakeDamage(Dps * dt, DamageType.True);
            flameTimer -= dt;
            if (flameTimer <= 0f && TimeLeft > 0.3f)
            {
                flameTimer = 0.25f;
                var offset = Random.insideUnitCircle * Radius * 0.8f;
                field.Effects.GroundFlame(Position + new Vector2(offset.x, offset.y * 0.6f), 0.6f);
            }
        }
    }
}
