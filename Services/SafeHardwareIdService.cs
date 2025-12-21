using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Linq;
using System.Management;

namespace SR3H_MACRO.Services
{
    /// <summary>
    /// Device Fingerprint Service - توليد معرف جهاز آمن بناءً على عناصر فيزيائية
    /// يستخدم SHA-256 hash من: MachineGuid + BIOS UUID + Disk Serial + CPU ID
    /// غير قابل للتحايل عبر حذف ملفات محلية
    /// </summary>
    public class SafeHardwareIdService
    {
        private const string HARDWARE_ID_CACHE_FILE = "device_id.cache";
        private const string APP_SALT = "SR3H_MACRO_v2_SECURITY_HASH_2025";

        /// <summary>
        /// توليد معرف فريد للجهاز - يقرأ من cache (للأداء فقط - ليس أمني)
        /// للعمليات الأمنية: استخدم GetFreshHardwareId() بدلاً من هذا
        /// </summary>
        public static string GenerateHardwareId()
        {
            try
            {
                // محاولة قراءة المعرف المحفوظ أولاً (للأداء فقط)
                var cachedId = GetCachedHardwareId();
                if (!string.IsNullOrEmpty(cachedId))
                {
                    return cachedId;
                }

                // توليد معرف جديد
                var hardwareId = GenerateNewHardwareId();
                
                // حفظ المعرف للاستخدام المستقبلي
                SaveHardwareIdToCache(hardwareId);
                
                return hardwareId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating hardware ID: {ex.Message}");
                
                // في حالة الفشل، استخدم معرف بديل
                return GenerateFallbackHardwareId();
            }
        }

        /// <summary>
        /// ✓ توليد Device Fingerprint Hash طازج من خصائص الجهاز الفعلية
        /// لا يعتمد على cache - يُولد من MachineGuid + BIOS + Disk + CPU في كل مرة
        /// استخدم هذا للعمليات الأمنية: Trial validation, Transfer verification
        /// </summary>
        public static string GetFreshHardwareId()
        {
            try
            {
                var hardwareId = GenerateNewHardwareId();
                return hardwareId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating fresh hardware ID: {ex.Message}");
                return GenerateFallbackHardwareId();
            }
        }

        /// <summary>
        /// توليد Device Fingerprint Hash من عناصر فيزيائية قوية فقط
        /// المكونات (بالترتيب من الأقوى للأضعف):
        /// 1. Disk Serial 1 (قوة: 9/10)
        /// 2. Disk Serial 2 (قوة: 9/10)
        /// 3. CPU ProcessorId 1 (قوة: 9/10)
        /// 4. CPU ProcessorId 2 (قوة: 9/10)
        /// 5. BIOS UUID (قوة: 7/10)
        /// ✗ Removed: MachineGuid (عُرضة للتعديل)
        /// </summary>
        private static string GenerateNewHardwareId()
        {
            var components = new StringBuilder();

            try
            {
                // 1. Primary Disk Serial (مهم جداً)
                var diskSerial1 = GetDiskSerialNumber();
                System.Diagnostics.Debug.WriteLine($"[Fingerprint] Disk Serial 1: {(!string.IsNullOrEmpty(diskSerial1) ? "✓" : "✗")}");
                if (!string.IsNullOrEmpty(diskSerial1))
                {
                    components.Append(diskSerial1.ToUpper().Trim());
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Primary Disk Serial not found, using fallback");
                    return GenerateFallbackHardwareId();
                }

                // 2. Secondary Disk Serial (إن وجد)
                var diskSerial2 = GetSecondaryDiskSerialNumber();
                System.Diagnostics.Debug.WriteLine($"[Fingerprint] Disk Serial 2: {(!string.IsNullOrEmpty(diskSerial2) ? "✓" : "✗")}");
                if (!string.IsNullOrEmpty(diskSerial2))
                {
                    components.Append("|");
                    components.Append(diskSerial2.ToUpper().Trim());
                }

                // 3. Primary CPU ProcessorId (مهم جداً)
                var cpuId1 = GetCpuProcessorId();
                System.Diagnostics.Debug.WriteLine($"[Fingerprint] CPU ProcessorId 1: {(!string.IsNullOrEmpty(cpuId1) ? "✓" : "✗")}");
                if (!string.IsNullOrEmpty(cpuId1))
                {
                    components.Append("|");
                    components.Append(cpuId1.ToUpper().Trim());
                }

                // 4. Secondary CPU ProcessorId (إن وجد - للأنظمة متعددة المعالجات)
                var cpuId2 = GetSecondaryProcessorId();
                System.Diagnostics.Debug.WriteLine($"[Fingerprint] CPU ProcessorId 2: {(!string.IsNullOrEmpty(cpuId2) ? "✓" : "✗")}");
                if (!string.IsNullOrEmpty(cpuId2))
                {
                    components.Append("|");
                    components.Append(cpuId2.ToUpper().Trim());
                }

                // 5. BIOS UUID (متوسط القوة لكن مهم)
                var biosUuid = GetBiosUUID();
                System.Diagnostics.Debug.WriteLine($"[Fingerprint] BIOS UUID: {(!string.IsNullOrEmpty(biosUuid) ? "✓" : "✗")}");
                if (!string.IsNullOrEmpty(biosUuid))
                {
                    components.Append("|");
                    components.Append(biosUuid.ToUpper().Trim());
                }

                // 6. إضافة APP_SALT
                components.Append("|");
                components.Append(APP_SALT);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error collecting fingerprint components: {ex.Message}");
                return GenerateFallbackHardwareId();
            }

            // حساب SHA-256 hash النهائي
            return ComputeDeviceFingerprintHash(components.ToString());
        }

