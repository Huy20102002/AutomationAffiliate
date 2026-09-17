using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader.Controls;

public partial class TelegramConfigDialog : Form
{
    public TelegramConfig Config { get; private set; }
    private readonly TelegramBotService _botService;

    public TelegramConfigDialog(TelegramConfig config)
    {
        InitializeComponent();
        Config = config;
        _botService = new TelegramBotService(config);

        txtBotToken.Text = config.BotToken;
        txtChatId.Text = config.AdminChatIds;
        chkEnableNotifications.Checked = config.EnableNotifications;
        chkNotifyOnStart.Checked = config.NotifyOnStart;
        chkNotifyOnSuccess.Checked = config.NotifyOnSuccess;
        chkNotifyOnError.Checked = config.NotifyOnError;
        chkSendScreenshotOnError.Checked = config.SendScreenshotOnError;
        chkEnableRemoteControl.Checked = config.EnableRemoteControl;
        cboDefaultPlatform.SelectedItem = string.IsNullOrEmpty(config.DefaultPlatform) ? "Shopee" : config.DefaultPlatform;
        chkUseAiTitle.Checked = config.UseAiTitle;
        numDelayMin.Value = Math.Max(0, config.DelayMinMinutes);
        numDelayMax.Value = Math.Max(0, config.DelayMaxMinutes);

        UpdateEnabledStates();
        chkEnableNotifications.CheckedChanged += (_, _) => UpdateEnabledStates();
        chkNotifyOnError.CheckedChanged += (_, _) => UpdateEnabledStates();
    }

    private void UpdateEnabledStates()
    {
        var enabled = chkEnableNotifications.Checked;
        chkNotifyOnStart.Enabled = enabled;
        chkNotifyOnSuccess.Enabled = enabled;
        chkNotifyOnError.Enabled = enabled;
        chkSendScreenshotOnError.Enabled = enabled && chkNotifyOnError.Checked;
    }

    private async void btnTest_Click(object sender, EventArgs e)
    {
        var token = txtBotToken.Text.Trim();
        var chatId = txtChatId.Text.Trim();

        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show(this, "Vui lòng nhập Bot Token trước khi kiểm tra.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnTest.Enabled = false;
        btnTest.Text = "Đang kiểm tra...";

        try
        {
            var (success, message, botName) = await _botService.TestConnectionAsync(token, chatId);
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
            MessageBox.Show(this, $"Lỗi: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTest.Enabled = true;
            btnTest.Text = "⚡ Kiểm tra kết nối";
        }
    }

    private async void btnDetectChatId_Click(object sender, EventArgs e)
    {
        var token = txtBotToken.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show(this, "Vui lòng nhập Bot Token trước để tìm Chat ID.", "Thiếu Token", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnDetectChatId.Enabled = false;
        btnDetectChatId.Text = "Đang tìm...";

        try
        {
            var chats = await _botService.GetRecentChatsAsync(token);
            if (chats.Count == 0)
            {
                MessageBox.Show(this, "Chưa tìm thấy tin nhắn nào!\nHãy mở Telegram, tìm Bot của bạn và bấm /start hoặc nhắn một tin bất kỳ, sau đó bấm nút này lại.", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var firstChat = chats.First();
                txtChatId.Text = firstChat.chatId.ToString();
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
            btnDetectChatId.Enabled = true;
            btnDetectChatId.Text = "🔍 Tìm Chat ID";
        }
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        Config.BotToken = txtBotToken.Text.Trim();
        Config.AdminChatIds = txtChatId.Text.Trim();
        Config.EnableNotifications = chkEnableNotifications.Checked;
        Config.NotifyOnStart = chkNotifyOnStart.Checked;
        Config.NotifyOnSuccess = chkNotifyOnSuccess.Checked;
        Config.NotifyOnError = chkNotifyOnError.Checked;
        Config.SendScreenshotOnError = chkSendScreenshotOnError.Checked;
        Config.EnableRemoteControl = chkEnableRemoteControl.Checked;
        Config.DefaultPlatform = cboDefaultPlatform.SelectedItem?.ToString() ?? "Shopee";
        Config.UseAiTitle = chkUseAiTitle.Checked;
        Config.DelayMinMinutes = (int)numDelayMin.Value;
        Config.DelayMaxMinutes = (int)numDelayMax.Value;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
