namespace MagnetArena.Model
{
    public class Player : GameObject
    {
        public void Move(Direction dir, double speed = 1.0)
        {
            Velocity = dir switch
            {
                Direction.Up => new Vector2(0, -speed),
                Direction.Down => new Vector2(0, speed),
                Direction.Left => new Vector2(-speed, 0),
                Direction.Right => new Vector2(speed, 0),
                _ => new Vector2(0, 0)
            };
        }
    }
}
