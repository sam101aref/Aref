using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.Play;
using Siavosh.Story;
using UnityEngine;

namespace Siavosh.UI
{
    /// <summary>The story book: watch again any cutscene already seen, and count the leaves found.</summary>
    public class StoryBookScreen : ScreenBase
    {
        protected override void Build(RectTransform canvas)
        {
            var save = SaveSystem.Data;
            UIKit.Backdrop(canvas, "bg/palace");
            var dim = UIKit.Fill(canvas, "Dim", Palette.WithAlpha(Palette.LapisNight, 0.45f));
            UIKit.Stretch(dim.rectTransform);
            BackButton(canvas, Game.ShowMenu);

            var book = UIKit.Panel(canvas, "Book", Palette.Paper, 30, true, true);
            UIKit.Centered(book.rectTransform, new Vector2(0f, -20f), new Vector2(1400f, 900f));
            var title = UIKit.Label(book.transform, Ui.StoryBookTitle, 54, Palette.Lapis, TextAnchor.MiddleCenter, true);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(900f, 80f));
            var total = 0;
            foreach (var chapter in Catalog.Chapters)
                total += chapter.Stages.Count * 3;
            var leaves = UIKit.Label(book.transform, Ui.LeavesFound.Format(save.TotalLeaves, total), 32, Palette.EarthDark);
            UIKit.PlaceFixed(leaves.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 50f));

