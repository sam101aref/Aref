using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arash.Art;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Arash.Combat
{
    /// <summary>
    /// A raider (F-63) runs at Arash and, once there, cuts at his shield or at him until shot down.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class Raider : MonoBehaviour
    {
        public float speed = 2.2f;
        public float goalX;
        public float damage = 12f;
        public float interval = 1.4f;
        /// <summary>The shield in front of Arash, if any; struck first.</summary>
        public Func<Barrier> Shield;
        public Health Target;

        Combatant self;
        ArcherRig rig;
        float timer;
        float bob;

        public bool HasArrived { get; private set; }

        void Awake()
        {
            self = GetComponent<Combatant>();
            rig = GetComponent<ArcherRig>();
        }

        void Update()
        {
            if (!self.IsAlive || Target == null || Target.IsDead)
                return;

            if (!HasArrived)
            {
                var position = transform.position;
                var direction = Mathf.Sign(goalX - position.x);
                position.x += direction * speed * Time.deltaTime;
                bob += Time.deltaTime * speed * 4f;
                if ((goalX - position.x) * direction <= 0f)
                {
                    position.x = goalX;
                    HasArrived = true;
                    timer = interval * 0.5f;
                }
                transform.position = position;
                if (rig != null)
                    rig.AimAtAngle(self.FacingRight ? -30f + Mathf.Sin(bob) * 15f : 210f - Mathf.Sin(bob) * 15f);
                return;
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = interval;
                StartCoroutine(Strike());
            }
        }

        IEnumerator Strike()
        {
            var up = self.FacingRight ? 60f : 120f;
            var down = self.FacingRight ? -40f : 220f;
            for (var t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                if (rig != null)
                    rig.AimAtAngle(Mathf.LerpAngle(down, up, t / 0.2f));
                yield return null;
            }
            for (var t = 0f; t < 0.1f; t += Time.deltaTime)
            {
                if (rig != null)
                    rig.AimAtAngle(Mathf.LerpAngle(up, down, t / 0.1f));
                yield return null;
            }
            if (!self.IsAlive)
                yield break;

            var shield = Shield != null ? Shield() : null;
            if (shield != null && !shield.IsBroken)
            {
                shield.Absorb(damage * 2f);
                Audio.AudioService.Play(Audio.Sfx.HitWood, 0.8f);
            }
            else if (Target != null)
            {
                var amount = damage * Target.DamageTaken(HitZoneType.Torso);
                Target.ApplyDamage(new DamageInfo(amount, HitZoneType.Torso, Target.transform.position + Vector3.up * 1.2f,
                    new Vector2(self.FacingRight ? 4f : -4f, 1f), null, transform));
            }
        }
    }

    /// <summary>
    /// A Turanian shaman (F-63) wraps an ally in a magic shield every few seconds; the shield soaks
    /// up arrows until it pops. Shoot the shaman first.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class ShamanCaster : MonoBehaviour
    {
        public float interval = 6f;
        public float shieldStrength = 80f;
        public Func<IEnumerable<Combatant>> Allies;

        Combatant self;
        ArcherRig rig;
        float timer = 3f;

        void Awake()
        {
            self = GetComponent<Combatant>();
            rig = GetComponent<ArcherRig>();
        }

        void Update()
        {
            if (!self.IsAlive || Allies == null)
                return;
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;
            timer = interval;
            var candidates = Allies().Where(a => a != null && a.IsAlive && a.GetComponentInChildren<MagicShield>() == null).ToList();
            if (candidates.Count == 0)
                return;
            StartCoroutine(Cast(candidates[Random.Range(0, candidates.Count)]));
        }

        IEnumerator Cast(Combatant ally)
        {
            var glow = ArtLibrary.Renderer(transform, "Cast", ArtLibrary.Fx("glow"), 38, new Vector2(self.FacingRight ? 0.7f : -0.7f, 2.3f));
            glow.color = new Color(0.75f, 0.5f, 1f, 0.9f);
            for (var t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                if (rig != null)
                    rig.AimAtAngle(self.FacingRight ? 70f : 110f);
                glow.transform.localScale = Vector3.one * (0.6f + t);
                yield return null;
            }
            Destroy(glow.gameObject);
            if (rig != null)
                rig.Relax();
            if (self.IsAlive && ally != null && ally.IsAlive)
                MagicShield.Add(ally, shieldStrength);
        }
    }

    /// <summary>A bubble of magic around a character that absorbs arrows until it pops.</summary>
    public class MagicShield : MonoBehaviour
    {
        public static MagicShield Add(Combatant target, float strength)
        {
            var scale = target.transform.localScale.y;
            var go = new GameObject("Magic Shield");
            go.transform.SetParent(target.transform, false);
            go.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            var renderer = ArtLibrary.Renderer(go.transform, "Bubble", ArtLibrary.Prop("bubble"), 45);
            renderer.transform.localScale = Vector3.one * 2.1f;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.3f;
            var barrier = go.AddComponent<Barrier>();
            barrier.Configure(strength, false);
            var shield = go.AddComponent<MagicShield>();
            target.Health.Died += info =>
            {
                if (barrier != null)
                    barrier.Break();
            };
            go.transform.localScale = Vector3.one * 0.2f;
            shield.StartCoroutine(shield.Grow(scale > 0f ? 1f : 1f));
            return shield;
        }

        IEnumerator Grow(float size)
        {
            for (var t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(0.2f, size, t / 0.25f);
                yield return null;
            }
            transform.localScale = Vector3.one * size;
        }
    }

    /// <summary>Fire arrows (F-55) set the target burning: damage over a few seconds.</summary>
    public class Burning : MonoBehaviour
    {
        public float damagePerSecond = 10f;
        public float remaining = 3f;
        public Transform source;

        Health health;
        SpriteRenderer flame;
        float tick;

        public static void Apply(Health target, float seconds, float damagePerSecond, Transform source)
        {
            if (target == null || target.IsDead)
                return;
            var burning = target.GetComponent<Burning>();
            if (burning == null)
                burning = target.gameObject.AddComponent<Burning>();
            burning.remaining = Mathf.Max(burning.remaining, seconds);
            burning.damagePerSecond = damagePerSecond;
            burning.source = source;
        }

        void Start()
        {
            health = GetComponent<Health>();
            var combatant = GetComponent<Combatant>();
            var anchor = combatant != null ? combatant.BodyTarget : transform;
            flame = ArtLibrary.Renderer(anchor, "Flame", ArtLibrary.Fx("flame"), 46, new Vector2(0f, -0.2f));
            flame.transform.localScale = Vector3.one * 1.3f;
        }

        void Update()
        {
            if (health == null || health.IsDead || remaining <= 0f)
            {
                if (flame != null)
                    Destroy(flame.gameObject);
                Destroy(this);
                return;
            }
            remaining -= Time.deltaTime;
            tick += Time.deltaTime;
            if (flame != null)
                flame.transform.localScale = new Vector3(1.3f, 1.3f + Mathf.Sin(Time.time * 20f) * 0.15f, 1f);
            if (tick >= 0.5f)
            {
                tick -= 0.5f;
                // Armour zone: burning earns no farr and is not a "hit" for star purposes.
                health.ApplyDamage(new DamageInfo(damagePerSecond * 0.5f, HitZoneType.Armor, transform.position, Vector2.zero, null, source));
            }
        }
    }

    /// <summary>Short-lived effects: lightning between enemies, sparks, healing marks.</summary>
    public static class BattleEffects
    {
        /// <summary>Thunder bow (F-55): lightning jumps from the hit enemy to the nearest other one.</summary>
        public static void ChainLightning(Health from, IEnumerable<Combatant> enemies, float damage, float range, Transform source)
        {
            if (from == null)
                return;
            var origin = (Vector2)from.transform.position + Vector2.up * 1.2f;
            var next = enemies.Where(e => e != null && e.IsAlive && e.Health != from)
                .OrderBy(e => ((Vector2)e.BodyTarget.position - origin).sqrMagnitude)
                .FirstOrDefault();
            if (next == null || ((Vector2)next.BodyTarget.position - origin).magnitude > range)
                return;
            Bolt(origin, next.BodyTarget.position);
            next.Health.ApplyDamage(new DamageInfo(damage * next.Health.DamageTaken(HitZoneType.Torso), HitZoneType.Torso,
                next.BodyTarget.position, Vector2.right, null, source));
        }

        public static void Bolt(Vector2 from, Vector2 to)
        {
            var sprite = ArtLibrary.Fx("bolt");
            if (sprite == null)
                return;
            var renderer = ArtLibrary.Renderer(null, "Bolt", sprite, 65, (from + to) * 0.5f);
            var delta = to - from;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
            renderer.transform.localScale = new Vector3(1.6f, delta.magnitude / Mathf.Max(0.01f, sprite.bounds.size.y), 1f);
            UnityEngine.Object.Destroy(renderer.gameObject, 0.25f);
        }

        /// <summary>A sprite that pops up, drifts and fades: sparks, healing crosses.</summary>
        public static void Pop(string fx, Vector2 position, float size, Color color, float duration = 0.5f)
        {
            var sprite = ArtLibrary.Fx(fx);
            if (sprite == null)
                return;
            var renderer = ArtLibrary.Renderer(null, fx, sprite, 66, position);
            renderer.color = color;
            renderer.transform.localScale = Vector3.one * size;
            renderer.gameObject.AddComponent<FadeAway>().duration = duration;
        }
    }

    /// <summary>Drifts up, grows a little and fades out, then destroys itself.</summary>
    public class FadeAway : MonoBehaviour
    {
        public float duration = 0.5f;
        public Vector2 drift = new Vector2(0f, 0.8f);

        SpriteRenderer sprite;
        Color color;
        Vector3 scale;
        float t;

        void Start()
        {
            sprite = GetComponent<SpriteRenderer>();
            color = sprite != null ? sprite.color : Color.white;
            scale = transform.localScale;
        }

        void Update()
        {
            t += Time.deltaTime;
            var k = t / duration;
            transform.position += (Vector3)(drift * Time.deltaTime);
            transform.localScale = scale * (1f + k * 0.3f);
            if (sprite != null)
            {
                var c = color;
                c.a *= 1f - k;
                sprite.color = c;
            }
            if (k >= 1f)
                Destroy(gameObject);
        }
    }
}
