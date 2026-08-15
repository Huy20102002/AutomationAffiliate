using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

/// <summary>Form thêm/sửa sản phẩm. Chế độ Thêm hỗ trợ chọn nhiều file, mỗi file có link riêng.</summary>
public sealed class ProductEditorDialog : Form
{
    // ── Chế độ sửa ──
    private Guna.UI2.WinForms.Guna2TextBox? _videoPath;
    private Guna.UI2.WinForms.Guna2TextBox? _title;
    private Guna.UI2.WinForms.Guna2TextBox? _affiliateLink;
    private Guna.UI2.WinForms.Guna2ComboBox? _status;
    private Guna.UI2.WinForms.Guna2ComboBox? _fbStatus;

    // ── Chế độ thêm hàng loạt ──
    private DataGridView? _bulkGrid;
    private ComboBox? _bulkLinkPicker;
    private CheckBox? _bulkSuggestionsToggle;
    private CheckBox? _chkUseFileName;
    private Label? _fileCountLabel;

    private Guna.UI2.WinForms.Guna2ComboBox? _folderPicker;
    private readonly IReadOnlyList<FolderItem> _folders;
    private readonly JobItem? _original;
    private readonly HashSet<string> _existingPaths;
    private readonly bool _isEditMode;
    private readonly bool _isBulkEditMode;

    public JobItem Result { get; private set; } = new();
    public List<JobItem> BulkResults { get; } = [];
    public bool IsBulkAdd => BulkResults.Count > 0;

    public ProductEditorDialog(JobItem? product = null, IEnumerable<string>? existingPaths = null, IEnumerable<FolderItem>? folders = null)
    {
        _original = product;
        _isEditMode = product != null;
        _existingPaths = new HashSet<string>(existingPaths ?? [], StringComparer.OrdinalIgnoreCase);
        _folders = folders?.ToList() ?? [];

        Text = _isEditMode ? "Sửa sản phẩm" : "Thêm sản phẩm";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = _isEditMode ? false : true;
        ShowInTaskbar = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        if (_isEditMode)
            BuildEditMode(product!);
        else
            BuildBulkAddMode();
    }

    public ProductEditorDialog(IReadOnlyList<JobItem> productsToEdit, IEnumerable<JobItem>? allProducts = null, IEnumerable<FolderItem>? folders = null)
    {
        _original = null;
        _isEditMode = false;
        _isBulkEditMode = true;
        _existingPaths = new HashSet<string>(allProducts?.Select(j => j.VideoPath) ?? [], StringComparer.OrdinalIgnoreCase);
        _folders = folders?.ToList() ?? [];

        Text = "Sửa nhiều sản phẩm";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        BuildBulkAddMode(productsToEdit, allProducts);
    }

    // ═══════════════════════════════════════════════════════
    //  CHẾ ĐỘ SỬA (1 sản phẩm)
    // ═══════════════════════════════════════════════════════
    private void BuildEditMode(JobItem product)
    {
        ClientSize = new Size(640, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 0) };

        // Video path
        var lblVideo = CreateLabel("Video", 0, 8);
        _videoPath = CreateTextBox(product.VideoPath);
        _videoPath.ReadOnly = true;
        _videoPath.SetBounds(110, 4, 410, 32);
        _videoPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var btnBrowse = CreateSmallButton("...", 46);
        btnBrowse.SetBounds(524, 4, 46, 32);
        btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowse.Click += (_, _) => BrowseSingleFile();

        // Title
        var lblTitle = CreateLabel("Tiêu đề", 0, 52);
        _title = CreateTextBox(product.Title);
        _title.SetBounds(110, 48, 460, 32);
        _title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // Hashtags
        var hashPanel = CreateHashtagPanel(94);

