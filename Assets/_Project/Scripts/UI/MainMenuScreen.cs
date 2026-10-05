using Arash.Art;
using Arash.Core;
using Arash.Levels;
using Arash.Localization;
using UnityEngine;

namespace Arash.UI
{
    /// <summary>Main menu (F-13): title, play, story book (F-21), settings and a quick language switch.</summary>
    public class MainMenuScreen : ScreenBase
    {
        [SerializeField] Material spriteMaterial;

        bool storyBookOpen;

        protected override void Start()
        {
            ArtLibrary.SpriteMaterial = spriteMaterial;
            DrawScene();
            base.Start();
        }

        /// <summary>The menu's backdrop: Arash on the mountainside at dusk, Damavand behind him.</summary>
        void DrawScene()
        {
            var camera = Camera.main;
            if (camera == null)
                return;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 1.6f, -10f);
            const float ground = -2.4f;
            Backdrop.Build(Biome.Mountain, TimeOfDay.Dusk, camera, ground, 0f, 40f);

            var mountain = ArtLibrary.Renderer(null, "Damavand", ArtLibrary.Prop("damavand"), -40, new Vector2(5.5f, ground + 0.6f));
            mountain.transform.localScale = Vector3.one * 0.9f;
            mountain.color = new Color(0.95f, 0.78f, 0.75f);
            var bird = ArtLibrary.Renderer(null, "Simurgh", ArtLibrary.Prop("simurgh"), -30, new Vector2(4f, 6f));
            bird.transform.localScale = Vector3.one * 0.45f;
            bird.color = new Color(1f, 0.9f, 0.85f, 0.85f);
            bird.gameObject.AddComponent<Story.ParallaxDrift>().velocity = new Vector2(-0.15f, 0.02f);

            var arash = new GameObject("Arash").transform;
            arash.position = new Vector3(-5.2f, ground, 0f);
            arash.localScale = Vector3.one * 1.25f;
            var skin = CharacterSkin.Build(arash, true, CharacterLook.Arash);
            var outfit = Armory.Find(SaveSystem.Data.equippedOutfit) ?? Armory.Find(Armory.DefaultOutfit);
            skin.SetColors(outfit.Tunic, outfit.Cape);
            skin.SetHat(null, outfit.Cap);
            skin.Tint(new Color(1f, 0.9f, 0.84f));
            var pivot = arash.Find("AimPivot");
            if (pivot != null)
                pivot.rotation = Quaternion.Euler(0f, 0f, 28f);
        }

        protected override void Build(RectTransform canvas)
        {
            var shade = UIFactory.Panel(canvas, "Shade", new Color(0.03f, 0.05f, 0.12f, 0.35f));
            UIFactory.Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            var title = UIFactory.Label(canvas, Loc.T("game.title"), 130, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1700f, 200f));

            var subtitle = UIFactory.Label(canvas, Loc.T("game.subtitle"), 46, UIFactory.Cream);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 175f), new Vector2(1700f, 80f));

            var play = UIFactory.Button(canvas, Loc.T("ui.play"), SceneFlow.ToWorldMap, UIFactory.Turquoise, 60);
            UIFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 130f));

            var story = UIFactory.Button(canvas, Loc.T("ui.story_book"), () =>
            {
                storyBookOpen = true;
                BuildStoryBook(canvas);
            }, UIFactory.LapisLight, 48);
            UIFactory.Place((RectTransform)story.transform, new Vector2(0.5f, 0.5f), new Vector2(-300f, -160f), new Vector2(560f, 110f));

            var settings = UIFactory.Button(canvas, Loc.T("ui.settings"), OpenSettings, UIFactory.LapisLight, 48);
            UIFactory.Place((RectTransform)settings.transform, new Vector2(0.5f, 0.5f), new Vector2(300f, -160f), new Vector2(560f, 110f));

            // Shows the other language's name, written in that language.
            var other = Loc.Current == Language.Persian ? Language.English : Language.Persian;
            var otherName = other == Language.Persian ? Loc.Display("فارسی") : "English";
            var language = UIFactory.Button(canvas, otherName, () => GameSettings.Language = other, UIFactory.LapisLight, 44);
            UIFactory.Place((RectTransform)language.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(560f, 100f));

            if (storyBookOpen)
                BuildStoryBook(canvas);

            var version = UIFactory.Label(canvas, "v" + Application.version, 28, new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerRight);
            UIFactory.Place(version.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(400f, 50f));
        }

        /// <summary>The story book (F-21): every cutscene seen so far, to watch again.</summary>
        void BuildStoryBook(RectTransform canvas)
        {
            var overlay = UIFactory.Overlay(canvas, "Story Book");
            var window = UIFactory.Window(overlay);
            UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 900f));

            var title = UIFactory.Label(window, Loc.T("ui.story_book"), 64, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 90f));

            var area = UIFactory.Rect(window, "Entries");
            UIFactory.Stretch(area);
            area.offsetMin = new Vector2(60f, 160f);
            area.offsetMax = new Vector2(-60f, -140f);
            var content = UIFactory.ScrollColumn(area, 18f);

            var catalog = LevelCatalog.Load();
            var seen = 0;
            if (catalog != null)
            {
                foreach (var cutscene in catalog.AllCutscenes())
                {
                    if (!SaveSystem.Data.HasSeen(cutscene.id))
                        continue;
                    seen++;
                    var entry = cutscene;
                    var button = UIFactory.Button(content, Loc.T(entry.titleKey),
                        () => SceneFlow.PlayCutscene(entry, SceneFlow.ToMainMenu), UIFactory.Turquoise, 40);
                    UIFactory.Height(button, 100f);
                }
            }
            if (seen == 0)
                UIFactory.Height(UIFactory.Label(content, Loc.T("story.empty"), 40, UIFactory.Cream), 100f);

            var close = UIFactory.Button(window, Loc.T("ui.close"), () =>
            {
                storyBookOpen = false;
                Destroy(overlay.gameObject);
            }, UIFactory.Gold, 48);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 100f));
        }
    }
}
