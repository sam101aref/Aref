using IranVsTuran.Art;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace IranVsTuran.UI
{
    /// <summary>The heroes of the Shahnameh: choose one, unlock with gems, train with gold, change skins.</summary>
    public class HeroesScreen : ScreenBase
    {
        protected override void Build()
        {
            Background(ArtLibrary.Backdrop(Backdrop.Steppe), new Color(0.55f, 0.55f, 0.6f));
            TopBar("heroes.title", () => Root.ToMap());

            var area = UIKit.Rect(Rect, "Cards");
            area.anchorMin = new Vector2(0f, 0f);
            area.anchorMax = new Vector2(1f, 1f);
            area.offsetMin = new Vector2(30f, 30f);
            area.offsetMax = new Vector2(-30f, -140f);
            var content = UIKit.ScrollRow(area, 30f);
            foreach (var hero in HeroDefs.All)
                Card(content, hero);
        }

        void Card(RectTransform parent, HeroDef hero)
        {
            var save = SaveSystem.Data;
            var owned = save.OwnsHero(hero.id);
            var selected = Heroes.Selected == hero;
            var level = Heroes.Level(hero.id);

            var card = UIKit.Panel(parent, selected ? UIKit.Gold : UIKit.Parchment, "Hero " + hero.id);
            UIKit.Size(card, 430f, 880f);

            var portrait = UIKit.SpriteImage(card.transform, ArtLibrary.Character(HeroDefs.LookFor(hero, Heroes.Skin(hero.id)), 256), new Vector2(330f, 330f));
            UIKit.Place(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(330f, 330f), false);
            if (!owned)
                portrait.color = new Color(0.3f, 0.3f, 0.3f);

            var name = UIKit.Label(card.transform, Loc.T(hero.NameKey), 50, UIKit.Ink, TextAnchor.MiddleCenter, true, false);
            UIKit.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(400f, 70f), false);
            if (owned)
            {
                var lv = UIKit.Label(card.transform, Loc.T("heroes.level", level, HeroDefs.MaxLevel), 32, UIKit.Muted, TextAnchor.MiddleCenter, true, false);
                UIKit.Place(lv.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(400f, 50f), false);
            }

            var ability = UIKit.Paragraph(card.transform, Loc.Get(hero.AbilityKey), 28, UIKit.Ink, 370f, TextAnchor.UpperCenter);
            UIKit.Place(ability.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -455f), ability.rectTransform.sizeDelta, false);

            var multiplier = HeroDefs.LevelMultiplier(level);
            var stats = Loc.T("heroes.stats", Mathf.RoundToInt(hero.hp * multiplier),
                Mathf.RoundToInt(hero.damageMin * multiplier) + "-" + Mathf.RoundToInt(hero.damageMax * multiplier));
            var statsLabel = UIKit.Label(card.transform, stats, 28, UIKit.Muted, TextAnchor.MiddleCenter, false, false);
            UIKit.Place(statsLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 290f), new Vector2(400f, 50f), false);

            // skins
            var skins = Heroes.SkinsOf(hero);
            for (var i = 0; i < skins.Count; i++)
                SkinButton(card.transform, hero, skins[i], i, owned);

            if (owned)
            {
                var select = UIKit.Button(card.transform, Loc.T(selected ? "heroes.selected" : "heroes.select"), () =>
                {
                    Heroes.Select(hero);
                    Rebuild();
                }, selected ? UIKit.Muted : UIKit.Green, 36, selected ? Icon.Check : (Icon?)null);
                select.interactable = !selected;
                UIKit.Place(select.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(380f, 96f), false);

                if (level < HeroDefs.MaxLevel)
                {
                    var cost = HeroDefs.TrainCost(level);
                    var train = UIKit.Button(card.transform, Loc.T("heroes.train", cost), () =>
                    {
                        if (Heroes.Train(hero))
                            Rebuild();
                        else
                            Root.Toast(Loc.T("shop.not_enough_gold"));
                    }, UIKit.LapisLight, 32, Icon.Coin);
                    UIKit.Place(train.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(380f, 96f), false);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(hero.unlockLevel))
                {
                    var hint = UIKit.Label(card.transform, Loc.T("heroes.unlock_level", LevelDefs.Get(hero.unlockLevel).index + 1), 28, UIKit.Muted,
                        TextAnchor.MiddleCenter, false, false);
                    UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(400f, 50f), false);
                }
                var buy = UIKit.Button(card.transform, Loc.Number(hero.gemPrice), () =>
                {
                    Popup.Confirm(Loc.T(hero.NameKey), Loc.Format(Loc.Get("heroes.buy_confirm"), Loc.Get(hero.NameKey), hero.gemPrice), Loc.T("store.buy"), () =>
                    {
                        if (Heroes.Buy(hero))
                        {
                            Heroes.Select(hero);
                            Rebuild();
                        }
                        else
                            Root.Toast(Loc.T("shop.not_enough_gems"));
                    });
                }, UIKit.Purple, 40, Icon.Gem);
                UIKit.Place(buy.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(380f, 100f), false);
            }
        }

        void SkinButton(Transform card, HeroDef hero, SkinDef skin, int index, bool heroOwned)
        {
            var owned = Heroes.OwnsSkin(skin);
            var wearing = Heroes.Skin(hero.id) == skin.id;
            var frame = UIKit.Panel(card, wearing ? UIKit.Turquoise : new Color(0f, 0f, 0f, 0.2f), "Skin");
            UIKit.Place(frame.rectTransform, new Vector2(0f, 0f), new Vector2(25f + index * 125f, 350f), new Vector2(115f, 115f), false);
            var image = UIKit.SpriteImage(frame.transform, ArtLibrary.Character(HeroDefs.LookFor(hero, skin.id)), new Vector2(110f, 110f));
            UIKit.Stretch(image.rectTransform, 4f);
            if (!owned)
            {
                image.color = new Color(0.45f, 0.45f, 0.45f);
                var tag = skin.gemPrice > 0 ? UIKit.Icon(frame.transform, Icon.Gem, 40f) : UIKit.Icon(frame.transform, Icon.Crown, 40f);
                UIKit.Place(tag.rectTransform, new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(40f, 40f), false);
            }
            var button = frame.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (owned)
                {
                    if (!heroOwned)
                        return;
                    Heroes.Wear(hero, wearing ? null : skin);
                    Rebuild();
                }
                else if (skin.gemPrice > 0)
                {
                    Popup.Confirm(Loc.T(skin.NameKey), Loc.Format(Loc.Get("heroes.skin_confirm"), Loc.Get(skin.NameKey), skin.gemPrice), Loc.T("store.buy"), () =>
                    {
                        if (Heroes.BuySkin(skin))
                        {
                            Heroes.Wear(hero, skin);
                            Rebuild();
                        }
                        else
                            Root.Toast(Loc.T("shop.not_enough_gems"));
                    });
                }
                else
                {
                    Root.Toast(Loc.T("heroes.skin_pass", Loc.Get(skin.NameKey)));
                }
            });
        }

        public override void OnBack()
        {
            Root.ToMap();
        }
    }
}
