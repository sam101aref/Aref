using IranVsTuran.Art;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Battle
{
    /// <summary>
    /// Anything with hit points on the battlefield. The root object stays unscaled; the body
    /// sprite underneath is scaled, flipped and animated (bob while walking, lunge when hitting).
    /// </summary>
    public abstract class Unit
    {
        protected readonly Battlefield field;
        public GameObject Root { get; private set; }
        protected Transform body;
        protected SpriteRenderer sprite;
        SpriteRenderer shadow;
        HealthBar bar;
        float scale = 1f;
        float facing = 1f;
        float animTime;
        float flash;
        float lunge;

        public Vector2 Position;
        public float Hp;
        public float MaxHp;
        public float Armor;
        public float MagicResist;
        public float StunTimer;
        /// <summary>World height of the sprite, for health bars and where projectiles hit.</summary>
        public float Height { get; private set; }
        public bool Removed { get; private set; }
        public bool Alive { get { return Hp > 0f && !Removed; } }
        public Vector2 Center { get { return Position + new Vector2(0f, Height * 0.45f); } }

        protected bool Moving;
        protected Color Tint = Color.white;

        protected Unit(Battlefield field)
        {
            this.field = field;
        }

        protected void CreateVisual(string name, Sprite bodySprite, float unitScale, float height, bool shadowOnGround = true)
        {
            scale = unitScale;
            Height = height * unitScale;
            Root = new GameObject(name);
            Root.transform.SetParent(field.transform, false);

            if (shadowOnGround)
            {
                shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
                shadow.sprite = ArtLibrary.SoftCircle();
                shadow.color = new Color(0f, 0f, 0f, 0.35f);
                shadow.sortingOrder = Depth.Ground;
                shadow.transform.SetParent(Root.transform, false);
                shadow.transform.localScale = new Vector3(0.45f * unitScale, 0.16f * unitScale, 1f);
            }

            sprite = new GameObject("Body").AddComponent<SpriteRenderer>();
            sprite.sprite = bodySprite;
            body = sprite.transform;
            body.SetParent(Root.transform, false);
            body.localScale = new Vector3(unitScale, unitScale, 1f);

            bar = new HealthBar(Root.transform, Height + 0.08f, Mathf.Clamp(0.35f * unitScale, 0.35f, 0.9f));
            SyncVisual(0f);
        }

        protected void ChangeSprite(Sprite bodySprite)
        {
            sprite.sprite = bodySprite;
        }

        public void Face(float dx)
        {
            if (Mathf.Abs(dx) > 0.01f)
                facing = dx > 0f ? 1f : -1f;
        }

        protected void Lunge()
        {
            lunge = 1f;
        }

        public virtual float TakeDamage(float amount, DamageType type)
        {
            if (!Alive || amount <= 0f)
                return 0f;
            var resist = type == DamageType.Physical ? Armor : type == DamageType.Magic ? MagicResist : 0f;
            var dealt = amount * (1f - Mathf.Clamp(resist, 0f, 0.9f));
            dealt = ModifyDamage(dealt);
            Hp -= dealt;
            flash = 0.12f;
            if (Hp <= 0f)
            {
                Hp = 0f;
                OnDeath();
            }
            return dealt;
        }

        /// <summary>Last chance to change incoming damage (e.g. Esfandiar's brazen body).</summary>
        protected virtual float ModifyDamage(float amount)
        {
            return amount;
        }

        public void Heal(float amount)
        {
            if (!Alive)
                return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
        }

        protected abstract void OnDeath();

        /// <summary>Updates position, depth, animation, tint and health bar.</summary>
        protected void SyncVisual(float dt)
        {
            if (Root == null)
                return;
            animTime += dt;
            flash = Mathf.Max(0f, flash - dt);
            lunge = Mathf.Max(0f, lunge - dt * 5f);

            Root.transform.localPosition = new Vector3(Position.x, Position.y, 0f);
            var bob = Moving ? Mathf.Abs(Mathf.Sin(animTime * 11f)) * 0.04f : Mathf.Sin(animTime * 2f) * 0.006f;
            var tilt = Moving ? Mathf.Sin(animTime * 11f) * 4f : 0f;
            body.localPosition = new Vector3(facing * Mathf.Sin(lunge * Mathf.PI) * 0.1f, bob, 0f);
            body.localRotation = Quaternion.Euler(0f, 0f, tilt * -facing);
            body.localScale = new Vector3(scale * facing, scale, 1f);

            sprite.sortingOrder = Depth.Of(Position.y);
            var color = Tint;
            if (flash > 0f)
                color = Color.Lerp(color, new Color(1f, 0.45f, 0.4f), 0.6f);
            if (StunTimer > 0f)
                color = Color.Lerp(color, new Color(1f, 1f, 0.6f), 0.5f + 0.3f * Mathf.Sin(animTime * 20f));
            sprite.color = color;
            bar.Set(MaxHp > 0f ? Hp / MaxHp : 0f);
        }

        protected void SetVisible(bool visible)
        {
            if (Root != null)
                Root.SetActive(visible);
        }

        public virtual void Remove()
        {
            if (Removed)
                return;
            Removed = true;
            if (Root != null)
                Object.Destroy(Root);
        }
    }
}
