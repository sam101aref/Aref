using UnityEngine;

namespace Arash.Core
{
    /// <summary>Pauses gameplay by stopping time. Other systems check <see cref="IsPaused"/>.</summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        public static void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
        }

        public static void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }
    }
}
