using System;
using System.Drawing;
using System.Windows.Forms;
using MagnetArena.Model;
using System.Linq;

namespace MagnetArena.View
{
    public partial class Form1 : Form
    {
        private World _world;
        private Timer _gameTimer;
        private const int CELL_SIZE = 40;

        public Form1()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(800, 600);
            this.Name = "Form1";
            this.Text = "Магнит-Арена";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            this.KeyDown += Form1_KeyDown;
            this.Paint += Form1_Paint;

            this.ResumeLayout(false);
        }

        private void InitializeGame()
        {
            _world = new World();
            var player = new Player { Position = new Vector2(5, 5) };
            _world.SetPlayer(player);

            var box = new Box { Position = new Vector2(10, 5) };
            _world.Boxes.Add(box);

            _gameTimer = new Timer();
            _gameTimer.Interval = 30;
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            _world.Update();
            _world.CheckCollisions();
            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            DrawGrid(e.Graphics);
            DrawObjects(e.Graphics);
        }

        private void DrawGrid(Graphics g)
        {
            Pen gridPen = new Pen(Color.FromArgb(50, 50, 50), 1);
            int width = 20;
            int height = 15;
            for (int x = 0; x <= width; x++)
                g.DrawLine(gridPen, x * CELL_SIZE, 0, x * CELL_SIZE, height * CELL_SIZE);
            for (int y = 0; y <= height; y++)
                g.DrawLine(gridPen, 0, y * CELL_SIZE, width * CELL_SIZE, y * CELL_SIZE);
        }

        private void DrawObjects(Graphics g)
        {
            foreach (var wall in _world.Walls)
            {
                var rect = GetRectangle(wall);
                g.FillRectangle(Brushes.Gray, rect);
                g.DrawRectangle(Pens.DarkGray, rect);
            }

            foreach (var pit in _world.Pits)
            {
                var rect = GetRectangle(pit);
                g.FillRectangle(Brushes.DarkBlue, rect);
                g.DrawRectangle(Pens.Blue, rect);
            }

            foreach (var zone in _world.Zones)
            {
                var rect = GetRectangle(zone);
                g.FillRectangle(Brushes.DarkGreen, rect);
                g.DrawRectangle(Pens.Green, rect);
            }

            foreach (var box in _world.Boxes)
            {
                if (!box.IsRemoved)
                {
                    var rect = GetRectangle(box.Position);
                    g.FillRectangle(Brushes.Brown, rect);
                    g.DrawRectangle(Pens.SaddleBrown, rect);
                }
            }

            foreach (var enemy in _world.Enemies)
            {
                if (!enemy.IsRemoved)
                {
                    var rect = GetRectangle(enemy.Position);
                    g.FillRectangle(Brushes.Red, rect);
                    g.DrawRectangle(Pens.DarkRed, rect);
                }
            }

            if (_world.Player != null)
            {
                var rect = GetRectangle(_world.Player.Position);
                g.FillRectangle(Brushes.Lime, rect);
                g.DrawRectangle(Pens.Green, rect);
            }
        }

        private Rectangle GetRectangle(Vector2 position)
        {
            int x = (int)Math.Round(position.X) * CELL_SIZE + 1;
            int y = (int)Math.Round(position.Y) * CELL_SIZE + 1;
            return new Rectangle(x, y, CELL_SIZE - 2, CELL_SIZE - 2);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (_world.Player == null) return;

            var currentX = (int)Math.Round(_world.Player.Position.X);
            var currentY = (int)Math.Round(_world.Player.Position.Y);
            int newX = currentX;
            int newY = currentY;
            bool shouldMove = false;

            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
            {
                newY = currentY - 1;
                shouldMove = true;
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S)
            {
                newY = currentY + 1;
                shouldMove = true;
            }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
            {
                newX = currentX - 1;
                shouldMove = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
            {
                newX = currentX + 1;
                shouldMove = true;
            }
            else if (e.KeyCode == Keys.Q)
            {
                _world.ApplyMagnetForce(false, 1.0);
                this.Invalidate();
                return;
            }
            else if (e.KeyCode == Keys.E)
            {
                _world.ApplyMagnetForce(true, 1.0);
                this.Invalidate();
                return;
            }
            else
            {
                return;
            }

            if (shouldMove && CanMoveTo(newX, newY))
            {
                _world.Player.Position = new Vector2(newX, newY);
                this.Invalidate();
            }
        }

        private bool CanMoveTo(int x, int y)
        {
            if (x < 0 || x >= 20 || y < 0 || y >= 15)
                return false;

            foreach (var box in _world.Boxes.Where(b => !b.IsRemoved))
            {
                var boxX = (int)Math.Round(box.Position.X);
                var boxY = (int)Math.Round(box.Position.Y);
                if (x == boxX && y == boxY)
                    return false;
            }

            foreach (var enemy in _world.Enemies.Where(e => !e.IsRemoved))
            {
                var enemyX = (int)Math.Round(enemy.Position.X);
                var enemyY = (int)Math.Round(enemy.Position.Y);
                if (x == enemyX && y == enemyY)
                    return false;
            }

            return true;
        }
    }
}