using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace MagnitArena.Model
{
    public class World
    {
        public const int WIDTH = 20, HEIGHT = 15;
        public Player Player { get; private set; }
        public List<Box> Boxes { get; private set; } = new List<Box>();
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
        public List<Vector2> Walls { get; private set; } = new List<Vector2>();
        public List<Vector2> Pits { get; private set; } = new List<Vector2>();
        public List<Vector2> Zones { get; private set; } = new List<Vector2>();
        public GameState State { get; set; } = GameState.Playing;
        public int CurrentLevel { get; private set; } = 1;
        public int TotalLevels { get; private set; } = 3;

        public void SetPlayer(Player p) => Player = p;

        public void LoadLevel(int idx)
        {
            var levels = LevelLoader.LoadAllLevels();
            if (idx < 0 || idx >= levels.Count) return;
            var lvl = levels[idx];
            CurrentLevel = idx + 1;
            Boxes.Clear();
            Enemies.Clear();
            Walls.Clear();
            Pits.Clear();
            Zones.Clear();

            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 15; y++)
                {
                    if (lvl.Walls[x, y] == 1) Walls.Add(new Vector2(x, y));
                    if (lvl.Pits[x, y] == 1) Pits.Add(new Vector2(x, y));
                    if (lvl.Zones[x, y] == 1) Zones.Add(new Vector2(x, y));
                }

            Player = new Player { Position = lvl.PlayerStart };
            foreach (var b in lvl.BoxStarts) Boxes.Add(new Box { Position = b });
            foreach (var e in lvl.EnemyStarts) Enemies.Add(new Enemy { Position = e });
            State = GameState.Playing;
        }

        public void Update()
        {
            if (State != GameState.Playing) return;
            if (Player != null) Player.Update();
            ClampAll();
        }

        private void ClampAll()
        {
            Clamp(Player);
            foreach (var b in Boxes) Clamp(b);
            foreach (var e in Enemies) Clamp(e);
        }

        private void Clamp(GameObject o)
        {
            if (o == null) return;
            var p = o.Position;
            p.X = Math.Max(0, Math.Min(WIDTH - 1, p.X));
            p.Y = Math.Max(0, Math.Min(HEIGHT - 1, p.Y));
            o.Position = p;
        }

        public void ApplyMagnetForce(bool pull, double force = 5.0)
        {
            if (Player == null || State != GameState.Playing) return;
            var px = (int)Math.Round(Player.Position.X);
            var py = (int)Math.Round(Player.Position.Y);

            var boxes = Boxes.Where(b => !b.IsRemoved)
                .OrderBy(b => {
                    var x = (int)Math.Round(b.Position.X);
                    var y = (int)Math.Round(b.Position.Y);
                    return Math.Abs(x - px) + Math.Abs(y - py);
                })
                .ThenBy(b => b.Position.X)
                .ThenBy(b => b.Position.Y)
                .ToList();

            var box = boxes.FirstOrDefault();
            if (box != null)
            {
                int dir = pull ? -1 : 1;
                for (int i = 0; i < 5; i++)
                {
                    if (!MoveBoxOneStep(box, px, py, dir))
                        break;
                }
            }
        }

        private bool MoveBoxOneStep(Box box, int px, int py, int dir)
        {
            int bx = (int)Math.Round(box.Position.X);
            int by = (int)Math.Round(box.Position.Y);
            int dx = bx - px;
            int dy = by - py;

            if (dx == 0 && dy == 0) return false;

            int mx = 0, my = 0;
            if (dx == 0) my = dy > 0 ? 1 : -1;
            else if (dy == 0) mx = dx > 0 ? 1 : -1;
            else
            {
                if (Math.Abs(dx) > Math.Abs(dy)) mx = dx > 0 ? 1 : -1;
                else my = dy > 0 ? 1 : -1;
            }

            mx *= dir;
            my *= dir;

            int nx = bx + mx;
            int ny = by + my;

            return TryMoveBox(box, nx, ny, mx, my);
        }

        private bool TryMoveBox(Box box, int tx, int ty, int mx, int my)
        {
            if (tx < 0 || tx >= WIDTH || ty < 0 || ty >= HEIGHT) return false;

            foreach (var w in Walls)
                if ((int)Math.Round(w.X) == tx && (int)Math.Round(w.Y) == ty)
                    return false;

            if (Player != null)
            {
                var px = (int)Math.Round(Player.Position.X);
                var py = (int)Math.Round(Player.Position.Y);
                if (tx == px && ty == py) return false;
            }

            foreach (var b in Boxes.Where(o => o != box && !o.IsRemoved))
            {
                var bx = (int)Math.Round(b.Position.X);
                var by = (int)Math.Round(b.Position.Y);
                if (tx == bx && ty == by) return false;
            }

            Enemy enemy = null;
            foreach (var e in Enemies.Where(o => !o.IsRemoved))
            {
                var ex = (int)Math.Round(e.Position.X);
                var ey = (int)Math.Round(e.Position.Y);
                if (tx == ex && ty == ey)
                {
                    enemy = e;
                    break;
                }
            }

            if (enemy != null)
            {
                int enx = tx + mx;
                int eny = ty + my;
                if (!CanEnemyMove(enemy, enx, eny)) return false;
                box.Position = new Vector2(tx, ty);
                box.Velocity = new Vector2(0, 0);
                enemy.Position = new Vector2(enx, eny);
                enemy.Velocity = new Vector2(0, 0);
                return true;
            }

            box.Position = new Vector2(tx, ty);
            box.Velocity = new Vector2(0, 0);
            return true;
        }

        private bool CanEnemyMove(Enemy e, int x, int y)
        {
            if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT) return false;

            if (Player != null)
            {
                var px = (int)Math.Round(Player.Position.X);
                var py = (int)Math.Round(Player.Position.Y);
                if (x == px && y == py) return false;
            }

            foreach (var w in Walls)
                if ((int)Math.Round(w.X) == x && (int)Math.Round(w.Y) == y)
                    return false;

            foreach (var b in Boxes.Where(o => !o.IsRemoved))
            {
                var bx = (int)Math.Round(b.Position.X);
                var by = (int)Math.Round(b.Position.Y);
                if (x == bx && y == by) return false;
            }

            foreach (var o in Enemies.Where(en => en != e && !en.IsRemoved))
            {
                var ex = (int)Math.Round(o.Position.X);
                var ey = (int)Math.Round(o.Position.Y);
                if (x == ex && y == ey) return false;
            }

            return true;
        }

        public void CheckCollisions()
        {
            if (State != GameState.Playing) return;

            if (Player != null)
            {
                var playerPos = new Vector2(
                    (int)Math.Round(Player.Position.X),
                    (int)Math.Round(Player.Position.Y)
                );
                if (Pits.Contains(playerPos))
                {
                    State = GameState.Lost;
                    return;
                }
            }

            foreach (var e in Enemies.Where(o => !o.IsRemoved))
            {
                var enemyPos = new Vector2(
                    (int)Math.Round(e.Position.X),
                    (int)Math.Round(e.Position.Y)
                );
                var playerPos = new Vector2(
                    (int)Math.Round(Player.Position.X),
                    (int)Math.Round(Player.Position.Y)
                );
                if (enemyPos.X == playerPos.X && enemyPos.Y == playerPos.Y)
                {
                    State = GameState.Lost;
                    return;
                }
            }

            foreach (var e in Enemies.Where(o => !o.IsRemoved))
            {
                var ex = (int)Math.Round(e.Position.X);
                var ey = (int)Math.Round(e.Position.Y);

                bool inCorner = (ex == 0 || ex == WIDTH - 1) && (ey == 0 || ey == HEIGHT - 1);

                if (inCorner)
                {
                    var pos = new Vector2(ex, ey);
                    if (!Pits.Contains(pos))
                    {
                        State = GameState.Lost;
                        return;
                    }
                }

                bool atEdge = (ex == 0 || ex == WIDTH - 1 || ey == 0 || ey == HEIGHT - 1);

                if (atEdge && !inCorner)
                {
                    bool canPushToCenter = true;

                    if (ex == 0)
                    {
                        var checkPos = new Vector2(ex + 1, ey);
                        if (Walls.Contains(checkPos) || Boxes.Any(b => (int)Math.Round(b.Position.X) == ex + 1 && (int)Math.Round(b.Position.Y) == ey && !b.IsRemoved))
                            canPushToCenter = false;
                    }
                    else if (ex == WIDTH - 1)
                    {
                        var checkPos = new Vector2(ex - 1, ey);
                        if (Walls.Contains(checkPos) || Boxes.Any(b => (int)Math.Round(b.Position.X) == ex - 1 && (int)Math.Round(b.Position.Y) == ey && !b.IsRemoved))
                            canPushToCenter = false;
                    }
                    else if (ey == 0)
                    {
                        var checkPos = new Vector2(ex, ey + 1);
                        if (Walls.Contains(checkPos) || Boxes.Any(b => (int)Math.Round(b.Position.X) == ex && (int)Math.Round(b.Position.Y) == ey + 1 && !b.IsRemoved))
                            canPushToCenter = false;
                    }
                    else if (ey == HEIGHT - 1)
                    {
                        var checkPos = new Vector2(ex, ey - 1);
                        if (Walls.Contains(checkPos) || Boxes.Any(b => (int)Math.Round(b.Position.X) == ex && (int)Math.Round(b.Position.Y) == ey - 1 && !b.IsRemoved))
                            canPushToCenter = false;
                    }

                    if (!canPushToCenter)
                    {
                        State = GameState.Lost;
                        return;
                    }
                }
            }

            foreach (var e in Enemies.ToList())
            {
                if (e.IsRemoved) continue;
                var p = new Vector2(
                    (int)Math.Round(e.Position.X),
                    (int)Math.Round(e.Position.Y)
                );
                if (Pits.Contains(p)) e.IsRemoved = true;
            }

            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            if (State == GameState.Lost) return;

            if (Enemies.Any(e => !e.IsRemoved))
            {
                State = GameState.Playing;
                return;
            }

            var boxes = Boxes.Where(b => !b.IsRemoved).ToList();
            if (boxes.Count == 0)
            {
                State = GameState.Playing;
                return;
            }

            int inZone = 0;
            foreach (var b in boxes)
            {
                var p = new Vector2(
                    (int)Math.Round(b.Position.X),
                    (int)Math.Round(b.Position.Y)
                );
                if (Zones.Contains(p)) inZone++;
            }

            State = (inZone == boxes.Count && inZone == Zones.Count)
                ? GameState.Won
                : GameState.Playing;
        }

        public bool CheckWin()
        {
            if (Enemies.Any(e => !e.IsRemoved)) return false;
            var boxes = Boxes.Count(b => !b.IsRemoved);
            int inZone = 0;
            foreach (var b in Boxes.Where(o => !o.IsRemoved))
            {
                var p = new Vector2(
                    (int)Math.Round(b.Position.X),
                    (int)Math.Round(b.Position.Y)
                );
                if (Zones.Contains(p)) inZone++;
            }
            return inZone == boxes && inZone == Zones.Count;
        }

        public void NextLevel()
        {
            if (CurrentLevel < TotalLevels)
                LoadLevel(CurrentLevel);
            else
                State = GameState.Won;
        }

        public void RestartLevel() => LoadLevel(CurrentLevel - 1);
    }
}