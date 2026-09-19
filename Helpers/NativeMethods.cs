using System.Runtime.InteropServices;

namespace ShopeeVideoUploader.Helpers;

/// <summary>
/// P/Invoke declarations cho Win32 API.
/// Được sử dụng chủ yếu để nhúng cửa sổ Scrcpy vào Panel WinForms.
/// 
/// === GIẢI THÍCH KỸ THUẬT NHÚNG SCRCPY ===
/// 
/// Scrcpy là ứng dụng SDL tạo cửa sổ top-level riêng biệt.
/// Để nhúng vào WinForm, ta cần:
/// 
/// 1. FindWindow() - Tìm HWND (handle) của cửa sổ scrcpy dựa trên title.
/// 2. SetParent() - Đặt cửa sổ scrcpy thành con (child) của Panel WinForm.
///    Khi gọi SetParent, Windows sẽ clip rendering của child window 
///    vào vùng client area của parent (Panel).
/// 3. GetWindowLong/SetWindowLong - Thay đổi Window Style:
///    - Xóa WS_POPUP (cửa sổ popup độc lập) 
///    - Xóa WS_CAPTION (thanh title bar)
///    - Xóa WS_THICKFRAME (viền resize)
///    - Thêm WS_CHILD (đánh dấu là child window)
///    Việc này đảm bảo scrcpy không còn border/titlebar và 
///    hoàn toàn nằm gọn trong Panel.
/// 4. MoveWindow() - Di chuyển và resize scrcpy khớp khít với Panel.
/// 5. SetWindowPos() - Điều chỉnh Z-order nếu cần.
/// </summary>
public static class NativeMethods
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length <= 0) return string.Empty;

        var buffer = new char[length + 1];
        _ = GetWindowText(hWnd, buffer, buffer.Length);
        return new string(buffer).TrimEnd('\0');
    }

    // ====================================================================
    // WINDOW SEARCH
    // ====================================================================

    /// <summary>
    /// Tìm cửa sổ top-level theo class name và/hoặc window title.
    /// Dùng để tìm cửa sổ scrcpy sau khi khởi chạy process.
    /// 
    /// lpClassName: Tên window class (null để bỏ qua).
    /// lpWindowName: Title cửa sổ (ta đặt qua --window-title khi chạy scrcpy).
    /// Return: HWND nếu tìm thấy, IntPtr.Zero nếu không.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    // ====================================================================
    // PARENT-CHILD RELATIONSHIP
    // ====================================================================

    /// <summary>
    /// Đặt cửa sổ hWndChild thành con của hWndNewParent.
    /// Đây là hàm cốt lõi để "nhúng" scrcpy vào Panel WinForm.
    /// 
    /// Khi gọi: SetParent(scrcpyHwnd, panelPhone.Handle)
    /// → Scrcpy sẽ được render BÊN TRONG Panel thay vì là cửa sổ riêng.
    /// 
    /// Return: HWND của parent cũ, hoặc IntPtr.Zero nếu lỗi.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    // ====================================================================
    // WINDOW STYLE MANIPULATION
    // ====================================================================

    /// <summary>
    /// Đọc thuộc tính style của cửa sổ.
    /// Dùng để lấy style hiện tại trước khi modify (bitmask operation).
    /// 
    /// nIndex = GWL_STYLE (-16) → lấy Window Style.
    /// Return: Giá trị style hiện tại (int bitmask).
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    /// <summary>
    /// Thay đổi thuộc tính style của cửa sổ.
    /// Dùng để xóa border, caption và thêm WS_CHILD cho scrcpy.
    /// 
    /// Ví dụ sử dụng:
    ///   int style = GetWindowLong(hwnd, GWL_STYLE);
    ///   style &= ~(WS_POPUP | WS_CAPTION | WS_THICKFRAME); // Xóa popup + border
    ///   style |= WS_CHILD | WS_VISIBLE;                      // Thêm child + visible
    ///   SetWindowLong(hwnd, GWL_STYLE, style);
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // ====================================================================
    // WINDOW POSITIONING & SIZING
    // ====================================================================

    /// <summary>
    /// Di chuyển và resize cửa sổ.
    /// Dùng để khớp scrcpy vào đúng kích thước Panel.
    /// 
    /// MoveWindow(scrcpyHwnd, 0, 0, panel.Width, panel.Height, true)
    /// → Scrcpy sẽ chiếm toàn bộ diện tích Panel.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// Điều chỉnh vị trí, kích thước, và Z-order của cửa sổ.
    /// Linh hoạt hơn MoveWindow, cho phép set flags bổ sung.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>
    /// Gửi message đến cửa sổ (dùng cho close, resize events, v.v.)
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // ====================================================================
    // THREAD INPUT - Xử lý focus keyboard khi nhúng cửa sổ ngoài
    // ====================================================================

    /// <summary>
    /// Gắn input queue của 2 thread lại với nhau.
    /// Cần thiết khi nhúng scrcpy để keyboard focus hoạt động đúng.
    /// 
    /// Khi scrcpy là child window, focus có thể "kẹt" vì 
    /// input queue của scrcpy thread tách biệt với WinForm thread.
    /// AttachThreadInput gộp 2 queue lại → focus chuyển mượt.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    /// <summary>
    /// Lấy Thread ID của thread tạo ra cửa sổ.
    /// Dùng kết hợp với AttachThreadInput.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // ====================================================================
    // CONSTANTS
    // ====================================================================

    /// <summary>Index cho Get/SetWindowLong: Window Style</summary>
    public const int GWL_STYLE = -16;

    /// <summary>Index cho Get/SetWindowLong: Extended Window Style</summary>
    public const int GWL_EXSTYLE = -20;

    // --- Window Styles ---

    /// <summary>Cửa sổ con (child) - không có title bar riêng</summary>
    public const int WS_CHILD = 0x40000000;

    /// <summary>Cửa sổ hiển thị (visible)</summary>
    public const int WS_VISIBLE = 0x10000000;

    /// <summary>Cửa sổ popup (top-level, không gắn parent)</summary>
    public const int WS_POPUP = unchecked((int)0x80000000);

    /// <summary>Thanh tiêu đề (title bar + border mỏng)</summary>
    public const int WS_CAPTION = 0x00C00000;

    /// <summary>Viền dày cho phép resize bằng chuột</summary>
    public const int WS_THICKFRAME = 0x00040000;

    /// <summary>Nút Maximize trên title bar</summary>
    public const int WS_MAXIMIZEBOX = 0x00010000;

    /// <summary>Nút Minimize trên title bar</summary>
    public const int WS_MINIMIZEBOX = 0x00020000;

    /// <summary>System menu (nút ở góc trái title bar)</summary>
    public const int WS_SYSMENU = 0x00080000;

    // --- SetWindowPos Flags ---

    /// <summary>Giữ nguyên kích thước khi gọi SetWindowPos</summary>
    public const uint SWP_NOSIZE = 0x0001;

    /// <summary>Giữ nguyên vị trí khi gọi SetWindowPos</summary>
    public const uint SWP_NOMOVE = 0x0002;

    /// <summary>Không thay đổi Z-order</summary>
    public const uint SWP_NOZORDER = 0x0004;

    /// <summary>Vẽ lại frame sau khi thay đổi</summary>
    public const uint SWP_FRAMECHANGED = 0x0020;

    // --- Messages ---

    /// <summary>Message đóng cửa sổ</summary>
    public const uint WM_CLOSE = 0x0010;

    // --- ShowWindow Commands ---
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;

    /// <summary>Clip các cửa sổ con khi vẽ (ngăn flicker/clipping)</summary>
    public const int WS_CLIPCHILDREN = 0x02000000;

    /// <summary>Clip các cửa sổ cùng cấp khi vẽ</summary>
    public const int WS_CLIPSIBLINGS = 0x04000000;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
}
