using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arash.Art;
using Arash.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Arash.Combat
{
    /// <summary>
    /// Arash's bows in battle (F-54 to F-56, F-64). He carries one bow per slot, each with its own
    /// quiver; the player switches between them. Each bow has its own strength, reload and ability
    /// (fire, three arrows, piercing, Simurgh homing, haoma healing, Tishtrya's rain, lightning), and
    /// every bow but the plain wooden one leaves a coloured trail. Good shots fill the farr meter;
    /// when it is full, a rain of golden arrows can be called down on every enemy.
    /// </summary>
    public class PlayerArsenal : MonoBehaviour
    {
        const float BaseDamage = DamageRules.StandardArrowDamage;
        const float TripleSpread = 4f;
        const int PierceBodies = 3;
        const float SimurghTurnRate = 140f;
        const float HaomaHeal = 8f;
        const float FrostFactor = 1.8f;
        const float FrostSeconds = 4f;
        const float ThunderDamage = 45f;
        const float ThunderRange = 7f;
        const float RefillFraction = 0.35f;
        const int RainArrowsPerEnemy = 3;

        public class BowState
        {
            public ShopItem Item;
            public int Ammo;
            public int Capacity;
        }

        Bow bow;
        AimController aim;
        TrajectoryPreview preview;
        Health health;
        CharacterSkin skin;
        Arrow arrowPrefab;
        Arrow fireArrowPrefab;
        Func<IEnumerable<Combatant>> enemies;
        readonly List<BowState> bows = new List<BowState>();
        float basePreview;
        float rainUntil;

        public FarrMeter Farr { get; private set; }
        public IList<BowState> Bows { get { return bows; } }
        public int ActiveIndex { get; private set; }
        public BowState Active { get { return bows.Count > 0 ? bows[ActiveIndex] : null; } }
        public int TotalAmmo { get { return bows.Sum(b => b.Ammo); } }
        public float ReloadTime { get { return Active != null ? Active.Item.Reload : 0.75f; } }
        public bool CanRain { get { return Farr != null && Farr.IsFull; } }

        /// <summary>Raised when ammo, the active bow or the farr meter change.</summary>
        public event Action Changed;

        public void Configure(Bow playerBow, AimController playerAim, TrajectoryPreview playerPreview, Health playerHealth,
            CharacterSkin playerSkin, Arrow arrow, Arrow fireArrow, Func<IEnumerable<Combatant>> enemyList, Loadout loadout,
            float previewSeconds)
        {
            bow = playerBow;
            aim = playerAim;
            preview = playerPreview;
            health = playerHealth;
            skin = playerSkin;
            arrowPrefab = arrow;
            fireArrowPrefab = fireArrow;
            enemies = enemyList;
            basePreview = previewSeconds;
            foreach (var carried in loadout.Bows)
                bows.Add(new BowState { Item = carried.Item, Ammo = carried.Capacity, Capacity = carried.Capacity });
            Farr = new FarrMeter();
            Farr.Changed += Notify;
            aim.FireOverride = Fire;
            Select(0);
        }

        void OnEnable()
        {
            Health.AnyDamaged += OnAnyDamaged;
        }

        void OnDisable()
        {
            Health.AnyDamaged -= OnAnyDamaged;
        }

        public void Select(int index)
        {
            if (index < 0 || index >= bows.Count)
                return;
            ActiveIndex = index;
            var item = Active.Item;
            if (bow != null)
                bow.SetProjectile(null, 8f, item.MaxSpeed, 1f, 1f);
            // Hard difficulty has no aim guide at all (GDD 4.2); better bows lengthen it.
            if (preview != null)
                preview.Duration = basePreview > 0f ? basePreview + item.PreviewBonus : 0f;
            if (skin != null && !string.IsNullOrEmpty(item.Sprite))
            {
                var sprite = ArtLibrary.Get(item.Sprite);
                if (sprite != null)
                    skin.SetWeapon(sprite);
            }
            Notify();
        }

        /// <summary>The next bow that still has arrows, starting after the active one.</summary>
        public void SelectNext()
        {
            for (var i = 1; i <= bows.Count; i++)
            {
                var index = (ActiveIndex + i) % bows.Count;
                if (bows[index].Ammo > 0)
                {
                    Select(index);
                    return;
                }
            }
        }

        /// <summary>Supplies after a wave (F-54): each quiver gets back part of its arrows.</summary>
        public int Refill()
        {
            var added = 0;
            foreach (var state in bows)
            {
                var amount = Mathf.Min(state.Capacity - state.Ammo, Mathf.CeilToInt(state.Capacity * RefillFraction));
                state.Ammo += amount;
                added += amount;
            }
            Notify();
            return added;
        }

        Arrow Fire(AimState shot)
        {
            if (Active == null)
                return null;
            if (Active.Ammo <= 0)
            {
                SelectNext();
                if (Active.Ammo <= 0)
                    return null;
            }
            Active.Ammo--;
            var item = Active.Item;
            Arrow main;

            switch (item.Ability)
            {
                case BowAbility.Fire:
                    main = Launch(shot, fireArrowPrefab != null ? fireArrowPrefab : arrowPrefab, item, null);
                    break;
                case BowAbility.Triple:
                    Launch(new AimState(true, shot.Angle + TripleSpread, shot.Power, shot.Facing), arrowPrefab, item, null);
                    Launch(new AimState(true, shot.Angle - TripleSpread, shot.Power, shot.Facing), arrowPrefab, item, null);
                    main = Launch(shot, arrowPrefab, item, null);
                    break;
                case BowAbility.Golden:
                    main = Launch(shot, arrowPrefab, item, "arrow_gold");
                    break;
                default:
                    main = Launch(shot, arrowPrefab, item, null);
                    break;
            }

            if (main != null)
            {
                if (item.Ability == BowAbility.Piercing)
                    main.MakePiercing(PierceBodies);
                else if (item.Ability == BowAbility.Simurgh)
                    main.MakeHoming(() => NearestEnemy(main != null ? (Vector2)main.transform.position : Vector2.zero), SimurghTurnRate);
            }

            if (Active.Ammo <= 0)
                SelectNext();
            Notify();
            return main;
        }

        Arrow Launch(AimState shot, Arrow prefab, ShopItem item, string sprite)
        {
            if (bow == null || prefab == null)
                return null;
            var arrow = bow.FireWith(shot, prefab);
            if (arrow == null)
                return null;
            arrow.SetDamage(BaseDamage * item.Damage);
            if (sprite != null)
                SetArrowSprite(arrow, sprite);
            AddTrail(arrow, item.Trail);
            arrow.Intercepted += a => Farr.Add(FarrRules.Intercept);
            var ability = item.Ability;
            arrow.Stuck += (a, hit) => OnArrowHit(ability, hit);
            return arrow;
        }

        void OnArrowHit(BowAbility ability, ArrowHit hit)
        {
            var zone = hit.Collider != null ? hit.Collider.GetComponent<HitZone>() : null;
            var target = zone != null ? zone.Health : null;
            if (target == null || target == health || zone.Zone == HitZoneType.Armor)
                return;
            switch (ability)
            {
                case BowAbility.Fire:
                    Burning.Apply(target, 3f, 12f, transform);
                    break;
                case BowAbility.Haoma:
                    health.Heal(HaomaHeal);
                    BattleEffects.Pop("heal", (Vector2)health.transform.position + Vector2.up * 2.6f, 0.9f, Color.white, 0.7f);
                    break;
                case BowAbility.Frost:
                    var brain = target.GetComponent<EnemyBrain>();
                    if (brain != null)
                        brain.Slow(FrostFactor, FrostSeconds);
                    break;
                case BowAbility.Thunder:
                    BattleEffects.ChainLightning(target, enemies(), ThunderDamage, ThunderRange, transform);
                    break;
            }
        }

        /// <summary>The farr ability (F-64): golden arrows fall on every enemy.</summary>
        public void CastRain(float groundY)
        {
            if (!CanRain || !Farr.TrySpend())
                return;
            StartCoroutine(Rain(groundY));
        }

        IEnumerator Rain(float groundY)
        {
            rainUntil = Time.time + 3f;
            var targets = enemies().Where(e => e != null && e.IsAlive).ToList();
            for (var i = 0; i < RainArrowsPerEnemy; i++)
            {
                foreach (var enemy in targets)
                {
                    if (enemy == null || !enemy.IsAlive)
                        continue;
                    var tx = enemy.HeadTarget.position.x + Random.Range(-0.5f, 0.5f);
                    var start = new Vector2(tx + 2.5f, groundY + 16f);
                    var arrow = Instantiate(arrowPrefab, start, Quaternion.identity);
                    var fall = start.y - enemy.HeadTarget.position.y;
                    var time = fall / 24f;
                    arrow.Launch(new Vector2(-2.5f / time, -24f), Physics2D.gravity * 0.3f, transform);
                    arrow.SetDamage(BaseDamage);
                    SetArrowSprite(arrow, "arrow_gold");
                    AddTrail(arrow, new Color(1f, 0.85f, 0.35f));
                    Audio.AudioService.Play(Audio.Sfx.Whoosh, 0.5f);
                }
                yield return new WaitForSeconds(0.22f);
            }
        }

        static void SetArrowSprite(Arrow arrow, string name)
        {
            var sprite = ArtLibrary.Projectile(name);
            var renderer = arrow.GetComponentInChildren<SpriteRenderer>();
            if (sprite != null && renderer != null)
                renderer.sprite = sprite;
        }

        /// <summary>Bought bows leave a coloured trail (F-55); the starting bow leaves none.</summary>
        static void AddTrail(Arrow arrow, Color color)
        {
            if (color.a <= 0f)
                return;
            var holder = new GameObject("Trail");
            holder.transform.SetParent(arrow.transform, false);
            holder.transform.localPosition = new Vector3(-0.8f, 0f, 0f);
            var trail = holder.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.minVertexDistance = 0.06f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.16f * color.a + 0.04f, 1f, 0f);
            var gradient = new Gradient();
            var solid = color;
            solid.a = 1f;
            gradient.SetKeys(
                new[] { new GradientColorKey(solid, 0f), new GradientColorKey(solid, 1f) },
                new[] { new GradientAlphaKey(Mathf.Clamp01(color.a + 0.2f), 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.numCapVertices = 2;
            trail.sortingOrder = 39;
            if (ArtLibrary.SpriteMaterial != null)
                trail.sharedMaterial = ArtLibrary.SpriteMaterial;
            arrow.Stuck += (a, hit) => trail.emitting = false;
        }

        Transform NearestEnemy(Vector2 from)
        {
            var target = enemies().Where(e => e != null && e.IsAlive)
                .OrderBy(e => ((Vector2)e.BodyTarget.position - from).sqrMagnitude)
                .FirstOrDefault();
            return target != null ? target.BodyTarget : null;
        }

        void OnAnyDamaged(Health target, DamageInfo info, bool killed)
        {
            if (Farr == null || target == health || info.Source != transform || Time.time < rainUntil)
                return;
            Farr.Add(FarrRules.Gain(info.Zone, killed));
        }

        bool wasFull;

        void Notify()
        {
            if (Farr != null && Farr.IsFull && !wasFull)
                Audio.AudioService.Play(Audio.Sfx.FarrReady);
            wasFull = Farr != null && Farr.IsFull;
            if (Changed != null)
                Changed();
        }
    }
}
