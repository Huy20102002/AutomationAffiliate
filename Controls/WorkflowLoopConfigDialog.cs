using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace ShopeeVideoUploader.Controls;

public sealed class WorkflowLoopConfigDialog : Form
{
    private readonly Guna2NumericUpDown _numStartStep;
    private readonly Guna2NumericUpDown _numEndStep;
    private readonly Label _lblStepRangeSummary;
    private readonly Guna2ComboBox _cboVariable;
    private readonly Guna2CheckBox _chkLimitMax;
    private readonly Guna2NumericUpDown _numMaxCount;
    private readonly Guna2CheckBox _chkSkipIfEmpty;
    private readonly Guna2Button _btnConfirm;
    private readonly Guna2Button _btnTestNow;
    private readonly Guna2Button _btnCancel;

    public int StartStepNumber => (int)_numStartStep.Value;
    public int EndStepNumber => (int)_numEndStep.Value;
    public string LoopVariable { get; private set; } = "ShopeeAffLink";
    public int LoopMaxCount { get; private set; } = 0;
    public bool LoopSkipIfEmpty { get; private set; } = true;
    public bool RequestTestRunImmediately { get; private set; } = false;

    public WorkflowLoopConfigDialog(
        int startStepNumber,
        int endStepNumber,
        int totalStepsCount,
        string currentVariable = "ShopeeAffLink",
        int currentMaxCount = 0,
        bool currentSkipIfEmpty = true,
        bool isEditing = false)
    {
        Text = isEditing ? "Chỉnh sửa cấu hình vòng lặp" : "Cấu hình vòng lặp theo biến";
        Size = new Size(570, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        int maxStepLimit = Math.Max(1, totalStepsCount);
        int initialStart = Math.Clamp(startStepNumber, 1, maxStepLimit);
        int initialEnd = Math.Clamp(endStepNumber, initialStart, maxStepLimit);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Color.FromArgb(249, 250, 253),
            Padding = new Padding(20, 12, 20, 10)
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
            Text = "Cấu hình lặp lại một đoạn bước tự động theo số lượng affiliate link trong mỗi video",
            Font = new Font("Segoe UI", 8.75F),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(20, 40)
        };

        pnlHeader.Controls.AddRange([lblTitle, lblSubtitle]);

        // Main Body Panel
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 14),
            AutoScroll = true
        };

        int curY = 14;

        // 1. Phạm vi các bước áp dụng vòng lặp (Từ bước ... Đến bước ...)
        var lblRangeHeader = new Label
        {
            Text = "Phạm vi các bước lặp (Bạn có thể tự nhập tay số thứ tự bước):",
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(24, curY)
        };
        curY += 26;

        var lblFrom = new Label
        {
            Text = "Từ bước:",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true,
            Location = new Point(24, curY + 6)
        };

        _numStartStep = new Guna2NumericUpDown
        {
            Location = new Point(88, curY),
            Size = new Size(82, 32),
            BorderRadius = 6,
            BorderColor = Color.FromArgb(203, 213, 225),
            Minimum = 1,
            Maximum = maxStepLimit,
            Value = initialStart
        };

        var lblTo = new Label
        {
            Text = "Đến bước:",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true,
            Location = new Point(188, curY + 6)
        };

        _numEndStep = new Guna2NumericUpDown
        {
            Location = new Point(258, curY),
            Size = new Size(82, 32),
            BorderRadius = 6,
            BorderColor = Color.FromArgb(203, 213, 225),
            Minimum = 1,
            Maximum = maxStepLimit,
            Value = initialEnd
        };

        _lblStepRangeSummary = new Label
        {
            Text = $"({Math.Max(0, initialEnd - initialStart + 1)} bước)",
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(79, 70, 229),
            AutoSize = true,
            Location = new Point(356, curY + 6)
        };

        void UpdateRangeSummary()
        {
            int s = (int)_numStartStep.Value;
            int e = (int)_numEndStep.Value;
            int count = e >= s ? e - s + 1 : 0;
            _lblStepRangeSummary.Text = count > 0 ? $"({count} bước được lặp)" : "(Khoảng bước không hợp lệ)";
            _lblStepRangeSummary.ForeColor = count > 0 ? Color.FromArgb(79, 70, 229) : Color.FromArgb(220, 38, 38);
        }

        _numStartStep.ValueChanged += (_, _) => UpdateRangeSummary();
        _numEndStep.ValueChanged += (_, _) => UpdateRangeSummary();

        curY += 44;

        // Separator 1
        var sep1 = new Panel
        {
            Location = new Point(24, curY),
            Size = new Size(495, 1),
            BackColor = Color.FromArgb(241, 245, 249)
        };
        curY += 12;

        // 2. Biến lặp dữ liệu
        var lblVar = new Label
        {
            Text = "Biến lặp dữ liệu (Đoạn này sẽ lặp theo số lượng link của biến):",
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(24, curY)
        };
        curY += 26;

        _cboVariable = new Guna2ComboBox
        {
            Location = new Point(24, curY),
            Size = new Size(495, 36),
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

        curY += 44;

        // Note about internal variable injection
        var lblNote = new Label
        {
            Text = "💡 Trong mỗi vòng lặp i: Biến {ShopeeAffLink} hoặc {CurrentAffLink} sẽ tự động nhận link thứ i. Đồng thời bạn có thể dùng biến {LoopIndex} (vòng thứ mấy) và {LoopTotal} (tổng số link).",
            Font = new Font("Segoe UI", 8.25F, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 116, 139),
            Size = new Size(495, 34),
            Location = new Point(24, curY)
        };
        curY += 40;

        // Separator 2
        var sep2 = new Panel
        {
            Location = new Point(24, curY),
            Size = new Size(495, 1),
            BackColor = Color.FromArgb(241, 245, 249)
        };
        curY += 12;

        // 3. Số lần lặp lại
        var lblCountHeader = new Label
        {
            Text = "Số lần lặp lại:",
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(24, curY)
        };
        curY += 24;

        var lblAutoCount = new Label
        {
            Text = "✔ Tự động lặp theo số lượng affiliate link có trong mỗi video",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(5, 150, 105),
            AutoSize = true,
            Location = new Point(24, curY)
        };
        curY += 26;

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

        curY += 42;

        // 4. Skip if empty
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
            lblRangeHeader, lblFrom, _numStartStep, lblTo, _numEndStep, _lblStepRangeSummary,
            sep1,
            lblVar, _cboVariable, lblNote,
            sep2,
            lblCountHeader, lblAutoCount,
            _chkLimitMax, _numMaxCount,
            _chkSkipIfEmpty
        ]);

        // Footer Buttons Panel
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            BackColor = Color.FromArgb(249, 250, 253),
            Padding = new Padding(16, 11, 16, 11)
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
            Size = new Size(125, 38),
            Location = new Point(pnlFooter.Width - 142, 11),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnConfirm.Click += (_, _) =>
        {
            if (!ValidateInputs()) return;
            RequestTestRunImmediately = false;
            DialogResult = DialogResult.OK;
            Close();
        };

        _btnTestNow = new Guna2Button
        {
            Text = "▶ Lưu & Test ngay",
            Font = new Font("Segoe UI Semibold", 9F),
            FillColor = Color.FromArgb(243, 232, 255),
            ForeColor = Color.FromArgb(126, 34, 206),
            BorderColor = Color.FromArgb(216, 180, 254),
            BorderThickness = 1,
            BorderRadius = 7,
            Size = new Size(130, 38),
            Location = new Point(pnlFooter.Width - 280, 11),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnTestNow.Click += (_, _) =>
        {
            if (!ValidateInputs()) return;
            RequestTestRunImmediately = true;
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
            Size = new Size(82, 38),
            Location = new Point(pnlFooter.Width - 370, 11),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        pnlFooter.Controls.AddRange([_btnCancel, _btnTestNow, _btnConfirm]);

        Controls.Add(pnlBody);
        Controls.Add(pnlFooter);
        Controls.Add(pnlHeader);
    }

    private bool ValidateInputs()
    {
        if (_numStartStep.Value > _numEndStep.Value)
        {
            MessageBox.Show(this, "Bước bắt đầu không được lớn hơn bước kết thúc. Vui lòng kiểm tra lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var varName = _cboVariable.Text?.Trim() ?? "";
        if (varName.StartsWith("{") && varName.EndsWith("}"))
            varName = varName[1..^1];

        if (string.IsNullOrWhiteSpace(varName))
        {
            MessageBox.Show(this, "Vui lòng chọn hoặc nhập tên biến để lặp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        LoopVariable = varName;
        LoopMaxCount = _chkLimitMax.Checked ? (int)_numMaxCount.Value : 0;
        LoopSkipIfEmpty = _chkSkipIfEmpty.Checked;
        return true;
    }
}
