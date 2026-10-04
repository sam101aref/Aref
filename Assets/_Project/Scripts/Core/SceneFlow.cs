using Arash.Levels;
using UnityEngine.SceneManagement;

namespace Arash.Core
{
    /// <summary>Moves between the game's scenes and remembers which level is being played.</summary>
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string WorldMapScene = "WorldMap";
        public const string BattleScene = "Battle";

        /// <summary>The level the Battle scene plays. Null means the first level.</summary>
        public static LevelDefinition CurrentLevel { get; private set; }

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
            Load(BattleScene);
        }

        public static void Retry()
        {
            Load(SceneManager.GetActiveScene().name);
        }

        static void Load(string scene)
        {
            GamePause.Resume();
            SceneManager.LoadScene(scene);
        }
    }
}
