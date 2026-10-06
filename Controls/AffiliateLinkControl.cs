using System.Diagnostics;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader.Controls;

/// <summary>Kho Link AFF có ảnh đại diện và tìm kiếm bằng hình ảnh.</summary>
public sealed class AffiliateLinkControl : UserControl
{
    private const int SimilarImageDistance = 18;
    private readonly DatabaseService _database;
    private readonly Guna.UI2.WinForms.Guna2DataGridView _grid;
    private readonly Guna.UI2.WinForms.Guna2TextBox _txtSearch;
    private readonly Label _lblCount;
    private readonly Label _lblImageFilter;
    private readonly Guna.UI2.WinForms.Guna2Button _btnClearImageFilter;
    private readonly ContextMenuStrip _rowMenu;
    private readonly System.Windows.Forms.Timer _searchDebounceTimer;
    private readonly Dictionary<string, Image> _thumbnailCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _thumbnailLoads = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _thumbnailSync = new();
    private readonly Image _placeholderImage;
    private List<AffiliateLinkItem> _items = [];
    private string _searchImageHash = string.Empty;
    private string _searchImagePath = string.Empty;
    private bool _hasLoaded;
    private bool _isLoadingData;
    private bool _isDisposed;

    public event EventHandler<string>? StatusChanged;

