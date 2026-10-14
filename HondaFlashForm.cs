using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RemixHondaFlash
{
    public class MainForm : Form
    {
        // Theme Colors (Modern Automotive Dark Studio)
        private static readonly Color BgDark = Color.FromArgb(16, 20, 29);
        private static readonly Color CardBg = Color.FromArgb(25, 34, 49);
        private static readonly Color CardHeaderBg = Color.FromArgb(32, 43, 62);
        private static readonly Color AccentBlue = Color.FromArgb(0, 145, 255);
        private static readonly Color AccentGreen = Color.FromArgb(0, 230, 118);
        private static readonly Color AccentPurple = Color.FromArgb(179, 136, 255);
        private static readonly Color AccentRed = Color.FromArgb(255, 82, 82);
        private static readonly Color TextWhite = Color.FromArgb(240, 246, 252);
        private static readonly Color TextMuted = Color.FromArgb(144, 164, 174);
        private static readonly Color BorderColor = Color.FromArgb(44, 57, 75);
        private static readonly Color AccentCyan = Color.FromArgb(0, 229, 255);
        private static readonly Color TextPrimary = Color.FromArgb(240, 246, 252);
        private static readonly Color TextSecondary = Color.FromArgb(144, 164, 174);
        private static readonly Color InputBg = Color.FromArgb(16, 22, 32);

        // Header & Navigation Sidebar Controls
        private Panel pnlSidebar;
        private Panel pnlNavMenu;
        private Panel pnlSidebarStatus;
        private Panel pnlSidebarUsbLed;
        private Label lblSidebarUsbStatus;
        private Panel pnlSidebarEcmLed;
        private Label lblSidebarEcmStatus;
        private Label lblHeaderSectionTitle;
        private Label lblAppTitle;
        private Label lblAppSubtitle;

        // Tabs
        private TabControl tabControl;
        private TabPage tabFlasher;
        private TabPage tabLiveMonitor;
        private TabPage tabQuickTune;
        private TabPage tabDiag;
        private TabPage tabTools;
        private TabPage tabDatabase;
        private List<Button> navButtons = new List<Button>();

        // Flasher Tab Controls
        private ComboBox cbPort;
        private Button btnRefreshPorts;
        private Button btnConnect;
        private Panel pnlUsbIndicator;
        private Label lblUsbStatus;
        private Panel pnlEcmIndicator;
        private Label lblEcmStatus;
        private System.Windows.Forms.Timer tmrUsbDetector;
        private string lastHwSig = "";

        private Label lblEcmIdVal;
        private Label lblPartCodeVal;
        private Label lblModelNameVal;
        private Label lblMcuTypeVal;
        private Label lblFlashCountVal;

        private TextBox txtBinPath;
        private Button btnBrowseBin;
        private Label lblFileSize;
        private TextBox txtStartOffset;
        private TextBox txtChecksumOffset;
        private CheckBox chkFastMode;
        private CheckBox chkManualOffset;

        private Button btnReadFlash;
        private Button btnWriteFlash;
        private ProgressBar progressBar;
        private Label lblProgressText;
        private Label lblSpeedText;
        private TextBox txtFlasherLog;

        // Live Monitor Tab Controls
        private Label lblRpmVal;
        private Label lblTpsVal;
        private Label lblEotVal;
        private Label lblMapVal;
        private Label lblIatVal;
        private Label lblBattVal;
        private Label lblInjVal;
        private Label lblAdvVal;
        private Label lblAfrVal;
        private Button btnStartMonitor;
        private Button btnPauseMonitor;
        private Button btnRecordCsv;
        private Label lblRecordStatus;
        private Panel pnlWaveform;
        private System.Windows.Forms.Timer tmrTelemetry;
        private List<int> rpmHistory = new List<int>();
        private List<int> tpsHistory = new List<int>();
        private TelemetryDataLogger dataLogger = new TelemetryDataLogger();
        private bool isTelemetryRunning = false;

        // Quick Tune Tab Controls
        private Label lblQuickRomInfo;
        private ComboBox cbIdleChopLevel;
        private Button btnApplyIdleChop;
        private Label lblIdleChopResult;
        private ComboBox cbRevLimit;
        private Button btnApplyRevLimit;
        private Label lblRevLimitResult;
        private ComboBox cbStageTune;
        private Button btnApplyStageTune;
        private Label lblStageTuneResult;

        // Diagnostic Tab Controls
        private Button btnPreFlashCheck;
        private TextBox txtPreFlashStatus;
        private Button btnReadDtc;
        private Button btnClearDtc;
        private Button btnResetTp;
        private Button btnResetEcm;
        private Button btnResetFlashCount;
        private Button btnTestInjector;
        private Button btnTestFuelPump;
        private Button btnTestTacho;
        private Button btnTestSpark;
        private Label lblActuatorResult;
        private TextBox txtLog;

        // Map Editor Tab Controls
        private TabPage tabMapEditor;
        private MapTuningSession mapSession = new MapTuningSession();
        private TableGridControl gridEditor;
        private Surface3DGraphControl graph3D;
        private TreeView tvMapTables;
        private TextBox txtSearchTable;
        private ComboBox cmbMapModelPreset;
        private Button btnLoadBinMap;
        private Button btnLoadXdfMap;
        private Button btnSaveBinMap;
        private Button btnFlashDirect;
        private Label lblMapBinStatus;
        private Label lblActiveTableTitle;
        private Label lblSelectionStats;
        private Button btnView2D;
        private Button btnView3D;
        private Button btnViewDiff;

        // Tools Tab Controls
        private Button btnUnlockXdf;
        private Button btnConvertFile;
        private Button btnFixChecksumFile;

        // Database Tab Controls
        private TextBox txtDbSearch;
        private ListView lvDatabase;

        // State
        private byte[] loadedBin = null;
        private EcuType detectedEcuType = EcuType.Unknown;
        private IKLinePort activePort = null;
        private HondaKLineEngine activeEngine = null;

        public MainForm()
        {
            InitializeComponent();
            RefreshPortList();
            UpdateHardwareStatus(false);
            PopulateDatabaseView("");

            tmrUsbDetector = new System.Windows.Forms.Timer();
            tmrUsbDetector.Interval = 1200;
            tmrUsbDetector.Tick += (s, e) => UpdateHardwareStatus(true);
            tmrUsbDetector.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "JOSAVE TUNER - Professional Honda ECU Studio (2D/3D Tuning & K-Line Flasher)";
            this.Size = new Size(1260, 820);
            this.MinimumSize = new Size(1024, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgDark;
            this.ForeColor = TextWhite;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.DoubleBuffered = true;

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
                else
                {
                    this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }

            // 1. Modern Motorsport Left Navigation Sidebar (Width = 230, Dock = Left)
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 230,
                BackColor = Color.FromArgb(12, 16, 25),
                Padding = new Padding(0)
            };
            this.Controls.Add(pnlSidebar);

            // 1.1 Branding Header at Top of Sidebar
            Panel pnlBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(9, 13, 20),
                Padding = new Padding(12, 12, 12, 8)
            };
            pnlSidebar.Controls.Add(pnlBrand);

            PictureBox pbLogo = new PictureBox
            {
                Location = new Point(12, 14),
                Size = new Size(42, 42),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try
            {
                string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.png");
                if (File.Exists(pngPath))
                {
                    pbLogo.Image = Image.FromFile(pngPath);
                }
            }
            catch { }
            pnlBrand.Controls.Add(pbLogo);

            lblAppTitle = new Label
            {
                Text = "JOSAVE TUNER",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = AccentCyan,
                Location = new Point(58, 13),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblAppTitle);

            lblAppSubtitle = new Label
            {
                Text = "HONDA RACING STUDIO",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AccentGreen,
                Location = new Point(60, 36),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblAppSubtitle);

            Label lblVerTag = new Label
            {
                Text = "K-LINE PRO • 265+ MODELS",
                Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                ForeColor = TextMuted,
                Location = new Point(60, 52),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblVerTag);

            pnlBrand.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(28, 38, 54), 1))
                {
                    e.Graphics.DrawLine(borderPen, 0, pnlBrand.Height - 1, pnlBrand.Width, pnlBrand.Height - 1);
                }
            };

            // 1.2 Sidebar Bottom Status Card (Dock = Bottom)
            pnlSidebarStatus = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = Color.FromArgb(9, 13, 20),
                Padding = new Padding(12, 8, 12, 8)
            };
            pnlSidebar.Controls.Add(pnlSidebarStatus);

            pnlSidebarStatus.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(28, 38, 54), 1))
                {
                    e.Graphics.DrawLine(borderPen, 0, 0, pnlSidebarStatus.Width, 0);
                }
            };

            pnlSidebarUsbLed = new Panel { Location = new Point(12, 16), Size = new Size(10, 10), BackColor = AccentRed };
            pnlSidebarUsbLed.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Brush b = new SolidBrush(pnlSidebarUsbLed.BackColor))
                {
                    e.Graphics.FillEllipse(b, 0, 0, 9, 9);
                }
            };
            pnlSidebarStatus.Controls.Add(pnlSidebarUsbLed);

            lblSidebarUsbStatus = new Label
            {
                Text = "K-Line: ตรวจหาสาย...",
                Location = new Point(28, 13),
                Size = new Size(185, 18),
                ForeColor = AccentRed,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            pnlSidebarStatus.Controls.Add(lblSidebarUsbStatus);

            pnlSidebarEcmLed = new Panel { Location = new Point(12, 38), Size = new Size(10, 10), BackColor = Color.FromArgb(120, 120, 120) };
            pnlSidebarEcmLed.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Brush b = new SolidBrush(pnlSidebarEcmLed.BackColor))
                {
                    e.Graphics.FillEllipse(b, 0, 0, 9, 9);
                }
            };
            pnlSidebarStatus.Controls.Add(pnlSidebarEcmLed);

            lblSidebarEcmStatus = new Label
            {
                Text = "ECM: สแตนด์บาย",
                Location = new Point(28, 35),
                Size = new Size(185, 18),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            pnlSidebarStatus.Controls.Add(lblSidebarEcmStatus);

            Label lblSidebarTag = new Label
            {
                Text = "[AUTO CHECKSUM] [FAST 62.5K]",
                Location = new Point(12, 65),
                Size = new Size(205, 18),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold)
            };
            pnlSidebarStatus.Controls.Add(lblSidebarTag);

            // 1.3 Navigation Rail Menu (Dock = Fill)
            pnlNavMenu = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 16, 25),
                Padding = new Padding(6, 10, 6, 10),
                AutoScroll = true
            };
            pnlSidebar.Controls.Add(pnlNavMenu);
            pnlNavMenu.BringToFront();

            // 2. Right Main Work Content Container (Dock = Fill)
            Panel pnlMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark
            };
            this.Controls.Add(pnlMainContent);
            pnlMainContent.BringToFront();

            // 2.1 Studio Header Bar
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(16, 22, 34),
                Padding = new Padding(16, 0, 16, 0)
            };
            pnlMainContent.Controls.Add(pnlHeader);

            lblHeaderSectionTitle = new Label
            {
                Text = "ECU MAP STUDIO — ตารางจูนน้ำมันและองศาจุดระเบิด 2D Heatmap & 3D Surface Mesh",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = AccentCyan,
                Location = new Point(14, 13),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderSectionTitle);

            Label lblHeaderBadge = new Label
            {
                Text = "PRO STUDIO EDITION | ISO 14230 K-LINE",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AccentGreen,
                BackColor = Color.FromArgb(14, 38, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 4, 6, 4),
                Location = new Point(650, 10),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderBadge);
            pnlHeader.Resize += (s, e) =>
            {
                lblHeaderBadge.Location = new Point(pnlHeader.ClientSize.Width - lblHeaderBadge.Width - 16, 10);
            };

            pnlHeader.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(28, 38, 54), 1))
                {
                    e.Graphics.DrawLine(borderPen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            // 2.2 Main Tab Control (Tabs Hidden)
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed
            };
            pnlMainContent.Controls.Add(tabControl);
            tabControl.BringToFront();

            tabMapEditor = new TabPage("MAP STUDIO") { BackColor = BgDark };
            tabFlasher = new TabPage("ECU FLASHER") { BackColor = BgDark };
            tabLiveMonitor = new TabPage("LIVE TELEMETRY") { BackColor = BgDark };
            tabQuickTune = new TabPage("QUICK TUNE") { BackColor = BgDark };
            tabDiag = new TabPage("DIAGNOSTICS") { BackColor = BgDark };
            tabTools = new TabPage("TUNER TOOLS") { BackColor = BgDark };
            tabDatabase = new TabPage("MODEL DATABASE") { BackColor = BgDark };

            tabControl.TabPages.AddRange(new TabPage[] { 
                tabMapEditor, 
                tabFlasher, 
                tabLiveMonitor, 
                tabQuickTune, 
                tabDiag, 
                tabTools, 
                tabDatabase 
            });

            // Setup each tab
            SetupMapEditorTab();
            SetupFlasherTab();
            SetupLiveMonitorTab();
            SetupQuickTuneTab();
            SetupDiagTab();
            SetupToolsTab();
            SetupDatabaseTab();

            // Build Modern Vertical Navigation Buttons on Sidebar
            string[] navBadges = new string[]
            {
                "[MAPS]",
                "[FLASH]",
                "[LIVE]",
                "[TUNE]",
                "[DIAG]",
                "[TOOLS]",
                "[DATA]"
            };

            string[] navTitles = new string[]
            {
                "ตารางจูน 2D / 3D (Map Studio)",
                "แฟลช & ดูดไฟล์ (ECU Flasher)",
                "มาตรวัดสด (Live Telemetry)",
                "สูตรจูนด่วน 1-Click (Quick Tune)",
                "ตรวจเช็ค & ลบโค้ด (Diagnostics)",
                "เครื่องมือช่าง & Checksum",
                "ฐานข้อมูล 265+ รุ่น (Database)"
            };

            navButtons.Clear();
            int navY = 6;
            for (int i = 0; i < navBadges.Length; i++)
            {
                int tabIdx = i;
                Button btnNav = new Button
                {
                    Text = string.Format(" {0}  {1}", navBadges[i], navTitles[i]),
                    Location = new Point(6, navY),
                    Size = new Size(218, 44),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 6, 0)
                };
                btnNav.FlatAppearance.BorderSize = 0;
                btnNav.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 28, 42);
                btnNav.FlatAppearance.MouseDownBackColor = Color.FromArgb(16, 22, 34);

                btnNav.Paint += (s, e) =>
                {
                    Button b = (Button)s;
                    if (tabControl.SelectedIndex == tabIdx)
                    {
                        using (Brush brush = new SolidBrush(AccentCyan))
                        {
                            e.Graphics.FillRectangle(brush, 0, 5, 4, b.Height - 10);
                        }
                    }
                };

                btnNav.Click += (s, e) => { SelectNavTab(tabIdx); };
                pnlNavMenu.Controls.Add(btnNav);
                navButtons.Add(btnNav);
                navY += 48;
            }

            SelectNavTab(0);
        }

        private void SelectNavTab(int index)
        {
            if (index < 0 || index >= tabControl.TabPages.Count) return;
            tabControl.SelectedIndex = index;
            for (int i = 0; i < navButtons.Count; i++)
            {
                bool active = (i == index);
                navButtons[i].ForeColor = active ? AccentCyan : Color.FromArgb(144, 164, 174);
                navButtons[i].BackColor = active ? Color.FromArgb(18, 27, 42) : Color.Transparent;
                navButtons[i].Invalidate();
            }

            if (lblHeaderSectionTitle != null)
            {
                string[] titles = new string[]
                {
                    "ECU MAP STUDIO — ตารางจูนน้ำมันและองศาจุดระเบิด 2D Heatmap & 3D Surface Mesh",
                    "ECU FLASHER — เขียนและอ่านไฟล์รอม BIN ผ่านสาย K-Line ความเร็วสูง",
                    "LIVE TELEMETRY — หน้าปัดมาตรวัดสดดิจิทัล & กราฟ Data Logging ความเร็วสูง",
                    "QUICK TUNE — ปรับแต่งสูตรสำเร็จรูป 1-Click (เปิดรอบ, เดินหอบ, ปรับสเต็ป)",
                    "DIAGNOSTICS — ตรวจเช็คโค้ดลบโค้ด DTC & สั่งทดสอบระบบหัวฉีด/ปั๊มติ๊ก/หัวเทียน",
                    "TUNER TOOLS — เครื่องมือช่าง (แปลงไฟล์, แก้ไข Checksum อัตโนมัติ, ปลดล็อก XDF)",
                    "ECU DATABASE — คลังข้อมูลรุ่นรถและรหัสกล่อง ECM ฮอนด้ากว่า 265+ รุ่น"
                };
                if (index >= 0 && index < titles.Length)
                {
                    lblHeaderSectionTitle.Text = titles[index];
                }
            }
        }

        #region Tab 1: Flasher Tab Setup

        private void SetupFlasherTab()
        {
            int w = tabFlasher.ClientSize.Width > 0 ? tabFlasher.ClientSize.Width - 32 : 930;

            // Panel 1: Connection Card (With USB Cable & ECM Status)
            Panel pnlConn = CreateCardPanel(16, 12, w, 86, "การเชื่อมต่อสายฮาร์ดแวร์และการตรวจจับสาย (Hardware Interface & Cable Detection)");
            tabFlasher.Controls.Add(pnlConn);

            Label lblPort = new Label { Text = "พอร์ต / สายเคเบิ้ล:", Location = new Point(16, 26), AutoSize = true, ForeColor = TextMuted };
            pnlConn.Controls.Add(lblPort);

            cbPort = new ComboBox
            {
                Location = new Point(130, 23),
                Size = new Size(220, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CardHeaderBg,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };
            pnlConn.Controls.Add(cbPort);

            btnRefreshPorts = CreateStyledButton("รีเฟรช", 358, 22, 75, 27, CardHeaderBg, TextWhite);
            btnRefreshPorts.Click += (s, e) => { RefreshPortList(); UpdateHardwareStatus(true); };
            pnlConn.Controls.Add(btnRefreshPorts);

            btnConnect = CreateStyledButton("เชื่อมต่อ ECM", 442, 20, 130, 31, AccentBlue, Color.White, true);
            btnConnect.Click += BtnConnect_Click;
            pnlConn.Controls.Add(btnConnect);

            // Line 2: Dual Status Displays (USB Cable Status vs ECM Vehicle Status)
            pnlUsbIndicator = new Panel { Location = new Point(16, 59), Size = new Size(12, 12), BackColor = AccentRed };
            pnlUsbIndicator.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Brush b = new SolidBrush(pnlUsbIndicator.BackColor))
                {
                    e.Graphics.FillEllipse(b, 0, 0, 11, 11);
                }
            };
            pnlConn.Controls.Add(pnlUsbIndicator);

            lblUsbStatus = new Label { Text = "สาย USB: กำลังตรวจจับ...", Location = new Point(34, 56), Size = new Size(420, 18), ForeColor = AccentRed, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            pnlConn.Controls.Add(lblUsbStatus);

            pnlEcmIndicator = new Panel { Location = new Point(480, 59), Size = new Size(12, 12), BackColor = Color.FromArgb(120, 120, 120) };
            pnlEcmIndicator.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Brush b = new SolidBrush(pnlEcmIndicator.BackColor))
                {
                    e.Graphics.FillEllipse(b, 0, 0, 11, 11);
                }
            };
            pnlConn.Controls.Add(pnlEcmIndicator);

            lblEcmStatus = new Label { Text = "กล่อง ECM: ยังไม่เชื่อมต่อ", Location = new Point(498, 56), Size = new Size(420, 18), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            pnlConn.Controls.Add(lblEcmStatus);

            pnlConn.Resize += (s, e) =>
            {
                int col2X = Math.Max(460, (int)(pnlConn.ClientSize.Width * 0.48));
                pnlEcmIndicator.Location = new Point(col2X, 59);
                lblEcmStatus.Location = new Point(col2X + 18, 56);
                lblUsbStatus.Width = Math.Max(150, col2X - 40);
                lblEcmStatus.Width = Math.Max(150, pnlConn.ClientSize.Width - (col2X + 24));
            };

            // Panel 2: Live ECM Information Card
            Panel pnlEcm = CreateCardPanel(16, 106, w, 98, "ข้อมูลกล่อง ECM (Live Identification)");
            tabFlasher.Controls.Add(pnlEcm);

            Label l1 = new Label { Text = "รุ่นรถ (Model):", Location = new Point(16, 26), AutoSize = true, ForeColor = TextMuted };
            Label l2 = new Label { Text = "ไอดี ECM ID:", Location = new Point(16, 48), AutoSize = true, ForeColor = TextMuted };
            Label l3 = new Label { Text = "รหัสพาร์ท (Part Code):", Location = new Point(16, 70), AutoSize = true, ForeColor = TextMuted };

            Label l4 = new Label { Text = "สถาปัตยกรรม MCU:", Location = new Point(480, 26), AutoSize = true, ForeColor = TextMuted };
            Label l5 = new Label { Text = "จำนวนครั้งที่เคยแฟลช:", Location = new Point(480, 48), AutoSize = true, ForeColor = TextMuted };

            pnlEcm.Controls.AddRange(new Control[] { l1, l2, l3, l4, l5 });

            lblModelNameVal = new Label { Text = "-", Location = new Point(165, 26), Size = new Size(300, 18), ForeColor = AccentGreen, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblEcmIdVal = new Label { Text = "-", Location = new Point(165, 48), Size = new Size(300, 18), ForeColor = AccentBlue, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblPartCodeVal = new Label { Text = "-", Location = new Point(165, 70), Size = new Size(300, 18), ForeColor = TextWhite, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            lblMcuTypeVal = new Label { Text = "-", Location = new Point(635, 26), Size = new Size(280, 18), ForeColor = AccentPurple, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblFlashCountVal = new Label { Text = "-", Location = new Point(635, 48), Size = new Size(180, 18), ForeColor = Color.FromArgb(255, 171, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            pnlEcm.Controls.AddRange(new Control[] { lblModelNameVal, lblEcmIdVal, lblPartCodeVal, lblMcuTypeVal, lblFlashCountVal });

            pnlEcm.Resize += (s, e) =>
            {
                int col2X = Math.Max(480, (int)(pnlEcm.ClientSize.Width * 0.50));
                l4.Location = new Point(col2X, 26);
                l5.Location = new Point(col2X, 48);
                lblMcuTypeVal.Location = new Point(col2X + 155, 26);
                lblMcuTypeVal.Width = Math.Max(120, pnlEcm.ClientSize.Width - (col2X + 155) - 16);
                lblFlashCountVal.Location = new Point(col2X + 155, 48);
                lblFlashCountVal.Width = Math.Max(120, pnlEcm.ClientSize.Width - (col2X + 155) - 16);
            };

            // Panel 3: Binary File Operations Card
            Panel pnlFile = CreateCardPanel(16, 212, w, 122, "จัดการไฟล์ BIN สำหรับ แฟลช / ดูดไฟล์ (Flash Binary & Offsets)");
            tabFlasher.Controls.Add(pnlFile);

            btnBrowseBin = CreateStyledButton("เลือกไฟล์...", pnlFile.ClientSize.Width - 120, 24, 105, 26, CardHeaderBg, TextWhite);
            btnBrowseBin.Click += BtnBrowseBin_Click;

            txtBinPath = new TextBox
            {
                Location = new Point(16, 26),
                Size = new Size(Math.Max(150, pnlFile.ClientSize.Width - 145), 24),
                BackColor = CardHeaderBg,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };
            pnlFile.Controls.AddRange(new Control[] { txtBinPath, btnBrowseBin });

            pnlFile.Resize += (s, e) =>
            {
                btnBrowseBin.Location = new Point(pnlFile.ClientSize.Width - btnBrowseBin.Width - 16, 24);
                txtBinPath.Width = Math.Max(150, btnBrowseBin.Left - txtBinPath.Left - 10);
            };

            Label lStart = new Label { Text = "Start Offset:", Location = new Point(16, 61), AutoSize = true, ForeColor = TextMuted };
            txtStartOffset = new TextBox { Text = "0x0", Location = new Point(100, 58), Size = new Size(85, 23), BackColor = CardHeaderBg, ForeColor = TextWhite, BorderStyle = BorderStyle.FixedSingle };

            Label lCk = new Label { Text = "Checksum Offset:", Location = new Point(210, 61), AutoSize = true, ForeColor = TextMuted };
            txtChecksumOffset = new TextBox { Text = "0x0", Location = new Point(335, 58), Size = new Size(85, 23), BackColor = CardHeaderBg, ForeColor = TextWhite, BorderStyle = BorderStyle.FixedSingle };

            lblFileSize = new Label { Text = "ขนาดไฟล์: -", Location = new Point(445, 61), AutoSize = true, ForeColor = TextMuted };

            chkFastMode = new CheckBox { Text = "โหมดความเร็วสูง (Fast Mode 62,500 baud)", Location = new Point(16, 90), AutoSize = true, Checked = true, ForeColor = TextWhite };
            chkManualOffset = new CheckBox { Text = "ปรับแต่ง Offset เอง (Manual Offsets)", Location = new Point(335, 90), AutoSize = true, ForeColor = TextWhite };

            pnlFile.Controls.AddRange(new Control[] { lStart, txtStartOffset, lCk, txtChecksumOffset, lblFileSize, chkFastMode, chkManualOffset });

            // Panel 4: Actions & Progress Card (Fills remaining height)
            int actH = Math.Max(240, tabFlasher.ClientSize.Height - 356);
            Panel pnlActions = CreateCardPanel(16, 342, w, actH, "การทำงาน (Read / Write Engine & Live Monitor)");
            tabFlasher.Controls.Add(pnlActions);

            btnReadFlash = CreateStyledButton("[READ] ดูดไฟล์จาก ECM (Read Flash)", 16, 26, 240, 42, Color.FromArgb(0, 168, 150), Color.White, true);
            btnReadFlash.Click += BtnReadFlash_Click;

            btnWriteFlash = CreateStyledButton("[WRITE] เขียนไฟล์ลง ECM (Write Flash)", 268, 26, 250, 42, AccentGreen, Color.Black, true);
            btnWriteFlash.Click += BtnWriteFlash_Click;

            pnlActions.Controls.AddRange(new Control[] { btnReadFlash, btnWriteFlash });

            lblProgressText = new Label
            {
                Text = "พร้อมทำงาน (Ready)",
                Location = new Point(16, 76),
                Size = new Size(Math.Max(100, pnlActions.ClientSize.Width - 32), 20),
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            lblSpeedText = new Label
            {
                Text = "",
                Location = new Point(16, 98),
                Size = new Size(Math.Max(100, pnlActions.ClientSize.Width - 32), 18),
                ForeColor = TextMuted
            };

            progressBar = new ProgressBar
            {
                Location = new Point(16, 120),
                Size = new Size(Math.Max(100, pnlActions.ClientSize.Width - 32), 22),
                Minimum = 0,
                Maximum = 10000,
                BackColor = CardHeaderBg
            };
            pnlActions.Controls.AddRange(new Control[] { lblProgressText, lblSpeedText, progressBar });

            txtFlasherLog = new TextBox
            {
                Location = new Point(16, 150),
                Size = new Size(Math.Max(100, pnlActions.ClientSize.Width - 32), Math.Max(80, pnlActions.ClientSize.Height - 162)),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(12, 16, 24),
                ForeColor = Color.FromArgb(0, 230, 118),
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlActions.Controls.Add(txtFlasherLog);

            pnlActions.Resize += (s, e) =>
            {
                int innerW = Math.Max(100, pnlActions.ClientSize.Width - 32);
                int btnHalfW = Math.Max(100, (innerW - 12) / 2);
                btnReadFlash.Width = btnHalfW;
                btnWriteFlash.Location = new Point(btnReadFlash.Right + 12, 26);
                btnWriteFlash.Width = btnHalfW;

                lblProgressText.Width = innerW;
                lblSpeedText.Width = innerW;
                progressBar.Width = innerW;
                txtFlasherLog.Width = innerW;
                txtFlasherLog.Height = Math.Max(80, pnlActions.ClientSize.Height - 162);
            };

            tabFlasher.Resize += (s, e) =>
            {
                int totalW = tabFlasher.ClientSize.Width;
                int totalH = tabFlasher.ClientSize.Height;

                if (totalW >= 880)
                {
                    int leftW = 440;
                    pnlConn.Location = new Point(16, 12);
                    pnlConn.Size = new Size(leftW, 86);

                    pnlEcm.Location = new Point(16, 106);
                    pnlEcm.Size = new Size(leftW, 118);

                    pnlFile.Location = new Point(16, 232);
                    pnlFile.Size = new Size(leftW, Math.Max(145, totalH - 248));

                    int rightX = leftW + 28;
                    int rightW = Math.Max(380, totalW - rightX - 16);
                    pnlActions.Location = new Point(rightX, 12);
                    pnlActions.Size = new Size(rightW, Math.Max(350, totalH - 24));
                }
                else
                {
                    int cardW = Math.Max(200, totalW - 32);
                    pnlConn.Location = new Point(16, 12);
                    pnlConn.Size = new Size(cardW, 86);

                    pnlEcm.Location = new Point(16, 106);
                    pnlEcm.Size = new Size(cardW, 98);

                    pnlFile.Location = new Point(16, 212);
                    pnlFile.Size = new Size(cardW, 122);

                    pnlActions.Location = new Point(16, 342);
                    pnlActions.Size = new Size(cardW, Math.Max(200, totalH - 356));
                }
            };
        }

        #endregion

        #region Tab 2: ECU Map Editor (2D Table & 3D Surface Mesh)

        private void SetupMapEditorTab()
        {
            // 1. Top Bar Panel (Header, Model Presets, File Actions)
            Panel pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = CardBg,
                Padding = new Padding(10, 8, 10, 8)
            };
            tabMapEditor.Controls.Add(pnlTopBar);

            Label lblPreset = new Label
            {
                Text = "โมเดลรถ:",
                Location = new Point(10, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = TextSecondary
            };
            pnlTopBar.Controls.Add(lblPreset);

            cmbMapModelPreset = new ComboBox
            {
                Location = new Point(72, 14),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = InputBg,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            cmbMapModelPreset.Items.AddRange(new object[] {
                "Wave 110i LED 2021-2022 (K2J)",
                "Wave 125i (K73)",
                "PCX 160 (K1Z)",
                "ADV 160 (K0W)",
                "Forza 350 (K1B)",
                "Giorno+ (K3M)",
                "Lead 125 (K2T)"
            });
            cmbMapModelPreset.SelectedIndex = 0;
            pnlTopBar.Controls.Add(cmbMapModelPreset);

            btnLoadBinMap = CreateStyledButton("เปิดไฟล์ BIN", 300, 13, 120, 30, InputBg, TextPrimary);
            btnLoadXdfMap = CreateStyledButton("โหลดนิยาม XDF", 428, 13, 130, 30, InputBg, TextPrimary);
            btnSaveBinMap = CreateStyledButton("บันทึกไฟล์จูน (Auto Checksum)", 566, 13, 205, 30, AccentGreen, Color.Black, true);
            btnFlashDirect = CreateStyledButton("อัดลงกล่องทันที (Flash Direct)", 779, 13, 130, 30, AccentCyan, Color.Black, true);

            pnlTopBar.Controls.AddRange(new Control[] { btnLoadBinMap, btnLoadXdfMap, btnSaveBinMap, btnFlashDirect });

            // 2. Main Work Area Container
            Panel pnlMainArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(8)
            };
            tabMapEditor.Controls.Add(pnlMainArea);
            pnlMainArea.BringToFront();

            // Left Sidebar: Categories & Tables Tree
            Panel pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = CardBg,
                Padding = new Padding(6)
            };
            pnlMainArea.Controls.Add(pnlSidebar);

            Label lblTreeHead = new Label
            {
                Text = "รายการตารางจูน (Map List)",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = AccentCyan,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlSidebar.Controls.Add(lblTreeHead);

            txtSearchTable = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = InputBg,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9F),
                Text = "ค้นหาตาราง..."
            };
            txtSearchTable.Enter += (s, e) => { if (txtSearchTable.Text == "ค้นหาตาราง...") txtSearchTable.Text = ""; };
            txtSearchTable.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearchTable.Text)) txtSearchTable.Text = "ค้นหาตาราง..."; };
            pnlSidebar.Controls.Add(txtSearchTable);

            tvMapTables = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(16, 20, 30),
                ForeColor = TextPrimary,
                LineColor = Color.FromArgb(60, 75, 100),
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.None,
                HideSelection = false
            };
            pnlSidebar.Controls.Add(tvMapTables);
            tvMapTables.BringToFront();

            // Status strip at bottom of sidebar
            lblMapBinStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(12, 15, 22),
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 8F),
                Text = "สถานะ: พร้อมใช้งาน\nChecksum: อัตโนมัติ (ปลอดภัย)",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4)
            };
            pnlSidebar.Controls.Add(lblMapBinStatus);

            // Right Area: Workspace
            Panel pnlWorkspace = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(6, 0, 0, 0)
            };
            pnlMainArea.Controls.Add(pnlWorkspace);
            pnlWorkspace.BringToFront();

            // Workspace Toolbar
            Panel pnlWorkToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = CardBg,
                Padding = new Padding(8, 6, 8, 6)
            };
            pnlWorkspace.Controls.Add(pnlWorkToolbar);

            // Row 1 of Toolbar: Table title, View toggles, AFR references
            lblActiveTableTitle = new Label
            {
                Location = new Point(8, 8),
                Size = new Size(250, 24),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = AccentCyan,
                Text = "น้ำมัน 1-4 (Fuel In-Gear) [29x29]"
            };
            pnlWorkToolbar.Controls.Add(lblActiveTableTitle);

            btnView2D = CreateStyledButton("ตาราง 2D (Grid)", 265, 6, 115, 26, AccentCyan, Color.Black, true);
            btnView3D = CreateStyledButton("กราฟ 3D (Surface)", 386, 6, 125, 26, InputBg, TextPrimary);
            btnViewDiff = CreateStyledButton("เทียบเดิม (Diff Compare)", 517, 6, 110, 26, InputBg, TextPrimary);

            pnlWorkToolbar.Controls.AddRange(new Control[] { btnView2D, btnView3D, btnViewDiff });

            Label lblAfrTitle = new Label
            {
                Text = "AFR Target:",
                Location = new Point(635, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = TextSecondary
            };
            pnlWorkToolbar.Controls.Add(lblAfrTitle);

            int afrX = 710;
            string[] afrVals = new string[] { "12.0", "12.5", "13.2", "14.0", "14.7" };
            foreach (var afr in afrVals)
            {
                Button btnAfr = CreateStyledButton(afr, afrX, 7, 42, 24, Color.FromArgb(24, 32, 48), Color.FromArgb(0, 229, 255));
                btnAfr.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                btnAfr.Click += (s, e) =>
                {
                    float targetAfr;
                    if (float.TryParse(afr, out targetAfr))
                    {
                        float ratio = 14.7f / targetAfr;
                        mapSession.Scale(ratio);
                        gridEditor.Invalidate();
                        graph3D.Invalidate();
                        UpdateSelectionStats();
                    }
                };
                pnlWorkToolbar.Controls.Add(btnAfr);
                afrX += 46;
            }

            // Row 2 of Toolbar: Math operations & Selection stats
            int toolX = 8;
            Action<string, Action> addMathBtn = (txt, action) =>
            {
                Button b = CreateStyledButton(txt, toolX, 38, 50, 26, InputBg, TextPrimary);
                b.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                b.Click += (s, e) =>
                {
                    action();
                    gridEditor.Invalidate();
                    graph3D.Invalidate();
                    UpdateSelectionStats();
                };
                pnlWorkToolbar.Controls.Add(b);
                toolX += 54;
            };

            addMathBtn("+1%", () => mapSession.AddPercent(1f));
            addMathBtn("+5%", () => mapSession.AddPercent(5f));
            addMathBtn("-1%", () => mapSession.AddPercent(-1f));
            addMathBtn("-5%", () => mapSession.AddPercent(-5f));
            addMathBtn("+0.1", () => mapSession.AddOffset(0.1f));
            addMathBtn("-0.1", () => mapSession.AddOffset(-0.1f));

            Button btnSmooth = CreateStyledButton("เกลี่ย Smooth", toolX, 38, 92, 26, InputBg, TextPrimary);
            btnSmooth.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnSmooth.Click += (s, e) =>
            {
                mapSession.Smooth(0.5f);
                gridEditor.Invalidate();
                graph3D.Invalidate();
                UpdateSelectionStats();
            };
            pnlWorkToolbar.Controls.Add(btnSmooth);
            toolX += 96;

            Button btnInterpH = CreateStyledButton("เกลี่ยแนวนอน", toolX, 38, 88, 26, InputBg, TextPrimary);
            btnInterpH.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnInterpH.Click += (s, e) =>
            {
                mapSession.InterpolateHorizontal();
                gridEditor.Invalidate();
                graph3D.Invalidate();
                UpdateSelectionStats();
            };
            pnlWorkToolbar.Controls.Add(btnInterpH);
            toolX += 92;

            Button btnInterpV = CreateStyledButton("เกลี่ยแนวตั้ง", toolX, 38, 88, 26, InputBg, TextPrimary);
            btnInterpV.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnInterpV.Click += (s, e) =>
            {
                mapSession.InterpolateVertical();
                gridEditor.Invalidate();
                graph3D.Invalidate();
                UpdateSelectionStats();
            };
            pnlWorkToolbar.Controls.Add(btnInterpV);
            toolX += 92;

            Button btnUndo = CreateStyledButton("↺ ย้อนกลับ", toolX, 38, 76, 26, InputBg, TextPrimary);
            btnUndo.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnUndo.Click += (s, e) =>
            {
                if (mapSession.Undo())
                {
                    gridEditor.Invalidate();
                    graph3D.Invalidate();
                    UpdateSelectionStats();
                }
            };
            pnlWorkToolbar.Controls.Add(btnUndo);
            toolX += 80;

            lblSelectionStats = new Label
            {
                Location = new Point(toolX + 6, 42),
                Size = new Size(380, 20),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AccentGreen,
                Text = "ค่าต่ำสุด: 0.000 | สูงสุด: 0.000 | ค่าเฉลี่ย: 0.000 | เลือก: 1 เซลล์"
            };
            pnlWorkToolbar.Controls.Add(lblSelectionStats);

            // Center Panel: Controls Container
            Panel pnlDisplayHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(16, 20, 29)
            };
            pnlWorkspace.Controls.Add(pnlDisplayHost);
            pnlDisplayHost.BringToFront();

            gridEditor = new TableGridControl
            {
                Dock = DockStyle.Fill,
                Visible = true
            };
            gridEditor.SelectionChanged += (s, e) => UpdateSelectionStats();
            pnlDisplayHost.Controls.Add(gridEditor);

            graph3D = new Surface3DGraphControl
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlDisplayHost.Controls.Add(graph3D);

            // Wire up view mode switcher
            btnView2D.Click += (s, e) =>
            {
                gridEditor.Visible = true;
                gridEditor.ShowDiffMode = false;
                graph3D.Visible = false;
                btnView2D.BackColor = AccentCyan; btnView2D.ForeColor = Color.Black;
                btnView3D.BackColor = InputBg; btnView3D.ForeColor = TextPrimary;
                btnViewDiff.BackColor = InputBg; btnViewDiff.ForeColor = TextPrimary;
                gridEditor.Invalidate();
            };

            btnView3D.Click += (s, e) =>
            {
                gridEditor.Visible = false;
                graph3D.Visible = true;
                btnView2D.BackColor = InputBg; btnView2D.ForeColor = TextPrimary;
                btnView3D.BackColor = AccentCyan; btnView3D.ForeColor = Color.Black;
                btnViewDiff.BackColor = InputBg; btnViewDiff.ForeColor = TextPrimary;
                graph3D.Invalidate();
            };

            btnViewDiff.Click += (s, e) =>
            {
                gridEditor.Visible = true;
                gridEditor.ShowDiffMode = !gridEditor.ShowDiffMode;
                graph3D.Visible = false;
                btnViewDiff.BackColor = gridEditor.ShowDiffMode ? AccentGreen : InputBg;
                btnViewDiff.ForeColor = gridEditor.ShowDiffMode ? Color.Black : TextPrimary;
                gridEditor.Invalidate();
            };

            // Wire up buttons
            btnLoadBinMap.Click += (s, e) => LoadBinForMapTuning();
            btnLoadXdfMap.Click += (s, e) => LoadXdfForMapTuning();
            btnSaveBinMap.Click += (s, e) => SaveTunedBinFile();
            btnFlashDirect.Click += (s, e) => FlashDirectFromMap();

            cmbMapModelPreset.SelectedIndexChanged += (s, e) =>
            {
                string preset = (cmbMapModelPreset.SelectedItem != null) ? cmbMapModelPreset.SelectedItem.ToString() : "Wave 110i LED";
                LoadPresetDefinition(preset);
            };

            txtSearchTable.TextChanged += (s, e) =>
            {
                string q = txtSearchTable.Text.Trim();
                if (q == "ค้นหาตาราง...") q = "";
                FilterTableTree(q);
            };

            tvMapTables.AfterSelect += (s, e) =>
            {
                if (e.Node != null)
                {
                    XdfTable tbl = e.Node.Tag as XdfTable;
                    if (tbl != null)
                    {
                        mapSession.SelectTable(tbl);
                        lblActiveTableTitle.Text = tbl.Title + " [" + tbl.Rows + "x" + tbl.Cols + "]";
                        gridEditor.UpdateScrollbars();
                        gridEditor.Invalidate();
                        graph3D.Invalidate();
                        UpdateSelectionStats();
                    }
                }
            };

            // Auto-load default template and sample file if available
            LoadPresetDefinition("Wave 110i LED 2021-2022 (K2J)");
        }

        private void LoadPresetDefinition(string modelName)
        {
            XdfDefinition def = null;

            // Check if there is an exact XDF on disk for Wave 110i
            string sampleXdf = @"C:\Users\Jo_save\Desktop\realtime\XDF\64KB\38770-K2J-T01_8000-0_WAVE 110i 2021-2022.xdf";
            if (modelName.Contains("Wave 110i") && File.Exists(sampleXdf))
            {
                def = XdfDefinition.LoadFromFile(sampleXdf);
            }

            if (def == null || def.Tables.Count == 0)
            {
                def = XdfDefinition.GetBuiltinDefinition(modelName);
            }

            mapSession.LoadDefinition(def);

            // Auto-load sample BIN if available and no BIN currently loaded
            string sampleBin = @"C:\Users\Jo_save\Desktop\realtime\BIN\64KB\38770-K2J-T01_8000-0_WAVE 110i 2021-2022.bin";
            if (mapSession.ActiveBin == null)
            {
                if (loadedBin != null)
                {
                    mapSession.LoadBin(loadedBin, txtBinPath.Text);
                }
                else if (File.Exists(sampleBin))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(sampleBin);
                        mapSession.LoadBin(bytes, sampleBin);
                    }
                    catch { }
                }
            }

            gridEditor.BindSession(mapSession);
            graph3D.BindSession(mapSession);
            PopulateTableTree();
            UpdateSelectionStats();

            if (mapSession.ActiveBin != null)
            {
                lblMapBinStatus.Text = string.Format("ไฟล์: {0} ({1:N0} KB)\nChecksum: อัตโนมัติ (พร้อมแฟลช)",
                    Path.GetFileName(mapSession.BinFilePath ?? "Sample.bin"), mapSession.ActiveBin.Length / 1024);
            }
        }

        private void LoadBinForMapTuning()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Honda ECU Binary (*.bin)|*.bin|All Files (*.*)|*.*";
                ofd.Title = "เลือกไฟล์ BIN สำหรับจูนตาราง (Open BIN Map)";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    byte[] bytes = File.ReadAllBytes(ofd.FileName);
                    mapSession.LoadBin(bytes, ofd.FileName);

                    // Also set into main Flasher tab
                    loadedBin = bytes;
                    txtBinPath.Text = ofd.FileName;
                    lblFileSize.Text = string.Format("{0:N0} Bytes ({1} KB)", bytes.Length, bytes.Length / 1024);

                    gridEditor.BindSession(mapSession);
                    graph3D.BindSession(mapSession);
                    UpdateSelectionStats();

                    lblMapBinStatus.Text = string.Format("ไฟล์: {0} ({1:N0} KB)\nChecksum: อัตโนมัติ (พร้อมแฟลช)",
                        Path.GetFileName(ofd.FileName), bytes.Length / 1024);
                }
            }
        }

        private void LoadXdfForMapTuning()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "TunerPro Definition (*.xdf)|*.xdf|All Files (*.*)|*.*";
                ofd.Title = "โหลดนิยามตารางจูน TunerPro XDF (Open XDF)";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    XdfDefinition def = XdfDefinition.LoadFromFile(ofd.FileName);
                    if (def != null && def.Tables.Count > 0)
                    {
                        mapSession.LoadDefinition(def, ofd.FileName);
                        gridEditor.BindSession(mapSession);
                        graph3D.BindSession(mapSession);
                        PopulateTableTree();
                        UpdateSelectionStats();
                        MessageBox.Show(string.Format("โหลดนิยาม XDF สำเร็จ!\nพบตารางทั้งหมด {0} ตาราง และค่าคงที่ {1} รายการ",
                            def.Tables.Count, def.Constants.Count), "โหลดสำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("ไม่สามารถอ่านโครงสร้างตารางจากไฟล์ XDF นี้ได้", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void SaveTunedBinFile()
        {
            if (mapSession.ActiveBin == null)
            {
                MessageBox.Show("กรุณาเปิดไฟล์ BIN ก่อนทำการบันทึก", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Honda Tuned BIN (*.bin)|*.bin|All Files (*.*)|*.*";
                sfd.Title = "บันทึกไฟล์จูน (Save Tuned BIN with Auto-Checksum)";
                sfd.FileName = Path.GetFileNameWithoutExtension(mapSession.BinFilePath ?? "Tuned_ECU") + "_Tuned.bin";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    int ckOff;
                    if (mapSession.SaveTunedBin(sfd.FileName, out ckOff))
                    {
                        string msg = string.Format("บันทึกไฟล์จูนเรียบร้อยแล้ว!\nตำแหน่งไฟล์: {0}\n\n[ความปลอดภัย] ระบบคำนวณและแก้ Checksum อัตโนมัติเรียบร้อยแล้ว (Offset: 0x{1:X})\nสามารถนำไปอัดลงกล่อง ECU ได้ทันทีโดยไม่ล็อค",
                            sfd.FileName, ckOff);
                        MessageBox.Show(msg, "บันทึกสำเร็จ (Checksum Auto-Fixed)", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Update flasher path
                        txtBinPath.Text = sfd.FileName;
                        loadedBin = mapSession.ActiveBin;
                    }
                    else
                    {
                        MessageBox.Show("เกิดข้อผิดพลาดในการบันทึกไฟล์", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void FlashDirectFromMap()
        {
            if (mapSession.ActiveBin == null)
            {
                MessageBox.Show("กรุณาเปิดไฟล์ BIN และปรับแต่งตารางก่อนอัดลงกล่อง", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save changes to loadedBin
            loadedBin = (byte[])mapSession.ActiveBin.Clone();

            // Recalculate checksum
            if (loadedBin.Length == 0x10000) ChecksumEngine.FixKeihinChecksum(loadedBin, 0x7600);
            else if (loadedBin.Length == 0x20000) ChecksumEngine.FixKeihinChecksum(loadedBin, 0xDFEF);
            else if (loadedBin.Length == 0x40000) ChecksumEngine.FixKeihinChecksum(loadedBin, 0x3FFF8);
            else if (loadedBin.Length == 0xC000 || loadedBin.Length == 0xE000)
            {
                int shCk;
                ChecksumEngine.FixShindengenChecksum(loadedBin, out shCk);
            }

            // Switch to Flasher tab
            SelectNavTab(1);
            LogMessage("[MAP EDITOR] ถ่ายโอนข้อมูลจากตารางจูน 2D/3D ไปยัง Flasher พร้อม Auto-Checksum เรียบร้อยแล้ว!");
            LogMessage("[MAP EDITOR] กรุณากด 'เขียน Flash (อัดไฟล์)' เพื่อเริ่มกระบวนการแฟลชลงกล่อง");
        }

        private void PopulateTableTree(string filter = "")
        {
            tvMapTables.Nodes.Clear();
            if (mapSession.Definition == null) return;

            Dictionary<string, TreeNode> catNodes = new Dictionary<string, TreeNode>();

            foreach (var tbl in mapSession.Definition.Tables)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    if (tbl.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        tbl.CategoryName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                string cat = string.IsNullOrEmpty(tbl.CategoryName) ? "ทั่วไป (General)" : tbl.CategoryName;
                if (!catNodes.ContainsKey(cat))
                {
                    TreeNode cn = new TreeNode(cat) { ForeColor = AccentCyan };
                    catNodes[cat] = cn;
                    tvMapTables.Nodes.Add(cn);
                }

                TreeNode tn = new TreeNode(string.Format("{0} [{1}x{2}]", tbl.Title, tbl.Rows, tbl.Cols))
                {
                    Tag = tbl,
                    ForeColor = TextPrimary
                };
                catNodes[cat].Nodes.Add(tn);
            }

            // Constants
            if (mapSession.Definition.Constants.Count > 0)
            {
                TreeNode constCat = new TreeNode("สวิตช์ & ค่าคงที่ (Scalars)") { ForeColor = Color.FromArgb(255, 215, 64) };
                foreach (var c in mapSession.Definition.Constants)
                {
                    if (!string.IsNullOrEmpty(filter) && c.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    TreeNode cn = new TreeNode(c.Title + " (" + c.Units + ")")
                    {
                        Tag = c,
                        ForeColor = TextSecondary
                    };
                    constCat.Nodes.Add(cn);
                }
                if (constCat.Nodes.Count > 0) tvMapTables.Nodes.Add(constCat);
            }

            tvMapTables.ExpandAll();

            if (tvMapTables.Nodes.Count > 0 && tvMapTables.Nodes[0].Nodes.Count > 0)
            {
                tvMapTables.SelectedNode = tvMapTables.Nodes[0].Nodes[0];
            }
        }

        private void FilterTableTree(string query)
        {
            PopulateTableTree(query);
        }

        private void UpdateSelectionStats()
        {
            if (mapSession.ActiveTable == null || mapSession.ActiveBin == null)
            {
                lblSelectionStats.Text = "ค่าต่ำสุด: 0.000 | สูงสุด: 0.000 | ค่าเฉลี่ย: 0.000 | เลือก: 0 เซลล์";
                return;
            }

            XdfTable tbl = mapSession.ActiveTable;
            byte[] bin = mapSession.ActiveBin;
            Rectangle sel = mapSession.SelectedRange;

            int startR = Math.Max(0, sel.Y);
            int endR = Math.Min(tbl.Rows - 1, sel.Bottom - 1);
            int startC = Math.Max(0, sel.X);
            int endC = Math.Min(tbl.Cols - 1, sel.Right - 1);

            float min = float.MaxValue;
            float max = float.MinValue;
            float sum = 0f;
            int count = 0;

            for (int r = startR; r <= endR; r++)
            {
                for (int c = startC; c <= endC; c++)
                {
                    float v = tbl.GetDisplayValue(bin, r, c);
                    if (v < min) min = v;
                    if (v > max) max = v;
                    sum += v;
                    count++;
                }
            }

            if (count == 0)
            {
                lblSelectionStats.Text = "เลือก: 0 เซลล์";
            }
            else
            {
                float avg = sum / count;
                lblSelectionStats.Text = string.Format("ต่ำสุด: {0:F3} | สูงสุด: {1:F3} | เฉลี่ย: {2:F3} | เลือก: {3} เซลล์",
                    min, max, avg, count);
            }
        }

        #endregion

        #region Tab 3: Live Monitor Setup

        private void SetupLiveMonitorTab()
        {
            int w = tabLiveMonitor.ClientSize.Width > 0 ? tabLiveMonitor.ClientSize.Width - 32 : 930;

            // 1. Top Control Bar (Start, Pause, Record, Status)
            Panel pnlMonitorBar = CreateCardPanel(16, 12, w, 68, "ระบบควบคุมมาตรวัดสดและการบันทึกข้อมูล (Live Telemetry Controls)");
            tabLiveMonitor.Controls.Add(pnlMonitorBar);

            btnStartMonitor = CreateStyledButton("▶ เริ่มอ่านค่าสด (Start)", 16, 24, 155, 32, AccentGreen, Color.Black, true);
            btnStartMonitor.Click += BtnStartMonitor_Click;
            pnlMonitorBar.Controls.Add(btnStartMonitor);

            btnPauseMonitor = CreateStyledButton("หยุดชั่วคราว (Pause)", 178, 24, 155, 32, CardHeaderBg, TextWhite, false);
            btnPauseMonitor.Click += BtnPauseMonitor_Click;
            pnlMonitorBar.Controls.Add(btnPauseMonitor);

            btnRecordCsv = CreateStyledButton("บันทึก Data Log (CSV)", 340, 24, 175, 32, AccentBlue, Color.White, true);
            btnRecordCsv.Click += BtnRecordCsv_Click;
            pnlMonitorBar.Controls.Add(btnRecordCsv);

            lblRecordStatus = new Label
            {
                Text = "สถานะ: พร้อมอ่านค่าจากสาย K-Line",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = TextMuted,
                Location = new Point(525, 31),
                AutoSize = true
            };
            pnlMonitorBar.Controls.Add(lblRecordStatus);

            // 2. Gauges & Digital Tiles Card
            Panel pnlGauges = CreateCardPanel(16, 86, w, 280, "หน้าปัดเกจวัดดิจิทัลความเร็วสูง (High-Speed Digital Cockpit)");
            tabLiveMonitor.Controls.Add(pnlGauges);

            int tileW = 168;
            int tileH = 110;
            int gapX = 14;
            int gapY = 14;
            int startX = 16;
            int startY = 28;

            // Tile 1: RPM (Double width)
            Panel pnlRpm = CreateGaugeTile(startX, startY, tileW * 2 + gapX, tileH, "รอบเครื่องยนต์ (ENGINE RPM)", AccentBlue);
            lblRpmVal = new Label
            {
                Text = "0 RPM",
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Location = new Point(12, 32),
                AutoSize = true
            };
            pnlRpm.Controls.Add(lblRpmVal);
            pnlGauges.Controls.Add(pnlRpm);

            // Tile 2: TPS
            Panel pnlTps = CreateGaugeTile(startX + (tileW * 2 + gapX) + gapX, startY, tileW, tileH, "องศาลิ้นเร่ง (TPS)", AccentGreen);
            lblTpsVal = new Label { Text = "0.0 %", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = AccentGreen, Location = new Point(10, 36), AutoSize = true };
            pnlTps.Controls.Add(lblTpsVal);
            pnlGauges.Controls.Add(pnlTps);

            // Tile 3: ECT / EOT
            Panel pnlEot = CreateGaugeTile(startX + (tileW * 3 + gapX * 2) + gapX, startY, tileW, tileH, "อุณหภูมิเครื่อง (EOT)", Color.FromArgb(255, 145, 0));
            lblEotVal = new Label { Text = "0.0 °C", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(255, 171, 0), Location = new Point(10, 36), AutoSize = true };
            pnlEot.Controls.Add(lblEotVal);
            pnlGauges.Controls.Add(pnlEot);

            // Tile 4: Battery Voltage
            Panel pnlBatt = CreateGaugeTile(startX + (tileW * 4 + gapX * 3) + gapX, startY, tileW, tileH, "แรงดันแบตเตอรี่ (BATT)", AccentGreen);
            lblBattVal = new Label { Text = "0.0 V", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = AccentGreen, Location = new Point(10, 36), AutoSize = true };
            pnlBatt.Controls.Add(lblBattVal);
            pnlGauges.Controls.Add(pnlBatt);

            // Row 2:
            int r2Y = startY + tileH + gapY;

            // Tile 5: MAP
            Panel pnlMap = CreateGaugeTile(startX, r2Y, tileW, tileH, "แรงดันไอดี (MAP)", AccentPurple);
            lblMapVal = new Label { Text = "0 kPa", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = AccentPurple, Location = new Point(10, 36), AutoSize = true };
            pnlMap.Controls.Add(lblMapVal);
            pnlGauges.Controls.Add(pnlMap);

            // Tile 6: IAT
            Panel pnlIat = CreateGaugeTile(startX + tileW + gapX, r2Y, tileW, tileH, "อุณหภูมิไอดี (IAT)", Color.FromArgb(128, 222, 234));
            lblIatVal = new Label { Text = "0.0 °C", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(128, 222, 234), Location = new Point(10, 36), AutoSize = true };
            pnlIat.Controls.Add(lblIatVal);
            pnlGauges.Controls.Add(pnlIat);

            // Tile 7: Injector Duration
            Panel pnlInj = CreateGaugeTile(startX + (tileW + gapX) * 2, r2Y, tileW, tileH, "ระยะยกหัวฉีด (INJ)", Color.FromArgb(255, 171, 145));
            lblInjVal = new Label { Text = "0.00 ms", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(255, 171, 145), Location = new Point(10, 36), AutoSize = true };
            pnlInj.Controls.Add(lblInjVal);
            pnlGauges.Controls.Add(pnlInj);

            // Tile 8: Spark Advance
            Panel pnlAdv = CreateGaugeTile(startX + (tileW + gapX) * 3, r2Y, tileW, tileH, "องศาไฟจุดระเบิด (ADV)", Color.FromArgb(255, 238, 88));
            lblAdvVal = new Label { Text = "0.0° BTDC", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(255, 238, 88), Location = new Point(10, 36), AutoSize = true };
            pnlAdv.Controls.Add(lblAdvVal);
            pnlGauges.Controls.Add(pnlAdv);

            // Tile 9: O2 / AFR
            Panel pnlAfr = CreateGaugeTile(startX + (tileW + gapX) * 4, r2Y, tileW, tileH, "อัตราส่วนผสม (AFR)", Color.FromArgb(0, 230, 118));
            lblAfrVal = new Label { Text = "14.7 AFR", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = AccentGreen, Location = new Point(10, 36), AutoSize = true };
            pnlAfr.Controls.Add(lblAfrVal);
            pnlGauges.Controls.Add(pnlAfr);

            // 3. Real-time Telemetry Waveform Strip
            int waveH = Math.Max(160, tabLiveMonitor.ClientSize.Height - 380);
            Panel pnlWaveBox = CreateCardPanel(16, 374, w, waveH, "กราฟคลื่นสัญญาณสด (Real-time Telemetry Waveform: Cyan=RPM, Green=TPS)");
            tabLiveMonitor.Controls.Add(pnlWaveBox);

            pnlWaveform = new Panel
            {
                Location = new Point(16, 26),
                Size = new Size(Math.Max(100, pnlWaveBox.ClientSize.Width - 32), Math.Max(80, pnlWaveBox.ClientSize.Height - 36)),
                BackColor = Color.FromArgb(10, 14, 20),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlWaveform.Paint += PnlWaveform_Paint;
            pnlWaveBox.Controls.Add(pnlWaveform);

            pnlWaveBox.Resize += (s, e) =>
            {
                pnlWaveform.Width = Math.Max(100, pnlWaveBox.ClientSize.Width - 32);
                pnlWaveform.Height = Math.Max(80, pnlWaveBox.ClientSize.Height - 36);
            };

            tabLiveMonitor.Resize += (s, e) =>
            {
                int cardW = Math.Max(200, tabLiveMonitor.ClientSize.Width - 32);
                pnlMonitorBar.Width = cardW;
                pnlGauges.Width = cardW;
                pnlWaveBox.Width = cardW;
                pnlWaveBox.Height = Math.Max(140, tabLiveMonitor.ClientSize.Height - 380);
            };

            // Setup Timer
            tmrTelemetry = new System.Windows.Forms.Timer();
            tmrTelemetry.Interval = 100;
            tmrTelemetry.Tick += TmrTelemetry_Tick;
        }

        private Panel CreateGaugeTile(int x, int y, int w, int h, string title, Color accent)
        {
            Panel tile = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = CardHeaderBg,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = accent,
                Location = new Point(8, 8),
                AutoSize = true
            };
            tile.Controls.Add(lbl);
            return tile;
        }

        #endregion

        #region Tab 3: Quick Tune Setup

        private void SetupQuickTuneTab()
        {
            int w = tabQuickTune.ClientSize.Width > 0 ? tabQuickTune.ClientSize.Width - 32 : 930;

            Panel pnlRom = CreateCardPanel(16, 12, w, 70, "ไฟล์ ROM ที่กำลังปรับแต่ง (Active ROM Target)");
            tabQuickTune.Controls.Add(pnlRom);

            lblQuickRomInfo = new Label
            {
                Text = "ยังไม่ได้เปิดไฟล์ ROM (กรุณาเลือกไฟล์ในแท็บ Flasher หรือเปิดไฟล์ .bin)",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = AccentRed,
                Location = new Point(16, 28),
                AutoSize = true
            };
            pnlRom.Controls.Add(lblQuickRomInfo);

            Panel pnlChop = CreateCardPanel(16, 90, w, 150, "1. สูตรเดินหอบ (Thai Idle Chop / Lope Cam Sound Generator)");
            tabQuickTune.Controls.Add(pnlChop);

            Label lblChopDesc = new Label
            {
                Text = "ปรับแต่งจังหวะการจุดระเบิด (Ignition Advance) และการชดเชยน้ำมันรอบเดินเบา เพื่อให้เสียงท่อไอเสียเดินหอบดุดันสไตล์แคมซิ่ง\nระบบจะคำนวณ Checksum อัตโนมัติและสร้างไฟล์ใหม่พร้อมอัดลงกล่องได้ทันที",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = TextMuted,
                Location = new Point(16, 26),
                Size = new Size(Math.Max(100, pnlChop.ClientSize.Width - 32), 34)
            };
            pnlChop.Controls.Add(lblChopDesc);

            Label lblChopLvl = new Label { Text = "ระดับความหอบ:", Location = new Point(16, 68), AutoSize = true, ForeColor = TextWhite };
            pnlChop.Controls.Add(lblChopLvl);

            cbIdleChopLevel = new ComboBox
            {
                Location = new Point(120, 65),
                Size = new Size(260, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CardHeaderBg,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };
            cbIdleChopLevel.Items.Add("ระดับ 1: เดินหอบนุ่มนวล (Mild Lope Sound)");
            cbIdleChopLevel.Items.Add("ระดับ 2: เดินหอบลั่นดุดัน (Aggressive Cam Chop)");
            cbIdleChopLevel.SelectedIndex = 1;
            pnlChop.Controls.Add(cbIdleChopLevel);

            btnApplyIdleChop = CreateStyledButton("ใส่สูตรเดินหอบลงในไฟล์ (Apply Idle Chop)", 390, 63, 290, 30, Color.FromArgb(255, 109, 0), Color.White, true);
            btnApplyIdleChop.Click += BtnApplyIdleChop_Click;
            pnlChop.Controls.Add(btnApplyIdleChop);

            lblIdleChopResult = new Label
            {
                Text = "สถานะ: พร้อมใช้งาน (เลือกไฟล์แล้วกดปุ่มใส่สูตร)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = TextMuted,
                Location = new Point(16, 106),
                AutoSize = true
            };
            pnlChop.Controls.Add(lblIdleChopResult);

            Panel pnlRev = CreateCardPanel(16, 248, w, 150, "2. ปลดรอบ / ปรับรอบตัดเครื่องยนต์ (RPM Limiter Modifier)");
            tabQuickTune.Controls.Add(pnlRev);

            Label lblRevDesc = new Label
            {
                Text = "ค้นหาจุดตัดรอบเครื่องยนต์ (Stock Rev Limiter 9,200 - 9,800 RPM) และปรับเพิ่มความเร็วรอบตามต้องการ\nปลดล็อคความเร็วปลายของรถมอเตอร์ไซค์ พร้อมแก้ไข Checksum ให้อัตโนมัติ",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = TextMuted,
                Location = new Point(16, 26),
                Size = new Size(Math.Max(100, pnlRev.ClientSize.Width - 32), 34)
            };
            pnlRev.Controls.Add(lblRevDesc);

            Label lblRevTgt = new Label { Text = "เลือกรอบตัดที่ต้องการ:", Location = new Point(16, 68), AutoSize = true, ForeColor = TextWhite };
            pnlRev.Controls.Add(lblRevTgt);

            cbRevLimit = new ComboBox
            {
                Location = new Point(155, 65),
                Size = new Size(225, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CardHeaderBg,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };
            cbRevLimit.Items.AddRange(new object[] {
                "10,000 RPM (ปลอดภัย สเต็ปเดิม)",
                "10,500 RPM (ยอดนิยม Wave / PCX / Click)",
                "11,000 RPM (สเต็ปไล่ข้าง / ชามแต่ง)",
                "11,500 RPM (สเต็ปท่อกรอง)",
                "12,000 RPM (สเต็ปแคมซิ่ง / ลูกโต)",
                "12,500 RPM (สเต็ปโมดิฟายเต็มระบบ)",
                "13,000 RPM (สเต็ปแข่งขัน Drag Race)"
            });
            cbRevLimit.SelectedIndex = 1;
            pnlRev.Controls.Add(cbRevLimit);

            btnApplyRevLimit = CreateStyledButton("ปลดรอบเครื่องยนต์ (Patch Limiter)", 390, 63, 290, 30, AccentBlue, Color.White, true);
            btnApplyRevLimit.Click += BtnApplyRevLimit_Click;
            pnlRev.Controls.Add(btnApplyRevLimit);

            lblRevLimitResult = new Label
            {
                Text = "สถานะ: พร้อมใช้งาน (เลือกรอบที่ต้องการแล้วกดปลดรอบ)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = TextMuted,
                Location = new Point(16, 106),
                AutoSize = true
            };
            pnlRev.Controls.Add(lblRevLimitResult);

            Panel pnlStage = CreateCardPanel(16, 406, w, 150, "3. สูตรสเต็ปรถสำเร็จรูป (Quick Stage Presets)");
            tabQuickTune.Controls.Add(pnlStage);

            Label lblStageDesc = new Label
            {
                Text = "ปรับชดเชยน้ำมันเชื้อเพลิง (Fuel Trim) และองศาไฟจุดระเบิด (Spark Advance) ตามสเต็ปการตกแต่งเครื่องยนต์\nช่วยแก้ปัญหาน้ำมันบาง ปรับคันเร่งให้ตอบสนองไวขึ้น บิดติดมือรอบต้นและรอบปลาย",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = TextMuted,
                Location = new Point(16, 26),
                Size = new Size(Math.Max(100, pnlStage.ClientSize.Width - 32), 34)
            };
            pnlStage.Controls.Add(lblStageDesc);

            Label lblStageChoice = new Label { Text = "เลือกสเต็ปการแต่งรถ:", Location = new Point(16, 68), AutoSize = true, ForeColor = TextWhite };
            pnlStage.Controls.Add(lblStageChoice);

            cbStageTune = new ComboBox
            {
                Location = new Point(155, 65),
                Size = new Size(320, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CardHeaderBg,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };
            cbStageTune.Items.AddRange(new object[] {
                "สเต็ป 1: เดิมไล่ข้าง (Stock + Roller/Variator) [+4% Fuel, +2° Spark]",
                "สเต็ป 2: ท่อกรอง (Exhaust + High-Flow Air Filter) [+8% Fuel, +3° Spark]",
                "สเต็ป 3: ลูกโต (Big Bore Kit) [+14% Fuel, +1° Spark]"
            });
            cbStageTune.SelectedIndex = 0;
            pnlStage.Controls.Add(cbStageTune);

            btnApplyStageTune = CreateStyledButton("ใส่สูตรสเต็ปรถลงในไฟล์ (Apply Stage)", 485, 63, 260, 30, AccentGreen, Color.Black, true);
            btnApplyStageTune.Click += BtnApplyStageTune_Click;
            pnlStage.Controls.Add(btnApplyStageTune);

            lblStageTuneResult = new Label
            {
                Text = "สถานะ: พร้อมใช้งาน (เลือกสเต็ปแล้วกดใส่สูตร)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = TextMuted,
                Location = new Point(16, 106),
                AutoSize = true
            };
            pnlStage.Controls.Add(lblStageTuneResult);

            pnlChop.Resize += (s, e) => { lblChopDesc.Width = Math.Max(100, pnlChop.ClientSize.Width - 32); };
            pnlRev.Resize += (s, e) => { lblRevDesc.Width = Math.Max(100, pnlRev.ClientSize.Width - 32); };
            pnlStage.Resize += (s, e) => { lblStageDesc.Width = Math.Max(100, pnlStage.ClientSize.Width - 32); };

            tabQuickTune.Resize += (s, e) =>
            {
                int cardW = Math.Max(200, tabQuickTune.ClientSize.Width - 32);
                pnlRom.Width = cardW;
                pnlChop.Width = cardW;
                pnlRev.Width = cardW;
                pnlStage.Width = cardW;
            };
        }

        #endregion

        #region Tab 4: Diagnostics & Actuator Testing Setup

        private void SetupDiagTab()
        {
            int w = tabDiag.ClientSize.Width > 0 ? tabDiag.ClientSize.Width - 32 : 930;

            Panel pnlPreCheck = CreateCardPanel(16, 12, w, 115, "1. ตรวจสอบความพร้อมของรถก่อนแฟลช (Pre-Flash Safety Check)");
            tabDiag.Controls.Add(pnlPreCheck);

            btnPreFlashCheck = CreateStyledButton("ตรวจสอบความพร้อมของรถก่อนแฟลช (Pre-Flash Check)", 16, 26, 270, 34, AccentBlue, Color.White, true);
            btnPreFlashCheck.Click += BtnPreFlashCheck_Click;
            pnlPreCheck.Controls.Add(btnPreFlashCheck);

            txtPreFlashStatus = new TextBox
            {
                Location = new Point(296, 24),
                Size = new Size(Math.Max(100, pnlPreCheck.ClientSize.Width - 312), 78),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(12, 16, 24),
                ForeColor = Color.FromArgb(0, 230, 118),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "กดปุ่มเพื่อตรวจสอบ: แรงดันแบตเตอรี่, ตำแหน่งลิ้นเร่ง, โค้ดความผิดปกติค้างในกล่อง"
            };
            pnlPreCheck.Controls.Add(txtPreFlashStatus);

            Panel pnlDtcReset = CreateCardPanel(16, 134, w, 120, "2. โค้ดความผิดปกติและการรีเซ็ตกล่อง (DTC & Adaptations Reset)");
            tabDiag.Controls.Add(pnlDtcReset);

            btnReadDtc = CreateStyledButton("อ่านโค้ด DTC (Read Faults)", 16, 26, 140, 34, CardHeaderBg, TextWhite, true);
            btnReadDtc.Click += BtnReadDtc_Click;
            btnClearDtc = CreateStyledButton("ลบโค้ด (Clear DTC)", 164, 26, 160, 34, AccentRed, Color.White, true);
            btnClearDtc.Click += (s, e) => RunAction("ลบโค้ด DTC", () => activeEngine.ClearDTC());

            btnResetTp = CreateStyledButton("รีเซ็ตลิ้นเร่ง (TP Reset)", 332, 26, 175, 34, Color.FromArgb(0, 200, 83), Color.Black, true);
            btnResetTp.Click += (s, e) => RunAction("รีเซ็ตตำแหน่งลิ้นเร่ง (TP Reset)", () => activeEngine.ResetThrottlePosition());

            btnResetEcm = CreateStyledButton("รีเซ็ตกล่อง (ECM Reset)", 515, 26, 140, 34, Color.FromArgb(255, 145, 0), Color.Black, true);
            btnResetEcm.Click += (s, e) => RunAction("รีเซ็ต ECM", () => activeEngine.ResetECM());

            btnResetFlashCount = CreateStyledButton("ล้าง Flash Counter (Shindengen)", 663, 26, 230, 34, AccentPurple, Color.Black, true);
            btnResetFlashCount.Click += (s, e) => RunAction("รีเซ็ตแฟลชเคาท์", () => activeEngine.ResetFlashCounterShindengen());

            Label lblDtcHelp = new Label
            {
                Text = "คำแนะนำ: เลือกลบโค้ดหลังทำการซ่อมแซม และรีเซ็ตลิ้นเร่ง (TP Reset) เมื่อเปลี่ยนเรือนลิ้นเร่งหรือสายคันเร่งใหม่",
                Location = new Point(16, 75),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };
            pnlDtcReset.Controls.AddRange(new Control[] { btnReadDtc, btnClearDtc, btnResetTp, btnResetEcm, btnResetFlashCount, lblDtcHelp });

            Panel pnlActuators = CreateCardPanel(16, 262, w, 110, "3. ทดสอบสั่งงานอุปกรณ์จริง (Active Actuator Tests)");
            tabDiag.Controls.Add(pnlActuators);

            btnTestInjector = CreateStyledButton("ทดสอบหัวฉีด (Injector)", 16, 26, 185, 34, CardHeaderBg, TextWhite, true);
            btnTestInjector.Click += (s, e) => RunActuatorTest(ActuatorType.FuelInjector);

            btnTestFuelPump = CreateStyledButton("ทดสอบปั๊มติ๊ก (Fuel Pump)", 209, 26, 195, 34, CardHeaderBg, TextWhite, true);
            btnTestFuelPump.Click += (s, e) => RunActuatorTest(ActuatorType.FuelPump);

            btnTestTacho = CreateStyledButton("กวาดเรือนไมล์ (Tacho Sweep)", 412, 26, 205, 34, CardHeaderBg, TextWhite, true);
            btnTestTacho.Click += (s, e) => RunActuatorTest(ActuatorType.TachometerSweep);

            btnTestSpark = CreateStyledButton("ทดสอบไฟหัวเทียน (Spark)", 625, 26, 185, 34, CardHeaderBg, TextWhite, true);
            btnTestSpark.Click += (s, e) => RunActuatorTest(ActuatorType.IgnitionSparkCoil);

            lblActuatorResult = new Label
            {
                Text = "สถานะ: พร้อมสั่งงาน (กดปุ่มเพื่อทดสอบอุปกรณ์ตามต้องการ)",
                Location = new Point(16, 72),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };
            pnlActuators.Controls.AddRange(new Control[] { btnTestInjector, btnTestFuelPump, btnTestTacho, btnTestSpark, lblActuatorResult });

            int logH = Math.Max(200, tabDiag.ClientSize.Height - 390);
            Panel pnlLog = CreateCardPanel(16, 380, w, logH, "บันทึกการทำงานและผลลัพธ์ (Diagnostic Terminal Log)");
            tabDiag.Controls.Add(pnlLog);

            txtLog = new TextBox
            {
                Location = new Point(16, 26),
                Size = new Size(Math.Max(100, pnlLog.ClientSize.Width - 32), Math.Max(100, pnlLog.ClientSize.Height - 36)),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(12, 16, 24),
                ForeColor = Color.FromArgb(0, 230, 118),
                Font = new Font("Consolas", 9.5F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlLog.Controls.Add(txtLog);

            pnlPreCheck.Resize += (s, e) => { txtPreFlashStatus.Width = Math.Max(100, pnlPreCheck.ClientSize.Width - 312); };
            pnlLog.Resize += (s, e) => { txtLog.Width = Math.Max(100, pnlLog.ClientSize.Width - 32); txtLog.Height = Math.Max(100, pnlLog.ClientSize.Height - 36); };

            tabDiag.Resize += (s, e) =>
            {
                int cardW = Math.Max(200, tabDiag.ClientSize.Width - 32);
                pnlPreCheck.Width = cardW;
                pnlDtcReset.Width = cardW;
                pnlActuators.Width = cardW;
                pnlLog.Width = cardW;
                pnlLog.Height = Math.Max(160, tabDiag.ClientSize.Height - 390);
            };

            LogMessage("JOSAVE TUNER Diagnostic & Testing Studio initialized.");
        }

        #endregion

        #region Tab 3: Tools Setup

        private void SetupToolsTab()
        {
            int w = tabTools.ClientSize.Width > 0 ? tabTools.ClientSize.Width - 32 : 930;

            Panel pnlXdf = CreateCardPanel(16, 12, w, 115, "1. ปลดรหัส TunerPro XDF & ADX (Password Cleaner)");
            tabTools.Controls.Add(pnlXdf);

            Label lXdf = new Label
            {
                Text = "ถอดรหัสและลบรหัสผ่านออกจากไฟล์ XDF และ ADX ของ TunerPro โดยอัตโนมัติ ทำให้เปิดแก้ไขตารางจูนได้ทันที",
                Location = new Point(16, 28),
                Size = new Size(Math.Max(100, pnlXdf.ClientSize.Width - 32), 22),
                ForeColor = TextMuted
            };
            btnUnlockXdf = CreateStyledButton("เลือกไฟล์ XDF/ADX เพื่อปลดรหัส...", 16, 56, 260, 36, AccentBlue, Color.White, true);
            btnUnlockXdf.Click += BtnUnlockXdf_Click;
            pnlXdf.Controls.AddRange(new Control[] { lXdf, btnUnlockXdf });

            Panel pnlConv = CreateCardPanel(16, 136, w, 115, "2. แปลงไฟล์ .ACG และ .ECU เป็น .BIN (ROM Decrypter)");
            tabTools.Controls.Add(pnlConv);

            Label lConv = new Label
            {
                Text = "ถอดรหัสไฟล์ ROM ที่ถูกล็อคในรูปแบบ .ACG หรือ .ECU ให้กลับมาเป็นไฟล์ .BIN แบบมาตรฐานสำหรับเปิดจูน",
                Location = new Point(16, 28),
                Size = new Size(Math.Max(100, pnlConv.ClientSize.Width - 32), 22),
                ForeColor = TextMuted
            };
            btnConvertFile = CreateStyledButton("เลือกไฟล์ ACG/ECU เพื่อแปลงเป็น BIN...", 16, 56, 300, 36, AccentGreen, Color.Black, true);
            btnConvertFile.Click += BtnConvertFile_Click;
            pnlConv.Controls.AddRange(new Control[] { lConv, btnConvertFile });

            Panel pnlCk = CreateCardPanel(16, 260, w, 115, "3. ตรวจสอบและแก้ไข Checksum ไฟล์ BIN (Checksum Fixer)");
            tabTools.Controls.Add(pnlCk);

            Label lCkInfo = new Label
            {
                Text = "คำนวณและอัปเดต Checksum ไฟล์ BIN ทั้งกล่อง Keihin (8-bit) และ Shindengen (32-bit) โดยไม่ต้องเชื่อมต่อกล่อง",
                Location = new Point(16, 28),
                Size = new Size(Math.Max(100, pnlCk.ClientSize.Width - 32), 22),
                ForeColor = TextMuted
            };
            btnFixChecksumFile = CreateStyledButton("เลือกไฟล์ BIN เพื่อแก้ Checksum...", 16, 56, 280, 36, AccentPurple, Color.Black, true);
            btnFixChecksumFile.Click += BtnFixChecksumFile_Click;
            pnlCk.Controls.AddRange(new Control[] { lCkInfo, btnFixChecksumFile });

            pnlXdf.Resize += (s, e) => { lXdf.Width = Math.Max(100, pnlXdf.ClientSize.Width - 32); };
            pnlConv.Resize += (s, e) => { lConv.Width = Math.Max(100, pnlConv.ClientSize.Width - 32); };
            pnlCk.Resize += (s, e) => { lCkInfo.Width = Math.Max(100, pnlCk.ClientSize.Width - 32); };

            tabTools.Resize += (s, e) =>
            {
                int cardW = Math.Max(200, tabTools.ClientSize.Width - 32);
                pnlXdf.Width = cardW;
                pnlConv.Width = cardW;
                pnlCk.Width = cardW;
            };
        }

        #endregion

        #region Tab 4: Database Setup

        private void SetupDatabaseTab()
        {
            int w = tabDatabase.ClientSize.Width > 0 ? tabDatabase.ClientSize.Width - 32 : 930;

            Panel pnlSearch = CreateCardPanel(16, 12, w, 66, "ค้นหารุ่นรถ / รหัสกล่อง ECM (Search Database 265+ Models)");
            tabDatabase.Controls.Add(pnlSearch);

            Label lSearch = new Label { Text = "พิมพ์ค้นหา (รุ่นรถ / รหัสพาร์ท / ECM ID):", Location = new Point(16, 26), AutoSize = true, ForeColor = TextMuted };
            pnlSearch.Controls.Add(lSearch);

            txtDbSearch = new TextBox
            {
                Location = new Point(255, 23),
                Size = new Size(Math.Max(200, pnlSearch.ClientSize.Width - 275), 25),
                BackColor = CardHeaderBg,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtDbSearch.TextChanged += (s, e) => PopulateDatabaseView(txtDbSearch.Text);
            pnlSearch.Controls.Add(txtDbSearch);

            pnlSearch.Resize += (s, e) =>
            {
                txtDbSearch.Width = Math.Max(200, pnlSearch.ClientSize.Width - txtDbSearch.Left - 20);
            };

            int dbListH = Math.Max(300, tabDatabase.ClientSize.Height - 96);
            lvDatabase = new ListView
            {
                Location = new Point(16, 86),
                Size = new Size(w, dbListH),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(12, 16, 24),
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle
            };

            lvDatabase.Columns.Add("ลำดับ", 50);
            lvDatabase.Columns.Add("รุ่นรถ (Model)", 250);
            lvDatabase.Columns.Add("รหัสพาร์ท (Part Code)", 180);
            lvDatabase.Columns.Add("ECM ID", 120);
            lvDatabase.Columns.Add("ผู้ผลิต (Vendor)", 110);
            lvDatabase.Columns.Add("Start Offset", 90);
            lvDatabase.Columns.Add("Checksum Offset", 100);

            lvDatabase.Resize += (s, e) => AutoFitDatabaseColumns();
            tabDatabase.Controls.Add(lvDatabase);

            tabDatabase.Resize += (s, e) =>
            {
                int tabW = Math.Max(200, tabDatabase.ClientSize.Width - 32);
                pnlSearch.Width = tabW;
                lvDatabase.Width = tabW;
                lvDatabase.Height = Math.Max(200, tabDatabase.ClientSize.Height - 96);
            };
        }

        private void AutoFitDatabaseColumns()
        {
            if (lvDatabase == null || lvDatabase.Columns.Count < 7) return;
            int scrollWidth = SystemInformation.VerticalScrollBarWidth + 4;
            int totalWidth = lvDatabase.ClientSize.Width - scrollWidth;
            if (totalWidth < 750) totalWidth = 750;

            lvDatabase.Columns[0].Width = 50;                          // ลำดับ
            lvDatabase.Columns[1].Width = (int)(totalWidth * 0.32);     // รุ่นรถ (Model)
            lvDatabase.Columns[2].Width = (int)(totalWidth * 0.22);     // รหัสพาร์ท (Part Code)
            lvDatabase.Columns[3].Width = (int)(totalWidth * 0.16);     // ECM ID
            lvDatabase.Columns[4].Width = (int)(totalWidth * 0.12);     // ผู้ผลิต (Vendor)
            lvDatabase.Columns[5].Width = (int)(totalWidth * 0.09);     // Start Offset
            lvDatabase.Columns[6].Width = (int)(totalWidth * 0.09);     // Checksum Offset
        }

        private void PopulateDatabaseView(string query)
        {
            lvDatabase.Items.Clear();
            query = (query ?? "").Trim().ToLowerInvariant();

            int count = 1;
            foreach (var r in EcmDatabase.AllRecords)
            {
                if (string.IsNullOrEmpty(query) ||
                    r.ModelName.ToLowerInvariant().Contains(query) ||
                    r.PartCode.ToLowerInvariant().Contains(query) ||
                    r.EcmId.ToLowerInvariant().Contains(query))
                {
                    ListViewItem item = new ListViewItem(count.ToString());
                    item.SubItems.Add(r.ModelName);
                    item.SubItems.Add(r.PartCode);
                    item.SubItems.Add(r.EcmId);
                    item.SubItems.Add(r.Vendor.ToString());
                    item.SubItems.Add("0x" + r.StartOffset.ToString("X"));
                    item.SubItems.Add("0x" + r.ChecksumOffset.ToString("X"));

                    if (r.Vendor == EcuVendor.Shindengen)
                    {
                        item.ForeColor = Color.FromArgb(128, 222, 234);
                    }
                    else
                    {
                        item.ForeColor = Color.FromArgb(200, 230, 201);
                    }

                    lvDatabase.Items.Add(item);
                    count++;
                }
            }
            AutoFitDatabaseColumns();
        }

        #endregion

        #region Helpers & UI Factories

        private Panel CreateCardPanel(int x, int y, int width, int height, string title)
        {
            Panel card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.FromArgb(17, 23, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            card.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(32, 44, 64), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
                }
                using (Brush accentBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
                {
                    e.Graphics.FillRectangle(accentBrush, 0, 0, 28, 2);
                }
            };

            Label lblTitle = new Label
            {
                Text = title.ToUpperInvariant(),
                Location = new Point(14, 7),
                AutoSize = true,
                ForeColor = Color.FromArgb(88, 166, 255),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold)
            };
            card.Controls.Add(lblTitle);
            return card;
        }

        private Button CreateStyledButton(string text, int x, int y, int w, int h, Color bg, Color fg, bool bold = false)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75F, bold ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(Math.Min(255, bg.R + 25), Math.Min(255, bg.G + 25), Math.Min(255, bg.B + 25));
            return btn;
        }

        private void LogMessage(string msg)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate { LogMessage(msg); });
                return;
            }

            string line = string.Format("[{0:HH:mm:ss}] {1}\r\n", DateTime.Now, msg);
            if (txtLog != null && !txtLog.IsDisposed)
            {
                txtLog.AppendText(line);
            }
            if (txtFlasherLog != null && !txtFlasherLog.IsDisposed)
            {
                txtFlasherLog.AppendText(line);
            }
        }

        #endregion

        #region Operations: Connect, Read, Write, Tools

        private void RefreshPortList()
        {
            string selected = cbPort.SelectedItem != null ? cbPort.SelectedItem.ToString() : null;
            cbPort.Items.Clear();

            var hwList = HardwareDetector.DetectAllHardware();
            bool hasFtdi = false;
            foreach (var hw in hwList)
            {
                if (hw.IsFtdiDirect)
                {
                    cbPort.Items.Add("FTDI D2XX (Direct)");
                    hasFtdi = true;
                }
                else
                {
                    cbPort.Items.Add(hw.PortName);
                }
            }

            if (!hasFtdi)
            {
                cbPort.Items.Add("FTDI D2XX (Direct)");
            }

            cbPort.Items.Add("Virtual Simulator (Keihin 64KB)");
            cbPort.Items.Add("Virtual Simulator (Shindengen 512KB)");

            if (!string.IsNullOrEmpty(selected) && cbPort.Items.Contains(selected))
            {
                cbPort.SelectedItem = selected;
            }
            else if (cbPort.Items.Count > 0)
            {
                cbPort.SelectedIndex = 0;
            }
        }

        private void UpdateHardwareStatus(bool logChanges)
        {
            var hwList = HardwareDetector.DetectAllHardware();
            StringBuilder sb = new StringBuilder();
            foreach (var h in hwList)
            {
                sb.Append(h.PortName).Append(":").Append(h.SerialNumber).Append(";");
            }
            string currentSig = sb.ToString();

            bool isCablePlugged = hwList.Count > 0;
            string cableSummary = "";
            if (isCablePlugged)
            {
                var first = hwList[0];
                if (first.IsFtdiDirect)
                {
                    string snPart = string.IsNullOrEmpty(first.SerialNumber) ? "" : " [" + first.SerialNumber + "]";
                    cableSummary = string.Format("สาย USB: เสียบแล้ว ({0}{1})", first.CableName, snPart);
                }
                else
                {
                    cableSummary = string.Format("สาย USB: เสียบแล้ว ({0})", first.CableName);
                }
            }
            else
            {
                cableSummary = "สาย USB: ไม่พบสายเคเบิ้ล (กรุณาเสียบสาย K-Line เข้าพอร์ต USB)";
            }

            if (pnlUsbIndicator != null && !pnlUsbIndicator.IsDisposed)
            {
                pnlUsbIndicator.BackColor = isCablePlugged ? AccentGreen : AccentRed;
            }
            if (lblUsbStatus != null && !lblUsbStatus.IsDisposed)
            {
                lblUsbStatus.ForeColor = isCablePlugged ? AccentGreen : AccentRed;
                lblUsbStatus.Text = cableSummary;
            }

            if (pnlSidebarUsbLed != null && !pnlSidebarUsbLed.IsDisposed)
            {
                pnlSidebarUsbLed.BackColor = isCablePlugged ? AccentGreen : AccentRed;
            }
            if (lblSidebarUsbStatus != null && !lblSidebarUsbStatus.IsDisposed)
            {
                lblSidebarUsbStatus.ForeColor = isCablePlugged ? AccentGreen : AccentRed;
                lblSidebarUsbStatus.Text = isCablePlugged ? "K-Line: เสียบสายแล้ว" : "K-Line: ตรวจหาสาย...";
            }

            if (pnlEcmIndicator != null && !pnlEcmIndicator.IsDisposed && lblEcmStatus != null && !lblEcmStatus.IsDisposed)
            {
                if (activeEngine != null)
                {
                    pnlEcmIndicator.BackColor = AccentGreen;
                    lblEcmStatus.ForeColor = AccentGreen;
                    lblEcmStatus.Text = string.Format("กล่อง ECM: เชื่อมต่อสำเร็จ ({0} - พร้อมทำงาน)", detectedEcuType != EcuType.Unknown ? detectedEcuType.ToString() : "Online");
                }
                else
                {
                    if (isCablePlugged)
                    {
                        pnlEcmIndicator.BackColor = Color.FromArgb(255, 171, 0); // Amber
                        lblEcmStatus.ForeColor = Color.FromArgb(255, 171, 0);
                        lblEcmStatus.Text = "กล่อง ECM: ยังไม่เชื่อมต่อ (รอต่อสายกับรถ & เปิดกุญแจ)";
                    }
                    else
                    {
                        pnlEcmIndicator.BackColor = Color.FromArgb(120, 120, 120);
                        lblEcmStatus.ForeColor = TextMuted;
                        lblEcmStatus.Text = "กล่อง ECM: ยังไม่เชื่อมต่อ";
                    }
                }
            }

            if (pnlSidebarEcmLed != null && !pnlSidebarEcmLed.IsDisposed)
            {
                pnlSidebarEcmLed.BackColor = activeEngine != null ? AccentGreen : (isCablePlugged ? Color.FromArgb(255, 171, 0) : Color.FromArgb(120, 120, 120));
            }
            if (lblSidebarEcmStatus != null && !lblSidebarEcmStatus.IsDisposed)
            {
                if (activeEngine != null)
                {
                    lblSidebarEcmStatus.ForeColor = AccentGreen;
                    lblSidebarEcmStatus.Text = "ECM: ออนไลน์";
                }
                else
                {
                    lblSidebarEcmStatus.ForeColor = isCablePlugged ? Color.FromArgb(255, 171, 0) : TextMuted;
                    lblSidebarEcmStatus.Text = isCablePlugged ? "ECM: สแตนด์บาย" : "ECM: ออฟไลน์";
                }
            }

            if (currentSig != lastHwSig)
            {
                lastHwSig = currentSig;
                RefreshPortList();
                if (logChanges)
                {
                    if (isCablePlugged)
                    {
                        LogMessage("[USB CONNECT] ตรวจพบสายเคเบิ้ล USB: " + hwList[0].CableName + (string.IsNullOrEmpty(hwList[0].SerialNumber) ? "" : " [" + hwList[0].SerialNumber + "]") + " เสียบพร้อมใช้งาน");
                    }
                    else
                    {
                        LogMessage("[USB DISCONNECT] ถอดสายเคเบิ้ล USB ออกแล้ว");
                    }
                }
            }
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (activeEngine != null)
            {
                // Disconnect
                activeEngine.Dispose();
                activeEngine = null;
                activePort = null;
                btnConnect.Text = "เชื่อมต่อ ECM";
                btnConnect.BackColor = AccentBlue;
                UpdateHardwareStatus(false);
                LogMessage("Disconnected from ECM.");
                return;
            }

            string selected = cbPort.SelectedItem != null ? cbPort.SelectedItem.ToString() : "FTDI D2XX (Direct)";
            if (selected.StartsWith("Virtual Simulator (Keihin", StringComparison.OrdinalIgnoreCase))
            {
                activePort = new VirtualKLinePort(EcuVendor.Keihin, 64);
            }
            else if (selected.StartsWith("Virtual Simulator (Shindengen", StringComparison.OrdinalIgnoreCase))
            {
                activePort = new VirtualKLinePort(EcuVendor.Shindengen, 512);
            }
            else if (selected.StartsWith("FTDI"))
            {
                activePort = new FtdiD2xxPort(0);
            }
            else
            {
                activePort = new SerialKLinePort(selected);
            }

            activeEngine = new HondaKLineEngine(activePort);
            pnlEcmIndicator.BackColor = Color.FromArgb(255, 171, 0);
            lblEcmStatus.ForeColor = Color.FromArgb(255, 171, 0);
            lblEcmStatus.Text = "กล่อง ECM: กำลังส่ง Fast Init 10400 baud...";
            LogMessage("Initiating K-Line Fast Init on " + selected + "...");

            ThreadPool.QueueUserWorkItem(state =>
            {
                bool ok = activeEngine.Connect();
                this.Invoke((MethodInvoker)delegate
                {
                    if (ok)
                    {
                        pnlEcmIndicator.BackColor = AccentGreen;
                        lblEcmStatus.ForeColor = AccentGreen;
                        lblEcmStatus.Text = "กล่อง ECM: เชื่อมต่อสำเร็จ! กำลังอ่านข้อมูล ECM...";
                        btnConnect.Text = "ตัดการเชื่อมต่อ";
                        btnConnect.BackColor = AccentRed;
                        LogMessage("Connected successfully. Reading ECM Identification...");

                        ThreadPool.QueueUserWorkItem(s => QueryEcmData());
                    }
                    else
                    {
                        pnlEcmIndicator.BackColor = AccentRed;
                        lblEcmStatus.ForeColor = AccentRed;
                        lblEcmStatus.Text = "กล่อง ECM: ไม่ตอบสนอง (ตรวจสอบ: 1.เสียบปลั๊ก DLC กับรถ 2.เปิดสวิตช์กุญแจ ON)";
                        LogMessage("[ERROR] Connection failed. Check motorcycle DLC connector and 12V ignition power.");
                        activeEngine.Dispose();
                        activeEngine = null;
                        activePort = null;
                    }
                });
            });
        }

        private void QueryEcmData()
        {
            if (activeEngine == null) return;

            string ecmId;
            int flashCount;
            bool ok = activeEngine.QueryEcmId(out ecmId);
            activeEngine.QueryFlashCount(out flashCount);

            this.Invoke((MethodInvoker)delegate
            {
                if (ok && !string.IsNullOrEmpty(ecmId))
                {
                    lblEcmIdVal.Text = ecmId;
                    LogMessage("Found ECM ID: " + ecmId);

                    var rec = EcmDatabase.LookupByEcmId(ecmId);
                    if (rec != null)
                    {
                        lblModelNameVal.Text = rec.ModelName;
                        lblPartCodeVal.Text = rec.PartCode;
                        lblMcuTypeVal.Text = rec.Vendor + " (" + rec.RomSizeKb + " KB)";

                        if (!chkManualOffset.Checked)
                        {
                            txtStartOffset.Text = "0x" + rec.StartOffset.ToString("X");
                            txtChecksumOffset.Text = "0x" + rec.ChecksumOffset.ToString("X");
                        }
                        LogMessage("Identified Bike: " + rec.ModelName + " | Part: " + rec.PartCode);
                    }
                    else
                    {
                        lblModelNameVal.Text = "Honda Generic ECM";
                        lblPartCodeVal.Text = "Unknown in DB";
                        lblMcuTypeVal.Text = "Generic";
                    }

                    if (flashCount >= 0)
                    {
                        lblFlashCountVal.Text = flashCount.ToString() + " ครั้ง";
                        LogMessage("Flash Write Counter: " + flashCount);
                    }
                }
            });
        }

        private void BtnBrowseBin_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "ECU Binary Files (*.bin;*.hex)|*.bin;*.hex|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadBinaryFile(ofd.FileName);
                }
            }
        }

        private void LoadBinaryFile(string path)
        {
            txtBinPath.Text = path;
            loadedBin = File.ReadAllBytes(path);
            lblFileSize.Text = string.Format("ขนาดไฟล์: {0:F1} KB ({1} bytes)", loadedBin.Length / 1024.0, loadedBin.Length);

            detectedEcuType = EcmDatabase.DetectEcuTypeFromBin(loadedBin);
            lblMcuTypeVal.Text = detectedEcuType.ToString();

            int startOff, ckOff;
            if (EcmDatabase.DetectOffsets(loadedBin, detectedEcuType, out startOff, out ckOff))
            {
                if (!chkManualOffset.Checked)
                {
                    txtStartOffset.Text = "0x" + startOff.ToString("X");
                    txtChecksumOffset.Text = "0x" + ckOff.ToString("X");
                }
            }

            // Auto-calculate checksum
            if (detectedEcuType == EcuType.ShindengenV850 || detectedEcuType == EcuType.ShindengenRH850 || detectedEcuType == EcuType.ShindengenRH850B)
            {
                ChecksumEngine.FixShindengenChecksum(loadedBin, out ckOff);
                lblProgressText.Text = "คำนวณ Checksum 32-bit ของ Shindengen เรียบร้อยแล้ว (Valid)";
            }
            else
            {
                ChecksumEngine.FixKeihinChecksum(loadedBin, ckOff);
                lblProgressText.Text = "คำนวณ Checksum 8-bit ของ Keihin เรียบร้อยแล้ว (Valid)";
            }

            if (lblQuickRomInfo != null)
            {
                lblQuickRomInfo.Text = string.Format("ไฟล์ที่เปิด: {0} ({1:F1} KB) | ระบบกล่อง: {2} | Checksum: 0x{3:X} (OK)",
                    Path.GetFileName(path), loadedBin.Length / 1024.0, detectedEcuType, ckOff);
                lblQuickRomInfo.ForeColor = AccentGreen;
            }

            LogMessage("Loaded BIN file: " + path + " (" + loadedBin.Length + " bytes, " + detectedEcuType + ")");
        }

        private void BtnReadFlash_Click(object sender, EventArgs e)
        {
            if (activeEngine == null || !activePort.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อกับกล่อง ECM ก่อนดูดไฟล์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                string defaultName = string.Format("Honda_Dump_{0}_{1:yyyyMMdd_HHmmss}.bin",
                    lblEcmIdVal.Text != "-" ? lblEcmIdVal.Text : "ROM", DateTime.Now);
                sfd.FileName = defaultName;
                sfd.Filter = "ECU Binary Files (*.bin)|*.bin|All Files (*.*)|*.*";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string savePath = sfd.FileName;
                    int startOffset = 0;
                    try { startOffset = Convert.ToInt32(txtStartOffset.Text.Replace("0x", "").Trim(), 16); } catch { startOffset = 0; }

                    int romSize = 0x10000; // default 64KB
                    var rec = EcmDatabase.LookupByEcmId(lblEcmIdVal.Text);
                    if (rec != null && rec.RomSizeKb > 0)
                    {
                        romSize = rec.RomSizeKb * 1024;
                    }

                    bool fast = chkFastMode.Checked;

                    btnReadFlash.Enabled = false;
                    btnWriteFlash.Enabled = false;
                    progressBar.Value = 0;
                    lblProgressText.Text = "กำลังเริ่มกระบวนการดูดไฟล์ ROM จาก ECM...";
                    LogMessage("Starting Flash Read (Dump)... Target size: " + (romSize / 1024) + " KB");

                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        byte[] dumped;
                        bool ok = activeEngine.ReadFlash(startOffset, romSize, detectedEcuType, fast, out dumped, (snd, p) =>
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                progressBar.Value = (int)(p.Percentage * 100);
                                lblProgressText.Text = p.StatusMessage;
                                lblSpeedText.Text = string.Format("ความเร็วดูดไฟล์: {0:F1} KB/s | เวลาที่ใช้: {1:mm\\:ss}", p.SpeedKbps, p.Elapsed);
                            });
                        });

                        this.Invoke((MethodInvoker)delegate
                        {
                            btnReadFlash.Enabled = true;
                            btnWriteFlash.Enabled = true;

                            if (ok && dumped != null && dumped.Length > 0)
                            {
                                File.WriteAllBytes(savePath, dumped);
                                progressBar.Value = 10000;
                                lblProgressText.Text = "ดูดไฟล์สำเร็จสมบูรณ์ 100%! ขนาด: " + (dumped.Length / 1024) + " KB";
                                LogMessage("[SUCCESS] Flash Dump saved successfully: " + savePath);
                                MessageBox.Show("ดูดไฟล์จากกล่อง ECM สำเร็จเรียบร้อย!!\nไฟล์ถูกบันทึกไปที่:\n" + savePath, "ดูดไฟล์สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // Automatically load into editor
                                LoadBinaryFile(savePath);
                            }
                            else
                            {
                                lblProgressText.Text = "การดูดไฟล์ล้มเหลว!";
                                LogMessage("[ERROR] Flash Dump failed. Check connections and retry.");
                                MessageBox.Show("การดูดไฟล์จากกล่อง ECM ล้มเหลว!\nกรุณาตรวจสอบสายและลองใหม่อีกครั้ง", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        });
                    });
                }
            }
        }

        private void BtnWriteFlash_Click(object sender, EventArgs e)
        {
            if (loadedBin == null)
            {
                MessageBox.Show("กรุณาเลือกไฟล์ .BIN ก่อนเขียนแฟลช", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (activeEngine == null || !activePort.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อกับกล่อง ECM ก่อนทำการเขียนไฟล์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult dr = MessageBox.Show("คุณแน่ใจหรือไม่ว่าต้องการเขียนไฟล์ลงในกล่อง ECM?\nห้ามถอดสายหรือปิดไฟเลี้ยงระหว่างการเขียนไฟล์",
                "ยืนยันการเขียนแฟลช", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            int startOffset = 0;
            try { startOffset = Convert.ToInt32(txtStartOffset.Text.Replace("0x", "").Trim(), 16); } catch { startOffset = 0; }
            bool fast = chkFastMode.Checked;

            btnReadFlash.Enabled = false;
            btnWriteFlash.Enabled = false;
            progressBar.Value = 0;
            lblProgressText.Text = "กำลังเตรียมการเขียนแฟลช (Erasing & Flashing)...";
            LogMessage("Starting Flash Write... Start Offset: 0x" + startOffset.ToString("X") + " | Fast Mode: " + fast);

            ThreadPool.QueueUserWorkItem(state =>
            {
                bool success = activeEngine.WriteFlash(loadedBin, startOffset, detectedEcuType, fast, (snd, p) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        progressBar.Value = (int)(p.Percentage * 100);
                        lblProgressText.Text = p.StatusMessage;
                        lblSpeedText.Text = string.Format("ความเร็วเขียนไฟล์: {0:F1} KB/s | เวลาที่ใช้: {1:mm\\:ss}", p.SpeedKbps, p.Elapsed);
                    });
                });

                this.Invoke((MethodInvoker)delegate
                {
                    btnReadFlash.Enabled = true;
                    btnWriteFlash.Enabled = true;

                    if (success)
                    {
                        progressBar.Value = 10000;
                        lblProgressText.Text = "การเขียนไฟล์เสร็จสิ้นสมบูรณ์ 100%!";
                        LogMessage("[SUCCESS] Flash Write completed successfully 100%.");
                        MessageBox.Show("การเขียนไฟล์ลงกล่อง ECM สำเร็จเรียบร้อย!!\nกรุณาปิดสวิตช์กุญแจ 10 วินาทีแล้วเปิดใหม่อีกครั้ง", "เขียนไฟล์สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblProgressText.Text = "การเขียนไฟล์ล้มเหลว!";
                        LogMessage("[ERROR] Flash Write failed!");
                        MessageBox.Show("การเขียนไฟล์ลงกล่อง ECM ล้มเหลว!!\nกรุณาตรวจสอบสายและไฟเลี้ยง แล้วลองใหม่อีกครั้ง", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            });
        }

        private void RunAction(string name, Func<bool> action)
        {
            if (activeEngine == null || !activePort.IsOpen)
            {
                MessageBox.Show("กรุณาเชื่อมต่อกล่อง ECM ก่อน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LogMessage("Executing diagnostic command: " + name + "...");
            ThreadPool.QueueUserWorkItem(s =>
            {
                bool ok = action();
                this.Invoke((MethodInvoker)delegate
                {
                    if (ok)
                    {
                        LogMessage("[SUCCESS] " + name + " completed successfully.");
                        MessageBox.Show(name + " สำเร็จเรียบร้อย!", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        LogMessage("[FAILED] " + name + " execution failed.");
                        MessageBox.Show(name + " ล้มเหลว!", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            });
        }

        private void BtnUnlockXdf_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "TunerPro Files (*.xdf;*.adx)|*.xdf;*.adx|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string openPass, modPass, cleanXml;
                    if (TunerProPasswordCleaner.DecryptAndClean(ofd.FileName, out openPass, out modPass, out cleanXml))
                    {
                        string savePath = Path.Combine(Path.GetDirectoryName(ofd.FileName), Path.GetFileNameWithoutExtension(ofd.FileName) + "_unlocked" + Path.GetExtension(ofd.FileName));
                        File.WriteAllText(savePath, cleanXml, Encoding.UTF8);
                        LogMessage("TunerPro File Unlocked: " + savePath + " (Pass: " + openPass + ")");
                        MessageBox.Show(string.Format("ปลดรหัสไฟล์สำเร็จ!\nรหัสผ่านเปิด: {0}\nรหัสผ่านแก้ไข: {1}\nบันทึกไฟล์ไปที่:\n{2}",
                            openPass, modPass, savePath), "ปลดรหัสสำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("ไม่สามารถปลดรหัสไฟล์ได้ หรือไฟล์นี้ไม่มีการใส่รหัสผ่าน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void BtnConvertFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Encrypted ECU Files (*.acg;*.ecu)|*.acg;*.ecu|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] bin;
                        if (ofd.FileName.EndsWith(".acg", StringComparison.OrdinalIgnoreCase))
                        {
                            bin = FileConverterEngine.ConvertAcgToBin(ofd.FileName);
                        }
                        else
                        {
                            bin = FileConverterEngine.ConvertEcuToBin(ofd.FileName);
                        }

                        string savePath = Path.Combine(Path.GetDirectoryName(ofd.FileName), Path.GetFileNameWithoutExtension(ofd.FileName) + ".bin");
                        File.WriteAllBytes(savePath, bin);
                        LogMessage("Converted encrypted ROM -> BIN: " + savePath);
                        MessageBox.Show("แปลงไฟล์เป็น BIN สำเร็จ (" + bin.Length + " bytes)\nบันทึกไปที่:\n" + savePath, "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("เกิดข้อผิดพลาดในการแปลงไฟล์: " + ex.Message, "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnFixChecksumFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "ECU Binary Files (*.bin)|*.bin|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    byte[] data = File.ReadAllBytes(ofd.FileName);
                    EcuType ecu = EcmDatabase.DetectEcuTypeFromBin(data);
                    int startOff, ckOff;
                    EcmDatabase.DetectOffsets(data, ecu, out startOff, out ckOff);

                    if (ecu == EcuType.ShindengenV850 || ecu == EcuType.ShindengenRH850 || ecu == EcuType.ShindengenRH850B)
                    {
                        ChecksumEngine.FixShindengenChecksum(data, out ckOff);
                    }
                    else
                    {
                        ChecksumEngine.FixKeihinChecksum(data, ckOff);
                    }

                    string outPath = Path.Combine(Path.GetDirectoryName(ofd.FileName), Path.GetFileNameWithoutExtension(ofd.FileName) + "_fixed_cksum.bin");
                    File.WriteAllBytes(outPath, data);
                    LogMessage("Checksum corrected and saved to " + outPath);
                    MessageBox.Show(string.Format("แก้ไข Checksum สำเร็จ!\nประเภท: {0}\nChecksum Offset: 0x{1:X}\nบันทึกไปที่:\n{2}",
                        ecu, ckOff, outPath), "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        #endregion

        #region Live Monitor & Quick Tune & Diag Event Handlers

        private void BtnStartMonitor_Click(object sender, EventArgs e)
        {
            if (activeEngine == null)
            {
                DialogResult dr = MessageBox.Show(
                    "ขณะนี้ยังไม่ได้เชื่อมต่อกล่อง ECM กับตัวรถ\nคุณต้องการเปิดโหมดจำลองมาตรวัดสด (Simulation Demo Mode) เพื่อทดสอบหน้าปัดและเกจวัดหรือไม่?",
                    "เริ่มอ่านค่าสด (Live Telemetry)",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    activePort = new VirtualKLinePort(EcuVendor.Shindengen, 512);
                    activeEngine = new HondaKLineEngine(activePort);
                    activeEngine.Connect();
                    LogMessage("Live Monitor: Started in Simulation Demo Mode.");
                }
                else
                {
                    return;
                }
            }

            isTelemetryRunning = true;
            tmrTelemetry.Start();
            btnStartMonitor.BackColor = CardHeaderBg;
            btnStartMonitor.ForeColor = TextMuted;
            btnPauseMonitor.BackColor = Color.FromArgb(255, 145, 0);
            btnPauseMonitor.ForeColor = Color.Black;
            lblRecordStatus.Text = "สถานะ: กำลังอ่านค่าสด (Live Streaming 10Hz)...";
            lblRecordStatus.ForeColor = AccentGreen;
            LogMessage("Live Telemetry monitor started.");
        }

        private void BtnPauseMonitor_Click(object sender, EventArgs e)
        {
            isTelemetryRunning = false;
            tmrTelemetry.Stop();
            btnStartMonitor.BackColor = AccentGreen;
            btnStartMonitor.ForeColor = Color.Black;
            btnPauseMonitor.BackColor = CardHeaderBg;
            btnPauseMonitor.ForeColor = TextWhite;
            lblRecordStatus.Text = "สถานะ: หยุดอ่านค่าชั่วคราว (Paused)";
            lblRecordStatus.ForeColor = Color.FromArgb(255, 171, 0);
            if (gridEditor != null) gridEditor.ClearLivePosition();
            if (graph3D != null) graph3D.ClearLivePosition();
            LogMessage("Live Telemetry monitor paused.");
        }

        private void BtnRecordCsv_Click(object sender, EventArgs e)
        {
            if (dataLogger.IsLogging)
            {
                dataLogger.StopLogging();
                btnRecordCsv.Text = "บันทึก Data Log (CSV)";
                btnRecordCsv.BackColor = AccentBlue;
                lblRecordStatus.Text = string.Format("บันทึกเสร็จสิ้น ({0} แถว): {1}", dataLogger.RowCount, Path.GetFileName(dataLogger.FilePath));
                LogMessage("Data logging stopped. File saved: " + dataLogger.FilePath);
            }
            else
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV Data Log (*.csv)|*.csv";
                    sfd.FileName = string.Format("honda_log_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        if (dataLogger.StartLogging(sfd.FileName))
                        {
                            btnRecordCsv.Text = "หยุดบันทึก CSV";
                            btnRecordCsv.BackColor = AccentRed;
                            lblRecordStatus.Text = "กำลังบันทึก Log ลงไฟล์: " + Path.GetFileName(sfd.FileName);
                            LogMessage("Data logging started: " + sfd.FileName);
                            if (!isTelemetryRunning)
                            {
                                BtnStartMonitor_Click(null, null);
                            }
                        }
                    }
                }
            }
        }

        private void TmrTelemetry_Tick(object sender, EventArgs e)
        {
            if (activeEngine == null) return;

            LiveTelemetryData data;
            if (activeEngine.QueryLiveTelemetry(out data))
            {
                lblRpmVal.Text = data.EngineRpm.ToString("N0") + " RPM";
                lblTpsVal.Text = string.Format("{0:F1} % ({1:F2}V)", data.ThrottleDegrees, data.ThrottleVolts);
                lblEotVal.Text = string.Format("{0:F1} °C", data.EngineOilTempC);
                lblMapVal.Text = string.Format("{0:F0} kPa", data.ManifoldPressureKpa);
                lblIatVal.Text = string.Format("{0:F1} °C", data.IntakeAirTempC);
                lblBattVal.Text = string.Format("{0:F1} V", data.BatteryVolts);

                if (data.BatteryVolts >= 12.8)
                {
                    lblBattVal.ForeColor = AccentGreen;
                }
                else if (data.BatteryVolts >= 12.0)
                {
                    lblBattVal.ForeColor = Color.FromArgb(255, 238, 88);
                }
                else
                {
                    lblBattVal.ForeColor = AccentRed;
                }

                lblInjVal.Text = string.Format("{0:F2} ms", data.InjectorDurationMs);
                lblAdvVal.Text = string.Format("{0:F1}° BTDC", data.IgnitionAdvanceDeg);
                lblAfrVal.Text = string.Format("{0:F1} AFR", data.AirFuelRatio);

                // Add to waveform history
                rpmHistory.Add(data.EngineRpm);
                tpsHistory.Add((int)data.ThrottleDegrees);
                if (rpmHistory.Count > 120) rpmHistory.RemoveAt(0);
                if (tpsHistory.Count > 120) tpsHistory.RemoveAt(0);

                if (dataLogger.IsLogging)
                {
                    dataLogger.LogData(data);
                }

                if (pnlWaveform != null && !pnlWaveform.IsDisposed)
                {
                    pnlWaveform.Invalidate();
                }

                // Real-time cell tracking on 2D table & 3D surface map
                if (gridEditor != null && !gridEditor.IsDisposed)
                {
                    gridEditor.SetLiveEnginePosition(data.EngineRpm, (float)data.ThrottleDegrees);
                }
                if (graph3D != null && !graph3D.IsDisposed)
                {
                    graph3D.SetLiveEnginePosition(data.EngineRpm, (float)data.ThrottleDegrees);
                }
            }
        }

        private void PnlWaveform_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = pnlWaveform.ClientSize.Width;
            int h = pnlWaveform.ClientSize.Height;

            using (Pen gridPen = new Pen(Color.FromArgb(24, 32, 45)))
            {
                for (int y = 0; y < h; y += 30) g.DrawLine(gridPen, 0, y, w, y);
                for (int x = 0; x < w; x += 40) g.DrawLine(gridPen, x, 0, x, h);
            }

            if (rpmHistory.Count < 2) return;

            using (Pen rpmPen = new Pen(Color.FromArgb(0, 229, 255), 2f))
            using (Pen tpsPen = new Pen(AccentGreen, 1.5f))
            {
                float stepX = (float)w / Math.Max(60, rpmHistory.Count);
                for (int i = 0; i < rpmHistory.Count - 1; i++)
                {
                    float x1 = i * stepX;
                    float x2 = (i + 1) * stepX;

                    float yRpm1 = h - (Math.Min(12000, rpmHistory[i]) / 12000f * (h - 10));
                    float yRpm2 = h - (Math.Min(12000, rpmHistory[i + 1]) / 12000f * (h - 10));
                    g.DrawLine(rpmPen, x1, yRpm1, x2, yRpm2);

                    float yTps1 = h - (Math.Min(100, tpsHistory[i]) / 100f * (h - 10));
                    float yTps2 = h - (Math.Min(100, tpsHistory[i + 1]) / 100f * (h - 10));
                    g.DrawLine(tpsPen, x1, yTps1, x2, yTps2);
                }
            }
        }

        private void BtnApplyIdleChop_Click(object sender, EventArgs e)
        {
            if (loadedBin == null || loadedBin.Length == 0)
            {
                MessageBox.Show("กรุณาเลือกหรือเปิดไฟล์ BIN ก่อนทำการใส่สูตรเดินหอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int intensity = (cbIdleChopLevel.SelectedIndex == 0) ? 1 : 2;
            byte[] modBin;
            string status;

            if (OneClickTunerEngine.ApplyIdleChopMod(loadedBin, detectedEcuType, intensity, out modBin, out status))
            {
                string origPath = txtBinPath.Text;
                string dir = Path.GetDirectoryName(origPath);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(origPath);
                string newPath = Path.Combine(string.IsNullOrEmpty(dir) ? AppDomain.CurrentDomain.BaseDirectory : dir,
                    string.Format("{0}_idle_chop_lvl{1}.bin", nameWithoutExt, intensity));

                File.WriteAllBytes(newPath, modBin);
                LoadBinaryFile(newPath);

                lblIdleChopResult.Text = status;
                lblIdleChopResult.ForeColor = AccentGreen;
                LogMessage("Idle Chop Applied: " + newPath);

                MessageBox.Show(status + "\n\nบันทึกไฟล์เรียบร้อยที่:\n" + newPath + "\n\nโปรแกรมได้โหลดไฟล์นี้เข้าสู่ระบบแล้ว คุณสามารถกดไปที่แท็บ ECU Flasher แล้วกด 'อัดไฟล์' ได้ทันที!",
                    "สำเร็จ - สูตรเดินหอบ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblIdleChopResult.Text = "[FAIL] " + status;
                lblIdleChopResult.ForeColor = AccentRed;
            }
        }

        private void BtnApplyRevLimit_Click(object sender, EventArgs e)
        {
            if (loadedBin == null || loadedBin.Length == 0)
            {
                MessageBox.Show("กรุณาเลือกหรือเปิดไฟล์ BIN ก่อนทำการปลดรอบ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int[] limits = new int[] { 10000, 10500, 11000, 11500, 12000, 12500, 13000 };
            int selectedLimit = limits[Math.Max(0, Math.Min(limits.Length - 1, cbRevLimit.SelectedIndex))];

            byte[] modBin;
            string status;

            if (OneClickTunerEngine.ApplyRpmLimiterPatch(loadedBin, detectedEcuType, selectedLimit, out modBin, out status))
            {
                string origPath = txtBinPath.Text;
                string dir = Path.GetDirectoryName(origPath);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(origPath);
                string newPath = Path.Combine(string.IsNullOrEmpty(dir) ? AppDomain.CurrentDomain.BaseDirectory : dir,
                    string.Format("{0}_rev_{1}.bin", nameWithoutExt, selectedLimit));

                File.WriteAllBytes(newPath, modBin);
                LoadBinaryFile(newPath);

                lblRevLimitResult.Text = status;
                lblRevLimitResult.ForeColor = AccentGreen;
                LogMessage("Rev Limiter Patched: " + newPath);

                MessageBox.Show(status + "\n\nบันทึกไฟล์เรียบร้อยที่:\n" + newPath + "\n\nโปรแกรมได้โหลดไฟล์นี้เข้าสู่ระบบแล้ว คุณสามารถกดไปที่แท็บ ECU Flasher แล้วกด 'อัดไฟล์' ได้ทันที!",
                    "สำเร็จ - ปลดรอบเครื่องยนต์", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblRevLimitResult.Text = "[FAIL] " + status;
                lblRevLimitResult.ForeColor = AccentRed;
            }
        }

        private void BtnApplyStageTune_Click(object sender, EventArgs e)
        {
            if (loadedBin == null || loadedBin.Length == 0)
            {
                MessageBox.Show("กรุณาเลือกหรือเปิดไฟล์ BIN ก่อนทำการใส่สูตรสเต็ปรถ", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int stage = cbStageTune.SelectedIndex + 1;
            byte[] modBin;
            string status;

            if (OneClickTunerEngine.ApplyStageQuickTune(loadedBin, detectedEcuType, stage, out modBin, out status))
            {
                string origPath = txtBinPath.Text;
                string dir = Path.GetDirectoryName(origPath);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(origPath);
                string newPath = Path.Combine(string.IsNullOrEmpty(dir) ? AppDomain.CurrentDomain.BaseDirectory : dir,
                    string.Format("{0}_stage{1}.bin", nameWithoutExt, stage));

                File.WriteAllBytes(newPath, modBin);
                LoadBinaryFile(newPath);

                lblStageTuneResult.Text = status;
                lblStageTuneResult.ForeColor = AccentGreen;
                LogMessage("Stage Tune Applied: " + newPath);

                MessageBox.Show(status + "\n\nบันทึกไฟล์เรียบร้อยที่:\n" + newPath + "\n\nโปรแกรมได้โหลดไฟล์นี้เข้าสู่ระบบแล้ว คุณสามารถกดไปที่แท็บ ECU Flasher แล้วกด 'อัดไฟล์' ได้ทันที!",
                    "สำเร็จ - สูตรสเต็ปรถ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblStageTuneResult.Text = "[FAIL] " + status;
                lblStageTuneResult.ForeColor = AccentRed;
            }
        }

        private void BtnPreFlashCheck_Click(object sender, EventArgs e)
        {
            if (activeEngine == null)
            {
                MessageBox.Show("กรุณาเชื่อมต่อสายและกล่อง ECM ก่อนตรวจสอบความพร้อม", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var check = activeEngine.PerformPreFlashSafetyCheck();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== ผลการตรวจสอบความพร้อมก่อนแฟลช (Pre-Flash Safety Check) ===");
            foreach (var msg in check.CheckMessages)
            {
                sb.AppendLine(msg);
            }

            if (check.IsSafe)
            {
                sb.AppendLine("\n[RESULT OK] รถและกล่อง ECM อยู่ในสภาวะพร้อมและปลอดภัยสำหรับการแฟลช!");
                txtPreFlashStatus.ForeColor = AccentGreen;
            }
            else
            {
                sb.AppendLine("\n[RESULT WARN] ตรวจพบจุดที่ต้องแก้ไขก่อนแฟลช กรุณาตรวจสอบคำเตือนด้านบน");
                txtPreFlashStatus.ForeColor = AccentRed;
            }

            txtPreFlashStatus.Text = sb.ToString();
            LogMessage("Pre-flash safety check performed. Safe=" + check.IsSafe);
        }

        private void BtnReadDtc_Click(object sender, EventArgs e)
        {
            if (activeEngine == null)
            {
                MessageBox.Show("กรุณาเชื่อมต่อกล่อง ECM ก่อนอ่านโค้ด", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<DtcFaultCode> dtcs;
            if (activeEngine.ReadDtcFaults(out dtcs))
            {
                if (dtcs.Count == 0)
                {
                    LogMessage("DTC Scan: ไม่พบโค้ดความผิดปกติ (ระบบปกติ 100%)");
                    MessageBox.Show("[OK] ไม่พบโค้ดความผิดปกติค้างในกล่อง ECM (ระบบสะอาด 100%)", "DTC Scan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("พบโค้ดความผิดปกติ " + dtcs.Count + " รายการ:");
                    foreach (var d in dtcs)
                    {
                        sb.AppendLine("- " + d.ToString());
                        LogMessage("DTC Found: " + d.ToString());
                    }
                    MessageBox.Show(sb.ToString(), "พบโค้ดความผิดปกติ (DTC)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                LogMessage("DTC Scan: ไม่สามารถอ่านโค้ดได้ (ECM ไม่ตอบรับ)");
            }
        }

        private void RunActuatorTest(ActuatorType type)
        {
            if (activeEngine == null)
            {
                MessageBox.Show("กรุณาเชื่อมต่อสายและกล่อง ECM ก่อนทดสอบสั่งงานอุปกรณ์", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string result;
            if (activeEngine.ExecuteActuatorTest(type, out result))
            {
                lblActuatorResult.Text = result;
                lblActuatorResult.ForeColor = AccentGreen;
                LogMessage("Actuator Test: " + result);
                MessageBox.Show(result, "สั่งงานอุปกรณ์สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblActuatorResult.Text = result;
                lblActuatorResult.ForeColor = AccentRed;
                LogMessage("Actuator Test Failed: " + result);
            }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (tmrUsbDetector != null)
            {
                tmrUsbDetector.Stop();
                tmrUsbDetector.Dispose();
                tmrUsbDetector = null;
            }
            if (tmrTelemetry != null)
            {
                tmrTelemetry.Stop();
                tmrTelemetry.Dispose();
                tmrTelemetry = null;
            }
            if (dataLogger != null)
            {
                dataLogger.Dispose();
                dataLogger = null;
            }
            if (activeEngine != null)
            {
                activeEngine.Dispose();
                activeEngine = null;
            }
            base.OnFormClosing(e);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
