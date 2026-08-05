import re

filepath = r"c:\tool\toolupvideoshopee\MainForm.Designer.cs"

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Find and replace AddColumn lines by regex
lines = content.split('\n')
new_lines = []
skip_until_log = False
replaced = False

for i, line in enumerate(lines):
    stripped = line.strip().rstrip('\r')
    
    if 'AddColumn("STT", "colId"' in stripped:
        # Replace this block of 7 AddColumn lines
        indent = '        '
        new_lines.append(indent + 'AddColumn("STT", "colId", 50);\r')
        new_lines.append(indent + 'AddColumn("\u0110\u01b0\u1eddng d\u1eabn video", "colVideo", 320);\r')
        new_lines.append(indent + 'AddColumn("Ti\u00eau \u0111\u1ec1", "colTitle", 350);\r')
        new_lines.append(indent + 'AddColumn("Li\u00ean k\u1ebft ti\u1ebfp th\u1ecb", "colLink", 260);\r')
        new_lines.append(indent + 'AddColumn("Tr\u1ea1ng th\u00e1i", "colStatus", 110);\r')
        new_lines.append(indent + 'AddColumn("Tr\u1ea1ng th\u00e1i Shopee", "colShopeeStatus", 140);\r')
        new_lines.append(indent + 'AddColumn("Nh\u1eadt k\u00fd", "colLog", 300);\r')
        skip_until_log = True
        replaced = True
        continue
    
    if skip_until_log:
        if 'AddColumn(' in stripped and 'colLog' in stripped:
            skip_until_log = False
            continue
        elif 'AddColumn(' in stripped:
            continue
        else:
            skip_until_log = False
    
    new_lines.append(line)

if replaced:
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write('\n'.join(new_lines))
    print("OK - columns updated in Designer.cs")
else:
    print("AddColumn block not found")

# Also update RefreshJobGrid in MainForm.cs to match new column order
mainform = r"c:\tool\toolupvideoshopee\MainForm.cs"
with open(mainform, 'r', encoding='utf-8') as f:
    mc = f.read()

# The row add order must match column order: Id, Video, Title, Link, Status, ShopeeStatus, Log
old_add = 'job.Id, job.VideoPath, job.ShopeeAffLink, job.Title, job.Status, job.ShopeeStatus, job.Log'
new_add = 'job.Id, job.VideoPath, job.Title, job.ShopeeAffLink, job.Status, job.ShopeeStatus, job.Log'

if old_add in mc:
    mc = mc.replace(old_add, new_add)
    with open(mainform, 'w', encoding='utf-8') as f:
        f.write(mc)
    print("OK - RefreshJobGrid row order updated")
else:
    print("Row order already correct or not found")

# Also update ApplyProductChange cell assignments
old_cells = '''row.Cells["colVideo"].Value = updated.VideoPath;
            row.Cells["colLink"].Value = updated.ShopeeAffLink;
            row.Cells["colTitle"].Value = updated.Title;'''
new_cells = '''row.Cells["colVideo"].Value = updated.VideoPath;
            row.Cells["colTitle"].Value = updated.Title;
            row.Cells["colLink"].Value = updated.ShopeeAffLink;'''

with open(mainform, 'r', encoding='utf-8') as f:
    mc = f.read()

if old_cells in mc:
    mc = mc.replace(old_cells, new_cells)
    with open(mainform, 'w', encoding='utf-8') as f:
        f.write(mc)
    print("OK - ApplyProductChange cell order updated")
else:
    print("Cell order already correct or not found")
