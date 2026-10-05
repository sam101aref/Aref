using System.Collections.Generic;
using Siavosh.Audio;
using Siavosh.Core;
using Siavosh.Localization;
using Siavosh.Play;
using Siavosh.Story;
using UnityEngine;
using UnityEngine.UI;

namespace Siavosh.UI
{
    /// <summary>
    /// A stage being played: owns the <see cref="StageWorld"/>, the heads-up display, the touch
    /// controls, tutorial tips, floating numbers, the opening dialogue, pausing, and winning or
    /// losing. Drives the world from its own Update so input and simulation run in a fixed order.
    /// </summary>
    public class StageScreen : ScreenBase
    {
        public StageDef Stage;

        StageWorld world;
        HeroStats stats;
        Queue<Beat> dialogue;
        float endTimer = -1f;
        bool won;
        bool lost;

        // HUD parts rebuilt with the canvas
        Image health;
        Image farr;
        Text farrLabel;
        Text goal;
        Text coins;
        Image[] leaves;
        Image[] hearts;
        Image progress;
        RectTransform progressHorse;
        RectTransform tipBox;
        Text tipText;
        float tipTime;
        LocText currentTip;
        RectTransform interactButton;
        Text interactLabel;
        RectTransform farrButton;
        RectTransform controls;
        RectTransform dialogueBox;
        RectTransform overlay;
        readonly List<Floater> floaters = new List<Floater>();

        class Floater
        {
            public Text Label;
            public Vector3 World;
            public float Age;
        }

        protected override int SortingOrder { get { return 20; } }

        protected override void Start()
        {
            stats = HeroStats.From(SaveSystem.Data, Settings.Difficulty);
            world = new StageWorld(Stage, stats, Game.Instance.Camera);
            world.TipShown += t => { currentTip = t; tipTime = 6f; RefreshTip(); };
            world.Floating += AddFloater;
            world.GoalChanged += RefreshGoal;
            Controls.Clear();
            world.Tick(0f); // place every figure before the first frame (and during the opening dialogue)
            base.Start();

            var introSeen = SaveSystem.Data.Has("intro." + Stage.Id);
            if (Stage.Intro != null && !introSeen)
            {
                dialogue = new Queue<Beat>();
                foreach (var beat in Stage.Intro.Beats)
                    if (beat.Kind == BeatKind.Line)
                        dialogue.Enqueue(beat);
                SaveSystem.Data.Set("intro." + Stage.Id);
                SaveSystem.Save();
                ShowDialogue();
            }
        }

        void OnDestroy()
        {
            if (world != null)
                world.Dispose();
            Controls.Clear();
            Pause.Set(false);
        }

        void Update()
        {
            if (world == null)
                return;
            UpdateFloaters();
            if (Pause.IsPaused || dialogue != null || overlay != null)
                return;

            Controls.Poll();
            if (Controls.Pressed(Act.Interact) && world.Interaction != null && world.Hero == null)
                world.Interaction.Interact();
            world.Tick(Time.deltaTime);
            UpdateHud();

            if (tipTime > 0f)
            {
                tipTime -= Time.deltaTime;
                if (tipTime <= 0f && tipBox != null)
                    tipBox.gameObject.SetActive(false);
            }

            if (won || lost)
            {
                endTimer -= Time.deltaTime;
                if (endTimer <= 0f)
                {
                    if (won)
                        Win();
                    else
                        ShowDefeat();
                    endTimer = float.MaxValue;
                }
                return;
            }

            if (world.Hero != null && !world.Hero.Alive)
                Lose(1.4f);
            else if (world.Rider != null && world.Rider.Fallen)
                Lose(1.4f);
            else if (world.Finished)
            {
                if (Stage.GoalKind == GoalKind.Hunt && world.GoalDone < world.GoalNeeded)
                    Lose(0.3f);
                else
                {
                    won = true;
                    endTimer = 0.6f;
                }
            }
        }

