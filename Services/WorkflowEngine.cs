using System.Text.RegularExpressions;
using AdvancedSharpAdbClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;
namespace ShopeeVideoUploader.Services;

/// <summary>Executes an ordered workflow for each imported job.</summary>
public sealed class WorkflowEngine
{
    private readonly AdbManager _adb;
    private readonly Func<DeviceData, string, Task>? _humanInput;

    public WorkflowEngine(AdbManager adb, Func<DeviceData, string, Task>? humanInput = null)
    {
        _adb = adb;
        _humanInput = humanInput;
    }

    public static void SaveWorkflow(List<WorkflowStep> steps, string filePath)
        => SaveWorkflow(steps, [], [], filePath);

    public static void SaveWorkflow(List<WorkflowStep> steps, List<WorkflowVariable> variables, string filePath)
        => SaveWorkflow(steps, variables, [], filePath);

    public static void SaveWorkflow(
        List<WorkflowStep> steps,
        List<WorkflowVariable> variables,
        List<JobItem> products,
        string filePath)
    {
        var document = new WorkflowDocument
        {
            Steps = steps,
            Variables = variables.Where(v => !v.IsBuiltIn).ToList(),
            Products = products
        };
        File.WriteAllText(filePath, JsonConvert.SerializeObject(document, Formatting.Indented));
        Logger.Info($"Workflow saved: {filePath}");
    }

    public static List<WorkflowStep> LoadWorkflow(string filePath)
        => LoadWorkflowDocument(filePath).Steps;

    public static WorkflowDocument LoadWorkflowDocument(string filePath)
    {
        var token = JToken.Parse(File.ReadAllText(filePath));
        var document = token.Type == JTokenType.Array
            ? new WorkflowDocument { Steps = token.ToObject<List<WorkflowStep>>() ?? [] }
            : token.ToObject<WorkflowDocument>() ?? new WorkflowDocument();
        Logger.Info($"Workflow loaded: {filePath} ({document.Steps.Count} steps, {document.Variables.Count} variables)");
        return document;
    }

    public async Task<DeviceData> ExecuteWorkflowAsync(
        List<WorkflowStep> steps,
        JobItem job,
        DeviceData device,
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
            Logger.Info($"Job #{job.Id}: {stepInfo}");
            if (isSkippedOpenApp)
                continue;

            onStepStarted?.Invoke(i);

            try
            {
                await ExecuteStepAsync(step, job, device, ct, variables);
            }
            catch (Exception ex) when (ex.Message.Contains("no device with transport id", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Warn($"[ADB] Thiết bị mất kết nối (transport id thay đổi), đang thử kết nối lại...");
                await Task.Delay(2000, ct); // Đợi ADB server nhận diện lại thiết bị
                var newDevice = await _adb.GetDeviceBySerialAsync(device.Serial);
                if (newDevice != null)
                {
                    device = newDevice;
                    Logger.Info($"[ADB] Đã lấy lại kết nối thiết bị {device.Serial}, thử lại bước hiện tại...");
                    await ExecuteStepAsync(step, job, device, ct, variables);
                }
                else
                {
                    throw new InvalidOperationException($"Không thể tìm lại thiết bị {device.Serial} sau khi mất kết nối ADB.", ex);
                }
            }

            if (step.Type != StepType.Delay && step.DelayAfterMs > 0)
                await Task.Delay(step.DelayAfterMs, ct);
        }
        return device;
    }

    public async Task<DeviceData> RunAllJobsAsync(
        List<WorkflowStep> steps,
        List<JobItem> jobs,
        DeviceData device,
        CancellationToken ct,
        Action<int, string, string>? onJobUpdate = null,
        IReadOnlyList<WorkflowVariable>? variables = null,
        Action<int>? onStepStarted = null,
        bool skipOpenAppAfterFirstJob = true)
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

