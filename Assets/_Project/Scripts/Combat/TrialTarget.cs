using System;
using UnityEngine;

namespace Arash.Combat
{
    public enum TrialTargetKind
    {
        Board,
        Apple,
        Lantern,
    }

    /// <summary>
    /// Something to hit in a precision trial (F-25). Arrows that stick into its collider count as
    /// a hit once. Lanterns go dark when hit.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TrialTarget : MonoBehaviour
    {
        public TrialTargetKind kind;

        public event Action<TrialTarget> Hit;

        public bool IsHit { get; private set; }

        public void RegisterHit()
        {
            if (IsHit)
                return;
            IsHit = true;

            var renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
                renderer.color = kind == TrialTargetKind.Lantern ? new Color(0.25f, 0.2f, 0.15f) : renderer.color * 0.6f;

            if (Hit != null)
                Hit(this);
        }
    }
}