        void Lose(float delay)
        {
            lost = true;
            endTimer = delay;
        }

        void Win()
        {
            var outcome = new StageOutcome
            {
                Xp = world.Xp,
                Dinars = world.Coins * 5,
                LeavesMask = world.LeavesMask,
                Honor = world.Honor,
                HonorNote = world.HonorNote,
                Flags = new List<string>(world.Flags),
            };
            Game.StageWon(Stage, outcome);
        }

        // ------------------------------------------------------------- building

        protected override void Build(RectTransform canvas)
        {
            if (world == null)
                return;
            floaters.Clear();
            BuildTopBar(canvas);
            BuildTip(canvas);
            controls = UIKit.Rect(canvas, "Controls");
            UIKit.Stretch(controls);
            if (Stage.Kind == StageKind.Walk)
                BuildWalkControls(controls);
            else
                BuildRideControls(controls);
            controls.gameObject.SetActive(dialogue == null);
            if (dialogue != null)
                ShowDialogue();
            RefreshGoal();
            RefreshTip();
            UpdateHud();
            if (Pause.IsPaused)
            {
                // Rebuilt while paused (the language changed in settings): bring the pause panel back.
                overlay = null;
                Pause.Set(false);
                OpenPause();
            }
        }

        void BuildTopBar(RectTransform canvas)
        {
            var status = UIKit.Rect(canvas, "Status");
            UIKit.Place(status, new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(560f, 150f));
            if (Stage.Kind == StageKind.Walk)
            {
                var portrait = UIKit.Portrait(status, "siavosh", 120f, Palette.Paper, Loc.IsRtl);
                UIKit.Place(portrait, new Vector2(0f, 1f), Vector2.zero, new Vector2(120f, 120f));
                health = UIKit.Bar(status, new Vector2(380f, 36f), Palette.Vermilion, Palette.Hex(0x2B1B12, 0.85f));
                UIKit.Place(health.transform.parent as RectTransform, new Vector2(0f, 1f), new Vector2(140f, -14f), new Vector2(380f, 36f));
                if (stats.HasFarr)
                {
                    farr = UIKit.Bar(status, new Vector2(300f, 26f), Palette.GoldLight, Palette.Hex(0x2B1B12, 0.85f));
                    UIKit.Place(farr.transform.parent as RectTransform, new Vector2(0f, 1f), new Vector2(140f, -60f), new Vector2(300f, 26f));
                    farrLabel = UIKit.Label(status, Rules.Farr[0].Name, 22, Palette.GoldLight, TextAnchor.MiddleLeft, true, true);
                    UIKit.Place(farrLabel.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -92f), new Vector2(380f, 30f));
                }
            }
            else
            {
                hearts = new Image[world.Rider.MaxHearts];
                for (var i = 0; i < hearts.Length; i++)
                {
                    hearts[i] = UIKit.Icon(status, "heart", Palette.Vermilion, 64f);
                    UIKit.Place(hearts[i].rectTransform, new Vector2(0f, 1f), new Vector2(i * 74f, 0f), new Vector2(64f, 64f));
                }
            }

            var goalPanel = UIKit.Panel(canvas, "Goal", Palette.Hex(0x2B1B12, 0.78f), 30, true);
            UIKit.Centered(goalPanel.rectTransform, Vector2.zero, new Vector2(900f, 64f));
            goalPanel.rectTransform.anchorMin = goalPanel.rectTransform.anchorMax = goalPanel.rectTransform.pivot = new Vector2(0.5f, 1f);
            goalPanel.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            goal = UIKit.Label(goalPanel.transform, "", 30, Palette.Paper, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(goal.rectTransform);

            if (Stage.Kind == StageKind.Ride)
            {
                progress = UIKit.Bar(canvas, new Vector2(700f, 24f), Palette.Saffron, Palette.Hex(0x2B1B12, 0.8f));
                var frame = progress.transform.parent as RectTransform;
                frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 1f);
                frame.anchoredPosition = new Vector2(0f, -104f);
                progress.fillOrigin = 0;
                progressHorse = UIKit.Icon(frame, "shoe", Palette.GoldLight, 44f).rectTransform;
            }

