using System.Collections.Generic;

namespace MagnitArena.Model
{
    public static class LevelLoader
    {
        public static List<Level> LoadAllLevels()
        {
            return new List<Level> { LoadLevel1(), LoadLevel2(), LoadLevel3() };
        }

        private static Level LoadLevel1()
        {
            var level = new Level
            {
                Walls = new int[20, 15],
                Pits = new int[20, 15],
                Zones = new int[20, 15],
                PlayerStart = new Vector2(2, 7),
                BoxStarts = new List<Vector2> { new Vector2(10, 5) },
                EnemyStarts = new List<Vector2>()
            };
            level.Zones[15, 5] = 1;
            return level;
        }

        private static Level LoadLevel2()
        {
            var walls = new int[20, 15];
            walls[8, 3] = 1; walls[8, 4] = 1; walls[8, 5] = 1;
            walls[12, 3] = 1; walls[12, 4] = 1; walls[12, 5] = 1;
            var pits = new int[20, 15];
            pits[18, 7] = 1;
            var zones = new int[20, 15];
            zones[15, 7] = 1;
            return new Level
            {
                Walls = walls,
                Pits = pits,
                Zones = zones,
                PlayerStart = new Vector2(2, 7),
                BoxStarts = new List<Vector2> { new Vector2(5, 7) },
                EnemyStarts = new List<Vector2> { new Vector2(10, 7) }
            };
        }

        private static Level LoadLevel3()
        {
            var walls = new int[20, 15];
            for (int x = 5; x < 15; x++)
            {
                walls[x, 3] = 1;
                walls[x, 11] = 1;
            }
            walls[10, 7] = 1;

            var pits = new int[20, 15];
            pits[18, 3] = 1;
            pits[18, 11] = 1;

            var zones = new int[20, 15];
            zones[15, 3] = 1;
            zones[15, 7] = 1;
            zones[15, 11] = 1;

            return new Level
            {
                Walls = walls,
                Pits = pits,
                Zones = zones,
                PlayerStart = new Vector2(2, 7),
                BoxStarts = new List<Vector2>
        {
            new Vector2(8, 3),   // ← ЯЩИК 1
            new Vector2(8, 7),   // ← ЯЩИК 2
            new Vector2(8, 11)   // ← ЯЩИК 3
        },
                EnemyStarts = new List<Vector2>
        {
            new Vector2(12, 2),
            new Vector2(12, 12)
        }
            };
        }
    }
}