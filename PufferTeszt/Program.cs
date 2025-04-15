using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PufferTeszt
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {   
            try
            {
                // Eseménynapló írása
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PufferTeszt());
            }
            catch (Exception ex)
            {
                // Hibakezelés (pl. logolás, hibaüzenet megjelenítése)
                MessageBox.Show($"Hiba az eseménynaplóba íráskor: {ex.Message}");
            }
        }
    }
}
