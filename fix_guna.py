import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Hint -> PlaceholderText
    content = content.replace('Hint = "Tên biến"', 'PlaceholderText = "Tên biến"')
    content = content.replace('Hint = "Giá trị mặc định"', 'PlaceholderText = "Giá trị mặc định"')
    
    # PrimaryColor -> FillColor for CreateButton
    content = content.replace("PrimaryColor = color", "FillColor = color")

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

if __name__ == "__main__":
    process_file(sys.argv[1])
