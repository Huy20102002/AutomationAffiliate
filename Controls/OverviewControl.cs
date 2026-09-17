using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

public sealed class OverviewControl : UserControl
{
    // --- Events for MainForm ---
    public event EventHandler? RunRequested;
    public event EventHandler? ImportRequested;
    public event EventHandler? CampaignsRequested;
    public event EventHandler? RefreshRequested;
    public event EventHandler? WorkflowRequested;
    public event EventHandler? TestWorkflowRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler<string>? DeviceToolRequested;

    // --- Header controls ---
    private Label _lblSystemStatus = null!;
    private Guna2Button _btnQuickImport = null!;
    private Guna2Button _btnQuickCampaigns = null!;
    private Guna2Button _btnQuickRefresh = null!;

    // --- KPI 1: Jobs ---
    private Label _lblTotalJobs = null!;
    private Label _lblDoneBadge = null!;
    private Label _lblWaitingBadge = null!;
    private Guna2ProgressBar _pbJobCompletion = null!;

    // --- KPI 2: Success Rate ---
    private Label _lblSuccessRate = null!;
    private Label _lblSuccessBadge = null!;
    private Label _lblSuccessRateDesc = null!;

    // --- KPI 3: Device ---
    private Label _lblDeviceModel = null!;
    private Label _lblDeviceStatus = null!;
    private Label _lblDeviceDetails = null!;

    // --- KPI 4: AI & Telegram ---
    private Label _lblAiProvider = null!;
    private Label _lblTelegramStatus = null!;
    private Guna2Button _btnConfigSettings = null!;

    // --- Campaign Progress Container ---
    private FlowLayoutPanel _pnlCampaignList = null!;

    // --- Video Table Toolbar ---
    private Guna2TextBox _txtSearchVideo = null!;
    private Guna2ComboBox _cboStatusFilter = null!;
    private Label _lblTableCount = null!;

    // --- Recent Jobs Grid ---
    private Guna2DataGridView _dgvRecentJobs = null!;

    // --- Workflow Info & Pipeline ---
    private Label _lblWorkflowName = null!;
    private Label _lblWorkflowSteps = null!;
    private Label _lblPlatformBadge = null!;
    private Panel _pnlWorkflowPipeline = null!;

    // --- Device Control Toolbox ---
    private Panel _pnlDeviceControlContainer = null!;
    private TableLayoutPanel _gridToolbox = null!;
    private Panel _pnlDeviceEmptyState = null!;
    private Label _lblToolResult = null!;

    // --- Cached Data for fast filtering ---
    private List<JobItem> _allRecentJobs = [];
    private Dictionary<int, string> _folderMap = [];

    public OverviewControl()
    {
        BackColor = Color.FromArgb(248, 250, 252); // Modern Slate-50 background
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        AutoScroll = true;
        AutoScrollMinSize = new Size(1060, 680);
        BuildUi();
    }

