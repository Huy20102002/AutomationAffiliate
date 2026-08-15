using System.Drawing.Drawing2D;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Controls;

/// <summary>
/// Canvas hiển thị workflow dưới dạng các node nối tiếp nhau.
/// Canvas giữ phần trình bày tách khỏi engine để UI có thể mở rộng thành
/// node editor mà không làm ảnh hưởng logic ADB.
/// </summary>
public sealed class WorkflowCanvas : ScrollableControl
{
    private readonly System.Windows.Forms.Timer _pulseTimer;
    private IReadOnlyList<WorkflowStep> _steps = [];
    private int _selectedIndex = -1;
    private int _runningIndex = -1;
    private int _hoverIndex = -1;
    private float _pulse;
    private bool _lightTheme;
    private int _dragIndex = -1;
    private Point _dragOffset;
    private bool _wasDragged;
    private bool _isPanning;
    private Point _panStartPoint;
    private PointF _panStartOffset;
    private PointF _panOffset = new PointF(0, 0);
    private float _zoom = 1f;

    public event EventHandler<int>? StepSelected;
    public event EventHandler<int>? StepEditRequested;
    public event EventHandler<int>? StepContextRequested;
    public event EventHandler<(int From, int To)>? StepReorderRequested;
    public event EventHandler<int>? StepMoved;
    public event EventHandler<(StepType Type, Point Location)>? StepDropRequested;
    public event EventHandler<float>? ZoomChanged;

    public int SelectedIndex => _selectedIndex;
    public float Zoom => _zoom;

    public void SetRunningStep(int index)
    {
        _runningIndex = index >= 0 && index < _steps.Count ? index : -1;
        Invalidate();
    }

    public void ClearRunningStep()
    {
        if (_runningIndex == -1) return;
        _runningIndex = -1;
        Invalidate();
    }

    public void ApplyTheme(bool lightTheme)
    {
        _lightTheme = lightTheme;
        BackColor = lightTheme ? Color.FromArgb(248, 252, 255) : Color.FromArgb(12, 17, 30);
        ForeColor = lightTheme ? Color.FromArgb(27, 55, 82) : Color.FromArgb(228, 235, 246);
        Invalidate();
    }

