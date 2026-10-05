using IranVsTuran.Art;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Battle
{
    /// <summary>
    /// Arrows, sacred fire and light that home in on an enemy; stones and naphtha jars lobbed
    /// at a spot; enemy arrows aimed at soldiers; and the rock bosses throw to silence towers.
    /// </summary>
    public class Projectile
    {
        enum Mode
        {
            HomingEnemy,
            HomingAlly,
            Lobbed,
            Rock,
            Rain,
        }

        readonly Battlefield field;
        Mode mode;
        ProjectileKind kind;
        Vector2 position;
        Vector2 start;
        Vector2 end;
        Enemy target;
        Ally allyTarget;
        Tower towerTarget;
        float speed;
        float flight;
        float time;
        float arc;
        float damage;
        DamageType damageType;
        float splash;
        bool crit;
        int chains;
        public TowerSpecial Special;
        public float SpecialValue;
        GameObject root;
        SpriteRenderer renderer;

        public bool Done { get; private set; }

        Projectile(Battlefield field)
        {
            this.field = field;
        }

        public static Projectile Homing(Battlefield field, ProjectileKind kind, Vector2 from, Enemy target, float damage,
            DamageType type, bool crit = false)
        {
            var p = new Projectile(field)
            {
                mode = Mode.HomingEnemy, kind = kind, position = from, target = target, damage = damage, damageType = type, crit = crit,
                speed = kind == ProjectileKind.Arrow ? 10f : kind == ProjectileKind.Light ? 8f : 6.5f,
                end = target.Center,
            };
            if (kind == ProjectileKind.Light)
                p.chains = 2;
            p.CreateVisual();
            return p;
        }

        public static Projectile Lobbed(Battlefield field, ProjectileKind kind, Vector2 from, Vector2 point, float seconds,
            float damage, float splash, DamageType type = DamageType.Physical)
        {
            var p = new Projectile(field)
            {
                mode = Mode.Lobbed, kind = kind, start = from, position = from, end = point, flight = seconds, damage = damage,
                splash = splash, damageType = type, arc = 1.6f + Vector2.Distance(from, point) * 0.15f,
            };
            p.CreateVisual();
            return p;
        }

        public static Projectile AtAlly(Battlefield field, Vector2 from, Ally target, float damage)
        {
            var p = new Projectile(field)
            {
                mode = Mode.HomingAlly, kind = ProjectileKind.Arrow, position = from, allyTarget = target, damage = damage,
                speed = 8f, end = target.Center,
            };
            p.CreateVisual();
            p.renderer.color = new Color(0.8f, 0.6f, 0.6f);
            return p;
        }

        public static Projectile Rock(Battlefield field, Vector2 from, Tower tower, float disableSeconds)
        {
            var p = new Projectile(field)
            {
                mode = Mode.Rock, kind = ProjectileKind.Boulder, start = from, position = from, towerTarget = tower,
                end = tower.Position + new Vector2(0f, 0.4f), flight = 1.1f, arc = 2.2f, damage = disableSeconds,
            };
            p.CreateVisual();
            p.root.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            return p;
        }

        public static Projectile RainArrow(Battlefield field, Vector2 point, float damage, float delay)
        {
            var p = new Projectile(field)
            {
                mode = Mode.Rain, kind = ProjectileKind.Arrow, start = point + new Vector2(1.2f, 6f), end = point, flight = 0.55f,
                time = -delay, damage = damage, splash = 0.5f, damageType = DamageType.Physical,
            };
            p.position = p.start;
            p.CreateVisual();
            p.root.SetActive(false);
            return p;
        }

        void CreateVisual()
        {
            root = new GameObject("Projectile");
            root.transform.SetParent(field.transform, false);
            renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = Depth.Projectiles;
            switch (kind)
            {
                case ProjectileKind.Arrow: renderer.sprite = ArtLibrary.Arrow(); break;
                case ProjectileKind.Fire: renderer.sprite = ArtLibrary.Fireball(); break;
                case ProjectileKind.Light: renderer.sprite = ArtLibrary.LightOrb(); break;
                case ProjectileKind.Boulder: renderer.sprite = ArtLibrary.Boulder(); break;
                case ProjectileKind.Naphtha: renderer.sprite = ArtLibrary.Jar(); break;
            }
            if (crit)
                root.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            Place(position, Vector2.right);
        }

        void Place(Vector2 p, Vector2 velocity)
        {
            root.transform.localPosition = new Vector3(p.x, p.y, 0f);
            if (kind == ProjectileKind.Arrow && velocity.sqrMagnitude > 0.0001f)
                root.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            else if (kind == ProjectileKind.Boulder || kind == ProjectileKind.Naphtha)
                root.transform.localRotation = Quaternion.Euler(0f, 0f, -time * 400f);
        }

        public void Update(float dt)
        {
            if (Done)
                return;
            switch (mode)
            {
                case Mode.HomingEnemy:
                    if (target != null && target.Alive)
                        end = target.Center;
                    Home(dt, () =>
                    {
                        if (target != null && target.Alive)
                            HitEnemy(target);
                    });
                    break;
                case Mode.HomingAlly:
                    if (allyTarget != null && allyTarget.Alive)
                        end = allyTarget.Center;
                    Home(dt, () =>
                    {
                        if (allyTarget != null && allyTarget.Alive)
                            allyTarget.TakeDamage(damage, DamageType.Physical);
                    });
                    break;
                default:
                    Arc(dt);
                    break;
            }
        }

        void Home(float dt, System.Action onHit)
        {
            var delta = end - position;
            var step = speed * dt;
            if (delta.magnitude <= step + 0.05f)
            {
                position = end;
                onHit();
                Finish();
                return;
            }
            position += delta.normalized * step;
            Place(position, delta);
        }

        void Arc(float dt)
        {
            time += dt;
            if (time < 0f)
                return;
            if (!root.activeSelf)
                root.SetActive(true);
            var t = Mathf.Clamp01(time / flight);
            var previous = position;
            position = Vector2.Lerp(start, end, t) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * arc);
            Place(position, position - previous);
            if (t < 1f)
                return;

            switch (mode)
            {
                case Mode.Lobbed:
                    Impact();
                    break;
                case Mode.Rock:
                    if (towerTarget != null)
                    {
                        towerTarget.DisabledTimer = Mathf.Max(towerTarget.DisabledTimer, damage);
                        field.Effects.Smoke(end, 1f);
                        field.Sound(Audio.Sfx.Boom, 0.5f);
                    }
                    break;
                case Mode.Rain:
                    field.DamageEnemies(end, splash, damage, DamageType.Physical, true, 0f);
                    field.Effects.Sparkle(end, new Color(1f, 0.95f, 0.8f));
                    break;
            }
            Finish();
        }

        void Impact()
        {
            var stun = Special == TowerSpecial.Stun ? SpecialValue : 0f;
            field.DamageEnemies(end, splash, damage, damageType, false, stun);
            if (kind == ProjectileKind.Naphtha)
            {
                field.Effects.Burst(end, splash * 1.1f, new Color(1f, 0.5f, 0.1f, 0.85f));
                if (Special == TowerSpecial.BurningGround)
                    field.AddFire(end, splash * 0.8f, SpecialValue, 3f);
                field.Sound(Audio.Sfx.Fire, 0.6f);
            }
            else
            {
                field.Effects.Ring(end, splash, new Color(0.85f, 0.75f, 0.55f, 0.8f), 0.35f);
                field.Effects.Smoke(end, splash * 0.8f);
                field.Sound(Audio.Sfx.Boom, 0.5f);
            }
        }

        void HitEnemy(Enemy enemy)
        {
            enemy.TakeDamage(damage, damageType);
            switch (kind)
            {
                case ProjectileKind.Arrow:
                    if (crit)
                        field.Effects.Sparkle(enemy.Center, new Color(1f, 0.85f, 0.3f));
                    field.Sound(Audio.Sfx.Hit, 0.25f);
                    break;
                case ProjectileKind.Fire:
                    field.Effects.Burst(enemy.Center, 0.35f, new Color(1f, 0.55f, 0.15f, 0.8f));
                    if (Special == TowerSpecial.Burn && enemy.Alive)
                        enemy.Burn(SpecialValue, 3f);
                    break;
                case ProjectileKind.Light:
                    field.Effects.Burst(enemy.Center, 0.3f, new Color(0.6f, 0.9f, 1f, 0.8f));
                    if (Special == TowerSpecial.SlowChain)
                    {
                        if (enemy.Alive)
                            enemy.Slow(SpecialValue, 2f);
                        if (chains > 0)
                        {
                            var next = field.NearestEnemyExcept(enemy.Position, 1.6f, enemy);
                            if (next != null)
                            {
                                var bolt = Homing(field, ProjectileKind.Light, enemy.Center, next, damage * 0.6f, DamageType.Magic);
                                bolt.Special = Special;
                                bolt.SpecialValue = SpecialValue;
                                bolt.chains = chains - 1;
                                field.Projectiles.Add(bolt);
                            }
                        }
                    }
                    break;
            }
        }

        void Finish()
        {
            Done = true;
            if (root != null)
                Object.Destroy(root);
        }

        public void Discard()
        {
            Finish();
        }
    }
}
