using System;
using System.IO;
using System.Windows.Forms;
using PingPong.Forms;

namespace PingPong
{
    internal static class Program
    {
        public static readonly string AssetsPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent?.FullName + "\\Assets";

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}