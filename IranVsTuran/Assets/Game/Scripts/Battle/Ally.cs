using IranVsTuran.Art;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Battle
{
    public enum AllyKind
    {
        Soldier,
        Reinforcement,
        Hero,
    }

    /// <summary>
    /// A fighter on Iran's side: a barracks soldier, one of Kaveh's reinforcements, or the hero.
    /// Melee allies walk up to an enemy near their post and block it; ranged heroes shoot from
    /// where they stand. Soldiers and heroes come back after a while when they fall.
    /// </summary>
    public class Ally : Unit
    {
        public readonly AllyKind Kind;
        public Vector2 Home;
        public Vector2 SpawnPoint;
        public Enemy Target;
        public float DamageMin;
        public float DamageMax;
        public float Interval = 1f;
        public float Range;
        public DamageType DamageType = DamageType.Physical;
        public float EngageRadius = 1.4f;
        public float Speed = 1.4f;
        public float Regen;
        public float RespawnDelay = 10f;
        public float Lifetime;
        public Tower Owner;

        // hero
        public HeroDef HeroDef;
        public float AbilityCooldown;
        public float AbilityReady { get { return HeroDef == null ? 1f : 1f - Mathf.Clamp01(AbilityCooldown / HeroDef.abilityCooldown); } }
        float levelMultiplier = 1f;
        float invulnerable;
        bool ordered;

        float attackCooldown;
        public float RespawnTimer { get; private set; }

        public Ally(Battlefield field, AllyKind kind, Look look, Vector2 spawn, Vector2 home) : base(field)
        {
            Kind = kind;
            SpawnPoint = spawn;
            Position = spawn;
            Home = home;
            CreateVisual(kind.ToString(), ArtLibrary.Character(look), look.scale * (kind == AllyKind.Hero ? 1f : 0.85f), 0.72f);
        }

        public static Ally CreateHero(Battlefield field, HeroDef def, string skin, int level, Vector2 spawn, Vector2 home)
        {
            var hero = new Ally(field, AllyKind.Hero, HeroDefs.LookFor(def, skin), spawn, home)
            {
                HeroDef = def,
                levelMultiplier = HeroDefs.LevelMultiplier(level),
                Armor = def.armor,
                Range = def.range,
                DamageType = def.damageType,
                Interval = def.interval,
                Speed = def.speed,
                RespawnDelay = def.respawn,
                EngageRadius = 1.5f,
                Regen = 4f,
            };
            hero.MaxHp = hero.Hp = def.hp * hero.levelMultiplier;
            hero.DamageMin = def.damageMin * hero.levelMultiplier;
            hero.DamageMax = def.damageMax * hero.levelMultiplier;
            hero.AbilityCooldown = def.abilityCooldown * 0.5f;
            return hero;
        }

        /// <summary>Sends the hero (or reinforcement) somewhere; it walks there before picking fights.</summary>
        public void Order(Vector2 point)
        {
            Home = point;
            ordered = true;
            ReleaseTarget();
        }

        public void Stun(float seconds)
        {
            if (invulnerable <= 0f)
                StunTimer = Mathf.Max(StunTimer, seconds);
        }

        protected override float ModifyDamage(float amount)
        {
            return invulnerable > 0f ? 0f : amount;
        }

        protected override void OnDeath()
        {
            ReleaseTarget();
            if (Kind == AllyKind.Reinforcement)
            {
                field.Effects.Smoke(Position, 0.6f);
                Remove();
                return;
            }
            RespawnTimer = RespawnDelay;
            field.Effects.Smoke(Position, 0.6f);
            SetVisible(false);
        }

        void ReleaseTarget()
        {
            if (Target != null && Target.Blocker == this)
                Target.Blocker = null;
            Target = null;
        }

        public void Update(float dt)
        {
            if (Removed)
                return;

            if (Hp <= 0f)
            {
                RespawnTimer -= dt;
                if (RespawnTimer <= 0f)
                    Revive();
                return;
            }

            if (Kind == AllyKind.Reinforcement)
            {
                Lifetime -= dt;
                if (Lifetime <= 0f)
                {
                    ReleaseTarget();
                    field.Effects.Smoke(Position, 0.6f);
                    Remove();
                    return;
                }
            }

            invulnerable = Mathf.Max(0f, invulnerable - dt);
            Tint = invulnerable > 0f ? new Color(1f, 0.9f, 0.5f) : Color.white;
            if (Regen > 0f && Target == null)
                Heal(Regen * dt);

            Moving = false;
            if (StunTimer > 0f)
            {
                StunTimer -= dt;
                SyncVisual(dt);
                return;
            }

            if (HeroDef != null)
                UpdateAbility(dt);

            attackCooldown -= dt;
            if (ordered)
            {
                if (MoveTowards(Home, dt))
                    ordered = false;
                if (Range > 0f)
                    Shoot();
            }
            else if (Range > 0f)
            {
                if (!Shoot())
                    MoveTowards(Home, dt);
            }
            else
            {
                Melee(dt);
            }

            SyncVisual(dt);
        }

        bool MoveTowards(Vector2 point, float dt)
        {
            var delta = point - Position;
            if (delta.sqrMagnitude < 0.0025f)
                return true;
            var step = Speed * dt;
            Face(delta.x);
            Moving = true;
            if (delta.magnitude <= step)
            {
                Position = point;
                return true;
            }
            Position += delta.normalized * step;
            return false;
        }

        void Melee(float dt)
        {
            if (Target != null && (!Target.Alive || (Target.Position - Home).sqrMagnitude > (EngageRadius + 0.6f) * (EngageRadius + 0.6f)))
                ReleaseTarget();
            if (Target == null)
            {
                Target = field.FindEnemyFor(this);
                if (Target != null && (Target.Blocker == null || !Target.Blocker.Alive))
                    Target.Blocker = this;
            }
            if (Target == null)
            {
                MoveTowards(Home, dt);
                return;
            }

            // stand beside the enemy, on the side we came from
            var side = Position.x <= Target.Position.x ? -1f : 1f;
            var spot = Target.Position + new Vector2(side * 0.32f, -0.02f);
            if ((spot - Position).sqrMagnitude > 0.04f * 0.04f)
                MoveTowards(spot, dt);
            Face(Target.Position.x - Position.x);
            if ((Target.Position - Position).sqrMagnitude < 0.5f * 0.5f && attackCooldown <= 0f)
            {
                attackCooldown = Interval;
                Target.TakeDamage(Random.Range(DamageMin, DamageMax), DamageType);
                Lunge();
            }
        }

        bool Shoot()
        {
            var target = field.FirstEnemyInRange(Position, Range, true);
            if (target == null)
                return false;
            Face(target.Position.x - Position.x);
            if (attackCooldown <= 0f)
            {
                attackCooldown = Interval;
                var kind = DamageType == DamageType.Magic ? ProjectileKind.Light : ProjectileKind.Arrow;
                field.Projectiles.Add(Projectile.Homing(field, kind, Center, target, Random.Range(DamageMin, DamageMax), DamageType));
                Lunge();
            }
            return true;
        }

        void UpdateAbility(float dt)
        {
            AbilityCooldown -= dt;
            if (AbilityCooldown > 0f)
                return;
            var def = HeroDef;
            var radius = def.abilityRadius;
            var power = def.abilityValue * levelMultiplier;
            switch (def.ability)
            {
                case HeroAbility.Stomp:
                    if (field.CountEnemiesNear(Position, radius, false) == 0)
                        return;
                    field.DamageEnemies(Position, radius, power, DamageType.Physical, false, 1.5f);
                    field.Effects.Ring(Position, radius, new Color(1f, 0.85f, 0.4f, 0.9f), 0.4f);
                    field.Shake(0.15f);
                    field.Sound(Audio.Sfx.Boom, 0.7f);
                    break;
                case HeroAbility.Volley:
                    var shots = 0;
                    foreach (var enemy in field.Enemies)
                        if (enemy.Alive && (enemy.Position - Position).sqrMagnitude < radius * radius && shots < 8)
                        {
                            field.Projectiles.Add(Projectile.Homing(field, ProjectileKind.Arrow, Center, enemy, power, DamageType.Physical));
                            shots++;
                        }
                    if (shots == 0)
                        return;
                    field.Sound(Audio.Sfx.Bow, 0.8f);
                    break;
                case HeroAbility.Feather:
                    var hurt = Hp < MaxHp * 0.8f || field.CountEnemiesNear(Position, radius, true) > 0;
                    if (!hurt)
                        return;
                    field.HealAllies(Position, radius, power);
                    field.DamageEnemies(Position, radius, power * 0.6f, DamageType.Magic, true, 0f);
                    field.Effects.Burst(Position + new Vector2(0f, 0.3f), radius, new Color(0.4f, 1f, 0.8f, 0.7f));
                    field.Sound(Audio.Sfx.Magic, 0.8f);
                    break;
                case HeroAbility.Brazen:
                    if (Hp > MaxHp * 0.6f && field.CountEnemiesNear(Position, radius, false) < 3)
                        return;
                    invulnerable = def.abilityValue;
                    field.DamageEnemies(Position, radius, 40f * levelMultiplier, DamageType.Physical, false, 0f);
                    field.Effects.Ring(Position, radius, new Color(1f, 0.85f, 0.3f, 0.9f), 0.5f);
                    field.Sound(Audio.Sfx.Clash, 0.9f);
                    break;
            }
            AbilityCooldown = def.abilityCooldown;
            Lunge();
        }

        void Revive()
        {
            Hp = MaxHp;
            Position = SpawnPoint;
            StunTimer = 0f;
            ordered = false;
            SetVisible(true);
            field.Effects.Sparkle(Center, Color.white);
        }

        public override void Remove()
        {
            ReleaseTarget();
            base.Remove();
        }
    }
}
