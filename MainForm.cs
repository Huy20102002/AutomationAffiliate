using System.Drawing;
using AdvancedSharpAdbClient.Models;
using Newtonsoft.Json;
using ReaLTaiizor.Colors;
using ReaLTaiizor.Forms;
using ShopeeVideoUploader.Helpers;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using ShopeeVideoUploader.Controls;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader;

/// <summary>Shell chính của FlowPilot: thiết bị, workflow canvas, job queue và log.</summary>
public partial class MainForm : Form
{
    private bool _isUpdatingFolders;
    private readonly AdbManager _adb = new();
    private readonly AdbActionRecorder _actionRecorder;
    private readonly ScrcpyMouseCapture _scrcpyMouseCapture = new();
    private WorkflowEngine? _engine;
    private readonly List<WorkflowStep> _androidSteps = [];
    private readonly List<WorkflowStep> _iosSteps = [];
    private List<WorkflowStep> _workflowSteps => _isIosMode ? _iosSteps : _androidSteps;
    private bool _isIosMode = false;
    private IosManager _iosManager = new();
    private IosWorkflowEngine? _iosEngine;
    private readonly List<WorkflowVariable> _variables = [];
    private string _workflowsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowPilot", "Workflows");
    private string _currentWorkflowFile = "Shopee_Upload.json";
    private List<JobItem> _jobs = [];
    private CancellationTokenSource? _cts;
    private readonly Services.DatabaseService _dbService;
    private CancellationTokenSource? _recordingCts;
    private CancellationTokenSource? _coordinateCaptureCts;
    private DeviceData? _currentDevice;
    private DeviceInfo? _currentDeviceInfo;
    private List<DeviceInfo> _deviceList = [];
    private System.Windows.Forms.Timer? _fadeTimer;
    private double _fadeValue;
    private Button? _navTikTok;
    private TikTokDownloaderControl? _tikTokDownloaderControl;
    private Button? _navSettings;
    private SettingsControl? _settingsControl;
    private Control? _activeView;
    private readonly Services.TelegramConfigService _telegramConfigService = new();
    private readonly Services.AiConfigService _aiConfigService = new();
    private TelegramConfig _telegramConfig = new();
    private TelegramBotService? _telegramBotService;
    private bool _isGeneratingAiTitles = false;

    private sealed record VideoSourceChoice(VideoSourceMode Mode)
    {
        public override string ToString() => Mode switch
        {
            VideoSourceMode.ExcelPath => "Lấy VideoPath từ danh sách sản phẩm / Excel",
            VideoSourceMode.FolderAndExcelFileName => "Thư mục + tên file từ danh sách sản phẩm / Excel",
            VideoSourceMode.FixedFile => "Một file cố định",
            _ => Mode.ToString()
        };
    }

    public MainForm()
    {
        PreventSleep();
        _dbService = new Services.DatabaseService();
        try { _dbService.InitializeDatabase(); _ = Task.Run(() => _dbService.CreateDailyBackup()); } catch (Exception ex) { Logger.Error("Lỗi khởi tạo DB", ex); }
        _actionRecorder = new AdbActionRecorder(_adb);
        InitializeComponent();
        InitializeTikTokDownloader();
        InitializeTelegramBot();
        InitializeSettingsModule();
        ApplyLightTheme();
        LayoutRoot();
        Resize += (_, _) => LayoutRoot();
        KeyPreview = true;
        Logger.Initialize();
        Logger.OnLog += OnLogReceived;
        InitializeVariables();
        LoadAutoSavedWorkflow();
        WireEvents();
        Logger.Info("FlowPilot đã khởi động.");
        _ = InitAdbAsync();
        ShowOverviewView();
    }


    private void LayoutRoot()
    {
        const int sidebarWidth = 224;
        var topInset = Math.Max(0, Padding.Top);
        var bottomInset = Math.Max(0, Padding.Bottom);
        var contentHeight = Math.Max(0, ClientSize.Height - topInset - bottomInset);

        sidebar.Dock = DockStyle.None;
        sidebar.SetBounds(0, topInset, sidebarWidth, contentHeight);
        sidebar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        mainContent.Dock = DockStyle.None;
        mainContent.SetBounds(sidebarWidth, topInset, Math.Max(0, ClientSize.Width - sidebarWidth), contentHeight);
        mainContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        sidebar.BringToFront();
    }

    private void WireEvents()
    {
        btnRefreshDevices.Click += async (_, _) => await RefreshDevicesAsync();
        btnConnect.Click += async (_, _) => await ConnectDeviceAsync();
        btnDisconnect.Click += (_, _) => DisconnectDevice();
        cboMainFolderSelect.SelectedIndexChanged += (_, _) => 
        {
            if (_isUpdatingFolders) return;
            if (cboMainFolderSelect.SelectedItem is ShopeeVideoUploader.Models.FolderItem folder)
            {
                productListControl.FilterByFolder(folder.Id);
                FilterJobsGrid();
            }
        };

        var btnManageFoldersTop = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "⚙️", Location = new Point(cboMainFolderSelect.Right + 5, cboMainFolderSelect.Top), Size = new Size(32, 32),
            FillColor = Color.FromArgb(244, 248, 252), BorderRadius = 6,
            BorderColor = Color.FromArgb(190, 198, 211), BorderThickness = 1,
            ForeColor = Color.FromArgb(31, 31, 44), Cursor = Cursors.Hand
        };
        btnManageFoldersTop.Click += (_, _) => ShowFolderManager();
        if (cboMainFolderSelect.Parent != null) cboMainFolderSelect.Parent.Controls.Add(btnManageFoldersTop);

