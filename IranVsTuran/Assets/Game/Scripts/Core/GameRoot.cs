using System;
using System.Collections.Generic;
using IranVsTuran.Audio;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using IranVsTuran.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IranVsTuran.Core
{
    /// <summary>
    /// The whole game runs in one scene. GameRoot is created automatically after the scene loads:
    /// it sets up the camera, audio, the UI canvas and the store/ads backends, then shows screens
    /// (menu, map, battle, cutscenes…) one at a time. Popups stack on top; Android's back button
    /// closes the top popup or goes back one screen.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        public Camera Camera { get; private set; }
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform PopupLayer { get; private set; }

        ScreenBase current;
        readonly List<Popup> popups = new List<Popup>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null)
                return;
            var go = new GameObject("GameRoot");
            DontDestroyOnLoad(go);
            go.AddComponent<GameRoot>();
        }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Loc.SetLanguage(GameSettings.Language);
            Loc.Changed += OnLanguageChanged;

            Camera = SetUpCamera();
            AudioService.Ensure(transform);
            ScreenLayer = UIKit.Canvas(transform, "Screens", 0);
            PopupLayer = UIKit.Canvas(transform, "Popups", 100);

            if (Monetization.Store == null)
                Monetization.Store = new TestStore();
            if (Monetization.Ads == null)
                Monetization.Ads = new TestAds();
        }

        void Start()
        {
            Show(new MainMenuScreen());
        }

        Camera SetUpCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.15f);
            FitCamera(camera);
            return camera;
        }

        /// <summary>Shows the whole 19.2 × 10.8 battlefield on any screen shape.</summary>
        public static void FitCamera(Camera camera)
        {
            var aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9f;
            camera.orthographicSize = Mathf.Max(5.4f, 9.6f / aspect);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Back();
        }

        public void Back()
        {
            if (popups.Count > 0)
            {
                var top = popups[popups.Count - 1];
                if (top.Dismissable)
                    top.Close();
                return;
            }
            if (current != null)
                current.OnBack();
        }

        // ------------------------------------------------------------ screens

        public void Show(ScreenBase screen)
        {
            CloseAllPopups();
            if (current != null)
                current.Dispose();
            current = screen;
            current.Open(this);
        }

        public ScreenBase Current { get { return current; } }

        void OnLanguageChanged()
        {
            // Rebuild everything so text, alignment and mirroring follow the new language.
            for (var i = popups.Count - 1; i >= 0; i--)
                popups[i].Close();
            if (current != null)
                current.Rebuild();
        }

        // ------------------------------------------------------------ popups

        internal void Register(Popup popup)
        {
            popups.Add(popup);
        }

        internal void Unregister(Popup popup)
        {
            popups.Remove(popup);
        }

        public void CloseAllPopups()
        {
            for (var i = popups.Count - 1; i >= 0; i--)
                popups[i].Close();
        }

        public bool HasPopup { get { return popups.Count > 0; } }

        /// <summary>A short message at the bottom of the screen.</summary>
        public void Toast(string displayText)
        {
            Toasts.Show(PopupLayer, displayText);
        }

        // ------------------------------------------------------------ flow helpers

        /// <summary>Plays a cutscene once (or again if <paramref name="always"/>), then continues.</summary>
        public void PlayCutscene(string id, Action then, bool always = false)
        {
            var scene = StoryDefs.Get(id);
            if (scene == null || (!always && SaveSystem.Data.HasSeen(id)))
            {
                if (then != null)
                    then();
                return;
            }
            Show(new CutsceneScreen(scene, () =>
            {
                SaveSystem.Data.MarkSeen(id);
                SaveSystem.Save();
                if (then != null)
                    then();
            }));
        }

        public void ToMainMenu()
        {
            Show(new MainMenuScreen());
        }

        public void ToMap()
        {
            Show(new WorldMapScreen());
        }

        /// <summary>Starts a level, after its chapter's intro the first time.</summary>
        public void StartLevel(LevelDef level, int difficulty)
        {
            var chapter = LevelDefs.ChapterOf(level);
            Action start = () => Show(new BattleScreen(level, difficulty));
            if (chapter.levels[0] == level.id)
                PlayCutscene(chapter.intro, start);
            else
                start();
        }
    }
}
