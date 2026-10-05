using System.Collections.Generic;
using UnityEngine;

namespace Siavosh.Play
{
    /// <summary>A solid block (ground) or a one-way platform (ledge) the bodies stand on.</summary>
    public struct Solid
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;
        public bool OneWay;

        public bool Spans(float x, float halfWidth)
        {
            return x + halfWidth > Left && x - halfWidth < Right;
        }
    }

    /// <summary>
    /// A small kinematic platformer body: gravity, landing on ground and ledges, and side walls at
    /// the edges of pits. Position is the centre of the feet. Deterministic and independent of the
    /// physics engine, so the game feels the same on every phone.
    /// </summary>
    public class Body
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float HalfWidth = 0.3f;
        public float Height = 2.2f;
        public float Gravity = 38f;
        public bool Grounded;
        public bool DropThrough;
        public float MinX = float.NegativeInfinity;
        public float MaxX = float.PositiveInfinity;

        public Rect Bounds { get { return new Rect(Position.x - HalfWidth, Position.y, HalfWidth * 2f, Height); } }
        public Vector2 Center { get { return new Vector2(Position.x, Position.y + Height * 0.5f); } }

        /// <summary>Moves the body by its velocity, colliding with <paramref name="solids"/>. Returns true on landing.</summary>
        public bool Step(float dt, List<Solid> solids)
        {
            var wasGrounded = Grounded;
            Velocity.y -= Gravity * dt;
            if (Velocity.y < -30f)
                Velocity.y = -30f;

            // Horizontal: blocked only by the sides of solid ground we are below the top of.
            var x = Position.x + Velocity.x * dt;
            foreach (var s in solids)
            {
                if (s.OneWay || Position.y >= s.Top - 0.02f || Position.y + Height <= s.Bottom)
                    continue;
                if (x + HalfWidth > s.Left && x - HalfWidth < s.Right)
                {
                    if (Position.x <= s.Left)
                        x = s.Left - HalfWidth;
                    else if (Position.x >= s.Right)
                        x = s.Right + HalfWidth;
                    Velocity.x = 0f;
                }
            }
            Position.x = Mathf.Clamp(x, MinX, MaxX);

            // Vertical: land on the highest surface we fell onto this step.
            var previousBottom = Position.y;
            var y = Position.y + Velocity.y * dt;
            Grounded = false;
            if (Velocity.y <= 0f)
            {
                var best = float.NegativeInfinity;
                foreach (var s in solids)
                {
                    if (!s.Spans(Position.x, HalfWidth * 0.8f))
                        continue;
                    if (s.OneWay && DropThrough)
                        continue;
                    if (previousBottom >= s.Top - 0.05f && y <= s.Top && s.Top > best)
                        best = s.Top;
                }
                if (best > float.NegativeInfinity)
                {
                    y = best;
                    Velocity.y = 0f;
                    Grounded = true;
                }
            }
            Position.y = y;
            return Grounded && !wasGrounded;
        }

        /// <summary>The surface height under a point, or negative infinity over a pit.</summary>
        public static float SurfaceAt(float x, List<Solid> solids, float below = float.PositiveInfinity)
        {
            var best = float.NegativeInfinity;
            foreach (var s in solids)
                if (x > s.Left && x < s.Right && s.Top <= below && s.Top > best)
                    best = s.Top;
            return best;
        }
    }
}
