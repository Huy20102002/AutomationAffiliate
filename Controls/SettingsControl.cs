using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader.Controls;

/// <summary>
/// Module Cấu hình tập trung: Telegram Bot, AI SEO Tiêu đề và Quản lý Chiến dịch.
/// </summary>
public sealed class SettingsControl : UserControl
{
    private readonly TelegramConfigService _telegramConfigService;
    private readonly AiConfigService _aiConfigService;
    private readonly DatabaseService _dbService;
    private readonly TelegramBotService? _botService;
    private readonly AiTitleService _aiTitleService = new();

    private TelegramConfig _teleConfig = new();
    private AiConfig _aiConfig = new();

    // Navigation buttons
    private readonly Guna2Button _btnTabTele = new();
    private readonly Guna2Button _btnTabAi = new();
    private readonly Guna2Button _btnTabCampaigns = new();

    // Panels for tabs
    private readonly Panel _pnlContentHost = new();
    private readonly Panel _pnlTeleTab = new();
    private readonly Panel _pnlAiTab = new();
    private readonly Panel _pnlCampaignsTab = new();

    // Telegram Controls
    private readonly Guna2TextBox _txtTeleToken = new();
    private readonly Guna2TextBox _txtTeleChatId = new();
    private readonly Guna2Button _btnTeleDetect = new();
    private readonly Guna2Button _btnTeleTest = new();
    private readonly Guna2CheckBox _chkTeleEnable = new();
    private readonly Guna2CheckBox _chkTeleNotifyStart = new();
    private readonly Guna2CheckBox _chkTeleNotifySuccess = new();
    private readonly Guna2CheckBox _chkTeleNotifyError = new();
    private readonly Guna2CheckBox _chkTeleSendScreenshot = new();
    private readonly Guna2CheckBox _chkTeleRemote = new();
    private readonly Guna2ComboBox _cboTelePlatform = new();
    private readonly Guna2CheckBox _chkTeleAi = new();
    private readonly Guna2NumericUpDown _numTeleDelayMin = new();
    private readonly Guna2NumericUpDown _numTeleDelayMax = new();

    // AI Controls
    private readonly Guna2TextBox _txtAiEndpoint = new();
    private readonly Guna2TextBox _txtAiKey = new();
    private readonly Guna2TextBox _txtAiModel = new();
    private readonly Guna2Button _btnAddBackupApi = new();
    private readonly FlowLayoutPanel _flpBackupApis = new();
    private readonly Guna2TextBox _txtAiPrompt = new();
    private readonly Guna2Button _btnAiTest = new();
    private readonly Label _lblAiTestResult = new();

    // Campaign Controls
    private readonly Guna2TextBox _txtCampName = new();
    private readonly Guna2Button _btnCampAdd = new();
    private readonly Guna2Button _btnCampUpdate = new();
    private readonly Guna2Button _btnCampDelete = new();
    private readonly Guna2DataGridView _gridCamp = new();
    private List<FolderItem> _campaignList = [];

    // Footer Controls
    private readonly Guna2Button _btnSaveAll = new();
    private readonly Guna2Button _btnReload = new();
    private readonly Label _lblSaveStatus = new();

    public event EventHandler? TelegramConfigSaved;
    public event EventHandler? AiConfigSaved;
    public event EventHandler? CampaignsChanged;
    public event EventHandler<string>? StatusChanged;

    public SettingsControl(
        TelegramConfigService telegramConfigService,
        AiConfigService aiConfigService,
        DatabaseService dbService,
        TelegramBotService? botService = null)
    {
        _telegramConfigService = telegramConfigService;
        _aiConfigService = aiConfigService;
        _dbService = dbService;
        _botService = botService;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 250, 254);

