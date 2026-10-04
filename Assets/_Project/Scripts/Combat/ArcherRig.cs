using UnityEngine;

namespace Arash.Combat
{
    /// <summary>Points the archer's bow arm along the aim and returns it to rest afterwards.</summary>
    public class ArcherRig : MonoBehaviour
    {
        [SerializeField, Tooltip("Shoulder joint; its +X axis points along the arm and bow.")]
        Transform aimPivot;
        [SerializeField, Tooltip("Arm angle in degrees below the facing direction when not aiming.")]
        float restAngle = -20f;
        [SerializeField] bool facingRight = true;

        public bool FacingRight { get { return facingRight; } }

        /// <summary>World angle (degrees) of the arm at rest.</summary>
        public float RestWorldAngle { get { return facingRight ? restAngle : 180f - restAngle; } }

        /// <summary>World angle (degrees) the arm currently points at.</summary>
        public float CurrentWorldAngle
        {
            get { return aimPivot != null ? aimPivot.eulerAngles.z : RestWorldAngle; }
        }

        void Awake()
        {
            Relax();
        }

        public void Aim(Vector2 direction)
        {
            if (aimPivot == null || direction.sqrMagnitude < 0.0001f)
                return;
            // The arm and bow extend along the pivot's +X, so this works for both facings.
            aimPivot.right = direction;
        }

        public void AimAtAngle(float worldAngle)
        {
            var radians = worldAngle * Mathf.Deg2Rad;
            Aim(new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)));
        }

        public void Relax()
        {
            if (aimPivot != null)
                aimPivot.localRotation = Quaternion.Euler(0f, 0f, RestWorldAngle);
        }
    }
}
