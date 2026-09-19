using System.Runtime.InteropServices;

namespace ShopeeVideoUploader.Services;

/// <summary>
/// Bắt một lần click trên cửa sổ scrcpy rồi quy đổi từ tọa độ cửa sổ sang tọa độ màn hình Android.
/// Dùng làm fallback cho các máy không cho shell đọc thiết bị touch bằng getevent.
/// </summary>
public sealed class ScrcpyMouseCapture
{
    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const uint GaRoot = 2;

    private delegate IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseHookStruct
    {
        public Point Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    public sealed record CapturedClick(int X, int Y, string WindowTitle);
    public sealed record CapturedSwipe(int StartX, int StartY, int EndX, int EndY, string WindowTitle);

    public IntPtr EmbeddedScrcpyHwnd { get; set; } = IntPtr.Zero;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, MouseHookCallback callback, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr handle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr handle, char[] text, int maxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr handle, ref Point point);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    private const int WmLButtonUp = 0x0202;

    public async Task<CapturedSwipe?> CaptureNextSwipeAsync(
        string expectedWindowTitle,
        int screenWidth,
        int screenHeight,
        CancellationToken cancellationToken)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(screenWidth), "Chưa có độ phân giải màn hình thiết bị.");

        var completion = new TaskCompletionSource<CapturedSwipe?>(TaskCreationOptions.RunContinuationsAsynchronously);
        MouseHookCallback? callback = null;
        IntPtr hook = IntPtr.Zero;

        Point? startPoint = null;

        callback = (code, message, data) =>
        {
            if (code >= 0)
            {
                if (message == (IntPtr)WmLButtonDown)
                {
                    var mouse = Marshal.PtrToStructure<MouseHookStruct>(data);
                    var directWnd = WindowFromPoint(mouse.Point);
                    var isEmbedded = EmbeddedScrcpyHwnd != IntPtr.Zero && 
                        (directWnd == EmbeddedScrcpyHwnd || GetAncestor(directWnd, 1) == EmbeddedScrcpyHwnd || GetAncestor(directWnd, 2) == EmbeddedScrcpyHwnd);

                    if (isEmbedded && TryMapToDevice(EmbeddedScrcpyHwnd, mouse.Point, screenWidth, screenHeight, out var pEmb))
                    {
                        startPoint = pEmb;
                    }
                    else
                    {
                        var root = GetAncestor(directWnd, GaRoot);
                        var title = ReadWindowTitle(root);
                        var isFlowPilot = title.Contains("FlowPilot", StringComparison.OrdinalIgnoreCase) ||
                                          title.Contains("Multi-Platform Studio", StringComparison.OrdinalIgnoreCase);

                        if (root != IntPtr.Zero && !isFlowPilot &&
                            TryMapToDevice(root, mouse.Point, screenWidth, screenHeight, out var point))
                        {
                            startPoint = point;
                        }
                    }
                }
                else if (message == (IntPtr)WmLButtonUp && startPoint.HasValue)
                {
                    var mouse = Marshal.PtrToStructure<MouseHookStruct>(data);
                    var directWnd = WindowFromPoint(mouse.Point);
                    var isEmbedded = EmbeddedScrcpyHwnd != IntPtr.Zero && 
                        (directWnd == EmbeddedScrcpyHwnd || GetAncestor(directWnd, 1) == EmbeddedScrcpyHwnd || GetAncestor(directWnd, 2) == EmbeddedScrcpyHwnd);

                    if (isEmbedded && TryMapToDevice(EmbeddedScrcpyHwnd, mouse.Point, screenWidth, screenHeight, out var endEmb))
                    {
                        completion.TrySetResult(new CapturedSwipe(startPoint.Value.X, startPoint.Value.Y, endEmb.X, endEmb.Y, "Màn hình nhúng"));
                        startPoint = null;
                    }
                    else
                    {
                        var root = GetAncestor(directWnd, GaRoot);
                        var title = ReadWindowTitle(root);

                        if (root != IntPtr.Zero && TryMapToDevice(root, mouse.Point, screenWidth, screenHeight, out var endPoint))
                        {
                            completion.TrySetResult(new CapturedSwipe(startPoint.Value.X, startPoint.Value.Y, endPoint.X, endPoint.Y, title));
                            startPoint = null;
                        }
                    }
                }
            }

            return CallNextHookEx(hook, code, message, data);
        };

        hook = SetWindowsHookEx(WhMouseLl, callback, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero)
            throw new InvalidOperationException("Không thể bắt sự kiện vuốt trên cửa sổ scrcpy.");

        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try
        {
            return await completion.Task.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            UnhookWindowsHookEx(hook);
            GC.KeepAlive(callback);
        }
    }

    public async Task<CapturedClick?> CaptureNextClickAsync(
        string expectedWindowTitle,
        int screenWidth,
        int screenHeight,
        CancellationToken cancellationToken)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(screenWidth), "Chưa có độ phân giải màn hình thiết bị.");

        var completion = new TaskCompletionSource<CapturedClick?>(TaskCreationOptions.RunContinuationsAsynchronously);
        MouseHookCallback? callback = null;
        IntPtr hook = IntPtr.Zero;

        callback = (code, message, data) =>
        {
            if (code >= 0 && message == (IntPtr)WmLButtonDown)
            {
                var mouse = Marshal.PtrToStructure<MouseHookStruct>(data);
                var directWnd = WindowFromPoint(mouse.Point);
                var isEmbedded = EmbeddedScrcpyHwnd != IntPtr.Zero && 
                    (directWnd == EmbeddedScrcpyHwnd || GetAncestor(directWnd, 1) == EmbeddedScrcpyHwnd || GetAncestor(directWnd, 2) == EmbeddedScrcpyHwnd);

                if (isEmbedded && TryMapToDevice(EmbeddedScrcpyHwnd, mouse.Point, screenWidth, screenHeight, out var pEmb))
                {
                    completion.TrySetResult(new CapturedClick(pEmb.X, pEmb.Y, "Màn hình nhúng"));
                }
                else
                {
                    var root = GetAncestor(directWnd, GaRoot);
                    var title = ReadWindowTitle(root);
                    var isFlowPilot = title.Contains("FlowPilot", StringComparison.OrdinalIgnoreCase) ||
                                      title.Contains("Multi-Platform Studio", StringComparison.OrdinalIgnoreCase);

                    if (root != IntPtr.Zero && !isFlowPilot &&
                        TryMapToDevice(root, mouse.Point, screenWidth, screenHeight, out var point))
                    {
                        completion.TrySetResult(new CapturedClick(point.X, point.Y, title));
                    }
                }
            }

            return CallNextHookEx(hook, code, message, data);
        };

        hook = SetWindowsHookEx(WhMouseLl, callback, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero)
            throw new InvalidOperationException("Không thể bắt click trên cửa sổ scrcpy.");

        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try
        {
            return await completion.Task.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            UnhookWindowsHookEx(hook);
            GC.KeepAlive(callback);
        }
    }

    private static bool TryMapToDevice(IntPtr window, Point screenPoint, int screenWidth, int screenHeight, out Point mapped)
    {
        mapped = default;
        if (!GetClientRect(window, out var client)) return false;

        var origin = new Point();
        if (!ClientToScreen(window, ref origin)) return false;

        var clientWidth = client.Right - client.Left;
        var clientHeight = client.Bottom - client.Top;
        var localX = screenPoint.X - origin.X;
        var localY = screenPoint.Y - origin.Y;
        if (clientWidth <= 0 || clientHeight <= 0) return false;

        var scale = Math.Min(clientWidth / (double)screenWidth, clientHeight / (double)screenHeight);
        var contentWidth = screenWidth * scale;
        var contentHeight = screenHeight * scale;
        var offsetX = (clientWidth - contentWidth) / 2d;
        var offsetY = (clientHeight - contentHeight) / 2d;
        if (localX < offsetX || localY < offsetY || localX >= offsetX + contentWidth || localY >= offsetY + contentHeight)
            return false;

        mapped = new Point
        {
            X = Math.Clamp((int)Math.Round((localX - offsetX) / scale), 0, screenWidth - 1),
            Y = Math.Clamp((int)Math.Round((localY - offsetY) / scale), 0, screenHeight - 1)
        };
        return true;
    }

    private static string ReadWindowTitle(IntPtr window)
    {
        if (window == IntPtr.Zero) return string.Empty;
        var buffer = new char[256];
        var length = GetWindowText(window, buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }
}
