#!/usr/bin/env python3
"""
Script to create a simple icon for SR3H MACRO application
"""

from PIL import Image, ImageDraw, ImageFont
import os

def create_app_icon():
    # Create a 256x256 image with transparent background
    size = 256
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Draw a modern circular background
    margin = 20
    circle_size = size - (margin * 2)
    
    # Gradient-like effect with multiple circles
    colors = [
        (33, 150, 243, 255),  # Blue
        (25, 118, 210, 255),  # Darker blue
        (13, 71, 161, 255),   # Even darker blue
    ]
    
    for i, color in enumerate(colors):
        offset = i * 8
        draw.ellipse([
            margin + offset, 
            margin + offset, 
            size - margin - offset, 
            size - margin - offset
        ], fill=color)
    
    # Draw "SR3H" text
    try:
        # Try to use a system font
        font_size = 48
        font = ImageFont.truetype("arial.ttf", font_size)
    except:
        # Fallback to default font
        font = ImageFont.load_default()
    
    # Draw main text "SR3H"
    text = "SR3H"
    bbox = draw.textbbox((0, 0), text, font=font)
    text_width = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    
    text_x = (size - text_width) // 2
    text_y = (size - text_height) // 2 - 10
    
    # Draw text with shadow effect
    draw.text((text_x + 2, text_y + 2), text, font=font, fill=(0, 0, 0, 128))
    draw.text((text_x, text_y), text, font=font, fill=(255, 255, 255, 255))
    
    # Draw "MACRO" subtitle
    try:
        small_font = ImageFont.truetype("arial.ttf", 20)
    except:
        small_font = ImageFont.load_default()
    
    subtitle = "MACRO"
    bbox = draw.textbbox((0, 0), subtitle, font=small_font)
    subtitle_width = bbox[2] - bbox[0]
    
    subtitle_x = (size - subtitle_width) // 2
    subtitle_y = text_y + text_height + 5
    
    draw.text((subtitle_x + 1, subtitle_y + 1), subtitle, font=small_font, fill=(0, 0, 0, 128))
    draw.text((subtitle_x, subtitle_y), subtitle, font=small_font, fill=(255, 255, 255, 255))
    
    return img

def main():
    # Create the icon
    icon = create_app_icon()
    
    # Save as PNG first
    icon.save('C:/MACRO_SR3H/Setup/app_icon.png', 'PNG')
    
    # Create different sizes for ICO file
    sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    icons = []
    
    for size in sizes:
        resized = icon.resize(size, Image.Resampling.LANCZOS)
        icons.append(resized)
    
    # Save as ICO file
    icons[0].save('C:/MACRO_SR3H/Setup/app_icon.ico', format='ICO', sizes=[(icon.width, icon.height) for icon in icons])
    
    print("Icon files created successfully!")
    print("- app_icon.png (256x256)")
    print("- app_icon.ico (multi-size)")

if __name__ == "__main__":
    main()