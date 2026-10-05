using System.Collections.Generic;
using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Play
{
    /// <summary>
    /// Siavosh on foot. Runs and jumps (with a little forgiveness at ledge edges), strikes in
    /// combos, charges a heavy blow, guards and parries with the shield, rolls through danger, draws
    /// the bow with an aiming guide, and — once Rostam has taught him — unleashes Rostam's Fist.
    /// Numbers come from <see cref="HeroStats"/> (level, skills, gear, difficulty).
    /// </summary>
    public class Hero : Entity
    {
        enum State
        {
            Free,
            Attack,
            Charge,
            Bow,
            Dodge,
            Hurt,
            Dead,
        }

        const float RunSpeed = 6.5f;
        const float JumpSpeed = 14.2f;
        const float SwingTime = 0.3f;
        const float ParryWindow = 0.22f;

        public readonly Body Body = new Body();
        public bool FacingRight = true;
        public float Health;
        public int MaxHealth;
        public float Farr;
        public bool Alive { get { return state != State.Dead; } }
        public bool CanBeHit { get { return state != State.Dead && invulnerable <= 0f; } }
        public bool Blocking { get; private set; }
        public bool Drawing { get { return state == State.Bow; } }
        public bool FarrReady { get { return World.Stats.HasFarr && Farr >= 1f; } }

        readonly HumanPuppet puppet;
        readonly SpriteRenderer guard;
        readonly List<SpriteRenderer> preview = new List<SpriteRenderer>();
        readonly HashSet<IHittable> struck = new HashSet<IHittable>();
        State state;
        float stateTime;
        int combo;
        bool comboQueued;
        bool heavySwing;
        float invulnerable;
        float coyote;
        float jumpBuffer;
        float blockStart = -10f;
        float dodgeCooldown;
        float draw;
        float aim = 12f;
        float landSquash;
        Vector2 lastSafe;
        float time;

        public Hero(Vector2 position)
        {
            Body.Position = position;
            lastSafe = position;
            Root = new GameObject("Siavosh");
            puppet = HumanPuppet.Create(Root.transform, "siavosh", 200);
            guard = new GameObject("Guard").AddComponent<SpriteRenderer>();
            guard.transform.SetParent(Root.transform, false);
            guard.sprite = Art.Glow;
            guard.color = new Color(1f, 0.85f, 0.45f, 0f);
            guard.sortingOrder = 210;
            guard.transform.localScale = new Vector3(1.6f, 2.6f, 1f);
            for (var i = 0; i < 14; i++)
            {
                var dot = new GameObject("Aim").AddComponent<SpriteRenderer>();
                dot.transform.SetParent(Root.transform, false);
                dot.sprite = Art.Circle;
                dot.transform.localScale = Vector3.one * (0.16f - i * 0.006f);
                dot.color = new Color(1f, 0.95f, 0.75f, 0.85f - i * 0.05f);
                dot.sortingOrder = 960;
                dot.gameObject.SetActive(false);
                preview.Add(dot);
            }
        }

        public void Init()
        {
            MaxHealth = World.Stats.MaxHealth;
            Health = MaxHealth;
            Body.MinX = World.MinX;
            Body.MaxX = World.MaxX;
        }

        public void Heal(int amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        public void GainFarr(float amount)
        {
            if (!World.Stats.HasFarr || Farr >= 1f)
                return;
            Farr = Mathf.Min(1f, Farr + amount);
            if (Farr >= 1f)
                AudioService.Play(Sfx.FarrReady, 0.8f);
        }

        public void Push(float vx)
        {
            Body.Velocity.x = vx;
        }

        public HitResult TakeHit(Hit hit)
        {
            if (!CanBeHit)
                return HitResult.Ignored;
            var attackerSide = hit.Source is Fighter ? Mathf.Sign(((Fighter)hit.Source).Body.Position.x - Body.Position.x)
                : hit.Source is RostamBoss ? Mathf.Sign(((RostamBoss)hit.Source).Body.Position.x - Body.Position.x)
                : -hit.Direction;
            var facing = FacingRight ? 1f : -1f;
            if (Blocking && attackerSide == facing && hit.Kind != HitKind.Heavy)
            {
                if (World.Stats.CanParry && time - blockStart <= ParryWindow)
                {
                    AudioService.Play(Sfx.Parry, 0.9f);
                    Fx.Spark(World, Body.Center + new Vector2(facing * 0.6f, 0.4f));
                    Fx.Ring(World, Body.Center + new Vector2(facing * 0.6f, 0.4f), 0.9f, new Color(0.6f, 1f, 0.95f, 0.8f));
                    World.Float(Body.Center + Vector2.up * 1.4f, Ui.Parry.ToString(), Palette.TurquoiseLight);
                    var stunnable = hit.Source as IStunnable;
                    if (stunnable != null)
                        stunnable.Stun(1.3f);
                    GainFarr(0.15f);
                    return HitResult.Blocked;
                }
                AudioService.Play(Sfx.Block, 0.8f);
                Fx.Spark(World, Body.Center + new Vector2(facing * 0.5f, 0.3f));
                Damage(hit.Damage * 0.2f);
                Body.Velocity.x = hit.Direction * 2f;
                return HitResult.Blocked;
            }

            Damage(hit.Damage * World.Stats.DamageTaken);
            Body.Velocity.x = hit.Direction * hit.Knockback;
            Body.Velocity.y = 4f;
            if (state != State.Dead)
            {
                SetState(State.Hurt);
                invulnerable = 0.7f;
            }
            return state == State.Dead ? HitResult.Defeated : HitResult.Landed;
        }

        void Damage(float amount)
        {
            Health -= amount;
            puppet.Flash(new Color(1f, 0.45f, 0.4f), 0.18f);
            AudioService.Play(Sfx.Hurt, 0.8f);
            World.Shake(0.5f);
            Settings.Vibrate();
            if (Health <= 0f)
            {
                Health = 0f;
                SetState(State.Dead);
                AudioService.Play(Sfx.Defeat, 0.8f);
            }
        }

        void SetState(State next)
        {
            if (state == State.Bow && next != State.Bow)
                puppet.SetArm("arm");
            state = next;
            stateTime = 0f;
        }

        public override void Tick(float dt)
        {
            time += dt;
            stateTime += dt;
            invulnerable -= dt;
            dodgeCooldown -= dt;
            jumpBuffer -= dt;
            coyote = Body.Grounded ? 0.1f : coyote - dt;
            landSquash = Mathf.MoveTowards(landSquash, 0f, dt * 5f);

            var stats = World.Stats;
            var move = Controls.MoveX;
            var targetVx = 0f;
            Blocking = false;

            if (state != State.Dead && Controls.Pressed(Act.Jump))
                jumpBuffer = 0.14f;

            switch (state)
            {
                case State.Free:
                    targetVx = move * RunSpeed;
                    if (Mathf.Abs(move) > 0.1f)
                        FacingRight = move > 0f;
                    if (Controls.Pressed(Act.Shield))
                        blockStart = time; // the parry window opens when the shield goes up
                    if (Controls.Held(Act.Shield) && Body.Grounded)
                    {
                        Blocking = true;
                        targetVx = move * RunSpeed * 0.25f;
                    }

                    if (jumpBuffer > 0f && coyote > 0f && !Blocking)
                    {
                        Body.Velocity.y = JumpSpeed;
                        jumpBuffer = 0f;
                        coyote = 0f;
                        AudioService.Play(Sfx.Jump, 0.5f);
                        Fx.Dust(World, Body.Position);
                    }
                    if (Controls.Released(Act.Jump) && Body.Velocity.y > 4f)
                        Body.Velocity.y *= 0.55f;

                    if (Controls.Pressed(Act.Attack))
                        StartSwing(1, false);
                    else if (Controls.Pressed(Act.Dodge) && dodgeCooldown <= 0f && Body.Grounded)
                    {
                        SetState(State.Dodge);
                        invulnerable = 0.32f;
                        dodgeCooldown = 0.55f;
                        AudioService.Play(Sfx.Jump, 0.4f);
                        Fx.Dust(World, Body.Position);
                    }
                    else if (Controls.Pressed(Act.Bow) && World.Stage.BowAllowed)
                    {
                        SetState(State.Bow);
                        draw = 0f;
                        puppet.SetArm("armbow");
                    }
                    else if (Controls.Pressed(Act.Farr) && FarrReady)
                        FarrBlast();
                    else if (Controls.Pressed(Act.Interact) && World.Interaction != null)
                        World.Interaction.Interact();
                    break;

                case State.Attack:
                    UpdateSwing(dt, ref targetVx);
                    break;

                case State.Charge:
                    puppet.ArmAngle = Mathf.MoveTowards(puppet.ArmAngle, 140f, dt * 600f);
                    if (stateTime > 0.45f && Mathf.Repeat(stateTime * 8f, 1f) > 0.5f)
                        puppet.Flash(Palette.GoldLight, 0.05f);
                    if (!Controls.Held(Act.Attack))
                    {
                        if (stateTime >= 0.45f)
                            StartSwing(99, true);
                        else
                            SetState(State.Free);
                    }
                    break;

                case State.Bow:
                    if (Mathf.Abs(move) > 0.4f)
                        FacingRight = move > 0f;
                    draw = Mathf.Min(1f, draw + dt / Mathf.Max(0.1f, stats.DrawTime));
                    aim = Mathf.Clamp(aim + Controls.AimY * dt * 70f, -30f, 62f);
                    ShowPreview(true);
                    if (!Controls.Held(Act.Bow))
                    {
                        ShowPreview(false);
                        if (draw >= 0.25f)
                            Shoot();
                        SetState(State.Free);
                    }
                    break;

                case State.Dodge:
                    targetVx = (FacingRight ? 1f : -1f) * 11f;
                    Body.Velocity.x = targetVx;
                    if (stateTime >= 0.32f)
                        SetState(State.Free);
                    break;

                case State.Hurt:
                    targetVx = Body.Velocity.x;
                    if (stateTime >= 0.32f)
                        SetState(State.Free);
                    break;

                case State.Dead:
                    targetVx = 0f;
                    break;
            }

            if (state != State.Bow)
                ShowPreview(false);

            if (state == State.Free || state == State.Bow || state == State.Charge)
                Body.Velocity.x = Mathf.MoveTowards(Body.Velocity.x, state == State.Free ? targetVx : 0f, dt * (Body.Grounded ? 60f : 35f));
            else if (state == State.Hurt || state == State.Dead)
                Body.Velocity.x = Mathf.MoveTowards(Body.Velocity.x, 0f, dt * 10f);

            if (Body.Step(dt, World.Solids))
            {
                landSquash = 1f;
                AudioService.Play(Sfx.Land, 0.4f);
                Fx.Dust(World, Body.Position);
            }

            if (Body.Grounded)
            {
                var surface = Body.SurfaceAt(Body.Position.x - 0.6f, World.Solids, Body.Position.y + 0.05f);
                var surface2 = Body.SurfaceAt(Body.Position.x + 0.6f, World.Solids, Body.Position.y + 0.05f);
                if (Mathf.Abs(surface - Body.Position.y) < 0.05f && Mathf.Abs(surface2 - Body.Position.y) < 0.05f)
                    lastSafe = Body.Position;
            }
            if (Body.Position.y < -4.5f && state != State.Dead)
            {
                Damage(15f);
                Body.Position = lastSafe + new Vector2(FacingRight ? -0.4f : 0.4f, 0.2f);
                Body.Velocity = Vector2.zero;
                invulnerable = 1f;
                World.Shake(0.4f);
            }

            Pose();
        }

        // ------------------------------------------------------------- sword

        void StartSwing(int step, bool heavy)
        {
            SetState(State.Attack);
            combo = step;
            heavySwing = heavy;
            comboQueued = false;
            struck.Clear();
            AudioService.Play(Sfx.Swing, heavy ? 1f : 0.7f);
        }

        void UpdateSwing(float dt, ref float targetVx)
        {
            var duration = heavySwing ? 0.42f : SwingTime;
            var t = stateTime / duration;
            // Wind up, cut through, follow through.
            if (t < 0.25f)
                puppet.ArmAngle = Mathf.Lerp(heavySwing ? 140f : 75f, 115f, t / 0.25f);
            else if (t < 0.6f)
                puppet.ArmAngle = Mathf.Lerp(115f, -55f, (t - 0.25f) / 0.35f);
            else
                puppet.ArmAngle = Mathf.Lerp(-55f, -10f, (t - 0.6f) / 0.4f);

            var active = t >= 0.25f && t < 0.62f;
            Body.Velocity.x = Mathf.MoveTowards(Body.Velocity.x, active ? (FacingRight ? 2.2f : -2.2f) : 0f, dt * 40f);
            if (active)
            {
                if (struck.Count == 0 && t - dt / duration < 0.25f)
                    Fx.Slash(World, Body.Position + new Vector2(FacingRight ? 1.0f : -1.0f, 1.3f), FacingRight, heavySwing);
                var dir = FacingRight ? 1f : -1f;
                var reach = heavySwing ? 1.9f : 1.6f;
                var box = new Rect(dir > 0f ? Body.Position.x - 0.1f : Body.Position.x - reach + 0.1f, Body.Position.y + 0.3f, reach, 2.0f);
                foreach (var target in World.Hittables())
                {
                    if (struck.Contains(target) || !box.Overlaps(target.HitBounds))
                        continue;
                    struck.Add(target);
                    var third = combo >= 3;
                    var damage = World.Stats.SwordDamage * (heavySwing ? 2.2f : third ? 1.4f : 1f);
                    var result = target.TakeHit(new Hit
                    {
                        Damage = damage,
                        Direction = dir,
                        Knockback = heavySwing ? 7f : third ? 5f : 2.2f,
                        Kind = heavySwing ? HitKind.Heavy : HitKind.Sword,
                        Source = this,
                    });
                    if (result == HitResult.Landed || result == HitResult.Defeated)
                    {
                        GainFarr(heavySwing ? 0.15f : 0.08f);
                        World.Shake(heavySwing ? 0.5f : 0.2f);
                    }
                }
            }

            if (Controls.Pressed(Act.Attack) && t > 0.2f)
                comboQueued = true;

            if (combo == 1 && World.Stats.CanHeavy && Controls.Held(Act.Attack) && t >= 1f)
            {
                SetState(State.Charge);
                return;
            }
            if (t >= 1f)
            {
                if (comboQueued && !heavySwing && combo < World.Stats.ComboLength)
                    StartSwing(combo + 1, false);
                else
                    SetState(State.Free);
            }
        }

        // ------------------------------------------------------------- bow

        Vector2 BowOrigin { get { return Body.Position + new Vector2(FacingRight ? 0.45f : -0.45f, 1.75f); } }

        Vector2 BowVelocity(float strength)
        {
            var rad = aim * Mathf.Deg2Rad;
            var speed = Mathf.Lerp(12f, 24f, strength);
            return new Vector2(Mathf.Cos(rad) * (FacingRight ? 1f : -1f), Mathf.Sin(rad)) * speed;
        }

        void ShowPreview(bool on)
        {
            var seconds = World.Stats.PreviewSeconds;
            var v = BowVelocity(draw);
            for (var i = 0; i < preview.Count; i++)
            {
                var t = (i + 1) / (float)preview.Count * seconds;
                var visible = on && draw > 0.05f;
                preview[i].gameObject.SetActive(visible);
                if (visible)
                    preview[i].transform.position = Ballistics.PointAt(BowOrigin, v, t);
            }
        }

        void Shoot()
        {
            var v = BowVelocity(draw);
            var damage = World.Stats.BowDamage * Mathf.Lerp(0.6f, 1.2f, draw);
            World.Add(new Arrow(BowOrigin, v, true, damage, this));
            if (World.Stats.TwinArrows)
            {
                var rad = (aim + 6f) * Mathf.Deg2Rad;
                var v2 = new Vector2(Mathf.Cos(rad) * (FacingRight ? 1f : -1f), Mathf.Sin(rad)) * v.magnitude;
                World.Add(new Arrow(BowOrigin, v2, true, damage, this));
            }
            AudioService.Play(Sfx.BowRelease, 0.8f);
        }

        // ------------------------------------------------------------- Farr

        void FarrBlast()
        {
            Farr = 0f;
            AudioService.Play(Sfx.FarrBlast, 1f);
            World.Shake(1.2f);
            Fx.Ring(World, Body.Center, 4f, new Color(1f, 0.85f, 0.4f, 0.9f));
            Fx.Glow(World, Body.Center, 6f, new Color(1f, 0.9f, 0.5f, 0.7f), 0.6f);
            Fx.Dust(World, Body.Position + Vector2.left);
            Fx.Dust(World, Body.Position + Vector2.right);
            foreach (var target in new List<IHittable>(World.Hittables()))
            {
                var c = target.HitBounds.center;
                if ((c - Body.Center).magnitude > 4.2f)
                    continue;
                target.TakeHit(new Hit { Damage = 40f, Direction = Mathf.Sign(c.x - Body.Position.x), Knockback = 8f, Kind = HitKind.Farr, Source = this });
            }
            invulnerable = 0.5f;
        }

        // ------------------------------------------------------------- pose

        void Pose()
        {
            puppet.transform.position = Body.Position;
            puppet.FacingRight = FacingRight;
            puppet.Speed = state == State.Free ? Body.Velocity.x : 0f;
            puppet.Airborne = !Body.Grounded;
            puppet.Squash = landSquash * 0.6f;
            puppet.Fallen = state == State.Dead ? Mathf.MoveTowards(puppet.Fallen, 1f, Time.deltaTime * 3f) : 0f;

            switch (state)
            {
                case State.Free:
                    puppet.ArmAngle = Blocking ? 70f : Body.Grounded ? 0f : 25f;
                    puppet.Crouch = Blocking ? 0.3f : 0f;
                    puppet.Lean = Body.Grounded ? Mathf.Abs(Body.Velocity.x) * 0.8f : 0f;
                    break;
                case State.Bow:
                    puppet.ArmAngle = aim - 8f;
                    puppet.Crouch = 0.15f;
                    puppet.Lean = 0f;
                    break;
                case State.Dodge:
                    puppet.Crouch = 0.8f;
                    puppet.Lean = 35f;
                    break;
                case State.Hurt:
                    puppet.Lean = -15f;
                    puppet.ArmAngle = -20f;
                    break;
                default:
                    puppet.Crouch = 0f;
                    puppet.Lean = state == State.Attack ? 8f : 0f;
                    break;
            }

            var g = guard.color;
            g.a = Mathf.MoveTowards(g.a, Blocking ? 0.35f : 0f, Time.deltaTime * 3f);
            guard.color = g;
            guard.transform.position = (Vector3)(Body.Position + new Vector2(FacingRight ? 0.6f : -0.6f, 1.2f));

            var blink = invulnerable > 0f && state != State.Dodge && state != State.Dead && Mathf.Repeat(invulnerable * 12f, 1f) > 0.5f;
            puppet.SetAlpha(blink ? 0.45f : 1f);
        }
    }
}
