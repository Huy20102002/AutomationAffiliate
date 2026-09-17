using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Helpers;

namespace ShopeeVideoUploader.Controls;

public class UiInspectorDialog : Form
{
    private List<UiElement> _allElements;
    private List<UiElement> _filteredElements = new();
    private TextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private TextBox _txtSelectedXPath = null!;
    private Button _btnSelect = null!;
    private Button _btnTestClick = null!;
    private Button _btnRescan = null!;
    private Label _lblTestStatus = null!;
    private CheckBox _chkHideEmptyLayouts = null!;

    public string SelectedXPath { get; private set; } = string.Empty;

    /// <summary>
    /// Callback thực thi Click thử element trên thiết bị.
    /// Trả về (success, message).
    /// </summary>
    public Func<UiElement, string, Task<(bool success, string message)>>? OnTestClick { get; set; }

    /// <summary>
    /// Callback quét lại màn hình hiện tại trên thiết bị.
    /// </summary>
    public Func<Task<IReadOnlyList<UiElement>?>>? OnRescan { get; set; }

    public UiInspectorDialog(IEnumerable<UiElement> elements)
    {
        _allElements = elements.ToList();
        BuildUi();
        ApplyFilter();
    }

    private void BuildUi()
    {
        Text = "Quét UI Điện Thoại";
        Size = new Size(1000, 640);
        MinimumSize = new Size(760, 480);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(250, 252, 254);
        Font = new Font("Segoe UI", 9F);

        // Top Panel: Tìm kiếm + Lọc + Quét lại
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(12, 10, 12, 8), BackColor = Color.FromArgb(250, 252, 254) };
        
        _btnRescan = new Button
        {
            Text = "🔄 Quét lại",
            Dock = DockStyle.Right,
            Width = 105,
            Height = 30,
            BackColor = Color.FromArgb(240, 244, 248),
            ForeColor = Color.FromArgb(33, 43, 54),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnRescan.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 220);
        _btnRescan.Click += async (_, _) => await RescanAsync();

