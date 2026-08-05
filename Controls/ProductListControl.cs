using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

/// <summary>
/// Module quản lý danh sách sản phẩm dùng chung với workflow.
/// </summary>
public sealed class ProductListControl : UserControl
{
    private readonly Guna.UI2.WinForms.Guna2DataGridView _grid;
    private bool _suggestionsEnabled = true;

    public event EventHandler? ImportRequested;
    public event EventHandler? AddRequested;
    public event EventHandler? EditRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? RunWorkflowRequested;
    public event EventHandler? ResetStatusesRequested;
    public event EventHandler? WorkflowRequested;
    public event EventHandler? BackupDbRequested;
    public event EventHandler? RestoreDbRequested;
    public event EventHandler<ProductChangedEventArgs>? ProductChanged;

    public ProductListControl()
    {
        BackColor = Color.FromArgb(244, 245, 248);
        Dock = DockStyle.Fill;
        Padding = new Padding(16, 14, 16, 14);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 8)
        };
        var title = new Label
        {
            Text = "DANH SÁCH SẢN PHẨM",
            AutoSize = true,
            Location = new Point(16, 10),
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Color.FromArgb(31, 31, 44)
        };
        var subtitle = new Label
        {
            Text = "Quản lý video, tiêu đề, link tiếp thị và trạng thái đã up Shopee",
            AutoSize = true,
            Location = new Point(17, 36),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(114, 117, 134)
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(14, 7, 14, 7)
        };
        var btnImport = CreateButton("Nhập Excel", Color.FromArgb(96, 82, 218), 112);
        var btnAdd = CreateButton("Thêm", Color.FromArgb(10, 151, 205), 78);
        var btnEdit = CreateButton("Sửa", Color.FromArgb(228, 240, 249), 72);
        var btnDelete = CreateButton("Xóa", Color.FromArgb(241, 226, 229), 72);
        var btnRun = CreateButton("Chạy workflow", Color.FromArgb(0, 161, 112), 128);
        var btnReset = CreateButton("Đặt lại trạng thái", Color.FromArgb(235, 237, 242), 130);
        var btnWorkflow = CreateButton("Mở Workflow", Color.FromArgb(228, 240, 249), 110);
        var btnBackup = CreateButton("Sao lưu DB", Color.FromArgb(114, 117, 134), 100);
        var btnRestore = CreateButton("Phục hồi DB", Color.FromArgb(114, 117, 134), 100);
        btnImport.Click += (_, _) => ImportRequested?.Invoke(this, EventArgs.Empty);
        btnAdd.Click += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
        btnEdit.Click += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);
        btnDelete.Click += (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty);
        btnRun.Click += (_, _) => RunWorkflowRequested?.Invoke(this, EventArgs.Empty);
        btnReset.Click += (_, _) => ResetStatusesRequested?.Invoke(this, EventArgs.Empty);
        btnWorkflow.Click += (_, _) => WorkflowRequested?.Invoke(this, EventArgs.Empty);
        btnBackup.Click += (_, _) => BackupDbRequested?.Invoke(this, EventArgs.Empty);
        btnRestore.Click += (_, _) => RestoreDbRequested?.Invoke(this, EventArgs.Empty);
        var chkSuggestions = new CheckBox
        {
            Text = "Gợi ý link tự động", Checked = true, AutoSize = true,
            Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(90, 95, 115),
            Margin = new Padding(12, 8, 0, 0)
        };
        chkSuggestions.CheckedChanged += (_, _) =>
        {
            _suggestionsEnabled = chkSuggestions.Checked;
            RefreshSuggestedLinks();
        };
        toolbar.Controls.AddRange([btnImport, btnAdd, btnEdit, btnDelete, btnRun, btnReset, btnWorkflow, btnBackup, btnRestore, chkSuggestions]);

        _grid = new Guna.UI2.WinForms.Guna2DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(235, 237, 242),
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false
        };
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(244, 245, 248),
            ForeColor = Color.FromArgb(96, 82, 218),
            Font = new Font("Segoe UI Semibold", 10F),
            Padding = new Padding(10, 8, 10, 8)
        };
        _grid.ColumnHeadersHeight = 44;
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 31, 44),
            SelectionBackColor = Color.FromArgb(225, 240, 252),
            SelectionForeColor = Color.FromArgb(31, 31, 44),
            Padding = new Padding(10, 6, 10, 6),
            Font = new Font("Segoe UI", 10F)
        };
        _grid.RowTemplate.Height = 48;
        AddColumn("VideoPath", "Đường dẫn video", 34F);
        AddColumn("Title", "Tiêu đề", 22F);
        AddColumn("SuggestedAffLink", "Gợi ý aff", 22F);
        AddColumn("ShopeeAffLink", "ShopeeAffLink", 26F);
        AddColumn("Status", "Trạng thái", 12F);
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "ShopeeStatus",
            HeaderText = "Trạng thái Shopee",
            FillWeight = 18F,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
            Items = { "Chưa up Shopee", "Đã up Shopee" },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.CellClick += GridCellClick;
        _grid.CellEndEdit += GridCellEndEdit;
        _grid.CellDoubleClick += GridCellDoubleClick;
        _grid.DataError += (_, e) => e.ThrowException = false;

        Controls.Add(_grid);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    public void SetProducts(IReadOnlyList<JobItem> products)
    {
        _grid.Rows.Clear();
        foreach (var product in products)
            AddProductRow(product);
        RefreshSuggestedLinks();
    }

    public void UpdateProduct(int index, JobItem product)
    {
        if (index < 0 || index >= _grid.Rows.Count) return;
        var row = _grid.Rows[index];
        row.Cells["VideoPath"].Value = product.VideoPath;
        row.Cells["Title"].Value = product.Title;
        row.Cells["SuggestedAffLink"].Value = string.Empty;
        row.Cells["ShopeeAffLink"].Value = product.ShopeeAffLink;
        row.Cells["Status"].Value = product.Status;
        row.Cells["ShopeeStatus"].Value = product.ShopeeStatus;
        row.Cells["ShopeeStatus"].Style.ForeColor = GetStatusColor(product.ShopeeStatus);
        StyleStatusCell(row.Cells["Status"], product.Status);
        RefreshSuggestedLinks();
    }

    public int SelectedIndex => _grid.CurrentRow?.Index ?? -1;
    public IReadOnlyList<int> SelectedIndices => _grid.SelectedRows
        .Cast<DataGridViewRow>()
        .Where(row => !row.IsNewRow)
        .Select(row => row.Index)
        .OrderBy(index => index)
        .ToList();

    private void AddProductRow(JobItem product)
    {
        var rowIndex = _grid.Rows.Add(product.VideoPath, product.Title, string.Empty, product.ShopeeAffLink, product.Status, product.ShopeeStatus);
        _grid.Rows[rowIndex].Cells["ShopeeStatus"].Style.ForeColor = GetStatusColor(product.ShopeeStatus);
        StyleStatusCell(_grid.Rows[rowIndex].Cells["Status"], product.Status);
        RefreshSuggestedLinks();
    }

    private void AddColumn(string name, string headerText, float fillWeight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = headerText,
            FillWeight = fillWeight,
            ReadOnly = name == "VideoPath" || name == "Status" || name == "SuggestedAffLink",
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void GridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (columnName == "SuggestedAffLink")
        {
            var suggestion = Convert.ToString(_grid.Rows[e.RowIndex].Cells["SuggestedAffLink"].Value) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(suggestion))
            {
                _grid.Rows[e.RowIndex].Cells["ShopeeAffLink"].Value = suggestion;
                RaiseProductChanged(e.RowIndex);
                RefreshSuggestedLinks();
            }
            return;
        }

        if (columnName == "VideoPath")
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn file video",
                Filter = "Video|*.mp4;*.mov;*.mkv;*.avi;*.webm;*.m4v|Tất cả file|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                _grid.Rows[e.RowIndex].Cells["VideoPath"].Value = dialog.FileName;
                RaiseProductChanged(e.RowIndex);
            }

            return;
        }

        _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        _grid.BeginEdit(true);
    }

    private void GridCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            RaiseProductChanged(e.RowIndex);
            RefreshSuggestedLinks();
        }
    }

    private void GridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        EditRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseProductChanged(int index)
    {
        if (index < 0 || index >= _grid.Rows.Count) return;

        var row = _grid.Rows[index];
        ProductChanged?.Invoke(this, new ProductChangedEventArgs(index, new JobItem
        {
            VideoPath = Convert.ToString(row.Cells["VideoPath"].Value) ?? string.Empty,
            Title = Convert.ToString(row.Cells["Title"].Value) ?? string.Empty,
            ShopeeAffLink = Convert.ToString(row.Cells["ShopeeAffLink"].Value) ?? string.Empty,
            ShopeeStatus = Convert.ToString(row.Cells["ShopeeStatus"].Value) ?? "Chưa up Shopee"
        }));
    }

    private void RefreshSuggestedLinks()
    {
        if (_grid.Rows.Count == 0) return;

        for (var i = 0; i < _grid.Rows.Count; i++)
        {
            var row = _grid.Rows[i];
            var title = Convert.ToString(row.Cells["Title"].Value) ?? string.Empty;
            var ownLink = Convert.ToString(row.Cells["ShopeeAffLink"].Value) ?? string.Empty;
            var suggestion = _suggestionsEnabled ? FindSuggestedLink(i, title, ownLink) : string.Empty;
            row.Cells["SuggestedAffLink"].Value = suggestion;
            row.Cells["SuggestedAffLink"].ToolTipText = suggestion;
        }
    }

    private string FindSuggestedLink(int currentIndex, string title, string ownLink)
    {
        var currentKey = NormalizeTitle(title);
        if (string.IsNullOrWhiteSpace(currentKey))
            return string.Empty;

        string? sameTitleLink = null;
        string? similarTitleLink = null;

        for (var i = 0; i < _grid.Rows.Count; i++)
        {
            if (i == currentIndex) continue;
            var other = _grid.Rows[i];
            var otherTitle = Convert.ToString(other.Cells["Title"].Value) ?? string.Empty;
            var otherLink = Convert.ToString(other.Cells["ShopeeAffLink"].Value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(otherLink)) continue;

            var otherKey = NormalizeTitle(otherTitle);
            if (string.IsNullOrWhiteSpace(otherKey)) continue;

            if (otherKey == currentKey)
            {
                sameTitleLink = otherLink;
                break;
            }

            if (similarTitleLink == null && IsTitleSimilar(currentKey, otherKey))
                similarTitleLink = otherLink;
        }

        if (!string.IsNullOrWhiteSpace(sameTitleLink))
            return sameTitleLink;

        if (!string.IsNullOrWhiteSpace(similarTitleLink) && !string.Equals(similarTitleLink, ownLink, StringComparison.OrdinalIgnoreCase))
            return similarTitleLink;

        return string.Empty;
    }

    private static bool IsTitleSimilar(string left, string right)
    {
        var leftTokens = left.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightTokens = right.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (leftTokens.Count == 0 || rightTokens.Count == 0) return false;

        var overlap = leftTokens.Count(token => rightTokens.Contains(token));
        var total = Math.Max(leftTokens.Count, rightTokens.Count);
        return overlap >= 2 || (total <= 3 && overlap >= 1);
    }

    private static string NormalizeTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var chars = value
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            .ToArray();
        return new string(chars).Trim();
    }

    private static Guna.UI2.WinForms.Guna2Button CreateButton(string text, Color color, int width)
    {
        var lightButton = color == Color.FromArgb(235, 237, 242) ||
                          color == Color.FromArgb(228, 240, 249) ||
                          color == Color.FromArgb(241, 226, 229);
        return new Guna.UI2.WinForms.Guna2Button
        {
            Text = text,
            Width = width,
            Height = 34,
            FillColor = color,
            BorderRadius = 7,
            BorderThickness = 0,
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = lightButton ? Color.FromArgb(70, 70, 85) : Color.White,
            Cursor = Cursors.Hand,
            Margin = new Padding(3, 0, 3, 0)
        };
    }

    private static Color GetStatusColor(string status)
        => status == "Đã up Shopee"
            ? Color.FromArgb(0, 161, 112)
            : Color.FromArgb(192, 133, 37);

    private static void StyleStatusCell(DataGridViewCell cell, string status)
    {
        switch (status)
        {
            case "Thành công":
                cell.Style.ForeColor = Color.FromArgb(0, 161, 112);
                break;
            case "Lỗi":
                cell.Style.ForeColor = Color.FromArgb(226, 82, 82);
                break;
            case "Đang chạy":
                cell.Style.ForeColor = Color.FromArgb(10, 151, 205);
                break;
            case "Đang thử lại":
            case "Đang chạy lại":
                cell.Style.ForeColor = Color.FromArgb(192, 133, 37);
                break;
            case "Chờ":
            default:
                cell.Style.ForeColor = Color.FromArgb(114, 117, 134);
                break;
        }
    }
}

public sealed class ProductChangedEventArgs(int index, JobItem product) : EventArgs
{
    public int Index { get; } = index;
    public JobItem Product { get; } = product;
}
