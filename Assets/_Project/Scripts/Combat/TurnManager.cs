using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Turn-based duel (F-08): the player shoots, then each living enemy in order, until one side is
    /// down. Each turn the camera frames the shooter, follows the arrow, and lingers where it lands.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] Combatant player;
        [SerializeField] List<Combatant> enemies = new List<Combatant>();
        [SerializeField] BattleCamera battleCamera;

        [Header("Pacing")]
        [SerializeField, Tooltip("Seconds before the first turn.")]
        float introDelay = 0.5f;
        [SerializeField, Tooltip("Seconds the camera lingers where an arrow landed.")]
        float landingPause = 0.9f;
        [SerializeField, Tooltip("Seconds for the camera to reach an enemy before it shoots.")]
        float enemyFocusTime = 0.7f;
        [SerializeField, Tooltip("Seconds between a kill and the end of the battle.")]
        float endDelay = 1.2f;

        public event Action<Combatant> TurnStarted;
        /// <summary>Raised once; true when the player won.</summary>
        public event Action<bool> BattleEnded;

        public Combatant Player { get { return player; } }
        public IList<Combatant> Enemies { get { return enemies; } }
        public Combatant CurrentTurn { get; private set; }
        public bool IsOver { get; private set; }

        IEnumerator Start()
        {
            if (player == null || enemies.Count == 0)
            {
                Debug.LogError("[TurnManager] Needs a player and at least one enemy.", this);
                yield break;
            }

            foreach (var combatant in AllCombatants())
                if (combatant.Controller != null)
                    combatant.Controller.CancelTurn();

            yield return new WaitForSeconds(introDelay);

            while (true)
            {
                yield return PlayTurn(player, FirstLivingEnemy());
                if (CheckForEnd())
                    break;

                foreach (var enemy in enemies.Where(e => e != null).ToList())
                {
                    if (!enemy.IsAlive)
                        continue;
                    yield return PlayTurn(enemy, player);
                    if (CheckForEnd())
                        break;
                }
                if (IsOver)
                    break;
            }

            yield return new WaitForSeconds(endDelay);
            if (BattleEnded != null)
                BattleEnded(player.IsAlive);
        }

        IEnumerator PlayTurn(Combatant actor, Combatant opponent)
        {
            CurrentTurn = actor;
            if (battleCamera != null)
            {
                battleCamera.ReturnHome();
                battleCamera.SetHome(actor.transform, actor.FacingRight);
            }
            if (TurnStarted != null)
                TurnStarted(actor);

            if (actor.Team != Team.Player)
                yield return new WaitForSeconds(enemyFocusTime);

            var controller = actor.Controller;
            if (controller == null)
            {
                Debug.LogError("[TurnManager] " + actor.name + " has no ITurnController; skipping its turn.", actor);
                yield break;
            }

            Arrow shot = null;
            var shotFired = false;
            controller.BeginTurn(actor, opponent, arrow =>
            {
                shot = arrow;
                shotFired = true;
            });

            while (!shotFired)
            {
                if (!actor.IsAlive)
                {
                    controller.CancelTurn();
                    yield break;
                }
                yield return null;
            }

            if (battleCamera != null && shot != null)
                battleCamera.Follow(shot);

            // A lost arrow destroys itself, which also ends the wait.
            while (shot != null && shot.IsFlying)
                yield return null;

            yield return new WaitForSeconds(landingPause);
            CurrentTurn = null;
        }

        bool CheckForEnd()
        {
            if (!player.IsAlive || FirstLivingEnemy() == null)
                IsOver = true;
            return IsOver;
        }

        Combatant FirstLivingEnemy()
        {
            return enemies.FirstOrDefault(e => e != null && e.IsAlive);
        }

        IEnumerable<Combatant> AllCombatants()
        {
            yield return player;
            foreach (var enemy in enemies)
                if (enemy != null)
                    yield return enemy;
        }
    }
}
