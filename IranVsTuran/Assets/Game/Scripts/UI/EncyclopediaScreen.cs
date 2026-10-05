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
    /// <summary>
    /// "The Book of Foes": every enemy from the Shahnameh met so far, with its lore and stats.
    /// Enemies of levels not yet unlocked stay hidden.
    /// </summary>
    public class EncyclopediaScreen : ScreenBase
    {
        protected override void Build()
        {
            Background(ArtLibrary.Backdrop(Backdrop.Palace), new Color(0.4f, 0.36f, 0.42f));
            TopBar("book.title", () => Root.ToMap());

            var known = KnownEnemies();
            var area = UIKit.Rect(Rect, "Grid");
            area.anchorMin = new Vector2(0.5f, 0f);
            area.anchorMax = new Vector2(0.5f, 1f);
            area.sizeDelta = new Vector2(1700f, -170f);
            area.anchoredPosition = new Vector2(0f, -70f);
            var content = UIKit.ScrollColumn(area, 20f);

            RectTransform row = null;
            for (var i = 0; i < EnemyDefs.All.Count; i++)
            {
                if (i % 7 == 0)
                {
                    row = UIKit.Rect(content, "Row");
                    var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 20f;
                    layout.childControlWidth = false;
                    layout.childControlHeight = false;
                    layout.childAlignment = Loc.IsRtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                    layout.reverseArrangement = Loc.IsRtl;
                    UIKit.Size(row, 0f, 240f);
                }
                Entry(row, EnemyDefs.All[i], known.Contains(EnemyDefs.All[i].id));
            }
        }

        static HashSet<string> KnownEnemies()
        {
            var known = new HashSet<string>();
            foreach (var level in LevelDefs.All)
            {
                if (!Progress.IsUnlocked(level))
                    continue;
                foreach (var wave in level.waves)
                    foreach (var group in wave.groups)
                    {
                        known.Add(group.enemy);
                        foreach (var ability in EnemyDefs.Get(group.enemy).abilities)
                            if (!string.IsNullOrEmpty(ability.summonId))
                                known.Add(ability.summonId);
                    }
            }
            return known;
        }

        void Entry(RectTransform row, EnemyDef def, bool known)
        {
            var frame = UIKit.Panel(row, def.boss ? Raster.Hex(0xE8B0A0) : UIKit.Parchment, "Enemy");
            frame.rectTransform.sizeDelta = new Vector2(225f, 230f);
            var portrait = UIKit.SpriteImage(frame.transform, ArtLibrary.Character(def.look), new Vector2(170f, 170f));
            UIKit.Place(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(170f, 170f), false);
            if (!known)
                portrait.color = Color.black;
            var name = UIKit.Label(frame.transform, known ? Loc.T(def.NameKey) : "?", 26, UIKit.Ink, TextAnchor.MiddleCenter, true, false);
            UIKit.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(220f, 44f), false);
            if (!known)
                return;
            frame.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                AudioService.Play(Sfx.Click, 0.5f);
                Detail(def);
            });
        }

        static void Detail(EnemyDef def)
        {
            var popup = Popup.Open(Loc.T(def.NameKey), new Vector2(1400f, 760f));
            var portrait = UIKit.SpriteImage(popup.Window, ArtLibrary.Character(def.look, 256), new Vector2(460f, 460f));
            UIKit.Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 10f), new Vector2(460f, 460f));
            var lore = UIKit.Paragraph(popup.Window, Loc.Get(def.LoreKey), 38, UIKit.Ink, 780f, TextAnchor.UpperLeft);
            UIKit.Place(lore.rectTransform, new Vector2(1f, 1f), new Vector2(-60f, -130f), lore.rectTransform.sizeDelta);

            var lines = new List<string>
            {
                Loc.Format(Loc.Get("book.hp"), Mathf.RoundToInt(def.hp)),
                Loc.Format(Loc.Get("book.speed"), Loc.Get(def.speed >= 1.3f ? "book.fast" : def.speed <= 0.6f ? "book.slow" : "book.normal")),
                Loc.Format(Loc.Get("book.armor"), Mathf.RoundToInt(def.armor * 100f), Mathf.RoundToInt(def.magicResist * 100f)),
                Loc.Format(Loc.Get("book.lives"), def.lives),
            };
            if (def.flying)
                lines.Add(Loc.Get("book.flying"));
            if (def.rangedRange > 0f)
                lines.Add(Loc.Get("book.ranged"));
            for (var i = 0; i < lines.Count; i++)
            {
                var stat = UIKit.Label(popup.Window, Loc.Display(lines[i]), 32, UIKit.Muted, TextAnchor.MiddleLeft, true, false);
                UIKit.Place(stat.rectTransform, new Vector2(1f, 0f), new Vector2(-60f, 60f + (lines.Count - 1 - i) * 50f), new Vector2(780f, 46f));
            }
        }

        public override void OnBack()
        {
            Root.ToMap();
        }
    }

    /// <summary>Language, sound, music, and resetting progress.</summary>
    public static class SettingsPopup
    {
        public static void Open()
        {
            var popup = Popup.Open(Loc.T("settings.title"), new Vector2(1100f, 820f));
            var y = 220f;
            Toggle(popup, Icon.Globe, Loc.T("settings.language"), Loc.IsRtl ? "English" : Loc.Display("فارسی"), y, () =>
            {
                GameSettings.Language = Loc.IsRtl ? Language.English : Language.Persian;
            });
            y -= 140f;
            Toggle(popup, Icon.Sound, Loc.T("settings.sound"), Loc.T(GameSettings.Sound ? "settings.on" : "settings.off"), y, () =>
            {
                GameSettings.Sound = !GameSettings.Sound;
                popup.Close();
                Open();
            });
            y -= 140f;
            Toggle(popup, Icon.Music, Loc.T("settings.music"), Loc.T(GameSettings.Music ? "settings.on" : "settings.off"), y, () =>
            {
                GameSettings.Music = !GameSettings.Music;
                popup.Close();
                Open();
            });
            y -= 140f;
            Toggle(popup, Icon.Close, Loc.T("settings.reset"), Loc.T("settings.reset_button"), y, () =>
            {
                Popup.Confirm(Loc.T("settings.reset"), Loc.Get("settings.reset_body"), Loc.T("settings.reset_button"), () =>
                {
                    SaveSystem.ResetProgress();
                    GameRoot.Instance.ToMainMenu();
                });
            }, UIKit.Danger);

            var credits = UIKit.Label(popup.Window, Loc.T("settings.credits"), 26, UIKit.Muted, TextAnchor.MiddleCenter, false, false);
            UIKit.Center(credits.rectTransform, new Vector2(0f, -360f), new Vector2(1000f, 50f), false);
        }

        static void Toggle(Popup popup, Icon icon, string label, string value, float y, System.Action onClick, Color? color = null)
        {
            var glyph = UIKit.Icon(popup.Window, icon, 80f);
            if (icon == Icon.Sound || icon == Icon.Music || icon == Icon.Close)
                glyph.color = UIKit.Ink;
            UIKit.Center(glyph.rectTransform, new Vector2(-430f, y), new Vector2(80f, 80f));
            var text = UIKit.Label(popup.Window, label, 42, UIKit.Ink, TextAnchor.MiddleLeft, true, false);
            UIKit.Center(text.rectTransform, new Vector2(-120f, y), new Vector2(480f, 80f));
            var button = UIKit.Button(popup.Window, value, () => onClick(), color ?? UIKit.Turquoise, 38);
            UIKit.Center(button.GetComponent<RectTransform>(), new Vector2(330f, y), new Vector2(320f, 100f));
        }
    }

    /// <summary>The seven-day login calendar, with a rewarded ad to double today's reward.</summary>
    public static class DailyPopup
    {
        public static void Open()
        {
            var popup = Popup.Open(Loc.T("daily.title"), new Vector2(1600f, 720f));
            var current = DailyReward.NextIndex;
            var canClaim = DailyReward.CanClaim;
            for (var i = 0; i < ShopDefs.Daily.Length; i++)
            {
                var today = i == current && canClaim;
                var done = i < current;
                var cell = UIKit.Panel(popup.Window, today ? Raster.Hex(0xA8E6A0) : i < current ? new Color(0.7f, 0.65f, 0.55f) : Raster.Hex(0xF8EBCB), "Day");
                UIKit.Center(cell.rectTransform, new Vector2((i - 3) * 205f, 60f), new Vector2(190f, 280f));
                var day = UIKit.Label(cell.transform, Loc.T("daily.day", i + 1), 30, UIKit.Ink, TextAnchor.MiddleCenter, true, false);
                UIKit.Place(day.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(180f, 50f), false);
                var badge = UIKit.RewardBadge(cell.transform, ShopDefs.Daily[i], 140f);
                UIKit.Place(badge, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(140f, 140f), false);
                if (done)
                {
                    var check = UIKit.Icon(cell.transform, Icon.Check, 80f);
                    check.color = UIKit.Green;
                    UIKit.Center(check.rectTransform, Vector2.zero, new Vector2(80f, 80f), false);
                }
                if (today)
                    cell.gameObject.AddComponent<Pulse>();
            }

            if (!canClaim)
            {
                var come = UIKit.Label(popup.Window, Loc.T("daily.come_back"), 40, UIKit.Muted, TextAnchor.MiddleCenter, true, false);
                UIKit.Center(come.rectTransform, new Vector2(0f, -230f), new Vector2(1400f, 80f), false);
                return;
            }

            var claim = UIKit.Button(popup.Window, Loc.T("daily.claim"), () =>
            {
                var reward = DailyReward.NextReward;
                if (DailyReward.Claim())
                {
                    Audio.AudioService.Play(Audio.Sfx.Reward, 1f);
                    GameRoot.Instance.Toast(UIKit.RewardName(reward));
                }
                popup.Close();
            }, UIKit.Green, 42);
            UIKit.Center(claim.GetComponent<RectTransform>(), new Vector2(-260f, -230f), new Vector2(420f, 120f));

            var doubled = UIKit.Button(popup.Window, Loc.T("daily.double"), () =>
            {
                Monetization.WatchAd(ShopDefs.AdDailyDouble, watched =>
                {
                    var reward = DailyReward.NextReward;
                    reward.amount *= watched ? 2 : 1;
                    if (DailyReward.Claim(watched ? 2 : 1))
                    {
                        Audio.AudioService.Play(Audio.Sfx.Reward, 1f);
                        GameRoot.Instance.Toast(UIKit.RewardName(reward));
                    }
                    popup.Close();
                });
            }, UIKit.Purple, 38, Icon.Ad);
            doubled.interactable = Monetization.CanWatchAd(ShopDefs.AdDailyDouble);
            UIKit.Center(doubled.GetComponent<RectTransform>(), new Vector2(260f, -230f), new Vector2(460f, 120f));
        }
    }
}
