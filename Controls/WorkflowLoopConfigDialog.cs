using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace ShopeeVideoUploader.Controls;

public sealed class WorkflowLoopConfigDialog : Form
{
    private readonly Guna2ComboBox _cboVariable;
    private readonly Guna2CheckBox _chkLimitMax;
    private readonly Guna2NumericUpDown _numMaxCount;
    private readonly Guna2CheckBox _chkSkipIfEmpty;
    private readonly Guna2Button _btnConfirm;
    private readonly Guna2Button _btnCancel;

    public string LoopVariable { get; private set; } = "ShopeeAffLink";
    public int LoopMaxCount { get; private set; } = 0;
    public bool LoopSkipIfEmpty { get; private set; } = true;

    public WorkflowLoopConfigDialog(
        int startStepNumber,
        int endStepNumber,
        int totalSteps,
        string currentVariable = "ShopeeAffLink",
        int currentMaxCount = 0,
        bool currentSkipIfEmpty = true,
        bool isEditing = false)
    {
        Text = isEditing ? "Chỉnh sửa cấu hình vòng lặp" : "Cấu hình vòng lặp theo biến";
        Size = new Size(540, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = Color.FromArgb(249, 250, 253),
            Padding = new Padding(20, 14, 20, 10)
        };
        pnlHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 235, 243));
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = isEditing ? "⚙️ Chỉnh sửa vòng lặp đoạn quy trình" : "🔁 Tạo vòng lặp lặp lại đoạn quy trình",
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(79, 70, 229),
            AutoSize = true,
            Location = new Point(18, 12)
        };

        var lblSubtitle = new Label
        {
            Text = $"Đoạn áp dụng: Từ Bước #{startStepNumber} đến Bước #{endStepNumber} ({totalSteps} bước)",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(20, 40)
        };

        pnlHeader.Controls.AddRange([lblTitle, lblSubtitle]);

        // Main Body Panel
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 16)
        };

        int curY = 16;

        // 1. Variable selector
        var lblVar = new Label
        {
            Text = "Biến lặp dữ liệu (Đoạn này sẽ lặp theo số lượng phần tử của biến):",
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(24, curY)
        };
        curY += 26;

        _cboVariable = new Guna2ComboBox
        {
            Location = new Point(24, curY),
            Size = new Size(472, 36),
            BorderRadius = 7,
            BorderColor = Color.FromArgb(203, 213, 225),
            FillColor = Color.White,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(15, 23, 42),
            DropDownStyle = ComboBoxStyle.DropDown
        };
        _cboVariable.Items.AddRange([
            "ShopeeAffLink",
            "VideoPath"
        ]);

        var matchedIdx = _cboVariable.FindStringExact(currentVariable);
        if (matchedIdx >= 0)
            _cboVariable.SelectedIndex = matchedIdx;
        else
            _cboVariable.Text = !string.IsNullOrWhiteSpace(currentVariable) ? currentVariable : "ShopeeAffLink";

        curY += 46;

        // Note about internal variable injection
        var lblNote = new Label
        {
            Text = "💡 Trong mỗi vòng lặp i: Biến {ShopeeAffLink} hoặc {CurrentAffLink} sẽ tự động nhận link thứ i. Đồng thời bạn có thể dùng biến {LoopIndex} (vòng thứ mấy) và {LoopTotal} (tổng số link).",
            Font = new Font("Segoe UI", 8.25F, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 116, 139),
            Size = new Size(472, 38),
            Location = new Point(24, curY)
        };
        curY += 48;

        // Separator
        var sep = new Panel
        {
            Location = new Point(24, curY),
            Size = new Size(472, 1),
            BackColor = Color.FromArgb(241, 245, 249)
        };
        curY += 14;

        // 2. Loop count options
        var lblCountHeader = new Label
        {
            Text = "Số lần lặp lại:",
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(24, curY)
        };
        curY += 26;

        var lblAutoCount = new Label
        {
            Text = "✔ Tự động lặp theo số lượng affiliate link có trong mỗi video",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(5, 150, 105),
            AutoSize = true,
            Location = new Point(24, curY)
        };
        curY += 28;

        _chkLimitMax = new Guna2CheckBox
        {
            Text = "Giới hạn tối đa số lần lặp:",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(51, 65, 85),
            Checked = currentMaxCount > 0,
            Location = new Point(24, curY + 4),
            AutoSize = true
        };

        _numMaxCount = new Guna2NumericUpDown
        {
            Location = new Point(210, curY),
            Size = new Size(90, 32),
            BorderRadius = 6,
            BorderColor = Color.FromArgb(203, 213, 225),
            Minimum = 1,
            Maximum = 9999,
            Value = currentMaxCount > 0 ? Math.Clamp(currentMaxCount, 1, 9999) : 5,
            Enabled = currentMaxCount > 0
        };

        _chkLimitMax.CheckedChanged += (_, _) =>
        {
            _numMaxCount.Enabled = _chkLimitMax.Checked;
        };

        curY += 46;

        // 3. Skip if empty
        _chkSkipIfEmpty = new Guna2CheckBox
        {
            Text = "Bỏ qua đoạn lặp này nếu video không có link (0 link tiếp thị)",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(51, 65, 85),
            Checked = currentSkipIfEmpty,
            Location = new Point(24, curY),
            AutoSize = true
        };
        curY += 34;

        pnlBody.Controls.AddRange([
            lblVar, _cboVariable, lblNote,
            sep,
            lblCountHeader, lblAutoCount,
            _chkLimitMax, _numMaxCount,
            _chkSkipIfEmpty
        ]);

        // Footer Buttons Panel
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(249, 250, 253),
            Padding = new Padding(20, 10, 20, 10)
        };
        pnlFooter.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 235, 243));
            e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };

        _btnConfirm = new Guna2Button
        {
            Text = isEditing ? "Lưu cấu hình" : "Tạo vòng lặp",
            Font = new Font("Segoe UI Semibold", 9.5F),
            FillColor = Color.FromArgb(79, 70, 229),
            ForeColor = Color.White,
            BorderRadius = 7,
            Size = new Size(130, 38),
            Location = new Point(pnlFooter.Width - 150, 11),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnConfirm.Click += (_, _) =>
        {
            var varName = _cboVariable.Text?.Trim() ?? "";
            if (varName.StartsWith("{") && varName.EndsWith("}"))
                varName = varName[1..^1];

            if (string.IsNullOrWhiteSpace(varName))
            {
                MessageBox.Show(this, "Vui lòng chọn hoặc nhập tên biến để lặp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoopVariable = varName;
            LoopMaxCount = _chkLimitMax.Checked ? (int)_numMaxCount.Value : 0;
            LoopSkipIfEmpty = _chkSkipIfEmpty.Checked;

            DialogResult = DialogResult.OK;
            Close();
        };

        _btnCancel = new Guna2Button
        {
            Text = "Hủy bỏ",
            Font = new Font("Segoe UI", 9F),
            FillColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            BorderRadius = 7,
            Size = new Size(90, 38),
            Location = new Point(pnlFooter.Width - 250, 11),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        pnlFooter.Controls.AddRange([_btnCancel, _btnConfirm]);

        Controls.Add(pnlBody);
        Controls.Add(pnlFooter);
        Controls.Add(pnlHeader);
    }
}
