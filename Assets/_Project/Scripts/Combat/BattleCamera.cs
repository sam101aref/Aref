using System;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Side-view battle camera. Frames the archer whose turn it is, follows an arrow in flight with a
    /// little look-ahead, zooms out as the arrow climbs, never shows much below the ground, and shakes
    /// on impacts.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BattleCamera : MonoBehaviour
    {
        [Header("Framing")]
        [SerializeField, Tooltip("What the camera frames when nothing is in flight, usually the player.")]
        Transform home;
        [SerializeField, Tooltip("Offset from home, for an archer facing right (mirrored when facing left).")]
        Vector2 homeOffset = new Vector2(5f, 2.5f);
        [SerializeField] bool homeFacingRight = true;
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

        [Header("Auto framing (real-time battles)")]
        [SerializeField] float autoMinSize = 6.2f;
        [SerializeField] float autoMaxSize = 11f;
        [SerializeField, Tooltip("Space kept around the framed characters, in units.")]
        Vector2 autoMargin = new Vector2(2.5f, 3.5f);

        Camera cam;
        Func<Rect?> frameProvider;
        Arrow arrowTarget;
        Transform target;
        Vector3 moveVelocity;
        float zoomVelocity;
        Vector3 focus;
        float shakeAmplitude;
        float shakeDuration;
        float shakeRemaining;

        public bool IsFollowing { get { return target != null; } }

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = baseSize;
            focus = transform.position;
            SnapHome();
        }

        public void SetHome(Transform newHome, bool facingRight)
        {
            home = newHome;
            homeFacingRight = facingRight;
        }

        /// <summary>Changes the resting zoom and framing offset, e.g. a wide view for real-time battles.</summary>
        public void SetFraming(float size, Vector2 offset)
        {
            baseSize = size;
            maxSize = Mathf.Max(maxSize, size);
            homeOffset = offset;
        }

        /// <summary>
        /// Real-time battles (F-53): keeps the rectangle the provider returns (Arash and every living
        /// enemy) in view, zooming out as far as needed and no further.
        /// </summary>
        public void SetAutoFrame(Func<Rect?> provider)
        {
            frameProvider = provider;
        }

        /// <summary>Shakes the view; runs on unscaled time so it also works in slow motion.</summary>
        public void Shake(float amplitude, float duration)
        {
            if (amplitude < shakeAmplitude * (shakeRemaining / Mathf.Max(0.0001f, shakeDuration)))
                return; // a stronger shake is already running
            shakeAmplitude = amplitude;
            shakeDuration = shakeRemaining = Mathf.Max(0.0001f, duration);
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
            focus = Clamp(new Vector3(desired.x, desired.y, transform.position.z), baseSize);
            transform.position = focus;
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
            else if (frameProvider != null && frameProvider().HasValue)
            {
                var rect = frameProvider().Value;
                var aspect = cam != null ? cam.aspect : 16f / 9f;
                var halfHeight = Mathf.Max((rect.height + autoMargin.y) * 0.5f, (rect.width + autoMargin.x * 2f) / (2f * aspect));
                desiredSize = Mathf.Clamp(halfHeight, autoMinSize, autoMaxSize);
                desired = new Vector2(rect.center.x, groundY - groundMargin + desiredSize);
            }
            else
            {
                desired = HomePoint();
                desiredSize = baseSize;
            }

            // Real time, so the camera still settles while the game is paused for dialogue. A zero time
            // step must never reach SmoothDamp: it divides by it and the camera would break for good.
            var dt = Time.unscaledDeltaTime;
            if (dt > 0f)
            {
                cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, desiredSize, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, dt);
                var goal = Clamp(new Vector3(desired.x, desired.y, focus.z), cam.orthographicSize);
                focus = Vector3.SmoothDamp(focus, goal, ref moveVelocity, following ? followSmoothTime : returnSmoothTime, Mathf.Infinity, dt);
            }
            if (!IsFinite(focus) || !IsFinite(cam.orthographicSize))
            {
                // Recover from anything that slipped through rather than showing an empty view.
                zoomVelocity = 0f;
                moveVelocity = Vector3.zero;
                cam.orthographicSize = desiredSize;
                focus = Clamp(new Vector3(desired.x, desired.y, -10f), desiredSize);
            }
            transform.position = focus + ShakeOffset();
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        Vector3 ShakeOffset()
        {
            if (shakeRemaining <= 0f)
                return Vector3.zero;
            shakeRemaining -= Time.unscaledDeltaTime;
            var strength = shakeAmplitude * Mathf.Clamp01(shakeRemaining / shakeDuration);
            return (Vector3)(UnityEngine.Random.insideUnitCircle * strength);
        }

        Vector2 HomePoint()
        {
            if (home == null)
                return focus;
            var offset = homeOffset;
            if (!homeFacingRight)
                offset.x = -offset.x;
            return (Vector2)home.position + offset;
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
