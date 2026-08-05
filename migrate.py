import re
import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Replacements in Designer
    content = content.replace("ReaLTaiizor.Controls.HopeTextBox", "Guna.UI2.WinForms.Guna2TextBox")
    content = content.replace("ReaLTaiizor.Controls.HopeComboBox", "Guna.UI2.WinForms.Guna2ComboBox")
    content = content.replace("ReaLTaiizor.Controls.HopeButton", "Guna.UI2.WinForms.Guna2Button")
    
    # CreateCardPanel
    content = re.sub(
        r'private static Panel CreateCardPanel\(\) => new\(\) \{ Dock = DockStyle\.Fill, BackColor = Color\.White, Margin = new Padding\(5\), Padding = new Padding\(1\) \};',
        'private static Guna.UI2.WinForms.Guna2Panel CreateCardPanel() => new() { Dock = DockStyle.Fill, FillColor = Color.White, BorderRadius = 8, Margin = new Padding(5), Padding = new Padding(1) };',
        content
    )
    
    # CreateButton
    create_btn_old = """    private static Control CreateButton(string text, Color color, int width)
    {
        var button = new Guna.UI2.WinForms.Guna2Button
        {
            Text = text,
            Width = width,
            Height = 34,
            PrimaryColor = color,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Margin = new Padding(3, 0, 3, 0),
            Cursor = Cursors.Hand
        };
        return button;
    }"""
    create_btn_new = """    private static Control CreateButton(string text, Color color, int width)
    {
        var button = new Guna.UI2.WinForms.Guna2Button
        {
            Text = text,
            Width = width,
            Height = 34,
            FillColor = color,
            BorderRadius = 4,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Margin = new Padding(3, 0, 3, 0),
            Cursor = Cursors.Hand
        };
        return button;
    }"""
    content = content.replace(create_btn_old.replace("Guna.UI2.WinForms.Guna2Button", "ReaLTaiizor.Controls.HopeButton"), create_btn_new)
    
    # AddField
    add_field_old = """    private static void AddField(TableLayoutPanel table, string label, out Guna.UI2.WinForms.Guna2TextBox textBox, int row)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, row);
        textBox = new Guna.UI2.WinForms.Guna2TextBox { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 4, 0, 4) };
        table.Controls.Add(textBox, 1, row);
    }"""
    add_field_new = """    private static void AddField(TableLayoutPanel table, string label, out Guna.UI2.WinForms.Guna2TextBox textBox, int row)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 111, 140), Font = new Font("Segoe UI", 8.5F) }, 0, row);
        textBox = new Guna.UI2.WinForms.Guna2TextBox { Dock = DockStyle.Fill, BorderRadius = 4, Margin = new Padding(0, 4, 0, 4) };
        table.Controls.Add(textBox, 1, row);
    }"""
    content = content.replace(add_field_old.replace("Guna.UI2.WinForms.Guna2TextBox", "ReaLTaiizor.Controls.HopeTextBox"), add_field_new)

    # Type change for panelWorkflowContainer and panelInspector
    content = content.replace("private Panel panelWorkflowContainer;", "private Guna.UI2.WinForms.Guna2Panel panelWorkflowContainer;")
    content = content.replace("private Panel panelInspector;", "private Guna.UI2.WinForms.Guna2Panel panelInspector;")

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

if __name__ == "__main__":
    process_file(sys.argv[1])
