using System;
using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Siavosh.UI
{
    public enum ButtonStyle
    {
        Primary,   // vermilion with a double gold border
        Secondary, // paper with a gold border
        Dark,      // ink with a gold border
        Lapis,
    }

    /// <summary>
    /// Builds uGUI screens in code in the game's manuscript style: rounded panels with gold borders,
    /// Vazirmatn text with right-to-left shaping, buttons, icons, bars and circular portraits.
    /// <see cref="Place"/> mirrors anchors and positions in Persian, so a screen is laid out once
    /// for both languages. All sizes are in reference pixels of a 1920x1080 screen.
    /// </summary>
    public static class UIKit
    {
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
            scaler.matchWidthOrHeight = 1f; // keep heights; wider phones get more room at the sides
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
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
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return rect;
        }

        /// <summary>
        /// Positions a rect by anchor (0–1 in each axis) and offset; the anchor is also the pivot.
        /// In right-to-left languages the horizontal anchor and offset are mirrored.
        /// </summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (Loc.IsRtl)
            {
                anchor.x = 1f - anchor.x;
                position.x = -position.x;
            }
            return PlaceFixed(rect, anchor, position, size);
        }

        /// <summary>Like <see cref="Place"/> but never mirrored (gameplay controls, artwork).</summary>
        public static RectTransform PlaceFixed(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Centered(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Fill(Transform parent, string name, Color color)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = Art.White;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A full-screen dark overlay that blocks touches to whatever is behind it.</summary>
        public static Image Overlay(Transform parent, Color color)
        {
            var overlay = Fill(parent, "Overlay", color);
            overlay.raycastTarget = true;
            Stretch(overlay.rectTransform);
            return overlay;
        }

        /// <summary>A rounded panel. With <paramref name="border"/> it gets a gold line just inside its edge.</summary>
        public static Image Panel(Transform parent, string name, Color color, int radius = 24, bool border = true, bool doubleBorder = false)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = Art.Panel(radius);
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            if (border)
                AddBorder(image.rectTransform, radius, Palette.Gold, 3, 0f);
            if (doubleBorder)
                AddBorder(image.rectTransform, Mathf.Max(4, radius - 8), Palette.GoldLight, 2, 8f);
            return image;
        }

        public static Image AddBorder(RectTransform target, int radius, Color color, int thickness, float inset)
        {
            var line = Rect(target, "Border").gameObject.AddComponent<Image>();
            line.sprite = Art.Outline(radius, thickness);
            line.type = Image.Type.Sliced;
            line.color = color;
            line.raycastTarget = false;
            Stretch(line.rectTransform, inset);
            return line;
        }

        /// <summary>A label; <paramref name="text"/> must already be display-ready (LocText.ToString()).</summary>
        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false, bool shadow = false)
        {
            var label = Rect(parent, "Label").gameObject.AddComponent<Text>();
            label.font = bold ? Loc.BoldFont : Loc.Font;
            label.fontSize = size;
            label.color = color;
            label.alignment = Loc.Align(alignment);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.supportRichText = false;
            label.text = text;
            if (shadow)
            {
                var s = label.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.55f);
                s.effectDistance = new Vector2(2f, -2f);
            }
            return label;
        }

        public static Text Label(Transform parent, LocText text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false, bool shadow = false)
        {
            return Label(parent, text.ToString(), size, color, alignment, bold, shadow);
        }

        /// <summary>
        /// A label whose logical text is word-wrapped to <paramref name="width"/> reference pixels
        /// before shaping, so Persian lines break in the right order.
        /// </summary>
        public static Text Wrapped(Transform parent, string raw, int size, Color color, float width,
            TextAnchor alignment = TextAnchor.UpperLeft, bool bold = false, float lineSpacing = 1.15f)
        {
            var label = Label(parent, string.Empty, size, color, alignment, bold);
            label.lineSpacing = lineSpacing;
            label.text = Loc.Display(string.Join("\n", WrapLines(label, raw, width)));
            return label;
        }

        public static System.Collections.Generic.List<string> WrapLines(Text label, string raw, float width)
        {
            var settings = label.GetGenerationSettings(Vector2.zero);
            var generator = label.cachedTextGeneratorForLayout;
            return TextLayout.Wrap(raw, line => generator.GetPreferredWidth(Loc.Display(line), settings) / label.pixelsPerUnit, width);
        }

        /// <summary>Width of display-ready text at a font size, in reference pixels.</summary>
        public static float Measure(Text label, string display)
        {
            return label.cachedTextGeneratorForLayout.GetPreferredWidth(display, label.GetGenerationSettings(Vector2.zero)) / label.pixelsPerUnit;
        }

        public static Button Button(Transform parent, LocText text, Action onClick, ButtonStyle style = ButtonStyle.Primary, int fontSize = 40)
        {
            return Button(parent, text.ToString(), onClick, style, fontSize);
        }

        public static Button Button(Transform parent, string displayText, Action onClick, ButtonStyle style = ButtonStyle.Primary, int fontSize = 40)
        {
            Color fill, ink;
            switch (style)
            {
                case ButtonStyle.Secondary: fill = Palette.Paper; ink = Palette.Ink; break;
                case ButtonStyle.Dark: fill = Palette.Hex(0x2B1B12, 0.88f); ink = Palette.Paper; break;
                case ButtonStyle.Lapis: fill = Palette.Lapis; ink = Palette.Paper; break;
                default: fill = Palette.Vermilion; ink = Palette.White; break;
            }
            var image = Panel(parent, "Button", fill, 40, true, style == ButtonStyle.Primary);
            image.raycastTarget = true;
            var button = MakeButton(image, onClick);
            var label = Label(image.transform, displayText, fontSize, ink, TextAnchor.MiddleCenter, true);
            Stretch(label.rectTransform);
            return button;
        }

        /// <summary>A round button with an icon (white icon art tinted to <paramref name="iconColor"/>).</summary>
        public static Button IconButton(Transform parent, string icon, Action onClick, float size, Color fill, Color iconColor, bool border = true)
        {
            var disc = Rect(parent, "IconButton").gameObject.AddComponent<Image>();
            disc.sprite = Art.Circle;
            disc.color = fill;
            disc.rectTransform.sizeDelta = new Vector2(size, size);
            if (border)
            {
                var ring = Rect(disc.transform, "Ring").gameObject.AddComponent<Image>();
                ring.sprite = Art.Ring;
                ring.color = Palette.Gold;
                ring.raycastTarget = false;
                Stretch(ring.rectTransform);
            }
            var glyph = Icon(disc.transform, icon, iconColor, size * 0.52f);
            glyph.rectTransform.anchoredPosition = Vector2.zero;
            return MakeButton(disc, onClick);
        }

        static Button MakeButton(Image target, Action onClick)
        {
            var button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.74f, 0.68f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            if (onClick != null)
                button.onClick.AddListener(() =>
                {
                    AudioService.Play(Sfx.Click, 0.5f);
                    onClick();
                });
            return button;
        }

        public static Image Icon(Transform parent, string icon, Color color, float size)
        {
            var image = Rect(parent, "Icon " + icon).gameObject.AddComponent<Image>();
            image.sprite = Art.Get("icons/" + icon);
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Centered(image.rectTransform, Vector2.zero, new Vector2(size, size));
            return image;
        }

        /// <summary>An art sprite (e.g. "titles/logo_fa") at a given height, keeping its proportions.</summary>
        public static Image Picture(Transform parent, string spriteName, float height, Color? tint = null)
        {
            var image = Rect(parent, spriteName).gameObject.AddComponent<Image>();
            image.sprite = Art.Get(spriteName);
            image.color = tint ?? Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var size = Art.Size(spriteName);
            var width = size.y > 0f ? height * size.x / size.y : height;
            Centered(image.rectTransform, Vector2.zero, new Vector2(width, height));
            return image;
        }

        /// <summary>A full-screen picture that covers the canvas, cropping what does not fit.</summary>
        public static Image Backdrop(Transform parent, string spriteName)
        {
            var image = Rect(parent, spriteName).gameObject.AddComponent<Image>();
            image.sprite = Art.Get(spriteName);
            image.raycastTarget = false;
            Stretch(image.rectTransform);
            var size = Art.Size(spriteName);
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = size.y > 0f ? size.x / size.y : 16f / 9f;
            return image;
        }

        /// <summary>A child rect covering a fraction of <paramref name="parent"/> (0–1, origin bottom-left).</summary>
        public static RectTransform Region(Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            var rect = Rect(parent, name);
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image RingImage(Transform parent, Color color, float inset = 0f)
        {
            var ring = Rect(parent, "Ring").gameObject.AddComponent<Image>();
            ring.sprite = Art.Ring;
            ring.color = color;
            ring.raycastTarget = false;
            Stretch(ring.rectTransform, inset);
            return ring;
        }

        /// <summary>A localized title picture (Nastaliq in Persian, Cinzel in English).</summary>
        public static Image Title(Transform parent, string key, float height, Color tint)
        {
            return Picture(parent, "titles/" + key + (Loc.IsPersian ? "_fa" : "_en"), height, tint);
        }

        /// <summary>A horizontal bar; returns the fill image (set fillAmount 0–1).</summary>
        public static Image Bar(Transform parent, Vector2 size, Color fillColor, Color back)
        {
            var frame = Panel(parent, "Bar", back, 12, true);
            frame.rectTransform.sizeDelta = size;
            var fill = Rect(frame.transform, "Fill").gameObject.AddComponent<Image>();
            fill.sprite = Art.Panel(10);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = Loc.IsRtl ? 1 : 0;
            fill.fillAmount = 1f;
            fill.color = fillColor;
            fill.raycastTarget = false;
            Stretch(fill.rectTransform, 5f);
            return fill;
        }

        /// <summary>A round portrait framed in gold, cropped to the character's head.</summary>
        public static RectTransform Portrait(Transform parent, string character, float size, Color background, bool mirror = false)
        {
            var disc = Rect(parent, "Portrait").gameObject.AddComponent<Image>();
            disc.sprite = Art.Circle;
            disc.color = background;
            disc.raycastTarget = false;
            disc.rectTransform.sizeDelta = new Vector2(size, size);
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            var name = "chars/" + character;
            var info = Art.Info(name);
            var face = Rect(disc.transform, "Face").gameObject.AddComponent<Image>();
            face.sprite = Art.Get(name);
            face.raycastTarget = false;
            if (info != null && info.h > 0f)
            {
                // Head centre in figure units (0.01 per SVG unit), relative to the feet.
                const float headX = 0.04f, headY = 3.5f, headSpan = 1.3f;
                var height = size * info.h / headSpan;
                var width = height * info.w / info.h;
                var fx = 0.5f + (headX - info.x) / info.w;
                var fy = 0.5f + (headY - info.y) / info.h;
                Centered(face.rectTransform, new Vector2((0.5f - fx) * width * (mirror ? -1f : 1f), (0.5f - fy) * height), new Vector2(width, height));
            }
            else
            {
                Stretch(face.rectTransform);
            }
            if (mirror)
                face.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            var ring = Rect(disc.transform, "Ring").gameObject.AddComponent<Image>();
            ring.sprite = Art.Ring;
            ring.color = Palette.Gold;
            ring.raycastTarget = false;
            Stretch(ring.rectTransform, -4f);
            return disc.rectTransform;
        }

        public static void SetAlpha(Graphic g, float alpha)
        {
            var c = g.color;
            c.a = alpha;
            g.color = c;
        }
    }
}
