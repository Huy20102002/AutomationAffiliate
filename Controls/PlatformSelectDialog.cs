using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShopeeVideoUploader.Controls;

public sealed class PlatformSelectDialog : Form
{
    public string SelectedPlatform { get; private set; } = string.Empty;
    public bool UseAiTitle { get; private set; }
    public int DelayMinMinutes { get; private set; }
    public int DelayMaxMinutes { get; private set; }
    public bool ShuffleVideos { get; private set; } = true;

    public PlatformSelectDialog()
    {
        Text = "Chọn nền tảng & Cấu hình chạy";
        ClientSize = new Size(390, 295);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        var lblTitle = new Label
        {
            Text = "Chọn nền tảng để upload",
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Color.FromArgb(31, 31, 44),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var lblDesc = new Label
        {
            Text = "Trạng thái thành công sẽ được cập nhật riêng cho nền tảng mà bạn chọn.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(114, 117, 134),
            Location = new Point(22, 48),
            Size = new Size(346, 30)
        };
        
        var chkUseAi = new Guna.UI2.WinForms.Guna2CheckBox
        {
            Text = "Tạo tiêu đề chuẩn SEO bằng AI trước khi đăng",
            Location = new Point(22, 95),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        
        var btnConfigAi = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "⚙",
            Location = new Point(340, 90),
            Size = new Size(30, 30),
            FillColor = Color.FromArgb(235, 237, 242),
            ForeColor = Color.FromArgb(70, 70, 85),
            Font = new Font("Segoe UI", 12F),
            BorderRadius = 4,
            Cursor = Cursors.Hand
        };
        btnConfigAi.Click += (_, _) =>
        {
            var configService = new Services.AiConfigService();
            var config = configService.Load();
            using var dialog = new AiConfigDialog(config);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                configService.Save(dialog.Config);
            }
        };

        var lblDelay = new Label
        {
            Text = "Nghỉ giữa các video (phút):",
            Location = new Point(22, 135),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F)
        };

        var numMin = new Guna.UI2.WinForms.Guna2NumericUpDown
        {
            Location = new Point(200, 130),
            Size = new Size(65, 30),
            Minimum = 0,
            Maximum = 999,
            Value = 0,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            UpDownButtonFillColor = Color.FromArgb(235, 237, 242)
        };

        var lblTo = new Label
        {
            Text = "-",
            Location = new Point(272, 135),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F)
        };

        var numMax = new Guna.UI2.WinForms.Guna2NumericUpDown
        {
            Location = new Point(295, 130),
            Size = new Size(65, 30),
            Minimum = 0,
            Maximum = 999,
            Value = 0,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            UpDownButtonFillColor = Color.FromArgb(235, 237, 242)
        };

        var chkShuffle = new Guna.UI2.WinForms.Guna2CheckBox
        {
            Text = "🔀 Xáo trộn thứ tự (tránh up liên tiếp cùng 1 sản phẩm)",
            Location = new Point(22, 175),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(31, 31, 44),
            Checked = true,
            Cursor = Cursors.Hand
        };

        var btnShopee = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Shopee",
            Location = new Point(22, 225),
            Size = new Size(160, 44),
            FillColor = Color.FromArgb(238, 77, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F),
            BorderRadius = 8,
            Cursor = Cursors.Hand
        };
        btnShopee.Click += (_, _) =>
        {
            UseAiTitle = chkUseAi.Checked;
            DelayMinMinutes = (int)numMin.Value;
            DelayMaxMinutes = (int)numMax.Value;
            ShuffleVideos = chkShuffle.Checked;
            SelectedPlatform = "Shopee";
            DialogResult = DialogResult.OK;
            Close();
        };

        var btnFb = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Facebook",
            Location = new Point(200, 225),
            Size = new Size(160, 44),
            FillColor = Color.FromArgb(24, 119, 242),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F),
            BorderRadius = 8,
            Cursor = Cursors.Hand
        };
        btnFb.Click += (_, _) =>
        {
            UseAiTitle = chkUseAi.Checked;
            DelayMinMinutes = (int)numMin.Value;
            DelayMaxMinutes = (int)numMax.Value;
            ShuffleVideos = chkShuffle.Checked;
            SelectedPlatform = "Facebook";
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange([lblTitle, lblDesc, chkUseAi, btnConfigAi, lblDelay, numMin, lblTo, numMax, chkShuffle, btnShopee, btnFb]);
    }
}
