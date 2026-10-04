using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arash.Core
{
    /// <summary>Lives in the Boot scene: applies app-wide settings, then loads the first real scene.</summary>
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField, Tooltip("Scene loaded after boot. Becomes MainMenu once the menu exists (F-13).")]
        string firstScene = "Battle";
        [SerializeField] int targetFrameRate = 60;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void Start()
        {
            SceneManager.LoadScene(firstScene);
        }
    }
}
