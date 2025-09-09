using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using RobotDashboard.Properties;

#pragma warning disable CS8618 // null 비허용 필드에 대한 경고 비활성화

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

            // MaterialSkin Manager를 대시보드와 동일한 테마로 설정
            var skinManager = MaterialSkinManager.Instance;
            skinManager.AddFormToManage(this);
            skinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            skinManager.ColorScheme = new ColorScheme(
                Primary.LightBlue400, Primary.LightBlue500,
                Primary.LightBlue100, Accent.LightBlue200,
                TextShade.BLACK
            );

            // [수정] 폼 자체의 배경색을 흰색으로 설정
            this.BackColor = Color.White;

            Text = "로그인";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(400, 550);

            InitializeModernLayout();
        }

        /// <summary>
        /// 대시보드 디자인과 통일감 있는 로그인 UI를 구성합니다.
        /// </summary>
        private void InitializeModernLayout()
        {
            // [수정] 중앙 패널의 배경색을 흰색으로 설정
            var pnlCenter = new Panel { BackColor = Color.White, Size = new Size(300, 400), Location = new Point(50, 100) };
            Controls.Add(pnlCenter);

            // 로고 이미지
            picLogo = new PictureBox
            {
                Image = Resources.QuadMedicine_img, // 리소스의 로고 이미지 사용
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(280, 120),
                Location = new Point(10, 10)
            };
            pnlCenter.Controls.Add(picLogo);

            // UI 컨트롤 구성
            txtId = new MaterialTextBox
            {
                Hint = "아이디",
                Location = new Point(50, picLogo.Bottom + 30),
                Width = 200,
                Font = new Font("맑은 고딕", 11)
            };

            txtPw = new MaterialTextBox
            {
                Hint = "비밀번호",
                Password = true,
                Location = new Point(50, txtId.Bottom + 15),
                Width = 200,
                Font = new Font("맑은 고딕", 11)
            };

            btnLogin = new MaterialButton
            {
                Text = "로그인",
                Location = new Point(50, txtPw.Bottom + 30),
                Width = 200,
                Height = 40,
                HighEmphasis = true // 버튼 강조 효과
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
                var dashboard = new FormDashboard(); // 대시보드 폼 열기
                dashboard.Show();
                this.Hide();
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