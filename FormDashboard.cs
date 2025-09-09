using MaterialSkin;
using MaterialSkin.Controls;
using EasyModbus;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using log4net;
using System.Diagnostics;
using RobotDashboard.Properties;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS8618

namespace RobotDashboard
{
    public partial class FormDashboard : MaterialForm
    {
        // --- 멤버 변수 선언 ---
        private static readonly ILog log = LogManager.GetLogger(typeof(FormDashboard));
        private ModbusClient? modbusClient;
        private readonly System.Windows.Forms.Timer statusUpdateTimer;
        private readonly System.Windows.Forms.Timer periodicReadTimer;

        // --- UI 컨트롤 변수 선언 ---
        private Panel pnlSidebar;
        private PictureBox picLogo;
        private readonly List<MaterialButton> robotButtons = new List<MaterialButton>();
        private MaterialTextBox txtIp, txtPort;
        private MaterialButton btnConnect, btnDisconnect;
        private Label lblConnectionStatus;
        private MaterialLabel lblResponseTime;
        private MaterialTabControl tabReadWrite;
        private TabPage tabPageRead, tabPageWrite;

        // READ 컨트롤
        private MaterialTabControl tabReadArea;
        private TabPage tabPageCoil, tabPageRegister;
        private MaterialTextBox txtSingleCoilAddr, txtRangeCoilStart, txtRangeCoilEnd;
        private MaterialButton btnReadSingleCoil, btnReadRangeCoil;
        private MaterialLabel lblSingleCoilValue;
        private MaterialTextBox txtCoilSingleInterval, txtCoilRangeInterval;
        private MaterialSwitch switchCoilSinglePeriodic, switchCoilRangePeriodic;
        private MaterialListView listCoilValues;
        private MaterialTextBox txtSingleRegAddr, txtRangeRegStart, txtRangeRegEnd;
        private MaterialButton btnReadSingleReg, btnReadRangeReg;
        private MaterialLabel lblSingleRegValue;
        private MaterialTextBox txtRegisterSingleInterval, txtRegisterRangeInterval;
        private MaterialSwitch switchRegisterSinglePeriodic, switchRegisterRangePeriodic;
        private MaterialListView listRegisterValues;

        // WRITE 컨트롤
        private MaterialTabControl tabWriteArea;
        private TabPage tabPageWriteCoil, tabPageWriteRegister;
        private MaterialTextBox txtWriteCoilAddr;
        private MaterialSwitch switchWriteCoilValue;
        private MaterialButton btnWriteSingleCoil;
        private MaterialTextBox txtWriteRegAddr, txtWriteRegValue;
        private MaterialButton btnWriteSingleRegister;
        private MaterialTextBox txtWriteRangeCoilStart, txtWriteRangeCoilEnd;
        private MaterialSwitch switchWriteRangeCoilValue;
        private MaterialButton btnWriteRangeCoil;
        private MaterialTextBox txtWriteRangeRegStart, txtWriteRangeRegEnd, txtWriteRangeRegValue;
        private MaterialButton btnWriteRangeRegister;

        public FormDashboard()
        {
            InitializeComponent();

            var skinManager = MaterialSkinManager.Instance;
            skinManager.AddFormToManage(this);
            skinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            skinManager.ColorScheme = new ColorScheme(Primary.LightBlue400, Primary.LightBlue500, Primary.LightBlue100, Accent.LightBlue200, TextShade.BLACK);

            Text = "QuadMedicine Robot Dashboard";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 800);

            InitializeModernLayout();

            statusUpdateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            statusUpdateTimer.Tick += StatusUpdateTimer_Tick;

            periodicReadTimer = new System.Windows.Forms.Timer();
            periodicReadTimer.Tick += PeriodicReadTimer_Tick;

            // 대시보드(메인 창)가 닫히면 프로그램 전체 종료
            this.FormClosed += (s, args) => Application.Exit();
            log.Info("Dashboard form initialized and displayed.");
        }

