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
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        Config.ApiEndpoint = txtEndpoint.Text.Trim();
        Config.ApiKey = txtApiKey.Text.Trim();
        Config.Model = txtModel.Text.Trim();
        Config.PromptTemplate = txtPrompt.Text.Trim();
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
        var config = new AiConfig
        {
            ApiEndpoint = txtEndpoint.Text.Trim(),
            ApiKey = txtApiKey.Text.Trim(),
            Model = txtModel.Text.Trim(),
            PromptTemplate = txtPrompt.Text.Trim()
        };

        if (string.IsNullOrWhiteSpace(config.ApiKey) || string.IsNullOrWhiteSpace(config.ApiEndpoint))
        {
            MessageBox.Show("Vui lòng điền API Endpoint và API Key", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnTest.Enabled = false;
        btnTest.Text = "Đang thử...";
        try
        {
            var aiService = new Services.AiTitleService();
            // Call the service with a dummy title
            string dummyTitle = "Tai nghe bluetooth không dây F9 pro";
            var result = await aiService.GenerateTitleAsync(config, dummyTitle);
            
            if (result == dummyTitle)
            {
                MessageBox.Show("API trả về tiêu đề không đổi (có thể do cấu hình model sai hoặc API từ chối). Vui lòng check log.", "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show($"Call API thành công!\n\nTiêu đề gốc: {dummyTitle}\nTiêu đề AI: {result}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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