        /// <summary>
        /// استخراج البيانات الفيزيائية الخام (بدون hash)
        /// للإرسال للسيرفر مشفّر عبر Railway Proxy
        /// السيرفر يحسب HardwareId من هذه البيانات
        /// </summary>
        public static string GetRawHardwareComponents()
        {
            try
            {
                var rawData = new
                {
                    DiskSerial1 = GetDiskSerialNumber(),
                    DiskSerial2 = GetSecondaryDiskSerialNumber(),
                    CpuProcessorId1 = GetCpuProcessorId(),
                    CpuProcessorId2 = GetSecondaryProcessorId(),
                    BiosUuid = GetBiosUUID(),
                    Timestamp = DateTime.UtcNow.ToString("O")
                };

                var json = System.Text.Json.JsonSerializer.Serialize(rawData);
                System.Diagnostics.Debug.WriteLine($"[RawComponents] Data prepared for server: {json.Substring(0, Math.Min(50, json.Length))}...");
                return json;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error preparing raw hardware components: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// حساب SHA-256 hash من بيانات الجهاز
        /// الناتج: 64 حرف (hex string من SHA-256)
        /// </summary>
        private static string ComputeDeviceFingerprintHash(string input)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                    var hexString = Convert.ToHexString(hashedBytes);
                    System.Diagnostics.Debug.WriteLine($"✓ Device Fingerprint Hash generated: {hexString.Substring(0, 16)}...");
                    return hexString;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error computing fingerprint hash: {ex.Message}");
                return GenerateFallbackHardwareId();
            }
        }

        /// <summary>
        /// الحصول على BIOS UUID من WMI
        /// </summary>
        private static string GetBiosUUID()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
                var results = searcher.Get();
                foreach (ManagementObject obj in results)
                {
                    return obj["UUID"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WMI] BIOS UUID failed: {ex.Message}");
            }
            return "";
        }

        /// <summary>
        /// الحصول على رقم تسلسل القرص الصلب من WMI
        /// </summary>
        private static string GetDiskSerialNumber()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive");
                var results = searcher.Get();
                foreach (ManagementObject obj in results)
                {
                    var serial = obj["SerialNumber"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(serial))
                    {
                        return serial;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WMI] Disk Serial failed: {ex.Message}");
            }

            // Fallback: Volume Serial
            try
            {
                var volumeSerial = GetVolumeSerialNumber();
                if (!string.IsNullOrEmpty(volumeSerial))
                {
                    return "VOL_" + volumeSerial;
                }
            }
            catch { }
            
