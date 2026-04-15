using System;
using System.Drawing;
using System.Windows.Forms;
using MagnitArena.Model;
using System.Linq;

namespace MagnitArena.View
{
    public partial class Form1 : Form
    {
        private World _world;
        private Timer _timer;
        private const int CELL = 40;
        private Label _status;
        private Label _level;

        public Form1()
        {
            SetupUI();
            InitGame();
        }

        private void SetupUI()
        {
            this.SuspendLayout();

            this.ClientSize = new Size(800, 650);
            this.Text = "Магнит-Арена";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            _status = new Label
            {
                Text = "Игра идёт",
                ForeColor = Color.Lime,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(10, 610),
                AutoSize = true
            };

            _level = new Label
            {
                Text = "Уровень 1",
                ForeColor = Color.Yellow,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(600, 610),
                AutoSize = true
            };

            this.Controls.Add(_status);
            this.Controls.Add(_level);

            this.Paint += (s, e) =>
            {
                DrawGrid(e.Graphics);
                DrawObjects(e.Graphics);
            };

            this.KeyDown += Form1_KeyDown;

            this.ResumeLayout();
        }

        private void InitGame()
        {
            _world = new World();
            _world.LoadLevel(0);

            _timer = new Timer { Interval = 30 };
            _timer.Tick += (s, e) =>
            {
                _world.Update();
                _world.CheckCollisions();
                UpdateStatus();
                Invalidate();
            };
            _timer.Start();
        }

        private void UpdateStatus()
        {
            if (_world.State == GameState.Won)
            {
                _status.Text = "ПОБЕДА! Нажмите N";
                _status.ForeColor = Color.Lime;
            }
            else if (_world.State == GameState.Lost)
            {
                _status.Text = "ПОРАЖЕНИЕ! Нажмите R";
                _status.ForeColor = Color.Red;
            }
            else
            {
                _status.Text = "Игра идёт";
                _status.ForeColor = Color.Lime;
            }

            _level.Text = $"Уровень {_world.CurrentLevel}/{_world.TotalLevels}";
        }

        private void DrawGrid(Graphics g)
        {
            var pen = new Pen(Color.FromArgb(50, 50, 50), 1);

            for (int x = 0; x <= World.WIDTH; x++)
                g.DrawLine(pen, x * CELL, 0, x * CELL, World.HEIGHT * CELL);

            for (int y = 0; y <= World.HEIGHT; y++)
                g.DrawLine(pen, 0, y * CELL, World.WIDTH * CELL, y * CELL);
        }

        private void DrawObjects(Graphics g)
        {
            foreach (var w in _world.Walls)
            {
                var r = Rect(w);
                g.FillRectangle(Brushes.Gray, r);
                g.DrawRectangle(Pens.DarkGray, r);
            }

            foreach (var p in _world.Pits)
            {
                var r = Rect(p);
                g.FillRectangle(Brushes.DarkBlue, r);
                g.DrawRectangle(Pens.Blue, r);
            }

            foreach (var z in _world.Zones)
            {
                var r = Rect(z);
                g.FillRectangle(Brushes.DarkGreen, r);
                g.DrawRectangle(Pens.Green, r);
            }

            foreach (var b in _world.Boxes.Where(o => !o.IsRemoved))
            {
                var r = Rect(b.Position);
                g.FillRectangle(Brushes.Yellow, r);
                g.DrawRectangle(Pens.Gold, r);
            }

            foreach (var e in _world.Enemies.Where(o => !o.IsRemoved))
            {
                var r = Rect(e.Position);
                g.FillRectangle(Brushes.Red, r);
                g.DrawRectangle(Pens.DarkRed, r);
            }

            if (_world.Player != null)
            {
                var r = Rect(_world.Player.Position);
                g.FillRectangle(Brushes.Lime, r);
                g.DrawRectangle(Pens.Green, r);
            }
        }

        private Rectangle Rect(Vector2 p)
        {
            return new Rectangle(
                (int)Math.Round(p.X) * CELL + 1,
                (int)Math.Round(p.Y) * CELL + 1,
                CELL - 2,
                CELL - 2
            );
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (_world.Player == null) return;

            if (_world.State == GameState.Won && e.KeyCode == Keys.N)
            {
                _world.NextLevel();
                Invalidate();
                return;
            }

            if (_world.State == GameState.Lost && e.KeyCode == Keys.R)
            {
                _world.RestartLevel();
                Invalidate();
                return;
            }

            int x = (int)Math.Round(_world.Player.Position.X);
            int y = (int)Math.Round(_world.Player.Position.Y);
            int nx = x, ny = y;
            bool move = false;

            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
            {
                ny = y - 1;
                move = true;
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S)
            {
                ny = y + 1;
                move = true;
            }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
            {
                nx = x - 1;
                move = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
            {
                nx = x + 1;
                move = true;
            }
            else if (e.KeyCode == Keys.Q)
            {
                _world.ApplyMagnetForce(false);
                Invalidate();
                return;
            }
            else if (e.KeyCode == Keys.E)
            {
                _world.ApplyMagnetForce(true);
                Invalidate();
                return;
            }
            else if (e.KeyCode == Keys.R)
            {
                _world.RestartLevel();
                Invalidate();
                return;
            }
            else return;

            if (move && CanMove(nx, ny))
            {
                _world.Player.Position = new Vector2(nx, ny);
                Invalidate();
            }
        }

        private bool CanMove(int x, int y)
        {
            if (x < 0 || x >= 20 || y < 0 || y >= 15) return false;

            foreach (var w in _world.Walls)
                if ((int)Math.Round(w.X) == x && (int)Math.Round(w.Y) == y)
                    return false;

            foreach (var b in _world.Boxes.Where(o => !o.IsRemoved))
            {
                var bx = (int)Math.Round(b.Position.X);
                var by = (int)Math.Round(b.Position.Y);
                if (x == bx && y == by) return false;
            }

            foreach (var en in _world.Enemies.Where(o => !o.IsRemoved))
            {
                var ex = (int)Math.Round(en.Position.X);
                var ey = (int)Math.Round(en.Position.Y);
                if (x == ex && y == ey) return false;
            }

            return true;
        }
    }
}