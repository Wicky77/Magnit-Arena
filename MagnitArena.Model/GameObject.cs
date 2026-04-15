using System;
using System.Drawing;

namespace MagnitArena.Model
{
    public class GameObject
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public bool IsRemoved { get; set; }
        public double Friction { get; set; } = 0.9;

        public virtual void Update()
        {
            Position = new Vector2(Position.X + Velocity.X, Position.Y + Velocity.Y);
            Velocity = new Vector2(Velocity.X * Friction, Velocity.Y * Friction);
            if (Math.Abs(Velocity.X) < 0.05) Velocity = new Vector2(0, Velocity.Y);
            if (Math.Abs(Velocity.Y) < 0.05) Velocity = new Vector2(Velocity.X, 0);
        }

        public Rectangle GetBounds()
        {
            int x = (int)Math.Round(Position.X);
            int y = (int)Math.Round(Position.Y);
            return new Rectangle(x, y, 1, 1);
        }

        public bool CollidesWith(GameObject other) => GetBounds().IntersectsWith(other.GetBounds());
    }
}