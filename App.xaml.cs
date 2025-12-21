using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MacroApp.Services;
using MacroApp.Views;
using SR3H_MACRO.Services;

namespace MacroApp
{
    public partial class App : Application
    {
        private AuthenticationService _authService;
        private LicenseWindow _licenseWindow;
        private static Mutex _mutex = null;
        private System.Windows.Forms.NotifyIcon _trayIcon;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            const string appName = "MacroApp_SR3H_SingleInstance";
            bool createdNew;
            
            try
            {
                _mutex = new Mutex(true, appName, out createdNew);
                
                if (!createdNew)
                {
                    System.Diagnostics.Debug.WriteLine("التطبيق يعمل بالفعل");
                    Shutdown();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في Mutex: {ex.Message}");
            }

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    var ex = args.ExceptionObject as Exception;
                    MessageBox.Show($"خطأ غير معالج: {ex?.Message}\n\nStack Trace:\n{ex?.StackTrace}", 
                                  "خطأ في التطبيق", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                };

                DispatcherUnhandledException += (sender, args) =>
                {
                    if (args.Exception.Message.Contains("resource not found") || 
                        args.Exception.Message.Contains("Dispatcher processing has been suspended"))
                    {
                        System.Diagnostics.Debug.WriteLine($"تم تجاهل خطأ الواجهة: {args.Exception.Message}");
                        args.Handled = true;
                        return;
                    }
                    
                    MessageBox.Show($"خطأ في الواجهة: {args.Exception.Message}\n\nStack Trace:\n{args.Exception.StackTrace}", 
                                  "خطأ في الواجهة", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    args.Handled = true;
                };

                await CheckActivationAndProceedAsync();
                InitializeSystemTray();

            }
            catch (Exception ex)
            {
                var errorMsg = $"خطأ في بدء التطبيق:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                MessageBox.Show($"خطأ في بدء التطبيق: {ex.Message}\n\n{ex.StackTrace}", "خطأ - ماكرو سرعة", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                Shutdown();
            }
        }

        private async System.Threading.Tasks.Task CheckActivationAndProceedAsync()
        {
            try
            {
                var freshHardwareId = SafeHardwareIdService.GetFreshHardwareId();
                
                // أولاً: فحص قاعدة البيانات للبحث عن اشتراك نشط (إجباري عند البدء)
                var activationService = MacroFortActivationService.Instance;
                var dbActivation = await activationService.GetSubscriptionByHardwareIdAsync(freshHardwareId);

                if (dbActivation != null && !string.IsNullOrEmpty(dbActivation.Email))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ وجدت اشتراك في قاعدة البيانات: {dbActivation.Email}");
                    
                    // تحويل MacroFortSubscriptionData إلى ActivationData
                    var activationData = new ActivationData
                    {
                        Email = dbActivation.Email,
                        ExpiryDate = dbActivation.ExpiryDate,
                        IsActive = dbActivation.IsActive,
                        SubscriptionType = dbActivation.SubscriptionType,
                        HardwareId = freshHardwareId
                    };
                    
                    // حفظ في cache الذاكرة لـ grace period
                    SessionActivationCache.SetCachedActivation(activationData);
                    SessionActivationCache.SetGracePeriodExpiry(DateTime.UtcNow.AddMinutes(5));
                    
                    await VerifyWithServerInBackgroundAsync(dbActivation.Email, freshHardwareId);
                    return;
                }

                // ثانياً: محاولة قراءة من cache الذاكرة (في حالة الفشل في الخادم)
                var cachedActivation = SessionActivationCache.GetCachedActivation();

                if (cachedActivation != null && !string.IsNullOrEmpty(cachedActivation.Email))
                {
                    System.Diagnostics.Debug.WriteLine("✅ وجدت بيانات في cache الذاكرة - بدء التحقق من الخادم في الخلفية");
                    await VerifyWithServerInBackgroundAsync(cachedActivation.Email, freshHardwareId);
                    return;
                }

                // لا توجد بيانات في الخادم أو الذاكرة - عرض نافذة الترخيص
                System.Diagnostics.Debug.WriteLine("✗ لم يتم العثور على تفعيل - يتم عرض نافذة الترخيص");
                ShowLicenseWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في فحص التفعيل: {ex.Message}");
                ShowLicenseWindow();
            }
        }

