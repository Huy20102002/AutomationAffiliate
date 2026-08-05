import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Replace types
    content = content.replace("ReaLTaiizor.Controls.HopeTextBox", "Guna.UI2.WinForms.Guna2TextBox")
    content = content.replace("ReaLTaiizor.Controls.HopeComboBox", "Guna.UI2.WinForms.Guna2ComboBox")
    content = content.replace("ReaLTaiizor.Controls.HopeButton", "Guna.UI2.WinForms.Guna2Button")
    
    # In ApplyLightTheme, HopeTextBox properties: BaseColor, BorderColorA, BorderColorB
    content = content.replace("hopeTxt.BaseColor = Color.White;", "hopeTxt.FillColor = Color.White;")
    content = content.replace("hopeTxt.BorderColorA = primary;", "hopeTxt.FocusedState.BorderColor = primary;")
    content = content.replace("hopeTxt.BorderColorB = Color.FromArgb(180, 185, 200);", "hopeTxt.BorderColor = Color.FromArgb(180, 185, 200);")
    
    # HopeButton properties: PrimaryColor -> FillColor
    content = content.replace(".PrimaryColor =", ".FillColor =")

    # In UpdateInspectorFieldLayout, we should fix TableLayoutPanel layout by making elements Visible instead of changing Row height.
    # Actually, Guna2TextBox inside TableLayoutPanel works perfectly if we just use RowStyles height = 0, because it handles rendering better than HopeTextBox!
    # So we don't necessarily need to change the TableLayoutPanel logic unless it still glitches.

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

if __name__ == "__main__":
    process_file(sys.argv[1])