        var topFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false
        };
        var lblSearch = new Label { Text = "Tìm kiếm:", AutoSize = true, Margin = new Padding(0, 6, 6, 0) };
        _txtSearch = new TextBox { Width = 260, PlaceholderText = "Nhập text, resource-id hoặc class...", Margin = new Padding(0, 2, 14, 0) };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();
        
        _chkHideEmptyLayouts = new CheckBox 
        { 
            Text = "Ẩn các Layout/Container trống (để dễ tìm icon/nút hơn)", 
            AutoSize = true, 
            Checked = true,
            Margin = new Padding(0, 4, 0, 0)
        };
        _chkHideEmptyLayouts.CheckedChanged += (_, _) => ApplyFilter();

        topFlow.Controls.Add(lblSearch);
        topFlow.Controls.Add(_txtSearch);
        topFlow.Controls.Add(_chkHideEmptyLayouts);

        topPanel.Controls.Add(topFlow);
        topPanel.Controls.Add(_btnRescan);

        // DataGridView
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Phân cấp", DataPropertyName = "DepthIndent", Width = 55 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Class", DataPropertyName = "ClassName", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Text", DataPropertyName = "Text", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mô tả", DataPropertyName = "ContentDesc", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "ResourceId", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bounds", DataPropertyName = "Bounds", Width = 125 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "XPath", DataPropertyName = "XPath", Visible = false });
        
        _grid.SelectionChanged += Grid_SelectionChanged;
        _grid.CellDoubleClick += (_, _) => { if (_btnSelect.Enabled) SelectAndClose(); };

        // Bottom Panel: XPath + Test Click + Chọn Element
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 108, BackColor = Color.White };
        var borderTop = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 235, 240) };

        var bottomRightPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 275,
            Padding = new Padding(0, 24, 12, 0)
        };

        _btnTestClick = new Button
        {
            Text = "🎯 Click thử",
            Location = new Point(4, 24),
            Width = 125,
            Height = 36,
            BackColor = Color.FromArgb(0, 161, 112),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _btnTestClick.FlatAppearance.BorderSize = 0;
        _btnTestClick.Click += BtnTestClick_Click;

        _btnSelect = new Button
        {
            Text = "✓ Chọn Element",
            Location = new Point(138, 24),
            Width = 125,
            Height = 36,
            BackColor = Color.FromArgb(35, 149, 218),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _btnSelect.FlatAppearance.BorderSize = 0;
        _btnSelect.Click += (_, _) => SelectAndClose();

        bottomRightPanel.Controls.Add(_btnTestClick);
        bottomRightPanel.Controls.Add(_btnSelect);

        var bottomLeftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 6, 8, 6)
        };

        var lblXPath = new Label 
        { 
            Text = "XPath được tạo tự động:", 
            Location = new Point(14, 8), 
            AutoSize = true, 
            Font = new Font("Segoe UI Semibold", 9F) 
        };

        _txtSelectedXPath = new TextBox 
        { 
            Location = new Point(14, 30), 
            Height = 28, 
            Width = 600,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true, 
            BackColor = Color.FromArgb(245, 247, 250),
            Font = new Font("Segoe UI", 9F)
        };

        _lblTestStatus = new Label
        {
            Location = new Point(14, 66),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(120, 140, 160),
            Text = "Chọn một element trong bảng để xem thông tin và kiểm tra click thử trên điện thoại."
        };

        bottomLeftPanel.Controls.Add(lblXPath);
        bottomLeftPanel.Controls.Add(_txtSelectedXPath);
        bottomLeftPanel.Controls.Add(_lblTestStatus);

        bottomPanel.Controls.Add(bottomLeftPanel);
        bottomPanel.Controls.Add(bottomRightPanel);
        bottomPanel.Controls.Add(borderTop);

        Controls.Add(_grid);
        Controls.Add(topPanel);
        Controls.Add(bottomPanel);
        _grid.BringToFront();
    }

    private void ApplyFilter()
    {
        var query = _txtSearch.Text.Trim().ToLowerInvariant();
        var hideEmpty = _chkHideEmptyLayouts.Checked;
        
        _filteredElements = _allElements.Where(e => 
        {
            if (hideEmpty && 
                string.IsNullOrWhiteSpace(e.Text) && 
                string.IsNullOrWhiteSpace(e.ContentDesc) && 
                string.IsNullOrWhiteSpace(e.ResourceId) &&
                !e.IsClickable &&
                !e.ClassName.Contains("Image", StringComparison.OrdinalIgnoreCase) &&
                !e.ClassName.Contains("Button", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(query) ||
                e.Text.ToLowerInvariant().Contains(query) ||
                e.ContentDesc.ToLowerInvariant().Contains(query) ||
                e.ResourceId.ToLowerInvariant().Contains(query) ||
                e.ClassName.ToLowerInvariant().Contains(query);
        }).ToList();

        var displayList = _filteredElements.Select(e => new
        {
            DepthIndent = new string(' ', e.Depth * 2) + "└",
            e.ClassName,
            e.Text,
            e.ContentDesc,
            e.ResourceId,
            e.Bounds,
            e.XPath
        }).ToList();

        _grid.DataSource = displayList;

        if (_grid.Rows.Count > 0)
        {
            Grid_SelectionChanged(null, EventArgs.Empty);
        }
    }

    private UiElement? GetSelectedElement()
    {
        if (_grid.SelectedRows.Count > 0)
        {
            var index = _grid.SelectedRows[0].Index;
            if (index >= 0 && index < _filteredElements.Count)
                return _filteredElements[index];
        }
        return null;
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        var element = GetSelectedElement();
        if (element != null)
        {
            _txtSelectedXPath.Text = element.XPath;
            _btnSelect.Enabled = !string.IsNullOrWhiteSpace(_txtSelectedXPath.Text);
            
            var center = element.GetCenterPoint();
            _btnTestClick.Enabled = center != null;
            if (center != null)
            {
                _lblTestStatus.Text = $"Tọa độ tâm: ({center.Value.X}, {center.Value.Y}) | Vùng Bounds: {element.Bounds}";
                _lblTestStatus.ForeColor = Color.FromArgb(83, 111, 140);
            }
            else
            {
                _lblTestStatus.Text = "Element không có thuộc tính Bounds hợp lệ.";
                _lblTestStatus.ForeColor = Color.OrangeRed;
            }
        }
        else
        {
            _txtSelectedXPath.Text = string.Empty;
            _btnSelect.Enabled = false;
            _btnTestClick.Enabled = false;
            _lblTestStatus.Text = "Chọn một element trong bảng để xem thông tin và kiểm tra click thử.";
            _lblTestStatus.ForeColor = Color.FromArgb(120, 140, 160);
        }
    }

    private async void BtnTestClick_Click(object? sender, EventArgs e)
    {
        var element = GetSelectedElement();
        if (element == null) return;

        var center = element.GetCenterPoint();
        if (center == null)
        {
            _lblTestStatus.Text = "Element không có tọa độ hợp lệ để click.";
            _lblTestStatus.ForeColor = Color.Red;
            return;
        }

        if (OnTestClick == null)
        {
            _lblTestStatus.Text = "Chưa kết nối thiết bị để thực hiện click thử.";
            _lblTestStatus.ForeColor = Color.Red;
            return;
        }

        _btnTestClick.Enabled = false;
        _lblTestStatus.Text = $"Đang click thử tại ({center.Value.X}, {center.Value.Y}) trên điện thoại...";
        _lblTestStatus.ForeColor = Color.FromArgb(0, 120, 215);

        try
        {
            var result = await OnTestClick(element, _txtSelectedXPath.Text);
            _lblTestStatus.Text = result.message;
            _lblTestStatus.ForeColor = result.success ? Color.FromArgb(0, 150, 80) : Color.Red;
        }
        catch (Exception ex)
        {
            _lblTestStatus.Text = $"Lỗi khi click thử: {ex.Message}";
            _lblTestStatus.ForeColor = Color.Red;
        }
        finally
        {
            _btnTestClick.Enabled = true;
        }
    }

    private async Task RescanAsync()
    {
        if (OnRescan == null) return;
        try
        {
            _btnRescan.Enabled = false;
            _btnRescan.Text = "Đang quét...";
            _lblTestStatus.Text = "Đang quét lại UI từ thiết bị...";
            _lblTestStatus.ForeColor = Color.FromArgb(0, 120, 215);

            var newElements = await OnRescan();
            if (newElements != null && newElements.Count > 0)
            {
                _allElements = newElements.ToList();
                ApplyFilter();
                _lblTestStatus.Text = $"✓ Đã quét lại UI: tìm thấy {_allElements.Count} elements.";
                _lblTestStatus.ForeColor = Color.FromArgb(0, 150, 80);
            }
            else
            {
                _lblTestStatus.Text = "Không tìm thấy UI element nào trên màn hình thiết bị.";
                _lblTestStatus.ForeColor = Color.OrangeRed;
            }
        }
        catch (Exception ex)
        {
            _lblTestStatus.Text = $"Lỗi khi quét lại: {ex.Message}";
            _lblTestStatus.ForeColor = Color.Red;
        }
        finally
        {
            _btnRescan.Enabled = true;
            _btnRescan.Text = "🔄 Quét lại";
        }
    }

    private void SelectAndClose()
    {
        SelectedXPath = _txtSelectedXPath.Text;
        DialogResult = DialogResult.OK;
        Close();
    }
}
