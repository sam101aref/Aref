using System.Collections.Generic;
using IranVsTuran.Art;
using IranVsTuran.Audio;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace IranVsTuran.UI
{
    /// <summary>The campaign map of Iran and Turan with every battle on it.</summary>
    public class WorldMapScreen : ScreenBase
    {
        protected override void OnOpened()
        {
            AudioService.Mood = MusicMood.Calm;
        }

        protected override void Build()
        {
            var map = Background(ArtLibrary.WorldMap(), Color.white);

            Region(map.rectTransform, "map.iran", new Vector2(0.36f, 0.36f), 64);
            Region(map.rectTransform, "map.turan", new Vector2(0.85f, 0.3f), 64);
            Region(map.rectTransform, "map.mazandaran", new Vector2(0.14f, 0.52f), 40);
            Region(map.rectTransform, "map.caspian", new Vector2(0.26f, 0.95f), 34);

            LevelDef next = null;
            foreach (var level in LevelDefs.All)
            {
                Node(map.rectTransform, level);
                if (next == null && Progress.IsUnlocked(level) && !SaveSystem.Data.IsCompleted(level.id))
                    next = level;
            }

            TopBar("map.title", () => Root.ToMainMenu());
            BottomBar();

            var stars = UIKit.Amount(Rect, Icon.Star, SaveSystem.Data.TotalStars, 40, UIKit.Cream);
            var starsRect = stars.transform.parent as RectTransform;
            UIKit.Place(starsRect, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(300f, 70f), false);
        }

        void Region(RectTransform map, string key, Vector2 position, int size)
        {
            var label = UIKit.Label(map, Loc.T(key), size, new Color(0.35f, 0.2f, 0.1f, 0.75f), TextAnchor.MiddleCenter, true, false);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = position;
            label.rectTransform.sizeDelta = new Vector2(600f, size * 1.5f);
        }

        void Node(RectTransform map, LevelDef level)
        {
            var unlocked = Progress.IsUnlocked(level);
            var stars = SaveSystem.Data.GetStars(level.id);
            var node = UIKit.Rect(map, "Level " + level.id);
            node.anchorMin = node.anchorMax = new Vector2(level.mapPosition.x, level.mapPosition.y);
            node.sizeDelta = new Vector2(110f, 110f);

            var color = !unlocked ? new Color(0.45f, 0.42f, 0.4f) : stars > 0 ? UIKit.Gold : UIKit.Green;
            if (LevelDefs.ChapterOf(level).bonus && unlocked)
                color = UIKit.Purple;
            var circle = UIKit.Image(node, "Circle", color);
            circle.sprite = ArtLibrary.SolidCircle();
            UIKit.Stretch(circle.rectTransform);
            var outline = circle.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.12f, 0.05f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            var button = circle.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                AudioService.Play(Sfx.Click, 0.6f);
                if (unlocked)
                    LevelPopup.Open(level);
                else
                    Root.Toast(Loc.T("map.locked"));
            });

            if (unlocked)
            {
                var number = UIKit.Label(node, Loc.Number(level.index + 1), 52, Color.white, TextAnchor.MiddleCenter, true);
                UIKit.Stretch(number.rectTransform);
                if (!string.IsNullOrEmpty(level.boss))
                {
                    var skull = UIKit.Icon(node, Icon.Skull, 52f);
                    UIKit.Place(skull.rectTransform, new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(52f, 52f), false);
                }
                if (stars == 0)
                    node.gameObject.AddComponent<Pulse>();
            }
            else
            {
                var lockIcon = UIKit.Icon(node, Icon.Lock, 60f);
                UIKit.Center(lockIcon.rectTransform, Vector2.zero, new Vector2(60f, 60f), false);
            }

            if (stars > 0)
            {
                for (var i = 0; i < 3; i++)
                {
                    var star = UIKit.Icon(node, i < stars ? Icon.Star : Icon.StarEmpty, 44f);
                    UIKit.Center(star.rectTransform, new Vector2((i - 1) * 40f, -76f + (i == 1 ? -8f : 0f)), new Vector2(44f, 44f), false);
                }
            }
        }

        void BottomBar()
        {
            var row = UIKit.Rect(Rect, "BottomBar");
            UIKit.Place(row, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1500f, 120f), false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 22f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.reverseArrangement = Loc.IsRtl;
            UIKit.Button(row, Loc.T("menu.heroes"), () => Root.Show(new HeroesScreen()), UIKit.LapisLight, 34, Icon.Helmet);
            UIKit.Button(row, Loc.T("menu.armory"), () => Root.Show(new UpgradesScreen()), UIKit.LapisLight, 34, Icon.Upgrade);
            UIKit.Button(row, Loc.T("menu.book"), () => Root.Show(new EncyclopediaScreen()), UIKit.LapisLight, 34, Icon.Book);
            UIKit.Button(row, Loc.T("menu.shop"), () => Root.Show(new ShopScreen()), UIKit.Purple, 34, Icon.Gem);
            var pass = UIKit.Button(row, Loc.T("menu.pass"), () => Root.Show(new BattlePassScreen()), UIKit.Turquoise, 34, Icon.Crown);
            MainMenuScreen.Badge(pass.transform, BattlePass.UnclaimedCount);
        }

        public override void OnBack()
        {
            Root.ToMainMenu();
        }
    }

    /// <summary>Gently pulses a UI element (the next level to play).</summary>
    public class Pulse : MonoBehaviour
    {
        float time;

        void Update()
        {
            time += Time.unscaledDeltaTime;
            var s = 1f + Mathf.Sin(time * 4f) * 0.07f;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>Details of a level before fighting it: story, enemies, difficulty and hero.</summary>
    public static class LevelPopup
    {
        public static void Open(LevelDef level)
        {
            var popup = Popup.Open(Loc.T(level.NameKey), new Vector2(1500f, 900f));
            var window = popup.Window;

            var chapter = UIKit.Label(window, Loc.T(LevelDefs.ChapterOf(level).NameKey), 36, UIKit.Muted, TextAnchor.MiddleCenter, true, false);
            UIKit.Center(chapter.rectTransform, new Vector2(0f, 330f), new Vector2(1200f, 60f), false);

            var desc = UIKit.Paragraph(window, Loc.Get(level.DescKey), 38, UIKit.Ink, 820f, TextAnchor.UpperLeft);
            UIKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -170f), desc.rectTransform.sizeDelta);

            var stars = SaveSystem.Data.GetStars(level.id);
            for (var i = 0; i < 3; i++)
            {
                var star = UIKit.Icon(window, i < stars ? Icon.Star : Icon.StarEmpty, 80f);
                UIKit.Place(star.rectTransform, new Vector2(1f, 1f), new Vector2(-90f - i * 90f, -160f), new Vector2(80f, 80f));
            }

            // the enemies of this battle
            var heading = UIKit.Label(window, Loc.T("level.enemies"), 36, UIKit.Ink, TextAnchor.MiddleLeft, true, false);
            UIKit.Place(heading.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -360f), new Vector2(600f, 60f));
            var roster = new List<string>();
            foreach (var wave in level.waves)
                foreach (var group in wave.groups)
                    if (!roster.Contains(group.enemy))
                        roster.Add(group.enemy);
            for (var i = 0; i < Mathf.Min(roster.Count, 8); i++)
            {
                var def = EnemyDefs.Get(roster[i]);
                var frame = UIKit.Panel(window, def.boss ? UIKit.Danger : new Color(0f, 0f, 0f, 0.15f), "Enemy");
                UIKit.Place(frame.rectTransform, new Vector2(0f, 1f), new Vector2(70f + i * 120f, -430f), new Vector2(110f, 110f));
                var portrait = UIKit.SpriteImage(frame.transform, ArtLibrary.Character(def.look), new Vector2(110f, 110f));
                UIKit.Stretch(portrait.rectTransform, 4f);
            }

            var info = UIKit.Label(window, Loc.T("level.info", level.waves.Length, level.startCoins), 34, UIKit.Muted, TextAnchor.MiddleLeft, false, false);
            UIKit.Place(info.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -570f), new Vector2(900f, 60f));

            // difficulty
            var difficultyLabel = UIKit.Label(window, Loc.T("level.difficulty"), 34, UIKit.Ink, TextAnchor.MiddleLeft, true, false);
            UIKit.Place(difficultyLabel.rectTransform, new Vector2(0f, 0f), new Vector2(70f, 170f), new Vector2(500f, 60f));
            var buttons = new Button[3];
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                buttons[i] = UIKit.Button(window, Loc.T("difficulty." + i), () =>
                {
                    SaveSystem.Data.settings.difficulty = index;
                    SaveSystem.Save();
                    for (var k = 0; k < 3; k++)
                        buttons[k].GetComponent<Image>().color = k == index ? UIKit.Turquoise : UIKit.Muted;
                }, i == SaveSystem.Data.settings.difficulty ? UIKit.Turquoise : UIKit.Muted, 32);
                UIKit.Place(buttons[i].GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(70f + i * 250f, 60f), new Vector2(235f, 96f));
            }

            // hero
            var hero = Heroes.Selected;
            var heroFrame = UIKit.Panel(window, new Color(0f, 0f, 0f, 0.15f), "Hero");
            UIKit.Place(heroFrame.rectTransform, new Vector2(1f, 1f), new Vector2(-70f, -270f), new Vector2(380f, 330f));
            var heroPortrait = UIKit.SpriteImage(heroFrame.transform, ArtLibrary.Character(HeroDefs.LookFor(hero, Heroes.Skin(hero.id)), 256), new Vector2(260f, 260f));
            UIKit.Center(heroPortrait.rectTransform, new Vector2(0f, 30f), new Vector2(260f, 260f), false);
            var heroName = UIKit.Label(heroFrame.transform, Loc.T("level.hero", Loc.Get(hero.NameKey), Heroes.Level(hero.id)), 32, UIKit.Ink, TextAnchor.MiddleCenter, true, false);
            UIKit.Center(heroName.rectTransform, new Vector2(0f, -130f), new Vector2(360f, 50f), false);
            var change = UIKit.Button(window, Loc.T("level.change_hero"), () =>
            {
                popup.Close();
                GameRoot.Instance.Show(new HeroesScreen());
            }, UIKit.LapisLight, 30);
            UIKit.Place(change.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-120f, -620f), new Vector2(280f, 80f));

            var fight = UIKit.Button(window, Loc.T("level.fight"), () =>
            {
                popup.Close();
                GameRoot.Instance.StartLevel(level, SaveSystem.Data.settings.difficulty);
            }, UIKit.Danger, 52, Icon.Sword);
            UIKit.Place(fight.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-70f, 50f), new Vector2(420f, 130f));
        }
    }
}
