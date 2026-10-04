using System.Collections;
using Arash.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Arash.UI
{
    /// <summary>
    /// Minimal battle overlay built in code: pop-up messages ("Headshot!") and the win/lose screen
    /// with tap-to-retry. Placeholder until the real HUD, end screen (F-12) and localization (F-15).
    /// </summary>
    public class BattleHud : MonoBehaviour
    {
        [SerializeField] TurnManager turnManager;
        [SerializeField] Color popupColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] float popupDuration = 1.1f;

        Text popup;
        Text result;
        Coroutine popupRoutine;
        bool canRestart;

        void Awake()
        {
            BuildCanvas();
        }

        void OnEnable()
        {
            if (turnManager != null)
                turnManager.BattleEnded += ShowResult;
        }

        void OnDisable()
        {
            if (turnManager != null)
                turnManager.BattleEnded -= ShowResult;
        }

        void Update()
        {
            if (!canRestart)
                return;
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ShowPopup(string message)
        {
            if (popupRoutine != null)
                StopCoroutine(popupRoutine);
            popupRoutine = StartCoroutine(PopupRoutine(message));
        }

        void ShowResult(bool playerWon)
        {
            result.text = playerWon ? "VICTORY\n<size=40>Tap to play again</size>" : "DEFEAT\n<size=40>Tap to try again</size>";
            result.color = playerWon ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.25f);
            result.gameObject.SetActive(true);
            StartCoroutine(EnableRestart());
        }

        IEnumerator EnableRestart()
        {
            yield return new WaitForSecondsRealtime(1f);
            canRestart = true;
        }

        IEnumerator PopupRoutine(string message)
        {
            popup.text = message;
            popup.gameObject.SetActive(true);
            for (var t = 0f; t < popupDuration; t += Time.unscaledDeltaTime)
            {
                var k = t / popupDuration;
                popup.transform.localScale = Vector3.one * (1f + 0.4f * (1f - Mathf.Clamp01(k * 4f)));
                var color = popupColor;
                color.a = 1f - Mathf.Clamp01((k - 0.7f) / 0.3f);
                popup.color = color;
                yield return null;
            }
            popup.gameObject.SetActive(false);
            popupRoutine = null;
        }

        void BuildCanvas()
        {
            var canvasObject = new GameObject("HUD Canvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            popup = CreateText(canvasObject.transform, "Popup", font, 96, new Vector2(0f, 300f));
            result = CreateText(canvasObject.transform, "Result", font, 120, Vector2.zero);
        }

        static Text CreateText(Transform parent, string name, Font font, int size, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1600f, 400f);
            rect.anchoredPosition = position;

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = true;
            text.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(3f, -3f);

            go.SetActive(false);
            return text;
        }
    }
}
