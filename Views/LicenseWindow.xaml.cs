using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using MacroApp.Services;
using SR3H_MACRO.Services;

namespace MacroApp.Views
{
    public partial class LicenseWindow : Window
    {
        private readonly MacroFortActivationService _activationService;
        private readonly EmailService _emailService;
        private readonly Dictionary<string, string> _sentOTPs = new Dictionary<string, string>();
        private string _currentHardwareId;

        private readonly Dictionary<string, DateTime> _lastOTPRequest = new Dictionary<string, DateTime>();
        private const int MIN_OTP_INTERVAL_SECONDS = 60;
        private const int MAX_OTP_REQUESTS_PER_10MIN = 5;
        private const int THROTTLE_MINUTES = 15;

        public LicenseWindow()
        {
            InitializeComponent();
            
            _activationService = MacroFortActivationService.Instance;
            _emailService = new EmailService();
            
            LoadHardwareId();
            SetupUI();
        }

        private void LoadHardwareId()
        {
            try
            {
                _currentHardwareId = SafeHardwareIdService.GetFreshHardwareId();
                HardwareIdText.Text = _currentHardwareId;
                System.Diagnostics.Debug.WriteLine($"✓ Hardware ID (fresh): {_currentHardwareId}");
            }
            catch (Exception ex)
            {
                HardwareIdText.Text = $"خطأ: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في توليد Hardware ID: {ex.Message}");
            }
        }

        private void SetupUI()
        {
            FlowDirection = FlowDirection.RightToLeft;
        }

