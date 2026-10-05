using System;
using System.Collections;
using System.Collections.Generic;
using Arash.Art;
using Arash.Combat;
using Arash.Core;
using Arash.Localization;
using Arash.Story;
using UnityEngine;
using UnityEngine.UI;

namespace Arash.UI
{
    /// <summary>
    /// Battle overlay: level title, pause, tutorial hint, wave status, wind, pop-ups, dialogue
    /// (F-19, F-61), the bow buttons with their arrows (F-54, F-56), the farr meter with the rain of
    /// arrows (F-64), the shield's strength (F-57), the level intro banner and the end-of-level
    /// screen with stars, coins and gems (F-12, F-59). Built in code; rebuilt (keeping its state)
    /// when the language changes.
    /// </summary>
    public class BattleHud : ScreenBase
    {
        class Result
        {
            public bool Won;
            public int Stars;
            public int Coins;
            public int Gems;
            public string DefeatKey;
            public Action Next;
            public Action Map;
            public bool Doubled;
        }

        [SerializeField] float popupDuration = 1.2f;
        [SerializeField] float introDuration = 3f;

        string titleKey;
        string hintKey;
        Result result;
        bool pauseOpen;
        Func<string> status;
        float? wind;
        PlayerArsenal arsenal;
        Action castRain;
        Barrier shield;
        List<DialogueLine> dialogue;
        int dialogueIndex;
        Action dialogueDone;

        readonly List<Image> bowFrames = new List<Image>();
        readonly List<Text> bowAmmo = new List<Text>();
        Image farrFill;
        Button rainButton;
        Image rainGlow;
        RectTransform shieldFill;
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

        public void ShowResult(bool won, int stars, int coins, int gems, string defeatKey, Action next, Action map)
        {
            result = new Result
            {
                Won = won, Stars = stars, Coins = coins, Gems = gems, DefeatKey = defeatKey,
                Next = next, Map = map ?? SceneFlow.ToWorldMap,
            };
            Audio.AudioService.Play(won ? Audio.Sfx.Victory : Audio.Sfx.Defeat);
            pauseOpen = false;
            Rebuild();
        }

        /// <summary>Shows the bows, arrows and farr meter; null hides them.</summary>
        public void SetArsenal(PlayerArsenal playerArsenal, Action rain)
        {
            if (arsenal != null)
                arsenal.Changed -= RefreshArsenal;
            arsenal = playerArsenal;
            castRain = rain;
            if (arsenal != null)
                arsenal.Changed += RefreshArsenal;
            Rebuild();
        }

        /// <summary>Shows the standing shield's strength; null hides it.</summary>
        public void SetShield(Barrier barrier)
        {
            shield = barrier;
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

        /// <summary>Plays dialogue lines one by one, then calls <paramref name="done"/>. Play pauses meanwhile.</summary>
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
            GamePause.Pause();
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
            if (!pauseOpen)
                GamePause.Resume();
            Rebuild();
            if (done != null)
                done();
        }

