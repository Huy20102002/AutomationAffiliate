namespace ShopeeVideoUploader.Models;

public class FolderItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public override string ToString() => Name;
}
