using System;
using System.Windows.Forms;

namespace MagnitArena.View
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mainMenu = new MainMenu();
            Form1 gameForm = null;

            mainMenu.StartClicked += (s, e) =>
            {
                if (gameForm == null || gameForm.IsDisposed)
                {
                    gameForm = new Form1();
                }

                gameForm.RestartGame();
                mainMenu.Hide();
                gameForm.Show();
                gameForm.BringToFront();

                gameForm.BackToMenu += (s2, e2) =>
                {
                    gameForm.Hide();
                    mainMenu.Show();
                    mainMenu.BringToFront();
                };
            };

            Application.Run(mainMenu);
        }
    }
}