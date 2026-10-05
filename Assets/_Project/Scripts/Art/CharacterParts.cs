using System.Collections;
using System.Collections.Generic;
using Arash.Combat;
using UnityEngine;

namespace Arash.Art
{
    /// <summary>Every drawn character; the sprites live in Resources/Art/Characters/{look}.</summary>
    public enum CharacterLook
    {
        Arash,
        Roshana,
        Mobad,
        Manuchehr,
        Afrasiab,
        Villager,
        Envoy,
        Turanian,
        ShieldBearer,
        Spearman,
        Rider,
        Slinger,
        FireArcher,
        Assassin,
        Raider,
        Shaman,
        Commander,
        Barman,
        Garsivaz,
        Div,
        WhiteDiv,
    }

    /// <summary>Default colours of a look: tunic and cape are drawn in greys and tinted.</summary>
    public struct LookStyle
    {
        public Color Tunic;
        public Color Cape;
        /// <summary>False for monsters, whose bodies are drawn in colour.</summary>
        public bool TintBody;

        public static LookStyle For(CharacterLook look)
        {
            switch (look)
            {
                case CharacterLook.Arash: return Make(0.10f, 0.42f, 0.55f, 0.66f, 0.15f, 0.17f);
                case CharacterLook.Roshana: return Make(0.22f, 0.45f, 0.30f, 0.5f, 0.3f, 0.2f);
                case CharacterLook.Mobad: return Make(0.96f, 0.94f, 0.87f, 0.9f, 0.85f, 0.7f);
                case CharacterLook.Manuchehr: return Make(0.42f, 0.20f, 0.55f, 0.70f, 0.14f, 0.16f);
                case CharacterLook.Afrasiab: return Make(0.26f, 0.12f, 0.12f, 0.45f, 0.08f, 0.08f);
                case CharacterLook.Villager: return Make(0.62f, 0.47f, 0.32f, 0.5f, 0.4f, 0.3f);
                case CharacterLook.Envoy: return Make(0.86f, 0.80f, 0.60f, 0.5f, 0.4f, 0.3f);
                case CharacterLook.Turanian: return Make(0.55f, 0.15f, 0.12f, 0.3f, 0.2f, 0.15f);
                case CharacterLook.ShieldBearer: return Make(0.42f, 0.32f, 0.26f, 0.3f, 0.2f, 0.15f);
                case CharacterLook.Spearman: return Make(0.62f, 0.50f, 0.30f, 0.3f, 0.2f, 0.15f);
                case CharacterLook.Rider: return Make(0.50f, 0.20f, 0.15f, 0.32f, 0.22f, 0.15f);
                case CharacterLook.Slinger: return Make(0.58f, 0.52f, 0.36f, 0.3f, 0.2f, 0.15f);
                case CharacterLook.FireArcher: return Make(0.78f, 0.36f, 0.12f, 0.3f, 0.2f, 0.15f);
                case CharacterLook.Assassin: return Make(0.20f, 0.19f, 0.23f, 0.15f, 0.14f, 0.17f);
                case CharacterLook.Raider: return Make(0.46f, 0.30f, 0.20f, 0.3f, 0.2f, 0.15f);
                case CharacterLook.Shaman: return Make(0.36f, 0.20f, 0.46f, 0.3f, 0.2f, 0.15f);
                case CharacterLook.Commander: return Make(0.62f, 0.12f, 0.10f, 0.40f, 0.10f, 0.08f);
                case CharacterLook.Barman: return Make(0.32f, 0.26f, 0.22f, 0.22f, 0.16f, 0.12f);
                case CharacterLook.Garsivaz: return Make(0.36f, 0.15f, 0.42f, 0.26f, 0.08f, 0.30f);
                default: return new LookStyle { Tunic = Color.white, Cape = Color.white, TintBody = false };
            }
        }

