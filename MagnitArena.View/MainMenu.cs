using System;
using System.Drawing;
using System.Windows.Forms;
using MagnitArena.Model;

namespace MagnitArena.View
{
    public partial class MainMenu : Form
    {
        private Panel _mainPanel;
        private Panel _settingsPanel;
        private Panel _achievementsPanel;
        private FlowLayoutPanel _achievementsList;
        private TrackBar _trackMasterVolume;
        private TrackBar _trackSfxVolume;
        private TrackBar _trackMagnetVolume;
        private CheckBox _chkMute;
        private SoundSettings _soundSettings;

        public MainMenu()
        {
            SetupUI();
            SetupSettingsPanel();
            SetupAchievementsPanel();

            SoundManager.Initialize();
            AchievementManager.Initialize();

            LoadSoundSettings();

            this.FormClosing += (s, e) => Application.Exit();
        }

        private void SetupUI()
        {
            this.ClientSize = new Size(800, 600);
            this.Text = "Магнит-Арена: Главное меню";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            _mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            this.Controls.Add(_mainPanel);

            var lblTitle = new Label { Text = "🧲 МАГНИТ-АРЕНА", Font = new Font("Arial", 36, FontStyle.Bold), ForeColor = Color.Cyan, AutoSize = true, Location = new Point(180, 80) };
            _mainPanel.Controls.Add(lblTitle);

            AddButton("▶ ИГРАТЬ", 250, 200, Color.FromArgb(0, 120, 215), (s, e) => StartGame());
            AddButton("⚙️ НАСТРОЙКИ", 250, 270, Color.FromArgb(100, 100, 100), (s, e) => ShowSettings());
            AddButton("🏆 ДОСТИЖЕНИЯ", 250, 340, Color.FromArgb(180, 100, 0), (s, e) => ShowAchievements());
            AddButton("❌ ВЫХОД", 250, 410, Color.FromArgb(180, 30, 30), (s, e) => Application.Exit());

            AddButton("🗑️ СБРОС АЧИВОК", 250, 480, Color.DarkRed, (s, e) =>
            {
                AchievementManager.ResetAll();
                MessageBox.Show("Ачивки сброшены!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshAchievements(); // Обновить список, если открыт
            });
        }

        private void AddButton(string text, int x, int y, Color color, EventHandler clickHandler)
        {
            var btn = new Button { Text = text, Font = new Font("Arial", 16, FontStyle.Bold), Location = new Point(x, y), Size = new Size(300, 50), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += clickHandler;
            _mainPanel.Controls.Add(btn);
        }

        private void SetupSettingsPanel()
        {
            _settingsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 20), Visible = false };
            var lblTitle = new Label { Text = "⚙️ НАСТРОЙКИ ЗВУКА", Font = new Font("Arial", 24, FontStyle.Bold), ForeColor = Color.Lime, Location = new Point(250, 30), AutoSize = true };
            _settingsPanel.Controls.Add(lblTitle);

            AddSlider("Общая громкость", 100, ref _trackMasterVolume, v => _soundSettings.MasterVolume = v);
            AddSlider("Эффекты", 150, ref _trackSfxVolume, v => _soundSettings.SfxVolume = v);
            AddSlider("Магнит", 200, ref _trackMagnetVolume, v => _soundSettings.MagnetVolume = v);

            _chkMute = new CheckBox { Text = "🔇 Выключить звук", ForeColor = Color.White, Font = new Font("Arial", 12), Location = new Point(250, 260), AutoSize = true };
            _chkMute.CheckedChanged += (s, e) => { _soundSettings.Muted = _chkMute.Checked; SoundManager.SetSettings(_soundSettings); _soundSettings.Save(); };
            _settingsPanel.Controls.Add(_chkMute);

            var btnBack = CreateBackButton("← НАЗАД", () => { _settingsPanel.Visible = false; _mainPanel.Visible = true; });
            _settingsPanel.Controls.Add(btnBack);
            this.Controls.Add(_settingsPanel);
        }

        private void AddSlider(string label, int y, ref TrackBar track, Action<float> action)
        {
            var lbl = new Label { Text = label, ForeColor = Color.White, Location = new Point(150, y), AutoSize = true };
            track = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, Location = new Point(300, y - 10), Width = 250 };
            var localTrack = track;

