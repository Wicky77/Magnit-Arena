using System;
using System.Collections.Generic;
using System.Linq;

namespace MagnitArena.Model
{
    public class World
    {
        public const int WIDTH = 20, HEIGHT = 15;
        public const int MAGNET_STRENGTH = 4;
        public const int MAGNET_COOLDOWN_MS = 200;

        public Player Player { get; private set; }
        public List<Box> Boxes { get; private set; } = new List<Box>();
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
        public List<Vector2> Walls { get; private set; } = new List<Vector2>();
        public List<Vector2> Pits { get; private set; } = new List<Vector2>();
        public List<Vector2> Zones { get; private set; } = new List<Vector2>();
        public GameState State { get; set; } = GameState.Playing;
        public int CurrentLevel { get; private set; } = 1;
        public int TotalLevels { get; private set; } = 3;

        public bool IsBoxMoving { get; private set; } = false;
        public bool MagnetBlocked { get; private set; } = false;

        public event EventHandler BoxMoved;
        public event EventHandler<BoxHitEventArgs> BoxHit;
        public event EventHandler<GameObject> ObjectBlinking;

        private DateTime _lastMagnetUse = DateTime.MinValue;
        private DateTime _lastPlayerMove = DateTime.MinValue;

        public void SetPlayer(Player p) => Player = p;

