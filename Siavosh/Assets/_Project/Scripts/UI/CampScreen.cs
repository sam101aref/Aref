using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.Play;
using UnityEngine;
using UnityEngine.UI;

namespace Siavosh.UI
{
    /// <summary>
    /// The camp: Siavosh's growth. The skill tree (three branches taught by his masters, plus the
    /// Farr powers the story grants) and the smith (swords, bows and armour bought with dinars).
    /// </summary>
    public class CampScreen : ScreenBase
    {
        public int Tab;
        string selectedSkill = "p1";
        LocText message;

        protected override void Build(RectTransform canvas)
        {
            var save = SaveSystem.Data;
            var bg = UIKit.Backdrop(canvas, "bg/dusk_far");
            var dim = UIKit.Fill(canvas, "Dim", Palette.WithAlpha(Palette.LapisNight, 0.55f));
            UIKit.Stretch(dim.rectTransform);
            bg.raycastTarget = false;

            BuildHero(canvas, save);

            var right = UIKit.Rect(canvas, "Right");
            right.anchorMin = new Vector2(0f, 0f);
            right.anchorMax = new Vector2(1f, 1f);
            right.offsetMin = new Vector2(Loc.IsRtl ? 40f : 640f, 40f);
            right.offsetMax = new Vector2(Loc.IsRtl ? -640f : -40f, -40f);

            var tabs = new[] { Ui.Skills, Ui.Smith };
            for (var i = 0; i < tabs.Length; i++)
            {
                var index = i;
                var b = UIKit.Button(right, tabs[i], () => { Tab = index; message = null; Rebuild(); }, Tab == i ? ButtonStyle.Primary : ButtonStyle.Secondary, 36);
                UIKit.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(i * 300f, 0f), new Vector2(280f, 92f));
            }
            var body = UIKit.Panel(right, "Body", Palette.Paper, 30, true, true);
            body.rectTransform.anchorMin = Vector2.zero;
            body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.offsetMin = Vector2.zero;
            body.rectTransform.offsetMax = new Vector2(0f, -110f);

            if (Tab == 0)
                BuildSkills(body.rectTransform, save);
            else
                BuildSmith(body.rectTransform, save);

            if (message != null)
            {
                var note = UIKit.Label(canvas, message, 32, Palette.GoldLight, TextAnchor.MiddleCenter, true, true);
                UIKit.Place(note.rectTransform, new Vector2(1f, 1f), new Vector2(-60f, -80f), new Vector2(700f, 50f));
            }
        }

