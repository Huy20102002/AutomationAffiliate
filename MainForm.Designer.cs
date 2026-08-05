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
    private Guna.UI2.WinForms.Guna2Panel panelInspector;
    private Panel panelBottom;
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
    private Guna.UI2.WinForms.Guna2ComboBox cboAppPackage;
    private Control btnRefreshApps;
    private Control btnCaptureCurrentApp;
    private Control btnCaptureTapCoordinates;
    private Guna.UI2.WinForms.Guna2ComboBox cboTapMode;
    private Guna.UI2.WinForms.Guna2TextBox txtTapXPath;
    private Guna.UI2.WinForms.Guna2TextBox txtTapImagePath;
    private Control btnBrowseTapImage;
    private TableLayoutPanel videoOptionsPanel;
    private TableLayoutPanel appOptionsPanel;
    private Panel tapOptionsPanel;
    private TableLayoutPanel inspectorFields;
    private ProductListControl productListControl;
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

        Text = "FlowPilot Â· Shopee Video Studio";
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

        sidebar = new Panel { Dock = DockStyle.Left, Width = 238, BackColor = Color.FromArgb(15, 73, 119), Padding = new Padding(16, 18, 14, 16) };
        var brand = new Label { Text = "◆  FLOWPILOT", AutoSize = true, Location = new Point(18, 20), Font = new Font("Segoe UI Semibold", 14F), ForeColor = Color.FromArgb(96, 82, 218) };
        var brandSub = new Label { Text = "SHOPEE VIDEO STUDIO", AutoSize = true, Location = new Point(20, 49), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(109, 137, 163) };
        sidebar.Controls.Add(brandSub);
        sidebar.Controls.Add(brand);

        var navTitle = new Label { Text = "KHÔNG GIAN LÀM VIỆC", AutoSize = true, Location = new Point(20, 101), Font = new Font("Segoe UI Semibold", 8F), ForeColor = Color.FromArgb(114, 117, 134) };
        sidebar.Controls.Add(navTitle);
        var nav = new FlowLayoutPanel { Location = new Point(14, 124), Size = new Size(208, 250), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        navOverview = CreateNavButton("⌂   Tổng quan", false);
        navWorkflow = CreateNavButton("⌘   Quy trình Android", true);
        navWorkflowIos = CreateNavButton("⌘   Quy trình iPhone", false);
        navProducts = CreateNavButton("▦   Dữ liệu & công việc", false);
        navDevices = CreateNavButton("◉   Thiết bị", false);
        navLogs = CreateNavButton("≡   Nhật ký hoạt động", false);
        nav.Controls.AddRange([navOverview, navWorkflow, navWorkflowIos, navProducts, navDevices, navLogs]);
        sidebar.Controls.Add(nav);

        var sideCard = new Panel { Location = new Point(16, 410), Size = new Size(206, 108), BackColor = Color.FromArgb(245, 246, 250), Padding = new Padding(14) };
        var sideCardTitle = new Label { Text = "SẴN SÀNG TỰ ĐỘNG", AutoSize = true, Location = new Point(14, 13), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(96, 82, 218) };
        var sideCardText = new Label { Text = "Kết nối thiết bị để bắt đầu\nxây dựng quy trình.", AutoSize = true, Location = new Point(14, 39), Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(114, 117, 134) };
        sideCard.Controls.Add(sideCardText);
        sideCard.Controls.Add(sideCardTitle);
        sidebar.Controls.Add(sideCard);
        var sideFooter = new Label { Text = "BỘ MÁY CỤC BỘ  •  v1.1\nQUY TRÌNH ADB", AutoSize = true, Location = new Point(20, 850), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(83, 111, 140) };
        sidebar.Controls.Add(sideFooter);
        Controls.Add(sidebar);

        mainContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 250, 254), Padding = new Padding(0) };
        Controls.Add(mainContent);

        topBar = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Color.White, Padding = new Padding(26, 14, 22, 10) };
        var title = new Label { Text = "Workflow Studio", AutoSize = true, Location = new Point(26, 12), Font = new Font("Segoe UI Semibold", 18F), ForeColor = Color.FromArgb(231, 240, 249) };
        var subtitle = new Label { Text = "Thiết kế và chạy tự động hóa video Shopee", AutoSize = true, Location = new Point(28, 43), Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(112, 140, 166) };
        lblDeviceBadge = new Label { Text = "●   Chưa kết nối thiết bị", AutoSize = true, Location = new Point(670, 29), Font = new Font("Segoe UI Semibold", 9F), ForeColor = Color.FromArgb(245, 186, 90) };
        btnStart = CreateButton("▶  CHẠY QUY TRÌNH", Color.FromArgb(0, 174, 139), 142);
        btnStart.Location = new Point(0, 18);
        btnStop = CreateButton("■  DỪNG", Color.FromArgb(151, 54, 74), 82);
        btnStop.Location = new Point(150, 18);
        btnStop.Enabled = false;
        var topActions = new Panel { Dock = DockStyle.Right, Width = 350 };
        topActions.Controls.Add(btnStop);
        btnTestWorkflow = CreateButton("▷  CHẠY THỬ", Color.FromArgb(35, 149, 218), 104);
        btnTestWorkflow.Location = new Point(148, 18);
        topActions.Controls.Add(btnTestWorkflow);
        topActions.Controls.Add(btnStart);
        btnStop.Location = new Point(258, 18);
        cboDevices = CreateComboBox(150, Color.FromArgb(244, 248, 252));
        cboDevices.Font = new Font("Segoe UI", 8F);
        cboDevices.Margin = new Padding(0, 2, 3, 0);
        btnRefreshDevices = CreateButton("»", Color.FromArgb(228, 240, 249), 34);
        btnRefreshDevices.Margin = new Padding(1, 0, 1, 0);
        btnConnect = CreateButton("Kết nối", Color.FromArgb(10, 151, 205), 72);
        btnConnect.Margin = new Padding(1, 0, 1, 0);
        btnDisconnect = CreateButton("Ngắt kết nối", Color.FromArgb(238, 225, 228), 88);
        btnDisconnect.Margin = new Padding(1, 0, 1, 0);
        btnDisconnect.Enabled = false;
        var deviceTools = new FlowLayoutPanel { Location = new Point(300, 18), Size = new Size(360, 34), FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Left };
        deviceTools.Controls.AddRange([cboDevices, btnRefreshDevices, btnConnect, btnDisconnect]);
        topBar.Controls.Add(topActions);
        topBar.Controls.Add(deviceTools);
        topBar.Controls.Add(lblDeviceBadge);
        topBar.Controls.Add(subtitle);
        topBar.Controls.Add(title);
        mainContent.Controls.Add(topBar);

        viewHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 250, 254) };
        mainContent.Controls.Add(viewHost);

        workspace = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 250, 254), ColumnCount = 1, RowCount = 2, Padding = new Padding(14, 12, 14, 0) };
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 64F));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
        viewHost.Controls.Add(workspace);

        var upper = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 73F));
        upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F));
        workspace.Controls.Add(upper, 0, 0);

        panelWorkflowContainer = CreateCardPanel();
        panelInspector = CreateCardPanel();
        upper.Controls.Add(panelWorkflowContainer, 0, 0);
        upper.Controls.Add(panelInspector, 1, 0);

        BuildWorkflowPanel();
        BuildInspectorPanel();
        BuildBottomPanel(workspace);

        productModulePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 245, 248), Visible = false };
        productListControl = new ProductListControl { Dock = DockStyle.Fill };
        productModulePanel.Controls.Add(productListControl);
        viewHost.Controls.Add(productModulePanel);

        panelStatusBar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.FromArgb(13, 20, 32), Padding = new Padding(18, 0, 18, 0) };
        lblStatus = new Label { Text = "Sẵn sàng", AutoSize = true, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(134, 161, 183) };
        progressBar = new ProgressBar { Dock = DockStyle.Right, Width = 190, Height = 12, Style = ProgressBarStyle.Continuous, Maximum = 100, Value = 0 };
        panelStatusBar.Controls.Add(progressBar);
        panelStatusBar.Controls.Add(lblStatus);
        mainContent.Controls.Add(panelStatusBar);
        sidebar.BringToFront();
        topBar.BringToFront();
        panelStatusBar.BringToFront();
        ResumeLayout(false);
    }

    private void BuildWorkflowPanel()
    {
        var heading = CreateHeading("SƠ ĐỒ QUY TRÌNH", "Kéo khối để sắp xếp · thả vào khối khác để đổi thứ tự", out lblStepCount);
        panelWorkflowContainer.Controls.Add(heading);
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 43, Padding = new Padding(14, 4, 14, 5), AutoScroll = true };
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
        btnSaveWorkflow = CreateButton("Lưu", Color.FromArgb(34, 49, 70), 50);
        btnSaveWorkflow.Location = new Point(410, 4);
        btnLoadWorkflow = CreateButton("Mở", Color.FromArgb(34, 49, 70), 50);
        btnLoadWorkflow.Location = new Point(470, 4);
        btnClearWorkflow = CreateButton("Làm sạch", Color.FromArgb(67, 43, 57), 68);
        btnClearWorkflow.Location = new Point(530, 4);
        btnRecordActions = CreateButton("● Ghi thao tác", Color.FromArgb(228, 240, 249), 108);
        btnRecordActions.Location = new Point(604, 4);
        btnStopRecording = CreateButton("■ Dừng ghi", Color.FromArgb(238, 225, 228), 90);
        btnStopRecording.Location = new Point(718, 4);
        btnStopRecording.Enabled = false;
        btnZoomOut = CreateButton("−", Color.FromArgb(228, 240, 249), 30);
        btnZoomOut.Location = new Point(816, 4);
        btnZoomReset = CreateButton("100%", Color.FromArgb(228, 240, 249), 52);
        btnZoomReset.Location = new Point(850, 4);
        btnZoomIn = CreateButton("+", Color.FromArgb(228, 240, 249), 30);
        btnZoomIn.Location = new Point(906, 4);
        lblZoom = new Label { Text = "Thu phóng", AutoSize = true, Location = new Point(944, 12), ForeColor = Color.FromArgb(91, 128, 157), Font = new Font("Segoe UI", 8F) };
        toolbar.Controls.AddRange([cboStepType, btnAddStep, btnRemoveStep, btnMoveUp, btnMoveDown, btnSaveWorkflow, btnLoadWorkflow, btnClearWorkflow, btnRecordActions, btnStopRecording, btnZoomOut, btnZoomReset, btnZoomIn, lblZoom]);
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
        AddPaletteItem(workflowPalette, "Nhập văn bản", StepType.InputText);
        AddPaletteItem(workflowPalette, "Vuốt", StepType.Swipe);
        AddPaletteItem(workflowPalette, "Mở ứng dụng", StepType.OpenApp);
        AddPaletteItem(workflowPalette, "Phím hệ thống", StepType.KeyEvent);
        AddPaletteItem(workflowPalette, "Lệnh ADB", StepType.AdbShell);
        AddPaletteItem(workflowPalette, "Đẩy video", StepType.PushVideo);
        AddPaletteItem(workflowPalette, "Quét thư viện", StepType.MediaScan);
        ResizePaletteItems();
        workflowPalette.AutoScrollMinSize = new Size(0, Math.Max(700, workflowPalette.PreferredSize.Height + 16));
        workflowPalette.AutoScrollPosition = Point.Empty;
        var paletteShell = new Panel { Dock = DockStyle.Left, Width = 194, BackColor = Color.FromArgb(248, 251, 253) };
        paletteShell.Controls.Add(workflowPalette);
        paletteShell.Controls.Add(workflowCorePalette);
        builderArea.Controls.Add(paletteShell);
        workflowCanvas = new WorkflowCanvas { Dock = DockStyle.Fill, Margin = new Padding(0) };
        builderArea.Controls.Add(workflowCanvas);
        paletteShell.BringToFront();
        panelWorkflowContainer.Controls.Add(builderArea);
        panelWorkflowContainer.Resize += (_, _) => LayoutWorkflowBody();
        toolbar.BringToFront();
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
        var heading = CreateHeading("CẤU HÌNH BƯỚC", "Thiết lập thao tác đang chọn");
        lblSelectedStep = new Label { Text = "Chưa chọn bước", Dock = DockStyle.Top, Height = 40, Padding = new Padding(14, 12, 14, 4), ForeColor = Color.FromArgb(0, 161, 112), Font = new Font("Segoe UI Semibold", 9.5F) };
        var inspectorBody = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        
        inspectorFields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 9, Padding = new Padding(14, 4, 14, 10) };
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
        inspectorFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        btnApplyConfig = CreateButton("✓ Lưu thay đổi", Color.FromArgb(0, 161, 112), 0);
        btnApplyConfig.Dock = DockStyle.Fill;
        btnApplyConfig.Margin = new Padding(0, 8, 0, 4);
        inspectorFields.Controls.Add(btnApplyConfig, 1, 8);

        actionOptionsPanel = new Panel { Dock = DockStyle.Top, Height = 230, Padding = new Padding(14, 10, 14, 10), BackColor = Color.White, Visible = false };
        var actionOptionsTitle = new Label { Text = "CẤU HÌNH NHANH", Dock = DockStyle.Top, Height = 24, ForeColor = Color.FromArgb(8, 132, 191), Font = new Font("Segoe UI Semibold", 8F) };
        
        videoOptionsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(0, 4, 0, 0) };
        videoOptionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        videoOptionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        videoOptionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        videoOptionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        videoOptionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        videoOptionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        videoOptionsPanel.Controls.Add(new Label { Text = "Nguồn video", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 0);
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

        appOptionsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 4, 0, 0), Visible = false };
        appOptionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        appOptionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        appOptionsPanel.Controls.Add(new Label { Text = "Ứng dụng", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 0);
        var appPackagePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 2) };
        cboAppPackage = CreateComboBox(0, Color.FromArgb(244, 248, 252));
        cboAppPackage.Dock = DockStyle.Fill;
        btnRefreshApps = CreateButton("»", Color.FromArgb(228, 240, 249), 32);
        btnRefreshApps.Dock = DockStyle.Right;
        btnCaptureCurrentApp = CreateButton("Lấy app", Color.FromArgb(228, 240, 249), 62);
        btnCaptureCurrentApp.Dock = DockStyle.Right;
        appPackagePanel.Controls.Add(cboAppPackage);
        appPackagePanel.Controls.Add(btnCaptureCurrentApp);
        appPackagePanel.Controls.Add(btnRefreshApps);
        appOptionsPanel.Controls.Add(appPackagePanel, 1, 0);
        actionOptionsPanel.Controls.Add(appOptionsPanel);

        tapOptionsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0), Visible = false };
        btnCaptureTapCoordinates = CreateButton("⌖ Lấy tọa độ từ điện thoại", Color.FromArgb(35, 149, 218), 0);
        btnCaptureTapCoordinates.Dock = DockStyle.Bottom;
        btnCaptureTapCoordinates.Height = 28;
        tapOptionsPanel.Controls.Add(btnCaptureTapCoordinates);
        var tapGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(0, 2, 0, 4) };
        tapGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        tapGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        for (var row = 0; row < 3; row++) tapGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        tapGrid.Controls.Add(new Label { Text = "Kiểu chạm", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 0);
        cboTapMode = CreateComboBox(0, Color.White);
        cboTapMode.Dock = DockStyle.Fill;
        cboTapMode.Items.AddRange([
            new TapModeChoice(TapMode.Coordinates),
            new TapModeChoice(TapMode.XPath),
            new TapModeChoice(TapMode.Image)
        ]);
        cboTapMode.SelectedIndex = 0;
        tapGrid.Controls.Add(cboTapMode, 1, 0);
        tapGrid.Controls.Add(new Label { Text = "XPath", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 1);
        txtTapXPath = CreateTextBox();
        txtTapXPath.Dock = DockStyle.Fill;
        txtTapXPath.PlaceholderText = "//node[@text='...']";
        tapGrid.Controls.Add(txtTapXPath, 1, 1);
        tapGrid.Controls.Add(new Label { Text = "Ảnh mẫu", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, 2);
        var tapImagePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 2) };
        txtTapImagePath = CreateTextBox();
        txtTapImagePath.Dock = DockStyle.Fill;
        btnBrowseTapImage = CreateButton("Mở", Color.FromArgb(228, 240, 249), 46);
        btnBrowseTapImage.Dock = DockStyle.Right;
        tapImagePanel.Controls.Add(txtTapImagePath);
        tapImagePanel.Controls.Add(btnBrowseTapImage);
        tapGrid.Controls.Add(tapImagePanel, 1, 2);
        tapOptionsPanel.Controls.Add(tapGrid);
        actionOptionsPanel.Controls.Add(tapOptionsPanel);
        actionOptionsPanel.Controls.Add(actionOptionsTitle);

        var variablesPanel = new Panel { Dock = DockStyle.Bottom, Height = 250, Padding = new Padding(14, 16, 14, 16), BackColor = Color.FromArgb(250, 252, 254) };
        var variablesTitle = new Label { Text = "BIẾN DỮ LIỆU", Dock = DockStyle.Top, Height = 26, ForeColor = Color.FromArgb(8, 132, 191), Font = new Font("Segoe UI Semibold", 8F) };
        variablesPanel.Controls.Add(variablesTitle);

        lstVariables = new ListBox { Dock = DockStyle.Top, Height = 95, BackColor = Color.White, ForeColor = Color.FromArgb(27, 55, 82), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 8.5F), Margin = new Padding(0, 0, 0, 6) };
        variablesPanel.Controls.Add(lstVariables);

        var inputPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 40, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 6, 0, 0) };
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        txtVariableName = CreateTextBox();
        txtVariableName.Dock = DockStyle.Fill;
        txtVariableName.Margin = new Padding(0, 4, 4, 0);
        txtVariableName.PlaceholderText = "Tên biến";
        txtVariableValue = CreateTextBox();
        txtVariableValue.Dock = DockStyle.Fill;
        txtVariableValue.Margin = new Padding(4, 4, 0, 0);
        txtVariableValue.PlaceholderText = "Giá trị mặc định";
        inputPanel.Controls.Add(txtVariableName, 0, 0);
        inputPanel.Controls.Add(txtVariableValue, 1, 0);
        variablesPanel.Controls.Add(inputPanel);

        var variableButtons = new TableLayoutPanel { Dock = DockStyle.Top, Height = 40, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 6, 0, 0) };
        variableButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        variableButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        variableButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        variableButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        btnAddVariable = CreateButton("Lưu", Color.FromArgb(10, 151, 205), 0);
        btnAddVariable.Dock = DockStyle.Fill;
        btnAddVariable.Margin = new Padding(0, 4, 4, 0);
        btnLoadWorkflow.Dock = DockStyle.Fill;
        btnLoadWorkflow.Margin = new Padding(2, 4, 2, 0);
        btnLoadWorkflow.Text = "Import";
        if (btnLoadWorkflow is Guna.UI2.WinForms.Guna2Button loadWorkflowButton)
            loadWorkflowButton.FillColor = Color.FromArgb(34, 75, 101);
        btnRemoveVariable = CreateButton("Xóa", Color.FromArgb(241, 226, 229), 0);
        btnRemoveVariable.Dock = DockStyle.Fill;
        btnRemoveVariable.Margin = new Padding(2, 4, 2, 0);
        btnRemoveVariable.ForeColor = Color.FromArgb(219, 82, 91);
        btnInsertVariable = CreateButton("Chèn", Color.FromArgb(0, 161, 112), 0);
        btnInsertVariable.Dock = DockStyle.Fill;
        btnInsertVariable.Margin = new Padding(4, 4, 0, 0);
        variableButtons.Controls.Add(btnAddVariable, 0, 0);
        variableButtons.Controls.Add(btnLoadWorkflow, 1, 0);
        variableButtons.Controls.Add(btnRemoveVariable, 2, 0);
        variableButtons.Controls.Add(btnInsertVariable, 3, 0);
        variablesPanel.Controls.Add(variableButtons);

        inspectorBody.Controls.Add(inspectorFields);
        inspectorBody.Controls.Add(actionOptionsPanel);
        inspectorBody.Controls.Add(variablesPanel);

        panelInspector.Controls.Add(inspectorBody);
        panelInspector.Controls.Add(lblSelectedStep);
        heading.BringToFront();
        panelInspector.Controls.Add(heading);
    }

    private void BuildBottomPanel(TableLayoutPanel parent)
    {
        panelBottom = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(14, 21, 34), Padding = new Padding(12, 10, 12, 10) };
        var tabs = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.FlatButtons, ItemSize = new Size(100, 24), SizeMode = TabSizeMode.Fixed };
        var jobsTab = new TabPage("  HÀNG ĐỢI CÔNG VIỆC  ") { BackColor = Color.FromArgb(14, 21, 34), Padding = new Padding(8) };
        var logTab = new TabPage("  NHẬT KÝ HOẠT ĐỘNG  ") { BackColor = Color.FromArgb(14, 21, 34), Padding = new Padding(8) };
        var jobToolbar = new Panel { Dock = DockStyle.Top, Height = 36 };
        btnImportExcel = CreateButton("Nhập Excel", Color.FromArgb(34, 75, 101), 105);
        btnExportTemplate = CreateButton("Mẫu", Color.FromArgb(34, 49, 70), 65);
        btnExportResult = CreateButton("Xuất kết quả", Color.FromArgb(34, 49, 70), 105);
        btnImportExcel.Location = new Point(0, 0);
        btnExportTemplate.Location = new Point(111, 0);
        btnExportResult.Location = new Point(197, 0);
        jobToolbar.Controls.AddRange([btnImportExcel, btnExportTemplate, btnExportResult]);
        jobsTab.Controls.Add(jobToolbar);
        dgvJobs = new Guna.UI2.WinForms.Guna2DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, AutoGenerateColumns = false, BackgroundColor = Color.FromArgb(14, 21, 34), BorderStyle = BorderStyle.None, GridColor = Color.FromArgb(34, 54, 72), RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, EnableHeadersVisualStyles = false };
        dgvJobs.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(22, 35, 50), ForeColor = Color.FromArgb(139, 174, 198), Font = new Font("Segoe UI Semibold", 8.5F), Padding = new Padding(5) };
        dgvJobs.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(14, 21, 34), ForeColor = Color.FromArgb(204, 220, 233), SelectionBackColor = Color.FromArgb(29, 57, 73), SelectionForeColor = Color.White, Padding = new Padding(5), Font = new Font("Segoe UI", 8.5F) };
        AddColumn("STT", "colId", 50);
        AddColumn("Đường dẫn video", "colVideo", 320);
        AddColumn("Tiêu đề", "colTitle", 350);
        AddColumn("Liên kết tiếp thị", "colLink", 260);
        AddColumn("Trạng thái", "colStatus", 110);
        AddColumn("Trạng thái Shopee", "colShopeeStatus", 140);
        AddColumn("Nhật ký", "colLog", 300);
        jobsTab.Controls.Add(dgvJobs);
        jobToolbar.BringToFront();
        tabs.TabPages.Add(jobsTab);

        txtLog = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.FromArgb(9, 14, 24), ForeColor = Color.FromArgb(150, 189, 190), BorderStyle = BorderStyle.None, Font = new Font("Cascadia Mono", 9F), DetectUrls = false };
        logTab.Controls.Add(txtLog);
        tabs.TabPages.Add(logTab);
        panelBottom.Controls.Add(tabs);
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
        var panel = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(14, 10, 14, 7), BackColor = Color.White };
        var titleLabel = new Label { Text = title, AutoSize = true, Location = new Point(14, 9), Font = new Font("Segoe UI Semibold", 10F), ForeColor = Color.FromArgb(31, 31, 44) };
        var subLabel = new Label { Text = subtitle, AutoSize = true, Location = new Point(15, 31), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(114, 117, 134) };
        count = new Label { Text = "0 BƯỚC", AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(350, 17), Font = new Font("Segoe UI Semibold", 8F), ForeColor = Color.FromArgb(96, 82, 218) };
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
        var primary = Color.FromArgb(96, 82, 218);
        var color = active ? primary : Color.White;
        var button = new Button { Text = text, Width = 204, Height = 38, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = active ? Color.White : Color.FromArgb(114, 117, 134), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 9F), Padding = new Padding(13, 0, 0, 0), Cursor = Cursors.Hand, Margin = new Padding(0, 2, 0, 2) };
        button.FlatAppearance.BorderSize = 0;
        ApplyRoundedCorners(button, 7);
        button.MouseEnter += (_, _) => { if (!active) button.BackColor = Color.FromArgb(244, 245, 248); };
        button.MouseLeave += (_, _) => { if (!active) button.BackColor = Color.White; };
        return button;
    }
}
