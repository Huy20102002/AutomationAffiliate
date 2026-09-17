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
        IReadOnlyList<WorkflowStep> steps,
        JobItem job,
        DeviceData device,
        CancellationToken ct,
        IProgress<string>? progress = null,
        IReadOnlyList<WorkflowVariable>? variables = null,
        Action<int>? onStepStarted = null,
        string targetPlatform = "Shopee",
        bool isNotFirstJob = false,
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
            var isSkippedOpenApp = isNotFirstJob && step.Type == StepType.OpenApp && step.SkipFromSecondJob;
            var stepInfo = $"[{i + 1}/{steps.Count}] {step.GetDisplayText()}";
            if (isSkippedOpenApp)
                stepInfo += " · bỏ qua từ job thứ 2";
            progress?.Report(stepInfo);
            Logger.Info($"Job #{job.Id}: {stepInfo}");
            if (isSkippedOpenApp)
                continue;

            onStepStarted?.Invoke(i);

            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await ExecuteStepAsync(step, job, device, ct, variables);
                    break;
                }
                catch (Exception ex) when (ex.Message.Contains("no device with transport id", StringComparison.OrdinalIgnoreCase))
                {
                    if (attempt == maxRetries) throw;
                    Logger.Warn($"[ADB] Thiết bị mất kết nối (transport id thay đổi), đang thử kết nối lại...");
                    await Task.Delay(2000, ct);
                    var newDevice = await _adb.GetDeviceBySerialAsync(device.Serial);
                    if (newDevice != null)
                    {
                        device = newDevice;
                        Logger.Info($"[ADB] Đã lấy lại kết nối thiết bị {device.Serial}, thử lại bước hiện tại...");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Không thể tìm lại thiết bị {device.Serial} sau khi mất kết nối ADB.", ex);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is FileNotFoundException || ex is InvalidDataException || (ex is InvalidOperationException && ex.Message.Contains("VideoPath")) || ex is UiAutomationNotReadyException || ex is OperationCanceledException)
                    {
                        throw;
                    }

                    if (attempt == maxRetries)
                    {
                        throw;
                    }

                    progress?.Report($"[Lỗi] {ex.Message} - Thử lại ({attempt}/{maxRetries})...");
                    Logger.Warn($"Job #{job.Id}: Lỗi bước '{step.GetDisplayText()}': {ex.Message}. Đang thử lại lần {attempt}/{maxRetries}...");
                    await Task.Delay(2000, ct);
                }
            }

            if (step.Type != StepType.Delay && step.DelayAfterMs > 0)
            {
                var actualDelay = step.DelayAfterMs;
                if (step.DelayMaxMs.HasValue && step.DelayMaxMs.Value > step.DelayAfterMs) {
                    actualDelay = new Random().Next(step.DelayAfterMs, step.DelayMaxMs.Value + 1);
                }
                await Task.Delay(actualDelay, ct);
            }
        }
        return device;
    }

    public async Task<DeviceData> RunAllJobsAsync(
        IReadOnlyList<WorkflowStep> steps,
        IReadOnlyList<JobItem> jobs,
        DeviceData device,
        CancellationToken ct,
        Action<int, string, string>? onJobUpdate = null,
        string targetPlatform = "Shopee",
        IReadOnlyList<WorkflowVariable>? variables = null,
        Action<int>? onStepStarted = null,
        bool skipOpenAppAfterFirstJob = true,
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
            
            if (JobStatus.IsCompleted(job, targetPlatform)) continue;

            var recoveryAttempted = false;
            try
            {
                if (preJobAction != null)
                {
                    onJobUpdate?.Invoke(i, "Đang chạy...", "[AI] Đang sinh tiêu đề chuẩn SEO...");
                    await preJobAction(job);
                }
                job.Status = JobStatus.Running;
                job.Log = string.Empty;
                onJobUpdate?.Invoke(i, job.Status, string.Empty);

                var progress = new Progress<string>(message =>
                {
                    job.Log = message;
                    onJobUpdate?.Invoke(i, JobStatus.Running, message);
                });

                device = await ExecuteWorkflowAsync(
                    steps,
                    job,
                    device,
                    ct,
                    progress,
                    variables,
                    onStepStarted,
                    isNotFirstJob: appOpenedInThisRun);
                if (hasOpenAppStep)
                    appOpenedInThisRun = true;
                JobStatus.MarkCompleted(job, targetPlatform);
                job.Status = JobStatus.Succeeded;
                job.Log = DeleteLocalVideosAfterSuccess(steps, jobs, i, job, variables, targetPlatform);
                onJobUpdate?.Invoke(i, job.Status, job.Log);
                Logger.Info($"Job #{job.Id}: SUCCESS");

                if (i < jobs.Count - 1 && (delayBetweenJobsMinMinutes > 0 || delayBetweenJobsMaxMinutes > 0))
                {
                    var delayMin = Math.Min(delayBetweenJobsMinMinutes, delayBetweenJobsMaxMinutes);
                    var delayMax = Math.Max(delayBetweenJobsMinMinutes, delayBetweenJobsMaxMinutes);
                    var randomDelayMinutes = new Random().Next(delayMin, delayMax + 1);
                    if (randomDelayMinutes > 0)
                    {
                        var delayMs = randomDelayMinutes * 60 * 1000;
                        Logger.Info($"Đang chờ {randomDelayMinutes} phút trước khi chạy video tiếp theo...");
                        onJobUpdate?.Invoke(i, JobStatus.Succeeded, $"Chờ {randomDelayMinutes} phút...");
                        await Task.Delay(delayMs, ct);
                    }
                }
            }
            catch (UiAutomationNotReadyException ex) when (!recoveryAttempted)
            {
                recoveryAttempted = true;
                job.Status = JobStatus.Retrying;
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
                    job.Status = JobStatus.RunningAgain;
                    job.Log = string.Empty;
                    onJobUpdate?.Invoke(i, job.Status, string.Empty);

                    var retryProgress = new Progress<string>(message =>
                    {
                        job.Log = message;
                        onJobUpdate?.Invoke(i, JobStatus.RunningAgain, message);
                    });

                    device = await ExecuteWorkflowAsync(
                        steps,
                        job,
                        device,
                        ct,
                        retryProgress,
                        variables,
                        onStepStarted,
                        isNotFirstJob: false);
                    if (hasOpenAppStep)
                        appOpenedInThisRun = true;
                    JobStatus.MarkCompleted(job, targetPlatform);
                    job.Status = JobStatus.Succeeded;
                    job.Log = DeleteLocalVideosAfterSuccess(steps, jobs, i, job, variables, targetPlatform);
                    onJobUpdate?.Invoke(i, job.Status, job.Log);
                    Logger.Info($"Job #{job.Id}: SUCCESS after recovery");
                }
                catch (OperationCanceledException)
                {
                    job.Status = JobStatus.Stopped;
                    job.Log = "Bị dừng bởi người dùng";
                    onJobUpdate?.Invoke(i, job.Status, job.Log);
                    throw;
                }
                catch (Exception retryEx)
                {
                    job.Status = JobStatus.Failed;
                    job.Log = retryEx.Message;
                    onJobUpdate?.Invoke(i, job.Status, retryEx.Message);
                    Logger.Error($"Job #{job.Id}: FAILED after recovery", retryEx);

                    // Thoát app và vào lại khi gặp lỗi để reset trạng thái sạch sẽ
                    await RestartAppOnFailureAsync(steps, job, variables, device, ct);
                    appOpenedInThisRun = true;
                }
            }
            catch (OperationCanceledException)
            {
                job.Status = JobStatus.Stopped;
                job.Log = "Bị dừng bởi người dùng";
                onJobUpdate?.Invoke(i, job.Status, job.Log);
                throw;
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException || (ex is InvalidOperationException && ex.Message.Contains("VideoPath")))
            {
                job.Status = JobStatus.Failed;
                job.Log = ex.Message;
                onJobUpdate?.Invoke(i, job.Status, ex.Message);
                Logger.Error($"Job #{job.Id}: VIDEO FAILED", ex);

                // Thoát app và vào lại khi gặp lỗi video
                await RestartAppOnFailureAsync(steps, job, variables, device, ct);
                appOpenedInThisRun = true;
            }
            catch (Exception ex)
            {
                job.Status = JobStatus.Failed;
                job.Log = ex.Message;
                onJobUpdate?.Invoke(i, job.Status, ex.Message);
                Logger.Error($"Job #{job.Id}: FAILED", ex);

                // Thoát app và vào lại khi gặp bất kỳ lỗi nào trong workflow
                await RestartAppOnFailureAsync(steps, job, variables, device, ct);
                appOpenedInThisRun = true;
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
                if (step.TapMode == TapMode.XPath && step.TapMultiMode != TapMultiMode.Single)
                {
                    var resolvedXpath = ResolveText(step.TapXPath, job, variables);
                    int targetCount = step.TapMultiMode switch
                    {
                        TapMultiMode.ByImageCount => Math.Max(1, ResolveImagePaths(step, job, variables).Count),
                        TapMultiMode.CustomCount => Math.Max(1, step.TapCustomCount),
                        _ => -1 // All
                    };

                    var points = await _adb.FindUiNodesCenterAsync(device, resolvedXpath, targetCount, ct);
                    if (points == null || points.Count == 0)
                        throw new InvalidOperationException($"Không tìm thấy phần tử nào theo XPath: {resolvedXpath}");

                    Logger.Info($"[XPATH] Chạm {points.Count} phần tử theo XPath: {resolvedXpath} (chế độ: {step.TapMultiMode})");
                    for (int i = 0; i < points.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        await _adb.TapAsync(device, points[i].X, points[i].Y);
                        if (i < points.Count - 1)
                            await Task.Delay(step.MultiTapDelayMs > 0 ? step.MultiTapDelayMs : 250, ct);
                    }
                }
                else
                {
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
                }
                break;

            case StepType.RandomTap:
                Point randomPoint;
                if (step.RandomCoordinates != null && step.RandomCoordinates.Count > 0)
                {
                    int chosenIdx = Random.Shared.Next(step.RandomCoordinates.Count);
                    randomPoint = step.RandomCoordinates[chosenIdx];
                    Logger.Info($"[CHẠM NGẪU NHIÊN] Job #{job.Id}: Chọn ngẫu nhiên tọa độ ({randomPoint.X}, {randomPoint.Y}) [Điểm {chosenIdx + 1}/{step.RandomCoordinates.Count}]");
                }
                else
                {
                    randomPoint = new Point(step.X, step.Y);
                    Logger.Info($"[CHẠM NGẪU NHIÊN] Job #{job.Id}: Nhóm rỗng, sử dụng tọa độ mặc định ({randomPoint.X}, {randomPoint.Y})");
                }
                await _adb.TapAsync(device, randomPoint.X, randomPoint.Y);
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
                        Logger.Info($"[AI] Video #{job.Id} đã được tạo tiêu đề AI trước đó ('{inputText}'), bỏ qua không gọi AI.");
                    }
                    else
                    {
                        var configService = new AiConfigService();
                        var config = configService.Load();
                        if (!string.IsNullOrWhiteSpace(config.ApiKey))
                        {
                            var originalTitle = inputText;
                            var aiService = new AiTitleService();
                            Logger.Info($"[AI] Đang sinh nội dung bằng AI cho video #{job.Id}...");
                            try
                            {
                                var aiResult = await aiService.GenerateTitleAsync(config, originalTitle);
                                if (!string.IsNullOrWhiteSpace(aiResult) && !string.Equals(aiResult.Trim(), originalTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    inputText = aiResult.Trim();
                                    job.IsAiTitleGenerated = true;
                                    Logger.Info($"[AI] Sinh tiêu đề AI thành công: '{inputText}'");
                                }
                                else
                                {
                                    inputText = !string.IsNullOrWhiteSpace(originalTitle) ? originalTitle : oldTitle;
                                    Logger.Warn($"[AI] Không sinh được tiêu đề AI mới, tự động dùng tiêu đề cũ: '{inputText}'");
                                }
                            }
                            catch (Exception ex)
                            {
                                inputText = !string.IsNullOrWhiteSpace(originalTitle) ? originalTitle : oldTitle;
                                Logger.Warn($"[AI] Lỗi kết nối API AI ({ex.Message}), tự động dùng tiêu đề cũ: '{inputText}'");
                            }
                        }
                        else
                        {
                            Logger.Info($"[AI] Chưa cấu hình API Key, dùng tiêu đề cũ: '{inputText}'");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(inputText))
                {
                    inputText = !string.IsNullOrWhiteSpace(oldTitle) ? oldTitle : (job.Title ?? string.Empty);
                }

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
                if (!IsValidVideoFile(videoPath))
                    throw new InvalidDataException($"File video bị lỗi định dạng hoặc không thể đọc được. Vui lòng kiểm tra lại file gốc: {videoPath}");
                var fileName = BuildRemoteVideoName(job, Path.GetFileName(videoPath));
                var remotePath = $"/sdcard/DCIM/Camera/{fileName}";
                if (step.ClearDeviceVideosBeforeUpload)
                    await _adb.ClearFlowPilotVideosAsync(device);
                Logger.Info($"[VIDEO] Job #{job.Id}: đẩy {videoPath} → {remotePath}");
                await _adb.PushFileAsync(device, videoPath, remotePath);
                await _adb.TriggerMediaScanAsync(device, remotePath);
                break;

            case StepType.PushImage:
                var imagePaths = ResolveImagePaths(step, job, variables);
                if (imagePaths.Count == 0)
                    throw new InvalidOperationException($"Sản phẩm #{job.Id} chưa có đường dẫn ảnh (ImagePath / VideoPath) trong danh sách sản phẩm / Excel.");

                foreach (var imgPath in imagePaths)
                {
                    if (!File.Exists(imgPath))
                        throw new FileNotFoundException($"Ảnh không tồn tại: {imgPath}");
                    if (!IsValidImageFile(imgPath))
                        throw new InvalidDataException($"File ảnh không đúng định dạng hoặc bị lỗi: {imgPath}");
                }

                if (step.ClearDeviceVideosBeforeUpload)
                    await _adb.ClearFlowPilotImagesAsync(device);

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
                    var imgRemotePath = $"/sdcard/DCIM/Camera/{imgFileName}";
                    Logger.Info($"[IMAGE] Job #{job.Id}: đẩy {imgPath} → {imgRemotePath}");
                    await _adb.PushFileAsync(device, imgPath, imgRemotePath);
                    await _adb.TriggerMediaScanAsync(device, imgRemotePath);
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
        return $"flowpilot_{job.Id}_{safeStem}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
    }

    private static string DeleteLocalVideosAfterSuccess(
        IReadOnlyList<WorkflowStep> steps,
        IReadOnlyList<JobItem> jobs,
        int currentIndex,
        JobItem currentJob,
        IReadOnlyList<WorkflowVariable>? variables,
        string targetPlatform)
    {
        var deleted = new List<string>();
        foreach (var step in steps.Where(item => (item.Type == StepType.PushVideo || item.Type == StepType.PushImage) && item.DeleteLocalVideoAfterSuccess))
        {
            var paths = step.Type == StepType.PushImage
                ? ResolveImagePaths(step, currentJob, variables)
                : new List<string> { ResolveVideoPath(step, currentJob, variables) };

            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;

                var isUsedByPendingJob = jobs
                    .Skip(currentIndex + 1)
                    .Where(item => !JobStatus.IsCompleted(item, targetPlatform))
                    .Any(item => step.Type == StepType.PushImage
                        ? ResolveImagePaths(step, item, variables).Contains(path, StringComparer.OrdinalIgnoreCase)
                        : string.Equals(ResolveVideoPath(step, item, variables), path, StringComparison.OrdinalIgnoreCase));
                if (isUsedByPendingJob) continue;

                try
                {
                    File.Delete(path);
                    deleted.Add(Path.GetFileName(path));
                    Logger.Info($"[MEDIA] Đã xóa file nguồn sau khi hoàn tất: {path}");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[MEDIA] Không thể xóa file nguồn {path}: {ex.Message}");
                }
            }
        }

        return deleted.Count == 0
            ? "Hoàn tất"
            : $"Hoàn tất · Đã xóa {string.Join(", ", deleted)}";
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

    private async Task RestartAppOnFailureAsync(
        IReadOnlyList<WorkflowStep> steps,
        JobItem job,
        IReadOnlyList<WorkflowVariable>? variables,
        DeviceData device,
        CancellationToken ct)
    {
        var packageName = ResolveOpenAppPackage(steps, job, variables);
        if (string.IsNullOrWhiteSpace(packageName))
            packageName = "com.shopee.vn";

        try
        {
            Logger.Warn($"[Tự phục hồi] Video #{job.Id} gặp sự cố! Đang thoát app {packageName} và mở lại để reset trạng thái sạch sẽ...");
            await _adb.ForceStopAppAsync(device, packageName);
            await Task.Delay(1000, ct);
            try { await _adb.SendKeyEventAsync(device, "KEYCODE_HOME"); } catch { }
            await Task.Delay(1000, ct);
            await _adb.OpenAppAsync(device, packageName);
            await Task.Delay(3000, ct);
            Logger.Info($"[Tự phục hồi] Đã khởi động lại {packageName} thành công, sẵn sàng cho video tiếp theo.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Tự phục hồi] Không thể khởi động lại app: {ex.Message}");
        }
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

        var result = template
            .Replace("{Title}", job.Title)
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

    private static string ResolveInputText(
        WorkflowStep step,
        JobItem job,
        IReadOnlyList<WorkflowVariable>? variables)
    {
        var input = step.TextValue.Trim();
        var takeAll = step.TakeAllLinks;
        if (string.IsNullOrWhiteSpace(step.BindingColumn))
        {
            // Cho phép nhập thẳng tên cột: Description hoặc {Description}.
            if (job.HasColumn(input))
                return job.GetColumnValue(input, takeAll);

            var resolved = ResolveText(step.TextValue, job, variables, takeAll);
            return Regex.Replace(resolved, @"\{([^{}]+)\}", match =>
            {
                var columnName = match.Groups[1].Value.Trim();
                return job.HasColumn(columnName)
                    ? job.GetColumnValue(columnName, takeAll)
                    : match.Value;
            });
        }

        var columnValue = job.GetColumnValue(step.BindingColumn, takeAll);
        if (string.IsNullOrWhiteSpace(step.TextValue))
            return columnValue;

        return ResolveText(step.TextValue, job, variables, takeAll)
            .Replace("{Value}", columnValue);
    }
}
