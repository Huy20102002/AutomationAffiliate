using Guna.UI2.WinForms;

namespace ShopeeVideoUploader.Controls;

/// <summary>
/// Dialog cho phép người dùng sửa hoặc dán hàng loạt tiêu đề cho các video được chọn (mỗi dòng 1 tiêu đề).
/// </summary>
public sealed class BulkTitleEditorDialog : Form
{
    private readonly DataGridView _grid;
    private readonly Label _lblInfo;
    public List<string> ResultTitles { get; } = [];

    public BulkTitleEditorDialog(IReadOnlyList<(string VideoPath, string Title)> items)
    {
        Text = $"Sửa danh sách tiêu đề ({items.Count} video)";
        Size = new Size(880, 600);
        MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(244, 245, 248);
        Font = new Font("Segoe UI", 9F);
        ShowIcon = false;
        MinimizeBox = false;

        // ── 1. Header ──
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };
        var lblTitle = new Label
        {
            Text = $"✏️ SỬA TIÊU ĐỀ HÀNG LOẠT ({items.Count} VIDEO)",
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Color.FromArgb(31, 31, 44),
            AutoSize = true,
            Location = new Point(18, 10)
        };
        var lblSubtitle = new Label
        {
            Text = "Mỗi dòng trong bảng tương ứng với 1 video theo thứ tự. Bạn có thể sửa trực tiếp hoặc bấm [Dán từ Clipboard] để nhập nhanh danh sách.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(114, 117, 134),
            AutoSize = true,
            Location = new Point(19, 35)
        };
        pnlHeader.Controls.AddRange([lblTitle, lblSubtitle]);

        // ── 2. Toolbar ──
        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.FromArgb(250, 251, 253),
            Padding = new Padding(16, 7, 16, 7),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var btnPasteList = CreateButton("📋 Dán từ Clipboard (mỗi dòng 1 tiêu đề)", Color.FromArgb(79, 70, 229), 260);
        btnPasteList.Click += (_, _) => PasteTitlesFromClipboard();

        var btnUseFileName = CreateButton("📂 Lấy lại tên file làm tiêu đề", Color.FromArgb(96, 82, 218), 195);
        btnUseFileName.Click += (_, _) => ResetTitlesToFileName();

        var btnAddHashtag = CreateButton("✨ Thêm Hashtag / Hậu tố...", Color.FromArgb(10, 151, 205), 185);
        btnAddHashtag.Click += (_, _) => PromptAddSuffix();

        var btnClear = CreateButton("🧹 Xóa trắng", Color.FromArgb(241, 226, 229), 95);
        btnClear.ForeColor = Color.FromArgb(180, 50, 50);
        btnClear.Click += (_, _) => ClearAllTitles();

        _lblInfo = new Label
        {
            Text = $"{items.Count} video",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.FromArgb(79, 70, 229),
            Margin = new Padding(12, 8, 0, 0)
        };

        pnlToolbar.Controls.AddRange([btnPasteList, btnUseFileName, btnAddHashtag, btnClear, _lblInfo]);

