using Guna.UI2.WinForms;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader.Controls;

/// <summary>
/// Dialog sửa danh sách tiêu đề dạng văn bản nhiều dòng (mỗi dòng 1 tiêu đề)
/// Hỗ trợ viết trực tiếp, dán danh sách, và tích hợp tạo/sửa tiêu đề bằng AI.
/// </summary>
public sealed class BulkTitleEditorDialog : Form
{
    private readonly TextBox _txtTitles;
    private readonly Label _lblLineCount;
    private readonly Guna2Button _btnAiGenerate;
    private readonly IReadOnlyList<(string VideoPath, string Title)> _items;
    private bool _isGeneratingAi = false;

    public List<string> ResultTitles { get; } = [];

    public BulkTitleEditorDialog(IReadOnlyList<(string VideoPath, string Title)> items)
    {
        _items = items ?? [];

        Text = $"Sửa danh sách tiêu đề ({_items.Count} video)";
        Size = new Size(820, 560);
        MinimumSize = new Size(650, 420);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24, 24, 27);
        Font = new Font("Segoe UI", 9F);
        ShowIcon = false;
        MinimizeBox = false;

        // ── 1. Header ──
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = Color.FromArgb(32, 33, 36),
            Padding = new Padding(18, 8, 18, 8)
        };
        var lblTitle = new Label
        {
            Text = $"📝 SỬA DANH SÁCH TIÊU ĐỀ ({_items.Count} VIDEO)",
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 8)
        };
        var lblSubtitle = new Label
        {
            Text = "Mỗi dòng tương ứng với 1 video theo thứ tự. Bạn có thể gõ, dán danh sách từ Excel/Notepad hoặc bấm [Tạo tiêu đề AI].",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(160, 165, 175),
            AutoSize = true,
            Location = new Point(17, 33)
        };
        pnlHeader.Controls.AddRange([lblTitle, lblSubtitle]);

        // ── 2. Toolbar ──
        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.FromArgb(39, 39, 42),
            Padding = new Padding(14, 6, 14, 6),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _btnAiGenerate = CreateButton("✨ Tạo tiêu đề AI", Color.FromArgb(147, 51, 234), 145);
        _btnAiGenerate.Click += async (_, _) => await GenerateAiTitlesAsync();

        var btnPaste = CreateButton("📋 Dán từ Clipboard", Color.FromArgb(79, 70, 229), 150);
        btnPaste.Click += (_, _) => PasteFromClipboard();

        var btnUseFileName = CreateButton("📂 Lấy tên file gốc", Color.FromArgb(59, 130, 246), 140);
        btnUseFileName.Click += (_, _) => ResetToFileName();

        var btnAddHashtag = CreateButton("✨ Thêm Hashtag...", Color.FromArgb(13, 148, 136), 140);
        btnAddHashtag.Click += (_, _) => PromptAddSuffix();

        var btnClear = CreateButton("🧹 Xóa trắng", Color.FromArgb(75, 85, 99), 100);
        btnClear.Click += (_, _) => ClearAll();

        _lblLineCount = new Label
        {
            Text = $"0 / {_items.Count} dòng",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(52, 211, 153),
            Margin = new Padding(12, 7, 0, 0)
        };

        pnlToolbar.Controls.AddRange([_btnAiGenerate, btnPaste, btnUseFileName, btnAddHashtag, btnClear, _lblLineCount]);

        // ── 3. Multi-line Text Editor (Dark Theme) ──
        var pnlEditor = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = Color.FromArgb(24, 24, 27)
        };

        _txtTitles = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(240, 240, 240),
            Font = new Font("Consolas", 10.5F),
            BorderStyle = BorderStyle.FixedSingle
        };
        _txtTitles.TextChanged += (_, _) => UpdateLineCount();

        // Nạp tiêu đề hiện tại
        var initialLines = _items.Select(i => i.Title ?? string.Empty).ToList();
        _txtTitles.Text = string.Join(Environment.NewLine, initialLines);
        _txtTitles.SelectionStart = 0;
        _txtTitles.SelectionLength = 0;

        pnlEditor.Controls.Add(_txtTitles);

        // ── 4. Bottom panel ──
        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.FromArgb(32, 33, 36),
            Padding = new Padding(18, 10, 18, 10),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var btnCancel = new Guna2Button
        {
            Text = "Hủy",
            Width = 90,
            Height = 36,
            BorderRadius = 7,
            FillColor = Color.FromArgb(63, 63, 70),
            ForeColor = Color.FromArgb(228, 228, 231),
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
            FillColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };
        btnSave.Click += (_, _) => SaveAndApply();

        pnlBottom.Controls.AddRange([btnCancel, btnSave]);

        // Add controls to Form
        Controls.Add(pnlEditor);
        Controls.Add(pnlBottom);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlHeader);
        pnlEditor.BringToFront();

        UpdateLineCount();
    }

    private void UpdateLineCount()
    {
        var lines = GetLines();
        var count = lines.Count;
        var target = _items.Count;

        if (count == target)
        {
            _lblLineCount.Text = $"✔ {count} / {target} dòng (khớp đủ video)";
            _lblLineCount.ForeColor = Color.FromArgb(52, 211, 153);
        }
        else if (count < target)
        {
            _lblLineCount.Text = $"⚠️ {count} / {target} dòng (thiếu {target - count} dòng)";
            _lblLineCount.ForeColor = Color.FromArgb(251, 191, 36);
        }
        else
        {
            _lblLineCount.Text = $"ℹ️ {count} / {target} dòng (thừa {count - target} dòng)";
            _lblLineCount.ForeColor = Color.FromArgb(96, 165, 250);
        }
    }

    private List<string> GetLines()
    {
        var raw = _txtTitles.Text ?? string.Empty;
        var lines = raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        return lines;
    }

    private async Task GenerateAiTitlesAsync()
    {
        if (_isGeneratingAi) return;

        var aiConfigService = new AiConfigService();
        var config = aiConfigService.Load();
        if (string.IsNullOrWhiteSpace(config.ApiKey) || string.IsNullOrWhiteSpace(config.ApiEndpoint))
        {
            MessageBox.Show(this, "Chưa cấu hình API AI (Endpoint / API Key).\nVui lòng cấu hình API Key trong mục Cài đặt trước khi dùng tính năng này.", "Chưa có API AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lines = GetLines();
        while (lines.Count < _items.Count) lines.Add(string.Empty);

        _isGeneratingAi = true;
        _btnAiGenerate.Enabled = false;
        _btnAiGenerate.FillColor = Color.FromArgb(107, 114, 128);

        try
        {
            var aiService = new AiTitleService();
            for (int i = 0; i < _items.Count; i++)
            {
                var (videoPath, currentTitle) = _items[i];
                var sourceTitle = !string.IsNullOrWhiteSpace(lines[i]) ? lines[i] : (!string.IsNullOrWhiteSpace(currentTitle) ? currentTitle : Path.GetFileNameWithoutExtension(videoPath.Split(';').FirstOrDefault()?.Trim() ?? videoPath));

                _btnAiGenerate.Text = $"⏳ Đang tạo AI ({i + 1}/{_items.Count})...";

                try
                {
                    var generated = await aiService.GenerateTitleAsync(config, sourceTitle);
                    if (!string.IsNullOrWhiteSpace(generated))
                    {
                        lines[i] = generated.Trim();
                        _txtTitles.Text = string.Join(Environment.NewLine, lines);
                    }
                }
                catch { }
            }
        }
        finally
        {
            _isGeneratingAi = false;
            _btnAiGenerate.Enabled = true;
            _btnAiGenerate.Text = "✨ Tạo tiêu đề AI";
            _btnAiGenerate.FillColor = Color.FromArgb(147, 51, 234);
            UpdateLineCount();
        }
    }

    private void PasteFromClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText()) return;
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            var selStart = _txtTitles.SelectionStart;
            if (_txtTitles.SelectionLength > 0)
            {
                _txtTitles.SelectedText = text;
            }
            else if (string.IsNullOrWhiteSpace(_txtTitles.Text))
            {
                _txtTitles.Text = text;
            }
            else
            {
                _txtTitles.Paste();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Lỗi dán dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetToFileName()
    {
        var lines = _items.Select(i =>
        {
            var firstFile = i.VideoPath.Split(';').FirstOrDefault()?.Trim() ?? i.VideoPath;
            return !string.IsNullOrWhiteSpace(firstFile) ? Path.GetFileNameWithoutExtension(firstFile) : "";
        }).ToList();

        _txtTitles.Text = string.Join(Environment.NewLine, lines);
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
            BackColor = Color.FromArgb(32, 33, 36),
            ForeColor = Color.White
        };

        var lbl = new Label
        {
            Text = "Nhập nội dung cần thêm vào cuối mỗi dòng tiêu đề (ví dụ: #shopee #review):",
            AutoSize = false,
            Size = new Size(380, 36),
            Location = new Point(16, 14),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(220, 225, 235)
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
            FillColor = Color.FromArgb(16, 185, 129),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand
        };
        btnOk.Click += (_, _) => { prompt.DialogResult = DialogResult.OK; prompt.Close(); };

        prompt.Controls.AddRange([lbl, txt, btnOk]);
        prompt.AcceptButton = btnOk;

        if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
        {
            var suffix = txt.Text.Trim();
            var lines = GetLines();
            for (int i = 0; i < lines.Count; i++)
            {
                var cur = lines[i]?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(cur))
                    lines[i] = suffix;
                else if (!cur.Contains(suffix, StringComparison.OrdinalIgnoreCase))
                    lines[i] = $"{cur} {suffix}";
            }
            _txtTitles.Text = string.Join(Environment.NewLine, lines);
        }
    }

    private void ClearAll()
    {
        _txtTitles.Clear();
    }

    private void SaveAndApply()
    {
        var lines = GetLines();
        ResultTitles.Clear();

        for (int i = 0; i < _items.Count; i++)
        {
            var val = i < lines.Count ? lines[i].Trim() : string.Empty;
            ResultTitles.Add(val);
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
            Height = 32,
            FillColor = color,
            BorderRadius = 6,
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
    }
}