        private async System.Threading.Tasks.Task VerifyWithServerMandatoryAsync(string email, string hardwareId)
        {
            try
            {
                var activationService = MacroFortActivationService.Instance;
                var result = await activationService.CheckActivationStatusAsync(email);

                if (result.IsSuccess)
                {
                    if (result.SubscriptionData?.HardwareId != hardwareId)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ تحذير أمان: عدم تطابق hardware_id");
                        System.Diagnostics.Debug.WriteLine($"   المحفوظ محلياً: {hardwareId}");
                        System.Diagnostics.Debug.WriteLine($"   في الخادم: {result.SubscriptionData?.HardwareId}");
                        
                        MessageBox.Show(
                            "تم اكتشاف عدم تطابق في معرف الجهاز!\n\n" +
                            "قد يكون هناك محاولة غير مصرح بها للوصول.\n" +
                            "يرجى إعادة تفعيل البرنامج.",
                            "تحذير أمان",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning,
                            MessageBoxResult.OK,
                            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                        
                        SessionActivationCache.Clear();
                        ShowLicenseWindow();
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"✓ تم التحقق من الترخيص بنجاح - النوع: {result.SubscriptionData?.SubscriptionType}");
                    
                    // حفظ في cache الذاكرة فقط
                    var activationData = new ActivationData
                    {
                        Email = result.SubscriptionData.Email,
                        HardwareId = hardwareId,
                        SubscriptionType = result.SubscriptionData.SubscriptionType,
                        SubscriptionCode = result.SubscriptionData.SubscriptionCode,
                        ActivationDate = result.SubscriptionData.ActivationDate,
                        ExpiryDate = result.SubscriptionData.ExpiryDate,
                        IsActive = result.SubscriptionData.IsActive,
                        EmailVerified = result.SubscriptionData.EmailVerified,
                        LastSync = DateTime.UtcNow,
                        DeviceTransferCount = result.SubscriptionData.DeviceTransferCount,
                        LastDeviceTransferDate = result.SubscriptionData.LastDeviceTransferDate ?? DateTime.UtcNow
                    };

                    SessionActivationCache.SetCachedActivation(activationData);
                    ShowMainWindow();
                }
                else if (result.ResultType == "expired")
                {
                    System.Diagnostics.Debug.WriteLine("❌ انتهت صلاحية الاشتراك");
                    MessageBox.Show(
                        "انتهت صلاحية اشتراكك\n\nيرجى تجديد الاشتراك للمتابعة.",
                        "الترخيص منتهي الصلاحية",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    SessionActivationCache.Clear();
                    ShowLicenseWindow();
                }
                else if (result.ResultType == "fortnite_closed")
                {
                    System.Diagnostics.Debug.WriteLine("❌ فورتنايت معطلة - التطبيق يتطلب فورتنايت");
                    MessageBox.Show(
                        "فورتنايت معطلة!\n\n" +
                        "ماكرو سرعة مخصص للعمل مع لعبة Fortnite فقط.\n" +
                        "يرجى تشغيل فورتنايت أولاً.",
                        "فورتنايت معطلة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    Shutdown();
                }
                else if (result.ResultType == "no_internet")
                {
                    System.Diagnostics.Debug.WriteLine("❌ لا يوجد اتصال بالإنترنت - التطبيق يتطلب إنترنت");
                    MessageBox.Show(
                        "التطبيق يتطلب اتصال بالإنترنت!\n\n" +
                        "مثل لعبة Fortnite، ماكرو سرعة يتطلب إنترنت مستمر للتحقق من صحة الترخيص.\n\n" +
                        "يرجى التأكد من اتصالك بالإنترنت وإعادة المحاولة.",
                        "لا يوجد اتصال إنترنت",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    Shutdown();
                }
                else if (result.Message.Contains("لم يتم العثور على") || result.Message.Contains("not found"))
                {
                    System.Diagnostics.Debug.WriteLine("الحساب غير موجود - يتم عرض نافذة التفعيل");
                    SessionActivationCache.Clear();
                    ShowLicenseWindow();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ فشل التحقق: {result.Message}");
                    MessageBox.Show($"فشل التحقق من الترخيص:\n{result.Message}\n\nيرجى التأكد من اتصالك بالإنترنت.", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    ShowLicenseWindow();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في التحقق من السيرفر: {ex.Message}");
                MessageBox.Show(
                    "❌ فشل الاتصال بخادم التحقق!\n\n" +
                    "تأكد من:\n" +
                    "✓ وجود اتصال إنترنت نشط\n" +
                    "✓ عدم وجود جدار حماية يحجب الاتصال\n" +
                    "✓ أن الخادم متاح\n\n" +
                    "الخطأ: " + ex.Message, 
                    "خطأ في الاتصال",
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error, 
                    MessageBoxResult.OK, 
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                Shutdown();
            }
        }

        private async Task VerifyWithServerInBackgroundAsync(string email, string hardwareId)
        {
            await VerifyWithServerMandatoryAsync(email, hardwareId);
        }

        private void ShowLicenseWindow()
        {
            _licenseWindow = new LicenseWindow();
            _licenseWindow.ShowDialog();

            var freshHardwareId = SafeHardwareIdService.GetFreshHardwareId();
            var activation = SessionActivationCache.GetCachedActivation();

            if (activation != null && !string.IsNullOrEmpty(activation.Email))
            {
                System.Diagnostics.Debug.WriteLine("🔄 جاري التحقق من الترخيص الجديد عبر السيرفر (إجباري) في الخلفية...");
                _ = VerifyWithServerInBackgroundAsync(activation.Email, freshHardwareId);
            }
            else
            {
                MessageBox.Show("لم يتم تفعيل البرنامج. سيتم إغلاق التطبيق.", "ترخيص - ماكرو سرعة", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                Shutdown();
            }
        }

        public void ShowMainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🎯 ShowMainWindow() called");
                
                if (MainWindow != null && MainWindow.IsVisible)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ MainWindow already visible, returning");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("🔨 Creating new MainWindow instance...");
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Topmost = true;
                
                System.Diagnostics.Debug.WriteLine("📺 Showing MainWindow...");
                mainWindow.Show();
                mainWindow.Activate();
                mainWindow.Focus();
                
                System.Diagnostics.Debug.WriteLine("✅ MainWindow shown successfully");
                
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => 
                {
                    try
                    {
                        Dispatcher.Invoke(() => mainWindow.Topmost = false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error setting Topmost to false: {ex.Message}");
                    }
                });
                
                _licenseWindow?.Close();
                System.Diagnostics.Debug.WriteLine("✓ License window closed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in ShowMainWindow(): {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"فشل فتح الواجهة الرئيسية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeSystemTray()
        {
            try
            {
                _trayIcon = new System.Windows.Forms.NotifyIcon();
                _trayIcon.BalloonTipTitle = "ماكرو سرعة";
                
                var appDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var iconPath = System.IO.Path.Combine(appDir, "icon.ico");
                
                if (System.IO.File.Exists(iconPath))
                {
                    _trayIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    _trayIcon.Icon = System.Drawing.SystemIcons.Application;
                }
                
                _trayIcon.Text = "ماكرو سرعة";
                _trayIcon.Visible = false;

                System.Windows.Forms.ContextMenuStrip contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
                contextMenuStrip.Items.Add("فتح", null, TrayOpen_Click);
                contextMenuStrip.Items.Add("إعدادات", null, TraySettings_Click);
                contextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                contextMenuStrip.Items.Add("خروج", null, TrayExit_Click);

                _trayIcon.ContextMenuStrip = contextMenuStrip;
                _trayIcon.DoubleClick += TrayIcon_DoubleClick;
                _trayIcon.MouseClick += TrayIcon_MouseClick;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تهيئة نظام الدرج: {ex.Message}");
            }
        }

        private void TrayIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                RestoreFromTray();
            }
        }

        public void ShowTrayIcon()
        {
            if (_trayIcon != null && _trayIcon.Icon != null)
            {
                try
                {
                    _trayIcon.Visible = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في إظهار أيقونة الدرج: {ex.Message}");
                }
            }
        }

        public void HideTrayIcon()
        {
            if (_trayIcon != null)
            {
                try
                {
                    _trayIcon.Visible = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في إخفاء أيقونة الدرج: {ex.Message}");
                }
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TrayIcon DoubleClick triggered");
                RestoreFromTray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في TrayIcon_DoubleClick: {ex.Message}");
            }
        }

        private void TrayOpen_Click(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void TraySettings_Click(object sender, EventArgs e)
        {
            RestoreFromTray();

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MainWindow is Views.MainWindow main)
                {
                    main.ShowSettingsWindow();
                }
            });
        }

        private void TrayExit_Click(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void RestoreFromTray()
        {
            System.Diagnostics.Debug.WriteLine($"RestoreFromTray called, MainWindow = {(MainWindow != null ? "not null" : "null")}");
            
            if (MainWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("MainWindow is null!");
                return;
            }
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    MainWindow.ShowInTaskbar = true;
                    MainWindow.Visibility = System.Windows.Visibility.Visible;
                    MainWindow.WindowState = WindowState.Normal;
                    MainWindow.Show();
                    MainWindow.Activate();
                    MainWindow.Focus();
                    
                    IntPtr handle = new System.Windows.Interop.WindowInteropHelper(MainWindow).Handle;
                    if (handle != IntPtr.Zero)
                    {
                        SetForegroundWindow(handle);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في RestoreFromTray: {ex}");
                }
            });
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void ExitApplication()
        {
            if (_trayIcon != null)
            {
                try
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في إغلاق أيقونة الدرج: {ex.Message}");
                }
            }
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_trayIcon != null)
            {
                try
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف نظام الدرج: {ex.Message}");
                }
            }

            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch { }
            }

            base.OnExit(e);
        }
    }
}
