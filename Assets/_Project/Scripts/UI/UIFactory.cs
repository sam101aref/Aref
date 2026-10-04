using System;
using Arash.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Arash.UI
{
    /// <summary>
    /// Builds placeholder uGUI screens in code: canvas, panels, labels and buttons in the game's
    /// palette, with Vazirmatn text and right-to-left mirroring. <see cref="Place"/> mirrors anchors
    /// and positions automatically in Persian, so a screen is laid out once for both languages.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color Lapis = new Color(0.07f, 0.12f, 0.25f);
        public static readonly Color LapisLight = new Color(0.13f, 0.21f, 0.40f);
        public static readonly Color Gold = new Color(0.90f, 0.72f, 0.32f);
        public static readonly Color Turquoise = new Color(0.10f, 0.55f, 0.60f);
        public static readonly Color Cream = new Color(0.98f, 0.94f, 0.85f);
        public static readonly Color Shade = new Color(0f, 0f, 0f, 0.65f);
        public static readonly Color Muted = new Color(0.32f, 0.33f, 0.40f);
        public static readonly Color Danger = new Color(0.75f, 0.25f, 0.20f);

        const string StarSpritePath = "UI/Star";
        const string CircleSpritePath = "UI/Circle";
        static Sprite starSprite;
        static Sprite circleSprite;

        public static Vector2 ReferenceResolution { get { return new Vector2(1920f, 1080f); } }

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
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
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

        /// <summary>
        /// Positions a rect by anchor (0–1 in each axis) and offset in reference pixels.
        /// In right-to-left languages the horizontal anchor and offset are mirrored.
        /// </summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (Loc.IsRtl)
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

        public static Image Panel(Transform parent, string name, Color color)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>A full-screen dark overlay that blocks touches to whatever is behind it.</summary>
        public static RectTransform Overlay(Transform parent, string name)
        {
            var overlay = Panel(parent, name, Shade);
            return Stretch(overlay.rectTransform);
        }

        /// <summary>A label; <paramref name="text"/> must already be display-ready (see <see cref="Loc.T(string)"/>).</summary>
        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
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

            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        public static Button Button(Transform parent, string text, Action onClick, Color color, int fontSize = 44)
        {
            var image = Panel(parent, "Button", color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            var label = Label(image.transform, text, fontSize, Cream, TextAnchor.MiddleCenter, true);
            Stretch(label.rectTransform);
            return button;
        }

        public static Image Star(Transform parent, bool earned, float size)
        {
            if (starSprite == null)
                starSprite = Resources.Load<Sprite>(StarSpritePath);

            var image = Panel(parent, "Star", earned ? Gold : new Color(0f, 0f, 0f, 0.35f));
            image.sprite = starSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        public static Image Circle(Transform parent, Color color, float size)
        {
            if (circleSprite == null)
                circleSprite = Resources.Load<Sprite>(CircleSpritePath);
            var image = Panel(parent, "Circle", color);
            image.sprite = circleSprite;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        /// <summary>
        /// A label whose logical text is word-wrapped to <paramref name="width"/> reference pixels
        /// before shaping, so Persian lines break in the right order.
        /// </summary>
        public static Text WrappedLabel(Transform parent, string logicalText, int size, Color color, float width,
            TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var label = Label(parent, string.Empty, size, color, alignment);
            var settings = label.GetGenerationSettings(Vector2.zero);
            var generator = label.cachedTextGeneratorForLayout;
            var lines = Story.TextLayout.Wrap(logicalText,
                line => generator.GetPreferredWidth(Loc.Display(line), settings) / label.pixelsPerUnit, width);
            label.text = Loc.Display(string.Join("\n", lines));
            return label;
        }

        /// <summary>A row of three stars centred on the parent's local position.</summary>
        public static RectTransform StarRow(Transform parent, int earned, float size, Vector2 position)
        {
            var row = Rect(parent, "Stars");
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = position;
            row.sizeDelta = new Vector2(size * 3.4f, size);
            for (var i = 0; i < 3; i++)
            {
                // Stars fill from the reading direction's start.
                var index = Loc.IsRtl ? 2 - i : i;
                var star = Star(row, i < earned, size);
                star.rectTransform.anchoredPosition = new Vector2((index - 1) * size * 1.15f, 0f);
            }
            return row;
        }

        /// <summary>A vertical scroll area; returns the content rect, which grows with its children.</summary>
        public static RectTransform ScrollColumn(RectTransform parent, float spacing)
        {
            var scrollRect = Rect(parent, "Scroll");
            Stretch(scrollRect);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = Stretch(Rect(scrollRect, "Viewport"));
            viewport.gameObject.AddComponent<RectMask2D>();
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear; // lets drags anywhere scroll the list

            var content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        public static LayoutElement Height(Component component, float height)
        {
            var element = component.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return element;
        }
    }
}
