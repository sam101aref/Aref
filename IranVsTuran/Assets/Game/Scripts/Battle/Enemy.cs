using IranVsTuran.Art;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Battle
{
    /// <summary>
    /// An enemy walking its path. It stops to fight a soldier who blocks it, shoots soldiers if it
    /// is an archer, uses its abilities on their cooldowns, and costs lives if it gets through.
    /// </summary>
    public class Enemy : Unit
    {
        public readonly EnemyDef Def;
        public readonly PathTrack Track;
        public float Distance;
        readonly float lateral;
        public Ally Blocker;

        float attackCooldown;
        float rangedCooldown;
        readonly float[] abilityCooldowns;
        bool enraged;
        float slowFactor = 1f;
        float slowTimer;
        float burnDps;
        float burnTimer;

        public bool Flying { get { return Def.flying; } }
        public bool Boss { get { return Def.boss; } }
        public int PathIndex { get; private set; }

        public Enemy(Battlefield field, EnemyDef def, int pathIndex, float distance, float hpMultiplier) : base(field)
        {
            Def = def;
            PathIndex = pathIndex;
            Track = field.Tracks[pathIndex];
            Distance = distance;
            lateral = Random.Range(-0.28f, 0.28f) * (def.boss ? 0.3f : 1f);
            MaxHp = Hp = def.hp * hpMultiplier;
            Armor = def.armor;
            MagicResist = def.magicResist;
            abilityCooldowns = new float[def.abilities.Length];
            for (var i = 0; i < abilityCooldowns.Length; i++)
                abilityCooldowns[i] = def.abilities[i].cooldown * Random.Range(0.4f, 0.8f);

            Position = PathPosition();
            CreateVisual(def.id, ArtLibrary.Character(def.look), def.look.scale, def.flying ? 1.0f : 0.72f);
            if (def.flying)
                body.localPosition = Vector3.zero;
        }

        Vector2 PathPosition()
        {
            Vector2 direction;
            var point = Track.At(Distance, out direction);
            return point + new Vector2(-direction.y, direction.x) * lateral;
        }

        /// <summary>Where this enemy will be after <paramref name="seconds"/> if it keeps walking.</summary>
        public Vector2 Predict(float seconds)
        {
            if (IsHeld)
                return Position;
            Vector2 direction;
            var point = Track.At(Distance + CurrentSpeed * seconds, out direction);
            return point + new Vector2(-direction.y, direction.x) * lateral;
        }

        bool IsHeld
        {
            get { return field.FreezeTimer > 0f || StunTimer > 0f || IsBlocked; }
        }

        bool IsBlocked
        {
            get { return Blocker != null && Blocker.Alive && (Blocker.Position - Position).sqrMagnitude < 0.6f * 0.6f; }
        }

        float CurrentSpeed
        {
            get { return Def.speed * slowFactor * (enraged ? EnrageValue : 1f); }
        }

        float EnrageValue
        {
            get
            {
                foreach (var ability in Def.abilities)
                    if (ability.type == EnemyAbility.Enrage)
                        return ability.value;
                return 1f;
            }
        }

        public void Slow(float fraction, float seconds)
        {
            slowFactor = Mathf.Min(slowFactor, 1f - fraction);
            slowTimer = Mathf.Max(slowTimer, seconds);
        }

        public void Burn(float dps, float seconds)
        {
            burnDps = Mathf.Max(burnDps, dps);
            burnTimer = Mathf.Max(burnTimer, seconds);
        }

        public void Stun(float seconds)
        {
            if (!Def.boss)
                StunTimer = Mathf.Max(StunTimer, seconds);
        }

        public void Update(float dt)
        {
            if (!Alive)
                return;

            if (slowTimer > 0f)
            {
                slowTimer -= dt;
                if (slowTimer <= 0f)
                    slowFactor = 1f;
            }
            if (burnTimer > 0f)
            {
                burnTimer -= dt;
                TakeDamage(burnDps * dt, DamageType.True);
                if (!Alive)
                    return;
            }
            if (Blocker != null && !Blocker.Alive)
                Blocker = null;

            UpdateAbilities(dt);
            if (!Alive)
                return;

            var frozen = field.FreezeTimer > 0f;
            Tint = frozen ? new Color(0.6f, 0.85f, 1f) : enraged ? new Color(1f, 0.6f, 0.55f) : Color.white;
            if (StunTimer > 0f)
                StunTimer -= dt;

            Moving = false;
            if (!frozen && StunTimer <= 0f)
            {
                if (IsBlocked)
                {
                    Face(Blocker.Position.x - Position.x);
                    attackCooldown -= dt * (enraged ? EnrageValue : 1f);
                    if (attackCooldown <= 0f && Def.meleeMax > 0f)
                    {
                        attackCooldown = Def.attackInterval;
                        Blocker.TakeDamage(Random.Range(Def.meleeMin, Def.meleeMax), DamageType.Physical);
                        Lunge();
                        field.Sound(Audio.Sfx.Clash, 0.35f);
                    }
                }
                else
                {
                    var before = Position;
                    Distance += CurrentSpeed * dt;
                    Position = PathPosition();
                    Face(Position.x - before.x);
                    Moving = true;
                    if (Distance >= Track.Length - 0.05f)
                    {
                        field.EnemyEscaped(this);
                        return;
                    }
                }

                if (Def.rangedRange > 0f)
                    UpdateRanged(dt);
            }

            SyncVisual(dt);
        }

        void UpdateRanged(float dt)
        {
            rangedCooldown -= dt;
            if (rangedCooldown > 0f)
                return;
            var target = field.NearestAlly(Position, Def.rangedRange);
            if (target == null)
                return;
            rangedCooldown = Def.rangedInterval;
            Face(target.Position.x - Position.x);
            field.FireAtAlly(Center, target, Random.Range(Def.rangedMin, Def.rangedMax));
        }

        void UpdateAbilities(float dt)
        {
            for (var i = 0; i < Def.abilities.Length; i++)
            {
                var ability = Def.abilities[i];
                switch (ability.type)
                {
                    case EnemyAbility.Regenerate:
                        Heal(ability.value * dt);
                        continue;
                    case EnemyAbility.Enrage:
                        if (!enraged && Hp < MaxHp * 0.5f)
                        {
                            enraged = true;
                            field.Effects.Ring(Position, 1.2f, new Color(1f, 0.3f, 0.2f, 0.8f), 0.5f);
                        }
                        continue;
                    case EnemyAbility.SpawnOnDeath:
                        continue;
                }

                if (field.FreezeTimer > 0f || StunTimer > 0f)
                    continue;
                abilityCooldowns[i] -= dt;
                if (abilityCooldowns[i] > 0f)
                    continue;
                if (Use(ability))
                    abilityCooldowns[i] = ability.cooldown;
                else
                    abilityCooldowns[i] = 0.5f; // nothing to do yet; look again soon
            }
        }

        bool Use(AbilitySpec ability)
        {
            switch (ability.type)
            {
                case EnemyAbility.HealAllies:
                    var healed = false;
                    foreach (var other in field.Enemies)
                        if (other != this && other.Alive && other.Hp < other.MaxHp &&
                            (other.Position - Position).sqrMagnitude < ability.radius * ability.radius)
                        {
                            other.Heal(ability.value);
                            field.Effects.Sparkle(other.Center, new Color(0.4f, 1f, 0.5f));
                            healed = true;
                        }
                    if (healed)
                        Lunge();
                    return healed;

                case EnemyAbility.Summon:
                    for (var k = 0; k < ability.count; k++)
                        field.SpawnEnemy(EnemyDefs.Get(ability.summonId), PathIndex, Mathf.Max(0f, Distance - 0.25f - k * 0.3f));
                    field.Effects.Smoke(Position, 1.2f);
                    Lunge();
                    return true;

                case EnemyAbility.FireBreath:
                    if (!field.AnyAllyNear(Position, ability.radius))
                        return false;
                    field.DamageAllies(Position, ability.radius, ability.value);
                    field.Effects.Burst(Position + new Vector2(0f, 0.3f), ability.radius, new Color(1f, 0.5f, 0.1f, 0.85f));
                    field.Sound(Audio.Sfx.Fire, 0.7f);
                    Lunge();
                    return true;

                case EnemyAbility.Stomp:
                    if (!field.AnyAllyNear(Position, ability.radius))
                        return false;
                    field.StunAllies(Position, ability.radius, ability.value, 20f);
                    field.Effects.Ring(Position, ability.radius, new Color(0.9f, 0.85f, 0.7f, 0.9f), 0.45f);
                    field.Shake(0.25f);
                    field.Sound(Audio.Sfx.Boom, 0.8f);
                    return true;

                case EnemyAbility.DisableTower:
                    var tower = field.NearestTower(Position, ability.radius);
                    if (tower == null)
                        return false;
                    field.Projectiles.Add(Projectile.Rock(field, Center, tower, ability.value));
                    Lunge();
                    return true;

                case EnemyAbility.Teleport:
                    if (Distance > Track.Length - ability.value - 1.5f)
                        return false;
                    field.Effects.Smoke(Position, 1f);
                    Distance += ability.value;
                    Blocker = null;
                    Position = PathPosition();
                    field.Effects.Smoke(Position, 1f);
                    field.Sound(Audio.Sfx.Whoosh, 0.8f);
                    return true;
            }
            return false;
        }

        protected override void OnDeath()
        {
            field.EnemyKilled(this);
            foreach (var ability in Def.abilities)
                if (ability.type == EnemyAbility.SpawnOnDeath)
                    for (var k = 0; k < ability.count; k++)
                        field.SpawnEnemy(EnemyDefs.Get(ability.summonId), PathIndex, Mathf.Max(0f, Distance - k * 0.25f));
        }
    }
}
