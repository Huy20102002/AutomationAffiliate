using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

/// <summary>
/// Dịch vụ kết nối và xử lý Telegram Bot API (Thông báo, Chụp ảnh, Điều khiển từ xa).
/// </summary>
public class TelegramBotService : IDisposable
{
    private readonly HttpClient _httpClient;
    private TelegramConfig _config;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private long _lastUpdateId = 0;
    private bool _isDisposed;

    // Các sự kiện ủy quyền cho MainForm xử lý
    public Func<Task<string>>? OnGetStatusInfo { get; set; }
    public Func<Task<string>>? OnGetStatsInfo { get; set; }
    public Func<Task<string>>? OnGetDevicesInfo { get; set; }
    public Func<List<FolderItem>>? OnGetFolders { get; set; }
    public Func<int, string, bool, int, int, bool, Task<bool>>? OnRequestRun { get; set; } // folderId, platform, useAi, delayMin, delayMax, onlyFailed -> returns true if started
    public Action? OnRequestStop { get; set; }
    public Func<Task<byte[]?>>? OnRequestScreenshot { get; set; }
    public Func<Task<(bool isConnected, string deviceName, string details)>>? OnCheckDeviceStatus { get; set; }
    public Func<Task<List<(string id, string name, string state)>>>? OnGetDeviceListDetailed { get; set; }
    public Func<string?, Task<(bool success, string message)>>? OnConnectDevice { get; set; }
    public Func<List<string>>? OnGetWorkflowFiles { get; set; }
    public Func<string>? OnGetCurrentWorkflowFile { get; set; }
    public Func<string, (bool success, string message)>? OnSelectWorkflowFile { get; set; }
    public Func<int, (bool success, string folderName, int videoCount)>? OnSelectFolder { get; set; }
    public Func<(int folderId, string folderName, int videoCount)>? OnGetSelectedFolderInfo { get; set; }
    public Action<TelegramConfig>? OnConfigChanged { get; set; }

