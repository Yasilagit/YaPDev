using System;
using System.Windows.Forms;
using Buraq.YaP.Core;


namespace Buraq.Yap.Core
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApplicationSetUp());
        }
    }
}
