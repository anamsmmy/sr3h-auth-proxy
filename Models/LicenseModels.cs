using System;

namespace MacroApp.Services
{
    public class ActivationRequest
    {
        public string Email { get; set; }
        public string OrderId { get; set; }
        public string HardwareId { get; set; }
        public bool IsReset { get; set; }
    }

    public class ActivationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public LicenseInfo LicenseInfo { get; set; }
        
        // خصائص إضافية مطلوبة
        public string Message => IsSuccess ? "تم التفعيل بنجاح" : ErrorMessage;
        public DateTime ExpiryDate => LicenseInfo?.ExpiryDate ?? DateTime.UtcNow.AddDays(30);
        public bool RequiresDeviceRebind { get; set; } = false;
    }

    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public LicenseInfo LicenseInfo { get; set; }
    }

    public class LicenseInfo
    {
        public string Email { get; set; }
        public string OrderId { get; set; }
        public string HardwareId { get; set; }
        public DateTime ActivationDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        
        // خصائص إضافية مطلوبة
        public bool IsActivated => IsActive;
        public DateTime LastCheck { get; set; } = DateTime.UtcNow;
        public int DaysUntilExpiry => Math.Max(0, (int)(ExpiryDate - DateTime.UtcNow).TotalDays);
        public string Message => IsActive ? "الترخيص نشط" : "الترخيص غير نشط";
    }
}