using System;
using System.Collections;
using Arash.Core;
using Arash.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Arash.UI
{
    /// <summary>
    /// Battle overlay: level title, pause menu, tutorial hint, pop-ups ("Headshot!"), the level
    /// intro banner and the end-of-level screen with stars and coins (F-12).
    /// Built in code; rebuilt (keeping its state) when the language changes.
    /// </summary>
    public class BattleHud : ScreenBase
    {
        class Result
        {
            public bool Won;
            public int Stars;
            public int Coins;
            public Action Next;
        }

        [SerializeField] float popupDuration = 1.1f;
        [SerializeField] float introDuration = 3f;

        string titleKey;
        string hintKey;
        Result result;
        bool pauseOpen;

        Text popup;
        Text hint;
        Coroutine popupRoutine;

        protected override int SortingOrder { get { return 10; } }

        public void SetLevel(string levelTitleKey, string levelHintKey)
        {
            titleKey = levelTitleKey;
            hintKey = levelHintKey;
            Rebuild();
        }

        public void ShowIntro(string introKey)
        {
            if (string.IsNullOrEmpty(introKey))
                return;
            if (Canvas == null)
                Rebuild();
            StartCoroutine(IntroRoutine(introKey));
        }

        public void HideHint()
        {
            hintKey = null;
            if (hint != null)
                hint.gameObject.SetActive(false);
        }

        public void ShowPopup(string message)
        {
            if (Canvas == null)
                Rebuild();
            if (popupRoutine != null)
                StopCoroutine(popupRoutine);
            popupRoutine = StartCoroutine(PopupRoutine(message));
        }

        public void ShowResult(bool won, int stars, int coins, Action next)
        {
            result = new Result { Won = won, Stars = stars, Coins = coins, Next = next };
            pauseOpen = false;
            Rebuild();
        }

        protected override void Build(RectTransform canvas)
        {
            if (!string.IsNullOrEmpty(titleKey))
            {
                var title = UIFactory.Label(canvas, Loc.T(titleKey), 44, UIFactory.Cream, TextAnchor.MiddleLeft, true);
                UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(1000f, 80f));
            }

            if (result == null)
            {
                var pause = UIFactory.Button(canvas, "II", OpenPause, new Color(0f, 0f, 0f, 0.35f), 48);
                UIFactory.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-30f, -25f), new Vector2(110f, 110f));
            }

            hint = UIFactory.Label(canvas, string.IsNullOrEmpty(hintKey) ? string.Empty : Loc.T(hintKey), 46, UIFactory.Cream);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1700f, 90f));
            hint.gameObject.SetActive(!string.IsNullOrEmpty(hintKey) && result == null);

            popup = UIFactory.Label(canvas, string.Empty, 100, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(popup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(1600f, 160f));
            popup.gameObject.SetActive(false);

            if (result != null)
                BuildResult(canvas);
            else if (pauseOpen)
                BuildPause(canvas);
        }

        void OpenPause()
        {
            if (result != null)
                return;
            GamePause.Pause();
            pauseOpen = true;
            BuildPause(Canvas);
        }

        void BuildPause(RectTransform canvas)
        {
            var overlay = UIFactory.Overlay(canvas, "Pause");
            var heading = UIFactory.Label(overlay, Loc.T("ui.paused"), 80, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1000f, 120f));

            var y = 140f;
            MenuButton(overlay, "ui.resume", () =>
            {
                pauseOpen = false;
                GamePause.Resume();
                Destroy(overlay.gameObject);
            }, UIFactory.Turquoise, ref y);
            MenuButton(overlay, "ui.retry", SceneFlow.Retry, UIFactory.LapisLight, ref y);
            MenuButton(overlay, "ui.settings", OpenSettings, UIFactory.LapisLight, ref y);
            MenuButton(overlay, "ui.map", SceneFlow.ToWorldMap, UIFactory.LapisLight, ref y);
        }

        void BuildResult(RectTransform canvas)
        {
            var overlay = UIFactory.Overlay(canvas, "Result");
            var heading = UIFactory.Label(overlay, Loc.T(result.Won ? "battle.victory" : "battle.defeat"), 110,
                result.Won ? UIFactory.Gold : UIFactory.Danger, TextAnchor.MiddleCenter, true);
            UIFactory.Place(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1200f, 160f));

            UIFactory.StarRow(overlay, result.Stars, 120f, new Vector2(0f, 170f));

            if (result.Coins > 0)
            {
                var coins = UIFactory.Label(overlay, Loc.T("battle.coins", result.Coins), 48, UIFactory.Cream);
                UIFactory.Place(coins.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(800f, 80f));
            }

            var y = -60f;
            if (result.Next != null)
                MenuButton(overlay, "ui.next", result.Next, UIFactory.Turquoise, ref y);
            MenuButton(overlay, "ui.retry", SceneFlow.Retry, result.Next != null ? UIFactory.LapisLight : UIFactory.Turquoise, ref y);
            MenuButton(overlay, "ui.map", SceneFlow.ToWorldMap, UIFactory.LapisLight, ref y);
        }

        static void MenuButton(RectTransform parent, string key, Action onClick, Color color, ref float y)
        {
            var button = UIFactory.Button(parent, Loc.T(key), onClick, color, 48);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(520f, 110f));
            y -= 130f;
        }

        IEnumerator IntroRoutine(string introKey)
        {
            var banner = UIFactory.Panel(Canvas, "Intro", new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1920f, 260f));
            banner.raycastTarget = false;
            var group = banner.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            if (!string.IsNullOrEmpty(titleKey))
            {
                var title = UIFactory.Label(banner.transform, Loc.T(titleKey), 76, UIFactory.Gold, TextAnchor.MiddleCenter, true);
                UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1800f, 110f));
            }
            var text = UIFactory.Label(banner.transform, Loc.T(introKey), 40, UIFactory.Cream);
            UIFactory.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(1800f, 90f));

            for (var t = 0f; t < introDuration && banner != null; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Clamp01(Mathf.Min(t / 0.3f, (introDuration - t) / 0.6f));
                yield return null;
            }
            if (banner != null)
                Destroy(banner.gameObject);
        }

        IEnumerator PopupRoutine(string message)
        {
            popup.text = message;
            popup.gameObject.SetActive(true);
            for (var t = 0f; t < popupDuration && popup != null; t += Time.unscaledDeltaTime)
            {
                var k = t / popupDuration;
                popup.transform.localScale = Vector3.one * (1f + 0.4f * (1f - Mathf.Clamp01(k * 4f)));
                var color = UIFactory.Gold;
                color.a = 1f - Mathf.Clamp01((k - 0.7f) / 0.3f);
                popup.color = color;
                yield return null;
            }
            if (popup != null)
                popup.gameObject.SetActive(false);
            popupRoutine = null;
        }
    }
}