                device = await ExecuteWorkflowAsync(
                    steps,
                    job,
                    device,
                    ct,
                    progress,
                    variables,
                    onStepStarted,
                    skipOpenApp: skipOpenAppAfterFirstJob && hasOpenAppStep && appOpenedInThisRun);
                if (hasOpenAppStep)
                    appOpenedInThisRun = true;
                job.ShopeeStatus = "Đã up Shopee";
                job.Status = "Thành công";
                job.Log = DeleteLocalVideosAfterSuccess(steps, jobs, i, job, variables);
                onJobUpdate?.Invoke(i, job.Status, job.Log);
                Logger.Info($"Job #{job.Id}: SUCCESS");
            }
            catch (UiAutomationNotReadyException ex) when (!recoveryAttempted)
            {
                recoveryAttempted = true;
                job.Status = "Đang thử lại";
                job.Log = ex.Message;
                onJobUpdate?.Invoke(i, job.Status, job.Log);
                Logger.Warn($"Job #{job.Id}: UI Automator not ready, restarting app and retrying once.");

                var packageName = ResolveOpenAppPackage(steps, job, variables);
                if (!string.IsNullOrWhiteSpace(packageName))
                {
                    await _adb.ForceStopAppAsync(device, packageName);
                    await Task.Delay(1200, ct);
                    await _adb.OpenAppAsync(device, packageName);
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

                    device = await ExecuteWorkflowAsync(
                        steps,
                        job,
                        device,
                        ct,
                        retryProgress,
                        variables,
                        onStepStarted,
                        skipOpenApp: false);
                    if (hasOpenAppStep)
                        appOpenedInThisRun = true;
                    job.ShopeeStatus = "Đã up Shopee";
                    job.Status = "Thành công";
                    job.Log = DeleteLocalVideosAfterSuccess(steps, jobs, i, job, variables);
                    onJobUpdate?.Invoke(i, job.Status, job.Log);
                    Logger.Info($"Job #{job.Id}: SUCCESS after recovery");
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
                    Logger.Error($"Job #{job.Id}: FAILED after recovery", retryEx);
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
                Logger.Error($"Job #{job.Id}: FAILED", ex);
            }
        }
        return device;
    }

    private async Task ExecuteStepAsync(
        WorkflowStep step,
        JobItem job,
        DeviceData device,
        CancellationToken ct,
        IReadOnlyList<WorkflowVariable>? variables)
    {
        switch (step.Type)
        {
            case StepType.Start:
            case StepType.End:
                // Control-flow markers; no device command required.
                break;

            case StepType.Tap:
                var tapPoint = step.TapMode switch
                {
                    TapMode.XPath => await _adb.FindUiNodeCenterStrictAsync(device, ResolveText(step.TapXPath, job, variables), ct),
                    TapMode.Image => await _adb.FindImageCenterAsync(
                        device,
                        ResolveText(step.TapImagePath, job, variables),
                        step.TapImageThreshold,
                        step.TapImageTimeoutMs,
                        ct),
                    _ => new Point(step.X, step.Y)
                };
                if (tapPoint == null)
                    throw new InvalidOperationException($"Không tìm thấy mục tiêu chạm ({step.TapMode}).");
                await _adb.TapAsync(device, tapPoint.Value.X, tapPoint.Value.Y);
                break;

            case StepType.InputText:
                var inputText = ResolveInputText(step, job, variables);
                if (string.IsNullOrWhiteSpace(inputText))
                    throw new InvalidOperationException($"Sản phẩm #{job.Id} không có dữ liệu để nhập cho cột '{step.BindingColumn}'.");
                Logger.Info($"[INPUT] Job #{job.Id}: nhập \"{(inputText.Length > 80 ? inputText[..80] + "..." : inputText)}\"");
                try
                {
                    var foregroundPackage = await _adb.GetForegroundPackageAsync(device);
                    Logger.Info($"[INPUT] Ứng dụng đang focus: {foregroundPackage}");
                    if (foregroundPackage.Contains("launcher", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Android đang ở màn hình chính, chưa ở ô nhập văn bản. Hãy kiểm tra lại các bước Chạm trước đó.");
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Không kiểm tra được ứng dụng đang focus: {ex.Message}");
                }
                if (_humanInput != null)
                    await _humanInput(device, inputText);
                else
                    await _adb.InputTextAsync(device, inputText);
                break;

            case StepType.PushVideo:
                var videoPath = ResolveVideoPath(step, job, variables);
                if (string.IsNullOrWhiteSpace(videoPath))
                    throw new InvalidOperationException($"Sản phẩm #{job.Id} chưa có VideoPath trong danh sách sản phẩm / Excel.");
                if (!File.Exists(videoPath))
                    throw new FileNotFoundException($"Video không tồn tại: {videoPath}");
                var fileName = BuildRemoteVideoName(job, Path.GetFileName(videoPath));
                var remotePath = $"/sdcard/DCIM/Camera/{fileName}";
                if (step.ClearDeviceVideosBeforeUpload)
                    await _adb.ClearFlowPilotVideosAsync(device);
                Logger.Info($"[VIDEO] Job #{job.Id}: đẩy {videoPath} → {remotePath}");
                await _adb.PushFileAsync(device, videoPath, remotePath);
                await _adb.TriggerMediaScanAsync(device, remotePath);
                break;

            case StepType.Delay:
                await Task.Delay(step.DelayAfterMs, ct);
                break;

            case StepType.Swipe:
                await _adb.SwipeAsync(device, step.X, step.Y, step.X2, step.Y2, step.SwipeDurationMs);
                break;

            case StepType.OpenApp:
                await _adb.OpenAppAsync(device, ResolveText(step.TextValue, job, variables));
                break;

            case StepType.KeyEvent:
                await _adb.SendKeyEventAsync(device, ResolveText(step.TextValue, job, variables));
                break;

            case StepType.MediaScan:
                var scanPath = string.IsNullOrEmpty(step.TextValue)
                    ? "/sdcard/DCIM/Camera/"
                    : ResolveText(step.TextValue, job, variables);
                await _adb.TriggerMediaScanAsync(device, scanPath);
                break;

            case StepType.AdbShell:
                var command = ResolveText(step.TextValue, job, variables);
                var output = await _adb.ShellAsync(device, command);
                Logger.Info($"[ADB Shell] {output}");
                break;
        }
    }

    private static string ResolveVideoPath(WorkflowStep step, JobItem job, IReadOnlyList<WorkflowVariable>? variables)
    {
        var path = step.VideoSource switch
        {
            VideoSourceMode.FolderAndExcelFileName => Path.Combine(
                ResolveText(step.VideoFolderPath, job, variables),
                Path.GetFileName(job.VideoPath)),
            VideoSourceMode.FixedFile => ResolveText(step.VideoFilePath, job, variables),
            // Chế độ Excel luôn lấy đúng video của từng dòng job, không dùng lại path của step.
            VideoSourceMode.ExcelPath => job.VideoPath,
            _ => job.VideoPath
        };

        return NormalizeLocalPath(path);
    }

    private static string BuildRemoteVideoName(JobItem job, string sourceFileName)
    {
        var extension = Path.GetExtension(sourceFileName);
        var stem = Path.GetFileNameWithoutExtension(sourceFileName);
        var safeStem = new string(stem
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_')
            .ToArray());
        if (string.IsNullOrWhiteSpace(safeStem)) safeStem = "video";
        if (safeStem.Length > 80) safeStem = safeStem[..80];
        return $"flowpilot_{job.Id}_{safeStem}{extension}";
    }

    private static string DeleteLocalVideosAfterSuccess(
        IReadOnlyList<WorkflowStep> steps,
        IReadOnlyList<JobItem> jobs,
        int currentIndex,
        JobItem currentJob,
        IReadOnlyList<WorkflowVariable>? variables)
    {
        var deleted = new List<string>();
        foreach (var step in steps.Where(item => item.Type == StepType.PushVideo && item.DeleteLocalVideoAfterSuccess))
        {
            var path = ResolveVideoPath(step, currentJob, variables);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;

            var isUsedByPendingJob = jobs
                .Skip(currentIndex + 1)
                .Where(item => item.Status != "Thành công" && item.ShopeeStatus != "Đã up Shopee")
                .Any(item => string.Equals(ResolveVideoPath(step, item, variables), path, StringComparison.OrdinalIgnoreCase));
            if (isUsedByPendingJob) continue;

            try
            {
                File.Delete(path);
                deleted.Add(Path.GetFileName(path));
                Logger.Info($"[VIDEO] Đã xóa file nguồn sau khi hoàn tất: {path}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"[VIDEO] Không thể xóa file nguồn {path}: {ex.Message}");
            }
        }

        return deleted.Count == 0
            ? "Hoàn tất"
            : $"Hoàn tất · Đã xóa {string.Join(", ", deleted)}";
    }

    private static string ResolveOpenAppPackage(
        IReadOnlyList<WorkflowStep> steps,
        JobItem job,
        IReadOnlyList<WorkflowVariable>? variables)
    {
        var openAppStep = steps.FirstOrDefault(step => step.Type == StepType.OpenApp);
        if (openAppStep == null)
            return string.Empty;

        return ResolveText(openAppStep.TextValue, job, variables).Trim();
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
        var result = template
            .Replace("{Title}", job.Title)
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

    private static string ResolveInputText(
        WorkflowStep step,
        JobItem job,
        IReadOnlyList<WorkflowVariable>? variables)
    {
        var input = step.TextValue.Trim();
        if (string.IsNullOrWhiteSpace(step.BindingColumn))
        {
            // Cho phép nhập thẳng tên cột: Description hoặc {Description}.
            if (job.HasColumn(input))
                return job.GetColumnValue(input);

            var resolved = ResolveText(step.TextValue, job, variables);
            return Regex.Replace(resolved, @"\{([^{}]+)\}", match =>
            {
                var columnName = match.Groups[1].Value.Trim();
                return job.HasColumn(columnName)
                    ? job.GetColumnValue(columnName)
                    : match.Value;
            });
        }

        var columnValue = job.GetColumnValue(step.BindingColumn);
        if (string.IsNullOrWhiteSpace(step.TextValue))
            return columnValue;

        return ResolveText(step.TextValue, job, variables)
            .Replace("{Value}", columnValue);
    }
}
