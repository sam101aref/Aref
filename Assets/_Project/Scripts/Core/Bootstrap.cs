using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arash.Core
{
    /// <summary>Lives in the Boot scene: applies app-wide settings, then loads the first real scene.</summary>
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField, Tooltip("Scene loaded after boot.")]
        string firstScene = SceneFlow.MainMenuScene;
        [SerializeField] int targetFrameRate = 60;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Localization.Loc.Apply(GameSettings.Language); // load the save and pick the language early
        }

        void Start()
        {
            SceneManager.LoadScene(firstScene);
        }
    }
}
