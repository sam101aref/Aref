using System;
using System.Collections.Generic;
using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Play
{
    public enum HitKind
    {
        Sword,
        Heavy,
        Arrow,
        Farr,
    }

    public enum HitResult
    {
        Ignored,
        Blocked,
        Landed,
        Defeated,
    }

    public struct Hit
    {
        public float Damage;
        public float Direction;     // +1 pushes right, -1 pushes left
        public float Knockback;
        public HitKind Kind;
        public Entity Source;
    }

    /// <summary>Something that can be struck by a sword, an arrow or the Farr.</summary>
    public interface IHittable
    {
        Rect HitBounds { get; }
        bool CanBeHit { get; }
        HitResult TakeHit(Hit hit);
    }

    /// <summary>Anything that lives in a stage and updates with it.</summary>
    public abstract class Entity
    {
        public StageWorld World;
        public GameObject Root;
        public bool Removed;

        public virtual void Tick(float dt)
        {
        }

        public void Remove()
        {
            Removed = true;
        }

        public virtual void Dispose()
        {
            if (Root != null)
                UnityEngine.Object.Destroy(Root);
        }
    }

    /// <summary>
    /// A stage being played: the ground and ledges, scenery, pickups, enemies and the hero, built
    /// from a <see cref="StageLayout"/>. Updated by the stage screen once per frame, after input.
    /// Also keeps the run's score (coins, leaves, honour) and the stage goal.
    /// </summary>
    public class StageWorld
    {
        public readonly StageDef Stage;
        public readonly StageLayout Layout;
        public readonly HeroStats Stats;
        public readonly GameObject Root;
        public readonly List<Solid> Solids = new List<Solid>();
        public readonly List<Entity> Entities = new List<Entity>();
        public readonly Camera Camera;
        public Hero Hero;
        public Rider Rider;

        // Score of this run
        public int Coins;
        public int Xp;
        public int LeavesMask;
        public int Honor;
        public LocText HonorNote;
        public int GoalDone;
        public int GoalNeeded;

        public event Action<LocText> TipShown;
        public event Action<Vector3, string, Color> Floating;
        public event Action GoalChanged;

        public float MinX;
        public float MaxX;
        public bool Finished;

        readonly List<Entity> pending = new List<Entity>();
        Parallax backdrop;
        float shake;
        float cameraY = 3f;
        int nextSorting = 20;

        public const float CameraBaseY = 3f;

        public StageWorld(StageDef stage, HeroStats stats, Camera camera)
        {
            Stage = stage;
            Stats = stats;
            Camera = camera;
            Layout = stage.Layout();
            Root = new GameObject("Stage " + stage.Id);
            MinX = stage.Kind == StageKind.Ride ? -20f : -1.5f;
            MaxX = Layout.Length + (stage.Kind == StageKind.Ride ? 40f : 0f);
            Build();
        }

        public int NextSorting()
        {
            nextSorting += 6;
            return nextSorting;
        }

        public T Add<T>(T entity) where T : Entity
        {
            entity.World = this;
            if (entity.Root != null)
                entity.Root.transform.SetParent(Root.transform, true);
            pending.Add(entity);
            return entity;
        }

        /// <summary>Kindness flags earned this run; saved when the stage is won.</summary>
        public readonly List<string> Flags = new List<string>();

        /// <summary>The interaction the hero can do right now (nearest offered this frame), or null.</summary>
        public IInteractable Interaction { get; private set; }
        IInteractable offered;

        public void OfferInteraction(IInteractable thing)
        {
            if (Hero == null || !thing.Available)
                return;
            var d = Mathf.Abs(thing.Position.x - Hero.Body.Position.x);
            if (d > 1.8f)
                return;
            if (offered == null || d < Mathf.Abs(offered.Position.x - Hero.Body.Position.x))
                offered = thing;
        }

        public float PlayerX
        {
            get
            {
                if (Hero != null) return Hero.Body.Position.x;
                if (Rider != null) return Rider.Position.x;
                return 0f;
            }
        }

        public bool TryGetPlayerBounds(out Rect bounds)
        {
            if (Hero != null)
            {
                bounds = Hero.Body.Bounds;
                return true;
            }
            if (Rider != null)
            {
                bounds = Rider.Bounds;
                return true;
            }
            bounds = default(Rect);
            return false;
        }

        public void Tip(LocText text)
        {
            if (TipShown != null)
                TipShown(text);
        }

        public void Float(Vector3 position, string displayText, Color color)
        {
            if (Floating != null)
                Floating(position, displayText, color);
        }

        public void CountGoal()
        {
            GoalDone++;
            if (GoalChanged != null)
                GoalChanged();
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, amount);
        }

        public void AddHonor(int amount, LocText note)
        {
            Honor += amount;
            if (note != null)
                HonorNote = note;
            AudioService.Play(amount > 0 ? Sfx.Honor : Sfx.Defeat, 0.7f);
        }

        public IEnumerable<IHittable> Hittables()
        {
            foreach (var e in Entities)
            {
                var h = e as IHittable;
                if (h != null && !e.Removed && h.CanBeHit)
                    yield return h;
            }
        }

        public void Tick(float dt)
        {
            if (pending.Count > 0)
            {
                Entities.AddRange(pending);
                pending.Clear();
            }
            Interaction = offered;
            offered = null;
            for (var i = 0; i < Entities.Count; i++)
                if (!Entities[i].Removed)
                    Entities[i].Tick(dt);
            for (var i = Entities.Count - 1; i >= 0; i--)
            {
                if (Entities[i].Removed)
                {
                    Entities[i].Dispose();
                    Entities.RemoveAt(i);
                }
            }
            UpdateCamera(dt);
        }

        public void Dispose()
        {
            foreach (var e in Entities)
                e.Dispose();
            Entities.Clear();
            UnityEngine.Object.Destroy(Root);
            Camera.transform.position = new Vector3(0f, CameraBaseY, -10f);
        }

        // ------------------------------------------------------------- camera

        void UpdateCamera(float dt)
        {
            var focus = Hero != null ? Hero.Body.Position : Rider != null ? (Vector2)Rider.Position : Vector2.zero;
            float x;
            if (Rider != null)
            {
                x = focus.x + 5.5f; // the horse sits left of centre, looking ahead
            }
            else
            {
                var look = Hero != null ? (Hero.FacingRight ? 1.6f : -1.6f) : 0f;
                var current = Camera.transform.position.x;
                x = Mathf.Lerp(current, focus.x + look, 1f - Mathf.Exp(-dt * 4f));
            }
            var halfWidth = Camera.orthographicSize * Camera.aspect;
            if (Stage.Kind == StageKind.Walk)
            {
                var left = MinX - 1f + halfWidth;
                var right = MaxX + 1.5f - halfWidth;
                x = right > left ? Mathf.Clamp(x, left, right) : (left + right) * 0.5f;
            }
            var targetY = Mathf.Max(CameraBaseY, focus.y - 0.6f);
            cameraY = Mathf.Lerp(cameraY, targetY, 1f - Mathf.Exp(-dt * 3f));
            var offset = Vector2.zero;
            if (shake > 0f)
            {
                offset = UnityEngine.Random.insideUnitCircle * shake * 0.25f;
                shake = Mathf.MoveTowards(shake, 0f, dt * 3f);
            }
            Camera.transform.position = new Vector3(x + offset.x, cameraY + offset.y, -10f);
            if (backdrop != null)
                backdrop.Follow(Camera);
        }

        // ------------------------------------------------------------- building

        void Build()
        {
            backdrop = new Parallax(Root.transform, "bg/" + Stage.Backdrop, Camera);
            var tile = "tiles/" + (Stage.GroundTile ?? "ground_meadow");
            var props = 0;
            foreach (var p in Layout.Items)
            {
                switch (p.What)
                {
                    case Thing.Ground:
                        Solids.Add(new Solid { Left = p.X, Right = p.X + p.W, Top = 0f, Bottom = -30f });
                        GroundSprite(p.X, p.W, tile);
                        break;
                    case Thing.Water:
                        WaterSprite(p.X, p.W);
                        break;
                    case Thing.Ledge:
                        Solids.Add(new Solid { Left = p.X, Right = p.X + p.W, Top = p.Y, Bottom = p.Y - 0.6f, OneWay = true });
                        LedgeSprite(p.X, p.Y, p.W);
                        break;
                    case Thing.Prop:
                        Sprite("props/" + p.Name, new Vector2(p.X, p.Y - 0.08f), -400 + (props++ % 50), Vector2.one * Scatter(p.X));
                        break;
                    case Thing.PropFront:
                        Sprite("props/" + p.Name, new Vector2(p.X, p.Y - 0.25f), 900 + (props++ % 50), Vector2.one);
                        break;
                    case Thing.Npc:
                        var npc = HumanPuppet.Create(Root.transform, p.Name, -300 + props++ * 4);
                        npc.transform.position = new Vector3(p.X, 0f, 0f);
                        npc.FacingRight = p.Index == 0;
                        break;
                }
            }

            foreach (var p in Layout.Items)
            {
                switch (p.What)
                {
                    case Thing.Leaf: Add(new Pickup(PickupKind.Leaf, new Vector2(p.X, p.Y), p.Index)); break;
                    case Thing.Coin: Add(new Pickup(PickupKind.Coin, new Vector2(p.X, p.Y), 0)); break;
                    case Thing.Heal: Add(new Pickup(PickupKind.Heal, new Vector2(p.X, p.Y), 0)); break;
                    case Thing.Tip: Add(new TipTrigger(p.X, p.Text)); break;
                    case Thing.Gate: Add(new Gate(p.X)); break;
                    case Thing.Lamb:
                    case Thing.HurtBird:
                        if (!SaveSystem.Data.Has(p.Name))
                            Add(new Kindness(p.What, p.X, p.Name));
                        break;
                    case Thing.Dummy: Add(new Dummy(p.X)); break;
                    case Thing.Target: Add(new Target(new Vector2(p.X, p.Y), false, p.Goal)); break;
                    case Thing.SwingTarget: Add(new Target(new Vector2(p.X, p.Y), true, p.Goal)); break;
                    case Thing.Trainee:
                        if (p.Name == null)
                            Add(new Fighter(FighterKind.Trainee, new Vector2(p.X, p.Y), p.Goal));
                        break;
                    case Thing.Spearman: Add(new Fighter(FighterKind.Spearman, new Vector2(p.X, p.Y), p.Goal)); break;
                    case Thing.Archer: Add(new Fighter(FighterKind.Archer, new Vector2(p.X, p.Y), p.Goal)); break;
                    case Thing.Rostam: Add(new RostamBoss(p.X)); break;
                    case Thing.Log:
                    case Thing.Branch:
                    case Thing.Stone:
                        Add(new Obstacle(p.What, p.X));
                        break;
                    case Thing.Onager:
                    case Thing.Foal:
                        Add(new Onager(p.X, p.What == Thing.Foal, p.Goal));
                        break;
                }
                if (p.Goal)
                    GoalNeeded++;
            }

            // A trainee who carries a kindness: once beaten, help him to his feet.
            foreach (var p in Layout.Items)
            {
                if (p.What != Thing.Trainee || p.Name == null || SaveSystem.Data.Has(p.Name))
                    continue;
                foreach (var e in pending)
                {
                    var f = e as Fighter;
                    if (f != null && Mathf.Abs(f.Body.Position.x - p.X) < 0.01f)
                        f.KindnessFlag = p.Name;
                }
            }

            if (Stage.GoalKind == GoalKind.Hunt || Stage.GoalKind == GoalKind.Duel)
                GoalNeeded = Stage.GoalCount;

            if (Stage.Kind == StageKind.Ride)
            {
                Rider = Add(new Rider(new Vector2(0f, 0f)));
                Rider.Init();
            }
            else
            {
                Hero = Add(new Hero(new Vector2(Stage.Boss ? 5f : 0.5f, 0f)));
                Hero.Init();
            }

            Camera.transform.position = new Vector3(Stage.Kind == StageKind.Ride ? 5.5f : 3f, CameraBaseY, -10f);
            cameraY = CameraBaseY;
            if (Stage.Kind == StageKind.Walk)
                Camera.transform.position = new Vector3(Mathf.Max(Camera.orthographicSize * Camera.aspect + MinX - 1f, 0f), CameraBaseY, -10f);
        }

        static float Scatter(float x)
        {
            return 0.88f + Mathf.Repeat(x * 0.37f, 0.24f);
        }

        public SpriteRenderer Sprite(string name, Vector2 bottomCentre, int order, Vector2 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root.transform, false);
            var size = Art.Size(name);
            go.transform.position = new Vector3(bottomCentre.x, bottomCentre.y + size.y * scale.y * 0.5f, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(name);
            sr.sortingOrder = order;
            return sr;
        }

        void GroundSprite(float x, float width, string tile)
        {
            var size = Art.Size(tile);
            var go = new GameObject("Ground");
            go.transform.SetParent(Root.transform, false);
            go.transform.position = new Vector3(x + width * 0.5f, -size.y * 0.5f + 0.04f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(tile);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, size.y);
            sr.sortingOrder = -100;

            var deep = new GameObject("Earth");
            deep.transform.SetParent(Root.transform, false);
            deep.transform.position = new Vector3(x + width * 0.5f, -size.y - 5f + 0.06f, 0f);
            var dr = deep.AddComponent<SpriteRenderer>();
            dr.sprite = Art.White;
            dr.drawMode = SpriteDrawMode.Sliced;
            dr.size = new Vector2(width, 10f);
            dr.color = tile.Contains("earth") ? Palette.Hex(0x6E4527) : Palette.Hex(0x7C5331);
            dr.sortingOrder = -101;
        }

        void WaterSprite(float x, float width)
        {
            var go = new GameObject("Water");
            go.transform.SetParent(Root.transform, false);
            go.transform.position = new Vector3(x + width * 0.5f, -2.75f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.White;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(width + 0.4f, 5f);
            sr.color = Palette.Hex(0x9FC3CC);
            sr.sortingOrder = -102;
            for (var i = 0; i < 3; i++)
            {
                var wave = new GameObject("Wave");
                wave.transform.SetParent(go.transform, false);
                wave.transform.localPosition = new Vector3(0f, 1.9f - i * 0.7f, 0f);
                var wr = wave.AddComponent<SpriteRenderer>();
                wr.sprite = Art.White;
                wr.drawMode = SpriteDrawMode.Sliced;
                wr.size = new Vector2(width * 0.7f, 0.06f);
                wr.color = new Color(1f, 1f, 1f, 0.55f);
                wr.sortingOrder = -101;
            }
        }

        void LedgeSprite(float x, float top, float width)
        {
            const string name = "props/ledge_l";
            var size = Art.Size(name);
            var go = new GameObject("Ledge");
            go.transform.SetParent(Root.transform, false);
            go.transform.position = new Vector3(x + width * 0.5f, top - size.y * 0.5f + 0.1f, 0f);
            go.transform.localScale = new Vector3(width / Mathf.Max(0.1f, size.x) * 1.08f, 1f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(name);
            sr.sortingOrder = -90;
        }
    }

    /// <summary>A painted backdrop that drifts slower than the camera, mirrored copies side by side.</summary>
    public class Parallax
    {
        const float Factor = 0.12f; // how much of the camera's motion the backdrop keeps
        readonly Transform[] copies = new Transform[3];
        readonly float width;

        public Parallax(Transform parent, string sprite, Camera camera)
        {
            var size = Art.Size(sprite);
            var height = camera.orthographicSize * 2f * 1.22f;
            var scale = height / Mathf.Max(0.1f, size.y);
            width = size.x * scale;
            for (var i = 0; i < copies.Length; i++)
            {
                var go = new GameObject("Backdrop " + i);
                go.transform.SetParent(parent, false);
                go.transform.localScale = new Vector3(scale * (i % 2 == 1 ? -1f : 1f), scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.Get(sprite);
                sr.sortingOrder = -1000;
                copies[i] = go.transform;
            }
        }

        public void Follow(Camera camera)
        {
            var cam = camera.transform.position;
            var scroll = cam.x * Factor;
            // Copy k sits at k*width in backdrop space; pick the three around the view.
            var first = Mathf.FloorToInt(scroll / width + 0.5f) - 1;
            for (var i = 0; i < copies.Length; i++)
            {
                var k = first + i;
                var mirrored = (k % 2 + 2) % 2 == 1;
                var t = copies[i];
                var s = t.localScale;
                t.localScale = new Vector3(Mathf.Abs(s.x) * (mirrored ? -1f : 1f), s.y, 1f);
                t.position = new Vector3(cam.x - scroll + k * width, cam.y + 0.9f, 0f);
            }
        }
    }
}
