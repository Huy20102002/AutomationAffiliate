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
        IReadOnlyList<WorkflowVariable>? variables = null,
        Action<int>? onStepStarted = null,
        bool skipOpenAppAfterFirstJob = false)
    {
        var hasOpenAppStep = steps.Any(step => step.Type == StepType.OpenApp);
        var hasPushVideoStep = steps.Any(step => step.Type == StepType.PushVideo);
        var appOpenedInThisRun = false;

        for (var i = 0; i < jobs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var job = jobs[i];
            if (job.Status == "Thành công" || job.ShopeeStatus == "Đã up Shopee") continue;

            var recoveryAttempted = false;
            try
            {
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
                    skipOpenApp: skipOpenAppAfterFirstJob && hasOpenAppStep && appOpenedInThisRun);
                if (hasOpenAppStep)
                    appOpenedInThisRun = true;
                job.ShopeeStatus = "Đã up Shopee";
                job.Status = "Thành công";
                job.Log = "Hoàn tất (iOS)"; // TODO: Local video deletion
                onJobUpdate?.Invoke(i, job.Status, job.Log);
                Logger.Info($"[iOS] Job #{job.Id}: SUCCESS");
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
                        skipOpenApp: true);
                    if (hasOpenAppStep)
                        appOpenedInThisRun = true;
                    job.ShopeeStatus = "Đã up Shopee";
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
                    job.Status = "Lỗi";
                    job.Log = retryEx.Message;
                    onJobUpdate?.Invoke(i, job.Status, retryEx.Message);
                    Logger.Error($"[iOS] Job #{job.Id}: FAILED after recovery", retryEx);
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
        bool skipOpenApp = false)
    {
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

            await ExecuteStepAsync(step, job, deviceId, ct, variables);
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

            case StepType.InputText:
                var inputText = ResolveInputText(step, job, variables);
                if (string.IsNullOrWhiteSpace(inputText))
                    throw new InvalidOperationException($"Sản phẩm #{job.Id} không có dữ liệu để nhập cho cột '{step.BindingColumn}'.");
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
                
                var fileName = BuildRemoteVideoName(job, Path.GetFileName(videoPath));
                var remotePath = $"/Documents/{fileName}";
                Logger.Info($"[iOS] [VIDEO] Job #{job.Id}: đẩy {videoPath} → {remotePath}");
                await _iosManager.PushFileAsync(deviceId, videoPath, remotePath, ct);
                break;

            case StepType.Delay:
                await Task.Delay(step.DelayAfterMs, ct);
                break;

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

    private static string BuildRemoteVideoName(JobItem job, string sourceFileName)
    {
        var extension = Path.GetExtension(sourceFileName);
        var stem = Path.GetFileNameWithoutExtension(sourceFileName);
        var safeStem = new string(stem.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
        if (string.IsNullOrWhiteSpace(safeStem)) safeStem = "video";
        if (safeStem.Length > 80) safeStem = safeStem[..80];
        return $"flowpilot_{job.Id}_{safeStem}{extension}";
    }

    private static string NormalizeLocalPath(string path)
    {
        var normalized = (path ?? string.Empty).Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;
        try { return Path.GetFullPath(normalized); }
        catch (Exception) { return normalized; }
    }

    private static string ResolveText(string template, JobItem job, IReadOnlyList<WorkflowVariable>? variables)
    {
        if (string.IsNullOrEmpty(template)) return template;
        var result = template.Replace("{Title}", job.Title)
                             .Replace("{ShopeeAffLink}", job.ShopeeAffLink)
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
        if (string.IsNullOrWhiteSpace(step.BindingColumn))
        {
            if (job.HasColumn(input)) return job.GetColumnValue(input);
            var resolved = ResolveText(step.TextValue, job, variables);
            return Regex.Replace(resolved, @"\{([^{}]+)\}", match =>
            {
                var columnName = match.Groups[1].Value.Trim();
                return job.HasColumn(columnName) ? job.GetColumnValue(columnName) : match.Value;
            });
        }
        var columnValue = job.GetColumnValue(step.BindingColumn);
        if (string.IsNullOrWhiteSpace(step.TextValue)) return columnValue;
        return ResolveText(step.TextValue, job, variables).Replace("{Value}", columnValue);
    }
}
