using System.Collections;
using Arash.Core;
using Arash.Localization;
using Arash.UI;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Hit feedback (F-10): camera shake on every hit, and for headshots a short slow motion,
    /// a "Headshot!" pop-up and a vibration.
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        [SerializeField] BattleCamera battleCamera;
        [SerializeField] BattleHud hud;

        [Header("Shake")]
        [SerializeField] float hitShake = 0.12f;
        [SerializeField] float killShake = 0.3f;
        [SerializeField] float shakeDuration = 0.35f;

        [Header("Headshot")]
        [SerializeField, Range(0.05f, 1f)] float slowMotionScale = 0.25f;
        [SerializeField, Tooltip("Real-time seconds of slow motion.")]
        float slowMotionDuration = 0.8f;
        [SerializeField] bool vibrate = true;

        Coroutine slowMotion;

        void OnEnable()
        {
            Health.AnyDamaged += OnDamaged;
        }

        void OnDisable()
        {
            Health.AnyDamaged -= OnDamaged;
            if (!GamePause.IsPaused)
                Time.timeScale = 1f;
        }

        void OnDamaged(Health health, DamageInfo info, bool killed)
        {
            if (battleCamera != null)
                battleCamera.Shake(killed ? killShake : hitShake, shakeDuration);

            // Slow motion and the pop-up celebrate the player's headshots, not the enemies'.
            var victim = health.GetComponent<Combatant>();
            if (!info.IsHeadshot || victim == null || victim.Team != Team.Enemy)
                return;

            if (hud != null)
                hud.ShowPopup(Loc.T("battle.headshot"));
            if (slowMotion != null)
                StopCoroutine(slowMotion);
            slowMotion = StartCoroutine(SlowMotion());
            if (vibrate && GameSettings.Vibration)
                Vibrate();
        }

        IEnumerator SlowMotion()
        {
            if (!GamePause.IsPaused)
                Time.timeScale = slowMotionScale;
            yield return new WaitForSecondsRealtime(slowMotionDuration);
            if (!GamePause.IsPaused)
                Time.timeScale = 1f;
            slowMotion = null;
        }

        static void Vibrate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