        public void LoadLevel(int idx)
        {
            var levels = LevelLoader.LoadAllLevels();
            if (idx < 0 || idx >= levels.Count) return;
            var lvl = levels[idx];
            CurrentLevel = idx + 1;
            Boxes.Clear(); Enemies.Clear(); Walls.Clear(); Pits.Clear(); Zones.Clear();

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
            _lastMagnetUse = DateTime.MinValue;
            _lastPlayerMove = DateTime.MinValue;
            IsBoxMoving = false;
            MagnetBlocked = false;
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

        public bool CanUseMagnet() => (DateTime.Now - _lastMagnetUse).TotalMilliseconds >= MAGNET_COOLDOWN_MS;

        public void ApplyMagnetForce(bool pull, int strength = MAGNET_STRENGTH)
        {
            if (Player == null || State != GameState.Playing || !CanUseMagnet()) return;

            _lastMagnetUse = DateTime.Now;
            IsBoxMoving = false;
            MagnetBlocked = false;

            var px = (int)Math.Round(Player.Position.X);
            var py = (int)Math.Round(Player.Position.Y);

            var boxes = Boxes.Where(b => !b.IsRemoved)
                .OrderBy(b => Math.Abs((int)Math.Round(b.Position.X) - px) + Math.Abs((int)Math.Round(b.Position.Y) - py))
                .ThenBy(b => b.Position.X).ThenBy(b => b.Position.Y).ToList();

            var box = boxes.FirstOrDefault();
            if (box != null)
            {
                int dir = pull ? -1 : 1;
                int movedSteps = 0; // ✅ СЧЁТЧИК РЕАЛЬНЫХ ШАГОВ ЯЩИКА

                for (int i = 0; i < strength; i++)
                {
                    if (!MoveBoxOneStep(box, px, py, dir)) { MagnetBlocked = true; break; }
                    movedSteps++;
                    IsBoxMoving = true;
                }

                if (movedSteps > 0)
                {
                    AddBlinkingEffect(box);
                    BoxMoved?.Invoke(this, EventArgs.Empty);

                    // ✅ АЧИВКА: если ящик пролетел 4+ клетки за одно нажатие
                    if (movedSteps >= 4) AchievementManager.OnMagnetUsed(movedSteps);
                }

                if (pull)
                {
                    int bx = (int)Math.Round(box.Position.X);
                    int by = (int)Math.Round(box.Position.Y);
                    int distance = Math.Abs(bx - px) + Math.Abs(by - py);

                    if (distance <= 2) BoxHit?.Invoke(this, new BoxHitEventArgs { Box = box, Player = Player });
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
            else { if (Math.Abs(dx) > Math.Abs(dy)) mx = dx > 0 ? 1 : -1; else my = dy > 0 ? 1 : -1; }

            mx *= dir; my *= dir;
            return TryMoveBox(box, bx + mx, by + my, mx, my);
        }

        private bool TryMoveBox(Box box, int tx, int ty, int mx, int my)
        {
            if (tx < 0 || tx >= WIDTH || ty < 0 || ty >= HEIGHT) return false;
            foreach (var w in Walls) if ((int)Math.Round(w.X) == tx && (int)Math.Round(w.Y) == ty) return false;
            if (Player != null) { var pp = Player.Position; if ((int)Math.Round(pp.X) == tx && (int)Math.Round(pp.Y) == ty) return false; }
            foreach (var b in Boxes.Where(o => o != box && !o.IsRemoved)) { var bp = b.Position; if ((int)Math.Round(bp.X) == tx && (int)Math.Round(bp.Y) == ty) return false; }

            Enemy enemy = null;
            foreach (var e in Enemies.Where(o => !o.IsRemoved)) { var ep = e.Position; if ((int)Math.Round(ep.X) == tx && (int)Math.Round(ep.Y) == ty) { enemy = e; break; } }

            if (enemy != null)
            {
                if (!CanEnemyMove(enemy, tx + mx, ty + my)) return false;
                box.Position = new Vector2(tx, ty); box.Velocity = new Vector2(0, 0);
                enemy.Position = new Vector2(tx + mx, ty + my); enemy.Velocity = new Vector2(0, 0);
                return true;
            }

            box.Position = new Vector2(tx, ty); box.Velocity = new Vector2(0, 0);
            return true;
        }

        private bool CanEnemyMove(Enemy e, int x, int y)
        {
            if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT) return false;
            if (Player != null) { var pp = Player.Position; if ((int)Math.Round(pp.X) == x && (int)Math.Round(pp.Y) == y) return false; }
            foreach (var w in Walls) if ((int)Math.Round(w.X) == x && (int)Math.Round(w.Y) == y) return false;
            foreach (var b in Boxes.Where(o => !o.IsRemoved)) { var bp = b.Position; if ((int)Math.Round(bp.X) == x && (int)Math.Round(bp.Y) == y) return false; }
            foreach (var o in Enemies.Where(en => en != e && !en.IsRemoved)) { var op = o.Position; if ((int)Math.Round(op.X) == x && (int)Math.Round(op.Y) == y) return false; }
            return true;
        }

        public void RegisterPlayerMove() => _lastPlayerMove = DateTime.Now;
        public bool CanPlayerMove() => (DateTime.Now - _lastPlayerMove).TotalMilliseconds >= 150;
        public void ResetMagnetBlocked() => MagnetBlocked = false;

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
                int ex = (int)Math.Round(e.Position.X);
                int ey = (int)Math.Round(e.Position.Y);

                bool atLeftEdge = (ex == 0);
                bool atRightEdge = (ex == WIDTH - 1);
                bool atTopEdge = (ey == 0);
                bool atBottomEdge = (ey == HEIGHT - 1);

                if (atLeftEdge || atRightEdge || atTopEdge || atBottomEdge)
                {
                    bool canPushAway = false;

                    if (atLeftEdge)
                    {
                        var checkPos = new Vector2(ex + 1, ey);
                        if (!Walls.Contains(checkPos) && !Boxes.Any(b => (int)Math.Round(b.Position.X) == checkPos.X && (int)Math.Round(b.Position.Y) == checkPos.Y && !b.IsRemoved))
                            canPushAway = true;
                    }
                    else if (atRightEdge)
                    {
                        var checkPos = new Vector2(ex - 1, ey);
                        if (!Walls.Contains(checkPos) && !Boxes.Any(b => (int)Math.Round(b.Position.X) == checkPos.X && (int)Math.Round(b.Position.Y) == checkPos.Y && !b.IsRemoved))
                            canPushAway = true;
                    }
                    else if (atTopEdge)
                    {
                        var checkPos = new Vector2(ex, ey + 1);
                        if (!Walls.Contains(checkPos) && !Boxes.Any(b => (int)Math.Round(b.Position.X) == checkPos.X && (int)Math.Round(b.Position.Y) == checkPos.Y && !b.IsRemoved))
                            canPushAway = true;
                    }
                    else if (atBottomEdge)
                    {
                        var checkPos = new Vector2(ex, ey - 1);
                        if (!Walls.Contains(checkPos) && !Boxes.Any(b => (int)Math.Round(b.Position.X) == checkPos.X && (int)Math.Round(b.Position.Y) == checkPos.Y && !b.IsRemoved))
                            canPushAway = true;
                    }

                    if (!canPushAway)
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
            if (Enemies.Any(e => !e.IsRemoved)) { State = GameState.Playing; return; }
            var boxes = Boxes.Where(b => !b.IsRemoved).ToList();
            if (boxes.Count == 0) { State = GameState.Playing; return; }

            int inZone = boxes.Count(b => Zones.Contains(new Vector2((int)Math.Round(b.Position.X), (int)Math.Round(b.Position.Y))));
            State = (inZone == boxes.Count && inZone == Zones.Count) ? GameState.Won : GameState.Playing;
        }

        public bool CheckWin()
        {
            if (Enemies.Any(e => !e.IsRemoved)) return false;
            var boxes = Boxes.Count(b => !b.IsRemoved);
            int inZone = Boxes.Where(b => !b.IsRemoved).Count(b => Zones.Contains(new Vector2((int)Math.Round(b.Position.X), (int)Math.Round(b.Position.Y))));
            return inZone == boxes && inZone == Zones.Count;
        }

        public void NextLevel() { if (CurrentLevel < TotalLevels) LoadLevel(CurrentLevel); else State = GameState.Won; }
        public void RestartLevel() => LoadLevel(CurrentLevel - 1);
        private void AddBlinkingEffect(GameObject obj) => ObjectBlinking?.Invoke(this, obj);
    }

    public class BoxHitEventArgs : EventArgs { public Box Box { get; set; } public Player Player { get; set; } }
}