namespace MagnetArena.Model
{
    public class World
    {
        public Player Player { get; private set; }
        public List<Box> Boxes { get; private set; }
        public List<Enemy> Enemies { get; private set; }
        public List<Vector2> Walls { get; private set; }
        public List<Vector2> Pits { get; private set; }
        public List<Vector2> Zones { get; private set; }

        public World()
        {
            Boxes = new List<Box>();
            Enemies = new List<Enemy>();
            Walls = new List<Vector2>();
            Pits = new List<Vector2>();
            Zones = new List<Vector2>();
        }

        public void SetPlayer(Player player) => Player = player;

        public void Update()
        {
            Player?.Update();
            foreach (var box in Boxes) box.Update();
            foreach (var enemy in Enemies) enemy.Update();
        }

        public void ApplyMagnetForce(bool isPull, double force = 5.0)
        {
            if (Player == null) return;
            var objects = Boxes.Concat<GameObject>(Enemies).ToList();

            foreach (var obj in objects)
            {
                var direction = obj.Position - Player.Position;
                if (direction.Length == 0) continue;

                var normalized = direction.Normalize();
                var multiplier = isPull ? -force : force;
                obj.Velocity = obj.Velocity + (normalized * multiplier);
            }
        }

        public void CheckCollisions()
        {
            foreach (var box in Boxes.ToList())
            {
                var pos = new Vector2(Math.Round(box.Position.X), Math.Round(box.Position.Y));
                if (Pits.Contains(pos)) box.IsRemoved = true;
            }

            foreach (var enemy in Enemies.ToList())
            {
                var pos = new Vector2(Math.Round(enemy.Position.X), Math.Round(enemy.Position.Y));
                if (Pits.Contains(pos)) enemy.IsRemoved = true;
            }
        }

        public bool CheckWin()
        {
            if (Enemies.Any(e => !e.IsRemoved)) return false;
            if (!Boxes.Any()) return true;

            foreach (var box in Boxes)
            {
                if (box.IsRemoved) continue;
                var pos = new Vector2(Math.Round(box.Position.X), Math.Round(box.Position.Y));
                if (!Zones.Contains(pos)) return false;
            }
            return true;
        }
    }
}
