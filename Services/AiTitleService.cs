using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

public class AiTitleService
{
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<string> GenerateTitleAsync(AiConfig config, string originalTitle, int attempt = 1)
    {
        if (string.IsNullOrWhiteSpace(originalTitle)) return string.Empty;
        if (string.IsNullOrWhiteSpace(config.ApiKey) || string.IsNullOrWhiteSpace(config.ApiEndpoint))
            return originalTitle; // Skip if not configured

        int maxRetries = 3;
        for (int i = 1; i <= maxRetries; i++)
        {
            try
            {
                var prompt = config.PromptTemplate
                    .Replace("{0}", originalTitle)
                    .Replace("{Title}", originalTitle)
                    .Replace("{title}", originalTitle);

                if (i > 1 || attempt > 1)
                {
                    // Thêm chuỗi rác để tránh bộ lọc "duplicate request" của server AI
                    prompt += $"\n\n[Ignore this: Retry {i}-{Guid.NewGuid().ToString().Substring(0, 4)}]";
                }

                var payload = new
                {
                    model = config.Model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7
                };

                var requestContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, config.ApiEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
                request.Headers.Add("User-Agent", "ShopeeVideoUploader/1.0");
                request.Content = requestContent;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\nChi tiết: {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                    {
                        var generatedTitle = content.GetString()?.Trim() ?? string.Empty;
                        // Remove quotes if the AI wraps the title in quotes
                        if (generatedTitle.StartsWith("\"") && generatedTitle.EndsWith("\"") && generatedTitle.Length >= 2)
                        {
                            generatedTitle = generatedTitle.Substring(1, generatedTitle.Length - 2);
                        }
                        
                        if (!string.IsNullOrWhiteSpace(generatedTitle))
                            return generatedTitle;
                        
                        throw new Exception("AI trả về nội dung rỗng.");
                    }
                    throw new Exception("Định dạng JSON không chứa message.content.");
                }
                
                throw new Exception("API không trả về bất kỳ choices nào.");
            }
            catch (Exception ex)
            {
                if (i < maxRetries)
                {
                    Helpers.Logger.Warn($"[AI] Lỗi lần {i}, thử lại sau 3 giây... Lỗi: {ex.Message}");
                    await Task.Delay(3000);
                }
                else
                {
                    Helpers.Logger.Error($"Lỗi khi tạo tiêu đề AI cho '{originalTitle}' sau {maxRetries} lần", ex);
                    return originalTitle;
                }
            }
        }
        return originalTitle;
    }
}
