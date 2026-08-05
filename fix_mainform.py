import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # Find where 'cboStepType.FillColor = card;' is
    target_idx = -1
    for i, line in enumerate(lines):
        if 'cboStepType.FillColor = card;' in line:
            target_idx = i
            break
            
    if target_idx == -1:
        print("Could not find target line")
        return
        
    # We want to insert the missing code after target_idx
    # The current code right after target_idx is:
    #         var ok = await _adb.InitializeAsync();
    
    missing_code = """        workflowCanvas.ApplyTheme(true);

        dgvJobs.GridColor = Color.FromArgb(235, 237, 242); dgvJobs.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(244, 245, 248); dgvJobs.ThemeStyle.HeaderStyle.ForeColor = primary; dgvJobs.ThemeStyle.RowsStyle.BackColor = Color.White; dgvJobs.ThemeStyle.RowsStyle.ForeColor = ink; dgvJobs.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(215, 239, 251); dgvJobs.ThemeStyle.RowsStyle.SelectionForeColor = ink;
        dgvJobs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 245, 248);
        dgvJobs.ColumnHeadersDefaultCellStyle.ForeColor = primary;
        dgvJobs.DefaultCellStyle.BackColor = Color.White;
        dgvJobs.DefaultCellStyle.ForeColor = ink;
        dgvJobs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 239, 251);
        dgvJobs.DefaultCellStyle.SelectionForeColor = ink;
        dgvJobs.ScrollBars = ScrollBars.Vertical;
        dgvJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvJobs.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        dgvJobs.RowTemplate.Height = 35;
        dgvJobs.ThemeStyle.RowsStyle.Height = 35;
        txtLog.BackColor = Color.FromArgb(248, 251, 253);
        txtLog.ForeColor = Color.FromArgb(49, 95, 125);
    }

    private static IEnumerable<Control> GetAllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in GetAllControls(child)) yield return nested;
        }
    }

    private static bool IsInside(Control control, Control parent)
    {
        for (var current = control.Parent; current != null; current = current.Parent)
            if (current == parent) return true;
        return false;
    }

    private async Task InitAdbAsync()
    {
        SetStatus("Đang khởi tạo ADB...");
"""
    
    # Insert missing_code
    lines.insert(target_idx + 1, missing_code)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Fixed MainForm.cs")

if __name__ == "__main__":
    process_file("MainForm.cs")