    public WorkflowCanvas()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                  ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer |
                  ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(12, 17, 30);
        ForeColor = Color.FromArgb(228, 235, 246);
        TabStop = true;
        AllowDrop = true;
        DragEnter += (_, e) =>
        {
            e.Effect = e.Data?.GetDataPresent(DataFormats.StringFormat) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        };
        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.StringFormat) is not string value ||
                !Enum.TryParse<StepType>(value, out var type)) return;
            var clientPoint = PointToClient(new Point(e.X, e.Y));
            StepDropRequested?.Invoke(this, (type, ToCanvasPoint(clientPoint)));
        };

        _pulseTimer = new System.Windows.Forms.Timer { Interval = 45 };
        _pulseTimer.Tick += (_, _) =>
        {
            _pulse += 0.08f;
            Invalidate();
        };
        _pulseTimer.Start();
    }

    public void SetSteps(IReadOnlyList<WorkflowStep> steps)
    {
        _steps = steps;
        if (_selectedIndex >= _steps.Count)
            _selectedIndex = _steps.Count - 1;
        
        Invalidate();
    }

    public void SelectStep(int index)
    {
        _selectedIndex = index >= 0 && index < _steps.Count ? index : -1;
        Invalidate();
    }

    public void ZoomIn() => SetZoom(_zoom + 0.1f);

    public void ZoomOut() => SetZoom(_zoom - 0.1f);

    public void ResetZoom() => SetZoom(1f, resetScroll: true);

    private void SetZoom(float value, bool resetScroll = false)
    {
        var next = Math.Clamp((float)Math.Round(value, 1), 0.1f, 3.0f);
        if (Math.Abs(next - _zoom) < 0.01f) return;

        var oldZoom = _zoom;
        var viewportCenter = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
        
        var logicalCenter = new PointF(
            (viewportCenter.X - _panOffset.X) / oldZoom,
            (viewportCenter.Y - _panOffset.Y) / oldZoom);

        _zoom = next;

        if (resetScroll)
        {
            _panOffset = new PointF(0, 0);
        }
        else
        {
            _panOffset = new PointF(
                viewportCenter.X - logicalCenter.X * _zoom,
                viewportCenter.Y - logicalCenter.Y * _zoom);
        }
        
        ZoomChanged?.Invoke(this, _zoom);
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == Keys.Control)
        {
            if (e.Delta > 0) ZoomIn();
            else if (e.Delta < 0) ZoomOut();
            return;
        }
        _panOffset.Y += e.Delta;
        Invalidate();
        base.OnMouseWheel(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isPanning)
        {
            var deltaX = e.X - _panStartPoint.X;
            var deltaY = e.Y - _panStartPoint.Y;
            _panOffset = new PointF(_panStartOffset.X + deltaX, _panStartOffset.Y + deltaY);
            _wasDragged = Math.Abs(deltaX) > 2 || Math.Abs(deltaY) > 2;
            Cursor = Cursors.SizeAll;
            Invalidate();
            return;
        }
        if (_dragIndex >= 0)
        {
            var point = ToCanvasPoint(e.Location);
            var step = _steps[_dragIndex];
            var newX = point.X - _dragOffset.X;
            var newY = point.Y - _dragOffset.Y;
            step.CanvasX = newX == -1 ? -2 : newX;
            step.CanvasY = newY == -1 ? -2 : newY;
            _wasDragged = true;
            StepMoved?.Invoke(this, _dragIndex);
            Invalidate();
            return;
        }
        var hit = HitTest(e.Location);
        if (hit != _hoverIndex)
        {
            _hoverIndex = hit;
            Cursor = hit >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverIndex = -1;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        if (_wasDragged)
        {
            _wasDragged = false;
            return;
        }
        var hit = HitTest(e.Location);
        if (hit < 0) return;
        _selectedIndex = hit;
        StepSelected?.Invoke(this, hit);
        Invalidate();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button != MouseButtons.Left) return;
        var hit = HitTest(e.Location);
        if (hit < 0) return;
        _selectedIndex = hit;
        StepEditRequested?.Invoke(this, hit);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle) return;
        var hit = HitTest(e.Location);

        // Kéo vùng trống bằng chuột trái, hoặc giữ Space/chuột giữa để di chuyển toàn canvas.
        if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && (hit < 0 || (ModifierKeys & Keys.Space) == Keys.Space)))
        {
            _isPanning = true;
            _panStartPoint = e.Location;
            _panStartOffset = _panOffset;
            _wasDragged = false;
            Capture = true;
            Cursor = Cursors.SizeAll;
            return;
        }
        if (hit < 0) return;
        _selectedIndex = hit;
        StepSelected?.Invoke(this, hit);
        var point = ToCanvasPoint(e.Location);
        var rect = GetNodeRects()[hit];
        const int canvasMargin = 260;
        rect.Offset(-canvasMargin, -canvasMargin);
        _dragIndex = hit;
        _dragOffset = new Point(point.X - rect.X, point.Y - rect.Y);
        _wasDragged = false;
        Capture = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isPanning)
        {
            _isPanning = false;
            Capture = false;
            Cursor = Cursors.Default;
            Invalidate();
            return;
        }
        if (e.Button == MouseButtons.Right)
        {
            var hit = HitTest(e.Location);
            if (hit >= 0)
            {
                _selectedIndex = hit;
                StepSelected?.Invoke(this, hit);
                StepContextRequested?.Invoke(this, hit);
                Invalidate();
            }
            return;
        }
        if (e.Button != MouseButtons.Left || _dragIndex < 0) return;
        var from = _dragIndex;
        var target = HitTest(e.Location);
        var didMove = _wasDragged;
        _dragIndex = -1;
        Capture = false;
        if (didMove && target >= 0 && target != from)
            StepReorderRequested?.Invoke(this, (from, target));
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);
        e.Graphics.TranslateTransform(_panOffset.X, _panOffset.Y);
        e.Graphics.ScaleTransform(_zoom, _zoom);

        DrawGrid(e.Graphics);
        if (_steps.Count == 0)
        {
            DrawEmptyState(e.Graphics);
            return;
        }

        var rects = GetNodeRects();
        using var connectorPen = new Pen(_lightTheme ? Color.FromArgb(116, 174, 211) : Color.FromArgb(72, 104, 133), 1.5f) { DashStyle = DashStyle.Dash };
        for (var i = 0; i < rects.Count - 1; i++)
        {
            var a = rects[i];
            var b = rects[i + 1];
            var start = new Point(a.Right, a.Top + a.Height / 2);
            var end = new Point(b.Left, b.Top + b.Height / 2);
            var middleX = (start.X + end.X) / 2;
            e.Graphics.DrawBezier(connectorPen, start,
                new Point(middleX, start.Y), new Point(middleX, end.Y), end);
            DrawArrow(e.Graphics, connectorPen.Color, end);
        }

        for (var i = 0; i < rects.Count; i++)
            DrawNode(e.Graphics, rects[i], _steps[i], i);
    }

    private void DrawGrid(Graphics g)
    {
        using var pen = new Pen(_lightTheme ? Color.FromArgb(222, 235, 245) : Color.FromArgb(22, 40, 58), 1);
        var startX = (int)(-_panOffset.X / _zoom);
        var startY = (int)(-_panOffset.Y / _zoom);
        var logicalWidth = startX + (int)Math.Ceiling(Width / _zoom) + 64;
        var logicalHeight = startY + (int)Math.Ceiling(Height / _zoom) + 64;
        
        var firstLineX = (startX / 32) * 32;
        var firstLineY = (startY / 32) * 32;
        
        for (var x = firstLineX; x < logicalWidth; x += 32) g.DrawLine(pen, x, startY, x, logicalHeight);
        for (var y = firstLineY; y < logicalHeight; y += 32) g.DrawLine(pen, startX, y, logicalWidth, y);
    }

    private void DrawEmptyState(Graphics g)
    {
        using var titleFont = new Font("Segoe UI Semibold", 13f);
        using var bodyFont = new Font("Segoe UI", 9.5f);
        using var titleBrush = new SolidBrush(_lightTheme ? Color.FromArgb(27, 70, 104) : Color.FromArgb(225, 235, 246));
        using var bodyBrush = new SolidBrush(_lightTheme ? Color.FromArgb(91, 128, 157) : Color.FromArgb(128, 151, 177));
        var title = "Workflow đang trống";
        var body = "Kéo một block từ thư viện bên trái vào canvas để bắt đầu";
        var titleSize = g.MeasureString(title, titleFont);
        var bodySize = g.MeasureString(body, bodyFont);
        var centerX = Width / 2f;
        g.DrawString(title, titleFont, titleBrush, centerX - titleSize.Width / 2, Height / 2f - 28);
        g.DrawString(body, bodyFont, bodyBrush, centerX - bodySize.Width / 2, Height / 2f + 2);
    }

    private void DrawNode(Graphics g, Rectangle rect, WorkflowStep step, int index)
    {
        var accent = GetAccent(step.Type);
        var selected = index == _selectedIndex;
        var running = index == _runningIndex;
        var hovered = index == _hoverIndex;
        var fill = running
            ? (_lightTheme ? Color.FromArgb(225, 248, 239) : Color.FromArgb(25, 67, 60))
            : selected
            ? (_lightTheme ? Color.FromArgb(226, 243, 252) : Color.FromArgb(31, 51, 69))
            : (_lightTheme ? Color.White : Color.FromArgb(22, 31, 46));
        if (hovered && !selected && !running) fill = _lightTheme ? Color.FromArgb(242, 249, 253) : Color.FromArgb(27, 42, 60);

        using var path = RoundedRect(rect, 10);
        using var fillBrush = new SolidBrush(fill);
        var borderColor = running
            ? Color.FromArgb(0, 174, 139)
            : selected
                ? Color.FromArgb(120, accent)
                : (_lightTheme ? Color.FromArgb(177, 209, 227) : Color.FromArgb(47, 73, 94));
        using var borderPen = new Pen(borderColor, running || selected ? 1.8f : 1f);
        g.FillPath(fillBrush, path);
        g.DrawPath(borderPen, path);

        if (selected || running)
        {
            var glowAlpha = 34 + (int)(22 * (0.5 + 0.5 * Math.Sin(_pulse)));
            var glowColor = running ? Color.FromArgb(0, 174, 139) : accent;
            using var glowPen = new Pen(Color.FromArgb(glowAlpha, glowColor), 4f);
            g.DrawPath(glowPen, path);
        }

        using var accentBrush = new SolidBrush(accent);
        g.FillEllipse(accentBrush, rect.Left + 12, rect.Top + 16, 23, 23);
        using var iconFont = new Font("Segoe UI Symbol", 10f, FontStyle.Bold);
        using var iconBrush = new SolidBrush(Color.FromArgb(8, 19, 30));
        var icon = GetIcon(step.Type);
        g.DrawString(icon, iconFont, iconBrush, rect.Left + 17, rect.Top + 17);

        using var stepFont = new Font("Segoe UI Semibold", 8.5f);
        using var textFont = new Font("Segoe UI", 8f);
        using var titleBrush = new SolidBrush(_lightTheme ? Color.FromArgb(27, 70, 104) : Color.FromArgb(220, 233, 245));
        using var textBrush = new SolidBrush(_lightTheme ? Color.FromArgb(91, 128, 157) : Color.FromArgb(139, 164, 188));
        g.DrawString($"{index + 1:00}  {WorkflowStep.GetTypeLabel(step.Type)}", stepFont, titleBrush, rect.Left + 45, rect.Top + 11);
        g.DrawString(GetSummary(step), textFont, textBrush, rect.Left + 45, rect.Top + 30);

        using var pinBrush = new SolidBrush(Color.FromArgb(98, 113, 236));
        g.FillEllipse(pinBrush, rect.Left - 4, rect.Top + rect.Height / 2 - 4, 8, 8);
        g.FillEllipse(pinBrush, rect.Right - 4, rect.Top + rect.Height / 2 - 4, 8, 8);
    }

    private List<Rectangle> GetNodeRects()
    {
        const int nodeWidth = 188;
        const int nodeHeight = 68;
        const int gapX = 58;
        const int gapY = 48;
        const int layoutMargin = 48;
        const int canvasMargin = 260;
        var result = new List<Rectangle>(_steps.Count);
        // Bố cục logic giữ nguyên khi zoom; zoom chỉ phóng to/thu nhỏ, không làm node đổi cột.
        var logicalWidth = Math.Max(nodeWidth, ClientSize.Width);
        var available = Math.Max(nodeWidth, logicalWidth - layoutMargin * 2);
        var columns = Math.Max(1, (available + gapX) / (nodeWidth + gapX));

        for (var i = 0; i < _steps.Count; i++)
        {
            if (_steps[i].CanvasX != -1 && _steps[i].CanvasY != -1)
            {
                result.Add(new Rectangle(canvasMargin + _steps[i].CanvasX, canvasMargin + _steps[i].CanvasY, nodeWidth, nodeHeight));
                continue;
            }
            var row = i / columns;
            var column = i % columns;
            var x = canvasMargin + layoutMargin + column * (nodeWidth + gapX);
            var y = canvasMargin + layoutMargin + row * (nodeHeight + gapY);
            result.Add(new Rectangle(x, y, nodeWidth, nodeHeight));
        }
        return result;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        
    }

    private void UpdateScrollSize()
    {
        const int nodeHeight = 68;
        const int gapY = 48;
        const int layoutMargin = 48;
        const int canvasMargin = 260;
        var logicalWidth = Math.Max(188, ClientSize.Width);
        var available = Math.Max(188, logicalWidth - layoutMargin * 2);
        var columns = Math.Max(1, (available + 58) / (188 + 58));
        var rows = Math.Max(1, (_steps.Count + columns - 1) / columns);
        var logicalHeight = canvasMargin * 2 + layoutMargin * 2 + rows * nodeHeight + Math.Max(0, rows - 1) * gapY;
        var logicalCanvasWidth = canvasMargin * 2 + layoutMargin * 2 + columns * 188 + Math.Max(0, columns - 1) * 58;
        foreach (var step in _steps)
        {
            if (step.CanvasX != -1) logicalCanvasWidth = Math.Max(logicalCanvasWidth, canvasMargin + step.CanvasX + 188 + canvasMargin);
            if (step.CanvasY != -1) logicalHeight = Math.Max(logicalHeight, canvasMargin + step.CanvasY + nodeHeight + canvasMargin);
        }
        AutoScrollMinSize = new Size(
            Math.Max(ClientSize.Width, (int)Math.Ceiling(logicalCanvasWidth * _zoom)),
            Math.Max(ClientSize.Height, (int)Math.Ceiling(logicalHeight * _zoom)));
    }

    private int HitTest(Point point)
    {
        point = ToCanvasPoint(point);
        const int canvasMargin = 260;
        point.Offset(canvasMargin, canvasMargin);
        var rects = GetNodeRects();
        for (var i = 0; i < rects.Count; i++)
            if (rects[i].Contains(point)) return i;
        return -1;
    }

    private Point ToCanvasPoint(Point clientPoint)
    {
        const int canvasMargin = 260;
        return new Point(
            (int)Math.Round((clientPoint.X - _panOffset.X) / _zoom - canvasMargin),
            (int)Math.Round((clientPoint.Y - _panOffset.Y) / _zoom - canvasMargin));
    }

    private static void DrawArrow(Graphics g, Color color, Point end)
    {
        using var brush = new SolidBrush(color);
        var points = new[] { end, new Point(end.X - 7, end.Y - 4), new Point(end.X - 7, end.Y + 4) };
        g.FillPolygon(brush, points);
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color GetAccent(StepType type) => type switch
    {
        StepType.Start => Color.FromArgb(71, 181, 111),
        StepType.End => Color.FromArgb(93, 108, 232),
        StepType.Tap => Color.FromArgb(92, 143, 255),
        StepType.InputText => Color.FromArgb(185, 111, 255),
        StepType.PushVideo => Color.FromArgb(0, 211, 164),
        StepType.Delay => Color.FromArgb(249, 189, 82),
        StepType.Swipe => Color.FromArgb(89, 205, 187),
        StepType.OpenApp => Color.FromArgb(255, 112, 153),
        StepType.KeyEvent => Color.FromArgb(149, 163, 255),
        StepType.MediaScan => Color.FromArgb(72, 197, 255),
        StepType.AdbShell => Color.FromArgb(232, 147, 61),
        _ => Color.FromArgb(140, 154, 174)
    };

    private static string GetIcon(StepType type) => type switch
    {
        StepType.Start => "▶",
        StepType.End => "⚑",
        StepType.Tap => "●",
        StepType.InputText => "T",
        StepType.PushVideo => "↑",
        StepType.Delay => "◷",
        StepType.Swipe => "↔",
        StepType.OpenApp => "◆",
        StepType.KeyEvent => "⌨",
        StepType.MediaScan => "◎",
        StepType.AdbShell => "$_",
        _ => "•"
    };

    private static string GetSummary(WorkflowStep step)
    {
        return step.Type switch
        {
            StepType.Start => "Bắt đầu quy trình",
            StepType.End => "Kết thúc quy trình",
            StepType.Tap => step.TapMode switch
            {
                TapMode.XPath => $"Chạm XPath  {Trim(step.TapXPath)}",
                TapMode.Image => $"Chạm ảnh  {Path.GetFileName(step.TapImagePath)}",
                _ => $"Chạm  ({step.X}, {step.Y})"
            },
            StepType.Swipe => $"Vuốt  ({step.X},{step.Y}) → ({step.X2},{step.Y2})",
            StepType.InputText => string.IsNullOrWhiteSpace(step.TextValue)
                ? (string.IsNullOrWhiteSpace(step.BindingColumn) ? "Chưa cấu hình" : $"Lấy cột {step.BindingColumn}")
                : Trim(step.TextValue),
            StepType.PushVideo => "Đẩy video lên thiết bị",
            StepType.Delay => $"Chờ {step.DelayAfterMs} ms",
            StepType.OpenApp => Trim(step.TextValue),
            StepType.KeyEvent => $"Phím {step.TextValue}",
            StepType.MediaScan => "Quét thư viện media",
            StepType.AdbShell => Trim(step.TextValue),
            _ => step.Description
        };
    }

    private static string Trim(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Chưa cấu hình";
        value = value.Replace(Environment.NewLine, " ");
        return value.Length > 27 ? value[..27] + "…" : value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pulseTimer.Dispose();
        base.Dispose(disposing);
    }
}
