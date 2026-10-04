using System;
using Arash.Core;
using Arash.Localization;
using UnityEngine;

namespace Arash.UI
{
    /// <summary>
    /// Settings window (F-16): language, sound, music, vibration and difficulty. Each row is a
    /// button that cycles its value; changes are saved at once. Changing the language makes the
    /// owning screen rebuild, so the owner reopens this panel after a rebuild.
    /// </summary>
    public static class SettingsPanel
    {
        public static GameObject Open(RectTransform canvas, Action onClosed)
        {
            var overlay = UIFactory.Overlay(canvas, "Settings");
            Build(overlay, onClosed);
            return overlay.gameObject;
        }

        static void Build(RectTransform overlay, Action onClosed)
        {
            for (var i = overlay.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(overlay.GetChild(i).gameObject);

            var window = UIFactory.Panel(overlay, "Window", UIFactory.LapisLight).rectTransform;
            UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 860f));

            var title = UIFactory.Label(window, Loc.T("ui.settings"), 64, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 90f));

            Action refresh = () => Build(overlay, onClosed);
            var y = -170f;
            Row(window, ref y, Loc.T("settings.language"), Loc.T("ui.language_name"), () =>
                GameSettings.Language = Loc.Current == Language.Persian ? Language.English : Language.Persian);
            Row(window, ref y, Loc.T("settings.sound"), OnOff(GameSettings.Sound), () =>
            {
                GameSettings.Sound = !GameSettings.Sound;
                refresh();
            });
            Row(window, ref y, Loc.T("settings.music"), OnOff(GameSettings.Music), () =>
            {
                GameSettings.Music = !GameSettings.Music;
                refresh();
            });
            Row(window, ref y, Loc.T("settings.vibration"), OnOff(GameSettings.Vibration), () =>
            {
                GameSettings.Vibration = !GameSettings.Vibration;
                refresh();
            });
            Row(window, ref y, Loc.T("settings.difficulty"), DifficultyName(GameSettings.Difficulty), () =>
            {
                GameSettings.Difficulty = (Difficulty)(((int)GameSettings.Difficulty + 1) % 3);
                refresh();
            });

            var close = UIFactory.Button(window, Loc.T("ui.close"), () =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                if (onClosed != null)
                    onClosed();
            }, UIFactory.Gold, 48);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 100f));
        }

        static void Row(RectTransform window, ref float y, string label, string value, Action onClick)
        {
            var name = UIFactory.Label(window, label, 44, UIFactory.Cream, TextAnchor.MiddleLeft);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(60f, y), new Vector2(400f, 90f));

            var button = UIFactory.Button(window, value, onClick, UIFactory.Turquoise, 40);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 1f), new Vector2(-60f, y), new Vector2(420f, 90f));
            y -= 110f;
        }

        static string OnOff(bool value)
        {
            return Loc.T(value ? "ui.on" : "ui.off");
        }

        static string DifficultyName(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return Loc.T("difficulty.easy");
                case Difficulty.Hard: return Loc.T("difficulty.hard");
                default: return Loc.T("difficulty.normal");
            }
        }
    }
}