        // ── 3. DataGridView ──
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(235, 237, 242),
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 40 },
            Font = new Font("Segoe UI", 9.5F),
            SelectionMode = DataGridViewSelectionMode.CellSelect
        };
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(244, 245, 248),
            ForeColor = Color.FromArgb(96, 82, 218),
            Font = new Font("Segoe UI Semibold", 9.5F),
            Padding = new Padding(8, 4, 8, 4)
        };
        _grid.ColumnHeadersHeight = 38;
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 31, 44),
            SelectionBackColor = Color.FromArgb(225, 240, 252),
            SelectionForeColor = Color.FromArgb(31, 31, 44),
            Padding = new Padding(8, 2, 8, 2)
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colStt",
            HeaderText = "STT",
            Width = 50,
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(120, 125, 145)
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colFile",
            HeaderText = "File video / ảnh",
            Width = 240,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Color.FromArgb(110, 115, 130)
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTitle",
            HeaderText = "Tiêu đề (click vào ô để sửa)",
            FillWeight = 60,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        // Nạp dữ liệu
        for (int i = 0; i < items.Count; i++)
        {
            var (path, title) = items[i];
            var fileName = Path.GetFileName(path.Split(';').FirstOrDefault()?.Trim() ?? path);
            var rowIdx = _grid.Rows.Add(i + 1, fileName, title);
            _grid.Rows[rowIdx].Cells["colFile"].ToolTipText = path;
            _grid.Rows[rowIdx].Tag = path;
        }

        // ── 4. Bottom panel ──
        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            BackColor = Color.FromArgb(250, 251, 253),
            Padding = new Padding(20, 10, 20, 10),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var btnCancel = new Guna2Button
        {
            Text = "Hủy",
            Width = 90,
            Height = 36,
            BorderRadius = 7,
            FillColor = Color.FromArgb(235, 237, 242),
            ForeColor = Color.FromArgb(70, 70, 85),
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 0, 0, 0)
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnSave = new Guna2Button
        {
            Text = "💾 Áp dụng & Lưu",
            Width = 145,
            Height = 36,
            BorderRadius = 7,
            FillColor = Color.FromArgb(0, 161, 112),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };
        btnSave.Click += (_, _) => SaveAndApply();

        pnlBottom.Controls.AddRange([btnCancel, btnSave]);

        // Dock layout: Fill phải ở dưới Bottom và Top
        Controls.Add(_grid);
        Controls.Add(pnlBottom);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlHeader);
        _grid.SendToBack();
    }

    private void PasteTitlesFromClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show(this, "Clipboard không có văn bản nào.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var text = Clipboard.GetText();
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(l => l.Trim())
                .ToList();

            // Bỏ các dòng trống ở cuối nếu có
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
                lines.RemoveAt(lines.Count - 1);

            if (lines.Count == 0)
            {
                MessageBox.Show(this, "Nội dung trong Clipboard trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var applyCount = Math.Min(lines.Count, _grid.Rows.Count);
            for (int i = 0; i < applyCount; i++)
            {
                _grid.Rows[i].Cells["colTitle"].Value = lines[i];
            }

            _lblInfo.Text = $"Đã dán {applyCount} / {_grid.Rows.Count} tiêu đề";
            _lblInfo.ForeColor = Color.FromArgb(0, 161, 112);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Lỗi dán dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetTitlesToFileName()
    {
        for (int i = 0; i < _grid.Rows.Count; i++)
        {
            var rawPath = _grid.Rows[i].Tag as string ?? "";
            var firstFile = rawPath.Split(';').FirstOrDefault()?.Trim() ?? "";
            var fileName = !string.IsNullOrWhiteSpace(firstFile) ? Path.GetFileNameWithoutExtension(firstFile) : "";
            _grid.Rows[i].Cells["colTitle"].Value = fileName;
        }
        _lblInfo.Text = $"Đã đặt lại {_grid.Rows.Count} tiêu đề theo tên file";
    }

    private void PromptAddSuffix()
    {
        using var prompt = new Form
        {
            Text = "Thêm Hashtag / Hậu tố vào tiêu đề",
            Size = new Size(420, 190),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White
        };

        var lbl = new Label
        {
            Text = "Nhập nội dung cần thêm vào cuối mỗi tiêu đề (ví dụ: #shopee #review):",
            AutoSize = false,
            Size = new Size(380, 36),
            Location = new Point(16, 14),
            Font = new Font("Segoe UI", 9F)
        };
        var txt = new Guna2TextBox
        {
            Width = 370,
            Height = 34,
            Location = new Point(16, 54),
            PlaceholderText = "Nhập hashtag hoặc chữ muốn thêm...",
            BorderRadius = 6,
            Font = new Font("Segoe UI", 9F)
        };
        var btnOk = new Guna2Button
        {
            Text = "Đồng ý",
            Width = 90,
            Height = 32,
            Location = new Point(296, 102),
            BorderRadius = 6,
            FillColor = Color.FromArgb(79, 70, 229),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand
        };
        btnOk.Click += (_, _) => { prompt.DialogResult = DialogResult.OK; prompt.Close(); };

        prompt.Controls.AddRange([lbl, txt, btnOk]);
        prompt.AcceptButton = btnOk;

        if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
        {
            var suffix = txt.Text.Trim();
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                var cur = Convert.ToString(_grid.Rows[i].Cells["colTitle"].Value) ?? "";
                if (string.IsNullOrWhiteSpace(cur))
                    _grid.Rows[i].Cells["colTitle"].Value = suffix;
                else if (!cur.Contains(suffix, StringComparison.OrdinalIgnoreCase))
                    _grid.Rows[i].Cells["colTitle"].Value = $"{cur} {suffix}";
            }
        }
    }

    private void ClearAllTitles()
    {
        for (int i = 0; i < _grid.Rows.Count; i++)
        {
            _grid.Rows[i].Cells["colTitle"].Value = string.Empty;
        }
        _lblInfo.Text = "Đã xóa trắng tất cả tiêu đề";
        _lblInfo.ForeColor = Color.FromArgb(180, 50, 50);
    }

    private void SaveAndApply()
    {
        _grid.EndEdit();
        ResultTitles.Clear();
        for (int i = 0; i < _grid.Rows.Count; i++)
        {
            ResultTitles.Add(Convert.ToString(_grid.Rows[i].Cells["colTitle"].Value)?.Trim() ?? string.Empty);
        }
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Guna2Button CreateButton(string text, Color color, int width)
    {
        return new Guna2Button
        {
            Text = text,
            Width = width,
            Height = 34,
            FillColor = color,
            BorderRadius = 7,
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
    }
}
