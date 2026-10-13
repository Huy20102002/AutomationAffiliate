using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader.Controls;

public sealed class PlatformSelectDialog : Form
{
    public string SelectedPlatform { get; private set; } = string.Empty;
    public bool IsMultiWorkflow { get; private set; } = false;
    public MultiWorkflowConfig MultiConfig { get; private set; } = new();
    public bool UseAiTitle { get; private set; }
    public int DelayMinMinutes { get; private set; }
    public int DelayMaxMinutes { get; private set; }
    public bool ShuffleVideos { get; private set; } = true;
    public bool LimitJobCount { get; private set; }
    public int MaxJobsToRun { get; private set; } = 50;
    public bool AutoCloseAppOnFinish { get; private set; }

    private readonly Guna2RadioButton _rbSingle;
    private readonly Guna2RadioButton _rbMulti;
    private readonly Panel _pnlSingleControls;
    private readonly Panel _pnlMultiControls;

    // Multi controls
    private readonly Guna2ComboBox _cboWf1;
    private readonly Guna2ComboBox _cboPlatform1;
    private readonly Guna2ComboBox _cboWf2;
    private readonly Guna2ComboBox _cboPlatform2;
    private readonly Guna2RadioButton _rbModePerVideo;
    private readonly Guna2RadioButton _rbModeBatch;
    private readonly Guna2NumericUpDown _numDelayWorkflows;
    private readonly Guna2NumericUpDown _numUploadWait;
    private readonly Guna2CheckBox _chkSkipCompleted;
    private readonly Guna2CheckBox _chkContinueOnError;

    // Common controls
    private readonly Guna2CheckBox _chkUseAi;
    private readonly Guna2NumericUpDown _numMin;
    private readonly Guna2NumericUpDown _numMax;
    private readonly Guna2CheckBox _chkShuffle;
    private readonly Guna2CheckBox _chkLimitJobs;
    private readonly Guna2NumericUpDown _numMaxJobs;
    private readonly Guna2CheckBox _chkAutoClose;

    // Bottom action buttons
    private readonly Guna2Button _btnShopee;
    private readonly Guna2Button _btnFb;
    private readonly Guna2Button _btnStartMulti;

    private readonly MultiWorkflowConfigService _multiConfigService = new();

