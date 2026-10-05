using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.Play;
using UnityEngine;
using UnityEngine.UI;

namespace Siavosh.UI
{
    /// <summary>
    /// The story curtain: the storyteller's painted cloth showing Siavosh's road from Zabulistan to
    /// Siavoshgerd, with a medallion for each chapter. Choosing a chapter lists its stages below.
    /// </summary>
    public class MapScreen : ScreenBase
    {
        // Medallion positions on the painting (fractions, origin bottom-left).
        static readonly Vector2[] Spots =
        {
            new Vector2(0.04f, 0.80f),  // prologue
            new Vector2(0.094f, 0.537f),
            new Vector2(0.297f, 0.611f),
            new Vector2(0.49f, 0.502f),
            new Vector2(0.471f, 0.125f),
            new Vector2(0.771f, 0.602f),
            new Vector2(0.858f, 0.431f),
            new Vector2(0.885f, 0.199f),
            new Vector2(0.95f, 0.80f),  // epilogue
        };

        int selectedChapter = -1;
        StageDef selectedStage;
        Image panel;

        protected override void Build(RectTransform canvas)
        {
            var save = SaveSystem.Data;
            if (selectedChapter < 0)
            {
                var next = Catalog.NextStage(save);
                selectedChapter = next != null ? next.Chapter : 1;
                selectedStage = next ?? Catalog.Chapters[1].Stages[0];
            }

            var map = UIKit.Backdrop(canvas, "bg/pardeh_map");
            for (var i = 0; i < Catalog.Chapters.Count && i < Spots.Length; i++)
                Medallion(map.rectTransform, Catalog.Chapters[i], Spots[i], save);

            BackButton(canvas, Game.ShowMenu);
            var title = UIKit.Panel(canvas, "Title", Palette.Hex(0x2B1B12, 0.9f), 34, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -42f), new Vector2(460f, 72f));
            var t = UIKit.Label(title.transform, Ui.MapTitle, 36, Palette.GoldLight, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(t.rectTransform);
            Chip(canvas, Ui.Level.Format(save.Level), null, Palette.Lapis, new Vector2(1f, 1f), new Vector2(-40f, -42f), 220f);
            Chip(canvas, Loc.Number(save.dinars), "props/coin", Palette.Hex(0x2B1B12, 0.9f), new Vector2(1f, 1f), new Vector2(-280f, -42f), 220f);
            Chip(canvas, Loc.Number(save.TotalLeaves), "props/leaf", Palette.Hex(0x2B1B12, 0.9f), new Vector2(1f, 1f), new Vector2(-520f, -42f), 200f);

            BuildPanel(canvas);
        }

        void Medallion(RectTransform map, ChapterDef chapter, Vector2 spot, SaveData save)
        {
            var available = chapter.Available;
            var done = available && chapter.Stages.TrueForAll(s => save.IsDone(s.Id));
            var current = available && !done;
            var color = chapter.Number == 0 ? Palette.Gold : done ? Palette.Turquoise : current ? Palette.Vermilion : Palette.Locked;
            var size = chapter.Number == selectedChapter ? 120f : 96f;

            var holder = UIKit.Region(map, "Chapter " + chapter.Number, spot.x, spot.y, spot.x, spot.y);
            holder.sizeDelta = new Vector2(size, size);
            var button = UIKit.IconButton(holder, chapter.Number == 0 ? "book" : available ? (done ? "check" : "play") : "lock",
                () => SelectChapter(chapter), size, color, chapter.Number == 0 || available ? Palette.White : Palette.EarthDark);
            UIKit.Stretch((RectTransform)button.transform);
            if (chapter.Number == selectedChapter)
            {
                var halo = UIKit.RingImage(holder, Palette.GoldLight, -14f);
                halo.color = Palette.WithAlpha(Palette.GoldLight, 0.8f);
            }
            if (chapter.Number > 0)
            {
                var n = UIKit.Label(holder, Loc.Number(chapter.Number), 30, Palette.Ink, TextAnchor.MiddleCenter, true);
                var badge = UIKit.Fill(holder, "Badge", Palette.Paper);
                badge.sprite = Art.Circle;
                badge.type = Image.Type.Simple;
                UIKit.PlaceFixed(badge.rectTransform, new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(46f, 46f));
                n.transform.SetParent(badge.transform, false);
                UIKit.Stretch(n.rectTransform);
            }
        }

