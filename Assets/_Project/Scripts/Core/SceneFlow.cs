using System;
using Arash.Levels;
using Arash.Story;
using UnityEngine.SceneManagement;

namespace Arash.Core
{
    /// <summary>
    /// Moves between the game's scenes and remembers what to play: the current level, and a cutscene
    /// with what comes after it. A chapter's intro cutscene plays automatically the first time its
    /// first level starts, and each level's own story scene the first time that level starts.
    /// </summary>
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string WorldMapScene = "WorldMap";
        public const string BattleScene = "Battle";
        public const string CutsceneScene = "Cutscene";
        public const string FinalFlightScene = "FinalFlight";

        /// <summary>The level being played. Null means the first level.</summary>
        public static LevelDefinition CurrentLevel { get; private set; }

        /// <summary>The cutscene the Cutscene scene plays.</summary>
        public static CutsceneDefinition PendingCutscene { get; private set; }

        static Action afterCutscene;

        public static void ToMainMenu()
        {
            Load(MainMenuScene);
        }

        public static void ToWorldMap()
        {
            Load(WorldMapScene);
        }

        public static void Play(LevelDefinition level)
        {
            CurrentLevel = level;
            var catalog = LevelCatalog.Load();
            var chapter = catalog != null ? catalog.ChapterOf(level) : null;
            var chapterIntro = chapter != null && chapter.levels.Count > 0 && chapter.levels[0] == level ? chapter.introCutscene : null;

            // Chapter story first, then the level's own scene (F-60), each only the first time.
            Action load = () => LoadLevelScene(level);
            var then = WithCutscene(level != null ? level.introCutscene : null, load);
            WithCutscene(chapterIntro, then)();
        }

        /// <summary>Plays a cutscene, then runs <paramref name="then"/> (the world map if null).</summary>
        public static void PlayCutscene(CutsceneDefinition cutscene, Action then)
        {
            PendingCutscene = cutscene;
            afterCutscene = then;
            Load(CutsceneScene);
        }

        /// <summary>Runs <paramref name="then"/>, first playing <paramref name="cutscene"/> if it has not been seen.</summary>
        public static Action WithCutscene(CutsceneDefinition cutscene, Action then)
        {
            if (cutscene == null || SaveSystem.Data.HasSeen(cutscene.id))
                return then;
            return () => PlayCutscene(cutscene, then);
        }

        /// <summary>Called by the cutscene player when the cutscene ends or is skipped.</summary>
        public static void FinishCutscene()
        {
            if (PendingCutscene != null)
            {
                SaveSystem.Data.MarkSeen(PendingCutscene.id);
                SaveSystem.Save();
            }
            var then = afterCutscene ?? ToWorldMap;
            PendingCutscene = null;
            afterCutscene = null;
            then();
        }

        public static void Retry()
        {
            Load(SceneManager.GetActiveScene().name);
        }

        static void LoadLevelScene(LevelDefinition level)
        {
            Load(level != null && level.mode == LevelMode.Flight ? FinalFlightScene : BattleScene);
        }

        static void Load(string scene)
        {
            GamePause.Resume();
            SceneManager.LoadScene(scene);
        }
    }
}
