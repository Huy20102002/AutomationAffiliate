using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

/// <summary>
/// Module quản lý danh sách sản phẩm dùng chung với workflow.
/// </summary>
public sealed class ProductListControl : UserControl
{
    private int _activeFolderId = -1;
    private readonly Guna.UI2.WinForms.Guna2DataGridView _grid;
    private Guna.UI2.WinForms.Guna2TextBox _txtSearch = null!;
    private Guna.UI2.WinForms.Guna2Button _btnClearSearch = null!;
    private Label _lblSearchCount = null!;
    private IReadOnlyList<JobItem> _allProducts = [];
    private IReadOnlyList<FolderItem> _allFolders = [];
    private bool _isFiltering = false;
    private bool _showDuplicatesOnly = false;

    public event EventHandler? ImportRequested;
    public event EventHandler? AddRequested;
    public event EventHandler? EditRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? RunWorkflowRequested;
    public event EventHandler? RunSelectedWorkflowRequested;
    public event EventHandler? StopWorkflowRequested;
    public event EventHandler? ResetStatusesRequested;
    public event EventHandler? ResetSelectedStatusesRequested;
    public event EventHandler? WorkflowRequested;
    public event EventHandler? BackupDbRequested;
    public event EventHandler? RestoreDbRequested;
    public event EventHandler? CampaignsRequested;
    public event EventHandler? GenerateAiTitleRequested;
    public event EventHandler? GenerateSelectedAiTitleRequested;
    public event EventHandler? RegenerateAiTitleRequested;
    public event EventHandler? RegenerateSelectedAiTitleRequested;
    public event EventHandler<ProductChangedEventArgs>? ProductChanged;

    private Guna.UI2.WinForms.Guna2Button _btnRun = null!;
    private Guna.UI2.WinForms.Guna2Button _btnStop = null!;
    private Guna.UI2.WinForms.Guna2Button _btnGenerateAiTitle = null!;