            var right = UIKit.Rect(canvas, "Score");
            UIKit.Place(right, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(640f, 110f));
            var pause = UIKit.IconButton(right, "pause", OpenPause, 100f, Palette.Hex(0x2B1B12, 0.85f), Palette.GoldLight);
            UIKit.Place((RectTransform)pause.transform, new Vector2(1f, 1f), Vector2.zero, new Vector2(100f, 100f));
            leaves = new Image[3];
            for (var i = 0; i < 3; i++)
            {
                leaves[i] = UIKit.Picture(right, "props/leaf", 70f);
                UIKit.Place(leaves[i].rectTransform, new Vector2(1f, 1f), new Vector2(-130f - (2 - i) * 62f, -12f), leaves[i].rectTransform.sizeDelta);
            }
            var coin = UIKit.Picture(right, "props/coin", 52f);
            UIKit.Place(coin.rectTransform, new Vector2(1f, 1f), new Vector2(-340f, -22f), coin.rectTransform.sizeDelta);
            coins = UIKit.Label(right, "0", 36, Palette.Paper, TextAnchor.MiddleRight, true, true);
            UIKit.Place(coins.rectTransform, new Vector2(1f, 1f), new Vector2(-400f, -20f), new Vector2(160f, 56f));
        }

        void BuildTip(RectTransform canvas)
        {
            tipBox = UIKit.Panel(canvas, "Tip", Palette.Paper, 26, true, true).rectTransform;
            tipBox.anchorMin = tipBox.anchorMax = tipBox.pivot = new Vector2(0.5f, 1f);
            tipBox.anchoredPosition = new Vector2(0f, Stage.Kind == StageKind.Ride ? -150f : -110f);
            tipBox.sizeDelta = new Vector2(1100f, 110f);
            var icon = UIKit.Portrait(tipBox, "rostam", 96f, Palette.Saffron, Loc.IsRtl);
            UIKit.Place(icon, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(96f, 96f));
            tipText = UIKit.Label(tipBox, "", 30, Palette.Ink, TextAnchor.MiddleLeft, true);
            UIKit.Stretch(tipText.rectTransform);
            tipText.rectTransform.offsetMin = new Vector2(Loc.IsRtl ? 20f : 124f, 6f);
            tipText.rectTransform.offsetMax = new Vector2(Loc.IsRtl ? -124f : -20f, -6f);
            tipBox.gameObject.SetActive(false);
        }

        void RefreshTip()
        {
            if (tipBox == null)
                return;
            tipBox.gameObject.SetActive(currentTip != null && tipTime > 0f);
            if (currentTip != null)
            {
                var lines = UIKit.WrapLines(tipText, currentTip.Raw, 940f);
                tipText.text = Loc.Display(string.Join("\n", lines));
                tipBox.sizeDelta = new Vector2(1100f, Mathf.Max(110f, 30f + lines.Count * 40f));
            }
        }

        void RefreshGoal()
        {
            if (goal == null)
                return;
            var text = Stage.Goal.Raw.Contains("{0}") ? Stage.Goal.RawFormat(world.GoalDone, world.GoalNeeded) : Stage.Goal.Raw;
            goal.text = Loc.Display(text);
            goal.color = world.GoalNeeded > 0 && world.GoalDone >= world.GoalNeeded ? Palette.GoldLight : Palette.Paper;
        }

        RectTransform PadButton(RectTransform parent, Act act, string icon, Vector2 position, float size, Color fill)
        {
            var disc = UIKit.Rect(parent, act.ToString()).gameObject.AddComponent<Image>();
            disc.sprite = Art.Circle;
            disc.color = fill;
            UIKit.PlaceFixed(disc.rectTransform, new Vector2(1f, 0f), position, new Vector2(size, size));
            UIKit.RingImage(disc.transform, Palette.Gold);
            UIKit.Icon(disc.transform, icon, Palette.White, size * 0.5f);
            var pad = disc.gameObject.AddComponent<PadButton>();
            pad.Act = act;
            Controls.Register(pad);
            return disc.rectTransform;
        }

