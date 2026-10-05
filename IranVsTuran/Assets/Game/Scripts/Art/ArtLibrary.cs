using System.Collections.Generic;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Art
{
    /// <summary>
    /// Creates every sprite the game uses on first request and keeps it for the session.
    /// World sizes: a human is about 0.7 world units tall, a tower about 1.3 units wide.
    /// </summary>
    public static class ArtLibrary
    {
        /// <summary>World units the whole character canvas covers at scale 1.</summary>
        public const float CharacterWorldSize = 0.9f;
        public const float TowerWorldSize = 1.5f;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Cached(string key, System.Func<Sprite> create)
        {
            Sprite sprite;
            if (!cache.TryGetValue(key, out sprite) || sprite == null)
            {
                sprite = create();
                cache[key] = sprite;
            }
            return sprite;
        }

        public static Sprite Character(Look look, int pixels = 128)
        {
            return Cached("char_" + look.Key + "_" + pixels, () =>
                CharacterArt.Paint(look, pixels).ToSprite("Character", 0f, 0f, pixels / CharacterWorldSize));
        }

        public static Sprite Tower(TowerDef def)
        {
            return Cached("tower_" + def.id, () =>
                TowerArt.Paint(def, 160).ToSprite("Tower " + def.id, 0f, 0f, 160 / TowerWorldSize));
        }

        public static Sprite Slot()
        {
            return Cached("slot", () => TowerArt.PaintSlot(128).ToSprite("Slot", 0f, 0f, 128 / TowerWorldSize));
        }

        public static Sprite Icon(Icon icon)
        {
            return Cached("icon_" + icon, () => IconArt.Paint(icon, 96).ToSprite(icon.ToString(), 0f, 0f, 96));
        }

        public static Sprite SolidCircle() { return Cached("solid", () => IconArt.SolidCircle(64).ToSprite("Solid", 0f, 0f, 64)); }
        public static Sprite SoftCircle() { return Cached("soft", () => IconArt.SoftCircle(64).ToSprite("Soft", 0f, 0f, 32)); }
        public static Sprite Ring() { return Cached("ring", () => IconArt.RingSprite(256, 0.02f).ToSprite("Ring", 0f, 0f, 128)); }
        public static Sprite Arrow() { return Cached("arrow", () => IconArt.ArrowProjectile(64).ToSprite("Arrow", 0f, 0f, 128)); }
        public static Sprite Fireball() { return Cached("fire", () => IconArt.Fireball(48, 0xFFF0A0, 0xF06A20).ToSprite("Fireball", 0f, 0f, 96)); }
        public static Sprite LightOrb() { return Cached("light", () => IconArt.Fireball(48, 0xFFFFFF, 0x7AD8F0).ToSprite("Light", 0f, 0f, 96)); }
        public static Sprite Boulder() { return Cached("boulder", () => IconArt.BoulderSprite(48).ToSprite("Boulder", 0f, 0f, 160)); }
        public static Sprite Jar() { return Cached("jar", () => IconArt.JarSprite(48).ToSprite("Jar", 0f, 0f, 140)); }
        public static Sprite Flame() { return Cached("flame", () => IconArt.FlameSprite(64).ToSprite("Flame", 0f, 0f, 110)); }
        public static Sprite Spark() { return Cached("spark", () => IconArt.Spark(48).ToSprite("Spark", 0f, 0f, 96)); }
        public static Sprite Smoke() { return Cached("smoke", () => IconArt.Smoke(64).ToSprite("Smoke", 0f, 0f, 64)); }
        public static Sprite RallyFlag() { return Cached("rally", () => IconArt.RallyFlag(64).ToSprite("Rally", 0f, 0f, 120)); }

        /// <summary>A 1×1 white pixel scaled into bars and overlays.</summary>
        public static Sprite Pixel()
        {
            return Cached("pixel", () =>
            {
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var pixels = new Color32[16];
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                texture.Apply();
                return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            });
        }

        /// <summary>White rounded panel for 9-slicing in uGUI (tint with Image.color).</summary>
        public static Sprite Panel()
        {
            return Cached("panel", () => IconArt.Panel(64, 18f, 5f).ToSprite("Panel", 0f, 0f, 100f, new Vector4(22, 22, 22, 22)));
        }

        public static Sprite Map(LevelDef level)
        {
            // Not cached: a level's background is only needed while it is played.
            return MapPainter.Paint(level, 40f).ToSprite("Map " + level.id, 0f, 0f, 40f);
        }

        public static Sprite Backdrop(Backdrop backdrop)
        {
            return Cached("backdrop_" + backdrop, () => BackdropArt.Paint(backdrop, 800, 450).ToSprite("Backdrop", 0f, 0f, 50f));
        }

        public static Sprite WorldMap()
        {
            return Cached("worldmap", () => WorldMapArt.Paint(1280, 720).ToSprite("World Map", 0f, 0f, 80f));
        }

        /// <summary>Releases cached textures (e.g. when the language or quality changes).</summary>
        public static void Clear()
        {
            foreach (var sprite in cache.Values)
                if (sprite != null)
                {
                    Object.Destroy(sprite.texture);
                    Object.Destroy(sprite);
                }
            cache.Clear();
        }
    }
}
