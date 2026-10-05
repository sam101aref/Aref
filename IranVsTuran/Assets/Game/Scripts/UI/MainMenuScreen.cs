using IranVsTuran.Art;
using IranVsTuran.Audio;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace IranVsTuran.UI
{
    /// <summary>Title screen: Rostam faces Afrasiab before the palace of the kings.</summary>
    public class MainMenuScreen : ScreenBase
    {
        static bool dailyShownThisSession;

        protected override void OnOpened()
        {
            AudioService.Mood = MusicMood.Calm;
        }

        protected override void Build()
        {
            Background(ArtLibrary.Backdrop(Backdrop.Palace), Color.white);
            var shade = UIKit.Image(Rect, "Shade", new Color(0.05f, 0.04f, 0.1f, 0.35f));
            UIKit.Stretch(shade.rectTransform);

            // the two kings' champions
            var rostam = UIKit.SpriteImage(Rect, ArtLibrary.Character(HeroDefs.LookFor(HeroDefs.Get(HeroIds.Rostam), Heroes.Skin(HeroIds.Rostam)), 256), new Vector2(620f, 620f));
            UIKit.Place(rostam.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(620f, 620f), false);
            var afrasiab = UIKit.SpriteImage(Rect, ArtLibrary.Character(EnemyDefs.Get("afrasiab").look, 256), new Vector2(620f, 620f));
            UIKit.Place(afrasiab.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(620f, 620f), false);
            afrasiab.rectTransform.pivot = new Vector2(0.5f, 0f);
            afrasiab.rectTransform.anchoredPosition = new Vector2(-350f, 40f);
            afrasiab.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            var title = UIKit.Label(Rect, Loc.T("game.title"), 150, UIKit.Gold, TextAnchor.MiddleCenter, true);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1400f, 200f), false);
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.1f, 0.05f, 0.9f);
            outline.effectDistance = new Vector2(4f, -4f);
            var subtitle = UIKit.Label(Rect, Loc.T("game.subtitle"), 50, UIKit.Cream, TextAnchor.MiddleCenter, false);
            UIKit.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1400f, 80f), false);

            var play = UIKit.Button(Rect, Loc.T("menu.play"), () => Root.ToMap(), UIKit.Green, 64, Icon.Play);
            UIKit.Place(play.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(560f, 160f), false);

            var row = UIKit.Rect(Rect, "Buttons");
            UIKit.Place(row, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1150f, 130f), false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 26f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.reverseArrangement = Loc.IsRtl;
            UIKit.Button(row, Loc.T("menu.heroes"), () => Root.Show(new HeroesScreen()), UIKit.LapisLight, 36, Icon.Helmet);
            UIKit.Button(row, Loc.T("menu.shop"), () => Root.Show(new ShopScreen()), UIKit.Purple, 36, Icon.Gem);
            var pass = UIKit.Button(row, Loc.T("menu.pass"), () => Root.Show(new BattlePassScreen()), UIKit.Turquoise, 36, Icon.Crown);
            Badge(pass.transform, BattlePass.UnclaimedCount);

            var settings = UIKit.IconButton(Rect, Icon.Gear, () => SettingsPopup.Open(), 100f, UIKit.LapisLight);
            UIKit.Place(settings.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(100f, 100f));
            var daily = UIKit.IconButton(Rect, Icon.Chest, () => DailyPopup.Open(), 100f, UIKit.Gold);
            UIKit.Place(daily.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(150f, -30f), new Vector2(100f, 100f));
            Badge(daily.transform, DailyReward.CanClaim ? 1 : 0);

            var money = UIKit.CurrencyBar(Rect, () => Root.Show(new ShopScreen()));
            UIKit.Place(money, new Vector2(1f, 1f), new Vector2(-30f, -40f), new Vector2(600f, 90f));

            var version = UIKit.Label(Rect, "v" + Application.version, 26, new Color(1f, 1f, 1f, 0.6f), TextAnchor.LowerRight, false);
            UIKit.Place(version.rectTransform, new Vector2(1f, 0f), new Vector2(-20f, 10f), new Vector2(300f, 40f), false);

            if (!dailyShownThisSession && DailyReward.CanClaim)
            {
                dailyShownThisSession = true;
                DailyPopup.Open();
            }
        }

        /// <summary>A red count bubble on a button's corner (hidden when zero).</summary>
        public static void Badge(Transform button, int count)
        {
            if (count <= 0)
                return;
            var bubble = UIKit.Image(button, "Badge", UIKit.Danger);
            bubble.sprite = ArtLibrary.SolidCircle();
            bubble.raycastTarget = false;
            UIKit.Place(bubble.rectTransform, new Vector2(1f, 1f), new Vector2(14f, 14f), new Vector2(54f, 54f), false);
            var label = UIKit.Label(bubble.transform, count > 9 ? "!" : Loc.Number(count), 32, Color.white, TextAnchor.MiddleCenter, true, false);
            UIKit.Stretch(label.rectTransform);
        }

        public override void OnBack()
        {
            Popup.Confirm(Loc.T("menu.quit_title"), Loc.Get("menu.quit_body"), Loc.T("menu.quit"), Application.Quit);
        }
    }
}
