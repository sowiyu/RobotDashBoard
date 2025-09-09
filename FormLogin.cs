using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using RobotDashboard.Properties;

#pragma warning disable CS8618

namespace RobotDashboard
{
    public partial class FormLogin : MaterialForm
    {
        public bool LoginSuccessful { get; private set; } = false;
        private MaterialTextBox txtId;
        private MaterialTextBox txtPw;
        private MaterialButton btnLogin;
        private PictureBox picLogo;

        public FormLogin()
        {
            InitializeComponent();
    
            this.BackColor = Color.White;
            Text = "로그인";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(400, 550);
            InitializeModernLayout();
        }

        private void InitializeModernLayout()
        {
            var pnlCenter = new Panel { BackColor = Color.White, Size = new Size(300, 400), Location = new Point(50, 100) };
            Controls.Add(pnlCenter);
            picLogo = new PictureBox { Image = Resources.QuadMedicine_img, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(280, 120), Location = new Point(10, 10) };
            pnlCenter.Controls.Add(picLogo);
            txtId = new MaterialTextBox { Hint = "아이디", Location = new Point(50, picLogo.Bottom + 30), Width = 200, Font = new Font("맑은 고딕", 11) };
            txtPw = new MaterialTextBox { Hint = "비밀번호", Password = true, Location = new Point(50, txtId.Bottom + 15), Width = 200, Font = new Font("맑은 고딕", 11) };
            btnLogin = new MaterialButton { Text = "로그인", Location = new Point(50, txtPw.Bottom + 30), Width = 200, Height = 40, HighEmphasis = true, DialogResult = DialogResult.None };
            pnlCenter.Controls.Add(txtId);
            pnlCenter.Controls.Add(txtPw);
            pnlCenter.Controls.Add(btnLogin);
            btnLogin.Click += BtnLogin_Click;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string id = txtId.Text;
            string pw = txtPw.Text;

            if (id == "admin" && pw == "1234")
            {
                this.LoginSuccessful = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("아이디 또는 비밀번호가 올바르지 않습니다.", "로그인 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtId.Clear();
                txtPw.Clear();
                txtId.Focus();
            }
        }
    }
}