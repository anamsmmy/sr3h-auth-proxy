@echo off
echo Building ماكرو سرعة - SR3H MACRO...
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "publish" rmdir /s /q "publish"

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo Failed to restore packages!
    pause
    exit /b 1
)

REM Build in Release mode
echo Building in Release mode...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

REM Publish self-contained executable
echo Publishing self-contained executable...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "publish"
if %errorlevel% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo Output directory: publish\
echo Executable: publish\MacroApp.exe
echo.
pause