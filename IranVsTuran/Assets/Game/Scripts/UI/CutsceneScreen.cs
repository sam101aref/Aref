using System;
using IranVsTuran.Art;
using IranVsTuran.Audio;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace IranVsTuran.UI
{
    /// <summary>
    /// A storybook cutscene: painted backdrop with a slow zoom, the speaker's portrait, and the
    /// line fading in. Tap to continue; Skip ends it.
    /// </summary>
    public class CutsceneScreen : ScreenBase
    {
        readonly CutsceneDef scene;
        readonly Action done;
        int index;
        bool finished;

        RectTransform stage;
        CanvasGroup textGroup;
        float shownAt;

        public CutsceneScreen(CutsceneDef scene, Action done)
        {
            this.scene = scene;
            this.done = done;
        }

        protected override void OnOpened()
        {
            AudioService.Mood = MusicMood.Calm;
        }

        protected override void Build()
        {
            var catcher = UIKit.Image(Rect, "Tap", Color.black);
            UIKit.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(Advance);

            stage = UIKit.Stretch(UIKit.Rect(Rect, "Stage"));
            var skip = UIKit.Button(Rect, Loc.T("story.skip"), Finish, new Color(0f, 0f, 0f, 0.55f), 36, Icon.Fast);
            UIKit.Place(skip.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(260f, 90f));
            ShowSlide();
        }

        void ShowSlide()
        {
            for (var i = stage.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(stage.GetChild(i).gameObject);

            var slide = scene.slides[index];
            var backdrop = UIKit.Image(stage, "Backdrop", Color.white);
            backdrop.sprite = ArtLibrary.Backdrop(slide.backdrop);
            backdrop.raycastTarget = false;
            UIKit.Stretch(backdrop.rectTransform);
            var fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            backdrop.gameObject.AddComponent<SlowZoom>();

            var look = StoryDefs.LookOf(slide.speaker);
            var isEnemy = slide.speaker != null && EnemyDefs.Get(slide.speaker) != null;
            if (look != null)
            {
                var portrait = UIKit.SpriteImage(stage, ArtLibrary.Character(look, 256), new Vector2(720f, 720f));
                var left = !isEnemy;
                portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = new Vector2(left ? 0f : 1f, 0f);
                portrait.rectTransform.pivot = new Vector2(0.5f, 0f);
                portrait.rectTransform.anchoredPosition = new Vector2(left ? 380f : -380f, 170f);
                if (!left)
                    portrait.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                portrait.gameObject.AddComponent<SlideIn>().From = new Vector2(left ? -300f : 300f, 0f);
            }

            var box = UIKit.Panel(stage, new Color(0.05f, 0.05f, 0.1f, 0.86f), "TextBox");
            box.raycastTarget = false;
            box.rectTransform.anchorMin = new Vector2(0f, 0f);
            box.rectTransform.anchorMax = new Vector2(1f, 0f);
            box.rectTransform.pivot = new Vector2(0.5f, 0f);
            box.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            box.rectTransform.sizeDelta = new Vector2(-60f, 300f);
            textGroup = box.gameObject.AddComponent<CanvasGroup>();
            textGroup.alpha = 0f;
            textGroup.blocksRaycasts = false;

            var nameKey = StoryDefs.NameKeyOf(slide.speaker);
            var speaker = UIKit.Label(box.transform, nameKey != null ? Loc.T(nameKey) : Loc.T("story.narrator"), 42, UIKit.Gold,
                TextAnchor.MiddleLeft, true);
            UIKit.Place(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(60f, -20f), new Vector2(900f, 60f));

            var text = UIKit.Paragraph(box.transform, Loc.Get(slide.textKey), 44, UIKit.Cream, 1560f, TextAnchor.UpperLeft, true);
            UIKit.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(60f, -95f), text.rectTransform.sizeDelta);

            var hint = UIKit.Label(box.transform, Loc.T("story.tap") + "  " + Loc.Number(index + 1) + "/" + Loc.Number(scene.slides.Count),
                28, new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerRight, false, false);
            UIKit.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 15f), new Vector2(600f, 40f));

            box.gameObject.AddComponent<FadeIn>().Group = textGroup;
            shownAt = Time.unscaledTime;
        }

        void Advance()
        {
            if (finished || Time.unscaledTime - shownAt < 0.25f)
                return;
            if (textGroup != null && textGroup.alpha < 0.95f)
            {
                textGroup.alpha = 1f;
                return;
            }
            AudioService.Play(Sfx.Click, 0.4f);
            index++;
            if (index >= scene.slides.Count)
                Finish();
            else
                ShowSlide();
        }

        void Finish()
        {
            if (finished)
                return;
            finished = true;
            if (done != null)
                done();
        }

        public override void OnBack()
        {
            Finish();
        }
    }

    /// <summary>Very slow zoom on a backdrop, the "Ken Burns" effect.</summary>
    public class SlowZoom : MonoBehaviour
    {
        float time;

        void Update()
        {
            time += Time.unscaledDeltaTime;
            var s = 1.02f + time * 0.012f;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    public class FadeIn : MonoBehaviour
    {
        public CanvasGroup Group;

        void Update()
        {
            if (Group != null && Group.alpha < 1f)
                Group.alpha = Mathf.Min(1f, Group.alpha + Time.unscaledDeltaTime * 1.6f);
        }
    }

    /// <summary>Slides a UI element in from an offset.</summary>
    public class SlideIn : MonoBehaviour
    {
        public Vector2 From;
        RectTransform rect;
        Vector2 target;
        float time;

        void Start()
        {
            rect = (RectTransform)transform;
            target = rect.anchoredPosition;
            rect.anchoredPosition = target + From;
        }

        void Update()
        {
            if (time >= 1f)
                return;
            time = Mathf.Min(1f, time + Time.unscaledDeltaTime * 2.5f);
            var t = 1f - (1f - time) * (1f - time);
            rect.anchoredPosition = Vector2.Lerp(target + From, target, t);
        }
    }
}
