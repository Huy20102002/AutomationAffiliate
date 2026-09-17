namespace ShopeeVideoUploader.Models;

public class AiConfig
{
    public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
    public string PromptTemplate { get; set; } = "Đóng vai chuyên gia sáng tạo nội dung viral TikTok và Shopee Video. Dựa vào thông tin dưới đây, hãy viết 1 tiêu đề (caption) thật thu hút, giật gân để kích thích mua hàng.\\nYêu cầu:\\n1. Ngắn gọn tối đa 15-20 từ.\\n2. Dùng 1-2 icon (emoji) sinh động.\\n3. Kèm 3 hashtag liên quan ở cuối.\\n4. CHỈ trả về đúng nội dung tiêu đề, không giải thích, không ngoặc kép.\\n\\nThông tin gốc: {0}";
    public string BackupApiEndpoint { get; set; } = string.Empty;
    public string BackupApiKey { get; set; } = string.Empty;
    public string BackupModel { get; set; } = string.Empty;
    public string ActiveProvider { get; set; } = "Primary";
    public List<BackupApiConfig> BackupApis { get; set; } = new List<BackupApiConfig>();
}

public class BackupApiConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
