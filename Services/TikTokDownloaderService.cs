using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

/// <summary>
/// Lists and downloads TikTok videos through the locally installed yt-dlp executable.
/// The optional proxy is passed only to yt-dlp for the current operation.
/// </summary>
public sealed class TikTokDownloaderService
{
    private readonly string _executablePath;

    public TikTokDownloaderService(string? executablePath = null)
    {
        _executablePath = ResolveExecutable(executablePath);
    }

    public string ExecutablePath => _executablePath;

    public async Task<IReadOnlyList<TikTokVideoItem>> ListChannelVideosAsync(
        string channelIdOrUrl,
        string? proxy,
        CancellationToken ct)
    {
        var channelUrl = NormalizeChannelUrl(channelIdOrUrl);
        var arguments = new List<string>
        {
            "--flat-playlist",
            "--dump-single-json",
            "--skip-download",
            "--no-warnings",
            "--ignore-errors",
            channelUrl
        };
        AddProxy(arguments, proxy);

        var result = await RunAsync(arguments, ct);
        if (string.IsNullOrWhiteSpace(result.StdOut))
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StdErr)
                ? "Không lấy được danh sách video từ kênh TikTok."
                : result.StdErr.Trim());

        try
        {
            using var document = JsonDocument.Parse(result.StdOut);
            var root = document.RootElement;
            var entries = new List<JsonElement>();
            if (root.ValueKind == JsonValueKind.Array)
            {
                entries.AddRange(root.EnumerateArray());
            }
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("entries", out var entryElement))
            {
                if (entryElement.ValueKind == JsonValueKind.Array)
                {
                    entries.AddRange(entryElement.EnumerateArray());
                }
                else if (entryElement.ValueKind == JsonValueKind.Null)
                {
                    var detail = string.IsNullOrWhiteSpace(result.StdErr)
                        ? "TikTok không trả về danh sách video cho kênh này."
                        : result.StdErr.Trim();
                    throw new InvalidOperationException($"Không lấy được video từ kênh TikTok. {Shorten(detail)}");
                }
                else
                {
                    throw new InvalidOperationException("Dữ liệu danh sách video từ TikTok không đúng định dạng.");
                }
            }
            else if (root.ValueKind == JsonValueKind.Object && !string.IsNullOrWhiteSpace(GetString(root, "id")))
            {
                entries.Add(root);
            }
            else
            {
                var detail = string.IsNullOrWhiteSpace(result.StdErr)
                    ? "TikTok không trả về dữ liệu video hợp lệ."
                    : result.StdErr.Trim();
                throw new InvalidOperationException($"Không đọc được dữ liệu TikTok. {Shorten(detail)}");
            }

            var videos = new List<TikTokVideoItem>();
            foreach (var entry in entries)
            {
                if (entry.ValueKind != JsonValueKind.Object) continue;

                var id = GetString(entry, "id");
                var url = GetString(entry, "webpage_url");
                if (string.IsNullOrWhiteSpace(url)) url = GetString(entry, "original_url");
                if (string.IsNullOrWhiteSpace(url) && GetString(entry, "url").StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    url = GetString(entry, "url");
                if (string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(id))
                    url = $"https://www.tiktok.com/@{ExtractUsername(channelUrl)}/video/{id}";
                if (string.IsNullOrWhiteSpace(url)) continue;

                videos.Add(new TikTokVideoItem
                {
                    VideoId = id,
                    Title = GetString(entry, "title") is { Length: > 0 } title ? title : id,
                    Url = url,
                    DurationText = FormatDuration(entry),
                    ThumbnailUrl = GetString(entry, "thumbnail")
                });
            }

            if (videos.Count == 0)
                throw new InvalidOperationException("Không tìm thấy video. Hãy kiểm tra lại @username hoặc URL kênh TikTok.");

            return videos
                .GroupBy(video => string.IsNullOrWhiteSpace(video.VideoId) ? video.Url : video.VideoId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("yt-dlp trả về dữ liệu không hợp lệ. Hãy kiểm tra phiên bản yt-dlp và URL kênh.", ex);
        }
    }

    public async Task DownloadAsync(
        IReadOnlyList<TikTokVideoItem> videos,
        string outputDirectory,
        string? proxy,
        Action<TikTokVideoItem, string>? onStatus,
        CancellationToken ct)
    {
        Directory.CreateDirectory(outputDirectory);

        foreach (var video in videos)
        {
            ct.ThrowIfCancellationRequested();
            onStatus?.Invoke(video, "Đang tải...");

            var outputTemplate = Path.Combine(outputDirectory, "%(upload_date)s_%(id)s_%(title)s.%(ext)s");
            var arguments = new List<string>
            {
                "--no-warnings",
                "--no-playlist",
                "--newline",
                "--windows-filenames",
                "--trim-filenames", "120",
                "--format", "best[ext=mp4]/best",
                "--output", outputTemplate,
                video.Url
            };
            AddProxy(arguments, proxy);

            try
            {
                var result = await RunAsync(arguments, ct);
                if (result.ExitCode != 0)
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StdErr)
                        ? $"yt-dlp lỗi với mã {result.ExitCode}."
                        : result.StdErr.Trim());

                onStatus?.Invoke(video, "Đã tải");
                Logger.Info($"[TikTok] Đã tải video {video.VideoId} vào {outputDirectory}");
            }
            catch (OperationCanceledException)
            {
                onStatus?.Invoke(video, "Đã dừng");
                throw;
            }
            catch (Exception ex)
            {
                onStatus?.Invoke(video, $"Lỗi: {Shorten(ex.Message)}");
                Logger.Warn($"[TikTok] Không tải được {video.Url}: {ex.Message}");
            }
        }
    }

    public static string NormalizeChannelUrl(string value)
    {
        var input = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Hãy nhập ID hoặc username kênh TikTok.", nameof(value));

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return uri.ToString().TrimEnd('/');

        var username = input.Trim().TrimStart('@', '/');
        if (username.Contains('/') || username.Contains(' '))
            throw new ArgumentException("ID kênh không hợp lệ. Nhập username hoặc URL TikTok.", nameof(value));

        return $"https://www.tiktok.com/@{username}";
    }

    private static void AddProxy(List<string> arguments, string? proxy)
    {
        if (!string.IsNullOrWhiteSpace(proxy))
        {
            arguments.Add("--proxy");
            arguments.Add(proxy.Trim());
        }
    }

    private async Task<ProcessResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        if (!process.Start())
            throw new InvalidOperationException("Không thể khởi động yt-dlp.");

        using var registration = ct.Register(() =>
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch { }
        });

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static string ResolveExecutable(string? configuredPath)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configuredPath)) candidates.Add(configuredPath.Trim().Trim('"'));
        candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe"));
        candidates.Add("yt-dlp.exe");
        candidates.Add("yt-dlp");

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate)) return candidate;
            if (!Path.IsPathFullyQualified(candidate))
            {
                try
                {
                    using var probe = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = candidate,
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    if (probe.Start())
                    {
                        probe.WaitForExit(1500);
                        if (probe.HasExited && probe.ExitCode == 0) return candidate;
                    }
                }
                catch { }
            }
        }

        throw new FileNotFoundException("Không tìm thấy yt-dlp.exe. Hãy đặt yt-dlp.exe cạnh file chương trình hoặc thêm yt-dlp vào PATH.");
    }

    private static string ExtractUsername(string channelUrl)
    {
        var marker = "/@";
        var index = channelUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return "user";
        var username = channelUrl[(index + marker.Length)..].Trim('/');
        var slash = username.IndexOf('/');
        return slash >= 0 ? username[..slash] : username;
    }

    private static string GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string FormatDuration(JsonElement element)
    {
        if (!element.TryGetProperty("duration", out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var seconds))
            return string.Empty;
        return $"{seconds / 60}:{seconds % 60:00}";
    }

    private static string Shorten(string value)
        => value.Length > 160 ? value[..160] + "..." : value;

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
}
