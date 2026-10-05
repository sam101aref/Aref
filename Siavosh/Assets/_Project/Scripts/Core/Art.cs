using System;
using System.Collections.Generic;
using UnityEngine;

namespace Siavosh.Core
{
    /// <summary>
    /// Loads the game's sprites (rendered from Siavosh/Design into Resources/Art) and the manifest
    /// that says how puppet parts fit together. Also makes the simple procedural shapes the UI uses
    /// (rounded panels, rings, circles, soft glows).
    /// </summary>
    public static class Art
    {
        public const float PixelsPerUnit = 200f;

#pragma warning disable 0649 // filled in by JsonUtility
        [Serializable]
        public class SpriteInfo
        {
            public string name;
            public float w;
            public float h;
            public float x;
            public float y;
        }

        [Serializable]
        public class AnchorInfo
        {
            public string name;
            public float x;
            public float y;
        }

        [Serializable]
        public class Rig
        {
            public string name;
            public List<AnchorInfo> anchors = new List<AnchorInfo>();
        }

        [Serializable]
        class Manifest
        {
            public float pixelsPerUnit;
            public List<SpriteInfo> sprites = new List<SpriteInfo>();
            public List<Rig> rigs = new List<Rig>();
        }
#pragma warning restore 0649

        static Dictionary<string, SpriteInfo> infos;
        static Dictionary<string, Rig> rigs;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        static void LoadManifest()
        {
            infos = new Dictionary<string, SpriteInfo>();
            rigs = new Dictionary<string, Rig>();
            var asset = Resources.Load<TextAsset>("Art/manifest");
            if (asset == null)
            {
                Debug.LogError("[Art] Resources/Art/manifest.json is missing.");
                return;
            }
            var manifest = JsonUtility.FromJson<Manifest>(asset.text);
            foreach (var s in manifest.sprites)
                infos[s.name] = s;
            foreach (var r in manifest.rigs)
                rigs[r.name] = r;
        }

        public static SpriteInfo Info(string name)
        {
            if (infos == null)
                LoadManifest();
            SpriteInfo info;
            return infos.TryGetValue(name, out info) ? info : null;
        }

        /// <summary>A rig joint position relative to the rig origin, in world units.</summary>
        public static Vector2 Anchor(string rig, string anchor)
        {
            if (rigs == null)
                LoadManifest();
            Rig r;
            if (rigs.TryGetValue(rig, out r))
                foreach (var a in r.anchors)
                    if (a.name == anchor)
                        return new Vector2(a.x, a.y);
            return Vector2.zero;
        }

        /// <summary>A sprite from Resources/Art by its manifest name, e.g. "parts/siavosh_body".</summary>
        public static Sprite Get(string name)
        {
            Sprite sprite;
            if (sprites.TryGetValue(name, out sprite) && sprite != null)
                return sprite;

            sprite = Resources.Load<Sprite>("Art/" + name);
            if (sprite == null)
            {
                // Fallback for a texture that was not imported as a sprite.
                var texture = Resources.Load<Texture2D>("Art/" + name);
                if (texture != null)
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
                else
                    Debug.LogWarning("[Art] Missing sprite Art/" + name);
            }
            sprites[name] = sprite;
            return sprite;
        }

        /// <summary>World size of a sprite (from the manifest, or the sprite's own bounds).</summary>
        public static Vector2 Size(string name)
        {
            var info = Info(name);
            if (info != null)
                return new Vector2(info.w, info.h);
            var sprite = Get(name);
            return sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
        }

        // ------------------------------------------------------------- procedural shapes

        static Sprite circle;
        static Sprite ring;
        static Sprite glow;
        static readonly Dictionary<int, Sprite> panels = new Dictionary<int, Sprite>();
        static readonly Dictionary<int, Sprite> outlines = new Dictionary<int, Sprite>();
        static Sprite white;

        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var pixels = new Color32[16];
                    for (var i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(255, 255, 255, 255);
                    t.SetPixels32(pixels);
                    t.Apply();
                    white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
                }
                return white;
            }
        }

        public static Sprite Circle { get { return circle != null ? circle : (circle = MakeDisc(128, 0f, false)); } }
        public static Sprite Ring { get { return ring != null ? ring : (ring = MakeDisc(128, 0.12f, false)); } }
        public static Sprite Glow { get { return glow != null ? glow : (glow = MakeDisc(128, 0f, true)); } }

        /// <summary>A white rounded rectangle, 9-sliced so it stretches to any size.</summary>
        public static Sprite Panel(int radius)
        {
            Sprite s;
            if (!panels.TryGetValue(radius, out s) || s == null)
                panels[radius] = s = MakeRounded(radius, 0);
            return s;
        }

        /// <summary>The outline of a rounded rectangle, <paramref name="thickness"/> pixels wide, 9-sliced.</summary>
        public static Sprite Outline(int radius, int thickness)
        {
            var key = radius * 1000 + thickness;
            Sprite s;
            if (!outlines.TryGetValue(key, out s) || s == null)
                outlines[key] = s = MakeRounded(radius, thickness);
            return s;
        }

        static Sprite MakeDisc(int size, float ringWidth, bool soft)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            var r = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r)) / r;
                float a;
                if (soft)
                    a = Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d);
                else if (ringWidth > 0f)
                    a = Mathf.Clamp01((1f - d) * r) * Mathf.Clamp01((d - (1f - ringWidth)) * r);
                else
                    a = Mathf.Clamp01((1f - d) * r);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(pixels);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeRounded(int radius, int thickness)
        {
            var size = radius * 2 + 4;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            var c = size / 2f;
            var inner = c - radius; // half-size of the straight part
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                // Signed distance to a rounded box whose edge is at half-size c.
                var px = Mathf.Abs(x + 0.5f - c) - inner;
                var py = Mathf.Abs(y + 0.5f - c) - inner;
                var outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                var a = Mathf.Clamp01(0.5f - outside);
                if (thickness > 0)
                    a *= Mathf.Clamp01(outside + thickness + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(pixels);
            t.Apply();
            var border = radius + 1;
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
