// FormMain.cs
using System;
using System.Windows.Forms;

namespace RobotDashboard
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            using (FormLogin loginForm = new FormLogin())
            {
                loginForm.ShowDialog();

                if (loginForm.LoginSuccessful == false)
                {
                    this.Close();
                }
            }
        }
    }
}