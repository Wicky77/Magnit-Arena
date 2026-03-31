namespace MagnetArena.Model
{
    public abstract class GameObject
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public bool IsRemoved { get; set; }

        public virtual void Update()
        {
            Position = Position + Velocity;
            Velocity = Velocity * 0.9;
            if (Velocity.Length < 0.01) Velocity = new Vector2(0, 0);
        }
    }
}
