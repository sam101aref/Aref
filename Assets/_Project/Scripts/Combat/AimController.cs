using System;
using Arash.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Arash.Combat
{
    /// <summary>
    /// Player aiming: touch anywhere, pull back and release to shoot (mouse works the same in the editor).
    /// Dragging back to roughly where the touch started cancels the shot.
    /// Input is only read while this component is enabled; as the player's <see cref="ITurnController"/>
    /// it enables itself for the player's turn and disables itself once the arrow is away.
    /// </summary>
    public class AimController : MonoBehaviour, ITurnController
    {
        [SerializeField] Bow bow;
        [SerializeField] TrajectoryPreview preview;
        [SerializeField] ArcherRig rig;
        [SerializeField] AimSettings settings = new AimSettings();
        [SerializeField] bool facingRight = true;

        public event Action AimStarted;
        public event Action AimCancelled;
        public event Action<Arrow> Shot;

        /// <summary>Replaces the normal shot, e.g. to fire an armed special arrow (F-32).</summary>
        public Func<AimState, Arrow> FireOverride;

        public bool IsAiming { get; private set; }
        public AimState CurrentAim { get; private set; }

        Vector2 dragStart;
        Action<Arrow> turnCallback;

        public void BeginTurn(Combatant self, Combatant opponent, Action<Arrow> onShot)
        {
            turnCallback = onShot;
            enabled = true;
        }

        public void CancelTurn()
        {
            turnCallback = null;
            enabled = false;
        }

        void OnDisable()
        {
            if (IsAiming)
                Cancel();
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || GamePause.IsPaused)
                return;

            var position = pointer.position.ReadValue();

            if (!IsAiming)
            {
                if (pointer.press.wasPressedThisFrame && !IsOverUI())
                    Begin(position);
                return;
            }

            UpdateAim(position);

            if (pointer.press.wasReleasedThisFrame || !pointer.press.isPressed)
                Release();
        }

        static bool IsOverUI()
        {
            var events = EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }

        void Begin(Vector2 screenPosition)
        {
            IsAiming = true;
            dragStart = screenPosition;
            CurrentAim = AimState.Cancelled;
            if (AimStarted != null)
                AimStarted();
        }

        void UpdateAim(Vector2 screenPosition)
        {
            CurrentAim = AimModel.Evaluate(dragStart, screenPosition, Screen.height, settings, facingRight);

            if (!CurrentAim.IsValid)
            {
                if (preview != null)
                    preview.Hide();
                if (rig != null)
                    rig.Relax();
                return;
            }

            // Move the arm first so the preview starts from the bow's new position.
            if (rig != null)
                rig.Aim(CurrentAim.Direction);
            if (preview != null && bow != null)
                preview.Show(bow.LaunchPosition, bow.LaunchVelocity(CurrentAim), bow.FlightAcceleration);
        }

        void Release()
        {
            var aim = CurrentAim;
            IsAiming = false;
            if (preview != null)
                preview.Hide();

            if (!aim.IsValid || bow == null)
            {
                Cancel();
                return;
            }

            var arrow = FireOverride != null ? FireOverride(aim) : bow.Fire(aim);
            if (rig != null)
                rig.Relax();
            if (arrow == null)
                return;

            if (Shot != null)
                Shot(arrow);
            if (turnCallback != null)
            {
                var callback = turnCallback;
                turnCallback = null;
                enabled = false;
                callback(arrow);
            }
        }

        void Cancel()
        {
            IsAiming = false;
            CurrentAim = AimState.Cancelled;
            if (preview != null)
                preview.Hide();
            if (rig != null)
                rig.Relax();
            if (AimCancelled != null)
                AimCancelled();
        }
    }
}
