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

        public event EventHandler BackToMenu;
        public event EventHandler GameExited;

        private Panel _pauseOverlay;
        private Panel _winOverlay;
        private Panel _loseOverlay;
        private Label _hintLabel;

        public Form1()
        {
            SetupUI();
            SetupOverlays();
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

            this.FormClosing += (s, e) =>
            {
                e.Cancel = true;
                GoToMainMenu();
            };

            this.ResumeLayout();
        }

        private void SetupOverlays()
        {
            _hintLabel = new Label
            {
                Text = "Q — оттолкнуть | E — притянуть | Esc — пауза",
                Font = new Font("Arial", 10),
                ForeColor = Color.Gray,
                Location = new Point(10, 10),
                AutoSize = true,
                BackColor = Color.FromArgb(30, 30, 30, 200)
            };
            _hintLabel.Padding = new Padding(10, 5, 10, 5);
            this.Controls.Add(_hintLabel);
            _hintLabel.BringToFront();

            _pauseOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(0, 0, 0, 180),
                Visible = false
            };

            var pauseTitle = new Label
            {
                Text = "⏸ ПАУЗА",
                Font = new Font("Arial", 28, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Location = new Point(275, 200),
                AutoSize = true
            };

            var btnResume = new Button
            {
                Text = "▶ ПРОДОЛЖИТЬ",
                Font = new Font("Arial", 16),
                Location = new Point(275, 300),
                Size = new Size(250, 50),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnResume.FlatAppearance.BorderSize = 2;
            btnResume.FlatAppearance.BorderColor = Color.Cyan;
            btnResume.Click += (s, e) => ResumeGame();

            var btnMenuPause = new Button
            {
                Text = "🏠 В МЕНЮ",
                Font = new Font("Arial", 16),
                Location = new Point(275, 370),
                Size = new Size(250, 50),
                BackColor = Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnMenuPause.FlatAppearance.BorderSize = 2;
            btnMenuPause.Click += (s, e) => GoToMainMenuFromPause();

            _pauseOverlay.Controls.Add(pauseTitle);
            _pauseOverlay.Controls.Add(btnResume);
            _pauseOverlay.Controls.Add(btnMenuPause);
            this.Controls.Add(_pauseOverlay);

            _winOverlay = CreateResultOverlay("🏆 ПОБЕДА!", Color.Lime, "▶ СЛЕДУЮЩИЙ УРОВЕНЬ", "🏠 В МЕНЮ");
            _winOverlay.Controls.Find("btnAction", true)[0].Click += (s, e) => NextLevel();
            _winOverlay.Controls.Find("btnMenu", true)[0].Click += (s, e) => GoToMainMenu();
            this.Controls.Add(_winOverlay);

            _loseOverlay = CreateResultOverlay("💀 ПОРАЖЕНИЕ", Color.OrangeRed, "🔄 РЕСТАРТ", "🏠 В МЕНЮ");
            _loseOverlay.Controls.Find("btnAction", true)[0].Click += (s, e) => RestartLevel();
            _loseOverlay.Controls.Find("btnMenu", true)[0].Click += (s, e) => GoToMainMenu();
            this.Controls.Add(_loseOverlay);
        }

        private Panel CreateResultOverlay(string title, Color titleColor, string btnActionText, string btnMenuText)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(0, 0, 0, 200),
                Visible = false
            };

            var lblTitle = new Label
            {
                Name = "lblTitle",
                Text = title,
                Font = new Font("Arial", 32, FontStyle.Bold),
                ForeColor = titleColor,
                Location = new Point(225, 180),
                AutoSize = true
            };

            var btnAction = new Button
            {
                Name = "btnAction",
                Text = btnActionText,
                Font = new Font("Arial", 16),
                Location = new Point(275, 300),
                Size = new Size(250, 50),
                BackColor = titleColor == Color.Lime ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAction.FlatAppearance.BorderSize = 2;

            var btnMenu = new Button
            {
                Name = "btnMenu",
                Text = btnMenuText,
                Font = new Font("Arial", 16),
                Location = new Point(275, 370),
                Size = new Size(250, 50),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnMenu.FlatAppearance.BorderSize = 2;

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(btnAction);
            panel.Controls.Add(btnMenu);

            return panel;
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
                _status.Text = "ПОБЕДА!";
                _status.ForeColor = Color.Lime;
                if (!_winOverlay.Visible)
                {
                    _winOverlay.Visible = true;
                    _winOverlay.BringToFront();
                    _timer.Stop();
                }
            }
            else if (_world.State == GameState.Lost)
            {
                _status.Text = "ПОРАЖЕНИЕ!";
                _status.ForeColor = Color.OrangeRed;
                if (!_loseOverlay.Visible)
                {
                    _loseOverlay.Visible = true;
                    _loseOverlay.BringToFront();
                    _timer.Stop();
                }
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
            if (e.KeyCode == Keys.Escape)
            {
                if (_pauseOverlay.Visible)
                {
                    ResumeGame();
                }
                else if (_winOverlay.Visible || _loseOverlay.Visible)
                {
                    GoToMainMenu();
                }
                else
                {
                    _pauseOverlay.Visible = true;
                    _pauseOverlay.BringToFront();
                    _timer.Stop();
                }
                e.Handled = true;
                return;
            }

            if (_pauseOverlay.Visible || _winOverlay.Visible || _loseOverlay.Visible) return;
            if (_world.Player == null) return;

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
                RestartLevel();
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

        private void ResumeGame()
        {
            _pauseOverlay.Visible = false;
            _timer.Start();
        }

        private void GoToMainMenu()
        {
            _timer.Stop();
            _pauseOverlay.Visible = false;
            _winOverlay.Visible = false;
            _loseOverlay.Visible = false;
            BackToMenu?.Invoke(this, EventArgs.Empty);
        }

        private void GoToMainMenuFromPause()
        {
            _pauseOverlay.Visible = false;
            _timer.Stop();
            this.Hide();
            BackToMenu?.Invoke(this, EventArgs.Empty);
        }

        private void NextLevel()
        {
            _winOverlay.Visible = false;
            if (_world.CurrentLevel < _world.TotalLevels)
            {
                _world.LoadLevel(_world.CurrentLevel);
                _timer.Start();
            }
            else
            {
                GoToMainMenu();
            }
        }

        private void RestartLevel()
        {
            _loseOverlay.Visible = false;
            _world.RestartLevel();
            _timer.Start();
        }

        public void RestartGame()
        {
            _world = new World();
            _world.LoadLevel(0);
            _pauseOverlay.Visible = false;
            _winOverlay.Visible = false;
            _loseOverlay.Visible = false;
            _timer.Start();
            Invalidate();
        }
    }
}