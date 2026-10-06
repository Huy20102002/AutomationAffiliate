using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

/// <summary>Hộp thoại thêm/sửa Link AFF và ảnh đại diện.</summary>
public sealed class AffiliateLinkEditorDialog : Form
{
    private readonly Guna.UI2.WinForms.Guna2TextBox _txtName;
    private readonly Guna.UI2.WinForms.Guna2TextBox _txtUrl;
    private readonly Guna.UI2.WinForms.Guna2TextBox _txtNotes;
    private readonly PictureBox _preview;
    private readonly Label _lblImageName;
    private string _pendingImagePath;

    public AffiliateLinkItem Value { get; private set; }

    public AffiliateLinkEditorDialog(AffiliateLinkItem? source = null)
    {
        Value = source == null
            ? new AffiliateLinkItem()
            : new AffiliateLinkItem
            {
                Id = source.Id,
                Name = source.Name,
                Url = source.Url,
                ImagePath = source.ImagePath,
                ImageHash = source.ImageHash,
                Notes = source.Notes,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt,
                SourceKey = source.SourceKey
            };
        _pendingImagePath = Value.ImagePath;

        Text = source == null ? "Thêm Link AFF" : "Sửa Link AFF";
        ClientSize = new Size(720, 525);
        MinimumSize = new Size(640, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(244, 245, 248);
        Font = new Font("Segoe UI", 9.5F);

        var header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.White };
        header.Controls.Add(new Label
        {
            Text = source == null ? "THÊM LINK AFF" : "CHỈNH SỬA LINK AFF",
            Location = new Point(24, 14),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13F),
            ForeColor = Color.FromArgb(31, 31, 44)
        });
        header.Controls.Add(new Label
        {
            Text = "Gắn ảnh sản phẩm để nhận diện và tìm lại link nhanh hơn",
            Location = new Point(25, 41),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139)
        });

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 12) };
        var imageCard = new Panel
        {
            Location = new Point(24, 20),
            Size = new Size(230, 340),
            BackColor = Color.White,
            Padding = new Padding(14)
        };
        imageCard.Controls.Add(new Label
        {
            Text = "ẢNH ĐẠI DIỆN",
            Location = new Point(14, 13),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(79, 70, 229)
        });
        _preview = new PictureBox
        {
            Location = new Point(14, 42),
            Size = new Size(202, 202),
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        _lblImageName = new Label
        {
            Text = "Chưa chọn ảnh",
            Location = new Point(14, 250),
            Size = new Size(202, 36),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoEllipsis = true
        };
        var btnChooseImage = CreateButton("🖼  Chọn ảnh", Color.FromArgb(79, 70, 229), Color.White);
        btnChooseImage.Location = new Point(14, 292);
        btnChooseImage.Size = new Size(130, 34);
        btnChooseImage.Click += (_, _) => ChooseImage();
        var btnRemoveImage = CreateButton("Xóa", Color.FromArgb(254, 242, 242), Color.FromArgb(220, 38, 38));
        btnRemoveImage.Location = new Point(150, 292);
        btnRemoveImage.Size = new Size(66, 34);
        btnRemoveImage.Click += (_, _) => SetPreview(string.Empty);
        imageCard.Controls.AddRange([_preview, _lblImageName, btnChooseImage, btnRemoveImage]);

        var formCard = new Panel
        {
            Location = new Point(272, 20),
            Size = new Size(400, 340),
            BackColor = Color.White,
            Padding = new Padding(18)
        };
        formCard.Controls.Add(CreateLabel("Tên sản phẩm / gợi nhớ *", 18, 18));
        _txtName = CreateTextBox("Ví dụ: Áo chống nắng nữ", false);
        _txtName.Location = new Point(18, 43);
        _txtName.Size = new Size(364, 38);

        formCard.Controls.Add(CreateLabel("Link tiếp thị liên kết *", 18, 94));
        _txtUrl = CreateTextBox("Mỗi link một dòng hoặc ngăn cách bằng dấu phẩy", true);
        _txtUrl.Location = new Point(18, 119);
        _txtUrl.Size = new Size(364, 82);

        formCard.Controls.Add(CreateLabel("Ghi chú", 18, 211));
        _txtNotes = CreateTextBox("Mã sản phẩm, ngành hàng, chiến dịch...", true);
        _txtNotes.Location = new Point(18, 236);
        _txtNotes.Size = new Size(364, 81);
        formCard.Controls.AddRange([_txtName, _txtUrl, _txtNotes]);

        content.Controls.Add(imageCard);
        content.Controls.Add(formCard);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 76, BackColor = Color.White };
        var btnCancel = CreateButton("Hủy", Color.FromArgb(243, 244, 246), Color.FromArgb(71, 85, 105));
        btnCancel.Size = new Size(92, 38);
        btnCancel.Location = new Point(500, 18);
        btnCancel.DialogResult = DialogResult.Cancel;
        var btnSave = CreateButton("Lưu Link AFF", Color.FromArgb(79, 70, 229), Color.White);
        btnSave.Size = new Size(112, 38);
        btnSave.Location = new Point(600, 18);
        btnSave.Click += (_, _) => SaveValue();
        footer.Controls.AddRange([btnCancel, btnSave]);

        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(header);
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        _txtName.Text = Value.Name;
        _txtUrl.Text = Value.Url;
        _txtNotes.Text = Value.Notes;
        SetPreview(_pendingImagePath);
        Shown += (_, _) => _txtName.Focus();
    }

    private static Label CreateLabel(string text, int x, int y) => new()
    {
        Text = text,
        Location = new Point(x, y),
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 9F),
        ForeColor = Color.FromArgb(51, 65, 85)
    };

    private static Guna.UI2.WinForms.Guna2TextBox CreateTextBox(string placeholder, bool multiline) => new()
    {
        PlaceholderText = placeholder,
        BorderRadius = 7,
        BorderColor = Color.FromArgb(203, 213, 225),
        FillColor = Color.FromArgb(248, 250, 252),
        Font = new Font("Segoe UI", 9.5F),
        Multiline = multiline,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
    };

    private static Guna.UI2.WinForms.Guna2Button CreateButton(string text, Color fill, Color fore) => new()
    {
        Text = text,
        FillColor = fill,
        ForeColor = fore,
        BorderRadius = 7,
        Font = new Font("Segoe UI Semibold", 9F),
        Cursor = Cursors.Hand
    };

    private void ChooseImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh đại diện cho Link AFF",
            Filter = "Tệp ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|Tất cả tệp|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            SetPreview(dialog.FileName);
    }

    private void SetPreview(string path)
    {
        _preview.Image?.Dispose();
        _preview.Image = null;
        _pendingImagePath = path;
        _lblImageName.Text = string.IsNullOrWhiteSpace(path) ? "Chưa chọn ảnh" : Path.GetFileName(path);

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try { _preview.Image = ImageSimilarity.LoadUnlocked(path); }
            catch
            {
                _pendingImagePath = string.Empty;
                _lblImageName.Text = "Không đọc được ảnh";
            }
        }
    }

    private void SaveValue()
    {
        var name = _txtName.Text.Trim();
        var url = _txtUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Vui lòng nhập tên để dễ nhận diện link.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtName.Focus();
            return;
        }
        var urls = AffiliateLinkItem.ParseUrls(url);
        if (urls.Count == 0)
        {
            MessageBox.Show(this, "Vui lòng nhập ít nhất một Link AFF http:// hoặc https:// hợp lệ.", "Link chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtUrl.Focus();
            return;
        }

        try
        {
            var storedImagePath = string.Empty;
            var imageHash = string.Empty;
            if (!string.IsNullOrWhiteSpace(_pendingImagePath))
            {
                storedImagePath = string.Equals(_pendingImagePath, Value.ImagePath, StringComparison.OrdinalIgnoreCase)
                    ? _pendingImagePath
                    : ImageSimilarity.SaveToLibrary(_pendingImagePath);
                imageHash = ImageSimilarity.ComputeDifferenceHash(storedImagePath);
            }

            Value.Name = name;
            Value.Url = string.Join(Environment.NewLine, urls);
            Value.Notes = _txtNotes.Text.Trim();
            Value.ImagePath = storedImagePath;
            Value.ImageHash = imageHash;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Không thể lưu ảnh: {ex.Message}", "Lỗi ảnh", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _preview?.Image?.Dispose();
        base.Dispose(disposing);
    }
}
