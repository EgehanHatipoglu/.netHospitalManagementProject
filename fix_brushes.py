import re
file_path = "MainWindow.axaml"
with open(file_path, "r") as f:
    text = f.read()

keys = ["PrimaryBgBrush", "SidebarBgBrush", "CardBgBrush", "HoverBgBrush", "InputBgBrush", 
        "TextPrimaryBrush", "TextSecondaryBrush", "TextMutedBrush", "BorderBrush", "DividerBrush",
        "PrimaryBg", "SidebarBg", "CardBg", "HoverBg", "InputBg", 
        "TextPrimary", "TextSecondary", "TextMuted", "TextOnAccent", "BorderClr", "DividerClr"]

for key in keys:
    text = text.replace("{StaticResource " + key + "}", "{DynamicResource " + key + "}")

with open(file_path, "w") as f:
    f.write(text)
