using MagnitArena.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MagnitArena.View
{
    public partial class Form1 : Form
    {
        private bool _soundInitialized = false;
        private List<MagnetLine> _magnetLines = new List<MagnetLine>();
        private Dictionary<GameObject, DateTime> _blinkingObjects = new Dictionary<GameObject, DateTime>();
        private HashSet<Keys> _pressedKeys = new HashSet<Keys>();
        private Dictionary<Keys, DateTime> _keyPressStartTime = new Dictionary<Keys, DateTime>();
        private World _world;
        private Timer _timer;
        private const int CELL = 40;
        private Label _status;
        private Label _level;
        private DateTime _levelStartTime;
        private Panel _pauseOverlay;
        private Panel _winOverlay;
        private Panel _loseOverlay;
        private Label _hintLabel;
        private Bitmap _imgPlayer, _imgBox, _imgEnemy, _imgWall, _imgPit, _imgZone;

        public Form1()
        {
            SetupUI();
            SetupOverlays();
            LoadImages();
            InitGame();
        }

        private void SetupUI()
        {
            this.ClientSize = new Size(800, 650);
            this.Text = "Магнит-Арена";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            _status = new Label { Text = "Игра идёт", ForeColor = Color.Lime, Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(10, 610), AutoSize = true };
            _level = new Label { Text = "Уровень 1", ForeColor = Color.Yellow, Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(600, 610), AutoSize = true };
            this.Controls.Add(_status);
            this.Controls.Add(_level);

            this.Paint += (s, e) => { DrawGrid(e.Graphics); DrawObjects(e.Graphics); };
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.FormClosing += (s, e) => { if (_timer != null) _timer.Stop(); SoundManager.StopMagnet(); };
            this.ResumeLayout();

            var btnPause = new Button { Location = new Point(750, 10), Size = new Size(40, 40), BackColor = Color.FromArgb(100, 100, 100), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPause.FlatAppearance.BorderSize = 0;
            btnPause.Paint += (s, e) =>
            {
                var g = e.Graphics;
                int w = 4, h = 20, gap = 6;
                int sx = (btnPause.Width - (2 * w + gap)) / 2;
                int sy = (btnPause.Height - h) / 2;
                g.FillRectangle(Brushes.White, sx, sy, w, h);
                g.FillRectangle(Brushes.White, sx + w + gap, sy, w, h);
            };
            btnPause.Click += (s, e) =>
            {
                if (_pauseOverlay.Visible) ResumeGame();
                else { _pauseOverlay.Visible = true; _pauseOverlay.BringToFront(); _timer.Stop(); SoundManager.StopMagnet(); }
            };
            this.Controls.Add(btnPause);
        }

        private void SetupOverlays()
        {
            _hintLabel = new Label { Text = "Q — оттолкнуть | E — притянуть | Esc — пауза", Font = new Font("Arial", 10), ForeColor = Color.Gray, Location = new Point(10, 10), AutoSize = true, BackColor = Color.FromArgb(30, 30, 30, 200) };
            _hintLabel.Padding = new Padding(10, 5, 10, 5);
            this.Controls.Add(_hintLabel); _hintLabel.BringToFront();

            _pauseOverlay = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(0, 0, 0, 180), Visible = false };
            _pauseOverlay.Controls.Add(new Label { Text = "ПАУЗА", Font = new Font("Arial", 28, FontStyle.Bold), ForeColor = Color.Yellow, Location = new Point(300, 200), AutoSize = true });

            var btnResume = new Button { Text = "▶ ПРОДОЛЖИТЬ", Font = new Font("Arial", 16), Location = new Point(275, 300), Size = new Size(250, 50), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnResume.FlatAppearance.BorderSize = 2; btnResume.FlatAppearance.BorderColor = Color.Cyan;
            btnResume.Click += (s, e) => ResumeGame();
            _pauseOverlay.Controls.Add(btnResume);

            var btnMenu = new Button { Text = "🏠 В МЕНЮ", Font = new Font("Arial", 16), Location = new Point(275, 370), Size = new Size(250, 50), BackColor = Color.FromArgb(180, 30, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMenu.FlatAppearance.BorderSize = 2; btnMenu.Click += (s, e) => GoToMainMenu();
            _pauseOverlay.Controls.Add(btnMenu);
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
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(0, 0, 0, 200), Visible = false };
            panel.Controls.Add(new Label { Name = "lblTitle", Text = title, Font = new Font("Arial", 32, FontStyle.Bold), ForeColor = titleColor, Location = new Point(225, 180), AutoSize = true });
            var btnA = new Button { Name = "btnAction", Text = btnActionText, Font = new Font("Arial", 16), Location = new Point(275, 300), Size = new Size(250, 50), BackColor = titleColor == Color.Lime ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 30, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnA.FlatAppearance.BorderSize = 2; panel.Controls.Add(btnA);
            var btnM = new Button { Name = "btnMenu", Text = btnMenuText, Font = new Font("Arial", 16), Location = new Point(275, 370), Size = new Size(250, 50), BackColor = Color.FromArgb(100, 100, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnM.FlatAppearance.BorderSize = 2; panel.Controls.Add(btnM);
            return panel;
        }

        private void InitGame()
        {
            if (!_soundInitialized) { SoundManager.Initialize(); _soundInitialized = true; }
            _world = new World(); _world.LoadLevel(0);

            _world.BoxMoved += (s, e) => { SoundManager.PlayHit(); SoundManager.StopMagnet(); };
            _world.BoxHit += (s, a) => { SoundManager.StopMagnet(); SoundManager.PlayHit(); if (!_blinkingObjects.ContainsKey(a.Box)) _blinkingObjects[a.Box] = DateTime.Now; };
            _world.ObjectBlinking += (s, o) => { if (!_blinkingObjects.ContainsKey(o)) _blinkingObjects[o] = DateTime.Now; };

            _levelStartTime = DateTime.Now;
            AchievementManager.ResetLevelProgress();

            _timer = new Timer { Interval = 30 };
            _timer.Tick += (s, e) =>
            {
                _world.Update(); _world.CheckCollisions();
                foreach (var k in _pressedKeys.ToList())
                {
                    if (IsMovementKey(k) && _keyPressStartTime.ContainsKey(k) && (DateTime.Now - _keyPressStartTime[k]).TotalMilliseconds >= 500)
                    { SoundManager.PlayStep1(); _keyPressStartTime[k] = DateTime.Now; }
                }
                if (_world.MagnetBlocked) { SoundManager.PlayHit(); _world.ResetMagnetBlocked(); }
                UpdateBlinkingEffects(); UpdateMagnetLines(); UpdateStatus(); Invalidate();
            };
            _timer.Start();
        }

        private void LoadImages()
        {
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            _imgPlayer = LoadImg("player.png", p); _imgBox = LoadImg("box.png", p);
            _imgEnemy = LoadImg("enemy.png", p); _imgWall = LoadImg("wall.png", p);
            _imgPit = LoadImg("pit.png", p); _imgZone = LoadImg("zone.png", p);
        }
        private Bitmap LoadImg(string f, string d) { string fp = Path.Combine(d, f); if (File.Exists(fp)) return new Bitmap(fp); return null; }
        private bool IsMovementKey(Keys k) { return k == Keys.Up || k == Keys.Down || k == Keys.Left || k == Keys.Right || k == Keys.W || k == Keys.S || k == Keys.A || k == Keys.D; }

        private void UpdateStatus()
        {
            if (_status == null || _level == null || _world == null) return;
            if (_world.State == GameState.Won)
            {
                _status.Text = "ПОБЕДА!"; _status.ForeColor = Color.Lime;
                if (!_winOverlay.Visible)
                {
                    SoundManager.StopMagnet(); SoundManager.PlayWin();
                    AchievementManager.OnLevelWon(_world.CurrentLevel, _world.TotalLevels, DateTime.Now - _levelStartTime);
                    _winOverlay.Visible = true; _winOverlay.BringToFront(); _timer.Stop();
                }
            }
            else if (_world.State == GameState.Lost)
            {
                _status.Text = "ПОРАЖЕНИЕ!"; _status.ForeColor = Color.OrangeRed;
                if (!_loseOverlay.Visible)
                {
                    SoundManager.StopMagnet(); SoundManager.PlayHit(); SoundManager.PlayLose();
                    _loseOverlay.Visible = true; _loseOverlay.BringToFront(); _timer.Stop();
                }
            }
            else { _status.Text = "Игра идёт"; _status.ForeColor = Color.Lime; }
            _level.Text = string.Format("Уровень {0}/{1}", _world.CurrentLevel, _world.TotalLevels);
        }

        private void DrawGrid(Graphics g)
        {
            using (Pen p = new Pen(Color.FromArgb(50, 50, 50), 1))
            {
                for (int x = 0; x <= World.WIDTH; x++) g.DrawLine(p, x * CELL, 0, x * CELL, World.HEIGHT * CELL);
                for (int y = 0; y <= World.HEIGHT; y++) g.DrawLine(p, 0, y * CELL, World.WIDTH * CELL, y * CELL);
            }
        }

        private void DrawObjects(Graphics g)
        {
            foreach (var w in _world.Walls) { var r = Rect(w); if (_imgWall != null) g.DrawImage(_imgWall, r); else { g.FillRectangle(Brushes.Gray, r); g.DrawRectangle(Pens.DarkGray, r); } }
            foreach (var p in _world.Pits) { var r = Rect(p); if (_imgPit != null) g.DrawImage(_imgPit, r); else { g.FillRectangle(Brushes.DarkBlue, r); g.DrawRectangle(Pens.Blue, r); } }
            foreach (var z in _world.Zones) { var r = Rect(z); if (_imgZone != null) g.DrawImage(_imgZone, r); else { g.FillRectangle(Brushes.DarkGreen, r); g.DrawRectangle(Pens.Green, r); } }
            foreach (var b in _world.Boxes.Where(o => !o.IsRemoved)) { var r = Rect(b.Position); if (IsBlinking(b)) g.FillRectangle(new SolidBrush(Color.FromArgb(150, Color.White)), r); else if (_imgBox != null) g.DrawImage(_imgBox, r); else { g.FillRectangle(Brushes.Yellow, r); g.DrawRectangle(Pens.Gold, r); } }
            foreach (var e in _world.Enemies.Where(o => !o.IsRemoved)) { var r = Rect(e.Position); if (IsBlinking(e)) g.FillRectangle(new SolidBrush(Color.FromArgb(150, Color.Pink)), r); else if (_imgEnemy != null) g.DrawImage(_imgEnemy, r); else { g.FillRectangle(Brushes.Red, r); g.DrawRectangle(Pens.DarkRed, r); } }
            if (_world.Player != null) { var r = Rect(_world.Player.Position); if (IsBlinking(_world.Player)) g.FillRectangle(new SolidBrush(Color.FromArgb(150, Color.White)), r); else if (_imgPlayer != null) g.DrawImage(_imgPlayer, r); else { g.FillRectangle(Brushes.Lime, r); g.DrawRectangle(Pens.Green, r); } }
            DrawMagnetLines(g);
        }

        private Rectangle Rect(Vector2 p) { return new Rectangle((int)Math.Round(p.X) * CELL, (int)Math.Round(p.Y) * CELL, CELL, CELL); }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (_pauseOverlay.Visible) ResumeGame();
                else if (_winOverlay.Visible || _loseOverlay.Visible) GoToMainMenu();
                else { _pauseOverlay.Visible = true; _pauseOverlay.BringToFront(); _timer.Stop(); SoundManager.StopMagnet(); }
                e.Handled = true; return;
            }
            if (_pauseOverlay.Visible || _winOverlay.Visible || _loseOverlay.Visible || _world.Player == null) return;

            if (e.KeyCode == Keys.Q && _world.CanUseMagnet()) { CreateMagnetLines(false); _world.ApplyMagnetForce(false); SoundManager.StartMagnet(); Invalidate(); return; }
            if (e.KeyCode == Keys.E && _world.CanUseMagnet()) { CreateMagnetLines(true); _world.ApplyMagnetForce(true); SoundManager.StartMagnet(); Invalidate(); return; }
            if (e.KeyCode == Keys.R) { RestartLevel(); return; }

            if (IsMovementKey(e.KeyCode))
            {
                if (!_pressedKeys.Contains(e.KeyCode)) { _keyPressStartTime[e.KeyCode] = DateTime.Now; SoundManager.PlayStep1(); }
                _pressedKeys.Add(e.KeyCode);
                int x = (int)Math.Round(_world.Player.Position.X), y = (int)Math.Round(_world.Player.Position.Y), nx = x, ny = y; bool move = false;
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) { ny--; move = true; }
                else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S) { ny++; move = true; }
                else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) { nx--; move = true; }
                else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) { nx++; move = true; }
                if (move && CanMove(nx, ny) && _world.CanPlayerMove()) { _world.Player.Position = new Vector2(nx, ny); _world.RegisterPlayerMove(); Invalidate(); }
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e) { if (IsMovementKey(e.KeyCode)) { _pressedKeys.Remove(e.KeyCode); _keyPressStartTime.Remove(e.KeyCode); } }
        private bool CanMove(int x, int y)
        {
            if (x < 0 || x >= 20 || y < 0 || y >= 15) return false;
            foreach (var w in _world.Walls) if ((int)Math.Round(w.X) == x && (int)Math.Round(w.Y) == y) return false;
            foreach (var b in _world.Boxes.Where(o => !o.IsRemoved)) { var bp = b.Position; if ((int)Math.Round(bp.X) == x && (int)Math.Round(bp.Y) == y) return false; }
            foreach (var en in _world.Enemies.Where(o => !o.IsRemoved)) { var ep = en.Position; if ((int)Math.Round(ep.X) == x && (int)Math.Round(ep.Y) == y) return false; }
            return true;
        }

        private void ResumeGame() { _pauseOverlay.Visible = false; _timer.Start(); }
        private void GoToMainMenu() { _timer.Stop(); _pauseOverlay.Visible = false; _winOverlay.Visible = false; _loseOverlay.Visible = false; SoundManager.StopMagnet(); this.Close(); }
        private void NextLevel() { _winOverlay.Visible = false; if (_world.CurrentLevel < _world.TotalLevels) { _world.LoadLevel(_world.CurrentLevel); _levelStartTime = DateTime.Now; _timer.Start(); } else GoToMainMenu(); }
        private void RestartLevel() { _loseOverlay.Visible = false; _world.RestartLevel(); _levelStartTime = DateTime.Now; _timer.Start(); }

        private void CreateMagnetLines(bool pull)
        {
            _magnetLines.Clear();
            if (_world.Player == null) return;

            int px = (int)Math.Round(_world.Player.Position.X);
            int py = (int)Math.Round(_world.Player.Position.Y);

            foreach (var box in _world.Boxes.Where(b => !b.IsRemoved))
            {
                int bx = (int)Math.Round(box.Position.X);
                int by = (int)Math.Round(box.Position.Y);

                _magnetLines.Add(new MagnetLine
                {
                    Start = new Point(px * CELL + CELL / 2, py * CELL + CELL / 2),
                    End = new Point(bx * CELL + CELL / 2, by * CELL + CELL / 2),
                    Pull = pull,
                    CreatedAt = DateTime.Now
                });
            }
        }
        private void UpdateMagnetLines() { _magnetLines.RemoveAll(l => (DateTime.Now - l.CreatedAt).TotalMilliseconds > 500); }
        private void UpdateBlinkingEffects() { var rem = _blinkingObjects.Where(kvp => (DateTime.Now - kvp.Value).TotalMilliseconds > 1000).Select(kvp => kvp.Key).ToList(); foreach (var o in rem) _blinkingObjects.Remove(o); }
        private void DrawMagnetLines(Graphics g) { foreach (var l in _magnetLines) { using (Pen p = new Pen(l.Pull ? Color.Lime : Color.Orange, 2)) { p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash; g.DrawLine(p, l.Start, l.End); } } }
        private bool IsBlinking(GameObject o) { if (!_blinkingObjects.ContainsKey(o)) return false; return ((int)(DateTime.Now - _blinkingObjects[o]).TotalMilliseconds / 100) % 2 == 0; }
    }
    public class MagnetLine { public Point Start { get; set; } public Point End { get; set; } public bool Pull { get; set; } public DateTime CreatedAt { get; set; } }
}