using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SR3H_MACRO.Services;

namespace MacroApp.Services
{
    public class BackgroundValidationScheduler
    {
        private readonly ServerValidationService _serverValidationService;
        private readonly string _email;
        private readonly string _hardwareId;
        private Timer _validationTimer;
        private Timer _gracePeriodTimer;
        private DateTime _lastVerificationTime;
        private DateTime _internetConnectionLostTime = DateTime.MinValue;
        private bool _isValidationInProgress = false;
        private bool _gracePeriodActive = false;
        private int _gracePeriodCountdown = 300;

        public event EventHandler<ValidationStateChangedEventArgs> ValidationStateChanged;

        public BackgroundValidationScheduler(string email, string hardwareId)
        {
            _email = email;
            _hardwareId = hardwareId;
            _serverValidationService = new ServerValidationService();
            _lastVerificationTime = DateTime.UtcNow;
        }

        public void Start()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("✓ تم بدء مجدول التحقق من الخلفية");

                _validationTimer = new Timer(
                    async state => await PerformValidationAsync(),
                    null,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromHours(1)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في بدء المجدول: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                if (_validationTimer != null)
                {
                    _validationTimer.Dispose();
                    System.Diagnostics.Debug.WriteLine("✓ تم إيقاف مجدول التحقق");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في إيقاف المجدول: {ex.Message}");
            }
        }