            for (var i = 0; i < Script.Book.Length; i++)
            {
                var cutscene = Script.Book[i];
                var seen = save.HasSeen(cutscene.Id);
                var row = UIKit.Panel(book.transform, "Entry", seen ? Palette.White : Palette.PaperDark, 20, true);
                UIKit.PlaceFixed(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f - i * 150f), new Vector2(1200f, 130f));
                var portrait = UIKit.Portrait(row.transform, FigureFor(cutscene), 100f, Palette.Lapis);
                UIKit.Place(portrait, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(100f, 100f));
                var name = UIKit.Label(row.transform, seen ? cutscene.Title.ToString() : Ui.NotSeenYet.ToString(), 38, seen ? Palette.Ink : Palette.EarthDark, TextAnchor.MiddleLeft, true);
                UIKit.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(760f, 60f));
                if (seen)
                {
                    var c = cutscene;
                    var play = UIKit.IconButton(row.transform, "play", () => Game.PlayCutscene(c, Game.ShowStoryBook), 90f, Palette.Vermilion, Palette.White);
                    UIKit.Place((RectTransform)play.transform, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(90f, 90f));
                }
            }
        }

        static string FigureFor(Cutscene cutscene)
        {
            foreach (var beat in cutscene.Beats)
                if (beat.Kind == BeatKind.Line && beat.Speaker != null && beat.Speaker.Figure != null && beat.Speaker != Cast.Naqqal)
                    return beat.Speaker.Figure;
            return "naqqal";
        }
    }

    /// <summary>After a won stage: what Siavosh earned, then onward.</summary>
    public class ResultScreen : ScreenBase
    {
        public StageResult Result;

        protected override void Start()
        {
            base.Start();
            AudioService.Play(Sfx.Victory, 0.9f);
        }

        protected override void Build(RectTransform canvas)
        {
            var r = Result;
            var save = SaveSystem.Data;
            UIKit.Backdrop(canvas, "bg/" + r.Stage.Backdrop);
            var dim = UIKit.Fill(canvas, "Dim", Palette.WithAlpha(Palette.LapisNight, 0.5f));
            UIKit.Stretch(dim.rectTransform);

            var figure = UIKit.Picture(canvas, "chars/siavosh", 760f);
            UIKit.Place(figure.rectTransform, new Vector2(0f, 0f), new Vector2(180f, -20f), figure.rectTransform.sizeDelta);
            if (Loc.IsRtl)
                figure.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            var rostam = UIKit.Picture(canvas, "chars/rostam", 780f);
            UIKit.Place(rostam.rectTransform, new Vector2(0f, 0f), new Vector2(-40f, -20f), rostam.rectTransform.sizeDelta);
            if (Loc.IsRtl)
                rostam.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            rostam.transform.SetSiblingIndex(figure.transform.GetSiblingIndex());

            var panel = UIKit.Panel(canvas, "Result", Palette.Paper, 26, true, true);
            UIKit.Place(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-80f, 0f), new Vector2(900f, 860f));

            var where = UIKit.Label(panel.transform, Loc.Display(Ui.ChapterN.RawFormat(r.Stage.Chapter) + " · " + Ui.Stage.RawFormat(r.Stage.Number)), 30, Palette.EarthDark);
            UIKit.PlaceFixed(where.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(800f, 44f));
            var name = UIKit.Label(panel.transform, r.Stage.Name, 56, Palette.Lapis, TextAnchor.MiddleCenter, true);
            UIKit.PlaceFixed(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(820f, 80f));
            var done = UIKit.Label(panel.transform, Ui.StageComplete, 32, Palette.Vermilion, TextAnchor.MiddleCenter, true);
            UIKit.PlaceFixed(done.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(820f, 50f));

            var y = -250f;
            int from, to;
            Rules.LevelRange(save.Level, out from, out to);
            Row(panel.rectTransform, Ui.Xp, "+" + Loc.Number(r.Xp), Palette.Lapis, ref y);
            var bar = UIKit.Bar(panel.transform, new Vector2(520f, 26f), Palette.Lapis, Palette.PaperDark);
            UIKit.Place(bar.transform.parent as RectTransform, new Vector2(1f, 1f), new Vector2(-60f, y + 36f), new Vector2(380f, 26f));
            bar.fillAmount = to > from ? Mathf.Clamp01((save.xp - from) / (float)(to - from)) : 1f;
            Row(panel.rectTransform, Ui.DinarsLabel, "+" + Loc.Number(r.Dinars), Palette.GoldDark, ref y);

            var leaves = UIKit.Label(panel.transform, Ui.LeavesLabel, 34, Palette.Ink, TextAnchor.MiddleLeft, true);
            UIKit.Place(leaves.rectTransform, new Vector2(0f, 1f), new Vector2(60f, y), new Vector2(260f, 60f));
            for (var i = 0; i < 3; i++)
            {
                var found = (r.LeavesMask & (1 << i)) != 0;
                var leaf = UIKit.Picture(panel.transform, "props/leaf", 70f, found ? Color.white : new Color(0.5f, 0.45f, 0.35f, 0.35f));
                UIKit.Place(leaf.rectTransform, new Vector2(1f, 1f), new Vector2(-60f - i * 70f, y + 6f), leaf.rectTransform.sizeDelta);
            }
            y -= 90f;

            if (r.Honor != 0)
            {
                Row(panel.rectTransform, Ui.HonorLabel, (r.Honor > 0 ? "+" : "−") + Loc.Number(Mathf.Abs(r.Honor)), r.Honor > 0 ? Palette.Turquoise : Palette.Vermilion, ref y);
                if (r.HonorNote != null)
                {
                    var note = UIKit.Wrapped(panel.transform, r.HonorNote.Raw, 26, Palette.EarthDark, 760f, TextAnchor.UpperLeft);
                    UIKit.Place(note.rectTransform, new Vector2(0f, 1f), new Vector2(60f, y + 20f), new Vector2(780f, 60f));
                    y -= 50f;
                }
            }

            var news = new System.Collections.Generic.List<string>();
            if (r.LevelUp)
                news.Add(Ui.LevelUp.Raw);
            if (r.Stage.GrantsSkill != null)
                news.Add(Ui.NewSkill.RawFormat(Rules.Skill(r.Stage.GrantsSkill).Name.Raw));
            if (r.Stage.GrantsFarr != null)
                news.Add(Ui.NewFarr.RawFormat(Rules.Farr.Find(f => f.Id == r.Stage.GrantsFarr).Name.Raw));
            foreach (var line in news)
            {
                var l = UIKit.Label(panel.transform, Loc.Display(line), 30, Palette.Vermilion, TextAnchor.MiddleCenter, true);
                UIKit.PlaceFixed(l.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(820f, 50f));
                y -= 52f;
            }

            var replay = UIKit.Button(panel.transform, Ui.Replay, () => Game.PlayStage(r.Stage), ButtonStyle.Secondary, 34);
            UIKit.PlaceFixed((RectTransform)replay.transform, new Vector2(0.5f, 0f), new Vector2(-280f, 40f), new Vector2(240f, 100f));
            var camp = UIKit.Button(panel.transform, Ui.ToCamp, () => Game.ShowCamp(0), ButtonStyle.Secondary, 34);
            UIKit.PlaceFixed((RectTransform)camp.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(240f, 100f));
            var next = UIKit.Button(panel.transform, Ui.Next, () => Game.AfterResult(r.Stage), ButtonStyle.Primary, 38);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(280f, 40f), new Vector2(260f, 100f));
        }

        static void Row(RectTransform panel, LocText label, string value, Color color, ref float y)
        {
            var l = UIKit.Label(panel, label, 34, Palette.Ink, TextAnchor.MiddleLeft, true);
            UIKit.Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(60f, y), new Vector2(300f, 60f));
            var v = UIKit.Label(panel, value, 40, color, TextAnchor.MiddleLeft, true);
            UIKit.Place(v.rectTransform, new Vector2(0f, 1f), new Vector2(340f, y), new Vector2(200f, 60f));
            y -= 90f;
        }

        public override void OnBack()
        {
            Game.AfterResult(Result.Stage);
        }
    }
}
