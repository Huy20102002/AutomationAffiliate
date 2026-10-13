using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

/// <summary>
/// Hộp thoại chọn 1 dữ liệu video mẫu để chạy test một bước hoặc từ bước được chọn đến hết trong sơ đồ quy trình.
/// </summary>
public sealed class SelectTestJobDialog : Form
{
    public JobItem? SelectedJob { get; private set; }
    public string SelectedPlatform { get; private set; } = "Shopee";

    private readonly List<JobItem> _allJobs;
    private readonly Func<JobItem, int> _getDisplayStt;
    private readonly DataGridView _grid;
    private readonly Guna2TextBox _txtSearch;
    private readonly Label _lblSelectedInfo;
    private readonly Guna2RadioButton _rbShopee;
    private readonly Guna2RadioButton _rbFacebook;
    private readonly Guna2Button _btnRun;

    public SelectTestJobDialog(
        int startIndex,
        WorkflowStep step,
        int stepsCount,
        bool runOnlyThisStep,
        IReadOnlyList<JobItem> candidateJobs,
        JobItem? preselectedJob,
        Func<JobItem, int> getDisplayStt,
        string defaultPlatform = "Shopee")
    {
        _getDisplayStt = getDisplayStt;

        if (candidateJobs != null && candidateJobs.Count > 0)
        {
            _allJobs = candidateJobs.ToList();
        }
        else
        {
            _allJobs =
            [
                new JobItem
                {
                    Id = 1,
                    Title = "Dữ liệu mẫu giả lập",
                    VideoPath = "video_sample.mp4",
                    ShopeeAffLink = "https://shopee.vn/sample"
                }
            ];
        }

        Text = runOnlyThisStep
            ? $"⚡ Chạy thử riêng Bước #{startIndex + 1}"
            : $"▶ Chạy thử từ Bước #{startIndex + 1} đến hết";
        ClientSize = new Size(820, 540);
        MinimumSize = new Size(720, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        // ── TOP PANEL ──
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 106,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(18, 12, 18, 10)
        };

        var lblTitle = new Label
        {
            Text = runOnlyThisStep
                ? $"⚡ Chạy thử riêng Bước #{startIndex + 1}: {step.GetDisplayText()}"
                : $"▶ Chạy thử từ Bước #{startIndex + 1} đến hết ({stepsCount} bước)",
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Color.FromArgb(31, 31, 44),
            AutoSize = true,
            Location = new Point(16, 10)
        };

        var lblDesc = new Label
        {
            Text = "Chọn 1 video mẫu từ danh sách dưới đây để truyền dữ liệu {Title}, {ShopeeAffLink}, {VideoPath}... cho bước thực thi.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(107, 114, 128),
            AutoSize = true,
            Location = new Point(18, 36)
        };

        var platformPanel = new FlowLayoutPanel
        {
            Location = new Point(18, 64),
            Size = new Size(240, 32),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var lblPlat = new Label
        {
            Text = "Nền tảng:",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(55, 65, 81),
            AutoSize = true,
            Padding = new Padding(0, 4, 6, 0)
        };

        _rbShopee = new Guna2RadioButton
        {
            Text = "Shopee",
            Checked = !defaultPlatform.Equals("Facebook", StringComparison.OrdinalIgnoreCase),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            CheckedState = { FillColor = Color.FromArgb(238, 77, 45) }
        };

        _rbFacebook = new Guna2RadioButton
        {
            Text = "Facebook",
            Checked = defaultPlatform.Equals("Facebook", StringComparison.OrdinalIgnoreCase),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            CheckedState = { FillColor = Color.FromArgb(24, 119, 242) },
            Margin = new Padding(12, 0, 0, 0)
        };

        platformPanel.Controls.AddRange([lblPlat, _rbShopee, _rbFacebook]);

        _txtSearch = new Guna2TextBox
        {
            PlaceholderText = "🔍 Tìm kiếm theo tên file video, tiêu đề, liên kết tiếp thị...",
            Font = new Font("Segoe UI", 9F),
            Location = new Point(270, 60),
            Size = new Size(530, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BorderRadius = 6,
            BorderColor = Color.FromArgb(209, 213, 219),
            TextOffset = new Point(4, 0)
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();

        topPanel.Controls.AddRange([lblTitle, lblDesc, platformPanel, _txtSearch]);

        // ── BOTTOM PANEL ──
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(16, 10, 16, 10)
        };

        _lblSelectedInfo = new Label
        {
            Text = "Chưa chọn video mẫu",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(79, 70, 229),
            AutoSize = true,
            Location = new Point(16, 18),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        var btnCancel = new Guna2Button
        {
            Text = "Hủy bỏ",
            DialogResult = DialogResult.Cancel,
            Size = new Size(95, 36),
            Location = new Point(530, 10),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            FillColor = Color.FromArgb(243, 244, 246),
            ForeColor = Color.FromArgb(55, 65, 81),
            BorderColor = Color.FromArgb(209, 213, 219),
            BorderThickness = 1,
            BorderRadius = 6,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9F)
        };

        _btnRun = new Guna2Button
        {
            Text = runOnlyThisStep ? "⚡ Bắt đầu chạy test" : "▶ Bắt đầu chạy test",
            DialogResult = DialogResult.OK,
            Size = new Size(165, 36),
            Location = new Point(635, 10),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            FillColor = Color.FromArgb(96, 82, 218),
            ForeColor = Color.White,
            BorderRadius = 6,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9.5F)
        };
        _btnRun.Click += (_, _) => ConfirmSelection();

        bottomPanel.Controls.AddRange([_lblSelectedInfo, btnCancel, _btnRun]);

        // ── GRID ──
        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 8, 16, 4),
            BackColor = Color.White
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            GridColor = Color.FromArgb(240, 242, 245),
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 36 },
            ColumnHeadersHeight = 36,
            Font = new Font("Segoe UI", 9F)
        };

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(244, 245, 248),
            ForeColor = Color.FromArgb(96, 82, 218),
            Font = new Font("Segoe UI Semibold", 9F),
            Padding = new Padding(8, 4, 8, 4)
        };

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 31, 44),
            SelectionBackColor = Color.FromArgb(225, 240, 252),
            SelectionForeColor = Color.FromArgb(31, 31, 44),
            Padding = new Padding(6, 2, 6, 2)
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colStt",
            HeaderText = "STT",
            Width = 55,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colVideo",
            HeaderText = "Tên video / File",
            Width = 190,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTitle",
            HeaderText = "Tiêu đề video",
            Width = 260,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLink",
            HeaderText = "Liên kết tiếp thị",
            Width = 180,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colStatus",
            HeaderText = "Trạng thái",
            Width = 100,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.SelectionChanged += (_, _) => UpdateSelectionInfo();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                ConfirmSelection();
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        gridHost.Controls.Add(_grid);

        Controls.Add(gridHost);
        Controls.Add(bottomPanel);
        Controls.Add(topPanel);

        AcceptButton = _btnRun;
        CancelButton = btnCancel;

        PopulateGrid(preselectedJob);
    }

    private void PopulateGrid(JobItem? targetSelect)
    {
        _grid.Rows.Clear();
        int targetRowIndex = -1;

        foreach (var job in _allJobs)
        {
            int stt = _getDisplayStt(job);
            string fileName = string.IsNullOrWhiteSpace(job.VideoPath)
                ? string.Empty
                : Path.GetFileName(job.VideoPath);

            int rowIndex = _grid.Rows.Add(
                stt > 0 ? stt : _grid.Rows.Count + 1,
                fileName,
                job.Title,
                job.ShopeeAffLink,
                job.Status
            );

            _grid.Rows[rowIndex].Tag = job;
            _grid.Rows[rowIndex].Cells["colVideo"].ToolTipText = job.VideoPath;

            if (targetSelect != null && (job.Id == targetSelect.Id || ReferenceEquals(job, targetSelect)))
            {
                targetRowIndex = rowIndex;
            }
        }

        if (targetRowIndex >= 0 && targetRowIndex < _grid.Rows.Count)
        {
            _grid.ClearSelection();
            _grid.Rows[targetRowIndex].Selected = true;
            _grid.FirstDisplayedScrollingRowIndex = targetRowIndex;
        }
        else if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
        }

        UpdateSelectionInfo();
    }

    private void ApplyFilter()
    {
        string query = _txtSearch.Text?.Trim() ?? string.Empty;
        var currentSelected = GetCurrentSelectedJob();

        _grid.Rows.Clear();
        int targetRowIndex = -1;

        foreach (var job in _allJobs)
        {
            if (!string.IsNullOrEmpty(query))
            {
                bool matchName = !string.IsNullOrEmpty(job.VideoPath) &&
                                 Path.GetFileName(job.VideoPath).Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchTitle = !string.IsNullOrEmpty(job.Title) &&
                                  job.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchLink = !string.IsNullOrEmpty(job.ShopeeAffLink) &&
                                 job.ShopeeAffLink.Contains(query, StringComparison.OrdinalIgnoreCase);

                if (!matchName && !matchTitle && !matchLink) continue;
            }

            int stt = _getDisplayStt(job);
            string fileName = string.IsNullOrWhiteSpace(job.VideoPath)
                ? string.Empty
                : Path.GetFileName(job.VideoPath);

            int rowIndex = _grid.Rows.Add(
                stt > 0 ? stt : _grid.Rows.Count + 1,
                fileName,
                job.Title,
                job.ShopeeAffLink,
                job.Status
            );

            _grid.Rows[rowIndex].Tag = job;
            _grid.Rows[rowIndex].Cells["colVideo"].ToolTipText = job.VideoPath;

            if (currentSelected != null && (job.Id == currentSelected.Id || ReferenceEquals(job, currentSelected)))
            {
                targetRowIndex = rowIndex;
            }
        }

        if (targetRowIndex >= 0 && targetRowIndex < _grid.Rows.Count)
        {
            _grid.ClearSelection();
            _grid.Rows[targetRowIndex].Selected = true;
        }
        else if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
        }

        UpdateSelectionInfo();
    }

    private JobItem? GetCurrentSelectedJob()
    {
        if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is JobItem job)
        {
            return job;
        }
        return null;
    }

    private void UpdateSelectionInfo()
    {
        var job = GetCurrentSelectedJob();
        if (job != null)
        {
            int stt = _getDisplayStt(job);
            string displayTitle = !string.IsNullOrWhiteSpace(job.Title)
                ? job.Title
                : Path.GetFileName(job.VideoPath);
            if (displayTitle.Length > 45) displayTitle = displayTitle[..42] + "...";

            _lblSelectedInfo.Text = $"🎯 Đã chọn: [STT #{stt}] {displayTitle}";
            _btnRun.Enabled = true;
        }
        else
        {
            _lblSelectedInfo.Text = "Chưa chọn video mẫu nào";
            _btnRun.Enabled = false;
        }
    }

    private void ConfirmSelection()
    {
        SelectedJob = GetCurrentSelectedJob() ?? _allJobs.FirstOrDefault();
        SelectedPlatform = _rbFacebook.Checked ? "Facebook" : "Shopee";
    }
}
