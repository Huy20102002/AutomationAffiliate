using Newtonsoft.Json;

namespace ShopeeVideoUploader.Models;

/// <summary>Biến có thể chèn vào TextValue bằng cú pháp {TênBiến}.</summary>
public sealed class WorkflowVariable
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
    public bool IsDeviceBuiltIn { get; set; }

    [JsonIgnore]
    public string Token => $"{{{Name}}}";

    public override string ToString() => IsDeviceBuiltIn
        ? $"{Token}  · thiết bị"
        : IsBuiltIn ? $"{Token}  · dữ liệu Excel" : Token;
}

/// <summary>File workflow: steps, biến dùng chung và danh sách sản phẩm.</summary>
public sealed class WorkflowDocument
{
    public List<WorkflowStep> Steps { get; set; } = [];
    public List<WorkflowVariable> Variables { get; set; } = [];
    public List<JobItem> Products { get; set; } = [];
}
