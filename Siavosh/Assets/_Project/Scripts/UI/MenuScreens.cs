using System;
using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.Play;
using Siavosh.Story;
using UnityEngine;
using UnityEngine.UI;

namespace Siavosh.UI
{
    /// <summary>The very first screen: Persian or English. Both names are shown in their own script.</summary>
    public class LanguageScreen : ScreenBase
    {
        protected override void Build(RectTransform canvas)
        {
            var bg = UIKit.Fill(canvas, "Lapis", Palette.LapisDark);
            UIKit.Stretch(bg.rectTransform);
            StarField(canvas);

            var frame = UIKit.Panel(canvas, "Frame", Palette.WithAlpha(Palette.LapisNight, 0f), 40, true, true);
            UIKit.Stretch(frame.rectTransform, 30f);

            var medallion = UIKit.Rect(canvas, "Medallion");
            UIKit.Centered(medallion, new Vector2(0f, 60f), new Vector2(1000f, 640f));
            var disc = medallion.gameObject.AddComponent<Image>();
            disc.sprite = Art.Circle;
            disc.color = Palette.Lapis;
            UIKit.RingImage(medallion, Palette.Gold);

            var logoFa = UIKit.Picture(canvas, "titles/logo_fa", 190f, Palette.GoldLight);
            logoFa.rectTransform.anchoredPosition = new Vector2(0f, 250f);
            var logoEn = UIKit.Picture(canvas, "titles/logo_en", 60f, Palette.Paper);
            logoEn.rectTransform.anchoredPosition = new Vector2(0f, 120f);

            var device = Loc.DetectDeviceLanguage();
            var fa = UIKit.Button(canvas, "فارسی", () => Choose(Language.Persian), device == Language.Persian ? ButtonStyle.Primary : ButtonStyle.Secondary, 58);
            UIKit.Centered((RectTransform)fa.transform, new Vector2(-250f, -90f), new Vector2(420f, 140f));
            fa.GetComponentInChildren<Text>().text = Loc.Display("فارسی");
            var en = UIKit.Button(canvas, "English", () => Choose(Language.English), device == Language.English ? ButtonStyle.Primary : ButtonStyle.Secondary, 58);
            UIKit.Centered((RectTransform)en.transform, new Vector2(250f, -90f), new Vector2(420f, 140f));

            var hint = UIKit.Label(canvas, Loc.Display("زبان را انتخاب کنید") + "   ·   Choose your language", 38, Palette.Paper);
            UIKit.Centered(hint.rectTransform, new Vector2(0f, -330f), new Vector2(1400f, 60f));
        }

        static void StarField(RectTransform canvas)
        {
            var rnd = new System.Random(5);
            for (var i = 0; i < 70; i++)
            {
                var star = UIKit.Fill(canvas, "Star", Palette.WithAlpha(Palette.Gold, 0.35f));
                star.sprite = Art.Circle;
                star.type = Image.Type.Simple;
                var x = (float)rnd.NextDouble();
                var y = (float)rnd.NextDouble();
                var s = 6f + (float)rnd.NextDouble() * 10f;
                UIKit.PlaceFixed(star.rectTransform, new Vector2(x, y), Vector2.zero, new Vector2(s, s));
            }
        }

        void Choose(Language language)
        {
            Settings.Language = language;
            Game.ShowMenu();
        }

        public override void OnBack()
        {
        }
    }

