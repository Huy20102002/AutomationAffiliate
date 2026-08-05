import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # In Designer
    content = content.replace("private DataGridView dgvJobs;", "private Guna.UI2.WinForms.Guna2DataGridView dgvJobs;")
    content = content.replace("dgvJobs = new DataGridView", "dgvJobs = new Guna.UI2.WinForms.Guna2DataGridView")
    
    # In MainForm.cs
    content = content.replace("dgvJobs.BackgroundColor = card;", "")
    content = content.replace("dgvJobs.GridColor = Color.FromArgb(235, 237, 242);", "dgvJobs.GridColor = Color.FromArgb(235, 237, 242); dgvJobs.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(244, 245, 248); dgvJobs.ThemeStyle.HeaderStyle.ForeColor = primary; dgvJobs.ThemeStyle.RowsStyle.BackColor = Color.White; dgvJobs.ThemeStyle.RowsStyle.ForeColor = ink; dgvJobs.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(215, 239, 251); dgvJobs.ThemeStyle.RowsStyle.SelectionForeColor = ink;")
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

if __name__ == "__main__":
    process_file(sys.argv[1])
