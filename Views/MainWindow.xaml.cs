using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using MacroApp.Models;
using MacroApp.Services;
using MacroApp.Views;
using SR3H_MACRO.Services;

namespace MacroApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly MacroConfiguration _configuration;
        private readonly EnhancedMacroService _macroService;
        private AutoBuildService _autoBuildService;
        private bool _isMacroRunning = false;
        private bool _isInitialized = false;
        private bool _isMinimizingToTray = false;
        private string _currentHardwareId;
        private BackgroundValidationScheduler _validationScheduler;
        private string _currentEmail;

        public MainWindow()
        {
            InitializeComponent();
            
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
            
            _configuration = new MacroConfiguration();
            _macroService = new EnhancedMacroService();
            _autoBuildService = new AutoBuildService(_configuration.AutoBuild);
            _currentHardwareId = SafeHardwareIdService.GenerateHardwareId();
            
            _macroService.StatusChanged += OnMacroStatusChanged;
            
            this.Closing += MainWindow_Closing;
            this.StateChanged += MainWindow_StateChanged;
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeConfiguration();
                CheckLicenseExpiryAsync();
                StartBackgroundValidation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في MainWindow_Loaded: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async void CheckLicenseExpiryAsync()
        {
            try
            {
                var cachedActivation = SessionActivationCache.GetCachedActivation();
                if (cachedActivation == null || string.IsNullOrEmpty(cachedActivation.Email))
                {
                    UpdateLicenseStatus("غير مفعل", false);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔄 جاري التحقق من الترخيص من الخادم...");
                
                var activationService = MacroFortActivationService.Instance;
                var serverResult = await activationService.CheckActivationStatusAsync(cachedActivation.Email);

                if (serverResult.IsSuccess && serverResult.SubscriptionData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✓ تم قراءة البيانات من الخادم بنجاح");
                    
                    var serverActivation = new ActivationData
                    {
                        Email = serverResult.SubscriptionData.Email,
                        HardwareId = _currentHardwareId,
                        SubscriptionType = serverResult.SubscriptionData.SubscriptionType,
                        SubscriptionCode = serverResult.SubscriptionData.SubscriptionCode,
                        ActivationDate = serverResult.SubscriptionData.ActivationDate,
                        ExpiryDate = serverResult.SubscriptionData.ExpiryDate,
                        IsActive = serverResult.SubscriptionData.IsActive,
                        EmailVerified = serverResult.SubscriptionData.EmailVerified,
                        LastSync = DateTime.UtcNow,
                        DeviceTransferCount = serverResult.SubscriptionData.DeviceTransferCount,
                        LastDeviceTransferDate = serverResult.SubscriptionData.LastDeviceTransferDate ?? DateTime.UtcNow
                    };

                    SessionActivationCache.SetCachedActivation(serverActivation);
                    DisplayLicenseExpiry(serverActivation);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ فشل قراءة الخادم، استخدام البيانات المحفوظة في الذاكرة");
                    var cached = SessionActivationCache.GetCachedActivation();
                    if (cached != null)
                    {
                        DisplayLicenseExpiry(cached);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ خطأ في التحقق من الخادم: {ex.Message}، استخدام البيانات المحفوظة في الذاكرة");
                
                var cachedActivation = SessionActivationCache.GetCachedActivation();
                if (cachedActivation != null)
                {
                    DisplayLicenseExpiry(cachedActivation);
                }
                else
                {
                    UpdateLicenseStatus("خطأ في التحقق", false);
                }
            }
        }

        private void DisplayLicenseExpiry(ActivationData activation)
        {
            try
            {
                if (activation == null)
                {
                    UpdateLicenseStatus("غير مفعل", false);
                    return;
                }

                int daysLeft = (int)(activation.ExpiryDate - DateTime.UtcNow).TotalDays;

                if (daysLeft <= 0)
                {
                    UpdateLicenseStatus("❌ منتهي الصلاحية", false);
                }
                else if (daysLeft <= 3)
                {
                    UpdateLicenseStatus($"⚠️ {daysLeft} أيام متبقية", false);
                }
                else if (daysLeft <= 7)
                {
                    UpdateLicenseStatus($"⏰ {daysLeft} أيام متبقية", false);
                }
                else
                {
                    var subscriptionType = GetSubscriptionTypeDisplay(activation.SubscriptionType);
                    UpdateLicenseStatus($"✅ {subscriptionType} - نشط", true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في عرض صلاحية الترخيص: {ex.Message}");
                UpdateLicenseStatus("خطأ في التحقق", false);
            }
        }

        private void StartBackgroundValidation()
        {
            try
            {
                var activation = SessionActivationCache.GetCachedActivation();
                if (activation != null && !string.IsNullOrEmpty(activation.Email))
                {
                    _currentEmail = activation.Email;
                    _validationScheduler = new BackgroundValidationScheduler(_currentEmail, _currentHardwareId);
                    _validationScheduler.ValidationStateChanged += OnValidationStateChanged;
                    _validationScheduler.Start();
                    System.Diagnostics.Debug.WriteLine("✓ تم بدء مجدول التحقق الدوري");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في بدء مجدول التحقق: {ex.Message}");
            }
        }

        private void OnValidationStateChanged(object sender, ValidationStateChangedEventArgs args)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (!args.IsValid)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ حالة التحقق غير صحيحة: {args.Message}");
                        UpdateStatus(args.Message, false);

                        if (args.Message.Contains("انقطع الإنترنت") || args.Message.Contains("انتهت صلاحية"))
                        {
                            if (_isMacroRunning)
                            {
                                StopMacro();
                                MessageBox.Show(args.Message, "تنبيه الترخيص", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في معالج حالة التحقق: {ex.Message}");
            }
        }



        private void InitializeConfiguration()
        {
            try
            {
                var configService = ConfigurationService.Instance;
                var savedConfig = configService.LoadMacroConfiguration();
                
                if (savedConfig != null && savedConfig.KeySequence != null)
                {
                    _configuration.KeySequence = savedConfig.KeySequence;
                }
                else
                {
                    _configuration.KeySequence = new KeySequenceTrigger
                    {
                        IsEnabled = true,
                        ActivationKey = "E",
                        PreHoldKey = "O",
                        HoldKey = "P",
                        ReleaseKey = "3",
                        Delay = 10
                    };
                }
                
                if (ActivationKeyButton != null)
                    ActivationKeyButton.Content = _configuration.KeySequence.ActivationKey;
                else
                    System.Diagnostics.Debug.WriteLine("خطأ: ActivationKeyButton is null");
                    
                if (PreHoldKeyButton != null)
                    PreHoldKeyButton.Content = _configuration.KeySequence.PreHoldKey;
                else
                    System.Diagnostics.Debug.WriteLine("خطأ: PreHoldKeyButton is null");
                    
                if (HoldKeyButton != null)
                    HoldKeyButton.Content = _configuration.KeySequence.HoldKey;
                else
                    System.Diagnostics.Debug.WriteLine("خطأ: HoldKeyButton is null");
                    
                if (ReleaseKeyButton != null)
                    ReleaseKeyButton.Content = _configuration.KeySequence.ReleaseKey;
                else
                    System.Diagnostics.Debug.WriteLine("خطأ: ReleaseKeyButton is null");
                    
                if (KeySequenceDelayTextBox != null)
                    KeySequenceDelayTextBox.Text = _configuration.KeySequence.Delay.ToString();
                else
                    System.Diagnostics.Debug.WriteLine("خطأ: KeySequenceDelayTextBox is null");
                
                if (_configuration.KeySequence != null)
                    _configuration.KeySequence.PropertyChanged += KeySequence_PropertyChanged;

                if (savedConfig != null && savedConfig.AutoBuild != null)
                {
                    _configuration.AutoBuild = savedConfig.AutoBuild;
                }
                else
                {
                    _configuration.AutoBuild = new AutoBuildConfiguration
                    {
                        IsEnabled = false,
                        PlaceBuilding = "K",
                        Wall = "Z",
                        Stairs = "X",
                        Floor = "C",
                        Roof = "V",
                        Delay = 0
                    };
                }

                if (PlaceBuildingButton != null)
                    PlaceBuildingButton.Content = _configuration.AutoBuild.PlaceBuilding;
                if (WallButton != null)
                    WallButton.Content = _configuration.AutoBuild.Wall;
                if (StairsButton != null)
                    StairsButton.Content = _configuration.AutoBuild.Stairs;
                if (FloorButton != null)
                    FloorButton.Content = _configuration.AutoBuild.Floor;
                if (RoofButton != null)
                    RoofButton.Content = _configuration.AutoBuild.Roof;
                if (AutoBuildDelayTextBox != null)
                    AutoBuildDelayTextBox.Text = _configuration.AutoBuild.Delay.ToString();

                if (_configuration.AutoBuild.IsEnabled)
                {
                    StartAutoBuildButton.IsEnabled = false;
                    StopAutoBuildButton.IsEnabled = true;
                }
                else
                {
                    StartAutoBuildButton.IsEnabled = true;
                    StopAutoBuildButton.IsEnabled = false;
                }
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في InitializeConfiguration: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void KeySequence_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var configService = ConfigurationService.Instance;
            configService.SaveMacroConfiguration(_configuration);
        }

        private void KeySequenceDelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized || _configuration?.KeySequence == null)
                return;
                
            if (double.TryParse(KeySequenceDelayTextBox.Text, out double delayValue))
            {
                _configuration.KeySequence.Delay = (int)delayValue;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        public void ShowSettingsWindow()
        {
            try
            {
                var propertiesWindow = new PropertiesWindow
                {
                    Owner = this
                };
                propertiesWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في فتح الإعدادات: {ex.Message}");
            }
        }

        private void MinimizeToTray()
        {
            _isMinimizingToTray = true;

            ShowInTaskbar = false;
            Visibility = System.Windows.Visibility.Hidden;
            WindowState = WindowState.Minimized;
            
            var app = Application.Current as App;
            app?.ShowTrayIcon();

            _isMinimizingToTray = false;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _autoBuildService?.Dispose();
            _validationScheduler?.Stop();
            e.Cancel = true;
            MinimizeToTray();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && !_isMinimizingToTray)
            {
                MinimizeToTray();
            }
            else if (WindowState == WindowState.Normal)
            {
                var app = Application.Current as App;
                app?.HideTrayIcon();
            }
        }

        private void OnMacroStatusChanged(object sender, MacroStatusEventArgs e)
        {
            if (e == null)
                return;

            Dispatcher.Invoke(() =>
            {
                var (message, isError) = TranslateStatus(e);
                UpdateStatus(message, isError);
            });
        }

        private (string Message, bool IsError) TranslateStatus(MacroStatusEventArgs status)
        {
            switch (status.Code)
            {
                case "error.sequence.rest":
                    return (string.Format("\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u062a\u0646\u0641\u064a\u0630 \u0627\u0644\u062a\u0633\u0644\u0633\u0644 \u0627\u0644\u0631\u0626\u064a\u0633\u064a: {0}", status.Detail), true);
                case "error.sequence.end":
                    return (string.Format("\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u0625\u0646\u0647\u0627\u0621 \u0627\u0644\u062a\u0633\u0644\u0633\u0644: {0}", status.Detail), true);
                case "info.macro.alreadyRunning":
                    return ("\u0627\u0644\u0645\u0627\u0643\u0631\u0648 \u0645\u0634\u063a\u0644 \u0628\u0627\u0644\u0641\u0639\u0644.", false);
                case "info.macro.waitingForActivation":
                    return ("\u062a\u0645 \u062a\u0634\u063a\u064a\u0644 \u0627\u0644\u0645\u0627\u0643\u0631\u0648 - \u062c\u0627\u0631ٍ \u0627\u0646\u062a\u0638\u0627\u0631 \u0645\u0641\u062a\u0627\u062d \u0627\u0644\u062a\u0641\u0639\u064a\u0644.", false);
                case "info.macro.stopped":
                    return ("جاهز للاستخدام", false);
                case "error.macro.loop":
                    return (string.Format("\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u062a\u0634\u063a\u064a\u0644 \u0627\u0644\u0645\u0627\u0643\u0631\u0648: {0}", status.Detail), true);
                case "info.hold.released":
                    return (string.Format("\u062a\u0645 \u062a\u062d\u0631\u064a\u0631 \u0645\u0641\u062a\u0627\u062d \u0627\u0644\u062a\u062b\u0628\u064a\u062a: {0}", status.Detail), false);
                case "info.macro.stopping":
                    return ("\u062c\u0627\u0631ٍ \u0625\u064a\u0642\u0627\u0641 \u0627\u0644\u0645\u0627\u0643\u0631\u0648...", false);
                case "error.activationKey.invalid":
                    return ("\u062a\u0639\u0630\u0631 \u0627\u0644\u062a\u0639\u0631\u0641 \u0639\u0644\u0649 \u0645\u0641\u062a\u0627\u062d \u0627\u0644\u062a\u0641\u0639\u064a\u0644.", true);
                case "info.macro.ready":
                    return (string.Format("\u0627\u0644\u0645\u0627\u0643\u0631\u0648 \u062c\u0627\u0647\u0632 - \u0627\u0636\u063a\u0637 {0} \u0644\u0644\u062a\u0641\u0639\u064a\u0644.", status.Detail), false);
                case "info.sequence.starting":
                    return ("\u062c\u0627\u0631ٍ \u0628\u062f\u0621 \u0627\u0644\u062a\u0633\u0644\u0633\u0644...", false);
                case "info.sequence.prehold":
                    return (string.Format("\u062a\u0645 \u0636\u063a\u0637 \u0627\u0644\u0645\u0641\u062a\u0627\u062d \u0627\u0644\u062a\u0645\u0647\u064a\u062f\u064a: {0}", status.Detail), false);
                case "info.sequence.holdPressed":
                    return (string.Format("\u062a\u0645 \u0636\u063a\u0637 \u0645\u0641\u062a\u0627\u062d \u0627\u0644\u062a\u062b\u0628\u064a\u062a: {0}", status.Detail), false);
                case "info.sequence.holdLocked":
                    return (string.Format("\u062a\u0645 \u062a\u062b\u0628\u064a\u062a \u0627\u0644\u0645\u0641\u062a\u0627\u062d: {0}", status.Detail), false);
                case "info.sequence.started":
                    return ("\u062a\u0645 \u062a\u0646\u0641\u064a\u0630 \u0628\u062f\u0627\u064a\u0629 \u0627\u0644\u062a\u0633\u0644\u0633\u0644.", false);
                case "info.sequence.cancelled":
                    return ("\u062a\u0645 \u0625\u0644\u063a\u0627\u0621 \u0627\u0644\u062a\u0633\u0644\u0633\u0644.", false);
                case "info.sequence.ending":
                    return (string.Format("\u062c\u0627\u0631ٍ \u0625\u0646\u0647\u0627\u0621 \u0627\u0644\u062a\u0633\u0644\u0633\u0644 (\u0645\u0641\u062a\u0627\u062d {0}).", status.Detail), false);
                case "info.release.pressed":
                    return (string.Format("\u062a\u0645 \u0636\u063a\u0637 \u0645\u0641\u062a\u0627\u062d \u0627\u0644\u0625\u0646\u0647\u0627\u0621: {0}", status.Detail), false);
                case "info.sequence.combination":
                    return (string.Format("\u062a\u0645 \u062a\u0646\u0641\u064a\u0630 \u0627\u0644\u062a\u0631\u0643\u064a\u0628: {0}.", status.Detail), false);
                case "info.sequence.completed":
                    return ("\u0627\u0643\u062a\u0645\u0644 \u0627\u0644\u062a\u0633\u0644\u0633\u0644.", false);
                case "info.sequence.ended":
                    return ("\u0627\u0646\u062a\u0647\u0649 \u0627\u0644\u062a\u0633\u0644\u0633\u0644 - \u064a\u0645\u0643\u0646\u0643 \u0625\u0637\u0644\u0627\u0642 \u0627\u0644\u0645\u0641\u0627\u062a\u064a\u062d.", false);
                case "error.sequence.execution":
                    return (string.Format("\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u062a\u0646\u0641\u064a\u0630 \u0627\u0644\u062a\u0633\u0644\u0633\u0644: {0}", status.Detail), true);
                case "error.input.releaseKey":
                    return (string.Format("\u062a\u0639\u0630\u0631 \u062a\u062d\u0631\u064a\u0631 \u0627\u0644\u0645\u0641\u062a\u0627\u062d: {0}", status.Detail), true);
                case "error.input.pressKey":
                    return (string.Format("\u062a\u0639\u0630\u0631 \u0636\u063a\u0637 \u0627\u0644\u0645\u0641\u062a\u0627\u062d: {0}", status.Detail), true);
                case "error.sequence.general":
                    return (string.Format("\u062d\u062f\u062b \u062e\u0637\u0623 \u063a\u064a\u0631 \u0645\u062a\u0648\u0642\u0639: {0}", status.Detail), true);
                case "error.sequence.start":
                    return (string.Format("\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u0628\u062f\u0621 \u0627\u0644\u062a\u0633\u0644\u0633\u0644: {0}", status.Detail), true);
                case "info.test.running":
                    return ("\u062c\u0627\u0631ٍ \u0627\u062e\u062a\u0628\u0627\u0631 \u0627\u0644\u062a\u0633\u0644\u0633\u0644...", false);
                case "error.test.activationKey":
                    return ("\u0645\u0641\u062a\u0627\u062d \u0627\u0644\u062a\u0641\u0639\u064a\u0644 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d.", true);
                case "error.test.delay":
                    return ("\u0642\u064a\u0645\u0629 \u0627\u0644\u062a\u0623\u062e\u064a\u0631 \u062e\u0627\u0631\u062c \u0627\u0644\u0646\u0637\u0627\u0642 (1-10000).", true);
                case "info.test.success":
                    return ("\u062a\u0645 \u0627\u062c\u062a\u064a\u0627\u0632 \u0627\u0644\u0627\u062e\u062a\u0628\u0627\u0631 \u0628\u0646\u062c\u0627\u062d.", false);
                case "error.test.exception":
                    return (string.Format("\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u0627\u0644\u0627\u062e\u062a\u0628\u0627\u0631: {0}", status.Detail), true);
                default:
                    var fallback = !string.IsNullOrWhiteSpace(status.Detail) ? status.Detail : status.Code;
                    return (fallback, false);
            }
        }

        private void UpdateStatus(string message, bool isError)
        {
            if (StatusText != null)
            {
                StatusText.Text = message;
                StatusText.Foreground = isError ? 
                    new SolidColorBrush(Colors.Red) : 
                    new SolidColorBrush(Colors.Green);
            }
        }

        private void UpdateAutoTestModeStatus()
        {
            if (StatusText != null)
            {
                if (_configuration.AutoBuild.IsEnabled)
                {
                    StatusText.Text = "\u0628\u0646\u0627\u0621 \u0622\u0644\u064a: \u0645\u0641\u0639\u0644";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    StatusText.Text = "\u0628\u0646\u0627\u0621 \u0622\u0644\u064a: \u0645\u0639\u0637\u0644";
                    StatusText.Foreground = new SolidColorBrush(Colors.Gray);
                }
            }
        }

        private void AutoBuildToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;

            try
            {
                _configuration.AutoBuild.IsEnabled = true;
                _autoBuildService.UpdateConfiguration(_configuration.AutoBuild);
                _autoBuildService.Start();
                StartAutoBuildButton.IsEnabled = false;
                StopAutoBuildButton.IsEnabled = true;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
                UpdateAutoTestModeStatus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"\u062e\u0637\u0623 \u0641\u064a \u062a\u0641\u0639\u064a\u0644 \u0627\u0644\u0628\u0646\u0627\u0621 \u0627\u0644\u0622\u0644\u064a: {ex.Message}", true);
            }
        }

        private void AutoBuildToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;

            try
            {
                _configuration.AutoBuild.IsEnabled = false;
                _autoBuildService.Stop();
                StartAutoBuildButton.IsEnabled = true;
                StopAutoBuildButton.IsEnabled = false;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
                UpdateAutoTestModeStatus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"\u062e\u0637\u0623 \u0641\u064a \u062a\u0639\u0637\u064a\u0644 \u0627\u0644\u0628\u0646\u0627\u0621 \u0627\u0644\u0622\u0644\u064a: {ex.Message}", true);
            }
        }

        private void KeySequenceToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;

            try
            {
                _configuration.KeySequence.IsEnabled = true;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                UpdateStatus($"\u062e\u0637\u0623 \u0641\u064a \u062a\u0641\u0639\u064a\u0644 \u0627\u0644\u062a\u0633\u0644\u0633\u0644: {ex.Message}", true);
            }
        }

        private void KeySequenceToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;

            try
            {
                _configuration.KeySequence.IsEnabled = false;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                UpdateStatus($"\u062e\u0637\u0623 \u0641\u064a \u062a\u0639\u0637\u064a\u0644 \u0627\u0644\u062a\u0633\u0644\u0633\u0644: {ex.Message}", true);
            }
        }

        private void SelectActivationKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: false);
            if (dialog.ShowDialog() == true)
            {
                ActivationKeyButton.Content = dialog.SelectedKey;
                _configuration.KeySequence.ActivationKey = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private void SelectPreHoldKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: false);
            if (dialog.ShowDialog() == true)
            {
                PreHoldKeyButton.Content = dialog.SelectedKey;
                _configuration.KeySequence.PreHoldKey = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private void SelectHoldKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: false);
            if (dialog.ShowDialog() == true)
            {
                HoldKeyButton.Content = dialog.SelectedKey;
                _configuration.KeySequence.HoldKey = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private void SelectReleaseKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: false);
            if (dialog.ShowDialog() == true)
            {
                ReleaseKeyButton.Content = dialog.SelectedKey;
                _configuration.KeySequence.ReleaseKey = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private void TestKeySequence_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new TestWindow();
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                UpdateStatus($"\u062e\u0637\u0623 \u0641\u064a \u0627\u062e\u062a\u0628\u0627\u0631 \u0627\u0644\u062a\u0633\u0644\u0633\u0644: {ex.Message}", true);
            }
        }

        private void UpdateKeySequenceDelay()
        {
            if (!_isInitialized || _configuration?.KeySequence == null)
                return;

            try
            {
                if (double.TryParse(KeySequenceDelayTextBox.Text, out double delayValue))
                {
                    if (delayValue < 1)
                    {
                        _configuration.KeySequence.Delay = 1;
                        KeySequenceDelayTextBox.Text = "1";
                        UpdateStatus("\u0642\u064a\u0645\u0629 \u0627\u0644\u062a\u0623\u062e\u064a\u0631 \u064a\u062c\u0628 \u0623\u0646 \u062a\u0643\u0648\u0646 \u0623\u0639\u0644\u0649 \u0645\u0646 1.", true);
                    }
                    else if (delayValue > 10000)
                    {
                        _configuration.KeySequence.Delay = 10000;
                        KeySequenceDelayTextBox.Text = "10000";
                        UpdateStatus("\u0642\u064a\u0645\u0629 \u0627\u0644\u062a\u0623\u062e\u064a\u0631 \u064a\u062c\u0628 \u0623\u0646 \u062a\u0643\u0648\u0646 \u0623\u0642\u0644 \u0645\u0646 10000.", true);
                    }
                    else
                    {
                        _configuration.KeySequence.Delay = (int)delayValue;
                    }
                    ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
                }
                else
                {
                    _configuration.KeySequence.Delay = 10;
                    KeySequenceDelayTextBox.Text = "10";
                    UpdateStatus("\u0642\u064a\u0645\u0629 \u0627\u0644\u062a\u0623\u062e\u064a\u0631 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d\u0629.", true);
                }
                
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                UpdateStatus(string.Format("\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u062a\u062d\u062f\u064a\u062b \u0627\u0644\u062a\u0623\u062e\u064a\u0631: {0}", ex.Message), true);
            }
        }

        private void ActivationButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }

        private void FeaturesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var instructionsWindow = new InstructionsWindow();
                instructionsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في فتح التعليمات: {ex.Message}");
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutWindow = new AboutWindow();
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في فتح حول: {ex.Message}");
            }
        }

        private void AffiliateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var affiliateWindow = new AffiliateWindow();
                affiliateWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في فتح نافذة التسويق بالعمولة: {ex.Message}");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "SR3H Encrypted Files (*.sr3h)|*.sr3h",
                    DefaultExt = ".sr3h",
                    FileName = $"SR3H_MACRO_Settings_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.sr3h"
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(_configuration, Newtonsoft.Json.Formatting.Indented);
                    var encrypted = EncryptionService.AdvancedEncrypt(json);
                    System.IO.File.WriteAllText(saveDialog.FileName, encrypted);
                    System.Diagnostics.Debug.WriteLine($"تم تصدير الإعدادات المشفرة بنجاح إلى: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصدير الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new System.Windows.Forms.OpenFileDialog
                {
                    Filter = "SR3H Encrypted Files (*.sr3h)|*.sr3h|JSON Files (*.json)|*.json"
                };

                if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileContent = System.IO.File.ReadAllText(openDialog.FileName);
                    string json;

                    if (openDialog.FileName.EndsWith(".sr3h"))
                    {
                        json = EncryptionService.AdvancedDecrypt(fileContent);
                    }
                    else
                    {
                        json = fileContent;
                    }

                    var importedConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<MacroConfiguration>(json);

                    if (importedConfig != null)
                    {
                        _configuration.KeySequence = importedConfig.KeySequence;
                        _configuration.AutoBuild = importedConfig.AutoBuild;
                        ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
                        InitializeConfiguration();
                        System.Diagnostics.Debug.WriteLine("تم استيراد الإعدادات بنجاح");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في استيراد الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SelectPlaceBuildingKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: false);
            if (dialog.ShowDialog() == true)
            {
                PlaceBuildingButton.Content = dialog.SelectedKey;
                _configuration.AutoBuild.PlaceBuilding = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private async void SelectWallKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: true);
            if (dialog.ShowDialog() == true)
            {
                WallButton.Content = dialog.SelectedKey;
                _configuration.AutoBuild.Wall = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private async void SelectStairsKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: true);
            if (dialog.ShowDialog() == true)
            {
                StairsButton.Content = dialog.SelectedKey;
                _configuration.AutoBuild.Stairs = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private async void SelectFloorKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: true);
            if (dialog.ShowDialog() == true)
            {
                FloorButton.Content = dialog.SelectedKey;
                _configuration.AutoBuild.Floor = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private async void SelectRoofKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new KeyCaptureDialog(allowEmpty: true);
            if (dialog.ShowDialog() == true)
            {
                RoofButton.Content = dialog.SelectedKey;
                _configuration.AutoBuild.Roof = dialog.SelectedKey;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private void AutoBuildDelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized || _configuration?.AutoBuild == null)
                return;

            if (double.TryParse(AutoBuildDelayTextBox.Text, out double delay))
            {
                if (delay < 0)
                    delay = 0;
                
                if (delay > 10000)
                    delay = 10000;
                    
                _configuration.AutoBuild.Delay = delay;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
        }

        private void StartAutoBuildButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _configuration.AutoBuild.IsEnabled = true;
                _autoBuildService.UpdateConfiguration(_configuration.AutoBuild);
                _autoBuildService.Start();
                
                StartAutoBuildButton.IsEnabled = false;
                StopAutoBuildButton.IsEnabled = true;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                UpdateStatus($"خطأ في تشغيل بناء آلي: {ex.Message}", true);
            }
        }

        private void StopAutoBuildButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _configuration.AutoBuild.IsEnabled = false;
                _autoBuildService.Stop();
                
                StartAutoBuildButton.IsEnabled = true;
                StopAutoBuildButton.IsEnabled = false;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                UpdateStatus($"خطأ في إيقاف بناء آلي: {ex.Message}", true);
            }
        }

        private async void StartMacroDrawButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await VerifyLicenseBeforeRunning())
                {
                    UpdateStatus("❌ فشل التحقق من الترخيص - الماكرو معطل", false);
                    MessageBox.Show("فشل التحقق من الترخيص. يرجى التأكد من اتصالك بالإنترنت.", "خطأ في الترخيص", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StartMacroDrawButton.IsEnabled = false;
                StopMacroDrawButton.IsEnabled = true;
                
                _configuration.KeySequence.IsEnabled = true;
                await _macroService.StartMacroAsync(_configuration.KeySequence);
                
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                UpdateStatus($"خطأ في تشغيل ماكرو رسم: {ex.Message}", true);
                StartMacroDrawButton.IsEnabled = true;
                StopMacroDrawButton.IsEnabled = false;
            }
        }

        private async Task<bool> VerifyLicenseBeforeRunning()
        {
            try
            {
                if (_validationScheduler == null)
                    return false;

                return await _validationScheduler.PerformImmediateValidationAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في التحقق من الترخيص قبل التشغيل: {ex.Message}");
                return false;
            }
        }

        private void StopMacro()
        {
            try
            {
                _configuration.KeySequence.IsEnabled = false;
                _macroService.StopMacro();
                
                StartMacroDrawButton.IsEnabled = true;
                StopMacroDrawButton.IsEnabled = false;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إيقاف الماكرو: {ex.Message}");
            }
        }

        private void StopMacroDrawButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _configuration.KeySequence.IsEnabled = false;
                _macroService.StopMacro();
                
                StartMacroDrawButton.IsEnabled = true;
                StopMacroDrawButton.IsEnabled = false;
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                UpdateStatus($"خطأ في إيقاف ماكرو رسم: {ex.Message}", true);
            }
        }

        private void SaveMacroDrawButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
                UpdateStatus("تم حفظ إعدادات ماكرو رسم", false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"خطأ في حفظ الإعدادات: {ex.Message}", true);
            }
        }

        private void SaveAutoBuildButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigurationService.Instance.SaveMacroConfiguration(_configuration);
                UpdateStatus("تم حفظ إعدادات بناء آلي", false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"خطأ في حفظ الإعدادات: {ex.Message}", true);
            }
        }

        private string GetSubscriptionTypeDisplay(string subscriptionType)
        {
            return subscriptionType?.ToLower() switch
            {
                "trial" => "تجريبي",
                "month" => "شهري",
                "monthly" => "شهري",
                "6-month" => "6 أشهر",
                "semi" => "نصف سنوي",
                "yearly" => "سنوي",
                "year" => "سنوي",
                "lifetime" => "مدى الحياة",
                _ => subscriptionType ?? "-"
            };
        }

        private void UpdateLicenseStatus(string message, bool isActive)
        {
            if (LicenseStatusText != null)
            {
                LicenseStatusText.Text = message;
                LicenseStatusText.Foreground = isActive ? 
                    new SolidColorBrush(Colors.Green) : 
                    new SolidColorBrush(Color.FromRgb(255, 127, 23));
            }
        }

        public void RefreshLicenseStatusFromDatabase()
        {
            System.Diagnostics.Debug.WriteLine("🔄 طلب تحديث معلومات الترخيص من الواجهة الرئيسية...");
            CheckLicenseExpiryAsync();
        }
    }
}
