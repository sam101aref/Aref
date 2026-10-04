using System;
using System.Linq;
using Arash.Core;
using Arash.Localization;
using UnityEngine;

namespace Arash.UI
{
    /// <summary>
    /// The armory (F-33, F-34): tabs for bows, upgrades, special arrows and outfits. Each row shows
    /// the item, what it does, and a button to buy it (with coins) or equip it.
    /// </summary>
    public static class ArmoryPanel
    {
        static ArmoryCategory s_Tab = ArmoryCategory.Bow;

        public static GameObject Open(RectTransform canvas, Action onClosed)
        {
            var overlay = UIFactory.Overlay(canvas, "Armory");
            Build(overlay, onClosed);
            return overlay.gameObject;
        }

        static void Build(RectTransform overlay, Action onClosed)
        {
            for (var i = overlay.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(overlay.GetChild(i).gameObject);

            var save = SaveSystem.Data;
            Action refresh = () => Build(overlay, onClosed);

            var window = UIFactory.Panel(overlay, "Window", UIFactory.LapisLight).rectTransform;
            UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 960f));

            var title = UIFactory.Label(window, Loc.T("ui.armory"), 60, UIFactory.Gold, TextAnchor.MiddleLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(50f, -25f), new Vector2(700f, 90f));
            var coins = UIFactory.Label(window, Loc.T("armory.coins", save.coins), 44, UIFactory.Cream, TextAnchor.MiddleRight, true);
            UIFactory.Place(coins.rectTransform, new Vector2(1f, 1f), new Vector2(-50f, -25f), new Vector2(600f, 90f));

            var tabs = new[] { ArmoryCategory.Bow, ArmoryCategory.Upgrade, ArmoryCategory.SpecialArrow, ArmoryCategory.Outfit };
            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                var button = UIFactory.Button(window, Loc.T("armory.tab." + tab.ToString().ToLowerInvariant()), () =>
                {
                    s_Tab = tab;
                    refresh();
                }, tab == s_Tab ? UIFactory.Gold : UIFactory.Lapis, 36);
                UIFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(50f + i * 300f, -130f), new Vector2(280f, 80f));
            }

            var area = UIFactory.Rect(window, "Items");
            UIFactory.Stretch(area);
            area.offsetMin = new Vector2(50f, 150f);
            area.offsetMax = new Vector2(-50f, -240f);
            var content = UIFactory.ScrollColumn(area, 14f);
            foreach (var item in Armory.InCategory(s_Tab))
                Row(content, item, save, refresh);

            var close = UIFactory.Button(window, Loc.T("ui.close"), () =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                if (onClosed != null)
                    onClosed();
            }, UIFactory.Gold, 44);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(380f, 95f));
        }

        static void Row(RectTransform content, ArmoryItem item, SaveData save, Action refresh)
        {
            var row = UIFactory.Panel(content, item.Id, new Color(0f, 0f, 0f, 0.25f));
            UIFactory.Height(row, 130f);

            if (item.Category == ArmoryCategory.Outfit)
            {
                var swatch = UIFactory.Circle(row.transform, item.Tunic, 90f);
                UIFactory.Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(90f, 90f));
            }
            var textX = item.Category == ArmoryCategory.Outfit ? 140f : 30f;

            var name = Loc.Get(item.NameKey);
            if (item.Category == ArmoryCategory.Upgrade)
                name += "  " + Loc.Number(save.UpgradeLevel(item.Id)) + "/" + Loc.Number(item.MaxLevel);
            var nameLabel = UIFactory.Label(row.transform, Loc.Display(name), 40, UIFactory.Gold, TextAnchor.MiddleLeft, true);
            UIFactory.Place(nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(textX, -12f), new Vector2(900f, 55f));
            var description = UIFactory.Label(row.transform, Loc.T(item.DescriptionKey), 30, UIFactory.Cream, TextAnchor.MiddleLeft);
            UIFactory.Place(description.rectTransform, new Vector2(0f, 1f), new Vector2(textX, -68f), new Vector2(1000f, 50f));

            var action = ActionFor(item, save, refresh);
            var button = UIFactory.Button(row.transform, action.Label, action.OnClick, action.Color, 34);
            button.interactable = action.OnClick != null;
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(330f, 95f));
        }

        struct RowAction
        {
            public string Label;
            public Action OnClick;
            public Color Color;
        }

        static RowAction ActionFor(ArmoryItem item, SaveData save, Action refresh)
        {
            if (!Armory.IsAvailable(save, item))
                return new RowAction { Label = Loc.T("armory.locked"), Color = UIFactory.Muted };

            var price = Armory.NextPrice(save, item);
            if (price >= 0)
            {
                Action buy = () =>
                {
                    if (Armory.TryBuy(save, item) == PurchaseResult.Bought)
                    {
                        if (item.Category != ArmoryCategory.Upgrade)
                            Armory.Equip(save, item);
                        SaveSystem.Save();
                        Telemetry.Event("armory_buy", "item", item.Id, "price", price);
                    }
                    refresh();
                };
                var affordable = save.coins >= price;
                return new RowAction
                {
                    Label = Loc.T("armory.buy", price),
                    OnClick = affordable ? buy : null,
                    Color = affordable ? UIFactory.Turquoise : UIFactory.Muted,
                };
            }

            if (item.Category == ArmoryCategory.Upgrade)
                return new RowAction { Label = Loc.T("armory.maxed"), Color = UIFactory.Muted };
            if (Armory.IsEquipped(save, item))
                return new RowAction { Label = Loc.T("armory.equipped"), Color = UIFactory.Gold };

            return new RowAction
            {
                Label = Loc.T("armory.equip"),
                Color = UIFactory.LapisLight,
                OnClick = () =>
                {
                    Armory.Equip(save, item);
                    SaveSystem.Save();
                    refresh();
                },
            };
        }
    }
}
