using System;
using System.Collections;
using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Siavosh.Story
{
    /// <summary>
    /// Plays a <see cref="Cutscene"/> the way a storyteller tells it. In the coffeehouse the
    /// storyteller stands before his curtain and the scene is painted on it; inside a painting the
    /// scene fills the screen within a gold frame, drifting in slowly while figures fade in. Lines
    /// appear in a subtitle box with the speaker's portrait; tap to go on. When a story choice is
    /// answered unlike the Shahnameh, the picture freezes, the storyteller says "No, young one…"
    /// and the choice is asked again.
    /// </summary>
    public class CutscenePlayer : ScreenBase
    {
        public Cutscene Cutscene;
        public Action Finished;

        int index = -1;
        Beat scene;                 // the scene being shown
        bool revealed;
        float sceneTime;
        bool waiting;               // waiting for a tap
        bool finished;
        bool rewinding;
        ChoiceOption rewindOption;

        RectTransform stage;        // the painting's frame (curtain or full screen)
        RectTransform painting;     // the painting itself, which drifts
        RectTransform cloth;
        Image naqqal;
        RectTransform layer;        // subtitles, titles, choices
        Image sepia;

        protected override int SortingOrder { get { return 30; } }

        protected override void Start()
        {
            base.Start();
            Next();
        }

        void Update()
        {
            if (painting == null || scene == null)
                return;
            if (!rewinding)
                sceneTime += Time.deltaTime;
            var t = Mathf.Clamp01(sceneTime / 14f);
            var ease = t * t * (3f - 2f * t);
            var zoom = Mathf.Lerp(1f, scene.Zoom, ease);
            painting.localScale = new Vector3(zoom, zoom, 1f);
            painting.anchoredPosition = new Vector2(scene.Pan.x * stage.rect.width * ease, scene.Pan.y * stage.rect.height * ease);
            if (naqqal != null)
                naqqal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.3f) * 1.2f);
        }

        protected override void Build(RectTransform canvas)
        {
            var black = UIKit.Fill(canvas, "Black", Color.black);
            UIKit.Stretch(black.rectTransform);
            var tap = black.gameObject.AddComponent<Button>();
            black.raycastTarget = true;
            tap.targetGraphic = black;
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(OnTap);

            BuildScene(canvas);

            // Letterbox bars
            var top = UIKit.Fill(canvas, "Bar", Color.black);
            UIKit.PlaceFixed(top.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(4000f, 56f));
            var bottom = UIKit.Fill(canvas, "Bar", Color.black);
            UIKit.PlaceFixed(bottom.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(4000f, 56f));

            layer = UIKit.Rect(canvas, "Layer");
            UIKit.Stretch(layer);

            var skip = UIKit.Button(canvas, Ui.Skip, Finish, ButtonStyle.Dark, 30);
            UIKit.Place((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(-30f, -70f), new Vector2(220f, 76f));
            var title = UIKit.Label(canvas, Cutscene.Title, 28, Palette.Gold, TextAnchor.MiddleLeft, true, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -70f), new Vector2(1000f, 60f));

            if (index >= 0 && index < Cutscene.Beats.Count)
                ShowBeat(Cutscene.Beats[index], rebuilt: true);
        }

        void BuildScene(RectTransform canvas)
        {
            stage = null;
            painting = null;
            cloth = null;
            naqqal = null;
            sepia = null;
            if (scene == null)
                return;

            if (scene.Mode == StageMode.Coffeehouse)
            {
                var room = UIKit.Backdrop(canvas, "bg/coffeehouse");
                stage = MenuScreen.CurtainRect(room.rectTransform);
                painting = PaintingImage(stage, scene.Painting);
                AddActors(painting, scene);
                if (scene.Covered && !revealed)
                {
                    cloth = UIKit.Rect(stage, "Cloth");
                    UIKit.Stretch(cloth);
                    var c = cloth.gameObject.AddComponent<Image>();
                    c.raycastTarget = false;
                    c.sprite = Art.White;
                    c.color = Palette.Hex(0x1A0F0B);
                    var fringe = UIKit.Fill(cloth, "Fringe", Palette.Gold);
                    fringe.rectTransform.anchorMin = new Vector2(0f, 0f);
                    fringe.rectTransform.anchorMax = new Vector2(0f, 1f);
                    fringe.rectTransform.pivot = new Vector2(1f, 0.5f);
                    fringe.rectTransform.sizeDelta = new Vector2(14f, 0f);
                    fringe.rectTransform.anchoredPosition = Vector2.zero;
                }
                sepia = Veil(stage);
                naqqal = UIKit.Picture(room.rectTransform, "chars/naqqal", 10f);
                var rect = naqqal.rectTransform;
                rect.anchorMin = new Vector2(0.02f, 0.02f);
                rect.anchorMax = new Vector2(0.24f, 0.96f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                var frame = UIKit.Rect(canvas, "Frame");
                UIKit.Stretch(frame);
                frame.gameObject.AddComponent<RectMask2D>();
                stage = frame;
                painting = PaintingImage(frame, scene.Painting);
                AddActors(painting, scene);
                sepia = Veil(frame);
                var border = UIKit.Rect(canvas, "Border");
                UIKit.Stretch(border, 56f);
                UIKit.AddBorder(border, 4, Palette.Vermilion, 16, 0f);
                UIKit.AddBorder(border, 4, Palette.Gold, 4, 22f);
            }
        }

        static RectTransform PaintingImage(RectTransform parent, string name)
        {
            var image = UIKit.Rect(parent, "Painting").gameObject.AddComponent<Image>();
            image.sprite = Art.Get("bg/" + name);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            UIKit.Stretch(rect);
            var size = Art.Size("bg/" + name);
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = size.y > 0f ? size.x / size.y : 2.2f;
            return rect;
        }

        static Image Veil(RectTransform parent)
        {
            var veil = UIKit.Fill(parent, "Sepia", new Color(0.45f, 0.3f, 0.15f, 0f));
            UIKit.Stretch(veil.rectTransform);
            return veil;
        }

        void AddActors(RectTransform parent, Beat beat)
        {
            foreach (var actor in beat.Actors)
            {
                var name = "chars/" + actor.Figure;
                var info = Art.Info(name);
                var image = UIKit.Rect(parent, actor.Figure).gameObject.AddComponent<Image>();
                image.sprite = Art.Get(name);
                image.raycastTarget = false;
                image.preserveAspect = true;
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(actor.X, actor.Ground);
                if (info != null && info.w > 0f && info.h > 0f)
                    rect.pivot = new Vector2(0.5f - info.x / info.w, 0.5f - info.y / info.h);
                else
                    rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                // Height is a fraction of the painting; resolved once layout knows the size.
                StartCoroutine(SizeActor(rect, parent, actor, info));
            }
        }

        IEnumerator SizeActor(RectTransform rect, RectTransform parent, Actor actor, Art.SpriteInfo info)
        {
            var image = rect.GetComponent<Image>();
            UIKit.SetAlpha(image, 0f);
            yield return null;
            if (rect == null)
                yield break;
            var height = parent.rect.height * actor.Height;
            // Full figures are 4.1 units tall for a 3.9-unit person; scale by the figure's own height.
            var spriteHeight = info != null ? info.h : 4f;
            var h = height * spriteHeight / 4.1f;
            var w = info != null && info.h > 0f ? h * info.w / info.h : h * 0.4f;
            rect.sizeDelta = new Vector2(w, h);
            rect.localScale = new Vector3(actor.Flip ? -1f : 1f, 1f, 1f);
            for (var t = -actor.Delay; t < 0.6f; t += Time.deltaTime)
            {
                if (image == null)
                    yield break;
                UIKit.SetAlpha(image, Mathf.Clamp01(t / 0.6f));
                yield return null;
            }
            if (image != null)
                UIKit.SetAlpha(image, 1f);
        }

        // ------------------------------------------------------------- beats

        void OnTap()
        {
            if (finished || rewinding)
                return;
            if (waiting)
            {
                AudioService.Play(Sfx.Page, 0.35f);
                Next();
            }
        }

        void Next()
        {
            waiting = false;
            index++;
            while (index < Cutscene.Beats.Count)
            {
                var beat = Cutscene.Beats[index];
                if (beat.Kind == BeatKind.Scene)
                {
                    if (beat.Mode == StageMode.Overlay)
                    {
                        index++;
                        continue;
                    }
                    scene = beat;
                    revealed = false;
                    sceneTime = 0f;
                    Rebuild();
                    index++;
                    continue;
                }
                ShowBeat(beat, rebuilt: false);
                return;
            }
            Finish();
        }

        void ShowBeat(Beat beat, bool rebuilt)
        {
            if (layer == null)
                return;
            foreach (Transform child in layer)
                Destroy(child.gameObject);

            switch (beat.Kind)
            {
                case BeatKind.Reveal:
                    if (cloth != null && !rebuilt)
                        StartCoroutine(PullCloth());
                    else
                    {
                        revealed = true;
                        Next();
                    }
                    break;

                case BeatKind.Line:
                    var withPortrait = !(scene != null && scene.Mode == StageMode.Coffeehouse && beat.Speaker == Cast.Naqqal);
                    Subtitle(layer, beat.Speaker, beat.Text, withPortrait);
                    if (naqqal != null)
                        naqqal.color = beat.Speaker == Cast.Naqqal ? Color.white : new Color(0.75f, 0.72f, 0.68f);
                    waiting = true;
                    break;

                case BeatKind.Title:
                    TitleCard(beat);
                    waiting = true;
                    break;

                case BeatKind.Choice:
                    if (rewinding && rewindOption != null)
                        ShowRewind(rewindOption);
                    else
                        ShowChoice(beat);
                    break;
            }
        }

        IEnumerator PullCloth()
        {
            AudioService.Play(Sfx.Page, 0.8f);
            for (var t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                if (cloth == null)
                    yield break;
                var k = t / 1.2f;
                k = k * k * (3f - 2f * k);
                cloth.anchorMin = new Vector2(k, 0f);
                cloth.offsetMin = new Vector2(0f, 0f);
                yield return null;
            }
            if (cloth != null)
                cloth.gameObject.SetActive(false);
            revealed = true;
            Next();
        }

        /// <summary>The subtitle box used by cutscenes and by dialogue over a stage.</summary>
        public static RectTransform Subtitle(RectTransform parent, Speaker speaker, LocText text, bool withPortrait)
        {
            var box = UIKit.Panel(parent, "Subtitle", new Color(0.08f, 0.05f, 0.03f, 0.9f), 26, true);
            var rect = box.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 76f);
            var portrait = withPortrait && speaker != null && speaker.Figure != null;
            var width = 1500f;
            var textWidth = width - (portrait ? 260f : 100f);

            var label = UIKit.Label(box.transform, "", 40, Palette.Paper, TextAnchor.UpperLeft);
            label.lineSpacing = 1.2f;
            var lines = UIKit.WrapLines(label, text.Raw, textWidth);
            label.text = Loc.Display(string.Join("\n", lines));
            var height = Mathf.Max(200f, 110f + lines.Count * 54f);
            rect.sizeDelta = new Vector2(width, height);
            UIKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(portrait ? 220f : 50f, -76f), new Vector2(textWidth, height - 90f));

            if (speaker != null)
            {
                var name = UIKit.Label(box.transform, speaker.Name, 32, speaker.NameColor, TextAnchor.MiddleLeft, true);
                UIKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(portrait ? 220f : 50f, -18f), new Vector2(800f, 50f));
            }
            if (portrait)
            {
                var face = UIKit.Portrait(box.transform, speaker.Figure, 160f, Palette.Paper, Loc.IsRtl);
                UIKit.Place(face, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(160f, 160f));
            }
            var more = UIKit.Icon(box.transform, "duck", Palette.GoldLight, 34f);
            UIKit.Place(more.rectTransform, new Vector2(1f, 0f), new Vector2(-24f, 16f), new Vector2(34f, 34f));
            var fade = box.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f;
            box.gameObject.AddComponent<FadeIn>();
            return rect;
        }

        void TitleCard(Beat beat)
        {
            var veil = UIKit.Fill(layer, "Veil", new Color(0.05f, 0.04f, 0.1f, 0.55f));
            UIKit.Stretch(veil.rectTransform);
            var card = UIKit.Panel(layer, "Card", Palette.Paper, 20, true, true);
            UIKit.Centered(card.rectTransform, new Vector2(0f, 20f), new Vector2(1100f, 560f));
            var band = UIKit.Fill(card.transform, "Band", Palette.Lapis);
            UIKit.PlaceFixed(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(1076f, 90f));
            var star = UIKit.Icon(band.transform, "oath", Palette.GoldLight, 60f);
            star.rectTransform.anchoredPosition = Vector2.zero;
            var title = UIKit.Title(card.transform, beat.TitleKey, Loc.IsPersian ? 230f : 120f, Palette.Vermilion);
            title.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            if (beat.Subtitle != null)
            {
                var sub = UIKit.Label(card.transform, beat.Subtitle, 38, Palette.Ink, TextAnchor.MiddleCenter, true);
                UIKit.PlaceFixed(sub.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1000f, 60f));
            }
            var tap = UIKit.Label(card.transform, Ui.TapToContinue, 26, Palette.EarthDark);
            UIKit.PlaceFixed(tap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1000f, 40f));
            card.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            card.gameObject.AddComponent<FadeIn>();
            AudioService.Play(Sfx.Leaf, 0.6f);
        }

        void ShowChoice(Beat beat)
        {
            var veil = UIKit.Fill(layer, "Veil", new Color(0.05f, 0.04f, 0.1f, 0.5f));
            UIKit.Stretch(veil.rectTransform);
            veil.raycastTarget = true;
            var prompt = UIKit.Label(layer, beat.Prompt, 44, Palette.GoldLight, TextAnchor.MiddleCenter, true, true);
            UIKit.Centered(prompt.rectTransform, new Vector2(0f, 220f), new Vector2(1400f, 70f));
            for (var i = 0; i < beat.Options.Count; i++)
            {
                var option = beat.Options[i];
                var b = UIKit.Button(layer, option.Text, () => Choose(option), ButtonStyle.Secondary, 36);
                UIKit.Centered((RectTransform)b.transform, new Vector2(0f, 80f - i * 150f), new Vector2(1300f, 120f));
            }
        }

        void Choose(ChoiceOption option)
        {
            if (option.Faithful)
            {
                if (option.Honor != 0)
                {
                    SaveSystem.Data.honor += option.Honor;
                    SaveSystem.Save();
                    AudioService.Play(Sfx.Honor, 0.7f);
                }
                Next();
                return;
            }
            rewinding = true;
            rewindOption = option;
            ShowRewind(option);
        }

        /// <summary>The storyteller stops the story: the picture yellows, and he sets it right.</summary>
        void ShowRewind(ChoiceOption option)
        {
            foreach (Transform child in layer)
                Destroy(child.gameObject);
            AudioService.Play(Sfx.Defeat, 0.6f);
            if (sepia != null)
                sepia.color = new Color(0.45f, 0.3f, 0.15f, 0.55f);
            if (painting != null)
                foreach (var image in painting.GetComponentsInChildren<Image>())
                    image.color = new Color(0.85f, 0.72f, 0.55f, image.color.a);

            var veil = UIKit.Fill(layer, "Veil", new Color(0.1f, 0.06f, 0.02f, 0.45f));
            UIKit.Stretch(veil.rectTransform);
            veil.raycastTarget = true;

            var chose = UIKit.Label(layer, Loc.Display(Ui.YouChose.Raw + " " + option.Text.Raw), 30, Palette.Rose, TextAnchor.MiddleCenter, true, true);
            UIKit.Centered(chose.rectTransform, new Vector2(0f, 380f), new Vector2(1600f, 50f));
            var strike = UIKit.Fill(chose.transform, "Strike", Palette.Rose);
            UIKit.Centered(strike.rectTransform, Vector2.zero, new Vector2(Mathf.Min(1500f, UIKit.Measure(chose, chose.text) + 20f), 4f));

            var spinner = UIKit.Icon(layer, "dodge", Palette.GoldLight, 140f);
            spinner.rectTransform.anchoredPosition = new Vector2(Loc.IsRtl ? 560f : -560f, 120f);
            spinner.gameObject.AddComponent<Spin>();

            var no = UIKit.Title(layer, "no", Loc.IsPersian ? 170f : 90f, Palette.GoldLight);
            no.rectTransform.anchoredPosition = new Vector2(0f, 200f);

            var naqqalFace = UIKit.Picture(layer, "chars/naqqal", 520f);
            UIKit.Place(naqqalFace.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 300f), naqqalFace.rectTransform.sizeDelta);
            naqqalFace.rectTransform.localScale = new Vector3(Loc.IsRtl ? 1f : -1f, 1f, 1f);

            Subtitle(layer, Cast.Naqqal, option.Rewind ?? new LocText("سیاوش چنین نکرد.", "Siavosh did no such thing."), false);
            var back = UIKit.Button(layer, Ui.BackToChoice, Unwind, ButtonStyle.Primary, 34);
            UIKit.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-60f, 330f), new Vector2(420f, 100f));
        }

        void Unwind()
        {
            rewinding = false;
            rewindOption = null;
            if (sepia != null)
                sepia.color = new Color(0.45f, 0.3f, 0.15f, 0f);
            if (painting != null)
                foreach (var image in painting.GetComponentsInChildren<Image>())
                    image.color = new Color(1f, 1f, 1f, image.color.a);
            ShowBeat(Cutscene.Beats[index], rebuilt: false);
        }

        void Finish()
        {
            if (finished)
                return;
            finished = true;
            if (Finished != null)
                Finished();
        }

        public override void OnBack()
        {
            Finish();
        }
    }

    /// <summary>Fades a CanvasGroup in.</summary>
    public class FadeIn : MonoBehaviour
    {
        CanvasGroup group;
        float t;

        void Start()
        {
            group = GetComponent<CanvasGroup>();
        }

        void Update()
        {
            if (group == null)
                return;
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / 0.35f);
            if (t > 0.35f)
                Destroy(this);
        }
    }

    public class Spin : MonoBehaviour
    {
        void Update()
        {
            transform.Rotate(0f, 0f, Time.unscaledDeltaTime * 200f);
        }
    }
}
