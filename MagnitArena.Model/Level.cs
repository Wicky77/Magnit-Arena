using System.Collections.Generic;

namespace MagnitArena.Model
{
    public class Level
    {
        public int[,] Walls { get; set; }
        public int[,] Pits { get; set; }
        public int[,] Zones { get; set; }
        public Vector2 PlayerStart { get; set; }
        public List<Vector2> BoxStarts { get; set; }
        public List<Vector2> EnemyStarts { get; set; }

        public Level()
        {
            BoxStarts = new List<Vector2>();
            EnemyStarts = new List<Vector2>();
        }
    }
}