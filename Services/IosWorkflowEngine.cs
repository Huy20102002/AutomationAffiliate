using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

/// <summary>Executes an ordered workflow for each imported job exclusively for iOS via IosManager.</summary>
public sealed class IosWorkflowEngine
{
    private readonly IosManager _iosManager;
    private readonly Func<string, string, Task>? _humanInput;

    public IosWorkflowEngine(IosManager iosManager, Func<string, string, Task>? humanInput = null)
    {
        _iosManager = iosManager;
        _humanInput = humanInput;
    }

    public async Task<string> RunAllJobsAsync(
        List<WorkflowStep> steps,
        List<JobItem> jobs,
        string deviceId,
        CancellationToken ct,
        Action<int, string, string>? onJobUpdate = null,
        string targetPlatform = "Shopee",
        IReadOnlyList<WorkflowVariable>? variables = null,
        Action<int>? onStepStarted = null,
        bool skipOpenAppAfterFirstJob = false,
        Func<ShopeeVideoUploader.Models.JobItem, Task>? preJobAction = null,
        int delayBetweenJobsMinMinutes = 0,
        int delayBetweenJobsMaxMinutes = 0)
    {
        var hasOpenAppStep = steps.Any(step => step.Type == StepType.OpenApp);
        var hasPushVideoStep = steps.Any(step => step.Type == StepType.PushVideo);
        var appOpenedInThisRun = false;

        for (var i = 0; i < jobs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var job = jobs[i];
            
            if (targetPlatform == "Shopee" && job.ShopeeStatus == "Đã up Shopee") continue;
            if (targetPlatform == "Facebook" && job.FbStatus == "Đã up Facebook") continue;

            var recoveryAttempted = false;
            try
            {
                if (preJobAction != null)
                {
                    onJobUpdate?.Invoke(i, "Đang chạy...", "[AI] Đang sinh tiêu đề chuẩn SEO...");
                    await preJobAction(job);
                }
                job.Status = "Đang chạy";
                job.Log = string.Empty;
                onJobUpdate?.Invoke(i, job.Status, string.Empty);

                var progress = new Progress<string>(message =>
                {
                    job.Log = message;
                    onJobUpdate?.Invoke(i, "Đang chạy", message);
                });

                deviceId = await ExecuteWorkflowAsync(
                    steps,
                    job,
                    deviceId,
                    ct,
                    progress,
                    variables,
                    onStepStarted,
                    targetPlatform,
                    skipOpenApp: skipOpenAppAfterFirstJob && hasOpenAppStep && appOpenedInThisRun);
                if (hasOpenAppStep)
                    appOpenedInThisRun = true;
                if (targetPlatform == "Facebook") job.FbStatus = "Đã up Facebook";
                else job.ShopeeStatus = "Đã up Shopee";
                job.Status = "Thành công";
                job.Log = "Hoàn tất (iOS)"; // TODO: Local video deletion
                onJobUpdate?.Invoke(i, job.Status, job.Log);
                Logger.Info($"[iOS] Job #{job.Id}: SUCCESS");

                if (i < jobs.Count - 1 && (delayBetweenJobsMinMinutes > 0 || delayBetweenJobsMaxMinutes > 0))
                {
                    var delayMin = Math.Min(delayBetweenJobsMinMinutes, delayBetweenJobsMaxMinutes);
                    var delayMax = Math.Max(delayBetweenJobsMinMinutes, delayBetweenJobsMaxMinutes);
                    var randomDelayMinutes = new Random().Next(delayMin, delayMax + 1);
                    if (randomDelayMinutes > 0)
                    {
                        var delayMs = randomDelayMinutes * 60 * 1000;
                        Logger.Info($"Đang chờ {randomDelayMinutes} phút trước khi chạy video tiếp theo...");
                        onJobUpdate?.Invoke(i, "Thành công", $"Chờ {randomDelayMinutes} phút...");
                        await Task.Delay(delayMs, ct);
                    }
                }
            }
            catch (Exception ex) when (!recoveryAttempted)
            {
                recoveryAttempted = true;
                job.Status = "Đang thử lại";
                job.Log = ex.Message;
                onJobUpdate?.Invoke(i, job.Status, job.Log);
                Logger.Warn($"[iOS] Job #{job.Id}: Lỗi, thử mở lại app: {ex.Message}");

                var openAppStep = steps.FirstOrDefault(s => s.Type == StepType.OpenApp);
                if (openAppStep != null)
                {
                    var pkg = ResolveText(openAppStep.TextValue, job, variables);
                    await _iosManager.ForceStopAppAsync(deviceId, pkg, ct);
                    await Task.Delay(1200, ct);
                    await _iosManager.OpenAppAsync(deviceId, pkg, ct);
                    await Task.Delay(2000, ct);
                }

                try
                {
                    job.Status = "Đang chạy lại";
                    job.Log = string.Empty;
                    onJobUpdate?.Invoke(i, job.Status, string.Empty);
                    var retryProgress = new Progress<string>(message =>
                    {
                        job.Log = message;
                        onJobUpdate?.Invoke(i, "Đang chạy lại", message);
                    });

                    deviceId = await ExecuteWorkflowAsync(
                        steps,
                        job,
                        deviceId,
                        ct,
                        retryProgress,
                        variables,
                        onStepStarted,
                        targetPlatform,
                        skipOpenApp: true);
                    if (hasOpenAppStep)
                        appOpenedInThisRun = true;
                    if (targetPlatform == "Facebook") job.FbStatus = "Đã up Facebook";
                    else job.ShopeeStatus = "Đã up Shopee";
                    job.Status = "Thành công";
                    job.Log = "Hoàn tất (iOS)";
                    onJobUpdate?.Invoke(i, job.Status, job.Log);
                    Logger.Info($"[iOS] Job #{job.Id}: SUCCESS after recovery");
                }
                catch (OperationCanceledException)
                {
                    job.Status = "Đã dừng";
                    job.Log = "Bị dừng bởi người dùng";
                    onJobUpdate?.Invoke(i, job.Status, job.Log);
                    throw;
                }
                catch (Exception retryEx)
                {
                    job.Status = JobStatus.Failed;
                    job.Log = retryEx.Message;
                    onJobUpdate?.Invoke(i, job.Status, retryEx.Message);
                    Logger.Error($"[iOS] Job #{job.Id}: FAILED after recovery", retryEx);

                    // Thoát app và vào lại khi gặp lỗi để reset trạng thái sạch sẽ
                    await RestartAppOnFailureAsync(steps, job, variables, deviceId, ct);
                    appOpenedInThisRun = true;
                }
            }
            catch (OperationCanceledException)
            {
                job.Status = "Đã dừng";
                job.Log = "Bị dừng bởi người dùng";
                onJobUpdate?.Invoke(i, job.Status, job.Log);
                throw;
            }
            catch (Exception ex)
            {
                job.Status = "Lỗi";
                job.Log = ex.Message;
                onJobUpdate?.Invoke(i, job.Status, ex.Message);
                Logger.Error($"[iOS] Job #{job.Id}: FAILED", ex);

                // Thoát app và vào lại khi gặp lỗi để reset trạng thái sạch sẽ
                await RestartAppOnFailureAsync(steps, job, variables, deviceId, ct);
                appOpenedInThisRun = true;
            }
        }
        return deviceId;
    }

