using System.Collections.Generic;
using UnityEngine;

namespace Arash.Art
{
    /// <summary>
    /// Loads the game's sprites from Resources/Art (rendered by tools/art). Missing sprites return
    /// null, so code can treat a part as optional.
    /// </summary>
    public static class ArtLibrary
    {
        const string Root = "Art/";

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Material spriteMaterial;

        public static Sprite Get(string path)
        {
            Sprite sprite;
            if (cache.TryGetValue(path, out sprite))
                return sprite;
            sprite = Resources.Load<Sprite>(Root + path);
            cache[path] = sprite;
            return sprite;
        }

        public static Sprite Character(CharacterLook look, string part)
        {
            return Get("Characters/" + look.ToString().ToLowerInvariant() + "/" + part);
        }

        public static Sprite Prop(string name)
        {
            return Get("Props/" + name);
        }

        public static Sprite Projectile(string name)
        {
            return Get("Projectiles/" + name);
        }

        public static Sprite Fx(string name)
        {
            return Get("Fx/" + name);
        }

        public static Sprite Icon(string name)
        {
            return Get("UI/Icons/" + name);
        }

        public static Sprite UI(string name)
        {
            return Get("UI/" + name);
        }

        /// <summary>The unlit sprite material scenes use; set once by whoever has a reference to it.</summary>
        public static Material SpriteMaterial
        {
            get { return spriteMaterial; }
            set
            {
                if (value != null)
                    spriteMaterial = value;
            }
        }

        /// <summary>A sprite renderer on a new child object.</summary>
        public static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, int order, Vector2 localPosition = default(Vector2))
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (spriteMaterial != null)
                renderer.sharedMaterial = spriteMaterial;
            return renderer;
        }
    }
}
