using Arash.Localization;
using UnityEngine;

namespace Arash.UI
{
    /// <summary>
    /// A screen whose UI is built in code. It is rebuilt when the language changes, and reopens the
    /// settings panel if that is where the language was changed.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        protected RectTransform Canvas { get; private set; }
        bool settingsOpen;

        protected virtual int SortingOrder { get { return 0; } }

        protected virtual void OnEnable()
        {
            Loc.Changed += Rebuild;
        }

        protected virtual void OnDisable()
        {
            Loc.Changed -= Rebuild;
        }

        protected virtual void Start()
        {
            if (Canvas == null)
                Rebuild();
        }

        protected void Rebuild()
        {
            if (Canvas != null)
                Destroy(Canvas.gameObject);
            Canvas = UIFactory.Canvas(transform, GetType().Name, SortingOrder);
            Build(Canvas);
            if (settingsOpen)
                OpenSettings();
        }

        protected abstract void Build(RectTransform canvas);

        protected void OpenSettings()
        {
            settingsOpen = true;
            SettingsPanel.Open(Canvas, () => settingsOpen = false);
        }
    }
}