        void BuildWalkControls(RectTransform root)
        {
            var stickArea = UIKit.Rect(root, "Stick").gameObject.AddComponent<Image>();
            stickArea.sprite = Art.Circle;
            stickArea.color = new Color(0.17f, 0.11f, 0.07f, 0.35f);
            UIKit.PlaceFixed(stickArea.rectTransform, new Vector2(0f, 0f), new Vector2(60f, 50f), new Vector2(300f, 300f));
            UIKit.RingImage(stickArea.transform, Palette.WithAlpha(Palette.Paper, 0.6f));
            var knob = UIKit.Fill(stickArea.transform, "Knob", Palette.WithAlpha(Palette.Paper, 0.85f));
            knob.sprite = Art.Circle;
            knob.type = Image.Type.Simple;
            UIKit.Centered(knob.rectTransform, Vector2.zero, new Vector2(130f, 130f));
            var stick = stickArea.gameObject.AddComponent<PadStick>();
            stick.Knob = knob.rectTransform;
            stick.Radius = 110f;
            Controls.Register(stick);

            var dark = Palette.Hex(0x2B1B12, 0.6f);
            PadButton(root, Act.Attack, "sword", new Vector2(-60f, 60f), 180f, Palette.WithAlpha(Palette.Vermilion, 0.85f));
            PadButton(root, Act.Jump, "jump", new Vector2(-265f, 40f), 135f, dark);
            PadButton(root, Act.Dodge, "dodge", new Vector2(-420f, 90f), 115f, dark);
            PadButton(root, Act.Shield, "shield", new Vector2(-70f, 268f), 125f, Palette.WithAlpha(Palette.Lapis, 0.75f));
            if (Stage.BowAllowed)
                PadButton(root, Act.Bow, "bow", new Vector2(-240f, 215f), 125f, dark);
            if (stats.HasFarr)
                farrButton = PadButton(root, Act.Farr, "palm", new Vector2(-400f, 255f), 120f, Palette.WithAlpha(Palette.Gold, 0.85f));

            interactButton = PadButton(root, Act.Interact, "hand", new Vector2(-250f, 390f), 130f, Palette.WithAlpha(Palette.Turquoise, 0.9f));
            interactLabel = UIKit.Label(interactButton, "", 28, Palette.Paper, TextAnchor.MiddleCenter, true, true);
            UIKit.PlaceFixed(interactLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 40f), new Vector2(420f, 44f));
            interactButton.gameObject.SetActive(false);
        }

        void BuildRideControls(RectTransform root)
        {
            var duck = PadButton(root, Act.Duck, "duck", new Vector2(0f, 0f), 200f, Palette.Hex(0x2B1B12, 0.6f));
            duck.anchorMin = duck.anchorMax = duck.pivot = new Vector2(0f, 0f);
            duck.anchoredPosition = new Vector2(60f, 50f);
            PadButton(root, Act.Jump, "jump", new Vector2(-60f, 50f), 200f, Palette.WithAlpha(Palette.Vermilion, 0.85f));
            if (Stage.BowAllowed)
                PadButton(root, Act.Bow, "bow", new Vector2(-300f, 70f), 160f, Palette.WithAlpha(Palette.Lapis, 0.85f));
        }

        // ------------------------------------------------------------- per frame

        void UpdateHud()
        {
            if (world == null || goal == null)
                return;
            if (world.Hero != null && health != null)
            {
                health.fillAmount = world.Hero.Health / Mathf.Max(1f, world.Hero.MaxHealth);
                if (farr != null)
                {
                    farr.fillAmount = world.Hero.Farr;
                    farr.color = world.Hero.FarrReady ? Color.Lerp(Palette.GoldLight, Palette.White, Mathf.PingPong(Time.time * 2f, 1f)) : Palette.GoldLight;
                }
                if (farrButton != null)
                {
                    var ready = world.Hero.FarrReady;
                    farrButton.GetComponent<Image>().color = ready ? Palette.Gold : Palette.WithAlpha(Palette.Smoke, 0.6f);
                    farrButton.localScale = Vector3.one * (ready ? 1f + Mathf.PingPong(Time.time, 0.12f) : 1f);
                }
                var offer = world.Interaction;
                if (interactButton != null)
                {
                    interactButton.gameObject.SetActive(offer != null);
                    if (offer != null)
                        interactLabel.text = offer.Prompt.ToString();
                }
            }
            if (world.Rider != null && hearts != null)
            {
                for (var i = 0; i < hearts.Length; i++)
                    hearts[i].color = i < world.Rider.Hearts ? Palette.Vermilion : Palette.WithAlpha(Palette.Smoke, 0.5f);
                if (progress != null)
                {
                    progress.fillAmount = world.Rider.Progress;
                    progressHorse.anchoredPosition = new Vector2((world.Rider.Progress - 0.5f) * 680f, 0f);
                }
            }
            coins.text = Loc.Number(world.Coins * 5);
            var saved = SaveSystem.Data.Stage(Stage.Id).leaves;
            for (var i = 0; i < 3; i++)
            {
                var now = (world.LeavesMask & (1 << i)) != 0;
                var before = (saved & (1 << i)) != 0;
                leaves[i].color = now ? Color.white : before ? new Color(1f, 1f, 1f, 0.45f) : new Color(0.3f, 0.25f, 0.2f, 0.35f);
            }
        }

        void AddFloater(Vector3 at, string text, Color color)
        {
            if (Canvas == null)
                return;
            var label = UIKit.Label(Canvas, text, 40, color, TextAnchor.MiddleCenter, true, true);
            label.rectTransform.sizeDelta = new Vector2(900f, 60f);
            floaters.Add(new Floater { Label = label, World = at });
            PositionFloater(floaters[floaters.Count - 1]);
        }

        void UpdateFloaters()
        {
            for (var i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                if (f.Label == null)
                {
                    floaters.RemoveAt(i);
                    continue;
                }
                f.Age += Time.unscaledDeltaTime;
                f.World += Vector3.up * Time.unscaledDeltaTime * 0.9f;
                PositionFloater(f);
                UIKit.SetAlpha(f.Label, Mathf.Clamp01(1.6f - f.Age));
                if (f.Age > 1.6f)
                {
                    Destroy(f.Label.gameObject);
                    floaters.RemoveAt(i);
                }
            }
        }

        void PositionFloater(Floater f)
        {
            var screen = Game.Instance.Camera.WorldToScreenPoint(f.World);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas, screen, null, out local);
            f.Label.rectTransform.anchorMin = f.Label.rectTransform.anchorMax = f.Label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            f.Label.rectTransform.anchoredPosition = local;
        }

        // ------------------------------------------------------------- dialogue

        void ShowDialogue()
        {
            if (dialogueBox != null)
                Destroy(dialogueBox.gameObject);
            if (dialogue == null || dialogue.Count == 0)
            {
                dialogue = null;
                dialogueBox = null;
                if (controls != null)
                    controls.gameObject.SetActive(true);
                return;
            }
            if (Canvas == null)
                return;
            if (controls != null)
                controls.gameObject.SetActive(false);
            var beat = dialogue.Peek();
            dialogueBox = UIKit.Overlay(Canvas, new Color(0f, 0f, 0f, 0.25f)).rectTransform;
            var tap = dialogueBox.gameObject.AddComponent<Button>();
            tap.targetGraphic = dialogueBox.GetComponent<Image>();
            tap.onClick.AddListener(() =>
            {
                AudioService.Play(Sfx.Page, 0.4f);
                dialogue.Dequeue();
                ShowDialogue();
            });
            CutscenePlayer.Subtitle(dialogueBox, beat.Speaker, beat.Text, true);
        }

        // ------------------------------------------------------------- pause and defeat

        void OpenPause()
        {
            if (overlay != null || won || lost)
                return;
            Pause.Set(true);
            overlay = UIKit.Overlay(Canvas, Palette.Shade).rectTransform;
            var panel = UIKit.Panel(overlay, "Pause", Palette.Paper, 30, true, true);
            UIKit.Centered(panel.rectTransform, Vector2.zero, new Vector2(700f, 720f));
            var title = UIKit.Label(panel.transform, Ui.Paused, 56, Palette.Lapis, TextAnchor.MiddleCenter, true);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 80f));
            var buttons = new[]
            {
                new System.Tuple<LocText, System.Action, ButtonStyle>(Ui.Resume, ClosePause, ButtonStyle.Primary),
                new System.Tuple<LocText, System.Action, ButtonStyle>(Ui.Restart, () => Game.PlayStage(Stage), ButtonStyle.Secondary),
                new System.Tuple<LocText, System.Action, ButtonStyle>(Ui.Settings, OpenSettings, ButtonStyle.Secondary),
                new System.Tuple<LocText, System.Action, ButtonStyle>(Ui.LeaveStage, Game.ShowMap, ButtonStyle.Dark),
            };
            for (var i = 0; i < buttons.Length; i++)
            {
                var b = UIKit.Button(panel.transform, buttons[i].Item1, buttons[i].Item2, buttons[i].Item3, 38);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0f, -150f - i * 130f), new Vector2(520f, 110f));
            }
        }

        void ClosePause()
        {
            if (overlay != null)
                Destroy(overlay.gameObject);
            overlay = null;
            Pause.Set(false);
        }

        void ShowDefeat()
        {
            AudioService.SetMood(MusicMood.Calm);
            overlay = UIKit.Overlay(Canvas, Palette.Shade).rectTransform;
            var panel = UIKit.Panel(overlay, "Defeat", Palette.Paper, 30, true, true);
            UIKit.Centered(panel.rectTransform, Vector2.zero, new Vector2(1000f, 560f));
            var hunt = Stage.GoalKind == GoalKind.Hunt && world.Rider != null && !world.Rider.Fallen;
            var heading = hunt ? Ui.HuntFailed : world.Rider != null ? Ui.DefeatedRide : Ui.Defeated;
            var title = UIKit.Label(panel.transform, heading, 52, Palette.Vermilion, TextAnchor.MiddleCenter, true);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(900f, 80f));
            var portrait = UIKit.Portrait(panel.transform, "rostam", 140f, Palette.Saffron, Loc.IsRtl);
            UIKit.PlaceFixed(portrait, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(140f, 140f));
            var hint = UIKit.Wrapped(panel.transform, Ui.DefeatHint.Raw, 32, Palette.Ink, 860f, TextAnchor.MiddleCenter);
            UIKit.PlaceFixed(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -310f), new Vector2(900f, 60f));
            var again = UIKit.Button(panel.transform, Ui.TryAgain, () => Game.PlayStage(Stage), ButtonStyle.Primary, 38);
            UIKit.PlaceFixed((RectTransform)again.transform, new Vector2(0.5f, 0f), new Vector2(200f, 40f), new Vector2(360f, 110f));
            var map = UIKit.Button(panel.transform, Ui.LeaveStage, Game.ShowMap, ButtonStyle.Secondary, 32);
            UIKit.PlaceFixed((RectTransform)map.transform, new Vector2(0.5f, 0f), new Vector2(-200f, 40f), new Vector2(360f, 110f));
        }

        public override void OnBack()
        {
            if (dialogue != null)
                return;
            if (overlay != null && !lost)
                ClosePause();
            else if (!lost)
                OpenPause();
        }
    }
}