        private async Task PerformValidationAsync()
        {
            if (_isValidationInProgress)
                return;

            try
            {
                _isValidationInProgress = true;

                var hoursSinceLastVerification = (DateTime.UtcNow - _lastVerificationTime).TotalHours;

                if (hoursSinceLastVerification < 24)
                    return;

                System.Diagnostics.Debug.WriteLine("🔄 جاري إجراء فحص التحقق الدوري من الترخيص...");

                var result = await _serverValidationService.PeriodicVerifyAsync(_email, _hardwareId);

                if (result.Success)
                {
                    _lastVerificationTime = DateTime.UtcNow;
                    
                    if (_gracePeriodActive)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ تم استعادة الاتصال بالإنترنت");
                        StopGracePeriod();
                        _internetConnectionLostTime = DateTime.MinValue;
                    }

                    System.Diagnostics.Debug.WriteLine($"✓ تم التحقق من الترخيص بنجاح - النوع: {result.SubscriptionType}");

                    var activation = SessionActivationCache.GetCachedActivation();
                    if (activation != null)
                    {
                        activation.LastSync = DateTime.UtcNow;
                        activation.IsActive = result.IsActive;
                        activation.SubscriptionType = result.SubscriptionType;
                        if (result.ExpiryDate.HasValue)
                            activation.ExpiryDate = result.ExpiryDate.Value;

                        // تحديث حالة التحقق من الأجهزة
                        SessionActivationCache.SetHardwareVerificationStatus("verified");
                        SessionActivationCache.SetGracePeriodExpiry(DateTime.UtcNow.AddMinutes(5));
                        
                        System.Diagnostics.Debug.WriteLine($"🔐 تم تحديث حالة التحقق من الجهاز: verified");
                        
                        SessionActivationCache.SetCachedActivation(activation);
                    }

                    ValidationStateChanged?.Invoke(this, new ValidationStateChangedEventArgs 
                    { 
                        IsValid = true, 
                        Message = "✓ تم التحقق من الترخيص بنجاح" 
                    });
                }
                else if (result.SubscriptionExpired)
                {
                    System.Diagnostics.Debug.WriteLine("❌ انتهت صلاحية الاشتراك");
                    ValidationStateChanged?.Invoke(this, new ValidationStateChangedEventArgs 
                    { 
                        IsValid = false, 
                        Message = "❌ انتهت صلاحية الاشتراك" 
                    });
                }
                else if (result.Message.Contains("لا يوجد اتصال"))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ لا يوجد اتصال بالإنترنت");

                    if (_internetConnectionLostTime == DateTime.MinValue)
                    {
                        _internetConnectionLostTime = DateTime.UtcNow;
                        System.Diagnostics.Debug.WriteLine("🔴 بدء فترة الرحمة 5 دقائق");
                        StartGracePeriod();
                    }

                    var timeSinceConnectionLost = (DateTime.UtcNow - _internetConnectionLostTime).TotalSeconds;
                    var remainingSeconds = 300 - timeSinceConnectionLost;

                    if (remainingSeconds > 0)
                    {
                        _gracePeriodCountdown = (int)remainingSeconds;
                        System.Diagnostics.Debug.WriteLine($"⏱️ وقت متبقي من فترة الرحمة: {remainingSeconds:F0} ثانية");
                        ValidationStateChanged?.Invoke(this, new ValidationStateChangedEventArgs 
                        { 
                            IsValid = true, 
                            Message = $"⚠️ لا يوجد اتصال بالإنترنت - وقت متبقي: {_gracePeriodCountdown} ثانية" 
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ انتهت فترة الرحمة - سيتم إيقاف التطبيق");
                        StopGracePeriod();
                        _internetConnectionLostTime = DateTime.MinValue;
                        
                        ValidationStateChanged?.Invoke(this, new ValidationStateChangedEventArgs 
                        { 
                            IsValid = false, 
                            Message = "❌ انقطع الإنترنت - تم إيقاف التطبيق" 
                        });

                        ShutdownApplication();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ فشل التحقق: {result.Message}");
                    ValidationStateChanged?.Invoke(this, new ValidationStateChangedEventArgs 
                    { 
                        IsValid = false, 
                        Message = result.Message 
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في مجدول التحقق: {ex.Message}");
            }
            finally
            {
                _isValidationInProgress = false;
            }
        }

        public async Task<bool> PerformImmediateValidationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 جاري التحقق الفوري من الترخيص...");

                var result = await _serverValidationService.ValidateSubscriptionAsync(_email, _hardwareId);

                if (result.Success)
                {
                    _lastVerificationTime = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine("✓ تم التحقق من الترخيص بنجاح");
                    return true;
                }
                else if (result.Message.Contains("لا يوجد اتصال"))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ لا يوجد اتصال بالإنترنت");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ فشل التحقق: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في التحقق الفوري: {ex.Message}");
                return false;
            }
        }

        public bool CanMacroRun()
        {
            var timeSinceLastSuccessfulVerification = (DateTime.UtcNow - _lastVerificationTime).TotalHours;

            if (timeSinceLastSuccessfulVerification > 24)
            {
                System.Diagnostics.Debug.WriteLine("❌ تجاوزت مدة التحقق - الماكرو معطل");
                return false;
            }

            return true;
        }

        private void StartGracePeriod()
        {
            try
            {
                _gracePeriodActive = true;
                _gracePeriodCountdown = 300;
                _gracePeriodTimer = new Timer(
                    state => UpdateGracePeriodCountdown(),
                    null,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في بدء فترة الرحمة: {ex.Message}");
            }
        }

        private void StopGracePeriod()
        {
            try
            {
                _gracePeriodActive = false;
                if (_gracePeriodTimer != null)
                {
                    _gracePeriodTimer.Dispose();
                    System.Diagnostics.Debug.WriteLine("✓ تم إيقاف عداد فترة الرحمة");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في إيقاف فترة الرحمة: {ex.Message}");
            }
        }

        private void UpdateGracePeriodCountdown()
        {
            if (_gracePeriodActive && _gracePeriodCountdown > 0)
            {
                _gracePeriodCountdown--;
                if (_gracePeriodCountdown % 10 == 0 || _gracePeriodCountdown <= 10)
                {
                    System.Diagnostics.Debug.WriteLine($"⏱️ وقت متبقي من فترة الرحمة: {_gracePeriodCountdown} ثانية");
                }
            }
        }

        private void ShutdownApplication()
        {
            try
            {
                MessageBox.Show(
                    "انقطع الاتصال بالإنترنت لمدة 5 دقائق.\nسيتم إغلاق التطبيق الآن.",
                    "انقطاع الإنترنت",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                System.Diagnostics.Debug.WriteLine("🔴 إيقاف التطبيق بسبب انقطاع الإنترنت");
                Stop();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في إيقاف التطبيق: {ex.Message}");
            }
        }
    }

    public class ValidationStateChangedEventArgs : EventArgs
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
}
