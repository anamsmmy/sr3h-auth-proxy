# Create a simple icon for SR3H MACRO
# This script creates a basic ICO file using PowerShell

Add-Type -AssemblyName System.Drawing

# Create a 32x32 bitmap
$bitmap = New-Object System.Drawing.Bitmap(32, 32)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Set background color (dark blue)
$graphics.Clear([System.Drawing.Color]::FromArgb(25, 25, 112))

# Create brushes and pens
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$yellowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Gold)
$whitePen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 2)

# Draw SR3H text
$font = New-Object System.Drawing.Font("Arial", 8, [System.Drawing.FontStyle]::Bold)
$graphics.DrawString("SR3H", $font, $whiteBrush, 2, 2)
$graphics.DrawString("MACRO", $font, $yellowBrush, 2, 18)

# Draw a simple geometric shape (rectangle with border)
$graphics.DrawRectangle($whitePen, 20, 8, 10, 16)
$graphics.FillRectangle($yellowBrush, 21, 9, 8, 14)

# Save as ICO file
$iconPath = "c:\MACRO_SR3H\Setup\sr3h_icon.ico"

try {
    # Convert bitmap to icon
    $icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
    $fileStream = [System.IO.File]::Create($iconPath)
    $icon.Save($fileStream)
    $fileStream.Close()
    
    Write-Host "Icon created successfully: $iconPath" -ForegroundColor Green
} catch {
    Write-Host "Failed to create icon: $($_.Exception.Message)" -ForegroundColor Red
}

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$whiteBrush.Dispose()
$yellowBrush.Dispose()
$whitePen.Dispose()
$font.Dispose()

if ($icon) { $icon.Dispose() }
if ($fileStream) { $fileStream.Dispose() }