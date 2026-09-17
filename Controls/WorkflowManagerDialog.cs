using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader.Controls;

public sealed class WorkflowManagerDialog : Form
{
    private readonly string _workflowsDirectory;
    private readonly Guna2DataGridView _grid;
    private readonly Guna2Button _btnDelete;
    private readonly Guna2Button _btnRename;
    private readonly Guna2Button _btnClone;
    private readonly Guna2Button _btnSelect;
    private readonly Guna2Button _btnCleanOrphans;
    private readonly Action<string>? _onSelectWorkflow;
    private string _currentWorkflowFile;

    public string SelectedWorkflowFile => _currentWorkflowFile;
    public bool HasChanges { get; private set; }

    public WorkflowManagerDialog(string workflowsDirectory, string currentWorkflowFile, Action<string>? onSelectWorkflow = null)
    {
        _workflowsDirectory = workflowsDirectory;
        _currentWorkflowFile = currentWorkflowFile;
        _onSelectWorkflow = onSelectWorkflow;

        Text = "Quản lý quy trình (Workflows)";
        Size = new Size(720, 520);
        MinimumSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Segoe UI", 9F);

        // Header Panel
        var panelHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(18, 12, 18, 12)
        };
        panelHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 234, 240));
            e.Graphics.DrawLine(pen, 0, panelHeader.ClientSize.Height - 1, panelHeader.ClientSize.Width, panelHeader.ClientSize.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "Danh sách quy trình tự động",
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Color.FromArgb(31, 31, 44),
            AutoSize = true,
            Location = new Point(16, 10)
        };
        var lblSubtitle = new Label
        {
            Text = "Quản lý, xóa, đổi tên hoặc nhân bản các tệp quy trình (.json)",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(17, 34)
        };
        panelHeader.Controls.Add(lblTitle);
        panelHeader.Controls.Add(lblSubtitle);

        // Bottom Actions Panel
        var panelBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 12)
        };
        panelBottom.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 234, 240));
            e.Graphics.DrawLine(pen, 0, 0, panelBottom.ClientSize.Width, 0);
        };

        _btnDelete = new Guna2Button
        {
            Text = "🗑️  Xóa quy trình",
            Width = 140,
            Height = 36,
            FillColor = Color.FromArgb(239, 68, 68),
            ForeColor = Color.White,
            BorderRadius = 6,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Enabled = false,
            Location = new Point(16, 13)
        };
        _btnDelete.Click += (_, _) => DeleteSelectedWorkflow();

        _btnRename = new Guna2Button
        {
            Text = "✏️  Đổi tên",
            Width = 100,
            Height = 36,
            FillColor = Color.FromArgb(243, 244, 246),
            ForeColor = Color.FromArgb(55, 65, 81),
            BorderColor = Color.FromArgb(209, 213, 219),
            BorderThickness = 1,
            BorderRadius = 6,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Enabled = false,
            Location = new Point(164, 13)
        };
        _btnRename.Click += (_, _) => RenameSelectedWorkflow();

        _btnClone = new Guna2Button
        {
            Text = "📋  Nhân bản",
            Width = 110,
            Height = 36,
            FillColor = Color.FromArgb(243, 244, 246),
            ForeColor = Color.FromArgb(55, 65, 81),
            BorderColor = Color.FromArgb(209, 213, 219),
            BorderThickness = 1,
            BorderRadius = 6,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Enabled = false,
            Location = new Point(272, 13)
        };
        _btnClone.Click += (_, _) => CloneSelectedWorkflow();

        _btnCleanOrphans = new Guna2Button
        {
            Text = "🧹  Dọn file rác iOS",
            Width = 135,
            Height = 36,
            FillColor = Color.FromArgb(243, 244, 246),
            ForeColor = Color.FromArgb(100, 116, 139),
            BorderColor = Color.FromArgb(226, 232, 240),
            BorderThickness = 1,
            BorderRadius = 6,
            Font = new Font("Segoe UI", 8.5F),
            Cursor = Cursors.Hand,
            Location = new Point(390, 13)
        };
        _btnCleanOrphans.Click += (_, _) => CleanOrphanIosFiles();

        _btnSelect = new Guna2Button
        {
            Text = "✓  Chọn dùng quy trình này",
            Width = 185,
            Height = 36,
            FillColor = Color.FromArgb(79, 70, 229),
            ForeColor = Color.White,
            BorderRadius = 6,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(panelBottom.ClientSize.Width - 201, 13)
        };
        _btnSelect.Click += (_, _) => SelectAndClose();

        panelBottom.Controls.Add(_btnDelete);
        panelBottom.Controls.Add(_btnRename);
        panelBottom.Controls.Add(_btnClone);
        panelBottom.Controls.Add(_btnCleanOrphans);
        panelBottom.Controls.Add(_btnSelect);

        // Center Grid
        var gridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16)
        };

        _grid = new Guna2DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(241, 245, 249),
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 40 }
        };

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI Semibold", 9F),
            Padding = new Padding(10, 8, 10, 8)
        };
        _grid.ColumnHeadersHeight = 38;

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 41, 59),
            SelectionBackColor = Color.FromArgb(238, 242, 255),
            SelectionForeColor = Color.FromArgb(79, 70, 229),
            Padding = new Padding(10, 4, 10, 4),
            Font = new Font("Segoe UI", 9F)
        };

        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 251, 253),
            ForeColor = Color.FromArgb(30, 41, 59),
            SelectionBackColor = Color.FromArgb(238, 242, 255),
            SelectionForeColor = Color.FromArgb(79, 70, 229),
            Padding = new Padding(10, 4, 10, 4),
            Font = new Font("Segoe UI", 9F)
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colName",
            HeaderText = "Tên quy trình",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 140
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colSteps",
            HeaderText = "Số bước",
            Width = 90
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colIos",
            HeaderText = "Bản iOS",
            Width = 110
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colDate",
            HeaderText = "Cập nhật lúc",
            Width = 140
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colActive",
            HeaderText = "Trạng thái",
            Width = 95
        });

        _grid.SelectionChanged += (_, _) => UpdateButtonStates();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) SelectAndClose();
        };

        gridContainer.Controls.Add(_grid);

        Controls.Add(gridContainer);
        Controls.Add(panelBottom);
        Controls.Add(panelHeader);

        LoadWorkflows();
    }

    public void LoadWorkflows()
    {
        _grid.Rows.Clear();
        if (!Directory.Exists(_workflowsDirectory)) return;

        var files = Directory.GetFiles(_workflowsDirectory, "*.json")
            .Select(Path.GetFileName)
            .Where(f => !string.IsNullOrEmpty(f) && !f.StartsWith("_ios_"))
            .OrderBy(f => f)
            .ToList();

        int selectRow = -1;
        foreach (var file in files)
        {
            var fullPath = Path.Combine(_workflowsDirectory, file!);
            var iosPath = Path.Combine(_workflowsDirectory, "_ios_" + file);

            int stepCount = 0;
            try
            {
                var doc = WorkflowEngine.LoadWorkflowDocument(fullPath);
                stepCount = doc.Steps.Count;
            }
            catch { }

            string iosInfo = "Không";
            if (File.Exists(iosPath))
            {
                try
                {
                    var iosDoc = WorkflowEngine.LoadWorkflowDocument(iosPath);
                    iosInfo = $"Có ({iosDoc.Steps.Count} bước)";
                }
                catch
                {
                    iosInfo = "Có";
                }
            }

            var fi = new FileInfo(fullPath);
            var dateStr = fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm");
            bool isActive = string.Equals(file, _currentWorkflowFile, StringComparison.OrdinalIgnoreCase);
            var statusStr = isActive ? "● Đang chọn" : "";

            int rowIndex = _grid.Rows.Add(file, $"{stepCount} bước", iosInfo, dateStr, statusStr);
            if (isActive) selectRow = rowIndex;
        }

        if (selectRow >= 0 && selectRow < _grid.Rows.Count)
        {
            _grid.ClearSelection();
            _grid.Rows[selectRow].Selected = true;
            _grid.FirstDisplayedScrollingRowIndex = selectRow;
        }
        else if (_grid.Rows.Count > 0)
        {
            _grid.Rows[0].Selected = true;
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        var hasSelection = _grid.SelectedRows.Count > 0;
        _btnDelete.Enabled = hasSelection && _grid.Rows.Count > 1;
        _btnRename.Enabled = hasSelection;
        _btnClone.Enabled = hasSelection;
        _btnSelect.Enabled = hasSelection;
    }

    private string? GetSelectedFileName()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        return _grid.SelectedRows[0].Cells["colName"].Value?.ToString();
    }

    private void DeleteSelectedWorkflow()
    {
        var fileName = GetSelectedFileName();
        if (string.IsNullOrEmpty(fileName)) return;

        if (_grid.Rows.Count <= 1)
        {
            MessageBox.Show("Không thể xóa quy trình cuối cùng! Hệ thống cần ít nhất 1 quy trình.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Bạn có chắc chắn muốn XÓA VĨNH VIỄN quy trình:\n\n👉 {fileName}\n\n(Tệp quy trình và bản iOS đi kèm sẽ bị xóa hoàn toàn khỏi máy tính)?",
            "Xác nhận xóa quy trình",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes) return;

        try
        {
            var path = Path.Combine(_workflowsDirectory, fileName);
            var iosPath = Path.Combine(_workflowsDirectory, "_ios_" + fileName);

            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(iosPath)) File.Delete(iosPath);

            Logger.Info($"[WorkflowManager] Đã xóa quy trình: {fileName}");
            HasChanges = true;

            if (string.Equals(_currentWorkflowFile, fileName, StringComparison.OrdinalIgnoreCase))
            {
                _currentWorkflowFile = "";
            }

            LoadWorkflows();
            MessageBox.Show($"Đã xóa thành công quy trình '{fileName}'!", "Đã xóa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"Lỗi khi xóa workflow: {ex.Message}", ex);
            MessageBox.Show($"Không thể xóa quy trình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenameSelectedWorkflow()
    {
        var fileName = GetSelectedFileName();
        if (string.IsNullOrEmpty(fileName)) return;

        var baseName = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^5]
            : fileName;

        var newName = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên mới cho quy trình:", "Đổi Tên Quy Trình", baseName);
        if (string.IsNullOrWhiteSpace(newName)) return;
        if (!newName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) newName += ".json";
        if (string.Equals(fileName, newName, StringComparison.OrdinalIgnoreCase)) return;

        var oldPath = Path.Combine(_workflowsDirectory, fileName);
        var newPath = Path.Combine(_workflowsDirectory, newName);

        if (File.Exists(newPath))
        {
            MessageBox.Show("Tên quy trình này đã tồn tại, vui lòng chọn tên khác!", "Trùng tên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            File.Move(oldPath, newPath);
            var oldIos = Path.Combine(_workflowsDirectory, "_ios_" + fileName);
            var newIos = Path.Combine(_workflowsDirectory, "_ios_" + newName);
            if (File.Exists(oldIos)) File.Move(oldIos, newIos);

            Logger.Info($"[WorkflowManager] Đã đổi tên quy trình '{fileName}' thành '{newName}'");
            HasChanges = true;

            if (string.Equals(_currentWorkflowFile, fileName, StringComparison.OrdinalIgnoreCase))
            {
                _currentWorkflowFile = newName;
            }

            LoadWorkflows();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể đổi tên quy trình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CloneSelectedWorkflow()
    {
        var fileName = GetSelectedFileName();
        if (string.IsNullOrEmpty(fileName)) return;

        var baseName = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? fileName[..^5] : fileName;
        var cloneName = $"{baseName}_ban_sao.json";
        int counter = 2;
        while (File.Exists(Path.Combine(_workflowsDirectory, cloneName)))
        {
            cloneName = $"{baseName}_ban_sao_{counter++}.json";
        }

        try
        {
            var oldPath = Path.Combine(_workflowsDirectory, fileName);
            var newPath = Path.Combine(_workflowsDirectory, cloneName);
            File.Copy(oldPath, newPath, true);

            var oldIos = Path.Combine(_workflowsDirectory, "_ios_" + fileName);
            var newIos = Path.Combine(_workflowsDirectory, "_ios_" + cloneName);
            if (File.Exists(oldIos)) File.Copy(oldIos, newIos, true);

            Logger.Info($"[WorkflowManager] Đã nhân bản quy trình '{fileName}' thành '{cloneName}'");
            HasChanges = true;
            _currentWorkflowFile = cloneName;
            LoadWorkflows();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể nhân bản quy trình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CleanOrphanIosFiles()
    {
        if (!Directory.Exists(_workflowsDirectory)) return;
        var iosFiles = Directory.GetFiles(_workflowsDirectory, "_ios_*.json");
        int deleted = 0;

        foreach (var iosFile in iosFiles)
        {
            var iosName = Path.GetFileName(iosFile);
            var mainName = iosName[5..]; // remove "_ios_"
            var mainPath = Path.Combine(_workflowsDirectory, mainName);
            if (!File.Exists(mainPath))
            {
                try
                {
                    File.Delete(iosFile);
                    deleted++;
                }
                catch { }
            }
        }

        if (deleted > 0)
        {
            MessageBox.Show($"Đã dọn dẹp {deleted} tệp bản sao iOS không còn tệp chính!", "Dọn dẹp hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            HasChanges = true;
            LoadWorkflows();
        }
        else
        {
            MessageBox.Show("Không có tệp rác nào cần dọn dẹp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SelectAndClose()
    {
        var fileName = GetSelectedFileName();
        if (!string.IsNullOrEmpty(fileName))
        {
            _currentWorkflowFile = fileName;
            _onSelectWorkflow?.Invoke(fileName);
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