        private void InitializeModernLayout()
        {
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(mainLayout);

            pnlSidebar = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            mainLayout.Controls.Add(pnlSidebar, 0, 0);

            picLogo = new PictureBox { Size = new Size(200, 100), Location = new Point(20, 20), Image = Resources.QuadMedicine_img, SizeMode = PictureBoxSizeMode.StretchImage };
            pnlSidebar.Controls.Add(picLogo);

            var btnShowManual = new MaterialButton { Text = "사용 매뉴얼", Location = new Point(20, picLogo.Bottom + 20), Width = 200 };
            btnShowManual.Click += BtnShowManual_Click;
            pnlSidebar.Controls.Add(btnShowManual);

            var lblExplore = new Label { Text = "탐색하기", Location = new Point(20, btnShowManual.Bottom + 20), Font = new Font("맑은 고딕", 10, FontStyle.Bold), AutoSize = true };
            pnlSidebar.Controls.Add(lblExplore);

            for (int i = 1; i <= 5; i++)
            {
                var robotButton = new MaterialButton { Text = $"Robot {i}", Location = new Point(20, lblExplore.Bottom + 10 + (i - 1) * 45) };
                robotButton.Click += RobotButton_Click;
                pnlSidebar.Controls.Add(robotButton);
                robotButtons.Add(robotButton);
            }

            var lastButtonY = lblExplore.Bottom + 10 + (4 * 45);
            var lblAddressInfo = new MaterialLabel
            {
                Text = "--- 주소 범위 안내 ---\n\n" + "[READ]\n" + "Coil: 0 ~ 65535\n" + "Register: 10000 ~ 11070\n\n" + "[WRITE]\n" + "Coil: 1288 ~ 1300\n" + "Register: 11060 ~ 11070",
                Location = new Point(20, lastButtonY + 60),
                AutoSize = false,
                Size = new Size(200, 180),
                Font = new Font("맑은 고딕", 9)
            };
            pnlSidebar.Controls.Add(lblAddressInfo);

            var pnlMainContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };
            mainLayout.Controls.Add(pnlMainContent, 1, 0);

            SelectRobotButton(robotButtons[0]);

