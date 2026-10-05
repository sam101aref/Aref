using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Play
{
    /// <summary>Short-lived visual effects: sparks, sword arcs, dust, shock rings.</summary>
    public class FxAnim : MonoBehaviour
    {
        public float Life = 0.3f;
        public float StartScale = 1f;
        public float EndScale = 1.4f;
        public Vector3 Velocity;
        public float Spin;
        SpriteRenderer sr;
        float age;
        float baseAlpha;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            baseAlpha = sr != null ? sr.color.a : 1f;
        }

        void Update()
        {
            age += Time.deltaTime;
            var t = Mathf.Clamp01(age / Life);
            var sign = Mathf.Sign(transform.localScale.x);
            var s = Mathf.Lerp(StartScale, EndScale, t);
            transform.localScale = new Vector3(s * sign, s, 1f);
            transform.position += Velocity * Time.deltaTime;
            transform.Rotate(0f, 0f, Spin * Time.deltaTime);
            if (sr != null)
            {
                var c = sr.color;
                c.a = baseAlpha * (1f - t * t);
                sr.color = c;
            }
            if (age >= Life)
                Destroy(gameObject);
        }
    }

    public static class Fx
    {
        static FxAnim Make(Transform parent, Sprite sprite, Vector3 position, float life, float from, float to, int order, Color color)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * from;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            var fx = go.AddComponent<FxAnim>();
            fx.Life = life;
            fx.StartScale = from;
            fx.EndScale = to;
            return fx;
        }

        public static void Spark(StageWorld world, Vector2 at)
        {
            var fx = Make(world.Root.transform, Art.Get("props/spark"), at, 0.22f, 0.5f, 1.1f, 950, Color.white);
            fx.Spin = 360f;
        }

        public static void Slash(StageWorld world, Vector2 at, bool right, bool heavy)
        {
            var fx = Make(world.Root.transform, Art.Get("props/slash"), at, heavy ? 0.26f : 0.18f, heavy ? 1.2f : 0.9f, heavy ? 1.5f : 1.1f, 940,
                heavy ? Palette.GoldLight : new Color(1f, 1f, 1f, 0.9f));
            var s = fx.transform.localScale;
            fx.transform.localScale = new Vector3(right ? s.x : -s.x, s.y, 1f);
        }

        public static void Dust(StageWorld world, Vector2 at)
        {
            var fx = Make(world.Root.transform, Art.Get("props/dust"), at + new Vector2(0f, 0.15f), 0.4f, 0.6f, 1.3f, 930, new Color(1f, 1f, 1f, 0.8f));
            fx.Velocity = new Vector3(0f, 0.6f, 0f);
        }

        public static void Ring(StageWorld world, Vector2 at, float radius, Color color)
        {
            var fx = Make(world.Root.transform, Art.Ring, at, 0.45f, 0.5f, radius * 2f / 1.28f, 945, color);
            fx.Life = 0.45f;
        }

        public static void Glow(StageWorld world, Vector2 at, float size, Color color, float life)
        {
            Make(world.Root.transform, Art.Glow, at, life, size * 0.6f, size, 935, color);
        }
    }

    // ----------------------------------------------------------------- pickups

    public enum PickupKind
    {
        Leaf,
        Coin,
        Heal,
    }

    /// <summary>A leaf of the Shahnameh, a dinar or a pomegranate, floating where it was placed.</summary>
    public class Pickup : Entity
    {
        readonly PickupKind kind;
        readonly Vector2 home;
        readonly int index;
        readonly SpriteRenderer sr;
        readonly float seed;

        public Pickup(PickupKind kind, Vector2 position, int index)
        {
            this.kind = kind;
            home = position;
            this.index = index;
            seed = position.x * 0.7f;
            var name = kind == PickupKind.Leaf ? "props/leaf" : kind == PickupKind.Coin ? "props/coin" : "props/pomegranate";
            Root = new GameObject(kind.ToString());
            Root.transform.position = position;
            sr = Root.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(name);
            sr.sortingOrder = 800;
            if (kind == PickupKind.Leaf)
            {
                var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
                glow.transform.SetParent(Root.transform, false);
                glow.transform.localScale = Vector3.one * 1.2f;
                glow.sprite = Art.Glow;
                glow.color = new Color(1f, 0.9f, 0.5f, 0.55f);
                glow.sortingOrder = 799;
            }
        }

        public override void Tick(float dt)
        {
            if (kind == PickupKind.Leaf && Time.frameCount % 30 == 0)
            {
                var found = SaveSystem.Data.LeavesFound(World.Stage.Id) > 0 &&
                            (SaveSystem.Data.Stage(World.Stage.Id).leaves & (1 << index)) != 0;
                var c = sr.color;
                c.a = found ? 0.55f : 1f;
                sr.color = c;
            }
            var t = Time.time * 2.2f + seed;
            Root.transform.position = home + new Vector2(0f, Mathf.Sin(t) * 0.12f);
            if (kind == PickupKind.Coin)
                Root.transform.localScale = new Vector3(Mathf.Cos(t * 1.4f), 1f, 1f);
            else
                Root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * 8f);

            Rect bounds;
            if (!World.TryGetPlayerBounds(out bounds))
                return;
            var p = (Vector2)Root.transform.position;
            var grow = kind == PickupKind.Coin ? 0.25f : 0.35f;
            if (p.x < bounds.xMin - grow || p.x > bounds.xMax + grow || p.y < bounds.yMin - grow || p.y > bounds.yMax + grow)
                return;

            switch (kind)
            {
                case PickupKind.Leaf:
                    World.LeavesMask |= 1 << index;
                    AudioService.Play(Sfx.Leaf, 0.8f);
                    World.Float(p, Ui.LeafFound.Format(index + 1, 3), Palette.GoldLight);
                    Fx.Glow(World, p, 2.5f, new Color(1f, 0.9f, 0.5f, 0.8f), 0.5f);
                    break;
                case PickupKind.Coin:
                    World.Coins++;
                    AudioService.Play(Sfx.Coin, 0.45f);
                    break;
                default:
                    if (World.Hero != null)
                        World.Hero.Heal(30);
                    AudioService.Play(Sfx.Heal, 0.7f);
                    World.Float(p, "+30", Palette.TurquoiseLight);
                    break;
            }
            Remove();
        }
    }

    /// <summary>Shows a tutorial hint when the hero reaches it.</summary>
    public class TipTrigger : Entity
    {
        readonly float x;
        readonly LocText text;

        public TipTrigger(float x, LocText text)
        {
            this.x = x;
            this.text = text;
        }

        public override void Tick(float dt)
        {
            if (World.PlayerX >= x)
            {
                World.Tip(text);
                Remove();
            }
        }
    }

    /// <summary>The iwan at the end of a stage. Barred until the stage's goal is met.</summary>
    public class Gate : Entity
    {
        readonly float x;
        readonly GameObject bars;
        readonly SpriteRenderer glow;
        bool wasOpen;

        public Gate(float x)
        {
            this.x = x;
            Root = new GameObject("Gate");
            var size = Art.Size("props/gate");
            Root.transform.position = new Vector3(x, size.y * 0.5f - 0.05f, 0f);
            var sr = Root.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get("props/gate");
            sr.sortingOrder = -60;

            glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(Root.transform, false);
            glow.transform.localPosition = new Vector3(0f, -size.y * 0.25f, 0f);
            glow.transform.localScale = new Vector3(2.4f, 3.2f, 1f);
            glow.sprite = Art.Glow;
            glow.color = new Color(1f, 0.9f, 0.55f, 0f);
            glow.sortingOrder = -59;

            bars = new GameObject("Bars");
            bars.transform.SetParent(Root.transform, false);
            for (var i = -2; i <= 2; i++)
            {
                var bar = new GameObject("Bar").AddComponent<SpriteRenderer>();
                bar.transform.SetParent(bars.transform, false);
                bar.transform.localPosition = new Vector3(i * 0.34f, -size.y * 0.18f, 0f);
                bar.sprite = Art.White;
                bar.drawMode = SpriteDrawMode.Sliced;
                bar.size = new Vector2(0.1f, size.y * 0.62f);
                bar.color = Palette.EarthDark;
                bar.sortingOrder = -58;
            }
        }

        public bool Open
        {
            get
            {
                return World.Stage.GoalKind != GoalKind.ClearGoals || World.GoalDone >= World.GoalNeeded;
            }
        }

        public override void Tick(float dt)
        {
            var open = Open;
            if (open && !wasOpen && World.Stage.GoalKind == GoalKind.ClearGoals)
            {
                AudioService.Play(Sfx.FarrReady, 0.8f);
                World.Tip(Ui.GateOpen);
            }
            wasOpen = open;
            bars.SetActive(!open);
            var c = glow.color;
            c.a = open ? 0.45f + Mathf.Sin(Time.time * 3f) * 0.15f : 0f;
            glow.color = c;
            if (open && World.Hero != null && Mathf.Abs(World.Hero.Body.Position.x - x) < 0.7f && World.Hero.Body.Grounded)
                World.Finished = true;
        }
    }

    /// <summary>A small kindness the hero can do on the way: worth a point of honour.</summary>
    public class Kindness : Entity, IInteractable
    {
        readonly Thing kind;
        readonly string flag;
        readonly SpriteRenderer sr;
        bool done;
        float doneTime;
        Vector3 velocity;

        public Kindness(Thing kind, float x, string flag)
        {
            this.kind = kind;
            this.flag = flag;
            Root = new GameObject(kind.ToString());
            var name = kind == Thing.Lamb ? "props/lamb" : "props/bird_hurt";
            var size = Art.Size(name);
            Root.transform.position = new Vector3(x, size.y * 0.5f, 0f);
            sr = Root.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(name);
            sr.sortingOrder = 120;
            if (kind == Thing.Lamb)
                Root.transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        public Vector2 Position { get { return Root.transform.position; } }
        public bool Available { get { return !done; } }
        public LocText Prompt { get { return kind == Thing.Lamb ? Ui.HelpLamb : Ui.HelpBird; } }

        public void Interact()
        {
            if (done)
                return;
            done = true;
            World.Flags.Add(flag);
            World.AddHonor(1, kind == Thing.Lamb ? Ui.HonorLamb : Ui.HonorBird);
            World.Float(Position + Vector2.up, Ui.HonorPlus.ToString(), Palette.TurquoiseLight);
            Fx.Glow(World, Position, 2f, new Color(0.6f, 1f, 0.9f, 0.7f), 0.6f);
            velocity = kind == Thing.Lamb ? new Vector3(-3f, 0f, 0f) : new Vector3(1.5f, 3.5f, 0f);
            if (kind == Thing.HurtBird)
                sr.sprite = Art.Get("props/bird_up");
        }

        public override void Tick(float dt)
        {
            if (!done)
            {
                if (kind == Thing.Lamb)
                    Root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f) * 3f);
                World.OfferInteraction(this);
                return;
            }
            doneTime += dt;
            if (kind == Thing.Lamb)
                Root.transform.position += velocity * dt + new Vector3(0f, Mathf.Abs(Mathf.Sin(doneTime * 9f)) * 0.03f, 0f);
            else
            {
                Root.transform.position += velocity * dt;
                sr.sprite = Art.Get(Mathf.Repeat(doneTime * 6f, 1f) > 0.5f ? "props/bird_up" : "props/bird_down");
            }
            var c = sr.color;
            c.a = Mathf.Clamp01(2.5f - doneTime);
            sr.color = c;
            if (doneTime > 2.5f)
                Remove();
        }
    }

    public interface IInteractable
    {
        Vector2 Position { get; }
        bool Available { get; }
        LocText Prompt { get; }
        void Interact();
    }

    public interface IStunnable
    {
        void Stun(float seconds);
    }

    // ----------------------------------------------------------------- arrows

    /// <summary>An arrow in flight, from the hero or an enemy archer.</summary>
    public class Arrow : Entity
    {
        const float Gravity = Ballistics.Gravity;
        Vector2 position;
        Vector2 velocity;
        readonly bool fromHero;
        readonly float damage;
        readonly Entity source;
        float life;
        bool stuck;
        readonly SpriteRenderer sr;

        public Arrow(Vector2 position, Vector2 velocity, bool fromHero, float damage, Entity source)
        {
            this.position = position;
            this.velocity = velocity;
            this.fromHero = fromHero;
            this.damage = damage;
            this.source = source;
            Root = new GameObject("Arrow");
            Root.transform.position = position;
            sr = Root.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get("props/arrow");
            sr.sortingOrder = 870;
            Root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }

        Vector2 Tip
        {
            get
            {
                var dir = velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector2.right;
                return position + dir * 0.5f;
            }
        }

        public override void Tick(float dt)
        {
            life += dt;
            if (stuck)
            {
                var c = sr.color;
                c.a = Mathf.Clamp01(2f - life);
                sr.color = c;
                if (life > 2f)
                    Remove();
                return;
            }

            velocity.y -= Gravity * dt;
            position += velocity * dt;
            Root.transform.position = position;
            Root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            var tip = Tip;

            if (fromHero)
            {
                foreach (var target in World.Hittables())
                {
                    if (!target.HitBounds.Contains(tip))
                        continue;
                    var result = target.TakeHit(new Hit { Damage = damage, Direction = Mathf.Sign(velocity.x), Knockback = 2f, Kind = HitKind.Arrow, Source = source });
                    if (result == HitResult.Ignored)
                        continue;
                    AudioService.Play(result == HitResult.Blocked ? Sfx.Block : Sfx.ArrowHit, 0.7f);
                    if (result != HitResult.Blocked && World.Hero != null)
                        World.Hero.GainFarr(0.05f);
                    Stick();
                    return;
                }
            }
            else if (World.Hero != null && World.Hero.CanBeHit && World.Hero.Body.Bounds.Contains(tip))
            {
                World.Hero.TakeHit(new Hit { Damage = damage, Direction = Mathf.Sign(velocity.x), Knockback = 2.5f, Kind = HitKind.Arrow, Source = source });
                Remove();
                return;
            }

            var ground = Body.SurfaceAt(tip.x, World.Solids, tip.y + 0.6f);
            if (ground > float.NegativeInfinity && tip.y <= ground)
            {
                AudioService.Play(Sfx.HitWood, 0.35f);
                Stick();
                return;
            }
            if (life > 5f || position.y < -8f)
                Remove();
        }

        void Stick()
        {
            stuck = true;
            life = 0f;
            velocity = Vector2.zero;
        }
    }

    // ----------------------------------------------------------------- training gear

    /// <summary>A straw training dummy: it wobbles when struck and never falls.</summary>
    public class Dummy : Entity, IHittable
    {
        readonly Transform visual;
        float wobble;
        float wobbleDir;
        readonly Vector2 basePos;

        public Dummy(float x)
        {
            var size = Art.Size("props/dummy");
            basePos = new Vector2(x, 0f);
            Root = new GameObject("Dummy");
            Root.transform.position = new Vector3(x, 0f, 0f);
            var go = new GameObject("Visual");
            go.transform.SetParent(Root.transform, false);
            go.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get("props/dummy");
            sr.sortingOrder = 90;
            visual = Root.transform;
        }

        public Rect HitBounds { get { return new Rect(basePos.x - 0.45f, 0.3f, 0.9f, 2.0f); } }
        public bool CanBeHit { get { return true; } }

        public HitResult TakeHit(Hit hit)
        {
            wobble = 1f;
            wobbleDir = hit.Direction;
            AudioService.Play(Sfx.HitWood, 0.7f);
            Fx.Spark(World, new Vector2(basePos.x - hit.Direction * 0.2f, 1.5f));
            return HitResult.Landed;
        }

        public override void Tick(float dt)
        {
            wobble = Mathf.MoveTowards(wobble, 0f, dt * 1.6f);
            visual.rotation = Quaternion.Euler(0f, 0f, -wobbleDir * Mathf.Sin(wobble * 14f) * wobble * 14f);
        }
    }

    /// <summary>A straw archery target, standing or swinging on a rope.</summary>
    public class Target : Entity, IHittable
    {
        readonly bool swinging;
        readonly bool goal;
        readonly Vector2 anchor;
        readonly Transform face;
        readonly SpriteRenderer sr;
        readonly float rope = 2.4f;
        bool hit;
        float swingTime;

        public Target(Vector2 position, bool swinging, bool goal)
        {
            this.swinging = swinging;
            this.goal = goal;
            Root = new GameObject("Target");
            var size = Art.Size("props/target");
            if (swinging)
            {
                anchor = position;
                var line = new GameObject("Rope").AddComponent<SpriteRenderer>();
                line.transform.SetParent(Root.transform, false);
                line.sprite = Art.White;
                line.drawMode = SpriteDrawMode.Sliced;
                line.size = new Vector2(0.05f, rope);
                line.color = Palette.EarthDark;
                line.sortingOrder = 80;
                line.transform.localPosition = new Vector3(0f, -rope * 0.5f, 0f);
                var go = new GameObject("Face");
                go.transform.SetParent(Root.transform, false);
                go.transform.localPosition = new Vector3(0f, -rope - 0.45f, 0f);
                go.transform.localScale = Vector3.one * 0.8f;
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.Get("props/target");
                sr.sortingOrder = 81;
                face = go.transform;
                Root.transform.position = anchor;
            }
            else
            {
                anchor = position;
                var go = new GameObject("Face");
                go.transform.SetParent(Root.transform, false);
                go.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.Get("props/target");
                sr.sortingOrder = 81;
                face = go.transform;
                Root.transform.position = position;
            }
        }

        Vector2 Centre
        {
            get
            {
                if (swinging)
                    return (Vector2)face.position + new Vector2(0f, 0.35f);
                return anchor + new Vector2(0f, 1.45f);
            }
        }

        public Rect HitBounds { get { var c = Centre; return new Rect(c.x - 0.55f, c.y - 0.55f, 1.1f, 1.1f); } }
        public bool CanBeHit { get { return true; } }

        public HitResult TakeHit(Hit h)
        {
            Fx.Spark(World, Centre);
            if (!hit)
            {
                hit = true;
                sr.color = new Color(1f, 0.92f, 0.6f);
                if (goal)
                {
                    World.CountGoal();
                    World.Float(Centre + Vector2.up * 0.8f, "+", Palette.GoldLight);
                }
                AudioService.Play(Sfx.Coin, 0.6f);
            }
            return HitResult.Landed;
        }

        public override void Tick(float dt)
        {
            if (swinging)
            {
                swingTime += dt;
                Root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(swingTime * 1.7f) * 32f);
            }
        }
    }
}
