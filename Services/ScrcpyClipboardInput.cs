using System.Runtime.InteropServices;
using ShopeeVideoUploader.Helpers;

namespace ShopeeVideoUploader.Services;

/// <summary>
/// Nhập văn bản qua cửa sổ scrcpy mà không cần đổi IME sang ADBKeyboard.
/// Scrcpy nhận clipboard của Windows và thực hiện phím tắt MOD+V để dán vào
/// ô đang được chọn trên điện thoại.
/// </summary>
public sealed class ScrcpyClipboardInput
{
    private const byte VkShift = 0x10;
    private const byte VkControl = 0x11;
    private const byte VkMenu = 0x12;
    private const byte VkReturn = 0x0D;
    private const byte VkTab = 0x09;
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;
    private const int SwRestore = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char character);

    public static bool CanTypeLikeHuman(string text)
        => text.All(character => character <= 127);

    /// <summary>Gửi chuỗi ASCII thành các phím riêng lẻ qua cửa sổ scrcpy.</summary>
    public void TypeLikeHuman(string text, string? preferredTitle = null)
    {
        var scrcpyWindow = FindRequiredScrcpyWindow(preferredTitle);
        ShowWindow(scrcpyWindow, SwRestore);
        if (!SetForegroundWindow(scrcpyWindow))
            Logger.Warn("Không đưa được cửa sổ scrcpy lên trước khi gõ phím.");

        Thread.Sleep(150);
        foreach (var character in text)
        {
            SendCharacter(character);
            Thread.Sleep(18);
        }

        Logger.Info($"[SCRCPY] Đã gõ từng phím ({text.Length} ký tự ASCII).");
    }
    /// <summary>
    /// Đặt text vào clipboard của PC để scrcpy đồng bộ sang Android.
    /// Việc gửi KEYCODE_PASTE được thực hiện qua AdbManager ở MainForm, vì
    /// như vậy không cần cửa sổ scrcpy phải nhận focus bàn phím Windows.
    /// </summary>
    public void CopyText(string text, string? preferredTitle = null)
    {
        if (string.IsNullOrEmpty(text)) return;

        _ = FindRequiredScrcpyWindow(preferredTitle);

        Clipboard.SetText(text);
        Logger.Info($"[SCRCPY] Đã chép văn bản ({text.Length} ký tự) vào clipboard, chờ Android đồng bộ.");
    }

    private void SendCharacter(char character)
    {
        if (character == '\r') return;
        if (character == '\n')
        {
            SendKey(VkReturn, 0);
            return;
        }
        if (character == '\t')
        {
            SendKey(VkTab, 0);
            return;
        }

        var mapped = VkKeyScan(character);
        if (mapped == -1)
            throw new InvalidOperationException($"Không ánh xạ được ký tự ASCII '{character}' sang phím Windows.");

        var virtualKey = (byte)(mapped & 0xFF);
        var modifiers = (byte)((mapped >> 8) & 0xFF);
        var modifierKeys = new List<byte>();
        if ((modifiers & 1) != 0) modifierKeys.Add(VkShift);
        if ((modifiers & 2) != 0) modifierKeys.Add(VkControl);
        if ((modifiers & 4) != 0) modifierKeys.Add(VkMenu);

        foreach (var modifier in modifierKeys) SendKey(modifier, 0);
        SendKey(virtualKey, 0);
        SendKey(virtualKey, KeyeventfKeyup);
        for (var index = modifierKeys.Count - 1; index >= 0; index--)
            SendKey(modifierKeys[index], KeyeventfKeyup);
    }

    private static void SendKey(byte virtualKey, uint flags)
    {
        var input = new[]
        {
            new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInput { VirtualKey = virtualKey, Flags = flags }
                }
            }
        };

        if (SendInput(1, input, Marshal.SizeOf<Input>()) != 1)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Không gửi được phím tới scrcpy (mã lỗi Windows {error}).");
        }
    }

    private static IntPtr FindRequiredScrcpyWindow(string? preferredTitle)
    {
        var handle = FindScrcpyWindow(preferredTitle);
        if (handle != IntPtr.Zero) return handle;

        throw new InvalidOperationException(
            "Không tìm thấy cửa sổ scrcpy. Hãy mở scrcpy và giữ cửa sổ scrcpy đang kết nối đúng thiết bị.");
    }

    private static IntPtr FindScrcpyWindow(string? preferredTitle)
    {
        var windows = new List<(IntPtr Handle, string Title)>();
        NativeMethods.EnumWindows((handle, _) =>
        {
            if (!NativeMethods.IsWindowVisible(handle)) return true;
            var title = NativeMethods.GetWindowTitle(handle);
            if (string.IsNullOrWhiteSpace(title)) return true;
            if (title.Contains("FlowPilot", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Shopee Video Studio", StringComparison.OrdinalIgnoreCase)) return true;

            windows.Add((handle, title));
            return true;
        }, IntPtr.Zero);

        if (!string.IsNullOrWhiteSpace(preferredTitle))
        {
            var exact = windows.FirstOrDefault(item =>
                string.Equals(item.Title, preferredTitle, StringComparison.OrdinalIgnoreCase));
            if (exact.Handle != IntPtr.Zero) return exact.Handle;

            var matchingDevice = windows.FirstOrDefault(item =>
                item.Title.Contains(preferredTitle, StringComparison.OrdinalIgnoreCase));
            if (matchingDevice.Handle != IntPtr.Zero) return matchingDevice.Handle;
        }

        return windows.FirstOrDefault(item =>
            item.Title.Contains("scrcpy", StringComparison.OrdinalIgnoreCase)).Handle;
    }
}
