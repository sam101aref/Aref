using Arash.Core;
using Arash.Levels;
using Arash.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Arash.UI
{
    /// <summary>
    /// Level select (F-13): each chapter with its levels, earned stars, and locked levels greyed out.
    /// Placeholder for the illustrated map of ancient Iran described in the GDD.
    /// </summary>
    public class WorldMapScreen : ScreenBase
    {
        bool armoryOpen;

        protected override void Build(RectTransform canvas)
        {
            var save = SaveSystem.Data;
            var catalog = LevelCatalog.Load();

            UIFactory.PatternBackground(canvas);

            var title = UIFactory.Label(canvas, Loc.T("ui.map"), 72, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 110f));

            var back = UIFactory.Button(canvas, Loc.T("ui.back"), SceneFlow.ToMainMenu, UIFactory.LapisLight, 40);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(260f, 90f));

            var armory = UIFactory.Button(canvas, Loc.T("ui.armory"), () =>
            {
                armoryOpen = true;
                OpenArmory(canvas);
            }, UIFactory.Gold, 40);
            UIFactory.Place((RectTransform)armory.transform, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(300f, 90f));

            var stats = UIFactory.Label(canvas, Loc.T("map.stats", save.TotalStars, save.coins), 40, UIFactory.Cream, TextAnchor.MiddleRight);
            UIFactory.Place(stats.rectTransform, new Vector2(1f, 1f), new Vector2(-370f, -40f), new Vector2(700f, 90f));

            var area = UIFactory.Rect(canvas, "Chapters");
            UIFactory.Stretch(area);
            area.offsetMin = new Vector2(80f, 40f);
            area.offsetMax = new Vector2(-80f, -170f);
            var content = UIFactory.ScrollColumn(area, 24f);

            if (catalog == null)
            {
                UIFactory.Height(UIFactory.Label(content, "No level catalog found.", 40, UIFactory.Danger), 80f);
                return;
            }

            var levelNumber = 0;
            for (var chapterIndex = 0; chapterIndex < catalog.chapters.Count; chapterIndex++)
            {
                var chapter = catalog.chapters[chapterIndex];
                var paywalled = !Monetization.IsChapterAccessible(chapterIndex, save);
                var locked = paywalled || save.TotalStars < chapter.starsToUnlock;
                var heading = Loc.T(chapter.titleKey);
                if (!paywalled && locked)
                    heading += "   " + Loc.T("map.locked", chapter.starsToUnlock);
                var header = UIFactory.Label(content, heading, 48, locked ? UIFactory.Muted : UIFactory.Gold, TextAnchor.MiddleLeft, true);
                UIFactory.Height(header, 80f);

                if (paywalled && Monetization.CanBuyFullGame(save))
                {
                    var unlock = UIFactory.Button(content, Loc.T("store.unlock_full", Monetization.Store.PriceText(Monetization.FullGameProduct)),
                        () => Monetization.BuyFullGame(save, success => Rebuild()), UIFactory.Gold, 40);
                    UIFactory.Height(unlock, 90f);
                }

                var grid = UIFactory.Rect(content, "Levels").gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(200f, 210f);
                grid.spacing = new Vector2(36f, 36f);
                grid.startCorner = Loc.IsRtl ? GridLayoutGroup.Corner.UpperRight : GridLayoutGroup.Corner.UpperLeft;
                grid.childAlignment = Loc.IsRtl ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 7;

                foreach (var level in chapter.levels)
                {
                    if (level == null)
                        continue;
                    levelNumber++;
                    LevelButton(grid.transform, level, levelNumber, catalog.IsUnlocked(level, save), save.GetStars(level.id));
                }
            }

            if (armoryOpen)
                OpenArmory(canvas);
        }

        void OpenArmory(RectTransform canvas)
        {
            ArmoryPanel.Open(canvas, () =>
            {
                armoryOpen = false;
                Rebuild(); // coins changed
            });
        }

        static void LevelButton(Transform parent, LevelDefinition level, int number, bool unlocked, int stars)
        {
            var button = UIFactory.Button(parent, Loc.Number(number), () => SceneFlow.Play(level),
                unlocked ? UIFactory.Turquoise : UIFactory.Muted, 72);
            button.interactable = unlocked;

            var label = button.GetComponentInChildren<Text>();
            label.rectTransform.offsetMin = new Vector2(0f, 50f); // leave room for the stars

            if (unlocked)
                UIFactory.StarRow(button.transform, stars, 46f, new Vector2(0f, -62f));
        }
    }
}
