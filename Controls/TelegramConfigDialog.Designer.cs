namespace ShopeeVideoUploader.Controls;

partial class TelegramConfigDialog
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.lblBotToken = new System.Windows.Forms.Label();
        this.txtBotToken = new Guna.UI2.WinForms.Guna2TextBox();
        this.lblChatId = new System.Windows.Forms.Label();
        this.txtChatId = new Guna.UI2.WinForms.Guna2TextBox();
        this.btnDetectChatId = new Guna.UI2.WinForms.Guna2Button();
        this.gbNotifications = new Guna.UI2.WinForms.Guna2GroupBox();
        this.chkEnableNotifications = new Guna.UI2.WinForms.Guna2CheckBox();
        this.chkNotifyOnStart = new Guna.UI2.WinForms.Guna2CheckBox();
        this.chkNotifyOnSuccess = new Guna.UI2.WinForms.Guna2CheckBox();
        this.chkNotifyOnError = new Guna.UI2.WinForms.Guna2CheckBox();
        this.chkSendScreenshotOnError = new Guna.UI2.WinForms.Guna2CheckBox();
        this.gbRemote = new Guna.UI2.WinForms.Guna2GroupBox();
        this.chkEnableRemoteControl = new Guna.UI2.WinForms.Guna2CheckBox();
        this.lblDefaultPlatform = new System.Windows.Forms.Label();
        this.cboDefaultPlatform = new Guna.UI2.WinForms.Guna2ComboBox();
        this.chkUseAiTitle = new Guna.UI2.WinForms.Guna2CheckBox();
        this.lblDelay = new System.Windows.Forms.Label();
        this.numDelayMin = new Guna.UI2.WinForms.Guna2NumericUpDown();
        this.lblDelayTo = new System.Windows.Forms.Label();
        this.numDelayMax = new Guna.UI2.WinForms.Guna2NumericUpDown();
        this.lblDelayUnit = new System.Windows.Forms.Label();
        this.btnTest = new Guna.UI2.WinForms.Guna2Button();
        this.btnSave = new Guna.UI2.WinForms.Guna2Button();
        this.btnCancel = new Guna.UI2.WinForms.Guna2Button();
        this.gbNotifications.SuspendLayout();
        this.gbRemote.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numDelayMin)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numDelayMax)).BeginInit();
        this.SuspendLayout();
        // 
        // lblBotToken
        // 
        this.lblBotToken.AutoSize = true;
        this.lblBotToken.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.lblBotToken.Location = new System.Drawing.Point(20, 16);
        this.lblBotToken.Name = "lblBotToken";
        this.lblBotToken.Size = new System.Drawing.Size(160, 15);
        this.lblBotToken.TabIndex = 0;
        this.lblBotToken.Text = "Bot Token (từ @BotFather):";
        // 
        // txtBotToken
        // 
        this.txtBotToken.BorderRadius = 6;
        this.txtBotToken.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtBotToken.Location = new System.Drawing.Point(20, 36);
        this.txtBotToken.Name = "txtBotToken";
        this.txtBotToken.PasswordChar = '•';
        this.txtBotToken.PlaceholderText = "123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ...";
        this.txtBotToken.Size = new System.Drawing.Size(460, 36);
        this.txtBotToken.TabIndex = 1;
        // 
        // lblChatId
        // 
        this.lblChatId.AutoSize = true;
        this.lblChatId.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.lblChatId.Location = new System.Drawing.Point(20, 82);
        this.lblChatId.Name = "lblChatId";
        this.lblChatId.Size = new System.Drawing.Size(225, 15);
        this.lblChatId.TabIndex = 2;
        this.lblChatId.Text = "Admin Chat ID (các ID cách nhau dấu phẩy):";
        // 
        // txtChatId
        // 
        this.txtChatId.BorderRadius = 6;
        this.txtChatId.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtChatId.Location = new System.Drawing.Point(20, 102);
        this.txtChatId.Name = "txtChatId";
        this.txtChatId.PlaceholderText = "Ví dụ: 123456789, 987654321";
        this.txtChatId.Size = new System.Drawing.Size(320, 36);
        this.txtChatId.TabIndex = 3;
        // 
        // btnDetectChatId
        // 
        this.btnDetectChatId.BorderRadius = 6;
        this.btnDetectChatId.FillColor = System.Drawing.Color.FromArgb(10, 151, 205);
        this.btnDetectChatId.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.btnDetectChatId.ForeColor = System.Drawing.Color.White;
        this.btnDetectChatId.Location = new System.Drawing.Point(350, 102);
        this.btnDetectChatId.Name = "btnDetectChatId";
        this.btnDetectChatId.Size = new System.Drawing.Size(130, 36);
        this.btnDetectChatId.TabIndex = 4;
        this.btnDetectChatId.Text = "🔍 Tìm Chat ID";
        this.btnDetectChatId.Click += new System.EventHandler(this.btnDetectChatId_Click);
        // 
        // gbNotifications
        // 
        this.gbNotifications.BorderRadius = 8;
        this.gbNotifications.Controls.Add(this.chkEnableNotifications);
        this.gbNotifications.Controls.Add(this.chkNotifyOnStart);
        this.gbNotifications.Controls.Add(this.chkNotifyOnSuccess);
        this.gbNotifications.Controls.Add(this.chkNotifyOnError);
        this.gbNotifications.Controls.Add(this.chkSendScreenshotOnError);
        this.gbNotifications.CustomBorderColor = System.Drawing.Color.FromArgb(240, 243, 248);
        this.gbNotifications.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
        this.gbNotifications.ForeColor = System.Drawing.Color.FromArgb(64, 70, 88);
        this.gbNotifications.Location = new System.Drawing.Point(20, 150);
        this.gbNotifications.Name = "gbNotifications";
        this.gbNotifications.Size = new System.Drawing.Size(460, 155);
        this.gbNotifications.TabIndex = 5;
        this.gbNotifications.Text = "Cài đặt Thông báo Realtime";
        // 
        // chkEnableNotifications
        // 
        this.chkEnableNotifications.AutoSize = true;
        this.chkEnableNotifications.Checked = true;
        this.chkEnableNotifications.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkEnableNotifications.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.chkEnableNotifications.Location = new System.Drawing.Point(15, 45);
        this.chkEnableNotifications.Name = "chkEnableNotifications";
        this.chkEnableNotifications.Size = new System.Drawing.Size(175, 19);
        this.chkEnableNotifications.TabIndex = 0;
        this.chkEnableNotifications.Text = "Bật hệ thống thông báo Bot";
        // 
        // chkNotifyOnStart
        // 
        this.chkNotifyOnStart.AutoSize = true;
        this.chkNotifyOnStart.Checked = true;
        this.chkNotifyOnStart.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkNotifyOnStart.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.chkNotifyOnStart.Location = new System.Drawing.Point(15, 72);
        this.chkNotifyOnStart.Name = "chkNotifyOnStart";
        this.chkNotifyOnStart.Size = new System.Drawing.Size(182, 19);
        this.chkNotifyOnStart.TabIndex = 1;
        this.chkNotifyOnStart.Text = "Báo khi bắt đầu phiên upload";
        // 
        // chkNotifyOnSuccess
        // 
        this.chkNotifyOnSuccess.AutoSize = true;
        this.chkNotifyOnSuccess.Checked = true;
        this.chkNotifyOnSuccess.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkNotifyOnSuccess.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.chkNotifyOnSuccess.Location = new System.Drawing.Point(15, 98);
        this.chkNotifyOnSuccess.Name = "chkNotifyOnSuccess";
        this.chkNotifyOnSuccess.Size = new System.Drawing.Size(211, 19);
        this.chkNotifyOnSuccess.TabIndex = 2;
        this.chkNotifyOnSuccess.Text = "Báo khi mỗi video up THÀNH CÔNG";
        // 
        // chkNotifyOnError
        // 
        this.chkNotifyOnError.AutoSize = true;
        this.chkNotifyOnError.Checked = true;
        this.chkNotifyOnError.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkNotifyOnError.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.chkNotifyOnError.Location = new System.Drawing.Point(15, 124);
        this.chkNotifyOnError.Name = "chkNotifyOnError";
        this.chkNotifyOnError.Size = new System.Drawing.Size(178, 19);
        this.chkNotifyOnError.TabIndex = 3;
        this.chkNotifyOnError.Text = "Báo khi video bị LỖI/Thất bại";
        // 
        // chkSendScreenshotOnError
        // 
        this.chkSendScreenshotOnError.AutoSize = true;
        this.chkSendScreenshotOnError.Checked = true;
        this.chkSendScreenshotOnError.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkSendScreenshotOnError.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.chkSendScreenshotOnError.Location = new System.Drawing.Point(240, 124);
        this.chkSendScreenshotOnError.Name = "chkSendScreenshotOnError";
        this.chkSendScreenshotOnError.Size = new System.Drawing.Size(200, 19);
        this.chkSendScreenshotOnError.TabIndex = 4;
        this.chkSendScreenshotOnError.Text = "📸 Tự động chụp ảnh gửi kèm lỗi";
        // 
        // gbRemote
        // 
        this.gbRemote.BorderRadius = 8;
        this.gbRemote.Controls.Add(this.chkEnableRemoteControl);
        this.gbRemote.Controls.Add(this.lblDefaultPlatform);
        this.gbRemote.Controls.Add(this.cboDefaultPlatform);
        this.gbRemote.Controls.Add(this.chkUseAiTitle);
        this.gbRemote.Controls.Add(this.lblDelay);
        this.gbRemote.Controls.Add(this.numDelayMin);
        this.gbRemote.Controls.Add(this.lblDelayTo);
        this.gbRemote.Controls.Add(this.numDelayMax);
        this.gbRemote.Controls.Add(this.lblDelayUnit);
        this.gbRemote.CustomBorderColor = System.Drawing.Color.FromArgb(240, 243, 248);
        this.gbRemote.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
        this.gbRemote.ForeColor = System.Drawing.Color.FromArgb(64, 70, 88);
        this.gbRemote.Location = new System.Drawing.Point(20, 315);
        this.gbRemote.Name = "gbRemote";
        this.gbRemote.Size = new System.Drawing.Size(460, 150);
        this.gbRemote.TabIndex = 6;
        this.gbRemote.Text = "Điều khiển từ xa qua Telegram";
        // 
        // chkEnableRemoteControl
        // 
        this.chkEnableRemoteControl.AutoSize = true;
        this.chkEnableRemoteControl.Checked = true;
        this.chkEnableRemoteControl.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkEnableRemoteControl.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.chkEnableRemoteControl.Location = new System.Drawing.Point(15, 45);
        this.chkEnableRemoteControl.Name = "chkEnableRemoteControl";
        this.chkEnableRemoteControl.Size = new System.Drawing.Size(275, 19);
        this.chkEnableRemoteControl.TabIndex = 0;
        this.chkEnableRemoteControl.Text = "Cho phép nhận lệnh điều khiển từ xa (Start/Stop)";
        // 
        // lblDefaultPlatform
        // 
        this.lblDefaultPlatform.AutoSize = true;
        this.lblDefaultPlatform.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.lblDefaultPlatform.Location = new System.Drawing.Point(15, 78);
        this.lblDefaultPlatform.Name = "lblDefaultPlatform";
        this.lblDefaultPlatform.Size = new System.Drawing.Size(117, 15);
        this.lblDefaultPlatform.TabIndex = 1;
        this.lblDefaultPlatform.Text = "Nền tảng mặc định:";
        // 
        // cboDefaultPlatform
        // 
        this.cboDefaultPlatform.BackColor = System.Drawing.Color.Transparent;
        this.cboDefaultPlatform.BorderRadius = 6;
        this.cboDefaultPlatform.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
        this.cboDefaultPlatform.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboDefaultPlatform.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.cboDefaultPlatform.Items.AddRange(new object[] {
            "Shopee",
            "Facebook"});
        this.cboDefaultPlatform.Location = new System.Drawing.Point(135, 72);
        this.cboDefaultPlatform.Name = "cboDefaultPlatform";
        this.cboDefaultPlatform.Size = new System.Drawing.Size(130, 30);
        this.cboDefaultPlatform.TabIndex = 2;
        // 
        // chkUseAiTitle
        // 
        this.chkUseAiTitle.AutoSize = true;
        this.chkUseAiTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.chkUseAiTitle.Location = new System.Drawing.Point(285, 78);
        this.chkUseAiTitle.Name = "chkUseAiTitle";
        this.chkUseAiTitle.Size = new System.Drawing.Size(142, 19);
        this.chkUseAiTitle.TabIndex = 3;
        this.chkUseAiTitle.Text = "Bật AI sinh tiêu đề SEO";
        // 
        // lblDelay
        // 
        this.lblDelay.AutoSize = true;
        this.lblDelay.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.lblDelay.Location = new System.Drawing.Point(15, 114);
        this.lblDelay.Name = "lblDelay";
        this.lblDelay.Size = new System.Drawing.Size(136, 15);
        this.lblDelay.TabIndex = 4;
        this.lblDelay.Text = "Nghỉ giữa các video:";
        // 
        // numDelayMin
        // 
        this.numDelayMin.BorderRadius = 4;
        this.numDelayMin.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.numDelayMin.Location = new System.Drawing.Point(155, 110);
        this.numDelayMin.Name = "numDelayMin";
        this.numDelayMin.Size = new System.Drawing.Size(55, 26);
        this.numDelayMin.TabIndex = 5;
        // 
        // lblDelayTo
        // 
        this.lblDelayTo.AutoSize = true;
        this.lblDelayTo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.lblDelayTo.Location = new System.Drawing.Point(215, 114);
        this.lblDelayTo.Name = "lblDelayTo";
        this.lblDelayTo.Size = new System.Drawing.Size(26, 15);
        this.lblDelayTo.TabIndex = 6;
        this.lblDelayTo.Text = "đến";
        // 
        // numDelayMax
        // 
        this.numDelayMax.BorderRadius = 4;
        this.numDelayMax.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.numDelayMax.Location = new System.Drawing.Point(245, 110);
        this.numDelayMax.Name = "numDelayMax";
        this.numDelayMax.Size = new System.Drawing.Size(55, 26);
        this.numDelayMax.TabIndex = 7;
        // 
        // lblDelayUnit
        // 
        this.lblDelayUnit.AutoSize = true;
        this.lblDelayUnit.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.lblDelayUnit.Location = new System.Drawing.Point(305, 114);
        this.lblDelayUnit.Name = "lblDelayUnit";
        this.lblDelayUnit.Size = new System.Drawing.Size(31, 15);
        this.lblDelayUnit.TabIndex = 8;
        this.lblDelayUnit.Text = "phút";
        // 
        // btnTest
        // 
        this.btnTest.BorderRadius = 6;
        this.btnTest.FillColor = System.Drawing.Color.FromArgb(244, 248, 252);
        this.btnTest.BorderColor = System.Drawing.Color.FromArgb(190, 198, 211);
        this.btnTest.BorderThickness = 1;
        this.btnTest.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.btnTest.ForeColor = System.Drawing.Color.FromArgb(31, 31, 44);
        this.btnTest.Location = new System.Drawing.Point(20, 480);
        this.btnTest.Name = "btnTest";
        this.btnTest.Size = new System.Drawing.Size(150, 36);
        this.btnTest.TabIndex = 7;
        this.btnTest.Text = "⚡ Kiểm tra kết nối";
        this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
        // 
        // btnSave
        // 
        this.btnSave.BorderRadius = 6;
        this.btnSave.FillColor = System.Drawing.Color.FromArgb(96, 82, 218);
        this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Location = new System.Drawing.Point(290, 480);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(95, 36);
        this.btnSave.TabIndex = 8;
        this.btnSave.Text = "Lưu cấu hình";
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
        // 
        // btnCancel
        // 
        this.btnCancel.BorderRadius = 6;
        this.btnCancel.FillColor = System.Drawing.Color.FromArgb(235, 237, 242);
        this.btnCancel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(70, 70, 85);
        this.btnCancel.Location = new System.Drawing.Point(395, 480);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(85, 36);
        this.btnCancel.TabIndex = 9;
        this.btnCancel.Text = "Hủy";
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        // 
        // TelegramConfigDialog
        // 
        this.BackColor = System.Drawing.Color.White;
        this.ClientSize = new System.Drawing.Size(500, 535);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.btnTest);
        this.Controls.Add(this.gbRemote);
        this.Controls.Add(this.gbNotifications);
        this.Controls.Add(this.btnDetectChatId);
        this.Controls.Add(this.txtChatId);
        this.Controls.Add(this.lblChatId);
        this.Controls.Add(this.txtBotToken);
        this.Controls.Add(this.lblBotToken);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "TelegramConfigDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Cấu hình Telegram Bot (Thông báo & Điều khiển)";
        this.gbNotifications.ResumeLayout(false);
        this.gbNotifications.PerformLayout();
        this.gbRemote.ResumeLayout(false);
        this.gbRemote.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numDelayMin)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numDelayMax)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.Label lblBotToken;
    private Guna.UI2.WinForms.Guna2TextBox txtBotToken;
    private System.Windows.Forms.Label lblChatId;
    private Guna.UI2.WinForms.Guna2TextBox txtChatId;
    private Guna.UI2.WinForms.Guna2Button btnDetectChatId;
    private Guna.UI2.WinForms.Guna2GroupBox gbNotifications;
    private Guna.UI2.WinForms.Guna2CheckBox chkEnableNotifications;
    private Guna.UI2.WinForms.Guna2CheckBox chkNotifyOnStart;
    private Guna.UI2.WinForms.Guna2CheckBox chkNotifyOnSuccess;
    private Guna.UI2.WinForms.Guna2CheckBox chkNotifyOnError;
    private Guna.UI2.WinForms.Guna2CheckBox chkSendScreenshotOnError;
    private Guna.UI2.WinForms.Guna2GroupBox gbRemote;
    private Guna.UI2.WinForms.Guna2CheckBox chkEnableRemoteControl;
    private System.Windows.Forms.Label lblDefaultPlatform;
    private Guna.UI2.WinForms.Guna2ComboBox cboDefaultPlatform;
    private Guna.UI2.WinForms.Guna2CheckBox chkUseAiTitle;
    private System.Windows.Forms.Label lblDelay;
    private Guna.UI2.WinForms.Guna2NumericUpDown numDelayMin;
    private System.Windows.Forms.Label lblDelayTo;
    private Guna.UI2.WinForms.Guna2NumericUpDown numDelayMax;
    private System.Windows.Forms.Label lblDelayUnit;
    private Guna.UI2.WinForms.Guna2Button btnTest;
    private Guna.UI2.WinForms.Guna2Button btnSave;
    private Guna.UI2.WinForms.Guna2Button btnCancel;
}
