using UnityEngine;

namespace Arash.Combat
{
    /// <summary>Points the archer's bow arm along the aim and returns it to rest afterwards.</summary>
    public class ArcherRig : MonoBehaviour
    {
        [SerializeField, Tooltip("Shoulder joint; its +X axis points along the arm and bow.")]
        Transform aimPivot;
        [SerializeField, Tooltip("Arm angle in degrees when not aiming.")]
        float restAngle = -20f;

        void Awake()
        {
            Relax();
        }

        public void Aim(Vector2 direction)
        {
            if (aimPivot == null || direction.sqrMagnitude < 0.0001f)
                return;
            // On a mirrored (left-facing) archer the local axis is flipped as well.
            aimPivot.right = transform.lossyScale.x < 0f ? -direction : direction;
        }

        public void Relax()
        {
            if (aimPivot != null)
                aimPivot.localRotation = Quaternion.Euler(0f, 0f, restAngle);
        }
    }
}
