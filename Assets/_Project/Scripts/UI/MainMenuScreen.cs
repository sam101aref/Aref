using Arash.Core;
using Arash.Localization;
using UnityEngine;

namespace Arash.UI
{
    /// <summary>Main menu (F-13): title, play, settings and a quick language switch.</summary>
    public class MainMenuScreen : ScreenBase
    {
        protected override void Build(RectTransform canvas)
        {
            UIFactory.Stretch(UIFactory.Panel(canvas, "Background", UIFactory.Lapis).rectTransform);

            var title = UIFactory.Label(canvas, Loc.T("game.title"), 130, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1700f, 200f));

            var subtitle = UIFactory.Label(canvas, Loc.T("game.subtitle"), 46, UIFactory.Cream);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 175f), new Vector2(1700f, 80f));

            var play = UIFactory.Button(canvas, Loc.T("ui.play"), SceneFlow.ToWorldMap, UIFactory.Turquoise, 60);
            UIFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 130f));

            var settings = UIFactory.Button(canvas, Loc.T("ui.settings"), OpenSettings, UIFactory.LapisLight, 48);
            UIFactory.Place((RectTransform)settings.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(560f, 110f));

            // Shows the other language's name, written in that language.
            var other = Loc.Current == Language.Persian ? Language.English : Language.Persian;
            var otherName = other == Language.Persian ? Loc.Display("فارسی") : "English";
            var language = UIFactory.Button(canvas, otherName, () => GameSettings.Language = other, UIFactory.LapisLight, 44);
            UIFactory.Place((RectTransform)language.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(560f, 100f));

            var version = UIFactory.Label(canvas, "v" + Application.version, 28, new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerRight);
            UIFactory.Place(version.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(400f, 50f));
        }
    }
}
