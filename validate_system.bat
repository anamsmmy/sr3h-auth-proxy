@echo off
echo ========================================
echo    ماكرو سرعة - SR3H MACRO
echo    التحقق من النظام الجديد
echo ========================================
echo.

set "errors=0"

echo 🔍 فحص ملفات النظام الجديد...
echo.

REM Check database files
echo [1/10] فحص ملفات قاعدة البيانات...
if not exist "Database\supabase_setup.sql" (
    echo ❌ ملف SQL غير موجود!
    set /a errors+=1
) else (
    findstr /C:"macro_subscriptions" "Database\supabase_setup.sql" >nul
    if %errorlevel% equ 0 (
        echo ✅ ملف SQL محدث بالجدول الجديد
    ) else (
        echo ❌ ملف SQL لا يحتوي على الجدول الجديد!
        set /a errors+=1
    )
)

REM Check authentication service
echo [2/10] فحص خدمة التفعيل...
if not exist "Services\AuthenticationService.cs" (
    echo ❌ خدمة التفعيل غير موجودة!
    set /a errors+=1
) else (
    findstr /C:"macro_subscriptions" "Services\AuthenticationService.cs" >nul
    if %errorlevel% equ 0 (
        echo ✅ خدمة التفعيل محدثة
    ) else (
        echo ❌ خدمة التفعيل لم يتم تحديثها!
        set /a errors+=1
    )
)

REM Check user subscription model
echo [3/10] فحص نموذج المستخدم...
if not exist "Models\UserSubscription.cs" (
    echo ❌ نموذج المستخدم غير موجود!
    set /a errors+=1
) else (
    findstr /C:"subscription_start" "Models\UserSubscription.cs" >nul
    if %errorlevel% equ 0 (
        echo ✅ نموذج المستخدم محدث
    ) else (
        echo ❌ نموذج المستخدم لم يتم تحديثه!
        set /a errors+=1
    )
)

REM Check hardware ID service
echo [4/10] فحص خدمة Hardware ID...
if not exist "Services\HardwareIdService.cs" (
    echo ❌ خدمة Hardware ID غير موجودة!
    set /a errors+=1
) else (
    findstr /C:"GetCpuId" "Services\HardwareIdService.cs" >nul
    if %errorlevel% equ 0 (
        echo ✅ خدمة Hardware ID متقدمة
    ) else (
        echo ❌ خدمة Hardware ID بسيطة!
        set /a errors+=1
    )
)

REM Check encryption service
echo [5/10] فحص خدمة التشفير...
if not exist "Services\EncryptionService.cs" (
    echo ❌ خدمة التشفير غير موجودة!
    set /a errors+=1
) else (
    findstr /C:"SecureSupabaseConfig" "Services\EncryptionService.cs" >nul
    if %errorlevel% equ 0 (
        echo ✅ خدمة التشفير متقدمة
    ) else (
        echo ❌ خدمة التشفير بسيطة!
        set /a errors+=1
    )
)

REM Check security service
echo [6/10] فحص خدمة الأمان...
if not exist "Services\SecurityService.cs" (
    echo ❌ خدمة الأمان غير موجودة!
    set /a errors+=1
) else (
    findstr /C:"PerformSecurityCheck" "Services\SecurityService.cs" >nul
    if %errorlevel% equ 0 (
        echo ✅ خدمة الأمان موجودة
    ) else (
        echo ❌ خدمة الأمان ناقصة!
        set /a errors+=1
    )
)

REM Check license window
echo [7/10] فحص نافذة التفعيل...
if not exist "Views\LicenseWindow.xaml.cs" (
    echo ❌ نافذة التفعيل غير موجودة!
    set /a errors+=1
) else (
    findstr /C:"ReactivateSubscriptionAsync" "Views\LicenseWindow.xaml.cs" >nul
    if %errorlevel% equ 0 (
        echo ✅ نافذة التفعيل محدثة
    ) else (
        echo ❌ نافذة التفعيل لم يتم تحديثها!
        set /a errors+=1
    )
)

REM Check branding
echo [8/10] فحص العلامة التجارية...
if not exist "Views\MainWindow.xaml" (
    echo ❌ النافذة الرئيسية غير موجودة!
    set /a errors+=1
) else (
    findstr /C:"ماكرو سرعة" "Views\MainWindow.xaml" >nul
    if %errorlevel% equ 0 (
        echo ✅ العلامة التجارية محدثة
    ) else (
        echo ❌ العلامة التجارية لم يتم تحديثها!
        set /a errors+=1
    )
)

REM Check documentation
echo [9/10] فحص التوثيق...
if not exist "NEW_AUTHENTICATION_SYSTEM.md" (
    echo ❌ توثيق النظام الجديد غير موجود!
    set /a errors+=1
) else (
    echo ✅ توثيق النظام الجديد موجود
)

REM Check .NET and build
echo [10/10] فحص البناء...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET 6.0 غير مثبت!
    set /a errors+=1
) else (
    dotnet build --configuration Debug --verbosity quiet --no-restore >nul 2>&1
    if %errorlevel% equ 0 (
        echo ✅ المشروع يبنى بنجاح
    ) else (
        echo ❌ فشل في بناء المشروع!
        set /a errors+=1
    )
)

echo.
echo ========================================
if %errors% equ 0 (
    echo ✅ جميع الفحوصات نجحت! النظام جاهز
    echo.
    echo 📋 الخطوات التالية:
    echo 1. نفذ Database\supabase_setup.sql في Supabase
    echo 2. أضف مستخدم للاختبار
    echo 3. شغل التطبيق واختبر التفعيل
    echo.
    echo هل تريد تشغيل التطبيق الآن؟ (Y/N)
    set /p choice=
    if /i "%choice%"=="Y" (
        echo.
        echo 🚀 تشغيل التطبيق...
        dotnet run --configuration Debug
    )
) else (
    echo ❌ تم العثور على %errors% مشاكل!
    echo يرجى إصلاح المشاكل أعلاه قبل المتابعة
)
echo ========================================

pause