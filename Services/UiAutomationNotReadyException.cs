namespace ShopeeVideoUploader.Services;

/// <summary>
/// Exception báo rằng UI Automator chưa lấy được cây giao diện ổn định để xử lý XPath.
/// </summary>
public sealed class UiAutomationNotReadyException : InvalidOperationException
{
    public UiAutomationNotReadyException()
    {
    }

    public UiAutomationNotReadyException(string message)
        : base(message)
    {
    }

    public UiAutomationNotReadyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
