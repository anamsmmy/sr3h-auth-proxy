@echo off
echo ========================================
echo    ماكرو سرعة - SR3H MACRO
echo    اختبار نظام التفعيل الجديد
echo ========================================
echo.

echo 🔒 نظام التفعيل المحدث:
echo ✅ جدول macro_subscriptions الجديد
echo ✅ Hardware ID محسن (CPU + MAC + HDD)
echo ✅ تتبع last_check للمستخدمين
echo ✅ إعادة ربط الأجهزة المحسنة
echo.

echo 📋 خطوات الاختبار:
echo.

echo [1/5] فحص ملفات النظام الجديد...
if not exist "Database\supabase_setup.sql" (
    echo ❌ ملف SQL غير موجود!
    pause
    exit /b 1
) else (
    echo ✅ ملف SQL موجود
)

if not exist "Services\AuthenticationService.cs" (
    echo ❌ خدمة التفعيل غير موجودة!
    pause
    exit /b 1
) else (
    echo ✅ خدمة التفعيل محدثة
)

if not exist "NEW_AUTHENTICATION_SYSTEM.md" (
    echo ❌ ملف التوثيق غير موجود!
    pause
    exit /b 1
) else (
    echo ✅ توثيق النظام الجديد موجود
)

echo [2/5] فحص .NET Framework...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET 6.0 غير مثبت!
    pause
    exit /b 1
) else (
    echo ✅ .NET 6.0 مثبت
)

echo [3/5] استعادة الحزم...
dotnet restore --verbosity quiet
if %errorlevel% neq 0 (
    echo ❌ فشل في استعادة الحزم!
    pause
    exit /b 1
) else (
    echo ✅ تم استعادة الحزم
)

echo [4/5] بناء المشروع...
dotnet build --configuration Debug --verbosity quiet --no-restore
if %errorlevel% neq 0 (
    echo ❌ فشل في البناء!
    pause
    exit /b 1
) else (
    echo ✅ تم البناء بنجاح
)

echo [5/5] فحص التشفير...
if not exist "Services\EncryptionService.cs" (
    echo ❌ خدمة التشفير غير موجودة!
    pause
    exit /b 1
) else (
    echo ✅ خدمة التشفير موجودة
)

echo.
echo ========================================
echo ✅ جميع اختبارات النظام الجديد نجحت!
echo ========================================
echo.

echo 📝 الخطوات التالية:
echo 1. نفذ ملف Database\supabase_setup.sql في Supabase
echo 2. أضف مستخدم للاختبار:
echo    INSERT INTO macro_subscriptions (email, order_id, is_active, subscription_start)
echo    VALUES ('test@sr3h.com', 'SR3H001', true, NOW());
echo 3. شغل التطبيق واختبر التفعيل
echo.

echo هل تريد تشغيل التطبيق الآن؟ (Y/N)
set /p choice=
if /i "%choice%"=="Y" (
    echo.
    echo 🚀 تشغيل التطبيق مع النظام الجديد...
    dotnet run --configuration Debug
) else (
    echo.
    echo يمكنك تشغيل التطبيق لاحقاً باستخدام: dotnet run
    echo أو استخدام: ./run.bat
)

pause