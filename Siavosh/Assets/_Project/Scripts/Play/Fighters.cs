using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Play
{
    public enum FighterKind
    {
        Trainee,
        Spearman,
        Archer,
    }

    /// <summary>
    /// A foot soldier: a Zabuli trainee (sparring with a wooden sword), a Turanian spearman or a
    /// Turanian archer. Every attack is telegraphed (the fighter flashes gold while winding up), so
    /// a watchful player can block, parry or roll. Beaten trainees yield; Turanians flee — no one
    /// dies in Siavosh's training.
    /// </summary>
    public class Fighter : Entity, IHittable, IStunnable, IInteractable
    {
        enum State
        {
            Idle,
            Approach,
            Windup,
            Strike,
            Recover,
            Hurt,
            Stunned,
            Yielded,
            Fleeing,
            Helped,
        }

        public readonly FighterKind Kind;
        public readonly Body Body = new Body();
        public string KindnessFlag;
        readonly HumanPuppet puppet;
        readonly bool goal;
        float health;
        readonly float maxHealth;
        State state = State.Idle;
        float timer;
        float cooldown;
        bool facingRight;
        bool struck;

        float Damage { get { return Kind == FighterKind.Trainee ? 7f : Kind == FighterKind.Spearman ? 12f : 10f; } }
        float Reach { get { return Kind == FighterKind.Spearman ? 1.9f : 1.45f; } }
        float WalkSpeed { get { return Kind == FighterKind.Trainee ? 2.6f : 3.0f; } }
        float WindupTime { get { return Kind == FighterKind.Archer ? 0.7f : Kind == FighterKind.Trainee ? 0.6f : 0.5f; } }
        int XpValue { get { return Kind == FighterKind.Trainee ? 20 : 30; } }

        public Fighter(FighterKind kind, Vector2 position, bool goal)
        {
            Kind = kind;
            this.goal = goal;
            maxHealth = health = kind == FighterKind.Trainee ? 30f : kind == FighterKind.Spearman ? 45f : 28f;
            Body.Position = position;
            Body.HalfWidth = 0.32f;
            Body.Height = 2.1f;
            var rig = kind == FighterKind.Trainee ? "trainee" : kind == FighterKind.Spearman ? "soldier" : "archer";
            Root = new GameObject(kind.ToString());
            puppet = HumanPuppet.Create(Root.transform, rig, 40);
            facingRight = false;
        }

        public Rect HitBounds { get { return Body.Bounds; } }
        public bool CanBeHit { get { return state != State.Yielded && state != State.Fleeing && state != State.Helped; } }

        // IInteractable: a beaten trainee who can be helped up.
        public Vector2 Position { get { return Body.Position; } }
        public bool Available { get { return state == State.Yielded && KindnessFlag != null; } }
        public LocText Prompt { get { return Ui.HelpTrainee; } }

        public void Interact()
        {
            if (!Available)
                return;
            World.Flags.Add(KindnessFlag);
            KindnessFlag = null;
            state = State.Helped;
            timer = 0f;
            World.AddHonor(1, Ui.HonorTrainee);
            World.Float(Body.Center + Vector2.up, Ui.HonorPlus.ToString(), Palette.TurquoiseLight);
        }

        public void Stun(float seconds)
        {
            if (!CanBeHit)
                return;
            state = State.Stunned;
            timer = seconds;
            puppet.Flash(Palette.TurquoiseLight, 0.3f);
        }

        public HitResult TakeHit(Hit hit)
        {
            if (!CanBeHit)
                return HitResult.Ignored;
            health -= hit.Damage;
            Body.Velocity.x = hit.Direction * hit.Knockback;
            puppet.Flash(new Color(1f, 0.5f, 0.45f));
            AudioService.Play(hit.Kind == HitKind.Arrow ? Sfx.ArrowHit : Sfx.HitFlesh, 0.8f);
            Fx.Spark(World, Body.Center + new Vector2(-hit.Direction * 0.2f, 0.3f));
            World.Float(Body.Center + Vector2.up * 1.3f, Loc.Number(Mathf.RoundToInt(hit.Damage)), Palette.White);
            if (health <= 0f)
            {
                Defeat();
                return HitResult.Defeated;
            }
            if (state != State.Strike)
            {
                state = State.Hurt;
                timer = 0.3f;
            }
            return HitResult.Landed;
        }

        void Defeat()
        {
            World.Xp += XpValue;
            if (goal)
                World.CountGoal();
            if (Kind == FighterKind.Trainee)
            {
                state = State.Yielded;
                World.Float(Body.Center + Vector2.up * 1.5f, Ui.Yield.ToString(), Palette.PaperDark);
            }
            else
            {
                state = State.Fleeing;
                facingRight = World.Hero == null || Body.Position.x > World.Hero.Body.Position.x;
            }
            timer = 0f;
        }

        public override void Tick(float dt)
        {
            var hero = World.Hero;
            timer -= dt;
            cooldown -= dt;
            var toHero = hero != null ? hero.Body.Position.x - Body.Position.x : 99f;
            var distance = Mathf.Abs(toHero);
            var targetVx = 0f;

            switch (state)
            {
                case State.Idle:
                    if (hero != null && distance < 10f && Mathf.Abs(hero.Body.Position.y - Body.Position.y) < 4f)
                        state = State.Approach;
                    break;

                case State.Approach:
                    if (hero == null || !hero.Alive)
                        break;
                    facingRight = toHero > 0f;
                    if (Kind == FighterKind.Archer)
                    {
                        if (distance < 5f)
                            targetVx = -Mathf.Sign(toHero) * WalkSpeed * 0.8f;
                        else if (distance > 11f)
                            targetVx = Mathf.Sign(toHero) * WalkSpeed;
                        if (cooldown <= 0f && distance < 13f)
                            StartWindup();
                    }
                    else if (distance > Reach * 0.85f)
                        targetVx = Mathf.Sign(toHero) * WalkSpeed;
                    else if (cooldown <= 0f)
                        StartWindup();
                    break;

                case State.Windup:
                    facingRight = toHero > 0f;
                    puppet.ArmAngle = Kind == FighterKind.Archer ? Mathf.Lerp(0f, 18f, 1f - timer / WindupTime) : Mathf.Lerp(0f, 95f, 1f - timer / WindupTime);
                    if (Mathf.Repeat(timer * 10f, 1f) > 0.5f)
                        puppet.Flash(Palette.GoldLight, 0.05f);
                    if (timer <= 0f)
                    {
                        state = State.Strike;
                        timer = 0.16f;
                        struck = false;
                    }
                    break;

                case State.Strike:
                    puppet.ArmAngle = Kind == FighterKind.Archer ? 10f : Mathf.Lerp(95f, -60f, 1f - timer / 0.16f);
                    if (!struck)
                    {
                        struck = true;
                        Strike(hero);
                    }
                    if (timer <= 0f)
                    {
                        state = State.Recover;
                        timer = Kind == FighterKind.Trainee ? 0.8f : 0.6f;
                    }
                    break;

                case State.Recover:
                    puppet.ArmAngle = Mathf.MoveTowards(puppet.ArmAngle, 0f, dt * 200f);
                    if (timer <= 0f)
                    {
                        state = State.Approach;
                        cooldown = Kind == FighterKind.Archer ? 1.8f : Random.Range(0.6f, 1.3f);
                    }
                    break;

                case State.Hurt:
                    puppet.ArmAngle = -20f;
                    if (timer <= 0f)
                        state = State.Approach;
                    break;

                case State.Stunned:
                    puppet.ArmAngle = -40f;
                    puppet.Lean = Mathf.Sin(Time.time * 12f) * 6f;
                    if (timer <= 0f)
                    {
                        puppet.Lean = 0f;
                        state = State.Approach;
                        cooldown = 0.5f;
                    }
                    break;

                case State.Yielded:
                    puppet.ArmAngle = -30f;
                    puppet.Crouch = Mathf.MoveTowards(puppet.Crouch, 1f, dt * 4f);
                    puppet.Lean = Mathf.MoveTowards(puppet.Lean, 12f, dt * 40f);
                    if (KindnessFlag != null)
                        World.OfferInteraction(this);
                    break;

                case State.Helped:
                    puppet.Crouch = Mathf.MoveTowards(puppet.Crouch, 0f, dt * 3f);
                    puppet.Lean = Mathf.MoveTowards(puppet.Lean, timer > -1f ? 20f : 0f, dt * 30f); // a bow of thanks
                    puppet.ArmAngle = 0f;
                    break;

                case State.Fleeing:
                    targetVx = (facingRight ? 1f : -1f) * 5f;
                    puppet.ArmAngle = 30f;
                    puppet.SetAlpha(Mathf.Clamp01(1.5f + timer));
                    if (timer < -1.5f)
                        Remove();
                    break;
            }

            if (state != State.Hurt && state != State.Stunned)
                Body.Velocity.x = Mathf.MoveTowards(Body.Velocity.x, targetVx, dt * 30f);
            else
                Body.Velocity.x = Mathf.MoveTowards(Body.Velocity.x, 0f, dt * 12f);
            if (state == State.Fleeing)
            {
                Body.MinX = float.NegativeInfinity;
                Body.MaxX = float.PositiveInfinity;
            }
            Body.Step(dt, World.Solids);
            if (Body.Position.y < -6f)
            {
                // Fell into the river: counts as driven off, so the stage can still be finished.
                if (CanBeHit)
                {
                    World.Xp += XpValue;
                    if (goal)
                        World.CountGoal();
                }
                Remove();
            }

            puppet.transform.position = Body.Position;
            puppet.FacingRight = facingRight;
            puppet.Speed = Body.Velocity.x;
            puppet.Airborne = !Body.Grounded;
        }

        void StartWindup()
        {
            state = State.Windup;
            timer = WindupTime;
        }

        void Strike(Hero hero)
        {
            if (hero == null)
                return;
            if (Kind == FighterKind.Archer)
            {
                var from = Body.Position + new Vector2(facingRight ? 0.5f : -0.5f, 1.7f);
                var aim = Ballistics.Aim(from, hero.Body.Center, 13f) ?? new Vector2(facingRight ? 13f : -13f, 2f);
                World.Add(new Arrow(from, aim, false, Damage, this));
                AudioService.Play(Sfx.BowRelease, 0.6f);
                return;
            }
            AudioService.Play(Sfx.Swing, 0.6f);
            var dir = facingRight ? 1f : -1f;
            var reachRect = new Rect(dir > 0f ? Body.Position.x : Body.Position.x - Reach - 0.3f, Body.Position.y + 0.3f, Reach + 0.3f, 1.8f);
            if (hero.CanBeHit && reachRect.Overlaps(hero.Body.Bounds))
                hero.TakeHit(new Hit { Damage = Damage, Direction = dir, Knockback = 4f, Kind = HitKind.Sword, Source = this });
        }
    }

    /// <summary>
    /// Rostam in the training ground (stage 1-6). He cannot be beaten — the trial is to land three
    /// clean blows. He smashes with his ox-head mace (a long, readable wind-up), sweeps low, and
    /// raises his guard; blows on his guard bounce off. After each clean blow he steps back and
    /// grows a little quicker.
    /// </summary>
    public class RostamBoss : Entity, IHittable, IStunnable
    {
        enum State
        {
            Walk,
            Guard,
            SmashWindup,
            Smash,
            SweepWindup,
            Sweep,
            Recover,
            Staggered,
            Done,
        }

        public readonly Body Body = new Body();
        readonly HumanPuppet puppet;
        State state = State.Walk;
        float timer = 1.5f;
        float invulnerable;
        bool facingRight;
        int hits;
        bool struck;

        static readonly LocText[] Praise =
        {
            new LocText("آفرین!", "Well struck!"),
            new LocText("خوب است، شاهزاده!", "Good, prince!"),
            new LocText("هه! پسرِ شاه است دیگر!", "Ha! A king's son indeed!"),
        };

        static readonly LocText Guarded = new LocText("سپر رستم!", "Rostam's guard!");

        public RostamBoss(float x)
        {
            Body.Position = new Vector2(x, 0f);
            Body.HalfWidth = 0.45f;
            Body.Height = 2.5f;
            Root = new GameObject("Rostam");
            puppet = HumanPuppet.Create(Root.transform, "rostam", 60);
        }

        float Pace { get { return 1f + hits * 0.18f; } }

        public Rect HitBounds { get { return Body.Bounds; } }
        public bool CanBeHit { get { return state != State.Done && invulnerable <= 0f; } }

        public void Stun(float seconds)
        {
            if (state == State.Done)
                return;
            state = State.Staggered;
            timer = seconds;
            puppet.Flash(Palette.TurquoiseLight, 0.3f);
        }

        public HitResult TakeHit(Hit hit)
        {
            if (!CanBeHit)
                return HitResult.Ignored;
            var fromFront = Mathf.Sign(-hit.Direction) == (facingRight ? 1f : -1f);
            if (state == State.Guard && fromFront && hit.Kind != HitKind.Heavy && hit.Kind != HitKind.Farr)
            {
                AudioService.Play(Sfx.Block, 0.8f);
                Fx.Spark(World, Body.Center + new Vector2(facingRight ? 0.5f : -0.5f, 0.3f));
                World.Float(Body.Center + Vector2.up * 1.6f, Guarded.ToString(), Palette.Steel);
                if (World.Hero != null)
                    World.Hero.Push(hit.Direction * -5f);
                return HitResult.Blocked;
            }

            hits++;
            World.CountGoal();
            invulnerable = 1.0f;
            AudioService.Play(Sfx.HitFlesh, 0.9f);
            Fx.Spark(World, Body.Center + new Vector2(-hit.Direction * 0.3f, 0.4f));
            World.Shake(0.6f);
            puppet.Flash(new Color(1f, 0.6f, 0.5f), 0.2f);
            World.Float(Body.Center + Vector2.up * 1.7f, Praise[Mathf.Clamp(hits - 1, 0, Praise.Length - 1)].ToString(), Palette.Saffron);
            Body.Velocity.x = hit.Direction * 6f;
            if (hits >= World.GoalNeeded)
            {
                state = State.Done;
                timer = 1.6f;
            }
            else
            {
                state = State.Staggered;
                timer = 0.6f;
            }
            return HitResult.Landed;
        }

        public override void Tick(float dt)
        {
            var hero = World.Hero;
            timer -= dt;
            invulnerable -= dt;
            var toHero = hero != null ? hero.Body.Position.x - Body.Position.x : 0f;
            var distance = Mathf.Abs(toHero);
            var targetVx = 0f;

            switch (state)
            {
                case State.Walk:
                    facingRight = toHero > 0f;
                    puppet.ArmAngle = 0f;
                    if (distance > 2.2f)
                        targetVx = Mathf.Sign(toHero) * 2.4f * Pace;
                    if (timer <= 0f)
                        Choose(distance);
                    break;

                case State.Guard:
                    facingRight = toHero > 0f;
                    puppet.ArmAngle = 70f;
                    puppet.Crouch = 0.25f;
                    if (timer <= 0f)
                    {
                        puppet.Crouch = 0f;
                        state = State.Walk;
                        timer = 0.6f;
                    }
                    break;

                case State.SmashWindup:
                    puppet.ArmAngle = Mathf.Lerp(0f, 150f, 1f - timer / (0.8f / Pace));
                    puppet.Lean = -6f;
                    if (Mathf.Repeat(timer * 8f, 1f) > 0.5f)
                        puppet.Flash(Palette.GoldLight, 0.05f);
                    if (timer <= 0f)
                    {
                        state = State.Smash;
                        timer = 0.18f;
                        struck = false;
                    }
                    break;

                case State.Smash:
                    puppet.ArmAngle = Mathf.Lerp(150f, -70f, 1f - timer / 0.18f);
                    puppet.Lean = 10f;
                    if (!struck && timer < 0.08f)
                    {
                        struck = true;
                        var dir = facingRight ? 1f : -1f;
                        var at = Body.Position + new Vector2(dir * 1.9f, 0f);
                        Fx.Dust(World, at);
                        Fx.Ring(World, at + new Vector2(0f, 0.2f), 1.6f, new Color(1f, 0.85f, 0.5f, 0.7f));
                        World.Shake(0.9f);
                        AudioService.Play(Sfx.FarrBlast, 0.5f);
                        if (hero != null && hero.CanBeHit && Mathf.Abs(hero.Body.Position.x - at.x) < 1.9f && hero.Body.Position.y < 1.2f)
                            hero.TakeHit(new Hit { Damage = 18f, Direction = dir, Knockback = 7f, Kind = HitKind.Heavy, Source = this });
                    }
                    if (timer <= 0f)
                    {
                        state = State.Recover;
                        timer = 1.1f / Pace;
                    }
                    break;

                case State.SweepWindup:
                    puppet.ArmAngle = Mathf.Lerp(0f, -110f, 1f - timer / (0.55f / Pace));
                    puppet.Crouch = 0.3f;
                    if (Mathf.Repeat(timer * 10f, 1f) > 0.5f)
                        puppet.Flash(Palette.GoldLight, 0.05f);
                    if (timer <= 0f)
                    {
                        state = State.Sweep;
                        timer = 0.2f;
                        struck = false;
                    }
                    break;

                case State.Sweep:
                    puppet.ArmAngle = Mathf.Lerp(-110f, 40f, 1f - timer / 0.2f);
                    if (!struck)
                    {
                        struck = true;
                        AudioService.Play(Sfx.Swing, 0.9f);
                        var dir = facingRight ? 1f : -1f;
                        var reach = new Rect(dir > 0f ? Body.Position.x : Body.Position.x - 2.6f, Body.Position.y, 2.6f, 1.2f);
                        if (hero != null && hero.CanBeHit && reach.Overlaps(hero.Body.Bounds))
                            hero.TakeHit(new Hit { Damage = 12f, Direction = dir, Knockback = 5f, Kind = HitKind.Sword, Source = this });
                    }
                    if (timer <= 0f)
                    {
                        puppet.Crouch = 0f;
                        state = State.Recover;
                        timer = 0.9f / Pace;
                    }
                    break;

                case State.Recover:
                    puppet.ArmAngle = Mathf.MoveTowards(puppet.ArmAngle, 0f, dt * 160f);
                    puppet.Lean = Mathf.MoveTowards(puppet.Lean, 0f, dt * 30f);
                    if (timer <= 0f)
                    {
                        state = State.Walk;
                        timer = Random.Range(0.6f, 1.2f) / Pace;
                    }
                    break;

                case State.Staggered:
                    puppet.ArmAngle = -30f;
                    puppet.Lean = -8f;
                    if (timer <= 0f)
                    {
                        puppet.Lean = 0f;
                        state = hits > 0 && Random.value < 0.5f ? State.Guard : State.Walk;
                        timer = state == State.Guard ? 1.4f : 0.5f;
                    }
                    break;

                case State.Done:
                    puppet.ArmAngle = Mathf.MoveTowards(puppet.ArmAngle, 0f, dt * 100f);
                    puppet.Lean = Mathf.MoveTowards(puppet.Lean, 15f, dt * 20f); // Rostam bows to his pupil
                    facingRight = toHero > 0f;
                    if (timer <= 0f)
                        World.Finished = true;
                    break;
            }

            Body.Velocity.x = Mathf.MoveTowards(Body.Velocity.x, targetVx, dt * 20f);
            Body.MinX = World.MinX;
            Body.MaxX = World.MaxX;
            Body.Step(dt, World.Solids);
            puppet.transform.position = Body.Position;
            puppet.FacingRight = facingRight;
            puppet.Speed = Body.Velocity.x;
            puppet.Airborne = !Body.Grounded;
        }

        void Choose(float distance)
        {
            var roll = Random.value;
            if (distance < 3.2f)
            {
                if (roll < 0.25f + hits * 0.08f)
                {
                    state = State.Guard;
                    timer = 1.2f + hits * 0.2f;
                }
                else if (roll < 0.65f)
                {
                    state = State.SmashWindup;
                    timer = 0.8f / Pace;
                }
                else
                {
                    state = State.SweepWindup;
                    timer = 0.55f / Pace;
                }
            }
            else
            {
                state = State.Walk;
                timer = 0.4f;
            }
        }
    }
}
