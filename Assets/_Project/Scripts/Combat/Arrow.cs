using System;
using UnityEngine;

namespace Arash.Combat
{
    public readonly struct ArrowHit
    {
        public readonly Collider2D Collider;
        public readonly Vector2 Point;
        public readonly Vector2 Normal;
        public readonly Vector2 Velocity;

        public ArrowHit(Collider2D collider, Vector2 point, Vector2 normal, Vector2 velocity)
        {
            Collider = collider;
            Point = point;
            Normal = normal;
            Velocity = velocity;
        }
    }

    /// <summary>
    /// A flying arrow. It moves itself with <see cref="Ballistics"/> on the fixed time step (so it
    /// follows the aim preview exactly) and line-casts each step to find what it hits. On impact it
    /// sticks into the collider and stays there. The transform pivot is the arrow tip.
    /// </summary>
    public class Arrow : MonoBehaviour
    {
        [SerializeField, Tooltip("How deep the tip sinks into what it hits.")]
        float penetration = 0.2f;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] float maxLifetime = 15f;
        [SerializeField] float destroyBelowY = -50f;

        /// <summary>Raised once when the arrow hits something.</summary>
        public event Action<Arrow, ArrowHit> Stuck;
        /// <summary>Raised once when the arrow leaves the world without hitting anything.</summary>
        public event Action<Arrow> Lost;

        public bool IsFlying { get; private set; }
        public Vector2 Velocity { get { return velocity; } }

        static readonly RaycastHit2D[] HitBuffer = new RaycastHit2D[16];

        Vector2 velocity;
        Vector2 acceleration;
        Vector2 previousPosition;
        Vector2 simulatedPosition;
        Transform ignoredRoot;
        float age;

        /// <summary>Starts the flight. Colliders under <paramref name="shooter"/> are never hit.</summary>
        public void Launch(Vector2 launchVelocity, Vector2 flightAcceleration, Transform shooter)
        {
            velocity = launchVelocity;
            acceleration = flightAcceleration;
            ignoredRoot = shooter;
            previousPosition = simulatedPosition = transform.position;
            age = 0f;
            IsFlying = true;
            PointAlong(velocity);
        }

        void FixedUpdate()
        {
            if (!IsFlying)
                return;

            age += Time.fixedDeltaTime;
            previousPosition = simulatedPosition;

            var next = simulatedPosition;
            Ballistics.Step(ref next, ref velocity, acceleration, Time.fixedDeltaTime);

            RaycastHit2D hit;
            if (TryHit(simulatedPosition, next, out hit))
            {
                StickInto(hit);
                return;
            }

            simulatedPosition = next;
            if (age > maxLifetime || simulatedPosition.y < destroyBelowY)
            {
                IsFlying = false;
                if (Lost != null)
                    Lost(this);
                Destroy(gameObject);
            }
        }

        void Update()
        {
            if (!IsFlying)
                return;

            // Interpolate between physics steps so the arrow moves smoothly at any frame rate.
            var t = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            transform.position = Vector2.Lerp(previousPosition, simulatedPosition, t);
            PointAlong(velocity);
        }

        bool TryHit(Vector2 from, Vector2 to, out RaycastHit2D closest)
        {
            closest = default(RaycastHit2D);
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(hitMask);

            var count = Physics2D.Linecast(from, to, filter, HitBuffer);
            var found = false;
            for (var i = 0; i < count; i++)
            {
                var hit = HitBuffer[i];
                if (hit.collider == null)
                    continue;
                if (ignoredRoot != null && hit.collider.transform.IsChildOf(ignoredRoot))
                    continue;
                if (!found || hit.distance < closest.distance)
                {
                    closest = hit;
                    found = true;
                }
            }
            return found;
        }

        void StickInto(RaycastHit2D hit)
        {
            IsFlying = false;
            var direction = velocity.normalized;
            simulatedPosition = hit.point + direction * penetration;
            transform.position = simulatedPosition;
            PointAlong(direction);
            transform.SetParent(hit.collider.transform, true);

            if (Stuck != null)
                Stuck(this, new ArrowHit(hit.collider, hit.point, hit.normal, velocity));
        }

        void PointAlong(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
