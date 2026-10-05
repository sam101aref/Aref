using System;
using System.Collections.Generic;
using IranVsTuran.Art;
using IranVsTuran.Audio;
using IranVsTuran.Battle;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IranVsTuran.UI
{
    /// <summary>
    /// The battle: the battlefield in the world, and over it the HUD — lives, coins and waves,
    /// pause and speed, the war horn that calls waves, the hero, the two spells, items, and the
    /// Kingdom-Rush style ring menus for building, upgrading and selling towers.
    /// </summary>
    public class BattleScreen : ScreenBase
    {
        enum Targeting
        {
            None,
            Hero,
            Arrows,
            Reinforce,
            Naphtha,
            Rally,
        }

        readonly LevelDef level;
        readonly int difficulty;
        Battlefield field;
        float speed = 1f;
        bool paused;

        Targeting targeting;
        Tower rallyTower;
        RectTransform menu;
        string pendingChoice;
        SpriteRenderer rangeRing;
        Text livesLabel;
        Text coinsLabel;
        Text waveLabel;
        Text hintLabel;
        RectTransform hintPanel;
        readonly List<KeyValuePair<RectTransform, int>> horns = new List<KeyValuePair<RectTransform, int>>();
        Image heroHp;
        Image heroAbility;
        Image heroFrame;
        Image arrowsCooldown;
        Image reinforceCooldown;
        Image arrowsFrame;
        Image reinforceFrame;
        readonly Dictionary<string, Text> itemCounts = new Dictionary<string, Text>();
        readonly Dictionary<string, Image> itemFrames = new Dictionary<string, Image>();
        Button speedButton;
        RectTransform hud;
        int hintStep;
        bool resultShown;

        public BattleScreen(LevelDef level, int difficulty)
        {
            this.level = level;
            this.difficulty = difficulty;
        }

        protected override void OnOpened()
        {
            AudioService.Mood = MusicMood.Battle;
            Time.timeScale = 1f;
            GameRoot.FitCamera(Root.Camera);
            field = Battlefield.Create(level, difficulty, Root.Camera);
            field.Changed += RefreshTop;
            field.BossArrived += OnBoss;
            field.Ended += OnEnded;
            rangeRing = new GameObject("Range").AddComponent<SpriteRenderer>();
            rangeRing.sprite = ArtLibrary.Ring();
            rangeRing.color = new Color(1f, 1f, 1f, 0.6f);
            rangeRing.sortingOrder = Depth.Overlay;
            rangeRing.transform.SetParent(field.transform, false);
            rangeRing.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            Time.timeScale = 1f;
            if (field != null)
                Object.Destroy(field.gameObject);
            base.Dispose();
        }

        protected override void Build()
        {
            horns.Clear();
            itemCounts.Clear();
            itemFrames.Clear();
            menu = null;

            // full-screen catcher for taps on the battlefield (below every HUD element)
            var catcher = UIKit.Image(Rect, "WorldTaps", Color.clear);
            UIKit.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<TapCatcher>().OnTap = OnWorldTap;

            hud = UIKit.Stretch(UIKit.Rect(Rect, "Hud"));
            TopLeft();
            TopRight();
            for (var p = 0; p < field.Tracks.Length; p++)
                Horn(p);
            HeroAndSpells();
            Items();
            Hint();

            if (Rect.GetComponent<Driver>() == null)
                Rect.gameObject.AddComponent<Driver>().Tick = Tick;
            RefreshTop();
        }

        // ------------------------------------------------------------ layout

        void TopLeft()
        {
            var panel = UIKit.Panel(hud, new Color(0.05f, 0.07f, 0.13f, 0.82f), "Stats");
            UIKit.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(560f, 100f));
            var row = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(20, 20, 10, 10);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.reverseArrangement = Loc.IsRtl;
            UIKit.Icon(panel.transform, Icon.Heart, 60f);
            livesLabel = StatLabel(panel.transform, 90f);
            UIKit.Icon(panel.transform, Icon.Coin, 60f);
            coinsLabel = StatLabel(panel.transform, 120f);
            UIKit.Icon(panel.transform, Icon.Skull, 60f);
            waveLabel = StatLabel(panel.transform, 130f);
        }

        static Text StatLabel(Transform parent, float width)
        {
            var label = UIKit.Label(parent, "0", 40, UIKit.Cream, TextAnchor.MiddleCenter, true);
            label.rectTransform.sizeDelta = new Vector2(width, 60f);
            return label;
        }

        void TopRight()
        {
            var pause = UIKit.IconButton(hud, Icon.Pause, Pause, 100f, UIKit.LapisLight);
            UIKit.Place(pause.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(100f, 100f));
            speedButton = UIKit.IconButton(hud, Icon.Fast, ToggleSpeed, 100f, UIKit.LapisLight);
            UIKit.Place(speedButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-135f, -20f), new Vector2(100f, 100f));
        }

        void Horn(int pathIndex)
        {
            var button = UIKit.IconButton(hud, Icon.Horn, () =>
            {
                var bonus = field.EarlyCallBonus;
                field.CallWave();
                if (bonus > 0)
                    Root.Toast(Loc.T("battle.early_bonus", bonus));
                if (hintStep == 1)
                    hintStep = 2;
            }, 110f, UIKit.Danger);
            button.gameObject.AddComponent<Pulse>();
            var label = UIKit.Label(button.transform, string.Empty, 28, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Center(label.rectTransform, new Vector2(0f, -78f), new Vector2(220f, 40f), false);
            horns.Add(new KeyValuePair<RectTransform, int>(button.GetComponent<RectTransform>(), pathIndex));
        }

        void HeroAndSpells()
        {
            var x = 20f;
            if (field.Hero != null)
            {
                heroFrame = UIKit.Panel(hud, UIKit.LapisLight, "Hero");
                UIKit.Place(heroFrame.rectTransform, new Vector2(0f, 0f), new Vector2(x, 20f), new Vector2(150f, 150f));
                var hero = field.Hero.HeroDef;
                var portrait = UIKit.SpriteImage(heroFrame.transform, ArtLibrary.Character(HeroDefs.LookFor(hero, Heroes.Skin(hero.id))), new Vector2(150f, 150f));
                UIKit.Stretch(portrait.rectTransform, 6f);
                heroAbility = UIKit.Image(heroFrame.transform, "Ability", new Color(0f, 0f, 0f, 0.55f));
                heroAbility.sprite = ArtLibrary.Pixel();
                heroAbility.type = Image.Type.Filled;
                heroAbility.fillMethod = Image.FillMethod.Vertical;
                heroAbility.raycastTarget = false;
                UIKit.Stretch(heroAbility.rectTransform, 6f);
                heroHp = UIKit.ProgressBar(heroFrame.transform, new Vector2(150f, 22f), UIKit.Green);
                UIKit.Place((RectTransform)heroHp.transform.parent, new Vector2(0.5f, 0f), new Vector2(0f, -26f), new Vector2(150f, 22f), false);
                heroFrame.gameObject.AddComponent<Button>().onClick.AddListener(() => SetTargeting(targeting == Targeting.Hero ? Targeting.None : Targeting.Hero));
                x += 170f;
            }
            arrowsFrame = SpellButton(Icon.Arrow, x, Targeting.Arrows, out arrowsCooldown);
            x += 150f;
            reinforceFrame = SpellButton(Icon.Shield, x, Targeting.Reinforce, out reinforceCooldown);
        }

        Image SpellButton(Icon icon, float x, Targeting mode, out Image cooldown)
        {
            var frame = UIKit.Panel(hud, UIKit.LapisLight, "Spell");
            UIKit.Place(frame.rectTransform, new Vector2(0f, 0f), new Vector2(x, 30f), new Vector2(130f, 130f));
            var glyph = UIKit.Icon(frame.transform, icon, 96f);
            UIKit.Center(glyph.rectTransform, Vector2.zero, new Vector2(96f, 96f), false);
            cooldown = UIKit.Image(frame.transform, "Cooldown", new Color(0f, 0f, 0f, 0.6f));
            cooldown.sprite = ArtLibrary.Pixel();
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Vertical;
            cooldown.raycastTarget = false;
            UIKit.Stretch(cooldown.rectTransform, 5f);
            frame.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                var ready = mode == Targeting.Arrows ? field.ArrowsCooldown <= 0f : field.ReinforceCooldown <= 0f;
                if (!ready)
                {
                    Root.Toast(Loc.T("battle.not_ready"));
                    return;
                }
                SetTargeting(targeting == mode ? Targeting.None : mode);
            });
            return frame;
        }

        void Items()
        {
            for (var i = 0; i < ItemDefs.All.Count; i++)
            {
                var item = ItemDefs.All[i];
                var frame = UIKit.Panel(hud, UIKit.Purple, "Item");
                UIKit.Place(frame.rectTransform, new Vector2(1f, 0f), new Vector2(-20f - i * 135f, 30f), new Vector2(120f, 120f));
                var glyph = UIKit.Icon(frame.transform, UIKit.ItemIcon(item.id), 84f);
                UIKit.Center(glyph.rectTransform, new Vector2(0f, 6f), new Vector2(84f, 84f), false);
                var count = UIKit.Label(frame.transform, string.Empty, 30, Color.white, TextAnchor.LowerRight, true);
                UIKit.Stretch(count.rectTransform, 8f);
                itemCounts[item.id] = count;
                itemFrames[item.id] = frame;
                var id = item.id;
                frame.gameObject.AddComponent<Button>().onClick.AddListener(() => UseItem(id));
            }
            RefreshItems();
        }

        void Hint()
        {
            var panel = UIKit.Panel(hud, new Color(0.05f, 0.07f, 0.13f, 0.88f), "Hint");
            panel.raycastTarget = false;
            hintPanel = panel.rectTransform;
            UIKit.Place(hintPanel, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1100f, 90f), false);
            hintLabel = UIKit.Label(panel.transform, string.Empty, 38, UIKit.Gold, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(hintLabel.rectTransform);
            hintPanel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ per frame

        void Tick()
        {
            if (field == null)
                return;

            var showHorns = field.CanCallWave && !field.IsOver;
            foreach (var horn in horns)
            {
                horn.Key.gameObject.SetActive(showHorns);
                if (!showHorns)
                    continue;
                var world = field.Tracks[horn.Value].EntryInView(0.9f);
                horn.Key.anchorMin = horn.Key.anchorMax = Vector2.zero;
                horn.Key.pivot = new Vector2(0.5f, 0.5f);
                horn.Key.anchoredPosition = ScreenToCanvas(Root.Camera.WorldToScreenPoint(world));
                var label = horn.Key.GetComponentInChildren<Text>();
                label.text = field.State == BattleState.Prep
                    ? Loc.T("battle.start")
                    : Loc.T("battle.next_in", Mathf.CeilToInt(field.NextWaveTimer));
            }

            if (field.Hero != null && heroFrame != null)
            {
                heroHp.fillAmount = field.Hero.Alive ? field.Hero.Hp / field.Hero.MaxHp : 0f;
                heroAbility.fillAmount = field.Hero.Alive ? 1f - field.Hero.AbilityReady : 1f;
                heroFrame.color = targeting == Targeting.Hero ? UIKit.Gold : UIKit.LapisLight;
            }
            arrowsCooldown.fillAmount = field.ArrowsCooldown / field.ArrowsCooldownMax;
            reinforceCooldown.fillAmount = field.ReinforceCooldown / field.ReinforceCooldownMax;
            arrowsFrame.color = targeting == Targeting.Arrows ? UIKit.Gold : UIKit.LapisLight;
            reinforceFrame.color = targeting == Targeting.Reinforce ? UIKit.Gold : UIKit.LapisLight;
            if (itemFrames.ContainsKey(ItemIds.Naphtha))
                itemFrames[ItemIds.Naphtha].color = targeting == Targeting.Naphtha ? UIKit.Gold : UIKit.Purple;

            UpdateHint();
        }

        void UpdateHint()
        {
            string key = null;
            if (targeting != Targeting.None)
                key = "battle.target." + targeting.ToString().ToLowerInvariant();
            else if (level.index == 0 && !field.IsOver)
            {
                if (hintStep == 0 && field.Towers.Count > 0)
                    hintStep = 1;
                if (hintStep == 1 && field.WavesStarted > 0)
                    hintStep = 2;
                if (hintStep == 2 && field.WavesStarted >= 2)
                    hintStep = 3;
                key = hintStep == 0 ? "hint.build" : hintStep == 1 ? "hint.horn" : hintStep == 2 ? "hint.hero" : null;
            }
            hintPanel.gameObject.SetActive(key != null);
            if (key != null)
                hintLabel.text = Loc.T(key);
        }

        void RefreshTop()
        {
            if (livesLabel == null)
                return;
            livesLabel.text = Loc.Number(field.Lives);
            coinsLabel.text = Loc.Number(field.Coins);
            waveLabel.text = Loc.Number(Mathf.Max(1, field.WavesStarted)) + "/" + Loc.Number(field.WaveCount);
            RefreshItems();
            if (menu != null && pendingChoice == null)
                RefreshMenuAffordability();
        }

        void RefreshItems()
        {
            foreach (var pair in itemCounts)
            {
                var count = Economy.ItemCount(pair.Key);
                pair.Value.text = count > 0 ? Loc.Number(count) : "+";
            }
        }

        Vector2 ScreenToCanvas(Vector3 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screen, null, out local);
            return local + Rect.rect.size * 0.5f;
        }

        Vector2 WorldOf(Vector2 screen)
        {
            var world = Root.Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            return new Vector2(world.x, world.y);
        }

        // ------------------------------------------------------------ input

        void SetTargeting(Targeting mode)
        {
            CloseMenu();
            targeting = mode;
        }

        void OnWorldTap(Vector2 screen)
        {
            if (field == null || field.IsOver || paused)
                return;
            var point = WorldOf(screen);

            switch (targeting)
            {
                case Targeting.Arrows:
                    field.CastArrows(point);
                    targeting = Targeting.None;
                    return;
                case Targeting.Reinforce:
                    field.CastReinforce(point);
                    targeting = Targeting.None;
                    return;
                case Targeting.Naphtha:
                    if (!field.UseItem(ItemIds.Naphtha, point))
                        Root.Toast(Loc.T("battle.no_item"));
                    targeting = Targeting.None;
                    return;
                case Targeting.Rally:
                    if (rallyTower != null)
                        field.SetRally(rallyTower, point);
                    targeting = Targeting.None;
                    rangeRing.gameObject.SetActive(false);
                    return;
                case Targeting.Hero:
                    if (field.SlotAt(point) == null)
                    {
                        field.MoveHero(point);
                        field.Effects.Ring(point, 0.4f, new Color(1f, 0.9f, 0.4f, 0.9f), 0.35f);
                        targeting = Targeting.None;
                        if (hintStep == 2)
                            hintStep = 3;
                        return;
                    }
                    targeting = Targeting.None;
                    break;
            }

            if (menu != null)
            {
                CloseMenu();
                return;
            }

            var slot = field.SlotAt(point);
            if (slot != null)
            {
                if (slot.Tower == null)
                    OpenBuildMenu(slot);
                else
                    OpenTowerMenu(slot.Tower);
                return;
            }
            if (field.IsHeroAt(point))
                SetTargeting(Targeting.Hero);
        }

        void UseItem(string id)
        {
            if (field.IsOver)
                return;
            if (Economy.ItemCount(id) <= 0)
            {
                Popup.Confirm(Loc.T(ItemDefs.Get(id).NameKey), Loc.Format(Loc.Get("battle.buy_item"), Loc.Get("item." + id), ItemDefs.Get(id).gemPrice),
                    Loc.T("store.buy"), () =>
                    {
                        if (Economy.BuyItem(ItemDefs.Get(id)))
                            RefreshItems();
                        else
                            Root.Toast(Loc.T("shop.not_enough_gems"));
                    });
                return;
            }
            if (id == ItemIds.Naphtha)
            {
                SetTargeting(targeting == Targeting.Naphtha ? Targeting.None : Targeting.Naphtha);
                return;
            }
            if (field.UseItem(id, Vector2.zero))
                Root.Toast(Loc.T("item." + id));
        }

        // ------------------------------------------------------------ ring menus

        void CloseMenu()
        {
            if (menu != null)
                Object.Destroy(menu.gameObject);
            menu = null;
            pendingChoice = null;
            if (targeting != Targeting.Rally)
                rangeRing.gameObject.SetActive(false);
        }

        RectTransform NewMenu(Vector2 world)
        {
            CloseMenu();
            menu = UIKit.Rect(hud, "Menu");
            menu.anchorMin = menu.anchorMax = Vector2.zero;
            menu.pivot = new Vector2(0.5f, 0.5f);
            menu.sizeDelta = new Vector2(10f, 10f);
            var position = ScreenToCanvas(Root.Camera.WorldToScreenPoint(world));
            var size = Rect.rect.size;
            position.x = Mathf.Clamp(position.x, 260f, size.x - 260f);
            position.y = Mathf.Clamp(position.y, 230f, size.y - 260f);
            menu.anchoredPosition = position;
            var ring = UIKit.Image(menu, "Ring", new Color(1f, 1f, 1f, 0.25f));
            ring.sprite = ArtLibrary.Ring();
            ring.raycastTarget = false;
            UIKit.Center(ring.rectTransform, Vector2.zero, new Vector2(330f, 330f), false);
            return menu;
        }

        void ShowRange(Vector2 center, float range, Color color)
        {
            rangeRing.gameObject.SetActive(true);
            rangeRing.transform.localPosition = new Vector3(center.x, center.y, 0f);
            rangeRing.transform.localScale = new Vector3(range, range * 0.75f, 1f);
            rangeRing.color = color;
        }

        /// <summary>A round menu choice. The first tap shows details; the second confirms.</summary>
        Button Choice(RectTransform parent, Vector2 offset, string id, Sprite picture, Icon? icon, int cost, Action confirm, Action preview)
        {
            var button = UIKit.Button(parent, null, null, UIKit.Lapis, 30);
            var rect = button.GetComponent<RectTransform>();
            UIKit.Center(rect, offset, new Vector2(130f, 130f), false);
            button.GetComponent<Image>().sprite = ArtLibrary.SolidCircle();
            button.GetComponent<Image>().type = Image.Type.Simple;
            if (picture != null)
            {
                var image = UIKit.SpriteImage(rect, picture, new Vector2(118f, 118f));
                UIKit.Center(image.rectTransform, new Vector2(0f, 6f), new Vector2(118f, 118f), false);
            }
            else if (icon.HasValue)
            {
                var glyph = UIKit.Icon(rect, icon.Value, 84f);
                UIKit.Center(glyph.rectTransform, Vector2.zero, new Vector2(84f, 84f), false);
            }
            if (cost != 0)
            {
                var price = UIKit.Panel(rect, new Color(0.05f, 0.05f, 0.1f, 0.9f), "Price");
                price.raycastTarget = false;
                UIKit.Center(price.rectTransform, new Vector2(0f, -70f), new Vector2(130f, 46f), false);
                var label = UIKit.Label(price.transform, Loc.Number(Mathf.Abs(cost)), 30, cost > 0 ? UIKit.Gold : UIKit.Green, TextAnchor.MiddleCenter, true);
                UIKit.Stretch(label.rectTransform);
            }
            button.name = id;
            button.onClick.AddListener(() =>
            {
                AudioService.Play(Sfx.Click, 0.6f);
                if (pendingChoice == id)
                {
                    pendingChoice = null;
                    confirm();
                    return;
                }
                pendingChoice = id;
                preview();
                MarkPending(id);
            });
            return button;
        }

        void MarkPending(string id)
        {
            if (menu == null)
                return;
            foreach (var button in menu.GetComponentsInChildren<Button>())
            {
                var check = button.transform.Find("Confirm");
                if (check != null)
                    Object.Destroy(check.gameObject);
                if (button.name != id)
                    continue;
                var glyph = UIKit.Icon(button.transform, Icon.Check, 64f);
                glyph.name = "Confirm";
                glyph.color = UIKit.Green;
                UIKit.Center(glyph.rectTransform, new Vector2(48f, 48f), new Vector2(64f, 64f), false);
            }
        }

        void Tooltip(string titleDisplay, string bodyLogical)
        {
            if (menu == null)
                return;
            var old = menu.Find("Tooltip");
            if (old != null)
                Object.Destroy(old.gameObject);
            var panel = UIKit.Panel(menu, new Color(0.05f, 0.07f, 0.13f, 0.92f), "Tooltip");
            panel.raycastTarget = false;
            var title = UIKit.Label(panel.transform, titleDisplay, 34, UIKit.Gold, TextAnchor.MiddleCenter, true);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(560f, 50f), false);
            var body = UIKit.Paragraph(panel.transform, bodyLogical, 28, UIKit.Cream, 540f, TextAnchor.UpperCenter);
            UIKit.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -64f), body.rectTransform.sizeDelta, false);
            var height = 80f + body.rectTransform.sizeDelta.y;
            UIKit.Center(panel.rectTransform, new Vector2(0f, 190f + height / 2f), new Vector2(580f, height), false);
        }

        void RefreshMenuAffordability()
        {
            foreach (var button in menu.GetComponentsInChildren<Button>())
            {
                var cost = 0;
                var def = TowerDefs.Get(button.name);
                if (def != null)
                    cost = def.cost;
                button.GetComponent<Image>().color = cost > field.Coins ? new Color(0.35f, 0.3f, 0.3f) : UIKit.Lapis;
            }
        }

        void OpenBuildMenu(Slot slot)
        {
            var root = NewMenu(slot.Position);
            var offsets = new[] { new Vector2(-120f, 120f), new Vector2(120f, 120f), new Vector2(-120f, -120f), new Vector2(120f, -120f) };
            for (var i = 0; i < TowerDefs.Buildable.Length; i++)
            {
                var def = TowerDefs.Get(TowerDefs.Buildable[i]);
                Choice(root, offsets[i], def.id, ArtLibrary.Tower(def), null, def.cost, () =>
                {
                    if (field.Build(slot, def))
                        CloseMenu();
                    else
                        Root.Toast(Loc.T("battle.not_enough_coins"));
                }, () =>
                {
                    Tooltip(Loc.T(def.NameKey), Loc.Get(def.DescKey));
                    ShowRange(slot.Position, def.range, new Color(1f, 1f, 1f, 0.7f));
                });
            }
            RefreshMenuAffordability();
        }

        void OpenTowerMenu(Tower tower)
        {
            var root = NewMenu(tower.Position + new Vector2(0f, 0.4f));
            ShowRange(tower.Position, tower.Range, new Color(0.6f, 0.9f, 1f, 0.6f));
            Tooltip(Loc.T(tower.Def.NameKey), Loc.Get(tower.Def.DescKey));

            var upgrades = tower.Def.upgrades;
            for (var i = 0; i < upgrades.Length; i++)
            {
                var next = TowerDefs.Get(upgrades[i]);
                var offset = upgrades.Length == 1 ? new Vector2(0f, 150f) : new Vector2(i == 0 ? -125f : 125f, 125f);
                if (upgrades.Length > 1)
                    offset.y = 110f;
                Choice(root, offset, next.id, next.tier >= 4 ? ArtLibrary.Tower(next) : null, Icon.Upgrade, next.cost, () =>
                {
                    if (field.Upgrade(tower, next))
                        CloseMenu();
                    else
                        Root.Toast(Loc.T("battle.not_enough_coins"));
                }, () =>
                {
                    Tooltip(Loc.T(next.NameKey), Loc.Get(next.DescKey));
                    ShowRange(tower.Position, next.range, new Color(1f, 1f, 1f, 0.7f));
                });
            }
            if (upgrades.Length > 1)
                Tooltip(Loc.T(tower.Def.NameKey), Loc.Get("battle.choose_path"));

            Choice(root, new Vector2(0f, -150f), "sell", null, Icon.Sell, -tower.SellValue, () =>
            {
                field.Sell(tower);
                CloseMenu();
            }, () => Tooltip(Loc.T("battle.sell"), Loc.Format(Loc.Get("battle.sell_body"), tower.SellValue)));

            if (tower.Def.family == TowerFamily.Barracks)
            {
                Choice(root, new Vector2(150f, -20f), "rally", null, Icon.Flag, 0, () => { }, () =>
                {
                    CloseMenu();
                    rallyTower = tower;
                    targeting = Targeting.Rally;
                    ShowRange(tower.Position, tower.Range, new Color(1f, 0.85f, 0.4f, 0.8f));
                });
            }
            RefreshMenuAffordability();
        }

        // ------------------------------------------------------------ pause and speed

        void ToggleSpeed()
        {
            speed = speed > 1f ? 1f : 2f;
            if (!paused)
                Time.timeScale = speed;
            speedButton.GetComponent<Image>().color = speed > 1f ? UIKit.Gold : UIKit.LapisLight;
        }

        void Pause()
        {
            if (field.IsOver || paused)
                return;
            paused = true;
            Time.timeScale = 0f;
            CloseMenu();
            var popup = Popup.Open(Loc.T("battle.paused"), new Vector2(900f, 760f));
            popup.OnClosed += Resume;
            var y = 180f;
            Row(popup, Loc.T("battle.resume"), UIKit.Green, Icon.Play, y, popup.Close);
            Row(popup, Loc.T("battle.restart"), UIKit.LapisLight, Icon.Back, y - 140f, () =>
            {
                popup.Close();
                Root.Show(new BattleScreen(level, difficulty));
            });
            Row(popup, Loc.T(GameSettings.Sound ? "battle.sound_off" : "battle.sound_on"), UIKit.LapisLight, Icon.Sound, y - 280f, () =>
            {
                GameSettings.Sound = !GameSettings.Sound;
                GameSettings.Music = GameSettings.Sound;
                popup.Close();
            });
            Row(popup, Loc.T("battle.quit"), UIKit.Danger, Icon.Close, y - 420f, () =>
            {
                popup.Close();
                Root.ToMap();
            });
        }

        static void Row(Popup popup, string text, Color color, Icon icon, float y, Action action)
        {
            var button = UIKit.Button(popup.Window, text, action, color, 40, icon);
            UIKit.Center(button.GetComponent<RectTransform>(), new Vector2(0f, y), new Vector2(620f, 115f), false);
        }

        void Resume()
        {
            paused = false;
            if (field != null && !field.IsOver)
                Time.timeScale = speed;
        }

        public override void OnBack()
        {
            if (targeting != Targeting.None || menu != null)
            {
                SetTargeting(Targeting.None);
                return;
            }
            Pause();
        }

        // ------------------------------------------------------------ events

        void OnBoss(EnemyDef boss)
        {
            var banner = UIKit.Panel(hud, new Color(0.4f, 0.05f, 0.05f, 0.92f), "Boss");
            banner.raycastTarget = false;
            UIKit.Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1300f, 230f), false);
            var portrait = UIKit.SpriteImage(banner.transform, ArtLibrary.Character(boss.look), new Vector2(220f, 220f));
            UIKit.Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(220f, 220f));
            var name = UIKit.Label(banner.transform, Loc.T(boss.NameKey), 64, UIKit.Gold, TextAnchor.MiddleLeft, true);
            UIKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(270f, -20f), new Vector2(980f, 90f));
            var line = UIKit.Label(banner.transform, "«" + Loc.Get("boss." + boss.id) + "»", 40, UIKit.Cream, TextAnchor.MiddleLeft, false);
            line.text = Loc.Display(line.text);
            UIKit.Place(line.rectTransform, new Vector2(0f, 0f), new Vector2(270f, 30f), new Vector2(980f, 80f));
            banner.gameObject.AddComponent<FadeAway>().Seconds = 4f;
        }

        void OnEnded(bool won)
        {
            if (resultShown)
                return;
            CloseMenu();
            targeting = Targeting.None;
            if (won)
                Victory();
            else
                Defeat();
        }

        void Victory()
        {
            resultShown = true;
            Time.timeScale = 1f;
            SaveSystem.Data.kills += field.Kills;
            var stars = Progress.StarsFor(field.Lives, field.StartLives);
            var rewards = Progress.RecordVictory(level, stars);
            var popup = Popup.Open(Loc.T("battle.victory"), new Vector2(1300f, 880f), false);

            for (var i = 0; i < 3; i++)
            {
                var star = UIKit.Icon(popup.Window, i < stars ? Icon.Star : Icon.StarEmpty, 170f);
                UIKit.Center(star.rectTransform, new Vector2((i - 1) * 190f, 230f + (i == 1 ? 30f : 0f)), new Vector2(170f, 170f), false);
                if (i < stars)
                    star.gameObject.AddComponent<SlideIn>().From = new Vector2(0f, 300f + i * 120f);
            }

            var goldRow = UIKit.Amount(popup.Window, Icon.Coin, rewards.gold, 50, UIKit.Ink);
            UIKit.Center((RectTransform)goldRow.transform.parent, new Vector2(-300f, 30f), new Vector2(300f, 80f), false);
            var gemRow = UIKit.Amount(popup.Window, Icon.Gem, rewards.gems, 50, UIKit.Ink);
            UIKit.Center((RectTransform)gemRow.transform.parent, new Vector2(0f, 30f), new Vector2(300f, 80f), false);
            var xpRow = UIKit.Amount(popup.Window, Icon.Crown, rewards.passXp, 50, UIKit.Ink);
            UIKit.Center((RectTransform)xpRow.transform.parent, new Vector2(300f, 30f), new Vector2(300f, 80f), false);

            if (!string.IsNullOrEmpty(rewards.unlockedHero))
            {
                var hero = UIKit.Label(popup.Window, Loc.T("battle.hero_joined", Loc.Get("hero." + rewards.unlockedHero)), 40, UIKit.Purple, TextAnchor.MiddleCenter, true, false);
                UIKit.Center(hero.rectTransform, new Vector2(0f, -70f), new Vector2(1200f, 70f), false);
            }

            var doubled = false;
            Button doubleButton = null;
            doubleButton = UIKit.Button(popup.Window, Loc.T("battle.double_gold"), () =>
            {
                Monetization.WatchAd(ShopDefs.AdDoubleReward, watched =>
                {
                    if (!watched || doubled)
                        return;
                    doubled = true;
                    Economy.AddGold(rewards.gold);
                    goldRow.text = Loc.Number(rewards.gold * 2);
                    doubleButton.interactable = false;
                    AudioService.Play(Sfx.Reward, 1f);
                });
            }, UIKit.Purple, 36, Icon.Ad);
            doubleButton.interactable = Monetization.CanWatchAd(ShopDefs.AdDoubleReward);
            UIKit.Center(doubleButton.GetComponent<RectTransform>(), new Vector2(0f, -170f), new Vector2(600f, 110f), false);

            var replay = UIKit.Button(popup.Window, Loc.T("battle.replay"), () =>
            {
                popup.Close();
                Root.Show(new BattleScreen(level, difficulty));
            }, UIKit.LapisLight, 40, Icon.Back);
            UIKit.Center(replay.GetComponent<RectTransform>(), new Vector2(-250f, -320f), new Vector2(420f, 115f));
            var next = UIKit.Button(popup.Window, Loc.T("battle.continue"), () =>
            {
                popup.Close();
                ContinueAfterVictory(rewards.firstWin);
            }, UIKit.Green, 44, Icon.Play);
            UIKit.Center(next.GetComponent<RectTransform>(), new Vector2(250f, -320f), new Vector2(420f, 115f));
        }

        void ContinueAfterVictory(bool firstWin)
        {
            var chapter = LevelDefs.ChapterOf(level);
            var last = chapter.levels[chapter.levels.Length - 1] == level.id;
            if (last && firstWin)
                Root.PlayCutscene(chapter.outro, Root.ToMap);
            else
                Root.ToMap();
        }

        void Defeat()
        {
            resultShown = true;
            var popup = Popup.Open(Loc.T("battle.defeat"), new Vector2(1100f, 820f), false);
            var text = UIKit.Paragraph(popup.Window, Loc.Get("battle.defeat_body"), 40, UIKit.Ink, 900f, TextAnchor.UpperCenter);
            UIKit.Center(text.rectTransform, new Vector2(0f, 230f), text.rectTransform.sizeDelta, false);

            if (!field.Revived)
            {
                var ad = UIKit.Button(popup.Window, Loc.T("battle.revive_ad", ShopDefs.ReviveLives), () =>
                {
                    Monetization.WatchAd(ShopDefs.AdRevive, watched =>
                    {
                        if (!watched)
                            return;
                        popup.Close();
                        Revive();
                    });
                }, UIKit.Purple, 36, Icon.Ad);
                ad.interactable = Monetization.CanWatchAd(ShopDefs.AdRevive);
                UIKit.Center(ad.GetComponent<RectTransform>(), new Vector2(0f, 80f), new Vector2(760f, 110f), false);

                var gems = UIKit.Button(popup.Window, Loc.T("battle.revive_gems", ShopDefs.ReviveLives, ShopDefs.ReviveGems), () =>
                {
                    if (!Economy.TrySpendGems(ShopDefs.ReviveGems))
                    {
                        Root.Toast(Loc.T("shop.not_enough_gems"));
                        return;
                    }
                    popup.Close();
                    Revive();
                }, UIKit.Turquoise, 36, Icon.Gem);
                UIKit.Center(gems.GetComponent<RectTransform>(), new Vector2(0f, -45f), new Vector2(760f, 110f), false);
            }

            var retry = UIKit.Button(popup.Window, Loc.T("battle.retry"), () =>
            {
                popup.Close();
                Root.Show(new BattleScreen(level, difficulty));
            }, UIKit.Green, 40, Icon.Back);
            UIKit.Center(retry.GetComponent<RectTransform>(), new Vector2(-220f, -230f), new Vector2(400f, 115f));
            var quit = UIKit.Button(popup.Window, Loc.T("battle.to_map"), () =>
            {
                popup.Close();
                Root.ToMap();
            }, UIKit.Danger, 40);
            UIKit.Center(quit.GetComponent<RectTransform>(), new Vector2(220f, -230f), new Vector2(400f, 115f));
        }

        void Revive()
        {
            resultShown = false;
            field.Revive();
            Time.timeScale = speed;
            AudioService.Play(Sfx.Reward, 1f);
        }
    }

    /// <summary>Reports taps on a full-screen transparent image (taps on the battlefield).</summary>
    public class TapCatcher : MonoBehaviour, IPointerClickHandler
    {
        public Action<Vector2> OnTap;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (OnTap != null && !eventData.dragging)
                OnTap(eventData.position);
        }
    }

    /// <summary>Calls a method every frame (screens are plain classes, not MonoBehaviours).</summary>
    public class Driver : MonoBehaviour
    {
        public Action Tick;

        void Update()
        {
            if (Tick != null)
                Tick();
        }
    }
}
