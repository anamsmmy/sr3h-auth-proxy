using System;
using System.Threading.Tasks;

namespace MacroApp.Services
{
    public class ActivationService
    {
        private static ActivationService _instance;
        private bool _isActivated = false;
        private DateTime _lastActivation = DateTime.MinValue;

        public static ActivationService Instance => _instance ??= new ActivationService();

        public bool IsActivated => _isActivated;
        public DateTime LastActivation => _lastActivation;

        private ActivationService()
        {
            // تفعيل تلقائي للاختبار
            _isActivated = true;
            _lastActivation = DateTime.Now;
        }

        public async Task<bool> ActivateAsync()
        {
            try
            {
                // محاكاة عملية التفعيل
                await Task.Delay(1000); // محاكاة تأخير الشبكة
                
                _isActivated = true;
                _lastActivation = DateTime.Now;
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Activation error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateActivationAsync()
        {
            try
            {
                await Task.Delay(100); // محاكاة التحقق
                return _isActivated;
            }
            catch
            {
                return _isActivated;
            }
        }

        public void ResetActivation()
        {
            _isActivated = false;
            _lastActivation = DateTime.MinValue;
        }

        public string GetActivationStatus()
        {
            if (_isActivated)
            {
                return $"نشط منذ: {_lastActivation:yyyy-MM-dd HH:mm:ss}";
            }
            return "غير نشط";
        }
    }

    // نموذج لجدول التفعيل (اختياري)
    public class ActivationLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime ActivationTime { get; set; }
        public string AppVersion { get; set; }
        public string MachineId { get; set; }
    }
}