    public PlatformSelectDialog(
        IReadOnlyList<string>? availableWorkflows = null,
        string? currentWorkflow = null,
        MultiWorkflowConfig? initialMultiConfig = null)
    {
        Text = "Cấu hình chạy & Chọn quy trình";
        ClientSize = new Size(530, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        AutoScroll = true;

        var savedConfig = initialMultiConfig ?? _multiConfigService.Load();

        // Lấy danh sách workflow files có sẵn
        var wfFiles = new List<string>();
        if (availableWorkflows != null && availableWorkflows.Count > 0)
        {
            wfFiles.AddRange(availableWorkflows);
        }
        else
        {
            var wfDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowPilot", "Workflows");
            if (Directory.Exists(wfDir))
            {
                var files = Directory.GetFiles(wfDir, "*.json")
                    .Select(Path.GetFileName)
                    .Where(f => !string.IsNullOrEmpty(f) && !f.StartsWith("_ios_"))
                    .Cast<string>();
                wfFiles.AddRange(files);
            }
        }
        if (wfFiles.Count == 0) wfFiles.Add("Shopee_Upload.json");

        // 1. Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(249, 250, 254),
            Padding = new Padding(20, 10, 20, 8)
        };
        pnlHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 235, 245));
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "🚀 Cấu hình chạy & Chọn Quy trình",
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(18, 10)
        };
        var lblDesc = new Label
        {
            Text = "Chọn chạy đơn lẻ một nền tảng hoặc chạy liên hoàn 2 quy trình (FB ➔ Shopee)",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(20, 36)
        };
        pnlHeader.Controls.AddRange([lblTitle, lblDesc]);

        // 2. Mode Selector (Single vs Multi-Workflow)
        var pnlModeSelector = new Panel
        {
            Location = new Point(18, 75),
            Size = new Size(475, 42),
            BackColor = Color.FromArgb(241, 245, 249)
        };

        _rbSingle = new Guna2RadioButton
        {
            Text = "Chạy đơn nền tảng",
            Location = new Point(20, 8),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(51, 65, 85),
            Checked = !savedConfig.Enabled,
            Cursor = Cursors.Hand
        };
        _rbMulti = new Guna2RadioButton
        {
            Text = "🔁 Chạy liên hoàn 2 quy trình (FB ➔ Shopee...)",
            Location = new Point(180, 8),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(124, 58, 237),
            Checked = savedConfig.Enabled,
            Cursor = Cursors.Hand
        };
        pnlModeSelector.Controls.AddRange([_rbSingle, _rbMulti]);

        // 3. Multi-Workflow Panel
        _pnlMultiControls = new Panel
        {
            Location = new Point(18, 125),
            Size = new Size(475, 255),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(253, 250, 255),
            Visible = savedConfig.Enabled
        };

        var lblWf1 = new Label
        {
            Text = "1️⃣ Quy trình 1 (Khởi đầu):",
            Location = new Point(12, 10),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(79, 70, 229)
        };
        _cboWf1 = new Guna2ComboBox
        {
            Location = new Point(12, 32),
            Size = new Size(290, 32),
            BorderRadius = 4,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        _cboWf1.Items.AddRange(wfFiles.ToArray());
        var targetWf1 = !string.IsNullOrEmpty(savedConfig.WorkflowFile1) && wfFiles.Contains(savedConfig.WorkflowFile1)
            ? savedConfig.WorkflowFile1
            : (wfFiles.FirstOrDefault(f => f.Contains("Facebook", StringComparison.OrdinalIgnoreCase)) ?? wfFiles[0]);
        _cboWf1.SelectedItem = targetWf1;

        _cboPlatform1 = new Guna2ComboBox
        {
            Location = new Point(310, 32),
            Size = new Size(145, 32),
            BorderRadius = 4,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        _cboPlatform1.Items.AddRange(["Facebook", "Shopee"]);
        _cboPlatform1.SelectedItem = !string.IsNullOrEmpty(savedConfig.Platform1) ? savedConfig.Platform1 : "Facebook";

        var btnSwap = new Guna2Button
        {
            Text = "⇅ Đổi chiều thứ tự 2 quy trình",
            Location = new Point(12, 70),
            Size = new Size(443, 26),
            FillColor = Color.FromArgb(243, 232, 255),
            ForeColor = Color.FromArgb(126, 34, 206),
            Font = new Font("Segoe UI Semibold", 8.25F),
            BorderRadius = 4,
            Cursor = Cursors.Hand
        };
        btnSwap.Click += (_, _) =>
        {
            var tmpWf = _cboWf1.SelectedItem;
            _cboWf1.SelectedItem = _cboWf2.SelectedItem;
            _cboWf2.SelectedItem = tmpWf;

            var tmpPlat = _cboPlatform1.SelectedItem;
            _cboPlatform1.SelectedItem = _cboPlatform2.SelectedItem;
            _cboPlatform2.SelectedItem = tmpPlat;
        };

        var lblWf2 = new Label
        {
            Text = "2️⃣ Quy trình 2 (Tiếp theo):",
            Location = new Point(12, 102),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(234, 88, 12)
        };
        _cboWf2 = new Guna2ComboBox
        {
            Location = new Point(12, 124),
            Size = new Size(290, 32),
            BorderRadius = 4,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        _cboWf2.Items.AddRange(wfFiles.ToArray());
        var targetWf2 = !string.IsNullOrEmpty(savedConfig.WorkflowFile2) && wfFiles.Contains(savedConfig.WorkflowFile2)
            ? savedConfig.WorkflowFile2
            : (wfFiles.FirstOrDefault(f => f.Contains("Shopee", StringComparison.OrdinalIgnoreCase)) ?? (wfFiles.Count > 1 ? wfFiles[1] : wfFiles[0]));
        _cboWf2.SelectedItem = targetWf2;

        _cboPlatform2 = new Guna2ComboBox
        {
            Location = new Point(310, 124),
            Size = new Size(145, 32),
            BorderRadius = 4,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        _cboPlatform2.Items.AddRange(["Shopee", "Facebook"]);
        _cboPlatform2.SelectedItem = !string.IsNullOrEmpty(savedConfig.Platform2) ? savedConfig.Platform2 : "Shopee";

        // Chế độ chạy
        var lblMode = new Label
        {
            Text = "Chế độ chạy liên hoàn:",
            Location = new Point(12, 164),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.75F),
            ForeColor = Color.FromArgb(51, 65, 85)
        };
        _rbModePerVideo = new Guna2RadioButton
        {
            Text = "Theo từng video (Xong QT1 chuyển ngay sang QT2)",
            Location = new Point(12, 184),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F),
            Checked = savedConfig.Mode == MultiWorkflowExecutionMode.PerVideo,
            Cursor = Cursors.Hand
        };
        _rbModeBatch = new Guna2RadioButton
        {
            Text = "Theo lượt danh sách (Xong hết QT1 mới sang QT2)",
            Location = new Point(12, 204),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F),
            Checked = savedConfig.Mode == MultiWorkflowExecutionMode.BatchByWorkflow,
            Cursor = Cursors.Hand
        };

        _chkSkipCompleted = new Guna2CheckBox
        {
            Text = "Tự động bỏ qua nền tảng đã up thành công (tránh up trùng)",
            Location = new Point(12, 226),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.FromArgb(5, 150, 105),
            Checked = savedConfig.SkipIfPlatformAlreadyDone,
            Cursor = Cursors.Hand
        };

        _pnlMultiControls.Controls.AddRange([
            lblWf1, _cboWf1, _cboPlatform1,
            btnSwap,
            lblWf2, _cboWf2, _cboPlatform2,
            lblMode, _rbModePerVideo, _rbModeBatch,
            _chkSkipCompleted
        ]);

        // 4. Panel Single Notice
        _pnlSingleControls = new Panel
        {
            Location = new Point(18, 125),
            Size = new Size(475, 42),
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = !savedConfig.Enabled
        };
        var lblSingleInfo = new Label
        {
            Text = $"Quy trình hiện tại: '{currentWorkflow ?? wfFiles[0]}'. Chọn nền tảng để upload bên dưới.",
            Location = new Point(10, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.75F),
            ForeColor = Color.FromArgb(71, 85, 105)
        };
        _pnlSingleControls.Controls.Add(lblSingleInfo);

        // 5. Common Configuration Settings
        int startY = 388;

        // Tùy chọn thời gian nghỉ giữa 2 workflow và thời gian chờ upload
        var pnlTimeConfig = new Panel
        {
            Location = new Point(18, startY),
            Size = new Size(475, 76),
            BackColor = Color.FromArgb(250, 250, 252),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblDelayWf = new Label
        {
            Text = "⏱ Nghỉ giữa 2 quy trình:",
            Location = new Point(10, 10),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.75F),
            ForeColor = Color.FromArgb(51, 65, 85)
        };
        _numDelayWorkflows = new Guna2NumericUpDown
        {
            Location = new Point(175, 6),
            Size = new Size(65, 28),
            Minimum = 0,
            Maximum = 600,
            Value = Math.Clamp(savedConfig.DelayBetweenWorkflowsSeconds, 0, 600),
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        var lblDelayWfUnit = new Label
        {
            Text = "giây (đóng app cũ, ổn định RAM)",
            Location = new Point(245, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.25F),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        var lblUploadWait = new Label
        {
            Text = "⏱ Chờ up video xong:",
            Location = new Point(10, 42),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.75F),
            ForeColor = Color.FromArgb(51, 65, 85)
        };
        _numUploadWait = new Guna2NumericUpDown
        {
            Location = new Point(175, 38),
            Size = new Size(65, 28),
            Minimum = 0,
            Maximum = 600,
            Value = Math.Clamp(savedConfig.UploadWaitTimeoutSeconds, 0, 600),
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        var lblUploadWaitUnit = new Label
        {
            Text = "giây (chờ video tải lên hoàn tất)",
            Location = new Point(245, 42),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.25F),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        pnlTimeConfig.Controls.AddRange([lblDelayWf, _numDelayWorkflows, lblDelayWfUnit, lblUploadWait, _numUploadWait, lblUploadWaitUnit]);

        // Tùy chọn AI
        _chkUseAi = new Guna2CheckBox
        {
            Text = "Tạo tiêu đề chuẩn SEO bằng AI trước khi đăng",
            Location = new Point(20, startY + 86),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        var btnConfigAi = new Guna2Button
        {
            Text = "⚙",
            Location = new Point(460, startY + 82),
            Size = new Size(32, 28),
            FillColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 11F),
            BorderRadius = 4,
            Cursor = Cursors.Hand
        };
        btnConfigAi.Click += (_, _) =>
        {
            var configService = new AiConfigService();
            var config = configService.Load();
            using var dialog = new AiConfigDialog(config);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                configService.Save(dialog.Config);
            }
        };

        // Nghỉ giữa các video (Delay Min - Max phút)
        var lblDelayVideos = new Label
        {
            Text = "Nghỉ giữa các video (phút):",
            Location = new Point(20, startY + 120),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F)
        };
        _numMin = new Guna2NumericUpDown
        {
            Location = new Point(220, startY + 116),
            Size = new Size(65, 30),
            Minimum = 0,
            Maximum = 999,
            Value = 0,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        var lblTo = new Label
        {
            Text = "-",
            Location = new Point(292, startY + 120),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F)
        };
        _numMax = new Guna2NumericUpDown
        {
            Location = new Point(310, startY + 116),
            Size = new Size(65, 30),
            Minimum = 0,
            Maximum = 999,
            Value = 0,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };

        // Xáo trộn video
        _chkShuffle = new Guna2CheckBox
        {
            Text = "🔀 Xáo trộn thứ tự (tránh up liên tiếp cùng 1 sản phẩm)",
            Location = new Point(20, startY + 154),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(31, 31, 44),
            Checked = true,
            Cursor = Cursors.Hand
        };

        // Giới hạn số video
        _chkLimitJobs = new Guna2CheckBox
        {
            Text = "Giới hạn số video chạy:",
            Location = new Point(20, startY + 188),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(31, 31, 44),
            Checked = false,
            Cursor = Cursors.Hand
        };
        _numMaxJobs = new Guna2NumericUpDown
        {
            Location = new Point(220, startY + 184),
            Size = new Size(155, 30),
            Minimum = 1,
            Maximum = 99999,
            Value = 50,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _chkLimitJobs.CheckedChanged += (_, _) => _numMaxJobs.Enabled = _chkLimitJobs.Checked;

        // Tự động tắt ứng dụng
        _chkAutoClose = new Guna2CheckBox
        {
            Text = "⚡ Tự động tắt ứng dụng sau khi hoàn tất",
            Location = new Point(20, startY + 222),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(109, 40, 217),
            Checked = false,
            Cursor = Cursors.Hand
        };

        // 6. Action buttons
        _btnShopee = new Guna2Button
        {
            Text = "Chạy Shopee",
            Location = new Point(20, startY + 260),
            Size = new Size(225, 46),
            FillColor = Color.FromArgb(238, 77, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            BorderRadius = 6,
            Cursor = Cursors.Hand
        };
        _btnShopee.Click += (_, _) =>
        {
            SelectedPlatform = "Shopee";
            IsMultiWorkflow = false;
            SaveAndExit();
        };

        _btnFb = new Guna2Button
        {
            Text = "Chạy Facebook",
            Location = new Point(265, startY + 260),
            Size = new Size(225, 46),
            FillColor = Color.FromArgb(24, 119, 242),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            BorderRadius = 6,
            Cursor = Cursors.Hand
        };
        _btnFb.Click += (_, _) =>
        {
            SelectedPlatform = "Facebook";
            IsMultiWorkflow = false;
            SaveAndExit();
        };

        _btnStartMulti = new Guna2Button
        {
            Text = "🚀 Bắt đầu Chạy Liên Hoàn 2 Quy Trình",
            Location = new Point(20, startY + 260),
            Size = new Size(470, 46),
            FillColor = Color.FromArgb(124, 58, 237),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            BorderRadius = 6,
            Cursor = Cursors.Hand,
            Visible = savedConfig.Enabled
        };
        _btnStartMulti.Click += (_, _) =>
        {
            SelectedPlatform = "Multi";
            IsMultiWorkflow = true;
            SaveAndExit();
        };

        // Event chuyển đổi Single / Multi
        void UpdateModeVisuals()
        {
            bool isMulti = _rbMulti.Checked;
            _pnlMultiControls.Visible = isMulti;
            _pnlSingleControls.Visible = !isMulti;
            _btnStartMulti.Visible = isMulti;
            _btnShopee.Visible = !isMulti;
            _btnFb.Visible = !isMulti;

            if (isMulti)
            {
                _pnlMultiControls.Location = new Point(18, 125);
                pnlTimeConfig.Location = new Point(18, 388);
                UpdateYPositions(388);
            }
            else
            {
                _pnlSingleControls.Location = new Point(18, 125);
                pnlTimeConfig.Location = new Point(18, 175);
                UpdateYPositions(175);
            }
        }

        void UpdateYPositions(int baseTimeY)
        {
            pnlTimeConfig.Location = new Point(18, baseTimeY);
            int cur = baseTimeY + 84;
            _chkUseAi.Location = new Point(20, cur);
            btnConfigAi.Location = new Point(460, cur - 4);
            cur += 34;
            lblDelayVideos.Location = new Point(20, cur + 4);
            _numMin.Location = new Point(220, cur);
            lblTo.Location = new Point(292, cur + 4);
            _numMax.Location = new Point(310, cur);
            cur += 36;
            _chkShuffle.Location = new Point(20, cur);
            cur += 32;
            _chkLimitJobs.Location = new Point(20, cur + 2);
            _numMaxJobs.Location = new Point(220, cur);
            cur += 34;
            _chkAutoClose.Location = new Point(20, cur);
            cur += 38;
            _btnShopee.Location = new Point(20, cur);
            _btnFb.Location = new Point(265, cur);
            _btnStartMulti.Location = new Point(20, cur);
            ClientSize = new Size(530, Math.Min(cur + 80, 740));
        }

        _rbSingle.CheckedChanged += (_, _) => UpdateModeVisuals();
        _rbMulti.CheckedChanged += (_, _) => UpdateModeVisuals();

        UpdateModeVisuals();

        Controls.AddRange([
            pnlHeader,
            pnlModeSelector,
            _pnlSingleControls,
            _pnlMultiControls,
            pnlTimeConfig,
            _chkUseAi, btnConfigAi,
            lblDelayVideos, _numMin, lblTo, _numMax,
            _chkShuffle,
            _chkLimitJobs, _numMaxJobs,
            _chkAutoClose,
            _btnShopee, _btnFb, _btnStartMulti
        ]);
    }

    private void SaveAndExit()
    {
        UseAiTitle = _chkUseAi.Checked;
        DelayMinMinutes = (int)_numMin.Value;
        DelayMaxMinutes = (int)_numMax.Value;
        ShuffleVideos = _chkShuffle.Checked;
        LimitJobCount = _chkLimitJobs.Checked;
        MaxJobsToRun = (int)_numMaxJobs.Value;
        AutoCloseAppOnFinish = _chkAutoClose.Checked;

        MultiConfig = new MultiWorkflowConfig
        {
            Enabled = IsMultiWorkflow,
            WorkflowFile1 = _cboWf1.SelectedItem as string ?? string.Empty,
            Platform1 = _cboPlatform1.SelectedItem as string ?? "Facebook",
            WorkflowFile2 = _cboWf2.SelectedItem as string ?? string.Empty,
            Platform2 = _cboPlatform2.SelectedItem as string ?? "Shopee",
            Mode = _rbModeBatch.Checked ? MultiWorkflowExecutionMode.BatchByWorkflow : MultiWorkflowExecutionMode.PerVideo,
            SkipIfPlatformAlreadyDone = _chkSkipCompleted.Checked,
            DelayBetweenWorkflowsSeconds = (int)_numDelayWorkflows.Value,
            UploadWaitTimeoutSeconds = (int)_numUploadWait.Value
        };

        // Lưu cấu hình cho lần sau
        _multiConfigService.Save(MultiConfig);

        DialogResult = DialogResult.OK;
        Close();
    }
}