        // Aff link
        var lblLink = CreateLabel("Link tiếp thị", 0, 130);
        _affiliateLink = CreateTextBox(product.ShopeeAffLink);
        _affiliateLink.SetBounds(110, 146, 460, 32);
        _affiliateLink.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // Status
        var lblStatus = CreateLabel("Trạng thái", 0, 174);
        _status = new Guna.UI2.WinForms.Guna2ComboBox
        {
            FillColor = Color.White, BorderRadius = 7, BorderThickness = 1,
            BorderColor = Color.FromArgb(190, 198, 211),
            FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _status.SetBounds(110, 190, 460, 32);
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _status.Items.AddRange(["Chưa up Shopee", "Đã up Shopee"]);
        _status.SelectedItem = product.ShopeeStatus;


        // Folder
        var lblFolder = CreateLabel("Chiến dịch", 0, 218);
        _folderPicker = new Guna.UI2.WinForms.Guna2ComboBox
        {
            FillColor = Color.White, BorderRadius = 7, BorderThickness = 1,
            BorderColor = Color.FromArgb(190, 198, 211),
            FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "Name", ValueMember = "Id"
        };
        _folderPicker.SetBounds(110, 234, 460, 32);
        _folderPicker.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _folderPicker.Items.Add(new FolderItem { Id = 0, Name = "[Chưa phân loại]" });
        foreach (var f in _folders) _folderPicker.Items.Add(f);
        
        var currentFolderId = product.FolderId ?? 0;
        foreach (var item in _folderPicker.Items)
        {
            if (item is FolderItem fi && fi.Id == currentFolderId)
            {
                _folderPicker.SelectedItem = item;
                break;
            }
        }

        // Fb Status
        var lblFbStatus = CreateLabel("Trạng thái FB", 0, 262);
        _fbStatus = new Guna.UI2.WinForms.Guna2ComboBox
        {
            FillColor = Color.White, BorderRadius = 7, BorderThickness = 1,
            BorderColor = Color.FromArgb(190, 198, 211),
            FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _fbStatus.SetBounds(110, 278, 460, 32);
        _fbStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _fbStatus.Items.AddRange(["Chưa up Facebook", "Đã up Facebook"]);
        _fbStatus.SelectedItem = product.FbStatus;

        mainPanel.Controls.AddRange([lblVideo, _videoPath, btnBrowse, lblTitle, _title, hashPanel, lblLink, _affiliateLink, lblStatus, _status, lblFolder, _folderPicker, lblFbStatus, _fbStatus]);


        // Buttons
        var buttons = CreateButtonPanel("Lưu", 92);
        Controls.Add(buttons);
        Controls.Add(mainPanel);
    }

    // ═══════════════════════════════════════════════════════
    //  CHẾ ĐỘ THÊM HÀNG LOẠT
    // ═══════════════════════════════════════════════════════
    private void BuildBulkAddMode(IReadOnlyList<JobItem>? editItems = null, IEnumerable<JobItem>? allProducts = null)
    {
        ClientSize = new Size(1200, 720);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(900, 560);

        // ── Toolbar ──
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 52,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(20, 10, 20, 6),
            BackColor = Color.FromArgb(250, 251, 253)
        };

        var btnAdd = CreateSmallButton("📂 Chọn video", 130);
        btnAdd.FillColor = Color.FromArgb(96, 82, 218);
        btnAdd.ForeColor = Color.White;
        btnAdd.Click += (_, _) => BrowseMultipleFiles();

        var btnRemove = CreateSmallButton("Xóa dòng đã chọn", 140);
        btnRemove.FillColor = Color.FromArgb(241, 226, 229);
        btnRemove.ForeColor = Color.FromArgb(160, 50, 50);
        btnRemove.Click += (_, _) => RemoveSelectedRows();

        _chkUseFileName = new CheckBox
        {
            Text = "Lấy tên file làm tiêu đề",
            AutoSize = true, Checked = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(55, 58, 75),
            Margin = new Padding(16, 6, 0, 0)
        };
        _chkUseFileName.CheckedChanged += (_, _) => ApplyFileNameToTitles();

        _fileCountLabel = new Label
        {
            Text = "0 video",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(140, 145, 165),
            Margin = new Padding(16, 8, 0, 0)
        };

        btnAdd.Visible = !_isBulkEditMode;
        btnRemove.Visible = !_isBulkEditMode;
        _chkUseFileName.Visible = !_isBulkEditMode;
        var lblFolderBulk = new Label
        {
            Text = "Gán chiến dịch:", AutoSize = true,
            ForeColor = Color.FromArgb(90, 95, 115),
            Margin = new Padding(18, 8, 6, 0)
        };
        _folderPicker = new Guna.UI2.WinForms.Guna2ComboBox
        {
            Width = 200, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "Name", ValueMember = "Id",
            Margin = new Padding(0, 1, 0, 0)
        };
        _folderPicker.Items.Add(new FolderItem { Id = 0, Name = "[Chưa phân loại]" });
        foreach (var f in _folders) _folderPicker.Items.Add(f);
        _folderPicker.SelectedIndex = 0;
        
        toolbar.Controls.AddRange([lblFolderBulk, _folderPicker]);

        if (_isBulkEditMode)
        {
            _bulkSuggestionsToggle = new CheckBox
            {
                Text = "Gợi ý tự động", Checked = true, AutoSize = true,
                Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(90, 95, 115),
                Margin = new Padding(12, 7, 0, 0)
            };
            _bulkSuggestionsToggle.CheckedChanged += (_, _) => RefreshBulkLinkSuggestions(allProducts);
            var linkLabel = new Label
            {
                Text = "Chọn link aff:", AutoSize = true,
                ForeColor = Color.FromArgb(90, 95, 115),
                Margin = new Padding(18, 8, 6, 0)
            };
            _bulkLinkPicker = new ComboBox
            {
                Width = 360, Height = 28, DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Margin = new Padding(0, 1, 0, 0)
            };
            _bulkLinkPicker.SelectedIndexChanged += (_, _) => ApplyPickedBulkLink();
            toolbar.Controls.AddRange([linkLabel, _bulkLinkPicker, _bulkSuggestionsToggle]);
        }
        toolbar.Controls.AddRange([btnAdd, btnRemove, _chkUseFileName, _fileCountLabel]);

        // ── Hashtag panel ──
        var hashBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 68,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(20, 6, 20, 4),
            BackColor = Color.White
        };
        var lblHashHint = new Label
        {
            Text = "Thêm vào tiêu đề:",
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(140, 145, 165),
            Margin = new Padding(0, 4, 6, 0)
        };
        hashBar.Controls.Add(lblHashHint);
        foreach (var tag in new[] { "#shopee", "#shopeevideo", "#shopeecreator", "#review", "#shopeevn", "#shopeelive", "#shopeehaul", "#shopeedeal", "#shopeeaffiliate", "#shopeefinds", "#muashopee", "#sambochinhhang" })
        {
            var btn = new Guna.UI2.WinForms.Guna2Button
            {
                Text = tag, Height = 26, AutoSize = true,
                FillColor = Color.FromArgb(240, 242, 245), ForeColor = Color.FromArgb(70, 70, 85),
                BorderRadius = 13, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8.5F),
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.Click += (_, _) => AppendHashtagToSelected(tag);
            hashBar.Controls.Add(btn);
        }