        static LookStyle Make(float r, float g, float b, float cr, float cg, float cb)
        {
            return new LookStyle { Tunic = new Color(r, g, b), Cape = new Color(cr, cg, cb), TintBody = true };
        }
    }

    /// <summary>
    /// The drawn body of a character: one sprite per part on the part objects that the archer
    /// prefab animates and ragdolls (torso, legs, head, aim pivot). Swapping the look, colours, hat
    /// or armour only swaps sprites. Hits flash the body briefly.
    /// </summary>
    public class CharacterSkin : MonoBehaviour
    {
        // Part positions in character space (feet at the origin, facing right).
        public static readonly Vector2 TorsoPosition = new Vector2(0f, 1.25f);
        public static readonly Vector2 LegsPosition = new Vector2(0f, 0.4f);
        public static readonly Vector2 HeadPosition = new Vector2(0f, 1.95f);
        public static readonly Vector2 ShoulderPosition = new Vector2(0.1f, 1.55f);
        /// <summary>Where arrows leave the bow, along the aim pivot's +X.</summary>
        public const float LaunchDistance = 0.78f;

        [SerializeField] bool facingRight = true;
        [SerializeField] SpriteRenderer legs;
        [SerializeField] SpriteRenderer torso;
        [SerializeField] SpriteRenderer armor;
        [SerializeField] SpriteRenderer detail;
        [SerializeField] SpriteRenderer cape;
        [SerializeField] SpriteRenderer back;
        [SerializeField] SpriteRenderer head;
        [SerializeField] SpriteRenderer hat;
        [SerializeField] SpriteRenderer arm;
        [SerializeField] SpriteRenderer weapon;

        static readonly Color FlashColor = new Color(1f, 0.55f, 0.5f);

        readonly Dictionary<SpriteRenderer, Color> baseColors = new Dictionary<SpriteRenderer, Color>();
        Coroutine flash;
        Health health;

        public CharacterLook Look { get; private set; }
        public bool FacingRight { get { return facingRight; } }
        public SpriteRenderer Weapon { get { return weapon; } }

        /// <summary>
        /// Creates the part objects and their renderers under <paramref name="root"/>. The archer
        /// prefab adds physics to the Torso, Legs, Head and AimPivot objects.
        /// </summary>
        public static CharacterSkin Build(Transform root, bool facingRight, CharacterLook look)
        {
            var f = facingRight ? 1f : -1f;
            var skin = root.gameObject.AddComponent<CharacterSkin>();
            skin.facingRight = facingRight;

            var torsoObject = Part(root, "Torso", TorsoPosition);
            skin.torso = Attach(torsoObject.gameObject, 31);
            skin.cape = ArtLibrary.Renderer(torsoObject, "Cape", null, 26);
            skin.back = ArtLibrary.Renderer(torsoObject, "Back", null, 27);
            skin.armor = ArtLibrary.Renderer(torsoObject, "Armor", null, 32);
            skin.detail = ArtLibrary.Renderer(torsoObject, "Detail", null, 33);

            var legsObject = Part(root, "Legs", LegsPosition);
            skin.legs = Attach(legsObject.gameObject, 30);

            var headObject = Part(root, "Head", HeadPosition);
            skin.head = Attach(headObject.gameObject, 34);
            skin.hat = ArtLibrary.Renderer(headObject, "Hat", null, 35);

            var pivot = Part(root, "AimPivot", new Vector2(ShoulderPosition.x * f, ShoulderPosition.y));
            skin.arm = ArtLibrary.Renderer(pivot, "Arm", null, 36);
            skin.weapon = ArtLibrary.Renderer(pivot, "Weapon", null, 37);

            skin.Apply(look);
            return skin;
        }

        static Transform Part(Transform root, string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        static SpriteRenderer Attach(GameObject go, int order)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            if (ArtLibrary.SpriteMaterial != null)
                renderer.sharedMaterial = ArtLibrary.SpriteMaterial;
            return renderer;
        }