        void Update()
        {
            if (shieldFill != null && shield != null)
                shieldFill.anchorMax = new Vector2(Mathf.Clamp01(shield.Fraction), 1f);
            if (shieldFill != null && (shield == null || shield.IsBroken))
                shieldFill.parent.gameObject.SetActive(false);
            if (rainGlow != null && rainGlow.enabled)
                rainGlow.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f));
        }

        protected override void Build(RectTransform canvas)
        {
            bowFrames.Clear();
            bowAmmo.Clear();
            farrFill = null;
            rainButton = null;
            rainGlow = null;
            shieldFill = null;

            if (!string.IsNullOrEmpty(titleKey))
            {
                var title = UIFactory.Label(canvas, Loc.T(titleKey), 40, UIFactory.Cream, TextAnchor.MiddleLeft, true);
                UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(800f, 70f));
            }

            if (result == null)
            {
                var pause = UIFactory.IconButton(canvas, "pause", OpenPause, 110f, UIFactory.LapisLight);
                UIFactory.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-30f, -25f), new Vector2(110f, 110f));
            }

            hint = UIFactory.Label(canvas, string.IsNullOrEmpty(hintKey) ? string.Empty : Loc.T(hintKey), 44, UIFactory.Cream);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(1500f, 90f));
            hint.gameObject.SetActive(!string.IsNullOrEmpty(hintKey) && result == null);

            if (status != null && result == null)
            {
                var band = UIFactory.Panel(canvas, "Status", Color.black);
                UIFactory.Skin(band, ArtLibrary.UI("panel"));
                UIFactory.Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(620f, 84f));
                statusLabel = UIFactory.Label(band.transform, status(), 42, UIFactory.Gold, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(statusLabel.rectTransform);
            }

            if (wind.HasValue && result == null)
                BuildWind(canvas, wind.Value);

            if (arsenal != null && result == null)
                BuildArsenal(canvas);
            if (shield != null && !shield.IsBroken && result == null)
                BuildShield(canvas);

            popup = UIFactory.Label(canvas, string.Empty, 88, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(popup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1600f, 150f));
            popup.gameObject.SetActive(false);

            if (result != null)
                BuildResult(canvas);
            else if (dialogue != null && dialogueIndex < dialogue.Count)
                DialogueView.Show(canvas, dialogue[dialogueIndex], () => AdvanceDialogue(false), () => AdvanceDialogue(true));
            else if (pauseOpen)
                BuildPause(canvas);
        }

        // ------------------------------------------------------------------ bows and farr

        void BuildArsenal(RectTransform canvas)
        {
            const float size = 150f;
            for (var i = 0; i < arsenal.Bows.Count; i++)
            {
                var index = i;
                var state = arsenal.Bows[i];
                var button = UIFactory.Button(canvas, string.Empty, () => arsenal.Select(index), UIFactory.LapisLight, 30);
                UIFactory.Place((RectTransform)button.transform, new Vector2(0f, 0f), new Vector2(30f + i * (size + 16f), 30f), new Vector2(size, size));
                bowFrames.Add(button.GetComponent<Image>());

                var icon = UIFactory.Icon(button.transform, "bow", size * 0.62f);
                icon.rectTransform.anchoredPosition = new Vector2(0f, 14f);
                if (state.Item.Trail.a > 0f)
                {
                    var trail = state.Item.Trail;
                    trail.a = 1f;
                    var dot = UIFactory.Circle(button.transform, trail, 30f);
                    dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(1f, 1f);
                    dot.rectTransform.anchoredPosition = new Vector2(-24f, -24f);
                }

                var ammo = UIFactory.Label(button.transform, string.Empty, 34, UIFactory.Cream, TextAnchor.MiddleCenter, true);
                ammo.rectTransform.anchorMin = new Vector2(0f, 0f);
                ammo.rectTransform.anchorMax = new Vector2(1f, 0f);
                ammo.rectTransform.pivot = new Vector2(0.5f, 0f);
                ammo.rectTransform.anchoredPosition = new Vector2(0f, 8f);
                ammo.rectTransform.sizeDelta = new Vector2(0f, 46f);
                bowAmmo.Add(ammo);
            }

            // Farr: a round meter that becomes the rain-of-arrows button when full.
            var rainSize = 170f;
            rainButton = UIFactory.Button(canvas, string.Empty, () =>
            {
                if (arsenal != null && arsenal.CanRain && castRain != null)
                    castRain();
            }, new Color(0f, 0f, 0f, 0.55f), 30);
            var rainRect = (RectTransform)rainButton.transform;
            UIFactory.Place(rainRect, new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(rainSize, rainSize));
            var back = rainButton.GetComponent<Image>();
            back.sprite = Resources.Load<Sprite>("UI/Circle");

            rainGlow = UIFactory.Circle(rainRect, new Color(1f, 0.85f, 0.4f, 0.45f), rainSize * 1.25f);
            rainGlow.rectTransform.SetAsFirstSibling();
            farrFill = UIFactory.Circle(rainRect, UIFactory.Gold, rainSize * 0.92f);
            farrFill.type = Image.Type.Filled;
            farrFill.fillMethod = Image.FillMethod.Radial360;
            farrFill.fillOrigin = (int)Image.Origin360.Bottom;
            var inner = UIFactory.Circle(rainRect, new Color(0.07f, 0.12f, 0.25f, 0.95f), rainSize * 0.74f);
            inner.raycastTarget = false;
            UIFactory.Icon(rainRect, "rain", rainSize * 0.5f);
            var label = UIFactory.Label(canvas, Loc.T("farr.rain"), 30, UIFactory.Cream, TextAnchor.MiddleCenter, true);
            UIFactory.Place(label.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 205f), new Vector2(rainSize + 60f, 44f));

            RefreshArsenal();
        }

        void RefreshArsenal()
        {
            if (arsenal == null)
                return;
            for (var i = 0; i < bowFrames.Count && i < arsenal.Bows.Count; i++)
            {
                var state = arsenal.Bows[i];
                var active = i == arsenal.ActiveIndex;
                bowFrames[i].color = active ? Color.white : new Color(0.55f, 0.6f, 0.75f, 0.9f);
                bowFrames[i].transform.localScale = Vector3.one * (active ? 1.08f : 0.95f);
                bowAmmo[i].text = Loc.Number(state.Ammo);
                bowAmmo[i].color = state.Ammo == 0 ? UIFactory.Danger : state.Ammo <= 3 ? UIFactory.Gold : UIFactory.Cream;
            }
            if (farrFill != null && arsenal.Farr != null)
            {
                farrFill.fillAmount = arsenal.Farr.Value;
                rainGlow.enabled = arsenal.CanRain;
            }
        }

        void BuildShield(RectTransform canvas)
        {
            var x = 30f + Mathf.Max(1, arsenal != null ? arsenal.Bows.Count : 1) * 166f + 10f;
            var icon = UIFactory.Icon(canvas, "shield", 64f);
            UIFactory.Place(icon.rectTransform, new Vector2(0f, 0f), new Vector2(x, 60f), new Vector2(64f, 64f));
            var bar = UIFactory.Panel(canvas, "Shield", Color.black);
            UIFactory.Skin(bar, ArtLibrary.UI("bar_bg"));
            UIFactory.Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(x + 70f, 76f), new Vector2(180f, 32f));
            var fill = UIFactory.Panel(bar.transform, "Fill", new Color(0.55f, 0.75f, 0.9f));
            fill.raycastTarget = false;
            shieldFill = fill.rectTransform;
            shieldFill.anchorMin = Vector2.zero;
            shieldFill.anchorMax = Vector2.one;
            shieldFill.offsetMin = new Vector2(4f, 4f);
            shieldFill.offsetMax = new Vector2(-4f, -4f);
            if (Loc.IsRtl)
                shieldFill.localScale = new Vector3(-1f, 1f, 1f);
        }

        static void BuildWind(RectTransform canvas, float strength)
        {
            // Direction is drawn with plain ASCII so it is never mirrored in Persian.
            var level = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(strength) / 1.5f), 0, 3);
            var arrows = level == 0 ? "-" : new string(strength > 0f ? '>' : '<', level);
            var name = UIFactory.Label(canvas, Loc.T("battle.wind"), 32, UIFactory.Cream, TextAnchor.MiddleCenter);
            UIFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(300f, 46f));
            var direction = UIFactory.Label(canvas, arrows, 50, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(direction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(300f, 56f));
        }

        // ------------------------------------------------------------------ pause and result

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
            var window = UIFactory.Window(overlay);
            UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 760f));
            var heading = UIFactory.Label(window, Loc.T("ui.paused"), 72, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(700f, 110f));

            var y = 140f;
            MenuButton(window, "ui.resume", () =>
            {
                pauseOpen = false;
                GamePause.Resume();
                Destroy(overlay.gameObject);
            }, UIFactory.Turquoise, ref y);
            MenuButton(window, "ui.retry", SceneFlow.Retry, UIFactory.LapisLight, ref y);
            MenuButton(window, "ui.settings", OpenSettings, UIFactory.LapisLight, ref y);
            MenuButton(window, "ui.map", SceneFlow.ToWorldMap, UIFactory.LapisLight, ref y);
        }

        void BuildResult(RectTransform canvas)
        {
            var overlay = UIFactory.Overlay(canvas, "Result");
            var window = UIFactory.Window(overlay);
            UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 960f));

            var heading = UIFactory.Label(window, Loc.T(result.Won ? "battle.victory" : "battle.defeat"), 100,
                result.Won ? UIFactory.Gold : UIFactory.Danger, TextAnchor.MiddleCenter, true);
            UIFactory.Place(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1000f, 140f));

            if (result.Won)
                UIFactory.StarRow(window, result.Stars, 120f, new Vector2(0f, 230f));
            else if (!string.IsNullOrEmpty(result.DefeatKey))
            {
                var reason = UIFactory.Label(window, Loc.T(result.DefeatKey), 44, UIFactory.Cream);
                UIFactory.Place(reason.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1000f, 80f));
            }

            if (result.Coins > 0 || result.Gems > 0)
            {
                var row = UIFactory.Rect(window, "Rewards");
                row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
                row.anchoredPosition = new Vector2(0f, 95f);
                row.sizeDelta = new Vector2(900f, 90f);
                var x = result.Gems > 0 ? -170f : 0f;
                Reward(row, "coin", result.Coins, x);
                if (result.Gems > 0)
                    Reward(row, "gem", result.Gems, 170f);

                if (!result.Doubled && result.Coins > 0 && Monetization.CanOfferDoubleCoins(result.Coins))
                {
                    var bonus = result.Coins;
                    var doubleCoins = UIFactory.Button(window, Loc.T("store.double_coins"), () =>
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
                    UIFactory.Place((RectTransform)doubleCoins.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(460f, 90f));
                }
            }

            var y = -110f;
            if (result.Next != null)
                MenuButton(window, "ui.next", result.Next, UIFactory.Turquoise, ref y);
            MenuButton(window, "ui.retry", SceneFlow.Retry, result.Next != null ? UIFactory.LapisLight : UIFactory.Turquoise, ref y);
            MenuButton(window, "ui.map", result.Map, UIFactory.LapisLight, ref y);
        }

        static void Reward(RectTransform row, string icon, int amount, float x)
        {
            var image = UIFactory.Icon(row, icon, 80f);
            image.rectTransform.anchoredPosition = new Vector2(x + (Loc.IsRtl ? 70f : -70f), 0f);
            var label = UIFactory.Label(row, "+" + Loc.Number(amount), 54, UIFactory.Cream, TextAnchor.MiddleCenter, true);
            label.rectTransform.anchoredPosition = new Vector2(x + (Loc.IsRtl ? -40f : 40f), 0f);
            label.rectTransform.sizeDelta = new Vector2(200f, 80f);
        }

        static void MenuButton(RectTransform parent, string key, Action onClick, Color color, ref float y)
        {
            var button = UIFactory.Button(parent, Loc.T(key), onClick, color, 46);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(520f, 110f));
            y -= 125f;
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
