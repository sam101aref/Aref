using System;
using Arash.Art;
using Arash.Core;
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
            UIFactory.Skin(box, ArtLibrary.UI("frame"));
            box.raycastTarget = false;
            UIFactory.Place(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1820f, 320f));

            var narrator = line.speaker == Speaker.Narrator;
            var textX = narrator ? 60f : 330f;
            if (!narrator)
            {
                var portrait = UIFactory.Circle(box.transform, SpeakerColor(line.speaker), 220f);
                UIFactory.Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(220f, 220f));
                var name = Loc.Get(SpeakerKey(line.speaker));
                if (!Portrait(portrait.rectTransform, line.speaker, 1.55f))
                {
                    var initial = UIFactory.Label(portrait.transform, Loc.Display(name.Substring(0, 1)), 110, UIFactory.Cream, TextAnchor.MiddleCenter, true);
                    UIFactory.Stretch(initial.rectTransform);
                }

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

        /// <summary>The speaker's drawn head and hat, centred on the parent; false if there is no art.</summary>
        public static bool Portrait(RectTransform parent, Speaker speaker, float scale)
        {
            CharacterLook look;
            if (!LookFor(speaker, out look))
                return false;
            var head = ArtLibrary.Character(look, "head");
            if (head == null)
                return false;
            Part(parent, head, scale, Color.white);
            var hat = ArtLibrary.Character(look, "hat");
            if (hat != null)
            {
                var tint = Color.white;
                if (look == CharacterLook.Arash)
                {
                    var outfit = Armory.Find(SaveSystem.Data.equippedOutfit) ?? Armory.Find(Armory.DefaultOutfit);
                    tint = outfit.Cap;
                }
                Part(parent, hat, scale, tint);
            }
            return true;
        }

        /// <summary>Places a sprite so its pivot (the head's centre) sits slightly below the parent's centre.</summary>
        static void Part(RectTransform parent, Sprite sprite, float scale, Color tint)
        {
            var image = UIFactory.Panel(parent, sprite.name, tint);
            image.sprite = sprite;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            var size = sprite.rect.size;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(sprite.pivot.x / size.x, sprite.pivot.y / size.y);
            rect.sizeDelta = size * scale;
            rect.anchoredPosition = new Vector2(Loc.IsRtl ? 0f : 0f, -12f);
            if (Loc.IsRtl)
                rect.localScale = new Vector3(-1f, 1f, 1f); // face into the text
        }

        static bool LookFor(Speaker speaker, out CharacterLook look)
        {
            switch (speaker)
            {
                case Speaker.Arash: look = CharacterLook.Arash; return true;
                case Speaker.Roshana: look = CharacterLook.Roshana; return true;
                case Speaker.Mobad: look = CharacterLook.Mobad; return true;
                case Speaker.Manuchehr: look = CharacterLook.Manuchehr; return true;
                case Speaker.Afrasiab: look = CharacterLook.Afrasiab; return true;
                case Speaker.Turanian: look = CharacterLook.Turanian; return true;
                case Speaker.Barman: look = CharacterLook.Barman; return true;
                case Speaker.Garsivaz: look = CharacterLook.Garsivaz; return true;
                case Speaker.WhiteDiv: look = CharacterLook.WhiteDiv; return true;
                case Speaker.Villager: look = CharacterLook.Villager; return true;
                case Speaker.Commander: look = CharacterLook.Commander; return true;
                case Speaker.Shaman: look = CharacterLook.Shaman; return true;
                case Speaker.Envoy: look = CharacterLook.Envoy; return true;
                case Speaker.Div: look = CharacterLook.Div; return true;
                default: look = CharacterLook.Arash; return false;
            }
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
