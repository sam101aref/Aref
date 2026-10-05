using System;
using System.Collections.Generic;
using IranVsTuran.Art;
using IranVsTuran.Audio;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IranVsTuran.UI
{
    /// <summary>
    /// Builds the game's uGUI in code: canvases, rounded panels, labels in Vazirmatn with Persian
    /// shaping, buttons with icons, currency bars and popups. <see cref="Place"/> mirrors anchors
    /// in Persian so every screen is laid out once for both reading directions.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Lapis = Raster.Hex(0x13213F);
        public static readonly Color LapisLight = Raster.Hex(0x22365F);
        public static readonly Color Gold = Raster.Hex(0xE6B852);
        public static readonly Color Turquoise = Raster.Hex(0x1FA2A6);
        public static readonly Color Cream = Raster.Hex(0xFAF0DA);
        public static readonly Color Parchment = Raster.Hex(0xF2E2B8);
        public static readonly Color Ink = Raster.Hex(0x2A1E14);
        public static readonly Color Danger = Raster.Hex(0xC0392B);
        public static readonly Color Green = Raster.Hex(0x3E9B4F);
        public static readonly Color Purple = Raster.Hex(0x6A2A7A);
        public static readonly Color Muted = Raster.Hex(0x6A6A72);
        public static readonly Color Shade = new Color(0f, 0f, 0f, 0.7f);

        public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        public static RectTransform Canvas(Transform parent, string name, int sortingOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 1f; // keep heights constant; wide phones get more width
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return rect;
        }

        /// <summary>Anchor (0–1) and offset in reference pixels; mirrored horizontally in Persian.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, bool mirror = true)
        {
            if (mirror && Loc.IsRtl)
            {
                anchor.x = 1f - anchor.x;
                position.x = -position.x;
            }
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Places a rect by its centre, relative to its parent's centre (mirrored in Persian).</summary>
        public static RectTransform Center(RectTransform rect, Vector2 position, Vector2 size, bool mirror = true)
        {
            if (mirror && Loc.IsRtl)
                position.x = -position.x;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Image(Transform parent, string name, Color color)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>A rounded panel (9-sliced) in <paramref name="color"/>.</summary>
        public static Image Panel(Transform parent, Color color, string name = "Panel")
        {
            var image = Image(parent, name, color);
            image.sprite = ArtLibrary.Panel();
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        public static RectTransform Overlay(Transform parent, string name, float alpha = 0.7f)
        {
            var image = Image(parent, name, new Color(0f, 0f, 0f, alpha));
            return Stretch(image.rectTransform);
        }

        public static Image Icon(Transform parent, Icon icon, float size)
        {
            var image = Image(parent, "Icon " + icon, Color.white);
            image.sprite = ArtLibrary.Icon(icon);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        public static Image SpriteImage(Transform parent, Sprite sprite, Vector2 size)
        {
            var image = Image(parent, "Sprite", Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        /// <summary>A label; <paramref name="text"/> must be display-ready (see <see cref="Loc.T(string)"/>).</summary>
        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter,
            bool bold = false, bool shadow = true)
        {
            var label = Rect(parent, "Label").gameObject.AddComponent<Text>();
            label.font = bold ? Loc.BoldFont : Loc.Font;
            label.fontSize = size;
            label.color = color;
            label.alignment = Loc.Align(alignment);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            if (shadow)
            {
                var effect = label.gameObject.AddComponent<Shadow>();
                effect.effectColor = new Color(0f, 0f, 0f, 0.45f);
                effect.effectDistance = new Vector2(2f, -2f);
            }
            return label;
        }

        /// <summary>
        /// A label whose logical (unshaped) text is word-wrapped to <paramref name="width"/>
        /// before shaping, so Persian lines break in the right order.
        /// </summary>
        public static Text Paragraph(Transform parent, string logicalText, int size, Color color, float width,
            TextAnchor alignment = TextAnchor.UpperLeft, bool shadow = false)
        {
            var label = Label(parent, string.Empty, size, color, alignment, false, shadow);
            var settings = label.GetGenerationSettings(Vector2.zero);
            var generator = label.cachedTextGeneratorForLayout;
            var lines = TextLayout.Wrap(logicalText,
                line => generator.GetPreferredWidth(Loc.Display(line), settings) / label.pixelsPerUnit, width);
            label.text = Loc.Display(string.Join("\n", lines));
            label.rectTransform.sizeDelta = new Vector2(width, lines.Count * size * 1.45f);
            label.lineSpacing = 1.1f;
            return label;
        }

        public static Button Button(Transform parent, string text, Action onClick, Color color, int fontSize = 40, Icon? icon = null)
        {
            var image = Panel(parent, color, "Button");
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.85f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(() =>
                {
                    AudioService.Play(Sfx.Click, 0.6f);
                    onClick();
                });

            if (icon.HasValue && string.IsNullOrEmpty(text))
            {
                var glyph = Icon(image.transform, icon.Value, fontSize * 1.6f);
                Center(glyph.rectTransform, Vector2.zero, new Vector2(fontSize * 1.6f, fontSize * 1.6f), false);
            }
            else if (icon.HasValue)
            {
                var row = Rect(image.transform, "Row");
                Stretch(row);
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 12f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.reverseArrangement = Loc.IsRtl;
                Icon(row, icon.Value, fontSize * 1.25f);
                var label = Label(row, text, fontSize, Cream, TextAnchor.MiddleCenter, true);
                label.rectTransform.sizeDelta = new Vector2(label.preferredWidth, fontSize * 1.4f);
            }
            else
            {
                var label = Label(image.transform, text, fontSize, Cream, TextAnchor.MiddleCenter, true);
                Stretch(label.rectTransform);
            }
            return button;
        }

        /// <summary>A round icon-only button (pause, close, settings…).</summary>
        public static Button IconButton(Transform parent, Icon icon, Action onClick, float size, Color color)
        {
            var button = Button(parent, null, onClick, color, Mathf.RoundToInt(size * 0.32f), icon);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
            return button;
        }

        /// <summary>An icon followed by a number, e.g. a price. Returns the number label.</summary>
        public static Text Amount(Transform parent, Icon icon, int amount, int fontSize, Color color)
        {
            var row = Rect(parent, "Amount");
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.reverseArrangement = Loc.IsRtl;
            Icon(row, icon, fontSize * 1.2f);
            var label = Label(row, Loc.Number(amount), fontSize, color, TextAnchor.MiddleCenter, true);
            label.rectTransform.sizeDelta = new Vector2(Mathf.Max(fontSize, label.preferredWidth), fontSize * 1.3f);
            return label;
        }

        /// <summary>A vertical scrolling list; returns the content rect, which grows with its children.</summary>
        public static RectTransform ScrollColumn(RectTransform parent, float spacing)
        {
            return Scroll(parent, spacing, false);
        }

        /// <summary>A horizontal scrolling list.</summary>
        public static RectTransform ScrollRow(RectTransform parent, float spacing)
        {
            return Scroll(parent, spacing, true);
        }

        static RectTransform Scroll(RectTransform parent, float spacing, bool horizontal)
        {
            var scrollRect = Stretch(Rect(parent, "Scroll"));
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var viewport = Stretch(Rect(scrollRect, "Viewport"));
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            var content = Rect(viewport, "Content");
            if (horizontal)
            {
                var rtl = Loc.IsRtl;
                content.anchorMin = new Vector2(rtl ? 1f : 0f, 0f);
                content.anchorMax = new Vector2(rtl ? 1f : 0f, 1f);
                content.pivot = new Vector2(rtl ? 1f : 0f, 0.5f);
                var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = spacing;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                layout.reverseArrangement = rtl;
                layout.padding = new RectOffset(20, 20, 0, 0);
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = spacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(0, 0, 10, 10);
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            content.sizeDelta = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        /// <summary>Gives a layout child a fixed size inside a scroll list.</summary>
        public static LayoutElement Size(Component component, float width, float height)
        {
            var element = component.gameObject.GetComponent<LayoutElement>() ?? component.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
                element.preferredWidth = element.minWidth = width;
            if (height > 0f)
                element.preferredHeight = element.minHeight = height;
            return element;
        }

        /// <summary>Shows a small progress bar; returns the fill image (set fillAmount).</summary>
        public static Image ProgressBar(Transform parent, Vector2 size, Color fillColor)
        {
            var back = Panel(parent, new Color(0f, 0f, 0f, 0.45f), "Progress");
            back.rectTransform.sizeDelta = size;
            var fill = Image(back.transform, "Fill", fillColor);
            fill.sprite = ArtLibrary.Pixel();
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = Loc.IsRtl ? 1 : 0;
            Stretch(fill.rectTransform, 6f);
            return fill;
        }

        // ------------------------------------------------------------ composite widgets

        /// <summary>Gold and gems with "+" buttons that open the shop; refreshes on every change.</summary>
        public static RectTransform CurrencyBar(Transform parent, Action openShop)
        {
            var bar = Rect(parent, "Currencies");
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.reverseArrangement = Loc.IsRtl;
            var gold = Pill(bar, Art.Icon.Coin, openShop);
            var gems = Pill(bar, Art.Icon.Gem, openShop);
            Action refresh = () =>
            {
                if (gold != null)
                    gold.text = Loc.Number(Economy.Gold);
                if (gems != null)
                    gems.text = Loc.Number(Economy.Gems);
            };
            refresh();
            var listener = bar.gameObject.AddComponent<EconomyListener>();
            listener.OnChange = refresh;
            return bar;
        }

        static Text Pill(Transform parent, Icon icon, Action openShop)
        {
            var pill = Panel(parent, new Color(0f, 0f, 0f, 0.55f), "Pill");
            pill.rectTransform.sizeDelta = new Vector2(280f, 76f);
            var glyph = Icon(pill.transform, icon, 64f);
            Place(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(64f, 64f));
            var label = Label(pill.transform, "0", 38, Cream, TextAnchor.MiddleCenter, true);
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-6f, 0f), new Vector2(150f, 60f));
            if (openShop != null)
            {
                var plus = IconButton(pill.transform, Art.Icon.Plus, openShop, 58f, Green);
                Place(plus.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(58f, 58f));
            }
            return label;
        }

        /// <summary>A reward as an icon with an amount (gold, gems, item, hero, skin).</summary>
        public static RectTransform RewardBadge(Transform parent, Reward reward, float size)
        {
            var root = Rect(parent, "Reward");
            root.sizeDelta = new Vector2(size, size);
            switch (reward.type)
            {
                case RewardType.Hero:
                case RewardType.Skin:
                    var heroId = reward.type == RewardType.Hero ? reward.id : HeroDefs.GetSkin(reward.id).heroId;
                    var look = reward.type == RewardType.Hero ? HeroDefs.Get(heroId).look : HeroDefs.LookFor(HeroDefs.Get(heroId), reward.id);
                    var portrait = SpriteImage(root, ArtLibrary.Character(look), new Vector2(size, size));
                    Stretch(portrait.rectTransform);
                    break;
                default:
                    var glyph = Icon(root, IconFor(reward), size * 0.7f);
                    Center(glyph.rectTransform, new Vector2(0f, size * 0.1f), new Vector2(size * 0.7f, size * 0.7f), false);
                    break;
            }
            if (reward.type != RewardType.Hero && reward.type != RewardType.Skin)
            {
                var amount = Label(root, Loc.Number(reward.amount), Mathf.RoundToInt(size * 0.26f), Cream, TextAnchor.LowerCenter, true);
                Center(amount.rectTransform, new Vector2(0f, -size * 0.36f), new Vector2(size, size * 0.4f), false);
            }
            return root;
        }

        public static Icon IconFor(Reward reward)
        {
            switch (reward.type)
            {
                case RewardType.Gold: return Art.Icon.Coin;
                case RewardType.Gems: return Art.Icon.Gem;
                case RewardType.PassXp: return Art.Icon.Star;
                case RewardType.Item: return ItemIcon(reward.id);
                default: return Art.Icon.Helmet;
            }
        }

        public static Icon ItemIcon(string itemId)
        {
            switch (itemId)
            {
                case ItemIds.Nushdaru: return Art.Icon.Elixir;
                case ItemIds.Simorgh: return Art.Icon.Feather;
                case ItemIds.Naphtha: return Art.Icon.Jar;
                default: return Art.Icon.Treasure;
            }
        }

        /// <summary>Text describing a reward ("150 gold", "Zal"…), display-ready.</summary>
        public static string RewardName(Reward reward)
        {
            switch (reward.type)
            {
                case RewardType.Gold: return Loc.T("reward.gold", reward.amount);
                case RewardType.Gems: return Loc.T("reward.gems", reward.amount);
                case RewardType.Item: return Loc.T("reward.item", reward.amount, Loc.Get("item." + reward.id));
                case RewardType.Hero: return Loc.T("hero." + reward.id);
                case RewardType.Skin: return Loc.T("skin." + reward.id);
                default: return Loc.T("reward.xp", reward.amount);
            }
        }
    }

    /// <summary>Calls back when currencies change, and unsubscribes when destroyed.</summary>
    public class EconomyListener : MonoBehaviour
    {
        public Action OnChange;

        void OnEnable() { Economy.Changed += Handle; }
        void OnDisable() { Economy.Changed -= Handle; }

        void Handle()
        {
            if (OnChange != null)
                OnChange();
        }
    }
}