            return "";
        }

        /// <summary>
        /// الحصول على Volume Serial Number من قسم النظام
        /// </summary>
        private static string GetVolumeSerialNumber()
        {
            try
            {
                var systemDrive = Path.GetPathRoot(Environment.SystemDirectory);
                var drive = System.IO.DriveInfo.GetDrives().FirstOrDefault(d => d.Name == systemDrive);
                if (drive != null)
                {
                    return drive.VolumeLabel;
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// الحصول على رقم تسلسل القرص الثاني من WMI (إن وجد)
        /// </summary>
        private static string GetSecondaryDiskSerialNumber()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive");
                var results = searcher.Get();
                var disks = results.Cast<ManagementObject>().ToList();
                
                if (disks.Count > 1)
                {
                    var serial = disks[1]["SerialNumber"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(serial))
                    {
                        return serial;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WMI] Secondary Disk Serial failed: {ex.Message}");
            }
            return "";
        }

        /// <summary>
        /// الحصول على Processor ID من WMI
        /// </summary>
        private static string GetCpuProcessorId()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                var results = searcher.Get();
                foreach (ManagementObject obj in results)
                {
                    return obj["ProcessorId"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WMI] CPU ProcessorId failed: {ex.Message}");
            }
            return "";
        }

        /// <summary>
        /// الحصول على Processor ID الثاني من WMI (إن وجد - للأنظمة متعددة المعالجات)
        /// </summary>
        private static string GetSecondaryProcessorId()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                var results = searcher.Get();
                var processors = results.Cast<ManagementObject>().ToList();
                
                if (processors.Count > 1)
                {
                    return processors[1]["ProcessorId"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WMI] Secondary CPU ProcessorId failed: {ex.Message}");
            }
            return "";
        }

        /// <summary>
        /// الحصول على Machine GUID من Registry
        /// </summary>
        private static string GetMachineGuid()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    return key?.GetValue("MachineGuid")?.ToString() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// الحصول على معرف المعالج من Registry
        /// </summary>
        private static string GetProcessorId()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    return key?.GetValue("ProcessorNameString")?.ToString() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// الحصول على MAC Address لكارت الشبكة الأول
        /// </summary>
        private static string GetMacAddress()
        {
            try
            {
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up 
                                        && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                return networkInterface?.GetPhysicalAddress().ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }



        /// <summary>
        /// قراءة المعرف المحفوظ من الملف (للأداء فقط - ليس للأمان)
        /// ملاحظة: هذا الملف يمكن تجاهله - المصدر الحقيقي هو Registry
        /// </summary>
        private static string GetCachedHardwareId()
        {
            try
            {
                var cacheFile = Path.Combine(GetAppDataPath(), HARDWARE_ID_CACHE_FILE);
                if (File.Exists(cacheFile))
                {
                    var cachedId = File.ReadAllText(cacheFile).Trim();
                    // تحقق من صيغة SHA-256 (64 حرف hex)
                    if (!string.IsNullOrEmpty(cachedId) && cachedId.Length == 64 && IsHexString(cachedId))
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ Cache hit: using stored fingerprint");
                        return cachedId;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Invalid cache format, regenerating...");
                        File.Delete(cacheFile);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cache] Error reading: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// التحقق من أن النص يحتوي على أحرف hex فقط
        /// </summary>
        private static bool IsHexString(string str)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(str, @"^[a-fA-F0-9]+$");
        }

        /// <summary>
        /// حفظ المعرف في ملف للاستخدام المستقبلي
        /// </summary>
        private static void SaveHardwareIdToCache(string hardwareId)
        {
            try
            {
                var appDataPath = GetAppDataPath();
                Directory.CreateDirectory(appDataPath);
                
                var cacheFile = Path.Combine(appDataPath, HARDWARE_ID_CACHE_FILE);
                File.WriteAllText(cacheFile, hardwareId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving hardware ID to cache: {ex.Message}");
            }
        }

        /// <summary>
        /// الحصول على مسار مجلد البيانات
        /// </summary>
        private static string GetAppDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SR3H_MACRO");
        }

        /// <summary>
        /// توليد معرف بديل في حالة فشل الطرق الأخرى
        /// </summary>
        private static string GenerateFallbackHardwareId()
        {
            try
            {
                var fallbackData = $"{Environment.UserName}|{Environment.MachineName}|{Environment.OSVersion.Platform}|{APP_SALT}";
                return ComputeDeviceFingerprintHash(fallbackData);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Fallback hash generation failed, using emergency ID");
                return Guid.NewGuid().ToString("N");
            }
        }

        /// <summary>
        /// مسح المعرف المحفوظ (للاختبار أو إعادة التعيين)
        /// </summary>
        public static void ClearCachedHardwareId()
        {
            try
            {
                var cacheFile = Path.Combine(GetAppDataPath(), HARDWARE_ID_CACHE_FILE);
                if (File.Exists(cacheFile))
                {
                    File.Delete(cacheFile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing cached hardware ID: {ex.Message}");
            }
        }

        /// <summary>
        /// التحقق من صحة معرف الجهاز (SHA-256 hash بصيغة hex)
        /// </summary>
        public static bool IsValidHardwareId(string hardwareId)
        {
            return !string.IsNullOrEmpty(hardwareId) && hardwareId.Length == 64 && IsHexString(hardwareId);
        }
    }
}