        // ── DataGridView ──
        _bulkGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(235, 237, 242),
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 36 },
            Font = new Font("Segoe UI", 9F)
        };
        _bulkGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(244, 245, 248),
            ForeColor = Color.FromArgb(96, 82, 218),
            Font = new Font("Segoe UI Semibold", 9F),
            Padding = new Padding(8, 4, 8, 4)
        };
        _bulkGrid.ColumnHeadersHeight = 38;
        _bulkGrid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 31, 44),
            SelectionBackColor = Color.FromArgb(225, 240, 252),
            SelectionForeColor = Color.FromArgb(31, 31, 44),
            Padding = new Padding(6, 2, 6, 2)
        };

        _bulkGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colFile", HeaderText = "File video", ReadOnly = true,
            FillWeight = 35, MinimumWidth = 200
        });
        _bulkGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTitle", HeaderText = "Tiêu đề",
            FillWeight = 35, MinimumWidth = 180
        });
        _bulkGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colSuggestedLink", HeaderText = "Gợi ý link aff",
            FillWeight = 24, MinimumWidth = 180, ReadOnly = true
        });
        _bulkGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLink", HeaderText = "Link tiếp thị (mỗi video 1 link)",
            FillWeight = 30, MinimumWidth = 180
        });
        _bulkGrid.CellClick += BulkGridCellClick;
        _bulkGrid.CellEndEdit += (_, _) => RefreshBulkLinkSuggestions();

        if (editItems != null)
        {
            foreach (var item in editItems)
                _bulkGrid.Rows.Add(item.VideoPath, item.Title, string.Empty, item.ShopeeAffLink);
            RefreshBulkLinkSuggestions(allProducts);
            RefreshBulkLinkPicker(allProducts);
            UpdateFileCount(0);
        }

        // ── Buttons ──
        var buttons = CreateButtonPanel(_isBulkEditMode ? "Lưu thay đổi" : "Thêm tất cả", _isBulkEditMode ? 140 : 130);

        Controls.Add(buttons);
        Controls.Add(_bulkGrid);
        Controls.Add(hashBar);
        Controls.Add(toolbar);
    }

    // ═══════════════════════════════════════════════════════
    //  ACTIONS
    // ═══════════════════════════════════════════════════════
    private void BrowseSingleFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn file video",
            Filter = "Video|*.mp4;*.mov;*.mkv;*.avi;*.webm;*.m4v|Tất cả file|*.*",
            CheckFileExists = true, Multiselect = false,
            FileName = _videoPath?.Text ?? ""
        };
        var dir = Path.GetDirectoryName(_videoPath?.Text ?? "");
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            dialog.InitialDirectory = dir;
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _videoPath!.Text = dialog.FileName;
    }

    private void BrowseMultipleFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn nhiều file video",
            Filter = "Video|*.mp4;*.mov;*.mkv;*.avi;*.webm;*.m4v|Tất cả file|*.*",
            CheckFileExists = true, Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var existingInGrid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in _bulkGrid!.Rows)
            existingInGrid.Add(row.Cells["colFile"].Value?.ToString() ?? "");

        var added = 0;
        foreach (var file in dialog.FileNames)
        {
            if (_existingPaths.Contains(file) || existingInGrid.Contains(file)) continue;
            var title = _chkUseFileName?.Checked == true ? Path.GetFileNameWithoutExtension(file) : "";
            _bulkGrid.Rows.Add(file, title, "", "");
            added++;
        }
        var skipped = dialog.FileNames.Length - added;
        UpdateFileCount(skipped);
    }

    private void RemoveSelectedRows()
    {
        var toRemove = _bulkGrid!.SelectedRows.Cast<DataGridViewRow>().ToList();
        foreach (var row in toRemove)
            if (!row.IsNewRow) _bulkGrid.Rows.Remove(row);
        UpdateFileCount(0);
    }

    private void ApplyFileNameToTitles()
    {
        if (_bulkGrid == null || _chkUseFileName?.Checked != true) return;
        foreach (DataGridViewRow row in _bulkGrid.Rows)
        {
            var file = row.Cells["colFile"].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(file))
                row.Cells["colTitle"].Value = Path.GetFileNameWithoutExtension(file);
        }
    }

    private void AppendHashtagToSelected(string tag)
    {
        if (_bulkGrid == null) return;
        var rows = _bulkGrid.SelectedRows.Count > 0
            ? _bulkGrid.SelectedRows.Cast<DataGridViewRow>()
            : _bulkGrid.Rows.Cast<DataGridViewRow>();

        foreach (var row in rows)
        {
            var current = (row.Cells["colTitle"].Value?.ToString() ?? "").TrimEnd();
            row.Cells["colTitle"].Value = (current + " " + tag).TrimStart();
        }
    }

    private void BulkGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_bulkGrid == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (_bulkGrid.Columns[e.ColumnIndex].Name != "colSuggestedLink") return;

        var suggestion = Convert.ToString(_bulkGrid.Rows[e.RowIndex].Cells["colSuggestedLink"].Value) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(suggestion)) return;
        _bulkGrid.Rows[e.RowIndex].Cells["colLink"].Value = suggestion;
        RefreshBulkLinkSuggestions();
    }

    private void RefreshBulkLinkPicker(IEnumerable<JobItem>? extraProducts = null)
    {
        if (_bulkLinkPicker == null) return;

        var links = new List<(string Link, string Title)>();
        void Add(string? link, string? title)
        {
            link = link?.Trim();
            if (string.IsNullOrWhiteSpace(link) || !link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
            if (links.Any(x => string.Equals(x.Link, link, StringComparison.OrdinalIgnoreCase))) return;
            links.Add((link, title?.Trim() ?? string.Empty));
        }

        if (_bulkGrid != null)
        {
            foreach (DataGridViewRow row in _bulkGrid.Rows)
                Add(Convert.ToString(row.Cells["colLink"].Value), Convert.ToString(row.Cells["colTitle"].Value));
        }
        foreach (var item in extraProducts ?? [])
            Add(item.ShopeeAffLink, item.Title);

        _bulkLinkPicker.BeginUpdate();
        _bulkLinkPicker.Items.Clear();
        foreach (var item in links)
            _bulkLinkPicker.Items.Add(string.IsNullOrWhiteSpace(item.Title)
                ? item.Link
                : $"{item.Link}  —  {item.Title}");
        _bulkLinkPicker.EndUpdate();
    }

    private void ApplyPickedBulkLink()
    {
        if (_bulkGrid == null || _bulkLinkPicker == null || _bulkLinkPicker.SelectedIndex < 0) return;
        var display = _bulkLinkPicker.SelectedItem?.ToString() ?? string.Empty;
        var link = display.Split("  —  ", 2, StringSplitOptions.None)[0].Trim();
        if (string.IsNullOrWhiteSpace(link)) return;

        var rows = _bulkGrid.SelectedRows.Count > 0
            ? _bulkGrid.SelectedRows.Cast<DataGridViewRow>()
            : _bulkGrid.CurrentRow != null ? [ _bulkGrid.CurrentRow ] : [];
        foreach (var row in rows)
            row.Cells["colLink"].Value = link;
        RefreshBulkLinkSuggestions();
        _bulkLinkPicker.Text = string.Empty;
        _bulkGrid.Focus();
    }

    private void RefreshBulkLinkSuggestions(IEnumerable<JobItem>? extraProducts = null)
    {
        if (_bulkGrid == null) return;

        var candidates = new List<(string Title, string Link)>();
        foreach (DataGridViewRow row in _bulkGrid.Rows)
        {
            var title = Convert.ToString(row.Cells["colTitle"].Value) ?? string.Empty;
            var link = Convert.ToString(row.Cells["colLink"].Value) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(link))
                candidates.Add((title, link));
        }

        if (extraProducts != null)
        {
            foreach (var item in extraProducts)
                if (!string.IsNullOrWhiteSpace(item.ShopeeAffLink))
                    candidates.Add((item.Title, item.ShopeeAffLink));
        }

        foreach (DataGridViewRow row in _bulkGrid.Rows)
        {
            var title = Convert.ToString(row.Cells["colTitle"].Value) ?? string.Empty;
            var ownLink = Convert.ToString(row.Cells["colLink"].Value) ?? string.Empty;
            var suggestion = _bulkSuggestionsToggle?.Checked != false
                ? FindSuggestedAffLink(title, ownLink, candidates)
                : string.Empty;
            row.Cells["colSuggestedLink"].Value = suggestion;
            row.Cells["colSuggestedLink"].ToolTipText = suggestion;
        }

        RefreshBulkLinkPicker(extraProducts);
    }

    private static string FindSuggestedAffLink(string title, string ownLink, IEnumerable<(string Title, string Link)> candidates)
    {
        var key = NormalizeTitle(title);
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        string? similar = null;
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.Link)) continue;
            if (string.Equals(candidate.Link, ownLink, StringComparison.OrdinalIgnoreCase)) continue;

            var candidateKey = NormalizeTitle(candidate.Title);
            if (string.IsNullOrWhiteSpace(candidateKey)) continue;
            if (candidateKey == key) return candidate.Link;
            if (similar == null && IsTitleSimilar(key, candidateKey))
                similar = candidate.Link;
        }

        return similar ?? string.Empty;
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

    private void UpdateFileCount(int skipped)
    {
        var count = _bulkGrid!.Rows.Count;
        _fileCountLabel!.Text = $"{count} video" + (skipped > 0 ? $" ({skipped} trùng, bỏ qua)" : "");
        _fileCountLabel.ForeColor = count > 0 ? Color.FromArgb(0, 161, 112) : Color.FromArgb(140, 145, 165);
    }

    // ═══════════════════════════════════════════════════════
    //  SAVE
    // ═══════════════════════════════════════════════════════
    private void SaveAndClose()
    {
        if (_isEditMode)
        {
            var videoPath = (_videoPath?.Text ?? "").Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(videoPath))
            { ShowWarn("Vui lòng nhập đường dẫn video."); return; }
            if (!File.Exists(videoPath))
            { ShowWarn("File video không tồn tại."); return; }
            if (_existingPaths.Contains(videoPath) && !string.Equals(_original?.VideoPath, videoPath, StringComparison.OrdinalIgnoreCase))
            { ShowWarn("Đường dẫn video này đã tồn tại."); return; }

            int? folderId = null;
            if (_folderPicker?.SelectedItem is FolderItem fi && fi.Id > 0) folderId = fi.Id;
            
            Result = new JobItem
            {
                Id = _original?.Id ?? 0,
                FolderId = folderId,
                VideoPath = videoPath,
                Title = (_title?.Text ?? "").Trim(),
                ShopeeAffLink = (_affiliateLink?.Text ?? "").Trim(),
                Status = _original?.Status ?? "Chờ",
                ShopeeStatus = _status?.SelectedItem?.ToString() ?? "Chưa up Shopee",
                FbStatus = _fbStatus?.SelectedItem?.ToString() ?? "Chưa up Facebook",
                Log = _original?.Log ?? string.Empty
            };
        }
        else
        {
            if (_bulkGrid == null || _bulkGrid.Rows.Count == 0)
            { ShowWarn("Vui lòng chọn ít nhất 1 file video."); return; }

            foreach (DataGridViewRow row in _bulkGrid.Rows)
            {
                var file = row.Cells["colFile"].Value?.ToString() ?? "";
                var title = row.Cells["colTitle"].Value?.ToString() ?? "";
                var link = row.Cells["colLink"].Value?.ToString() ?? "";

                int? bulkFolderId = null;
                if (_folderPicker?.SelectedItem is FolderItem bfi && bfi.Id > 0) bulkFolderId = bfi.Id;
                
                BulkResults.Add(new JobItem
                {
                    FolderId = bulkFolderId,
                    VideoPath = file,
                    Title = title,
                    ShopeeAffLink = link,
                    Status = "Chờ",
                    ShopeeStatus = "Chưa up Shopee",
                    FbStatus = "Chưa up Facebook"
                });
            }

            if (BulkResults.Count == 1)
                Result = BulkResults[0];
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    // ═══════════════════════════════════════════════════════
    //  UI HELPERS
    // ═══════════════════════════════════════════════════════
    private static Label CreateLabel(string text, int x, int y) => new()
    {
        Text = text, AutoSize = true, Location = new Point(x, y),
        Font = new Font("Segoe UI Semibold", 9F),
        ForeColor = Color.FromArgb(83, 111, 140)
    };

    private static Guna.UI2.WinForms.Guna2TextBox CreateTextBox(string value) => new()
    {
        Text = value, FillColor = Color.White,
        BorderRadius = 7, BorderThickness = 1,
        BorderColor = Color.FromArgb(190, 198, 211),
        FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
        Padding = new Padding(10, 0, 10, 0)
    };

    private static Guna.UI2.WinForms.Guna2Button CreateSmallButton(string text, int width) => new()
    {
        Text = text, Width = width, Height = 32,
        FillColor = Color.FromArgb(228, 240, 249),
        ForeColor = Color.FromArgb(70, 70, 85),
        BorderRadius = 7, BorderThickness = 0,
        Font = new Font("Segoe UI Semibold", 8.5F),
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 8, 0)
    };

    private FlowLayoutPanel CreateHashtagPanel(int y)
    {
        var panel = new FlowLayoutPanel
        {
            Location = new Point(110, y),
            Size = new Size(460, 60),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        foreach (var tag in new[] { "#shopee", "#shopeevideo", "#shopeecreator", "#review", "#shopeevn", "#shopeelive", "#shopeehaul", "#shopeedeal", "#shopeeaffiliate", "#shopeefinds", "#muashopee", "#sambochinhhang" })
        {
            var btn = new Guna.UI2.WinForms.Guna2Button
            {
                Text = tag, Height = 26, AutoSize = true,
                FillColor = Color.FromArgb(240, 242, 245), ForeColor = Color.FromArgb(70, 70, 85),
                BorderRadius = 13, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8.5F),
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.Click += (_, _) =>
            {
                if (_title == null) return;
                _title.Text = (_title.Text.TrimEnd() + " " + tag).TrimStart();
                _title.SelectionStart = _title.Text.Length;
                _title.Focus();
            };
            panel.Controls.Add(btn);
        }
        return panel;
    }

    private FlowLayoutPanel CreateButtonPanel(string saveText, int saveWidth)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 58,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            Padding = new Padding(18, 10, 18, 10),
            BackColor = Color.FromArgb(250, 251, 253)
        };
        var save = new Guna.UI2.WinForms.Guna2Button
        {
            Text = saveText, Width = saveWidth, Height = 36,
            FillColor = Color.FromArgb(0, 161, 112), ForeColor = Color.White,
            BorderRadius = 7, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9F),
            Margin = new Padding(4, 0, 0, 0)
        };
        var cancel = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Hủy", Width = 82, Height = 36,
            FillColor = Color.FromArgb(235, 237, 242),
            ForeColor = Color.FromArgb(70, 70, 85),
            BorderRadius = 7, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9F),
            Margin = new Padding(4, 0, 0, 0)
        };
        save.Click += (_, _) => SaveAndClose();
        cancel.DialogResult = DialogResult.Cancel;
        panel.Controls.Add(cancel);
        panel.Controls.Add(save);
        AcceptButton = save;
        CancelButton = cancel;
        return panel;
    }

    private void ShowWarn(string msg) => MessageBox.Show(this, msg, "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
