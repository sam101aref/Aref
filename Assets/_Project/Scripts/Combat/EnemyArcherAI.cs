using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Arash.Combat
{
    /// <summary>
    /// Enemy archer turn: pause, pick head or body, solve the perfect shot, draw the bow over a short
    /// animation, add an error that shrinks with every miss, and shoot.
    /// </summary>
    public class EnemyArcherAI : MonoBehaviour, ITurnController
    {
        [SerializeField] Bow bow;
        [SerializeField] ArcherRig rig;
        [SerializeField] AiAccuracy accuracy = new AiAccuracy();
        [SerializeField] float minAngle = -30f;
        [SerializeField] float maxAngle = 80f;

        [Header("Timing")]
        [SerializeField] float thinkTime = 0.5f;
        [SerializeField] float drawTime = 0.8f;
        [SerializeField] float holdTime = 0.25f;

        [Header("Power")]
        [SerializeField, Range(0f, 1f), Tooltip("Lowest draw power tried; higher powers give flatter shots.")]
        float preferredMinPower = 0.55f;

        float currentError = -1f;
        Coroutine turn;

        public float CurrentError
        {
            get { return currentError < 0f ? accuracy.initialAngleError : currentError; }
        }

        public void BeginTurn(Combatant self, Combatant opponent, Action<Arrow> onShot)
        {
            CancelTurn();
            turn = StartCoroutine(TakeShot(self, opponent, onShot));
        }

        public void CancelTurn()
        {
            if (turn != null)
                StopCoroutine(turn);
            turn = null;
        }

        void OnDisable()
        {
            CancelTurn();
        }

        IEnumerator TakeShot(Combatant self, Combatant opponent, Action<Arrow> onShot)
        {
            yield return new WaitForSeconds(thinkTime);

            var target = Random.value < accuracy.headshotChance ? opponent.HeadTarget : opponent.BodyTarget;
            var facing = self.FacingRight;

            AimState aim;
            if (!TrySolve(target.position, facing, out aim))
                aim = new AimState(true, 45f, 1f, facing ? 1f : -1f); // out of range: best effort

            // Draw the bow towards the solution.
            if (rig != null)
            {
                var from = rig.CurrentWorldAngle;
                var to = WorldAngle(aim);
                for (var t = 0f; t < drawTime; t += Time.deltaTime)
                {
                    rig.AimAtAngle(Mathf.LerpAngle(from, to, Mathf.SmoothStep(0f, 1f, t / drawTime)));
                    yield return null;
                }
                rig.Aim(aim.Direction);

                // The launch point moved with the arm; solve again from where the arrow really starts.
                AimState refined;
                if (TrySolve(target.position, facing, out refined))
                    aim = refined;
            }

            aim = accuracy.ApplyError(aim, CurrentError, Random.Range(-1f, 1f), Random.Range(-1f, 1f), minAngle, maxAngle);
            if (rig != null)
                rig.Aim(aim.Direction);

            yield return new WaitForSeconds(holdTime);

            turn = null;
            var arrow = bow != null ? bow.Fire(aim) : null;
            if (rig != null)
                rig.Relax();
            if (arrow == null)
            {
                Debug.LogError("[EnemyArcherAI] Could not fire.", this);
                yield break;
            }

            var opponentHealth = opponent.Health;
            arrow.Stuck += (a, hit) =>
            {
                var hitHealth = hit.Collider != null ? hit.Collider.GetComponentInParent<Health>() : null;
                if (hitHealth != opponentHealth)
                    currentError = accuracy.NextError(CurrentError);
            };
            arrow.Lost += a => currentError = accuracy.NextError(CurrentError);

            onShot(arrow);
        }

        bool TrySolve(Vector2 target, bool facingRight, out AimState aim)
        {
            aim = AimState.Cancelled;
            if (bow == null)
                return false;

            var facing = facingRight ? 1f : -1f;
            var offset = target - bow.LaunchPosition;
            offset.x *= facing;
            var acceleration = bow.FlightAcceleration;
            acceleration.x *= facing;

            for (var power = preferredMinPower; power <= 1.001f; power += 0.05f)
            {
                float angle;
                if (AimSolver.TrySolveAngle(offset, bow.SpeedForPower(power), acceleration, Time.fixedDeltaTime,
                        minAngle, maxAngle, out angle))
                {
                    aim = new AimState(true, angle, Mathf.Min(power, 1f), facing);
                    return true;
                }
            }
            return false;
        }

        static float WorldAngle(AimState aim)
        {
            var direction = aim.Direction;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }
}