    public async Task<string> ExecuteWorkflowAsync(
        List<WorkflowStep> steps,
        JobItem job,
        string deviceId,
        CancellationToken ct,
        IProgress<string>? progress = null,
        IReadOnlyList<WorkflowVariable>? variables = null,
        Action<int>? onStepStarted = null,
        string targetPlatform = "Shopee",
        bool skipOpenApp = false,
        Func<ShopeeVideoUploader.Models.JobItem, Task>? preJobAction = null)
    {
        if (preJobAction != null)
        {
            progress?.Report("[AI] Đang sinh tiêu đề chuẩn SEO...");
            await preJobAction(job);
        }

        for (var i = 0; i < steps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var step = steps[i];
            var isSkippedOpenApp = skipOpenApp && step.Type == StepType.OpenApp;
            var stepInfo = $"[{i + 1}/{steps.Count}] {step.GetDisplayText()}";
            if (isSkippedOpenApp)
                stepInfo += " · bỏ qua từ job thứ 2";
            progress?.Report(stepInfo);
            Logger.Info($"[iOS] Job #{job.Id}: {stepInfo}");
            if (isSkippedOpenApp)
                continue;

            onStepStarted?.Invoke(i);

            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await ExecuteStepAsync(step, job, deviceId, ct, variables);
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is FileNotFoundException || ex is InvalidDataException || (ex is InvalidOperationException && ex.Message.Contains("VideoPath")) || ex is OperationCanceledException)
                    {
                        throw;
                    }

                    if (attempt == maxRetries)
                    {
                        throw;
                    }

                    progress?.Report($"[Lỗi] {ex.Message} - Thử lại ({attempt}/{maxRetries})...");
                    Logger.Warn($"[iOS] Job #{job.Id}: Lỗi bước '{step.GetDisplayText()}': {ex.Message}. Đang thử lại lần {attempt}/{maxRetries}...");
                    await Task.Delay(2000, ct);
                }
            }
        }

        return deviceId;
    }

    private async Task ExecuteStepAsync(
        WorkflowStep step,
        JobItem job,
        string deviceId,
        CancellationToken ct,
        IReadOnlyList<WorkflowVariable>? variables)
    {
        switch (step.Type)
        {
            case StepType.Start:
            case StepType.End:
                break;

            case StepType.Tap:
                if (step.TapMode != TapMode.Coordinates)
                {
                    throw new InvalidOperationException("Quy trình iOS hiện tại chỉ hỗ trợ chạm theo Tọa độ (X, Y).");
                }
                await _iosManager.ClickAsync(deviceId, step.X, step.Y, ct);
                break;

            case StepType.RandomTap:
                Point iosPoint;
                if (step.RandomCoordinates != null && step.RandomCoordinates.Count > 0)
                {
                    int chosenIdx = Random.Shared.Next(step.RandomCoordinates.Count);
                    iosPoint = step.RandomCoordinates[chosenIdx];
                    Logger.Info($"[CHẠM NGẪU NHIÊN iOS] Job #{job.Id}: Chọn ngẫu nhiên tọa độ ({iosPoint.X}, {iosPoint.Y}) [Điểm {chosenIdx + 1}/{step.RandomCoordinates.Count}]");
                }
                else
                {
                    iosPoint = new Point(step.X, step.Y);
                }
                await _iosManager.ClickAsync(deviceId, iosPoint.X, iosPoint.Y, ct);
                break;

            case StepType.InputText:
                var inputText = ResolveInputText(step, job, variables);
                if (!step.TakeAllLinks && !string.IsNullOrWhiteSpace(inputText))
                {
                    var lines = inputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 1 && lines.All(l => l.Trim().StartsWith("http://", StringComparison.OrdinalIgnoreCase) || l.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    {
                        inputText = lines[0].Trim();
                    }
                }
                var oldTitle = !string.IsNullOrWhiteSpace(inputText)
                    ? inputText
                    : (!string.IsNullOrWhiteSpace(job.Title) ? job.Title : (Path.GetFileNameWithoutExtension(job.VideoPath) ?? string.Empty));

                if (string.IsNullOrWhiteSpace(inputText))
                {
                    if (!string.IsNullOrWhiteSpace(oldTitle))
                    {
                        inputText = oldTitle;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Sản phẩm #{job.Id} không có dữ liệu để nhập cho cột '{step.BindingColumn}'.");
                    }
                }
                
                if (step.UseAiForText)
                {
                    if (job.IsAiTitleGenerated)
                    {
                        Logger.Info($"[iOS] [AI] Video #{job.Id} đã được tạo tiêu đề AI trước đó ('{inputText}'), bỏ qua không gọi AI.");
                    }
                    else
                    {
                        var configService = new AiConfigService();
                        var config = configService.Load();
                        if (!string.IsNullOrWhiteSpace(config.ApiKey))
                        {
                            var originalTitle = inputText;
                            var aiService = new AiTitleService();
                            Logger.Info($"[iOS] [AI] Đang sinh nội dung bằng AI cho video #{job.Id}...");
                            try
                            {
                                var aiResult = await aiService.GenerateTitleAsync(config, originalTitle);
                                if (!string.IsNullOrWhiteSpace(aiResult) && !string.Equals(aiResult.Trim(), originalTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    inputText = aiResult.Trim();
                                    job.IsAiTitleGenerated = true;
                                    Logger.Info($"[iOS] [AI] Sinh tiêu đề AI thành công: '{inputText}'");
                                }
                                else
                                {
                                    inputText = !string.IsNullOrWhiteSpace(originalTitle) ? originalTitle : oldTitle;
                                    Logger.Warn($"[iOS] [AI] Không sinh được tiêu đề AI mới, tự động dùng tiêu đề cũ: '{inputText}'");
                                }
                            }
                            catch (Exception ex)
                            {
                                inputText = !string.IsNullOrWhiteSpace(originalTitle) ? originalTitle : oldTitle;
                                Logger.Warn($"[iOS] [AI] Lỗi kết nối API AI ({ex.Message}), tự động dùng tiêu đề cũ: '{inputText}'");
                            }
                        }
                        else
                        {
                            Logger.Info($"[iOS] [AI] Chưa cấu hình API Key, dùng tiêu đề cũ: '{inputText}'");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(inputText))
                {
                    inputText = !string.IsNullOrWhiteSpace(oldTitle) ? oldTitle : (job.Title ?? string.Empty);
                }

                Logger.Info($"[iOS] [INPUT] Job #{job.Id}: nhập \"{(inputText.Length > 80 ? inputText[..80] + "..." : inputText)}\"");

                if (_humanInput != null)
                    await _humanInput(deviceId, inputText);
                else
                    await _iosManager.InputTextAsync(deviceId, inputText, ct);
                break;

            case StepType.PushVideo:
                var videoPath = ResolveVideoPath(step, job, variables);
                if (string.IsNullOrWhiteSpace(videoPath))
                    throw new InvalidOperationException($"Sản phẩm #{job.Id} chưa có VideoPath.");
                if (!File.Exists(videoPath))
                    throw new FileNotFoundException($"Video không tồn tại: {videoPath}");
                if (!IsValidVideoFile(videoPath))
                    throw new InvalidDataException($"File video bị lỗi định dạng hoặc không thể đọc được. Vui lòng kiểm tra lại file gốc: {videoPath}");
                
                if (step.ClearDeviceVideosBeforeUpload)
                    await _iosManager.ClearFlowPilotVideosAsync(ct);

                var fileName = BuildRemoteVideoName(job, Path.GetFileName(videoPath));
                var remotePath = $"DCIM/100APPLE/{fileName}";
                Logger.Info($"[iOS] [VIDEO] Job #{job.Id}: đẩy {videoPath} → {remotePath}");
                await _iosManager.PushFileAsync(deviceId, videoPath, remotePath, ct);
                break;

            case StepType.PushImage:
                var imagePaths = ResolveImagePaths(step, job, variables);
                if (imagePaths.Count == 0)
                    throw new InvalidOperationException($"Sản phẩm #{job.Id} chưa có đường dẫn ảnh (ImagePath / VideoPath).");

                foreach (var imgPath in imagePaths)
                {
                    if (!File.Exists(imgPath))
                        throw new FileNotFoundException($"Ảnh không tồn tại: {imgPath}");
                    if (!IsValidImageFile(imgPath))
                        throw new InvalidDataException($"File ảnh không đúng định dạng hoặc bị lỗi: {imgPath}");
                }

                if (step.ClearDeviceVideosBeforeUpload)
                    await _iosManager.ClearFlowPilotVideosAsync(ct);

                for (int imgIdx = 0; imgIdx < imagePaths.Count; imgIdx++)
                {
                    var imgPath = imagePaths[imgIdx];
                    var imgFileName = BuildRemoteVideoName(job, Path.GetFileName(imgPath));
                    if (imagePaths.Count > 1)
                    {
                        var stem = Path.GetFileNameWithoutExtension(imgFileName);
                        var ext = Path.GetExtension(imgFileName);
                        imgFileName = $"{stem}_{imgIdx + 1}{ext}";
                    }
                    var imgRemotePath = $"DCIM/100APPLE/{imgFileName}";
                    Logger.Info($"[iOS] [IMAGE] Job #{job.Id}: đẩy {imgPath} → {imgRemotePath}");
                    await _iosManager.PushFileAsync(deviceId, imgPath, imgRemotePath, ct);
                }
                break;

            case StepType.Delay:
            {
                var actualDelay = step.DelayAfterMs;
                if (step.DelayMaxMs.HasValue && step.DelayMaxMs.Value > step.DelayAfterMs) {
                    actualDelay = new Random().Next(step.DelayAfterMs, step.DelayMaxMs.Value + 1);
                }
                await Task.Delay(actualDelay, ct);
                break;
            }

            case StepType.Swipe:
                await _iosManager.SwipeAsync(deviceId, step.X, step.Y, step.X2, step.Y2, step.SwipeDurationMs, ct);
                break;

            case StepType.OpenApp:
                await _iosManager.OpenAppAsync(deviceId, ResolveText(step.TextValue, job, variables), ct);
                break;

            case StepType.MediaScan:
                Logger.Info("[iOS] Bỏ qua bước MediaScan (không áp dụng trên iOS)");
                break;
                
            case StepType.AdbShell:
                Logger.Info("[iOS] Bỏ qua bước AdbShell (không áp dụng trên iOS)");
                break;
        }
    }

    // Helpers
    private static string ResolveVideoPath(WorkflowStep step, JobItem job, IReadOnlyList<WorkflowVariable>? variables)
    {
        var path = step.VideoSource switch
        {
            VideoSourceMode.FolderAndExcelFileName => Path.Combine(
                ResolveText(step.VideoFolderPath, job, variables),
                Path.GetFileName(job.VideoPath)),
            VideoSourceMode.FixedFile => ResolveText(step.VideoFilePath, job, variables),
            VideoSourceMode.ExcelPath => job.VideoPath,
            _ => job.VideoPath
        };

        return NormalizeLocalPath(path);
    }

    private static List<string> ResolveImagePaths(WorkflowStep step, JobItem job, IReadOnlyList<WorkflowVariable>? variables)
    {
        var rawPath = step.VideoSource switch
        {
            VideoSourceMode.FolderAndExcelFileName => Path.Combine(
                ResolveText(step.VideoFolderPath, job, variables),
                Path.GetFileName(job.GetColumnValue(string.IsNullOrWhiteSpace(step.BindingColumn) ? "ImagePath" : step.BindingColumn))),
            VideoSourceMode.FixedFile => ResolveText(step.VideoFilePath, job, variables),
            _ => !string.IsNullOrWhiteSpace(step.BindingColumn)
                ? job.GetColumnValue(step.BindingColumn)
                : job.GetColumnValue("ImagePath")
        };

        if (string.IsNullOrWhiteSpace(rawPath))
            rawPath = job.VideoPath;

        if (string.IsNullOrWhiteSpace(rawPath)) return new List<string>();

        var parts = rawPath.Split(new[] { '\r', '\n', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<string>();
        foreach (var p in parts)
        {
            var normalized = NormalizeLocalPath(p.Trim().Trim('"'));
            if (!string.IsNullOrWhiteSpace(normalized))
                result.Add(normalized);
        }
        return result;
    }

    private static bool IsValidImageFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var validExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".heic", ".gif" };
            if (!validExts.Contains(ext)) return false;

            var info = new FileInfo(path);
            return info.Length > 100;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildRemoteVideoName(JobItem job, string sourceFileName)
    {
        var extension = Path.GetExtension(sourceFileName);
        var stem = Path.GetFileNameWithoutExtension(sourceFileName);
        var safeStem = new string(stem.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
        if (string.IsNullOrWhiteSpace(safeStem)) safeStem = "video";
        if (safeStem.Length > 80) safeStem = safeStem[..80];
        return $"flowpilot_{job.Id}_{safeStem}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
    }

    
    private static bool IsValidVideoFile(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Length < 1024) return false;

            try
            {
                using var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ffprobe",
                        Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{path}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(3000);
                
                if (process.ExitCode == 0 && double.TryParse(output, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var duration))
                {
                    return duration > 0;
                }
                
                return false;
            }
            catch
            {
                // Fallback to basic header check if ffprobe is not installed
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var buffer = new byte[1024];
                var bytesRead = fs.Read(buffer, 0, buffer.Length);
                var header = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                return header.Contains("ftyp") || header.Contains("moov") || header.Contains("webm") || header.Contains("matroska");
            }
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeLocalPath(string path)
    {
        var normalized = (path ?? string.Empty).Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;
        try { return Path.GetFullPath(normalized); }
        catch (Exception) { return normalized; }
    }

    private static string ResolveText(
        string template,
        JobItem job,
        IReadOnlyList<WorkflowVariable>? variables,
        bool takeAllLinks = false)
    {
        if (string.IsNullOrEmpty(template)) return template;

        var links = job.GetShopeeAffLinks();
        var allLinksNewline = links.Count > 0 ? string.Join("\n", links) : job.ShopeeAffLink;
        var primaryLink = links.Count > 0 ? links[0] : job.ShopeeAffLink;
        var chosenLink = takeAllLinks ? allLinksNewline : primaryLink;

        var result = template.Replace("{Title}", job.Title)
                             .Replace("{ShopeeAffLink}", chosenLink)
                             .Replace("{ShopeeAffLinks}", allLinksNewline)
                             .Replace("{ShopeeAffLink_All}", allLinksNewline)
                             .Replace("{ShopeeAffLink_First}", primaryLink)
                             .Replace("{ShopeeAffLink:1}", links.Count > 0 ? links[0] : "")
                             .Replace("{ShopeeAffLink:2}", links.Count > 1 ? links[1] : "")
                             .Replace("{ShopeeAffLink:3}", links.Count > 2 ? links[2] : "")
                             .Replace("{ShopeeAffLink:4}", links.Count > 3 ? links[3] : "")
                             .Replace("{ShopeeAffLink:5}", links.Count > 4 ? links[4] : "")
                             .Replace("{ShopeeAffLink:6}", links.Count > 5 ? links[5] : "")
                             .Replace("{ShopeeAffLink_1}", links.Count > 0 ? links[0] : "")
                             .Replace("{ShopeeAffLink_2}", links.Count > 1 ? links[1] : "")
                             .Replace("{ShopeeAffLink_3}", links.Count > 2 ? links[2] : "")
                             .Replace("{ShopeeAffLink_4}", links.Count > 3 ? links[3] : "")
                             .Replace("{ShopeeAffLink_5}", links.Count > 4 ? links[4] : "")
                             .Replace("{ShopeeAffLink_6}", links.Count > 5 ? links[5] : "")
                             .Replace("{ShopeeAffLinkAll}", allLinksNewline)
                             .Replace("{VideoPath}", job.VideoPath)
                             .Replace("{Id}", job.Id.ToString());

        if (variables == null) return result;
        foreach (var variable in variables)
        {
            if ((variable.IsBuiltIn && !variable.IsDeviceBuiltIn) || string.IsNullOrWhiteSpace(variable.Name)) continue;
            result = result.Replace(variable.Token, variable.Value);
        }
        return result;
    }

    private static string ResolveInputText(WorkflowStep step, JobItem job, IReadOnlyList<WorkflowVariable>? variables)
    {
        var input = step.TextValue.Trim();
        var takeAll = step.TakeAllLinks;
        if (string.IsNullOrWhiteSpace(step.BindingColumn))
        {
            if (job.HasColumn(input)) return job.GetColumnValue(input, takeAll);
            var resolved = ResolveText(step.TextValue, job, variables, takeAll);
            return Regex.Replace(resolved, @"\{([^{}]+)\}", match =>
            {
                var columnName = match.Groups[1].Value.Trim();
                return job.HasColumn(columnName) ? job.GetColumnValue(columnName, takeAll) : match.Value;
            });
        }
        var columnValue = job.GetColumnValue(step.BindingColumn, takeAll);
        if (string.IsNullOrWhiteSpace(step.TextValue)) return columnValue;
        return ResolveText(step.TextValue, job, variables, takeAll).Replace("{Value}", columnValue);
    }

    private async Task RestartAppOnFailureAsync(
        IReadOnlyList<WorkflowStep> steps,
        JobItem job,
        IReadOnlyList<WorkflowVariable>? variables,
        string deviceId,
        CancellationToken ct)
    {
        var openAppStep = steps.FirstOrDefault(s => s.Type == StepType.OpenApp);
        var pkg = openAppStep != null
            ? ResolveText(openAppStep.TextValue, job, variables).Trim()
            : "com.bee.shopee.vn";

        if (string.IsNullOrWhiteSpace(pkg))
            pkg = "com.bee.shopee.vn";

        try
        {
            Logger.Warn($"[iOS Tự phục hồi] Video #{job.Id} gặp sự cố. Đang thoát app {pkg} và mở lại để reset trạng thái sạch sẽ...");
            await _iosManager.ForceStopAppAsync(deviceId, pkg, ct);
            await Task.Delay(1200, ct);
            await _iosManager.OpenAppAsync(deviceId, pkg, ct);
            await Task.Delay(3000, ct);
            Logger.Info($"[iOS Tự phục hồi] Đã mở lại {pkg} thành công trên iOS, sẵn sàng cho video tiếp theo.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[iOS Tự phục hồi] Không thể khởi động lại app trên iOS: {ex.Message}");
        }
    }
}