        IEnumerable<SpriteRenderer> All()
        {
            yield return legs;
            yield return torso;
            yield return armor;
            yield return detail;
            yield return cape;
            yield return back;
            yield return head;
            yield return hat;
            yield return arm;
            yield return weapon;
        }

        /// <summary>Shows a look with its default colours.</summary>
        public void Apply(CharacterLook look)
        {
            Look = look;
            Set(legs, ArtLibrary.Character(look, "legs"));
            Set(torso, ArtLibrary.Character(look, "torso"));
            Set(detail, ArtLibrary.Character(look, "detail"));
            Set(cape, ArtLibrary.Character(look, "cape"));
            Set(back, ArtLibrary.Character(look, "back"));
            Set(head, ArtLibrary.Character(look, "head"));
            Set(hat, ArtLibrary.Character(look, "hat"));
            Set(arm, ArtLibrary.Character(look, "arm"));
            Set(weapon, ArtLibrary.Character(look, "weapon"));
            Set(armor, null);

            foreach (var renderer in All())
                if (renderer != null)
                {
                    renderer.flipX = !facingRight && renderer != arm && renderer != weapon;
                    renderer.flipY = !facingRight && (renderer == arm || renderer == weapon);
                    renderer.color = Color.white;
                }

            var style = LookStyle.For(look);
            if (style.TintBody)
                SetColors(style.Tunic, style.Cape);
            RememberColors();
        }

        static void Set(SpriteRenderer renderer, Sprite sprite)
        {
            if (renderer == null)
                return;
            renderer.sprite = sprite;
            renderer.enabled = sprite != null;
        }

        /// <summary>Tints the tunic (and sleeve) and the cape.</summary>
        public void SetColors(Color tunic, Color capeColor)
        {
            if (torso != null)
                torso.color = tunic;
            if (arm != null)
                arm.color = tunic;
            if (cape != null)
                cape.color = capeColor;
            RememberColors();
        }

        /// <summary>Multiplies every part by a colour, e.g. a darker variant of an enemy.</summary>
        public void Tint(Color tint)
        {
            foreach (var renderer in All())
                if (renderer != null)
                    renderer.color *= tint;
            RememberColors();
        }

        /// <summary>Replaces the hat (helmets) and tints it; null keeps the look's own hat.</summary>
        public void SetHat(Sprite sprite, Color tint)
        {
            if (hat == null)
                return;
            if (sprite != null)
                Set(hat, sprite);
            hat.color = tint;
            RememberColors();
        }

        public void SetArmor(Sprite sprite)
        {
            Set(armor, sprite);
            RememberColors();
        }

        public void SetWeapon(Sprite sprite)
        {
            Set(weapon, sprite);
        }

        void RememberColors()
        {
            baseColors.Clear();
            foreach (var renderer in All())
                if (renderer != null)
                    baseColors[renderer] = renderer.color;
        }

        void OnEnable()
        {
            health = GetComponent<Health>();
            if (health != null)
                health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        void OnDamaged(DamageInfo info, bool killed)
        {
            if (flash != null)
                StopCoroutine(flash);
            flash = StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {
            const float duration = 0.18f;
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                var k = 1f - t / duration;
                foreach (var pair in baseColors)
                    if (pair.Key != null)
                        pair.Key.color = Color.Lerp(pair.Value, pair.Value * FlashColor + new Color(0.25f, 0f, 0f, 0f), k);
                yield return null;
            }
            foreach (var pair in baseColors)
                if (pair.Key != null)
                    pair.Key.color = pair.Value;
            flash = null;
        }

        /// <summary>Fades every part out (bodies that have lain on the ground long enough).</summary>
        public IEnumerator FadeOut(float duration)
        {
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                foreach (var pair in baseColors)
                    if (pair.Key != null)
                    {
                        var c = pair.Value;
                        c.a *= 1f - t / duration;
                        pair.Key.color = c;
                    }
                yield return null;
            }
        }
    }
}
