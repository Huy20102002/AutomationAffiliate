namespace ShopeeVideoUploader.Models;

public class AiConfig
{
    public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
    public string PromptTemplate { get; set; } = "Dưới đây là một tiêu đề sản phẩm. Hãy viết lại một tiêu đề chuẩn SEO cho nền tảng Facebook, độ dài khoảng 10-15 từ, thật hấp dẫn và có kèm 2-3 hashtag liên quan ở cuối. Chỉ trả về tiêu đề mới, không giải thích gì thêm.\n\nTiêu đề gốc: {0}";
}
