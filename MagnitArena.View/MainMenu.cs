using System;
using System.Drawing;
using System.Windows.Forms;

namespace MagnitArena.View
{
    public class MainMenu : Form
    {
        private Button _btnStart;
        private Button _btnExit;
        private Label _title;

        public event EventHandler StartClicked;

        public MainMenu()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            this.SuspendLayout();

            this.ClientSize = new Size(800, 650);
            this.Text = "Магнит-Арена — Главное меню";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            _title = new Label
            {
                Text = "🧲 МАГНИТ-АРЕНА 🧲",
                Font = new Font("Arial", 32, FontStyle.Bold),
                ForeColor = Color.Cyan,
                Location = new Point(150, 150),
                AutoSize = true
            };

            _btnStart = new Button
            {
                Text = "▶ СТАРТ",
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 120, 215),
                Location = new Point(275, 300),
                Size = new Size(250, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnStart.FlatAppearance.BorderSize = 2;
            _btnStart.FlatAppearance.BorderColor = Color.Cyan;
            _btnStart.Click += (s, e) => StartClicked?.Invoke(this, EventArgs.Empty);

            _btnExit = new Button
            {
                Text = "✕ ВЫХОД",
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(180, 30, 30),
                Location = new Point(275, 380),
                Size = new Size(250, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnExit.FlatAppearance.BorderSize = 2;
            _btnExit.FlatAppearance.BorderColor = Color.OrangeRed;
            _btnExit.Click += BtnExit_Click;

            var hint = new Label
            {
                Text = "Управление: WASD / Стрелки • E — притянуть • Q — оттолкнуть",
                Font = new Font("Arial", 10),
                ForeColor = Color.Gray,
                Location = new Point(50, 580),
                AutoSize = true
            };

            this.Controls.Add(_title);
            this.Controls.Add(_btnStart);
            this.Controls.Add(_btnExit);
            this.Controls.Add(hint);

            this.ResumeLayout();
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
            Environment.Exit(0);
        }
    }
}