using UnityEngine;

namespace Arash.Art
{
    public enum Biome
    {
        Village,
        Border,
        Forest,
        Valley,
        Mountain,
        Peak,
        River,
    }

    public enum TimeOfDay
    {
        Day,
        Dusk,
        Night,
    }

    /// <summary>Moves a layer with the camera by a fraction, for depth.</summary>
    public class ParallaxLayer : MonoBehaviour
    {
        public float factor;
        public Transform cameraTransform;

        Vector3 origin;
        Vector3 cameraOrigin;

        void Start()
        {
            origin = transform.position;
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform != null)
                cameraOrigin = cameraTransform.position;
        }

        void LateUpdate()
        {
            if (cameraTransform == null)
                return;
            var delta = cameraTransform.position - cameraOrigin;
            transform.position = origin + new Vector3(delta.x * factor, delta.y * factor * 0.6f, 0f);
        }
    }

    /// <summary>Keeps a gradient sky sprite filling the camera's view.</summary>
    public class SkyFill : MonoBehaviour
    {
        public Camera target;

        SpriteRenderer sky;

        void LateUpdate()
        {
            if (target == null)
                return;
            if (sky == null)
                sky = GetComponent<SpriteRenderer>();
            var size = sky != null && sky.sprite != null ? (Vector2)sky.sprite.bounds.size : Vector2.one;
            var height = target.orthographicSize * 2f + 1f;
            var width = height * target.aspect + 1f;
            transform.position = new Vector3(target.transform.position.x, target.transform.position.y, 5f);
            transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        }
    }

    /// <summary>
    /// Builds a biome backdrop: gradient sky, sun or moon, and the far, mid and ground layers drawn
    /// by tools/art, tiled across the battlefield and tinted for the time of day.
    /// </summary>
    public static class Backdrop
    {
        public struct Palette
        {
            public Color SkyTop;
            public Color SkyBottom;
            public Color Far;
            public Color Mid;
            public Color Ground;
        }

        public static Palette For(Biome biome, TimeOfDay time)
        {
            var p = new Palette { Far = Color.white, Mid = Color.white, Ground = Color.white };
            switch (time)
            {
                case TimeOfDay.Dusk:
                    p.SkyTop = new Color(0.30f, 0.33f, 0.55f);
                    p.SkyBottom = new Color(0.98f, 0.62f, 0.40f);
                    p.Far = new Color(0.95f, 0.72f, 0.70f);
                    p.Mid = new Color(0.95f, 0.80f, 0.72f);
                    p.Ground = new Color(0.92f, 0.82f, 0.74f);
                    break;
                case TimeOfDay.Night:
                    p.SkyTop = new Color(0.04f, 0.06f, 0.16f);
                    p.SkyBottom = new Color(0.17f, 0.25f, 0.42f);
                    p.Far = new Color(0.36f, 0.42f, 0.60f);
                    p.Mid = new Color(0.42f, 0.48f, 0.66f);
                    p.Ground = new Color(0.52f, 0.56f, 0.70f);
                    break;
                default:
                    p.SkyTop = new Color(0.40f, 0.64f, 0.86f);
                    p.SkyBottom = new Color(0.96f, 0.88f, 0.72f);
                    break;
            }
            if (time == TimeOfDay.Day && (biome == Biome.Mountain || biome == Biome.Peak))
                p.SkyBottom = new Color(0.84f, 0.90f, 0.96f);
            if (time == TimeOfDay.Day && biome == Biome.Forest)
                p.SkyBottom = new Color(0.82f, 0.88f, 0.80f);
            return p;
        }

        /// <summary>
        /// Builds the backdrop under a new root. <paramref name="groundY"/> is the world height of
        /// the walking surface; layers span <paramref name="width"/> units around <paramref name="centerX"/>.
        /// </summary>
        public static Transform Build(Biome biome, TimeOfDay time, Camera camera, float groundY, float centerX, float width)
        {
            var root = new GameObject("Backdrop").transform;
            var palette = For(biome, time);
            var name = biome.ToString().ToLowerInvariant();

            if (camera != null)
            {
                camera.backgroundColor = palette.SkyBottom;
                var sky = ArtLibrary.Renderer(root, "Sky", GradientSprite(palette.SkyTop, palette.SkyBottom), -100);
                sky.gameObject.AddComponent<SkyFill>().target = camera;
            }

            var celestial = ArtLibrary.Prop(time == TimeOfDay.Night ? "moon" : "sun");
            if (celestial != null)
            {
                var y = time == TimeOfDay.Day ? groundY + 11f : groundY + 6.5f;
                var body = ArtLibrary.Renderer(root, "Sun", celestial, -90, new Vector2(centerX + width * 0.12f, y));
                if (time == TimeOfDay.Dusk)
                    body.color = new Color(1f, 0.7f, 0.45f);
                Parallax(body.transform, camera, 0.95f);
            }

            Layer(root, camera, "Backdrops/" + name + "_far", palette.Far, groundY, centerX, width * 0.8f, -60, 0.8f);
            Layer(root, camera, "Backdrops/" + name + "_mid", palette.Mid, groundY, centerX, width * 0.9f, -50, 0.45f);
            Layer(root, camera, "Backdrops/" + name + "_ground", palette.Ground, groundY, centerX, width * 1.2f, 5, 0f);
            return root;
        }

        static void Layer(Transform root, Camera camera, string path, Color tint, float groundY, float centerX, float width, int order, float parallax)
        {
            var sprite = ArtLibrary.Get(path);
            if (sprite == null)
                return;
            var renderer = ArtLibrary.Renderer(root, path, sprite, order, new Vector2(centerX, groundY));
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            // Keep a whole number of tiles so the pattern lines up with the pivot.
            var tileWidth = sprite.bounds.size.x;
            var tiles = Mathf.Max(1, Mathf.CeilToInt(width / tileWidth));
            renderer.size = new Vector2(tiles * tileWidth, sprite.bounds.size.y);
            renderer.color = tint;
            if (parallax > 0f)
                Parallax(renderer.transform, camera, parallax);
        }

        static void Parallax(Transform layer, Camera camera, float factor)
        {
            if (camera == null)
                return;
            var p = layer.gameObject.AddComponent<ParallaxLayer>();
            p.factor = factor;
            p.cameraTransform = camera.transform;
        }

        /// <summary>A 1×64 vertical gradient, stretched over the view by <see cref="SkyFill"/>.</summary>
        public static Sprite GradientSprite(Color top, Color bottom)
        {
            const int height = 64;
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (var y = 0; y < height; y++)
                texture.SetPixel(0, y, Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / (height - 1f))));
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f), height, 0, SpriteMeshType.FullRect);
        }
    }
}
