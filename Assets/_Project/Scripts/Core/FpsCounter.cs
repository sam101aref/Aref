using UnityEngine;

namespace Arash.Core
{
    /// <summary>
    /// Frame-rate readout for development builds (F-36): shows the average fps and the slowest frame
    /// of the last second in the corner. Never appears in release builds.
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        float elapsed;
        int frames;
        float worst;
        string text = string.Empty;
        GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
                return;
            var go = new GameObject("FPS");
            DontDestroyOnLoad(go);
            go.AddComponent<FpsCounter>();
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            elapsed += dt;
            frames++;
            worst = Mathf.Max(worst, dt);
            if (elapsed < 1f)
                return;
            text = string.Format("{0:0} fps  (worst {1:0} ms)", frames / elapsed, worst * 1000f);
            elapsed = 0f;
            frames = 0;
            worst = 0f;
        }

        void OnGUI()
        {
            if (style == null)
                style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 45) };
            GUI.Label(new Rect(10f, Screen.height - style.fontSize * 2f, 600f, style.fontSize * 1.6f), text, style);
        }
    }
}
