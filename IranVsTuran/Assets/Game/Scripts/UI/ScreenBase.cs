using System;
using IranVsTuran.Art;
using IranVsTuran.Audio;
using IranVsTuran.Core;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IranVsTuran.UI
{
    /// <summary>A full-screen page. Built in code; rebuilt when the language changes.</summary>
    public abstract class ScreenBase
    {
        protected GameRoot Root { get; private set; }
        protected RectTransform Rect { get; private set; }

        public void Open(GameRoot root)
        {
            Root = root;
            Rect = UIKit.Stretch(UIKit.Rect(root.ScreenLayer, GetType().Name));
            OnOpened();
            Build();
        }

        public void Rebuild()
        {
            for (var i = Rect.childCount - 1; i >= 0; i--)
                Object.Destroy(Rect.GetChild(i).gameObject);
            Build();
        }

        protected abstract void Build();

        protected virtual void OnOpened()
        {
        }

        public virtual void OnBack()
        {
        }

        public virtual void Dispose()
        {
            if (Rect != null)
                Object.Destroy(Rect.gameObject);
        }

        /// <summary>The usual top bar: back button, title, gold and gems.</summary>
        protected RectTransform TopBar(string titleKey, Action back, bool currencies = true)
        {
            var bar = UIKit.Image(Rect, "TopBar", new Color(0.05f, 0.07f, 0.13f, 0.85f)).rectTransform;
            UIKit.Place(bar, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 120f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.sizeDelta = new Vector2(0f, 120f);

            if (back != null)
            {
                var button = UIKit.IconButton(bar, Icon.Back, back, 92f, UIKit.LapisLight);
                if (Loc.IsRtl)
                    button.transform.localScale = new Vector3(-1f, 1f, 1f);
                UIKit.Place(button.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(92f, 92f));
            }
            if (!string.IsNullOrEmpty(titleKey))
            {
                var title = UIKit.Label(bar, Loc.T(titleKey), 52, UIKit.Gold, TextAnchor.MiddleLeft, true);
                UIKit.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(140f, 0f), new Vector2(700f, 100f));
            }
            if (currencies)
            {
                var money = UIKit.CurrencyBar(bar, () => Root.Show(new ShopScreen()));
                UIKit.Place(money, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(600f, 90f));
            }
            return bar;
        }

        /// <summary>A painted backdrop that fills the screen.</summary>
        protected Image Background(Sprite sprite, Color tint)
        {
            var image = UIKit.Image(Rect, "Background", tint);
            image.sprite = sprite;
            image.preserveAspect = false;
            UIKit.Stretch(image.rectTransform);
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            return image;
        }
    }

    /// <summary>A modal window over a dark overlay.</summary>
    public class Popup
    {
        public RectTransform Root { get; private set; }
        public RectTransform Window { get; private set; }
        public bool Dismissable { get; private set; }
        public Action OnClosed;
        bool closed;

        public static Popup Open(string titleDisplay, Vector2 size, bool dismissable = true, Color? color = null)
        {
            var game = GameRoot.Instance;
            var popup = new Popup { Dismissable = dismissable };
            popup.Root = UIKit.Overlay(game.PopupLayer, "Popup", 0.72f);
            if (dismissable)
            {
                var catcher = popup.Root.gameObject.AddComponent<Button>();
                catcher.transition = Selectable.Transition.None;
                catcher.onClick.AddListener(popup.Close);
            }

            var window = UIKit.Panel(popup.Root, color ?? UIKit.Parchment, "Window");
            window.raycastTarget = true;
            popup.Window = UIKit.Center(window.rectTransform, Vector2.zero, size, false);
            // swallow clicks on the window so they don't reach the overlay
            window.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            if (!string.IsNullOrEmpty(titleDisplay))
            {
                var ribbon = UIKit.Panel(popup.Window, UIKit.Lapis, "Ribbon");
                UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 40f), new Vector2(Mathf.Min(size.x - 80f, 760f), 100f), false);
                var title = UIKit.Label(ribbon.transform, titleDisplay, 48, UIKit.Gold, TextAnchor.MiddleCenter, true);
                UIKit.Stretch(title.rectTransform);
            }
            if (dismissable)
            {
                var close = UIKit.IconButton(popup.Window, Icon.Close, popup.Close, 84f, UIKit.Danger);
                UIKit.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(26f, 26f), new Vector2(84f, 84f));
            }
            game.Register(popup);
            AudioService.Play(Sfx.Click, 0.4f);
            return popup;
        }

        public void Close()
        {
            if (closed)
                return;
            closed = true;
            GameRoot.Instance.Unregister(this);
            if (Root != null)
                Object.Destroy(Root.gameObject);
            if (OnClosed != null)
                OnClosed();
        }

        /// <summary>A yes/no question.</summary>
        public static Popup Confirm(string titleDisplay, string messageLogical, string yesDisplay, Action yes)
        {
            var popup = Open(titleDisplay, new Vector2(1000f, 560f));
            var text = UIKit.Paragraph(popup.Window, messageLogical, 40, UIKit.Ink, 860f, TextAnchor.UpperCenter);
            UIKit.Center(text.rectTransform, new Vector2(0f, 60f), text.rectTransform.sizeDelta, false);
            var ok = UIKit.Button(popup.Window, yesDisplay, () =>
            {
                yes();
                popup.Close();
            }, UIKit.Green, 42);
            UIKit.Center(ok.GetComponent<RectTransform>(), new Vector2(-200f, -170f), new Vector2(340f, 110f));
            var cancel = UIKit.Button(popup.Window, Loc.T("ui.cancel"), popup.Close, UIKit.Muted, 42);
            UIKit.Center(cancel.GetComponent<RectTransform>(), new Vector2(200f, -170f), new Vector2(340f, 110f));
            return popup;
        }

        /// <summary>A message with an OK button.</summary>
        public static Popup Message(string titleDisplay, string messageLogical)
        {
            var popup = Open(titleDisplay, new Vector2(1000f, 520f));
            var text = UIKit.Paragraph(popup.Window, messageLogical, 40, UIKit.Ink, 860f, TextAnchor.UpperCenter);
            UIKit.Center(text.rectTransform, new Vector2(0f, 50f), text.rectTransform.sizeDelta, false);
            var ok = UIKit.Button(popup.Window, Loc.T("ui.ok"), popup.Close, UIKit.Green, 42);
            UIKit.Center(ok.GetComponent<RectTransform>(), new Vector2(0f, -160f), new Vector2(340f, 110f), false);
            return popup;
        }
    }

    /// <summary>Brief messages that fade out by themselves.</summary>
    public static class Toasts
    {
        public static void Show(RectTransform layer, string displayText)
        {
            var panel = UIKit.Panel(layer, new Color(0.05f, 0.07f, 0.13f, 0.92f), "Toast");
            panel.raycastTarget = false;
            var label = UIKit.Label(panel.transform, displayText, 40, UIKit.Cream, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(label.rectTransform, 10f);
            var width = Mathf.Clamp(label.preferredWidth + 100f, 400f, 1600f);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(width, 100f), false);
            panel.gameObject.AddComponent<FadeAway>().Seconds = 2.2f;
        }
    }

    /// <summary>Fades a UI element and destroys it (unscaled time, so it works while paused).</summary>
    public class FadeAway : MonoBehaviour
    {
        public float Seconds = 2f;
        float age;
        CanvasGroup group;

        void Start()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            if (group != null)
                group.alpha = Mathf.Clamp01((Seconds - age) / 0.5f);
            if (age >= Seconds)
                Destroy(gameObject);
        }
    }

    /// <summary>Simulated rewarded ad (marked TEST) until a real ad network is plugged in.</summary>
    public class TestAds : IAdsBackend
    {
        public bool IsRewardedReady { get { return true; } }

        public void ShowRewarded(string placement, Action<bool> done)
        {
            var paused = Time.timeScale;
            Time.timeScale = 0f;
            var popup = Popup.Open(null, new Vector2(1500f, 860f), false, new Color(0.1f, 0.12f, 0.2f));
            var tag = UIKit.Label(popup.Window, Loc.T("ads.test"), 44, UIKit.Gold, TextAnchor.MiddleCenter, true);
            UIKit.Center(tag.rectTransform, new Vector2(0f, 330f), new Vector2(1400f, 80f), false);
            var art = UIKit.SpriteImage(popup.Window, ArtLibrary.Backdrop(Defs.Backdrop.Castle), new Vector2(960f, 540f));
            UIKit.Center(art.rectTransform, new Vector2(0f, 20f), new Vector2(960f, 540f), false);
            var counter = UIKit.Label(popup.Window, string.Empty, 44, UIKit.Cream, TextAnchor.MiddleCenter, true);
            UIKit.Center(counter.rectTransform, new Vector2(0f, -320f), new Vector2(900f, 80f), false);
            var close = UIKit.Button(popup.Window, Loc.T("ads.close"), null, UIKit.Muted, 38);
            UIKit.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(260f, 90f));

            var timer = popup.Root.gameObject.AddComponent<Countdown>();
            timer.Seconds = 5f;
            timer.Tick = left => counter.text = left > 0 ? Loc.T("ads.countdown", left) : Loc.T("ads.reward_ready");
            var finished = false;
            timer.Done = () => finished = true;
            close.onClick.AddListener(() =>
            {
                popup.Close();
                Time.timeScale = paused;
                done(finished);
            });
        }
    }

    /// <summary>Simulated store (marked TEST): asks for confirmation instead of charging money.</summary>
    public class TestStore : IStoreBackend
    {
        public bool IsAvailable { get { return true; } }

        public string PriceText(string productId)
        {
            return null;
        }

        public void Purchase(string productId, Action<bool> done)
        {
            var product = Defs.ShopDefs.Product(productId);
            var name = product != null ? Loc.Get(product.NameKey) : productId;
            var price = product != null ? product.fallbackPrice : string.Empty;
            var answered = false;
            var popup = Popup.Confirm(Loc.T("store.test_title"), Loc.Format(Loc.Get("store.test_body"), name, price), Loc.T("store.buy"), () =>
            {
                answered = true;
                done(true);
            });
            popup.OnClosed += () =>
            {
                if (!answered)
                {
                    answered = true;
                    done(false);
                }
            };
        }
    }

    /// <summary>Counts down in unscaled seconds, reporting whole seconds left.</summary>
    public class Countdown : MonoBehaviour
    {
        public float Seconds;
        public Action<int> Tick;
        public Action Done;
        int lastShown = -1;
        bool finished;

        void Update()
        {
            Seconds -= Time.unscaledDeltaTime;
            var left = Mathf.Max(0, Mathf.CeilToInt(Seconds));
            if (left != lastShown)
            {
                lastShown = left;
                if (Tick != null)
                    Tick(left);
            }
            if (!finished && Seconds <= 0f)
            {
                finished = true;
                if (Done != null)
                    Done();
            }
        }
    }
}
