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
            // 이제부터 FormMain이 이 프로그램의 진짜 주인입니다.
            Application.Run(new FormMain());
        }
    }
}