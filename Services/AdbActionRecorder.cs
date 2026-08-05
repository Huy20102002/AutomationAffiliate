using System.Globalization;
using System.Text.RegularExpressions;
using AdvancedSharpAdbClient.Models;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

/// <summary>
/// Chuyển sự kiện input thô của Android thành các bước Tap, Swipe và KeyEvent.
/// Scrcpy không phát ra lịch sử thao tác cho ứng dụng desktop; nguồn dữ liệu
/// chính xác cần lấy từ luồng getevent qua ADB.
/// </summary>
public sealed class AdbActionRecorder
{
    private readonly AdbManager _adb;

    public AdbActionRecorder(AdbManager adb) => _adb = adb;

    public Task RecordAsync(DeviceData device, Action<WorkflowStep> onAction, CancellationToken ct)
    {
        var parser = new InputEventParser(onAction);
        return _adb.StreamInputEventsAsync(device, parser.ProcessLine, ct);
    }

    /// <summary>Chờ một lần chạm trên thiết bị rồi trả về bước Tap tương ứng.</summary>
    public async Task<WorkflowStep?> CaptureNextTapAsync(DeviceData device, CancellationToken ct)
    {
        WorkflowStep? captured = null;
        using var captureCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            await RecordAsync(device, step =>
            {
                if (captured != null || step.Type != StepType.Tap) return;
                captured = step;
                captureCts.Cancel();
            }, captureCts.Token);
        }
        catch (OperationCanceledException) when (captured != null || ct.IsCancellationRequested)
        {
            // Hủy luồng sau khi đã nhận đủ một lần chạm hoặc người dùng dừng bắt.
        }

        return captured;
    }

    private sealed class InputEventParser
    {
        private static readonly Regex PositionRegex = new(
            @"\b(?<axis>ABS_MT_POSITION_X|ABS_MT_POSITION_Y)\s+(?<value>[0-9a-fA-F]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly Action<WorkflowStep> _onAction;
        private bool _touching;
        private int _startX = -1;
        private int _startY = -1;
        private int _lastX = -1;
        private int _lastY = -1;

        public InputEventParser(Action<WorkflowStep> onAction) => _onAction = onAction;

        public void ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            var position = PositionRegex.Match(line);
            if (position.Success && TryHex(position.Groups["value"].Value, out var value))
            {
                if (position.Groups["axis"].Value.EndsWith("_X", StringComparison.Ordinal))
                    _lastX = value;
                else
                    _lastY = value;

                if (_touching && (_startX < 0 || _startY < 0) && _lastX >= 0 && _lastY >= 0)
                {
                    _startX = _lastX;
                    _startY = _lastY;
                }
            }

            if (line.Contains("BTN_TOUCH DOWN", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("ABS_MT_TRACKING_ID") && !line.Contains("ffffffff", StringComparison.OrdinalIgnoreCase))
            {
                _touching = true;
                _startX = -1;
                _startY = -1;
                return;
            }

            if (line.Contains("BTN_TOUCH UP", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("ABS_MT_TRACKING_ID") && line.Contains("ffffffff", StringComparison.OrdinalIgnoreCase))
            {
                FinishTouch();
                return;
            }

            if (line.Contains("EV_KEY", StringComparison.OrdinalIgnoreCase) &&
                line.Contains(" DOWN", StringComparison.OrdinalIgnoreCase))
            {
                var key = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(token => token.StartsWith("KEY_", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(key) && !key.Equals("KEY_RESERVED", StringComparison.OrdinalIgnoreCase))
                {
                    _onAction(new WorkflowStep
                    {
                        Type = StepType.KeyEvent,
                        TextValue = key,
                        DelayAfterMs = 300,
                        Description = $"Phím {key}"
                    });
                }
            }
        }

        private void FinishTouch()
        {
            if (!_touching)
                return;

            if (_startX >= 0 && _startY >= 0 && _lastX >= 0 && _lastY >= 0)
            {
                var distance = Math.Abs(_lastX - _startX) + Math.Abs(_lastY - _startY);
                var step = distance >= 24
                    ? new WorkflowStep
                    {
                        Type = StepType.Swipe,
                        X = _startX,
                        Y = _startY,
                        X2 = _lastX,
                        Y2 = _lastY,
                        SwipeDurationMs = 300,
                        DelayAfterMs = 500,
                        Description = $"Vuốt ({_startX},{_startY}) → ({_lastX},{_lastY})"
                    }
                    : new WorkflowStep
                    {
                        Type = StepType.Tap,
                        X = _lastX,
                        Y = _lastY,
                        DelayAfterMs = 500,
                        Description = $"Chạm ({_lastX},{_lastY})"
                    };
                _onAction(step);
            }

            _touching = false;
            _startX = _startY = _lastX = _lastY = -1;
        }

        private static bool TryHex(string text, out int value)
        {
            if (uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed) && parsed <= int.MaxValue)
            {
                value = (int)parsed;
                return true;
            }

            value = -1;
            return false;
        }
    }
}
