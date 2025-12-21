using System;
using MacroApp.Services;

namespace SR3H_MACRO.Services
{
    /// <summary>
    /// Cache تفعيل الجهاز في الذاكرة فقط (بدون ملفات)
    /// البيانات تُفقد عند إغلاق التطبيق
    /// آمن ضد الاستغلالات لأن:
    /// - لا توجد ملفات يمكن تعديلها
    /// - grace period محدود (30 دقيقة)
    /// - كل بدء تطبيق جديد = تحقق سيرفر إجباري
    /// </summary>
    public static class SessionActivationCache
    {
        private static ActivationData _cachedData = null;
        private static DateTime _lastServerCheckTime = DateTime.MinValue;
        private const int GRACE_PERIOD_MINUTES = 5;
        private static string _hardwareVerificationStatus = "pending";
        private static DateTime? _gracePeriodExpiresAt = null;

        /// <summary>
        /// الحصول على بيانات التفعيل المحفوظة في الذاكرة
        /// إذا تجاوزت المدة المسموحة (grace period) = null (إجبار تحقق جديد)
        /// </summary>
        public static ActivationData GetCachedActivation()
        {
            if (_cachedData == null)
            {
                System.Diagnostics.Debug.WriteLine("📭 لا توجد بيانات في cache الذاكرة");
                return null;
            }

            var timeElapsed = DateTime.UtcNow - _lastServerCheckTime;
            
            if (timeElapsed.TotalMinutes > GRACE_PERIOD_MINUTES)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⏳ انتهت فترة الصلاحية المحلية ({GRACE_PERIOD_MINUTES} دقائق)");
                _cachedData = null;
                return null;
            }

            System.Diagnostics.Debug.WriteLine(
                $"✅ استخدام البيانات المحفوظة في الذاكرة - متبقي: {GRACE_PERIOD_MINUTES - (int)timeElapsed.TotalMinutes} دقائق");
            
            return _cachedData;
        }

        /// <summary>
        /// حفظ بيانات التفعيل في الذاكرة (بعد تحقق سيرفر ناجح)
        /// يتم فقط بعد تحقق السيرفر الناجح
        /// </summary>
        public static void SetCachedActivation(ActivationData data)
        {
            if (data == null)
            {
                Clear();
                return;
            }

            _cachedData = data;
            _lastServerCheckTime = DateTime.UtcNow;
            
            System.Diagnostics.Debug.WriteLine(
                $"💾 تم حفظ بيانات التفعيل في الذاكرة للبريد: {data.Email}");
            System.Diagnostics.Debug.WriteLine(
                $"   صلاحية الترخيص: {_cachedData.ExpiryDate:yyyy-MM-dd}");
            System.Diagnostics.Debug.WriteLine(
                $"   انتهاء الفترة المحلية المسموحة: {_lastServerCheckTime.AddMinutes(GRACE_PERIOD_MINUTES):yyyy-MM-dd HH:mm:ss}");
        }

        /// <summary>
        /// مسح البيانات من الذاكرة (مثل logout)
        /// </summary>
        public static void Clear()
        {
            _cachedData = null;
            _lastServerCheckTime = DateTime.MinValue;
            System.Diagnostics.Debug.WriteLine("🗑️ تم مسح بيانات التفعيل من الذاكرة");
        }

        /// <summary>
        /// التحقق مما إذا كانت هناك بيانات محفوظة
        /// </summary>
        public static bool HasCachedActivation()
        {
            return GetCachedActivation() != null;
        }

        /// <summary>
        /// الحصول على الوقت المتبقي من grace period (بالدقائق)
        /// -1 إذا انتهت الفترة
        /// </summary>
        public static int GetRemainingGracePeriodMinutes()
        {
            if (_cachedData == null)
                return -1;

            var timeElapsed = DateTime.UtcNow - _lastServerCheckTime;
            var remaining = GRACE_PERIOD_MINUTES - (int)timeElapsed.TotalMinutes;
            
            return remaining > 0 ? remaining : -1;
        }

        /// <summary>
        /// تحديث حالة التحقق من الأجهزة
        /// </summary>
        public static void SetHardwareVerificationStatus(string status)
        {
            _hardwareVerificationStatus = status;
            System.Diagnostics.Debug.WriteLine($"🔐 تم تحديث حالة التحقق من الجهاز: {status}");
        }

        /// <summary>
        /// الحصول على حالة التحقق من الأجهزة
        /// </summary>
        public static string GetHardwareVerificationStatus()
        {
            return _hardwareVerificationStatus;
        }

        /// <summary>
        /// تحديث فترة الرحمة
        /// </summary>
        public static void SetGracePeriodExpiry(DateTime expiryTime)
        {
            _gracePeriodExpiresAt = expiryTime;
            System.Diagnostics.Debug.WriteLine($"⏰ تم تحديث انتهاء فترة الرحمة: {expiryTime:yyyy-MM-dd HH:mm:ss}");
        }

        /// <summary>
        /// الحصول على وقت انتهاء فترة الرحمة
        /// </summary>
        public static DateTime? GetGracePeriodExpiry()
        {
            return _gracePeriodExpiresAt;
        }

        /// <summary>
        /// التحقق مما إذا كانت فترة الرحمة لا تزال سارية
        /// </summary>
        public static bool IsGracePeriodActive()
        {
            if (!_gracePeriodExpiresAt.HasValue)
                return false;

            if (DateTime.UtcNow > _gracePeriodExpiresAt.Value)
            {
                _gracePeriodExpiresAt = null;
                return false;
            }

            return true;
        }
    }
}
