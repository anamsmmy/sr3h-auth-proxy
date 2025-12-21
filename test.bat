@echo off
echo ========================================
echo    ماكرو سرعة - SR3H MACRO
echo    اختبار سريع للتطبيق
echo ========================================
echo.

REM Check .NET installation
echo [1/4] فحص تثبيت .NET 6.0...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET 6.0 غير مثبت!
    echo يرجى تحميله من: https://dotnet.microsoft.com/download/dotnet/6.0
    pause
    exit /b 1
) else (
    echo ✅ .NET 6.0 مثبت بنجاح
)

REM Check project files
echo [2/4] فحص ملفات المشروع...
if not exist "MacroApp.csproj" (
    echo ❌ ملف المشروع غير موجود!
    pause
    exit /b 1
) else (
    echo ✅ ملفات المشروع موجودة
)

REM Restore packages
echo [3/4] استعادة الحزم...
dotnet restore --verbosity quiet
if %errorlevel% neq 0 (
    echo ❌ فشل في استعادة الحزم!
    pause
    exit /b 1
) else (
    echo ✅ تم استعادة الحزم بنجاح
)

REM Build project
echo [4/4] بناء المشروع...
dotnet build --configuration Debug --verbosity quiet --no-restore
if %errorlevel% neq 0 (
    echo ❌ فشل في بناء المشروع!
    echo يرجى مراجعة الأخطاء أعلاه
    pause
    exit /b 1
) else (
    echo ✅ تم بناء المشروع بنجاح
)

echo.
echo ========================================
echo ✅ جميع الاختبارات نجحت!
echo يمكنك الآن تشغيل التطبيق بأمان
echo ========================================
echo.

echo هل تريد تشغيل التطبيق الآن؟ (Y/N)
set /p choice=
if /i "%choice%"=="Y" (
    echo.
    echo تشغيل التطبيق...
    dotnet run --configuration Debug
) else (
    echo.
    echo يمكنك تشغيل التطبيق لاحقاً باستخدام: dotnet run
)

pause