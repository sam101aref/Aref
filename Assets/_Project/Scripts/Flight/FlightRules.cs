using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arash.Flight
{
    public enum FlightObstacleKind
    {
        /// <summary>Storm cloud: costs strength.</summary>
        Cloud,
        /// <summary>Mountain peak rising from the ground: costs strength.</summary>
        Peak,
        /// <summary>Eagle gliding up and down: costs strength.</summary>
        Eagle,
        /// <summary>Light of farr (divine glory): restores strength.</summary>
        Farr,
    }

    public struct FlightObstacle
    {
        public FlightObstacleKind Kind;
        public float X;
        public float Y;
        /// <summary>Radius, or height for peaks.</summary>
        public float Size;
    }

    public class FlightState
    {
        public float X;
        public float Y;
        public float VerticalSpeed;
        public float Energy = FlightRules.MaxEnergy;
        public float Invulnerable;
        public int Hits;
        public bool Finished;
        public bool Failed;
    }

    /// <summary>
    /// The great arrow's flight (F-30), from Damavand at dawn to the Oxus at noon. The arrow flies
    /// forward on its own; holding the screen lifts it, releasing lets it sink. Arash's strength
    /// drains as it flies, obstacles cost strength, and lights of farr restore it.
    /// Pure rules for testing; <see cref="ArrowFlightController"/> draws them.
    /// </summary>
    public static class FlightRules
    {
        public const float MinY = -4f;
        public const float MaxY = 6f;
        public const float MaxEnergy = 100f;
        public const float Climb = 6f;
        public const float Sink = 4.5f;
        public const float Response = 10f;
        public const float DrainPerSecond = 0.9f;
        public const float HitCost = 25f;
        public const float FarrGain = 15f;
        public const float InvulnerableTime = 1.2f;
        public const float ArrowRadius = 0.35f;

        public static void Step(FlightState state, bool holding, float deltaTime, float speed, float length)
        {
            if (state.Finished || state.Failed)
                return;

            var target = holding ? Climb : -Sink;
            state.VerticalSpeed = Mathf.MoveTowards(state.VerticalSpeed, target, Response * deltaTime);
            state.Y = Mathf.Clamp(state.Y + state.VerticalSpeed * deltaTime, MinY + 0.5f, MaxY);
            state.X += speed * deltaTime;
            state.Energy -= DrainPerSecond * deltaTime;
            state.Invulnerable = Mathf.Max(0f, state.Invulnerable - deltaTime);

            if (state.X >= length)
                state.Finished = true;
            else if (state.Energy <= 0f)
                state.Failed = true;
        }

        public static bool Overlaps(FlightObstacle obstacle, float x, float y)
        {
            if (obstacle.Kind == FlightObstacleKind.Peak)
            {
                // Triangle with its tip Size above the ground and slopes of 45 degrees.
                var surface = MinY + obstacle.Size - Mathf.Abs(x - obstacle.X);
                return y - ArrowRadius < surface;
            }
            var dx = x - obstacle.X;
            var dy = y - obstacle.Y;
            var reach = obstacle.Size + ArrowRadius;
            return dx * dx + dy * dy < reach * reach;
        }

        /// <summary>Applies touching an obstacle. Returns true if it should be removed (lights of farr).</summary>
        public static bool Touch(FlightState state, FlightObstacle obstacle)
        {
            if (obstacle.Kind == FlightObstacleKind.Farr)
            {
                state.Energy = Mathf.Min(MaxEnergy, state.Energy + FarrGain);
                return true;
            }
            if (state.Invulnerable > 0f)
                return false;

            state.Energy -= HitCost;
            state.Hits++;
            state.Invulnerable = InvulnerableTime;
            if (state.Energy <= 0f)
                state.Failed = true;
            return false;
        }
    }

    /// <summary>Generates the course: the same seed always gives the same flight.</summary>
    public static class FlightCourse
    {
        public static List<FlightObstacle> Generate(int seed, float length, float density)
        {
            var random = new System.Random(seed);
            Func<float, float, float> range = (min, max) => min + (float)random.NextDouble() * (max - min);
            var obstacles = new List<FlightObstacle>();
            var x = 45f;
            while (x < length - 40f)
            {
                x += Mathf.Lerp(26f, 9f, Mathf.Clamp01(density)) * range(0.7f, 1.3f);
                var roll = random.NextDouble();
                var obstacle = new FlightObstacle { X = x };
                if (roll < 0.3)
                {
                    obstacle.Kind = FlightObstacleKind.Cloud;
                    obstacle.Y = range(0f, FlightRules.MaxY - 1f);
                    obstacle.Size = range(1.1f, 1.9f);
                }
                else if (roll < 0.55)
                {
                    obstacle.Kind = FlightObstacleKind.Peak;
                    obstacle.Y = FlightRules.MinY;
                    obstacle.Size = range(3f, 6.5f);
                }
                else if (roll < 0.78)
                {
                    obstacle.Kind = FlightObstacleKind.Eagle;
                    obstacle.Y = range(FlightRules.MinY + 2f, FlightRules.MaxY - 1f);
                    obstacle.Size = 0.6f;
                }
                else
                {
                    obstacle.Kind = FlightObstacleKind.Farr;
                    obstacle.Y = range(FlightRules.MinY + 1.5f, FlightRules.MaxY - 1f);
                    obstacle.Size = 0.6f;
                }
                obstacles.Add(obstacle);
            }
            return obstacles;
        }
    }
}
