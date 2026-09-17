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
    private CheckBox? _chkUseFileName;
    private Label? _fileCountLabel;
    private string _searchKeyword = string.Empty;
    private bool _showBulkDuplicatesOnly = false;

    private Guna.UI2.WinForms.Guna2ComboBox? _folderPicker;
    private readonly IReadOnlyList<FolderItem> _folders;
    private readonly JobItem? _original;
    private readonly HashSet<string> _existingPaths;
    private readonly List<JobItem> _allProducts = [];
    private bool _isApplyingLink = false;
    private readonly bool _isEditMode;
    private readonly bool _isBulkEditMode;

    public JobItem Result { get; private set; } = new();
    public List<JobItem> BulkResults { get; } = [];
    public bool IsBulkAdd => BulkResults.Count > 0;

    public ProductEditorDialog(JobItem? product = null, IEnumerable<string>? existingPaths = null, IEnumerable<FolderItem>? folders = null, int defaultFolderId = 0)
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
            BuildEditMode(product!, defaultFolderId);
        else
            BuildBulkAddMode(defaultFolderId: defaultFolderId);
    }

    public ProductEditorDialog(IEnumerable<JobItem> allProducts, IEnumerable<FolderItem>? folders = null, int defaultFolderId = 0)
        : this((JobItem?)null, allProducts, folders, defaultFolderId)
    {
    }

    public ProductEditorDialog(JobItem? product, IEnumerable<JobItem>? allProducts, IEnumerable<FolderItem>? folders, int defaultFolderId = 0)
    {
        _original = product;
        _isEditMode = product != null;
        _allProducts = allProducts?.ToList() ?? [];
        _existingPaths = new HashSet<string>(_allProducts.Select(j => j.VideoPath), StringComparer.OrdinalIgnoreCase);
        _folders = folders?.ToList() ?? [];

        Text = _isEditMode ? "Sửa sản phẩm" : "Thêm sản phẩm";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = _isEditMode ? false : true;
        ShowInTaskbar = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        if (_isEditMode)
            BuildEditMode(product!, defaultFolderId);
        else
            BuildBulkAddMode(allProducts: _allProducts, defaultFolderId: defaultFolderId);
    }

    public ProductEditorDialog(IReadOnlyList<JobItem> productsToEdit, IEnumerable<JobItem>? allProducts = null, IEnumerable<FolderItem>? folders = null, int defaultFolderId = -1)
    {
        _original = null;
        _isEditMode = false;
        _isBulkEditMode = true;
        _allProducts = allProducts?.ToList() ?? [];
        _existingPaths = new HashSet<string>(_allProducts.Select(j => j.VideoPath), StringComparer.OrdinalIgnoreCase);
        _folders = folders?.ToList() ?? [];

        Text = "Sửa nhiều sản phẩm";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        BuildBulkAddMode(productsToEdit, _allProducts, defaultFolderId);
    }

    // ═══════════════════════════════════════════════════════
    //  CHẾ ĐỘ SỬA (1 sản phẩm)
    // ═══════════════════════════════════════════════════════
    private void BuildEditMode(JobItem product, int defaultFolderId = 0)
    {
        ClientSize = new Size(660, 450);
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 0) };

        int inputX = 115;
        int inputWidth = 495;
        int currentY = 14;

        // Video path
        var lblVideo = CreateLabel("Video", 0, currentY + 6);
        _videoPath = CreateTextBox(product.VideoPath);
        _videoPath.ReadOnly = true;
        _videoPath.SetBounds(inputX, currentY, inputWidth - 48, 34);
        _videoPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var btnBrowse = CreateSmallButton("...", 44);
        btnBrowse.SetBounds(inputX + inputWidth - 44, currentY, 44, 34);
        btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowse.Click += (_, _) => BrowseSingleFile();

        currentY += 46;

        // Title
        var lblTitle = CreateLabel("Tiêu đề", 0, currentY + 6);
        _title = CreateTextBox(product.Title);
        _title.SetBounds(inputX, currentY, inputWidth, 34);
        _title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        currentY += 42;

        // Hashtags
        var hashPanel = CreateHashtagPanel(currentY);
        hashPanel.Width = inputWidth;

        currentY += 66;

        // Aff link
        var lblLink = CreateLabel("Link tiếp thị", 0, currentY + 6);
        _affiliateLink = CreateTextBox(product.ShopeeAffLink);
        _affiliateLink.SetBounds(inputX, currentY, inputWidth - 48, 34);
        _affiliateLink.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var btnMultiLink = CreateSmallButton("📋", 44);
        btnMultiLink.SetBounds(inputX + inputWidth - 44, currentY, 44, 34);
        btnMultiLink.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnMultiLink.Font = new Font("Segoe UI Emoji", 10F);
        var toolTip = new ToolTip();
        toolTip.SetToolTip(btnMultiLink, "Nhập danh sách nhiều link tiếp thị cho video này");
        btnMultiLink.Click += (_, _) => ShowSingleProductMultiLinkDialog();

        currentY += 46;

        // Status
        var lblStatus = CreateLabel("Trạng thái", 0, currentY + 6);
        _status = new Guna.UI2.WinForms.Guna2ComboBox
        {
            FillColor = Color.White, BorderRadius = 7, BorderThickness = 1,
            BorderColor = Color.FromArgb(190, 198, 211),
            FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _status.SetBounds(inputX, currentY, inputWidth, 34);
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _status.Items.AddRange([JobStatus.ShopeePending, JobStatus.ShopeeDone]);
        _status.SelectedItem = product.ShopeeStatus;

        currentY += 46;

        // Folder
        var lblFolder = CreateLabel("Chiến dịch", 0, currentY + 6);
        _folderPicker = new Guna.UI2.WinForms.Guna2ComboBox
        {
            FillColor = Color.White, BorderRadius = 7, BorderThickness = 1,
            BorderColor = Color.FromArgb(190, 198, 211),
            FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "Name", ValueMember = "Id"
        };
        _folderPicker.SetBounds(inputX, currentY, inputWidth, 34);
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

        currentY += 46;

        // Fb Status
        var lblFbStatus = CreateLabel("Trạng thái FB", 0, currentY + 6);
        _fbStatus = new Guna.UI2.WinForms.Guna2ComboBox
        {
            FillColor = Color.White, BorderRadius = 7, BorderThickness = 1,
            BorderColor = Color.FromArgb(190, 198, 211),
            FocusedState = { BorderColor = Color.FromArgb(96, 82, 218) },
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _fbStatus.SetBounds(inputX, currentY, inputWidth, 34);
        _fbStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _fbStatus.Items.AddRange([JobStatus.FacebookPending, JobStatus.FacebookDone]);
        _fbStatus.SelectedItem = product.FbStatus;

        mainPanel.Controls.AddRange([
            lblVideo, _videoPath, btnBrowse,
            lblTitle, _title,
            hashPanel,
            lblLink, _affiliateLink, btnMultiLink,
            lblStatus, _status,
            lblFolder, _folderPicker,
            lblFbStatus, _fbStatus
        ]);

        // Buttons
        var buttons = CreateButtonPanel("Lưu", 92);
        Controls.Add(buttons);
        Controls.Add(mainPanel);
    }

    // ═══════════════════════════════════════════════════════
    //  CHẾ ĐỘ THÊM HÀNG LOẠT
    // ═══════════════════════════════════════════════════════
    private void BuildBulkAddMode(IReadOnlyList<JobItem>? editItems = null, IEnumerable<JobItem>? allProducts = null, int defaultFolderId = 0)
    {
        ClientSize = new Size(1200, 720);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(900, 560);

        // ── Toolbar ──
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 96,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(20, 10, 20, 6),
            BackColor = Color.FromArgb(250, 251, 253)
        };

        var btnAdd = CreateSmallButton("📂 Chọn video", 125);
        btnAdd.FillColor = Color.FromArgb(96, 82, 218);
        btnAdd.ForeColor = Color.White;
        btnAdd.Click += (_, _) => BrowseMultipleFiles();

        var btnAddImages = CreateSmallButton("🖼️ Chọn ảnh", 125);
        btnAddImages.FillColor = Color.FromArgb(79, 70, 229);
        btnAddImages.ForeColor = Color.White;
        btnAddImages.Click += (_, _) => BrowseMultipleImages();

        var imgMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };
        var mnuPickPost = new ToolStripMenuItem("🖼️ Chọn ảnh cho 1 bài viết (nhiều ảnh = 1 bài)");
        mnuPickPost.Click += (_, _) => BrowseMultipleImages();
        var mnuPickFolder = new ToolStripMenuItem("📁 Chọn thư mục ảnh (mỗi thư mục con = 1 bài viết nhiều ảnh)");
        mnuPickFolder.Click += (_, _) => BrowseFolderForImagePosts();
        imgMenu.Items.AddRange([mnuPickPost, mnuPickFolder]);
        btnAddImages.ContextMenuStrip = imgMenu;

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
            Text = "0 video / ảnh",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(140, 145, 165),
            Margin = new Padding(16, 8, 0, 0)
        };

        btnAdd.Visible = !_isBulkEditMode;
        btnAddImages.Visible = !_isBulkEditMode;
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
        _folderPicker.Items.Add(new FolderItem { Id = -1, Name = "[Không đổi chiến dịch]" });
        _folderPicker.Items.Add(new FolderItem { Id = 0, Name = "[Chưa phân loại]" });
        foreach (var f in _folders) _folderPicker.Items.Add(f);
        var matchingItem = _folderPicker.Items.OfType<FolderItem>().FirstOrDefault(f => f.Id == defaultFolderId);
        if (matchingItem != null) _folderPicker.SelectedItem = matchingItem;
        else _folderPicker.SelectedIndex = 0;
        
        toolbar.Controls.AddRange([lblFolderBulk, _folderPicker]);

        var linkLabel = new Label
        {
            Text = "Chọn link aff:", AutoSize = true,
            ForeColor = Color.FromArgb(90, 95, 115),
            Margin = new Padding(12, 8, 4, 0)
        };
        _bulkLinkPicker = new ComboBox
        {
            Width = 260, Height = 28, DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            Margin = new Padding(0, 1, 0, 0)
        };
        _bulkLinkPicker.SelectedIndexChanged += (_, _) => ApplyPickedBulkLink();
        _bulkLinkPicker.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ApplyPickedBulkLink();
            }
        };

        var btnSearchLinks = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "🔍 Chọn link có sẵn",
            Width = 145, Height = 28,
            FillColor = Color.FromArgb(79, 70, 229),
            ForeColor = Color.White,
            BorderRadius = 4,
            Margin = new Padding(6, 1, 0, 0),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand
        };
        btnSearchLinks.Click += (_, _) => ShowLinkPickerForm();

        var btnBulkMultiLink = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "📋 Nhập nhiều link",
            Width = 135, Height = 28,
            FillColor = Color.FromArgb(96, 82, 218),
            ForeColor = Color.White,
            BorderRadius = 4,
            Margin = new Padding(6, 1, 0, 0),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand
        };
        btnBulkMultiLink.Click += (_, _) => ShowBulkMultiLinkDialog();

        toolbar.Controls.AddRange([linkLabel, _bulkLinkPicker, btnSearchLinks, btnBulkMultiLink]);

        var txtSearch = new Guna.UI2.WinForms.Guna2TextBox
        {
            PlaceholderText = "Tìm kiếm video...", Width = 220, Height = 28,
            BorderRadius = 4, Margin = new Padding(16, 2, 0, 0),
            Font = new Font("Segoe UI", 9F)
        };
        txtSearch.TextChanged += (_, _) => 
        {
            _searchKeyword = txtSearch.Text;
            ApplyBulkFilter();
        };

        var btnFilterDuplicates = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Lọc video trùng", Width = 120, Height = 28,
            FillColor = Color.FromArgb(249, 189, 82), ForeColor = Color.White,
            BorderRadius = 4, Margin = new Padding(6, 2, 0, 0),
            Font = new Font("Segoe UI", 9F)
        };
        btnFilterDuplicates.Click += (_, _) =>
        {
            _showBulkDuplicatesOnly = !_showBulkDuplicatesOnly;
            btnFilterDuplicates.Text = _showBulkDuplicatesOnly ? "Hủy lọc trùng" : "Lọc video trùng";
            btnFilterDuplicates.FillColor = _showBulkDuplicatesOnly ? Color.FromArgb(226, 82, 82) : Color.FromArgb(249, 189, 82);
            ApplyBulkFilter();
        };

        toolbar.Controls.AddRange([btnAdd, btnAddImages, btnRemove, _chkUseFileName, _fileCountLabel, txtSearch, btnFilterDuplicates]);

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
            Font = new Font("Segoe UI", 9F),
            ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable
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
            Name = "colFile", HeaderText = "File video / ảnh", ReadOnly = true,
            FillWeight = 35, MinimumWidth = 200
        });
        _bulkGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTitle", HeaderText = "Tiêu đề",
            FillWeight = 35, MinimumWidth = 180
        });
        _bulkGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLink", HeaderText = "Link tiếp thị",
            FillWeight = 30, MinimumWidth = 180
        });

        _bulkGrid.EditingControlShowing += (s, e) =>
        {
            if (e.Control is TextBox tb)
            {
                var editMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };
                var itemCut = new ToolStripMenuItem("✂ Cắt (Ctrl+X)", null, (_, _) => tb.Cut());
                var itemCopy = new ToolStripMenuItem("📋 Sao chép (Ctrl+C)", null, (_, _) =>
                {
                    try
                    {
                        if (tb.SelectionLength > 0)
                            tb.Copy();
                        else if (!string.IsNullOrEmpty(tb.Text))
                            Clipboard.SetText(tb.Text);
                    }
                    catch { }
                });
                var itemPaste = new ToolStripMenuItem("📑 Dán (Ctrl+V)", null, (_, _) => tb.Paste());
                var itemSelectAll = new ToolStripMenuItem("Chọn tất cả (Ctrl+A)", null, (_, _) => tb.SelectAll());

                editMenu.Opening += (_, _) =>
                {
                    itemCut.Enabled = tb.SelectionLength > 0 && !tb.ReadOnly;
                    itemCopy.Enabled = tb.SelectionLength > 0 || !string.IsNullOrEmpty(tb.Text);
                    itemPaste.Enabled = Clipboard.ContainsText() && !tb.ReadOnly;
                };

                editMenu.Items.AddRange(new ToolStripItem[] { itemCut, itemCopy, itemPaste, new ToolStripSeparator(), itemSelectAll });
                tb.ContextMenuStrip = editMenu;

                tb.KeyDown -= OnEditingControlKeyDown;
                tb.KeyDown += OnEditingControlKeyDown;
            }
        };

        _bulkGrid.KeyDown += (s, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyBulkGridContent();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteContentToBulkGrid();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                DuplicateSelectedRows();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        _bulkGrid.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_bulkGrid.Columns[e.ColumnIndex].Name == "colFile")
            {
                var filePath = _bulkGrid.Rows[e.RowIndex].Cells["colFile"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    var targetFile = filePath.Split(';').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrWhiteSpace(targetFile) && File.Exists(targetFile))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = targetFile,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        };

        _bulkGrid.CellMouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                if (!_bulkGrid.Rows[e.RowIndex].Selected)
                {
                    _bulkGrid.ClearSelection();
                    _bulkGrid.Rows[e.RowIndex].Selected = true;
                }
                if (_bulkGrid.SelectedRows.Count <= 1 && e.ColumnIndex >= 0)
                {
                    try
                    {
                        _bulkGrid.CurrentCell = _bulkGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    }
                    catch { }
                }
            }
        };

        var contextMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };

        var menuCopyCell = new ToolStripMenuItem("📋 Sao chép ô đang chọn (Ctrl+C)");
        menuCopyCell.Click += (_, _) => CopyBulkGridContent();
        contextMenu.Items.Add(menuCopyCell);

        var menuCopyLink = new ToolStripMenuItem("🔗 Sao chép link tiếp thị");
        menuCopyLink.Click += (_, _) => CopyBulkColumn("colLink");
        contextMenu.Items.Add(menuCopyLink);

        var menuCopyTitle = new ToolStripMenuItem("📝 Sao chép tiêu đề");
        menuCopyTitle.Click += (_, _) => CopyBulkColumn("colTitle");
        contextMenu.Items.Add(menuCopyTitle);

        var menuCopyFile = new ToolStripMenuItem("📁 Sao chép đường dẫn video");
        menuCopyFile.Click += (_, _) => CopyBulkColumn("colFile");
        contextMenu.Items.Add(menuCopyFile);

        contextMenu.Items.Add(new ToolStripSeparator());

        var menuPasteLinks = new ToolStripMenuItem("📑 Dán danh sách link từ Clipboard (Ctrl+V)");
        menuPasteLinks.Click += (_, _) => PasteContentToBulkGrid();
        contextMenu.Items.Add(menuPasteLinks);

        var menuMultiLink = new ToolStripMenuItem("📋 Nhập danh sách nhiều link tiếp thị...");
        menuMultiLink.Click += (_, _) => ShowBulkMultiLinkDialog();
        contextMenu.Items.Add(menuMultiLink);

        var menuAssignLink = new ToolStripMenuItem("🔍 Chọn / Gán link tiếp thị...");
        menuAssignLink.Click += (_, _) => ShowLinkPickerForm();
        contextMenu.Items.Add(menuAssignLink);

        var menuAutoMatchLink = new ToolStripMenuItem("⚡ Tự động tìm & gán link theo tên video");
        menuAutoMatchLink.Click += (_, _) =>
        {
            var rows = _bulkGrid.SelectedRows.Count > 0
                ? _bulkGrid.SelectedRows.Cast<DataGridViewRow>().ToList()
                : _bulkGrid.Rows.Cast<DataGridViewRow>().Where(r => r.Visible).ToList();
            AutoMatchLinksForRows(rows);
        };
        contextMenu.Items.Add(menuAutoMatchLink);

        contextMenu.Items.Add(new ToolStripSeparator());

        var menuDuplicate = new ToolStripMenuItem("📄 Nhân bản các video đã chọn (Ctrl+D)");
        menuDuplicate.Click += (_, _) => DuplicateSelectedRows();
        contextMenu.Items.Add(menuDuplicate);

        var menuRemove = new ToolStripMenuItem("❌ Xóa các video đã chọn");
        menuRemove.Click += (_, _) => RemoveSelectedRows();
        contextMenu.Items.Add(menuRemove);

        contextMenu.Opening += (_, _) =>
        {
            var selCount = _bulkGrid.SelectedRows.Count;
            if (selCount <= 1)
            {
                var curCell = _bulkGrid.CurrentCell;
                var colHeader = curCell?.OwningColumn?.HeaderText ?? "ô";
                menuCopyCell.Text = $"📋 Sao chép {colHeader} (Ctrl+C)";
                menuCopyCell.Visible = true;
                menuCopyLink.Text = "🔗 Sao chép link tiếp thị";
                menuCopyTitle.Text = "📝 Sao chép tiêu đề";
                menuCopyFile.Text = "📁 Sao chép đường dẫn video";
                menuAssignLink.Text = "🔍 Chọn / Gán link tiếp thị cho video này...";
                menuAutoMatchLink.Text = "⚡ Tự động khớp link theo tên cho video này";
                menuDuplicate.Text = "📄 Nhân bản video này (Ctrl+D)";
                menuRemove.Text = "❌ Xóa video này";
            }
            else
            {
                menuCopyCell.Visible = false;
                menuCopyLink.Text = $"🔗 Sao chép {selCount} link tiếp thị";
                menuCopyTitle.Text = $"📝 Sao chép {selCount} tiêu đề";
                menuCopyFile.Text = $"📁 Sao chép {selCount} đường dẫn video";
                menuAssignLink.Text = $"🔍 Chọn / Gán link tiếp thị cho {selCount} video đã chọn...";
                menuAutoMatchLink.Text = $"⚡ Tự động khớp link theo tên cho {selCount} video đã chọn";
                menuDuplicate.Text = $"📄 Nhân bản {selCount} video đã chọn (Ctrl+D)";
                menuRemove.Text = $"❌ Xóa {selCount} video đã chọn";
            }
        };

        _bulkGrid.ContextMenuStrip = contextMenu;

        if (editItems != null)
        {
            foreach (var item in editItems)
                _bulkGrid.Rows.Add(item.VideoPath, item.Title, item.ShopeeAffLink);
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
            Title = "Chọn file video hoặc ảnh",
            Filter = "Video & Ảnh|*.mp4;*.mov;*.mkv;*.avi;*.webm;*.m4v;*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.heic|Video|*.mp4;*.mov;*.mkv;*.avi;*.webm;*.m4v|Ảnh|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.heic|Tất cả file|*.*",
            CheckFileExists = true, Multiselect = true,
            FileName = _videoPath?.Text ?? ""
        };
        var firstPath = _videoPath?.Text?.Split(';').FirstOrDefault()?.Trim();
        var dir = !string.IsNullOrWhiteSpace(firstPath) ? Path.GetDirectoryName(firstPath) : null;
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            dialog.InitialDirectory = dir;
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (dialog.FileNames.Length > 1)
                _videoPath!.Text = string.Join("; ", dialog.FileNames);
            else
                _videoPath!.Text = dialog.FileName;
        }
    }

    private void BrowseMultipleFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn file video",
            Filter = "Video|*.mp4;*.mov;*.mkv;*.avi;*.webm;*.m4v|Tất cả media (Video & Ảnh)|*.mp4;*.mov;*.mkv;*.avi;*.webm;*.m4v;*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.heic|Tất cả file|*.*",
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
            _bulkGrid.Rows.Add(file, title, "");
            added++;
        }
        var skipped = dialog.FileNames.Length - added;
        UpdateFileCount(skipped);
    }

    private void BrowseMultipleImages()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn các file ảnh cho 1 bài viết (nhiều ảnh = 1 bài)",
            Filter = "Ảnh (*.jpg, *.png, *.webp, *.jpeg, *.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.heic|Tất cả file|*.*",
            CheckFileExists = true, Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var selectedFiles = dialog.FileNames
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedFiles.Length == 0) return;

        // Logic ảnh: Nhiều ảnh gộp thành 1 bài viết duy nhất (phân cách bằng dấu ;)
        var combinedPath = string.Join("; ", selectedFiles);

        var existingInGrid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in _bulkGrid!.Rows)
            existingInGrid.Add(row.Cells["colFile"].Value?.ToString() ?? "");

        if (_existingPaths.Contains(combinedPath) || existingInGrid.Contains(combinedPath))
        {
            MessageBox.Show(this, "Bài viết với danh sách ảnh này đã có trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var firstFile = selectedFiles[0];
        var title = _chkUseFileName?.Checked == true ? Path.GetFileNameWithoutExtension(firstFile) : "";
        _bulkGrid.Rows.Add(combinedPath, title, "");
        UpdateFileCount(0);
    }

    private void BrowseFolderForImagePosts()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Chọn thư mục chứa ảnh (mỗi thư mục con hoặc thư mục này sẽ là 1 bài viết gồm nhiều ảnh)",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;

        var supportedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".heic"
        };

        var rootDir = dialog.SelectedPath;
        var subDirs = Directory.GetDirectories(rootDir);

        var existingInGrid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in _bulkGrid!.Rows)
            existingInGrid.Add(row.Cells["colFile"].Value?.ToString() ?? "");

        int added = 0;
        int skipped = 0;

        if (subDirs.Length > 0)
        {
            foreach (var sub in subDirs)
            {
                var imgs = Directory.GetFiles(sub)
                    .Where(f => supportedExts.Contains(Path.GetExtension(f)))
                    .OrderBy(f => f)
                    .ToArray();

                if (imgs.Length == 0) continue;

                var combined = string.Join("; ", imgs);
                if (_existingPaths.Contains(combined) || existingInGrid.Contains(combined))
                {
                    skipped++;
                    continue;
                }

                var title = _chkUseFileName?.Checked == true ? Path.GetFileName(sub) : "";
                _bulkGrid.Rows.Add(combined, title, "");
                existingInGrid.Add(combined);
                added++;
            }
        }
        else
        {
            var imgs = Directory.GetFiles(rootDir)
                .Where(f => supportedExts.Contains(Path.GetExtension(f)))
                .OrderBy(f => f)
                .ToArray();

            if (imgs.Length > 0)
            {
                var combined = string.Join("; ", imgs);
                if (!_existingPaths.Contains(combined) && !existingInGrid.Contains(combined))
                {
                    var title = _chkUseFileName?.Checked == true ? Path.GetFileName(rootDir) : "";
                    _bulkGrid.Rows.Add(combined, title, "");
                    added++;
                }
                else
                {
                    skipped++;
                }
            }
        }

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
            {
                var firstPath = file.Split(';').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(firstPath))
                {
                    try
                    {
                        row.Cells["colTitle"].Value = Path.GetFileNameWithoutExtension(firstPath);
                    }
                    catch { }
                }
            }
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

    private void RefreshBulkLinkPicker(IEnumerable<JobItem>? extraProducts = null)
    {
        if (_bulkLinkPicker == null) return;

        if (extraProducts != null)
        {
            foreach (var item in extraProducts)
            {
                if (!string.IsNullOrWhiteSpace(item.ShopeeAffLink) && !_allProducts.Any(x => string.Equals(x.ShopeeAffLink, item.ShopeeAffLink, StringComparison.OrdinalIgnoreCase)))
                {
                    _allProducts.Add(item);
                }
            }
        }

        var allLinks = GetAllAvailableAffLinks();
        _bulkLinkPicker.BeginUpdate();
        _bulkLinkPicker.Items.Clear();
        foreach (var item in allLinks)
        {
            _bulkLinkPicker.Items.Add(item.DisplayText);
        }
        _bulkLinkPicker.EndUpdate();
    }

    private void ApplyPickedBulkLink()
    {
        if (_isApplyingLink || _bulkGrid == null || _bulkLinkPicker == null) return;
        var display = _bulkLinkPicker.SelectedItem?.ToString() ?? _bulkLinkPicker.Text.Trim();
        if (string.IsNullOrWhiteSpace(display)) return;

        var link = ExtractLink(display);
        if (string.IsNullOrWhiteSpace(link) || !link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var matched = _bulkLinkPicker.Items.Cast<object>()
                .Select(o => o.ToString() ?? "")
                .FirstOrDefault(s => s.IndexOf(display, StringComparison.OrdinalIgnoreCase) >= 0);
            if (matched != null)
                link = ExtractLink(matched);
        }

        if (string.IsNullOrWhiteSpace(link) || !link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            _isApplyingLink = true;
            var rows = _bulkGrid.SelectedRows.Count > 0
                ? _bulkGrid.SelectedRows.Cast<DataGridViewRow>()
                : _bulkGrid.CurrentRow != null ? [ _bulkGrid.CurrentRow ] : [];
            foreach (var row in rows)
                row.Cells["colLink"].Value = link;
        }
        finally
        {
            _isApplyingLink = false;
        }
        _bulkGrid.Focus();
    }

    private void UpdateFileCount(int skipped)
    {
        var count = _bulkGrid!.Rows.Count;
        _fileCountLabel!.Text = $"{count} bài viết" + (skipped > 0 ? $" ({skipped} trùng, bỏ qua)" : "");
        _fileCountLabel.ForeColor = count > 0 ? Color.FromArgb(0, 161, 112) : Color.FromArgb(140, 145, 165);
    }

    private void ApplyBulkFilter()
    {
        if (_bulkGrid == null) return;

        HashSet<string>? duplicatePaths = null;
        if (_showBulkDuplicatesOnly)
        {
            var paths = new List<string>();
            foreach (DataGridViewRow row in _bulkGrid.Rows)
            {
                var p = Convert.ToString(row.Cells["colFile"].Value);
                if (!string.IsNullOrWhiteSpace(p)) paths.Add(p);
            }
            duplicatePaths = paths.GroupBy(p => p, StringComparer.OrdinalIgnoreCase)
                                  .Where(g => g.Count() > 1)
                                  .Select(g => g.Key)
                                  .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        _bulkGrid.CurrentCell = null; 

        foreach (DataGridViewRow row in _bulkGrid.Rows)
        {
            var title = Convert.ToString(row.Cells["colTitle"].Value) ?? string.Empty;
            var path = Convert.ToString(row.Cells["colFile"].Value) ?? string.Empty;

            bool matchesSearch = string.IsNullOrWhiteSpace(_searchKeyword) ||
                                 title.Contains(_searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                                 path.Contains(_searchKeyword, StringComparison.OrdinalIgnoreCase);

            bool matchesDuplicate = !_showBulkDuplicatesOnly || (duplicatePaths != null && duplicatePaths.Contains(path));

            row.Visible = matchesSearch && matchesDuplicate;
        }
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
            { ShowWarn("Vui lòng nhập đường dẫn video hoặc ảnh."); return; }

            var paths = videoPath.Split(new[] { ';', '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (paths.Length == 0)
            { ShowWarn("Vui lòng nhập đường dẫn video hoặc ảnh."); return; }
            if (!paths.All(File.Exists))
            { ShowWarn("Một hoặc nhiều file video/ảnh không tồn tại."); return; }

            if (_existingPaths.Contains(videoPath) && !string.Equals(_original?.VideoPath, videoPath, StringComparison.OrdinalIgnoreCase))
            { ShowWarn("Đường dẫn này đã tồn tại trong danh sách."); return; }

            int? folderId = _original?.FolderId;
            if (_folderPicker?.SelectedItem is FolderItem fi) folderId = fi.Id > 0 ? fi.Id : (fi.Id == 0 ? null : _original?.FolderId);
            
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
            { ShowWarn("Vui lòng chọn ít nhất 1 video hoặc ảnh."); return; }

            foreach (DataGridViewRow row in _bulkGrid.Rows)
            {
                var file = row.Cells["colFile"].Value?.ToString() ?? "";
                var title = row.Cells["colTitle"].Value?.ToString() ?? "";
                var link = row.Cells["colLink"].Value?.ToString() ?? "";

                int? bulkFolderId = null;
                if (_folderPicker?.SelectedItem is FolderItem bfi) {
                    if (bfi.Id == -1) bulkFolderId = -1; // special flag for keep original
                    else if (bfi.Id > 0) bulkFolderId = bfi.Id;
                }
                
                BulkResults.Add(new JobItem
                {
                    FolderId = bulkFolderId,
                    VideoPath = file,
                    Title = title,
                    ShopeeAffLink = link,
                    Status = JobStatus.Waiting,
                    ShopeeStatus = JobStatus.ShopeePending,
                    FbStatus = JobStatus.FacebookPending
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

    private sealed class AffLinkRecord
    {
        public string Title { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public string DisplayText => string.IsNullOrWhiteSpace(Title) ? Link : $"{Title}  —  {Link}";
    }

    private List<AffLinkRecord> GetAllAvailableAffLinks()
    {
        var result = new List<AffLinkRecord>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string? link, string? title, int? folderId = null)
        {
            link = link?.Trim();
            if (string.IsNullOrWhiteSpace(link) || !link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
            if (!seen.Add(link)) return;

            var folderName = string.Empty;
            if (folderId.HasValue && folderId.Value > 0)
            {
                var folder = _folders.FirstOrDefault(f => f.Id == folderId.Value);
                if (folder != null) folderName = folder.Name;
            }

            result.Add(new AffLinkRecord
            {
                Link = link,
                Title = title?.Trim() ?? string.Empty,
                FolderName = folderName
            });
        }

        if (_bulkGrid != null)
        {
            foreach (DataGridViewRow row in _bulkGrid.Rows)
                TryAdd(Convert.ToString(row.Cells["colLink"].Value), Convert.ToString(row.Cells["colTitle"].Value));
        }

        foreach (var item in _allProducts)
        {
            TryAdd(item.ShopeeAffLink, item.Title, item.FolderId);
        }

        return result;
    }

    private void ShowLinkPickerForm()
    {
        if (_bulkGrid == null || _bulkGrid.Rows.Count == 0)
        {
            MessageBox.Show(this, "Chưa có video nào trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var targetRows = _bulkGrid.SelectedRows.Count > 0
            ? _bulkGrid.SelectedRows.Cast<DataGridViewRow>().OrderBy(r => r.Index).ToList()
            : _bulkGrid.CurrentRow != null
                ? new List<DataGridViewRow> { _bulkGrid.CurrentRow }
                : _bulkGrid.Rows.Cast<DataGridViewRow>().Where(r => r.Visible).ToList();

        if (targetRows.Count == 0)
        {
            MessageBox.Show(this, "Vui lòng chọn ít nhất một video.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var linkRecords = GetAllAvailableAffLinks();

        using var dlg = new Form
        {
            Text = "🔍 Chọn & Gán Link Tiếp Thị Có Sẵn",
            Width = 840,
            Height = 560,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.Sizable,
            MinimizeBox = false,
            MaximizeBox = true,
            BackColor = Color.FromArgb(246, 250, 254),
            Font = new Font("Segoe UI", 9F)
        };

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 8)
        };

        var lblTitle = new Label
        {
            Text = targetRows.Count == 1
                ? $"Gán link tiếp thị cho dòng {targetRows[0].Index + 1}: {targetRows[0].Cells["colTitle"].Value}"
                : $"Gán link tiếp thị cho {targetRows.Count} video đang chọn",
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = Color.FromArgb(31, 31, 44),
            Dock = DockStyle.Top,
            Height = 22
        };

        var lblSub = new Label
        {
            Text = "Tìm kiếm từ những link cũ trong danh sách hoặc nhập/dán link mới bên dưới:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(120, 125, 145),
            Dock = DockStyle.Top,
            Height = 18
        };

        var pnlSearchRow = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            Padding = new Padding(0, 2, 0, 0)
        };

        var txtSearch = new Guna.UI2.WinForms.Guna2TextBox
        {
            PlaceholderText = "🔍 Nhập từ khóa sản phẩm hoặc link cần tìm (VD: tai nghe, k8, https://...)",
            Dock = DockStyle.Fill,
            BorderRadius = 5,
            Font = new Font("Segoe UI", 9.5F),
            BorderColor = Color.FromArgb(200, 205, 220)
        };

        var btnPasteClip = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "📋 Dán Clipboard",
            Dock = DockStyle.Right,
            Width = 135,
            Margin = new Padding(8, 0, 0, 0),
            FillColor = Color.FromArgb(235, 238, 245),
            ForeColor = Color.FromArgb(45, 50, 65),
            BorderRadius = 5,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };

        pnlSearchRow.Controls.Add(txtSearch);
        pnlSearchRow.Controls.Add(btnPasteClip);
        pnlHeader.Controls.AddRange([pnlSearchRow, lblSub, lblTitle]);

        // Grid
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 10, 20, 8),
            BackColor = Color.FromArgb(246, 250, 254)
        };

        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            EnableHeadersVisualStyles = false,
            Font = new Font("Segoe UI", 9F)
        };

        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 243, 249);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(70, 75, 95);
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        dgv.ColumnHeadersHeight = 32;
        dgv.RowTemplate.Height = 28;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 253);

        var colTitle = new DataGridViewTextBoxColumn
        {
            Name = "colTitle",
            HeaderText = "Tên sản phẩm / Tiêu đề",
            Width = 380,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };
        var colLink = new DataGridViewTextBoxColumn
        {
            Name = "colLink",
            HeaderText = "Link tiếp thị Shopee",
            Width = 270
        };
        var colFolder = new DataGridViewTextBoxColumn
        {
            Name = "colFolder",
            HeaderText = "Chiến dịch",
            Width = 120
        };

        dgv.Columns.AddRange([colTitle, colLink, colFolder]);
        pnlBody.Controls.Add(dgv);

        // Footer
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.White,
            Padding = new Padding(20, 9, 20, 9)
        };

        var btnAutoMatch = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "⚡ Tự động khớp theo tên video",
            Dock = DockStyle.Left,
            Width = 235,
            Height = 34,
            FillColor = Color.FromArgb(0, 161, 112),
            ForeColor = Color.White,
            BorderRadius = 5,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };

        var btnCancel = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Đóng",
            Dock = DockStyle.Right,
            Width = 85,
            Height = 34,
            FillColor = Color.FromArgb(228, 240, 249),
            ForeColor = Color.FromArgb(70, 70, 85),
            BorderRadius = 5,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };
        btnCancel.Click += (_, _) => dlg.Close();

        var btnApply = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Áp dụng link này",
            Dock = DockStyle.Right,
            Width = 155,
            Height = 34,
            Margin = new Padding(0, 0, 8, 0),
            FillColor = Color.FromArgb(79, 70, 229),
            ForeColor = Color.White,
            BorderRadius = 5,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };

        pnlFooter.Controls.AddRange([btnAutoMatch, btnApply, btnCancel]);

        dlg.Controls.AddRange([pnlBody, pnlHeader, pnlFooter]);

        void Populate(string keyword)
        {
            dgv.Rows.Clear();
            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? linkRecords
                : linkRecords.Where(r =>
                    r.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.Link.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.FolderName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            foreach (var r in filtered)
                dgv.Rows.Add(r.Title, r.Link, r.FolderName);

            if (dgv.Rows.Count > 0)
                dgv.Rows[0].Selected = true;
        }

        txtSearch.TextChanged += (_, _) => Populate(txtSearch.Text);

        btnPasteClip.Click += (_, _) =>
        {
            var clip = Clipboard.GetText()?.Trim();
            if (!string.IsNullOrWhiteSpace(clip))
            {
                txtSearch.Text = clip;
                txtSearch.SelectionStart = txtSearch.Text.Length;
            }
        };

        void DoApply()
        {
            string chosenLink = string.Empty;

            if (dgv.SelectedRows.Count > 0)
            {
                chosenLink = Convert.ToString(dgv.SelectedRows[0].Cells["colLink"].Value) ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(chosenLink))
            {
                chosenLink = ExtractLink(txtSearch.Text);
            }

            if (string.IsNullOrWhiteSpace(chosenLink) || !chosenLink.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(dlg, "Vui lòng chọn 1 dòng sản phẩm hoặc nhập link hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var row in targetRows)
                row.Cells["colLink"].Value = chosenLink;

            RefreshBulkLinkPicker();
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        }

        btnApply.Click += (_, _) => DoApply();
        dgv.CellDoubleClick += (_, _) => DoApply();
        dgv.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                DoApply();
            }
        };
        txtSearch.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                DoApply();
            }
        };

        btnAutoMatch.Click += (_, _) =>
        {
            AutoMatchLinksForRows(targetRows);
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        var initialKeyword = targetRows.Count == 1 ? Convert.ToString(targetRows[0].Cells["colTitle"].Value)?.Trim() ?? string.Empty : string.Empty;
        if (!string.IsNullOrWhiteSpace(initialKeyword))
        {
            var words = initialKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
            {
                var query = string.Join(" ", words.Take(Math.Min(words.Length, 3)));
                txtSearch.Text = query;
            }
            else
            {
                Populate(string.Empty);
            }
        }
        else
        {
            Populate(string.Empty);
        }

        dlg.ShowDialog(this);
    }

    private void AutoMatchLinksForRows(IReadOnlyList<DataGridViewRow> targetRows)
    {
        var allRecords = GetAllAvailableAffLinks();
        if (allRecords.Count == 0)
        {
            MessageBox.Show(this, "Không có dữ liệu link tiếp thị nào trong hệ thống để đối soát khớp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int matchedCount = 0;
        foreach (var row in targetRows)
        {
            var title = Convert.ToString(row.Cells["colTitle"].Value)?.Trim() ?? string.Empty;
            var file = Convert.ToString(row.Cells["colFile"].Value)?.Trim() ?? string.Empty;
            var firstFile = file.Split(';').FirstOrDefault()?.Trim() ?? string.Empty;
            var fileNameWithoutExt = string.Empty;
            if (!string.IsNullOrWhiteSpace(firstFile))
            {
                try { fileNameWithoutExt = Path.GetFileNameWithoutExtension(firstFile); } catch { }
            }

            var bestLink = FindBestMatchingLink(title, fileNameWithoutExt, allRecords);
            if (!string.IsNullOrWhiteSpace(bestLink))
            {
                row.Cells["colLink"].Value = bestLink;
                matchedCount++;
            }
        }

        RefreshBulkLinkPicker();
        MessageBox.Show(this, $"Đã tự động tìm và khớp link cho {matchedCount}/{targetRows.Count} video!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string? FindBestMatchingLink(string title, string fileNameWithoutExt, List<AffLinkRecord> records)
    {
        if (records.Count == 0) return null;

        string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var s = text.ToLowerInvariant();
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\b(video|tap|tập|part|final|hoanthien|review|mp4|mov)\s*\d*\b", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"#[a-zA-Z0-9_]+", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^\w\s]", " ");
            return string.Join(" ", s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        var normTitle = Normalize(title);
        var normFile = Normalize(fileNameWithoutExt);

        AffLinkRecord? bestRecord = null;
        int highestScore = 0;

        foreach (var rec in records)
        {
            var normRec = Normalize(rec.Title);
            if (string.IsNullOrWhiteSpace(normRec)) continue;

            if ((!string.IsNullOrWhiteSpace(normTitle) && (normTitle.Contains(normRec) || normRec.Contains(normTitle))) ||
                (!string.IsNullOrWhiteSpace(normFile) && (normFile.Contains(normRec) || normRec.Contains(normFile))))
            {
                int score = 100 + Math.Min(normRec.Length, 50);
                if (score > highestScore)
                {
                    highestScore = score;
                    bestRecord = rec;
                }
                continue;
            }

            var recWords = normRec.Split(' ').Where(w => w.Length >= 2).ToHashSet();
            if (recWords.Count == 0) continue;

            var targetWords = (normTitle + " " + normFile).Split(' ').Where(w => w.Length >= 2).ToHashSet();
            int matchCount = recWords.Count(w => targetWords.Contains(w));

            if (matchCount >= 2 && matchCount > highestScore)
            {
                highestScore = matchCount;
                bestRecord = rec;
            }
        }

        return bestRecord?.Link;
    }

    private string? PromptForLink()
    {
        ShowLinkPickerForm();
        return null;
    }

    private static string ExtractLink(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var match = System.Text.RegularExpressions.Regex.Match(input, @"https?://[^\s""',;]+");
        return match.Success ? match.Value.Trim() : string.Empty;
    }

    private FlowLayoutPanel CreateHashtagPanel(int y)
    {
        var panel = new FlowLayoutPanel
        {
            Location = new Point(115, y),
            Size = new Size(495, 56),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        foreach (var tag in new[] { "#shopee", "#shopeevideo", "#shopeecreator", "#review", "#shopeevn", "#shopeelive", "#shopeehaul", "#shopeedeal" })
        {
            var btn = new Guna.UI2.WinForms.Guna2Button
            {
                Text = tag, Height = 25, AutoSize = true,
                FillColor = Color.FromArgb(240, 242, 245), ForeColor = Color.FromArgb(70, 70, 85),
                BorderRadius = 12, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8.5F),
                Margin = new Padding(0, 0, 6, 4)
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

    private void ShowSingleProductMultiLinkDialog()
    {
        if (_affiliateLink == null) return;

        using var dlg = new Form
        {
            Text = "Nhập danh sách link tiếp thị",
            Width = 600,
            Height = 480,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(246, 250, 254),
            Font = new Font("Segoe UI", 9F)
        };

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 5)
        };
        var lblTitle = new Label
        {
            Text = "Danh sách link Shopee Affiliate cho video này",
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = Color.FromArgb(31, 31, 44),
            Dock = DockStyle.Top,
            Height = 22
        };
        var lblSub = new Label
        {
            Text = "Mỗi dòng là một link. Hệ thống sẽ tự động thêm tất cả link này khi chạy quy trình Shopee.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(140, 145, 165),
            Dock = DockStyle.Top,
            Height = 18
        };
        pnlHeader.Controls.AddRange(new Control[] { lblSub, lblTitle });

        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 12, 20, 10)
        };

        var txtInput = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5F),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var currentLinks = ExtractAllLinks(_affiliateLink.Text);
        if (currentLinks.Count > 0)
            txtInput.Text = string.Join(Environment.NewLine, currentLinks);
        else
            txtInput.Text = _affiliateLink.Text.Trim();

        var lblCount = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            Text = $"Đã nhận diện: {currentLinks.Count} link hợp lệ",
            ForeColor = Color.FromArgb(0, 161, 112),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Padding = new Padding(0, 6, 0, 0)
        };

        txtInput.TextChanged += (_, _) =>
        {
            var links = ExtractAllLinks(txtInput.Text);
            lblCount.Text = $"Đã nhận diện: {links.Count} link hợp lệ";
        };

        pnlBody.Controls.Add(txtInput);
        pnlBody.Controls.Add(lblCount);

        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(15, 8, 15, 8),
            BackColor = Color.FromArgb(240, 244, 248)
        };

        var btnApply = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Xác nhận",
            Width = 100,
            Height = 34,
            FillColor = Color.FromArgb(0, 161, 112),
            ForeColor = Color.White,
            BorderRadius = 6,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };

        var btnCancel = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Hủy",
            Width = 80,
            Height = 34,
            FillColor = Color.FromArgb(228, 232, 240),
            ForeColor = Color.FromArgb(70, 70, 85),
            BorderRadius = 6,
            Font = new Font("Segoe UI", 9F),
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand
        };

        btnApply.Click += (_, _) =>
        {
            var links = ExtractAllLinks(txtInput.Text);
            _affiliateLink.Text = links.Count > 0 ? string.Join(", ", links) : txtInput.Text.Trim();
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        pnlBottom.Controls.AddRange(new Control[] { btnCancel, btnApply });
        dlg.Controls.Add(pnlBody);
        dlg.Controls.Add(pnlBottom);
        dlg.Controls.Add(pnlHeader);

        dlg.ShowDialog(this);
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

    private void OnEditingControlKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                try
                {
                    if (tb.SelectionLength > 0)
                        tb.Copy();
                    else if (!string.IsNullOrEmpty(tb.Text))
                        Clipboard.SetText(tb.Text);
                }
                catch { }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                tb.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }

    private void CopyBulkGridContent()
    {
        if (_bulkGrid == null || _bulkGrid.Rows.Count == 0) return;

        if (_bulkGrid.IsCurrentCellInEditMode && _bulkGrid.EditingControl is TextBox tb)
        {
            try
            {
                if (tb.SelectionLength > 0)
                    Clipboard.SetText(tb.SelectedText);
                else if (!string.IsNullOrEmpty(tb.Text))
                    Clipboard.SetText(tb.Text);
            }
            catch { }
            return;
        }

        var colName = _bulkGrid.CurrentCell?.OwningColumn?.Name;
        if (string.IsNullOrEmpty(colName))
            colName = "colLink";

        CopyBulkColumn(colName);
    }

    private void CopyBulkColumn(string colName)
    {
        if (_bulkGrid == null || _bulkGrid.Rows.Count == 0) return;
        var selectedRows = _bulkGrid.SelectedRows.Cast<DataGridViewRow>().OrderBy(r => r.Index).ToList();
        if (selectedRows.Count == 0 && _bulkGrid.CurrentRow != null)
            selectedRows.Add(_bulkGrid.CurrentRow);
        if (selectedRows.Count == 0) return;

        var values = selectedRows
            .Select(r => r.Cells[colName].Value?.ToString() ?? "")
            .Where(s => !string.IsNullOrEmpty(s));
        var text = string.Join(Environment.NewLine, values);
        if (!string.IsNullOrEmpty(text))
        {
            try { Clipboard.SetText(text); } catch { }
        }
    }

    private void DuplicateSelectedRows()
    {
        if (_bulkGrid == null) return;
        var rows = _bulkGrid.SelectedRows.Cast<DataGridViewRow>().OrderBy(r => r.Index).ToList();
        if (rows.Count == 0 && _bulkGrid.CurrentRow != null)
            rows.Add(_bulkGrid.CurrentRow);
        if (rows.Count == 0) return;

        var newIndices = new List<int>();
        foreach (var r in rows)
        {
            var file = r.Cells["colFile"].Value?.ToString() ?? "";
            var title = r.Cells["colTitle"].Value?.ToString() ?? "";
            var link = r.Cells["colLink"].Value?.ToString() ?? "";
            var newIdx = _bulkGrid.Rows.Add(file, title, link);
            newIndices.Add(newIdx);
        }
        _bulkGrid.ClearSelection();
        foreach (var idx in newIndices)
        {
            if (idx >= 0 && idx < _bulkGrid.Rows.Count)
                _bulkGrid.Rows[idx].Selected = true;
        }
        UpdateFileCount(0);
    }

    private void PasteContentToBulkGrid()
    {
        if (_bulkGrid == null || _bulkGrid.Rows.Count == 0) return;
        string text;
        try { text = Clipboard.GetText(); } catch { return; }
        if (string.IsNullOrWhiteSpace(text)) return;

        if (_bulkGrid.IsCurrentCellInEditMode && _bulkGrid.EditingControl is TextBox tb)
        {
            try { tb.Paste(); } catch { }
            return;
        }

        if (_bulkGrid.CurrentCell?.OwningColumn?.Name == "colTitle" && !text.Contains("://"))
        {
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1)
            {
                var startRowIndex = _bulkGrid.CurrentCell?.RowIndex ?? 0;
                for (int i = 0; i < lines.Length && (startRowIndex + i) < _bulkGrid.Rows.Count; i++)
                {
                    _bulkGrid.Rows[startRowIndex + i].Cells["colTitle"].Value = lines[i].Trim();
                }
            }
            else if (lines.Length == 1)
            {
                var targetRows = _bulkGrid.SelectedRows.Count > 0
                    ? _bulkGrid.SelectedRows.Cast<DataGridViewRow>()
                    : _bulkGrid.CurrentRow != null ? [ _bulkGrid.CurrentRow ] : [];
                foreach (var row in targetRows)
                    row.Cells["colTitle"].Value = lines[0].Trim();
            }
            return;
        }

        var links = ExtractAllLinks(text);
        if (links.Count == 0) return;

        if (links.Count > 1)
        {
            var startRowIndex = _bulkGrid.CurrentCell?.RowIndex ?? 0;
            for (int i = 0; i < links.Count && (startRowIndex + i) < _bulkGrid.Rows.Count; i++)
            {
                var row = _bulkGrid.Rows[startRowIndex + i];
                row.Cells["colLink"].Value = links[i];
            }
        }
        else
        {
            var link = links[0];
            var targetRows = _bulkGrid.SelectedRows.Count > 0
                ? _bulkGrid.SelectedRows.Cast<DataGridViewRow>()
                : _bulkGrid.CurrentRow != null ? [ _bulkGrid.CurrentRow ] : [];
            foreach (var row in targetRows)
                row.Cells["colLink"].Value = link;
        }
        RefreshBulkLinkPicker();
    }

    private void PasteLinksFromClipboard() => PasteContentToBulkGrid();

    public static List<string> ExtractAllLinks(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        var matches = System.Text.RegularExpressions.Regex.Matches(input, @"https?://[^\s,;]+");
        var list = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            var url = m.Value.Trim();
            if (!string.IsNullOrWhiteSpace(url))
                list.Add(url);
        }
        if (list.Count == 0)
        {
            var lines = input.Split(new[] { "\r\n", "\r", "\n", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var l in lines)
            {
                var t = l.Trim();
                if (!string.IsNullOrWhiteSpace(t)) list.Add(t);
            }
        }
        return list;
    }

    private void ShowBulkMultiLinkDialog()
    {
        if (_bulkGrid == null || _bulkGrid.Rows.Count == 0)
        {
            MessageBox.Show(this, "Chưa có video nào trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var targetRows = _bulkGrid.SelectedRows.Count > 0
            ? _bulkGrid.SelectedRows.Cast<DataGridViewRow>().OrderBy(r => r.Index).ToList()
            : _bulkGrid.CurrentRow != null
                ? new List<DataGridViewRow> { _bulkGrid.CurrentRow }
                : _bulkGrid.Rows.Cast<DataGridViewRow>().Where(r => r.Visible).ToList();

        var isSingleRow = targetRows.Count == 1;

        using var dlg = new Form
        {
            Text = isSingleRow ? $"Nhập nhiều link cho video dòng {targetRows[0].Index + 1}" : $"Nhập link cho {targetRows.Count} video đã chọn",
            Width = 630,
            Height = 530,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(246, 250, 254),
            Font = new Font("Segoe UI", 9F)
        };

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 5)
        };
        var lblTitle = new Label
        {
            Text = isSingleRow
                ? $"Dán các link tiếp thị cho video dòng {targetRows[0].Index + 1}"
                : $"Dán các link tiếp thị cho {targetRows.Count} video đã chọn",
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = Color.FromArgb(31, 31, 44),
            Dock = DockStyle.Top,
            Height = 22
        };
        var lblSub = new Label
        {
            Text = "Mỗi dòng là một link Shopee. Hệ thống sẽ tự động lọc link và gắn vào quy trình video.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(140, 145, 165),
            Dock = DockStyle.Top,
            Height = 18
        };
        pnlHeader.Controls.AddRange(new Control[] { lblSub, lblTitle });

        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 12, 20, 10)
        };

        var txtInput = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5F),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var pnlOptions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 90,
            Padding = new Padding(0, 8, 0, 0)
        };

        var rbCombine = new RadioButton
        {
            Text = isSingleRow
                ? $"Gán TẤT CẢ các link này cho 1 video đang chọn (Dòng {targetRows[0].Index + 1})"
                : $"Gán TẤT CẢ các link này cho từng video đã chọn ({targetRows.Count} video)",
            Checked = true,
            AutoSize = true,
            Location = new Point(4, 8),
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(0, 161, 112)
        };

        var rbSequential = new RadioButton
        {
            Text = isSingleRow
                ? $"Điền lần lượt từ video này trở xuống các dòng tiếp theo (Dòng {targetRows[0].Index + 1} -> Link 1, Dòng {targetRows[0].Index + 2} -> Link 2...)"
                : $"Điền lần lượt từng link vào từng video đã chọn (Video 1 -> Link 1, Video 2 -> Link 2...)",
            Checked = false,
            AutoSize = true,
            Location = new Point(4, 32),
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(45, 55, 72)
        };

        var lblCount = new Label
        {
            Text = $"Đã nhận diện: 0 link hợp lệ | Đang áp dụng cho: {(isSingleRow ? $"Dòng {targetRows[0].Index + 1} (1 video)" : $"{targetRows.Count} video đã chọn")} / Tổng { _bulkGrid.Rows.Count} video",
            AutoSize = true,
            Location = new Point(4, 58),
            ForeColor = Color.FromArgb(0, 161, 112),
            Font = new Font("Segoe UI Semibold", 8.5F)
        };

        txtInput.TextChanged += (_, _) =>
        {
            var links = ExtractAllLinks(txtInput.Text);
            lblCount.Text = $"Đã nhận diện: {links.Count} link hợp lệ | Đang áp dụng cho: {(isSingleRow ? $"Dòng {targetRows[0].Index + 1} (1 video)" : $"{targetRows.Count} video đã chọn")} / Tổng {_bulkGrid.Rows.Count} video";
        };

        pnlOptions.Controls.AddRange(new Control[] { rbSequential, rbCombine, lblCount });

        pnlBody.Controls.Add(txtInput);
        pnlBody.Controls.Add(pnlOptions);

        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(15, 8, 15, 8),
            BackColor = Color.FromArgb(240, 244, 248)
        };

        var btnApply = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Áp dụng",
            Width = 110,
            Height = 34,
            FillColor = Color.FromArgb(0, 161, 112),
            ForeColor = Color.White,
            BorderRadius = 6,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };

        var btnCancel = new Guna.UI2.WinForms.Guna2Button
        {
            Text = "Hủy",
            Width = 80,
            Height = 34,
            FillColor = Color.FromArgb(228, 232, 240),
            ForeColor = Color.FromArgb(70, 70, 85),
            BorderRadius = 6,
            Font = new Font("Segoe UI", 9F),
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand
        };

        btnApply.Click += (_, _) =>
        {
            var links = ExtractAllLinks(txtInput.Text);
            if (links.Count == 0)
            {
                MessageBox.Show(dlg, "Vui lòng nhập ít nhất 1 đường link hợp lệ.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (rbSequential.Checked)
            {
                if (isSingleRow)
                {
                    var startIndex = targetRows[0].Index;
                    for (int i = 0; i < links.Count && (startIndex + i) < _bulkGrid.Rows.Count; i++)
                    {
                        _bulkGrid.Rows[startIndex + i].Cells["colLink"].Value = links[i];
                    }
                }
                else
                {
                    for (int i = 0; i < targetRows.Count; i++)
                    {
                        if (i < links.Count)
                        {
                            targetRows[i].Cells["colLink"].Value = links[i];
                        }
                    }
                }
            }
            else
            {
                var joined = string.Join(", ", links);
                foreach (var row in targetRows)
                {
                    row.Cells["colLink"].Value = joined;
                }
            }

            RefreshBulkLinkPicker();
            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        pnlBottom.Controls.AddRange(new Control[] { btnCancel, btnApply });

        dlg.Controls.Add(pnlBody);
        dlg.Controls.Add(pnlBottom);
        dlg.Controls.Add(pnlHeader);

        // Preload existing link if single row, else check clipboard
        var existingLink = isSingleRow ? (targetRows[0].Cells["colLink"].Value?.ToString() ?? "").Trim() : "";
        if (!string.IsNullOrWhiteSpace(existingLink))
        {
            var existingLinks = ExtractAllLinks(existingLink);
            txtInput.Text = existingLinks.Count > 0 ? string.Join(Environment.NewLine, existingLinks) : existingLink;
            txtInput.SelectionStart = txtInput.Text.Length;
        }
        else
        {
            try
            {
                var clip = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(clip) && clip.Contains("http", StringComparison.OrdinalIgnoreCase))
                {
                    txtInput.Text = clip.Trim();
                    txtInput.SelectionStart = txtInput.Text.Length;
                }
            }
            catch { }
        }

        dlg.ShowDialog(this);
    }

    private void ShowWarn(string msg) => MessageBox.Show(this, msg, "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
