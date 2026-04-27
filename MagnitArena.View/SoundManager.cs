using System;
using System.IO;
using System.Media;
using System.Timers;
using System.Diagnostics;

namespace MagnitArena.View
{
    public static class SoundManager
    {
        private static SoundPlayer _step1Sound;
        private static SoundPlayer _hitSound;
        private static SoundPlayer _winSound;
        private static SoundPlayer _loseSound;
        private static SoundPlayer _magnetSound;

        private static Timer _magnetTimer;
        private static bool _magnetPlaying = false;

        public static void Initialize()
        {
            string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");

            if (Directory.Exists(soundPath))
            {
                LoadSound("step1.wav", ref _step1Sound);
                LoadSound("hit.wav", ref _hitSound);
                LoadSound("win.wav", ref _winSound);
                LoadSound("lose.wav", ref _loseSound);
                LoadSound("magnet.wav", ref _magnetSound);
            }

            _magnetTimer = new Timer(2000);
            _magnetTimer.AutoReset = false;
            _magnetTimer.Elapsed += (s, e) => StopMagnet();
        }

        private static void LoadSound(string fileName, ref SoundPlayer player)
        {
            string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", fileName);
            if (File.Exists(soundPath))
            {
                player = new SoundPlayer(soundPath);
                player.Load();
            }
        }

        public static void PlayStep1()
        {
            if (_step1Sound != null)
            {
                _step1Sound.Stop();
                _step1Sound.Play();
            }
        }

        public static void StartMagnet()
        {
            if (_magnetSound != null && !_magnetPlaying)
            {
                _magnetSound.Play();
                _magnetPlaying = true;
                _magnetTimer.Stop();
                _magnetTimer.Start();
            }
        }

        public static void StopMagnet()
        {
            if (_magnetSound != null && _magnetPlaying)
            {
                _magnetSound.Stop();
                _magnetPlaying = false;
                _magnetTimer.Stop();
            }
        }

        public static void StopAllStepSounds()
        {
        }

        public static void PlayHit() => _hitSound?.Play();
        public static void PlayWin() => _winSound?.Play();
        public static void PlayLose() => _loseSound?.Play();
    }
}