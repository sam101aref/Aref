using System.Collections;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Shot loop for the practice range: aim → follow the arrow → pause where it lands → back to the archer.
    /// The turn system (F-08) will take over this flow for real battles.
    /// </summary>
    public class PracticeRange : MonoBehaviour
    {
        [SerializeField] AimController aim;
        [SerializeField] BattleCamera battleCamera;
        [SerializeField, Tooltip("Seconds the camera lingers where the arrow landed.")]
        float landingPause = 0.8f;

        void OnEnable()
        {
            if (aim != null)
                aim.Shot += OnShot;
        }

        void OnDisable()
        {
            if (aim != null)
                aim.Shot -= OnShot;
        }

        void OnShot(Arrow arrow)
        {
            aim.enabled = false;
            if (battleCamera != null)
                battleCamera.Follow(arrow);

            arrow.Stuck += (a, hit) => StartCoroutine(FinishShot());
            arrow.Lost += a => StartCoroutine(FinishShot());
        }

        IEnumerator FinishShot()
        {
            yield return new WaitForSeconds(landingPause);
            if (battleCamera != null)
                battleCamera.ReturnHome();
            aim.enabled = true;
        }
    }
}
