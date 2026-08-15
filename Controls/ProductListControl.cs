using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

/// <summary>
/// Module quản lý danh sách sản phẩm dùng chung với workflow.
/// </summary>
public sealed class ProductListControl : UserControl
{
    private readonly Guna.UI2.WinForms.Guna2DataGridView _grid;
    private readonly Panel _sidebar;
    private readonly CheckedListBox _folderList;
    private IReadOnlyList<JobItem> _allProducts = [];
    private IReadOnlyList<FolderItem> _allFolders = [];
    public event EventHandler? FolderCrudRequested;
    private bool _isFiltering = false;
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
            Text = "Quản lý video, tiêu đề, link tiếp thị và trạng thái đăng tải",
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
        AddColumn("ShopeeAffLink", "Link tiếp thị", 26F);
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
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "FbStatus",
            HeaderText = "Trạng thái FB",
            FillWeight = 18F,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
            Items = { "Chưa up Facebook", "Đã up Facebook" },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.CellClick += GridCellClick;
        _grid.CellEndEdit += GridCellEndEdit;
        _grid.CellDoubleClick += GridCellDoubleClick;
        _grid.DataError += (_, e) => e.ThrowException = false;

        _sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = Color.White,
            Padding = new Padding(0, 0, 16, 0)
        };
        
        var sidebarHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 32 };
        var sidebarTitle = new Label
        {
            Text = "THƯ MỤC (CHIẾN DỊCH)",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(96, 82, 218),
            TextAlign = ContentAlignment.MiddleLeft
        };
        
        var btnManageFolders = new Button
        {
            Text = "⚙️",
            Dock = DockStyle.Right,
            Width = 32,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnManageFolders.FlatAppearance.BorderSize = 0;
        btnManageFolders.Click += (_, _) => FolderCrudRequested?.Invoke(this, EventArgs.Empty);
        
        sidebarHeaderPanel.Controls.Add(sidebarTitle);
        sidebarHeaderPanel.Controls.Add(btnManageFolders);

        _folderList = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9F),
            CheckOnClick = true,
            BackColor = Color.White,
            DisplayMember = "Name",
            ValueMember = "Id"
        };
        _folderList.ItemCheck += (_, _) => BeginInvoke(new Action(ApplyFilter));
        _sidebar.Controls.Add(_folderList);
        _sidebar.Controls.Add(sidebarHeaderPanel);

        Controls.Add(_grid);
        Controls.Add(_sidebar);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    public void SetData(IReadOnlyList<JobItem> products, IReadOnlyList<FolderItem> folders)
    {
        _allProducts = products ?? [];
        _allFolders = folders ?? [];
        UpdateFolderList();
        ApplyFilter();
    }

    private void UpdateFolderList()
    {
        var checkedIds = _folderList.CheckedItems.Cast<FolderItem>().Select(f => f.Id).ToHashSet();
        _folderList.Items.Clear();
        
        var unassigned = new FolderItem { Id = 0, Name = "[Chưa phân loại]" };
        _folderList.Items.Add(unassigned, checkedIds.Contains(0));
        
        foreach (var folder in _allFolders)
        {
            _folderList.Items.Add(folder, checkedIds.Contains(folder.Id));
        }
    }

    private void ApplyFilter()
    {
        if (_isFiltering) return;
        _isFiltering = true;
        try
        {
            _grid.Rows.Clear();
            var checkedIds = _folderList.CheckedItems.Cast<FolderItem>().Select(f => f.Id).ToHashSet();
            
            for (var i = 0; i < _allProducts.Count; i++)
            {
                var product = _allProducts[i];
                var folderId = product.FolderId ?? 0;
                
                if (checkedIds.Count == 0 || checkedIds.Contains(folderId))
                {
                    AddProductRow(i, product);
                }
            }
            RefreshSuggestedLinks();
        }
        finally
        {
            _isFiltering = false;
        }
    }
    

    public void FilterByFolder(int folderId)
    {
        for (int i = 0; i < _folderList.Items.Count; i++)
        {
            if (_folderList.Items[i] is FolderItem folder)
            {
                _folderList.SetItemChecked(i, folderId == -1 || folder.Id == folderId);
            }
        }
        ApplyFilter();
    }

    public IReadOnlyList<JobItem> GetFilteredJobs()
    {
        var checkedIds = _folderList.CheckedItems.Cast<FolderItem>().Select(f => f.Id).ToHashSet();
        if (checkedIds.Count == 0) return _allProducts;
        return _allProducts.Where(p => checkedIds.Contains(p.FolderId ?? 0)).ToList();
    }

    public void UpdateProduct(int index, JobItem product)
    {
        DataGridViewRow? targetRow = null;
        foreach (DataGridViewRow r in _grid.Rows)
        {
            if (r.Tag is int idx && idx == index)
            {
                targetRow = r;
                break;
            }
        }
        if (targetRow == null) return;
        var row = targetRow;
        row.Cells["VideoPath"].Value = product.VideoPath;
        row.Cells["Title"].Value = product.Title;
        row.Cells["SuggestedAffLink"].Value = string.Empty;
        row.Cells["ShopeeAffLink"].Value = product.ShopeeAffLink;
        row.Cells["Status"].Value = product.Status;
        row.Cells["ShopeeStatus"].Value = product.ShopeeStatus;
        row.Cells["FbStatus"].Value = product.FbStatus;
        row.Cells["ShopeeStatus"].Style.ForeColor = GetStatusColor(product.ShopeeStatus, "Đã up Shopee");
        row.Cells["FbStatus"].Style.ForeColor = GetStatusColor(product.FbStatus, "Đã up Facebook");
        StyleStatusCell(row.Cells["Status"], product.Status);
        RefreshSuggestedLinks();
    }

    public int SelectedIndex => _grid.CurrentRow?.Tag is int idx ? idx : -1;
    public IReadOnlyList<int> SelectedIndices => _grid.SelectedRows
        .Cast<DataGridViewRow>()
        .Where(row => !row.IsNewRow && row.Tag is int)
        .Select(row => (int)row.Tag)
        .OrderBy(index => index)
        .ToList();

    private void AddProductRow(int originalIndex, JobItem product)
    {
        var rowIndex = _grid.Rows.Add(product.VideoPath, product.Title, string.Empty, product.ShopeeAffLink, product.Status, product.ShopeeStatus, product.FbStatus);
        var row = _grid.Rows[rowIndex];
        row.Tag = originalIndex;
        row.Cells["ShopeeStatus"].Style.ForeColor = GetStatusColor(product.ShopeeStatus, "Đã up Shopee");
        row.Cells["FbStatus"].Style.ForeColor = GetStatusColor(product.FbStatus, "Đã up Facebook");
        StyleStatusCell(row.Cells["Status"], product.Status);
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

    private void RaiseProductChanged(int gridRowIndex)
    {
        if (gridRowIndex < 0 || gridRowIndex >= _grid.Rows.Count) return;

        var row = _grid.Rows[gridRowIndex];
        if (row.Tag is not int originalIndex) return;

        ProductChanged?.Invoke(this, new ProductChangedEventArgs(originalIndex, new JobItem
        {
            VideoPath = Convert.ToString(row.Cells["VideoPath"].Value) ?? string.Empty,
            Title = Convert.ToString(row.Cells["Title"].Value) ?? string.Empty,
            ShopeeAffLink = Convert.ToString(row.Cells["ShopeeAffLink"].Value) ?? string.Empty,
            ShopeeStatus = Convert.ToString(row.Cells["ShopeeStatus"].Value) ?? "Chưa up Shopee",
            FbStatus = Convert.ToString(row.Cells["FbStatus"].Value) ?? "Chưa up Facebook"
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

    private static Color GetStatusColor(string status, string successText)
        => status == successText
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

