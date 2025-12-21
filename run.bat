@echo off
echo ========================================
echo    ماكرو سرعة - SR3H MACRO
echo    تشغيل التطبيق (الوضع العادي)
echo ========================================
echo.

echo 🚀 تشغيل التطبيق...
echo ⚠️ فحص الأمان مفعل
echo ⚠️ سيتم إغلاق التطبيق عند اكتشاف أدوات مشبوهة
echo.

REM Check if .NET 6 is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo .NET 6.0 SDK is not installed!
    echo Please install .NET 6.0 SDK from: https://dotnet.microsoft.com/download/dotnet/6.0
    pause
    exit /b 1
)

REM Restore packages if needed
if not exist "bin" (
    echo Restoring packages...
    dotnet restore
)

REM Run the application
dotnet run --project MacroApp.csproj --configuration Debug

echo.
echo انتهى التشغيل.
pause