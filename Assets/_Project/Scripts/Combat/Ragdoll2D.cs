using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// The body parts are kinematic rigidbodies joined by hinges. While alive they hold their pose;
    /// on death they turn dynamic and the killing arrow knocks the body over.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Ragdoll2D : MonoBehaviour
    {
        [SerializeField, Tooltip("Impulse per unit of arrow speed applied to the part that was hit.")]
        float deathImpulse = 0.12f;
        [SerializeField, Tooltip("Components disabled on death (aiming, AI, rig…).")]
        Behaviour[] disableOnDeath;

        Rigidbody2D[] parts;
        Health health;

        public bool IsActive { get; private set; }

        void Awake()
        {
            parts = GetComponentsInChildren<Rigidbody2D>(true);
            // No interpolation while alive: the rig moves parts through their transforms,
            // which interpolation would overwrite.
            foreach (var part in parts)
            {
                part.bodyType = RigidbodyType2D.Kinematic;
                part.interpolation = RigidbodyInterpolation2D.None;
            }

            health = GetComponent<Health>();
            health.Died += Activate;
        }

        void OnDestroy()
        {
            if (health != null)
                health.Died -= Activate;
        }

        /// <summary>Adds a component that must stop when this character dies (movers added at runtime).</summary>
        public void DisableOnDeath(Behaviour behaviour)
        {
            var list = new System.Collections.Generic.List<Behaviour>(disableOnDeath ?? new Behaviour[0]) { behaviour };
            disableOnDeath = list.ToArray();
        }

        public void Activate(DamageInfo killingBlow)
        {
            if (IsActive)
                return;
            IsActive = true;

            if (disableOnDeath != null)
                foreach (var behaviour in disableOnDeath)
                    if (behaviour != null)
                        behaviour.enabled = false;

            foreach (var part in parts)
            {
                part.bodyType = RigidbodyType2D.Dynamic;
                part.interpolation = RigidbodyInterpolation2D.Interpolate;
                part.linearVelocity = Vector2.zero;
                part.angularVelocity = 0f;
            }

            var hitBody = killingBlow.Collider != null ? killingBlow.Collider.attachedRigidbody : null;
            if (hitBody != null)
                hitBody.AddForceAtPosition(killingBlow.Velocity * deathImpulse, killingBlow.Point, ForceMode2D.Impulse);
        }
    }
}
