#nullable disable

using ShopeeVideoUploader.Controls;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel sidebar;
    private Panel mainContent;
    private Panel topBar;
    private Panel viewHost;
    private TableLayoutPanel workspace;
    private Panel productModulePanel;
    private Guna.UI2.WinForms.Guna2Panel panelWorkflowContainer;
    private TableLayoutPanel upperWorkflowLayout;
    private Guna.UI2.WinForms.Guna2Panel panelWorkflowPhone;
    private Panel topBarWorkflowPhone;
    private Panel pnlWorkflowScrcpyHost;
    private Control btnToggleWorkflowPhone;
    private Control btnWfPhoneHome;
    private Control btnWfPhoneBack;
    private Control btnWfPhonePower;
    private Control btnWfPhoneRestart;
    private Control btnWfPhoneClose;
    private Panel panelDevicesView;
    private Guna.UI2.WinForms.Guna2Panel phoneCardDevices;
    private Panel phoneToolBarDevices;
    private Panel phoneBottomBarDevices;
    private Panel pnlDevicesScrcpyHost;
    private Label lblDevicesPhoneTitle;
    private Label lblDevicesPhoneStatus;
    private Label lblDeviceInfoModel;
    private Label lblDeviceInfoSerial;
    private Label lblDeviceInfoRes;
    private Label lblDeviceInfoAndroid;
    private Label lblDeviceInfoStatus;
    private Control btnDevPhonePower;
    private Control btnDevPhoneHome;
    private Control btnDevPhoneBack;
    private Control btnDevPhoneRecents;
    private Control btnDevPhoneVolUp;
    private Control btnDevPhoneVolDown;
    private Control btnDevPhoneScreenshot;
    private Control btnDevicesStartScrcpy;
    private Control btnDevicesStopScrcpy;
    private Control btnOpenShopee;
    private Control btnOpenTikTok;
    private Control btnOpenFacebook;
    private Control btnOpenSettings;
    private Control btnMediaScan;
    private Control btnRestartAdb;
    private Guna.UI2.WinForms.Guna2TextBox txtWifiConnectIp;
    private Control btnWifiConnect;
    private Guna.UI2.WinForms.Guna2Panel panelInspector;
    private Guna.UI2.WinForms.Guna2Panel panelBottom;
    private Panel panelStatusBar;
    private Panel workflowBody;
    private Guna.UI2.WinForms.Guna2ComboBox cboDevices;
    private Guna.UI2.WinForms.Guna2ComboBox cboStepType;
    private Control btnRefreshDevices;
    private Control btnConnect;
    private Control btnDisconnect;
    private Control btnAddStep;
    private Control btnRemoveStep;
    private Control btnMoveUp;
    private Control btnMoveDown;
    private Control btnSaveWorkflow;
    private Control btnLoadWorkflow;
    private Control btnApplyConfig;
    private Control btnClearWorkflow;
    private Control btnRecordActions;
    private Control btnStopRecording;
    private Control btnZoomOut;
    private Control btnZoomReset;
    private Control btnZoomIn;
    private Label lblZoom;
    private Control btnImportExcel;
    private Control btnExportTemplate;
    private Control btnExportResult;
    private Control btnStart;
    private Control btnTestWorkflow;
    private Control btnStop;
    private Guna.UI2.WinForms.Guna2ComboBox cboMainFolderSelect;
    private Guna.UI2.WinForms.Guna2ComboBox cboWorkflowProfiles;
    private Control btnAddWorkflowProfile;
    private Control btnDeleteWorkflowProfile;
    private Control btnRenameWorkflowProfile;
    private Control btnManageWorkflows;
    private Control btnManualSaveProfile;
    private Control btnExportProfile;
    private WorkflowCanvas workflowCanvas;
    private FlowLayoutPanel workflowPalette;
    private FlowLayoutPanel workflowCorePalette;
    private Guna.UI2.WinForms.Guna2TextBox txtX;
    private Guna.UI2.WinForms.Guna2TextBox txtY;
    private Guna.UI2.WinForms.Guna2TextBox txtX2;
    private Guna.UI2.WinForms.Guna2TextBox txtY2;
    private Guna.UI2.WinForms.Guna2TextBox txtTextValue;
    private Guna.UI2.WinForms.Guna2TextBox txtBindingCol;
    private Guna.UI2.WinForms.Guna2TextBox txtDelayAfter;
    private Guna.UI2.WinForms.Guna2TextBox txtDescription;
    private Guna.UI2.WinForms.Guna2DataGridView dgvJobs;
    private RichTextBox txtLog;
    private ProgressBar progressBar;
    private Label lblStatus;
    private Label lblStepCount;
    private Label lblDeviceBadge;
    private Label lblSelectedStep;
    private ListBox lstVariables;
    private Guna.UI2.WinForms.Guna2TextBox txtVariableName;
    private Guna.UI2.WinForms.Guna2TextBox txtVariableValue;
    private Control btnAddVariable;
    private Control btnRemoveVariable;
    private Control btnInsertVariable;
    private Panel actionOptionsPanel;
    private Guna.UI2.WinForms.Guna2ComboBox cboVideoSource;
    private Guna.UI2.WinForms.Guna2TextBox txtVideoPath;
    private Control btnBrowseVideoPath;
    private CheckBox chkDeleteLocalVideo;
    private CheckBox chkClearDeviceVideos;
    private CheckBox chkUseAiForText;
    private CheckBox chkTakeAllLinks;
    private Guna.UI2.WinForms.Guna2ComboBox cboAppPackage;
    private Control btnRefreshApps;
    private Control btnCaptureCurrentApp;
    private Control btnCaptureTapCoordinates;
    private Guna.UI2.WinForms.Guna2ComboBox cboTapMode;
    private Guna.UI2.WinForms.Guna2ComboBox cboTapMultiMode;
    private Guna.UI2.WinForms.Guna2NumericUpDown numMultiTapDelay;
    private Label lblTapMultiMode;
    private Label lblMultiTapDelay;
    private Guna.UI2.WinForms.Guna2TextBox txtTapXPath;
    private Guna.UI2.WinForms.Guna2TextBox txtTapImagePath;
    private Control btnBrowseTapImage;
    private Control btnInspectUi;
    private TableLayoutPanel videoOptionsPanel;
    private Label lblVideoSource;
    private TableLayoutPanel appOptionsPanel;
    private CheckBox chkSkipFromSecondJob;
    private Guna.UI2.WinForms.Guna2CheckBox chkOnlyWithLink;
    private Panel tapOptionsPanel;
    private Panel randomTapOptionsPanel;
    private ListBox lstRandomCoordinates;
    private Control btnCaptureRandomTapCoordinate;
    private Control btnAddManualCoordinate;
    private Control btnRemoveRandomCoordinate;
    private Control btnClearRandomCoordinates;
    private Control btnGroupToRandomTap;
    private Panel swipeOptionsPanel;
    private Control btnCaptureSwipeCoordinates;
    private TableLayoutPanel inspectorFields;
    private ProductListControl productListControl;
    private Controls.OverviewControl _overviewControl;
    private Button navOverview;
    private Button navWorkflow;
    private Button navWorkflowIos;
    private Button navProducts;
    private Button navDevices;
    private Button navLogs;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

        Text = "FlowPilot - Multi-Platform Studio";
        ClientSize = new Size(1600, 940);
        MinimumSize = new Size(1260, 760);
        FormBorderStyle = FormBorderStyle.Sizable;
        ControlBox = true;
        MinimizeBox = true;
        MaximizeBox = true;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(246, 250, 254);
        ForeColor = Color.FromArgb(27, 55, 82);
        Font = new Font("Segoe UI", 9.5F);
        DoubleBuffered = true;

        sidebar = new Panel { Dock = DockStyle.Left, Width = 236, BackColor = Color.White, Padding = new Padding(16, 18, 14, 16) };
        var brand = new Label { Text = "◆  FLOWPILOT", AutoSize = true, Location = new Point(18, 20), Font = new Font("Segoe UI Semibold", 13.5F, FontStyle.Bold), ForeColor = Color.FromArgb(79, 70, 229) };
        var brandSub = new Label { Text = "AUTOMATION STUDIO", AutoSize = true, Location = new Point(20, 48), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };
        sidebar.Controls.Add(brandSub);
        sidebar.Controls.Add(brand);

        var navTitle = new Label { Text = "KHÔNG GIAN LÀM VIỆC", AutoSize = true, Location = new Point(20, 96), Font = new Font("Segoe UI Semibold", 8F), ForeColor = Color.FromArgb(148, 163, 184) };
        sidebar.Controls.Add(navTitle);
        var nav = new FlowLayoutPanel { Location = new Point(14, 120), Size = new Size(208, 360), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        navOverview = CreateNavButton("⌂   Tổng quan", false);
        navWorkflow = CreateNavButton("⌘   Quy trình", true);
        navWorkflowIos = CreateNavButton("⌘   Quy trình iPhone", false);
        navWorkflowIos.Visible = false;
        navProducts = CreateNavButton("▦   Dữ liệu & công việc", false);
        navDevices = CreateNavButton("◉   Thiết bị", false);
        navLogs = CreateNavButton("≡   Nhật ký hoạt động", false);
        nav.Controls.AddRange([navOverview, navWorkflow, navProducts, navDevices, navLogs]);
        sidebar.Controls.Add(nav);

        var sideCard = new Panel { Location = new Point(16, 500), Size = new Size(204, 100), BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(12) };
        var sideCardTitle = new Label { Text = "SẴN SÀNG TỰ ĐỘNG", AutoSize = true, Location = new Point(12, 12), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(79, 70, 229) };
        var sideCardText = new Label { Text = "Kết nối thiết bị để bắt đầu\nxây dựng quy trình.", AutoSize = true, Location = new Point(12, 36), Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139) };
        sideCard.Controls.Add(sideCardText);
        sideCard.Controls.Add(sideCardTitle);
        sidebar.Controls.Add(sideCard);
        var sideFooter = new Label { Text = "BỘ MÁY CỤC BỘ  •  v1.1\nQUY TRÌNH ADB", AutoSize = true, Location = new Point(20, 850), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(83, 111, 140) };
        sidebar.Controls.Add(sideFooter);
        Controls.Add(sidebar);

        mainContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 250, 254), Padding = new Padding(0) };
        Controls.Add(mainContent);

        topBar = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.White, Padding = new Padding(0) };
        
        // --- LEFT: logo + title block ---
        var titleBlock = new Panel { Location = new Point(0, 0), Size = new Size(245, 70), BackColor = Color.Transparent };
        var pnlLogo = new Guna.UI2.WinForms.Guna2Panel
        {
            Size = new Size(36, 36),
            Location = new Point(14, 17),
            BorderRadius = 8,
            FillColor = Color.FromArgb(79, 70, 229)
        };
        var lblLogoIcon = new Label
        {
            Text = "⚡",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Color.White
        };
        pnlLogo.Controls.Add(lblLogoIcon);

        var title = new Label { Text = "Workflow Studio", AutoSize = true, Location = new Point(55, 14), Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
        var subtitle = new Label { Text = "Thiết kế & chạy quy trình video", AutoSize = true, Location = new Point(57, 39), Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139) };
        titleBlock.Controls.AddRange([pnlLogo, title, subtitle]);
        topBar.Controls.Add(titleBlock);
        
        // --- CENTER: device connection row ---
        var centerPanel = new Panel { Location = new Point(250, 0), Size = new Size(362, 70), BackColor = Color.Transparent };
        lblDeviceBadge = new Label
        {
            Text = "●  Chưa kết nối thiết bị",
            AutoSize = true,
            Location = new Point(2, 4),
            Font = new Font("Segoe UI Semibold", 7.5F),
            ForeColor = Color.FromArgb(146, 64, 14),
            BackColor = Color.FromArgb(254, 243, 199),
            Padding = new Padding(6, 1, 6, 1)
        };
        cboDevices = CreateComboBox(150, Color.White);
        cboDevices.Font = new Font("Segoe UI", 9F);
        cboDevices.Location = new Point(0, 22);
        if (cboDevices is Guna.UI2.WinForms.Guna2ComboBox gunaDevices)
        {
            gunaDevices.BorderRadius = 8;
            gunaDevices.BorderColor = Color.FromArgb(226, 232, 240);
            gunaDevices.Size = new Size(155, 38);
        }
        btnRefreshDevices = CreateButton("🔄", Color.FromArgb(248, 250, 252), 38);
        btnRefreshDevices.Location = new Point(160, 22);
        if (btnRefreshDevices is Guna.UI2.WinForms.Guna2Button rfBtn)
        {
            rfBtn.ForeColor = Color.FromArgb(71, 85, 105);
            rfBtn.BorderColor = Color.FromArgb(226, 232, 240);
            rfBtn.BorderThickness = 1;
            rfBtn.BorderRadius = 8;
            rfBtn.Size = new Size(38, 38);
            rfBtn.Font = new Font("Segoe UI", 9.5F);
        }
        btnConnect = CreateButton("Kết nối", Color.FromArgb(79, 70, 229), 80);
        btnConnect.Location = new Point(204, 22);
        if (btnConnect is Guna.UI2.WinForms.Guna2Button cnBtn)
        {
            cnBtn.BorderRadius = 8;
            cnBtn.Size = new Size(80, 38);
            cnBtn.Font = new Font("Segoe UI Semibold", 9F);
        }
        btnDisconnect = CreateButton("Ngắt", Color.FromArgb(248, 250, 252), 70);
        btnDisconnect.Location = new Point(288, 22);
        if (btnDisconnect is Guna.UI2.WinForms.Guna2Button dcBtn)
        {
            dcBtn.ForeColor = Color.FromArgb(71, 85, 105);
            dcBtn.BorderColor = Color.FromArgb(226, 232, 240);
            dcBtn.BorderThickness = 1;
            dcBtn.BorderRadius = 8;
            dcBtn.Size = new Size(70, 38);
            dcBtn.Font = new Font("Segoe UI Semibold", 9F);
        }
        btnDisconnect.Enabled = false;
        centerPanel.Controls.AddRange([lblDeviceBadge, cboDevices, btnRefreshDevices, btnConnect, btnDisconnect]);
        topBar.Controls.Add(centerPanel);
        
        // --- RIGHT: folder select + action buttons ---
        var rightPanel = new Panel { Dock = DockStyle.Right, Width = 524, BackColor = Color.Transparent };
        
        cboMainFolderSelect = new Guna.UI2.WinForms.Guna2ComboBox
        {
            Location = new Point(0, 16),
            Size = new Size(160, 38),
            FillColor = Color.White,
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(15, 23, 42),
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "Name",
            ValueMember = "Id",
            Font = new Font("Segoe UI", 9F)
        };
        rightPanel.Controls.Add(cboMainFolderSelect);
        
        btnStart = CreateButton("▶  CHẠY QUY TRÌNH", Color.FromArgb(79, 70, 229), 160);
        btnStart.Location = new Point(168, 16);
        if (btnStart is Guna.UI2.WinForms.Guna2Button stBtn)
        {
            stBtn.Size = new Size(160, 38);
            stBtn.BorderRadius = 8;
            stBtn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }
        rightPanel.Controls.Add(btnStart);
        
        btnTestWorkflow = CreateButton("▷  CHẠY THỬ", Color.FromArgb(248, 250, 252), 105);
        btnTestWorkflow.Location = new Point(336, 16);
        if (btnTestWorkflow is Guna.UI2.WinForms.Guna2Button twBtn)
        {
            twBtn.Size = new Size(105, 38);
            twBtn.BorderRadius = 8;
            twBtn.BorderColor = Color.FromArgb(226, 232, 240);
            twBtn.BorderThickness = 1;
            twBtn.ForeColor = Color.FromArgb(51, 65, 85);
            twBtn.Font = new Font("Segoe UI Semibold", 9F);
        }
        rightPanel.Controls.Add(btnTestWorkflow);
        
        btnStop = CreateButton("■  DỪNG", Color.FromArgb(254, 242, 242), 74);
        btnStop.Location = new Point(448, 16);
        btnStop.Enabled = false;
        if (btnStop is Guna.UI2.WinForms.Guna2Button spBtn)
        {
            spBtn.Size = new Size(74, 38);
            spBtn.BorderRadius = 8;
            spBtn.BorderColor = Color.FromArgb(254, 202, 202);
            spBtn.BorderThickness = 1;
            spBtn.ForeColor = Color.FromArgb(185, 28, 28);
            spBtn.Font = new Font("Segoe UI Semibold", 9F);
        }
        rightPanel.Controls.Add(btnStop);
        
        topBar.Controls.Add(rightPanel);

        topBar.Resize += (_, _) =>
        {
            var midSpace = topBar.ClientSize.Width - titleBlock.Width - rightPanel.Width;
            if (midSpace >= centerPanel.Width + 10)
            {
                centerPanel.Left = titleBlock.Width + (midSpace - centerPanel.Width) / 2;
            }
            else
            {
                centerPanel.Left = titleBlock.Width + 4;
                if (centerPanel.Right > rightPanel.Left - 4)
                {
                    centerPanel.Left = Math.Max(titleBlock.Right + 2, rightPanel.Left - centerPanel.Width - 4);
                }
            }
        };

        mainContent.Controls.Add(topBar);

        viewHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 250, 254) };
        mainContent.Controls.Add(viewHost);

        workspace = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 250, 254), ColumnCount = 1, RowCount = 2, Padding = new Padding(16, 18, 16, 14) };
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 64F));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
        viewHost.Controls.Add(workspace);

        upperWorkflowLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent };
        upperWorkflowLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        upperWorkflowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        upperWorkflowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
        upperWorkflowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330F));
        workspace.Controls.Add(upperWorkflowLayout, 0, 0);

        panelWorkflowContainer = CreateCardPanel();
        panelWorkflowPhone = CreateCardPanel();
        panelWorkflowPhone.Visible = false;
        panelWorkflowPhone.Padding = new Padding(4);
        panelInspector = CreateCardPanel();
        upperWorkflowLayout.Controls.Add(panelWorkflowContainer, 0, 0);
        upperWorkflowLayout.Controls.Add(panelWorkflowPhone, 1, 0);
        upperWorkflowLayout.Controls.Add(panelInspector, 2, 0);

        BuildWorkflowPanel();
        BuildWorkflowPhonePanel();
        BuildInspectorPanel();
        BuildBottomPanel(workspace);
        BuildDevicesPanel();

        productModulePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 245, 248), Visible = false };
        productListControl = new ProductListControl { Dock = DockStyle.Fill };
        productModulePanel.Controls.Add(productListControl);
        viewHost.Controls.Add(productModulePanel);

        _overviewControl = new Controls.OverviewControl { Dock = DockStyle.Fill, Visible = false };
        viewHost.Controls.Add(_overviewControl);

        panelStatusBar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.FromArgb(248, 249, 251), Padding = new Padding(18, 0, 18, 0) };
        lblStatus = new Label { Text = "Sẵn sàng", AutoSize = true, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(107, 114, 128), Font = new Font("Segoe UI", 8.5F) };
        progressBar = new ProgressBar { Dock = DockStyle.Right, Width = 190, Height = 12, Style = ProgressBarStyle.Continuous, Maximum = 100, Value = 0 };
        panelStatusBar.Controls.Add(progressBar);
        panelStatusBar.Controls.Add(lblStatus);
        mainContent.Controls.Add(panelStatusBar);
        sidebar.BringToFront();
        viewHost.BringToFront();
        ResumeLayout(false);
    }

    private void BuildWorkflowPanel()
    {
        var heading = CreateHeading("SƠ ĐỒ QUY TRÌNH", "Kéo khối để sắp xếp · thả vào khối khác để đổi thứ tự", out lblStepCount);
        var profilePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 20, 16, 0)
        };

        cboWorkflowProfiles = CreateComboBox(170, Color.White);
        cboWorkflowProfiles.BorderRadius = 6;
        cboWorkflowProfiles.BorderColor = Color.FromArgb(209, 213, 219);
        cboWorkflowProfiles.Height = 34;
        cboWorkflowProfiles.Font = new Font("Segoe UI", 9F);
        cboWorkflowProfiles.Margin = new Padding(2, 0, 4, 0);

        btnManualSaveProfile = CreateButton("💾 Lưu", Color.FromArgb(16, 185, 129), 68);
        btnManualSaveProfile.ForeColor = Color.White;
        btnManualSaveProfile.Margin = new Padding(2, 0, 4, 0);
        if (btnManualSaveProfile is Guna.UI2.WinForms.Guna2Button bSav)
        {
            bSav.BorderRadius = 6;
            bSav.Height = 34;
            bSav.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnAddWorkflowProfile = CreateButton("＋ Mới", Color.FromArgb(96, 82, 218), 70);
        btnAddWorkflowProfile.ForeColor = Color.White;
        btnAddWorkflowProfile.Margin = new Padding(2, 0, 4, 0);
        if (btnAddWorkflowProfile is Guna.UI2.WinForms.Guna2Button bAdd)
        {
            bAdd.BorderRadius = 6;
            bAdd.Height = 34;
            bAdd.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnDeleteWorkflowProfile = CreateButton("🗑️ Xóa", Color.FromArgb(239, 68, 68), 68);
        btnDeleteWorkflowProfile.ForeColor = Color.White;
        btnDeleteWorkflowProfile.Margin = new Padding(2, 0, 4, 0);
        if (btnDeleteWorkflowProfile is Guna.UI2.WinForms.Guna2Button bDel)
        {
            bDel.BorderColor = Color.FromArgb(220, 38, 38);
            bDel.BorderThickness = 0;
            bDel.BorderRadius = 6;
            bDel.Height = 34;
            bDel.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnRenameWorkflowProfile = CreateButton("✏️ Sửa", Color.FromArgb(243, 244, 246), 66);
        btnRenameWorkflowProfile.ForeColor = Color.FromArgb(55, 65, 81);
        btnRenameWorkflowProfile.Margin = new Padding(2, 0, 4, 0);
        if (btnRenameWorkflowProfile is Guna.UI2.WinForms.Guna2Button bRen)
        {
            bRen.BorderColor = Color.FromArgb(209, 213, 219);
            bRen.BorderThickness = 1;
            bRen.BorderRadius = 6;
            bRen.Height = 34;
            bRen.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnManageWorkflows = CreateButton("📋 Quản lý", Color.FromArgb(243, 244, 246), 82);
        btnManageWorkflows.ForeColor = Color.FromArgb(55, 65, 81);
        btnManageWorkflows.Margin = new Padding(2, 0, 4, 0);
        if (btnManageWorkflows is Guna.UI2.WinForms.Guna2Button bMan)
        {
            bMan.BorderColor = Color.FromArgb(209, 213, 219);
            bMan.BorderThickness = 1;
            bMan.BorderRadius = 6;
            bMan.Height = 34;
            bMan.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnLoadWorkflow = CreateButton("📥 Nhập", Color.FromArgb(243, 244, 246), 70);
        btnLoadWorkflow.ForeColor = Color.FromArgb(55, 65, 81);
        btnLoadWorkflow.Margin = new Padding(2, 0, 4, 0);
        if (btnLoadWorkflow is Guna.UI2.WinForms.Guna2Button bImp)
        {
            bImp.BorderColor = Color.FromArgb(209, 213, 219);
            bImp.BorderThickness = 1;
            bImp.BorderRadius = 6;
            bImp.Height = 34;
            bImp.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnExportProfile = CreateButton("📤 Xuất", Color.FromArgb(243, 244, 246), 70);
        btnExportProfile.ForeColor = Color.FromArgb(55, 65, 81);
        btnExportProfile.Margin = new Padding(2, 0, 2, 0);
        if (btnExportProfile is Guna.UI2.WinForms.Guna2Button bExp)
        {
            bExp.BorderColor = Color.FromArgb(209, 213, 219);
            bExp.BorderThickness = 1;
            bExp.BorderRadius = 6;
            bExp.Height = 34;
            bExp.Font = new Font("Segoe UI Semibold", 9F);
        }

        var profileTips = new ToolTip();
        btnGroupToRandomTap = CreateButton("🎲 Gom ngẫu nhiên", Color.FromArgb(168, 85, 247), 140);
        btnGroupToRandomTap.ForeColor = Color.White;
        btnGroupToRandomTap.Visible = false;
        btnGroupToRandomTap.Margin = new Padding(2, 0, 4, 0);
        if (btnGroupToRandomTap is Guna.UI2.WinForms.Guna2Button bGrp)
        {
            bGrp.BorderRadius = 6;
            bGrp.Height = 34;
            bGrp.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnToggleWorkflowPhone = CreateButton("📱 Màn hình", Color.FromArgb(243, 244, 246), 115);
        btnToggleWorkflowPhone.ForeColor = Color.FromArgb(55, 65, 81);
        btnToggleWorkflowPhone.Margin = new Padding(2, 0, 4, 0);
        if (btnToggleWorkflowPhone is Guna.UI2.WinForms.Guna2Button bTog)
        {
            bTog.BorderColor = Color.FromArgb(209, 213, 219);
            bTog.BorderThickness = 1;
            bTog.BorderRadius = 6;
            bTog.Height = 34;
            bTog.Font = new Font("Segoe UI Semibold", 9F);
        }

        profileTips.SetToolTip(btnToggleWorkflowPhone, "Bật / Ẩn màn hình điện thoại Scrcpy bên cạnh sơ đồ quy trình");
        profileTips.SetToolTip(btnGroupToRandomTap, "Gom các bước Chạm đã bôi chọn thành 1 bước Chạm ngẫu nhiên");
        profileTips.SetToolTip(btnManualSaveProfile, "Lưu thay đổi vào quy trình hiện tại");
        profileTips.SetToolTip(btnAddWorkflowProfile, "Tạo một quy trình mới");
        profileTips.SetToolTip(btnDeleteWorkflowProfile, "Xóa vĩnh viễn quy trình đang chọn (hoặc click Quản lý)");
        profileTips.SetToolTip(btnRenameWorkflowProfile, "Đổi tên quy trình đang chọn");
        profileTips.SetToolTip(btnManageWorkflows, "Mở cửa sổ Quản lý quy trình (xem tất cả, xóa, nhân bản)");
        profileTips.SetToolTip(btnLoadWorkflow, "Nhập quy trình từ tệp .json");
        profileTips.SetToolTip(btnExportProfile, "Xuất quy trình ra tệp .json");

        profilePanel.Controls.AddRange([cboWorkflowProfiles, btnToggleWorkflowPhone, btnGroupToRandomTap, btnManualSaveProfile, btnAddWorkflowProfile, btnDeleteWorkflowProfile, btnRenameWorkflowProfile, btnManageWorkflows, btnLoadWorkflow, btnExportProfile]);
        heading.Controls.Add(profilePanel);
        panelWorkflowContainer.Controls.Add(heading);
        var toolbar = new Panel { Visible = false, Height = 0 };
        cboStepType = CreateComboBox(145, Color.FromArgb(24, 35, 51));
        cboStepType.Location = new Point(14, 5);
        cboStepType.Items.AddRange(Enum.GetValues<Models.StepType>().Select(WorkflowStep.GetTypeLabel).ToArray());
        cboStepType.SelectedIndex = 0;
        btnAddStep = CreateButton("＋ Thêm bước", Color.FromArgb(35, 75, 101), 94);
        btnAddStep.Location = new Point(166, 4);
        btnRemoveStep = CreateButton("Xóa", Color.FromArgb(67, 43, 57), 55);
        btnRemoveStep.Location = new Point(264, 4);
        btnMoveUp = CreateButton("↑", Color.FromArgb(34, 49, 70), 32);
        btnMoveUp.Location = new Point(335, 4);
        btnMoveDown = CreateButton("↓", Color.FromArgb(34, 49, 70), 32);
        btnMoveDown.Location = new Point(371, 4);
        btnSaveWorkflow = CreateButton("Xuất", Color.FromArgb(34, 49, 70), 50);
        btnSaveWorkflow.Location = new Point(410, 4);
        btnClearWorkflow = CreateButton("Làm sạch", Color.FromArgb(67, 43, 57), 68);
        btnClearWorkflow.Location = new Point(470, 4);
        btnRecordActions = CreateButton("● Ghi thao tác", Color.FromArgb(228, 240, 249), 108);
        btnRecordActions.Location = new Point(544, 4);
        btnStopRecording = CreateButton("■ Dừng ghi", Color.FromArgb(238, 225, 228), 90);
        btnStopRecording.Location = new Point(658, 4);
        btnStopRecording.Enabled = false;
        btnZoomOut = CreateButton("−", Color.FromArgb(228, 240, 249), 30);
        btnZoomOut.Location = new Point(756, 4);
        btnZoomReset = CreateButton("100%", Color.FromArgb(228, 240, 249), 52);
        btnZoomReset.Location = new Point(790, 4);
        btnZoomIn = CreateButton("+", Color.FromArgb(228, 240, 249), 30);
        btnZoomIn.Location = new Point(846, 4);
        lblZoom = new Label { Text = "Thu phóng", AutoSize = true, Location = new Point(884, 12), ForeColor = Color.FromArgb(91, 128, 157), Font = new Font("Segoe UI", 8F) };
        toolbar.Controls.AddRange([cboStepType, btnAddStep, btnRemoveStep, btnMoveUp, btnMoveDown, btnSaveWorkflow, btnClearWorkflow, btnRecordActions, btnStopRecording, btnZoomOut, btnZoomReset, btnZoomIn, lblZoom]);
        panelWorkflowContainer.Controls.Add(toolbar);
        var builderArea = new Panel { Dock = DockStyle.None, BackColor = Color.White };
        workflowBody = builderArea;
        workflowCorePalette = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 144, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false, Padding = new Padding(10, 8, 8, 6), BackColor = Color.FromArgb(248, 251, 253) };
        workflowCorePalette.Resize += (_, _) => ResizePaletteItems();
        AddPaletteSection(workflowCorePalette, "CƠ BẢN");
        AddPaletteItem(workflowCorePalette, "Bắt đầu", StepType.Start);
        AddPaletteItem(workflowCorePalette, "Kết thúc", StepType.End);
        AddPaletteItem(workflowCorePalette, "Chờ", StepType.Delay);

        workflowPalette = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, TabStop = true, Padding = new Padding(10, 8, 8, 10), BackColor = Color.FromArgb(248, 251, 253) };
        workflowPalette.HorizontalScroll.Enabled = false;
        workflowPalette.VerticalScroll.SmallChange = 28;
        workflowPalette.Resize += (_, _) => ResizePaletteItems();
        workflowPalette.HandleCreated += (_, _) => workflowPalette.BeginInvoke(new Action(() => ResetPaletteScroll()));
        AddPaletteSection(workflowPalette, "THAO TÁC ADB");
        AddPaletteItem(workflowPalette, "Chạm", StepType.Tap);
        AddPaletteItem(workflowPalette, "Chạm ngẫu nhiên", StepType.RandomTap);
        AddPaletteItem(workflowPalette, "Nhập văn bản", StepType.InputText);
        AddPaletteItem(workflowPalette, "Vuốt", StepType.Swipe);
        AddPaletteItem(workflowPalette, "Mở ứng dụng", StepType.OpenApp);
        AddPaletteItem(workflowPalette, "Phím hệ thống", StepType.KeyEvent);
        AddPaletteItem(workflowPalette, "Lệnh ADB", StepType.AdbShell);
        AddPaletteItem(workflowPalette, "Đẩy video", StepType.PushVideo);
        AddPaletteItem(workflowPalette, "Đẩy ảnh", StepType.PushImage);
        AddPaletteItem(workflowPalette, "Quét thư viện", StepType.MediaScan);
        ResizePaletteItems();
        workflowPalette.AutoScrollMinSize = new Size(0, Math.Max(700, workflowPalette.PreferredSize.Height + 16));
        workflowPalette.AutoScrollPosition = Point.Empty;
        var paletteShell = new Panel { Dock = DockStyle.Left, Width = 195, BackColor = Color.FromArgb(248, 251, 253) };
        var borderRight = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Color.FromArgb(210, 220, 230) };
        paletteShell.Controls.Add(workflowPalette);
        paletteShell.Controls.Add(workflowCorePalette);
        paletteShell.Controls.Add(borderRight);
        builderArea.Controls.Add(paletteShell);
        workflowCanvas = new WorkflowCanvas { Dock = DockStyle.Fill, Margin = new Padding(0) };
        builderArea.Controls.Add(workflowCanvas);
        paletteShell.BringToFront();
        panelWorkflowContainer.Controls.Add(builderArea);
        panelWorkflowContainer.Resize += (_, _) => LayoutWorkflowBody();
        heading.BringToFront();
        LayoutWorkflowBody();
    }

    private void LayoutWorkflowBody()
    {
        if (workflowBody == null || panelWorkflowContainer == null) return;
        var top = panelWorkflowContainer.Controls
            .Cast<Control>()
            .Where(control => control != workflowBody && control.Dock == DockStyle.Top)
            .Sum(control => control.Height);
        workflowBody.SetBounds(
            0,
            top,
            panelWorkflowContainer.ClientSize.Width,
            Math.Max(0, panelWorkflowContainer.ClientSize.Height - top));
        workflowBody.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    }

    private void BuildInspectorPanel()
    {
        var inspectorHeading = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            Padding = new Padding(0),
            BackColor = Color.White
        };
        var lblInspectorTitle = new Label
        {
            Text = "CẤU HÌNH BƯỚC",
            AutoSize = true,
            Location = new Point(18, 18),
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = Color.FromArgb(31, 31, 44)
        };
        lblSelectedStep = new Label
        {
            Text = "Chưa chọn bước",
            AutoSize = true,
            Location = new Point(18, 44),
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(96, 82, 218)
        };
        inspectorHeading.Controls.Add(lblInspectorTitle);
        inspectorHeading.Controls.Add(lblSelectedStep);

        var inspectorBody = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        
        inspectorFields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 11, Padding = new Padding(14, 4, 14, 10) };
        inspectorFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        inspectorFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        AddField(inspectorFields, "Tọa độ X", out txtX, 0);
        AddField(inspectorFields, "Tọa độ Y", out txtY, 1);
        AddField(inspectorFields, "Tọa độ X2", out txtX2, 2);
        AddField(inspectorFields, "Tọa độ Y2", out txtY2, 3);
        AddField(inspectorFields, "Nội dung / App", out txtTextValue, 4);
        AddField(inspectorFields, "Cột dữ liệu", out txtBindingCol, 5);
        txtBindingCol.PlaceholderText = "Tên cột Excel, ví dụ: Description";
        AddField(inspectorFields, "Độ trễ (ms)", out txtDelayAfter, 6);
        AddField(inspectorFields, "Mô tả", out txtDescription, 7);
        inspectorFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        chkUseAiForText = new CheckBox
        {
            Text = "Sử dụng AI sinh nội dung (yêu cầu cấu hình AI)",
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(83, 111, 140),
            Font = new Font("Segoe UI", 8.5F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 4)
        };
        inspectorFields.Controls.Add(chkUseAiForText, 1, 8);

        inspectorFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        chkTakeAllLinks = new CheckBox
        {
            Text = "Nhập tất cả link (bỏ chọn: chỉ lấy 1 link)",
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(83, 111, 140),
            Font = new Font("Segoe UI", 8.5F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 4)
        };
        inspectorFields.Controls.Add(chkTakeAllLinks, 1, 9);

        inspectorFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        btnApplyConfig = CreateButton("✓ Lưu thay đổi", Color.FromArgb(0, 161, 112), 0);
        btnApplyConfig.Dock = DockStyle.Fill;
        btnApplyConfig.Margin = new Padding(0, 8, 0, 4);
        inspectorFields.Controls.Add(btnApplyConfig, 1, 10);

        actionOptionsPanel = new Panel { Dock = DockStyle.Top, Height = 270, Padding = new Padding(14, 10, 14, 10), BackColor = Color.White, Visible = false };
        var actionOptionsTitle = new Label { Text = "CẤU HÌNH NHANH", Dock = DockStyle.Top, Height = 24, ForeColor = Color.FromArgb(8, 132, 191), Font = new Font("Segoe UI Semibold", 8F) };
        
        videoOptionsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(0, 4, 0, 0) };
        videoOptionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        videoOptionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        videoOptionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        videoOptionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        videoOptionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        videoOptionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        lblVideoSource = new Label { Text = "Nguồn video", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) };
        videoOptionsPanel.Controls.Add(lblVideoSource, 0, 0);
        cboVideoSource = CreateComboBox(0, Color.White);
        cboVideoSource.Dock = DockStyle.Fill;
        cboVideoSource.Cursor = Cursors.Hand;
        cboVideoSource.Margin = new Padding(0, 2, 0, 2);
        cboVideoSource.Items.AddRange([new VideoSourceChoice(VideoSourceMode.ExcelPath), new VideoSourceChoice(VideoSourceMode.FolderAndExcelFileName), new VideoSourceChoice(VideoSourceMode.FixedFile)]);
        cboVideoSource.SelectedIndex = 0;
        videoOptionsPanel.Controls.Add(cboVideoSource, 1, 0);
        videoOptionsPanel.Controls.Add(new Label { Text = "Đường dẫn", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 1);
        var videoPathPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 2) };
        txtVideoPath = CreateTextBox();
        txtVideoPath.Dock = DockStyle.Fill;
        btnBrowseVideoPath = CreateButton("Mở", Color.FromArgb(228, 240, 249), 46);
        btnBrowseVideoPath.Dock = DockStyle.Right;
        videoPathPanel.Controls.Add(txtVideoPath);
        videoPathPanel.Controls.Add(btnBrowseVideoPath);
        videoOptionsPanel.Controls.Add(videoPathPanel, 1, 1);
        chkDeleteLocalVideo = new CheckBox
        {
            Text = "Xóa file nguồn trên máy sau khi workflow thành công",
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(83, 111, 140),
            Font = new Font("Segoe UI", 8.5F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 0)
        };
        videoOptionsPanel.Controls.Add(chkDeleteLocalVideo, 0, 2);
        videoOptionsPanel.SetColumnSpan(chkDeleteLocalVideo, 2);
        chkClearDeviceVideos = new CheckBox
        {
            Text = "Xóa video FlowPilot cũ trên Android trước khi đẩy",
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(83, 111, 140),
            Font = new Font("Segoe UI", 8.5F),
            Cursor = Cursors.Hand,
            Checked = true,
            Margin = new Padding(0, 2, 0, 0)
        };
        videoOptionsPanel.Controls.Add(chkClearDeviceVideos, 0, 3);
        videoOptionsPanel.SetColumnSpan(chkClearDeviceVideos, 2);
        actionOptionsPanel.Controls.Add(videoOptionsPanel);

        appOptionsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(0, 4, 0, 0), Visible = false };
        appOptionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        appOptionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        appOptionsPanel.Controls.Add(new Label { Text = "Ứng dụng", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 0);
        var appPackagePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 2) };
        cboAppPackage = CreateComboBox(0, Color.FromArgb(244, 248, 252));
        cboAppPackage.DropDownStyle = ComboBoxStyle.DropDown;
        cboAppPackage.Dock = DockStyle.Fill;
        btnRefreshApps = CreateButton("»", Color.FromArgb(228, 240, 249), 32);
        btnRefreshApps.Dock = DockStyle.Right;
        btnCaptureCurrentApp = CreateButton("Lấy app", Color.FromArgb(228, 240, 249), 62);
        btnCaptureCurrentApp.Dock = DockStyle.Right;
        appPackagePanel.Controls.Add(cboAppPackage);
        appPackagePanel.Controls.Add(btnCaptureCurrentApp);
        appPackagePanel.Controls.Add(btnRefreshApps);
        appOptionsPanel.Controls.Add(appPackagePanel, 1, 0);

        chkSkipFromSecondJob = new CheckBox
        {
            Text = "Bỏ qua từ lần chạy thứ 2 (chỉ mở 1 lần đầu)",
            Dock = DockStyle.Fill,
            AutoSize = true,
            ForeColor = Color.FromArgb(83, 111, 140),
            Font = new Font("Segoe UI", 8.5F)
        };
        appOptionsPanel.Controls.Add(chkSkipFromSecondJob, 0, 1);
        appOptionsPanel.SetColumnSpan(chkSkipFromSecondJob, 2);

        actionOptionsPanel.Controls.Add(appOptionsPanel);

        tapOptionsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0), Visible = false };
        btnCaptureTapCoordinates = CreateButton("🎯 Lấy tọa độ từ điện thoại", Color.FromArgb(35, 149, 218), 0);
        btnCaptureTapCoordinates.Dock = DockStyle.Bottom;
        btnCaptureTapCoordinates.Height = 28;
        tapOptionsPanel.Controls.Add(btnCaptureTapCoordinates);
        
        swipeOptionsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0), Visible = false };
        btnCaptureSwipeCoordinates = CreateButton("🎯 Lấy tọa độ vuốt từ điện thoại", Color.FromArgb(35, 149, 218), 0);
        btnCaptureSwipeCoordinates.Dock = DockStyle.Bottom;
        btnCaptureSwipeCoordinates.Height = 28;
        swipeOptionsPanel.Controls.Add(btnCaptureSwipeCoordinates);
        

        var tapGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(0, 2, 0, 4) };
        tapGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        tapGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        for (var row = 0; row < 5; row++) tapGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        tapGrid.Controls.Add(new Label { Text = "Kiểu chạm", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 0);
        cboTapMode = CreateComboBox(0, Color.White);
        cboTapMode.Dock = DockStyle.Fill;
        cboTapMode.Items.AddRange([
            new TapModeChoice(TapMode.Coordinates),
            new TapModeChoice(TapMode.XPath),
            new TapModeChoice(TapMode.Image)
        ]);
        cboTapMode.SelectedIndex = 0;
        cboTapMode.SelectedIndexChanged += (_, _) => UpdateTapModeFields();
        tapGrid.Controls.Add(cboTapMode, 1, 0);
        tapGrid.Controls.Add(new Label { Text = "XPath", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 1);
        var tapXpathPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 2) };
        txtTapXPath = CreateTextBox();
        txtTapXPath.Dock = DockStyle.Fill;
        txtTapXPath.PlaceholderText = "//node[@text='...']";
        btnInspectUi = CreateButton("🔍 Quét UI", Color.FromArgb(228, 240, 249), 70);
        btnInspectUi.Dock = DockStyle.Right;
        tapXpathPanel.Controls.Add(txtTapXPath);
        tapXpathPanel.Controls.Add(btnInspectUi);
        tapGrid.Controls.Add(tapXpathPanel, 1, 1);

        lblTapMultiMode = new Label { Text = "Số lượng chạm", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) };
        tapGrid.Controls.Add(lblTapMultiMode, 0, 2);
        cboTapMultiMode = CreateComboBox(0, Color.White);
        cboTapMultiMode.Dock = DockStyle.Fill;
        cboTapMultiMode.Items.AddRange([
            new TapMultiModeChoice(TapMultiMode.Single),
            new TapMultiModeChoice(TapMultiMode.ByImageCount),
            new TapMultiModeChoice(TapMultiMode.All),
            new TapMultiModeChoice(TapMultiMode.CustomCount)
        ]);
        cboTapMultiMode.SelectedIndex = 0;
        tapGrid.Controls.Add(cboTapMultiMode, 1, 2);

        lblMultiTapDelay = new Label { Text = "Nghỉ giữa chạm", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) };
        tapGrid.Controls.Add(lblMultiTapDelay, 0, 3);
        var tapDelayPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 2) };
        numMultiTapDelay = new Guna.UI2.WinForms.Guna2NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 50,
            Maximum = 10000,
            Value = 250,
            Increment = 50,
            Font = new Font("Segoe UI", 8.5F),
            BorderRadius = 4
        };
        var lblMs = new Label { Text = "ms", Dock = DockStyle.Right, Width = 30, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(120, 130, 150), Font = new Font("Segoe UI", 8F) };
        tapDelayPanel.Controls.Add(numMultiTapDelay);
        tapDelayPanel.Controls.Add(lblMs);
        tapGrid.Controls.Add(tapDelayPanel, 1, 3);

        tapGrid.Controls.Add(new Label { Text = "Ảnh mẫu", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 4);
        var tapImagePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 2) };
        txtTapImagePath = CreateTextBox();
        txtTapImagePath.Dock = DockStyle.Fill;
        btnBrowseTapImage = CreateButton("Mở", Color.FromArgb(228, 240, 249), 46);
        btnBrowseTapImage.Dock = DockStyle.Right;
        tapImagePanel.Controls.Add(txtTapImagePath);
        tapImagePanel.Controls.Add(btnBrowseTapImage);
        tapGrid.Controls.Add(tapImagePanel, 1, 4);
        tapOptionsPanel.Controls.Add(tapGrid);

        randomTapOptionsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0), Visible = false };
        var lblRandomGroupTitle = new Label 
        { 
            Text = "Danh sách tọa độ (chọn ngẫu nhiên 1 khi chạy):", 
            Dock = DockStyle.Top, 
            Height = 22, 
            ForeColor = Color.FromArgb(83, 111, 140), 
            Font = new Font("Segoe UI Semibold", 8F) 
        };
        lstRandomCoordinates = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F),
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            ItemHeight = 22
        };
        var randomBtnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };
        btnCaptureRandomTapCoordinate = CreateButton("🎯 Lấy tọa độ từ ĐT", Color.FromArgb(35, 149, 218), 125);
        btnCaptureRandomTapCoordinate.Height = 28;
        btnAddManualCoordinate = CreateButton("＋ Thêm (X,Y)", Color.FromArgb(16, 185, 129), 92);
        btnAddManualCoordinate.ForeColor = Color.White;
        btnAddManualCoordinate.Height = 28;
        btnRemoveRandomCoordinate = CreateButton("🗑️ Xóa", Color.FromArgb(239, 68, 68), 58);
        btnRemoveRandomCoordinate.ForeColor = Color.White;
        btnRemoveRandomCoordinate.Height = 28;
        btnClearRandomCoordinates = CreateButton("Xóa hết", Color.FromArgb(243, 244, 246), 65);
        btnClearRandomCoordinates.ForeColor = Color.FromArgb(55, 65, 81);
        btnClearRandomCoordinates.Height = 28;
        randomBtnRow.Controls.AddRange([btnCaptureRandomTapCoordinate, btnAddManualCoordinate, btnRemoveRandomCoordinate, btnClearRandomCoordinates]);
        randomTapOptionsPanel.Controls.Add(lstRandomCoordinates);
        randomTapOptionsPanel.Controls.Add(lblRandomGroupTitle);
        randomTapOptionsPanel.Controls.Add(randomBtnRow);

        actionOptionsPanel.Controls.Add(tapOptionsPanel);
        actionOptionsPanel.Controls.Add(randomTapOptionsPanel);
        actionOptionsPanel.Controls.Add(swipeOptionsPanel);
        actionOptionsPanel.Controls.Add(actionOptionsTitle);

        var variablesPanel = new Panel { Dock = DockStyle.Bottom, Height = 200, Padding = new Padding(14, 10, 14, 12), BackColor = Color.FromArgb(250, 252, 254) };
        var variablesHeader = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Color.Transparent };
        var variablesTitle = new Label { Text = "BIẾN DỮ LIỆU", AutoSize = true, Location = new Point(0, 4), ForeColor = Color.FromArgb(96, 82, 218), Font = new Font("Segoe UI Semibold", 8.5F) };
        var variablesHint = new Label { Text = "(Nhấp đúp để chèn)", AutoSize = true, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(140, 145, 165), Font = new Font("Segoe UI", 8F) };
        variablesHeader.Controls.AddRange([variablesHint, variablesTitle]);
        variablesPanel.Controls.Add(variablesHeader);

        lstVariables = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 31, 44),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F),
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 24
        };
        lstVariables.DrawItem += (s, e) =>
        {
            if (e.Index < 0 || e.Index >= lstVariables.Items.Count) return;
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using var bgBrush = new SolidBrush(isSelected ? Color.FromArgb(224, 231, 255) : (e.Index % 2 == 0 ? Color.White : Color.FromArgb(250, 251, 253)));
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            if (lstVariables.Items[e.Index] is WorkflowVariable v)
            {
                using var fontBold = new Font("Segoe UI Semibold", 8.5F);
                using var tokenBrush = new SolidBrush(isSelected ? Color.FromArgb(79, 70, 229) : Color.FromArgb(96, 82, 218));
                var tokenText = "{" + v.Name + "}";
                e.Graphics.DrawString(tokenText, fontBold, tokenBrush, e.Bounds.X + 6, e.Bounds.Y + 3);

                var tokenWidth = (int)e.Graphics.MeasureString(tokenText, fontBold).Width;

                using var fontReg = new Font("Segoe UI", 8.5F);
                using var valBrush = new SolidBrush(isSelected ? Color.FromArgb(55, 65, 81) : Color.FromArgb(120, 125, 140));
                var valText = $" : {v.Value}";
                e.Graphics.DrawString(valText, fontReg, valBrush, e.Bounds.X + 6 + tokenWidth, e.Bounds.Y + 3);
            }
            else
            {
                using var fontReg = new Font("Segoe UI", 8.5F);
                using var textBrush = new SolidBrush(Color.FromArgb(31, 31, 44));
                e.Graphics.DrawString(lstVariables.Items[e.Index]?.ToString() ?? "", fontReg, textBrush, e.Bounds.X + 6, e.Bounds.Y + 3);
            }
        };
        variablesPanel.Controls.Add(lstVariables);

        txtVariableName = CreateTextBox();
        txtVariableValue = CreateTextBox();
        btnAddVariable = new Control();
        btnRemoveVariable = new Control();
        btnInsertVariable = new Control();

        inspectorBody.Controls.Add(inspectorFields);
        inspectorBody.Controls.Add(actionOptionsPanel);
        inspectorBody.Controls.Add(variablesPanel);

        panelInspector.Controls.Add(inspectorBody);
        panelInspector.Controls.Add(inspectorHeading);
    }

    private void BuildBottomPanel(TableLayoutPanel parent)
    {
        panelBottom = CreateCardPanel();
        panelBottom.Padding = new Padding(6);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.FlatButtons,
            ItemSize = new Size(185, 32),
            SizeMode = TabSizeMode.Fixed,
            Font = new Font("Segoe UI Semibold", 9F)
        };

        var jobsTab = new TabPage("📋  HÀNG ĐỢI CÔNG VIỆC") { BackColor = Color.White, Padding = new Padding(8, 6, 8, 6) };
        var logTab = new TabPage("📜  NHẬT KÝ HOẠT ĐỘNG") { BackColor = Color.White, Padding = new Padding(8, 6, 8, 6) };

        var jobToolbar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White };
        btnImportExcel = CreateButton("📊 Nhập Excel", Color.FromArgb(96, 82, 218), 120);
        btnImportExcel.ForeColor = Color.White;
        if (btnImportExcel is Guna.UI2.WinForms.Guna2Button bImport)
        {
            bImport.FillColor = Color.FromArgb(96, 82, 218);
            bImport.BorderRadius = 6;
            bImport.Height = 32;
            bImport.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnExportTemplate = CreateButton("📋 Tải mẫu Excel", Color.FromArgb(243, 244, 246), 125);
        btnExportTemplate.ForeColor = Color.FromArgb(55, 65, 81);
        if (btnExportTemplate is Guna.UI2.WinForms.Guna2Button bTemplate)
        {
            bTemplate.FillColor = Color.FromArgb(243, 244, 246);
            bTemplate.BorderColor = Color.FromArgb(209, 213, 219);
            bTemplate.BorderThickness = 1;
            bTemplate.BorderRadius = 6;
            bTemplate.Height = 32;
            bTemplate.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnExportResult = CreateButton("📤 Xuất kết quả", Color.FromArgb(238, 242, 255), 125);
        btnExportResult.ForeColor = Color.FromArgb(79, 70, 229);
        if (btnExportResult is Guna.UI2.WinForms.Guna2Button bResult)
        {
            bResult.FillColor = Color.FromArgb(238, 242, 255);
            bResult.BorderColor = Color.FromArgb(199, 210, 254);
            bResult.BorderThickness = 1;
            bResult.BorderRadius = 6;
            bResult.Height = 32;
            bResult.Font = new Font("Segoe UI Semibold", 9F);
        }

        btnImportExcel.Location = new Point(0, 3);
        btnExportTemplate.Location = new Point(128, 3);
        btnExportResult.Location = new Point(261, 3);
        jobToolbar.Controls.AddRange([btnImportExcel, btnExportTemplate, btnExportResult]);
        jobsTab.Controls.Add(jobToolbar);

        dgvJobs = new Guna.UI2.WinForms.Guna2DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(240, 242, 245),
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false
        };
        dgvJobs.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 249, 251),
            ForeColor = Color.FromArgb(75, 85, 99),
            Font = new Font("Segoe UI Semibold", 9F),
            Padding = new Padding(8, 6, 8, 6)
        };
        dgvJobs.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 31, 44),
            SelectionBackColor = Color.FromArgb(238, 242, 255),
            SelectionForeColor = Color.FromArgb(31, 31, 44),
            Padding = new Padding(8, 4, 8, 4),
            Font = new Font("Segoe UI", 9F)
        };
        dgvJobs.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 251, 253),
            ForeColor = Color.FromArgb(31, 31, 44),
            SelectionBackColor = Color.FromArgb(238, 242, 255),
            SelectionForeColor = Color.FromArgb(31, 31, 44),
            Padding = new Padding(8, 4, 8, 4),
            Font = new Font("Segoe UI", 9F)
        };

        AddColumn("STT", "colId", 50);
        AddColumn("Đường dẫn video", "colVideo", 320);
        AddColumn("Tiêu đề", "colTitle", 350);
        AddColumn("Liên kết tiếp thị", "colLink", 260);
        AddColumn("Trạng thái", "colStatus", 110);
        AddColumn("Trạng thái Shopee", "colShopeeStatus", 140);
        AddColumn("Trạng thái FB", "colFbStatus", 140);
        AddColumn("Nhật ký", "colLog", 300);

        jobsTab.Controls.Add(dgvJobs);
        jobToolbar.BringToFront();
        tabs.TabPages.Add(jobsTab);

        txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(248, 251, 253),
            ForeColor = Color.FromArgb(49, 95, 125),
            BorderStyle = BorderStyle.None,
            Font = new Font("Cascadia Mono", 9F),
            DetectUrls = false
        };
        logTab.Controls.Add(txtLog);
        tabs.TabPages.Add(logTab);
        panelBottom.Controls.Add(tabs);

        chkOnlyWithLink = new Guna.UI2.WinForms.Guna2CheckBox
        {
            Text = "Chỉ up video có link",
            Location = new Point(390, 8),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(79, 70, 229),
            Cursor = Cursors.Hand,
            Checked = false,
            BackColor = Color.White
        };
        chkOnlyWithLink.CheckedState.FillColor = Color.FromArgb(79, 70, 229);
        chkOnlyWithLink.CheckedState.BorderColor = Color.FromArgb(79, 70, 229);
        chkOnlyWithLink.CheckedState.BorderRadius = 4;
        chkOnlyWithLink.UncheckedState.BorderColor = Color.FromArgb(180, 185, 200);
        chkOnlyWithLink.UncheckedState.BorderRadius = 4;
        new ToolTip().SetToolTip(chkOnlyWithLink, "Khi chọn: Chỉ hiển thị và chỉ tải lên các video có link tiếp thị (bỏ qua video không có link).");
        panelBottom.Controls.Add(chkOnlyWithLink);
        chkOnlyWithLink.BringToFront();
        panelBottom.Resize += (_, _) => chkOnlyWithLink.BringToFront();
        tabs.SelectedIndexChanged += (_, _) => chkOnlyWithLink.BringToFront();

        parent.Controls.Add(panelBottom, 0, 1);
    }

    private void AddColumn(string header, string name, int width)
    {
        dgvJobs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, Name = name, Width = width, AutoSizeMode = width == 260 ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None });
    }

    private static Guna.UI2.WinForms.Guna2Panel CreateCardPanel() => new() { Dock = DockStyle.Fill, FillColor = Color.White, BorderRadius = 8, Margin = new Padding(5), Padding = new Padding(1) };

    private static Panel CreateHeading(string title, string subtitle)
    {
        var panel = CreateHeading(title, subtitle, out var count);
        count.Visible = false;
        return panel;
    }

    private static Panel CreateHeading(string title, string subtitle, out Label count)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 74, Padding = new Padding(0), BackColor = Color.White };
        var titleLabel = new Label { Text = title, AutoSize = true, Location = new Point(18, 18), Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = Color.FromArgb(31, 31, 44) };
        var subLabel = new Label { Text = subtitle, AutoSize = true, Location = new Point(19, 44), Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(114, 117, 134) };
        count = new Label { Text = "0 BƯỚC", AutoSize = true, Location = new Point(180, 17), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(96, 82, 218), BackColor = Color.FromArgb(238, 242, 255), Padding = new Padding(6, 2, 6, 2) };
        panel.Controls.Add(count);
        panel.Controls.Add(subLabel);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private static void AddField(TableLayoutPanel table, string label, out Guna.UI2.WinForms.Guna2TextBox textBox, int row)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, row);
        textBox = CreateTextBox();
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(0, 4, 0, 4);
        table.Controls.Add(textBox, 1, row);
    }

    private static void AddPaletteSection(FlowLayoutPanel palette, string title)
    {
        palette.Controls.Add(new Label
        {
            Text = title,
            Width = 158,
            Height = 23,
            Margin = new Padding(2, 6, 0, 2),
            ForeColor = Color.FromArgb(99, 128, 153),
            Font = new Font("Segoe UI Semibold", 7.5F)
        });
    }

    private static void AddPaletteItem(FlowLayoutPanel palette, string text, StepType type)
    {
        var button = new Button
        {
            Text = $"＋  {text}",
            Tag = type,
            Width = 158,
            Height = 30,
            Margin = new Padding(0, 2, 0, 2),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = Color.White,
            ForeColor = Color.FromArgb(35, 87, 123),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5F),
            Cursor = Cursors.Hand
        };
        ApplyRoundedCorners(button, 7);
        palette.Controls.Add(button);
    }

    private void ResizePaletteItems()
    {
        ResizePaletteItems(workflowCorePalette);
        ResizePaletteItems(workflowPalette);
    }

    private static void ResizePaletteItems(FlowLayoutPanel palette)
    {
        if (palette == null) return;
        var width = Math.Max(120, palette.ClientSize.Width - palette.Padding.Horizontal - 2);
        foreach (Control control in palette.Controls)
            if (control is Button or Label)
                control.Width = width;
    }

    private void ResetPaletteScroll()
    {
        if (workflowPalette == null || workflowPalette.IsDisposed || !workflowPalette.IsHandleCreated) return;
        workflowPalette.AutoScrollPosition = Point.Empty;
        if (workflowPalette.VerticalScroll.Visible)
            workflowPalette.VerticalScroll.Value = workflowPalette.VerticalScroll.Minimum;
        workflowPalette.ScrollControlIntoView(workflowPalette.Controls.Count > 0 ? workflowPalette.Controls[0] : workflowPalette);
        workflowPalette.AutoScrollPosition = Point.Empty;
    }

    private static Control CreateButton(string text, Color color, int width)
    {
        var button = new Guna.UI2.WinForms.Guna2Button
        {
            Text = text,
            Width = width,
            Height = 34,
            FillColor = color,
            BorderRadius = 7,
            BorderThickness = 0,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Margin = new Padding(3, 0, 3, 0),
            Cursor = Cursors.Hand
        };
        return button;
    }

    private static Guna.UI2.WinForms.Guna2TextBox CreateTextBox() => new()
    {
        BackColor = Color.White,
        BorderRadius = 7,
        BorderThickness = 1,
        BorderColor = Color.FromArgb(190, 198, 211),
        FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
        Font = new Font("Segoe UI", 9F),
        Padding = new Padding(10, 0, 10, 0)
    };

    private static Guna.UI2.WinForms.Guna2ComboBox CreateComboBox(int width, Color fillColor) => new()
    {
        Width = width,
        Height = 34,
        DropDownStyle = ComboBoxStyle.DropDownList,
        FillColor = fillColor,
        ForeColor = Color.FromArgb(27, 55, 82),
        BorderRadius = 7,
        BorderThickness = 1,
        BorderColor = Color.FromArgb(190, 198, 211),
        FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
        Font = new Font("Segoe UI", 8.5F),
        Cursor = Cursors.Hand
    };

    private static void ApplyRoundedCorners(Control control, int radius)
    {
        void UpdateRegion()
        {
            if (control.IsDisposed || control.Width <= 0 || control.Height <= 0) return;

            var bounds = new Rectangle(0, 0, control.Width, control.Height);
            var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            var oldRegion = control.Region;
            control.Region = new Region(path);
            oldRegion?.Dispose();
        }

        control.Resize += (_, _) => UpdateRegion();
        UpdateRegion();
    }

    private static Button CreateNavButton(string text, bool active)
    {
        var primary = Color.FromArgb(79, 70, 229);
        var color = active ? primary : Color.Transparent;
        var button = new Button { Text = text, Width = 206, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = active ? Color.White : Color.FromArgb(71, 85, 105), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 9.5F), Padding = new Padding(14, 0, 0, 0), Cursor = Cursors.Hand, Margin = new Padding(0, 2, 0, 2) };
        button.FlatAppearance.BorderSize = 0;
        ApplyRoundedCorners(button, 8);
        button.MouseEnter += (_, _) => { if (button.BackColor != primary) button.BackColor = Color.FromArgb(241, 245, 249); };
        button.MouseLeave += (_, _) => { if (button.BackColor != primary) button.BackColor = Color.Transparent; };
        return button;
    }

    private void BuildWorkflowPhonePanel()
    {
        topBarWorkflowPhone = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 4) };
        var lblTitle = new Label
        {
            Text = "📱 Điện thoại",
            AutoSize = true,
            Location = new Point(4, 7),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55)
        };
        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        btnWfPhoneHome = CreateIconButton("🏠", "Về màn hình chính (Home)");
        btnWfPhoneBack = CreateIconButton("⬅", "Quay lại (Back)");
        btnWfPhonePower = CreateIconButton("⏻", "Bật/Tắt màn hình (Power)");
        btnWfPhoneRestart = CreateIconButton("🔄", "Khởi động lại Scrcpy");
        btnWfPhoneClose = CreateIconButton("✕", "Ẩn màn hình điện thoại");
        btnWfPhoneClose.ForeColor = Color.FromArgb(239, 68, 68);

        actionsPanel.Controls.AddRange([btnWfPhoneHome, btnWfPhoneBack, btnWfPhonePower, btnWfPhoneRestart, btnWfPhoneClose]);
        topBarWorkflowPhone.Controls.Add(actionsPanel);
        topBarWorkflowPhone.Controls.Add(lblTitle);

        pnlWorkflowScrcpyHost = new Panel
        {
            BackColor = Color.Black,
            Margin = new Padding(0)
        };

        panelWorkflowPhone.Controls.Add(pnlWorkflowScrcpyHost);
        panelWorkflowPhone.Controls.Add(topBarWorkflowPhone);
        panelWorkflowPhone.Resize += (_, _) => LayoutWorkflowPhone();
    }

    private void BuildDevicesPanel()
    {
        panelDevicesView = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(246, 250, 254),
            Visible = false,
            Padding = new Padding(18, 14, 18, 14),
            AutoScroll = true
        };

        var heading = CreateHeading("QUẢN LÝ THIẾT BỊ & TRUYỀN HÌNH ẢNH (SCRCPY)", "Xem màn hình điện thoại thời gian thực · điều khiển phím cứng · kết nối Wi-Fi ADB");
        panelDevicesView.Controls.Add(heading);

        var bodyLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0)
        };
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400F)); // Left: Phone Mockup
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Right: Controls & Info

        // Left: Phone Mirroring Card
        var phoneCard = CreateCardPanel();
        phoneCard.Dock = DockStyle.Fill;
        phoneCard.Padding = new Padding(12);

        var phoneTopBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
        lblDevicesPhoneTitle = new Label
        {
            Text = "📱 Màn hình điện thoại",
            AutoSize = true,
            Location = new Point(4, 10),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        lblDevicesPhoneStatus = new Label
        {
            Text = "● Đang chờ",
            Dock = DockStyle.Right,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.FromArgb(217, 119, 6),
            Padding = new Padding(0, 10, 4, 0)
        };
        phoneTopBar.Controls.Add(lblDevicesPhoneStatus);
        phoneTopBar.Controls.Add(lblDevicesPhoneTitle);

        var phoneToolBar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(2) };
        var flpPhoneActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        btnDevPhonePower = CreateIconButton("⏻", "Nguồn / Bật tắt màn hình");
        btnDevPhoneHome = CreateIconButton("🏠", "Màn hình chính (Home)");
        btnDevPhoneBack = CreateIconButton("⬅", "Quay lại (Back)");
        btnDevPhoneRecents = CreateIconButton("📋", "Ứng dụng gần đây (Recents)");
        btnDevPhoneVolUp = CreateIconButton("🔊", "Tăng âm lượng");
        btnDevPhoneVolDown = CreateIconButton("🔉", "Giảm âm lượng");
        btnDevPhoneScreenshot = CreateIconButton("📸", "Chụp ảnh màn hình");

        flpPhoneActions.Controls.AddRange([btnDevPhonePower, btnDevPhoneHome, btnDevPhoneBack, btnDevPhoneRecents, btnDevPhoneVolUp, btnDevPhoneVolDown, btnDevPhoneScreenshot]);
        phoneToolBar.Controls.Add(flpPhoneActions);

        phoneCardDevices = phoneCard;
        phoneToolBarDevices = phoneToolBar;

        pnlDevicesScrcpyHost = new Panel
        {
            BackColor = Color.Black,
            Margin = new Padding(0)
        };

        var phoneBottomBar = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        phoneBottomBarDevices = phoneBottomBar;
        btnDevicesStartScrcpy = CreateButton("⚡ Bật Scrcpy", Color.FromArgb(79, 70, 229), 115);
        btnDevicesStartScrcpy.ForeColor = Color.White;
        btnDevicesStopScrcpy = CreateButton("⏹ Dừng Scrcpy", Color.FromArgb(239, 68, 68), 115);
        btnDevicesStopScrcpy.ForeColor = Color.White;
        var flpBottomActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        flpBottomActions.Controls.AddRange([btnDevicesStartScrcpy, btnDevicesStopScrcpy]);
        phoneBottomBar.Controls.Add(flpBottomActions);

        phoneCard.Controls.Add(pnlDevicesScrcpyHost);
        phoneCard.Controls.Add(phoneBottomBar);
        phoneCard.Controls.Add(phoneToolBar);
        phoneCard.Controls.Add(phoneTopBar);
        phoneCard.Resize += (_, _) => LayoutDevicesPhone();

        bodyLayout.Controls.Add(phoneCard, 0, 0);

        // Right: Device Info & Advanced Tools Card
        var rightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 0, 0, 0)
        };

        // Right Card 1: Device Information
        var cardInfo = CreateCardPanel();
        cardInfo.Width = 650;
        cardInfo.Height = 175;
        cardInfo.Padding = new Padding(16, 12, 16, 12);
        var lblInfoHead = new Label { Text = "THÔNG TIN THIẾT BỊ ĐANG KẾT NỐI", Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(79, 70, 229), Dock = DockStyle.Top, Height = 24 };
        var tblInfoGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3 };
        tblInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        tblInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tblInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        tblInfoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        lblDeviceInfoModel = new Label { Text = "Chưa kết nối", AutoSize = true, Font = new Font("Segoe UI Semibold", 9F) };
        lblDeviceInfoSerial = new Label { Text = "N/A", AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
        lblDeviceInfoRes = new Label { Text = "N/A", AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
        lblDeviceInfoAndroid = new Label { Text = "N/A", AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
        lblDeviceInfoStatus = new Label { Text = "Chưa kết nối", AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(146, 64, 14) };

        tblInfoGrid.Controls.Add(new Label { Text = "Thiết bị:", ForeColor = Color.Gray }, 0, 0);
        tblInfoGrid.Controls.Add(lblDeviceInfoModel, 1, 0);
        tblInfoGrid.Controls.Add(new Label { Text = "Serial / IP:", ForeColor = Color.Gray }, 2, 0);
        tblInfoGrid.Controls.Add(lblDeviceInfoSerial, 3, 0);

        tblInfoGrid.Controls.Add(new Label { Text = "Độ phân giải:", ForeColor = Color.Gray }, 0, 1);
        tblInfoGrid.Controls.Add(lblDeviceInfoRes, 1, 1);
        tblInfoGrid.Controls.Add(new Label { Text = "Hệ điều hành:", ForeColor = Color.Gray }, 2, 1);
        tblInfoGrid.Controls.Add(lblDeviceInfoAndroid, 3, 1);

        tblInfoGrid.Controls.Add(new Label { Text = "Trạng thái:", ForeColor = Color.Gray }, 0, 2);
        tblInfoGrid.Controls.Add(lblDeviceInfoStatus, 1, 2);

        cardInfo.Controls.Add(tblInfoGrid);
        cardInfo.Controls.Add(lblInfoHead);
        rightPanel.Controls.Add(cardInfo);

        // Right Card 2: App Launchers & Quick Actions
        var cardApps = CreateCardPanel();
        cardApps.Width = 650;
        cardApps.Height = 135;
        cardApps.Margin = new Padding(0, 12, 0, 0);
        cardApps.Padding = new Padding(16, 12, 16, 12);
        var lblAppsHead = new Label { Text = "MỞ NHANH ỨNG DỤNG & CÔNG CỤ", Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 24 };
        var flpApps = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

        btnOpenShopee = CreateButton("🟠 Shopee", Color.FromArgb(238, 77, 45), 105);
        btnOpenShopee.ForeColor = Color.White;
        btnOpenTikTok = CreateButton("⚫ TikTok", Color.FromArgb(18, 18, 18), 100);
        btnOpenTikTok.ForeColor = Color.White;
        btnOpenFacebook = CreateButton("🔵 Facebook", Color.FromArgb(24, 119, 242), 110);
        btnOpenFacebook.ForeColor = Color.White;
        btnOpenSettings = CreateButton("⚙️ Cài đặt máy", Color.FromArgb(243, 244, 246), 115);
        btnOpenSettings.ForeColor = Color.FromArgb(55, 65, 81);
        btnMediaScan = CreateButton("🧹 Quét Media", Color.FromArgb(243, 244, 246), 110);
        btnMediaScan.ForeColor = Color.FromArgb(55, 65, 81);
        btnRestartAdb = CreateButton("🔄 Reset ADB", Color.FromArgb(243, 244, 246), 105);
        btnRestartAdb.ForeColor = Color.FromArgb(55, 65, 81);

        flpApps.Controls.AddRange([btnOpenShopee, btnOpenTikTok, btnOpenFacebook, btnOpenSettings, btnMediaScan, btnRestartAdb]);
        cardApps.Controls.Add(flpApps);
        cardApps.Controls.Add(lblAppsHead);
        rightPanel.Controls.Add(cardApps);

        // Right Card 3: Wi-Fi ADB Wireless Connection
        var cardWifi = CreateCardPanel();
        cardWifi.Width = 650;
        cardWifi.Height = 145;
        cardWifi.Margin = new Padding(0, 12, 0, 0);
        cardWifi.Padding = new Padding(16, 12, 16, 12);
        var lblWifiHead = new Label { Text = "KẾT NỐI ADB KHÔNG DÂY (WI-FI)", Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Dock = DockStyle.Top, Height = 24 };
        var lblWifiSub = new Label { Text = "Nhập IP và Cổng trên điện thoại (Cài đặt -> Tùy chọn nhà phát triển -> Gỡ lỗi không dây)", Dock = DockStyle.Top, Height = 20, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8F) };

        var flpWifi = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 4, 0, 0) };
        txtWifiConnectIp = CreateTextBox();
        txtWifiConnectIp.PlaceholderText = "192.168.1.x:cổng (VD: 192.168.100.73:35029)";
        txtWifiConnectIp.Width = 320;
        btnWifiConnect = CreateButton("⚡ Kết nối Wi-Fi", Color.FromArgb(16, 185, 129), 140);
        btnWifiConnect.ForeColor = Color.White;

        flpWifi.Controls.AddRange([txtWifiConnectIp, btnWifiConnect]);
        cardWifi.Controls.Add(flpWifi);
        cardWifi.Controls.Add(lblWifiSub);
        cardWifi.Controls.Add(lblWifiHead);
        rightPanel.Controls.Add(cardWifi);

        bodyLayout.Controls.Add(rightPanel, 1, 0);
        panelDevicesView.Controls.Add(bodyLayout);

        viewHost.Controls.Add(panelDevicesView);
    }

    private static Control CreateIconButton(string icon, string tooltip)
    {
        var btn = new Guna.UI2.WinForms.Guna2Button
        {
            Text = icon,
            Size = new Size(30, 30),
            FillColor = Color.FromArgb(243, 244, 246),
            ForeColor = Color.FromArgb(55, 65, 81),
            BorderRadius = 5,
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 2, 2, 2),
            Font = new Font("Segoe UI Emoji", 9F)
        };
        var tip = new ToolTip();
        tip.SetToolTip(btn, tooltip);
        return btn;
    }
}
