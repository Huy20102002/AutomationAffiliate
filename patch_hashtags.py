import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Increase client size
    content = content.replace(
        "ClientSize = new Size(620, 300);",
        "ClientSize = new Size(620, 340);"
    )

    # Increase TableLayoutPanel row count and height
    content = content.replace(
        "Height = 210,",
        "Height = 250,"
    ).replace(
        "RowCount = 4,",
        "RowCount = 5,"
    ).replace(
        "for (var i = 0; i < 4; i++)",
        "for (var i = 0; i < 5; i++)"
    )

    # Insert hashtag panel creation
    hashtag_code = """
        var hashtagsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(0)
        };
        string[] commonHashtags = ["#shopee", "#shopeevideo", "#shopeecreator", "#review"];
        foreach (var tag in commonHashtags)
        {
            var btn = new Guna.UI2.WinForms.Guna2Button
            {
                Text = tag,
                Height = 26,
                AutoSize = true,
                FillColor = Color.FromArgb(240, 242, 245),
                ForeColor = Color.FromArgb(70, 70, 85),
                BorderRadius = 13,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5F),
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.Click += (_, _) =>
            {
                _title.Text = (_title.Text.TrimEnd() + " " + tag).TrimStart();
                _title.SelectionStart = _title.Text.Length;
                _title.Focus();
            };
            hashtagsPanel.Controls.Add(btn);
        }

        AddField(fields, "VideoPath", CreateVideoPathField(), 0);
        AddField(fields, "Tiêu đề", _title, 1);
        AddField(fields, "", hashtagsPanel, 2);
        AddField(fields, "ShopeeAffLink", _affiliateLink, 3);
        AddField(fields, "Trạng thái", _status, 4);
"""
    
    old_fields_code = """        AddField(fields, "VideoPath", CreateVideoPathField(), 0);
        AddField(fields, "Tiêu đề", _title, 1);
        AddField(fields, "ShopeeAffLink", _affiliateLink, 2);
        AddField(fields, "Trạng thái", _status, 3);"""

    content = content.replace(old_fields_code, hashtag_code.strip('\n'))

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    print("ProductEditorDialog patched with hashtags.")

if __name__ == "__main__":
    process_file("Controls/ProductEditorDialog.cs")
