using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// Spawns and launches projectiles (arrows, spears, stones…). Draw power maps linearly to launch
    /// speed; flight acceleration is gravity plus the current wind.
    /// </summary>
    public class Bow : MonoBehaviour
    {
        [SerializeField] Arrow arrowPrefab;
        [SerializeField, Tooltip("Where arrows spawn. Defaults to this transform.")]
        Transform launchPoint;
        [SerializeField, Tooltip("Colliders under this transform are never hit by our own arrows. Defaults to the root.")]
        Transform owner;

        [Header("Draw")]
        [SerializeField, Min(0f)] float minLaunchSpeed = 8f;
        [SerializeField, Min(0f)] float maxLaunchSpeed = 28f;
        [SerializeField, Tooltip("Multiplier on Physics2D gravity for arrow flight.")]
        float gravityScale = 1f;
        [SerializeField, Tooltip("How strongly wind pushes this bow's projectiles.")]
        float windScale = 1f;

        [Header("Cleanup")]
        [SerializeField, Min(1), Tooltip("Oldest stuck arrows are removed beyond this count.")]
        int maxArrowsInWorld = 20;

        public event Action<Arrow> Fired;

        readonly Queue<Arrow> spawnedArrows = new Queue<Arrow>();

        public Vector2 LaunchPosition { get { return (launchPoint != null ? launchPoint : transform).position; } }
        public Vector2 FlightAcceleration
        {
            get { return Physics2D.gravity * gravityScale + BattleEnvironment.WindAcceleration * windScale; }
        }
        public Transform Owner { get { return owner != null ? owner : transform.root; } }

        /// <summary>Swaps the projectile, e.g. spears or stones for special enemies.</summary>
        public void SetProjectile(Arrow prefab, float minSpeed, float maxSpeed, float gravity, float wind)
        {
            if (prefab != null)
                arrowPrefab = prefab;
            minLaunchSpeed = minSpeed;
            maxLaunchSpeed = maxSpeed;
            gravityScale = gravity;
            windScale = wind;
        }

        public Vector2 LaunchVelocity(AimState aim)
        {
            return aim.Direction * SpeedForPower(aim.Power);
        }

        public float SpeedForPower(float power)
        {
            return Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, power);
        }

        public Arrow Fire(AimState aim)
        {
            if (!aim.IsValid)
                return null;
            if (arrowPrefab == null)
            {
                Debug.LogError("[Bow] No arrow prefab assigned.", this);
                return null;
            }

            var arrow = Instantiate(arrowPrefab, LaunchPosition, Quaternion.identity);
            arrow.Launch(LaunchVelocity(aim), FlightAcceleration, Owner);
            Track(arrow);

            if (Fired != null)
                Fired(arrow);
            return arrow;
        }

        void Track(Arrow arrow)
        {
            spawnedArrows.Enqueue(arrow);
            while (spawnedArrows.Count > maxArrowsInWorld)
            {
                var oldest = spawnedArrows.Dequeue();
                if (oldest != null)
                    Destroy(oldest.gameObject);
            }
        }
    }
}
