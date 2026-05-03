using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MagnitArena.Model
{
    public class Achievement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool Unlocked { get; set; }
        public DateTime UnlockedAt { get; set; }

        public Achievement() { }
        public Achievement(string id, string name, string description, string icon)
        {
            Id = id; Name = name; Description = description; Icon = icon; Unlocked = false;
        }
    }

    public static class AchievementManager
    {
        private static Dictionary<string, Achievement> _achievements = new Dictionary<string, Achievement>();
        private static string _savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MagnitArena", "achievements.json");

        public static event EventHandler<Achievement> AchievementUnlocked;
        public static int LevelsCompleted { get; private set; } = 0;
        public static int MaxMagnetDistance { get; private set; } = 0;

        public static void Initialize()
        {
            if (_achievements.Count > 0) return;
            _achievements = new Dictionary<string, Achievement>();
            _achievements.Add("first_win", new Achievement("first_win", "Первая победа", "Выиграйте первый уровень", "🏆"));
            _achievements.Add("magnet_master", new Achievement("magnet_master", "Магнит-мастер", "Притяните ящик на 4+ клетки за раз", "🧲"));
            _achievements.Add("speedrun", new Achievement("speedrun", "Спидраннер", "Пройдите уровень за <30 сек", "⚡"));
            _achievements.Add("perfectionist", new Achievement("perfectionist", "Перфекционист", "Пройдите ВСЕ 3 уровня", "💎"));
            _achievements.Add("sound_on", new Achievement("sound_on", "Меломан", "Измените настройки звука", "🎵"));
            Load();
        }

        public static void Unlock(string id)
        {
            if (_achievements.TryGetValue(id, out var ach) && !ach.Unlocked)
            {
                ach.Unlocked = true;
                ach.UnlockedAt = DateTime.Now;
                Save();
                if (AchievementUnlocked != null) AchievementUnlocked(null, ach);
            }
        }

        public static bool IsUnlocked(string id) { return _achievements.TryGetValue(id, out var ach) && ach.Unlocked; }
        public static List<Achievement> GetAll() { return _achievements.Values.ToList(); }
        public static List<Achievement> GetUnlocked() { return _achievements.Values.Where(a => a.Unlocked).ToList(); }

        public static int GetProgress()
        {
            var total = _achievements.Count;
            var unlocked = _achievements.Count(a => a.Value.Unlocked);
            return total > 0 ? (int)(unlocked * 100.0 / total) : 0;
        }

        public static void OnLevelWon(int currentLevel, int totalLevels, TimeSpan levelTime)
        {
            if (!IsUnlocked("first_win")) Unlock("first_win");
            if (levelTime.TotalSeconds < 30 && !IsUnlocked("speedrun")) Unlock("speedrun");
            LevelsCompleted++;
            if (LevelsCompleted >= 3 && totalLevels >= 3 && !IsUnlocked("perfectionist")) Unlock("perfectionist");
        }

        public static void OnMagnetUsed(int movedSteps)
        {
            if (movedSteps > MaxMagnetDistance) MaxMagnetDistance = movedSteps;
            if (movedSteps >= 4 && !IsUnlocked("magnet_master")) Unlock("magnet_master");
        }

        public static void OnSoundSettingsChanged()
        {
            if (!IsUnlocked("sound_on")) Unlock("sound_on");
        }

        public static void ResetLevelProgress() { LevelsCompleted = 0; }

        public static void ResetAll()
        {
            foreach (var ach in _achievements.Values) { ach.Unlocked = false; ach.UnlockedAt = DateTime.MinValue; }
            LevelsCompleted = 0; MaxMagnetDistance = 0; Save();
        }

        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(_achievements, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_savePath, json);
            }
            catch { }
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    string json = File.ReadAllText(_savePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, Achievement>>(json);
                    if (loaded != null)
                    {
                        foreach (var kvp in loaded)
                            if (_achievements.ContainsKey(kvp.Key))
                                _achievements[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch { }
        }
    }

    public class SoundSettings
    {
        public float MasterVolume { get; set; } = 1.0f;
        public float SfxVolume { get; set; } = 1.0f;
        public float MagnetVolume { get; set; } = 1.0f;
        public bool Muted { get; set; } = false;

        private static string _savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MagnitArena", "sound.json");

        public static SoundSettings Load()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    string json = File.ReadAllText(_savePath);
                    var s = JsonSerializer.Deserialize<SoundSettings>(json);
                    if (s != null) return s;
                }
            }
            catch { }
            return new SoundSettings();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_savePath, json);
            }
            catch { }
        }
    }
}