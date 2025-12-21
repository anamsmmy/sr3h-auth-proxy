using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace MacroApp.Services
{
    /// <summary>
    /// خدمة الأمان والحماية من التلاعب
    /// </summary>
    public class SecurityService
    {
        private static readonly string[] SuspiciousProcesses = {
            "cheatengine", "ce", "artmoney", "speedhack", "gameguardian",
            "ollydbg", "x64dbg", "ida", "wireshark", "fiddler", "processhacker",
            "procmon", "regmon", "filemon", "apimonitor", "detours"
        };

        public static bool DevelopmentMode { get; set; } = false;
        public static List<string> SecurityLog { get; private set; } = new List<string>();

        /// <summary>
        /// فحص البيئة للتأكد من عدم وجود أدوات تلاعب
        /// </summary>
        public static SecurityCheckResult PerformSecurityCheck()
        {
            var result = new SecurityCheckResult();
            SecurityLog.Clear();

            try
            {
                LogSecurity("بدء فحص الأمان...");

                // فحص العمليات المشبوهة
                var suspiciousProcesses = GetSuspiciousProcesses();
                if (suspiciousProcesses.Any())
                {
                    result.SuspiciousProcesses = suspiciousProcesses;
                    result.IsOverallSecure = false;
                    LogSecurity($"تم العثور على عمليات مشبوهة: {string.Join(", ", suspiciousProcesses)}");
                }

                // فحص تكامل التطبيق
                if (!VerifyApplicationIntegrity())
                {
                    result.IntegrityIssues.Add("فشل في التحقق من تكامل التطبيق");
                    result.IsOverallSecure = false;
                    LogSecurity("فشل في التحقق من تكامل التطبيق");
                }

                // فحص المتغيرات البيئية المشبوهة
                var suspiciousEnvVars = GetSuspiciousEnvironmentVariables();
                if (suspiciousEnvVars.Any())
                {
                    result.SuspiciousEnvironmentVariables = suspiciousEnvVars;
                    result.IsOverallSecure = false;
                    LogSecurity($"متغيرات بيئية مشبوهة: {string.Join(", ", suspiciousEnvVars)}");
                }

                if (result.IsOverallSecure)
                {
                    LogSecurity("فحص الأمان مكتمل - البيئة آمنة");
                }

                // في وضع التطوير، نتجاهل مشاكل الأمان
                if (DevelopmentMode)
                {
                    LogSecurity("وضع التطوير مفعل - تجاهل مشاكل الأمان");
                    result.IsOverallSecure = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSecurity($"خطأ في فحص الأمان: {ex.Message}");
                result.IsOverallSecure = DevelopmentMode; // في وضع التطوير نسمح بالمتابعة
                result.IntegrityIssues.Add($"خطأ في فحص الأمان: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// فحص البيئة للتأكد من عدم وجود أدوات تلاعب (للتوافق مع الكود القديم)
        /// </summary>
        public static bool IsEnvironmentSecure()
        {
            return PerformSecurityCheck().IsOverallSecure;
        }

        /// <summary>
        /// الحصول على قائمة العمليات المشبوهة
        /// </summary>
        private static List<string> GetSuspiciousProcesses()
        {
            var suspiciousFound = new List<string>();
            
            try
            {
                var processes = Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        var processName = process.ProcessName.ToLower();
                        foreach (var suspicious in SuspiciousProcesses)
                        {
                            if (processName.Contains(suspicious))
                            {
                                suspiciousFound.Add($"{process.ProcessName} (PID: {process.Id})");
                            }
                        }
                    }
                    catch
                    {
                        // تجاهل الأخطاء في قراءة العمليات
                    }
                }
            }
            catch (Exception ex)
            {
                LogSecurity($"خطأ في فحص العمليات: {ex.Message}");
            }
            
            return suspiciousFound;
        }

        /// <summary>
        /// فحص وجود عمليات مشبوهة (للتوافق مع الكود القديم)
        /// </summary>
        private static bool HasSuspiciousProcesses()
        {
            return GetSuspiciousProcesses().Any();
        }

        /// <summary>
        /// التحقق من تكامل التطبيق
        /// </summary>
        private static bool VerifyApplicationIntegrity()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var location = assembly.Location;
                
                if (string.IsNullOrEmpty(location) || !File.Exists(location))
                    return false;

                // فحص التوقيع الرقمي (إذا كان متوفراً)
                // يمكن إضافة فحص إضافي هنا

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// الحصول على المتغيرات البيئية المشبوهة
        /// </summary>
        private static List<string> GetSuspiciousEnvironmentVariables()
        {
            var suspiciousFound = new List<string>();
            
            try
            {
                var suspiciousVars = new[] { "COR_ENABLE_PROFILING", "COR_PROFILER", "DOTNET_STARTUP_HOOKS" };
                
                foreach (var varName in suspiciousVars)
                {
                    var value = Environment.GetEnvironmentVariable(varName);
                    if (!string.IsNullOrEmpty(value))
                    {
                        suspiciousFound.Add($"{varName}={value}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogSecurity($"خطأ في فحص المتغيرات البيئية: {ex.Message}");
            }
            
            return suspiciousFound;
        }

        /// <summary>
        /// فحص المتغيرات البيئية المشبوهة (للتوافق مع الكود القديم)
        /// </summary>
        private static bool HasSuspiciousEnvironmentVariables()
        {
            return GetSuspiciousEnvironmentVariables().Any();
        }

        /// <summary>
        /// تسجيل رسالة أمنية
        /// </summary>
        private static void LogSecurity(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            SecurityLog.Add(logEntry);
            
            // كتابة إلى ملف السجل
            try
            {
                var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logsDir))
                    Directory.CreateDirectory(logsDir);
                
                var logFile = Path.Combine(logsDir, $"security_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch
            {
                // تجاهل أخطاء الكتابة
            }
        }

        /// <summary>
        /// الحصول على سجل الأمان
        /// </summary>
        public static string GetSecurityLogAsString()
        {
            return string.Join(Environment.NewLine, SecurityLog);
        }

        /// <summary>
        /// توليد بصمة فريدة للتطبيق
        /// </summary>
        public static string GenerateApplicationFingerprint()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var location = assembly.Location;
                var version = assembly.GetName().Version?.ToString() ?? "1.0.0.0";
                
                var fingerprint = $"{location}|{version}|{Environment.MachineName}|{Environment.UserName}";
                
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch
            {
                return "UNKNOWN_FINGERPRINT";
            }
        }

        /// <summary>
        /// فحص صحة بيانات التشفير
        /// </summary>
        public static bool ValidateEncryptionIntegrity()
        {
            try
            {
                // اختبار تشفير وفك تشفير بسيط
                var testData = "SR3H_MACRO_TEST_2024";
                var encrypted = EncryptionService.Encrypt(testData);
                var decrypted = EncryptionService.Decrypt(encrypted);
                
                return testData == decrypted;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// تسجيل محاولة تلاعب مشبوهة
        /// </summary>
        public static void LogSuspiciousActivity(string activity)
        {
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MacroApp", "security.log");
                
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SUSPICIOUS: {activity}{Environment.NewLine}";
                
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // تجاهل أخطاء التسجيل
            }
        }


    }

    /// <summary>
    /// نتيجة فحص الأمان
    /// </summary>
    public class SecurityCheckResult
    {
        public bool IsOverallSecure { get; set; } = true;
        public List<string> SuspiciousProcesses { get; set; } = new List<string>();
        public List<string> SuspiciousEnvironmentVariables { get; set; } = new List<string>();
        public List<string> IntegrityIssues { get; set; } = new List<string>();
        public DateTime CheckTimestamp { get; set; } = DateTime.Now;
        
        public string GetDetailedMessage()
        {
            if (IsOverallSecure)
                return "البيئة آمنة ومحمية";
            
            var issues = new List<string>();
            
            if (SuspiciousProcesses.Any())
                issues.Add($"عمليات مشبوهة: {string.Join(", ", SuspiciousProcesses)}");
            
            if (SuspiciousEnvironmentVariables.Any())
                issues.Add($"متغيرات بيئية مشبوهة: {string.Join(", ", SuspiciousEnvironmentVariables)}");
            
            if (IntegrityIssues.Any())
                issues.Add($"مشاكل التكامل: {string.Join(", ", IntegrityIssues)}");
            
            return $"تحذير أمني: {string.Join(" | ", issues)}";
        }

        public string GetSimpleMessage()
        {
            if (IsOverallSecure)
                return "البيئة آمنة";
            
            return "تم اكتشاف أدوات تلاعب مشبوهة";
        }
    }
}