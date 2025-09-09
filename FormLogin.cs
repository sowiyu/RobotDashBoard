using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using RobotDashboard.Properties;

#pragma warning disable CS8618 // null ����� �ʵ忡 ���� ��� ��Ȱ��ȭ

namespace RobotDashboard
{
    public partial class FormLogrin : MaterialForm
    {
        public bool LoginSuccessful { get; private set; } = false;
        private MaterialTextBox txtId;
        private MaterialTextBox txtPw;
        private MaterialButton btnLogin;
        private PictureBox picLogo;

        public FormLogrin()
        {
            InitializeComponent();

            // MaterialSkin Manager�� ��ú���� ������ �׸��� ����
            var skinManager = MaterialSkinManager.Instance;
            skinManager.AddFormToManage(this);
            skinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            skinManager.ColorScheme = new ColorScheme(
                Primary.LightBlue400, Primary.LightBlue500,
                Primary.LightBlue100, Accent.LightBlue200,
                TextShade.BLACK
            );

            // [����] �� ��ü�� ������ ������� ����
            this.BackColor = Color.White;

            Text = "�α���";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(400, 550);

            InitializeModernLayout();
        }

        /// <summary>
        /// ��ú��� �����ΰ� ���ϰ� �ִ� �α��� UI�� �����մϴ�.
        /// </summary>
        private void InitializeModernLayout()
        {
            // [����] �߾� �г��� ������ ������� ����
            var pnlCenter = new Panel { BackColor = Color.White, Size = new Size(300, 400), Location = new Point(50, 100) };
            Controls.Add(pnlCenter);

            // �ΰ� �̹���
            picLogo = new PictureBox
            {
                Image = Resources.QuadMedicine_img, // ���ҽ��� �ΰ� �̹��� ���
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(280, 120),
                Location = new Point(10, 10)
            };
            pnlCenter.Controls.Add(picLogo);

            // UI ��Ʈ�� ����
            txtId = new MaterialTextBox
            {
                Hint = "���̵�",
                Location = new Point(50, picLogo.Bottom + 30),
                Width = 200,
                Font = new Font("���� ���", 11)
            };

            txtPw = new MaterialTextBox
            {
                Hint = "��й�ȣ",
                Password = true,
                Location = new Point(50, txtId.Bottom + 15),
                Width = 200,
                Font = new Font("���� ���", 11)
            };

            btnLogin = new MaterialButton
            {
                Text = "�α���",
                Location = new Point(50, txtPw.Bottom + 30),
                Width = 200,
                Height = 40,
                HighEmphasis = true // ��ư ���� ȿ��
            };

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
                var dashboard = new FormDashboard(); // ��ú��� �� ����
                dashboard.Show();
                this.Hide();
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