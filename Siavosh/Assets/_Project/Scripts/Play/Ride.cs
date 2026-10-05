using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Play
{
    /// <summary>
    /// Siavosh riding Shabrang in a stage of the gallop: the horse runs on its own; the player
    /// leaps logs and stones, ducks under branches and, in the hunt, shoots from the saddle.
    /// Hitting an obstacle costs a heart.
    /// </summary>
    public class Rider : Entity
    {
        const float TopSpeed = 9.5f;
        const float Gravity = 34f;
        const float LeapSpeed = 13.5f;

        public Vector2 Position;
        public int Hearts;
        public int MaxHearts;
        public bool Fallen;
        float vy;
        bool grounded = true;
        bool usedDouble;
        float speed;
        float duck;
        float invulnerable;
        float shootCooldown;
        float hoof;
        bool hoofLeft;
        readonly HorsePuppet puppet;

        public Rider(Vector2 position)
        {
            Position = position;
            Root = new GameObject("Shabrang");
            puppet = HorsePuppet.Create(Root.transform, "shabrang", 200, "rider");
        }

        public void Init()
        {
            MaxHearts = Hearts = World.Stats.HorseHearts;
        }

        public float Speed { get { return speed; } }
        public float Progress { get { return Mathf.Clamp01(Position.x / Mathf.Max(1f, World.Layout.Length)); } }
        public bool Ducking { get { return duck > 0.5f; } }

        /// <summary>Horse and rider together, for picking things up.</summary>
        public Rect Bounds { get { return new Rect(Position.x - 1.6f, Position.y, 3.4f, Ducking ? 2.2f : 3.0f); } }

        Rect Legs { get { return new Rect(Position.x - 1.2f, Position.y, 2.7f, 0.9f); } }
        Rect RiderBox { get { return new Rect(Position.x - 0.2f, Position.y + 1.6f, 0.7f, Ducking ? 0.5f : 1.4f); } }

        public override void Tick(float dt)
        {
            if (Fallen)
            {
                puppet.Fallen = Mathf.MoveTowards(puppet.Fallen, 1f, dt * 2f);
                puppet.Speed = 0f;
                return;
            }

            speed = Mathf.MoveTowards(speed, TopSpeed, dt * 6f);
            invulnerable -= dt;
            shootCooldown -= dt;

            if (Controls.Pressed(Act.Jump))
            {
                if (grounded)
                {
                    vy = LeapSpeed;
                    grounded = false;
                    usedDouble = false;
                    AudioService.Play(Sfx.Jump, 0.6f);
                }
                else if (World.Stats.DoubleLeap && !usedDouble)
                {
                    vy = LeapSpeed * 0.85f;
                    usedDouble = true;
                    AudioService.Play(Sfx.Jump, 0.6f);
                    Fx.Dust(World, Position + new Vector2(0f, 0.3f));
                }
            }
            duck = Mathf.MoveTowards(duck, Controls.Held(Act.Duck) || Controls.AimY < -0.6f ? 1f : 0f, dt * 8f);

            Position.x += speed * dt;
            if (!grounded)
            {
                vy -= Gravity * dt;
                Position.y += vy * dt;
                if (Position.y <= 0f)
                {
                    Position.y = 0f;
                    vy = 0f;
                    grounded = true;
                    AudioService.Play(Sfx.Land, 0.5f);
                    Fx.Dust(World, Position + new Vector2(-1f, 0f));
                }
            }
            else
            {
                hoof -= dt;
                if (hoof <= 0f)
                {
                    hoof = 0.16f;
                    hoofLeft = !hoofLeft;
                    AudioService.Play(Sfx.Hoof, hoofLeft ? 0.35f : 0.25f);
                }
            }

            if (Controls.Pressed(Act.Bow) && World.Stage.BowAllowed && shootCooldown <= 0f)
                Shoot();

            Collide();

            if (Position.x >= World.Layout.Length)
                World.Finished = true;

            puppet.transform.position = Position;
            puppet.Speed = speed;
            puppet.Airborne = !grounded;
            puppet.Duck = duck;
            puppet.Stumble = Mathf.Clamp01(invulnerable - 0.6f);
            var blink = invulnerable > 0f && Mathf.Repeat(invulnerable * 10f, 1f) > 0.5f;
            puppet.SetAlpha(blink ? 0.5f : 1f);
        }

        void Collide()
        {
            if (invulnerable > 0f)
                return;
            foreach (var e in World.Entities)
            {
                var o = e as Obstacle;
                if (o == null || o.Removed || o.Passed)
                    continue;
                var hit = o.Kind == Thing.Branch ? o.Box.Overlaps(RiderBox) : o.Box.Overlaps(Legs);
                if (!hit)
                    continue;
                o.Passed = true;
                Hearts--;
                invulnerable = 1.4f;
                speed *= 0.55f;
                puppet.Flash(0.5f);
                AudioService.Play(Sfx.Hurt, 0.9f);
                World.Shake(0.7f);
                Settings.Vibrate();
                if (Hearts <= 0)
                {
                    Hearts = 0;
                    Fallen = true;
                    AudioService.Play(Sfx.Defeat, 0.8f);
                }
                return;
            }
        }

        void Shoot()
        {
            Onager best = null;
            var bestDistance = 22f;
            foreach (var e in World.Entities)
            {
                var o = e as Onager;
                if (o == null || o.Removed || !o.CanBeHit)
                    continue;
                var d = o.Position.x - Position.x;
                if (d > 1.5f && d < bestDistance)
                {
                    best = o;
                    bestDistance = d;
                }
            }
            var from = Position + new Vector2(0.6f, 2.5f);
            Vector2 velocity;
            if (best != null)
            {
                // Lead the running target.
                var flight = bestDistance / 26f;
                var aimAt = best.Position + new Vector2(best.Speed * flight - speed * flight * 0.5f, 0.9f);
                velocity = (Ballistics.Aim(from, aimAt, 26f) ?? new Vector2(26f, 1.5f)) + new Vector2(speed, 0f);
            }
            else
            {
                velocity = new Vector2(26f + speed, 2.5f);
            }
            World.Add(new Arrow(from, velocity, true, World.Stats.BowDamage * World.Stats.MountedShotMultiplier, this));
            AudioService.Play(Sfx.BowRelease, 0.8f);
            shootCooldown = 0.5f;
        }
    }

    /// <summary>A log or stone to leap, or a low branch to duck under.</summary>
    public class Obstacle : Entity
    {
        public readonly Thing Kind;
        public readonly Rect Box;
        public bool Passed;

        public Obstacle(Thing kind, float x)
        {
            Kind = kind;
            var name = kind == Thing.Log ? "props/log" : kind == Thing.Stone ? "props/stone" : "props/branch";
            var size = Art.Size(name);
            Root = new GameObject(kind.ToString());
            var sr = Root.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(name);
            sr.sortingOrder = kind == Thing.Branch ? 950 : 180;
            if (kind == Thing.Branch)
            {
                // Hangs from above: the rider must duck under it.
                const float bottom = 2.25f;
                Root.transform.position = new Vector3(x, bottom + size.y * 0.5f, 0f);
                Root.transform.localScale = new Vector3(-1f, 1f, 1f);
                Box = new Rect(x - size.x * 0.35f, bottom, size.x * 0.7f, 3f);
                var trunk = new GameObject("Trunk").AddComponent<SpriteRenderer>();
                trunk.transform.SetParent(Root.transform, false);
                trunk.transform.localPosition = new Vector3(-size.x * 0.55f, 1.5f, 0f);
                trunk.sprite = Art.White;
                trunk.drawMode = SpriteDrawMode.Sliced;
                trunk.size = new Vector2(0.35f, 6f);
                trunk.color = Palette.EarthDark;
                trunk.sortingOrder = 949;
            }
            else
            {
                Root.transform.position = new Vector3(x, size.y * 0.5f - 0.05f, 0f);
                Box = new Rect(x - size.x * 0.4f, 0f, size.x * 0.8f, size.y * 0.8f);
            }
        }

        public override void Tick(float dt)
        {
            if (World.Rider != null && World.Rider.Position.x - Box.xMax > 12f)
                Remove();
        }
    }

    /// <summary>A wild ass of the plain (or a foal, which must be spared) running ahead of the hunter.</summary>
    public class Onager : Entity, IHittable
    {
        public Vector2 Position;
        public float Speed;
        readonly bool foal;
        readonly bool goal;
        readonly HorsePuppet puppet;
        bool down;
        bool running;
        float downTime;

        static readonly LocText FoalHit = new LocText("تیر به کرّه‌گور خورد! رستم سر تکان می‌دهد.", "The arrow struck the foal! Rostam shakes his head.");

        public Onager(float x, bool foal, bool goal)
        {
            this.foal = foal;
            this.goal = goal;
            Position = new Vector2(x, 0f);
            Root = new GameObject(foal ? "Foal" : "Onager");
            puppet = HorsePuppet.Create(Root.transform, "onager", 150, null, foal ? 0.55f : 0.82f);
            puppet.Speed = 0f;
        }

        public Rect HitBounds
        {
            get
            {
                var s = foal ? 0.55f : 0.82f;
                return new Rect(Position.x - 1.9f * s, Position.y + 0.8f * s, 3.8f * s, 1.5f * s);
            }
        }

        public bool CanBeHit { get { return !down; } }

        public HitResult TakeHit(Hit hit)
        {
            if (down)
                return HitResult.Ignored;
            if (foal)
            {
                World.AddHonor(-1, FoalHit);
                World.Float(Position + Vector2.up * 2f, FoalHit.ToString(), Palette.Rose);
                puppet.Flash(0.4f);
                Speed += 3f;
                return HitResult.Landed;
            }
            down = true;
            World.Xp += 25;
            if (goal)
                World.CountGoal();
            AudioService.Play(Sfx.HitFlesh, 0.8f);
            Fx.Dust(World, Position);
            return HitResult.Defeated;
        }

        public override void Tick(float dt)
        {
            var rider = World.Rider;
            if (!running && rider != null && Position.x - rider.Position.x < 17f)
            {
                running = true;
                Speed = foal ? 7.2f : 7.8f;
            }
            if (down)
            {
                downTime += dt;
                Speed = Mathf.MoveTowards(Speed, 0f, dt * 12f);
                puppet.Fallen = Mathf.Clamp01(downTime * 2f);
            }
            Position.x += Speed * dt;
            if (running && !down)
                Position.y = Mathf.Abs(Mathf.Sin(Time.time * 5f + Position.x)) * 0.12f;
            puppet.transform.position = Position;
            puppet.Speed = Speed;
            if (rider != null && rider.Position.x - Position.x > 14f)
                Remove();
        }
    }
}
