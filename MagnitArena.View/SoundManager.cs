using System;
using System.IO;
using System.Media;
using MagnitArena.Model;

namespace MagnitArena.View
{
    public static class SoundManager
    {
        private static SoundPlayer _step1, _hit, _win, _lose, _magnet;
        private static System.Timers.Timer _magnetTimer;
        private static bool _magnetPlaying = false;
        private static SoundSettings _settings;

        public static void Initialize()
        {
            _settings = SoundSettings.Load();
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
            if (Directory.Exists(p))
            {
                Load("step1.wav", ref _step1, p); Load("hit.wav", ref _hit, p);
                Load("win.wav", ref _win, p); Load("lose.wav", ref _lose, p);
                Load("magnet.wav", ref _magnet, p);
            }
            _magnetTimer = new System.Timers.Timer(2000) { AutoReset = false };
            _magnetTimer.Elapsed += (s, e) => StopMagnet();
        }
        private static void Load(string f, ref SoundPlayer sp, string d) { string fp = Path.Combine(d, f); if (File.Exists(fp)) { sp = new SoundPlayer(fp); sp.Load(); } }

        public static void SetSettings(SoundSettings s) { _settings = s; _settings.Save(); }
        public static SoundSettings GetSettings() { if (_settings == null) _settings = SoundSettings.Load(); return _settings; }

        public static void PlayStep1() { if (_settings != null && _settings.Muted != true && _step1 != null) _step1.Play(); }
        public static void PlayHit() { if (_settings != null && _settings.Muted != true && _hit != null) _hit.Play(); }
        public static void PlayWin() { if (_settings != null && _settings.Muted != true && _win != null) _win.Play(); }
        public static void PlayLose() { if (_settings != null && _settings.Muted != true && _lose != null) _lose.Play(); }
        public static void StartMagnet() { if (_settings != null && _settings.Muted != true && _magnet != null && !_magnetPlaying) { _magnet.Play(); _magnetPlaying = true; _magnetTimer.Stop(); _magnetTimer.Start(); } }
        public static void StopMagnet() { if (_magnet != null && _magnetPlaying) { _magnet.Stop(); _magnetPlaying = false; _magnetTimer.Stop(); } }
        public static void StopAllStepSounds() { }
    }
}