using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace MacroApp.Services
{
    /// <summary>
    /// ⚠️ نموذج البيانات الأساسي فقط
    /// لا يتم الحفظ المحلي - البيانات تُحفظ في الذاكرة فقط عبر SessionActivationCache
    /// </summary>
    public class ActivationData
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }

        [JsonProperty("subscription_type")]
        public string SubscriptionType { get; set; }

        [JsonProperty("subscription_code")]
        public string SubscriptionCode { get; set; }

        [JsonProperty("activation_date")]
        public DateTime ActivationDate { get; set; }

        [JsonProperty("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonProperty("last_sync")]
        public DateTime LastSync { get; set; }

        [JsonProperty("device_transfer_count")]
        public int DeviceTransferCount { get; set; }

        [JsonProperty("last_device_transfer_date")]
        public DateTime LastDeviceTransferDate { get; set; }

        [JsonProperty("max_devices")]
        public int MaxDevices { get; set; } = 10;
    }

    /// <summary>
    /// ⚠️ هذا الفئة قد تم حذفها من النظام
    /// استخدم SessionActivationCache بدلاً منها
    /// 
    /// السبب:
    /// - إزالة احتمالية تعديل ملفات التفعيل محلياً
    /// - منع الاستغلالات الأمنية (تمديد النسخة التجريبية، نسخ الترخيص)
    /// - فرض تحقق السيرفر الإجباري عند كل بدء تطبيق جديد
    /// - Grace period آمن (30 دقيقة) فقط عند قطع الإنترنت
    /// </summary>
    public class ActivationDataStore
    {
        [Obsolete("استخدم SessionActivationCache بدلاً من هذا الفئة")]
        private const string ENCRYPTION_SALT = "SR3H_MACRO_ACTIVATION_2025";
        [Obsolete("استخدم SessionActivationCache بدلاً من هذا الفئة")]
        private const int PBKDF2_ITERATIONS = 480000;

        [Obsolete("استخدم SessionActivationCache.SetCachedActivation بدلاً من هذه الدالة")]
        public void SaveActivation(ActivationData data)
        {
            throw new NotImplementedException("تم حذف هذه الدالة. استخدم SessionActivationCache.SetCachedActivation");
        }

        [Obsolete("استخدم SessionActivationCache.GetCachedActivation بدلاً من هذه الدالة")]
        public ActivationData LoadActivation(string hardwareId)
        {
            throw new NotImplementedException("تم حذف هذه الدالة. استخدم SessionActivationCache.GetCachedActivation");
        }

        [Obsolete("استخدم SessionActivationCache.Clear بدلاً من هذه الدالة")]
        public void DeleteActivation()
        {
            throw new NotImplementedException("تم حذف هذه الدالة. استخدم SessionActivationCache.Clear");
        }

        [Obsolete("استخدم SessionActivationCache.HasCachedActivation بدلاً من هذه الدالة")]
        public bool ActivationExists()
        {
            throw new NotImplementedException("تم حذف هذه الدالة. استخدم SessionActivationCache.HasCachedActivation");
        }

        [Obsolete("تم حذف هذه الدالة")]
        public bool IsActivationValid(string hardwareId)
        {
            throw new NotImplementedException("تم حذف هذه الدالة");
        }
    }
}