    public AffiliateLinkControl(DatabaseService database)
    {
        _database = database;
        _placeholderImage = CreatePlaceholderImage();
        _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 180 };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilters();
        };
        _rowMenu = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9.5F),
            RenderMode = ToolStripRenderMode.System
        };
        _rowMenu.Items.Add("Sao chép tất cả link", null, (_, _) => CopySelectedLink());
        _rowMenu.Items.Add("Mở tất cả link", null, (_, _) => OpenSelectedLink());
        _rowMenu.Items.Add("Sửa", null, (_, _) => EditSelectedItem());
        _rowMenu.Items.Add(new ToolStripSeparator());
        _rowMenu.Items.Add("Xóa", null, (_, _) => DeleteSelectedItem());
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(244, 245, 248);
        Padding = new Padding(16, 14, 16, 14);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = Color.White,
            Padding = new Padding(18, 10, 18, 8)
        };
        header.Controls.Add(new Label
        {
            Text = "KHO LINK AFF",
            AutoSize = true,
            Location = new Point(18, 11),
            Font = new Font("Segoe UI Semibold", 12F),
            ForeColor = Color.FromArgb(31, 31, 44)
        });
        header.Controls.Add(new Label
        {
            Text = "Lưu link tiếp thị cùng ảnh sản phẩm · tìm nhanh bằng từ khóa hoặc hình ảnh",
            AutoSize = true,
            Location = new Point(19, 39),
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 8.7F)
        });

        var searchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 15, 0, 0),
            BackColor = Color.Transparent
        };
        _txtSearch = new Guna.UI2.WinForms.Guna2TextBox
        {
            PlaceholderText = "🔍  Tìm tên, link hoặc ghi chú...",
            Width = 320,
            Height = 38,
            BorderRadius = 8,
            BorderColor = Color.FromArgb(203, 213, 225),
            FillColor = Color.FromArgb(248, 250, 252),
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(0, 0, 8, 0)
        };
        _txtSearch.TextChanged += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        };
        var btnClearText = CreateButton("✕", Color.FromArgb(243, 244, 246), Color.FromArgb(100, 116, 139), 38);
        btnClearText.Margin = new Padding(0, 0, 8, 0);
        btnClearText.Click += (_, _) => _txtSearch.Clear();
        _lblCount = new Label
        {
            Text = "0 sản phẩm",
            AutoSize = true,
            Padding = new Padding(9, 7, 9, 7),
            Margin = new Padding(0, 3, 0, 0),
            BackColor = Color.FromArgb(238, 242, 255),
            ForeColor = Color.FromArgb(79, 70, 229),
            Font = new Font("Segoe UI Semibold", 8.5F)
        };
        searchPanel.Controls.AddRange([_txtSearch, btnClearText, _lblCount]);
        header.Controls.Add(searchPanel);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 54,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };
        var btnAdd = CreateButton("＋ Thêm Link AFF", Color.FromArgb(79, 70, 229), Color.White, 130);
        var btnEdit = CreateButton("✎ Sửa", Color.FromArgb(224, 231, 255), Color.FromArgb(67, 56, 202), 82);
        var btnDelete = CreateButton("🗑 Xóa", Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28), 82);
        var btnCopy = CreateButton("⧉ Sao chép các link", Color.FromArgb(236, 253, 245), Color.FromArgb(5, 150, 105), 142);
        var btnOpen = CreateButton("↗ Mở các link", Color.FromArgb(240, 249, 255), Color.FromArgb(3, 105, 161), 108);
        var btnSearchImage = CreateButton("▣ Tìm theo ảnh", Color.FromArgb(255, 247, 237), Color.FromArgb(194, 65, 12), 124);
        _btnClearImageFilter = CreateButton("Bỏ lọc ảnh", Color.FromArgb(243, 244, 246), Color.FromArgb(71, 85, 105), 98);
        _btnClearImageFilter.Visible = false;

        btnAdd.Click += (_, _) => AddItem();
        btnEdit.Click += (_, _) => EditSelectedItem();
        btnDelete.Click += (_, _) => DeleteSelectedItem();
        btnCopy.Click += (_, _) => CopySelectedLink();
        btnOpen.Click += (_, _) => OpenSelectedLink();
        btnSearchImage.Click += (_, _) => SearchByImage();
        _btnClearImageFilter.Click += (_, _) => ClearImageFilter();
        toolbar.Controls.AddRange([btnAdd, btnEdit, btnDelete, btnCopy, btnOpen, btnSearchImage, _btnClearImageFilter]);

        var imageFilterBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 35,
            Visible = true,
            BackColor = Color.FromArgb(255, 251, 235),
            Padding = new Padding(18, 8, 12, 4)
        };
        _lblImageFilter = new Label
        {
            Text = "Mẹo: dùng “Tìm theo ảnh” để chọn ảnh sản phẩm cần tra cứu.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(146, 64, 14),
            Font = new Font("Segoe UI", 8.5F)
        };
        imageFilterBar.Controls.Add(_lblImageFilter);

        _grid = new Guna.UI2.WinForms.Guna2DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(235, 237, 242),
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false
        };
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(244, 245, 248),
            ForeColor = Color.FromArgb(79, 70, 229),
            Font = new Font("Segoe UI Semibold", 9.5F),
            Padding = new Padding(10, 8, 10, 8)
        };
        _grid.ColumnHeadersHeight = 44;
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 41, 55),
            SelectionBackColor = Color.FromArgb(238, 242, 255),
            SelectionForeColor = Color.FromArgb(31, 41, 55),
            Padding = new Padding(10, 6, 10, 6),
            Font = new Font("Segoe UI", 9.5F),
            WrapMode = DataGridViewTriState.False
        };
        _grid.RowTemplate.Height = 86;
        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "Image",
            HeaderText = "Ảnh",
            Width = 118,
            MinimumWidth = 118,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        AddTextColumn("Name", "Tên gợi nhớ", 22F);
        AddTextColumn("Url", "Link AFF", 38F);
        AddTextColumn("Notes", "Ghi chú", 25F);
        AddTextColumn("UpdatedAt", "Cập nhật", 15F);
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelectedItem(); };
        _grid.KeyDown += GridKeyDown;
        _grid.CellMouseDown += GridCellMouseDown;
        typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(_grid, true);

        Controls.Add(_grid);
        Controls.Add(imageFilterBar);
        Controls.Add(toolbar);
        Controls.Add(header);
        Load += (_, _) => RefreshData();
    }

    private static Guna.UI2.WinForms.Guna2Button CreateButton(string text, Color fill, Color fore, int width) => new()
    {
        Text = text,
        Width = width,
        Height = 36,
        FillColor = fill,
        ForeColor = fore,
        BorderRadius = 7,
        Font = new Font("Segoe UI Semibold", 9F),
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 7, 0)
    };

    private void AddTextColumn(string name, string header, float weight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    public void RefreshData(bool force = false)
    {
        if ((_hasLoaded && !force) || _isLoadingData || _isDisposed) return;
        try
        {
            _isLoadingData = true;
            var imported = !_hasLoaded ? _database.ImportAffiliateLinksFromJobs() : 0;
            _items = _database.GetAllAffiliateLinks();
            _hasLoaded = true;
            ApplyFilters();
            if (imported > 0)
                RaiseStatus($"Đã đồng bộ thêm {imported} Link AFF từ Dữ liệu & công việc.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Không thể tải kho Link AFF: {ex.Message}", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private void ApplyFilters()
    {
        if (_isDisposed) return;
        _searchDebounceTimer.Stop();

        var query = _txtSearch.Text.Trim();
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var filtered = _items
            .Select(item => new
            {
                Item = item,
                Distance = string.IsNullOrWhiteSpace(_searchImageHash)
                    ? (int?)null
                    : ImageSimilarity.HammingDistance(_searchImageHash, item.ImageHash)
            })
            .Where(x => tokens.Length == 0 || tokens.All(token =>
                $"{x.Item.Name} {x.Item.Url} {x.Item.Notes}".Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Where(x => string.IsNullOrWhiteSpace(_searchImageHash) || x.Distance <= SimilarImageDistance)
            .OrderBy(x => x.Distance ?? 0)
            .ThenByDescending(x => x.Item.UpdatedAt)
            .ToList();

        var thumbnailsToLoad = new List<(AffiliateLinkItem Item, string CacheKey)>();
        _grid.SuspendLayout();
        try
        {
            _grid.Rows.Clear();
            foreach (var result in filtered)
            {
                var image = _placeholderImage;
                var cacheKey = GetThumbnailCacheKey(result.Item.ImagePath);
                if (cacheKey != null)
                {
                    lock (_thumbnailSync)
                    {
                        if (_thumbnailCache.TryGetValue(cacheKey, out var cached))
                            image = cached;
                        else
                            thumbnailsToLoad.Add((result.Item, cacheKey));
                    }
                }

                var urls = result.Item.GetUrls();
                var rowIndex = _grid.Rows.Add(
                    image,
                    result.Item.Name,
                    FormatUrlsForGrid(urls),
                    result.Item.Notes,
                    FormatDate(result.Item.UpdatedAt));
                var row = _grid.Rows[rowIndex];
                row.Tag = result.Item;
                row.Height = Math.Max(86, 38 + urls.Count * 19);
                row.Cells["Image"].ToolTipText = result.Distance.HasValue
                    ? $"Độ lệch ảnh: {result.Distance.Value}/64 (càng thấp càng giống)"
                    : (cacheKey != null ? Path.GetFileName(result.Item.ImagePath) : "Chưa có ảnh");
                row.Cells["Url"].Style.ForeColor = Color.FromArgb(37, 99, 235);
                row.Cells["Url"].Style.WrapMode = DataGridViewTriState.True;
                row.Cells["Url"].ToolTipText = string.Join(Environment.NewLine, urls);
            }
        }
        finally
        {
            _grid.ResumeLayout(false);
        }

        _lblCount.Text = filtered.Count == _items.Count
            ? $"{_items.Count} sản phẩm"
            : $"{filtered.Count} / {_items.Count} sản phẩm";
        QueueThumbnailLoads(thumbnailsToLoad);
    }

    private static string? GetThumbnailCacheKey(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)) return null;
        try
        {
            return $"{Path.GetFullPath(imagePath)}|{File.GetLastWriteTimeUtc(imagePath).Ticks}";
        }
        catch { return null; }
    }

    private static Image CreatePlaceholderImage()
    {
        var placeholder = new Bitmap(104, 70);
        using var graphics = Graphics.FromImage(placeholder);
        graphics.Clear(Color.FromArgb(248, 250, 252));
        using var pen = new Pen(Color.FromArgb(203, 213, 225));
        graphics.DrawRectangle(pen, 0, 0, 103, 69);
        using var font = new Font("Segoe UI", 8F);
        using var brush = new SolidBrush(Color.FromArgb(148, 163, 184));
        var text = "Chưa có ảnh";
        var size = graphics.MeasureString(text, font);
        graphics.DrawString(text, font, brush, (104 - size.Width) / 2, (70 - size.Height) / 2);
        return placeholder;
    }

    private void QueueThumbnailLoads(IEnumerable<(AffiliateLinkItem Item, string CacheKey)> candidates)
    {
        var queue = new List<(AffiliateLinkItem Item, string CacheKey)>();
        lock (_thumbnailSync)
        {
            foreach (var candidate in candidates)
            {
                if (_thumbnailCache.ContainsKey(candidate.CacheKey) || !_thumbnailLoads.Add(candidate.CacheKey)) continue;
                queue.Add(candidate);
            }
        }
        if (queue.Count == 0) return;

        _ = Task.Run(() =>
        {
            foreach (var candidate in queue)
            {
                Image? thumbnail = null;
                try { thumbnail = ImageSimilarity.CreateThumbnail(candidate.Item.ImagePath, 104, 70); }
                catch { }

                lock (_thumbnailSync)
                {
                    _thumbnailLoads.Remove(candidate.CacheKey);
                    if (thumbnail != null && !_isDisposed)
                        _thumbnailCache[candidate.CacheKey] = thumbnail;
                    else
                    {
                        thumbnail?.Dispose();
                        thumbnail = null;
                    }
                }

                if (thumbnail == null || _isDisposed || !IsHandleCreated) continue;
                try
                {
                    BeginInvoke(() =>
                    {
                        if (_isDisposed) return;
                        foreach (DataGridViewRow row in _grid.Rows)
                        {
                            if (row.Tag is AffiliateLinkItem item && item.Id == candidate.Item.Id)
                            {
                                row.Cells["Image"].Value = thumbnail;
                                _grid.InvalidateCell(row.Cells["Image"]);
                                break;
                            }
                        }
                    });
                }
                catch (InvalidOperationException) { }
            }
        });
    }

    private static string FormatDate(string value)
        => DateTime.TryParse(value, out var date) ? date.ToString("dd/MM/yyyy HH:mm") : value;

    private static string FormatUrlsForGrid(IReadOnlyList<string> urls)
    {
        if (urls.Count == 0) return "Chưa có link";
        if (urls.Count == 1) return urls[0];
        return $"{urls.Count} LINK AFF\n" + string.Join("\n", urls);
    }

    private AffiliateLinkItem? SelectedItem => _grid.CurrentRow?.Tag as AffiliateLinkItem;

    private void AddItem()
    {
        using var dialog = new AffiliateLinkEditorDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _database.SaveAffiliateLink(dialog.Value);
            RefreshData(force: true);
            RaiseStatus($"Đã thêm Link AFF “{dialog.Value.Name}”.");
        }
        catch (Exception ex) { ShowDataError("Không thể thêm Link AFF", ex); }
    }

    private void EditSelectedItem()
    {
        var selected = SelectedItem;
        if (selected == null) { ShowSelectMessage(); return; }
        using var dialog = new AffiliateLinkEditorDialog(selected);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _database.UpdateAffiliateLink(dialog.Value);
            RefreshData(force: true);
            RaiseStatus($"Đã cập nhật Link AFF “{dialog.Value.Name}”.");
        }
        catch (Exception ex) { ShowDataError("Không thể cập nhật Link AFF", ex); }
    }

    private void DeleteSelectedItem()
    {
        var selected = SelectedItem;
        if (selected == null) { ShowSelectMessage(); return; }
        if (MessageBox.Show(this, $"Xóa Link AFF “{selected.Name}”?\n\nẢnh đã lưu vẫn được giữ lại để tránh mất dữ liệu ngoài ý muốn.",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            _database.DeleteAffiliateLink(selected.Id);
            RefreshData(force: true);
            RaiseStatus($"Đã xóa Link AFF “{selected.Name}”.");
        }
        catch (Exception ex) { ShowDataError("Không thể xóa Link AFF", ex); }
    }

    private void CopySelectedLink()
    {
        var selected = SelectedItem;
        if (selected == null) { ShowSelectMessage(); return; }
        try
        {
            var urls = selected.GetUrls();
            if (urls.Count == 0) return;
            Clipboard.SetText(string.Join(Environment.NewLine, urls));
            RaiseStatus($"Đã sao chép {urls.Count} link của “{selected.Name}”.");
        }
        catch (Exception ex) { ShowDataError("Không thể sao chép link", ex); }
    }

    private void OpenSelectedLink()
    {
        var selected = SelectedItem;
        if (selected == null) { ShowSelectMessage(); return; }
        var urls = selected.GetUrls();
        if (urls.Count == 0) return;
        if (urls.Count > 1 && MessageBox.Show(this,
                $"Sản phẩm này có {urls.Count} Link AFF. Mở tất cả trong trình duyệt?",
                "Mở nhiều link", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        try
        {
            foreach (var url in urls)
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            RaiseStatus($"Đã mở {urls.Count} link của “{selected.Name}”.");
        }
        catch (Exception ex) { ShowDataError("Không thể mở link", ex); }
    }

    private void SearchByImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh sản phẩm cần tìm",
            Filter = "Tệp ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|Tất cả tệp|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            BackfillMissingImageHashes();
            _searchImageHash = ImageSimilarity.ComputeDifferenceHash(dialog.FileName);
            _searchImagePath = dialog.FileName;
            _btnClearImageFilter.Visible = true;
            _lblImageFilter.Text = $"Đang tìm ảnh giống “{Path.GetFileName(_searchImagePath)}” · kết quả gần giống nhất được xếp lên đầu.";
            ApplyFilters();
            RaiseStatus($"Đã tìm Link AFF theo ảnh {Path.GetFileName(_searchImagePath)}.");
        }
        catch (Exception ex) { ShowDataError("Không thể tìm theo ảnh đã chọn", ex); }
    }

    private void BackfillMissingImageHashes()
    {
        foreach (var item in _items.Where(x => string.IsNullOrWhiteSpace(x.ImageHash) && File.Exists(x.ImagePath)))
        {
            try
            {
                item.ImageHash = ImageSimilarity.ComputeDifferenceHash(item.ImagePath);
                _database.UpdateAffiliateLinkImageHash(item.Id, item.ImageHash);
            }
            catch { /* Bỏ qua file ảnh lỗi; các link khác vẫn tìm được. */ }
        }
    }

    private void ClearImageFilter()
    {
        _searchImageHash = string.Empty;
        _searchImagePath = string.Empty;
        _btnClearImageFilter.Visible = false;
        _lblImageFilter.Text = "Mẹo: dùng “Tìm theo ảnh” để chọn ảnh sản phẩm cần tra cứu.";
        ApplyFilters();
        RaiseStatus("Đã bỏ bộ lọc tìm theo ảnh.");
    }

    private void GridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) { EditSelectedItem(); e.Handled = true; }
        else if (e.KeyCode == Keys.Delete) { DeleteSelectedItem(); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.C) { CopySelectedLink(); e.Handled = true; }
    }

    private void GridCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
        _grid.ClearSelection();
        _grid.Rows[e.RowIndex].Selected = true;
        _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)];
        _rowMenu.Show(_grid, _grid.PointToClient(Cursor.Position));
    }

    private void ShowSelectMessage()
        => MessageBox.Show(this, "Vui lòng chọn một Link AFF trong danh sách.", "Chưa chọn link", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void ShowDataError(string message, Exception ex)
        => MessageBox.Show(this, $"{message}: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private void RaiseStatus(string message) => StatusChanged?.Invoke(this, message);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposed = true;
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Dispose();
            _rowMenu.Dispose();
            lock (_thumbnailSync)
            {
                foreach (var image in _thumbnailCache.Values) image.Dispose();
                _thumbnailCache.Clear();
                _thumbnailLoads.Clear();
            }
            _placeholderImage.Dispose();
        }
        base.Dispose(disposing);
    }
}