        void SelectChapter(ChapterDef chapter)
        {
            if (chapter.Number == 0)
            {
                Game.PlayCutscene(Story.Script.Prologue, Game.ShowMap);
                return;
            }
            selectedChapter = chapter.Number;
            if (chapter.Available)
            {
                var save = SaveSystem.Data;
                selectedStage = chapter.Stages.Find(s => !save.IsDone(s.Id)) ?? chapter.Stages[0];
            }
            else
            {
                selectedStage = null;
            }
            Rebuild();
        }

        void BuildPanel(RectTransform canvas)
        {
            var save = SaveSystem.Data;
            var chapter = Catalog.Chapters.Find(c => c.Number == selectedChapter);
            if (chapter == null)
                return;
            panel = UIKit.Panel(canvas, "Chapter", Palette.Hex(0x2B1B12, 0.92f), 30, true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1500f, 230f));

            // Join the raw (logical) pieces and shape once, so Persian reads right to left as a whole.
            var heading = Loc.Display(Ui.ChapterN.RawFormat(chapter.Number) + "  ·  " + chapter.Name.Raw + "  ·  " + chapter.Place.Raw);
            var name = UIKit.Label(panel.transform, heading, 34, Palette.GoldLight, TextAnchor.MiddleLeft, true);
            UIKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -22f), new Vector2(1100f, 56f));

            if (!chapter.Available)
            {
                var soon = UIKit.Wrapped(panel.transform, Ui.ChapterLocked.Raw, 32, Palette.Paper, 1300f, TextAnchor.MiddleLeft);
                UIKit.Place(soon.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(1400f, 110f));
                return;
            }

            for (var i = 0; i < chapter.Stages.Count; i++)
            {
                var stage = chapter.Stages[i];
                var unlocked = Catalog.IsUnlocked(stage, save);
                var done = save.IsDone(stage.Id);
                var selected = stage == selectedStage;
                var fill = done ? Palette.Turquoise : unlocked ? Palette.Vermilion : Palette.Smoke;
                var b = UIKit.Button(panel.transform, stage.Boss ? Ui.Boss.ToString() : Loc.Number(stage.Number), () =>
                {
                    if (!Catalog.IsUnlocked(stage, SaveSystem.Data))
                        return;
                    selectedStage = stage;
                    Rebuild();
                }, ButtonStyle.Dark, 36);
                ((Image)b.targetGraphic).color = fill;
                var rect = (RectTransform)b.transform;
                UIKit.Place(rect, new Vector2(0f, 0f), new Vector2(40f + i * 130f, 30f), new Vector2(stage.Boss ? 150f : 110f, 110f));
                if (selected)
                    UIKit.AddBorder(rect, 40, Palette.GoldLight, 6, -8f);
                if (unlocked)
                {
                    var leaves = UIKit.Label(rect, Loc.Number(save.LeavesFound(stage.Id)) + "/" + Loc.Number(3), 22, Palette.Paper, TextAnchor.MiddleCenter);
                    UIKit.PlaceFixed(leaves.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -30f), new Vector2(110f, 30f));
                }
            }

            if (selectedStage != null && selectedStage.Chapter == chapter.Number)
            {
                var stageName = UIKit.Label(panel.transform, selectedStage.Name, 40, Palette.Paper, TextAnchor.MiddleRight, true);
                UIKit.Place(stageName.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -80f), new Vector2(560f, 60f));
                var play = UIKit.Button(panel.transform, save.IsDone(selectedStage.Id) ? Ui.Replay : Ui.Play, () => Game.PlayStage(selectedStage), ButtonStyle.Primary, 40);
                UIKit.Place((RectTransform)play.transform, new Vector2(1f, 0f), new Vector2(-40f, 26f), new Vector2(300f, 100f));
            }
        }

        public override void OnBack()
        {
            Game.ShowMenu();
        }
    }
}
