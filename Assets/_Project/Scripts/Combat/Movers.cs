using System;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>Rides back and forth around the start position (mounted Turanians, F-27).</summary>
    public class Patrol : MonoBehaviour
    {
        public float range = 3f;
        public float speed = 2.5f;

        Vector3 origin;
        float phase;

        void Start()
        {
            origin = transform.position;
        }

        void Update()
        {
            phase += Time.deltaTime * speed / Mathf.Max(0.01f, range);
            transform.position = origin + new Vector3(Mathf.Sin(phase) * range, 0f, 0f);
        }
    }

    /// <summary>Walks towards a goal line (wave levels, F-23); reports when it gets there.</summary>
    public class Walker : MonoBehaviour
    {
        public float speed = 1.5f;
        public float goalX = -3f;

        public event Action<Walker> ReachedGoal;

        public bool HasArrived { get; private set; }

        void Update()
        {
            if (HasArrived)
                return;

            var position = transform.position;
            var direction = Mathf.Sign(goalX - position.x);
            position.x += direction * speed * Time.deltaTime;
            if ((goalX - position.x) * direction <= 0f)
            {
                position.x = goalX;
                HasArrived = true;
                if (ReachedGoal != null)
                    ReachedGoal(this);
            }
            transform.position = position;
        }
    }

    /// <summary>Moves a target along a line and back (precision trials, F-25).</summary>
    public class Oscillator : MonoBehaviour
    {
        public Vector2 amplitude = new Vector2(0f, 1.5f);
        public float period = 2.5f;

        Vector3 origin;
        float time;

        void Start()
        {
            origin = transform.localPosition;
        }

        void Update()
        {
            time += Time.deltaTime;
            var k = Mathf.Sin(time * Mathf.PI * 2f / Mathf.Max(0.1f, period));
            transform.localPosition = origin + (Vector3)(amplitude * k);
        }
    }
}
