using System;
using UnityEngine;

namespace Arash.Combat
{
    public enum Team
    {
        Player,
        Enemy,
    }

    /// <summary>Takes a turn: aims and shoots one arrow, then reports it.</summary>
    public interface ITurnController
    {
        /// <summary>Starts the turn. <paramref name="onShot"/> must be called once with the fired arrow.</summary>
        void BeginTurn(Combatant self, Combatant opponent, Action<Arrow> onShot);

        /// <summary>Stops any turn in progress without shooting.</summary>
        void CancelTurn();
    }

    /// <summary>A character that takes part in a battle: who it is, which way it faces and how it shoots.</summary>
    [RequireComponent(typeof(Health))]
    public class Combatant : MonoBehaviour
    {
        [SerializeField] Team team = Team.Enemy;
        [SerializeField] bool facingRight = false;
        [SerializeField, Tooltip("Point aimed at for body shots.")]
        Transform bodyTarget;
        [SerializeField, Tooltip("Point aimed at for headshots.")]
        Transform headTarget;

        Health health;
        ITurnController controller;

        public Team Team { get { return team; } }
        public bool FacingRight { get { return facingRight; } }
        public Health Health { get { return health != null ? health : (health = GetComponent<Health>()); } }
        public bool IsAlive { get { return !Health.IsDead; } }
        public Transform BodyTarget { get { return bodyTarget != null ? bodyTarget : transform; } }
        public Transform HeadTarget { get { return headTarget != null ? headTarget : BodyTarget; } }

        public ITurnController Controller
        {
            get
            {
                if (controller == null)
                    controller = GetComponent<ITurnController>();
                return controller;
            }
        }
    }
}
