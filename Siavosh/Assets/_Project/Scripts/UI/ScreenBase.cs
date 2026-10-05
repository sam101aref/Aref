using Siavosh.Core;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.UI
{
    /// <summary>
    /// A screen whose interface is built in code. It is rebuilt when the language changes (so
    /// mirroring and text follow), and reopens the settings panel if that is where it changed.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour, IBackHandler
    {
        protected RectTransform Canvas { get; private set; }
        protected virtual int SortingOrder { get { return 10; } }
        bool settingsOpen;

        protected virtual void OnEnable()
        {
            Loc.Changed += Rebuild;
        }

        protected virtual void OnDisable()
        {
            Loc.Changed -= Rebuild;
        }

        protected virtual void Start()
        {
            if (Canvas == null)
                Rebuild();
        }

        protected void Rebuild()
        {
            if (Canvas != null)
                Destroy(Canvas.gameObject);
            Canvas = UIKit.Canvas(transform, GetType().Name, SortingOrder);
            Build(Canvas);
            if (settingsOpen)
                OpenSettings();
        }

        protected abstract void Build(RectTransform canvas);

        protected void OpenSettings()
        {
            settingsOpen = true;
            SettingsPanel.Open(Canvas, () => settingsOpen = false);
        }

        public virtual void OnBack()
        {
            Game.ShowMenu();
        }

        /// <summary>A round back button in the top corner (mirrored in Persian).</summary>
        protected void BackButton(RectTransform canvas, System.Action onBack)
        {
            var back = UIKit.IconButton(canvas, "back", onBack, 96f, Palette.Hex(0x2B1B12, 0.88f), Palette.GoldLight);
            UIKit.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(96f, 96f));
            if (Loc.IsRtl)
                back.transform.Find("Icon back").localScale = new Vector3(-1f, 1f, 1f);
        }

        /// <summary>A rounded dark chip with a short text, e.g. a counter in the top bar.</summary>
        protected static RectTransform Chip(RectTransform parent, string text, string sprite, Color fill, Vector2 anchor, Vector2 position, float width)
        {
            var icon = sprite;
            var chip = UIKit.Panel(parent, "Chip", fill, 34, true);
            UIKit.Place(chip.rectTransform, anchor, position, new Vector2(width, 72f));
            if (icon != null)
            {
                var glyph = UIKit.Picture(chip.transform, sprite, 44f, sprite.StartsWith("icons/") ? Palette.GoldLight : Color.white);
                UIKit.Place(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(44f, 44f));
            }
            var label = UIKit.Label(chip.transform, text, 32, Palette.Paper, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(icon != null ? 52f : 12f, 0f);
            if (Loc.IsRtl && icon != null)
            {
                label.rectTransform.offsetMin = new Vector2(12f, 0f);
                label.rectTransform.offsetMax = new Vector2(-52f, 0f);
            }
            return chip.rectTransform;
        }
    }
}
