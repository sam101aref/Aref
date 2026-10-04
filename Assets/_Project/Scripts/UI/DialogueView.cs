using System;
using Arash.Localization;
using Arash.Story;
using UnityEngine;
using UnityEngine.UI;

namespace Arash.UI
{
    /// <summary>
    /// Draws one dialogue line (F-19): a portrait (placeholder disc with the speaker's initial), the
    /// speaker's name and the wrapped text. Tap anywhere to continue; Skip ends the conversation.
    /// Stateless: the owner keeps track of which line is showing and redraws after a rebuild.
    /// </summary>
    public static class DialogueView
    {
        const float TextWidth = 1360f;

        public static RectTransform Show(RectTransform canvas, DialogueLine line, Action next, Action skip)
        {
            var overlay = UIFactory.Stretch(UIFactory.Rect(canvas, "Dialogue"));

            var catcher = UIFactory.Panel(overlay, "Tap", new Color(0f, 0f, 0f, 0.3f));
            UIFactory.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(() => next());

            var box = UIFactory.Panel(overlay, "Box", new Color(0.05f, 0.08f, 0.18f, 0.94f));
            box.raycastTarget = false;
            UIFactory.Place(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1820f, 320f));

            var narrator = line.speaker == Speaker.Narrator;
            var textX = narrator ? 60f : 330f;
            if (!narrator)
            {
                var portrait = UIFactory.Circle(box.transform, SpeakerColor(line.speaker), 220f);
                UIFactory.Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(220f, 220f));
                var name = Loc.Get(SpeakerKey(line.speaker));
                var initial = UIFactory.Label(portrait.transform, Loc.Display(name.Substring(0, 1)), 110, UIFactory.Cream, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(initial.rectTransform);

                var nameLabel = UIFactory.Label(box.transform, Loc.Display(name), 46, UIFactory.Gold, TextAnchor.MiddleLeft, true);
                UIFactory.Place(nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(textX, -20f), new Vector2(TextWidth, 70f));
            }

            var text = UIFactory.WrappedLabel(box.transform, Loc.Get(line.textKey), 42,
                narrator ? new Color(0.9f, 0.85f, 0.7f) : UIFactory.Cream, narrator ? TextWidth + 270f : TextWidth);
            UIFactory.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(textX, narrator ? -40f : -100f),
                new Vector2(narrator ? TextWidth + 270f : TextWidth, 200f));

            var skipButton = UIFactory.Button(overlay, Loc.T("ui.skip"), skip, new Color(0f, 0f, 0f, 0.45f), 36);
            UIFactory.Place((RectTransform)skipButton.transform, new Vector2(1f, 1f), new Vector2(-30f, -150f), new Vector2(220f, 80f));
            return overlay;
        }

        public static string SpeakerKey(Speaker speaker)
        {
            return "speaker." + speaker.ToString().ToLowerInvariant();
        }

        public static Color SpeakerColor(Speaker speaker)
        {
            switch (speaker)
            {
                case Speaker.Arash: return new Color(0.10f, 0.42f, 0.55f);
                case Speaker.Roshana: return new Color(0.55f, 0.25f, 0.45f);
                case Speaker.Mobad: return new Color(0.75f, 0.7f, 0.55f);
                case Speaker.Manuchehr: return new Color(0.75f, 0.58f, 0.15f);
                case Speaker.Afrasiab: return new Color(0.35f, 0.08f, 0.08f);
                case Speaker.WhiteDiv: return new Color(0.82f, 0.85f, 0.9f);
                default: return new Color(0.55f, 0.15f, 0.12f);
            }
        }
    }
}