    /// <summary>The storyteller's coffeehouse: the title on the curtain and the main choices.</summary>
    public class MenuScreen : ScreenBase
    {
        protected override void Build(RectTransform canvas)
        {
            var save = SaveSystem.Data;
            var bg = UIKit.Backdrop(canvas, "bg/coffeehouse");

            // The painting on the curtain (the coffeehouse curtain spans x 470–1450, y 130–690 of 1920x864).
            var curtain = CurtainRect(bg.rectTransform);
            var painting = UIKit.Picture(curtain, "bg/zabul", 10f);
            UIKit.Stretch(painting.rectTransform);
            painting.preserveAspect = false;
            var tint = UIKit.Fill(curtain, "Tint", Palette.WithAlpha(Palette.LapisDark, 0.35f));
            UIKit.Stretch(tint.rectTransform);

            var cartouche = UIKit.Panel(curtain, "Cartouche", Palette.Paper, 60, true, true);
            cartouche.rectTransform.anchorMin = cartouche.rectTransform.anchorMax = cartouche.rectTransform.pivot = new Vector2(0.5f, 0.62f);
            cartouche.rectTransform.sizeDelta = new Vector2(620f, 190f);
            cartouche.rectTransform.anchoredPosition = Vector2.zero;
            var logo = UIKit.Title(cartouche.transform, "logo", Loc.IsPersian ? 150f : 62f, Palette.Lapis);
            logo.rectTransform.anchoredPosition = new Vector2(0f, Loc.IsPersian ? 6f : 0f);
            var tag = UIKit.Label(curtain, Ui.Tagline, 30, Palette.Paper, TextAnchor.MiddleCenter, true, true);
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = tag.rectTransform.pivot = new Vector2(0.5f, 0.28f);
            tag.rectTransform.sizeDelta = new Vector2(900f, 60f);
            tag.rectTransform.anchoredPosition = Vector2.zero;

            var naqqal = UIKit.Picture(canvas, "chars/naqqal", 760f);
            // English: menu on the left, storyteller on the right; Persian mirrors it.
            UIKit.Place(naqqal.rectTransform, new Vector2(1f, 0f), new Vector2(-10f, -10f), naqqal.rectTransform.sizeDelta);
            if (Loc.IsRtl)
                naqqal.rectTransform.localScale = Vector3.one;
            else
                naqqal.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            // Menu panel
            var panel = UIKit.Panel(canvas, "Menu", Palette.Hex(0x2B1B12, 0.9f), 28, true);
            UIKit.Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, -20f), new Vector2(520f, 700f));
            var next = Catalog.NextStage(save);
            var started = save.stages.Count > 0 || save.HasSeen(Script.Prologue.Id);
            var cont = UIKit.Button(panel.transform, started ? Ui.Continue : Ui.Begin, Game.Continue, ButtonStyle.Primary, 44);
            UIKit.PlaceFixed((RectTransform)cont.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(460f, 150f));
            var sub = next != null ? Ui.NextUp.Format(next.Chapter, next.Number, next.Name.Raw) : Ui.AllDone.ToString();
            var subLabel = UIKit.Label(cont.transform, sub, 24, Palette.Paper, TextAnchor.MiddleCenter);
            UIKit.PlaceFixed(subLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(440f, 40f));
            ((RectTransform)cont.transform.Find("Label")).offsetMin = new Vector2(0f, 40f);

