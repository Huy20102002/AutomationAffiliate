namespace ShopeeVideoUploader.Controls;

partial class AiConfigDialog
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.lblEndpoint = new System.Windows.Forms.Label();
        this.txtEndpoint = new Guna.UI2.WinForms.Guna2TextBox();
        this.lblApiKey = new System.Windows.Forms.Label();
        this.txtApiKey = new Guna.UI2.WinForms.Guna2TextBox();
        this.lblModel = new System.Windows.Forms.Label();
        this.txtModel = new Guna.UI2.WinForms.Guna2TextBox();
        this.lblPrompt = new System.Windows.Forms.Label();
        this.txtPrompt = new Guna.UI2.WinForms.Guna2TextBox();
        this.lblBackupApis = new System.Windows.Forms.Label();
        this.btnAddBackupApi = new Guna.UI2.WinForms.Guna2Button();
        this.flpBackupApis = new System.Windows.Forms.FlowLayoutPanel();
        this.btnSave = new Guna.UI2.WinForms.Guna2Button();
        this.btnCancel = new Guna.UI2.WinForms.Guna2Button();
        this.btnTest = new Guna.UI2.WinForms.Guna2Button();
        this.SuspendLayout();
        // 
        // lblEndpoint
        // 
        this.lblEndpoint.AutoSize = true;
        this.lblEndpoint.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblEndpoint.Location = new System.Drawing.Point(20, 20);
        this.lblEndpoint.Name = "lblEndpoint";
        this.lblEndpoint.Size = new System.Drawing.Size(76, 15);
        this.lblEndpoint.TabIndex = 0;
        this.lblEndpoint.Text = "API Endpoint";
        // 
        // txtEndpoint
        // 
        this.txtEndpoint.Location = new System.Drawing.Point(20, 40);
        this.txtEndpoint.Name = "txtEndpoint";
        this.txtEndpoint.Size = new System.Drawing.Size(360, 36);
        this.txtEndpoint.TabIndex = 1;
        // 
        // lblApiKey
        // 
        this.lblApiKey.AutoSize = true;
        this.lblApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblApiKey.Location = new System.Drawing.Point(20, 90);
        this.lblApiKey.Name = "lblApiKey";
        this.lblApiKey.Size = new System.Drawing.Size(47, 15);
        this.lblApiKey.TabIndex = 2;
        this.lblApiKey.Text = "API Key";
        // 
        // txtApiKey
        // 
        this.txtApiKey.Location = new System.Drawing.Point(20, 110);
        this.txtApiKey.Name = "txtApiKey";
        this.txtApiKey.Size = new System.Drawing.Size(360, 36);
        this.txtApiKey.TabIndex = 3;
        // 
        // lblModel
        // 
        this.lblModel.AutoSize = true;
        this.lblModel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblModel.Location = new System.Drawing.Point(20, 160);
        this.lblModel.Name = "lblModel";
        this.lblModel.Size = new System.Drawing.Size(41, 15);
        this.lblModel.TabIndex = 4;
        this.lblModel.Text = "Model";
        // 
        // txtModel
        // 
        this.txtModel.Location = new System.Drawing.Point(20, 180);
        this.txtModel.Name = "txtModel";
        this.txtModel.Size = new System.Drawing.Size(360, 36);
        this.txtModel.TabIndex = 5;
        // 
        // 
        // lblBackupApis
        // 
        this.lblBackupApis.AutoSize = true;
        this.lblBackupApis.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblBackupApis.Location = new System.Drawing.Point(20, 230);
        this.lblBackupApis.Name = "lblBackupApis";
        this.lblBackupApis.Size = new System.Drawing.Size(85, 15);
        this.lblBackupApis.TabIndex = 6;
        this.lblBackupApis.Text = "API Dự phòng";
        // 
        // btnAddBackupApi
        // 
        this.btnAddBackupApi.FillColor = System.Drawing.Color.FromArgb(96, 82, 218);
        this.btnAddBackupApi.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.btnAddBackupApi.ForeColor = System.Drawing.Color.White;
        this.btnAddBackupApi.Location = new System.Drawing.Point(120, 220);
        this.btnAddBackupApi.Name = "btnAddBackupApi";
        this.btnAddBackupApi.Size = new System.Drawing.Size(32, 32);
        this.btnAddBackupApi.TabIndex = 7;
        this.btnAddBackupApi.Text = "+";
        this.btnAddBackupApi.Click += new System.EventHandler(this.btnAddBackupApi_Click);
        // 
        // flpBackupApis
        // 
        this.flpBackupApis.AutoScroll = true;
        this.flpBackupApis.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.flpBackupApis.Location = new System.Drawing.Point(20, 260);
        this.flpBackupApis.Name = "flpBackupApis";
        this.flpBackupApis.Size = new System.Drawing.Size(370, 150);
        this.flpBackupApis.TabIndex = 8;
        this.flpBackupApis.WrapContents = false;
        // 
        // lblPrompt
        // 
        this.lblPrompt.AutoSize = true;
        this.lblPrompt.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblPrompt.Location = new System.Drawing.Point(20, 420);
        this.lblPrompt.Name = "lblPrompt";
        this.lblPrompt.Size = new System.Drawing.Size(100, 15);
        this.lblPrompt.TabIndex = 10;
        this.lblPrompt.Text = "Prompt Template";
        // 
        // txtPrompt
        // 
        this.txtPrompt.Location = new System.Drawing.Point(20, 440);
        this.txtPrompt.Multiline = true;
        this.txtPrompt.Name = "txtPrompt";
        this.txtPrompt.Size = new System.Drawing.Size(360, 100);
        this.txtPrompt.TabIndex = 11;
        // 
        // btnSave
        // 
        this.btnSave.FillColor = System.Drawing.Color.FromArgb(96, 82, 218);
        this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Location = new System.Drawing.Point(200, 560);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(80, 36);
        this.btnSave.TabIndex = 12;
        this.btnSave.Text = "Lưu";
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
        // 
        // btnCancel
        // 
        this.btnCancel.FillColor = System.Drawing.Color.FromArgb(235, 237, 242);
        this.btnCancel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(70, 70, 85);
        this.btnCancel.Location = new System.Drawing.Point(300, 560);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(80, 36);
        this.btnCancel.TabIndex = 13;
        this.btnCancel.Text = "Hủy";
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        // 
        // btnTest
        // 
        this.btnTest.FillColor = System.Drawing.Color.FromArgb(10, 151, 205);
        this.btnTest.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
        this.btnTest.ForeColor = System.Drawing.Color.White;
        this.btnTest.Location = new System.Drawing.Point(20, 560);
        this.btnTest.Name = "btnTest";
        this.btnTest.Size = new System.Drawing.Size(120, 36);
        this.btnTest.TabIndex = 14;
        this.btnTest.Text = "Kiểm tra API";
        this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
        // 
        // AiConfigDialog
        // 
        this.BackColor = System.Drawing.Color.White;
        this.ClientSize = new System.Drawing.Size(400, 620);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnTest);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.txtPrompt);
        this.Controls.Add(this.lblPrompt);
        this.Controls.Add(this.flpBackupApis);
        this.Controls.Add(this.btnAddBackupApi);
        this.Controls.Add(this.lblBackupApis);
        this.Controls.Add(this.txtModel);
        this.Controls.Add(this.lblModel);
        this.Controls.Add(this.txtApiKey);
        this.Controls.Add(this.lblApiKey);
        this.Controls.Add(this.txtEndpoint);
        this.Controls.Add(this.lblEndpoint);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "AiConfigDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Cấu hình AI Tiêu đề";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.Label lblEndpoint;
    private Guna.UI2.WinForms.Guna2TextBox txtEndpoint;
    private System.Windows.Forms.Label lblApiKey;
    private Guna.UI2.WinForms.Guna2TextBox txtApiKey;
    private System.Windows.Forms.Label lblModel;
    private Guna.UI2.WinForms.Guna2TextBox txtModel;
    private System.Windows.Forms.Label lblBackupApis;
    private Guna.UI2.WinForms.Guna2Button btnAddBackupApi;
    private System.Windows.Forms.FlowLayoutPanel flpBackupApis;
    private System.Windows.Forms.Label lblPrompt;
    private Guna.UI2.WinForms.Guna2TextBox txtPrompt;
    private Guna.UI2.WinForms.Guna2Button btnSave;
    private Guna.UI2.WinForms.Guna2Button btnCancel;
    private Guna.UI2.WinForms.Guna2Button btnTest;
}
