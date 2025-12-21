using System;
using System.Windows;
using MacroApp.Models;
using MacroApp.Services;

namespace MacroApp.Views
{
    public partial class MacroFortActivationWindow : Window
    {
        private readonly MacroFortActivationType _activationType;
        private readonly MacroFortActivationService _activationService;
        private string _currentHardwareId;
        private string _currentEmail;
        private string _currentCode;

        public MacroFortActivationWindow(MacroFortActivationType activationType)
        {
            InitializeComponent();
            _activationType = activationType;
            _activationService = MacroFortActivationService.Instance;
            
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
            
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            try
            {
                _currentHardwareId = _activationService.GenerateHardwareId();
                HardwareIdText.Text = $"معرف الجهاز: {_currentHardwareId}";

                switch (_activationType)
                {
                    case MacroFortActivationType.Trial:
                        TrialSection.Visibility = Visibility.Visible;
                        break;
                    case MacroFortActivationType.CodeActivation:
                        CodeActivationSection.Visibility = Visibility.Visible;
                        break;
                    case MacroFortActivationType.Rebind:
                        RebindSection.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في التهيئة: {ex.Message}");
            }
        }

        private async void TrialStartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentEmail = TrialEmailTextBox.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(_currentEmail))
                {
                    ShowError("يرجى إدخال بريد إلكتروني صحيح");
                    return;
                }

                TrialStartButton.IsEnabled = false;
                
                var result = await _activationService.StartTrialAsync(_currentEmail);
                
                if (result.IsSuccess)
                {
                    TrialSection.Visibility = Visibility.Collapsed;
                    VerificationSection.Visibility = Visibility.Visible;
                    ShowInfo("تم إرسال رمز التحقق إلى بريدك الإلكتروني");
                }
                else
                {
                    ShowError(result.Message);
                    TrialStartButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
                TrialStartButton.IsEnabled = true;
            }
        }

        private async void CodeActivateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentEmail = CodeEmailTextBox.Text.Trim();
                _currentCode = CodeTextBox.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(_currentEmail) || string.IsNullOrWhiteSpace(_currentCode))
                {
                    ShowError("يرجى إدخال البريد والكود");
                    return;
                }

                CodeActivateButton.IsEnabled = false;
                
                var result = await _activationService.SendOtpForCodeActivationAsync(_currentEmail, _currentCode);
                
                if (result.IsSuccess)
                {
                    CodeActivationSection.Visibility = Visibility.Collapsed;
                    VerificationSection.Visibility = Visibility.Visible;
                    ShowInfo("تم إرسال رمز التحقق إلى بريدك الإلكتروني");
                }
                else
                {
                    ShowError(result.Message);
                    CodeActivateButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
                CodeActivateButton.IsEnabled = true;
            }
        }

        private async void RebindButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentEmail = RebindEmailTextBox.Text.Trim();
                _currentCode = RebindCodeTextBox.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(_currentEmail) || string.IsNullOrWhiteSpace(_currentCode))
                {
                    ShowError("يرجى إدخال البريد والكود");
                    return;
                }

                RebindButton.IsEnabled = false;
                
                var result = await _activationService.RebindSubscriptionCodeAsync(_currentEmail, _currentCode);
                
                if (result.IsSuccess)
                {
                    RebindSection.Visibility = Visibility.Collapsed;
                    VerificationSection.Visibility = Visibility.Visible;
                    ShowInfo("تم إرسال رمز التحقق إلى بريدك الإلكتروني");
                }
                else
                {
                    ShowError(result.Message);
                    RebindButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
                RebindButton.IsEnabled = true;
            }
        }

        private async void VerifyOtpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string otp = OtpTextBox.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(otp) || otp.Length != 6)
                {
                    ShowError("يرجى إدخال رمز التحقق الصحيح (6 أرقام)");
                    return;
                }

                VerifyOtpButton.IsEnabled = false;
                
                MacroFortActivationResult result = null;
                
                if (_activationType == MacroFortActivationType.CodeActivation)
                {
                    result = await _activationService.ConfirmCodeActivationAsync(_currentEmail, _currentCode, otp);
                }
                else if (_activationType == MacroFortActivationType.Rebind)
                {
                    var newHardwareId = _activationService.GenerateHardwareId();
                    result = await _activationService.ConfirmRebindAsync(_currentEmail, _currentCode, otp, newHardwareId);
                }
                else
                {
                    result = await _activationService.VerifyOtpAsync(_currentEmail, otp);
                }
                
                if (result.IsSuccess)
                {
                    ShowSuccess(result, _activationType);
                }
                else
                {
                    ShowError(result.Message);
                    VerifyOtpButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
                VerifyOtpButton.IsEnabled = true;
            }
        }

        private async void ResendOtpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResendOtpButton.IsEnabled = false;
                ShowInfo("جاري إعادة إرسال رمز التحقق...");
                
                await System.Threading.Tasks.Task.Delay(2000);
                OtpTextBox.Clear();
                OtpTextBox.Focus();
                
                ResendOtpButton.IsEnabled = true;
                ShowInfo("تم إعادة إرسال رمز التحقق");
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
                ResendOtpButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorMessageText.Text = $"❌ {message}";
            ErrorSection.Visibility = Visibility.Visible;
            SuccessSection.Visibility = Visibility.Collapsed;
        }

        private void ShowInfo(string message)
        {
            ErrorMessageText.Text = $"ℹ️ {message}";
            ErrorSection.Visibility = Visibility.Visible;
        }

        private void ShowSuccess(MacroFortActivationResult result, MacroFortActivationType type)
        {
            string typeText = type switch
            {
                MacroFortActivationType.Trial => "نسخة تجريبية",
                MacroFortActivationType.CodeActivation => "اشتراك",
                MacroFortActivationType.Rebind => "إعادة ربط",
                _ => "تفعيل"
            };

            SuccessMessageText.Text = $"✅ تم تفعيل {typeText} بنجاح!";
            SubscriptionDetailsText.Text = $"البريد: {result.Email}\n" +
                                           $"النوع: {result.SubscriptionType}\n" +
                                           $"الأيام المتبقية: {result.RemainingDays} يوم\n" +
                                           $"تاريخ الانتهاء: {result.ExpiryDate:yyyy-MM-dd}";

            VerificationSection.Visibility = Visibility.Collapsed;
            SuccessSection.Visibility = Visibility.Visible;
            
            System.Diagnostics.Debug.WriteLine($"تم التفعيل: {result.Message}");
            
            System.Diagnostics.Debug.WriteLine("🎉 سيتم إغلاق نافذة التفعيل وفتح البرنامج الرئيسي...");
            
            System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("⏰ Delay completed, invoking on Dispatcher...");
                    Dispatcher.Invoke(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("🔄 Getting App instance...");
                        var app = (App)Application.Current;
                        System.Diagnostics.Debug.WriteLine("📞 Calling app.ShowMainWindow()...");
                        app.ShowMainWindow();
                        
                        System.Diagnostics.Debug.WriteLine("🔄 تحديث معلومات الترخيص في الواجهة الرئيسية...");
                        var mainWindow = app.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.RefreshLicenseStatusFromDatabase();
                            System.Diagnostics.Debug.WriteLine("✓ تم طلب تحديث معلومات الترخيص");
                        }
                        
                        System.Diagnostics.Debug.WriteLine("🚪 Closing activation window...");
                        this.Close();
                        System.Diagnostics.Debug.WriteLine("✓ Activation window closed");
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error in activation completion: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }
    }
}