        void BuildHero(RectTransform canvas, SaveData save)
        {
            var stats = HeroStats.From(save, Settings.Difficulty);
            var left = UIKit.Panel(canvas, "Hero", Palette.Hex(0x2B1B12, 0.9f), 30, true);
            UIKit.Place(left.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(560f, 1000f));

            var back = UIKit.IconButton(left.transform, "back", Game.ShowMenu, 88f, Palette.LapisDark, Palette.GoldLight);
            UIKit.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(88f, 88f));
            if (Loc.IsRtl)
                back.transform.Find("Icon back").localScale = new Vector3(-1f, 1f, 1f);
            var title = UIKit.Label(left.transform, Loc.Display(new LocText("سیاوش", "Siavosh").Raw + " · " + Ui.Level.RawFormat(save.Level)), 40, Palette.GoldLight, TextAnchor.MiddleLeft, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -36f), new Vector2(400f, 64f));

            var figure = UIKit.Picture(left.transform, "chars/siavosh", 600f);
            UIKit.PlaceFixed(figure.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), figure.rectTransform.sizeDelta);

            int from, to;
            Rules.LevelRange(save.Level, out from, out to);
            var bar = UIKit.Bar(left.transform, new Vector2(480f, 30f), Palette.TurquoiseLight, Palette.LapisDark);
            UIKit.PlaceFixed(bar.transform.parent as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 200f), new Vector2(480f, 30f));
            bar.fillAmount = to > from ? Mathf.Clamp01((save.xp - from) / (float)(to - from)) : 1f;
            var xp = UIKit.Label(left.transform, Ui.Experience.Format(save.xp, to), 26, Palette.Paper);
            UIKit.PlaceFixed(xp.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(480f, 40f));

            var line = UIKit.Label(left.transform, Ui.Stats.Format(stats.MaxHealth, Mathf.RoundToInt(stats.SwordDamage), Mathf.RoundToInt(stats.BowDamage)), 28, Palette.Paper);
            UIKit.PlaceFixed(line.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(520f, 40f));
            var honor = UIKit.Label(left.transform, Loc.Display(Ui.Honor.RawFormat(save.honor) + "   ·   " + Ui.Dinars.RawFormat(save.dinars)), 28, Palette.GoldLight);
            UIKit.PlaceFixed(honor.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(520f, 40f));
            var points = UIKit.Label(left.transform, Ui.SkillPoints.Format(save.SkillPoints), 30, save.SkillPoints > 0 ? Palette.Saffron : Palette.PaperDark, TextAnchor.MiddleCenter, true);
            UIKit.PlaceFixed(points.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(520f, 44f));
        }

        // ------------------------------------------------------------- skills

        void BuildSkills(RectTransform body, SaveData save)
        {
            var chapter = Catalog.UnlockedChapter(save);
            var branches = new[] { SkillBranch.Champion, SkillBranch.Archery, SkillBranch.Riding };
            var names = new[] { Ui.Champion, Ui.Archery, Ui.Riding };
            var masters = new[] { Ui.MasterRostam, Ui.MasterZavareh, Ui.WithShabrang };
            var colors = new[] { Palette.Vermilion, Palette.Lapis, Palette.Ink };

            for (var b = 0; b < branches.Length; b++)
            {
                var column = UIKit.Panel(body, "Branch", Palette.White, 22, true);
                UIKit.Place(column.rectTransform, new Vector2(0f, 1f), new Vector2(30f + b * 245f, -30f), new Vector2(230f, 620f));
                var n = UIKit.Label(column.transform, names[b], 34, colors[b], TextAnchor.MiddleCenter, true);
                UIKit.PlaceFixed(n.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(240f, 50f));
                var m = UIKit.Label(column.transform, masters[b], 22, Palette.EarthDark);
                UIKit.PlaceFixed(m.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(240f, 34f));
                var line = UIKit.Fill(column.transform, "Line", Palette.PaperDark);
                UIKit.PlaceFixed(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(6f, 420f));

                var tier = 0;
                foreach (var skill in Rules.Skills)
                {
                    if (skill.Branch != branches[b])
                        continue;
                    var state = Rules.StateOf(skill, save, chapter);
                    var fill = state == Rules.SkillState.Learned ? Palette.Gold
                        : state == Rules.SkillState.Available ? Palette.White
                        : Palette.Locked;
                    var ink = state == Rules.SkillState.Learned ? Palette.Ink : state == Rules.SkillState.Available ? Palette.Vermilion : Palette.EarthDark;
                    var id = skill.Id;
                    var node = UIKit.IconButton(column.transform, skill.Icon, () => { selectedSkill = id; message = null; Rebuild(); }, 96f, fill, ink);
                    UIKit.PlaceFixed((RectTransform)node.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f - tier * 128f), new Vector2(96f, 96f));
                    if (skill.Id == selectedSkill)
                        UIKit.RingImage(node.transform, Palette.Vermilion, -10f);
                    var label = UIKit.Label(column.transform, skill.Name, 21, Palette.Ink, TextAnchor.MiddleCenter);
                    UIKit.PlaceFixed(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -208f - tier * 128f), new Vector2(225f, 28f));
                    tier++;
                }
            }

            BuildSkillDetail(body, save, chapter);
            BuildFarr(body, save);
        }

        void BuildSkillDetail(RectTransform body, SaveData save, int chapter)
        {
            var skill = Rules.Skill(selectedSkill) ?? Rules.Skills[0];
            var state = Rules.StateOf(skill, save, chapter);
            var detail = UIKit.Panel(body, "Detail", Palette.Hex(0x2B1B12, 0.95f), 22, true);
            UIKit.Place(detail.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(420f, 620f));

            var icon = UIKit.IconButton(detail.transform, skill.Icon, null, 110f, state == Rules.SkillState.Learned ? Palette.Gold : Palette.Vermilion, Palette.White);
            UIKit.PlaceFixed((RectTransform)icon.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(110f, 110f));
            var name = UIKit.Label(detail.transform, skill.Name, 36, Palette.GoldLight, TextAnchor.MiddleCenter, true);
            UIKit.PlaceFixed(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(400f, 50f));
            var desc = UIKit.Wrapped(detail.transform, skill.Description.Raw, 27, Palette.Paper, 360f, TextAnchor.UpperCenter, false, 1.25f);
            UIKit.PlaceFixed(desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(380f, 220f));

            if (state == Rules.SkillState.Available)
            {
                var learn = UIKit.Button(detail.transform, Ui.Learn, () =>
                {
                    if (Rules.TryLearn(SaveSystem.Data, skill.Id, chapter))
                    {
                        SaveSystem.Save();
                        Audio.AudioService.Play(Audio.Sfx.FarrReady, 0.8f);
                        Rebuild();
                    }
                }, ButtonStyle.Primary, 32);
                UIKit.PlaceFixed((RectTransform)learn.transform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(360f, 100f));
            }
            else
            {
                var why = state == Rules.SkillState.Learned ? Ui.Learned
                    : state == Rules.SkillState.NeedsPrevious ? Ui.NeedsPrevious
                    : state == Rules.SkillState.NeedsPoint ? Ui.NeedsPoint
                    : Ui.StoryLocked;
                var note = UIKit.Wrapped(detail.transform, why.Raw, 28, state == Rules.SkillState.Learned ? Palette.TurquoiseLight : Palette.PaperDark, 360f, TextAnchor.MiddleCenter);
                UIKit.PlaceFixed(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(380f, 90f));
            }
        }

        void BuildFarr(RectTransform body, SaveData save)
        {
            var strip = UIKit.Panel(body, "Farr", Palette.LapisDark, 22, true);
            strip.rectTransform.anchorMin = new Vector2(0f, 0f);
            strip.rectTransform.anchorMax = new Vector2(1f, 0f);
            strip.rectTransform.pivot = new Vector2(0.5f, 0f);
            strip.rectTransform.offsetMin = new Vector2(30f, 24f);
            strip.rectTransform.offsetMax = new Vector2(-30f, 144f);
            var title = UIKit.Label(strip.transform, Ui.FarrTitle, 30, Palette.GoldLight, TextAnchor.MiddleLeft, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(180f, 60f));
            for (var i = 0; i < Rules.Farr.Count; i++)
            {
                var farr = Rules.Farr[i];
                var has = save.HasFarr(farr.Id);
                var item = UIKit.Rect(strip.transform, "Power");
                UIKit.Place(item, new Vector2(0f, 0.5f), new Vector2(210f + i * 250f, 0f), new Vector2(240f, 100f));
                var icon = UIKit.IconButton(item, farr.Icon, null, 76f, has ? Palette.Gold : Palette.Lapis, has ? Palette.Ink : Palette.Steel);
                UIKit.Place((RectTransform)icon.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(76f, 76f));
                var name = UIKit.Label(item, farr.Name, 24, Palette.Paper, TextAnchor.MiddleLeft, true);
                UIKit.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(86f, 16f), new Vector2(160f, 34f));
                var state = UIKit.Label(item, has ? Ui.Earned.ToString() : Ui.ChapterN.Format(farr.Chapter), 21, has ? Palette.GoldLight : Palette.Steel, TextAnchor.MiddleLeft);
                UIKit.Place(state.rectTransform, new Vector2(0f, 0.5f), new Vector2(86f, -18f), new Vector2(160f, 30f));
            }
        }

        // ------------------------------------------------------------- smith

        void BuildSmith(RectTransform body, SaveData save)
        {
            var chapter = Catalog.UnlockedChapter(save);
            var slots = new[] { GearSlot.Sword, GearSlot.Bow, GearSlot.Armor };
            var slotNames = new[] { Ui.Sword, Ui.Bow, Ui.Armor };
            var icons = new[] { "sword", "bow", "armor" };
            for (var s = 0; s < slots.Length; s++)
            {
                var header = UIKit.Label(body, slotNames[s], 32, Palette.Lapis, TextAnchor.MiddleLeft, true);
                UIKit.Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -24f - s * 220f), new Vector2(400f, 50f));
                var column = 0;
                foreach (var item in Rules.Gear)
                {
                    if (item.Slot != slots[s])
                        continue;
                    Card(body, item, icons[s], save, chapter, new Vector2(30f + column * 395f, -76f - s * 220f));
                    column++;
                }
            }
        }

        void Card(RectTransform body, GearDef item, string icon, SaveData save, int chapter, Vector2 position)
        {
            var owned = save.Owns(item.Id);
            var equipped = save.sword == item.Id || save.bow == item.Id || save.armor == item.Id;
            var locked = item.Chapter > chapter;
            var card = UIKit.Panel(body, "Card", Palette.White, 18, true);
            UIKit.Place(card.rectTransform, new Vector2(0f, 1f), position, new Vector2(380f, 150f));
            if (equipped)
                UIKit.AddBorder(card.rectTransform, 18, Palette.Turquoise, 6, 0f);
            var swatch = UIKit.IconButton(card.transform, icon, null, 90f, Palette.Parse(item.Color), Palette.White);
            UIKit.Place((RectTransform)swatch.transform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(90f, 90f));
            var name = UIKit.Label(card.transform, item.Name, 28, Palette.Ink, TextAnchor.MiddleLeft, true);
            UIKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(124f, -14f), new Vector2(250f, 40f));
            var stat = item.Slot == GearSlot.Armor ? Loc.Display(Ui.Health.Raw + " +" + Loc.Number(item.Health))
                : Loc.Display(Ui.Damage.Raw + " " + Loc.Number(Mathf.RoundToInt(item.Damage)));
            var statLabel = UIKit.Label(card.transform, stat, 24, Palette.EarthDark, TextAnchor.MiddleLeft);
            UIKit.Place(statLabel.rectTransform, new Vector2(0f, 1f), new Vector2(124f, -54f), new Vector2(250f, 34f));

            string action;
            ButtonStyle style;
            System.Action onClick = null;
            if (equipped)
            {
                action = Ui.Equipped.ToString();
                style = ButtonStyle.Lapis;
            }
            else if (owned)
            {
                action = Ui.Equip.ToString();
                style = ButtonStyle.Secondary;
                onClick = () => { Rules.Equip(SaveSystem.Data, item.Id); SaveSystem.Save(); Rebuild(); };
            }
            else if (locked)
            {
                action = Ui.ChapterN.Format(item.Chapter);
                style = ButtonStyle.Dark;
            }
            else
            {
                action = Ui.Buy.Format(item.Price);
                style = ButtonStyle.Primary;
                onClick = () =>
                {
                    if (Rules.TryBuy(SaveSystem.Data, item.Id, chapter))
                    {
                        SaveSystem.Save();
                        Audio.AudioService.Play(Audio.Sfx.Coin, 0.9f);
                        message = null;
                    }
                    else
                    {
                        message = Ui.NotEnough;
                    }
                    Rebuild();
                };
            }
            var button = UIKit.Button(card.transform, action, onClick, style, 24);
            UIKit.Place((RectTransform)button.transform, new Vector2(0f, 0f), new Vector2(124f, 14f), new Vector2(250f, 62f));
            if (locked)
                ((Image)button.targetGraphic).color = Palette.Smoke;
        }
    }
}
