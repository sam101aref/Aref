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
    /// The bazaar: gem packs and offers for real money, gold and items for gems, and free gold
    /// and gems for watching ads.
    /// </summary>
    public class ShopScreen : ScreenBase
    {
        enum Tab
        {
            Offers,
            Gems,
            Gold,
            Items,
            Free,
        }

        static Tab tab = Tab.Offers;
        RectTransform content;

        protected override void Build()
        {
            Background(ArtLibrary.Backdrop(Backdrop.Palace), new Color(0.45f, 0.42f, 0.5f));
            TopBar("shop.title", () => Root.ToMap());

            var tabs = UIKit.Rect(Rect, "Tabs");
            UIKit.Place(tabs, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1500f, 100f), false);
            var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.reverseArrangement = Loc.IsRtl;
            foreach (Tab t in System.Enum.GetValues(typeof(Tab)))
            {
                var current = t;
                var button = UIKit.Button(tabs, Loc.T("shop.tab." + t.ToString().ToLowerInvariant()), () =>
                {
                    tab = current;
                    Rebuild();
                }, t == tab ? UIKit.Gold : UIKit.LapisLight, 34);
                if (t == Tab.Free && Monetization.AdsLeftToday(ShopDefs.AdFreeGold) + Monetization.AdsLeftToday(ShopDefs.AdFreeGems) > 0)
                    MainMenuScreen.Badge(button.transform, 1);
            }

            var area = UIKit.Rect(Rect, "Area");
            area.anchorMin = new Vector2(0f, 0f);
            area.anchorMax = new Vector2(1f, 1f);
            area.offsetMin = new Vector2(30f, 40f);
            area.offsetMax = new Vector2(-30f, -280f);
            content = UIKit.ScrollRow(area, 30f);

            switch (tab)
            {
                case Tab.Offers:
                    ProductCard(ShopDefs.Product(ShopDefs.StarterPack), Icon.Chest);
                    ProductCard(ShopDefs.Product(ShopDefs.BattlePassProduct), Icon.Crown);
                    break;
                case Tab.Gems:
                    foreach (var product in ShopDefs.Products)
                        if (product.type == ProductType.Consumable)
                            ProductCard(product, Icon.Gem);
                    break;
                case Tab.Gold:
                    foreach (var offer in ShopDefs.GoldOffers)
                        GoldCard(offer);
                    break;
                case Tab.Items:
                    foreach (var item in ItemDefs.All)
                        ItemCard(item);
                    break;
                case Tab.Free:
                    AdCard(ShopDefs.AdFreeGold);
                    AdCard(ShopDefs.AdFreeGems);
                    break;
            }
        }

        Image Card(string titleDisplay, Icon icon, Color color)
        {
            var card = UIKit.Panel(content, color, "Card");
            UIKit.Size(card, 400f, 620f);
            var title = UIKit.Label(card.transform, titleDisplay, 40, UIKit.Ink, TextAnchor.MiddleCenter, true, false);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(380f, 70f), false);
            var glyph = UIKit.Icon(card.transform, icon, 190f);
            UIKit.Place(glyph.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(190f, 190f), false);
            return card;
        }

        void Ribbon(Transform card, string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            var ribbon = UIKit.Panel(card, UIKit.Danger, "Ribbon");
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 24f), new Vector2(260f, 56f), false);
            var label = UIKit.Label(ribbon.transform, Loc.T(key), 30, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(label.rectTransform);
        }

        void ProductCard(ProductDef product, Icon icon)
        {
            var owned = Monetization.Owns(product);
            var card = Card(Loc.T(product.NameKey), icon, product.type == ProductType.Consumable ? UIKit.Parchment : Raster.Hex(0xF4D98A));
            Ribbon(card.transform, product.badgeKey);

            if (product.id == ShopDefs.BattlePassProduct)
            {
                var text = UIKit.Paragraph(card.transform, Loc.Get("shop.pass_desc"), 28, UIKit.Ink, 340f, TextAnchor.UpperCenter);
                UIKit.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -320f), text.rectTransform.sizeDelta, false);
            }
            else
            {
                // what's inside
                for (var i = 0; i < product.rewards.Length; i++)
                {
                    var badge = UIKit.RewardBadge(card.transform, product.rewards[i], 100f);
                    var columns = Mathf.Min(3, product.rewards.Length);
                    var x = (i % 3 - (columns - 1) / 2f) * 115f;
                    UIKit.Place(badge, new Vector2(0.5f, 1f), new Vector2(x, -320f - (i / 3) * 110f), new Vector2(100f, 100f), false);
                }
            }

            var buy = UIKit.Button(card.transform, owned ? Loc.T("shop.owned") : Monetization.Price(product), () =>
            {
                Monetization.Buy(product, success =>
                {
                    if (success)
                    {
                        AudioService.Play(Sfx.Reward, 1f);
                        Root.Toast(Loc.T("shop.thanks"));
                        Rebuild();
                    }
                });
            }, owned ? UIKit.Muted : UIKit.Green, 40);
            buy.interactable = !owned;
            UIKit.Place(buy.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(330f, 100f), false);
        }

        void GoldCard(GoldOffer offer)
        {
            var card = Card(Loc.T("reward.gold", offer.gold), Icon.Coin, UIKit.Parchment);
            var buy = UIKit.Button(card.transform, Loc.Number(offer.gems), () =>
            {
                if (Economy.BuyGold(offer))
                {
                    AudioService.Play(Sfx.Coin, 1f);
                    Root.Toast(Loc.T("reward.got", Loc.Get("reward.gold_word"), offer.gold));
                }
                else
                    Root.Toast(Loc.T("shop.not_enough_gems"));
            }, UIKit.Purple, 40, Icon.Gem);
            UIKit.Place(buy.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(330f, 100f), false);
        }

        void ItemCard(ItemDef item)
        {
            var card = Card(Loc.T(item.NameKey), UIKit.ItemIcon(item.id), UIKit.Parchment);
            var desc = UIKit.Paragraph(card.transform, Loc.Get(item.DescKey), 28, UIKit.Ink, 340f, TextAnchor.UpperCenter);
            UIKit.Place(desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -320f), desc.rectTransform.sizeDelta, false);
            var owned = UIKit.Label(card.transform, Loc.T("shop.have", Economy.ItemCount(item.id)), 30, UIKit.Muted, TextAnchor.MiddleCenter, true, false);
            UIKit.Place(owned.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(360f, 50f), false);
            var buy = UIKit.Button(card.transform, Loc.Number(item.gemPrice), () =>
            {
                if (Economy.BuyItem(item))
                {
                    AudioService.Play(Sfx.Reward, 0.8f);
                    Rebuild();
                }
                else
                    Root.Toast(Loc.T("shop.not_enough_gems"));
            }, UIKit.Purple, 40, Icon.Gem);
            UIKit.Place(buy.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(330f, 100f), false);
        }

        void AdCard(string placementId)
        {
            var placement = ShopDefs.Ad(placementId);
            var card = Card(UIKit.RewardName(placement.reward), UIKit.IconFor(placement.reward), UIKit.Parchment);
            var left = Monetization.AdsLeftToday(placementId);
            var info = UIKit.Label(card.transform, Loc.T("shop.ads_left", left, placement.dailyLimit), 30, UIKit.Muted, TextAnchor.MiddleCenter, true, false);
            UIKit.Place(info.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(360f, 50f), false);
            var watch = UIKit.Button(card.transform, Loc.T("shop.watch"), () =>
            {
                Monetization.WatchAd(placementId, watched =>
                {
                    if (watched)
                    {
                        AudioService.Play(Sfx.Reward, 1f);
                        Root.Toast(UIKit.RewardName(placement.reward));
                    }
                    Rebuild();
                });
            }, left > 0 ? UIKit.Green : UIKit.Muted, 38, Icon.Ad);
            watch.interactable = Monetization.CanWatchAd(placementId);
            UIKit.Place(watch.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(330f, 100f), false);
        }

        public override void OnBack()
        {
            Root.ToMap();
        }
    }
}
