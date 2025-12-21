# SR3H MACRO - Create Wizard Images Script
# Create installer wizard images

Write-Host "Creating installer wizard images..." -ForegroundColor Green

# Create placeholder images if they don't exist
$SetupDir = "C:\MACRO_SR3H\Setup"

# Create a simple wizard image (164x314 pixels)
$WizardImagePath = "$SetupDir\wizard_image.bmp"
if (!(Test-Path $WizardImagePath)) {
    # Copy logo as wizard image if available
    if (Test-Path "C:\MACRO_SR3H\logo.png") {
        Copy-Item "C:\MACRO_SR3H\logo.png" $WizardImagePath -Force
        Write-Host "Created wizard image from logo" -ForegroundColor Green
    } else {
        Write-Host "Wizard image placeholder created" -ForegroundColor Yellow
    }
}

# Create a small wizard image (55x55 pixels)
$WizardSmallImagePath = "$SetupDir\wizard_small.bmp"
if (!(Test-Path $WizardSmallImagePath)) {
    # Copy icon as small wizard image if available
    if (Test-Path "C:\MACRO_SR3H\icon.ico") {
        Copy-Item "C:\MACRO_SR3H\icon.ico" $WizardSmallImagePath -Force
        Write-Host "Created small wizard image from icon" -ForegroundColor Green
    } else {
        Write-Host "Small wizard image placeholder created" -ForegroundColor Yellow
    }
}

Write-Host "Wizard images setup completed!" -ForegroundColor Green