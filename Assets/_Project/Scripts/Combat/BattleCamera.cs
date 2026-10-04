using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Side-view battle camera. Frames the archer while aiming, follows an arrow in flight with a
    /// little look-ahead, zooms out as the arrow climbs, and never shows much below the ground.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BattleCamera : MonoBehaviour
    {
        [Header("Framing")]
        [SerializeField, Tooltip("What the camera frames when nothing is in flight, usually the player.")]
        Transform home;
        [SerializeField] Vector2 homeOffset = new Vector2(5f, 2.5f);
        [SerializeField, Tooltip("World Y of the ground surface.")]
        float groundY = -3f;
        [SerializeField, Tooltip("How much ground stays visible below the ground line.")]
        float groundMargin = 1.5f;
        [SerializeField] float minX = -20f;
        [SerializeField] float maxX = 200f;

        [Header("Zoom")]
        [SerializeField] float baseSize = 5.4f;
        [SerializeField] float maxSize = 10f;
        [SerializeField, Tooltip("Extra orthographic size per unit the arrow is above the home point.")]
        float zoomPerHeight = 0.5f;

        [Header("Motion")]
        [SerializeField, Tooltip("Seconds of arrow velocity to look ahead.")]
        float lookAhead = 0.2f;
        [SerializeField] float followSmoothTime = 0.1f;
        [SerializeField] float returnSmoothTime = 0.4f;
        [SerializeField] float zoomSmoothTime = 0.35f;

        Camera cam;
        Arrow arrowTarget;
        Transform target;
        Vector3 moveVelocity;
        float zoomVelocity;

        public bool IsFollowing { get { return target != null; } }

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = baseSize;
            SnapHome();
        }

        public void SetHome(Transform newHome)
        {
            home = newHome;
        }

        /// <summary>Follows the arrow until it lands or is destroyed.</summary>
        public void Follow(Arrow arrow)
        {
            arrowTarget = arrow;
            target = arrow != null ? arrow.transform : null;
        }

        public void ReturnHome()
        {
            arrowTarget = null;
            target = null;
        }

        public void SnapHome()
        {
            var desired = HomePoint();
            transform.position = Clamp(new Vector3(desired.x, desired.y, transform.position.z), baseSize);
        }

        void LateUpdate()
        {
            var following = target != null;
            Vector2 desired;
            float desiredSize;

            if (following)
            {
                desired = target.position;
                if (arrowTarget != null && arrowTarget.IsFlying)
                    desired += arrowTarget.Velocity * lookAhead;

                var heightAboveHome = desired.y - HomePoint().y;
                desiredSize = Mathf.Clamp(baseSize + Mathf.Max(0f, heightAboveHome) * zoomPerHeight, baseSize, maxSize);
            }
            else
            {
                desired = HomePoint();
                desiredSize = baseSize;
            }

            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, desiredSize, ref zoomVelocity, zoomSmoothTime);

            var goal = Clamp(new Vector3(desired.x, desired.y, transform.position.z), cam.orthographicSize);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref moveVelocity,
                following ? followSmoothTime : returnSmoothTime);
        }

        Vector2 HomePoint()
        {
            return home != null ? (Vector2)home.position + homeOffset : (Vector2)transform.position;
        }

        Vector3 Clamp(Vector3 position, float size)
        {
            // Keep the bottom edge of the view at most groundMargin below the ground.
            position.y = Mathf.Max(position.y, groundY - groundMargin + size);
            var halfWidth = size * (cam != null ? cam.aspect : 16f / 9f);
            position.x = Mathf.Clamp(position.x, minX + halfWidth, Mathf.Max(minX + halfWidth, maxX - halfWidth));
            return position;
        }
    }
}
