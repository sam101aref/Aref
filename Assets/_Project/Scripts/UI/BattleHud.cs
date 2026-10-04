using System;
using System.Collections;
using System.Collections.Generic;
using Arash.Combat;
using Arash.Core;
using Arash.Localization;
using Arash.Story;
using UnityEngine;
using UnityEngine.UI;

namespace Arash.UI
{
    /// <summary>
    /// Battle overlay: level title, pause menu, tutorial hint, mode status (lives, time, targets),
    /// wind, pop-ups ("Headshot!"), dialogue (F-19), the level intro banner and the end-of-level
    /// screen with stars and coins (F-12).
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
            public Action Map;
            public bool Doubled;
        }

        [SerializeField] float popupDuration = 1.1f;
        [SerializeField] float introDuration = 3f;

        string titleKey;
        string hintKey;
        Result result;
        bool pauseOpen;
        Func<string> status;
        float? wind;
        PlayerArsenal arsenal;
        RectTransform farrFill;
        Text farrLabel;
        Text specialLabel;
        Image specialButton;
        List<DialogueLine> dialogue;
        int dialogueIndex;
        Action dialogueDone;

        Text popup;
        Text hint;
        Text statusLabel;
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

        public void ShowResult(bool won, int stars, int coins, Action next, Action map)
        {
            result = new Result { Won = won, Stars = stars, Coins = coins, Next = next, Map = map ?? SceneFlow.ToWorldMap };
            Audio.AudioService.Play(won ? Audio.Sfx.Victory : Audio.Sfx.Defeat);
            pauseOpen = false;
            Rebuild();
        }

        /// <summary>Shows the farr meter and special-arrow button (F-31, F-32); null hides them.</summary>
        public void SetArsenal(PlayerArsenal playerArsenal)
        {
            if (arsenal != null)
                arsenal.Changed -= RefreshArsenal;
            arsenal = playerArsenal;
            if (arsenal != null)
                arsenal.Changed += RefreshArsenal;
            Rebuild();
        }

        /// <summary>Shows a status line; the provider is called again whenever the HUD redraws.</summary>
        public void SetStatus(Func<string> provider)
        {
            status = provider;
            Rebuild();
        }

        public void RefreshStatus()
        {
            if (statusLabel != null && status != null)
                statusLabel.text = status();
        }

        /// <summary>Shows the wind (units/s², positive to the right); null hides it.</summary>
        public void SetWind(float? strength)
        {
            wind = strength;
            Rebuild();
        }

        /// <summary>Plays dialogue lines one by one, then calls <paramref name="done"/>.</summary>
        public void PlayDialogue(List<DialogueLine> lines, Action done)
        {
            if (lines == null || lines.Count == 0)
            {
                if (done != null)
                    done();
                return;
            }
            dialogue = lines;
            dialogueIndex = 0;
            dialogueDone = done;
            Rebuild();
        }

        void AdvanceDialogue(bool skip)
        {
            dialogueIndex++;
            if (!skip && dialogue != null && dialogueIndex < dialogue.Count)
            {
                Rebuild();
                return;
            }
            var done = dialogueDone;
            dialogue = null;
            dialogueDone = null;
            Rebuild();
            if (done != null)
                done();
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

            if (status != null && result == null)
            {
                statusLabel = UIFactory.Label(canvas, status(), 44, UIFactory.Cream, TextAnchor.MiddleCenter, true);
                UIFactory.Place(statusLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 80f));
            }

            if (wind.HasValue && result == null)
                BuildWind(canvas, wind.Value);

            if (arsenal != null && result == null)
                BuildArsenal(canvas);

            popup = UIFactory.Label(canvas, string.Empty, 100, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(popup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(1600f, 160f));
            popup.gameObject.SetActive(false);

            if (result != null)
                BuildResult(canvas);
            else if (dialogue != null && dialogueIndex < dialogue.Count)
                DialogueView.Show(canvas, dialogue[dialogueIndex], () => AdvanceDialogue(false), () => AdvanceDialogue(true));
            else if (pauseOpen)
                BuildPause(canvas);
        }

        void BuildArsenal(RectTransform canvas)
        {
            const float width = 360f;
            var bar = UIFactory.Panel(canvas, "Farr", new Color(0f, 0f, 0f, 0.45f));
            UIFactory.Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(width, 34f));
            var fill = UIFactory.Panel(bar.transform, "Fill", UIFactory.Gold);
            farrFill = fill.rectTransform;
            farrFill.anchorMin = new Vector2(Loc.IsRtl ? 1f : 0f, 0f);
            farrFill.anchorMax = new Vector2(Loc.IsRtl ? 1f : 0f, 1f);
            farrFill.pivot = new Vector2(Loc.IsRtl ? 1f : 0f, 0.5f);
            farrFill.offsetMin = farrFill.offsetMax = Vector2.zero;

            farrLabel = UIFactory.Label(canvas, Loc.T("farr.name"), 32, UIFactory.Cream, TextAnchor.MiddleLeft, true);
            UIFactory.Place(farrLabel.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 80f), new Vector2(width, 50f));

            if (arsenal.HasSpecials)
            {
                var button = UIFactory.Button(canvas, string.Empty, arsenal.ToggleArmed, UIFactory.Muted, 32);
                specialButton = button.GetComponent<Image>();
                specialLabel = button.GetComponentInChildren<Text>();
                UIFactory.Place((RectTransform)button.transform, new Vector2(0f, 0f), new Vector2(420f, 30f), new Vector2(300f, 100f));

                var cycle = UIFactory.Button(canvas, "<>", arsenal.CycleSelection, new Color(0f, 0f, 0f, 0.45f), 32);
                UIFactory.Place((RectTransform)cycle.transform, new Vector2(0f, 0f), new Vector2(735f, 30f), new Vector2(100f, 100f));
            }
            RefreshArsenal();
        }

        void RefreshArsenal()
        {
            if (arsenal == null || farrFill == null || arsenal.Farr == null)
                return;
            farrFill.sizeDelta = new Vector2(360f * arsenal.Farr.Value, 0f);

            if (specialLabel == null)
                return;
            var name = Loc.T("special." + arsenal.Selected.ToString().ToLowerInvariant());
            if (arsenal.Armed)
            {
                specialLabel.text = Loc.T("farr.armed", Loc.Get("special." + arsenal.Selected.ToString().ToLowerInvariant()));
                specialButton.color = UIFactory.Gold;
            }
            else
            {
                specialLabel.text = name;
                specialButton.color = arsenal.Farr.IsFull ? UIFactory.Turquoise : UIFactory.Muted;
            }
        }

        static void BuildWind(RectTransform canvas, float strength)
        {
            // Direction is drawn with plain ASCII so it is never mirrored in Persian.
            var level = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(strength) / 1.5f), 0, 3);
            var arrows = level == 0 ? "-" : new string(strength > 0f ? '>' : '<', level);
            var name = UIFactory.Label(canvas, Loc.T("battle.wind"), 36, UIFactory.Cream, TextAnchor.MiddleCenter);
            UIFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(300f, 50f));
            var direction = UIFactory.Label(canvas, arrows, 52, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(direction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(300f, 60f));
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

                if (!result.Doubled && Monetization.CanOfferDoubleCoins(result.Coins))
                {
                    var bonus = result.Coins;
                    var doubleCoins = UIFactory.Button(overlay, Loc.T("store.double_coins"), () =>
                        Monetization.Ads.ShowRewarded(rewarded =>
                        {
                            if (!rewarded)
                                return;
                            SaveSystem.Data.coins += bonus;
                            SaveSystem.Save();
                            result.Coins += bonus;
                            result.Doubled = true;
                            Rebuild();
                        }), UIFactory.Gold, 34);
                    UIFactory.Place((RectTransform)doubleCoins.transform, new Vector2(0.5f, 0.5f), new Vector2(560f, 50f), new Vector2(380f, 80f));
                }
            }

            var y = -60f;
            if (result.Next != null)
                MenuButton(overlay, "ui.next", result.Next, UIFactory.Turquoise, ref y);
            MenuButton(overlay, "ui.retry", SceneFlow.Retry, result.Next != null ? UIFactory.LapisLight : UIFactory.Turquoise, ref y);
            MenuButton(overlay, "ui.map", result.Map, UIFactory.LapisLight, ref y);
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
