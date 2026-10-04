using System.Linq;
using Arash.Core;
using Arash.Levels;
using Arash.Localization;
using Arash.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arash.Story
{
    /// <summary>Drifts a layer slowly for parallax.</summary>
    public class ParallaxDrift : MonoBehaviour
    {
        public Vector2 velocity;

        void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);
        }
    }

    /// <summary>
    /// Plays a <see cref="CutsceneDefinition"/> (F-20) in the Cutscene scene: each panel is a simple
    /// illustrated scene drawn from placeholder shapes, drifting in parallax layers, with a caption.
    /// Tap for the next panel, Skip to leave. Real illustrations replace the motifs later.
    /// </summary>
    public class CutscenePlayer : ScreenBase
    {
        [SerializeField] Sprite square;
        [SerializeField] Sprite circle;
        [SerializeField] Material spriteMaterial;
        [SerializeField] Camera sceneCamera;

        CutsceneDefinition cutscene;
        int index;
        float panelTime;
        Transform world;
        bool finished;

        protected override void Start()
        {
            cutscene = SceneFlow.PendingCutscene;
            if (cutscene == null)
            {
                var catalog = LevelCatalog.Load();
                cutscene = catalog != null ? catalog.AllCutscenes().FirstOrDefault() : null;
            }
            if (cutscene == null || cutscene.panels.Count == 0)
            {
                Finish();
                return;
            }
            ShowPanel(0);
            base.Start();
        }

        void Update()
        {
            if (finished || cutscene == null || GamePause.IsPaused)
                return;
            panelTime += Time.deltaTime;
            if (panelTime >= cutscene.panels[index].duration)
                Next();
        }

        void Next()
        {
            if (index + 1 >= cutscene.panels.Count)
                Finish();
            else
                ShowPanel(index + 1);
        }

        void Finish()
        {
            if (finished)
                return;
            finished = true;
            SceneFlow.FinishCutscene();
        }

        void ShowPanel(int panelIndex)
        {
            index = panelIndex;
            panelTime = 0f;
            DrawWorld(cutscene.panels[index]);
            if (Canvas != null)
                Rebuild();
        }

        protected override void Build(RectTransform canvas)
        {
            if (cutscene == null)
                return;
            var panel = cutscene.panels[index];

            var catcher = UIFactory.Panel(canvas, "Tap", Color.clear);
            UIFactory.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(Next);

            if (index == 0)
            {
                var title = UIFactory.Label(canvas, Loc.T(cutscene.titleKey), 64, UIFactory.Gold, TextAnchor.MiddleCenter, true);
                UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1600f, 100f));
            }

            var band = UIFactory.Panel(canvas, "Caption", new Color(0f, 0f, 0f, 0.6f));
            band.raycastTarget = false;
            UIFactory.Place(band.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 230f));
            var caption = UIFactory.WrappedLabel(band.transform, Loc.Get(panel.captionKey), 44, UIFactory.Cream, 1600f, TextAnchor.MiddleCenter);
            UIFactory.Place(caption.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700f, 200f));

            var skip = UIFactory.Button(canvas, Loc.T("ui.skip"), Finish, new Color(0f, 0f, 0f, 0.45f), 36);
            UIFactory.Place((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(220f, 80f));

            var progress = UIFactory.Label(canvas, Loc.Number(index + 1) + " / " + Loc.Number(cutscene.panels.Count),
                30, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleLeft);
            UIFactory.Place(progress.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(300f, 60f));
        }

        // ------------------------------------------------------------- placeholder illustration

        void DrawWorld(CutscenePanel panel)
        {
            if (world != null)
                Destroy(world.gameObject);
            world = new GameObject("Cutscene World").transform;

            if (sceneCamera != null)
            {
                sceneCamera.backgroundColor = panel.sky;
                sceneCamera.transform.position = new Vector3(0f, 0f, -10f);
            }

            var far = Layer("Far", 0.15f);
            var mid = Layer("Mid", 0.35f);
            var near = Layer("Near", 0.7f);
            var sky = Layer("Sky", 0.05f);

            Shape(near, square, new Vector2(0f, -4.6f), new Vector2(60f, 3f), panel.land, 5);
            var shade = Color.Lerp(panel.land, panel.sky, 0.45f);

            switch (panel.motif)
            {
                case CutsceneMotif.Village:
                    Shape(sky, circle, new Vector2(6f, 3.2f), Vector2.one * 1.6f, new Color(1f, 0.9f, 0.6f), 1);
                    for (var i = 0; i < 6; i++)
                        House(mid, new Vector2(-8f + i * 3.2f, -2.4f), Color.Lerp(panel.land, Color.white, 0.25f));
                    Figure(near, new Vector2(-1f, -3.1f), new Color(0.10f, 0.42f, 0.55f));
                    break;

                case CutsceneMotif.Army:
                    Peaks(far, shade, 5);
                    for (var row = 0; row < 3; row++)
                    for (var i = 0; i < 14; i++)
                        Figure(row == 2 ? near : mid, new Vector2(-12f + i * 1.8f + row * 0.6f, -2.6f - row * 0.35f),
                            row % 2 == 0 ? new Color(0.55f, 0.15f, 0.12f) : new Color(0.10f, 0.42f, 0.55f));
                    for (var i = 0; i < 4; i++)
                        Banner(mid, new Vector2(-9f + i * 6f, -1f), i % 2 == 0 ? new Color(0.9f, 0.72f, 0.32f) : new Color(0.7f, 0.15f, 0.1f));
                    break;

                case CutsceneMotif.Forest:
                    for (var i = 0; i < 16; i++)
                        Tree(i % 2 == 0 ? far : mid, new Vector2(-14f + i * 1.9f, -1.8f), i % 2 == 0 ? 0.8f : 1.1f);
                    for (var i = 0; i < 4; i++)
                        Shape(near, square, new Vector2(-10f + i * 7f, -1.5f + i % 2), new Vector2(6f, 0.5f), new Color(1f, 1f, 1f, 0.25f), 20);
                    break;

                case CutsceneMotif.Camp:
                    for (var i = 0; i < 6; i++)
                        Tent(mid, new Vector2(-9f + i * 3.6f, -3f), i % 3 == 0 ? new Color(0.85f, 0.8f, 0.65f) : new Color(0.6f, 0.2f, 0.15f));
                    Shape(near, circle, new Vector2(1f, -3.3f), Vector2.one * 0.8f, new Color(1f, 0.55f, 0.15f), 22);
                    Figure(near, new Vector2(-0.5f, -3.1f), new Color(0.10f, 0.42f, 0.55f));
                    Figure(near, new Vector2(2.4f, -3.1f), new Color(0.75f, 0.58f, 0.15f));
                    break;

                case CutsceneMotif.Mountains:
                    Peaks(far, shade, 6);
                    Peaks(mid, Color.Lerp(shade, panel.land, 0.5f), 4);
                    Figure(near, new Vector2(-3f, -3.1f), new Color(0.10f, 0.42f, 0.55f));
                    break;

                case CutsceneMotif.Damavand:
                    Peak(far, new Vector2(0f, -6f), 9f, new Color(0.45f, 0.42f, 0.45f), true);
                    Figure(mid, new Vector2(0f, 2.75f), new Color(0.10f, 0.42f, 0.55f));
                    Shape(sky, circle, new Vector2(-7f, 3.5f), Vector2.one * 1.4f, new Color(1f, 0.8f, 0.5f), 1);
                    break;

                case CutsceneMotif.ArrowFlight:
                    for (var i = 0; i < 6; i++)
                        Shape(i % 2 == 0 ? far : mid, circle, new Vector2(-12f + i * 5f, 1f + (i % 3)), new Vector2(3.5f, 1.4f), new Color(1f, 1f, 1f, 0.7f), 3);
                    var arrow = Arrow(sky, new Vector2(-8f, 0f));
                    arrow.gameObject.AddComponent<ParallaxDrift>().velocity = new Vector2(3.2f, 0.3f);
                    break;

                case CutsceneMotif.River:
                    Shape(mid, square, new Vector2(0f, -3.4f), new Vector2(60f, 1.2f), new Color(0.25f, 0.5f, 0.75f), 8);
                    for (var i = 0; i < 12; i++)
                        Shape(near, square, new Vector2(-10f + i * 1.7f, -2.5f), new Vector2(0.08f, 0.9f), new Color(0.35f, 0.5f, 0.25f), 9);
                    var stuck = Arrow(near, new Vector2(1.5f, -2.8f));
                    stuck.rotation = Quaternion.Euler(0f, 0f, -55f);
                    break;

                case CutsceneMotif.Celebration:
                    for (var i = 0; i < 18; i++)
                    {
                        var lantern = Shape(i % 2 == 0 ? mid : near, circle, new Vector2(-12f + i * 1.4f, -2f + (i * 7 % 5)),
                            Vector2.one * 0.45f, new Color(1f, 0.75f, 0.3f), 15);
                        lantern.gameObject.AddComponent<ParallaxDrift>().velocity = new Vector2(0f, 0.3f + (i % 3) * 0.1f);
                    }
                    for (var i = 0; i < 8; i++)
                        Figure(near, new Vector2(-7f + i * 2f, -3.1f), i % 2 == 0 ? new Color(0.10f, 0.42f, 0.55f) : new Color(0.75f, 0.58f, 0.15f));
                    break;
            }
        }

        Transform Layer(string name, float speed)
        {
            var layer = new GameObject(name).transform;
            layer.SetParent(world, false);
            layer.gameObject.AddComponent<ParallaxDrift>().velocity = new Vector2(-speed, 0f);
            return layer;
        }

        Transform Shape(Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject("Shape");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            if (spriteMaterial != null)
                renderer.sharedMaterial = spriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (sprite == square)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = size;
            }
            else
            {
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
            return go.transform;
        }

        void Peaks(Transform layer, Color color, int count)
        {
            for (var i = 0; i < count; i++)
                Peak(layer, new Vector2(-14f + i * (28f / count) + (i % 2) * 1.5f, -5f), 4f + (i * 37 % 4), color, i % 2 == 0);
        }

        void Peak(Transform layer, Vector2 basePosition, float height, Color color, bool snow)
        {
            // A rotated square is a passable mountain silhouette.
            var side = height * 1.414f;
            var peak = Shape(layer, square, basePosition, new Vector2(side, side), color, 2);
            peak.localRotation = Quaternion.Euler(0f, 0f, 45f);
            if (snow)
            {
                var cap = Shape(layer, square, basePosition + new Vector2(0f, height * 0.78f), new Vector2(side * 0.22f, side * 0.22f), Color.white, 3);
                cap.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        void House(Transform layer, Vector2 position, Color color)
        {
            Shape(layer, square, position, new Vector2(2f, 1.6f), color, 6);
            var roof = Shape(layer, square, position + new Vector2(0f, 0.8f), new Vector2(1.45f, 1.45f), new Color(0.45f, 0.25f, 0.15f), 7);
            roof.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        void Tent(Transform layer, Vector2 position, Color color)
        {
            var tent = Shape(layer, square, position, new Vector2(2f, 2f), color, 6);
            tent.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        void Tree(Transform layer, Vector2 position, float scale)
        {
            Shape(layer, square, position + new Vector2(0f, -0.8f * scale), new Vector2(0.3f, 2f) * scale, new Color(0.3f, 0.2f, 0.12f), 6);
            Shape(layer, circle, position + new Vector2(0f, 0.6f * scale), new Vector2(1.8f, 2.2f) * scale, new Color(0.15f, 0.35f, 0.2f), 7);
        }

        void Figure(Transform layer, Vector2 position, Color color)
        {
            Shape(layer, square, position, new Vector2(0.45f, 1.1f), color, 12);
            Shape(layer, circle, position + new Vector2(0f, 0.8f), Vector2.one * 0.42f, new Color(0.87f, 0.68f, 0.52f), 13);
        }

        void Banner(Transform layer, Vector2 position, Color color)
        {
            Shape(layer, square, position, new Vector2(0.08f, 3f), new Color(0.3f, 0.2f, 0.12f), 10);
            Shape(layer, square, position + new Vector2(0.45f, 1.1f), new Vector2(0.8f, 0.6f), color, 11);
        }

        Transform Arrow(Transform layer, Vector2 position)
        {
            var arrow = new GameObject("Arrow").transform;
            arrow.SetParent(layer, false);
            arrow.localPosition = position;
            Shape(arrow, square, new Vector2(-0.9f, 0f), new Vector2(1.8f, 0.09f), new Color(0.9f, 0.72f, 0.32f), 30);
            Shape(arrow, square, new Vector2(-1.75f, 0f), new Vector2(0.35f, 0.24f), new Color(0.72f, 0.16f, 0.12f), 31);
            var head = Shape(arrow, square, Vector2.zero, new Vector2(0.24f, 0.24f), new Color(0.85f, 0.85f, 0.9f), 31);
            head.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return arrow;
        }
    }
}
