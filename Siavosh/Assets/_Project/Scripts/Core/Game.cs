using System;
using System.Collections;
using Siavosh.Audio;
using Siavosh.Localization;
using Siavosh.Play;
using Siavosh.Story;
using Siavosh.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Siavosh.Core
{
    /// <summary>
    /// The whole game lives under one object created at startup in the (empty) Main scene: the camera,
    /// audio and whichever screen is showing. Screens are swapped with a short fade. This class also
    /// holds the story flow: what "Continue" plays next, chapter intros and outros, and stage results.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }

        public Camera Camera { get; private set; }

        GameObject current;
        RectTransform fadeCanvas;
        Image fade;
        bool fading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance == null)
                new GameObject("Siavosh").AddComponent<Game>();
        }

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;
            Loc.Apply(Settings.Language);

            var cam = new GameObject("Camera") { tag = "MainCamera" };
            cam.transform.SetParent(transform, false);
            cam.transform.position = new Vector3(0f, 3f, -10f);
            Camera = cam.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.orthographicSize = 5.4f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Palette.LapisNight;
            cam.AddComponent<AudioListener>();

            AudioService.Create(transform);

            fadeCanvas = UIKit.Canvas(transform, "Fade", 1000);
            fade = UIKit.Overlay(fadeCanvas, Color.black);
            fade.raycastTarget = false;
            UIKit.SetAlpha(fade, 1f);
        }

        void Start()
        {
            if (!Settings.LanguageChosen)
                Show<LanguageScreen>(null);
            else
                ShowMenu();
        }

        void Update()
        {
            // Android back button.
            if (Input.GetKeyDown(KeyCode.Escape) && current != null && !fading)
            {
                var handler = current.GetComponent<IBackHandler>();
                if (handler != null)
                    handler.OnBack();
            }
        }

        // ------------------------------------------------------------- screens

        /// <summary>Fades out, replaces the current screen with a new <typeparamref name="T"/>, fades in.</summary>
        public static T Show<T>(Action<T> setup) where T : Component
        {
            return Instance.ShowInternal(setup);
        }

        T ShowInternal<T>(Action<T> setup) where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var screen = go.AddComponent<T>();
            if (setup != null)
                setup(screen);
            StartCoroutine(SwapTo(go));
            return screen;
        }

        IEnumerator SwapTo(GameObject next)
        {
            while (fading)
                yield return null;
            fading = true;
            fade.raycastTarget = true;
            yield return FadeRoutine(1f, 0.25f);
            Pause.Set(false);
            if (current != null)
                Destroy(current);
            current = next;
            next.SetActive(true);
            yield return null; // let the new screen build
            yield return FadeRoutine(0f, 0.35f);
            fade.raycastTarget = false;
            fading = false;
        }

        IEnumerator FadeRoutine(float target, float seconds)
        {
            var start = fade.color.a;
            for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                UIKit.SetAlpha(fade, Mathf.Lerp(start, target, t / seconds));
                yield return null;
            }
            UIKit.SetAlpha(fade, target);
        }

        // ------------------------------------------------------------- flow

        public static void ShowMenu()
        {
            AudioService.SetMood(MusicMood.Calm);
            Show<MenuScreen>(null);
        }

        public static void ShowMap()
        {
            AudioService.SetMood(MusicMood.Calm);
            Show<MapScreen>(null);
        }

        public static void ShowCamp(int tab)
        {
            AudioService.SetMood(MusicMood.Calm);
            Show<CampScreen>(s => s.Tab = tab);
        }

        public static void ShowStoryBook()
        {
            AudioService.SetMood(MusicMood.Calm);
            Show<StoryBookScreen>(null);
        }

        /// <summary>Plays a cutscene, then runs <paramref name="then"/> (the map if null).</summary>
        public static void PlayCutscene(Cutscene cutscene, Action then)
        {
            AudioService.SetMood(MusicMood.Calm);
            Show<CutscenePlayer>(p =>
            {
                p.Cutscene = cutscene;
                p.Finished = () =>
                {
                    SaveSystem.Data.MarkSeen(cutscene.Id);
                    SaveSystem.Save();
                    (then ?? ShowMap)();
                };
            });
        }

        /// <summary>Runs <paramref name="then"/>, first playing <paramref name="cutscene"/> if it has not been seen.</summary>
        public static Action WithCutscene(Cutscene cutscene, Action then)
        {
            if (cutscene == null || SaveSystem.Data.HasSeen(cutscene.Id))
                return then;
            return () => PlayCutscene(cutscene, then);
        }

        /// <summary>"Continue the tale": the first unfinished stage, with any story that comes before it.</summary>
        public static void Continue()
        {
            var next = Catalog.NextStage(SaveSystem.Data);
            if (next == null)
                ShowMap();
            else
                PlayStage(next);
        }

        public static void PlayStage(StageDef stage)
        {
            var chapter = Catalog.ChapterOf(stage);
            Action start = () =>
            {
                AudioService.SetMood(stage.Kind == StageKind.Ride ? MusicMood.Ride : MusicMood.Battle);
                Show<StageScreen>(s => s.Stage = stage);
            };
            if (chapter != null && chapter.Stages.Count > 0 && chapter.Stages[0] == stage)
                start = WithCutscene(chapter.Intro, start);
            if (stage == Catalog.FirstStage)
                start = WithCutscene(Script.Prologue, start);
            start();
        }

        /// <summary>Called by the stage when it is won. Records progress and shows the result.</summary>
        public static void StageWon(StageDef stage, StageOutcome outcome)
        {
            var save = SaveSystem.Data;
            var levelBefore = save.Level;
            var record = save.Stage(stage.Id);
            var firstClear = !record.done;
            record.done = true;
            record.leaves |= outcome.LeavesMask;
            var xp = outcome.Xp + (firstClear ? stage.XpReward : stage.XpReward / 4);
            var dinars = outcome.Dinars + (firstClear ? stage.DinarReward : stage.DinarReward / 4);
            save.xp += xp;
            save.dinars += dinars;
            save.honor += outcome.Honor;
            if (outcome.Flags != null)
                foreach (var flag in outcome.Flags)
                    save.Set(flag);
            if (stage.GrantsSkill != null && !save.Knows(stage.GrantsSkill))
                save.skills.Add(stage.GrantsSkill);
            if (stage.GrantsFarr != null && !save.HasFarr(stage.GrantsFarr))
                save.farr.Add(stage.GrantsFarr);
            SaveSystem.Save();

            var result = new StageResult
            {
                Stage = stage,
                Xp = xp,
                Dinars = dinars,
                Honor = outcome.Honor,
                LeavesMask = record.leaves,
                NewLeaves = outcome.LeavesMask,
                LevelUp = save.Level > levelBefore,
                HonorNote = outcome.HonorNote,
            };
            AudioService.SetMood(MusicMood.Calm);
            Show<ResultScreen>(r => r.Result = result);
        }

        /// <summary>After the result screen: the stage's closing story, then the map or the next stage.</summary>
        public static void AfterResult(StageDef stage)
        {
            var chapter = Catalog.ChapterOf(stage);
            Action then = ShowMap;
            if (chapter != null && chapter.Stages.Count > 0 && chapter.Stages[chapter.Stages.Count - 1] == stage)
                then = WithCutscene(chapter.Outro, ShowMap);
            if (stage.Outro != null)
                then = WithCutscene(stage.Outro, then);
            then();
        }
    }

    /// <summary>Screens that react to the Android back button.</summary>
    public interface IBackHandler
    {
        void OnBack();
    }
}