        var btnTelegramTop = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "✈️ Telegram", Location = new Point(btnManageFoldersTop.Right + 8, cboMainFolderSelect.Top), Size = new Size(110, 32),
            FillColor = Color.FromArgb(244, 248, 252), BorderRadius = 6,
            BorderColor = Color.FromArgb(190, 198, 211), BorderThickness = 1,
            ForeColor = Color.FromArgb(31, 31, 44), Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 8.5F)
        };
        btnTelegramTop.Click += (_, _) => ShowSettingsView();
        if (cboMainFolderSelect.Parent != null) cboMainFolderSelect.Parent.Controls.Add(btnTelegramTop);

        btnAddStep.Click += (_, _) => AddStep();
        btnRemoveStep.Click += (_, _) => RemoveStep();
        btnMoveUp.Click += (_, _) => MoveStep(-1);
        btnMoveDown.Click += (_, _) => MoveStep(1);
        btnApplyConfig.Click += (_, _) => ApplyStepConfig();
        btnClearWorkflow.Click += (_, _) => ClearWorkflow();
        btnSaveWorkflow.Click += (_, _) => SaveWorkflow();
        btnLoadWorkflow.Click += (_, _) => LoadWorkflow();
        btnImportExcel.Click += (_, _) => ImportExcel();
        btnExportTemplate.Click += (_, _) => ExportTemplate();
        chkOnlyWithLink.CheckedChanged += (_, _) =>
        {
            FilterJobsGrid();
            if (_telegramConfig != null && _telegramConfig.OnlyRunWithLink != chkOnlyWithLink.Checked)
            {
                _telegramConfig.OnlyRunWithLink = chkOnlyWithLink.Checked;
                _telegramConfigService.Save(_telegramConfig);
                _telegramBotService?.UpdateConfig(_telegramConfig);
            }
        };
        btnStart.Click += async (_, _) => await StartRunAsync();
        btnTestWorkflow.Click += async (_, _) => await StartTestWorkflowAsync();
        btnStop.Click += (_, _) => StopRun();
        btnRecordActions.Click += async (_, _) => await StartRecordingAsync();
        btnStopRecording.Click += (_, _) => StopRecording();
        btnZoomOut.Click += (_, _) => workflowCanvas.ZoomOut();
        btnZoomReset.Click += (_, _) => workflowCanvas.ResetZoom();
        btnZoomIn.Click += (_, _) => workflowCanvas.ZoomIn();
        workflowCanvas.ZoomChanged += (_, zoom) =>
        {
            btnZoomReset.Text = $"{zoom:0}%";
            btnZoomReset.Invalidate();
        };
        foreach (var block in workflowCorePalette.Controls.OfType<Button>().Concat(workflowPalette.Controls.OfType<Button>()))
        {
            if (block.Tag is not StepType type) continue;
            block.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    block.DoDragDrop(type.ToString(), DragDropEffects.Copy);
            };
            block.MouseEnter += (_, _) => block.BackColor = Color.FromArgb(229, 242, 250);
            block.MouseLeave += (_, _) => block.BackColor = Color.White;
        }
        lstVariables.SelectedIndexChanged += (_, _) => LoadVariableEditor();
        lstVariables.DoubleClick += (_, _) => InsertSelectedVariable();
        btnAddVariable.Click += (_, _) => AddOrUpdateVariable();
        btnRemoveVariable.Click += (_, _) => RemoveVariable();
        btnInsertVariable.Click += (_, _) => InsertSelectedVariable();
        btnBrowseVideoPath.Click += (_, _) => BrowseVideoPath();
        btnBrowseTapImage.Click += (_, _) => BrowseTapImage();
        btnInspectUi.Click += async (_, _) => await InspectUiElementsAsync();
        btnRefreshApps.Click += async (_, _) => await RefreshInstalledAppsAsync();
        btnCaptureCurrentApp.Click += async (_, _) => await CaptureCurrentAppAsync();
        btnCaptureTapCoordinates.Click += async (_, _) => await CaptureTapCoordinatesAsync();
        btnCaptureSwipeCoordinates.Click += async (_, _) => await CaptureSwipeCoordinatesAsync();
        btnCaptureRandomTapCoordinate.Click += async (_, _) => await CaptureRandomTapCoordinateAsync();
        btnAddManualCoordinate.Click += (_, _) => AddManualRandomCoordinate();
        btnRemoveRandomCoordinate.Click += (_, _) => RemoveSelectedRandomCoordinate();
        btnClearRandomCoordinates.Click += (_, _) => ClearRandomCoordinates();
        lstRandomCoordinates.SelectedIndexChanged += (_, _) =>
        {
            var idx = workflowCanvas.SelectedIndex;
            if (idx < 0 || idx >= _workflowSteps.Count) return;
            var step = _workflowSteps[idx];
            if (step.Type != StepType.RandomTap) return;

            var sel = lstRandomCoordinates.SelectedIndex;
            if (sel >= 0 && sel < step.RandomCoordinates.Count)
            {
                txtX.Text = step.RandomCoordinates[sel].X.ToString();
                txtY.Text = step.RandomCoordinates[sel].Y.ToString();
            }
        };
        btnGroupToRandomTap.Click += (_, _) => GroupSelectedStepsToRandomTap();
        workflowCanvas.MultiSelectionChanged += (_, selectedIndices) =>
        {
            if (selectedIndices.Count > 1)
            {
                var tapCount = selectedIndices.Count(i => i >= 0 && i < _workflowSteps.Count &&
                    (_workflowSteps[i].Type == StepType.Tap || _workflowSteps[i].Type == StepType.RandomTap));
                btnGroupToRandomTap.Text = $"🎲 Gom {selectedIndices.Count} bước";
                btnGroupToRandomTap.Visible = true;
                SetStatus($"Đang bôi chọn {selectedIndices.Count} bước ({tapCount} bước Chạm). Nhấn 'Gom bước' hoặc chuột phải để gộp thành Chạm ngẫu nhiên.");
            }
            else
            {
                btnGroupToRandomTap.Visible = false;
            }
        };
        cboTapMode.SelectedIndexChanged += (_, _) => UpdateTapModeFields();
        cboVideoSource.SelectedIndexChanged += (_, _) => UpdateVideoSourceHint();
        cboWorkflowProfiles.SelectedIndexChanged += (_, _) => SwitchWorkflowProfile();
        btnAddWorkflowProfile.Click += (_, _) => CreateNewWorkflowProfile();
        btnManualSaveProfile.Click += (_, _) => { SaveAutoSavedWorkflow(); SetStatus($"Đã lưu vào {_currentWorkflowFile}"); };
        btnExportProfile.Click += (_, _) => SaveWorkflow();
        btnLoadWorkflow.Click += (_, _) => LoadWorkflow();
        btnDeleteWorkflowProfile.Click += (_, _) => DeleteWorkflowProfile();
        btnRenameWorkflowProfile.Click += (_, _) => RenameWorkflowProfile();
        btnManageWorkflows.Click += (_, _) => ShowWorkflowManager();

        var cboMenu = new ContextMenuStrip();
        var mnuDelete = cboMenu.Items.Add("🗑️ Xóa quy trình này");
        mnuDelete.Click += (_, _) => DeleteWorkflowProfile();
        var mnuRename = cboMenu.Items.Add("✏️ Đổi tên quy trình");
        mnuRename.Click += (_, _) => RenameWorkflowProfile();
        var mnuClone = cboMenu.Items.Add("📋 Nhân bản quy trình");
        mnuClone.Click += (_, _) => CloneCurrentWorkflowProfile();
        cboMenu.Items.Add(new ToolStripSeparator());
        var mnuManage = cboMenu.Items.Add("⚙️ Quản lý tất cả quy trình...");
        mnuManage.Click += (_, _) => ShowWorkflowManager();
        var mnuNew = cboMenu.Items.Add("＋ Tạo quy trình mới...");
        mnuNew.Click += (_, _) => CreateNewWorkflowProfile();
        cboWorkflowProfiles.ContextMenuStrip = cboMenu;
        navOverview.Click += (_, _) => ShowOverviewView();
        navWorkflow.Click += (_, _) => ShowWorkflowView();
        navWorkflowIos.Click += (_, _) => ShowIosWorkflowView();
        navProducts.Click += (_, _) => ShowProductView();
        if (_navTikTok != null) _navTikTok.Click += (_, _) => ShowTikTokDownloaderView();
        _overviewControl.RunRequested += async (_, _) => await StartRunAsync();
        _overviewControl.ImportRequested += (_, _) => ImportExcel();
        _overviewControl.CampaignsRequested += (_, _) => ShowFolderManager();
        _overviewControl.RefreshRequested += (_, _) => RefreshOverviewDashboard();
        _overviewControl.WorkflowRequested += (_, _) => ShowWorkflowView();
        _overviewControl.TestWorkflowRequested += async (_, _) => await StartTestWorkflowAsync();
        _overviewControl.SettingsRequested += (_, _) => ShowSettingsView();
        _overviewControl.DeviceToolRequested += async (_, tool) => await HandleOverviewDeviceToolAsync(tool);
        // productListControl.FolderCrudRequested removed
        productListControl.ImportRequested += (_, _) => ImportExcel();
        productListControl.AddRequested += (_, _) => AddProduct();
        productListControl.EditRequested += (_, _) => EditProduct();
        productListControl.ProductChanged += (_, e) => ApplyProductChange(e);
        productListControl.DeleteRequested += (_, _) => DeleteProduct();
        productListControl.RunWorkflowRequested += async (_, _) => await StartRunAsync();
        productListControl.RunSelectedWorkflowRequested += async (_, _) => await StartRunAsync(onlySelected: true);
        productListControl.StopWorkflowRequested += (_, _) => StopRun();
        productListControl.ResetStatusesRequested += (_, _) => ResetProductStatuses();
        productListControl.ResetSelectedStatusesRequested += (_, _) => ResetSelectedProductStatuses();
        productListControl.WorkflowRequested += (_, _) => ShowWorkflowView();
        productListControl.CampaignsRequested += (_, _) => ShowFolderManager();
        productListControl.GenerateAiTitleRequested += async (_, _) => await GenerateAiTitlesBatchAsync(onlySelected: false, forceRegenerate: false);
        productListControl.GenerateSelectedAiTitleRequested += async (_, _) => await GenerateAiTitlesBatchAsync(onlySelected: true, forceRegenerate: false);
        productListControl.RegenerateAiTitleRequested += async (_, _) => await GenerateAiTitlesBatchAsync(onlySelected: false, forceRegenerate: true);
        productListControl.RegenerateSelectedAiTitleRequested += async (_, _) => await GenerateAiTitlesBatchAsync(onlySelected: true, forceRegenerate: true);
        productListControl.BackupDbRequested += (_, _) => BackupDatabase();
        productListControl.RestoreDbRequested += (_, _) => RestoreDatabase();
        workflowCanvas.StepSelected += (_, index) =>
        {
            LoadStepConfig(index);
        };
        workflowCanvas.StepEditRequested += (_, index) => LoadStepConfig(index);
        workflowCanvas.StepContextRequested += (_, index) => ShowStepContextMenu(index);
        workflowCanvas.StepReorderRequested += (_, order) =>
        {
            if (order.From < 0 || order.From >= _workflowSteps.Count || order.To < 0 || order.To >= _workflowSteps.Count) return;
            var step = _workflowSteps[order.From];
            _workflowSteps.RemoveAt(order.From);
            _workflowSteps.Insert(order.To, step);
            RefreshWorkflow();
            workflowCanvas.SelectStep(order.To);
            LoadStepConfig(order.To);
            SetStatus($"Đã đổi thứ tự step {order.From + 1} → {order.To + 1}");
        };
        workflowCanvas.StepDropRequested += (_, request) => AddStep(request.Type, request.Location);
        FormClosing += MainForm_FormClosing;
        KeyDown += MainForm_KeyDown;
    }

    private void InitializeVariables()
    {
        _variables.AddRange([
            new WorkflowVariable { Name = "Title", Value = "Tiêu đề từ Excel hiện tại", IsBuiltIn = true },
            new WorkflowVariable { Name = "ShopeeAffLink", Value = "Liên kết tiếp thị từ Excel", IsBuiltIn = true },
            new WorkflowVariable { Name = "VideoPath", Value = "Đường dẫn video từ Excel", IsBuiltIn = true },
            new WorkflowVariable { Name = "Id", Value = "Mã công việc từ Excel", IsBuiltIn = true },
            new WorkflowVariable { Name = "DeviceSerial", Value = "Chưa kết nối", IsBuiltIn = true, IsDeviceBuiltIn = true },
            new WorkflowVariable { Name = "DeviceModel", Value = "Chưa kết nối", IsBuiltIn = true, IsDeviceBuiltIn = true },
            new WorkflowVariable { Name = "AndroidVersion", Value = "Chưa kết nối", IsBuiltIn = true, IsDeviceBuiltIn = true },
            new WorkflowVariable { Name = "ScreenWidth", Value = "0", IsBuiltIn = true, IsDeviceBuiltIn = true },
            new WorkflowVariable { Name = "ScreenHeight", Value = "0", IsBuiltIn = true, IsDeviceBuiltIn = true }
        ]);
        RefreshVariableList();
        LoadVariableEditor();
    }

    private void RefreshVariableList()
    {
        lstVariables.DataSource = null;
        lstVariables.DataSource = _variables;
        if (lstVariables.Items.Count > 0 && lstVariables.SelectedIndex < 0)
            lstVariables.SelectedIndex = 0;
    }

    private void LoadVariableEditor()
    {
        if (lstVariables.SelectedItem is not WorkflowVariable variable) return;
        txtVariableName.Text = variable.Name;
        txtVariableValue.Text = variable.Value;
        txtVariableValue.Enabled = !variable.IsBuiltIn;
        btnRemoveVariable.Enabled = !variable.IsBuiltIn;
        btnAddVariable.Enabled = true;
    }

    private void AddOrUpdateVariable()
    {
        var name = txtVariableName.Text.Trim().Trim('{', '}').Replace(" ", "_");
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Nhập tên biến trước.");
            return;
        }
        if (_variables.Any(v => v.IsBuiltIn && v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            ShowError("Tên này là biến hệ thống và không thể sửa.");
            return;
        }

        var variable = _variables.FirstOrDefault(v => !v.IsBuiltIn && v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (variable == null)
        {
            variable = new WorkflowVariable { Name = name };
            _variables.Add(variable);
        }
        variable.Value = txtVariableValue.Text;
        RefreshVariableList();
        lstVariables.SelectedItem = variable;
        SetStatus($"Đã lưu biến {variable.Token}");
    }

    private void RemoveVariable()
    {
        if (lstVariables.SelectedItem is not WorkflowVariable variable || variable.IsBuiltIn) return;
        _variables.Remove(variable);
        RefreshVariableList();
        SetStatus($"Đã xóa biến {variable.Token}");
    }

    private void InsertSelectedVariable()
    {
        if (lstVariables.SelectedItem is not WorkflowVariable variable) return;
        var token = variable.Token;
        txtTextValue.Focus();
        var start = txtTextValue.SelectionStart;
        txtTextValue.Text = txtTextValue.Text.Insert(start, token);
        txtTextValue.SelectionStart = start + token.Length;
        SetStatus($"Đã chèn {token} vào Text / Package");
    }

    /// <summary>Theme Dashboard GemLogin-style</summary>
    private void ApplyLightTheme()
    {
        var primary = Color.FromArgb(79, 70, 229); // Modern Indigo-600
        var page = Color.FromArgb(244, 245, 248);
        var card = Color.White;
        var ink = Color.FromArgb(31, 31, 44);
        var muted = Color.FromArgb(114, 117, 134);
        var danger = Color.FromArgb(226, 82, 82);

        BackColor = page;
        ForeColor = ink;
        sidebar.BackColor = Color.White;
        mainContent.BackColor = page;
        topBar.BackColor = card;
        panelWorkflowContainer.BackColor = card;
        panelInspector.BackColor = card;
        panelBottom.BackColor = card;
        panelStatusBar.BackColor = card;
        sidebar.Width = 236;
        sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 234, 240));
            e.Graphics.DrawLine(pen, sidebar.ClientSize.Width - 1, 0, sidebar.ClientSize.Width - 1, sidebar.ClientSize.Height);
        };
        topBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 234, 240));
            e.Graphics.DrawLine(pen, 0, topBar.ClientSize.Height - 1, topBar.ClientSize.Width, topBar.ClientSize.Height - 1);
        };
        panelStatusBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 234, 240));
            e.Graphics.DrawLine(pen, 0, 0, panelStatusBar.ClientSize.Width, 0);
        };
        lblStatus.ForeColor = muted;
        lblDeviceBadge.ForeColor = Color.FromArgb(220, 151, 38);

        // Nút chính
        if (btnStart is Guna.UI2.WinForms.Guna2Button startBtn) startBtn.FillColor = primary;
        if (btnConnect is Guna.UI2.WinForms.Guna2Button connectBtn) connectBtn.FillColor = primary;
        if (btnTestWorkflow is Guna.UI2.WinForms.Guna2Button testBtn)
        {
            testBtn.FillColor = Color.FromArgb(238, 242, 255);
            testBtn.ForeColor = Color.FromArgb(79, 70, 229);
            testBtn.BorderColor = Color.FromArgb(199, 210, 254);
            testBtn.BorderThickness = 1;
        }
        if (btnStop is Guna.UI2.WinForms.Guna2Button stopBtn)
        {
            stopBtn.FillColor = danger;
            stopBtn.ForeColor = Color.White;
            stopBtn.DisabledState.FillColor = Color.FromArgb(243, 244, 246);
            stopBtn.DisabledState.ForeColor = Color.FromArgb(156, 163, 175);
            stopBtn.DisabledState.BorderColor = Color.FromArgb(229, 231, 235);
        }
        if (btnDisconnect is Guna.UI2.WinForms.Guna2Button disconBtn)
        {
            disconBtn.FillColor = Color.FromArgb(254, 242, 242);
            disconBtn.ForeColor = Color.FromArgb(220, 38, 38);
            disconBtn.BorderColor = Color.FromArgb(254, 202, 202);
            disconBtn.BorderThickness = 1;
            disconBtn.DisabledState.FillColor = Color.FromArgb(243, 244, 246);
            disconBtn.DisabledState.ForeColor = Color.FromArgb(156, 163, 175);
            disconBtn.DisabledState.BorderColor = Color.FromArgb(229, 231, 235);
        }

        btnStart.Text = "▶  CHẠY QUY TRÌNH";
        btnStop.Text = "■  DỪNG";
        btnConnect.Text = "Kết nối";
        btnDisconnect.Text = "Ngắt";
        btnTestWorkflow.Text = "▷  CHẠY THỬ";
        btnManualSaveProfile.Text = "💾 Lưu";
        btnAddWorkflowProfile.Text = "＋ Mới";
        btnLoadWorkflow.Text = "📥 Nhập";
        btnExportProfile.Text = "📤 Xuất";
        btnRenameWorkflowProfile.Text = "✏️ Sửa";
        btnDeleteWorkflowProfile.Text = "🗑️ Xóa";
        btnManageWorkflows.Text = "📋 Quản lý";
        btnAddStep.Text = "＋ Thêm bước";
        btnRemoveStep.Text = "Xóa";
        btnSaveWorkflow.Text = "Xuất";
        btnClearWorkflow.Text = "Làm sạch";
        btnApplyConfig.Text = "Áp dụng";
        btnRecordActions.Text = "● Ghi thao tác";
        btnStopRecording.Text = "■ Dừng ghi";

        foreach (var panel in GetAllControls(this).OfType<Panel>())
        {
            if (panel != sidebar && !IsInside(panel, sidebar)) 
                panel.BackColor = card;
        }
        panelStatusBar.BackColor = card;
        topBar.BackColor = card;

        // Sidebar nav buttons — GemLogin style: active = gradient purple pill, others = transparent + dark text
        foreach (var button in GetAllControls(sidebar).OfType<Guna.UI2.WinForms.Guna2Button>())
        {
            button.BorderRadius = 10;
            button.Font = new Font("Segoe UI Semibold", 9.5F);
            button.Height = 42;
            button.TextAlign = HorizontalAlignment.Left;
        }
        SetActiveNavButton(navWorkflow); // re-apply active highlight

        // Sidebar labels — GemLogin style: brand purple, section headers gray, body text dark
        foreach (var label in GetAllControls(this).OfType<Label>())
        {
            if (IsInside(label, sidebar))
            {
                label.BackColor = Color.Transparent;
                if (label.Font.Size >= 14F)
                    label.ForeColor = Color.FromArgb(96, 82, 218); // brand purple
                else if (label.Text.StartsWith("MULTI-PLATFORM") || label.Text.StartsWith("BỘ MÁY") || label.Text.StartsWith("QUY TRÌNH ADB"))
                    label.ForeColor = Color.FromArgb(160, 165, 180);
                else if (label.Text.StartsWith("KHÔNG GIAN") || label.Text.StartsWith("SẢN PHẨM") || label.Text.StartsWith("GIAO DỊCH") || label.Text.StartsWith("HỖ TRỢ"))
                    label.ForeColor = Color.FromArgb(140, 145, 165);
                else if (label.Text.StartsWith("SẴN SÀNG"))
                    label.ForeColor = Color.FromArgb(96, 82, 218);
                else
                    label.ForeColor = Color.FromArgb(55, 58, 75);
            }
            else
            {
                label.BackColor = Color.Transparent;
            }
        }

        foreach (var panel in GetAllControls(sidebar).OfType<Panel>())
            panel.BackColor = Color.Transparent;

        foreach (var button in GetAllControls(this).OfType<Guna.UI2.WinForms.Guna2Button>())
        {
            if (IsInside(button, sidebar) || button == btnStart || button == btnTestWorkflow || button == btnStop ||
                button == btnConnect || button == btnDisconnect || button == btnRefreshDevices || IsInside(button, productListControl) ||
                button == btnLoadWorkflow || button == btnExportProfile || button == btnManualSaveProfile ||
                button == btnAddWorkflowProfile || button == btnRenameWorkflowProfile || button == btnDeleteWorkflowProfile ||
                button == btnManageWorkflows ||
                button == btnApplyConfig || button == btnCaptureTapCoordinates || button == btnCaptureSwipeCoordinates ||
                button == btnCaptureRandomTapCoordinate || button == btnAddManualCoordinate || button == btnRemoveRandomCoordinate || button == btnClearRandomCoordinates ||
                button == btnImportExcel || button == btnExportTemplate || button == btnExportResult) continue;
            button.FillColor = Color.FromArgb(235, 237, 242);
            button.ForeColor = Color.FromArgb(70, 70, 85);
        }

        if (btnRefreshDevices is Guna.UI2.WinForms.Guna2Button rfBtn)
        {
            rfBtn.FillColor = Color.FromArgb(243, 244, 246);
            rfBtn.ForeColor = Color.FromArgb(75, 85, 99);
            rfBtn.BorderColor = Color.FromArgb(209, 213, 219);
            rfBtn.BorderThickness = 1;
        }

        if (btnImportExcel is Guna.UI2.WinForms.Guna2Button bie)
        {
            bie.FillColor = Color.FromArgb(96, 82, 218);
            bie.ForeColor = Color.White;
        }
        if (btnExportTemplate is Guna.UI2.WinForms.Guna2Button bet)
        {
            bet.FillColor = Color.FromArgb(243, 244, 246);
            bet.ForeColor = Color.FromArgb(55, 65, 81);
            bet.BorderColor = Color.FromArgb(209, 213, 219);
            bet.BorderThickness = 1;
        }
        if (btnExportResult is Guna.UI2.WinForms.Guna2Button ber)
        {
            ber.FillColor = Color.FromArgb(238, 242, 255);
            ber.ForeColor = Color.FromArgb(79, 70, 229);
            ber.BorderColor = Color.FromArgb(199, 210, 254);
            ber.BorderThickness = 1;
        }

        if (btnApplyConfig is Guna.UI2.WinForms.Guna2Button applyBtn)
        {
            applyBtn.FillColor = Color.FromArgb(16, 185, 129);
            applyBtn.ForeColor = Color.White;
            applyBtn.BorderRadius = 6;
        }

        foreach (var control in GetAllControls(this))
        {
            if (control is Label label && !IsInside(control, sidebar))
            {
                label.ForeColor = label == lblStepCount || label == lblSelectedStep
                    ? primary
                    : ink;
            }
            else if (control is Guna.UI2.WinForms.Guna2TextBox hopeTxt)
            {
                hopeTxt.FillColor = Color.White;
                hopeTxt.BorderRadius = 7;
                hopeTxt.FocusedState.BorderColor = primary;
                hopeTxt.BorderColor = Color.FromArgb(180, 185, 200); // Darker border
                hopeTxt.ForeColor = ink;
            }
            else if (control is Guna.UI2.WinForms.Guna2ComboBox hopeCbo)
            {
                hopeCbo.BorderRadius = 7;
                hopeCbo.ForeColor = ink;
            }
        }

        cboDevices.BackColor = card;
        cboDevices.FillColor = card;
        cboStepType.BackColor = card;
        cboStepType.FillColor = card;
        workflowCanvas.ApplyTheme(true);

        foreach (var panel in new[] { panelWorkflowContainer, panelInspector, panelBottom })
        {
            panel.BorderRadius = 6;
            panel.BorderThickness = 1;
            panel.BorderColor = Color.FromArgb(232, 234, 240);
        }

        dgvJobs.BackgroundColor = Color.White;
        dgvJobs.GridColor = Color.FromArgb(240, 242, 245);
        dgvJobs.ThemeStyle.GridColor = Color.FromArgb(240, 242, 245);

        dgvJobs.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(248, 249, 251);
        dgvJobs.ThemeStyle.HeaderStyle.ForeColor = Color.FromArgb(75, 85, 99);
        dgvJobs.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI Semibold", 9F);
        dgvJobs.ThemeStyle.HeaderStyle.Height = 38;

        dgvJobs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 251);
        dgvJobs.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(75, 85, 99);
        dgvJobs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        dgvJobs.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
        dgvJobs.ColumnHeadersHeight = 38;

        dgvJobs.DefaultCellStyle.BackColor = Color.White;
        dgvJobs.DefaultCellStyle.ForeColor = ink;
        dgvJobs.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        dgvJobs.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        dgvJobs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
        dgvJobs.DefaultCellStyle.SelectionForeColor = ink;

        dgvJobs.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);
        dgvJobs.AlternatingRowsDefaultCellStyle.ForeColor = ink;
        dgvJobs.AlternatingRowsDefaultCellStyle.Font = new Font("Segoe UI", 9F);
        dgvJobs.AlternatingRowsDefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        dgvJobs.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
        dgvJobs.AlternatingRowsDefaultCellStyle.SelectionForeColor = ink;

        dgvJobs.ThemeStyle.RowsStyle.BackColor = Color.White;
        dgvJobs.ThemeStyle.RowsStyle.ForeColor = ink;
        dgvJobs.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
        dgvJobs.ThemeStyle.RowsStyle.SelectionForeColor = ink;
        dgvJobs.ThemeStyle.RowsStyle.Height = 38;

        dgvJobs.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(250, 251, 253);
        dgvJobs.ThemeStyle.AlternatingRowsStyle.ForeColor = ink;
        dgvJobs.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
        dgvJobs.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = ink;

        dgvJobs.ScrollBars = ScrollBars.Both;
        dgvJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        dgvJobs.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        dgvJobs.RowTemplate.Height = 38;
        txtLog.BackColor = Color.FromArgb(248, 251, 253);
        txtLog.ForeColor = Color.FromArgb(49, 95, 125);
    }

    private static IEnumerable<Control> GetAllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in GetAllControls(child)) yield return nested;
        }
    }

    private static bool IsInside(Control control, Control parent)
    {
        for (var current = control.Parent; current != null; current = current.Parent)
            if (current == parent) return true;
        return false;
    }

    private async Task InitAdbAsync()
    {
        SetStatus("Đang khởi tạo ADB...");
        var ok = await _adb.InitializeAsync();
        SetStatus(ok ? "ADB sẵn sàng" : "ADB lỗi · kiểm tra adb.exe");
        if (ok) await RefreshDevicesAsync();
    }

    private async Task RefreshDevicesAsync()
    {
        SetStatus("Đang quét thiết bị...");
        cboDevices.Items.Clear();

        if (_isIosMode)
        {
            var ports = await _iosManager.GetConnectedWdaPortsAsync();
            foreach (var port in ports) cboDevices.Items.Add($"iOS (Port {port})");
            if (cboDevices.Items.Count > 0) cboDevices.SelectedIndex = 0;
            SetStatus($"Tìm thấy {ports.Count} thiết bị iOS");
        }
        else
        {
            _deviceList = await _adb.GetConnectedDevicesAsync();
            foreach (var device in _deviceList) cboDevices.Items.Add(device);
            if (cboDevices.Items.Count > 0) cboDevices.SelectedIndex = 0;
            SetStatus($"Tìm thấy {_deviceList.Count} thiết bị Android");
        }
    }

    private string _currentIosDeviceId = "8100";

    private async Task ConnectDeviceAsync()
    {
        if (_isIosMode)
        {
            if (cboDevices.SelectedItem is string iosDeviceStr)
            {
                // Format is "iOS (Port 8100)"
                var port = iosDeviceStr.Replace("iOS (Port ", "").Replace(")", "");
                _currentIosDeviceId = port;
                SetStatus($"Đã chọn thiết bị iOS tại cổng {port}");
                
                // Hiển thị tên thiết bị trên Badge
                lblDeviceBadge.Text = $"iOS WDA";
                lblDeviceBadge.BackColor = Color.FromArgb(76, 175, 80); // Green
                RefreshOverviewDashboard();
            }
            return;
        }

        if (cboDevices.SelectedItem is not DeviceInfo info) return;
        _currentDevice = await _adb.GetDeviceBySerialAsync(info.Serial);
        if (_currentDevice == null)
        {
            ShowError("Không thể kết nối thiết bị.");
            return;
        }

        SetStatus($"Đang kết nối {info.Serial}...");
        btnConnect.Enabled = false;
        btnDisconnect.Enabled = true;
        lblDeviceBadge.Text = $"●  {info.Model} · {info.Serial}";
        lblDeviceBadge.ForeColor = Color.FromArgb(3, 84, 63);
        lblDeviceBadge.BackColor = Color.FromArgb(222, 247, 236);
        _currentDeviceInfo = info;
        // Không đổi IME sang ADBKeyboard. Nhập văn bản sẽ đi qua scrcpy clipboard.
        await UpdateDeviceVariablesAsync(info);
        SetStatus($"Đã kết nối: {info.Model}");
        RefreshOverviewDashboard();
    }

    private void DisconnectDevice()
    {
        StopRecording();
        _currentDevice = null;
        _currentDeviceInfo = null;
        btnConnect.Enabled = true;
        btnDisconnect.Enabled = false;
        lblDeviceBadge.Text = "●  Chưa kết nối thiết bị";
        lblDeviceBadge.ForeColor = Color.FromArgb(146, 64, 14);
        lblDeviceBadge.BackColor = Color.FromArgb(254, 243, 199);
        SetDeviceVariable("DeviceSerial", "Chưa kết nối");
        SetDeviceVariable("DeviceModel", "Chưa kết nối");
        SetDeviceVariable("AndroidVersion", "Chưa kết nối");
        SetDeviceVariable("ScreenWidth", "0");
        SetDeviceVariable("ScreenHeight", "0");
        RefreshVariableList();
        SetStatus("Đã ngắt kết nối");
        RefreshOverviewDashboard();
    }

    private async Task UpdateDeviceVariablesAsync(DeviceInfo info)
    {
        SetDeviceVariable("DeviceSerial", info.Serial);
        SetDeviceVariable("DeviceModel", info.Model);
        SetDeviceVariable("AndroidVersion", info.AndroidVersion);

        var width = "0";
        var height = "0";
        if (_currentDevice != null)
        {
            try
            {
                var size = await _adb.ShellAsync(_currentDevice, "wm size");
                var dimension = size.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault(line => line.Contains("x", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(dimension))
                {
                    var values = dimension[(dimension.LastIndexOf(' ') + 1)..].Split('x');
                    if (values.Length == 2)
                    {
                        width = values[0];
                        height = values[1];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Không đọc được kích thước màn hình: {ex.Message}");
            }
        }

        SetDeviceVariable("ScreenWidth", width);
        SetDeviceVariable("ScreenHeight", height);
        RefreshVariableList();
    }

    private void SetDeviceVariable(string name, string value)
    {
        var variable = _variables.FirstOrDefault(v => v.IsDeviceBuiltIn && v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (variable != null) variable.Value = value;
    }

    private async Task StartRecordingAsync()
    {
        if (_currentDevice == null)
        {
            ShowError("Hãy kết nối thiết bị trước khi ghi thao tác.");
            return;
        }

        if (_recordingCts != null) return;

        var device = _currentDevice;
        var cts = new CancellationTokenSource();
        _recordingCts = cts;
        btnRecordActions.Enabled = false;
        btnStopRecording.Enabled = true;
        SetStatus("Đang ghi thao tác từ thiết bị...");
        Logger.Info("Bắt đầu ghi thao tác qua ADB getevent.");

        try
        {
            await _actionRecorder.RecordAsync(device, step =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(() => AddRecordedStep(step));
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Đã dừng ghi thao tác.");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi ghi thao tác ADB", ex);
            if (!IsDisposed) ShowError($"Không thể ghi thao tác: {ex.Message}");
        }
        finally
        {
            if (ReferenceEquals(_recordingCts, cts))
            {
                _recordingCts = null;
                cts.Dispose();
                btnRecordActions.Enabled = true;
                btnStopRecording.Enabled = false;
                SetStatus("Đã dừng ghi thao tác");
            }
        }
    }

    private void StopRecording()
    {
        _recordingCts?.Cancel();
        btnRecordActions.Enabled = true;
        btnStopRecording.Enabled = false;
    }

    private void AddRecordedStep(WorkflowStep step)
    {
        if (IsDisposed) return;
        _workflowSteps.Add(step);
        RefreshWorkflow();
        workflowCanvas.SelectStep(_workflowSteps.Count - 1);
        LoadStepConfig(_workflowSteps.Count - 1);
        SetStatus($"Đã ghi: {WorkflowStep.GetTypeLabel(step.Type)}");
        Logger.Info($"[REC] {step.GetDisplayText()}");
    }

    private void AddStep()
    {
        if (cboStepType.SelectedItem == null) return;
        var label = cboStepType.SelectedItem.ToString()!;
        var type = Enum.GetValues<StepType>().First(type => WorkflowStep.GetTypeLabel(type) == label);
        AddStep(type);
    }

    private void AddStep(StepType type, Point? canvasLocation = null)
    {
        var step = new WorkflowStep { Type = type, DelayAfterMs = 500 };
        if (canvasLocation.HasValue)
        {
            step.CanvasX = Math.Max(20, canvasLocation.Value.X - 94);
            step.CanvasY = Math.Max(20, canvasLocation.Value.Y - 34);
        }

        _workflowSteps.Add(step);
        RefreshWorkflow();
        workflowCanvas.SelectStep(_workflowSteps.Count - 1);
        LoadStepConfig(_workflowSteps.Count - 1);
    }

    private void RemoveStep()
    {
        var index = workflowCanvas.SelectedIndex;
        RemoveStep(index);
    }

    private void RemoveStep(int index)
    {
        if (index < 0 || index >= _workflowSteps.Count) return;
        _workflowSteps.RemoveAt(index);
        RefreshWorkflow();
        var next = Math.Min(index, _workflowSteps.Count - 1);
        workflowCanvas.SelectStep(next);
        LoadStepConfig(next);
    }

    private void ShowStepContextMenu(int index)
    {
        if (index < 0 || index >= _workflowSteps.Count) return;
        var menu = new ContextMenuStrip();

        var selectedIndices = workflowCanvas.SelectedIndices;
        var hasMultiSelection = selectedIndices.Count > 1 && selectedIndices.Contains(index);

        if (hasMultiSelection)
        {
            var tapCount = selectedIndices.Count(i => i >= 0 && i < _workflowSteps.Count && 
                (_workflowSteps[i].Type == StepType.Tap || _workflowSteps[i].Type == StepType.RandomTap));

            var groupItem = menu.Items.Add($"🎲 Gom {selectedIndices.Count} bước thành Chạm ngẫu nhiên ({tapCount} tọa độ)", null, (_, _) => GroupSelectedStepsToRandomTap());
            groupItem.Font = new Font(menu.Font, FontStyle.Bold);
            menu.Items.Add(new ToolStripSeparator());
        }

        menu.Items.Add("Sửa bước", null, (_, _) => LoadStepConfig(index));
        menu.Items.Add("Nhân bản bước", null, (_, _) => DuplicateStep(index));
        menu.Items.Add("Đưa đến vị trí...", null, (_, _) => MoveStepToPosition(index));
        menu.Items.Add(new ToolStripSeparator());

        if (hasMultiSelection)
        {
            menu.Items.Add($"🗑️ Xóa {selectedIndices.Count} bước đã chọn", null, (_, _) => RemoveSelectedSteps());
        }
        else
        {
            menu.Items.Add("Xóa bước", null, (_, _) => RemoveStep(index));
        }

        menu.Show(workflowCanvas, workflowCanvas.PointToClient(Cursor.Position));
    }

    private void GroupSelectedStepsToRandomTap()
    {
        var selectedIndices = workflowCanvas.SelectedIndices
            .Where(i => i >= 0 && i < _workflowSteps.Count)
            .OrderBy(i => i)
            .ToList();

        if (selectedIndices.Count < 2)
        {
            ShowError("Vui lòng bôi chọn ít nhất 2 bước trên sơ đồ để gom nhóm.");
            return;
        }

        var points = new List<Point>();
        int lastDelay = 500;

        foreach (var idx in selectedIndices)
        {
            var s = _workflowSteps[idx];
            if (s.Type == StepType.Tap)
            {
                if (s.X != 0 || s.Y != 0)
                {
                    points.Add(new Point(s.X, s.Y));
                }
            }
            else if (s.Type == StepType.RandomTap)
            {
                if (s.RandomCoordinates.Count > 0)
                    points.AddRange(s.RandomCoordinates);
                else if (s.X != 0 || s.Y != 0)
                    points.Add(new Point(s.X, s.Y));
            }
            if (s.DelayAfterMs > 0)
                lastDelay = s.DelayAfterMs;
        }

        if (points.Count == 0)
        {
            ShowError("Các bước được bôi chọn không chứa tọa độ Chạm nào để gom nhóm.");
            return;
        }

        var firstIndex = selectedIndices[0];
        var firstStep = _workflowSteps[firstIndex];

        var newStep = new WorkflowStep
        {
            Type = StepType.RandomTap,
            RandomCoordinates = points,
            X = points[0].X,
            Y = points[0].Y,
            DelayAfterMs = lastDelay,
            Description = $"Gom ngẫu nhiên {points.Count} tọa độ",
            CanvasX = firstStep.CanvasX,
            CanvasY = firstStep.CanvasY
        };

        for (int i = selectedIndices.Count - 1; i >= 0; i--)
        {
            _workflowSteps.RemoveAt(selectedIndices[i]);
        }

        _workflowSteps.Insert(firstIndex, newStep);

        workflowCanvas.ClearMultiSelection();
        RefreshWorkflow();
        workflowCanvas.SelectStep(firstIndex);
        LoadStepConfig(firstIndex);
        SaveAutoSavedWorkflow();

        SetStatus($"Đã gom thành công {points.Count} tọa độ vào 1 bước Chạm ngẫu nhiên!");
    }

    private void RemoveSelectedSteps()
    {
        var selectedIndices = workflowCanvas.SelectedIndices
            .Where(i => i >= 0 && i < _workflowSteps.Count)
            .OrderByDescending(i => i)
            .ToList();

        if (selectedIndices.Count == 0) return;

        if (MessageBox.Show($"Bạn có chắc chắn muốn xóa {selectedIndices.Count} bước đã chọn?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        foreach (var idx in selectedIndices)
        {
            _workflowSteps.RemoveAt(idx);
        }

        workflowCanvas.ClearMultiSelection();
        RefreshWorkflow();
        var next = Math.Min(selectedIndices.Last(), _workflowSteps.Count - 1);
        if (next >= 0)
        {
            workflowCanvas.SelectStep(next);
            LoadStepConfig(next);
        }
        SaveAutoSavedWorkflow();
        SetStatus($"Đã xóa {selectedIndices.Count} bước khỏi quy trình");
    }

    private void MoveStepToPosition(int index)
    {
        if (index < 0 || index >= _workflowSteps.Count) return;

        using var dialog = new Form
        {
            Text = "Đổi vị trí bước",
            ClientSize = new Size(330, 142),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9F)
        };
        var label = new Label
        {
            Text = $"Bước {index + 1} → chuyển đến vị trí (1–{_workflowSteps.Count})",
            AutoSize = true,
            Location = new Point(18, 18),
            ForeColor = Color.FromArgb(27, 55, 82)
        };
        var position = new NumericUpDown
        {
            Minimum = 1,
            Maximum = _workflowSteps.Count,
            Value = index + 1,
            Width = 90,
            Location = new Point(18, 48),
            TextAlign = HorizontalAlignment.Center
        };
        ApplyRoundedCorners(position, 6);
        var ok = new Button
        {
            Text = "Đổi vị trí",
            DialogResult = DialogResult.OK,
            Width = 100,
            Height = 30,
            Location = new Point(208, 94),
            BackColor = Color.FromArgb(35, 149, 218),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        ApplyRoundedCorners(ok, 7);
        var cancel = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Width = 70,
            Height = 30,
            Location = new Point(128, 94),
            BackColor = Color.FromArgb(235, 242, 247),
            ForeColor = Color.FromArgb(27, 55, 82),
            FlatStyle = FlatStyle.Flat
        };
        ApplyRoundedCorners(cancel, 7);
        dialog.Controls.AddRange([label, position, cancel, ok]);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var target = (int)position.Value - 1;
        if (target == index) return;
        var step = _workflowSteps[index];
        _workflowSteps.RemoveAt(index);
        _workflowSteps.Insert(target, step);
        RefreshWorkflow();
        workflowCanvas.SelectStep(target);
        LoadStepConfig(target);
        SetStatus($"Đã chuyển bước {index + 1} thành bước {target + 1}");
    }

    private void DuplicateStep(int index)
    {
        if (index < 0 || index >= _workflowSteps.Count) return;
        var copy = JsonConvert.DeserializeObject<WorkflowStep>(JsonConvert.SerializeObject(_workflowSteps[index])) ?? new WorkflowStep();
        copy.CanvasX = -1;
        copy.CanvasY = -1;
        _workflowSteps.Insert(index + 1, copy);
        RefreshWorkflow();
        workflowCanvas.SelectStep(index + 1);
        LoadStepConfig(index + 1);
        SetStatus("Đã nhân bản bước");
    }

    private void MoveStep(int direction)
    {
        var index = workflowCanvas.SelectedIndex;
        var next = index + direction;
        if (index < 0 || next < 0 || next >= _workflowSteps.Count) return;
        (_workflowSteps[index], _workflowSteps[next]) = (_workflowSteps[next], _workflowSteps[index]);
        RefreshWorkflow();
        workflowCanvas.SelectStep(next);
        LoadStepConfig(next);
    }

    private void ClearWorkflow()
    {
        if (_workflowSteps.Count == 0) return;
        if (MessageBox.Show("Xóa toàn bộ quy trình hiện tại?", "Làm sạch quy trình", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _workflowSteps.Clear();
        RefreshWorkflow();
        LoadStepConfig(-1);
    }

    private void RefreshWorkflow()
    {
        workflowCanvas.SetSteps(_workflowSteps);
        lblStepCount.Text = $"{_workflowSteps.Count} BƯỚC";
    }

    private void LoadStepConfig(int index)
    {
        if (index < 0 || index >= _workflowSteps.Count)
        {
            lblSelectedStep.Text = "Chưa chọn bước";
            actionOptionsPanel.Visible = false;
            tapOptionsPanel.Visible = false;
            txtTextValue.Enabled = true;
            UpdateInspectorFieldLayout(null);
            return;
        }
        var step = _workflowSteps[index];
        lblSelectedStep.Text = $"BƯỚC {index + 1:00}  ·  {WorkflowStep.GetTypeLabel(step.Type)}";
        txtX.Text = step.X.ToString();
        txtY.Text = step.Y.ToString();
        txtTapXPath.Text = step.TapXPath;
        txtTapImagePath.Text = step.TapImagePath;
        cboTapMode.SelectedItem = cboTapMode.Items.OfType<TapModeChoice>()
            .FirstOrDefault(choice => choice.Mode == step.TapMode) ?? cboTapMode.Items[0];
        cboTapMultiMode.SelectedItem = cboTapMultiMode.Items.OfType<TapMultiModeChoice>()
            .FirstOrDefault(choice => choice.Mode == step.TapMultiMode) ?? cboTapMultiMode.Items[0];
        numMultiTapDelay.Value = Math.Clamp(step.MultiTapDelayMs > 0 ? step.MultiTapDelayMs : 250, numMultiTapDelay.Minimum, numMultiTapDelay.Maximum);
        txtX2.Text = step.X2.ToString();
        txtY2.Text = step.Y2.ToString();
        txtTextValue.Text = step.TextValue;
        txtBindingCol.Text = step.BindingColumn;
        txtTextValue.PlaceholderText = step.Type == StepType.InputText
            ? "Tên cột hoặc {TênCột}, ví dụ: Description"
            : string.Empty;
        txtBindingCol.PlaceholderText = step.Type == StepType.InputText
            ? "Không bắt buộc nếu đã ghi tên cột ở ô trên"
            : "Tên cột dữ liệu (nếu có)";
        txtDelayAfter.Text = step.DelayMaxMs.HasValue ? $"{step.DelayAfterMs},{step.DelayMaxMs.Value}" : step.DelayAfterMs.ToString();
        txtDescription.Text = step.Description;
        chkUseAiForText.Checked = step.UseAiForText;
        chkTakeAllLinks.Checked = step.TakeAllLinks;
        UpdateInspectorFieldLayout(step.Type);
        txtTextValue.Enabled = !(step.Type is StepType.OpenApp or StepType.PushVideo or StepType.PushImage);
        actionOptionsPanel.Visible = step.Type is StepType.OpenApp or StepType.PushVideo or StepType.PushImage or StepType.Tap or StepType.Swipe or StepType.RandomTap;
        videoOptionsPanel.Visible = step.Type is StepType.PushVideo or StepType.PushImage;
        appOptionsPanel.Visible = step.Type == StepType.OpenApp;
        tapOptionsPanel.Visible = step.Type == StepType.Tap;
        randomTapOptionsPanel.Visible = step.Type == StepType.RandomTap;
        swipeOptionsPanel.Visible = step.Type == StepType.Swipe;
        if (step.Type == StepType.RandomTap)
        {
            RefreshRandomCoordinatesList(step);
        }
        UpdateTapModeFields();

        if (step.Type is StepType.PushVideo or StepType.PushImage)
        {
            if (lblVideoSource != null)
                lblVideoSource.Text = step.Type == StepType.PushImage ? "Nguồn ảnh" : "Nguồn video";
            chkDeleteLocalVideo.Text = step.Type == StepType.PushImage ? "Xóa ảnh nguồn trên PC sau khi xong" : "Xóa video nguồn trên PC sau khi xong";
            chkClearDeviceVideos.Text = step.Type == StepType.PushImage ? "Xóa ảnh cũ trên máy trước khi đẩy" : "Xóa video cũ trên máy trước khi đẩy";

            var choice = cboVideoSource.Items.OfType<VideoSourceChoice>()
                .FirstOrDefault(item => item.Mode == step.VideoSource);
            cboVideoSource.SelectedItem = choice ?? cboVideoSource.Items[0];
            txtVideoPath.Text = step.VideoSource switch
            {
                VideoSourceMode.FolderAndExcelFileName => step.VideoFolderPath,
                VideoSourceMode.FixedFile => step.VideoFilePath,
                _ => step.Type == StepType.PushImage ? "Lấy ImagePath từ danh sách sản phẩm / Excel" : "Lấy VideoPath từ danh sách sản phẩm / Excel"
            };
            chkDeleteLocalVideo.Checked = step.DeleteLocalVideoAfterSuccess;
            chkClearDeviceVideos.Checked = step.ClearDeviceVideosBeforeUpload;
            btnBrowseVideoPath.Enabled = step.VideoSource != VideoSourceMode.ExcelPath;
        }
        else if (step.Type == StepType.OpenApp)
        {
            if (!string.IsNullOrWhiteSpace(step.TextValue) && !cboAppPackage.Items.Contains(step.TextValue))
                cboAppPackage.Items.Add(step.TextValue);
            cboAppPackage.Text = step.TextValue;
            chkSkipFromSecondJob.Checked = step.SkipFromSecondJob;
            _ = RefreshInstalledAppsAsync(step.TextValue);
        }
    }

    private void ApplyStepConfig()
    {
        var index = workflowCanvas.SelectedIndex;
        if (index < 0 || index >= _workflowSteps.Count) return;
        var step = _workflowSteps[index];
        if (int.TryParse(txtX.Text, out var x)) step.X = x;
        if (int.TryParse(txtY.Text, out var y)) step.Y = y;
        if (cboTapMode.SelectedItem is TapModeChoice tapMode)
            step.TapMode = tapMode.Mode;
        if (cboTapMultiMode.SelectedItem is TapMultiModeChoice tapMulti)
            step.TapMultiMode = tapMulti.Mode;
        step.MultiTapDelayMs = (int)numMultiTapDelay.Value;
        step.TapXPath = txtTapXPath.Text.Trim();
        step.TapImagePath = txtTapImagePath.Text.Trim();
        if (int.TryParse(txtX2.Text, out var x2)) step.X2 = x2;
        if (int.TryParse(txtY2.Text, out var y2)) step.Y2 = y2;
        var delayText = txtDelayAfter.Text.Trim();
        if (delayText.Contains(",")) {
            var parts = delayText.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max)) {
                step.DelayAfterMs = Math.Max(0, min);
                step.DelayMaxMs = Math.Max(0, max);
            }
        } else {
            if (int.TryParse(delayText, out var delay)) step.DelayAfterMs = Math.Max(0, delay);
            step.DelayMaxMs = null;
        }
        step.TextValue = txtTextValue.Text;
        step.BindingColumn = txtBindingCol.Text;
        step.Description = txtDescription.Text;
        step.UseAiForText = chkUseAiForText.Checked;
        step.TakeAllLinks = chkTakeAllLinks.Checked;
        if (step.Type == StepType.OpenApp)
        {
            var packageName = cboAppPackage.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(packageName))
                step.TextValue = packageName;
            step.SkipFromSecondJob = chkSkipFromSecondJob.Checked;
        }
        if ((step.Type is StepType.PushVideo or StepType.PushImage) && cboVideoSource.SelectedItem is VideoSourceChoice source)
        {
            step.VideoSource = source.Mode;
            if (source.Mode == VideoSourceMode.FolderAndExcelFileName)
                step.VideoFolderPath = txtVideoPath.Text;
            else if (source.Mode == VideoSourceMode.FixedFile)
                step.VideoFilePath = txtVideoPath.Text;
            step.DeleteLocalVideoAfterSuccess = chkDeleteLocalVideo.Checked;
            step.ClearDeviceVideosBeforeUpload = chkClearDeviceVideos.Checked;
            step.TextValue = source.Mode == VideoSourceMode.ExcelPath 
                ? (step.Type == StepType.PushImage ? "{ImagePath}" : "{VideoPath}") 
                : string.Empty;
        }
        if (step.Type == StepType.RandomTap)
        {
            var sel = lstRandomCoordinates.SelectedIndex;
            if (sel >= 0 && sel < step.RandomCoordinates.Count)
            {
                step.RandomCoordinates[sel] = new Point(step.X, step.Y);
                RefreshRandomCoordinatesList(step);
            }
            else if (step.RandomCoordinates.Count == 0 && (step.X != 0 || step.Y != 0))
            {
                step.RandomCoordinates.Add(new Point(step.X, step.Y));
                RefreshRandomCoordinatesList(step);
            }
        }
        RefreshWorkflow();
        workflowCanvas.SelectStep(index);
        SetStatus($"Đã cập nhật step #{index + 1}");
    }

    private void UpdateTapModeFields()
    {
        var mode = (cboTapMode?.SelectedItem as TapModeChoice)?.Mode ?? TapMode.Coordinates;
        txtTapXPath.Enabled = mode == TapMode.XPath;
        if (btnInspectUi != null) btnInspectUi.Enabled = mode == TapMode.XPath;
        if (cboTapMultiMode != null) cboTapMultiMode.Enabled = mode == TapMode.XPath;
        if (numMultiTapDelay != null) numMultiTapDelay.Enabled = mode == TapMode.XPath;
        if (lblTapMultiMode != null) lblTapMultiMode.Enabled = mode == TapMode.XPath;
        if (lblMultiTapDelay != null) lblMultiTapDelay.Enabled = mode == TapMode.XPath;
        txtTapImagePath.Enabled = mode == TapMode.Image;
        btnBrowseTapImage.Enabled = mode == TapMode.Image;
        btnCaptureTapCoordinates.Enabled = mode == TapMode.Coordinates;
    }

    private void BrowseTapImage()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Ảnh mẫu|*.png;*.jpg;*.jpeg;*.bmp|Tất cả file|*.*",
            Title = "Chọn ảnh mẫu để chạm"
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtTapImagePath.Text = dialog.FileName;
            if (workflowCanvas.SelectedIndex >= 0)
                SetStatus("Đã chọn ảnh mẫu chạm");
        }
    }

    private async Task InspectUiElementsAsync()
    {
        if (_currentDevice == null)
        {
            ShowError("Vui lòng kết nối và chọn thiết bị Android trước.");
            return;
        }

        try
        {
            SetStatus("Đang quét UI thiết bị...");
            var xml = await _adb.DumpUiHierarchyAsync(_currentDevice);
            
            var elements = new List<ShopeeVideoUploader.Models.UiElement>();
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                ParseUiElements(doc.Root, elements, 0, "");
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi đọc UI XML: {ex.Message}");
                return;
            }

            if (elements.Count == 0)
            {
                ShowError("Không tìm thấy UI element nào trên màn hình.");
                return;
            }

            using var dialog = new ShopeeVideoUploader.Controls.UiInspectorDialog(elements);
            
            dialog.OnTestClick = async (elem, xpath) =>
            {
                if (_currentDevice == null)
                    return (false, "Chưa kết nối hoặc chưa chọn thiết bị Android.");

                var center = elem.GetCenterPoint();
                if (center == null)
                    return (false, "Không xác định được tọa độ Bounds của element.");

                try
                {
                    await _adb.TapAsync(_currentDevice, center.Value.X, center.Value.Y);
                    return (true, $"✓ Đã click tại ({center.Value.X}, {center.Value.Y}) trên điện thoại thành công!");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi click: {ex.Message}");
                }
            };

            dialog.OnRescan = async () =>
            {
                if (_currentDevice == null) return null;
                var newXml = await _adb.DumpUiHierarchyAsync(_currentDevice);
                var newElements = new List<ShopeeVideoUploader.Models.UiElement>();
                var newDoc = System.Xml.Linq.XDocument.Parse(newXml);
                ParseUiElements(newDoc.Root, newElements, 0, "");
                return newElements;
            };

            if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedXPath))
            {
                txtTapXPath.Text = dialog.SelectedXPath;
                SetStatus("Đã lấy XPath từ thiết bị");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi quét UI: {ex.Message}");
        }
    }

    private void ParseUiElements(System.Xml.Linq.XElement? node, List<ShopeeVideoUploader.Models.UiElement> list, int depth, string currentPath)
    {
        if (node == null) return;
        
        var nodeClass = node.Attribute("class")?.Value ?? node.Name.LocalName;
        var nodeText = node.Attribute("text")?.Value ?? string.Empty;
        var nodeDesc = node.Attribute("content-desc")?.Value ?? string.Empty;
        var nodeId = node.Attribute("resource-id")?.Value ?? string.Empty;
        var bounds = node.Attribute("bounds")?.Value ?? string.Empty;
        var isClickable = string.Equals(node.Attribute("clickable")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        var isEnabled = string.Equals(node.Attribute("enabled")?.Value, "true", StringComparison.OrdinalIgnoreCase);

        // Build XPath heuristically
        var xpath = string.Empty;
        if (!string.IsNullOrEmpty(nodeText))
            xpath = $"//node[@text='{nodeText.Replace("'", "''")}']";
        else if (!string.IsNullOrEmpty(nodeDesc))
            xpath = $"//node[@content-desc='{nodeDesc.Replace("'", "''")}']";
        else if (!string.IsNullOrEmpty(nodeId))
            xpath = $"//node[@resource-id='{nodeId}']";
        else if (!string.IsNullOrEmpty(nodeClass))
            xpath = $"//node[@class='{nodeClass}']";

        if (node.Name.LocalName == "node")
        {
            list.Add(new ShopeeVideoUploader.Models.UiElement
            {
                ClassName = nodeClass,
                Text = nodeText,
                ContentDesc = nodeDesc,
                ResourceId = nodeId,
                Bounds = bounds,
                IsClickable = isClickable,
                IsEnabled = isEnabled,
                Depth = depth,
                XPath = string.IsNullOrEmpty(xpath) ? "//node" : xpath
            });
        }

        foreach (var child in node.Elements())
        {
            ParseUiElements(child, list, depth + 1, currentPath);
        }
    }

    private void UpdateInspectorFieldLayout(StepType? type)
    {
        var visibleRows = type switch
        {
            StepType.Tap => new[] { 0, 1, 6, 7 },
            StepType.RandomTap => new[] { 0, 1, 6, 7 },
            StepType.Swipe => new[] { 0, 1, 2, 3, 6, 7 },
            StepType.InputText => new[] { 4, 5, 6, 7, 8, 9 },
            StepType.PushVideo or StepType.PushImage => new[] { 6, 7 },
            StepType.Delay => new[] { 6, 7 },
            StepType.OpenApp => new[] { 6, 7 },
            StepType.KeyEvent => new[] { 4, 6, 7 },
            StepType.MediaScan => new[] { 4, 6, 7 },
            StepType.AdbShell => new[] { 4, 6, 7 },
            StepType.Start or StepType.End => new[] { 7 },
            _ => Enumerable.Range(0, 10).ToArray()
        };

        for (var row = 0; row < 10; row++)
        {
            var show = visibleRows.Contains(row);
            inspectorFields.RowStyles[row].SizeType = SizeType.Absolute;
            inspectorFields.RowStyles[row].Height = show ? 36F : 0F;
            var left = inspectorFields.GetControlFromPosition(0, row);
            var right = inspectorFields.GetControlFromPosition(1, row);
            if (left != null) left.Visible = show;
            if (right != null) right.Visible = show;
        }
        inspectorFields.RowStyles[10].SizeType = SizeType.Absolute;
        inspectorFields.RowStyles[10].Height = 40F;
        btnApplyConfig.Visible = true;
        inspectorFields.PerformLayout();
    }

    private void UpdateVideoSourceHint()
    {
        if (cboVideoSource.SelectedItem is not VideoSourceChoice source) return;
        btnBrowseVideoPath.Enabled = source.Mode != VideoSourceMode.ExcelPath;
        var isImage = workflowCanvas.SelectedIndex >= 0 &&
                      workflowCanvas.SelectedIndex < _workflowSteps.Count &&
                      _workflowSteps[workflowCanvas.SelectedIndex].Type == StepType.PushImage;

        if (source.Mode == VideoSourceMode.ExcelPath)
            txtVideoPath.Text = isImage ? "Lấy ImagePath từ danh sách sản phẩm / Excel" : "Lấy VideoPath từ danh sách sản phẩm / Excel";
        else if (string.IsNullOrWhiteSpace(txtVideoPath.Text) || txtVideoPath.Text.StartsWith("Lấy ", StringComparison.Ordinal))
            txtVideoPath.Text = "";
    }

    private void BrowseVideoPath()
    {
        if (cboVideoSource.SelectedItem is not VideoSourceChoice source || source.Mode == VideoSourceMode.ExcelPath)
            return;

        var isImage = workflowCanvas.SelectedIndex >= 0 &&
                      workflowCanvas.SelectedIndex < _workflowSteps.Count &&
                      _workflowSteps[workflowCanvas.SelectedIndex].Type == StepType.PushImage;

        if (source.Mode == VideoSourceMode.FolderAndExcelFileName)
        {
            using var dialog = new FolderBrowserDialog { Description = isImage ? "Chọn thư mục chứa ảnh" : "Chọn thư mục chứa video" };
            if (dialog.ShowDialog() == DialogResult.OK)
                txtVideoPath.Text = dialog.SelectedPath;
            return;
        }

        using var fileDialog = new OpenFileDialog
        {
            Filter = isImage 
                ? "Ảnh|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.heic|Tất cả tệp|*.*"
                : "Video|*.mp4;*.mov;*.mkv;*.avi|Tất cả tệp|*.*",
            Title = isImage ? "Chọn file ảnh cố định" : "Chọn file video cố định"
        };
        if (fileDialog.ShowDialog() == DialogResult.OK)
            txtVideoPath.Text = fileDialog.FileName;
    }

    private async Task CaptureCurrentAppAsync()
    {
        if (_currentDevice == null)
        {
            ShowError("Hãy kết nối điện thoại trước.");
            return;
        }

        try
        {
            btnCaptureCurrentApp.Enabled = false;
            var packageName = await _adb.GetForegroundPackageAsync(_currentDevice);
            if (string.IsNullOrWhiteSpace(packageName))
            {
                ShowError("Không xác định được ứng dụng đang mở. Hãy mở app trên scrcpy rồi thử lại.");
                return;
            }

            if (!cboAppPackage.Items.Contains(packageName))
                cboAppPackage.Items.Add(packageName);
            cboAppPackage.SelectedItem = packageName;
            ApplyStepConfig();
            SetStatus($"Đã nhận diện ứng dụng: {packageName}");
            Logger.Info($"[ADB] Ứng dụng đang mở: {packageName}");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi nhận diện ứng dụng đang mở", ex);
            ShowError($"Không xác định được ứng dụng: {ex.Message}");
        }
        finally
        {
            btnCaptureCurrentApp.Enabled = true;
        }
    }

    private async Task CaptureTapCoordinatesAsync()
    {
        if (_currentDevice == null)
        {
            ShowError("Hãy kết nối điện thoại trước.");
            return;
        }

        var index = workflowCanvas.SelectedIndex;
        if (index < 0 || index >= _workflowSteps.Count || _workflowSteps[index].Type != StepType.Tap)
        {
            ShowError("Hãy chọn một bước Chạm trước.");
            return;
        }

        if (_recordingCts != null)
        {
            ShowError("Hãy dừng ghi thao tác trước khi lấy một tọa độ riêng.");
            return;
        }

        _coordinateCaptureCts?.Cancel();
        _coordinateCaptureCts = new CancellationTokenSource();
        btnCaptureTapCoordinates.Enabled = false;
        btnCaptureTapCoordinates.Text = "Hãy click trên điện thoại...";
        SetStatus("Đang chờ một lần chạm trên scrcpy...");

        try
        {
            var width = int.TryParse(_variables.FirstOrDefault(v => v.Name == "ScreenWidth")?.Value, out var parsedWidth) ? parsedWidth : 0;
            var height = int.TryParse(_variables.FirstOrDefault(v => v.Name == "ScreenHeight")?.Value, out var parsedHeight) ? parsedHeight : 0;
            var captured = await _scrcpyMouseCapture.CaptureNextClickAsync(
                _currentDevice.Model ?? _currentDevice.Serial,
                width,
                height,
                _coordinateCaptureCts.Token);
            if (captured == null) return;

            txtX.Text = captured.X.ToString();
            txtY.Text = captured.Y.ToString();
            txtDescription.Text = $"Chạm ({captured.X}, {captured.Y}) trên {captured.WindowTitle}";
            ApplyStepConfig();
            SetStatus($"Đã lấy tọa độ ({captured.X}, {captured.Y})");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Đã hủy lấy tọa độ");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi lấy tọa độ chạm", ex);
            ShowError($"Không lấy được tọa độ: {ex.Message}");
        }
        finally
        {
            if (_coordinateCaptureCts != null)
            {
                _coordinateCaptureCts.Dispose();
                _coordinateCaptureCts = null;
            }
            btnCaptureTapCoordinates.Enabled = true;
            btnCaptureTapCoordinates.Text = "🎯 Lấy tọa độ từ điện thoại";
        }
    }

    private void RefreshRandomCoordinatesList(WorkflowStep step)
    {
        lstRandomCoordinates.BeginUpdate();
        lstRandomCoordinates.Items.Clear();
        for (int i = 0; i < step.RandomCoordinates.Count; i++)
        {
            var p = step.RandomCoordinates[i];
            lstRandomCoordinates.Items.Add($"Điểm {i + 1}: ({p.X}, {p.Y})");
        }
        lstRandomCoordinates.EndUpdate();
        if (lstRandomCoordinates.Items.Count > 0)
            lstRandomCoordinates.SelectedIndex = lstRandomCoordinates.Items.Count - 1;
    }

    private void AddManualRandomCoordinate()
    {
        var index = workflowCanvas.SelectedIndex;
        if (index < 0 || index >= _workflowSteps.Count) return;
        var step = _workflowSteps[index];
        if (step.Type != StepType.RandomTap) return;

        var inputX = txtX.Text.Trim();
        var inputY = txtY.Text.Trim();

        if (inputX.Contains(';') || inputX.Contains('|') || (inputX.Contains(',') && string.IsNullOrWhiteSpace(inputY)))
        {
            var raw = inputX.Replace("(", "").Replace(")", "");
            var parts = raw.Split([';', '|', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;
            foreach (var part in parts)
            {
                var xy = part.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
                if (xy.Length >= 2 && int.TryParse(xy[0], out var px) && int.TryParse(xy[1], out var py))
                {
                    step.RandomCoordinates.Add(new Point(px, py));
                    addedCount++;
                }
            }
            if (addedCount > 0)
            {
                RefreshRandomCoordinatesList(step);
                workflowCanvas.Invalidate();
                SaveAutoSavedWorkflow();
                SetStatus($"Đã thêm {addedCount} tọa độ vào nhóm (Tổng: {step.RandomCoordinates.Count})");
                return;
            }
        }

        if (int.TryParse(inputX, out var x) && int.TryParse(inputY, out var y))
        {
            step.RandomCoordinates.Add(new Point(x, y));
            RefreshRandomCoordinatesList(step);
            workflowCanvas.Invalidate();
            SaveAutoSavedWorkflow();
            SetStatus($"Đã thêm tọa độ ({x}, {y}) vào nhóm (Tổng: {step.RandomCoordinates.Count})");
        }
        else
        {
            ShowError("Vui lòng nhập tọa độ hợp lệ vào ô Tọa độ X và Y (hoặc dán danh sách x,y; x2,y2 vào ô X).");
        }
    }

    private void RemoveSelectedRandomCoordinate()
    {
        var index = workflowCanvas.SelectedIndex;
        if (index < 0 || index >= _workflowSteps.Count) return;
        var step = _workflowSteps[index];
        if (step.Type != StepType.RandomTap) return;

        var sel = lstRandomCoordinates.SelectedIndex;
        if (sel >= 0 && sel < step.RandomCoordinates.Count)
        {
            var removed = step.RandomCoordinates[sel];
            step.RandomCoordinates.RemoveAt(sel);
            RefreshRandomCoordinatesList(step);
            workflowCanvas.Invalidate();
            SaveAutoSavedWorkflow();
            SetStatus($"Đã xóa điểm ({removed.X}, {removed.Y}) khỏi nhóm");
        }
        else
        {
            ShowError("Vui lòng chọn một điểm trong danh sách để xóa.");
        }
    }

    private void ClearRandomCoordinates()
    {
        var index = workflowCanvas.SelectedIndex;
        if (index < 0 || index >= _workflowSteps.Count) return;
        var step = _workflowSteps[index];
        if (step.Type != StepType.RandomTap) return;

        if (step.RandomCoordinates.Count == 0) return;
        step.RandomCoordinates.Clear();
        RefreshRandomCoordinatesList(step);
        workflowCanvas.Invalidate();
        SaveAutoSavedWorkflow();
        SetStatus("Đã làm sạch toàn bộ danh sách tọa độ nhóm");
    }

    private async Task CaptureRandomTapCoordinateAsync()
    {
        if (_currentDevice == null)
        {
            ShowError("Chưa kết nối điện thoại! Hãy chọn một thiết bị ở tab Danh sách thiết bị.");
            return;
        }

        var index = workflowCanvas.SelectedIndex;
        if (index < 0 || index >= _workflowSteps.Count || _workflowSteps[index].Type != StepType.RandomTap)
        {
            ShowError("Vui lòng chọn một bước 'Chạm ngẫu nhiên' trên sơ đồ.");
            return;
        }

        if (_recordingCts != null)
        {
            ShowError("Hãy dừng ghi thao tác trước khi lấy một tọa độ riêng.");
            return;
        }

        _coordinateCaptureCts?.Cancel();
        _coordinateCaptureCts = new CancellationTokenSource();
        btnCaptureRandomTapCoordinate.Enabled = false;
        btnCaptureRandomTapCoordinate.Text = "Hãy click trên ĐT...";
        SetStatus("Đang chờ một lần chạm trên scrcpy để thêm vào nhóm...");

        try
        {
            var width = int.TryParse(_variables.FirstOrDefault(v => v.Name == "ScreenWidth")?.Value, out var parsedWidth) ? parsedWidth : 0;
            var height = int.TryParse(_variables.FirstOrDefault(v => v.Name == "ScreenHeight")?.Value, out var parsedHeight) ? parsedHeight : 0;
            var captured = await _scrcpyMouseCapture.CaptureNextClickAsync(
                _currentDevice.Model ?? _currentDevice.Serial,
                width,
                height,
                _coordinateCaptureCts.Token);
            if (captured == null) return;

            var step = _workflowSteps[index];
            step.RandomCoordinates.Add(new Point(captured.X, captured.Y));
            txtX.Text = captured.X.ToString();
            txtY.Text = captured.Y.ToString();
            RefreshRandomCoordinatesList(step);
            workflowCanvas.Invalidate();
            SaveAutoSavedWorkflow();
            SetStatus($"Đã thêm tọa độ ({captured.X}, {captured.Y}) vào nhóm [Tổng: {step.RandomCoordinates.Count} điểm]");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Đã hủy lấy tọa độ");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi lấy tọa độ chạm ngẫu nhiên", ex);
            ShowError($"Không lấy được tọa độ: {ex.Message}");
        }
        finally
        {
            if (_coordinateCaptureCts != null)
            {
                _coordinateCaptureCts.Dispose();
                _coordinateCaptureCts = null;
            }
            btnCaptureRandomTapCoordinate.Enabled = true;
            btnCaptureRandomTapCoordinate.Text = "🎯 Lấy tọa độ từ ĐT";
        }
    }

    private async Task CaptureSwipeCoordinatesAsync()
    {
        if (_currentDevice == null)
        {
            ShowError("Chưa kết nối điện thoại! Hãy chọn một thiết bị ở tab Danh sách thiết bị.");
            return;
        }

        _coordinateCaptureCts?.Cancel();
        _coordinateCaptureCts = new CancellationTokenSource();
        btnCaptureSwipeCoordinates.Enabled = false;
        btnCaptureSwipeCoordinates.Text = "Hãy vuốt trên điện thoại...";
        SetStatus("Đang chờ một lần vuốt trên scrcpy...");

        try
        {
            var width = int.TryParse(_variables.FirstOrDefault(v => v.Name == "ScreenWidth")?.Value, out var parsedWidth) ? parsedWidth : 0;
            var height = int.TryParse(_variables.FirstOrDefault(v => v.Name == "ScreenHeight")?.Value, out var parsedHeight) ? parsedHeight : 0;
            var captured = await _scrcpyMouseCapture.CaptureNextSwipeAsync(
                _currentDevice.Model ?? _currentDevice.Serial,
                width,
                height,
                _coordinateCaptureCts.Token);
            if (captured == null) return;

            txtX.Text = captured.StartX.ToString();
            txtY.Text = captured.StartY.ToString();
            txtX2.Text = captured.EndX.ToString();
            txtY2.Text = captured.EndY.ToString();
            txtDescription.Text = $"Vuốt ({captured.StartX}, {captured.StartY}) → ({captured.EndX}, {captured.EndY})";
            ApplyStepConfig();
            SetStatus($"Đã lấy tọa độ vuốt: ({captured.StartX}, {captured.StartY}) → ({captured.EndX}, {captured.EndY})");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Đã hủy lấy tọa độ");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi lấy tọa độ vuốt", ex);
            ShowError($"Không lấy được tọa độ: {ex.Message}");
        }
        finally
        {
            if (_coordinateCaptureCts != null)
            {
                _coordinateCaptureCts.Dispose();
                _coordinateCaptureCts = null;
            }
            btnCaptureSwipeCoordinates.Enabled = true;
            btnCaptureSwipeCoordinates.Text = "🎯 Lấy tọa độ vuốt từ điện thoại";
        }
    }

    private async Task RefreshInstalledAppsAsync(string preferredPackage = "")
    {
        if (_currentDevice == null)
        {
            if (string.IsNullOrWhiteSpace(preferredPackage))
                ShowError("Hãy kết nối điện thoại trước để tải danh sách ứng dụng.");
            return;
        }

        try
        {
            btnRefreshApps.Enabled = false;
            var packages = await _adb.GetInstalledPackagesAsync(_currentDevice);
            var selected = string.IsNullOrWhiteSpace(preferredPackage) ? cboAppPackage.SelectedItem?.ToString() : preferredPackage;
            cboAppPackage.Items.Clear();
            cboAppPackage.Items.AddRange(packages.ToArray());
            if (!string.IsNullOrWhiteSpace(selected) && !cboAppPackage.Items.Contains(selected))
                cboAppPackage.Items.Add(selected);
            if (!string.IsNullOrWhiteSpace(selected))
                cboAppPackage.SelectedItem = selected;
            else if (cboAppPackage.Items.Count > 0)
                cboAppPackage.SelectedIndex = 0;
            SetStatus($"Đã tải {packages.Count} ứng dụng trên điện thoại");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi tải danh sách ứng dụng", ex);
            ShowError($"Không tải được ứng dụng: {ex.Message}");
        }
        finally
        {
            btnRefreshApps.Enabled = true;
        }
    }

    private void SaveWorkflow()
    {
        using var dialog = new SaveFileDialog { Filter = "Quy trình JSON|*.json", Title = "Lưu quy trình", FileName = "quy-trinh.json" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try { WorkflowEngine.SaveWorkflow(_workflowSteps, _variables, _jobs, dialog.FileName); SetStatus("Đã lưu workflow, sản phẩm và biến"); }
        catch (Exception ex) { ShowError($"Lỗi lưu workflow: {ex.Message}"); }
    }

    private sealed record TapModeChoice(TapMode Mode)
    {
        public override string ToString() => Mode switch
        {
            TapMode.XPath => "Chạm theo XPath",
            TapMode.Image => "Chạm theo ảnh mẫu",
            _ => "Chạm theo tọa độ"
        };
    }

    private sealed record TapMultiModeChoice(TapMultiMode Mode)
    {
        public override string ToString() => Mode switch
        {
            TapMultiMode.ByImageCount => "Chạm theo số ảnh của bài (Nhiều ảnh)",
            TapMultiMode.All => "Chạm tất cả phần tử tìm thấy",
            TapMultiMode.CustomCount => "Chạm số lượng tùy chỉnh",
            _ => "Chạm 1 phần tử (mặc định)"
        };
    }

    private string CurrentWorkflowPath => Path.Combine(_workflowsDirectory, _currentWorkflowFile);
    private string CurrentIosWorkflowPath => Path.Combine(_workflowsDirectory, "_ios_" + _currentWorkflowFile);

    private void LoadAutoSavedWorkflow()
    {
        if (!Directory.Exists(_workflowsDirectory))
            Directory.CreateDirectory(_workflowsDirectory);

        var files = Directory.GetFiles(_workflowsDirectory, "*.json");
        if (files.Length == 0)
        {
            _currentWorkflowFile = "Shopee_Upload.json";
            WorkflowEngine.SaveWorkflow(_androidSteps, _variables, [], CurrentWorkflowPath);
        }

        RefreshWorkflowProfilesList();
        _currentWorkflowFile = "";
        SwitchWorkflowProfile();
        
        try 
        {
            _jobs.Clear();
            _jobs.AddRange(_dbService.GetAllJobs());
            RefreshJobGrid();
            Logger.Info($"Đã tải {_jobs.Count} sản phẩm từ CSDL.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Không thể tải dữ liệu từ CSDL: {ex.Message}", ex);
        }
    }

    private void RefreshWorkflowProfilesList(string selectFile = "")
    {
        cboWorkflowProfiles.Items.Clear();
        if (Directory.Exists(_workflowsDirectory))
        {
            var files = Directory.GetFiles(_workflowsDirectory, "*.json")
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrEmpty(f) && !f.StartsWith("_ios_"))
                .ToArray();
            cboWorkflowProfiles.Items.AddRange(files);
            
            if (!string.IsNullOrEmpty(selectFile) && cboWorkflowProfiles.Items.Contains(selectFile))
                cboWorkflowProfiles.SelectedItem = selectFile;
            else if (!string.IsNullOrEmpty(_currentWorkflowFile) && files.Contains(_currentWorkflowFile))
                cboWorkflowProfiles.SelectedItem = _currentWorkflowFile;
            else if (files.Length > 0)
                cboWorkflowProfiles.SelectedIndex = 0;
            else
            {
                _currentWorkflowFile = "Shopee_Upload.json";
                WorkflowEngine.SaveWorkflow(_androidSteps, _variables, _jobs, CurrentWorkflowPath);
                cboWorkflowProfiles.Items.Add(_currentWorkflowFile);
                cboWorkflowProfiles.SelectedIndex = 0;
            }
        }
    }

    private void SwitchWorkflowProfile(string? explicitTarget = null)
    {
        var selectedFile = explicitTarget ?? (cboWorkflowProfiles.SelectedItem as string);
        if (string.IsNullOrEmpty(selectedFile)) return;
        if (selectedFile == _currentWorkflowFile && explicitTarget == null) return;
        
        SaveAutoSavedWorkflow();
        _currentWorkflowFile = selectedFile;
        if (cboWorkflowProfiles.SelectedItem as string != selectedFile && cboWorkflowProfiles.Items.Contains(selectedFile))
        {
            cboWorkflowProfiles.SelectedItem = selectedFile;
        }
        
        if (File.Exists(CurrentWorkflowPath))
        {
            try
            {
                var document = WorkflowEngine.LoadWorkflowDocument(CurrentWorkflowPath);
                _androidSteps.Clear();
                _androidSteps.AddRange(document.Steps);
                _variables.RemoveAll(variable => !variable.IsBuiltIn);
                _variables.AddRange(document.Variables.Where(variable => !variable.IsBuiltIn));
                RefreshVariableList();
                RefreshWorkflow();

                var selectedIndex = _androidSteps.Count > 0 ? 0 : -1;
                workflowCanvas.SelectStep(selectedIndex);
                LoadStepConfig(selectedIndex);
                Logger.Info($"Đã load quy trình: {_currentWorkflowFile}");
                SetStatus($"Đang dùng quy trình: {_currentWorkflowFile}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Không thể load quy trình: {ex.Message}");
            }
        }

        // Load iOS workflow từ file companion
        _iosSteps.Clear();
        if (File.Exists(CurrentIosWorkflowPath))
        {
            try
            {
                var iosDoc = WorkflowEngine.LoadWorkflowDocument(CurrentIosWorkflowPath);
                _iosSteps.AddRange(iosDoc.Steps);
                Logger.Info($"Đã load quy trình iOS: _ios_{_currentWorkflowFile} ({_iosSteps.Count} bước)");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Không thể load quy trình iOS: {ex.Message}");
            }
        }
    }

    private void CreateNewWorkflowProfile()
    {
        var newName = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên quy trình mới:", "Tạo Quy Trình Mới", "Facebook_Upload");
        if (string.IsNullOrWhiteSpace(newName)) return;
        if (!newName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) newName += ".json";
        
        var path = Path.Combine(_workflowsDirectory, newName);
        if (File.Exists(path))
        {
            ShowError("Tên quy trình này đã tồn tại!");
            return;
        }

        _androidSteps.Clear();
        _iosSteps.Clear();
        WorkflowEngine.SaveWorkflow(_androidSteps, _variables, _jobs, path);
        RefreshWorkflowProfilesList(newName);
        SetStatus($"Đã tạo quy trình mới: {newName}");
    }

    private void DeleteWorkflowProfile()
    {
        if (string.IsNullOrWhiteSpace(_currentWorkflowFile))
        {
            ShowError("Vui lòng chọn một quy trình để xóa!");
            return;
        }

        var availableFiles = Directory.Exists(_workflowsDirectory)
            ? Directory.GetFiles(_workflowsDirectory, "*.json")
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrEmpty(f) && !f.StartsWith("_ios_"))
                .ToList()
            : [];

        if (availableFiles.Count <= 1)
        {
            ShowError("Không thể xóa quy trình cuối cùng! Hệ thống cần ít nhất 1 quy trình.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn XÓA VĨNH VIỄN quy trình:\n\n👉 {_currentWorkflowFile}\n\n(Tệp quy trình và bản iOS đi kèm sẽ bị xóa hoàn toàn khỏi máy tính)?",
            "Xác nhận xóa quy trình",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes) return;

        try
        {
            var fileToDelete = _currentWorkflowFile;
            var path = CurrentWorkflowPath;
            var iosPath = CurrentIosWorkflowPath;

            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(iosPath)) File.Delete(iosPath);

            Logger.Info($"Đã xóa quy trình: {fileToDelete}");

            _currentWorkflowFile = "";
            RefreshWorkflowProfilesList();

            SetStatus($"Đã xóa quy trình '{fileToDelete}' thành công.");
            MessageBox.Show($"Đã xóa thành công quy trình '{fileToDelete}'!", "Đã xóa quy trình", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"Lỗi khi xóa workflow: {ex.Message}", ex);
            ShowError($"Không thể xóa quy trình: {ex.Message}");
        }
    }

    private void RenameWorkflowProfile()
    {
        if (string.IsNullOrWhiteSpace(_currentWorkflowFile)) return;

        var baseName = _currentWorkflowFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? _currentWorkflowFile[..^5]
            : _currentWorkflowFile;

        var newName = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên mới cho quy trình:", "Đổi Tên Quy Trình", baseName);
        if (string.IsNullOrWhiteSpace(newName)) return;
        if (!newName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) newName += ".json";
        if (string.Equals(_currentWorkflowFile, newName, StringComparison.OrdinalIgnoreCase)) return;
        
        var oldPath = CurrentWorkflowPath;
        var newPath = Path.Combine(_workflowsDirectory, newName);
        
        if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase)) return;
        
        if (File.Exists(newPath))
        {
            ShowError("Tên quy trình này đã tồn tại!");
            return;
        }

        File.Move(oldPath, newPath);
        var oldIosPath = Path.Combine(_workflowsDirectory, "_ios_" + Path.GetFileName(oldPath));
        var newIosPath = Path.Combine(_workflowsDirectory, "_ios_" + newName);
        if (File.Exists(oldIosPath)) File.Move(oldIosPath, newIosPath);
        _currentWorkflowFile = newName;
        RefreshWorkflowProfilesList(newName);
        SetStatus($"Đã đổi tên quy trình thành: {newName}");
    }

    private void CloneCurrentWorkflowProfile()
    {
        if (string.IsNullOrWhiteSpace(_currentWorkflowFile)) return;

        var baseName = _currentWorkflowFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? _currentWorkflowFile[..^5]
            : _currentWorkflowFile;

        var cloneName = $"{baseName}_ban_sao.json";
        int counter = 2;
        while (File.Exists(Path.Combine(_workflowsDirectory, cloneName)))
        {
            cloneName = $"{baseName}_ban_sao_{counter++}.json";
        }

        try
        {
            var oldPath = CurrentWorkflowPath;
            var newPath = Path.Combine(_workflowsDirectory, cloneName);
            if (File.Exists(oldPath)) File.Copy(oldPath, newPath, true);

            var oldIos = CurrentIosWorkflowPath;
            var newIos = Path.Combine(_workflowsDirectory, "_ios_" + cloneName);
            if (File.Exists(oldIos)) File.Copy(oldIos, newIos, true);

            Logger.Info($"Đã nhân bản quy trình '{_currentWorkflowFile}' thành '{cloneName}'");
            RefreshWorkflowProfilesList(cloneName);
            SetStatus($"Đã nhân bản quy trình thành {cloneName}");
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi nhân bản quy trình: {ex.Message}");
        }
    }

    private void ShowWorkflowManager()
    {
        using var dialog = new Controls.WorkflowManagerDialog(_workflowsDirectory, _currentWorkflowFile, (selected) =>
        {
            SwitchWorkflowProfile(selected);
        });
        dialog.ShowDialog(this);
        if (dialog.HasChanges)
        {
            RefreshWorkflowProfilesList(dialog.SelectedWorkflowFile);
        }
    }

    private void BackupDatabase()
    {
        using var dialog = new SaveFileDialog 
        { 
            Filter = "SQLite Database|*.db", 
            Title = "Sao lưu CSDL", 
            FileName = $"app_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
            RestoreDirectory = true
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            _dbService.BackupDatabase(dialog.FileName);
            SetStatus("Đã sao lưu CSDL an toàn thành công");
            Logger.Info($"Đã sao lưu CSDL ra {dialog.FileName}");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi sao lưu CSDL", ex);
            ShowError($"Không thể sao lưu: {ex.Message}");
        }
    }

    private void RestoreDatabase()
    {
        using var dialog = new OpenFileDialog 
        { 
            Filter = "SQLite Database|*.db", 
            Title = "Phục hồi CSDL",
            RestoreDirectory = true
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            _dbService.RestoreDatabase(dialog.FileName);
            _jobs.Clear();
            _jobs.AddRange(_dbService.GetAllJobs());
            RefreshJobGrid();
            SetStatus("Đã phục hồi CSDL thành công");
            Logger.Info($"Đã phục hồi CSDL từ {dialog.FileName}");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi phục hồi CSDL", ex);
            ShowError($"Không thể phục hồi: {ex.Message}");
        }
    }

    private void SaveAutoSavedWorkflow()
    {
        if (string.IsNullOrWhiteSpace(_workflowsDirectory) || string.IsNullOrWhiteSpace(_currentWorkflowFile)) return;

        try
        {
            Directory.CreateDirectory(_workflowsDirectory);
            var temp = CurrentWorkflowPath + ".tmp";
            WorkflowEngine.SaveWorkflow(_androidSteps, _variables, [], temp);
            File.Move(temp, CurrentWorkflowPath, true);
            
            Logger.Info($"Đã tự lưu workflow {_currentWorkflowFile} và {_jobs.Count} sản phẩm.");
        }
        catch (Exception ex)
        {
            Logger.Error("Không thể tự lưu workflow khi đóng ứng dụng", ex);
        }

        // Lưu iOS workflow vào file companion
        try
        {
            if (_iosSteps.Count > 0)
            {
                var iosTemp = CurrentIosWorkflowPath + ".tmp";
                WorkflowEngine.SaveWorkflow(_iosSteps, iosTemp);
                File.Move(iosTemp, CurrentIosWorkflowPath, true);
                Logger.Info($"Đã tự lưu quy trình iOS: _ios_{_currentWorkflowFile} ({_iosSteps.Count} bước)");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Không thể tự lưu quy trình iOS", ex);
        }
    }

    private void LoadWorkflow()
    {
        using var dialog = new OpenFileDialog { Filter = "Quy trình JSON|*.json", Title = "Mở quy trình" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            var document = WorkflowEngine.LoadWorkflowDocument(dialog.FileName);
            _workflowSteps.Clear();
            _workflowSteps.AddRange(document.Steps);
            _variables.RemoveAll(v => !v.IsBuiltIn);
            _variables.AddRange(document.Variables.Where(v => !v.IsBuiltIn));
            RefreshVariableList();
            RefreshWorkflow();
            workflowCanvas.SelectStep(_workflowSteps.Count > 0 ? 0 : -1);
            LoadStepConfig(_workflowSteps.Count > 0 ? 0 : -1);
            SetStatus("Đã load workflow");
            SaveAutoSavedWorkflow();
        }
        catch (Exception ex) { ShowError($"Lỗi load workflow: {ex.Message}"); }
    }

    private void ImportExcel()
    {
        using var dialog = new OpenFileDialog { Filter = "Excel|*.xlsx;*.xls", Title = "Import jobs" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            var previousStatuses = _jobs
                .Where(job => !string.IsNullOrWhiteSpace(job.VideoPath))
                .GroupBy(job => job.VideoPath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var importedJobs = ExcelService.ImportExcel(dialog.FileName);
            foreach (var job in importedJobs)
            {
                if (!previousStatuses.TryGetValue(job.VideoPath, out var previous)) continue;
                job.FolderId = previous.FolderId;
                job.ShopeeStatus = previous.ShopeeStatus;
                job.FbStatus = previous.FbStatus;
                job.Status = previous.Status;
                job.Log = previous.Log;
            }
            _jobs = importedJobs;
            RefreshJobGrid();
            _dbService.ReplaceAllJobs(_jobs);
            SaveAutoSavedWorkflow();
            SetStatus($"Đã import {_jobs.Count} sản phẩm");
        }
        catch (Exception ex) { ShowError($"Lỗi import Excel: {ex.Message}"); }
    }

    private void ExportTemplate()
    {
        using var dialog = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "ShopeeTemplate.xlsx" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try { ExcelService.ExportTemplate(dialog.FileName); SetStatus("Đã xuất template"); }
        catch (Exception ex) { ShowError($"Lỗi xuất template: {ex.Message}"); }
    }

    private void ExportResult()
    {
        if (_jobs.Count == 0) { ShowError("Chưa có job để xuất."); return; }
        using var dialog = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "ShopeeResult.xlsx" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try { ExcelService.ExportResult(dialog.FileName, _jobs); SetStatus("Đã xuất kết quả"); }
        catch (Exception ex) { ShowError($"Lỗi xuất kết quả: {ex.Message}"); }
    }


    private void FilterJobsGrid()
    {
        var selectedFolderId = -1;
        if (cboMainFolderSelect != null && cboMainFolderSelect.SelectedItem is ShopeeVideoUploader.Models.FolderItem fi)
        {
            selectedFolderId = fi.Id;
        }

        dgvJobs.Rows.Clear();
        foreach (var job in _jobs)
        {
            if (selectedFolderId != -1 && selectedFolderId != -2 && (job.FolderId ?? 0) != selectedFolderId) continue;
            if (chkOnlyWithLink != null && chkOnlyWithLink.Checked && string.IsNullOrWhiteSpace(job.ShopeeAffLink)) continue;
            var row = dgvJobs.Rows.Add(job.Id, job.VideoPath, job.Title, job.ShopeeAffLink, job.Status, job.ShopeeStatus, job.FbStatus, job.Log);
            dgvJobs.Rows[row].Tag = job.Id;
            StyleStatusCell(dgvJobs.Rows[row].Cells["colStatus"], job.Status);
            StyleShopeeStatusCell(dgvJobs.Rows[row].Cells["colShopeeStatus"], job.ShopeeStatus);
            StyleFbStatusCell(dgvJobs.Rows[row].Cells["colFbStatus"], job.FbStatus);
        }
        UpdateLinkFilterCount();
    }

    private void UpdateLinkFilterCount()
    {
        if (chkOnlyWithLink == null) return;
        var selectedFolderId = -1;
        if (cboMainFolderSelect != null && cboMainFolderSelect.SelectedItem is ShopeeVideoUploader.Models.FolderItem fi)
        {
            selectedFolderId = fi.Id;
        }

        var candidateJobs = _jobs;
        if (selectedFolderId != -1 && selectedFolderId != -2)
        {
            candidateJobs = _jobs.Where(j => (j.FolderId ?? 0) == selectedFolderId).ToList();
        }

        var total = candidateJobs.Count;
        var withLink = candidateJobs.Count(j => !string.IsNullOrWhiteSpace(j.ShopeeAffLink));
        chkOnlyWithLink.Text = total > 0 ? $"Chỉ up video có link ({withLink}/{total})" : "Chỉ up video có link";
    }

    private void RefreshJobGrid()
    {
        var folders = _dbService.GetAllFolders();
        productListControl.SetData(_jobs, folders);
        
        if (cboMainFolderSelect != null)
        {
            _isUpdatingFolders = true;
            var currentId = cboMainFolderSelect.SelectedItem is ShopeeVideoUploader.Models.FolderItem fi ? fi.Id : -1;
            cboMainFolderSelect.Items.Clear();
            cboMainFolderSelect.Items.Add(new ShopeeVideoUploader.Models.FolderItem { Id = -1, Name = "Tất cả chiến dịch" });
            cboMainFolderSelect.Items.Add(new ShopeeVideoUploader.Models.FolderItem { Id = 0, Name = "[Chưa phân loại]" });
            foreach (var f in folders) cboMainFolderSelect.Items.Add(f);
            
            var found = false;
            foreach (var item in cboMainFolderSelect.Items)
            {
                if (item is ShopeeVideoUploader.Models.FolderItem folder && folder.Id == currentId)
                {
                    cboMainFolderSelect.SelectedItem = item;
                    found = true;
                    break;
                }
            }
            if (!found && cboMainFolderSelect.Items.Count > 0) cboMainFolderSelect.SelectedIndex = 0;
            _isUpdatingFolders = false;
            
            // Re-sync ProductListControl filter with the combo box selection
            if (cboMainFolderSelect.SelectedItem is ShopeeVideoUploader.Models.FolderItem selectedFolder)
            {
                productListControl.FilterByFolder(selectedFolder.Id);
            }
        }
        
        FilterJobsGrid();
        RefreshOverviewDashboard();
    }

    private void UpdateJobRow(JobItem job, string status, string log, string targetPlatform = "Shopee")
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateJobRow(job, status, log, targetPlatform));
            return;
        }

        var index = _jobs.FindIndex(item => item.Id == job.Id);
        if (index < 0) return;

        var current = _jobs[index];
        // Progress<T> callbacks can arrive after the engine has already
        // published the terminal result. Do not let an old "Đang chạy"
        // message roll a completed/failed job back to a transient state.
        if (JobStatus.IsTerminal(current.Status) && status.StartsWith("Đang", StringComparison.Ordinal))
            return;
        current.Status = status;
        current.Log = log;
        if (status == JobStatus.Succeeded)
            JobStatus.MarkCompleted(current, targetPlatform);

        productListControl.UpdateProduct(index, current);
        UpdateLegacyJobGrid(current);
        _dbService.UpdateJobStatus(current.Id, current.Status, current.Log, current.ShopeeStatus, current.FbStatus);
    }

    private void UpdateLegacyJobGrid(JobItem job)
    {
        foreach (DataGridViewRow row in dgvJobs.Rows)
        {
            if (!Equals(row.Tag, job.Id)) continue;
            row.Cells["colId"].Value = job.Id;
            row.Cells["colVideo"].Value = job.VideoPath;
            row.Cells["colTitle"].Value = job.Title;
            row.Cells["colLink"].Value = job.ShopeeAffLink;
            row.Cells["colStatus"].Value = job.Status;
            row.Cells["colShopeeStatus"].Value = job.ShopeeStatus;
            row.Cells["colFbStatus"].Value = job.FbStatus;
            row.Cells["colLog"].Value = job.Log;
            StyleStatusCell(row.Cells["colStatus"], job.Status);
            StyleShopeeStatusCell(row.Cells["colShopeeStatus"], job.ShopeeStatus);
            StyleFbStatusCell(row.Cells["colFbStatus"], job.FbStatus);
            break;
        }
    }

    private static void StyleFbStatusCell(DataGridViewCell cell, string status)
    {
        cell.Style.ForeColor = status == "Đã up Facebook"
            ? Color.FromArgb(16, 185, 129)
            : Color.FromArgb(156, 163, 175);
        cell.Style.Font = new Font("Segoe UI Semibold", 8.5F);
    }

    private static void StyleShopeeStatusCell(DataGridViewCell cell, string status)
    {
        cell.Style.ForeColor = status == "Đã up Shopee"
            ? Color.FromArgb(16, 185, 129)
            : Color.FromArgb(156, 163, 175);
        cell.Style.Font = new Font("Segoe UI Semibold", 8.5F);
    }

    private void ResetProductStatuses()
    {
        foreach (var job in _jobs)
            JobStatus.Reset(job);
        RefreshJobGrid();
        _dbService.ResetAllJobStatuses();
        SaveAutoSavedWorkflow();
        SetStatus("Đã đặt lại trạng thái sản phẩm");
    }

    private void ResetSelectedProductStatuses()
    {
        var selectedIndices = productListControl.SelectedIndices;
        if (selectedIndices.Count == 0) return;

        var selectedIds = new List<int>();
        foreach (var index in selectedIndices)
        {
            if (index >= 0 && index < _jobs.Count)
            {
                var job = _jobs[index];
                JobStatus.Reset(job);
                productListControl.UpdateProduct(index, job);
                UpdateLegacyJobGrid(job);
                if (job.Id > 0) selectedIds.Add(job.Id);
            }
        }
        if (selectedIds.Count > 0)
        {
            _dbService.ResetJobStatuses(selectedIds);
        }
        SaveAutoSavedWorkflow();
        SetStatus($"Đã đặt lại trạng thái {selectedIndices.Count} sản phẩm");
    }

    private void AddProduct()
    {
        var checkedFolders = productListControl.CheckedFolderIds;
        int defaultFolderId = checkedFolders.Count == 1 ? checkedFolders[0] : 0;
        using var dialog = new ProductEditorDialog(_jobs, _dbService.GetAllFolders(), defaultFolderId);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var newItems = dialog.IsBulkAdd ? dialog.BulkResults : new List<JobItem> { dialog.Result };
        _dbService.SaveJobs(newItems);
        _jobs.AddRange(newItems);
        RefreshJobGrid();
        SaveAutoSavedWorkflow();
        SetStatus($"Đã thêm {newItems.Count} sản phẩm");
    }

    private void ApplyProductChange(ProductChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyProductChange(e));
            return;
        }
        if (e.Index < 0 || e.Index >= _jobs.Count) return;

        var original = _jobs[e.Index];
        var updated = e.Product;
        updated.Id = original.Id;
        updated.FolderId = original.FolderId;
        updated.Status = original.Status;
        updated.Log = original.Log;
        updated.Data = original.Data;

        if (!string.Equals(original.VideoPath, updated.VideoPath, StringComparison.OrdinalIgnoreCase))
        {
            JobStatus.Reset(updated);
        }

        _jobs[e.Index] = updated;
        productListControl.UpdateProduct(e.Index, updated);
        UpdateLegacyJobGrid(updated);
        _dbService.UpdateJob(updated);
        SaveAutoSavedWorkflow();
        SetStatus($"Đã cập nhật sản phẩm dòng {e.Index + 1}");
    }

    private void ShowFolderManager()
    {
        using var dialog = new Controls.FolderManagerDialog(_dbService);
        dialog.ShowDialog(this);
        RefreshJobGrid();
    }

    private void ShowTelegramConfig()
    {
        using var dialog = new Controls.TelegramConfigDialog(_telegramConfig);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _telegramConfig = dialog.Config;
            _telegramConfigService.Save(_telegramConfig);
            _telegramBotService?.UpdateConfig(_telegramConfig);
            SetStatus("Đã lưu cấu hình Telegram Bot");
        }
    }

    private void InitializeTelegramBot()
    {
        try
        {
            _telegramConfig = _telegramConfigService.Load();
            if (chkOnlyWithLink != null)
            {
                chkOnlyWithLink.Checked = _telegramConfig.OnlyRunWithLink;
            }
            _telegramBotService = new TelegramBotService(_telegramConfig);
            SetupTelegramBotCallbacks();
            _telegramBotService.Start();
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Telegram] Không thể khởi tạo bot: {ex.Message}");
        }
    }

    private void SetupTelegramBotCallbacks()
    {
        if (_telegramBotService == null) return;

        _telegramBotService.OnGetWorkflowFiles = () =>
        {
            if (!Directory.Exists(_workflowsDirectory)) return [];
            return Directory.GetFiles(_workflowsDirectory, "*.json")
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrEmpty(f) && !f.StartsWith("_ios_"))
                .ToList()!;
        };

        _telegramBotService.OnGetCurrentWorkflowFile = () => _currentWorkflowFile;

        _telegramBotService.OnSelectWorkflowFile = (fileName) => SelectWorkflowProfile(fileName);

        _telegramBotService.OnSelectFolder = (folderId) => SelectFolderFilter(folderId);

        _telegramBotService.OnGetSelectedFolderInfo = () => GetCurrentSelectedFolderInfo();

        _telegramBotService.OnConfigChanged = (cfg) =>
        {
            _telegramConfig = cfg;
            _telegramConfigService.Save(cfg);
            if (InvokeRequired)
            {
                BeginInvoke(() =>
                {
                    if (chkOnlyWithLink != null && chkOnlyWithLink.Checked != cfg.OnlyRunWithLink)
                        chkOnlyWithLink.Checked = cfg.OnlyRunWithLink;
                });
            }
            else
            {
                if (chkOnlyWithLink != null && chkOnlyWithLink.Checked != cfg.OnlyRunWithLink)
                    chkOnlyWithLink.Checked = cfg.OnlyRunWithLink;
            }
        };

        _telegramBotService.OnGetFolders = () =>
        {
            try { return _dbService.GetAllFolders(); }
            catch { return []; }
        };

        _telegramBotService.OnCheckDeviceStatus = () =>
        {
            if (_isIosMode)
            {
                var isConn = !string.IsNullOrEmpty(_currentIosDeviceId);
                var name = isConn ? $"iPhone (WDA Port {_currentIosDeviceId})" : "Chưa kết nối iOS";
                return Task.FromResult((isConn, name, isConn ? "Online" : "Offline"));
            }
            else
            {
                var isConn = _currentDevice != null;
                var name = isConn ? $"{_currentDevice!.Model ?? _currentDevice.Serial} ({_currentDevice.Serial})" : "Chưa kết nối Android";
                return Task.FromResult((isConn, name, isConn ? _currentDevice!.State.ToString() : "Offline"));
            }
        };

        _telegramBotService.OnGetDeviceListDetailed = async () =>
        {
            var list = new List<(string id, string name, string state)>();
            if (_isIosMode)
            {
                var ports = await _iosManager.GetConnectedWdaPortsAsync();
                foreach (var p in ports) list.Add((p, $"iOS Device (Port {p})", "Online"));
            }
            else
            {
                var devs = await _adb.GetConnectedDevicesAsync();
                foreach (var d in devs) list.Add((d.Serial, $"{d.Model} ({d.Serial})", d.State));
            }
            return list;
        };

        _telegramBotService.OnConnectDevice = (deviceId) =>
        {
            var tcs = new TaskCompletionSource<(bool success, string message)>();
            if (InvokeRequired)
            {
                BeginInvoke(async () =>
                {
                    var res = await ConnectDeviceBySerialOrAutoAsync(deviceId);
                    tcs.SetResult(res);
                });
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    var res = await ConnectDeviceBySerialOrAutoAsync(deviceId);
                    tcs.SetResult(res);
                });
            }
            return tcs.Task;
        };

        _telegramBotService.OnGetDevicesInfo = async () =>
        {
            if (_isIosMode)
            {
                return string.IsNullOrEmpty(_currentIosDeviceId)
                    ? "❌ Chưa kết nối thiết bị iOS"
                    : $"🍏 iOS Device (WDA Port: {_currentIosDeviceId}) - Online";
            }

            if (_currentDevice != null)
            {
                return $"🤖 Android: {_currentDevice.Model ?? _currentDevice.Serial} (Serial: {_currentDevice.Serial}, State: {_currentDevice.State})";
            }

            var devices = await _adb.GetConnectedDevicesAsync();
            if (devices.Count == 0) return "❌ Không tìm thấy thiết bị Android nào.";
            return string.Join("\n", devices.Select(d => $"• {d} [{d.State}]"));
        };

        _telegramBotService.OnGetStatsInfo = () =>
        {
            int total = 0, waiting = 0, running = 0, succeeded = 0, failed = 0, shopeeDone = 0, fbDone = 0;
            void GetStats()
            {
                total = _jobs.Count;
                waiting = _jobs.Count(j => j.Status == JobStatus.Waiting);
                running = _jobs.Count(j => j.Status == JobStatus.Running);
                succeeded = _jobs.Count(j => j.Status == JobStatus.Succeeded);
                failed = _jobs.Count(j => j.Status == JobStatus.Failed);
                shopeeDone = _jobs.Count(j => j.ShopeeStatus == JobStatus.ShopeeDone);
                fbDone = _jobs.Count(j => j.FbStatus == JobStatus.FacebookDone);
            }
            if (InvokeRequired) Invoke(GetStats);
            else GetStats();

            var info = $"📁 Tổng số video: <b>{total}</b>\n" +
                       $"⏳ Chờ xử lý: <b>{waiting}</b>\n" +
                       $"🏃 Đang chạy: <b>{running}</b>\n" +
                       $"✅ Thành công: <b>{succeeded}</b>\n" +
                       $"❌ Lỗi: <b>{failed}</b>\n" +
                       $"🛒 Đã up Shopee: <b>{shopeeDone}</b>\n" +
                       $"📘 Đã up Facebook: <b>{fbDone}</b>";
            return Task.FromResult(info);
        };

        _telegramBotService.OnGetStatusInfo = () =>
        {
            if (_cts != null)
            {
                int val = 0;
                int max = 0;
                if (InvokeRequired)
                {
                    Invoke(() => { val = progressBar.Value; max = progressBar.Maximum; });
                }
                else
                {
                    val = progressBar.Value;
                    max = progressBar.Maximum;
                }
                var progressText = $"Đang chạy ({val}/{max} video)\nThiết bị: {(_isIosMode ? _currentIosDeviceId : _currentDevice?.Serial ?? "N/A")}";
                return Task.FromResult(progressText);
            }
            return Task.FromResult("🟢 Tool đang rảnh rỗi (Idle). Sẵn sàng nhận lệnh!");
        };

        _telegramBotService.OnRequestScreenshot = async () =>
        {
            try
            {
                if (_isIosMode && !string.IsNullOrEmpty(_currentIosDeviceId))
                {
                    return await _iosManager.TakeScreenshotBytesAsync(_currentIosDeviceId);
                }
                else if (!_isIosMode && _currentDevice != null)
                {
                    return await _adb.TakeScreenshotBytesAsync(_currentDevice);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Telegram] Lỗi chụp màn hình: {ex.Message}");
            }
            return null;
        };

        _telegramBotService.OnRequestStop = () =>
        {
            if (InvokeRequired) BeginInvoke(StopRun);
            else StopRun();
        };

        _telegramBotService.OnRequestRun = (folderId, platform, useAi, delayMin, delayMax, onlyFailed) =>
        {
            var tcs = new TaskCompletionSource<bool>();
            if (InvokeRequired)
            {
                BeginInvoke(async () =>
                {
                    var res = await StartRunFromTelegramAsync(folderId, platform, useAi, delayMin, delayMax, onlyFailed);
                    tcs.SetResult(res);
                });
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    var res = await StartRunFromTelegramAsync(folderId, platform, useAi, delayMin, delayMax, onlyFailed);
                    tcs.SetResult(res);
                });
            }
            return tcs.Task;
        };
    }

    private (bool success, string message) SelectWorkflowProfile(string fileName)
    {
        var filePath = Path.Combine(_workflowsDirectory, fileName);
        if (!File.Exists(filePath)) return (false, $"File quy trình '{fileName}' không tồn tại.");

        void ApplyOnUi()
        {
            RefreshWorkflowProfilesList(fileName);
            cboWorkflowProfiles.SelectedItem = fileName;
            SwitchWorkflowProfile();
        }

        if (InvokeRequired) Invoke(ApplyOnUi);
        else ApplyOnUi();

        var stepsCount = _isIosMode ? _iosSteps.Count : _workflowSteps.Count;
        return (true, $"Đang kích hoạt quy trình gồm {stepsCount} bước.");
    }

    private (bool success, string folderName, int videoCount) SelectFolderFilter(int folderId)
    {
        string folderName = "Tất cả chiến dịch";
        int videoCount = _jobs.Count;

        void ApplyOnUi()
        {
            if (cboMainFolderSelect != null)
            {
                foreach (var item in cboMainFolderSelect.Items)
                {
                    if (item is FolderItem fi && fi.Id == folderId)
                    {
                        cboMainFolderSelect.SelectedItem = item;
                        folderName = fi.Name;
                        break;
                    }
                }
            }
            if (folderId == -1)
            {
                productListControl.FilterByFolder(-1);
                FilterJobsGrid();
                videoCount = _jobs.Count;
                folderName = "Tất cả chiến dịch";
            }
            else if (folderId == 0)
            {
                productListControl.FilterByFolder(0);
                FilterJobsGrid();
                videoCount = _jobs.Count(j => (j.FolderId ?? 0) == 0);
                folderName = "[Chưa phân loại]";
            }
            else
            {
                productListControl.FilterByFolder(folderId);
                FilterJobsGrid();
                videoCount = _jobs.Count(j => (j.FolderId ?? 0) == folderId);
            }
        }

        if (InvokeRequired) Invoke(ApplyOnUi);
        else ApplyOnUi();

        return (true, folderName, videoCount);
    }

    private (int folderId, string folderName, int videoCount) GetCurrentSelectedFolderInfo()
    {
        if (InvokeRequired)
        {
            return (((int, string, int))Invoke(GetCurrentSelectedFolderInfo));
        }

        int folderId = -1;
        string folderName = "Tất cả chiến dịch";
        int videoCount = _jobs.Count;

        if (cboMainFolderSelect?.SelectedItem is FolderItem fi)
        {
            folderId = fi.Id;
            folderName = fi.Name;
            if (folderId == -1) videoCount = _jobs.Count;
            else if (folderId == 0) videoCount = _jobs.Count(j => (j.FolderId ?? 0) == 0);
            else videoCount = _jobs.Count(j => (j.FolderId ?? 0) == folderId);
        }

        return (folderId, folderName, videoCount);
    }

    private async Task<(bool success, string message)> ConnectDeviceBySerialOrAutoAsync(string? targetId = null)
    {
        try
        {
            if (_isIosMode)
            {
                var ports = await _iosManager.GetConnectedWdaPortsAsync();
                if (ports.Count == 0) return (false, "Không tìm thấy thiết bị iOS (WDA port 8100/8200) nào.");
                var port = !string.IsNullOrEmpty(targetId) && ports.Contains(targetId) ? targetId : ports[0];
                _currentIosDeviceId = port;
                
                void UpdateIosUi()
                {
                    cboDevices.Items.Clear();
                    foreach (var p in ports) cboDevices.Items.Add($"iOS (Port {p})");
                    cboDevices.SelectedItem = $"iOS (Port {port})";
                    lblDeviceBadge.Text = $"iOS WDA";
                    lblDeviceBadge.BackColor = Color.FromArgb(76, 175, 80);
                    SetStatus($"Đã chọn thiết bị iOS tại cổng {port}");
                }

                if (InvokeRequired) BeginInvoke(UpdateIosUi);
                else UpdateIosUi();

                return (true, $"Thiết bị iOS (WDA Port: {port})");
            }
            else
            {
                var devices = await _adb.GetConnectedDevicesAsync();
                if (devices.Count == 0) return (false, "Không tìm thấy thiết bị Android nào qua ADB.");

                var targetInfo = !string.IsNullOrEmpty(targetId)
                    ? devices.FirstOrDefault(d => d.Serial.Equals(targetId, StringComparison.OrdinalIgnoreCase))
                    : devices[0];

                if (targetInfo == null) targetInfo = devices[0];

                var dev = await _adb.GetDeviceBySerialAsync(targetInfo.Serial);
                if (dev == null) return (false, $"Không thể kết nối thiết bị {targetInfo.Serial}");

                _currentDevice = dev;
                _currentDeviceInfo = targetInfo;

                void UpdateAndroidUi()
                {
                    cboDevices.Items.Clear();
                    foreach (var d in devices) cboDevices.Items.Add(d);
                    cboDevices.SelectedItem = targetInfo;
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    lblDeviceBadge.Text = $"●  {targetInfo.Model} · {targetInfo.Serial}";
                    lblDeviceBadge.ForeColor = Color.FromArgb(0, 161, 112);
                    SetStatus($"Đã kết nối: {targetInfo.Model}");
                }

                if (InvokeRequired) BeginInvoke(UpdateAndroidUi);
                else UpdateAndroidUi();

                await UpdateDeviceVariablesAsync(targetInfo);
                return (true, $"{targetInfo.Model} ({targetInfo.Serial})");
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<bool> StartRunFromTelegramAsync(int folderId, string platform, bool useAi, int delayMin, int delayMax, bool onlyFailed)
    {
        if (_cts != null) return false;
        if (!_isIosMode && _currentDevice == null) return false;
        if (_workflowSteps.Count == 0) return false;
        if (_jobs.Count == 0) return false;

        List<JobItem> jobsToRun;
        if (onlyFailed)
        {
            jobsToRun = _jobs.Where(j => j.Status == JobStatus.Failed).ToList();
            if (jobsToRun.Count == 0) return false;
            foreach (var job in jobsToRun)
            {
                if (platform == "Facebook") job.FbStatus = JobStatus.FacebookPending;
                else job.ShopeeStatus = JobStatus.ShopeePending;
                job.Status = JobStatus.Waiting;
                job.Log = string.Empty;
                var idx = _jobs.IndexOf(job);
                if (idx >= 0) productListControl.UpdateProduct(idx, job);
                UpdateLegacyJobGrid(job);
                _dbService.UpdateJob(job);
            }
        }
        else if (folderId == -1)
        {
            jobsToRun = _jobs.ToList();
        }
        else if (folderId == 0)
        {
            jobsToRun = _jobs.Where(j => (j.FolderId ?? 0) == 0).ToList();
        }
        else
        {
            jobsToRun = _jobs.Where(j => (j.FolderId ?? 0) == folderId).ToList();
        }

        if (chkOnlyWithLink != null && chkOnlyWithLink.Checked)
        {
            jobsToRun = jobsToRun.Where(j => !string.IsNullOrWhiteSpace(j.ShopeeAffLink)).ToList();
        }

        if (jobsToRun.Count == 0) return false;

        var campaignName = folderId == -1 
            ? "Tất cả chiến dịch" 
            : (folderId == 0 ? "[Chưa phân loại]" : (_dbService.GetAllFolders().FirstOrDefault(f => f.Id == folderId)?.Name ?? $"Folder #{folderId}"));
        if (onlyFailed) campaignName = "Chạy lại video lỗi";

        _ = ExecuteWorkflowRunAsync(jobsToRun, platform, useAi, delayMin, delayMax, campaignName);
        return true;
    }
    private void EditProduct()
    {
        var selectedIndices = productListControl.SelectedIndices;
        if (selectedIndices.Count > 1)
        {
            var selectedProducts = selectedIndices
                .Where(index => index >= 0 && index < _jobs.Count)
                .Select(index => _jobs[index])
                .ToList();
            if (selectedProducts.Count == 0)
            {
                ShowError("Hãy chọn sản phẩm để sửa.");
                return;
            }

            var checkedFolders = productListControl.CheckedFolderIds;
            int defaultFolderId = checkedFolders.Count == 1 ? checkedFolders[0] : -1;
            using var bulkDialog = new ProductEditorDialog(selectedProducts, _jobs, _dbService.GetAllFolders(), defaultFolderId);
            if (bulkDialog.ShowDialog(this) != DialogResult.OK) return;

            for (var i = 0; i < selectedIndices.Count && i < bulkDialog.BulkResults.Count; i++)
            {
                var indexToUpdate = selectedIndices[i];
                if (indexToUpdate < 0 || indexToUpdate >= _jobs.Count) continue;

                var originalItem = _jobs[indexToUpdate];
                var updatedItem = bulkDialog.BulkResults[i];
                updatedItem.Id = originalItem.Id;
                updatedItem.Status = originalItem.Status;
                updatedItem.ShopeeStatus = originalItem.ShopeeStatus;
                updatedItem.FbStatus = originalItem.FbStatus;
                if (updatedItem.FolderId == -1) updatedItem.FolderId = originalItem.FolderId;
                updatedItem.Log = originalItem.Log;
                updatedItem.Data = originalItem.Data;

                if (!string.Equals(originalItem.VideoPath, updatedItem.VideoPath, StringComparison.OrdinalIgnoreCase))
                {
                    JobStatus.Reset(updatedItem);
                }

                _jobs[indexToUpdate] = updatedItem;
            }

            RefreshJobGrid();
            _dbService.UpdateJobs(bulkDialog.BulkResults);
            SaveAutoSavedWorkflow();
            SetStatus($"Đã cập nhật {bulkDialog.BulkResults.Count} sản phẩm");
            return;
        }

        var index = productListControl.SelectedIndex;
        if (index < 0 || index >= _jobs.Count)
        {
            ShowError("Hãy chọn một sản phẩm để sửa.");
            return;
        }

        var original = _jobs[index];
        var singleCheckedFolders = productListControl.CheckedFolderIds;
        int singleDefaultFolderId = singleCheckedFolders.Count == 1 ? singleCheckedFolders[0] : 0;
        using var dialog = new ProductEditorDialog(original, _jobs, _dbService.GetAllFolders(), singleDefaultFolderId);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var updated = dialog.Result;
        updated.Data = original.Data;
        if (!string.Equals(original.VideoPath, updated.VideoPath, StringComparison.OrdinalIgnoreCase))
        {
            JobStatus.Reset(updated);
        }
        _jobs[index] = updated;
        RefreshJobGrid();
        _dbService.UpdateJob(updated);
        SaveAutoSavedWorkflow();
        SetStatus("Đã cập nhật sản phẩm");
    }
    private void DeleteProduct()
    {
        var selectedIndices = productListControl.SelectedIndices;
        if (selectedIndices.Count > 1)
        {
            if (MessageBox.Show(
                    this,
                    $"Xóa {selectedIndices.Count} sản phẩm đã chọn khỏi danh sách?",
                    "Xóa sản phẩm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var toRemove = selectedIndices
                .Where(i => i >= 0 && i < _jobs.Count)
                .Select(i => _jobs[i])
                .ToList();

            var idsToRemove = toRemove.Where(j => j.Id > 0).Select(j => j.Id).ToList();
            if (idsToRemove.Count > 0)
            {
                _dbService.DeleteJobs(idsToRemove);
            }

            foreach (var item in toRemove)
            {
                _jobs.Remove(item);
            }

            RefreshJobGrid();
            SaveAutoSavedWorkflow();
            SetStatus($"Đã xóa {toRemove.Count} sản phẩm");
            return;
        }

        var index = productListControl.SelectedIndex;
        if (index < 0 || index >= _jobs.Count)
        {
            ShowError("Hãy chọn một sản phẩm để xóa.");
            return;
        }

        var targetJob = _jobs[index];
        if (MessageBox.Show(
                this,
                $"Xóa sản phẩm “{targetJob.Title}” khỏi danh sách?",
                "Xóa sản phẩm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        if (targetJob.Id > 0)
        {
            _dbService.DeleteJob(targetJob.Id);
        }

        _jobs.RemoveAt(index);
        RefreshJobGrid();
        SaveAutoSavedWorkflow();
        SetStatus("Đã xóa sản phẩm");
    }

    private void AssignMissingJobIds()
    {
        var usedIds = new HashSet<int>();
        var nextId = usedIds.DefaultIfEmpty(0).Max() + 1;
        foreach (var job in _jobs)
        {
            if (job.Id > 0 && usedIds.Add(job.Id)) continue;
            while (usedIds.Contains(nextId)) nextId++;
            job.Id = nextId++;
            usedIds.Add(job.Id);
        }
    }

    private async Task GenerateAiTitlesBatchAsync(bool onlySelected = false, bool forceRegenerate = false)
    {
        if (_isGeneratingAiTitles)
        {
            MessageBox.Show(this, "Tiến trình tạo tiêu đề AI đang chạy ngầm trong nền. Vui lòng chờ hoàn thành!", "Đang xử lý", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var config = _aiConfigService.Load();
        if (string.IsNullOrWhiteSpace(config.ApiKey) || string.IsNullOrWhiteSpace(config.ApiEndpoint))
        {
            var res = MessageBox.Show(
                this,
                "Bạn chưa cấu hình API AI (Endpoint / API Key).\nBạn có muốn chuyển sang tab Cài đặt để cấu hình ngay bây giờ không?",
                "Chưa cấu hình AI",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                ShowSettingsView();
            }
            return;
        }

        var selectedIndices = productListControl.SelectedIndices;
        List<(int Index, JobItem Job)> targetItems;

        if (onlySelected && selectedIndices.Count > 0)
        {
            targetItems = selectedIndices
                .Where(idx => idx >= 0 && idx < _jobs.Count)
                .Select(idx => (idx, _jobs[idx]))
                .ToList();
        }
        else if (!onlySelected && selectedIndices.Count > 1)
        {
            targetItems = selectedIndices
                .Where(idx => idx >= 0 && idx < _jobs.Count)
                .Select(idx => (idx, _jobs[idx]))
                .ToList();
        }
        else
        {
            var filtered = productListControl.GetFilteredJobs();
            targetItems = filtered
                .Select(j => (_jobs.IndexOf(j), j))
                .Where(x => x.Item1 >= 0)
                .ToList();
        }

        if (targetItems.Count == 0)
        {
            MessageBox.Show(this, "Không có video nào trong danh sách để tạo tiêu đề.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        bool IsTrulyAiGenerated(JobItem j)
        {
            if (!j.IsAiTitleGenerated) return false;
            if (string.IsNullOrWhiteSpace(j.Title)) return false;
            var rawName = Path.GetFileNameWithoutExtension(j.VideoPath);
            return !string.Equals(j.Title.Trim(), rawName?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        var totalTarget = targetItems.Count;
        List<(int Index, JobItem Job)> alreadyGenerated;
        List<(int Index, JobItem Job)> pendingToGenerate;

        if (forceRegenerate)
        {
            var prompt = totalTarget > 1
                ? $"Bạn có chắc chắn muốn TẠO LẠI (ghi đè) tiêu đề AI cho {totalTarget} video đã chọn không?\n\n(Hệ thống sẽ gọi lại AI để viết tiêu đề mới cho tất cả các video này)"
                : "Bạn có chắc chắn muốn TẠO LẠI (ghi đè) tiêu đề AI cho video này không?\n\n(Hệ thống sẽ gọi lại AI để viết tiêu đề mới)";

            if (MessageBox.Show(this, prompt, "Xác nhận tạo lại tiêu đề AI", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            foreach (var item in targetItems)
            {
                item.Job.IsAiTitleGenerated = false;
            }
            pendingToGenerate = targetItems.ToList();
            alreadyGenerated = new List<(int Index, JobItem Job)>();
        }
        else
        {
            alreadyGenerated = targetItems.Where(x => IsTrulyAiGenerated(x.Job)).ToList();
            pendingToGenerate = targetItems.Where(x => !IsTrulyAiGenerated(x.Job)).ToList();

            if (pendingToGenerate.Count == 0)
            {
                var confirmForce = MessageBox.Show(
                    this,
                    $"Tất cả {totalTarget} video đã được tạo tiêu đề AI trước đó.\n\nTheo quy định, hệ thống sẽ KHÔNG gọi lại AI để tránh tốn token API và tránh trùng lặp.\n\nBạn có muốn BẮT BUỘC tạo lại (ghi đè) tiêu đề AI cho {totalTarget} video này không?",
                    "Tất cả video đã có tiêu đề AI",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmForce == DialogResult.Yes)
                {
                    foreach (var item in targetItems)
                    {
                        item.Job.IsAiTitleGenerated = false;
                    }
                    pendingToGenerate = targetItems.ToList();
                    alreadyGenerated.Clear();
                }
                else
                {
                    return;
                }
            }
            else
            {
                string msg;
                if (alreadyGenerated.Count > 0)
                {
                    msg = $"Danh sách xử lý gồm {totalTarget} video:\n" +
                          $"• Đã tạo tiêu đề AI trước đó: {alreadyGenerated.Count} video (sẽ TỰ ĐỘNG BỎ QUA không gọi AI lại)\n" +
                          $"• Cần tạo tiêu đề AI mới: {pendingToGenerate.Count} video\n\n" +
                          $"Tiến trình sẽ chạy ngầm và cập nhật hiển thị trực tiếp lên bảng dữ liệu.\n" +
                          $"Bạn có muốn bắt đầu tạo tiêu đề AI cho {pendingToGenerate.Count} video chưa tạo không?";
                }
                else
                {
                    msg = $"Bạn có muốn bắt đầu tạo tiêu đề AI chạy ngầm cho {pendingToGenerate.Count} video không?\n\n(Tiêu đề mới sẽ được cập nhật trực tiếp lên bảng dữ liệu trong thời gian thực)";
                }

                if (MessageBox.Show(this, msg, "Xác nhận tạo tiêu đề AI", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }
        }

        _isGeneratingAiTitles = true;
        productListControl.SetAiTitleGeneratingState(true);
        SetStatus($"Đang kết nối AI ({config.Model}) {(forceRegenerate ? "tạo lại" : "tạo")} tiêu đề chạy ngầm...");
        progressBar.Maximum = Math.Max(1, pendingToGenerate.Count);
        progressBar.Value = 0;

        _ = Task.Run(async () =>
        {
            var aiService = new Services.AiTitleService();
            int successCount = 0;
            int failCount = 0;

            try
            {
                for (int i = 0; i < pendingToGenerate.Count; i++)
                {
                    var item = pendingToGenerate[i];
                    var job = item.Job;
                    var idx = item.Index;

                    var fileName = Path.GetFileName(job.VideoPath);
                    BeginInvoke(() => SetStatus($"[AI {i + 1}/{pendingToGenerate.Count}] Đang {(forceRegenerate ? "tạo lại" : "tạo")} tiêu đề cho: {fileName}..."));

                    try
                    {
                        string sourceTitle;
                        if (job.Data.TryGetValue("OriginalTitle", out var orig) && !string.IsNullOrWhiteSpace(orig))
                        {
                            sourceTitle = orig;
                        }
                        else
                        {
                            sourceTitle = !string.IsNullOrWhiteSpace(job.Title) ? job.Title : Path.GetFileNameWithoutExtension(job.VideoPath);
                            if (!string.IsNullOrWhiteSpace(sourceTitle))
                            {
                                job.Data["OriginalTitle"] = sourceTitle;
                            }
                        }

                        var newTitle = await aiService.GenerateTitleAsync(config, sourceTitle);
                        if (!string.IsNullOrWhiteSpace(newTitle) && !string.Equals(newTitle.Trim(), sourceTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            job.Title = newTitle.Trim();
                            job.IsAiTitleGenerated = true;
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            job.IsAiTitleGenerated = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[AI] Lỗi tạo tiêu đề cho video {job.VideoPath}: {ex.Message}", ex);
                        failCount++;
                        job.IsAiTitleGenerated = false;
                    }

                    _dbService.UpdateJob(job);

                    // Cập nhật hiển thị trực tiếp từng dòng trên giao diện ngay lập tức
                    var currentProgress = i + 1;
                    BeginInvoke(() =>
                    {
                        productListControl.UpdateProduct(idx, job);
                        UpdateLegacyJobGrid(job);
                        progressBar.Value = Math.Min(progressBar.Maximum, currentProgress);
                    });
                }

                // Khi chạy xong: tự động Refresh toàn bộ bảng dữ liệu để hiển thị ngay lập tức, người dùng không cần load lại
                BeginInvoke(() =>
                {
                    RefreshJobGrid();
                    SaveAutoSavedWorkflow();
                    RefreshOverviewDashboard();

                    SetStatus($"Hoàn tất {(forceRegenerate ? "tạo lại" : "tạo")} tiêu đề AI: Thành công {successCount}/{pendingToGenerate.Count}" + (failCount > 0 ? $", Thất bại: {failCount}" : ""));
                    
                    string summaryMsg = $"Đã hoàn tất {(forceRegenerate ? "tạo lại" : "tạo")} tiêu đề AI trong nền!\n\n" +
                        $"• Thành công: {successCount} video\n" +
                        (failCount > 0 ? $"• Lỗi / Thất bại: {failCount} video\n" : "") +
                        (alreadyGenerated.Count > 0 ? $"• Bỏ qua (đã có tiêu đề AI trước đó): {alreadyGenerated.Count} video\n\n" : "\n");

                    if (failCount > 0 && successCount == 0)
                    {
                        summaryMsg += "⚠️ CẢNH BÁO: Tất cả các API AI được cấu hình đều trả về lỗi hoặc hết hạn ngạch (quota)!\n" +
                            "Vui lòng kiểm tra lại cấu hình API tại tab Cài đặt hoặc xem file Log.";
                    }
                    else
                    {
                        summaryMsg += "Toàn bộ tiêu đề mới đã được hiển thị trực tiếp lên bảng dữ liệu (không cần tải lại).";
                    }

                    MessageBox.Show(
                        this,
                        summaryMsg,
                        failCount > 0 ? "Kết quả tạo tiêu đề AI (Có lỗi)" : "Hoàn thành tạo tiêu đề AI",
                        MessageBoxButtons.OK,
                        failCount > 0 && successCount == 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                });
            }
            finally
            {
                _isGeneratingAiTitles = false;
                BeginInvoke(() =>
                {
                    productListControl.SetAiTitleGeneratingState(false);
                    progressBar.Value = 0;
                });
            }
        });
    }

    private void ShowOverviewView()
    {
        _iosPlaceholderLabel?.Hide();
        ShowAnimatedView(_overviewControl);
        SetActiveNavButton(navOverview);
        RefreshOverviewDashboard();
        SetStatus("Trung tâm điều khiển & Tổng quan hệ thống FlowPilot");
    }

    private void RefreshOverviewDashboard()
    {
        if (_overviewControl == null) return;

        var isConnected = _isIosMode || _currentDevice != null;
        var devModel = _isIosMode ? "Apple iPhone (WDA)" : (_currentDeviceInfo?.Model ?? "Chưa kết nối");
        var devSerial = _isIosMode ? $"Port {_currentIosDeviceId}" : (_currentDeviceInfo?.Serial ?? "N/A");
        var w = _variables.FirstOrDefault(v => v.Name == "ScreenWidth")?.Value;
        var h = _variables.FirstOrDefault(v => v.Name == "ScreenHeight")?.Value;
        var res = (!string.IsNullOrWhiteSpace(w) && !string.IsNullOrWhiteSpace(h) && w != "0")
            ? $"{w} x {h}"
            : (_isIosMode ? "Retina HD" : "Auto");
        var battery = isConnected ? "Đang sạc / Tốt" : "N/A";

        var aiConfig = _aiConfigService?.Load();
        var aiProvider = (aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey))
            ? $"AI: {aiConfig.Model}"
            : "Chưa cấu hình AI";

        var tgActive = _telegramConfig != null && _telegramConfig.EnableNotifications && !string.IsNullOrWhiteSpace(_telegramConfig.BotToken);

        var wfName = _currentWorkflowFile;
        var stepCount = _workflowSteps.Count;

        var folders = _dbService.GetAllFolders();

        _overviewControl.UpdateDashboard(
            _jobs,
            folders,
            devModel,
            devSerial,
            battery,
            res,
            isConnected,
            aiProvider,
            tgActive,
            wfName,
            stepCount,
            _isIosMode
        );
    }

    private async Task HandleOverviewDeviceToolAsync(string tool)
    {
        if (_overviewControl == null) return;

        if (!_isIosMode && _currentDevice == null && tool != "restart_adb")
        {
            _overviewControl.SetToolResult("Vui lòng kết nối thiết bị trước khi sử dụng công cụ!", true);
            return;
        }

        try
        {
            switch (tool)
            {
                case "power":
                    if (_isIosMode)
                    {
                        _overviewControl.SetToolResult("Phím nguồn không hỗ trợ trên iOS WDA.", true);
                    }
                    else
                    {
                        await _adb.SendKeyEventAsync(_currentDevice!, "26");
                        _overviewControl.SetToolResult("Đã gửi tín hiệu Bật/Tắt màn hình (Power).");
                    }
                    break;

                case "home":
                    if (_isIosMode)
                    {
                        _overviewControl.SetToolResult("Về màn hình chính (Home) trên iOS.");
                    }
                    else
                    {
                        await _adb.SendKeyEventAsync(_currentDevice!, "3");
                        _overviewControl.SetToolResult("Đã về Màn hình chính (Home).");
                    }
                    break;

                case "back":
                    if (_isIosMode)
                    {
                        _overviewControl.SetToolResult("Phím quay lại không có trên iOS.", true);
                    }
                    else
                    {
                        await _adb.SendKeyEventAsync(_currentDevice!, "4");
                        _overviewControl.SetToolResult("Đã nhấn phím Quay lại (Back).");
                    }
                    break;

                case "screenshot":
                    byte[]? bytes = null;
                    if (_isIosMode)
                    {
                        bytes = await _iosManager.TakeScreenshotBytesAsync(_currentIosDeviceId);
                    }
                    else
                    {
                        bytes = await _adb.TakeScreenshotBytesAsync(_currentDevice!);
                    }

                    if (bytes != null && bytes.Length > 0)
                    {
                        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                        Directory.CreateDirectory(dir);
                        var filePath = Path.Combine(dir, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                        await File.WriteAllBytesAsync(filePath, bytes);
                        _overviewControl.SetToolResult($"Đã lưu ảnh: {Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        _overviewControl.SetToolResult("Không thể chụp màn hình thiết bị.", true);
                    }
                    break;

                case "restart_adb":
                    _overviewControl.SetToolResult("Đang khởi động lại ADB server...");
                    await _adb.InitializeAsync();
                    await RefreshDevicesAsync();
                    _overviewControl.SetToolResult("Đã khởi động lại ADB và làm mới danh sách thiết bị!");
                    RefreshOverviewDashboard();
                    break;

                case "mediascan":
                    if (_isIosMode)
                    {
                        _overviewControl.SetToolResult("MediaScan chỉ dành cho thiết bị Android.");
                    }
                    else
                    {
                        await _adb.TriggerMediaScanAsync(_currentDevice!, "/sdcard/Download");
                        await _adb.TriggerMediaScanAsync(_currentDevice!, "/sdcard/DCIM/Camera");
                        _overviewControl.SetToolResult("Đã quét lại thư viện media (/sdcard/Download & DCIM)!");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _overviewControl.SetToolResult($"Lỗi thực hiện: {ex.Message}", true);
        }
    }

    private void ShowProductView()
    {
        _iosPlaceholderLabel?.Hide();
        ShowAnimatedView(productModulePanel);
        SetActiveNavButton(navProducts);
    }

    private void ShowWorkflowView()
    {
        _iosPlaceholderLabel?.Hide();
        ShowAnimatedView(workspace);
        SetActiveNavButton(navWorkflow);

        _isIosMode = false;
        UpdateUiForIosMode();
        RefreshWorkflow();
        LoadStepConfig(-1);
        lblStatus.Text = "Đã chuyển sang Sơ đồ quy trình";
    }

    private Label? _iosPlaceholderLabel;

    private void UpdateUiForIosMode()
    {
        var iosSupportedSteps = new HashSet<StepType> { 
            StepType.Start, StepType.End, StepType.Delay, 
            StepType.Tap, StepType.RandomTap, StepType.Swipe, StepType.InputText, 
            StepType.OpenApp, StepType.PushVideo, StepType.PushImage 
        };

        cboStepType.Items.Clear();
        foreach (var type in Enum.GetValues<StepType>())
        {
            if (_isIosMode && !iosSupportedSteps.Contains(type)) continue;
            cboStepType.Items.Add(WorkflowStep.GetTypeLabel(type));
        }
        if (cboStepType.Items.Count > 0) cboStepType.SelectedIndex = 0;

        foreach (Control control in workflowPalette.Controls)
        {
            if (control is Button btn && btn.Tag is StepType type)
                btn.Visible = !_isIosMode || iosSupportedSteps.Contains(type);
            else if (control is Label lbl)
                lbl.Visible = !_isIosMode || lbl.Text != "THAO TAC ADB";
        }
        foreach (Control control in workflowCorePalette.Controls)
        {
            if (control is Button btn && btn.Tag is StepType type)
                btn.Visible = !_isIosMode || iosSupportedSteps.Contains(type);
        }
    }

    private void ShowIosWorkflowView()
    {
        ShowAnimatedView(workspace);
        SetActiveNavButton(navWorkflowIos);

        _isIosMode = true;
        UpdateUiForIosMode();
        RefreshWorkflow();
        LoadStepConfig(-1);
        SetStatus("Tính năng Quy trình iPhone (WDA) đã sẵn sàng!");
    }

    private void InitializeTikTokDownloader()
    {
        var navPanel = sidebar.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
        if (navPanel != null)
        {
            navPanel.Height = 360;
            _navTikTok = CreateNavButton("◌   Tải video TikTok", false);
            _navTikTok.Click += (_, _) => ShowTikTokDownloaderView();
            navPanel.Controls.Add(_navTikTok);
        }

        _tikTokDownloaderControl = new TikTokDownloaderControl
        {
            Visible = false
        };
        _tikTokDownloaderControl.StatusChanged += (_, message) => SetStatus(message);
        viewHost.Controls.Add(_tikTokDownloaderControl);
    }

    private void InitializeSettingsModule()
    {
        var navPanel = sidebar.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
        if (navPanel != null)
        {
            navPanel.Height = 360;
            _navSettings = CreateNavButton("⚙   Cài đặt & Cấu hình", false);
            _navSettings.Click += (_, _) => ShowSettingsView();
            navPanel.Controls.Add(_navSettings);
        }

        _settingsControl = new SettingsControl(_telegramConfigService, _aiConfigService, _dbService, _telegramBotService)
        {
            Visible = false
        };
        _settingsControl.StatusChanged += (_, message) => SetStatus(message);
        _settingsControl.TelegramConfigSaved += (_, _) =>
        {
            _telegramConfig = _telegramConfigService.Load();
            _telegramBotService?.UpdateConfig(_telegramConfig);
        };
        _settingsControl.CampaignsChanged += (_, _) => RefreshJobGrid();
        viewHost.Controls.Add(_settingsControl);
    }

    private void ShowTikTokDownloaderView()
    {
        _iosPlaceholderLabel?.Hide();
        if (_tikTokDownloaderControl == null) return;

        ShowAnimatedView(_tikTokDownloaderControl);
        if (_navTikTok != null) SetActiveNavButton(_navTikTok);
        SetStatus("Sẵn sàng tải video TikTok");
    }

    private void ShowSettingsView()
    {
        _iosPlaceholderLabel?.Hide();
        if (_settingsControl == null) return;

        ShowAnimatedView(_settingsControl);
        if (_navSettings != null) SetActiveNavButton(_navSettings);
        SetStatus("Cấu hình hệ thống FlowPilot");
    }

    private void ShowAnimatedView(Control target)
    {
        viewHost.BringToFront();
        if (_activeView == target && target.Visible) return;

        _fadeTimer?.Stop();
        _fadeTimer?.Dispose();
        _fadeTimer = null;

        foreach (Control view in viewHost.Controls)
            view.Visible = view == target;

        _activeView = target;
        target.Visible = true;
        target.BringToFront();

        var finalBounds = viewHost.ClientRectangle;
        if (finalBounds.Width <= 0 || finalBounds.Height <= 0)
        {
            target.Dock = DockStyle.Fill;
            return;
        }

        target.Dock = DockStyle.None;
        target.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        target.Bounds = new Rectangle(finalBounds.X + 22, finalBounds.Y, finalBounds.Width, finalBounds.Height);
        _fadeValue = 0;
        _fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _fadeTimer.Tick += (_, _) =>
        {
            if (target.IsDisposed)
            {
                _fadeTimer?.Stop();
                return;
            }

            _fadeValue = Math.Min(1, _fadeValue + 0.12);
            var eased = 1 - Math.Pow(1 - _fadeValue, 3);
            var left = finalBounds.X + (int)Math.Round(22 * (1 - eased));
            target.Bounds = new Rectangle(left, finalBounds.Y, finalBounds.Width, finalBounds.Height);

            if (_fadeValue >= 1)
            {
                _fadeTimer?.Stop();
                _fadeTimer?.Dispose();
                _fadeTimer = null;
                target.Dock = DockStyle.Fill;
            }
        };
        _fadeTimer.Start();
    }

    private void SetActiveNavButton(Button activeButton)
    {
        var primary = Color.FromArgb(79, 70, 229);
        foreach (var button in new Button?[] { navOverview, navWorkflow, navWorkflowIos, navProducts, navDevices, navLogs, _navTikTok, _navSettings })
        {
            if (button == null) continue;
            var isActive = button == activeButton;
            button.BackColor = isActive ? primary : Color.Transparent;
            button.ForeColor = isActive ? Color.White : Color.FromArgb(71, 85, 105);
            button.Font = new Font("Segoe UI Semibold", 9.5F);
            button.Padding = isActive ? new Padding(16, 0, 0, 0) : new Padding(14, 0, 0, 0);
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = isActive ? primary : Color.FromArgb(241, 245, 249);
            button.Invalidate();
        }
    }

    private static void StyleStatusCell(DataGridViewCell cell, string status)
    {
        cell.Style.ForeColor = status switch
        {
            "Thành công" => Color.FromArgb(16, 185, 129),
            "Lỗi" => Color.FromArgb(220, 38, 38),
            "Đang chạy" => Color.FromArgb(14, 165, 233),
            "Đã dừng" => Color.FromArgb(245, 158, 11),
            _ => Color.FromArgb(107, 114, 128)
        };
        cell.Style.Font = new Font("Segoe UI Semibold", 8.5F);
    }

    private async Task SendTextThroughAdbAsync(DeviceData device, string text)
    {
        // ADBKeyboard nhận Unicode ổn định qua broadcast base64; không phụ thuộc
        // cửa sổ scrcpy có đang nhận focus hay không.
        await _adb.SetupAdbKeyboardAsync(device);
        await _adb.InputTextAsync(device, text);
        Logger.Info($"[ADB] Đã nhập văn bản ({text.Length} ký tự) qua ADBKeyboard.");
    }

    private async Task StartRunAsync(bool onlySelected = false)
    {
        if (!_isIosMode && _currentDevice == null) { ShowError("Chưa kết nối thiết bị Android."); return; }
        if (_workflowSteps.Count == 0) { ShowError("Workflow đang trống."); return; }
        if (_jobs.Count == 0) { ShowError("Chưa có sản phẩm trong danh sách."); return; }

        var selectedIndices = productListControl.SelectedIndices;
        if (onlySelected && selectedIndices.Count == 0)
        {
            ShowError("Vui lòng chọn ít nhất một video trong danh sách.");
            return;
        }

        string targetPlatform = "Shopee";
        bool useAiTitle = false;
        int delayMin = 0;
        int delayMax = 0;
        int folderId = (cboMainFolderSelect?.SelectedItem as ShopeeVideoUploader.Models.FolderItem)?.Id ?? -1;
        
        using (var dialog = new Controls.PlatformSelectDialog())
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            targetPlatform = dialog.SelectedPlatform;
            useAiTitle = dialog.UseAiTitle;
            delayMin = dialog.DelayMinMinutes;
            delayMax = dialog.DelayMaxMinutes;
        }

        List<Models.JobItem> jobsToRun = [];
        if (onlySelected || folderId == -2) // Run selected rows
        {
            jobsToRun = selectedIndices.Where(i => i >= 0 && i < _jobs.Count).Select(i => _jobs[i]).ToList();
        }
        else if (folderId == -1) // All campaigns
        {
            jobsToRun = _jobs.ToList();
        }
        else // Specific campaign
        {
            jobsToRun = _jobs.Where(j => (j.FolderId ?? 0) == folderId).ToList();
        }

        if (chkOnlyWithLink != null && chkOnlyWithLink.Checked)
        {
            jobsToRun = jobsToRun.Where(j => !string.IsNullOrWhiteSpace(j.ShopeeAffLink)).ToList();
        }

        if (jobsToRun.Count == 0)
        {
            ShowError(chkOnlyWithLink != null && chkOnlyWithLink.Checked
                ? "Không có video nào có link tiếp thị để chạy."
                : (onlySelected ? "Không tìm thấy video đã chọn." : "Chưa có sản phẩm nào thuộc thư mục này."));
            return;
        }

        if (onlySelected)
        {
            var completedCount = jobsToRun.Count(j => JobStatus.IsCompleted(j, targetPlatform));
            if (completedCount == jobsToRun.Count)
            {
                foreach (var job in jobsToRun)
                {
                    if (targetPlatform == "Facebook") job.FbStatus = JobStatus.FacebookPending;
                    else job.ShopeeStatus = JobStatus.ShopeePending;
                    job.Status = JobStatus.Waiting;
                    job.Log = string.Empty;
                    var idx = _jobs.IndexOf(job);
                    if (idx >= 0) productListControl.UpdateProduct(idx, job);
                    UpdateLegacyJobGrid(job);
                    _dbService.UpdateJob(job);
                }
            }
            else if (completedCount > 0)
            {
                var msg = MessageBox.Show(
                    this,
                    $"Có {completedCount}/{jobsToRun.Count} video đã chọn có trạng thái 'Đã up {targetPlatform}'.\nBạn có muốn đặt lại trạng thái để chạy lại tất cả {jobsToRun.Count} video không?",
                    "Chạy lại video đã chọn",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (msg == DialogResult.Yes)
                {
                    foreach (var job in jobsToRun)
                    {
                        if (targetPlatform == "Facebook") job.FbStatus = JobStatus.FacebookPending;
                        else job.ShopeeStatus = JobStatus.ShopeePending;
                        job.Status = JobStatus.Waiting;
                        job.Log = string.Empty;
                        var idx = _jobs.IndexOf(job);
                        if (idx >= 0) productListControl.UpdateProduct(idx, job);
                        UpdateLegacyJobGrid(job);
                        _dbService.UpdateJob(job);
                    }
                }
            }
        }

        var campaignName = (cboMainFolderSelect?.SelectedItem as ShopeeVideoUploader.Models.FolderItem)?.Name ?? "Tất cả chiến dịch";
        if (onlySelected) campaignName = "Video đã chọn";

        await ExecuteWorkflowRunAsync(jobsToRun, targetPlatform, useAiTitle, delayMin, delayMax, campaignName);
    }

    private async Task ExecuteWorkflowRunAsync(
        List<JobItem> jobsToRun,
        string targetPlatform,
        bool useAiTitle,
        int delayMin,
        int delayMax,
        string campaignName)
    {
        Func<JobItem, Task>? generateTitleFunc = null;
        if (useAiTitle)
        {
            var configService = new Services.AiConfigService();
            var aiService = new Services.AiTitleService();
            var config = configService.Load();
            generateTitleFunc = async (job) =>
            {
                var rawName = Path.GetFileNameWithoutExtension(job.VideoPath);
                bool isRealAi = job.IsAiTitleGenerated && !string.IsNullOrWhiteSpace(job.Title) && !string.Equals(job.Title.Trim(), rawName?.Trim(), StringComparison.OrdinalIgnoreCase);
                if (isRealAi)
                {
                    Logger.Info($"[AI] Video #{job.Id} đã có tiêu đề AI trước đó ('{job.Title}'), bỏ qua không gọi AI lại.");
                    return;
                }
                try
                {
                    var sourceTitle = !string.IsNullOrWhiteSpace(job.Title) ? job.Title : rawName;
                    var newTitle = await aiService.GenerateTitleAsync(config, sourceTitle);
                    if (!string.IsNullOrWhiteSpace(newTitle) && !string.Equals(newTitle.Trim(), sourceTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        job.Title = newTitle.Trim();
                        job.IsAiTitleGenerated = true;
                        if (InvokeRequired)
                        {
                            BeginInvoke(() => {
                                var index = _jobs.IndexOf(job);
                                if (index >= 0) productListControl.UpdateProduct(index, job);
                                UpdateLegacyJobGrid(job);
                            });
                        }
                        else
                        {
                            var index = _jobs.IndexOf(job);
                            if (index >= 0) productListControl.UpdateProduct(index, job);
                            UpdateLegacyJobGrid(job);
                        }
                        _dbService.UpdateJob(job);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[AI] Bỏ qua vì lỗi API: {ex.Message}");
                    if (InvokeRequired) BeginInvoke(() => SetStatus($"Lỗi AI: {ex.Message}"));
                    else SetStatus($"Lỗi AI: {ex.Message}");
                }
            };
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        void UpdateUiStart()
        {
            btnStart.Enabled = false;
            btnTestWorkflow.Enabled = false;
            btnStop.Enabled = true;
            productListControl.SetRunningState(true);
            progressBar.Maximum = Math.Max(1, jobsToRun.Count);
            progressBar.Value = 0;
            SetStatus($"Đang chạy workflow ({jobsToRun.Count} video)...");
            workflowCanvas.ClearRunningStep();
        }

        if (InvokeRequired) BeginInvoke(UpdateUiStart);
        else UpdateUiStart();

        var startTime = DateTime.Now;
        _ = _telegramBotService?.SendRunStartedNotificationAsync(jobsToRun.Count, targetPlatform, campaignName);

        try
        {
            Action<int, string, string> onUpdate = (index, status, log) =>
            {
                if (index >= 0 && index < jobsToRun.Count)
                {
                    var currentJob = jobsToRun[index];
                    UpdateJobRow(currentJob, status, log, targetPlatform);

                    if (status is JobStatus.Succeeded or JobStatus.Failed)
                    {
                        BeginInvoke(() => progressBar.Value = Math.Min(progressBar.Maximum, progressBar.Value + 1));
                    }

                    if (status == JobStatus.Succeeded)
                    {
                        var avgDelay = (delayMin + delayMax) / 2;
                        _ = _telegramBotService?.SendVideoFinishedNotificationAsync(
                            currentJob,
                            index + 1,
                            jobsToRun.Count,
                            true,
                            targetPlatform,
                            null,
                            null,
                            avgDelay);
                    }
                    else if (status == JobStatus.Failed)
                    {
                        _ = Task.Run(async () =>
                        {
                            byte[]? screenshot = null;
                            try
                            {
                                if (_isIosMode && !string.IsNullOrEmpty(_currentIosDeviceId))
                                    screenshot = await _iosManager.TakeScreenshotBytesAsync(_currentIosDeviceId);
                                else if (!_isIosMode && _currentDevice != null)
                                    screenshot = await _adb.TakeScreenshotBytesAsync(_currentDevice);
                            }
                            catch { }

                            if (_telegramBotService != null)
                            {
                                await _telegramBotService.SendVideoFinishedNotificationAsync(
                                    currentJob,
                                    index + 1,
                                    jobsToRun.Count,
                                    false,
                                    targetPlatform,
                                    log,
                                    screenshot,
                                    0);
                            }
                        });
                    }
                }
            };

            if (_isIosMode)
            {
                _iosEngine = new IosWorkflowEngine(_iosManager);
                await _iosEngine.RunAllJobsAsync(_workflowSteps, jobsToRun, _currentIosDeviceId, token, onUpdate, targetPlatform, _variables, stepIndex => SetRunningStepFromWorker(stepIndex), preJobAction: generateTitleFunc, delayBetweenJobsMinMinutes: delayMin, delayBetweenJobsMaxMinutes: delayMax);
            }
            else
            {
                _engine = new WorkflowEngine(_adb, SendTextThroughAdbAsync);
                _currentDevice = await _engine.RunAllJobsAsync(_workflowSteps, jobsToRun, _currentDevice!, token, onUpdate, targetPlatform, _variables, stepIndex => SetRunningStepFromWorker(stepIndex), preJobAction: generateTitleFunc, delayBetweenJobsMinMinutes: delayMin, delayBetweenJobsMaxMinutes: delayMax);
            }
            
            SetStatus("Hoàn tất tất cả jobs");
            var duration = DateTime.Now - startTime;
            var succeeded = jobsToRun.Count(j => JobStatus.IsCompleted(j, targetPlatform));
            var failed = jobsToRun.Count(j => j.Status == JobStatus.Failed);
            _ = _telegramBotService?.SendBatchFinishedNotificationAsync(jobsToRun.Count, succeeded, failed, duration, targetPlatform);
        }
        catch (OperationCanceledException) { SetStatus("Đã dừng bởi người dùng"); }
        catch (Exception ex) { ShowError($"Lỗi chạy workflow: {ex.Message}"); }
        finally
        {
            void UpdateUiEnd()
            {
                workflowCanvas.ClearRunningStep();
                btnStart.Enabled = true;
                btnTestWorkflow.Enabled = true;
                btnStop.Enabled = false;
                productListControl.SetRunningState(false);
            }

            if (InvokeRequired) BeginInvoke(UpdateUiEnd);
            else UpdateUiEnd();

            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task StartTestWorkflowAsync()
    {
        if (!_isIosMode && _currentDevice == null)
        {
            ShowError("Chưa kết nối thiết bị.");
            return;
        }
        if (_workflowSteps.Count == 0)
        {
            ShowError("Workflow đang trống.");
            return;
        }
        if (_cts != null) return;
        if (_workflowSteps.Any(step => step.Type is StepType.PushVideo or StepType.PushImage) && _jobs.Count == 0)
        {
            ShowError("Workflow có bước Đẩy video/ảnh. Hãy thêm hoặc nhập sản phẩm trước để chạy thử.");
            return;
        }

        var confirm = MessageBox.Show(
            "Chạy thử sẽ thao tác trực tiếp trên thiết bị hiện tại. Bạn có muốn tiếp tục?",
            "Chạy thử quy trình",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        var selectedIndices = productListControl.SelectedIndices;
        string targetPlatform = "Shopee";
        bool useAiTitle = false;
        int delayMin = 0;
        int delayMax = 0;
        int folderId = (cboMainFolderSelect?.SelectedItem as ShopeeVideoUploader.Models.FolderItem)?.Id ?? -1;
        using (var dialog = new Controls.PlatformSelectDialog())
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            targetPlatform = dialog.SelectedPlatform;
            useAiTitle = dialog.UseAiTitle;
            delayMin = dialog.DelayMinMinutes;
            delayMax = dialog.DelayMaxMinutes;
            
        }

        Func<JobItem, Task>? generateTitleFunc = null;
        if (useAiTitle)
        {
            var configService = new Services.AiConfigService();
            var aiService = new Services.AiTitleService();
            var config = configService.Load();
            generateTitleFunc = async (job) =>
            {
                var rawName = Path.GetFileNameWithoutExtension(job.VideoPath);
                bool isRealAi = job.IsAiTitleGenerated && !string.IsNullOrWhiteSpace(job.Title) && !string.Equals(job.Title.Trim(), rawName?.Trim(), StringComparison.OrdinalIgnoreCase);
                if (isRealAi)
                {
                    Logger.Info($"[AI] Video #{job.Id} đã có tiêu đề AI trước đó ('{job.Title}'), bỏ qua không gọi AI lại.");
                    return;
                }
                try
                {
                    var sourceTitle = !string.IsNullOrWhiteSpace(job.Title) ? job.Title : rawName;
                    var newTitle = await aiService.GenerateTitleAsync(config, sourceTitle);
                    if (!string.IsNullOrWhiteSpace(newTitle) && !string.Equals(newTitle.Trim(), sourceTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        job.Title = newTitle.Trim();
                        job.IsAiTitleGenerated = true;
                        if (InvokeRequired)
                        {
                            BeginInvoke(() => {
                                var index = _jobs.IndexOf(job);
                                if (index >= 0) productListControl.UpdateProduct(index, job);
                                UpdateLegacyJobGrid(job);
                            });
                        }
                        else
                        {
                            var index = _jobs.IndexOf(job);
                            if (index >= 0) productListControl.UpdateProduct(index, job);
                            UpdateLegacyJobGrid(job);
                        }
                        _dbService.UpdateJob(job);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[AI] Bỏ qua vì lỗi API: {ex.Message}");
                    if (InvokeRequired) BeginInvoke(() => SetStatus($"Lỗi AI: {ex.Message}"));
                    else SetStatus($"Lỗi AI: {ex.Message}");
                }
            };
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        
        var candidateJobs = folderId == -1 
            ? _jobs 
            : _jobs.Where(j => (j.FolderId ?? 0) == folderId).ToList();

        if (chkOnlyWithLink != null && chkOnlyWithLink.Checked)
        {
            candidateJobs = candidateJobs.Where(j => !string.IsNullOrWhiteSpace(j.ShopeeAffLink)).ToList();
        }

        JobItem? testJob = null;
        if (targetPlatform == "Shopee")
            testJob = candidateJobs.FirstOrDefault(j => !JobStatus.IsCompleted(j, targetPlatform));
        else if (targetPlatform == "Facebook")
            testJob = candidateJobs.FirstOrDefault(j => !JobStatus.IsCompleted(j, targetPlatform));

        testJob ??= candidateJobs.FirstOrDefault() ?? new JobItem { Id = 0, Title = "Chạy thử" };
        
        btnStart.Enabled = false;
        btnTestWorkflow.Enabled = false;
        btnStop.Enabled = true;
        progressBar.Maximum = Math.Max(1, _workflowSteps.Count);
        progressBar.Value = 0;
        SetStatus("Đang chạy thử workflow...");
        Logger.Info("Bắt đầu chạy thử workflow trên thiết bị.");
        workflowCanvas.ClearRunningStep();

        try
        {
            var progress = new Progress<string>(message =>
            {
                SetStatus($"Chạy thử: {message}");
                progressBar.Value = Math.Min(progressBar.Maximum, progressBar.Value + 1);
                Logger.Info($"[TEST] {message}");
            });

            if (_isIosMode)
            {
                _iosEngine = new IosWorkflowEngine(_iosManager);
                var result = await _iosEngine.ExecuteWorkflowAsync(_workflowSteps, testJob, _currentIosDeviceId, token, progress, _variables,
                    stepIndex => SetRunningStepFromWorker(stepIndex));
                // Chạy thử không phải lần đăng thật: không ghi trạng thái job vào DB.
            }
            else
            {
                _engine = new WorkflowEngine(_adb, SendTextThroughAdbAsync);
                var result = await _engine.ExecuteWorkflowAsync(_workflowSteps, testJob, _currentDevice!, token, progress, _variables,
                    stepIndex => SetRunningStepFromWorker(stepIndex));
                // Chạy thử không phải lần đăng thật: không ghi trạng thái job vào DB.
            }

            SetStatus("Đã dừng chạy thử");
            Logger.Info("Đã dừng chạy thử workflow.");
        }
        catch (Exception ex)
        {
            SetStatus("Chạy thử thất bại");
            Logger.Error("Lỗi chạy thử workflow", ex);
            ShowError($"Chạy thử thất bại: {ex.Message}");
        }
        finally
        {
            workflowCanvas.ClearRunningStep();
            btnStart.Enabled = true;
            btnTestWorkflow.Enabled = true;
            btnStop.Enabled = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void SetRunningStepFromWorker(int stepIndex)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => SetRunningStepFromWorker(stepIndex));
            return;
        }

        workflowCanvas.SetRunningStep(stepIndex);
        SetStatus($"Đang chạy bước {stepIndex + 1}/{_workflowSteps.Count}");
    }

    private void StopRun()
    {
        _cts?.Cancel();
        SetStatus("Đang dừng...");
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F9)
        {
            _ = StartRunAsync();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            StopRun();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.S)
        {
            SaveWorkflow();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.O)
        {
            LoadWorkflow();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            RemoveStep();
            e.Handled = true;
        }
    }

    private void OnLogReceived(string message)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { BeginInvoke(() => OnLogReceived(message)); return; }
        txtLog.AppendText(message + Environment.NewLine);
        txtLog.ScrollToCaret();
    }

    private void SetStatus(string text)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { BeginInvoke(() => SetStatus(text)); return; }
        lblStatus.Text = text;
    }

    private static void ShowError(string message) => MessageBox.Show(message, "FlowPilot", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        AllowSleep();
        _fadeTimer?.Stop();
        _fadeTimer?.Dispose();
        SaveAutoSavedWorkflow();
        _cts?.Cancel();
        _recordingCts?.Cancel();
        _coordinateCaptureCts?.Cancel();
        _adb.Dispose();
        _iosManager.Dispose();
        Logger.OnLog -= OnLogReceived;
    }

    #region Anti-Sleep (Prevent Windows from Sleeping while tool is open)

    [Flags]
    private enum ExecutionState : uint
    {
        ES_SYSTEM_REQUIRED = 0x00000001,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_CONTINUOUS = 0x80000000
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    private static void PreventSleep()
    {
        try
        {
            SetThreadExecutionState(ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED | ExecutionState.ES_DISPLAY_REQUIRED);
            Logger.Info("Đã bật chế độ chống Sleep máy tính khi chạy FlowPilot.");
        }
        catch
        {
        }
    }

    private static void AllowSleep()
    {
        try
        {
            SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
        }
        catch
        {
        }
    }

    #endregion
}