        BuildUi();
        LoadAllConfigs();
        SwitchTab(0);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16, 14, 16, 14),
            BackColor = Color.FromArgb(246, 250, 254)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));  // Header
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));  // Tab Bar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content Host
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));  // Bottom Action Bar

        root.Controls.Add(BuildHeaderPanel(), 0, 0);
        root.Controls.Add(BuildTabBar(), 0, 1);
        root.Controls.Add(BuildContentHost(), 0, 2);
        root.Controls.Add(BuildBottomBar(), 0, 3);

        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18, 12, 18, 10)
        };

        var lblTitle = new Label
        {
            Text = "⚙   CÀI ĐẶT & CẤU HÌNH HỆ THỐNG",
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 31, 44),
            AutoSize = true,
            Location = new Point(16, 10)
        };

        var lblSub = new Label
        {
            Text = "Quản lý tập trung thông báo Telegram Bot, API AI SEO Tiêu đề và Chiến dịch",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(114, 117, 134),
            AutoSize = true,
            Location = new Point(18, 38)
        };

        header.Controls.AddRange([lblTitle, lblSub]);
        return header;
    }

    private Control BuildTabBar()
    {
        var tabBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4)
        };

        SetupTabButton(_btnTabTele, "✈️  Telegram Bot", 0, 0);
        SetupTabButton(_btnTabAi, "🤖  AI SEO Tiêu đề", 1, 175);
        SetupTabButton(_btnTabCampaigns, "📁  Quản lý Chiến dịch", 2, 350);

        tabBar.Controls.AddRange([_btnTabTele, _btnTabAi, _btnTabCampaigns]);
        return tabBar;
    }

    private void SetupTabButton(Guna2Button btn, string text, int tabIndex, int left)
    {
        btn.Text = text;
        btn.Size = new Size(165, 36);
        btn.Location = new Point(left, 4);
        btn.BorderRadius = 6;
        btn.Font = new Font("Segoe UI Semibold", 9F);
        btn.Cursor = Cursors.Hand;
        btn.Click += (_, _) => SwitchTab(tabIndex);
    }

    private void SwitchTab(int index)
    {
        var tabs = new[] { _btnTabTele, _btnTabAi, _btnTabCampaigns };
        var panels = new[] { _pnlTeleTab, _pnlAiTab, _pnlCampaignsTab };

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isCurrent = i == index;
            tabs[i].FillColor = isCurrent ? Color.FromArgb(96, 82, 218) : Color.White;
            tabs[i].ForeColor = isCurrent ? Color.White : Color.FromArgb(70, 75, 95);
            tabs[i].BorderThickness = isCurrent ? 0 : 1;
            tabs[i].BorderColor = Color.FromArgb(215, 222, 235);
            panels[i].Visible = isCurrent;
            if (isCurrent) panels[i].BringToFront();
        }

        if (index == 2) LoadCampaignsData();
    }

    private Control BuildContentHost()
    {
        _pnlContentHost.Dock = DockStyle.Fill;
        _pnlContentHost.BackColor = Color.White;
        _pnlContentHost.Padding = new Padding(20, 16, 20, 16);

        BuildTeleTab();
        BuildAiTab();
        BuildCampaignsTab();

        _pnlContentHost.Controls.AddRange([_pnlTeleTab, _pnlAiTab, _pnlCampaignsTab]);
        return _pnlContentHost;
    }

    #region Tab 1: Telegram Bot UI

    private void BuildTeleTab()
    {
        _pnlTeleTab.Dock = DockStyle.Fill;
        _pnlTeleTab.AutoScroll = true;
        _pnlTeleTab.BackColor = Color.White;

        var lblToken = new Label { Text = "Bot Token (từ @BotFather):", Font = new Font("Segoe UI Semibold", 9F), Location = new Point(10, 12), AutoSize = true };
        _txtTeleToken.Location = new Point(10, 32);
        _txtTeleToken.Size = new Size(480, 36);
        _txtTeleToken.BorderRadius = 6;
        _txtTeleToken.PasswordChar = '•';
        _txtTeleToken.PlaceholderText = "123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ...";

        var lblChatId = new Label { Text = "Admin Chat ID (các ID cách nhau dấu phẩy):", Font = new Font("Segoe UI Semibold", 9F), Location = new Point(10, 78), AutoSize = true };
        _txtTeleChatId.Location = new Point(10, 98);
        _txtTeleChatId.Size = new Size(330, 36);
        _txtTeleChatId.BorderRadius = 6;
        _txtTeleChatId.PlaceholderText = "Ví dụ: 123456789, 987654321";

        _btnTeleDetect.Text = "🔍 Tìm Chat ID";
        _btnTeleDetect.Location = new Point(350, 98);
        _btnTeleDetect.Size = new Size(140, 36);
        _btnTeleDetect.BorderRadius = 6;
        _btnTeleDetect.FillColor = Color.FromArgb(10, 151, 205);
        _btnTeleDetect.Font = new Font("Segoe UI Semibold", 9F);
        _btnTeleDetect.ForeColor = Color.White;
        _btnTeleDetect.Cursor = Cursors.Hand;
        _btnTeleDetect.Click += BtnTeleDetect_Click;

        _btnTeleTest.Text = "⚡ Test Bot";
        _btnTeleTest.Location = new Point(500, 98);
        _btnTeleTest.Size = new Size(110, 36);
        _btnTeleTest.BorderRadius = 6;
        _btnTeleTest.FillColor = Color.FromArgb(244, 248, 252);
        _btnTeleTest.BorderColor = Color.FromArgb(190, 198, 211);
        _btnTeleTest.BorderThickness = 1;
        _btnTeleTest.Font = new Font("Segoe UI Semibold", 9F);
        _btnTeleTest.ForeColor = Color.FromArgb(31, 31, 44);
        _btnTeleTest.Cursor = Cursors.Hand;
        _btnTeleTest.Click += BtnTeleTest_Click;

        // GroupBox Thông báo
        var gbNotif = new Guna2GroupBox
        {
            Text = "Cài đặt Thông báo Realtime",
            Location = new Point(10, 148),
            Size = new Size(600, 145),
            BorderRadius = 8,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 70, 88),
            CustomBorderColor = Color.FromArgb(240, 243, 248)
        };
        _chkTeleEnable.Text = "Bật hệ thống thông báo Telegram Bot";
        _chkTeleEnable.Location = new Point(15, 45);
        _chkTeleEnable.AutoSize = true;
        _chkTeleEnable.Checked = true;
        _chkTeleEnable.Font = new Font("Segoe UI Semibold", 9F);

        _chkTeleNotifyStart.Text = "Báo khi bắt đầu phiên upload";
        _chkTeleNotifyStart.Location = new Point(15, 72);
        _chkTeleNotifyStart.AutoSize = true;
        _chkTeleNotifyStart.Checked = true;

        _chkTeleNotifySuccess.Text = "Báo khi mỗi video up THÀNH CÔNG";
        _chkTeleNotifySuccess.Location = new Point(15, 98);
        _chkTeleNotifySuccess.AutoSize = true;
        _chkTeleNotifySuccess.Checked = true;

        _chkTeleNotifyError.Text = "Báo khi video bị LỖI";
        _chkTeleNotifyError.Location = new Point(15, 122);
        _chkTeleNotifyError.AutoSize = true;
        _chkTeleNotifyError.Checked = true;

        _chkTeleSendScreenshot.Text = "📸 Tự động chụp ảnh gửi kèm lỗi";
        _chkTeleSendScreenshot.Location = new Point(280, 122);
        _chkTeleSendScreenshot.AutoSize = true;
        _chkTeleSendScreenshot.Checked = true;

        gbNotif.Controls.AddRange([_chkTeleEnable, _chkTeleNotifyStart, _chkTeleNotifySuccess, _chkTeleNotifyError, _chkTeleSendScreenshot]);

        // GroupBox Điều khiển
        var gbRemote = new Guna2GroupBox
        {
            Text = "Điều khiển từ xa qua Telegram Bot",
            Location = new Point(10, 305),
            Size = new Size(600, 140),
            BorderRadius = 8,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 70, 88),
            CustomBorderColor = Color.FromArgb(240, 243, 248)
        };

        _chkTeleRemote.Text = "Cho phép nhận lệnh điều khiển từ xa (Start / Stop / Status / Screenshot)";
        _chkTeleRemote.Location = new Point(15, 45);
        _chkTeleRemote.AutoSize = true;
        _chkTeleRemote.Checked = true;
        _chkTeleRemote.Font = new Font("Segoe UI Semibold", 9F);

        var lblPlat = new Label { Text = "Nền tảng mặc định:", Location = new Point(15, 76), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
        _cboTelePlatform.Location = new Point(135, 70);
        _cboTelePlatform.Size = new Size(120, 30);
        _cboTelePlatform.BorderRadius = 6;
        _cboTelePlatform.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboTelePlatform.Items.AddRange(["Shopee", "Facebook"]);
        _cboTelePlatform.SelectedIndex = 0;

        _chkTeleAi.Text = "Bật AI sinh tiêu đề SEO";
        _chkTeleAi.Location = new Point(275, 76);
        _chkTeleAi.AutoSize = true;

        var lblDelay = new Label { Text = "Nghỉ giữa các video:", Location = new Point(15, 110), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
        _numTeleDelayMin.Location = new Point(140, 106);
        _numTeleDelayMin.Size = new Size(55, 26);
        _numTeleDelayMin.BorderRadius = 4;
        var lblTo = new Label { Text = "đến", Location = new Point(200, 110), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
        _numTeleDelayMax.Location = new Point(230, 106);
        _numTeleDelayMax.Size = new Size(55, 26);
        _numTeleDelayMax.BorderRadius = 4;
        var lblUnit = new Label { Text = "phút", Location = new Point(290, 110), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };

        gbRemote.Controls.AddRange([_chkTeleRemote, lblPlat, _cboTelePlatform, _chkTeleAi, lblDelay, _numTeleDelayMin, lblTo, _numTeleDelayMax, lblUnit]);

        _pnlTeleTab.Controls.AddRange([lblToken, _txtTeleToken, lblChatId, _txtTeleChatId, _btnTeleDetect, _btnTeleTest, gbNotif, gbRemote]);
    }

    private async void BtnTeleDetect_Click(object? sender, EventArgs e)
    {
        var token = _txtTeleToken.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show(this, "Vui lòng nhập Bot Token trước để tìm Chat ID.", "Thiếu Token", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnTeleDetect.Enabled = false;
        _btnTeleDetect.Text = "Đang tìm...";
        try
        {
            var service = _botService ?? new TelegramBotService(new TelegramConfig { BotToken = token });
            var chats = await service.GetRecentChatsAsync(token);
            if (chats.Count == 0)
            {
                MessageBox.Show(this, "Chưa tìm thấy tin nhắn nào!\nHãy mở Telegram, tìm Bot của bạn và bấm /start hoặc nhắn một tin bất kỳ, sau đó bấm nút này lại.", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var firstChat = chats.First();
                _txtTeleChatId.Text = firstChat.chatId.ToString();
                var info = string.Join("\n", chats.Select(c => $"• ID: {c.chatId} ({c.name} - @{c.username})"));
                MessageBox.Show(this, $"Đã tìm thấy Chat ID:\n{info}\n\nĐã tự động điền ID vào ô cấu hình!", "Tìm thấy Chat ID", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTeleDetect.Enabled = true;
            _btnTeleDetect.Text = "🔍 Tìm Chat ID";
        }
    }

    private async void BtnTeleTest_Click(object? sender, EventArgs e)
    {
        var token = _txtTeleToken.Text.Trim();
        var chatId = _txtTeleChatId.Text.Trim();

        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show(this, "Vui lòng nhập Bot Token trước khi kiểm tra.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnTeleTest.Enabled = false;
        _btnTeleTest.Text = "Đang test...";
        try
        {
            var service = _botService ?? new TelegramBotService(new TelegramConfig { BotToken = token });
            var (success, message, botName) = await service.TestConnectionAsync(token, chatId);
            if (success)
            {
                MessageBox.Show(this, message, "Kết nối thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, message, "Kiểm tra thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTeleTest.Enabled = true;
            _btnTeleTest.Text = "⚡ Test Bot";
        }
    }

    #endregion

    #region Tab 2: AI SEO Tiêu đề UI

    private void BuildAiTab()
    {
        _pnlAiTab.Dock = DockStyle.Fill;
        _pnlAiTab.AutoScroll = true;
        _pnlAiTab.BackColor = Color.White;
        _pnlAiTab.Padding = new Padding(16, 12, 16, 24);

        int curY = 12;

        // 1. API Endpoint
        var lblEndpoint = new Label
        {
            Text = "API Endpoint",
            Font = new Font("Segoe UI Semibold", 9F),
            Location = new Point(16, curY),
            AutoSize = true,
            ForeColor = Color.FromArgb(31, 31, 44)
        };
        curY += 24;

        _txtAiEndpoint.Location = new Point(16, curY);
        _txtAiEndpoint.Size = new Size(580, 36);
        _txtAiEndpoint.BorderRadius = 6;
        _txtAiEndpoint.PlaceholderText = "https://api.xkiro.com/v1/chat/completions";
        curY += 46;

        // 2. API Key
        var lblKey = new Label
        {
            Text = "API Key",
            Font = new Font("Segoe UI Semibold", 9F),
            Location = new Point(16, curY),
            AutoSize = true,
            ForeColor = Color.FromArgb(31, 31, 44)
        };
        curY += 24;

        _txtAiKey.Location = new Point(16, curY);
        _txtAiKey.Size = new Size(580, 36);
        _txtAiKey.BorderRadius = 6;
        _txtAiKey.PasswordChar = '•';
        _txtAiKey.PlaceholderText = "sk-...";
        curY += 46;

        // 3. Model
        var lblModel = new Label
        {
            Text = "Model",
            Font = new Font("Segoe UI Semibold", 9F),
            Location = new Point(16, curY),
            AutoSize = true,
            ForeColor = Color.FromArgb(31, 31, 44)
        };
        curY += 24;

        _txtAiModel.Location = new Point(16, curY);
        _txtAiModel.Size = new Size(580, 36);
        _txtAiModel.BorderRadius = 6;
        _txtAiModel.PlaceholderText = "minimax/minimax-m3:free";
        curY += 48;

        // 4. API Dự phòng & Add button
        var lblBackup = new Label
        {
            Text = "API Dự phòng",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Location = new Point(16, curY + 6),
            AutoSize = true,
            ForeColor = Color.FromArgb(31, 31, 44)
        };

        _btnAddBackupApi.Text = "+";
        _btnAddBackupApi.Location = new Point(130, curY);
        _btnAddBackupApi.Size = new Size(34, 32);
        _btnAddBackupApi.BorderRadius = 6;
        _btnAddBackupApi.FillColor = Color.FromArgb(96, 82, 218);
        _btnAddBackupApi.ForeColor = Color.White;
        _btnAddBackupApi.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _btnAddBackupApi.Cursor = Cursors.Hand;
        _btnAddBackupApi.Click += (_, _) => AddBackupApiRow();
        curY += 42;

        _flpBackupApis.Location = new Point(16, curY);
        _flpBackupApis.Size = new Size(585, 200);
        _flpBackupApis.AutoScroll = true;
        _flpBackupApis.WrapContents = false;
        _flpBackupApis.FlowDirection = FlowDirection.TopDown;
        _flpBackupApis.BackColor = Color.White;
        curY += 210;

        // 5. Prompt Template
        var lblPrompt = new Label
        {
            Text = "Prompt Template",
            Font = new Font("Segoe UI Semibold", 9F),
            Location = new Point(16, curY),
            AutoSize = true,
            ForeColor = Color.FromArgb(31, 31, 44)
        };
        curY += 24;

        _txtAiPrompt.Location = new Point(16, curY);
        _txtAiPrompt.Size = new Size(580, 120);
        _txtAiPrompt.Multiline = true;
        _txtAiPrompt.BorderRadius = 6;
        _txtAiPrompt.ScrollBars = ScrollBars.Vertical;
        curY += 130;

        // 6. Test button & status
        _btnAiTest.Text = "Kiểm tra API";
        _btnAiTest.Location = new Point(16, curY);
        _btnAiTest.Size = new Size(130, 38);
        _btnAiTest.BorderRadius = 6;
        _btnAiTest.FillColor = Color.FromArgb(10, 151, 205);
        _btnAiTest.ForeColor = Color.White;
        _btnAiTest.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        _btnAiTest.Cursor = Cursors.Hand;
        _btnAiTest.Click += BtnAiTest_Click;

        _lblAiTestResult.Location = new Point(160, curY + 10);
        _lblAiTestResult.AutoSize = true;
        _lblAiTestResult.Font = new Font("Segoe UI", 9F);
        _lblAiTestResult.ForeColor = Color.FromArgb(40, 167, 69);

        _pnlAiTab.Controls.Clear();
        _pnlAiTab.Controls.AddRange([
            lblEndpoint, _txtAiEndpoint,
            lblKey, _txtAiKey,
            lblModel, _txtAiModel,
            lblBackup, _btnAddBackupApi, _flpBackupApis,
            lblPrompt, _txtAiPrompt,
            _btnAiTest, _lblAiTestResult
        ]);
    }

    private void AddBackupApiRow(string endpoint = "", string apiKey = "", string model = "")
    {
        var rowPanel = new Panel
        {
            Width = 555,
            Height = 80,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        var txtEnd = new Guna2TextBox
        {
            Location = new Point(0, 0),
            Width = 465,
            Height = 36,
            Text = endpoint,
            PlaceholderText = "Endpoint (VD: https://api.groq.com/openai/v1/chat/completions)",
            BorderRadius = 5,
            Font = new Font("Segoe UI", 9F)
        };
        var btnTest = new Guna2Button
        {
            Location = new Point(475, 0),
            Width = 78,
            Height = 36,
            Text = "Test",
            ForeColor = Color.White,
            FillColor = Color.FromArgb(10, 151, 205),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BorderRadius = 5,
            Cursor = Cursors.Hand
        };
        var txtKey = new Guna2TextBox
        {
            Location = new Point(0, 40),
            Width = 265,
            Height = 36,
            Text = apiKey,
            PlaceholderText = "API Key",
            BorderRadius = 5,
            Font = new Font("Segoe UI", 9F)
        };
        var txtModelInput = new Guna2TextBox
        {
            Location = new Point(275, 40),
            Width = 190,
            Height = 36,
            Text = model,
            PlaceholderText = "Model (VD: llama-3.1-70b)",
            BorderRadius = 5,
            Font = new Font("Segoe UI", 9F)
        };
        var btnSwap = new Guna2Button
        {
            Location = new Point(475, 40),
            Width = 36,
            Height = 36,
            Text = "↑",
            ForeColor = Color.White,
            FillColor = Color.FromArgb(46, 204, 113),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            BorderRadius = 5,
            Cursor = Cursors.Hand
        };
        var btnDel = new Guna2Button
        {
            Location = new Point(517, 40),
            Width = 36,
            Height = 36,
            Text = "X",
            ForeColor = Color.White,
            FillColor = Color.FromArgb(239, 68, 68),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            BorderRadius = 5,
            Cursor = Cursors.Hand
        };

        btnDel.Click += (_, _) =>
        {
            _flpBackupApis.Controls.Remove(rowPanel);
            rowPanel.Dispose();
        };

        btnSwap.Click += (_, _) =>
        {
            string oldPriEnd = _txtAiEndpoint.Text;
            string oldPriKey = _txtAiKey.Text;
            string oldPriMod = _txtAiModel.Text;

            _txtAiEndpoint.Text = txtEnd.Text;
            _txtAiKey.Text = txtKey.Text;
            _txtAiModel.Text = string.IsNullOrWhiteSpace(txtModelInput.Text) ? _txtAiModel.Text : txtModelInput.Text;

            txtEnd.Text = oldPriEnd;
            txtKey.Text = oldPriKey;
            txtModelInput.Text = oldPriMod;
        };

        btnTest.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtEnd.Text) || string.IsNullOrWhiteSpace(txtKey.Text))
            {
                MessageBox.Show(this, "Vui lòng điền API Endpoint và API Key của cấu hình dự phòng này", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTest.Enabled = false;
            btnTest.Text = "...";
            try
            {
                var mod = string.IsNullOrWhiteSpace(txtModelInput.Text) ? _txtAiModel.Text.Trim() : txtModelInput.Text.Trim();
                var prompt = !string.IsNullOrWhiteSpace(_txtAiPrompt.Text) ? _txtAiPrompt.Text.Trim() : "Viết lại tiêu đề: {0}";
                string dummyTitle = "Tai nghe bluetooth không dây F9 pro";
                var result = await _aiTitleService.TestCustomApiAsync(txtEnd.Text.Trim(), txtKey.Text.Trim(), mod, prompt, dummyTitle);
                MessageBox.Show(this, $"Call API Dự phòng thành công!\n\nTiêu đề gốc: {dummyTitle}\nTiêu đề AI: {result}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Lỗi gọi API Dự phòng: {ex.Message}", "Lỗi Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "Test";
            }
        };

        rowPanel.Controls.Add(txtEnd);
        rowPanel.Controls.Add(btnTest);
        rowPanel.Controls.Add(txtKey);
        rowPanel.Controls.Add(txtModelInput);
        rowPanel.Controls.Add(btnSwap);
        rowPanel.Controls.Add(btnDel);

        _flpBackupApis.Controls.Add(rowPanel);
    }

    private async void BtnAiTest_Click(object? sender, EventArgs e)
    {
        var endpoint = _txtAiEndpoint.Text.Trim();
        var key = _txtAiKey.Text.Trim();
        var model = _txtAiModel.Text.Trim();
        var prompt = _txtAiPrompt.Text.Trim();

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(endpoint))
        {
            MessageBox.Show(this, "Vui lòng nhập đầy đủ API Endpoint và API Key trước khi kiểm tra.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnAiTest.Enabled = false;
        _btnAiTest.Text = "Đang thử...";
        _lblAiTestResult.Text = "";

        try
        {
            string dummyTitle = "Tai nghe bluetooth không dây F9 pro";
            var result = await _aiTitleService.TestCustomApiAsync(endpoint, key, model, prompt, dummyTitle);
            _lblAiTestResult.Text = "✅ Kết nối thành công!";
            _lblAiTestResult.ForeColor = Color.FromArgb(40, 167, 69);
            MessageBox.Show(this, $"Call API thành công!\n\nTiêu đề gốc: {dummyTitle}\nTiêu đề AI: {result}", "Kết quả Test API", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _lblAiTestResult.Text = "❌ Lỗi kết nối";
            _lblAiTestResult.ForeColor = Color.FromArgb(220, 53, 69);
            MessageBox.Show(this, $"Lỗi gọi API: {ex.Message}", "Lỗi Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnAiTest.Enabled = true;
            _btnAiTest.Text = "Kiểm tra API";
        }
    }

    #endregion

    #region Tab 3: Quản lý Chiến dịch UI

    private void BuildCampaignsTab()
    {
        _pnlCampaignsTab.Dock = DockStyle.Fill;
        _pnlCampaignsTab.BackColor = Color.White;

        var panelTop = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(8) };
        _txtCampName.Location = new Point(10, 10);
        _txtCampName.Size = new Size(260, 36);
        _txtCampName.BorderRadius = 4;
        _txtCampName.PlaceholderText = "Tên chiến dịch mới...";

        _btnCampAdd.Text = "+ Thêm mới";
        _btnCampAdd.Location = new Point(280, 10);
        _btnCampAdd.Size = new Size(100, 36);
        _btnCampAdd.BorderRadius = 4;
        _btnCampAdd.FillColor = Color.FromArgb(96, 82, 218);
        _btnCampAdd.Font = new Font("Segoe UI Semibold", 9F);
        _btnCampAdd.ForeColor = Color.White;
        _btnCampAdd.Cursor = Cursors.Hand;
        _btnCampAdd.Click += BtnCampAdd_Click;

        _btnCampUpdate.Text = "Cập nhật";
        _btnCampUpdate.Location = new Point(390, 10);
        _btnCampUpdate.Size = new Size(90, 36);
        _btnCampUpdate.BorderRadius = 4;
        _btnCampUpdate.FillColor = Color.FromArgb(40, 167, 69);
        _btnCampUpdate.Font = new Font("Segoe UI Semibold", 9F);
        _btnCampUpdate.ForeColor = Color.White;
        _btnCampUpdate.Cursor = Cursors.Hand;
        _btnCampUpdate.Enabled = false;
        _btnCampUpdate.Click += BtnCampUpdate_Click;

        _btnCampDelete.Text = "Xóa";
        _btnCampDelete.Location = new Point(490, 10);
        _btnCampDelete.Size = new Size(80, 36);
        _btnCampDelete.BorderRadius = 4;
        _btnCampDelete.FillColor = Color.FromArgb(220, 53, 69);
        _btnCampDelete.Font = new Font("Segoe UI Semibold", 9F);
        _btnCampDelete.ForeColor = Color.White;
        _btnCampDelete.Cursor = Cursors.Hand;
        _btnCampDelete.Enabled = false;
        _btnCampDelete.Click += BtnCampDelete_Click;

        panelTop.Controls.AddRange([_txtCampName, _btnCampAdd, _btnCampUpdate, _btnCampDelete]);

        _gridCamp.Dock = DockStyle.Fill;
        _gridCamp.AllowUserToAddRows = false;
        _gridCamp.AllowUserToDeleteRows = false;
        _gridCamp.ReadOnly = false;
        _gridCamp.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gridCamp.RowHeadersVisible = false;
        _gridCamp.GridColor = Color.FromArgb(231, 229, 255);
        _gridCamp.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 60, ReadOnly = true });
        _gridCamp.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Tên chiến dịch / Thư mục", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _gridCamp.SelectionChanged += GridCamp_SelectionChanged;

        _pnlCampaignsTab.Controls.Add(_gridCamp);
        _pnlCampaignsTab.Controls.Add(panelTop);
    }

    private void LoadCampaignsData()
    {
        try
        {
            _campaignList = _dbService.GetAllFolders();
            _gridCamp.Rows.Clear();
            foreach (var f in _campaignList)
            {
                _gridCamp.Rows.Add(f.Id, f.Name);
            }
            _gridCamp.ClearSelection();
        }
        catch { }
    }

    private void GridCamp_SelectionChanged(object? sender, EventArgs e)
    {
        var hasSelection = _gridCamp.SelectedRows.Count > 0;
        _btnCampUpdate.Enabled = hasSelection;
        _btnCampDelete.Enabled = hasSelection;
        if (hasSelection)
        {
            _txtCampName.Text = _gridCamp.SelectedRows[0].Cells["Name"].Value?.ToString() ?? "";
        }
        else
        {
            _txtCampName.Text = "";
        }
    }

    private void BtnCampAdd_Click(object? sender, EventArgs e)
    {
        var name = _txtCampName.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var folder = new FolderItem { Name = name };
        _dbService.SaveFolder(folder);
        _txtCampName.Text = "";
        LoadCampaignsData();
        CampaignsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnCampUpdate_Click(object? sender, EventArgs e)
    {
        if (_gridCamp.SelectedRows.Count == 0) return;
        var id = (int)_gridCamp.SelectedRows[0].Cells["Id"].Value;
        var name = _txtCampName.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var folder = new FolderItem { Id = id, Name = name };
        _dbService.UpdateFolder(folder);
        LoadCampaignsData();
        CampaignsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnCampDelete_Click(object? sender, EventArgs e)
    {
        if (_gridCamp.SelectedRows.Count == 0) return;
        var id = (int)_gridCamp.SelectedRows[0].Cells["Id"].Value;
        var name = _gridCamp.SelectedRows[0].Cells["Name"].Value?.ToString();

        if (MessageBox.Show(this, $"Bạn có chắc muốn xóa chiến dịch '{name}' không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        _dbService.DeleteFolder(id);
        _txtCampName.Text = "";
        LoadCampaignsData();
        CampaignsChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Bottom Bar & Save / Reload

    private Control BuildBottomBar()
    {
        var bottomBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };

        _btnSaveAll.Text = "💾  Lưu tất cả cấu hình";
        _btnSaveAll.Location = new Point(16, 8);
        _btnSaveAll.Size = new Size(180, 38);
        _btnSaveAll.BorderRadius = 6;
        _btnSaveAll.FillColor = Color.FromArgb(96, 82, 218);
        _btnSaveAll.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        _btnSaveAll.ForeColor = Color.White;
        _btnSaveAll.Cursor = Cursors.Hand;
        _btnSaveAll.Click += BtnSaveAll_Click;

        _btnReload.Text = "🔄  Tải lại";
        _btnReload.Location = new Point(205, 8);
        _btnReload.Size = new Size(100, 38);
        _btnReload.BorderRadius = 6;
        _btnReload.FillColor = Color.FromArgb(244, 248, 252);
        _btnReload.BorderColor = Color.FromArgb(190, 198, 211);
        _btnReload.BorderThickness = 1;
        _btnReload.Font = new Font("Segoe UI Semibold", 9F);
        _btnReload.ForeColor = Color.FromArgb(31, 31, 44);
        _btnReload.Cursor = Cursors.Hand;
        _btnReload.Click += (_, _) => { LoadAllConfigs(); SetSaveStatus("Đã tải lại cấu hình từ file."); };

        _lblSaveStatus.Location = new Point(320, 18);
        _lblSaveStatus.AutoSize = true;
        _lblSaveStatus.Font = new Font("Segoe UI", 9F);
        _lblSaveStatus.ForeColor = Color.FromArgb(40, 167, 69);

        bottomBar.Controls.AddRange([_btnSaveAll, _btnReload, _lblSaveStatus]);
        return bottomBar;
    }

    private void LoadAllConfigs()
    {
        _teleConfig = _telegramConfigService.Load();
        _txtTeleToken.Text = _teleConfig.BotToken;
        _txtTeleChatId.Text = _teleConfig.AdminChatIds;
        _chkTeleEnable.Checked = _teleConfig.EnableNotifications;
        _chkTeleNotifyStart.Checked = _teleConfig.NotifyOnStart;
        _chkTeleNotifySuccess.Checked = _teleConfig.NotifyOnSuccess;
        _chkTeleNotifyError.Checked = _teleConfig.NotifyOnError;
        _chkTeleSendScreenshot.Checked = _teleConfig.SendScreenshotOnError;
        _chkTeleRemote.Checked = _teleConfig.EnableRemoteControl;
        _cboTelePlatform.SelectedItem = string.IsNullOrEmpty(_teleConfig.DefaultPlatform) ? "Shopee" : _teleConfig.DefaultPlatform;
        _chkTeleAi.Checked = _teleConfig.UseAiTitle;
        _numTeleDelayMin.Value = Math.Max(0, _teleConfig.DelayMinMinutes);
        _numTeleDelayMax.Value = Math.Max(0, _teleConfig.DelayMaxMinutes);

        _aiConfig = _aiConfigService.Load();
        _txtAiEndpoint.Text = _aiConfig.ApiEndpoint;
        _txtAiKey.Text = _aiConfig.ApiKey;
        _txtAiModel.Text = _aiConfig.Model;
        _txtAiPrompt.Text = _aiConfig.PromptTemplate;

        _flpBackupApis.Controls.Clear();
        if (_aiConfig.BackupApis == null) _aiConfig.BackupApis = new List<BackupApiConfig>();
        if (_aiConfig.BackupApis.Count == 0 && !string.IsNullOrWhiteSpace(_aiConfig.BackupApiEndpoint))
        {
            _aiConfig.BackupApis.Add(new BackupApiConfig
            {
                Endpoint = _aiConfig.BackupApiEndpoint,
                ApiKey = _aiConfig.BackupApiKey,
                Model = _aiConfig.BackupModel
            });
        }

        foreach (var backup in _aiConfig.BackupApis)
        {
            AddBackupApiRow(backup.Endpoint, backup.ApiKey, backup.Model);
        }

        LoadCampaignsData();
    }

    private void BtnSaveAll_Click(object? sender, EventArgs e)
    {
        // 1. Save Telegram Config
        _teleConfig.BotToken = _txtTeleToken.Text.Trim();
        _teleConfig.AdminChatIds = _txtTeleChatId.Text.Trim();
        _teleConfig.EnableNotifications = _chkTeleEnable.Checked;
        _teleConfig.NotifyOnStart = _chkTeleNotifyStart.Checked;
        _teleConfig.NotifyOnSuccess = _chkTeleNotifySuccess.Checked;
        _teleConfig.NotifyOnError = _chkTeleNotifyError.Checked;
        _teleConfig.SendScreenshotOnError = _chkTeleSendScreenshot.Checked;
        _teleConfig.EnableRemoteControl = _chkTeleRemote.Checked;
        _teleConfig.DefaultPlatform = _cboTelePlatform.SelectedItem?.ToString() ?? "Shopee";
        _teleConfig.UseAiTitle = _chkTeleAi.Checked;
        _teleConfig.DelayMinMinutes = (int)_numTeleDelayMin.Value;
        _teleConfig.DelayMaxMinutes = (int)_numTeleDelayMax.Value;
        _telegramConfigService.Save(_teleConfig);
        _botService?.UpdateConfig(_teleConfig);
        TelegramConfigSaved?.Invoke(this, EventArgs.Empty);

        // 2. Save AI Config
        _aiConfig.ApiEndpoint = _txtAiEndpoint.Text.Trim();
        _aiConfig.ApiKey = _txtAiKey.Text.Trim();
        _aiConfig.Model = _txtAiModel.Text.Trim();
        _aiConfig.PromptTemplate = _txtAiPrompt.Text.Trim();

        _aiConfig.BackupApis.Clear();
        foreach (Control c in _flpBackupApis.Controls)
        {
            if (c is Panel p && p.Controls.Count >= 4)
            {
                var tEnd = p.Controls[0] as Guna2TextBox;
                var tKey = p.Controls[2] as Guna2TextBox;
                var tModel = p.Controls[3] as Guna2TextBox;
                if (tEnd != null && tKey != null && !string.IsNullOrWhiteSpace(tEnd.Text))
                {
                    _aiConfig.BackupApis.Add(new BackupApiConfig
                    {
                        Endpoint = tEnd.Text.Trim(),
                        ApiKey = tKey.Text.Trim(),
                        Model = tModel?.Text.Trim() ?? ""
                    });
                }
            }
        }

        if (_aiConfig.BackupApis.Count > 0)
        {
            _aiConfig.BackupApiEndpoint = _aiConfig.BackupApis[0].Endpoint;
            _aiConfig.BackupApiKey = _aiConfig.BackupApis[0].ApiKey;
            _aiConfig.BackupModel = _aiConfig.BackupApis[0].Model;
        }
        else
        {
            _aiConfig.BackupApiEndpoint = string.Empty;
            _aiConfig.BackupApiKey = string.Empty;
            _aiConfig.BackupModel = string.Empty;
        }

        _aiConfigService.Save(_aiConfig);
        AiConfigSaved?.Invoke(this, EventArgs.Empty);

        SetSaveStatus("✅ Đã lưu toàn bộ cấu hình thành công!");
        StatusChanged?.Invoke(this, "Đã lưu toàn bộ cấu hình hệ thống.");
    }

    private void SetSaveStatus(string msg)
    {
        _lblSaveStatus.Text = msg;
        var timer = new System.Windows.Forms.Timer { Interval = 4000 };
        timer.Tick += (_, _) => { _lblSaveStatus.Text = ""; timer.Stop(); timer.Dispose(); };
        timer.Start();
    }

    #endregion
}
