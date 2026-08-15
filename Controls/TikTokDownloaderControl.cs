using System.IO;
using System.Text.Json;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader.Controls;

public sealed class TikTokDownloaderControl : UserControl
{
    private TikTokDownloaderService? _service;
    private readonly TextBox _channelTextBox = new();
    private readonly TextBox _proxyTextBox = new();
    private readonly TextBox _outputTextBox = new();
    private readonly DataGridView _grid = new();
    private readonly Label _summaryLabel = new();
    private readonly Button _scanButton = new();
    private readonly Button _downloadButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _selectAllButton = new();
    private readonly Button _clearSelectionButton = new();
    private readonly Button _browseOutputButton = new();
    private readonly List<TikTokVideoItem> _videos = [];
    private CancellationTokenSource? _operationCts;
    private bool _isHeaderCheckBoxChecked = false;

    public event EventHandler<string>? StatusChanged;

    public TikTokDownloaderControl()
    {
        BackColor = Color.FromArgb(244, 245, 248);
        Dock = DockStyle.Fill;
        BuildUi();
        LoadConfig();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14, 12, 14, 0),
            BackColor = Color.FromArgb(244, 245, 248)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildSettings(), 0, 1);
        root.Controls.Add(BuildToolbar(), 0, 2);
        root.Controls.Add(BuildGrid(), 0, 3);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 10, 16, 8) };
        panel.Controls.Add(new Label
        {
            Text = "TẢI VIDEO TIKTOK",
            AutoSize = true,
            Location = new Point(16, 10),
            Font = new Font("Segoe UI Semibold", 12F),
            ForeColor = Color.FromArgb(31, 31, 44)
        });
        panel.Controls.Add(new Label
        {
            Text = "Quét video theo kênh, chọn danh sách cần tải và lưu về máy",
            AutoSize = true,
            Location = new Point(17, 38),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(114, 117, 134)
        });
        return panel;
    }

    private Control BuildSettings()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var i = 0; i < 3; i++) panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

        _channelTextBox.PlaceholderText = "@username hoặc URL kênh TikTok";
        _proxyTextBox.PlaceholderText = "http://user:pass@host:port hoặc socks5://host:port";
        _outputTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "FlowPilot", "TikTok");
        _browseOutputButton.Text = "Chọn thư mục";
        _browseOutputButton.AutoSize = true;
        _browseOutputButton.Click += BrowseOutput;

        AddLabel(panel, "ID / username kênh", 0, 0);
        panel.Controls.Add(_channelTextBox, 1, 0);
        AddLabel(panel, "Proxy tùy chọn", 2, 0);
        panel.Controls.Add(_proxyTextBox, 3, 0);
        AddLabel(panel, "Thư mục lưu", 0, 1);
        panel.Controls.Add(_outputTextBox, 1, 1);
        panel.Controls.Add(_browseOutputButton, 2, 1);
        panel.SetColumnSpan(_browseOutputButton, 2);
        var noteLabel = new Label
        {
            Text = "Proxy chỉ dùng cho phiên quét/tải hiện tại. Hãy dùng nguồn proxy hợp lệ và tuân thủ điều khoản dịch vụ.",
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(114, 117, 134),
            Font = new Font("Segoe UI", 7.5F)
        };
        panel.Controls.Add(noteLabel, 0, 2);
        panel.SetColumnSpan(noteLabel, 4);
        return panel;
    }

    private static void AddLabel(TableLayoutPanel panel, string text, int column, int row)
    {
        panel.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(83, 111, 140),
            Font = new Font("Segoe UI", 8.5F)
        }, column, row);
    }

    private Control BuildToolbar()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 5, 0, 4) };
        ConfigureButton(_scanButton, "Quét danh sách", Color.FromArgb(96, 82, 218), 120);
        ConfigureButton(_selectAllButton, "Chọn tất cả", Color.FromArgb(235, 237, 242), 100);
        ConfigureButton(_clearSelectionButton, "Bỏ chọn", Color.FromArgb(235, 237, 242), 88);
        ConfigureButton(_downloadButton, "Tải video đã chọn", Color.FromArgb(0, 161, 112), 135);
        ConfigureButton(_cancelButton, "Dừng", Color.FromArgb(238, 225, 228), 70);
        _cancelButton.Enabled = false;
        _scanButton.Click += async (_, _) => await ScanAsync();
        _selectAllButton.Click += (_, _) => SetAllSelected(true);
        _clearSelectionButton.Click += (_, _) => SetAllSelected(false);
        _downloadButton.Click += async (_, _) => await DownloadAsync();
        _cancelButton.Click += (_, _) => _operationCts?.Cancel();
        _summaryLabel.AutoSize = true;
        _summaryLabel.Margin = new Padding(12, 9, 0, 0);
        _summaryLabel.ForeColor = Color.FromArgb(96, 82, 218);
        _summaryLabel.Text = "Chưa có danh sách video";
        panel.Controls.AddRange([_scanButton, _selectAllButton, _clearSelectionButton, _downloadButton, _cancelButton, _summaryLabel]);
        return panel;
    }

    private Control BuildGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.ColumnHeadersHeight = 38;
        _grid.RowTemplate.Height = 34;
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(244, 245, 248),
            ForeColor = Color.FromArgb(75, 80, 100),
            Font = new Font("Segoe UI Semibold", 8.5F)
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 31, 44),
            SelectionBackColor = Color.FromArgb(229, 224, 255),
            SelectionForeColor = Color.FromArgb(31, 31, 44),
            Font = new Font("Segoe UI", 8.5F)
        };
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "", Width = 42 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", Width = 40, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 105 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Tiêu đề", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 34 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Thời lượng", Width = 85 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "URL", Width = 250 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", Width = 150 });
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == _grid.Columns["Selected"].Index && _grid.Rows[e.RowIndex].Tag is TikTokVideoItem video)
                video.Selected = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells["Selected"].Value);
        };
        _grid.CellPainting += Grid_CellPainting;
        _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
        return _grid;
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex == -1 && e.ColumnIndex == _grid.Columns["Selected"].Index)
        {
            e.PaintBackground(e.CellBounds, true);
            e.PaintContent(e.CellBounds);

            var rect = new Rectangle(
                e.CellBounds.X + (e.CellBounds.Width - 14) / 2,
                e.CellBounds.Y + (e.CellBounds.Height - 14) / 2,
                14, 14);

            var state = _isHeaderCheckBoxChecked 
                ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal 
                : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal;

            CheckBoxRenderer.DrawCheckBox(e.Graphics, rect.Location, state);
            e.Handled = true;
        }
    }

    private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex == _grid.Columns["Selected"].Index)
        {
            _isHeaderCheckBoxChecked = !_isHeaderCheckBoxChecked;
            SetAllSelected(_isHeaderCheckBoxChecked);
            _grid.InvalidateCell(e.ColumnIndex, -1);
        }
    }

    private async Task ScanAsync()
    {
        if (_operationCts != null) return;
        SaveConfig();
        try
        {
            _operationCts = new CancellationTokenSource();
            SetBusy(true);
            SetStatus("Đang quét danh sách video...");
            var videos = await GetService().ListChannelVideosAsync(_channelTextBox.Text, _proxyTextBox.Text, _operationCts.Token);
            _videos.Clear();
            _videos.AddRange(videos);
            RefreshGrid();
            SetStatus($"Đã tìm thấy {_videos.Count} video");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Đã dừng quét danh sách");
        }
        catch (Exception ex)
        {
            SetStatus("Quét thất bại");
            MessageBox.Show(this, ex.Message, "Tải video TikTok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _operationCts?.Dispose();
            _operationCts = null;
            SetBusy(false);
        }
    }

    private async Task DownloadAsync()
    {
        var selected = _videos.Where(video => video.Selected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Hãy chọn ít nhất một video để tải.", "Tải video TikTok", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var outputDirectory = _outputTextBox.Text.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            MessageBox.Show(this, "Hãy chọn thư mục lưu video.", "Tải video TikTok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SaveConfig();

        try
        {
            _operationCts = new CancellationTokenSource();
            SetBusy(true);
            await GetService().DownloadAsync(selected, outputDirectory, _proxyTextBox.Text, UpdateVideoStatus, _operationCts.Token);
            SetStatus("Đã xử lý xong danh sách tải");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Đã dừng tải video");
        }
        catch (Exception ex)
        {
            SetStatus("Tải video thất bại");
            MessageBox.Show(this, ex.Message, "Tải video TikTok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _operationCts?.Dispose();
            _operationCts = null;
            SetBusy(false);
        }
    }

    private void BrowseOutput(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = _outputTextBox.Text };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputTextBox.Text = dialog.SelectedPath;
            SaveConfig();
        }
    }

    private TikTokDownloaderService GetService()
        => _service ??= new TikTokDownloaderService();

    private void RefreshGrid()
    {
        _grid.Rows.Clear();
        int stt = 1;
        foreach (var video in _videos)
        {
            var row = _grid.Rows[_grid.Rows.Add(video.Selected, stt++, video.VideoId, video.Title, video.DurationText, video.Url, video.Status)];
            row.Tag = video;
        }
        _summaryLabel.Text = $"{_videos.Count} video · {_videos.Count(video => video.Selected)} video được chọn";
    }

    private void SetAllSelected(bool selected)
    {
        _isHeaderCheckBoxChecked = selected;
        _grid.InvalidateCell(_grid.Columns["Selected"].Index, -1);
        foreach (var video in _videos) video.Selected = selected;
        RefreshGrid();
    }

    private void UpdateVideoStatus(TikTokVideoItem video, string status)
    {
        video.Status = status;
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateVideoStatus(video, status));
            return;
        }

        var row = _grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(item => ReferenceEquals(item.Tag, video));
        if (row != null) row.Cells["Status"].Value = status;
        SetStatus(status);
    }

    private void ConfigureButton(Button button, string text, Color color, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 32;
        button.BackColor = color;
        button.ForeColor = color == Color.FromArgb(235, 237, 242) ? Color.FromArgb(70, 70, 85) : Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI Semibold", 8.5F);
        button.Margin = new Padding(3, 0, 3, 0);
        button.Cursor = Cursors.Hand;
    }

    private void SetBusy(bool busy)
    {
        _scanButton.Enabled = !busy;
        _downloadButton.Enabled = !busy;
        _selectAllButton.Enabled = !busy;
        _clearSelectionButton.Enabled = !busy;
        _browseOutputButton.Enabled = !busy;
        _cancelButton.Enabled = busy;
        _channelTextBox.Enabled = !busy;
        _proxyTextBox.Enabled = !busy;
        _outputTextBox.Enabled = !busy;
    }

    private void SetStatus(string message)
    {
        _summaryLabel.Text = message;
        StatusChanged?.Invoke(this, message);
    }

    private string GetConfigPath()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowPilot");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, "tiktok_config.json");
    }

    private void LoadConfig()
    {
        try
        {
            var path = GetConfigPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Proxy", out var proxyProp))
                    _proxyTextBox.Text = proxyProp.GetString();
                
                if (doc.RootElement.TryGetProperty("OutputDirectory", out var outProp))
                {
                    var dir = outProp.GetString();
                    if (!string.IsNullOrWhiteSpace(dir))
                        _outputTextBox.Text = dir;
                }
            }
        }
        catch { }
    }

    private void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                Proxy = _proxyTextBox.Text,
                OutputDirectory = _outputTextBox.Text
            });
            File.WriteAllText(GetConfigPath(), json);
        }
        catch { }
    }
}