    private void BuildUi()
    {
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(20, 14, 20, 16)
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));    // Row 0: Page Header (polished height)
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126F));   // Row 1: 4 KPI Cards (compact & spacious)
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // Row 2: 2-Column Main Section

        rootLayout.Controls.Add(BuildPageHeader(), 0, 0);
        rootLayout.Controls.Add(BuildKpiRow(), 0, 1);
        rootLayout.Controls.Add(BuildMainContentSection(), 0, 2);

        Controls.Add(rootLayout);
    }

    // =========================================================================
    // 1. PAGE HEADER (Non-intrusive, Clean SaaS Header)
    // =========================================================================
    private Control BuildPageHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };

        var titleBlock = new Panel { Dock = DockStyle.Left, Width = 520, BackColor = Color.Transparent };
        var title = new Label
        {
            Text = "Tổng quan hệ thống",
            AutoSize = true,
            Location = new Point(0, 0),
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42) // Slate-900
        };
        var subtitle = new Label
        {
            Text = "Theo dõi hiệu suất tự động hóa, tình trạng thiết bị và danh sách công việc",
            AutoSize = true,
            Location = new Point(1, 25),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(100, 116, 139) // Slate-500
        };
        titleBlock.Controls.AddRange([title, subtitle]);

        var actionsBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };

        _lblSystemStatus = new Label
        {
            Text = "●  SẴN SÀNG",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(16, 185, 129),
            BackColor = Color.FromArgb(236, 253, 245),
            Padding = new Padding(10, 6, 10, 6),
            Margin = new Padding(0, 2, 8, 0)
        };

        _btnQuickImport = new Guna2Button
        {
            Text = "📥  Nhập Excel",
            Size = new Size(120, 36),
            FillColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            BorderColor = Color.FromArgb(226, 232, 240),
            BorderThickness = 1,
            BorderRadius = 8,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
        _btnQuickImport.HoverState.FillColor = Color.FromArgb(241, 245, 249);
        _btnQuickImport.Click += (_, _) => ImportRequested?.Invoke(this, EventArgs.Empty);

        _btnQuickCampaigns = new Guna2Button
        {
            Text = "📁  Chiến dịch",
            Size = new Size(115, 36),
            FillColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            BorderColor = Color.FromArgb(226, 232, 240),
            BorderThickness = 1,
            BorderRadius = 8,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
        _btnQuickCampaigns.HoverState.FillColor = Color.FromArgb(241, 245, 249);
        _btnQuickCampaigns.Click += (_, _) => CampaignsRequested?.Invoke(this, EventArgs.Empty);

        _btnQuickRefresh = new Guna2Button
        {
            Text = "🔄",
            Size = new Size(36, 36),
            FillColor = Color.White,
            ForeColor = Color.FromArgb(71, 85, 105),
            BorderColor = Color.FromArgb(226, 232, 240),
            BorderThickness = 1,
            BorderRadius = 8,
            Font = new Font("Segoe UI", 9.5F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 0)
        };
        _btnQuickRefresh.HoverState.FillColor = Color.FromArgb(241, 245, 249);
        _btnQuickRefresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);

        actionsBar.Controls.AddRange([_lblSystemStatus, _btnQuickImport, _btnQuickCampaigns, _btnQuickRefresh]);
        panel.Controls.Add(actionsBar);
        panel.Controls.Add(titleBlock);
        return panel;
    }

    // =========================================================================
    // 2. 4 SMART KPI METRIC CARDS (Anti-clipping Layout)
    // =========================================================================
    private Control BuildKpiRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 10)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        // --- Card 1: Total Jobs ---
        var card1 = CreateCardPanel();
        card1.Margin = new Padding(0, 0, 6, 0);
        card1.Padding = new Padding(15, 12, 15, 10);

        var inner1 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        inner1.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F)); // Title
        inner1.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F)); // Big Stat (20F bold)
        inner1.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F)); // Badges Flow
        inner1.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F)); // Progress Bar

        var lblC1Title = new Label { Text = "🎬  TỔNG VIDEO", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 8F), ForeColor = Color.FromArgb(100, 116, 139) };
        _lblTotalJobs = new Label { Text = "0", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };

        var flowBadges1 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
        _lblDoneBadge = new Label
        {
            Text = "+0 thành công",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(3, 84, 63),
            BackColor = Color.FromArgb(222, 247, 236),
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(0, 1, 6, 0)
        };
        _lblWaitingBadge = new Label
        {
            Text = "0 chờ xử lý",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(146, 64, 14),
            BackColor = Color.FromArgb(254, 243, 199),
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(0, 1, 0, 0)
        };
        flowBadges1.Controls.AddRange([_lblDoneBadge, _lblWaitingBadge]);

        _pbJobCompletion = new Guna2ProgressBar
        {
            Dock = DockStyle.Fill,
            Height = 6,
            BorderRadius = 3,
            FillColor = Color.FromArgb(241, 245, 249),
            ProgressColor = Color.FromArgb(16, 185, 129),
            ProgressColor2 = Color.FromArgb(52, 211, 153),
            Value = 0,
            Margin = new Padding(0, 4, 0, 0)
        };

        inner1.Controls.Add(lblC1Title, 0, 0);
        inner1.Controls.Add(_lblTotalJobs, 0, 1);
        inner1.Controls.Add(flowBadges1, 0, 2);
        inner1.Controls.Add(_pbJobCompletion, 0, 3);
        card1.Controls.Add(inner1);
        row.Controls.Add(card1, 0, 0);

        // --- Card 2: Success Rate ---
        var card2 = CreateCardPanel();
        card2.Margin = new Padding(3, 0, 3, 0);
        card2.Padding = new Padding(15, 12, 15, 10);

        var inner2 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        inner2.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F)); // Title
        inner2.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F)); // Big Stat + Badge Flow
        inner2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Description

        var lblC2Title = new Label { Text = "📈  TỶ LỆ THÀNH CÔNG", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 8F), ForeColor = Color.FromArgb(100, 116, 139) };

        var flowStat2 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
        _lblSuccessRate = new Label { Text = "100%", AutoSize = true, Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), Margin = new Padding(0, 0, 8, 0) };
        _lblSuccessBadge = new Label
        {
            Text = "✓ XUẤT SẮC",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(3, 84, 63),
            BackColor = Color.FromArgb(222, 247, 236),
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(0, 4, 0, 0)
        };
        flowStat2.Controls.AddRange([_lblSuccessRate, _lblSuccessBadge]);

        _lblSuccessRateDesc = new Label { Text = "Chưa phát hiện sự cố", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), Margin = new Padding(0, 4, 0, 0) };

        inner2.Controls.Add(lblC2Title, 0, 0);
        inner2.Controls.Add(flowStat2, 0, 1);
        inner2.Controls.Add(_lblSuccessRateDesc, 0, 2);
        card2.Controls.Add(inner2);
        row.Controls.Add(card2, 1, 0);

        // --- Card 3: Device Controller ---
        var card3 = CreateCardPanel();
        card3.Margin = new Padding(3, 0, 3, 0);
        card3.Padding = new Padding(15, 12, 15, 10);

        var inner3 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        inner3.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F)); // Title
        inner3.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F)); // Model Name
        inner3.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F)); // Status Badge
        inner3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Details

        var lblC3Title = new Label { Text = "📱  THIẾT BỊ ĐIỀU KHIỂN", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 8F), ForeColor = Color.FromArgb(100, 116, 139) };
        _lblDeviceModel = new Label { Text = "Chưa kết nối", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoEllipsis = true };
        _lblDeviceStatus = new Label
        {
            Text = "● Chờ kết nối",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(146, 64, 14),
            BackColor = Color.FromArgb(254, 243, 199),
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(0, 1, 0, 0)
        };
        _lblDeviceDetails = new Label { Text = "Cắm cáp USB hoặc quét ADB", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), AutoEllipsis = true, Margin = new Padding(0, 4, 0, 0) };

        inner3.Controls.Add(lblC3Title, 0, 0);
        inner3.Controls.Add(_lblDeviceModel, 0, 1);
        inner3.Controls.Add(_lblDeviceStatus, 0, 2);
        inner3.Controls.Add(_lblDeviceDetails, 0, 3);
        card3.Controls.Add(inner3);
        row.Controls.Add(card3, 2, 0);

        // --- Card 4: AI Assistant & Telegram ---
        var card4 = CreateCardPanel();
        card4.Margin = new Padding(6, 0, 0, 0);
        card4.Padding = new Padding(15, 12, 15, 10);

        var inner4 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        inner4.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F)); // Title
        inner4.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F)); // AI Provider
        inner4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Telegram & Settings Button Flow

        var lblC4Title = new Label { Text = "🤖  TRỢ LÝ AI & TELEGRAM", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 8F), ForeColor = Color.FromArgb(100, 116, 139) };
        _lblAiProvider = new Label { Text = "AI: Chưa cấu hình", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold), ForeColor = Color.FromArgb(79, 70, 229), AutoEllipsis = true };

        var flowTg4 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Margin = new Padding(0) };
        _lblTelegramStatus = new Label
        {
            Text = "✈ Telegram: Đang tắt",
            AutoSize = true,
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0, 4, 8, 0)
        };
        _btnConfigSettings = new Guna2Button
        {
            Text = "⚙ Cấu hình",
            Size = new Size(84, 24),
            FillColor = Color.FromArgb(238, 242, 255),
            ForeColor = Color.FromArgb(79, 70, 229),
            BorderRadius = 6,
            Font = new Font("Segoe UI Semibold", 8F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 1, 0, 0)
        };
        _btnConfigSettings.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        flowTg4.Controls.AddRange([_lblTelegramStatus, _btnConfigSettings]);

        inner4.Controls.Add(lblC4Title, 0, 0);
        inner4.Controls.Add(_lblAiProvider, 0, 1);
        inner4.Controls.Add(flowTg4, 0, 2);
        card4.Controls.Add(inner4);
        row.Controls.Add(card4, 3, 0);

        return row;
    }

    // =========================================================================
    // 3. MAIN 2-COLUMN SECTION (67% Left / 33% Right)
    // =========================================================================
    private Control BuildMainContentSection()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67F)); // Left Main Panel
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F)); // Right Sidebar Panel

        grid.Controls.Add(BuildLeftMainPanel(), 0, 0);
        grid.Controls.Add(BuildRightSidebarPanel(), 1, 0);
        return grid;
    }

    // -------------------------------------------------------------------------
    // LEFT PANEL: Campaign Progress & Modern Video Data Table
    // -------------------------------------------------------------------------
    private Control BuildLeftMainPanel()
    {
        var card = CreateCardPanel();
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(0, 0, 8, 0);
        card.Padding = new Padding(16, 14, 16, 14);

        // Section A: Header
        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent };
        var lblHead = new Label
        {
            Text = "Tiến độ chiến dịch & công việc gần đây",
            AutoSize = true,
            Location = new Point(0, 0),
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        var lblSub = new Label
        {
            Text = "Phân loại theo chiến dịch và danh sách các video đã xử lý gần nhất",
            AutoSize = true,
            Location = new Point(1, 22),
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        headerPanel.Controls.AddRange([lblHead, lblSub]);

        // Section B: Campaign Progress Compact Cards
        _pnlCampaignList = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 80,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(4, 4, 4, 4),
            Margin = new Padding(0, 2, 0, 8)
        };

        // Section C: Table Toolbar (Search + Filters + Count)
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 6)
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));  // Search Box
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F)); // Status Filter
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));  // Count Label

        _txtSearchVideo = new Guna2TextBox
        {
            PlaceholderText = "🔍  Tìm kiếm video theo tiêu đề...",
            Dock = DockStyle.Fill,
            Height = 34,
            BorderRadius = 7,
            BorderColor = Color.FromArgb(226, 232, 240),
            FillColor = Color.White,
            Font = new Font("Segoe UI", 8.5F),
            Margin = new Padding(0, 2, 8, 2)
        };
        _txtSearchVideo.TextChanged += (_, _) => ApplyVideoFilters();

        _cboStatusFilter = new Guna2ComboBox
        {
            Dock = DockStyle.Fill,
            Height = 34,
            BorderRadius = 7,
            BorderColor = Color.FromArgb(226, 232, 240),
            FillColor = Color.White,
            Font = new Font("Segoe UI", 8.5F),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 2, 8, 2)
        };
        _cboStatusFilter.Items.AddRange(["Tất cả trạng thái", "Thành công", "Chờ", "Lỗi"]);
        _cboStatusFilter.SelectedIndex = 0;
        _cboStatusFilter.SelectedIndexChanged += (_, _) => ApplyVideoFilters();

        _lblTableCount = new Label
        {
            Text = "Hiển thị 0 video",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        toolbar.Controls.Add(_txtSearchVideo, 0, 0);
        toolbar.Controls.Add(_cboStatusFilter, 1, 0);
        toolbar.Controls.Add(_lblTableCount, 2, 0);

        // Section D: Modern DataGridView with Status Badges
        _dgvRecentJobs = new Guna2DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = new Font("Segoe UI", 8.5F)
        };

        _dgvRecentJobs.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(248, 250, 252);
        _dgvRecentJobs.ThemeStyle.HeaderStyle.ForeColor = Color.FromArgb(71, 85, 105);
        _dgvRecentJobs.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI Semibold", 8.5F);
        _dgvRecentJobs.ThemeStyle.HeaderStyle.Height = 38;
        _dgvRecentJobs.ThemeStyle.RowsStyle.BackColor = Color.White;
        _dgvRecentJobs.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(30, 41, 59);
        _dgvRecentJobs.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
        _dgvRecentJobs.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(79, 70, 229);
        _dgvRecentJobs.RowTemplate.Height = 44;

        _dgvRecentJobs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#ID", Width = 56 });
        _dgvRecentJobs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tiêu đề video", FillWeight = 48, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _dgvRecentJobs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chiến dịch", Width = 130 });
        _dgvRecentJobs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", Width = 112 });
        _dgvRecentJobs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Shopee", Width = 108 });

        _dgvRecentJobs.CellPainting += DgvRecentJobs_CellPainting;

        card.Controls.Add(_dgvRecentJobs);
        card.Controls.Add(toolbar);
        card.Controls.Add(_pnlCampaignList);
        card.Controls.Add(headerPanel);
        return card;
    }

    private void DgvRecentJobs_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0) return;

        // Custom pill badge for "Trạng thái" (Column 3) & "Shopee" (Column 4)
        if (e.ColumnIndex == 3 || e.ColumnIndex == 4)
        {
            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            var val = e.Value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(val))
            {
                Color bg, fg;
                if (val.Contains("Thành công") || val.Contains("Đã đăng") || val.Contains("Hoàn thành"))
                {
                    bg = Color.FromArgb(222, 247, 236); // #DEF7EC
                    fg = Color.FromArgb(3, 84, 63);     // #03543F
                }
                else if (val.Contains("Lỗi") || val.Contains("Thất bại"))
                {
                    bg = Color.FromArgb(253, 232, 232); // #FDE8E8
                    fg = Color.FromArgb(155, 28, 28);   // #9B1C1C
                }
                else if (val.Contains("Đang chạy") || val.Contains("Running"))
                {
                    bg = Color.FromArgb(237, 233, 254); // #EDE9FE
                    fg = Color.FromArgb(91, 33, 182);   // #5B21B6
                }
                else // Chờ / Chưa đăng
                {
                    bg = Color.FromArgb(254, 243, 199); // #FEF3C7
                    fg = Color.FromArgb(146, 64, 14);   // #92400E
                }

                var badgeWidth = Math.Min(e.CellBounds.Width - 10, 92);
                var badgeHeight = 24;
                var badgeX = e.CellBounds.X + (e.CellBounds.Width - badgeWidth) / 2;
                var badgeY = e.CellBounds.Y + (e.CellBounds.Height - badgeHeight) / 2;
                var badgeRect = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedRectanglePath(badgeRect, 6);
                using var brushBg = new SolidBrush(bg);
                using var brushFg = new SolidBrush(fg);
                e.Graphics.FillPath(brushBg, path);

                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var font = new Font("Segoe UI Semibold", 7.5F);
                e.Graphics.DrawString(val, font, brushFg, badgeRect, sf);
            }
            e.Handled = true;
        }
    }

    // -------------------------------------------------------------------------
    // RIGHT PANEL: Workflow Inspector & Compact Device Control
    // -------------------------------------------------------------------------
    private Control BuildRightSidebarPanel()
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 52F)); // Workflow Card (holds pipeline)
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 48F)); // Device Toolbox Card

        // --- Card A: Workflow Inspector ---
        var wfCard = CreateCardPanel();
        wfCard.Dock = DockStyle.Fill;
        wfCard.Margin = new Padding(0, 0, 0, 8);
        wfCard.Padding = new Padding(14, 12, 14, 12);

        var lblWfHead = new Label
        {
            Text = "SƠ ĐỒ QUY TRÌNH ĐANG CHỌN",
            Dock = DockStyle.Top,
            Height = 18,
            Font = new Font("Segoe UI Semibold", 8F),
            ForeColor = Color.FromArgb(79, 70, 229)
        };

        var topInfoPanel = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.Transparent };
        _lblWorkflowName = new Label
        {
            Text = "Shopee_Upload.json",
            AutoSize = true,
            Location = new Point(0, 0),
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoEllipsis = true
        };
        _lblWorkflowSteps = new Label
        {
            Text = "44 bước tự động hóa",
            AutoSize = true,
            Location = new Point(1, 23),
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        _lblPlatformBadge = new Label
        {
            Text = "Android | ADB Shell",
            AutoSize = true,
            Location = new Point(1, 42),
            Font = new Font("Segoe UI Semibold", 7.5F),
            ForeColor = Color.FromArgb(3, 84, 63),
            BackColor = Color.FromArgb(222, 247, 236),
            Padding = new Padding(6, 2, 6, 2)
        };
        topInfoPanel.Controls.AddRange([_lblWorkflowName, _lblWorkflowSteps, _lblPlatformBadge]);

        var btnBarWf = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 2, 0, 4)
        };
        var btnEditWf = new Guna2Button
        {
            Text = "✏️ Sửa sơ đồ",
            Size = new Size(106, 30),
            FillColor = Color.FromArgb(238, 242, 255),
            ForeColor = Color.FromArgb(79, 70, 229),
            BorderRadius = 7,
            Font = new Font("Segoe UI Semibold", 8F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
        btnEditWf.Click += (_, _) => WorkflowRequested?.Invoke(this, EventArgs.Empty);

        var btnTestWf = new Guna2Button
        {
            Text = "▷ Chạy thử",
            Size = new Size(96, 30),
            FillColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            BorderColor = Color.FromArgb(226, 232, 240),
            BorderThickness = 1,
            BorderRadius = 7,
            Font = new Font("Segoe UI Semibold", 8F),
            Cursor = Cursors.Hand
        };
        btnTestWf.Click += (_, _) => TestWorkflowRequested?.Invoke(this, EventArgs.Empty);
        btnBarWf.Controls.AddRange([btnEditWf, btnTestWf]);

        // Workflow Pipeline Stage Node Preview (Full width nodes!)
        _pnlWorkflowPipeline = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(6, 6, 6, 6),
            Margin = new Padding(0, 4, 0, 0)
        };
        PopulateDefaultWorkflowNodes();

        wfCard.Controls.Add(_pnlWorkflowPipeline);
        wfCard.Controls.Add(btnBarWf);
        wfCard.Controls.Add(topInfoPanel);
        wfCard.Controls.Add(lblWfHead);
        container.Controls.Add(wfCard, 0, 0);

        // --- Card B: Device Control Panel ---
        var tbCard = CreateCardPanel();
        tbCard.Dock = DockStyle.Fill;
        tbCard.Padding = new Padding(14, 12, 14, 12);

        var lblTbHead = new Label
        {
            Text = "Điều khiển thiết bị",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        var lblTbSub = new Label
        {
            Text = "Thao tác trực tiếp trên thiết bị Android",
            Dock = DockStyle.Top,
            Height = 18,
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        _lblToolResult = new Label
        {
            Text = "Sẵn sàng gửi lệnh tới thiết bị",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 116, 139),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 2, 0, 0)
        };

        _pnlDeviceControlContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0, 4, 0, 0) };

        // Grid of 6 buttons (Enhanced height 48px each, fills width)
        _gridToolbox = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 152,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        _gridToolbox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gridToolbox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gridToolbox.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        _gridToolbox.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        _gridToolbox.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        _gridToolbox.Controls.Add(CreateToolButton("📱 Bật/Tắt màn hình", () => TriggerTool("power")), 0, 0);
        _gridToolbox.Controls.Add(CreateToolButton("🏠 Màn hình chính", () => TriggerTool("home")), 1, 0);
        _gridToolbox.Controls.Add(CreateToolButton("⬅ Quay lại", () => TriggerTool("back")), 0, 1);
        _gridToolbox.Controls.Add(CreateToolButton("📸 Chụp màn hình", () => TriggerTool("screenshot")), 1, 1);
        _gridToolbox.Controls.Add(CreateToolButton("🔄 Reset ADB Server", () => TriggerTool("restart_adb")), 0, 2);
        _gridToolbox.Controls.Add(CreateToolButton("🧹 Quét MediaScan", () => TriggerTool("mediascan")), 1, 2);

        // Empty state card when disconnected (Centered, beautiful)
        _pnlDeviceEmptyState = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(254, 252, 232), // Warm light amber
            Padding = new Padding(12),
            Visible = false
        };
        var esLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        esLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        esLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        esLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        esLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

        var lblEsIcon = new Label { Text = "🔌", Font = new Font("Segoe UI", 18F), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        var lblEsTitle = new Label { Text = "Chưa kết nối thiết bị", Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(146, 64, 14), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        var lblEsDesc = new Label { Text = "Vui lòng cắm cáp USB hoặc quét thiết bị ADB để sử dụng các công cụ điều khiển.", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(180, 83, 9), Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopCenter };
        var btnEsConnect = new Guna2Button
        {
            Text = "⚡ Kết nối thiết bị",
            Size = new Size(140, 32),
            Anchor = AnchorStyles.None,
            FillColor = Color.FromArgb(217, 119, 6),
            ForeColor = Color.White,
            BorderRadius = 7,
            Font = new Font("Segoe UI Semibold", 8F),
            Cursor = Cursors.Hand
        };
        btnEsConnect.Click += (_, _) => DeviceToolRequested?.Invoke(this, "restart_adb");

        esLayout.Controls.Add(lblEsIcon, 0, 0);
        esLayout.Controls.Add(lblEsTitle, 0, 1);
        esLayout.Controls.Add(lblEsDesc, 0, 2);
        esLayout.Controls.Add(btnEsConnect, 0, 3);
        _pnlDeviceEmptyState.Controls.Add(esLayout);

        _pnlDeviceControlContainer.Controls.Add(_gridToolbox);
        _pnlDeviceControlContainer.Controls.Add(_pnlDeviceEmptyState);

        tbCard.Controls.Add(_lblToolResult);
        tbCard.Controls.Add(_pnlDeviceControlContainer);
        tbCard.Controls.Add(lblTbSub);
        tbCard.Controls.Add(lblTbHead);

        container.Controls.Add(tbCard, 0, 1);
        return container;
    }

    private void PopulateDefaultWorkflowNodes()
    {
        _pnlWorkflowPipeline.Controls.Clear();
        var nodes = new (string Title, string Type, string Status)[]
        {
            ("START", "Khởi tạo môi trường", "Completed"),
            ("Mở app Shopee", "Kích hoạt com.shopee.vn", "Completed"),
            ("Đăng nhập & Điều hướng", "Mở mục Shopee Video", "Completed"),
            ("Nạp Video & Caption AI", "Truyền MP4 & tiêu đề viral", "Running"),
            ("Gắn liên kết sản phẩm", "Đính kèm affiliate link", "Waiting"),
            ("Hoàn tất & Đăng video", "Xác nhận xuất bản", "Waiting")
        };

        // Add in reverse order because DockStyle.Top stacks downwards
        for (int i = nodes.Length - 1; i >= 0; i--)
        {
            var (title, type, status) = nodes[i];
            var pnlNode = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                BorderRadius = 7,
                Margin = new Padding(0, 0, 0, 4),
                Padding = new Padding(6, 2, 6, 2)
            };

            var dotColor = status switch
            {
                "Completed" => Color.FromArgb(16, 185, 129),
                "Running" => Color.FromArgb(99, 102, 241),
                _ => Color.FromArgb(203, 213, 225)
            };

            var lblDot = new Label
            {
                Text = status == "Completed" ? "✓" : (status == "Running" ? "●" : "○"),
                Dock = DockStyle.Left,
                Width = 26,
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = dotColor,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblStatusPill = new Label
            {
                Text = status,
                Dock = DockStyle.Right,
                Width = 76,
                Font = new Font("Segoe UI Semibold", 7F),
                ForeColor = dotColor,
                TextAlign = ContentAlignment.MiddleRight
            };

            var pnlCenter = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblNodeTitle = new Label
            {
                Text = title,
                AutoSize = true,
                Location = new Point(2, 2),
                Font = new Font("Segoe UI Semibold", 8F),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoEllipsis = true
            };
            var lblNodeType = new Label
            {
                Text = type,
                AutoSize = true,
                Location = new Point(3, 19),
                Font = new Font("Segoe UI", 7F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoEllipsis = true
            };
            pnlCenter.Controls.AddRange([lblNodeTitle, lblNodeType]);

            pnlNode.Controls.Add(pnlCenter);
            pnlNode.Controls.Add(lblStatusPill);
            pnlNode.Controls.Add(lblDot);

            var spacer = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Color.Transparent };
            _pnlWorkflowPipeline.Controls.Add(spacer);
            _pnlWorkflowPipeline.Controls.Add(pnlNode);
        }
    }

    private Control CreateToolButton(string text, Action onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 2, 2, 2),
            FillColor = Color.White,
            ForeColor = Color.FromArgb(30, 41, 59),
            BorderColor = Color.FromArgb(226, 232, 240),
            BorderThickness = 1,
            BorderRadius = 8,
            Font = new Font("Segoe UI Semibold", 8F),
            Cursor = Cursors.Hand
        };
        btn.HoverState.FillColor = Color.FromArgb(248, 250, 252);
        btn.HoverState.BorderColor = Color.FromArgb(203, 213, 225);
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private void TriggerTool(string toolName)
    {
        _lblToolResult.Text = $"Đang thực thi: {toolName}...";
        _lblToolResult.ForeColor = Color.FromArgb(79, 70, 229);
        DeviceToolRequested?.Invoke(this, toolName);
    }

    public void SetToolResult(string message, bool isError = false)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetToolResult(message, isError)));
            return;
        }
        _lblToolResult.Text = message;
        _lblToolResult.ForeColor = isError ? Color.FromArgb(220, 38, 38) : Color.FromArgb(16, 185, 129);
    }

    // =========================================================================
    // 4. DATA BINDING & UPDATE
    // =========================================================================
    public void UpdateDashboard(
        IReadOnlyList<JobItem> jobs,
        IReadOnlyList<FolderItem> folders,
        string deviceModel,
        string deviceSerial,
        string batteryInfo,
        string resolutionInfo,
        bool isConnected,
        string aiProvider,
        bool telegramActive,
        string workflowName,
        int stepCount,
        bool isIosMode)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateDashboard(
                jobs, folders, deviceModel, deviceSerial, batteryInfo, resolutionInfo,
                isConnected, aiProvider, telegramActive, workflowName, stepCount, isIosMode)));
            return;
        }

        // Cache jobs and folders for filtering
        _allRecentJobs = jobs.ToList();
        _folderMap = folders.ToDictionary(f => f.Id, f => f.Name);

        // 1. KPI 1: Jobs
        var total = jobs.Count;
        var done = jobs.Count(j => string.Equals(j.Status, JobStatus.Succeeded, StringComparison.OrdinalIgnoreCase));
        var failed = jobs.Count(j => string.Equals(j.Status, JobStatus.Failed, StringComparison.OrdinalIgnoreCase));
        var running = jobs.Count(j => string.Equals(j.Status, JobStatus.Running, StringComparison.OrdinalIgnoreCase));
        var waiting = total - done - failed - running;
        if (waiting < 0) waiting = 0;

        _lblTotalJobs.Text = $"{total}";
        _lblDoneBadge.Text = $"+{done} thành công";
        _lblWaitingBadge.Text = $"{waiting} chờ xử lý";
        var percent = total > 0 ? (int)Math.Round((double)done / total * 100) : 0;
        _pbJobCompletion.Value = Math.Clamp(percent, 0, 100);

        // 2. KPI 2: Success Rate
        var finished = done + failed;
        var successRate = finished > 0 ? (double)done / finished * 100 : 100.0;
        _lblSuccessRate.Text = $"{successRate:F1}%";
        if (failed == 0)
        {
            _lblSuccessBadge.Text = "✓ XUẤT SẮC";
            _lblSuccessBadge.ForeColor = Color.FromArgb(3, 84, 63);
            _lblSuccessBadge.BackColor = Color.FromArgb(222, 247, 236);
            _lblSuccessRateDesc.Text = "Tỷ lệ thực thi hoàn hảo";
        }
        else if (successRate >= 80)
        {
            _lblSuccessBadge.Text = "● ỔN ĐỊNH";
            _lblSuccessBadge.ForeColor = Color.FromArgb(29, 78, 216);
            _lblSuccessBadge.BackColor = Color.FromArgb(239, 246, 255);
            _lblSuccessRateDesc.Text = $"{failed} video gặp sự cố";
        }
        else
        {
            _lblSuccessBadge.Text = "⚠️ CẦN KIỂM TRA";
            _lblSuccessBadge.ForeColor = Color.FromArgb(153, 27, 27);
            _lblSuccessBadge.BackColor = Color.FromArgb(254, 242, 242);
            _lblSuccessRateDesc.Text = $"{failed} video cần kiểm tra";
        }

        // 3. KPI 3: Device Controller
        if (isConnected)
        {
            _lblDeviceModel.Text = string.IsNullOrWhiteSpace(deviceModel) ? "Thiết bị kết nối" : deviceModel;
            _lblDeviceStatus.Text = "● Đã kết nối";
            _lblDeviceStatus.ForeColor = Color.FromArgb(3, 84, 63);
            _lblDeviceStatus.BackColor = Color.FromArgb(222, 247, 236);
            _lblDeviceDetails.Text = $"Pin: {batteryInfo} · Màn hình: {resolutionInfo}";

            _lblSystemStatus.Text = "●  HỆ THỐNG SẴN SÀNG";
            _lblSystemStatus.ForeColor = Color.FromArgb(16, 185, 129);
            _lblSystemStatus.BackColor = Color.FromArgb(236, 253, 245);

            _gridToolbox.Visible = true;
            _pnlDeviceEmptyState.Visible = false;
        }
        else
        {
            _lblDeviceModel.Text = "Chưa kết nối";
            _lblDeviceStatus.Text = "● Chờ kết nối";
            _lblDeviceStatus.ForeColor = Color.FromArgb(146, 64, 14);
            _lblDeviceStatus.BackColor = Color.FromArgb(254, 243, 199);
            _lblDeviceDetails.Text = "Cắm cáp USB hoặc quét ADB";

            _lblSystemStatus.Text = "●  CHỜ THIẾT BỊ";
            _lblSystemStatus.ForeColor = Color.FromArgb(217, 119, 6);
            _lblSystemStatus.BackColor = Color.FromArgb(254, 243, 199);

            _gridToolbox.Visible = false;
            _pnlDeviceEmptyState.Visible = true;
        }

        // 4. KPI 4: AI & Telegram
        _lblAiProvider.Text = string.IsNullOrWhiteSpace(aiProvider) ? "AI: Chưa cấu hình" : aiProvider;
        if (telegramActive)
        {
            _lblTelegramStatus.Text = "✈ Telegram: Đang bật";
            _lblTelegramStatus.ForeColor = Color.FromArgb(16, 185, 129);
        }
        else
        {
            _lblTelegramStatus.Text = "✈ Telegram: Đang tắt";
            _lblTelegramStatus.ForeColor = Color.FromArgb(100, 116, 139);
        }

        // 5. Campaign Breakdown Compact Cards
        _pnlCampaignList.Controls.Clear();
        var folderGroups = jobs.GroupBy(j => j.FolderId ?? 0).ToList();

        if (folderGroups.Count == 0)
        {
            _pnlCampaignList.Controls.Add(new Label
            {
                Text = "Chưa có chiến dịch nào. Nhập Excel để bắt đầu!",
                AutoSize = true,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                Margin = new Padding(8, 20, 0, 0)
            });
        }
        else
        {
            foreach (var grp in folderGroups.Take(6))
            {
                var fName = grp.Key == 0 ? "Chưa phân loại" : (_folderMap.TryGetValue(grp.Key, out var name) ? name : $"Chiến dịch #{grp.Key}");
                var fTotal = grp.Count();
                var fDone = grp.Count(j => string.Equals(j.Status, JobStatus.Succeeded, StringComparison.OrdinalIgnoreCase));
                var fPct = fTotal > 0 ? (int)Math.Round((double)fDone / fTotal * 100) : 0;

                var fCard = new Guna2Panel
                {
                    Size = new Size(190, 68),
                    FillColor = Color.White,
                    BorderColor = Color.FromArgb(226, 232, 240),
                    BorderThickness = 1,
                    BorderRadius = 8,
                    Margin = new Padding(0, 0, 8, 0),
                    Padding = new Padding(8, 6, 8, 6)
                };
                var lblFn = new Label { Text = fName, AutoSize = false, Size = new Size(172, 18), Location = new Point(8, 6), Font = new Font("Segoe UI Semibold", 8F), ForeColor = Color.FromArgb(15, 23, 42), AutoEllipsis = true };
                var lblFSub = new Label { Text = $"{fDone}/{fTotal} video", AutoSize = true, Location = new Point(8, 26), Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(100, 116, 139) };
                var lblFPct = new Label { Text = $"{fPct}%", AutoSize = true, Location = new Point(144, 26), Font = new Font("Segoe UI Semibold", 7.5F), ForeColor = Color.FromArgb(79, 70, 229) };

                var pbF = new Guna2ProgressBar
                {
                    Location = new Point(8, 49),
                    Size = new Size(172, 6),
                    BorderRadius = 3,
                    FillColor = Color.FromArgb(241, 245, 249),
                    ProgressColor = Color.FromArgb(99, 102, 241),
                    ProgressColor2 = Color.FromArgb(129, 140, 248),
                    Value = fPct
                };
                fCard.Controls.AddRange([lblFn, lblFSub, lblFPct, pbF]);
                _pnlCampaignList.Controls.Add(fCard);
            }
        }

        // 6. Populate Recent Jobs Table
        ApplyVideoFilters();

        // 7. Workflow Profile Details
        _lblWorkflowName.Text = string.IsNullOrWhiteSpace(workflowName) ? "Chưa chọn file" : Path.GetFileName(workflowName);
        _lblWorkflowSteps.Text = $"{stepCount} bước tự động hóa";
        _lblPlatformBadge.Text = isIosMode ? "iOS | WebDriverAgent" : "Android | ADB Shell";
        _lblPlatformBadge.ForeColor = isIosMode ? Color.FromArgb(91, 33, 182) : Color.FromArgb(3, 84, 63);
        _lblPlatformBadge.BackColor = isIosMode ? Color.FromArgb(237, 233, 254) : Color.FromArgb(222, 247, 236);
    }

    private void ApplyVideoFilters()
    {
        _dgvRecentJobs.Rows.Clear();
        var search = _txtSearchVideo?.Text?.Trim().ToLowerInvariant() ?? "";
        var filterStatus = _cboStatusFilter?.SelectedItem?.ToString() ?? "Tất cả trạng thái";

        var filtered = _allRecentJobs.AsEnumerable();

        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(j =>
                (j.Title != null && j.Title.ToLowerInvariant().Contains(search)) ||
                (j.VideoPath != null && j.VideoPath.ToLowerInvariant().Contains(search)) ||
                j.Id.ToString().Contains(search)
            );
        }

        if (filterStatus != "Tất cả trạng thái")
        {
            filtered = filtered.Where(j => string.Equals(j.Status, filterStatus, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.Take(15).ToList();
        foreach (var j in list)
        {
            var fName = (j.FolderId == null || j.FolderId == 0) ? "Chưa phân loại" : (_folderMap.TryGetValue(j.FolderId.Value, out var n) ? n : $"#{j.FolderId}");
            var title = string.IsNullOrWhiteSpace(j.Title) ? Path.GetFileName(j.VideoPath) : j.Title;
            var shopeeSt = string.IsNullOrWhiteSpace(j.ShopeeStatus) ? "Chưa đăng" : j.ShopeeStatus;

            _dgvRecentJobs.Rows.Add(
                $"#{j.Id}",
                title,
                fName,
                string.IsNullOrWhiteSpace(j.Status) ? "Chờ" : j.Status,
                shopeeSt
            );
        }

        if (_lblTableCount != null)
        {
            _lblTableCount.Text = $"Hiển thị {list.Count} / {_allRecentJobs.Count} video";
        }
    }

    private static Guna2Panel CreateCardPanel() => new()
    {
        FillColor = Color.White,
        BorderRadius = 12,
        BorderColor = Color.FromArgb(226, 232, 240),
        BorderThickness = 1
    };

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
