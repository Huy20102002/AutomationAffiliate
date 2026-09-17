namespace ShopeeVideoUploader.Models;

public class UiElement
{
    public string ClassName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ContentDesc { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Bounds { get; set; } = string.Empty;
    public bool IsClickable { get; set; }
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// The generated XPath to find this node.
    /// </summary>
    public string XPath { get; set; } = string.Empty;

    /// <summary>
    /// Depth in the UI tree, useful for indentation in UI.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Tính toán tọa độ tâm (Center Point) từ thuộc tính Bounds.
    /// </summary>
    public System.Drawing.Point? GetCenterPoint()
    {
        if (string.IsNullOrEmpty(Bounds)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(Bounds, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
        if (match.Success)
        {
            var left = int.Parse(match.Groups[1].Value);
            var top = int.Parse(match.Groups[2].Value);
            var right = int.Parse(match.Groups[3].Value);
            var bottom = int.Parse(match.Groups[4].Value);
            return new System.Drawing.Point((left + right) / 2, (top + bottom) / 2);
        }
        return null;
    }

    /// <summary>
    /// Formats a concise display string for the UI element.
    /// </summary>
    public string DisplayText
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Text)) parts.Add($"Text: '{Text}'");
            if (!string.IsNullOrEmpty(ContentDesc)) parts.Add($"Desc: '{ContentDesc}'");
            if (!string.IsNullOrEmpty(ResourceId)) parts.Add($"Id: '{ResourceId.Split('/').LastOrDefault() ?? ResourceId}'");
            
            var info = string.Join(" | ", parts);
            if (string.IsNullOrEmpty(info)) return ClassName.Split('.').LastOrDefault() ?? ClassName;
            return $"{ClassName.Split('.').LastOrDefault() ?? ClassName} - {info}";
        }
    }
}
