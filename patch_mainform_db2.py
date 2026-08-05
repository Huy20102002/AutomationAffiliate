import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Wire up the events in WireEvents()
    wire_old = "        productListControl.WorkflowRequested += (_, _) => ShowWorkflowView();"
    wire_new = "        productListControl.WorkflowRequested += (_, _) => ShowWorkflowView();\n        productListControl.BackupDbRequested += (_, _) => BackupDatabase();\n        productListControl.RestoreDbRequested += (_, _) => RestoreDatabase();"
    content = content.replace(wire_old, wire_new)

    # Add the BackupDatabase and RestoreDatabase methods
    methods = """    private void BackupDatabase()
    {
        using var dialog = new SaveFileDialog { Filter = "SQLite Database|*.db", Title = "Sao lưu CSDL", FileName = "app_backup.db" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            if (File.Exists("app.db"))
            {
                File.Copy("app.db", dialog.FileName, true);
                SetStatus("Đã sao lưu CSDL thành công");
                Logger.Info($"Đã sao lưu CSDL ra {dialog.FileName}");
            }
            else
            {
                ShowError("Không tìm thấy file CSDL app.db để sao lưu.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi sao lưu CSDL", ex);
            ShowError($"Không thể sao lưu: {ex.Message}");
        }
    }

    private void RestoreDatabase()
    {
        using var dialog = new OpenFileDialog { Filter = "SQLite Database|*.db", Title = "Phục hồi CSDL" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            File.Copy(dialog.FileName, "app.db", true);
            _jobs.Clear();
            _jobs.AddRange(_dbService.GetAllJobs());
            RefreshJobGrid();
            SetStatus("Đã phục hồi CSDL thành công");
            Logger.Info($"Đã phục hồi CSDL từ {dialog.FileName}");
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi phục hồi CSDL", ex);
            ShowError($"Không thể phục hồi: {ex.Message}");
        }
    }
"""
    
    # insert before SaveAutoSavedWorkflow
    content = content.replace(
        "    private void SaveAutoSavedWorkflow()",
        methods + "\n    private void SaveAutoSavedWorkflow()"
    )

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    print("MainForm.cs patched for Backup/Restore.")

if __name__ == "__main__":
    process_file("MainForm.cs")