            var cardConnection = new MaterialCard { Location = new Point(10, 40), Width = pnlMainContent.Width - 40, Height = 100, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlMainContent.Controls.Add(cardConnection);

            var connectionTable = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 15, 0), ColumnCount = 5 };
            connectionTable.RowCount = 1;
            connectionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
            connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            cardConnection.Controls.Add(connectionTable);

            var ipPortTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            ipPortTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
            ipPortTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            ipPortTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            connectionTable.Controls.Add(ipPortTable, 0, 0);

            txtIp = new MaterialTextBox { Hint = "Robot IP", Dock = DockStyle.Fill, Text = "192.168.1.60" };
            txtPort = new MaterialTextBox { Hint = "Port", Dock = DockStyle.Fill, Text = "502" };
            ipPortTable.Controls.Add(txtIp, 0, 0);
            ipPortTable.Controls.Add(txtPort, 1, 0);

            btnConnect = new MaterialButton { Text = "연결", Dock = DockStyle.Fill, HighEmphasis = true };
            btnDisconnect = new MaterialButton { Text = "연결 끊기", Dock = DockStyle.Fill };
            lblConnectionStatus = new Label { Text = "DISCONNECTED", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("맑은 고딕", 10, FontStyle.Bold), BackColor = Color.FromArgb(213, 0, 0), ForeColor = Color.White };
            lblResponseTime = new MaterialLabel { Text = "- ms", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("맑은 고딕", 12, FontStyle.Bold) };
            connectionTable.Controls.Add(btnConnect, 1, 0);
            connectionTable.Controls.Add(btnDisconnect, 2, 0);
            connectionTable.Controls.Add(lblConnectionStatus, 3, 0);
            connectionTable.Controls.Add(lblResponseTime, 4, 0);

            var cardReadWrite = new MaterialCard { Location = new Point(10, 160), Width = pnlMainContent.Width - 40, Height = pnlMainContent.Height - 200, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            pnlMainContent.Controls.Add(cardReadWrite);

            var readWriteButtonPanel = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Top, Height = 48 };
            readWriteButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            readWriteButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            cardReadWrite.Controls.Add(readWriteButtonPanel);

            var btnReadTab = new MaterialButton { Text = "READ", Dock = DockStyle.Fill, HighEmphasis = true, Type = MaterialButton.MaterialButtonType.Contained };
            var btnWriteTab = new MaterialButton { Text = "WRITE", Dock = DockStyle.Fill, HighEmphasis = false, Type = MaterialButton.MaterialButtonType.Contained };
            var readWriteTabs = new List<MaterialButton> { btnReadTab, btnWriteTab };
            readWriteButtonPanel.Controls.AddRange(new Control[] { btnReadTab, btnWriteTab });

            tabReadWrite = new MaterialTabControl { Dock = DockStyle.Fill };
            cardReadWrite.Controls.Add(tabReadWrite);
            tabPageRead = new TabPage("READ");
            tabPageWrite = new TabPage("WRITE");
            tabReadWrite.TabPages.AddRange(new TabPage[] { tabPageRead, tabPageWrite });

            btnReadTab.Click += (s, e) => { tabReadWrite.SelectedIndex = 0; readWriteTabs.ForEach(b => b.HighEmphasis = (b == btnReadTab)); };
            btnWriteTab.Click += (s, e) => { tabReadWrite.SelectedIndex = 1; readWriteTabs.ForEach(b => b.HighEmphasis = (b == btnWriteTab)); };
            tabReadWrite.BringToFront();

            InitializeReadTab(tabPageRead);
            InitializeWriteTab(tabPageWrite);

            btnConnect.Click += BtnConnect_Click;
            btnDisconnect.Click += BtnDisconnect_Click;
        }

        private void InitializeReadTab(TabPage parent)
        {
            var pnlReadTabContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            parent.Controls.Add(pnlReadTabContent);

            var coilRegisterButtonPanel = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Top, Height = 48 };
            coilRegisterButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            coilRegisterButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlReadTabContent.Controls.Add(coilRegisterButtonPanel);

            var btnCoilTab = new MaterialButton { Text = "COIL", Dock = DockStyle.Fill, HighEmphasis = true, Type = MaterialButton.MaterialButtonType.Text };
            var btnRegisterTab = new MaterialButton { Text = "REGISTER", Dock = DockStyle.Fill, HighEmphasis = false, Type = MaterialButton.MaterialButtonType.Text };
            var coilRegisterTabs = new List<MaterialButton> { btnCoilTab, btnRegisterTab };
            coilRegisterButtonPanel.Controls.AddRange(new Control[] { btnCoilTab, btnRegisterTab });

            tabReadArea = new MaterialTabControl { Dock = DockStyle.Fill };
            pnlReadTabContent.Controls.Add(tabReadArea);
            tabPageCoil = new TabPage("Coil");
            tabPageRegister = new TabPage("Register");
            tabReadArea.TabPages.AddRange(new TabPage[] { tabPageCoil, tabPageRegister });
            tabReadArea.BringToFront();

            btnCoilTab.Click += (s, e) => { tabReadArea.SelectedIndex = 0; coilRegisterTabs.ForEach(b => b.HighEmphasis = (b == btnCoilTab)); };
            btnRegisterTab.Click += (s, e) => { tabReadArea.SelectedIndex = 1; coilRegisterTabs.ForEach(b => b.HighEmphasis = (b == btnRegisterTab)); };

            InitializeCoilReadTab(tabPageCoil);
            InitializeRegisterReadTab(tabPageRegister);
        }

        private void InitializeWriteTab(TabPage parent)
        {
            var pnlWriteTabContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            parent.Controls.Add(pnlWriteTabContent);

            var coilRegisterButtonPanel = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Top, Height = 48 };
            coilRegisterButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            coilRegisterButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlWriteTabContent.Controls.Add(coilRegisterButtonPanel);

            var btnCoilTab = new MaterialButton { Text = "COIL", Dock = DockStyle.Fill, HighEmphasis = true, Type = MaterialButton.MaterialButtonType.Text };
            var btnRegisterTab = new MaterialButton { Text = "REGISTER", Dock = DockStyle.Fill, HighEmphasis = false, Type = MaterialButton.MaterialButtonType.Text };
            var coilRegisterTabs = new List<MaterialButton> { btnCoilTab, btnRegisterTab };
            coilRegisterButtonPanel.Controls.AddRange(new Control[] { btnCoilTab, btnRegisterTab });

            tabWriteArea = new MaterialTabControl { Dock = DockStyle.Fill };
            pnlWriteTabContent.Controls.Add(tabWriteArea);
            tabPageWriteCoil = new TabPage("Coil");
            tabPageWriteRegister = new TabPage("Register");
            tabWriteArea.TabPages.AddRange(new TabPage[] { tabPageWriteCoil, tabPageWriteRegister });
            tabWriteArea.BringToFront();

            btnCoilTab.Click += (s, e) => { tabWriteArea.SelectedIndex = 0; coilRegisterTabs.ForEach(b => b.HighEmphasis = (b == btnCoilTab)); };
            btnRegisterTab.Click += (s, e) => { tabWriteArea.SelectedIndex = 1; coilRegisterTabs.ForEach(b => b.HighEmphasis = (b == btnRegisterTab)); };

            InitializeCoilWriteTab(tabPageWriteCoil);
            InitializeRegisterWriteTab(tabPageWriteRegister);
        }

        private void InitializeCoilReadTab(TabPage parent)
        {
            var singleReadPanel = new Panel { Dock = DockStyle.Top, Height = 50, Top = 50 };
            var rangeReadPanel = new Panel { Dock = DockStyle.Top, Height = 50, Top = 100 };
            parent.Controls.AddRange(new Control[] { rangeReadPanel, singleReadPanel });
            switchCoilSinglePeriodic = new MaterialSwitch { Location = new Point(110, 8), Text = "OFF" };
            txtCoilSingleInterval = new MaterialTextBox { Hint = "주기(ms)", Text = "1000", Location = new Point(20, 0), Width = 80 };
            txtSingleCoilAddr = new MaterialTextBox { Hint = "단일 주소", Location = new Point(220, 0), Width = 100 };
            btnReadSingleCoil = new MaterialButton { Text = "단일 Coil 읽기", Location = new Point(330, 0), Width = 140 };
            lblSingleCoilValue = new MaterialLabel { Text = "값: -", Location = new Point(480, 10), AutoSize = true };
            singleReadPanel.Controls.AddRange(new Control[] { txtCoilSingleInterval, switchCoilSinglePeriodic, txtSingleCoilAddr, btnReadSingleCoil, lblSingleCoilValue });
            switchCoilRangePeriodic = new MaterialSwitch { Location = new Point(110, 8), Text = "OFF" };
            txtCoilRangeInterval = new MaterialTextBox { Hint = "주기(ms)", Text = "1000", Location = new Point(20, 0), Width = 80 };
            txtRangeCoilStart = new MaterialTextBox { Hint = "시작 주소", Location = new Point(220, 0), Width = 100 };
            txtRangeCoilEnd = new MaterialTextBox { Hint = "끝 주소", Location = new Point(330, 0), Width = 100 };
            btnReadRangeCoil = new MaterialButton { Text = "범위 읽기", Location = new Point(440, 0), Width = 140 };
            rangeReadPanel.Controls.AddRange(new Control[] { txtCoilRangeInterval, switchCoilRangePeriodic, txtRangeCoilStart, txtRangeCoilEnd, btnReadRangeCoil });
            listCoilValues = new MaterialListView { Location = new Point(20, 160), Size = new Size(parent.Width - 40, parent.Height - 180), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, View = View.Details, FullRowSelect = true, Scrollable = true };
            listCoilValues.Columns.AddRange(new ColumnHeader[] { new ColumnHeader { Text = "주소", Width = 150 }, new ColumnHeader { Text = "값", Width = 150 } });
            parent.Controls.Add(listCoilValues);
            switchCoilSinglePeriodic.CheckedChanged += (s, e) => HandlePeriodicReadSwitch(switchCoilSinglePeriodic, txtCoilSingleInterval, new List<MaterialSwitch> { switchCoilRangePeriodic, switchRegisterSinglePeriodic, switchRegisterRangePeriodic });
            switchCoilRangePeriodic.CheckedChanged += (s, e) => HandlePeriodicReadSwitch(switchCoilRangePeriodic, txtCoilRangeInterval, new List<MaterialSwitch> { switchCoilSinglePeriodic, switchRegisterSinglePeriodic, switchRegisterRangePeriodic });
            btnReadSingleCoil.Click += (s, e) => ExecuteReadSingleCoil();
            btnReadRangeCoil.Click += (s, e) => ExecuteReadCoilRange();
        }

        private void InitializeRegisterReadTab(TabPage parent)
        {
            var singleReadPanel = new Panel { Dock = DockStyle.Top, Height = 50, Top = 50 };
            var rangeReadPanel = new Panel { Dock = DockStyle.Top, Height = 50, Top = 100 };
            parent.Controls.AddRange(new Control[] { rangeReadPanel, singleReadPanel });
            switchRegisterSinglePeriodic = new MaterialSwitch { Location = new Point(110, 8), Text = "OFF" };
            txtRegisterSingleInterval = new MaterialTextBox { Hint = "주기(ms)", Text = "1000", Location = new Point(20, 0), Width = 80 };
            txtSingleRegAddr = new MaterialTextBox { Hint = "단일 주소", Location = new Point(220, 0), Width = 100 };
            btnReadSingleReg = new MaterialButton { Text = "단일 Reg 읽기", Location = new Point(330, 0), Width = 140 };
            lblSingleRegValue = new MaterialLabel { Text = "값: -", Location = new Point(480, 10), AutoSize = true };
            singleReadPanel.Controls.AddRange(new Control[] { txtRegisterSingleInterval, switchRegisterSinglePeriodic, txtSingleRegAddr, btnReadSingleReg, lblSingleRegValue });
            switchRegisterRangePeriodic = new MaterialSwitch { Location = new Point(110, 8), Text = "OFF" };
            txtRegisterRangeInterval = new MaterialTextBox { Hint = "주기(ms)", Text = "1000", Location = new Point(20, 0), Width = 80 };
            txtRangeRegStart = new MaterialTextBox { Hint = "시작 주소", Location = new Point(220, 0), Width = 100 };
            txtRangeRegEnd = new MaterialTextBox { Hint = "끝 주소", Location = new Point(330, 0), Width = 100 };
            btnReadRangeReg = new MaterialButton { Text = "범위 읽기", Location = new Point(440, 0), Width = 140 };
            rangeReadPanel.Controls.AddRange(new Control[] { txtRegisterRangeInterval, switchRegisterRangePeriodic, txtRangeRegStart, txtRangeRegEnd, btnReadRangeReg });
            listRegisterValues = new MaterialListView { Location = new Point(20, 160), Size = new Size(parent.Width - 40, parent.Height - 180), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, View = View.Details, FullRowSelect = true, Scrollable = true };
            listRegisterValues.Columns.AddRange(new ColumnHeader[] { new ColumnHeader { Text = "주소", Width = 150 }, new ColumnHeader { Text = "값", Width = 150 } });
            parent.Controls.Add(listRegisterValues);
            switchRegisterSinglePeriodic.CheckedChanged += (s, e) => HandlePeriodicReadSwitch(switchRegisterSinglePeriodic, txtRegisterSingleInterval, new List<MaterialSwitch> { switchCoilSinglePeriodic, switchCoilRangePeriodic, switchRegisterRangePeriodic });
            switchRegisterRangePeriodic.CheckedChanged += (s, e) => HandlePeriodicReadSwitch(switchRegisterRangePeriodic, txtRegisterRangeInterval, new List<MaterialSwitch> { switchCoilSinglePeriodic, switchCoilRangePeriodic, switchRegisterSinglePeriodic });
            btnReadSingleReg.Click += (s, e) => ExecuteReadSingleRegister();
            btnReadRangeReg.Click += (s, e) => ExecuteReadRegisterRange();
        }

        private void InitializeCoilWriteTab(TabPage parent)
        {
            var singleWritePanel = new Panel { Dock = DockStyle.Top, Height = 50, Top = 50 };
            var rangeWritePanel = new Panel { Dock = DockStyle.Top, Height = 50, Top = 100 };
            parent.Controls.AddRange(new Control[] { rangeWritePanel, singleWritePanel });
            txtWriteCoilAddr = new MaterialTextBox { Hint = "Coil 주소", Location = new Point(20, 0), Width = 120 };
            switchWriteCoilValue = new MaterialSwitch { Text = "OFF", Location = new Point(160, 8), Checked = false };
            switchWriteCoilValue.CheckedChanged += (s, e) => switchWriteCoilValue.Text = switchWriteCoilValue.Checked ? "ON" : "OFF";
            btnWriteSingleCoil = new MaterialButton { Text = "단일 Coil 쓰기", Location = new Point(280, 0), Width = 140 };
            singleWritePanel.Controls.AddRange(new Control[] { txtWriteCoilAddr, switchWriteCoilValue, btnWriteSingleCoil });
            btnWriteSingleCoil.Click += (s, e) => ExecuteWriteSingleCoil();
            txtWriteRangeCoilStart = new MaterialTextBox { Hint = "시작 주소", Location = new Point(20, 0), Width = 100 };
            txtWriteRangeCoilEnd = new MaterialTextBox { Hint = "끝 주소", Location = new Point(130, 0), Width = 100 };
            switchWriteRangeCoilValue = new MaterialSwitch { Text = "OFF", Location = new Point(250, 8), Checked = false };
            switchWriteRangeCoilValue.CheckedChanged += (s, e) => switchWriteRangeCoilValue.Text = switchWriteRangeCoilValue.Checked ? "ON" : "OFF";
            btnWriteRangeCoil = new MaterialButton { Text = "범위 쓰기", Location = new Point(370, 0), Width = 140 };
            rangeWritePanel.Controls.AddRange(new Control[] { txtWriteRangeCoilStart, txtWriteRangeCoilEnd, switchWriteRangeCoilValue, btnWriteRangeCoil });
            btnWriteRangeCoil.Click += (s, e) => ExecuteWriteCoilRange();
        }

        private void InitializeRegisterWriteTab(TabPage parent)
        {
            var singleWritePanel = new Panel { Dock = DockStyle.Top, Height = 50, Top = 50 };
            var rangeWritePanel = new Panel { Dock = DockStyle.Top, Height = 50, Top = 100 };
            parent.Controls.AddRange(new Control[] { rangeWritePanel, singleWritePanel });
            txtWriteRegAddr = new MaterialTextBox { Hint = "Register 주소", Location = new Point(20, 0), Width = 120 };
            txtWriteRegValue = new MaterialTextBox { Hint = "쓸 값", Location = new Point(150, 0), Width = 120 };
            btnWriteSingleRegister = new MaterialButton { Text = "단일 Reg 쓰기", Location = new Point(280, 0), Width = 140 };
            singleWritePanel.Controls.AddRange(new Control[] { txtWriteRegAddr, txtWriteRegValue, btnWriteSingleRegister });
            btnWriteSingleRegister.Click += (s, e) => ExecuteWriteSingleRegister();
            txtWriteRangeRegStart = new MaterialTextBox { Hint = "시작 주소", Location = new Point(20, 0), Width = 100 };
            txtWriteRangeRegEnd = new MaterialTextBox { Hint = "끝 주소", Location = new Point(130, 0), Width = 100 };
            txtWriteRangeRegValue = new MaterialTextBox { Hint = "쓸 값", Location = new Point(240, 0), Width = 120 };
            btnWriteRangeRegister = new MaterialButton { Text = "범위 쓰기", Location = new Point(370, 0), Width = 140 };
            rangeWritePanel.Controls.AddRange(new Control[] { txtWriteRangeRegStart, txtWriteRangeRegEnd, txtWriteRangeRegValue, btnWriteRangeRegister });
            btnWriteRangeRegister.Click += (s, e) => ExecuteWriteRegisterRange();
        }

        private void BtnShowManual_Click(object? sender, EventArgs e)
        {
            var manualForm = new Form
            {
                Text = "사용 매뉴얼",
                Size = new Size(450, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var manualText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("맑은 고딕", 10),
                BorderStyle = BorderStyle.None,
                Text = "1. 사용자 PC의 IP를 Modbus 번역기와 동일한 대역으로 맞춰주세요.\n\n" +
                       "2. Robot IP, Port 입력 후 '연결' 버튼을 클릭하세요. 'CONNECTED'가 표시되면 정상 접속된 것입니다.\n\n" +
                       "3. 각 READ와 WRITE 기능은 단일/범위 읽기 및 쓰기를 지원합니다. 사이드바의 '주소 범위 안내'를 참고하여 입력해주세요.\n\n" +
                       "4. 사용 완료 후에는 반드시 '연결 끊기'를 눌러주세요."
            };
            manualForm.Controls.Add(manualText);
            manualForm.ShowDialog(this);
        }

        private void StatusUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (modbusClient == null || !modbusClient.Connected)
            {
                UpdateConnectionStatus(false);
                return;
            }

            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                // async/await 제거
                modbusClient.ReadHoldingRegisters(10000, 1);
                stopwatch.Stop();
                lblResponseTime.Text = $"{stopwatch.ElapsedMilliseconds} ms";
            }
            catch (Exception)
            {
                log.Warn("Connection lost during status check.");
                UpdateConnectionStatus(false);
            }
        }

        private void PeriodicReadTimer_Tick(object? sender, EventArgs e)
        {
            if (switchCoilSinglePeriodic.Checked) ExecuteReadSingleCoil(true);
            else if (switchCoilRangePeriodic.Checked) ExecuteReadCoilRange(true);
            else if (switchRegisterSinglePeriodic.Checked) ExecuteReadSingleRegister(true);
            else if (switchRegisterRangePeriodic.Checked) ExecuteReadRegisterRange(true);
        }

        private void HandlePeriodicReadSwitch(MaterialSwitch activeSwitch, MaterialTextBox intervalBox, List<MaterialSwitch> otherSwitches)
        {
            if (activeSwitch.Checked)
            {
                if (!IsConnected()) { activeSwitch.Checked = false; return; }
                otherSwitches.ForEach(s => { if (s.Checked) s.Checked = false; });
                if (!int.TryParse(intervalBox.Text, out int interval) || interval < 50)
                {
                    MessageBox.Show("주기는 50ms 이상의 숫자여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    activeSwitch.Checked = false;
                    return;
                }
                periodicReadTimer.Interval = interval;
                periodicReadTimer.Start();
                activeSwitch.Text = "ON";
                log.Info($"Periodic reading started with interval {interval}ms.");
            }
            else
            {
                periodicReadTimer.Stop();
                activeSwitch.Text = "OFF";
                log.Info("Periodic reading stopped.");
            }
        }

        private void RobotButton_Click(object? sender, EventArgs e)
        {
            if (sender is MaterialButton b) SelectRobotButton(b);
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            var stopwatch = new Stopwatch();
            try
            {
                log.Info($"Attempting to connect to {txtIp.Text}:{txtPort.Text}");
                modbusClient = new ModbusClient(txtIp.Text, int.Parse(txtPort.Text));
                stopwatch.Start();
                modbusClient.Connect();
                stopwatch.Stop();
                UpdateConnectionStatus(true);
                lblResponseTime.Text = $"{stopwatch.ElapsedMilliseconds} ms";
                MessageBox.Show("연결 성공!", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                log.Info($"Successfully connected. Response time: {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus(false);
                MessageBox.Show($"연결 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error($"Connection failed: {ex.Message}", ex);
            }
        }

        private void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            if (modbusClient != null && modbusClient.Connected)
            {
                modbusClient.Disconnect();
                log.Info("Disconnected from the robot.");
            }
            UpdateConnectionStatus(false);
        }

        private void SelectRobotButton(MaterialButton selectedButton)
        {
            foreach (var button in robotButtons)
            {
                button.HighEmphasis = (button == selectedButton);
            }
            log.Info($"Selected {selectedButton.Text}");
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                lblConnectionStatus.Text = "CONNECTED";
                lblConnectionStatus.BackColor = Color.FromArgb(0, 200, 83);
                statusUpdateTimer.Start();
            }
            else
            {
                lblConnectionStatus.Text = "DISCONNECTED";
                lblConnectionStatus.BackColor = Color.FromArgb(213, 0, 0);
                lblResponseTime.Text = "- ms";
                statusUpdateTimer.Stop();
                if (periodicReadTimer.Enabled)
                {
                    periodicReadTimer.Stop();
                    switchCoilSinglePeriodic.Checked = false;
                    switchCoilRangePeriodic.Checked = false;
                    switchRegisterSinglePeriodic.Checked = false;
                    switchRegisterRangePeriodic.Checked = false;
                }
            }
        }

        private bool IsConnected()
        {
            if (modbusClient == null || !modbusClient.Connected)
            {
                if (!periodicReadTimer.Enabled) MessageBox.Show("먼저 로봇에 연결해주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void ExecuteReadSingleCoil(bool isPeriodic = false)
        {
            if (!IsConnected()) return;
            if (!int.TryParse(txtSingleCoilAddr.Text, out int addr) || addr < 0 || addr > 65535)
            {
                if (!isPeriodic) MessageBox.Show("Coil 주소는 0에서 65535 사이의 숫자여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                // async/await 제거
                bool[] coil = modbusClient.ReadCoils(addr, 1);
                stopwatch.Stop();
                lblSingleCoilValue.Text = $"값: {coil[0]}";
                lblResponseTime.Text = $"{stopwatch.ElapsedMilliseconds} ms";
                if (!isPeriodic) log.Info($"Read single coil at address {addr}.");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                lblResponseTime.Text = "ERROR";
                if (!isPeriodic) { MessageBox.Show($"읽기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); log.Error($"Failed to read single coil at address {addr}: {ex.Message}", ex); }
                else { log.Warn($"Periodic single coil read failed: {ex.Message}"); UpdateConnectionStatus(false); }
            }
        }

        private void ExecuteReadCoilRange(bool isPeriodic = false)
        {
            if (!IsConnected()) return;
            if (!int.TryParse(txtRangeCoilStart.Text, out int start) || !int.TryParse(txtRangeCoilEnd.Text, out int end) || start < 0 || end > 65535 || start > end)
            {
                if (!isPeriodic) MessageBox.Show("Coil 주소 범위가 올바르지 않습니다. (0~65535, 시작 <= 끝)", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var stopwatch = new Stopwatch();
            try
            {
                int len = end - start + 1;
                stopwatch.Start();
                // async/await 제거
                bool[] coils = modbusClient.ReadCoils(start, len);
                stopwatch.Stop();
                listCoilValues.Items.Clear();
                for (int i = 0; i < coils.Length; i++) { listCoilValues.Items.Add(new ListViewItem(new[] { (start + i).ToString(), coils[i].ToString() })); }
                lblResponseTime.Text = $"{stopwatch.ElapsedMilliseconds} ms";
                if (!isPeriodic) log.Info($"Read coil range from {start} to {end}.");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                lblResponseTime.Text = "ERROR";
                if (!isPeriodic) { MessageBox.Show($"범위 읽기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); log.Error($"Failed to read coil range from {start} to {end}: {ex.Message}", ex); }
                else { log.Warn($"Periodic coil range read failed: {ex.Message}"); UpdateConnectionStatus(false); }
            }
        }

        private void ExecuteReadSingleRegister(bool isPeriodic = false)
        {
            if (!IsConnected()) return;
            if (!int.TryParse(txtSingleRegAddr.Text, out int addr) || addr < 10000 || addr > 11070)
            {
                if (!isPeriodic) MessageBox.Show("Register 주소는 10000에서 11070 사이의 숫자여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                // async/await 제거
                int[] reg = modbusClient.ReadHoldingRegisters(addr, 1);
                stopwatch.Stop();
                lblSingleRegValue.Text = $"값: {reg[0]}";
                lblResponseTime.Text = $"{stopwatch.ElapsedMilliseconds} ms";
                if (!isPeriodic) log.Info($"Read single register at address {addr}.");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                lblResponseTime.Text = "ERROR";
                if (!isPeriodic) { MessageBox.Show($"읽기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); log.Error($"Failed to read single register at address {addr}: {ex.Message}", ex); }
                else { log.Warn($"Periodic single register read failed: {ex.Message}"); UpdateConnectionStatus(false); }
            }
        }

        private void ExecuteReadRegisterRange(bool isPeriodic = false)
        {
            if (!IsConnected()) return;
            if (!int.TryParse(txtRangeRegStart.Text, out int start) || !int.TryParse(txtRangeRegEnd.Text, out int end) || start < 10000 || end > 11070 || start > end)
            {
                if (!isPeriodic) MessageBox.Show("Register 주소 범위가 올바르지 않습니다. (10000~11070, 시작 <= 끝)", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var stopwatch = new Stopwatch();
            try
            {
                int len = end - start + 1;
                stopwatch.Start();
                // async/await 제거
                int[] regs = modbusClient.ReadHoldingRegisters(start, len);
                stopwatch.Stop();
                listRegisterValues.Items.Clear();
                for (int i = 0; i < regs.Length; i++) { listRegisterValues.Items.Add(new ListViewItem(new[] { (start + i).ToString(), regs[i].ToString() })); }
                lblResponseTime.Text = $"{stopwatch.ElapsedMilliseconds} ms";
                if (!isPeriodic) log.Info($"Read register range from {start} to {end}.");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                lblResponseTime.Text = "ERROR";
                if (!isPeriodic) { MessageBox.Show($"범위 읽기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); log.Error($"Failed to read register range from {start} to {end}: {ex.Message}", ex); }
                else { log.Warn($"Periodic register range read failed: {ex.Message}"); UpdateConnectionStatus(false); }
            }
        }

        private void ExecuteWriteSingleCoil()
        {
            if (!IsConnected()) return;
            if (!int.TryParse(txtWriteCoilAddr.Text, out int addr) || addr < 1288 || addr > 1300) { MessageBox.Show("쓰기 가능한 Coil 주소는 1288에서 1300 사이여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            bool valueToWrite = switchWriteCoilValue.Checked;
            try
            {
                // async/await 제거
                modbusClient.WriteSingleCoil(addr, valueToWrite);
                string logMsg = $"Successfully wrote single coil at address {addr}. Value: {valueToWrite}";
                log.Info(logMsg);
                MessageBox.Show(logMsg, "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쓰기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error($"Failed to write single coil at address {addr}: {ex.Message}", ex);
            }
        }

        private void ExecuteWriteSingleRegister()
        {
            if (!IsConnected()) return;
            if (!int.TryParse(txtWriteRegAddr.Text, out int addr) || addr < 11060 || addr > 11070) { MessageBox.Show("쓰기 가능한 Register 주소는 11060에서 11070 사이여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (!int.TryParse(txtWriteRegValue.Text, out int valueToWrite)) { MessageBox.Show("Register 값은 유효한 숫자여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            try
            {
                // async/await 제거
                modbusClient.WriteSingleRegister(addr, valueToWrite);
                string logMsg = $"Successfully wrote single register at address {addr}. Value: {valueToWrite}";
                log.Info(logMsg);
                MessageBox.Show(logMsg, "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쓰기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error($"Failed to write single register at address {addr}: {ex.Message}", ex);
            }
        }

        private void ExecuteWriteCoilRange()
        {
            if (!IsConnected()) return;
            if (!int.TryParse(txtWriteRangeCoilStart.Text, out int start) || !int.TryParse(txtWriteRangeCoilEnd.Text, out int end) || start < 1288 || end > 1300 || start > end) { MessageBox.Show("쓰기 가능한 Coil 주소 범위가 올바르지 않습니다. (1288~1300, 시작 <= 끝)", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            bool valueToWrite = switchWriteRangeCoilValue.Checked;
            int quantity = end - start + 1;
            bool[] values = Enumerable.Repeat(valueToWrite, quantity).ToArray();
            try
            {
                // async/await 제거
                modbusClient.WriteMultipleCoils(start, values);
                string logMsg = $"Successfully wrote coil range from {start} to {end}. Value: {valueToWrite}";
                log.Info(logMsg);
                MessageBox.Show(logMsg, "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"범위 쓰기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error($"Failed to write coil range from {start} to {end}: {ex.Message}", ex);
            }
        }

        private void ExecuteWriteRegisterRange()
        {
            if (!IsConnected()) return;
            if (!int.TryParse(txtWriteRangeRegStart.Text, out int start) || !int.TryParse(txtWriteRangeRegEnd.Text, out int end) || start < 11060 || end > 11070 || start > end) { MessageBox.Show("쓰기 가능한 Register 주소 범위가 올바르지 않습니다. (11060~11070, 시작 <= 끝)", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (!int.TryParse(txtWriteRangeRegValue.Text, out int valueToWrite)) { MessageBox.Show("Register 값은 유효한 숫자여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            int quantity = end - start + 1;
            int[] values = Enumerable.Repeat(valueToWrite, quantity).ToArray();
            try
            {
                // async/await 제거
                modbusClient.WriteMultipleRegisters(start, values);
                string logMsg = $"Successfully wrote register range from {start} to {end}. Value: {valueToWrite}";
                log.Info(logMsg);
                MessageBox.Show(logMsg, "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"범위 쓰기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error($"Failed to write register range from {start} to {end}: {ex.Message}", ex);
            }
        }
    }
}