            localTrack.ValueChanged += (s, e) =>
            {
                action(localTrack.Value / 100f);
                SoundManager.SetSettings(_soundSettings);
                _soundSettings.Save();
                System.Diagnostics.Debug.WriteLine(">>> Ползунок изменен! Проверяем ачивку...");
                AchievementManager.OnSoundSettingsChanged();
            };

            _settingsPanel.Controls.Add(lbl);
            _settingsPanel.Controls.Add(localTrack);
        }

        private void SetupAchievementsPanel()
        {
            _achievementsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 20), Visible = false };
            var lblTitle = new Label { Text = "🏆 ДОСТИЖЕНИЯ", Font = new Font("Arial", 24, FontStyle.Bold), ForeColor = Color.Gold, Location = new Point(250, 20), AutoSize = true };
            _achievementsPanel.Controls.Add(lblTitle);

            var pb = new ProgressBar { Name = "pbProgress", Location = new Point(250, 70), Width = 300, Height = 20, Minimum = 0, Maximum = 100 };
            _achievementsPanel.Controls.Add(pb);

            _achievementsList = new FlowLayoutPanel { Location = new Point(50, 110), Size = new Size(700, 350), AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            _achievementsPanel.Controls.Add(_achievementsList);

            var btnBack = CreateBackButton("← НАЗАД", () => { _achievementsPanel.Visible = false; _mainPanel.Visible = true; });
            _achievementsPanel.Controls.Add(btnBack);
            this.Controls.Add(_achievementsPanel);
        }

        private Button CreateBackButton(string text, Action click)
        {
            var btn = new Button { Text = text, Font = new Font("Arial", 14), Location = new Point(275, 480), Size = new Size(250, 40), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => click();
            return btn;
        }

        private void LoadSoundSettings()
        {
            _soundSettings = SoundManager.GetSettings();
            if (_soundSettings == null) return;
            if (_trackMasterVolume != null) _trackMasterVolume.Value = (int)(_soundSettings.MasterVolume * 100);
            if (_trackSfxVolume != null) _trackSfxVolume.Value = (int)(_soundSettings.SfxVolume * 100);
            if (_trackMagnetVolume != null) _trackMagnetVolume.Value = (int)(_soundSettings.MagnetVolume * 100);
            if (_chkMute != null) _chkMute.Checked = _soundSettings.Muted;
        }

        private void StartGame()
        {
            AchievementManager.Initialize();
            this.Hide();
            var game = new Form1();
            game.FormClosed += (s, e) => { this.Show(); this.BringToFront(); RefreshAchievements(); };
            game.Show();
        }

        private void ShowSettings() { _mainPanel.Visible = false; _settingsPanel.Visible = true; LoadSoundSettings(); }
        private void ShowAchievements() { _mainPanel.Visible = false; _achievementsPanel.Visible = true; RefreshAchievements(); }

        private void RefreshAchievements()
        {
            _achievementsList.Controls.Clear();
            var all = AchievementManager.GetAll();
            var progress = all.Count > 0 ? (int)(AchievementManager.GetUnlocked().Count * 100f / all.Count) : 0;
            var pb = _achievementsPanel.Controls.Find("pbProgress", true)[0] as ProgressBar;
            if (pb != null) pb.Value = progress;

            foreach (var a in all)
            {
                var card = new Panel { Size = new Size(660, 60), Margin = new Padding(5), BackColor = a.Unlocked ? Color.FromArgb(40, 80, 40) : Color.FromArgb(40, 40, 40), BorderStyle = BorderStyle.FixedSingle };
                card.Controls.Add(new Label { Text = a.Unlocked ? a.Icon : "", Font = new Font("Arial", 24), Location = new Point(10, 10), AutoSize = true });
                card.Controls.Add(new Label { Text = a.Name, Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = a.Unlocked ? Color.Lime : Color.Gray, Location = new Point(60, 10), AutoSize = true });
                card.Controls.Add(new Label { Text = a.Description, Font = new Font("Arial", 10), ForeColor = Color.LightGray, Location = new Point(60, 30), AutoSize = true });
                _achievementsList.Controls.Add(card);
            }
        }
    }
}