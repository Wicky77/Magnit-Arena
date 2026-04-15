using System;

namespace MagnitArena.Model
{
    public struct Vector2
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector2(double x, double y) { X = x; Y = y; }
        public double Length => Math.Sqrt(X * X + Y * Y);

        public Vector2 Normalize()
        {
            var len = Length;
            if (len < 0.001) return new Vector2(0, 0);
            return new Vector2(X / len, Y / len);
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, double b) => new Vector2(a.X * b, a.Y * b);
    }
}