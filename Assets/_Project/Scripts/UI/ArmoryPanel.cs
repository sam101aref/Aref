using System;
using System.Linq;
using Arash.Art;
using Arash.Core;
using Arash.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Arash.UI
{
    /// <summary>
    /// The shop (F-55 to F-59): tabs for bows, armour, helmets, shields, quivers and bow slots, and
    /// outfits. Each row shows the item, what it does, and a button to buy it (coins or gems), wear
    /// it or take it off. Items the story has not reached yet show a lock and the chapter that
    /// opens them.
    /// </summary>
    public static class ArmoryPanel
    {
        static readonly ItemCategory[] Tabs =
        {
            ItemCategory.Bow, ItemCategory.Armor, ItemCategory.Helmet, ItemCategory.Shield, ItemCategory.Quiver, ItemCategory.Outfit,
        };

        static ItemCategory s_Tab = ItemCategory.Bow;

        public static GameObject Open(RectTransform canvas, Action onClosed)
        {
            var overlay = UIFactory.Overlay(canvas, "Shop");
            Build(overlay, onClosed);
            return overlay.gameObject;
        }

        static void Build(RectTransform overlay, Action onClosed)
        {
            for (var i = overlay.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(overlay.GetChild(i).gameObject);

            var save = SaveSystem.Data;
            Action refresh = () => Build(overlay, onClosed);

            var window = UIFactory.Window(overlay);
            UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1780f, 1020f));

            var title = UIFactory.Label(window, Loc.T("ui.armory"), 60, UIFactory.Gold, TextAnchor.MiddleLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(60f, -30f), new Vector2(600f, 90f));
            Wallet(window, "gem", save.gems, -60f);
            Wallet(window, "coin", save.coins, -340f);

            for (var i = 0; i < Tabs.Length; i++)
            {
                var tab = Tabs[i];
                var button = UIFactory.Button(window, Loc.T("shop.tab." + tab.ToString().ToLowerInvariant()), () =>
                {
                    s_Tab = tab;
                    refresh();
                }, tab == s_Tab ? UIFactory.Gold : UIFactory.LapisLight, 32);
                UIFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(60f + i * 278f, -135f), new Vector2(262f, 84f));
            }

            var area = UIFactory.Rect(window, "Items");
            UIFactory.Stretch(area);
            area.offsetMin = new Vector2(60f, 150f);
            area.offsetMax = new Vector2(-60f, -240f);
            var content = UIFactory.ScrollColumn(area, 14f);

            if (s_Tab == ItemCategory.Bow)
            {
                var slots = UIFactory.Label(content, Loc.T("shop.slots", save.equippedBows.Count, Armory.Slots(save)), 34, UIFactory.Cream, TextAnchor.MiddleLeft);
                UIFactory.Height(slots, 56f);
            }
            var items = Armory.InCategory(s_Tab).ToList();
            if (s_Tab == ItemCategory.Quiver)
                items.AddRange(Armory.InCategory(ItemCategory.Slot));
            foreach (var item in items)
                Row(content, item, save, refresh);

            var close = UIFactory.Button(window, Loc.T("ui.close"), () =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                if (onClosed != null)
                    onClosed();
            }, UIFactory.Gold, 44);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(380f, 100f));
        }

        static void Wallet(RectTransform window, string icon, int amount, float x)
        {
            var image = UIFactory.Icon(window, icon, 70f);
            UIFactory.Place(image.rectTransform, new Vector2(1f, 1f), new Vector2(x, -40f), new Vector2(70f, 70f));
            var label = UIFactory.Label(window, Loc.Number(amount), 46, UIFactory.Cream, TextAnchor.MiddleRight, true);
            UIFactory.Place(label.rectTransform, new Vector2(1f, 1f), new Vector2(x - 80f, -40f), new Vector2(190f, 70f));
        }

        static void Row(RectTransform content, ShopItem item, SaveData save, Action refresh)
        {
            var available = Armory.IsAvailable(save, item);
            var row = UIFactory.Panel(content, item.Id, Color.black);
            UIFactory.Skin(row, ArtLibrary.UI("panel"));
            UIFactory.Height(row, 150f);

            var picture = Picture(row.transform, item);
            UIFactory.Place(picture.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(120f, 120f));
            if (!available)
                picture.color = new Color(0.25f, 0.25f, 0.3f, 0.9f);

            var nameLabel = UIFactory.Label(row.transform, Loc.T(item.NameKey), 40, available ? UIFactory.Gold : UIFactory.Muted, TextAnchor.MiddleLeft, true);
            UIFactory.Place(nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(180f, -14f), new Vector2(1000f, 56f));
            var text = available ? Loc.T(item.DescriptionKey) + "   " + Stats(item) : UnlockText(item);
            var description = UIFactory.Label(row.transform, text, 30, UIFactory.Cream, TextAnchor.MiddleLeft);
            UIFactory.Place(description.rectTransform, new Vector2(0f, 1f), new Vector2(180f, -78f), new Vector2(1150f, 50f));

            var action = ActionFor(item, save, refresh);
            var button = UIFactory.Button(row.transform, action.Label, action.OnClick, action.Color, 34);
            button.interactable = action.OnClick != null;
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(300f, 100f));
            if (action.Icon != null)
            {
                var label = button.GetComponentInChildren<Text>();
                label.rectTransform.offsetMin = new Vector2(Loc.IsRtl ? 10f : 70f, 8f);
                label.rectTransform.offsetMax = new Vector2(Loc.IsRtl ? -70f : -10f, 0f);
                var icon = UIFactory.Icon(button.transform, action.Icon, 56f);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(Loc.IsRtl ? 1f : 0f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(Loc.IsRtl ? -46f : 46f, 3f);
            }
        }

        static Image Picture(Transform parent, ShopItem item)
        {
            switch (item.Category)
            {
                case ItemCategory.Bow:
                    var bow = UIFactory.Icon(parent, "bow", 120f);
                    if (item.Trail.a > 0f)
                    {
                        var trail = item.Trail;
                        trail.a = 1f;
                        var glow = UIFactory.Circle(bow.transform, trail, 40f);
                        glow.rectTransform.anchoredPosition = new Vector2(-38f, -38f);
                    }
                    return bow;
                case ItemCategory.Helmet:
                case ItemCategory.Shield:
                    var image = UIFactory.Icon(parent, "helmet", 120f);
                    var sprite = item.Category == ItemCategory.Shield ? ArtLibrary.Prop(item.Sprite) : ArtLibrary.Get(item.Sprite);
                    if (sprite != null)
                        image.sprite = sprite;
                    return image;
                case ItemCategory.Armor:
                    return UIFactory.Icon(parent, "armor", 120f);
                case ItemCategory.Quiver:
                    return UIFactory.Icon(parent, "quiver", 120f);
                case ItemCategory.Slot:
                    return UIFactory.Icon(parent, "plus", 120f);
                default:
                    var outfit = UIFactory.Icon(parent, "outfit", 120f);
                    outfit.color = Color.Lerp(item.Tunic, Color.white, 0.15f);
                    return outfit;
            }
        }

        /// <summary>The numbers that matter for the item, on one line.</summary>
        static string Stats(ShopItem item)
        {
            switch (item.Category)
            {
                case ItemCategory.Bow:
                    return Loc.T("shop.stats.bow", Mathf.RoundToInt(item.Damage * 100f), item.Ammo);
                case ItemCategory.Armor:
                    return Loc.T("shop.stats.armor", Mathf.RoundToInt(item.ResistBody * 100f), Mathf.RoundToInt(item.HealthBonus * 100f));
                case ItemCategory.Helmet:
                    return Loc.T("shop.stats.helmet", Mathf.RoundToInt(item.ResistHead * 100f));
                case ItemCategory.Shield:
                    return Loc.T("shop.stats.shield", Mathf.RoundToInt(item.Durability));
                case ItemCategory.Quiver:
                    return Loc.T("shop.stats.quiver", Mathf.RoundToInt(item.AmmoBonus * 100f));
                default:
                    return string.Empty;
            }
        }

        /// <summary>"Opens in chapter …" from the required level id (ch{chapter}_{level}).</summary>
        static string UnlockText(ShopItem item)
        {
            var id = item.RequiresLevel ?? string.Empty;
            var underscore = id.IndexOf('_');
            int chapter;
            if (id.StartsWith("ch") && underscore > 2 && int.TryParse(id.Substring(2, underscore - 2), out chapter))
                return Loc.T("shop.unlocks", Loc.Get("chapter." + chapter + ".title"));
            return Loc.T("armory.locked");
        }

        struct RowAction
        {
            public string Label;
            public string Icon;
            public Action OnClick;
            public Color Color;
        }

        static RowAction ActionFor(ShopItem item, SaveData save, Action refresh)
        {
            if (!Armory.IsAvailable(save, item))
                return new RowAction { Label = Loc.T("armory.locked"), Icon = "lock", Color = UIFactory.Muted };

            if (!Armory.IsOwned(save, item))
            {
                var gems = item.Currency == Currency.Gems;
                var affordable = gems ? save.gems >= item.Price : save.coins >= item.Price;
                Action buy = () =>
                {
                    if (Armory.TryBuy(save, item) == PurchaseResult.Bought)
                    {
                        SaveSystem.Save();
                        Audio.AudioService.Play(Audio.Sfx.FarrReady, 0.6f);
                        Telemetry.Event("shop_buy", "item", item.Id, "price", item.Price, "currency", item.Currency.ToString());
                    }
                    refresh();
                };
                return new RowAction
                {
                    Label = Loc.Number(item.Price),
                    Icon = gems ? "gem" : "coin",
                    OnClick = affordable ? buy : null,
                    Color = affordable ? UIFactory.Turquoise : UIFactory.Muted,
                };
            }

            if (item.Category == ItemCategory.Quiver || item.Category == ItemCategory.Slot)
                return new RowAction { Label = Loc.T("shop.owned"), Icon = "check", Color = UIFactory.Muted };

            Action toggle = () =>
            {
                Armory.Equip(save, item);
                SaveSystem.Save();
                refresh();
            };
            if (Armory.IsEquipped(save, item))
            {
                // Bows and outfits always keep one equipped; other gear can be taken off.
                var canRemove = item.Category != ItemCategory.Outfit && !(item.Category == ItemCategory.Bow && save.equippedBows.Count <= 1);
                return new RowAction
                {
                    Label = Loc.T(canRemove ? "shop.remove" : "armory.equipped"),
                    Icon = canRemove ? null : "check",
                    Color = UIFactory.Gold,
                    OnClick = canRemove ? toggle : null,
                };
            }
            return new RowAction { Label = Loc.T("armory.equip"), Color = UIFactory.LapisLight, OnClick = toggle };
        }
    }
}