    public void SetRunningState(bool isRunning)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetRunningState(isRunning));
            return;
        }
        if (_btnRun != null) _btnRun.Enabled = !isRunning;
        if (_btnStop != null) _btnStop.Enabled = isRunning;
    }

    public void SetAiTitleGeneratingState(bool isGenerating)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetAiTitleGeneratingState(isGenerating));
            return;
        }
        if (_btnGenerateAiTitle != null)
        {
            if (isGenerating)
            {
                _btnGenerateAiTitle.Text = "⏳ Đang tạo AI...";
                _btnGenerateAiTitle.FillColor = Color.FromArgb(147, 51, 234);
                _btnGenerateAiTitle.DisabledState.FillColor = Color.FromArgb(147, 51, 234);
                _btnGenerateAiTitle.DisabledState.ForeColor = Color.White;
                _btnGenerateAiTitle.Enabled = false;
            }
            else
            {
                _btnGenerateAiTitle.Text = "✨ Tạo tiêu đề AI";
                _btnGenerateAiTitle.FillColor = Color.FromArgb(79, 70, 229);
                _btnGenerateAiTitle.Enabled = true;
            }
        }
    }

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
        var searchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 16, 16, 0),
            BackColor = Color.Transparent
        };

        _txtSearch = new Guna.UI2.WinForms.Guna2TextBox
        {
            PlaceholderText = "🔍  Tìm kiếm tiêu đề, video, link, trạng thái...",
            Width = 330,
            Height = 36,
            BorderRadius = 7,
            BorderColor = Color.FromArgb(209, 213, 219),
            FillColor = Color.FromArgb(248, 250, 252),
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(0, 0, 6, 0)
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                _txtSearch.Text = "";
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        _btnClearSearch = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "✕",
            Width = 36,
            Height = 36,
            BorderRadius = 7,
            FillColor = Color.FromArgb(243, 244, 246),
            ForeColor = Color.FromArgb(107, 114, 128),
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            Visible = false
        };
        _btnClearSearch.Click += (_, _) => _txtSearch.Text = "";

        _lblSearchCount = new Label
        {
            Text = "0 sản phẩm",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.FromArgb(79, 70, 229),
            BackColor = Color.FromArgb(238, 242, 255),
            Padding = new Padding(8, 6, 8, 6),
            Margin = new Padding(0, 4, 0, 0)
        };

        searchPanel.Controls.Add(_txtSearch);
        searchPanel.Controls.Add(_btnClearSearch);
        searchPanel.Controls.Add(_lblSearchCount);

        header.Controls.Add(searchPanel);
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(14, 7, 14, 7)
        };
        var btnImport = CreateButton("Nhập Excel", Color.FromArgb(96, 82, 218), 112);
        var btnAdd = CreateButton("Thêm", Color.FromArgb(10, 151, 205), 78);
        var btnEdit = CreateButton("Sửa", Color.FromArgb(228, 240, 249), 72);
        var btnDelete = CreateButton("Xóa", Color.FromArgb(241, 226, 229), 72);
        _btnRun = CreateButton("▶ Chạy workflow", Color.FromArgb(0, 161, 112), 136);
        _btnStop = CreateButton("■ Dừng", Color.FromArgb(220, 38, 38), 85);
        _btnStop.Enabled = false;
        var btnReset = CreateButton("Đặt lại trạng thái", Color.FromArgb(235, 237, 242), 130);
        var btnWorkflow = CreateButton("Mở Workflow", Color.FromArgb(228, 240, 249), 110);
        var btnBackup = CreateButton("Sao lưu DB", Color.FromArgb(114, 117, 134), 100);
        var btnRestore = CreateButton("Phục hồi DB", Color.FromArgb(114, 117, 134), 100);
        var btnCampaigns = CreateButton("Quản lý chiến dịch", Color.FromArgb(96, 82, 218), 130);
        _btnGenerateAiTitle = CreateButton("✨ Tạo tiêu đề AI", Color.FromArgb(79, 70, 229), 136);
        var btnFilterDuplicates = CreateButton("Lọc video trùng", Color.FromArgb(249, 189, 82), 120);

        btnImport.Click += (_, _) => ImportRequested?.Invoke(this, EventArgs.Empty);
        btnAdd.Click += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
        btnEdit.Click += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);
        btnDelete.Click += (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty);
        _btnRun.Click += (_, _) => RunWorkflowRequested?.Invoke(this, EventArgs.Empty);
        _btnStop.Click += (_, _) => StopWorkflowRequested?.Invoke(this, EventArgs.Empty);
        btnReset.Click += (_, _) => ResetStatusesRequested?.Invoke(this, EventArgs.Empty);
        btnWorkflow.Click += (_, _) => WorkflowRequested?.Invoke(this, EventArgs.Empty);
        btnBackup.Click += (_, _) => BackupDbRequested?.Invoke(this, EventArgs.Empty);
        btnRestore.Click += (_, _) => RestoreDbRequested?.Invoke(this, EventArgs.Empty);
        btnCampaigns.Click += (_, _) => CampaignsRequested?.Invoke(this, EventArgs.Empty);
        _btnGenerateAiTitle.Click += (_, _) => GenerateAiTitleRequested?.Invoke(this, EventArgs.Empty);

        var btnAiMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5F), RenderMode = ToolStripRenderMode.System };
        var itemMenuGenerate = new ToolStripMenuItem("✨ Tạo tiêu đề AI (bỏ qua video đã có)")
        {
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(79, 70, 229)
        };
        itemMenuGenerate.Click += (_, _) => GenerateAiTitleRequested?.Invoke(this, EventArgs.Empty);

        var itemMenuRegenerate = new ToolStripMenuItem("🔄 Tạo lại tiêu đề AI (ghi đè tất cả video đã chọn / hiển thị)")
        {
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(124, 58, 237)
        };
        itemMenuRegenerate.Click += (_, _) => RegenerateSelectedAiTitleRequested?.Invoke(this, EventArgs.Empty);

        btnAiMenu.Items.Add(itemMenuGenerate);
        btnAiMenu.Items.Add(itemMenuRegenerate);
        _btnGenerateAiTitle.ContextMenuStrip = btnAiMenu;
        
        btnFilterDuplicates.Click += (_, _) =>
        {
            _showDuplicatesOnly = !_showDuplicatesOnly;
            btnFilterDuplicates.Text = _showDuplicatesOnly ? "Hủy lọc trùng" : "Lọc video trùng";
            btnFilterDuplicates.FillColor = _showDuplicatesOnly ? Color.FromArgb(226, 82, 82) : Color.FromArgb(249, 189, 82);
            ApplyFilter();
        };

        toolbar.Controls.AddRange([btnImport, btnAdd, btnEdit, btnDelete, btnFilterDuplicates, _btnRun, _btnStop, btnReset, btnWorkflow, btnBackup, btnRestore, btnCampaigns, _btnGenerateAiTitle]);

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
            EnableHeadersVisualStyles = false,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable
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
        AddColumn("VideoPath", "Đường dẫn video / ảnh", 20F);
        AddColumn("Title", "Tiêu đề", 42F);
        AddColumn("ShopeeAffLink", "Link tiếp thị", 20F);
        AddColumn("Status", "Trạng thái", 10F);
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "ShopeeStatus",
            HeaderText = "Trạng thái Shopee",
            FillWeight = 14F,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
            Items = { "Chưa up Shopee", "Đã up Shopee" },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "FbStatus",
            HeaderText = "Trạng thái FB",
            FillWeight = 14F,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
            Items = { "Chưa up Facebook", "Đã up Facebook" },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.CellClick += GridCellClick;
        _grid.CellEndEdit += GridCellEndEdit;
        _grid.CellDoubleClick += GridCellDoubleClick;
        _grid.MouseDown += GridMouseDown;
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.KeyDown += (s, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedContent();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        Controls.Add(_grid);
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
        // Campaigns are owned by MainForm/DatabaseService. This control only
        // keeps the snapshot needed for filtering and editing.
        if (_activeFolderId > 0 && !_allFolders.Any(folder => folder.Id == _activeFolderId))
            _activeFolderId = -1;
    }

    private static bool MatchesSearch(JobItem p, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var fileName = Path.GetFileName(p.VideoPath);
        var searchableText = $"{p.Title} {p.VideoPath} {fileName} {p.ShopeeAffLink} {p.Status} {p.ShopeeStatus} {p.FbStatus} {p.Id}";
        return tokens.All(token => searchableText.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F))
        {
            if (_txtSearch != null)
            {
                _txtSearch.Focus();
                _txtSearch.SelectAll();
            }
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ApplyFilter()
    {
        if (_isFiltering) return;
        _isFiltering = true;
        try
        {
            _grid.Rows.Clear();
            var checkedIds = _activeFolderId != -1 ? new HashSet<int> { _activeFolderId } : new HashSet<int>();
            
            HashSet<string>? duplicatePaths = null;
            if (_showDuplicatesOnly)
            {
                duplicatePaths = _allProducts
                    .GroupBy(p => p.VideoPath, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key))
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            var query = _txtSearch?.Text?.Trim();
            var hasQuery = !string.IsNullOrWhiteSpace(query);
            if (_btnClearSearch != null) _btnClearSearch.Visible = hasQuery;

            var productsToDisplay = _allProducts.Select((p, i) => new { Product = p, OriginalIndex = i })
                .Where(x => checkedIds.Count == 0 || checkedIds.Contains(x.Product.FolderId ?? 0))
                .Where(x => !_showDuplicatesOnly || (duplicatePaths != null && duplicatePaths.Contains(x.Product.VideoPath)))
                .Where(x => !hasQuery || MatchesSearch(x.Product, query!))
                .ToList();

            if (_showDuplicatesOnly)
            {
                productsToDisplay = productsToDisplay.OrderBy(x => x.Product.VideoPath).ToList();
            }

            foreach (var item in productsToDisplay)
            {
                AddProductRow(item.OriginalIndex, item.Product);
            }

            if (_lblSearchCount != null)
            {
                if (hasQuery || _showDuplicatesOnly || checkedIds.Count > 0)
                {
                    _lblSearchCount.Text = $"{productsToDisplay.Count} / {_allProducts.Count} sản phẩm";
                }
                else
                {
                    _lblSearchCount.Text = $"{_allProducts.Count} sản phẩm";
                }
            }
        }
        finally
        {
            _isFiltering = false;
        }
    }

    public void FilterByFolder(int folderId)
    {
        _activeFolderId = folderId;
        ApplyFilter();
    }

    public IReadOnlyList<int> CheckedFolderIds => _activeFolderId != -1 ? new List<int> { _activeFolderId } : new List<int>();

    public IReadOnlyList<JobItem> GetFilteredJobs()
    {
        if (_activeFolderId == -1) return _allProducts;
        return _allProducts.Where(p => (p.FolderId ?? 0) == _activeFolderId).ToList();
    }

    public void UpdateProduct(int index, JobItem product)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateProduct(index, product));
            return;
        }

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
        row.Cells["Title"].ToolTipText = product.IsAiTitleGenerated ? "✨ Tiêu đề đã được tạo bằng AI" : string.Empty;
        row.Cells["ShopeeAffLink"].Value = product.ShopeeAffLink;
        row.Cells["ShopeeStatus"].Value = product.ShopeeStatus;
        row.Cells["ShopeeStatus"].Style.ForeColor = GetStatusColor(product.ShopeeStatus, "Đã up Shopee");
        row.Cells["FbStatus"].Value = product.FbStatus;
        row.Cells["FbStatus"].Style.ForeColor = GetStatusColor(product.FbStatus, "Đã up Facebook");
        row.Cells["Status"].Value = product.Status;
        StyleStatusCell(row.Cells["Status"], product.Status);

        _grid.InvalidateRow(row.Index);
        _grid.Update();
    }

    public int SelectedIndex
    {
        get
        {
            var selected = SelectedIndices;
            return selected.Count > 0 ? selected[0] : (_grid.CurrentRow?.Tag is int idx ? idx : -1);
        }
    }

    public IReadOnlyList<int> SelectedIndices
    {
        get
        {
            var indices = new HashSet<int>();
            foreach (DataGridViewRow row in _grid.SelectedRows)
            {
                if (!row.IsNewRow && row.Tag is int idx && idx >= 0)
                    indices.Add(idx);
            }
            if (indices.Count == 0)
            {
                foreach (DataGridViewCell cell in _grid.SelectedCells)
                {
                    if (cell.RowIndex >= 0 && cell.RowIndex < _grid.Rows.Count)
                    {
                        var row = _grid.Rows[cell.RowIndex];
                        if (!row.IsNewRow && row.Tag is int idx && idx >= 0)
                            indices.Add(idx);
                    }
                }
            }
            if (indices.Count == 0 && _grid.CurrentRow?.Tag is int currentIdx && currentIdx >= 0)
            {
                indices.Add(currentIdx);
            }
            return indices.OrderBy(index => index).ToList();
        }
    }

    private void AddProductRow(int originalIndex, JobItem product)
    {
        var rowIndex = _grid.Rows.Add(product.VideoPath, product.Title, product.ShopeeAffLink, product.Status, product.ShopeeStatus, product.FbStatus);
        var row = _grid.Rows[rowIndex];
        row.Tag = originalIndex;
        row.Cells["ShopeeStatus"].Style.ForeColor = GetStatusColor(product.ShopeeStatus, "Đã up Shopee");
        row.Cells["FbStatus"].Style.ForeColor = GetStatusColor(product.FbStatus, "Đã up Facebook");
        row.Cells["Title"].ToolTipText = product.IsAiTitleGenerated ? "✨ Tiêu đề đã được tạo bằng AI" : string.Empty;
        StyleStatusCell(row.Cells["Status"], product.Status);
    }

    private void GridMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;

        var hit = _grid.HitTest(e.X, e.Y);
        if (hit.RowIndex >= 0)
        {
            if (!_grid.Rows[hit.RowIndex].Selected)
            {
                _grid.ClearSelection();
                _grid.Rows[hit.RowIndex].Selected = true;
                if (hit.ColumnIndex >= 0)
                    _grid.CurrentCell = _grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
            }
            ShowContextMenu(Cursor.Position);
        }
        else if (SelectedIndices.Count > 0)
        {
            ShowContextMenu(Cursor.Position);
        }
    }

    private void ShowContextMenu(Point screenPoint)
    {
        var selectedIndices = SelectedIndices;
        if (selectedIndices.Count == 0) return;

        var selectedJobs = selectedIndices
            .Where(i => i >= 0 && i < _allProducts.Count)
            .Select(i => _allProducts[i])
            .ToList();

        var menu = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9.5F),
            RenderMode = ToolStripRenderMode.System
        };

        var count = selectedIndices.Count;
        var runText = count > 1
            ? $"▶ Chạy quy trình {count} video đã chọn"
            : "▶ Chạy quy trình video đã chọn";

        var itemRun = new ToolStripMenuItem(runText)
        {
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(0, 140, 95)
        };
        itemRun.Click += (_, _) => RunSelectedWorkflowRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(itemRun);

        menu.Items.Add(new ToolStripSeparator());

        var editText = count > 1 ? $"✏️ Sửa hàng loạt ({count} video)..." : "✏️ Sửa video này...";
        var itemEdit = new ToolStripMenuItem(editText);
        itemEdit.Click += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(itemEdit);

        var deleteText = count > 1 ? $"🗑️ Xóa {count} video đã chọn" : "🗑️ Xóa video này";
        var itemDelete = new ToolStripMenuItem(deleteText)
        {
            ForeColor = Color.FromArgb(200, 50, 50)
        };
        itemDelete.Click += (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(itemDelete);

        var resetText = count > 1 ? $"🔄 Đặt lại trạng thái {count} video (Chờ)" : "🔄 Đặt lại trạng thái video này (Chờ)";
        var itemReset = new ToolStripMenuItem(resetText);
        itemReset.Click += (_, _) => ResetSelectedStatusesRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(itemReset);

        var aiTitleText = count > 1 ? $"✨ Tạo tiêu đề AI ({count} video đã chọn)..." : "✨ Tạo tiêu đề AI cho video này...";
        var itemAiTitle = new ToolStripMenuItem(aiTitleText)
        {
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(79, 70, 229)
        };
        itemAiTitle.Click += (_, _) => GenerateSelectedAiTitleRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(itemAiTitle);

        var reAiTitleText = count > 1 ? $"🔄 Tạo lại tiêu đề AI ({count} video đã chọn)..." : "🔄 Tạo lại tiêu đề AI cho video này...";
        var itemReAiTitle = new ToolStripMenuItem(reAiTitleText)
        {
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(124, 58, 237)
        };
        itemReAiTitle.Click += (_, _) => RegenerateSelectedAiTitleRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(itemReAiTitle);

        menu.Items.Add(new ToolStripSeparator());

        var curColHeader = _grid.CurrentCell?.OwningColumn?.HeaderText ?? "ô";
        var itemCopyCell = new ToolStripMenuItem($"📋 Sao chép {curColHeader} (Ctrl+C)");
        itemCopyCell.Click += (_, _) => CopySelectedContent();
        menu.Items.Add(itemCopyCell);

        var itemCopyPath = new ToolStripMenuItem("📁 Sao chép đường dẫn video / ảnh");
        itemCopyPath.Click += (_, _) =>
        {
            var paths = selectedJobs.Select(j => j.VideoPath).Where(p => !string.IsNullOrEmpty(p));
            var text = string.Join(Environment.NewLine, paths);
            if (!string.IsNullOrEmpty(text))
            {
                try { Clipboard.SetText(text); } catch { /* ignore */ }
            }
        };
        menu.Items.Add(itemCopyPath);

        var itemCopyLink = new ToolStripMenuItem("🔗 Sao chép link tiếp thị");
        itemCopyLink.Click += (_, _) =>
        {
            var links = selectedJobs.Select(j => j.ShopeeAffLink).Where(l => !string.IsNullOrEmpty(l));
            var text = string.Join(Environment.NewLine, links);
            if (!string.IsNullOrEmpty(text))
            {
                try { Clipboard.SetText(text); } catch { /* ignore */ }
            }
        };
        menu.Items.Add(itemCopyLink);

        var itemCopyTitle = new ToolStripMenuItem("📝 Sao chép tiêu đề");
        itemCopyTitle.Click += (_, _) =>
        {
            var titles = selectedJobs.Select(j => j.Title).Where(t => !string.IsNullOrEmpty(t));
            var text = string.Join(Environment.NewLine, titles);
            if (!string.IsNullOrEmpty(text))
            {
                try { Clipboard.SetText(text); } catch { /* ignore */ }
            }
        };
        menu.Items.Add(itemCopyTitle);

        menu.Show(screenPoint);
    }

    private void CopySelectedContent()
    {
        var selectedIndices = SelectedIndices;
        if (selectedIndices.Count == 0) return;

        var selectedJobs = selectedIndices
            .Where(i => i >= 0 && i < _allProducts.Count)
            .Select(i => _allProducts[i])
            .ToList();
        if (selectedJobs.Count == 0) return;

        var col = _grid.CurrentCell?.OwningColumn?.Name ?? "ShopeeAffLink";
        IEnumerable<string> values;
        if (col == "VideoPath")
            values = selectedJobs.Select(j => j.VideoPath).Where(p => !string.IsNullOrEmpty(p));
        else if (col == "Title")
            values = selectedJobs.Select(j => j.Title).Where(t => !string.IsNullOrEmpty(t));
        else if (col == "Status")
            values = selectedJobs.Select(j => j.Status).Where(s => !string.IsNullOrEmpty(s));
        else
            values = selectedJobs.Select(j => j.ShopeeAffLink).Where(l => !string.IsNullOrEmpty(l));

        var text = string.Join(Environment.NewLine, values);
        if (!string.IsNullOrEmpty(text))
        {
            try { Clipboard.SetText(text); } catch { }
        }
    }

    private void AddColumn(string name, string headerText, float fillWeight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = headerText,
            FillWeight = fillWeight,
            ReadOnly = name == "VideoPath" || name == "Status",
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void GridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        var columnName = _grid.Columns[e.ColumnIndex].Name;

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

        if (originalIndex < 0 || originalIndex >= _allProducts.Count) return;
        var current = _allProducts[originalIndex];
        ProductChanged?.Invoke(this, new ProductChangedEventArgs(originalIndex, new JobItem
        {
            Id = current.Id,
            FolderId = current.FolderId,
            VideoPath = Convert.ToString(row.Cells["VideoPath"].Value) ?? string.Empty,
            Title = Convert.ToString(row.Cells["Title"].Value) ?? string.Empty,
            ShopeeAffLink = Convert.ToString(row.Cells["ShopeeAffLink"].Value) ?? string.Empty,
            Status = current.Status,
            ShopeeStatus = Convert.ToString(row.Cells["ShopeeStatus"].Value) ?? JobStatus.ShopeePending,
            FbStatus = Convert.ToString(row.Cells["FbStatus"].Value) ?? JobStatus.FacebookPending,
            Log = current.Log,
            Data = new Dictionary<string, string>(current.Data, StringComparer.OrdinalIgnoreCase)
        }));
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
