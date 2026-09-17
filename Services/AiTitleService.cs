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

        try
        {
            var candidates = new List<(string Endpoint, string ApiKey, string Model, string Desc)>();

            // 1. Primary API
            if (!string.IsNullOrWhiteSpace(config.ApiEndpoint) && !string.IsNullOrWhiteSpace(config.ApiKey))
            {
                candidates.Add((config.ApiEndpoint.Trim(), config.ApiKey.Trim(), config.Model?.Trim() ?? "gpt-3.5-turbo", "API chính"));
            }

            // 2. Backup APIs list
            if (config.BackupApis != null)
            {
                for (int i = 0; i < config.BackupApis.Count; i++)
                {
                    var b = config.BackupApis[i];
                    if (!string.IsNullOrWhiteSpace(b.Endpoint) && !string.IsNullOrWhiteSpace(b.ApiKey))
                    {
                        string m = !string.IsNullOrWhiteSpace(b.Model) ? b.Model.Trim() : (config.Model?.Trim() ?? "gpt-3.5-turbo");
                        candidates.Add((b.Endpoint.Trim(), b.ApiKey.Trim(), m, $"API dự phòng #{i + 1}"));
                    }
                }
            }

            // 3. Backward compatibility with single BackupApiEndpoint if BackupApis was empty
            if (candidates.Count <= 1 && !string.IsNullOrWhiteSpace(config.BackupApiEndpoint) && !string.IsNullOrWhiteSpace(config.BackupApiKey))
            {
                if (!candidates.Any(c => c.Endpoint.Equals(config.BackupApiEndpoint.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    string m = !string.IsNullOrWhiteSpace(config.BackupModel) ? config.BackupModel.Trim() : (config.Model?.Trim() ?? "gpt-3.5-turbo");
                    candidates.Add((config.BackupApiEndpoint.Trim(), config.BackupApiKey.Trim(), m, "API dự phòng (phụ)"));
                }
            }

            if (candidates.Count == 0)
            {
                Helpers.Logger.Warn($"[AI] Chưa cấu hình bất kỳ API nào, tự động giữ nguyên tiêu đề cũ: '{originalTitle}'.");
                return originalTitle;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                var current = candidates[i];
                try
                {
                    var result = await CallApiAsync(current.Endpoint, current.ApiKey, current.Model, config.PromptTemplate, originalTitle, attempt);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        if (i > 0)
                        {
                            Helpers.Logger.Info($"[AI] Đã tự động chuyển đổi và tạo tiêu đề thành công bằng {current.Desc} ({current.Endpoint}).");
                        }
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Helpers.Logger.Warn($"[AI] {current.Desc} ({current.Endpoint}) gặp lỗi: {ex.Message}");
                    if (i < candidates.Count - 1)
                    {
                        Helpers.Logger.Info($"[AI] Đang chuyển tiếp sang {candidates[i + 1].Desc} ({candidates[i + 1].Endpoint})...");
                    }
                }
            }

            Helpers.Logger.Warn($"[AI] Tất cả {candidates.Count} API đều thất bại khi tạo tiêu đề. Tự động giữ nguyên tiêu đề cũ: '{originalTitle}'.");
            return originalTitle;
        }
        catch (Exception ex)
        {
            Helpers.Logger.Warn($"[AI] Lỗi xử lý AI ({ex.Message}), tự động giữ nguyên tiêu đề cũ: '{originalTitle}'.");
            return originalTitle;
        }
    }

    public async Task<string> TestApiAsync(AiConfig config, string originalTitle)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey) || string.IsNullOrWhiteSpace(config.ApiEndpoint))
            throw new Exception("API Endpoint hoặc API Key bị trống.");

        return await CallApiAsync(config.ApiEndpoint, config.ApiKey, config.Model, config.PromptTemplate, originalTitle, 1);
    }

    public async Task<string> TestCustomApiAsync(string endpoint, string apiKey, string model, string promptTemplate, string originalTitle)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(endpoint))
            throw new Exception("API Endpoint hoặc API Key bị trống.");

        return await CallApiAsync(endpoint, apiKey, model, promptTemplate, originalTitle, 1);
    }

    private async Task<string> CallApiAsync(string endpoint, string apiKey, string model, string promptTemplate, string originalTitle, int attempt)
    {
        // Tự động chuẩn hóa Google Gemini endpoint nếu người dùng dán link cũ hoặc thiếu /v1beta/openai
        if (endpoint.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
            if (model.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
            {
                model = model.Substring("models/".Length);
            }
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gemini-1.5-flash";
            }
        }
        // Tự động chuẩn hóa Groq model & API key nếu bị dán thừa ký tự
        else if (endpoint.Contains("api.groq.com", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = "https://api.groq.com/openai/v1/chat/completions";
            if (apiKey.Contains("gsk_"))
            {
                int gskIndex = apiKey.IndexOf("gsk_");
                apiKey = apiKey.Substring(gskIndex);
            }
            if (model.Equals("llama-3.3-70b-versatile", StringComparison.OrdinalIgnoreCase))
            {
                model = "llama-3.1-8b-instant";
            }
        }

        int maxRetries = 2;
        for (int i = 1; i <= maxRetries; i++)
        {
            try
            {
                var prompt = promptTemplate
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
                    model = model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7
                };

                var requestContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Headers.Add("User-Agent", "ShopeeVideoUploader/1.0");
                request.Content = requestContent;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMsg = $"HTTP {statusCode} {response.ReasonPhrase}\nChi tiết: {errorContent}";

                    // Lỗi 404 (sai model), 401/403 (sai key), 402 (hết tiền), 429 (hết quota ngày) -> Không retry vô ích, chuyển ngay sang API dự phòng!
                    if (statusCode == 404 || statusCode == 401 || statusCode == 402 || statusCode == 403 || statusCode == 429)
                    {
                        throw new NonRetryableApiException(errorMsg);
                    }

                    throw new Exception(errorMsg);
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
            catch (NonRetryableApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (i < maxRetries)
                {
                    Helpers.Logger.Warn($"[AI] Lỗi lần {i} tại {endpoint}, thử lại sau 2 giây... Lỗi: {ex.Message}");
                    await Task.Delay(2000);
                }
                else
                {
                    throw;
                }
            }
        }
        throw new Exception("Đã vượt quá số lần thử tối đa.");
    }
}

public sealed class NonRetryableApiException(string message) : Exception(message) { }