            var items = new[]
            {
                new Tuple<LocText, Action>(Ui.Chapters, Game.ShowMap),
                new Tuple<LocText, Action>(Ui.Camp, () => Game.ShowCamp(0)),
                new Tuple<LocText, Action>(Ui.StoryBook, Game.ShowStoryBook),
                new Tuple<LocText, Action>(Ui.Settings, OpenSettings),
            };
            for (var i = 0; i < items.Length; i++)
            {
                var b = UIKit.Button(panel.transform, items[i].Item1, items[i].Item2, ButtonStyle.Secondary, 38);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0f, -210f - i * 116f), new Vector2(440f, 100f));
            }

            Chip(canvas, Ui.Level.Format(save.Level), null, Palette.Lapis, new Vector2(1f, 1f), new Vector2(-50f, -30f), 220f);
            Chip(canvas, Loc.Number(save.dinars), "props/coin", Palette.Hex(0x2B1B12, 0.9f), new Vector2(1f, 1f), new Vector2(-290f, -30f), 220f);
        }

        /// <summary>The curtain area of the coffeehouse picture (x 470–1450, y 130–690 of 1920x864), masked.</summary>
        public static RectTransform CurtainRect(RectTransform coffeehouse)
        {
            var curtain = UIKit.Region(coffeehouse, "Curtain", 470f / 1920f, 1f - 690f / 864f, 1450f / 1920f, 1f - 130f / 864f);
            curtain.gameObject.AddComponent<RectMask2D>();
            return curtain;
        }

        public override void OnBack()
        {
            Application.Quit();
        }
    }

    /// <summary>The settings panel: language, sound, music, vibration, difficulty, reset.</summary>
    public static class SettingsPanel
    {
        public static void Open(RectTransform canvas, Action onClose)
        {
            var overlay = UIKit.Overlay(canvas, Palette.Shade);
            var panel = UIKit.Panel(overlay.transform, "Settings", Palette.Paper, 32, true, true);
            UIKit.Centered(panel.rectTransform, Vector2.zero, new Vector2(1100f, 860f));
            var title = UIKit.Label(panel.transform, Ui.Settings, 54, Palette.Lapis, TextAnchor.MiddleCenter, true);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 80f));

            Action close = () =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                if (onClose != null)
                    onClose();
            };

            var y = -150f;
            Row(panel.rectTransform, Ui.Language, ref y,
                new Tuple<string, bool, Action>("فارسی", Loc.IsPersian, () => Settings.Language = Language.Persian),
                new Tuple<string, bool, Action>("English", !Loc.IsPersian, () => Settings.Language = Language.English));
            Row(panel.rectTransform, Ui.Sound, ref y,
                new Tuple<string, bool, Action>(Ui.On.Raw, Settings.Sound, () => { Settings.Sound = true; Reopen(canvas, close, onClose); }),
                new Tuple<string, bool, Action>(Ui.Off.Raw, !Settings.Sound, () => { Settings.Sound = false; Reopen(canvas, close, onClose); }));
            Row(panel.rectTransform, Ui.Music, ref y,
                new Tuple<string, bool, Action>(Ui.On.Raw, Settings.Music, () => { Settings.Music = true; Reopen(canvas, close, onClose); }),
                new Tuple<string, bool, Action>(Ui.Off.Raw, !Settings.Music, () => { Settings.Music = false; Reopen(canvas, close, onClose); }));
            Row(panel.rectTransform, Ui.Vibration, ref y,
                new Tuple<string, bool, Action>(Ui.On.Raw, Settings.Vibration, () => { Settings.Vibration = true; Reopen(canvas, close, onClose); }),
                new Tuple<string, bool, Action>(Ui.Off.Raw, !Settings.Vibration, () => { Settings.Vibration = false; Reopen(canvas, close, onClose); }));
            Row(panel.rectTransform, Ui.Difficulty, ref y,
                new Tuple<string, bool, Action>(Ui.Easy.Raw, Settings.Difficulty == Difficulty.Easy, () => { Settings.Difficulty = Difficulty.Easy; Reopen(canvas, close, onClose); }),
                new Tuple<string, bool, Action>(Ui.Normal.Raw, Settings.Difficulty == Difficulty.Normal, () => { Settings.Difficulty = Difficulty.Normal; Reopen(canvas, close, onClose); }),
                new Tuple<string, bool, Action>(Ui.Hard.Raw, Settings.Difficulty == Difficulty.Hard, () => { Settings.Difficulty = Difficulty.Hard; Reopen(canvas, close, onClose); }));

            var reset = UIKit.Button(panel.transform, Ui.ResetProgress, () => Confirm(canvas, Ui.ResetConfirm, () =>
            {
                SaveSystem.ResetProgress();
                close();
                Game.ShowMenu();
            }), ButtonStyle.Dark, 30);
            UIKit.PlaceFixed((RectTransform)reset.transform, new Vector2(0.5f, 0f), new Vector2(-230f, 40f), new Vector2(380f, 90f));
            var done = UIKit.Button(panel.transform, Ui.Close, close, ButtonStyle.Primary, 36);
            UIKit.PlaceFixed((RectTransform)done.transform, new Vector2(0.5f, 0f), new Vector2(230f, 40f), new Vector2(380f, 90f));
        }

        static void Reopen(RectTransform canvas, Action close, Action onClose)
        {
            close();
            Open(canvas, onClose);
        }

        static void Row(RectTransform panel, LocText label, ref float y, params Tuple<string, bool, Action>[] options)
        {
            var text = UIKit.Label(panel, label, 38, Palette.Ink, TextAnchor.MiddleLeft, true);
            UIKit.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(70f, y), new Vector2(280f, 80f));
            var width = 600f / options.Length;
            for (var i = 0; i < options.Length; i++)
            {
                var o = options[i];
                var b = UIKit.Button(panel, Loc.Display(o.Item1), o.Item3, o.Item2 ? ButtonStyle.Lapis : ButtonStyle.Secondary, 32);
                var index = Loc.IsRtl ? options.Length - 1 - i : i;
                UIKit.Place((RectTransform)b.transform, new Vector2(1f, 1f), new Vector2(-70f - (options.Length - 1 - index) * (width + 10f), y), new Vector2(width, 84f));
            }
            y -= 108f;
        }

        public static void Confirm(RectTransform canvas, LocText question, Action yes)
        {
            var overlay = UIKit.Overlay(canvas, Palette.Shade);
            var panel = UIKit.Panel(overlay.transform, "Confirm", Palette.Paper, 28, true, true);
            UIKit.Centered(panel.rectTransform, Vector2.zero, new Vector2(900f, 420f));
            var q = UIKit.Wrapped(panel.transform, question.Raw, 38, Palette.Ink, 760f, TextAnchor.MiddleCenter);
            UIKit.PlaceFixed(q.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(780f, 180f));
            var no = UIKit.Button(panel.transform, Ui.No, () => UnityEngine.Object.Destroy(overlay.gameObject), ButtonStyle.Secondary, 36);
            UIKit.PlaceFixed((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(-190f, 40f), new Vector2(320f, 96f));
            var ok = UIKit.Button(panel.transform, Ui.Yes, () =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                yes();
            }, ButtonStyle.Primary, 36);
            UIKit.PlaceFixed((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(190f, 40f), new Vector2(320f, 96f));
        }
    }
}
