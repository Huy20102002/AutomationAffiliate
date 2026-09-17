using System;
using System.Drawing;
using System.Windows.Forms;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

public partial class AiConfigDialog : Form
{
    public AiConfig Config { get; private set; }

    public AiConfigDialog(AiConfig config)
    {
        InitializeComponent();
        Config = config;

        txtEndpoint.Text = config.ApiEndpoint;
        txtApiKey.Text = config.ApiKey;
        txtModel.Text = config.Model;
        txtPrompt.Text = config.PromptTemplate;

        if (config.BackupApis == null) config.BackupApis = new System.Collections.Generic.List<BackupApiConfig>();
        if (config.BackupApis.Count == 0 && !string.IsNullOrWhiteSpace(config.BackupApiEndpoint))
        {
            config.BackupApis.Add(new BackupApiConfig { Endpoint = config.BackupApiEndpoint, ApiKey = config.BackupApiKey, Model = config.BackupModel });
        }

        foreach (var backup in config.BackupApis)
        {
            AddBackupApiRow(backup.Endpoint, backup.ApiKey, backup.Model);
        }
    }

    private void btnAddBackupApi_Click(object sender, EventArgs e)
    {
        AddBackupApiRow();
    }

    private void AddBackupApiRow(string endpoint = "", string apiKey = "", string model = "")
    {
        var rowPanel = new Panel { Width = 340, Height = 76, Margin = new Padding(0, 0, 0, 10) };
        
        var txtEnd = new Guna.UI2.WinForms.Guna2TextBox { 
            Location = new Point(0, 0), Width = 270, Height = 36, Text = endpoint, PlaceholderText = "Endpoint" 
        };
        var btnTest = new Guna.UI2.WinForms.Guna2Button { 
            Location = new Point(280, 0), Width = 60, Height = 36, Text = "Test", ForeColor = Color.White, FillColor = Color.FromArgb(10, 151, 205), Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        var txtKey = new Guna.UI2.WinForms.Guna2TextBox { 
            Location = new Point(0, 40), Width = 160, Height = 36, Text = apiKey, PlaceholderText = "API Key" 
        };
        var txtModelInput = new Guna.UI2.WinForms.Guna2TextBox { 
            Location = new Point(170, 40), Width = 100, Height = 36, Text = model, PlaceholderText = "Model" 
        };
        var btnSwap = new Guna.UI2.WinForms.Guna2Button { 
            Location = new Point(275, 40), Width = 30, Height = 36, Text = "↑", ForeColor = Color.White, FillColor = Color.FromArgb(46, 204, 113), Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        var btnDel = new Guna.UI2.WinForms.Guna2Button { 
            Location = new Point(310, 40), Width = 30, Height = 36, Text = "X", ForeColor = Color.White, FillColor = Color.LightCoral, Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        
        btnDel.Click += (s, ev) => {
            flpBackupApis.Controls.Remove(rowPanel);
            rowPanel.Dispose();
        };

        btnSwap.Click += (s, ev) => {
            string oldPriEnd = txtEndpoint.Text;
            string oldPriKey = txtApiKey.Text;
            string oldPriMod = txtModel.Text;

            txtEndpoint.Text = txtEnd.Text;
            txtApiKey.Text = txtKey.Text;
            txtModel.Text = string.IsNullOrWhiteSpace(txtModelInput.Text) ? txtModel.Text : txtModelInput.Text;

            txtEnd.Text = oldPriEnd;
            txtKey.Text = oldPriKey;
            txtModelInput.Text = oldPriMod;
        };

        btnTest.Click += async (s, ev) => {
            if (string.IsNullOrWhiteSpace(txtEnd.Text) || string.IsNullOrWhiteSpace(txtKey.Text))
            {
                MessageBox.Show("Vui lòng điền API Endpoint và API Key của cấu hình này", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            btnTest.Enabled = false;
            btnTest.Text = "...";
            try
            {
                var testCfg = new AiConfig
                {
                    ApiEndpoint = txtEnd.Text.Trim(),
                    ApiKey = txtKey.Text.Trim(),
                    Model = string.IsNullOrWhiteSpace(txtModelInput.Text) ? txtModel.Text.Trim() : txtModelInput.Text.Trim(),
                    PromptTemplate = txtPrompt.Text.Trim()
                };
                
                var aiService = new Services.AiTitleService();
                string dummyTitle = "Tai nghe bluetooth không dây F9 pro";
                var result = await aiService.TestApiAsync(testCfg, dummyTitle);
                
                MessageBox.Show($"Call API thành công!\n\nTiêu đề gốc: {dummyTitle}\nTiêu đề AI: {result}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gọi API: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        flpBackupApis.Controls.Add(rowPanel);
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        Config.ApiEndpoint = txtEndpoint.Text.Trim();
        Config.ApiKey = txtApiKey.Text.Trim();
        Config.Model = txtModel.Text.Trim();
        Config.PromptTemplate = txtPrompt.Text.Trim();
        
        Config.BackupApis.Clear();
        foreach (Control c in flpBackupApis.Controls)
        {
            if (c is Panel p && p.Controls.Count >= 4)
            {
                var tEnd = p.Controls[0] as Guna.UI2.WinForms.Guna2TextBox;
                var tKey = p.Controls[2] as Guna.UI2.WinForms.Guna2TextBox;
                var tModel = p.Controls[3] as Guna.UI2.WinForms.Guna2TextBox;
                if (tEnd != null && tKey != null && !string.IsNullOrWhiteSpace(tEnd.Text))
                {
                    Config.BackupApis.Add(new BackupApiConfig { Endpoint = tEnd.Text.Trim(), ApiKey = tKey.Text.Trim(), Model = tModel?.Text.Trim() ?? "" });
                }
            }
        }

        if (Config.BackupApis.Count > 0)
        {
            Config.BackupApiEndpoint = Config.BackupApis[0].Endpoint;
            Config.BackupApiKey = Config.BackupApis[0].ApiKey;
            Config.BackupModel = Config.BackupApis[0].Model;
        }
        else
        {
            Config.BackupApiEndpoint = string.Empty;
            Config.BackupApiKey = string.Empty;
            Config.BackupModel = string.Empty;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private async void btnTest_Click(object sender, EventArgs e)
    {
        var configForTest = new AiConfig
        {
            ApiEndpoint = txtEndpoint.Text.Trim(),
            ApiKey = txtApiKey.Text.Trim(),
            Model = txtModel.Text.Trim(),
            PromptTemplate = txtPrompt.Text.Trim(),
            BackupApis = new System.Collections.Generic.List<BackupApiConfig>()
        };

        foreach (Control c in flpBackupApis.Controls)
        {
            if (c is Panel p && p.Controls.Count >= 4)
            {
                var tEnd = p.Controls[0] as Guna.UI2.WinForms.Guna2TextBox;
                var tKey = p.Controls[2] as Guna.UI2.WinForms.Guna2TextBox;
                var tModel = p.Controls[3] as Guna.UI2.WinForms.Guna2TextBox;
                if (tEnd != null && tKey != null && !string.IsNullOrWhiteSpace(tEnd.Text))
                {
                    configForTest.BackupApis.Add(new BackupApiConfig { Endpoint = tEnd.Text.Trim(), ApiKey = tKey.Text.Trim(), Model = tModel?.Text.Trim() ?? "" });
                }
            }
        }

        if (string.IsNullOrWhiteSpace(configForTest.ApiKey) || string.IsNullOrWhiteSpace(configForTest.ApiEndpoint))
        {
            MessageBox.Show("Vui lòng điền API Endpoint và API Key", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnTest.Enabled = false;
        btnTest.Text = "Đang thử...";
        try
        {
            var aiService = new Services.AiTitleService();
            string dummyTitle = "Tai nghe bluetooth không dây F9 pro";
            var result = await aiService.TestApiAsync(configForTest, dummyTitle);
            
            MessageBox.Show($"Call API thành công!\n\nTiêu đề gốc: {dummyTitle}\nTiêu đề AI: {result}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi gọi API: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTest.Enabled = true;
            btnTest.Text = "Kiểm tra API";
        }
    }
}
