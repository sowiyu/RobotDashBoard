using System;
using System.Windows.Forms;

namespace RobotDashboard
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new FormDashboard());
            
        }
    }
}