    public TelegramBotService(TelegramConfig config)
    {
        _config = config;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public void UpdateConfig(TelegramConfig newConfig)
    {
        _config = newConfig;
        if (!string.IsNullOrWhiteSpace(_config.BotToken) && _config.EnableRemoteControl)
        {
            StartPolling();
        }
        else
        {
            StopPolling();
        }
    }

    public void Start()
    {
        if (!string.IsNullOrWhiteSpace(_config.BotToken) && _config.EnableRemoteControl)
        {
            StartPolling();
        }
    }

    private void StartPolling()
    {
        StopPolling();
        _pollingCts = new CancellationTokenSource();
        _pollingTask = Task.Run(() => PollingLoopAsync(_pollingCts.Token));
        Logger.Info("[Telegram] Đã khởi động luồng lắng nghe tin nhắn từ bot.");
    }

    public void StopPolling()
    {
        if (_pollingCts != null)
        {
            _pollingCts.Cancel();
            _pollingCts.Dispose();
            _pollingCts = null;
        }
        _pollingTask = null;
    }

    #region Polling & Update Processing

    private async Task PollingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.BotToken))
                {
                    await Task.Delay(5000, ct);
                    continue;
                }

                var url = $"https://api.telegram.org/bot{_config.BotToken}/getUpdates?offset={_lastUpdateId + 1}&timeout=20";
                using var response = await _httpClient.GetAsync(url, ct);
                if (response.IsSuccessStatusCode)
                {
                    var jsonStr = await response.Content.ReadAsStringAsync(ct);
                    var root = JObject.Parse(jsonStr);
                    if (root["ok"]?.Value<bool>() == true && root["result"] is JArray updates)
                    {
                        foreach (var update in updates)
                        {
                            var updateId = update["update_id"]?.Value<long>() ?? 0;
                            if (updateId > _lastUpdateId) _lastUpdateId = updateId;

                            try
                            {
                                await ProcessUpdateAsync(update, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"[Telegram] Lỗi xử lý update {updateId}: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    await Task.Delay(3000, ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Telegram] Lỗi polling: {ex.Message}");
                await Task.Delay(5000, ct);
            }
        }
    }

    private async Task ProcessUpdateAsync(JToken update, CancellationToken ct)
    {
        // 1. Xử lý tin nhắn văn bản thông thường
        if (update["message"] is JToken message)
        {
            var chatId = message["chat"]?["id"]?.Value<long>() ?? 0;
            var text = message["text"]?.ToString()?.Trim() ?? string.Empty;
            var fromName = message["from"]?["first_name"]?.ToString() ?? "User";

            if (chatId == 0) return;

            // Kiểm tra phân quyền Admin
            if (!_config.IsAdmin(chatId))
            {
                var unauthorizedMsg = $"⛔ <b>Quyền truy cập bị từ chối</b>\nID của bạn: <code>{chatId}</code> chưa được thêm vào Whitelist cấu hình của Tool.";
                await SendMessageAsync(chatId, unauthorizedMsg, "HTML");
                Logger.Warn($"[Telegram] Từ chối người dùng lạ chat_id={chatId} ({fromName}): {text}");
                return;
            }

            Logger.Info($"[Telegram] Nhận lệnh từ Admin ({chatId}): {text}");

            if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase) || 
                text.StartsWith("/help", StringComparison.OrdinalIgnoreCase) || 
                text.Equals("menu", StringComparison.OrdinalIgnoreCase))
            {
                await SendMainMenuAsync(chatId);
            }
            else if (text.StartsWith("/status", StringComparison.OrdinalIgnoreCase))
            {
                await HandleStatusAsync(chatId);
            }
            else if (text.StartsWith("/stats", StringComparison.OrdinalIgnoreCase))
            {
                await HandleStatsAsync(chatId);
            }
            else if (text.StartsWith("/devices", StringComparison.OrdinalIgnoreCase))
            {
                await HandleDevicesListMenuAsync(chatId);
            }
            else if (text.StartsWith("/screen", StringComparison.OrdinalIgnoreCase) || text.StartsWith("/screenshot", StringComparison.OrdinalIgnoreCase))
            {
                await HandleScreenshotAsync(chatId);
            }
            else if (text.StartsWith("/stop", StringComparison.OrdinalIgnoreCase))
            {
                HandleStop(chatId);
            }
            else if (text.StartsWith("/run", StringComparison.OrdinalIgnoreCase))
            {
                await HandleRunNowAsync(chatId);
            }
            else if (text.StartsWith("/onlylink", StringComparison.OrdinalIgnoreCase) || text.StartsWith("/link", StringComparison.OrdinalIgnoreCase))
            {
                await HandleToggleOnlyLinkAsync(chatId);
            }
            else
            {
                await SendMainMenuAsync(chatId);
            }
        }
        // 2. Xử lý Callback Query từ các nút bấm (Inline Keyboard)
        else if (update["callback_query"] is JToken callbackQuery)
        {
            var callbackId = callbackQuery["id"]?.ToString() ?? string.Empty;
            var fromId = callbackQuery["from"]?["id"]?.Value<long>() ?? 0;
            var chatId = callbackQuery["message"]?["chat"]?["id"]?.Value<long>() ?? fromId;
            var data = callbackQuery["data"]?.ToString() ?? string.Empty;

            if (chatId == 0) return;

            if (!_config.IsAdmin(chatId))
            {
                await AnswerCallbackQueryAsync(callbackId, "⛔ Bạn không có quyền thực hiện thao tác này!");
                return;
            }

            await AnswerCallbackQueryAsync(callbackId);

            Logger.Info($"[Telegram] Callback query từ Admin ({chatId}): {data}");

            if (data == "cmd_menu")
            {
                await SendMainMenuAsync(chatId);
            }
            else if (data == "cmd_run_now" || data == "cmd_run_all")
            {
                await HandleRunNowAsync(chatId);
            }
            else if (data == "cmd_pick_workflow")
            {
                await SendWorkflowPickerMenuAsync(chatId);
            }
            else if (data.StartsWith("cmd_set_wf_"))
            {
                var wfName = data.Replace("cmd_set_wf_", "");
                await HandleSelectWorkflowAsync(chatId, wfName);
            }
            else if (data == "cmd_pick_folder" || data == "cmd_folders")
            {
                await SendFolderPickerMenuAsync(chatId);
            }
            else if (data.StartsWith("cmd_set_fld_"))
            {
                if (int.TryParse(data.Replace("cmd_set_fld_", ""), out var fldId))
                {
                    await HandleSelectFolderAsync(chatId, fldId);
                }
            }
            else if (data == "cmd_toggle_platform")
            {
                await HandleTogglePlatformAsync(chatId);
            }
            else if (data == "cmd_toggle_ai")
            {
                await HandleToggleAiAsync(chatId);
            }
            else if (data == "cmd_toggle_only_link")
            {
                await HandleToggleOnlyLinkAsync(chatId);
            }
            else if (data == "cmd_pick_delay")
            {
                await SendDelayPickerMenuAsync(chatId);
            }
            else if (data.StartsWith("cmd_set_delay_"))
            {
                var parts = data.Replace("cmd_set_delay_", "").Split('_');
                if (parts.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
                {
                    await HandleSelectDelayAsync(chatId, min, max);
                }
            }
            else if (data == "cmd_run_failed")
            {
                await HandleRunFailedAsync(chatId);
            }
            else if (data == "cmd_stop")
            {
                HandleStop(chatId);
            }
            else if (data == "cmd_status")
            {
                await HandleStatusAsync(chatId);
            }
            else if (data == "cmd_stats")
            {
                await HandleStatsAsync(chatId);
            }
            else if (data == "cmd_devices" || data == "cmd_devices_list")
            {
                await HandleDevicesListMenuAsync(chatId);
            }
            else if (data == "cmd_scan_connect")
            {
                await HandleScanAndConnectAsync(chatId);
            }
            else if (data.StartsWith("cmd_conn_dev_"))
            {
                var devId = data.Replace("cmd_conn_dev_", "");
                await HandleConnectSpecificDeviceAsync(chatId, devId);
            }
            else if (data == "cmd_screenshot")
            {
                await HandleScreenshotAsync(chatId);
            }
        }
    }

    #endregion

    #region Command Handlers

    public async Task SendMainMenuAsync(long chatId)
    {
        var devStatus = "Chưa xác định";
        var isConnected = false;
        if (OnCheckDeviceStatus != null)
        {
            var (conn, name, _) = await OnCheckDeviceStatus();
            isConnected = conn;
            devStatus = isConnected ? $"🟢 <b>{name}</b>" : "❌ <b>Chưa kết nối</b>";
        }

        var currentWf = OnGetCurrentWorkflowFile?.Invoke() ?? _config.SelectedWorkflowFile;
        if (string.IsNullOrEmpty(currentWf)) currentWf = "Shopee_Upload.json";

        var folderInfo = OnGetSelectedFolderInfo?.Invoke() ?? (_config.SelectedFolderId, "Tất cả chiến dịch", 0);
        var folderName = folderInfo.folderId == -1 ? "Tất cả chiến dịch" : folderInfo.folderName;
        var videoCountText = folderInfo.videoCount > 0 ? $" ({folderInfo.videoCount} video)" : "";

        var delayText = _config.DelayMinMinutes == 0 && _config.DelayMaxMinutes == 0 
            ? "0 phút (Không nghỉ)" 
            : $"{_config.DelayMinMinutes} - {_config.DelayMaxMinutes} phút";

        var msg = "🤖 <b>BẢNG ĐIỀU KHIỂN SHOPEE UPLOADER</b>\n" +
                  "━━━━━━━━━━━━━━━━━━━━━━━\n" +
                  $"📱 Thiết bị: {devStatus}\n" +
                  $"📄 Quy trình: <b>{currentWf}</b>\n" +
                  $"📁 Chiến dịch: <b>{folderName}{videoCountText}</b>\n" +
                  $"🛒 Nền tảng: <b>{_config.DefaultPlatform}</b>\n" +
                  $"🤖 AI SEO: <b>{(_config.UseAiTitle ? "BẬT" : "TẮT")}</b>\n" +
                  $"🔗 Chỉ up có link: <b>{(_config.OnlyRunWithLink ? "BẬT" : "TẮT")}</b>\n" +
                  $"⏱️ Nghỉ giữa video: <b>{delayText}</b>\n" +
                  "━━━━━━━━━━━━━━━━━━━━━━━\n" +
                  "👇 <i>Chọn cấu hình hoặc bấm Bắt đầu chạy:</i>";

        var rows = new List<object[]>();

        if (!isConnected)
        {
            rows.Add(new[]
            {
                new { text = "🔌 Quét & Tự động kết nối", callback_data = "cmd_scan_connect" },
                new { text = "📱 Danh sách thiết bị", callback_data = "cmd_devices_list" }
            });
        }

        rows.Add(new[]
        {
            new { text = "🚀 BẮT ĐẦU CHẠY NGAY", callback_data = "cmd_run_now" },
            new { text = "⏹️ Dừng lại", callback_data = "cmd_stop" }
        });

        rows.Add(new[]
        {
            new { text = $"📄 Quy trình: {currentWf}", callback_data = "cmd_pick_workflow" },
            new { text = $"📁 Chiến dịch: {folderName}", callback_data = "cmd_pick_folder" }
        });

        rows.Add(new[]
        {
            new { text = $"🛒 Platform: {_config.DefaultPlatform} ⇄", callback_data = "cmd_toggle_platform" },
            new { text = $"🤖 AI SEO: {(_config.UseAiTitle ? "BẬT" : "TẮT")}", callback_data = "cmd_toggle_ai" }
        });

        rows.Add(new[]
        {
            new { text = $"🔗 Chỉ video có link: {(_config.OnlyRunWithLink ? "BẬT" : "TẮT")}", callback_data = "cmd_toggle_only_link" },
            new { text = $"⏱️ Delay: {delayText}", callback_data = "cmd_pick_delay" }
        });

        rows.Add(new[]
        {
            new { text = "🔄 Chạy lại video Lỗi", callback_data = "cmd_run_failed" },
            new { text = "📸 Chụp màn hình", callback_data = "cmd_screenshot" }
        });

        rows.Add(new[]
        {
            new { text = "📊 Trạng thái", callback_data = "cmd_status" },
            new { text = "📈 Thống kê", callback_data = "cmd_stats" }
        });

        var keyboard = new { inline_keyboard = rows };
        await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
    }

    #region 1. Workflow Profile Picker

    private async Task SendWorkflowPickerMenuAsync(long chatId)
    {
        var currentWf = OnGetCurrentWorkflowFile?.Invoke() ?? _config.SelectedWorkflowFile;
        var files = OnGetWorkflowFiles?.Invoke() ?? [];

        if (files.Count == 0)
        {
            await SendMessageAsync(chatId, "⚠️ Không tìm thấy file quy trình (.json) nào trong thư mục Workflows.", "HTML");
            return;
        }

        var buttons = new List<object[]>();
        foreach (var file in files)
        {
            var isSelected = string.Equals(file, currentWf, StringComparison.OrdinalIgnoreCase);
            var text = isSelected ? $"✅ {file} (Đang chọn)" : $"📄 {file}";
            buttons.Add(new[]
            {
                new { text, callback_data = $"cmd_set_wf_{file}" }
            });
        }
        buttons.Add(new[]
        {
            new { text = "🔙 Quay lại Menu", callback_data = "cmd_menu" }
        });

        var keyboard = new { inline_keyboard = buttons };
        var msg = "📄 <b>CHỌN FILE QUY TRÌNH (.JSON) ĐỂ NẠP:</b>\n" +
                  $"Quy trình hiện tại: <b>{currentWf}</b>\n\n" +
                  "Bấm vào quy trình bạn muốn chọn:";
        await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
    }

    private async Task HandleSelectWorkflowAsync(long chatId, string fileName)
    {
        if (OnSelectWorkflowFile != null)
        {
            var (success, message) = OnSelectWorkflowFile(fileName);
            if (success)
            {
                _config.SelectedWorkflowFile = fileName;
                OnConfigChanged?.Invoke(_config);
                var msg = $"✅ <b>ĐÃ NẠP THÀNH CÔNG QUY TRÌNH:</b>\n<code>{fileName}</code>\n\n{message}";
                var keyboard = new
                {
                    inline_keyboard = new[]
                    {
                        new[]
                        {
                            new { text = "🚀 Bắt đầu chạy ngay", callback_data = "cmd_run_now" },
                            new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                        }
                    }
                };
                await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
            }
            else
            {
                await SendMessageAsync(chatId, $"❌ <b>Lỗi nạp quy trình:</b> {message}", "HTML");
            }
        }
    }

    #endregion

    #region 2. Folder / Campaign Picker

    private async Task SendFolderPickerMenuAsync(long chatId)
    {
        var folders = OnGetFolders?.Invoke() ?? [];
        var currentInfo = OnGetSelectedFolderInfo?.Invoke() ?? (_config.SelectedFolderId, "Tất cả", 0);

        var buttons = new List<object[]>();

        // Option 1: Tất cả chiến dịch (-1)
        var allText = currentInfo.folderId == -1 ? "✅ 🌐 Tất cả chiến dịch (Đang chọn)" : "🌐 Tất cả chiến dịch";
        buttons.Add(new[]
        {
            new { text = allText, callback_data = "cmd_set_fld_-1" }
        });

        // Option 2: Chưa phân loại (0)
        var uncatText = currentInfo.folderId == 0 ? "✅ 📁 [Chưa phân loại] (Đang chọn)" : "📁 [Chưa phân loại]";
        buttons.Add(new[]
        {
            new { text = uncatText, callback_data = "cmd_set_fld_0" }
        });

        // Specific Folders
        foreach (var f in folders)
        {
            var isSelected = currentInfo.folderId == f.Id;
            var text = isSelected ? $"✅ 📁 {f.Name} (Đang chọn)" : $"📁 {f.Name}";
            buttons.Add(new[]
            {
                new { text, callback_data = $"cmd_set_fld_{f.Id}" }
            });
        }

        buttons.Add(new[]
        {
            new { text = "🔙 Quay lại Menu", callback_data = "cmd_menu" }
        });

        var keyboard = new { inline_keyboard = buttons };
        var msg = "📁 <b>CHỌN CHIẾN DỊCH / THƯ MỤC ĐỂ UPLOAD:</b>\n" +
                  $"Chiến dịch hiện tại: <b>{currentInfo.folderName}</b>\n\n" +
                  "Bấm vào chiến dịch bạn muốn chọn:";
        await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
    }

    private async Task HandleSelectFolderAsync(long chatId, int folderId)
    {
        if (OnSelectFolder != null)
        {
            var (success, folderName, videoCount) = OnSelectFolder(folderId);
            if (success)
            {
                _config.SelectedFolderId = folderId;
                OnConfigChanged?.Invoke(_config);
                var msg = $"✅ <b>ĐÃ CHỌN CHIẾN DỊCH:</b>\n📁 <b>{folderName}</b> ({videoCount} video)";
                var keyboard = new
                {
                    inline_keyboard = new[]
                    {
                        new[]
                        {
                            new { text = "🚀 Bắt đầu chạy ngay", callback_data = "cmd_run_now" },
                            new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                        }
                    }
                };
                await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
            }
        }
    }

    #endregion

    #region 3. Platform & AI & Delay Toggle

    private async Task HandleTogglePlatformAsync(long chatId)
    {
        _config.DefaultPlatform = _config.DefaultPlatform == "Shopee" ? "Facebook" : "Shopee";
        OnConfigChanged?.Invoke(_config);
        await SendMainMenuAsync(chatId);
    }

    private async Task HandleToggleAiAsync(long chatId)
    {
        _config.UseAiTitle = !_config.UseAiTitle;
        OnConfigChanged?.Invoke(_config);
        await SendMainMenuAsync(chatId);
    }

    private async Task HandleToggleOnlyLinkAsync(long chatId)
    {
        _config.OnlyRunWithLink = !_config.OnlyRunWithLink;
        OnConfigChanged?.Invoke(_config);
        var status = _config.OnlyRunWithLink ? "BẬT (Chỉ up video có link)" : "TẮT (Up toàn bộ video)";
        await SendMessageAsync(chatId, $"🔗 Chế độ lọc link: <b>{status}</b>", "HTML");
        await SendMainMenuAsync(chatId);
    }

    private async Task SendDelayPickerMenuAsync(long chatId)
    {
        var currentText = _config.DelayMinMinutes == 0 && _config.DelayMaxMinutes == 0 
            ? "0 phút (Không nghỉ)" 
            : $"{_config.DelayMinMinutes} - {_config.DelayMaxMinutes} phút";

        var buttons = new List<object[]>
        {
            new[] { new { text = "⚡ 0 phút (Chạy liên tục không nghỉ)", callback_data = "cmd_set_delay_0_0" } },
            new[] { new { text = "☕ 1 - 3 phút (Khuyên dùng chống spam)", callback_data = "cmd_set_delay_1_3" } },
            new[] { new { text = "⏳ 3 - 5 phút (An toàn cao)", callback_data = "cmd_set_delay_3_5" } },
            new[] { new { text = "⏱️ 5 - 10 phút (Khoảng cách dài)", callback_data = "cmd_set_delay_5_10" } },
            new[] { new { text = "🕐 10 - 15 phút", callback_data = "cmd_set_delay_10_15" } },
            new[] { new { text = "🔙 Quay lại Menu", callback_data = "cmd_menu" } }
        };

        var keyboard = new { inline_keyboard = buttons };
        var msg = "⏱️ <b>CHỌN THỜI GIAN NGHỈ GIỮA CÁC VIDEO:</b>\n" +
                  $"Hiện tại: <b>{currentText}</b>\n\n" +
                  "Chọn mức nghỉ ngẫu nhiên bạn muốn:";
        await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
    }

    private async Task HandleSelectDelayAsync(long chatId, int min, int max)
    {
        _config.DelayMinMinutes = min;
        _config.DelayMaxMinutes = max;
        OnConfigChanged?.Invoke(_config);
        var delayText = min == 0 && max == 0 ? "0 phút (Không nghỉ)" : $"{min} - {max} phút";
        var msg = $"✅ <b>ĐÃ THIẾT LẬP THỜI GIAN NGHỈ:</b>\n⏱️ <b>{delayText}</b>";
        var keyboard = new
        {
            inline_keyboard = new[]
            {
                new[]
                {
                    new { text = "🚀 Bắt đầu chạy ngay", callback_data = "cmd_run_now" },
                    new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                }
            }
        };
        await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
    }

    #endregion

    private async Task<bool> EnsureDeviceConnectedAsync(long chatId)
    {
        if (OnCheckDeviceStatus != null)
        {
            var (isConnected, _, _) = await OnCheckDeviceStatus();
            if (!isConnected)
            {
                var msg = "⚠️ <b>KHÔNG THỂ CHẠY: Chưa kết nối thiết bị Android / iOS!</b>\n\n" +
                          "Vui lòng kết nối thiết bị bằng cách bấm nút bên dưới:";
                var keyboard = new
                {
                    inline_keyboard = new[]
                    {
                        new[]
                        {
                            new { text = "🔌 Quét & Kết nối tự động", callback_data = "cmd_scan_connect" },
                            new { text = "📱 Chọn thiết bị", callback_data = "cmd_devices_list" }
                        },
                        new[]
                        {
                            new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                        }
                    }
                };
                await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
                return false;
            }
        }
        return true;
    }

    private async Task HandleRunNowAsync(long chatId)
    {
        if (!await EnsureDeviceConnectedAsync(chatId)) return;

        if (OnRequestRun != null)
        {
            var folderId = _config.SelectedFolderId;
            var platform = _config.DefaultPlatform;
            var useAi = _config.UseAiTitle;
            var delayMin = _config.DelayMinMinutes;
            var delayMax = _config.DelayMaxMinutes;

            var started = await OnRequestRun(folderId, platform, useAi, delayMin, delayMax, false);
            if (started)
            {
                var folderInfo = OnGetSelectedFolderInfo?.Invoke() ?? (folderId, "Tất cả", 0);
                var folderName = folderId == -1 ? "Tất cả chiến dịch" : folderInfo.folderName;

                var msg = "🚀 <b>ĐÃ BẮT ĐẦU CHẠY UPLOAD VIDEO!</b>\n\n" +
                          $"📁 Chiến dịch: <b>{folderName}</b>\n" +
                          $"🛒 Nền tảng: <b>{platform}</b>\n" +
                          $"🤖 AI SEO: <b>{(useAi ? "Bật" : "Tắt")}</b>\n" +
                          $"🔗 Chỉ up có link: <b>{(_config.OnlyRunWithLink ? "Bật" : "Tắt")}</b>\n" +
                          $"⏱️ Delay: <b>{delayMin} - {delayMax} phút</b>";
                await SendMessageAsync(chatId, msg, "HTML");
            }
            else
            {
                await SendMessageAsync(chatId, "⚠️ Không thể bắt đầu (Có thể tool đang chạy hoặc danh sách video rỗng).", "HTML");
            }
        }
    }

    private async Task HandleRunFailedAsync(long chatId)
    {
        if (!await EnsureDeviceConnectedAsync(chatId)) return;

        if (OnRequestRun != null)
        {
            var folderId = _config.SelectedFolderId;
            var platform = _config.DefaultPlatform;
            var useAi = _config.UseAiTitle;
            var delayMin = _config.DelayMinMinutes;
            var delayMax = _config.DelayMaxMinutes;

            var started = await OnRequestRun(folderId, platform, useAi, delayMin, delayMax, true);
            if (started)
            {
                await SendMessageAsync(chatId, "🔄 <b>ĐÃ BẮT ĐẦU CHẠY LẠI CÁC VIDEO BỊ LỖI</b>", "HTML");
            }
            else
            {
                await SendMessageAsync(chatId, "⚠️ Không tìm thấy video bị lỗi hoặc tool đang chạy.", "HTML");
            }
        }
    }

    private void HandleStop(long chatId)
    {
        OnRequestStop?.Invoke();
        _ = SendMessageAsync(chatId, "⏹️ <b>ĐÃ GỬI LỆNH DỪNG QUY TRÌNH!</b>", "HTML");
    }

    private async Task HandleStatusAsync(long chatId)
    {
        bool isConnected = false;
        string devName = "Chưa kết nối";
        if (OnCheckDeviceStatus != null)
        {
            var (conn, name, _) = await OnCheckDeviceStatus();
            isConnected = conn;
            devName = name;
        }

        var status = OnGetStatusInfo != null ? await OnGetStatusInfo() : "Tool đang rảnh rỗi (Idle).";

        if (!isConnected)
        {
            var msg = "📊 <b>TRẠNG THÁI HIỆN TẠI:</b>\n" +
                      $"⚠️ Thiết bị: ❌ <b>Chưa kết nối thiết bị nào!</b>\n" +
                      $"📌 Trạng thái tool: {status}\n\n" +
                      "👉 <i>Hãy cắm cáp USB và bấm nút bên dưới để tự động kết nối:</i>";

            var keyboard = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "🔌 Quét & Tự động kết nối", callback_data = "cmd_scan_connect" },
                        new { text = "📱 Chọn thiết bị", callback_data = "cmd_devices_list" }
                    },
                    new[]
                    {
                        new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                    }
                }
            };
            await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
        }
        else
        {
            var currentWf = OnGetCurrentWorkflowFile?.Invoke() ?? _config.SelectedWorkflowFile;
            var folderInfo = OnGetSelectedFolderInfo?.Invoke() ?? (_config.SelectedFolderId, "Tất cả", 0);
            var folderName = folderInfo.folderId == -1 ? "Tất cả chiến dịch" : folderInfo.folderName;

            var msg = "📊 <b>TRẠNG THÁI HIỆN TẠI:</b>\n" +
                      $"📱 Thiết bị: 🟢 <b>{devName}</b> (Online)\n" +
                      $"📄 Quy trình: <b>{currentWf}</b>\n" +
                      $"📁 Chiến dịch: <b>{folderName}</b>\n" +
                      $"🛒 Nền tảng: <b>{_config.DefaultPlatform}</b>\n" +
                      $"📌 Tiến độ: {status}";
            await SendMessageAsync(chatId, msg, "HTML");
        }
    }

    private async Task HandleStatsAsync(long chatId)
    {
        if (OnGetStatsInfo != null)
        {
            var stats = await OnGetStatsInfo();
            await SendMessageAsync(chatId, $"📈 <b>THỐNG KÊ DANH SÁCH:</b>\n{stats}", "HTML");
        }
    }

    private async Task HandleScanAndConnectAsync(long chatId)
    {
        if (OnConnectDevice == null)
        {
            await SendMessageAsync(chatId, "⚠️ Tính năng kết nối chưa được kích hoạt.", "HTML");
            return;
        }

        await SendMessageAsync(chatId, "🔄 Đang quét và kết nối thiết bị qua USB / ADB...", "HTML");
        var (success, message) = await OnConnectDevice(null);
        if (success)
        {
            var msg = $"✅ <b>KẾT NỐI THIẾT BỊ THÀNH CÔNG!</b>\n\n" +
                      $"📱 <b>{message}</b>\n\n" +
                      "Bạn có thể bắt đầu chạy quy trình ngay:";
            var keyboard = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "🚀 Bắt đầu chạy ngay", callback_data = "cmd_run_now" },
                        new { text = "📂 Chọn chiến dịch", callback_data = "cmd_pick_folder" }
                    },
                    new[]
                    {
                        new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                    }
                }
            };
            await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
        }
        else
        {
            var msg = $"❌ <b>KHÔNG TÌM THẤY THIẾT BỊ NÀO!</b>\n\n" +
                      $"Chi tiết: <i>{message}</i>\n\n" +
                      "👉 <b>Hướng dẫn kết nối:</b>\n" +
                      "1. Cắm dây cáp USB kết nối điện thoại với máy tính.\n" +
                      "2. Bật <b>Gỡ lỗi USB (USB Debugging)</b> trên điện thoại Android.\n" +
                      "3. Bấm <b>Cho phép</b> (Always allow) trên màn hình điện thoại nếu có popup hiện lên.\n" +
                      "4. Với iPhone: Đảm bảo đã bật WebDriverAgent (WDA).\n\n" +
                      "Sau khi kiểm tra, bấm <b>[ 🔄 Thử quét lại ]</b>:";
            var keyboard = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "🔄 Thử quét lại", callback_data = "cmd_scan_connect" },
                        new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                    }
                }
            };
            await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
        }
    }

    private async Task HandleDevicesListMenuAsync(long chatId)
    {
        if (OnGetDeviceListDetailed == null)
        {
            if (OnGetDevicesInfo != null)
            {
                var dev = await OnGetDevicesInfo();
                await SendMessageAsync(chatId, $"📱 <b>THIẾT BỊ:</b>\n{dev}", "HTML");
            }
            return;
        }

        var devices = await OnGetDeviceListDetailed();
        if (devices.Count == 0)
        {
            var msg = "❌ <b>KHÔNG TÌM THẤY THIẾT BỊ NÀO ĐANG KẾT NỐI!</b>\n\n" +
                      "Vui lòng cắm cáp USB và bật USB Debugging trên điện thoại.";
            var keyboard = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "🔄 Thử quét lại", callback_data = "cmd_scan_connect" },
                        new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                    }
                }
            };
            await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
            return;
        }

        var buttons = new List<object[]>();
        foreach (var d in devices)
        {
            buttons.Add(new[]
            {
                new { text = $"🔌 Kết nối: {d.name} [{d.state}]", callback_data = $"cmd_conn_dev_{d.id}" }
            });
        }
        buttons.Add(new[]
        {
            new { text = "🔄 Quét lại", callback_data = "cmd_devices_list" },
            new { text = "🔙 Quay lại", callback_data = "cmd_menu" }
        });

        var keyboardObj = new { inline_keyboard = buttons };
        await SendMessageWithKeyboardAsync(chatId, "📱 <b>DANH SÁCH THIẾT BỊ TÌM THẤY:</b>\nBấm vào thiết bị bạn muốn kết nối:", keyboardObj, "HTML");
    }

    private async Task HandleConnectSpecificDeviceAsync(long chatId, string deviceId)
    {
        if (OnConnectDevice == null) return;
        await SendMessageAsync(chatId, $"🔄 Đang kết nối tới thiết bị <code>{deviceId}</code>...", "HTML");
        var (success, message) = await OnConnectDevice(deviceId);
        if (success)
        {
            var msg = $"✅ <b>ĐÃ KẾT NỐI THIẾT BỊ THÀNH CÔNG!</b>\n\n" +
                      $"📱 <b>{message}</b>\n\n" +
                      "Bạn có thể bắt đầu chạy quy trình ngay:";
            var keyboard = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "🚀 Bắt đầu chạy ngay", callback_data = "cmd_run_now" },
                        new { text = "📂 Chọn chiến dịch", callback_data = "cmd_pick_folder" }
                    },
                    new[]
                    {
                        new { text = "🏠 Menu chính", callback_data = "cmd_menu" }
                    }
                }
            };
            await SendMessageWithKeyboardAsync(chatId, msg, keyboard, "HTML");
        }
        else
        {
            await SendMessageAsync(chatId, $"❌ <b>Kết nối thất bại:</b> {message}", "HTML");
        }
    }

    private async Task HandleScreenshotAsync(long chatId)
    {
        if (OnRequestScreenshot != null)
        {
            await SendMessageAsync(chatId, "📸 Đang chụp ảnh màn hình thiết bị...", "HTML");
            var imgBytes = await OnRequestScreenshot();
            if (imgBytes != null && imgBytes.Length > 0)
            {
                await SendPhotoAsync(chatId, imgBytes, $"📸 Ảnh chụp màn hình lúc {DateTime.Now:HH:mm:ss}");
            }
            else
            {
                await SendMessageAsync(chatId, "❌ Không thể chụp màn hình (Chưa kết nối thiết bị hoặc thiết bị bận).", "HTML");
            }
        }
    }

    #endregion

    #region Notifications API

    /// <summary>
    /// Gửi thông báo bắt đầu phiên chạy đến toàn bộ Admin.
    /// </summary>
    public async Task SendRunStartedNotificationAsync(int totalJobs, string platform, string campaignName)
    {
        if (!_config.EnableNotifications || !_config.NotifyOnStart) return;

        var message = $"🚀 <b>BẮT ĐẦU QUY TRÌNH UPLOAD</b>\n" +
                      $"━━━━━━━━━━━━━━━━━━━━━\n" +
                      $"📁 Chiến dịch: <b>{campaignName}</b>\n" +
                      $"📱 Nền tảng: <b>{platform}</b>\n" +
                      $"📦 Tổng số video: <b>{totalJobs}</b>\n" +
                      $"🤖 AI SEO: <b>{(_config.UseAiTitle ? "Bật" : "Tắt")}</b>\n" +
                      $"⏰ Thời gian bắt đầu: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";

        await BroadcastMessageAsync(message);
    }

    /// <summary>
    /// Gửi thông báo khi hoàn thành 1 video (Thành công hoặc Thất bại).
    /// </summary>
    public async Task SendVideoFinishedNotificationAsync(
        JobItem job,
        int currentJobIndex,
        int totalJobs,
        bool isSuccess,
        string platform,
        string? errorMessage = null,
        byte[]? errorScreenshot = null,
        int delayNextMinutes = 0)
    {
        if (!_config.EnableNotifications) return;

        if (isSuccess && !_config.NotifyOnSuccess) return;
        if (!isSuccess && !_config.NotifyOnError) return;

        var statusIcon = isSuccess ? "✅" : "❌";
        var statusText = isSuccess ? "THÀNH CÔNG" : "THẤT BẠI";
        var videoFileName = !string.IsNullOrEmpty(job.VideoPath) ? Path.GetFileName(job.VideoPath) : "N/A";
        var delayInfo = isSuccess && delayNextMinutes > 0 ? $"\n⏳ Nghỉ: <b>{delayNextMinutes} phút</b> trước video tiếp theo" : "";

        var sb = new StringBuilder();
        sb.AppendLine($"{statusIcon} <b>VIDEO #{job.Id} {statusText} [{currentJobIndex}/{totalJobs}]</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"📹 Video: <code>{videoFileName}</code>");
        if (!string.IsNullOrEmpty(job.Title))
            sb.AppendLine($"🏷️ Tiêu đề (SEO): <b>{job.Title}</b>");
        if (!string.IsNullOrEmpty(job.ShopeeAffLink))
            sb.AppendLine($"🔗 Link SP: <code>{job.ShopeeAffLink}</code>");
        sb.AppendLine($"📱 Nền tảng: <b>{platform}</b>");

        if (!isSuccess && !string.IsNullOrEmpty(errorMessage))
        {
            sb.AppendLine($"⚠️ Lỗi: <i>{errorMessage}</i>");
        }

        sb.Append($"⏰ Lúc: {DateTime.Now:HH:mm:ss}{delayInfo}");

        var message = sb.ToString();

        if (!isSuccess && errorScreenshot != null && errorScreenshot.Length > 0 && _config.SendScreenshotOnError)
        {
            await BroadcastPhotoAsync(errorScreenshot, message);
        }
        else
        {
            await BroadcastMessageAsync(message);
        }
    }

    /// <summary>
    /// Gửi thông báo tổng kết khi hoàn thành toàn bộ danh sách.
    /// </summary>
    public async Task SendBatchFinishedNotificationAsync(
        int totalJobs,
        int succeededJobs,
        int failedJobs,
        TimeSpan totalDuration,
        string platform)
    {
        if (!_config.EnableNotifications) return;

        var rate = totalJobs > 0 ? (succeededJobs * 100.0 / totalJobs) : 0;
        var durationStr = $"{totalDuration.Hours:D2}h:{totalDuration.Minutes:D2}m:{totalDuration.Seconds:D2}s";

        var message = $"🏁 <b>HOÀN TẤT TẤT CẢ JOBS</b>\n" +
                      $"━━━━━━━━━━━━━━━━━━━━━\n" +
                      $"📱 Nền tảng: <b>{platform}</b>\n" +
                      $"📦 Tổng số video: <b>{totalJobs}</b>\n" +
                      $"✅ Thành công: <b>{succeededJobs}</b> ({rate:F1}%)\n" +
                      $"❌ Thất bại: <b>{failedJobs}</b>\n" +
                      $"⏱️ Tổng thời gian: <b>{durationStr}</b>\n" +
                      $"⏰ Kết thúc lúc: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";

        await BroadcastMessageAsync(message);
    }

    #endregion

    #region HTTP Helpers

    public async Task SendMessageAsync(long chatId, string text, string parseMode = "HTML")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.BotToken)) return;
            var url = $"https://api.telegram.org/bot{_config.BotToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = parseMode
            };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(url, content);
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Telegram] Lỗi gửi tin nhắn tới {chatId}: {ex.Message}");
        }
    }

    public async Task SendMessageWithKeyboardAsync(long chatId, string text, object replyMarkup, string parseMode = "HTML")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.BotToken)) return;
            var url = $"https://api.telegram.org/bot{_config.BotToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = parseMode,
                reply_markup = replyMarkup
            };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(url, content);
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Telegram] Lỗi gửi keyboard tới {chatId}: {ex.Message}");
        }
    }

    public async Task SendPhotoAsync(long chatId, byte[] photoBytes, string caption = "", string parseMode = "HTML")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.BotToken)) return;
            var url = $"https://api.telegram.org/bot{_config.BotToken}/sendPhoto";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(chatId.ToString()), "chat_id");
            if (!string.IsNullOrEmpty(caption))
            {
                form.Add(new StringContent(caption), "caption");
                form.Add(new StringContent(parseMode), "parse_mode");
            }

            var fileContent = new ByteArrayContent(photoBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            form.Add(fileContent, "photo", $"screenshot_{DateTime.Now.Ticks}.png");

            await _httpClient.PostAsync(url, form);
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Telegram] Lỗi gửi ảnh tới {chatId}: {ex.Message}");
        }
    }

    private async Task AnswerCallbackQueryAsync(string callbackQueryId, string? alertText = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.BotToken) || string.IsNullOrEmpty(callbackQueryId)) return;
            var url = $"https://api.telegram.org/bot{_config.BotToken}/answerCallbackQuery";
            var payload = new
            {
                callback_query_id = callbackQueryId,
                text = alertText,
                show_alert = !string.IsNullOrEmpty(alertText)
            };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(url, content);
        }
        catch { }
    }

    private async Task BroadcastMessageAsync(string message)
    {
        var adminIds = _config.GetAdminChatIdList();
        foreach (var chatId in adminIds)
        {
            await SendMessageAsync(chatId, message, "HTML");
        }
    }

    private async Task BroadcastPhotoAsync(byte[] photoBytes, string caption)
    {
        var adminIds = _config.GetAdminChatIdList();
        foreach (var chatId in adminIds)
        {
            await SendPhotoAsync(chatId, photoBytes, caption, "HTML");
        }
    }

    #endregion

    #region Diagnostic & Tools

    public async Task<(bool success, string message, string botName)> TestConnectionAsync(string token, string chatId)
    {
        try
        {
            var getMeUrl = $"https://api.telegram.org/bot{token}/getMe";
            using var meRes = await _httpClient.GetAsync(getMeUrl);
            if (!meRes.IsSuccessStatusCode)
            {
                return (false, "Bot Token không hợp lệ. Vui lòng kiểm tra lại token từ @BotFather.", string.Empty);
            }

            var meJson = await meRes.Content.ReadAsStringAsync();
            var meObj = JObject.Parse(meJson);
            var botName = meObj["result"]?["first_name"]?.ToString() ?? "FlowPilot Bot";
            var username = meObj["result"]?["username"]?.ToString() ?? "";

            if (!string.IsNullOrWhiteSpace(chatId) && long.TryParse(chatId.Split(',')[0].Trim(), out var parsedChatId))
            {
                var sendUrl = $"https://api.telegram.org/bot{token}/sendMessage";
                var payload = new
                {
                    chat_id = parsedChatId,
                    text = $"✅ <b>Kết nối thành công với Bot @{username}!</b>\nThiết bị và Tool FlowPilot đã sẵn sàng gửi thông báo và nhận lệnh điều khiển.",
                    parse_mode = "HTML"
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                using var sendRes = await _httpClient.PostAsync(sendUrl, content);
                if (!sendRes.IsSuccessStatusCode)
                {
                    return (true, $"Token hợp lệ (Bot: @{username}), nhưng không gửi được tin nhắn test tới Chat ID {parsedChatId}. Hãy chắc chắn bạn đã bấm /start với bot trên Telegram.", botName);
                }
            }

            return (true, $"Kết nối hoàn toàn thành công với Bot: @{username}! Đã gửi tin nhắn thử nghiệm.", botName);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi kiểm tra kết nối: {ex.Message}", string.Empty);
        }
    }

    public async Task<List<(long chatId, string name, string username)>> GetRecentChatsAsync(string token)
    {
        var result = new List<(long chatId, string name, string username)>();
        try
        {
            var url = $"https://api.telegram.org/bot{token}/getUpdates?limit=20";
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return result;

            var json = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
            if (root["result"] is JArray updates)
            {
                foreach (var u in updates)
                {
                    var msg = u["message"] ?? u["callback_query"]?["message"];
                    if (msg != null)
                    {
                        var chat = msg["chat"];
                        if (chat != null)
                        {
                            var cid = chat["id"]?.Value<long>() ?? 0;
                            var name = $"{chat["first_name"]} {chat["last_name"]}".Trim();
                            var uname = chat["username"]?.ToString() ?? "";
                            if (cid != 0 && !result.Any(r => r.chatId == cid))
                            {
                                result.Add((cid, name, uname));
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Telegram] Lỗi quét Chat ID: {ex.Message}");
        }
        return result;
    }

    #endregion

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        StopPolling();
        _httpClient.Dispose();
    }
}
