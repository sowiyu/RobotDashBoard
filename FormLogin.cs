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
            Text = "�α���";
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
            txtId = new MaterialTextBox { Hint = "���̵�", Location = new Point(50, picLogo.Bottom + 30), Width = 200, Font = new Font("���� ���", 11) };
            txtPw = new MaterialTextBox { Hint = "��й�ȣ", Password = true, Location = new Point(50, txtId.Bottom + 15), Width = 200, Font = new Font("���� ���", 11) };
            btnLogin = new MaterialButton { Text = "�α���", Location = new Point(50, txtPw.Bottom + 30), Width = 200, Height = 40, HighEmphasis = true, DialogResult = DialogResult.None };
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
                MessageBox.Show("���̵� �Ǵ� ��й�ȣ�� �ùٸ��� �ʽ��ϴ�.", "�α��� ����", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtId.Clear();
                txtPw.Clear();
                txtId.Focus();
            }
        }
    }
}