        private void ActivationType_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TrialPanel != null && CodePanel != null && TransferPanel != null && 
                    TrialRadio != null && CodeRadio != null && TransferRadio != null)
                {
                    TrialPanel.Visibility = TrialRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                    CodePanel.Visibility = CodeRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                    TransferPanel.Visibility = TransferRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تغيير نوع التفعيل: {ex.Message}");
            }
        }

        // ============ TRIAL SECTION ============
        private async void TrialSendOTP_Click(object sender, RoutedEventArgs e)
        {
            var email = TrialEmailBox.Text.Trim();
            
            if (!ValidateEmail(email))
            {
                ShowTrialStatus("يرجى إدخال بريد إلكتروني صحيح", false);
                return;
            }

            if (!CanRequestOTP(email))
            {
                ShowTrialStatus("يرجى الانتظار قبل طلب رمز جديد", false);
                return;
            }

            TrialSendOTPButton.IsEnabled = false;
            TrialSendOTPButton.Content = "جاري الإرسال...";

            try
            {
                _lastOTPRequest[email] = DateTime.UtcNow;
                
                var result = await _activationService.StartTrialAsync(email);
                
                if (result.IsSuccess)
                {
                    ShowTrialStatus("✓ تم إرسال الرمز إلى بريدك", true);
                    TrialOTPLabel.Visibility = Visibility.Visible;
                    TrialOTPBox.Visibility = Visibility.Visible;
                    TrialVerifyButton.Visibility = Visibility.Visible;
                    TrialOTPBox.Focus();
                }
                else
                {
                    ShowTrialStatus($"✗ {result.Message}", false);
                }
            }
            catch (Exception ex)
            {
                ShowTrialStatus($"✗ خطأ: {ex.Message}", false);
            }
            finally
            {
                TrialSendOTPButton.IsEnabled = true;
                TrialSendOTPButton.Content = "إرسال";
            }
        }

        private async void TrialVerify_Click(object sender, RoutedEventArgs e)
        {
            var email = TrialEmailBox.Text.Trim();
            var otp = TrialOTPBox.Text.Trim();

            if (!ValidateOTP(otp))
            {
                ShowTrialStatus("يرجى إدخال رمز صحيح", false);
                return;
            }

            TrialVerifyButton.IsEnabled = false;
            TrialVerifyButton.Content = "جاري التحقق...";

            try
            {
                var result = await _activationService.VerifyOtpAsync(email, otp);
                
                if (result.IsSuccess)
                {
                    var activation = new Services.ActivationData
                    {
                        Email = email,
                        HardwareId = _currentHardwareId,
                        SubscriptionType = "trial",
                        ActivationDate = DateTime.UtcNow,
                        ExpiryDate = DateTime.UtcNow.AddDays(7),
                        IsActive = true,
                        EmailVerified = true,
                        LastSync = DateTime.UtcNow
                    };

                    SessionActivationCache.SetCachedActivation(activation);
                    ShowTrialStatus("✓ تم تفعيل الفترة التجريبية بنجاح!", true);
                    
                    TrialVerifyButton.IsEnabled = true;
                    TrialVerifyButton.Content = "تحقق";
                    
                    await Task.Delay(2000);
                    this.Close();
                    var app = (App)Application.Current;
                    app?.ShowMainWindow();
                }
                else
                {
                    ShowTrialStatus($"✗ {result.Message}", false);
                    TrialVerifyButton.IsEnabled = true;
                    TrialVerifyButton.Content = "تحقق";
                }
            }
            catch (Exception ex)
            {
                ShowTrialStatus($"✗ خطأ: {ex.Message}", false);
                TrialVerifyButton.IsEnabled = true;
                TrialVerifyButton.Content = "تحقق";
            }
        }

        // ============ CODE SECTION ============
        private async void CodeSendOTP_Click(object sender, RoutedEventArgs e)
        {
            var email = CodeEmailBox.Text.Trim();
            var code = CodeActivationBox.Text.Trim();
            var orderId = CodeOrderIdBox.Text.Trim();

            if (!ValidateEmail(email))
            {
                ShowCodeStatus("يرجى إدخال بريد صحيح", false);
                return;
            }

            if (string.IsNullOrEmpty(code))
            {
                ShowCodeStatus("يرجى إدخال الكود", false);
                return;
            }

            if (string.IsNullOrEmpty(orderId))
            {
                ShowCodeStatus("يرجى إدخال رقم الطلب", false);
                return;
            }

            if (!CanRequestOTP(email))
            {
                ShowCodeStatus("يرجى الانتظار قبل طلب رمز جديد", false);
                return;
            }

            CodeSendOTPButton.IsEnabled = false;
            CodeSendOTPButton.Content = "جاري الإرسال...";

            try
            {
                _lastOTPRequest[email] = DateTime.UtcNow;
                
                var result = await _activationService.SendOtpForCodeActivationAsync(email, code);
                
                if (result.IsSuccess)
                {
                    ShowCodeStatus("✓ تم إرسال الرمز", true);
                    CodeOTPLabel.Visibility = Visibility.Visible;
                    CodeOTPBox.Visibility = Visibility.Visible;
                    CodeVerifyButton.Visibility = Visibility.Visible;
                    CodeOTPBox.Focus();
                }
                else
                {
                    ShowCodeStatus($"✗ {result.Message}", false);
                }
            }
            catch (Exception ex)
            {
                ShowCodeStatus($"✗ خطأ: {ex.Message}", false);
            }
            finally
            {
                CodeSendOTPButton.IsEnabled = true;
                CodeSendOTPButton.Content = "إرسال";
            }
        }

        private async void CodeVerify_Click(object sender, RoutedEventArgs e)
        {
            var email = CodeEmailBox.Text.Trim();
            var code = CodeActivationBox.Text.Trim();
            var otp = CodeOTPBox.Text.Trim();

            if (!ValidateOTP(otp))
            {
                ShowCodeStatus("يرجى إدخال رمز صحيح", false);
                return;
            }

            CodeVerifyButton.IsEnabled = false;
            CodeVerifyButton.Content = "جاري التحقق...";

            try
            {
                var result = await _activationService.ConfirmCodeActivationAsync(email, code, otp);
                
                if (result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("🔄 جلب البيانات الفعلية من الخادم بعد التفعيل...");
                    
                    var serverSubscription = await _activationService.GetSubscriptionByEmailAsync(email);
                    
                    if (serverSubscription != null)
                    {
                        var activation = new Services.ActivationData
                        {
                            Email = serverSubscription.Email,
                            HardwareId = serverSubscription.HardwareId,
                            SubscriptionType = serverSubscription.SubscriptionType,
                            SubscriptionCode = serverSubscription.SubscriptionCode ?? code,
                            ActivationDate = serverSubscription.ActivationDate,
                            ExpiryDate = serverSubscription.ExpiryDate,
                            IsActive = serverSubscription.IsActive,
                            EmailVerified = serverSubscription.EmailVerified,
                            LastSync = DateTime.UtcNow,
                            DeviceTransferCount = serverSubscription.DeviceTransferCount,
                            LastDeviceTransferDate = serverSubscription.LastDeviceTransferDate ?? DateTime.MinValue
                        };

                        SessionActivationCache.SetCachedActivation(activation);
                        
                        var remainingDays = (serverSubscription.ExpiryDate - DateTime.UtcNow).TotalDays;
                        ShowCodeStatus($"✓ تم تفعيل الاشتراك ({serverSubscription.SubscriptionType}) - متبقي {remainingDays:F0} أيام!", true);
                        
                        System.Diagnostics.Debug.WriteLine($"✓ تم حفظ البيانات من الخادم محلياً: {serverSubscription.SubscriptionType}, ينتهي في: {serverSubscription.ExpiryDate}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ لم يتم جلب البيانات من الخادم، استخدام البيانات المحسوبة محليًا");
                        
                        string subscriptionType = result.SubscriptionData?.SubscriptionType ?? "paid";
                        
                        DateTime expiryDate = DateTime.UtcNow;
                        if (result.ExpiryDate.HasValue && result.ExpiryDate > DateTime.UtcNow)
                        {
                            expiryDate = result.ExpiryDate.Value;
                        }
                        else
                        {
                            int durationDays = GetSubscriptionDurationDays(subscriptionType);
                            expiryDate = DateTime.UtcNow.AddDays(durationDays);
                        }

                        var activation = new Services.ActivationData
                        {
                            Email = email,
                            HardwareId = _currentHardwareId,
                            SubscriptionType = subscriptionType,
                            SubscriptionCode = code,
                            ActivationDate = DateTime.UtcNow,
                            ExpiryDate = expiryDate,
                            IsActive = true,
                            EmailVerified = true,
                            LastSync = DateTime.UtcNow
                        };

                        SessionActivationCache.SetCachedActivation(activation);

                        var remainingDays = (expiryDate - DateTime.UtcNow).TotalDays;
                        ShowCodeStatus($"✓ تم تفعيل الاشتراك ({subscriptionType}) - متبقي {remainingDays:F0} أيام!", true);
                    }
                    
                    await Task.Delay(2000);
                    var app = (App)Application.Current;
                    app?.ShowMainWindow();
                }
                else
                {
                    ShowCodeStatus($"✗ {result.Message}", false);
                }
            }
            catch (Exception ex)
            {
                ShowCodeStatus($"✗ خطأ: {ex.Message}", false);
            }
            finally
            {
                CodeVerifyButton.IsEnabled = true;
                CodeVerifyButton.Content = "تحقق";
            }
        }

        // ============ TRANSFER SECTION ============
        private async void TransferSendOTP_Click(object sender, RoutedEventArgs e)
        {
            var email = TransferEmailBox.Text.Trim();
            
            if (!ValidateEmail(email))
            {
                ShowTransferStatus("يرجى إدخال بريد صحيح", false);
                return;
            }

            if (!CanRequestOTP(email))
            {
                ShowTransferStatus("يرجى الانتظار قبل طلب رمز جديد", false);
                return;
            }

            TransferSendOTPButton.IsEnabled = false;
            TransferSendOTPButton.Content = "جاري الإرسال...";

            try
            {
                _lastOTPRequest[email] = DateTime.UtcNow;
                
                var result = await _activationService.RebindSubscriptionCodeAsync(email, "");
                
                if (result.IsSuccess)
                {
                    ShowTransferStatus("✓ تم إرسال الرمز", true);
                    TransferOTPLabel.Visibility = Visibility.Visible;
                    TransferOTPBox.Visibility = Visibility.Visible;
                    TransferVerifyButton.Visibility = Visibility.Visible;
                    TransferOTPBox.Focus();
                }
                else
                {
                    ShowTransferStatus($"✗ {result.Message}", false);
                }
            }
            catch (Exception ex)
            {
                ShowTransferStatus($"✗ خطأ: {ex.Message}", false);
            }
            finally
            {
                TransferSendOTPButton.IsEnabled = true;
                TransferSendOTPButton.Content = "إرسال";
            }
        }

        private string _transferEmail = "";
        private string _transferOldHardwareId = "";

        private async void TransferVerify_Click(object sender, RoutedEventArgs e)
        {
            var email = TransferEmailBox.Text.Trim();
            var otp = TransferOTPBox.Text.Trim();

            if (!ValidateOTP(otp))
            {
                ShowTransferStatus("يرجى إدخال رمز صحيح", false);
                return;
            }

            TransferVerifyButton.IsEnabled = false;
            TransferVerifyButton.Content = "جاري التحقق...";

            try
            {
                var result = await _activationService.VerifyOtpAsync(email, otp);
                if (!result.IsSuccess)
                {
                    ShowTransferStatus($"✗ {result.Message}", false);
                    return;
                }

                _transferEmail = email;
                _transferOldHardwareId = _currentHardwareId;

                TransferEmailBox.IsEnabled = false;
                TransferOTPBox.Clear();
                TransferOTPBox.IsEnabled = false;
                TransferSendOTPButton.IsEnabled = false;
                
                ShowTransferStatus("✓ تم التحقق من البريد بنجاح - يرجى إدخال الكود المفعل", true);

                TransferCodeLabel.Visibility = Visibility.Visible;
                TransferCodeBox.Visibility = Visibility.Visible;
                TransferCodeVerifyButton.Visibility = Visibility.Visible;
                TransferCodeBox.Focus();
            }
            catch (Exception ex)
            {
                ShowTransferStatus($"✗ خطأ: {ex.Message}", false);
            }
            finally
            {
                TransferVerifyButton.IsEnabled = true;
                TransferVerifyButton.Content = "تحقق";
            }
        }

        private async void TransferCodeVerify_Click(object sender, RoutedEventArgs e)
        {
            var code = TransferCodeBox.Text.Trim();

            if (string.IsNullOrEmpty(code))
            {
                ShowTransferStatus("يرجى إدخال الكود", false);
                return;
            }

            TransferCodeVerifyButton.IsEnabled = false;
            TransferCodeVerifyButton.Content = "جاري التحقق...";

            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 التحقق من الكود لإعادة الربط: {code}");

                var codeService = new SubscriptionCodeService();
                var verifyResult = await codeService.VerifyCodeForTransferAsync(code, _transferEmail);

                if (!verifyResult.Success)
                {
                    ShowTransferStatus($"✗ {verifyResult.Message}", false);
                    return;
                }

                if (!verifyResult.CanTransfer)
                {
                    ShowTransferStatus($"✗ {verifyResult.TransferLimitReason}", false);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"✓ الكود صحيح - متبقي: {verifyResult.TransfersRemaining} نقلات");

                var newHardwareId = SafeHardwareIdService.GetFreshHardwareId();
                var oldHardwareId = verifyResult.CurrentHardwareId;

                var completeResult = await codeService.CompleteDeviceTransferAsync(
                    code,
                    _transferEmail,
                    newHardwareId,
                    oldHardwareId
                );

                if (!completeResult.Success)
                {
                    ShowTransferStatus($"✗ {completeResult.Message}", false);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"✓ تم نقل الجهاز بنجاح - عمليات مستخدمة: {completeResult.TransfersUsed}/3");

                var subscription = await _activationService.GetSubscriptionByEmailAsync(_transferEmail);
                if (subscription != null)
                {
                    var activation = new Services.ActivationData
                    {
                        Email = subscription.Email,
                        HardwareId = subscription.HardwareId,
                        SubscriptionType = subscription.SubscriptionType,
                        SubscriptionCode = subscription.SubscriptionCode ?? code,
                        ActivationDate = subscription.ActivationDate,
                        ExpiryDate = subscription.ExpiryDate,
                        IsActive = subscription.IsActive,
                        EmailVerified = subscription.EmailVerified,
                        LastSync = DateTime.UtcNow,
                        DeviceTransferCount = subscription.DeviceTransferCount,
                        LastDeviceTransferDate = subscription.LastDeviceTransferDate ?? DateTime.MinValue
                    };

                    SessionActivationCache.SetCachedActivation(activation);

                    var remainingDays = (subscription.ExpiryDate - DateTime.UtcNow).TotalDays;
                    ShowTransferStatus($"✓ تم نقل الاشتراك بنجاح! ({subscription.SubscriptionType}) - متبقي {remainingDays:F0} أيام", true);
                }
                else
                {
                    ShowTransferStatus("✓ تم نقل الجهاز بنجاح لكن فشل جلب البيانات من الخادم", true);
                }

                await Task.Delay(2000);
                ResetTransferUI();
                var app = (App)Application.Current;
                app?.ShowMainWindow();
            }
            catch (Exception ex)
            {
                ShowTransferStatus($"✗ خطأ: {ex.Message}", false);
            }
            finally
            {
                TransferCodeVerifyButton.IsEnabled = true;
                TransferCodeVerifyButton.Content = "تحقق";
            }
        }

        private void ResetTransferUI()
        {
            TransferEmailBox.Clear();
            TransferEmailBox.IsEnabled = true;
            TransferOTPBox.Clear();
            TransferOTPBox.IsEnabled = true;
            TransferOTPLabel.Visibility = Visibility.Collapsed;
            TransferOTPBox.Visibility = Visibility.Collapsed;
            TransferVerifyButton.Visibility = Visibility.Visible;
            TransferCodeLabel.Visibility = Visibility.Collapsed;
            TransferCodeBox.Clear();
            TransferCodeBox.Visibility = Visibility.Collapsed;
            TransferCodeVerifyButton.Visibility = Visibility.Collapsed;
            TransferSendOTPButton.IsEnabled = true;
            _transferEmail = "";
            _transferOldHardwareId = "";
        }

        // ============ HELPER METHODS ============
        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateOTP(string otp)
        {
            return !string.IsNullOrEmpty(otp) && otp.Length == 6 && otp.All(char.IsDigit);
        }

        private string GenerateOTP()
        {
            var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            byte[] tokenData = new byte[4];
            rng.GetBytes(tokenData);
            int otp = (BitConverter.ToInt32(tokenData, 0) & 0x7FFFFFFF) % 1000000;
            return otp.ToString("D6");
        }

        private bool CanRequestOTP(string email)
        {
            if (!_lastOTPRequest.ContainsKey(email))
                return true;

            var timeSinceLastRequest = DateTime.UtcNow - _lastOTPRequest[email];
            return timeSinceLastRequest.TotalSeconds >= MIN_OTP_INTERVAL_SECONDS;
        }

        private void ShowTrialStatus(string message, bool isSuccess)
        {
            TrialStatus.Text = message;
            TrialStatus.Foreground = isSuccess ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            TrialStatus.Visibility = Visibility.Visible;
        }

        private void ShowCodeStatus(string message, bool isSuccess)
        {
            CodeStatus.Text = message;
            CodeStatus.Foreground = isSuccess ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            CodeStatus.Visibility = Visibility.Visible;
        }

        private void ShowTransferStatus(string message, bool isSuccess)
        {
            TransferStatus.Text = message;
            TransferStatus.Foreground = isSuccess ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            TransferStatus.Visibility = Visibility.Visible;
        }

        private int GetSubscriptionDurationDays(string subscriptionType)
        {
            return subscriptionType?.ToLower() switch
            {
                "trial" => 7,
                "month" => 30,
                "semi" => 180,
                "year" => 365,
                "lifetime" => 36500,
                _ => 30
            };
        }
    }
}
