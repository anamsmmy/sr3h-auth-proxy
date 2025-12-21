using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using MacroApp.Models;
using SR3H_MACRO.Services;

namespace MacroApp.Services
{

    public class MacroFortActivationService
    {
        private static readonly Lazy<MacroFortActivationService> _instance = 
            new Lazy<MacroFortActivationService>(() => new MacroFortActivationService());
        
        public static MacroFortActivationService Instance => _instance.Value;

        private static readonly HttpClient _httpClient = new HttpClient() 
        { 
            Timeout = System.TimeSpan.FromSeconds(30) 
        };

        private const string RAILWAY_PROXY_URL = "https://sr3h-auth-proxy-production.up.railway.app";
        private readonly SubscriptionCodeService _codeService;
        private const int TRIAL_DURATION_DAYS = 7;
        private const int OTP_VALIDITY_MINUTES = 10;
        private const int MIN_OTP_REQUEST_INTERVAL_SECONDS = 60;
        private const int MAX_OTP_REQUESTS_PER_10_MINUTES = 5;
        private const int THROTTLE_DURATION_MINUTES = 15;
        private const int DEVICE_TRANSFER_LIMIT_30_DAYS = 2;
        private const int REBIND_LIMIT_LONG_PLANS = 3;
        private const int GRACE_PERIOD_MINUTES = 5;
        private const int BACKGROUND_CHECK_INTERVAL_SECONDS = 30;
        
        private DateTime? _lastSuccessfulActivationCheck = null;
        private DateTime? _fortniteStoppedTime = null;
        private System.Threading.CancellationTokenSource _backgroundCheckCancellationTokenSource;
        private System.Threading.Tasks.Task _backgroundCheckTask;
        private bool _isBackgroundCheckRunning = false;

        private MacroFortActivationService()
        {
            _codeService = new SubscriptionCodeService();
            StartBackgroundCheck();
        }
        
        ~MacroFortActivationService()
        {
            StopBackgroundCheck();
        }

        public string GenerateHardwareId()
        {
            return SafeHardwareIdService.GenerateHardwareId();
        }

        public string GetFreshHardwareId()
        {
            return SafeHardwareIdService.GetFreshHardwareId();
        }

        /// <summary>
        /// التحقق من بيانات الجهاز (Hardware Fingerprint) عبر السيرفر
        /// يستخرج البيانات الخام ويرسلها للسيرفر مشفرة عبر Railway Proxy
        /// السيرفر يحسب HardwareId ويتحقق من البيانات
        /// </summary>
        public async Task<HardwareVerificationResponse> VerifyHardwareAsync(string email)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 بدء التحقق من بيانات الجهاز للبريد: {email}");

                // 1. استخراج البيانات الفيزيائية الخام (بدون hash)
                var rawComponents = SafeHardwareIdService.GetRawHardwareComponents();
                if (string.IsNullOrEmpty(rawComponents))
                {
                    System.Diagnostics.Debug.WriteLine("❌ فشل استخراج بيانات الجهاز الخام");
                    await LogHardwareVerificationAsync(null, email, "", rawComponents, "invalid", GetOsVersion(), "فشل استخراج البيانات");
                    return new HardwareVerificationResponse
                    {
                        IsSuccess = false,
                        Message = "فشل استخراج بيانات الجهاز"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"✓ تم استخراج البيانات الخام بنجاح");

                // 2. تشفير البيانات الخام (Base64 encoding كـ layer إضافي)
                var encryptedComponents = EncryptRawComponents(rawComponents);
                System.Diagnostics.Debug.WriteLine($"✓ تم تشفير البيانات بنجاح");

                // 3. إعداد الطلب للإرسال للسيرفر
                var payload = new
                {
                    email = email,
                    encrypted_components = encryptedComponents,
                    verification_timestamp = DateTime.UtcNow.ToString("O")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 4. إرسال البيانات عبر Railway Proxy (HTTPS)
                var url = $"{RAILWAY_PROXY_URL}/verify-hardware";
                System.Diagnostics.Debug.WriteLine($"🌐 إرسال طلب التحقق من الجهاز إلى: {url}");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<HardwareVerificationResponse>(responseContent);
                    
                    if (result?.IsSuccess == true)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ تم التحقق من بيانات الجهاز بنجاح");
                        System.Diagnostics.Debug.WriteLine($"💻 HardwareId من السيرفر: {result.HardwareId?.Substring(0, Math.Min(16, result.HardwareId.Length))}...");
                        
                        // تحديث cache بحالة التحقق الناجحة
                        SessionActivationCache.SetHardwareVerificationStatus("verified");
                        SessionActivationCache.SetGracePeriodExpiry(DateTime.UtcNow.AddMinutes(5));
                        
                        // تسجيل في السجل
                        await LogHardwareVerificationAsync(null, email, result.HardwareId, rawComponents, "success", GetOsVersion());
                        
                        return result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ الجهاز غير متطابق: {result?.Message}");
                        SessionActivationCache.SetHardwareVerificationStatus("mismatch");
                        await LogHardwareVerificationAsync(null, email, "", rawComponents, "mismatch", GetOsVersion(), result?.Message);
                        
                        return result ?? new HardwareVerificationResponse
                        {
                            IsSuccess = false,
                            Message = "فشل التحقق من بيانات الجهاز"
                        };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ خطأ من السيرفر: {response.StatusCode} - {errorContent}");
                    SessionActivationCache.SetHardwareVerificationStatus("failed");
                    await LogHardwareVerificationAsync(null, email, "", rawComponents, "error", GetOsVersion(), $"Server error: {response.StatusCode}");
                    
                    return new HardwareVerificationResponse
                    {
                        IsSuccess = false,
                        Message = $"خطأ في السيرفر: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ استثناء في التحقق من الجهاز: {ex.Message}");
                SessionActivationCache.SetHardwareVerificationStatus("failed");
                await LogHardwareVerificationAsync(null, email, "", "", "error", GetOsVersion(), ex.Message);
                
                return new HardwareVerificationResponse
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// تشفير البيانات الخام باستخدام Base64 encoding
        /// هذا layer إضافي من الحماية بالإضافة إلى HTTPS
        /// </summary>
        private string EncryptRawComponents(string rawComponents)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(rawComponents);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ خطأ في تشفير البيانات: {ex.Message}");
                return rawComponents;
            }
        }

        private string GenerateOtp()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[4];
                rng.GetBytes(tokenData);
                int otp = (BitConverter.ToInt32(tokenData, 0) & 0x7FFFFFFF) % 1000000;
                return otp.ToString("D6");
            }
        }

        public async Task<MacroFortActivationResult> StartTrialAsync(string email)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 بدء التجربة للبريد: {email}");
                
                if (string.IsNullOrWhiteSpace(email))
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "البريد الإلكتروني مطلوب",
                        ResultType = "invalid_email"
                    };

                var hardwareId = GetFreshHardwareId();
                System.Diagnostics.Debug.WriteLine($"💻 معرف الجهاز (fresh): {hardwareId}");



                System.Diagnostics.Debug.WriteLine("✓ بيانات الاعتماد موجودة");

                if (!await CheckInternetConnectionAsync())
                {
                    System.Diagnostics.Debug.WriteLine("❌ لا يوجد اتصال إنترنت");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لا يوجد اتصال إنترنت",
                        ResultType = "no_internet"
                    };
                }

                System.Diagnostics.Debug.WriteLine("✓ الاتصال بالإنترنت موجود");

                var normalizedEmail = email.ToLower().Trim();

                var (canProceed, spamMessage, remainingMinutes) = await CheckOtpSpamStatusAsync(normalizedEmail);
                if (!canProceed)
                {
                    System.Diagnostics.Debug.WriteLine($"⛔ {spamMessage}");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = spamMessage,
                        ResultType = remainingMinutes.HasValue && remainingMinutes > 0 ? 
                            (remainingMinutes >= THROTTLE_DURATION_MINUTES ? "rate_limit_throttled" : "rate_limit_interval") : 
                            "rate_limit_exceeded"
                    };
                }

                var trialStatus = await CheckTrialStatusAsync(email, hardwareId);
                if (!trialStatus.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ فحص التجربة فشل: {trialStatus.Message}");
                    return trialStatus;
                }

                var otp = GenerateOtp();
                var expiryDate = DateTime.UtcNow.AddDays(TRIAL_DURATION_DAYS);
                var activationDate = DateTime.UtcNow;
                var otpExpiry = DateTime.UtcNow.AddMinutes(OTP_VALIDITY_MINUTES);

                System.Diagnostics.Debug.WriteLine($"🔑 OTP: {otp}, ينتهي في: {otpExpiry}");

                var saveOtpResult = await SaveOtpViaProxyAsync(normalizedEmail, otp, hardwareId, otpExpiry);
                if (!saveOtpResult)
                {
                    System.Diagnostics.Debug.WriteLine("❌ فشل حفظ OTP في قاعدة البيانات");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "فشل في حفظ كود التحقق",
                        ResultType = "otp_storage_failed"
                    };
                }
                System.Diagnostics.Debug.WriteLine("✓ تم حفظ OTP بنجاح في جدول التحقق");

                bool insertResult;
                if (trialStatus.Message == "trial_exists_not_expired")
                {
                    System.Diagnostics.Debug.WriteLine("🔄 جاري تحديث التجربة الموجودة مع معرف جهاز جديد");
                    insertResult = await UpdateTrialSubscriptionAsync(email, hardwareId, otp, otpExpiry);
                    if (!insertResult)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ فشل تحديث بيانات الاشتراك في قاعدة البيانات");
                        return new MacroFortActivationResult
                        {
                            IsSuccess = false,
                            Message = "فشل تحديث التجربة المجانية",
                            ResultType = "database_error"
                        };
                    }
                    System.Diagnostics.Debug.WriteLine("✓ تم تحديث بيانات الاشتراك بنجاح");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✓ التجربة لم تُستخدم من قبل");
                    insertResult = await InsertTrialSubscriptionAsync(email, hardwareId, otp, otpExpiry, activationDate, expiryDate);
                    if (!insertResult)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ فشل إدراج بيانات الاشتراك في قاعدة البيانات");
                        return new MacroFortActivationResult
                        {
                            IsSuccess = false,
                            Message = "فشل إنشاء التجربة المجانية",
                            ResultType = "database_error"
                        };
                    }
                    System.Diagnostics.Debug.WriteLine("✓ تم إدراج بيانات الاشتراك بنجاح");
                }

                var sendEmailResult = await SendOtpEmailAsync(email, otp, "تفعيل التجربة المجانية");
                if (!sendEmailResult)
                {
                    System.Diagnostics.Debug.WriteLine("❌ فشل إرسال البريد الإلكتروني");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "فشل إرسال كود التحقق",
                        ResultType = "email_error"
                    };
                }

                System.Diagnostics.Debug.WriteLine("✓ تم إرسال كود التحقق بنجاح");

                await RecordOtpRequestAsync(normalizedEmail);

                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = "تم إرسال كود التحقق إلى بريدك الإلكتروني",
                    ResultType = "trial_started",
                    ExpiryDate = expiryDate,
                    SubscriptionData = new MacroFortSubscriptionData
                    {
                        Email = email,
                        HardwareId = hardwareId,
                        SubscriptionType = "trial",
                        ActivationDate = activationDate,
                        ExpiryDate = expiryDate,
                        IsActive = false,
                        EmailVerified = false,
                        LastCheckDate = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في بدء التجربة: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}",
                    ResultType = "error"
                };
            }
        }

        public async Task<MacroFortActivationResult> SendOtpForCodeActivationAsync(string email, string subscriptionCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subscriptionCode))
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "البريد والكود مطلوبان",
                        ResultType = "invalid_input"
                    };

                if (!await CheckInternetConnectionAsync())
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لا يوجد اتصال إنترنت",
                        ResultType = "no_internet"
                    };

                var normalizedEmail = email.ToLower().Trim();

                var (canProceed, spamMessage, remainingMinutes) = await CheckOtpSpamStatusAsync(normalizedEmail);
                if (!canProceed)
                {
                    System.Diagnostics.Debug.WriteLine($"⛔ {spamMessage}");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = spamMessage,
                        ResultType = remainingMinutes.HasValue && remainingMinutes > 0 ? 
                            (remainingMinutes >= THROTTLE_DURATION_MINUTES ? "rate_limit_throttled" : "rate_limit_interval") : 
                            "rate_limit_exceeded"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"✓ الطلب صالح - إرسال OTP للبريد: {email}");

                var otp = GenerateOtp();
                var otpExpiry = DateTime.UtcNow.AddMinutes(OTP_VALIDITY_MINUTES);
                var hardwareId = GetFreshHardwareId();

                var insertResult = await SaveOtpViaProxyAsync(email, otp, hardwareId, otpExpiry);
                if (!insertResult)
                {
                    System.Diagnostics.Debug.WriteLine("❌ فشل إدراج كود OTP في قاعدة البيانات");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "فشل إنشاء كود التحقق",
                        ResultType = "otp_creation_failed"
                    };
                }

                System.Diagnostics.Debug.WriteLine("✓ تم إدراج كود OTP بنجاح");

                var sendEmailResult = await SendOtpEmailAsync(email, otp, $"كود تفعيل الاشتراك: {otp}");
                if (!sendEmailResult)
                {
                    System.Diagnostics.Debug.WriteLine("❌ فشل إرسال البريد الإلكتروني");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "فشل إرسال كود التحقق",
                        ResultType = "email_error"
                    };
                }

                System.Diagnostics.Debug.WriteLine("✓ تم إرسال كود التحقق بنجاح");

                await RecordOtpRequestAsync(normalizedEmail);

                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = "تم إرسال كود التحقق إلى بريدك الإلكتروني",
                    ResultType = "otp_sent"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في إرسال OTP: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}",
                    ResultType = "error"
                };
            }
        }

        public async Task<MacroFortActivationResult> ConfirmCodeActivationAsync(string email, string subscriptionCode, string otpCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 تأكيد تفعيل الكود للبريد: {email}");

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subscriptionCode) || string.IsNullOrWhiteSpace(otpCode))
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "البريد والكود والتحقق مطلوبان",
                        ResultType = "invalid_input"
                    };

                if (!await CheckInternetConnectionAsync())
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لا يوجد اتصال إنترنت",
                        ResultType = "no_internet"
                    };

                var normalizedEmail = email.ToLower().Trim();

                var hardwareIdFromOtp = await VerifyOtpInVerificationTableAsync(normalizedEmail, otpCode);
                if (string.IsNullOrEmpty(hardwareIdFromOtp))
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "كود التحقق غير صحيح أو منتهي الصلاحية",
                        ResultType = "invalid_otp"
                    };

                System.Diagnostics.Debug.WriteLine("✓ تم التحقق من OTP بنجاح");

                var hardwareId = GetFreshHardwareId();

                var redeemResult = await RedeemCodeViaAuthProxyAsync(subscriptionCode, normalizedEmail, hardwareId);
                if (!redeemResult.IsSuccess)
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = redeemResult.Message,
                        ResultType = redeemResult.ResultType
                    };

                System.Diagnostics.Debug.WriteLine("✓ تم استرجاع الكود بنجاح عبر Auth Proxy");

                var markOtpResult = await MarkVerificationOtpAsUsedAsync(normalizedEmail, otpCode);
                if (!markOtpResult)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ فشل تحديث حالة OTP - لن يؤثر على نجاح التفعيل");
                }

                var verificationResult = await UpdateSubscriptionVerificationAsync(normalizedEmail, hardwareId, true);
                if (!verificationResult)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ فشل تحديث حالة التحقق");
                }

                System.Diagnostics.Debug.WriteLine("✓ تم تفعيل الكود بنجاح");

                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = "تم تفعيل الكود بنجاح",
                    ResultType = "activation_success",
                    ExpiryDate = redeemResult.ExpiryDate,
                    SubscriptionData = redeemResult.SubscriptionData
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تأكيد تفعيل الكود: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}",
                    ResultType = "error"
                };
            }
        }

        public async Task<MacroFortActivationResult> GetSubscriptionStatusAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "البريد الإلكتروني مطلوب",
                        ResultType = "invalid_email"
                    };

                var subscription = await GetSubscriptionByEmailAsync(email.ToLower().Trim());
                if (subscription == null)
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لا يوجد اشتراك نشط",
                        ResultType = "no_subscription"
                    };

                var isExpired = subscription.ExpiryDate <= DateTime.UtcNow;
                var remainingDays = (subscription.ExpiryDate - DateTime.UtcNow).TotalDays;

                return new MacroFortActivationResult
                {
                    IsSuccess = !isExpired,
                    Message = isExpired 
                        ? "انتهت صلاحية الاشتراك" 
                        : $"الاشتراك نشط - متبقي {remainingDays:F0} يوم",
                    ResultType = isExpired ? "subscription_expired" : "subscription_active",
                    ExpiryDate = subscription.ExpiryDate,
                    SubscriptionData = subscription
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في فحص الاشتراك: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}",
                    ResultType = "error"
                };
            }
        }

        private async Task<bool> CheckInternetConnectionAsync()
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    var response = await client.GetAsync("https://www.google.com");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private void StartBackgroundCheck()
        {
            if (_isBackgroundCheckRunning) return;
            _isBackgroundCheckRunning = true;
            _backgroundCheckCancellationTokenSource = new System.Threading.CancellationTokenSource();
            _backgroundCheckTask = Task.Run(async () => await BackgroundCheckAsync(_backgroundCheckCancellationTokenSource.Token));
        }

        private void StopBackgroundCheck()
        {
            if (!_isBackgroundCheckRunning) return;
            _isBackgroundCheckRunning = false;
            _backgroundCheckCancellationTokenSource?.Cancel();
            try { _backgroundCheckTask?.Wait(TimeSpan.FromSeconds(5)); } catch { }
        }

        private async Task BackgroundCheckAsync(System.Threading.CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(BACKGROUND_CHECK_INTERVAL_SECONDS * 1000, cancellationToken);
                    _lastSuccessfulActivationCheck = DateTime.UtcNow;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ خطأ في الفحص الخلفي: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// فحص أهلية الجهاز للحصول على تجربة مجانية
        /// يستخدم جدول macro_fort_trial_history كمصدر حقيقة
        /// آمن تماماً ضد التحايل عبر حذف ملفات محلية
        /// </summary>
        private async Task<MacroFortActivationResult> CheckTrialStatusAsync(string email, string deviceFingerprintHash)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceFingerprintHash))
                {
                    System.Diagnostics.Debug.WriteLine("❌ بصمة الجهاز فارغة");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "بصمة الجهاز غير صحيحة",
                        ResultType = "invalid_hardware_id"
                    };
                }

                // استدعاء RPC لفحص الأهلية
                var result = await CallTrialEligibilityCheckAsync(deviceFingerprintHash);
                
                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ فشل التحقق من أهلية التجربة");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "فشل التحقق من الخادم",
                        ResultType = "server_error"
                    };
                }

                var allowed = result["allowed"]?.ToObject<bool>() ?? false;
                var reason = result["reason"]?.ToString() ?? "";

                if (!allowed)
                {
                    var errorMsg = reason == "trial_already_used_on_device" 
                        ? "هذا الجهاز استخدم التجربة المجانية مرة واحدة فقط (منتهية الصلاحية)"
                        : $"الجهاز غير مؤهل: {reason}";
                    
                    System.Diagnostics.Debug.WriteLine($"❌ الجهاز غير مؤهل للتجربة: {reason}");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = errorMsg,
                        ResultType = "trial_already_used_on_device"
                    };
                }

                if (reason == "trial_exists_not_expired")
                {
                    System.Diagnostics.Debug.WriteLine($"✓ التجربة موجودة ولم تنتهِ بعد - سيتم تحديث البريد و OTP");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = true,
                        Message = "trial_exists_not_expired"
                    };
                }

                System.Diagnostics.Debug.WriteLine("✓ الجهاز مؤهل للحصول على تجربة مجانية جديدة");
                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = "trial_eligible"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في فحص التجربة: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}",
                    ResultType = "error"
                };
            }
        }

        /// <summary>
        /// استدعاء RPC check_trial_eligibility عبر Railway Proxy
        /// </summary>
        private async Task<Newtonsoft.Json.Linq.JObject> CallTrialEligibilityCheckAsync(string deviceFingerprintHash)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var url = $"{RAILWAY_PROXY_URL}/check-trial-eligibility";
                    var payload = new { p_device_fingerprint_hash = deviceFingerprintHash };
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔗 استدعاء Railway Proxy: check-trial-eligibility");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(responseContent);
                        System.Diagnostics.Debug.WriteLine($"✓ Response: {result?["reason"]}");
                        return result;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ Request فشل: {response.StatusCode} - {errorContent}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في الاتصال: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// إدراج تجربة جديدة عبر RPC activate_trial
        /// تتم العملية بشكل ذري (atomic) على السيرفر
        /// ثم ينشئ سجل في macro_fort_subscriptions للدعم الكامل
        /// </summary>
        private async Task<bool> InsertTrialSubscriptionAsync(string email, string deviceFingerprintHash, string otp, DateTime otpExpiry, DateTime activationDate, DateTime expiryDate)
        {
            try
            {
                var (rpcSuccess, rpcMessage) = await CallActivateTrialRpcAsync(deviceFingerprintHash, email, TRIAL_DURATION_DAYS);
                
                if (!rpcSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ فشل تفعيل التجربة: {rpcMessage}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"✓ تم تفعيل التجربة بنجاح");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ استثناء في InsertTrialSubscriptionAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// استدعاء Railway Proxy لتفعيل التجربة بشكل آمن
        /// يُرجع (success, message) يحتوي على تفاصيل النتيجة
        /// </summary>
        private async Task<(bool success, string message)> CallActivateTrialRpcAsync(string deviceFingerprintHash, string email, int trialDays)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var url = $"{RAILWAY_PROXY_URL}/activate-trial";
                    var payload = new
                    {
                        p_device_fingerprint_hash = deviceFingerprintHash,
                        p_email = email,
                        p_trial_days = trialDays
                    };
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"📝 استدعاء Railway Proxy: activate-trial");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(responseContent);
                        var success = result?["success"]?.ToObject<bool>() ?? false;
                        var message = result?["message"]?.ToString() ?? "";
                        
                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓ تم تفعيل التجربة بنجاح: {message}");
                            return (true, message);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ فشل تفعيل التجربة: {message}");
                            return (false, message);
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ Request فشل: {response.StatusCode} - {errorContent}");
                        return (false, $"خطأ الخادم: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في الاتصال: {ex.Message}");
                return (false, $"خطأ: {ex.Message}");
            }
        }

        private async Task<bool> UpdateTrialSubscriptionAsync(string email, string deviceFingerprintHash, string otp, DateTime otpExpiry)
        {
            try
            {


                System.Diagnostics.Debug.WriteLine($"🔄 جاري تحديث التجربة - البريد الجديد: {email}");

                var updateData = new
                {
                    email = email,
                    last_check_date = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                    var encodedFingerprintHash = System.Net.WebUtility.UrlEncode(deviceFingerprintHash);
                    var json = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/check-trial-subscription?hardware_id={encodedFingerprintHash}";
                    System.Diagnostics.Debug.WriteLine($"📝 تحديث بـ device_fingerprint: {deviceFingerprintHash.Substring(0, 16)}...");
                    
                    var response = await client.PatchAsync(url, content);
                    System.Diagnostics.Debug.WriteLine($"📊 كود الاستجابة: {(int)response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ فشل التحديث: {errorContent}");
                    }

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ استثناء في UpdateTrialSubscriptionAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<bool> InsertSubscriptionAsync(string email, string hardwareId, string subscriptionCode, string subscriptionType, DateTime activationDate, DateTime expiryDate)
        {
            try
            {


                System.Diagnostics.Debug.WriteLine($"🔄 جاري إدراج اشتراك جديد - النوع: {subscriptionType}, البريد: {email}");

                var subscriptionData = new
                {
                    email = email,
                    hardware_id = hardwareId,
                    subscription_code = subscriptionCode,
                    subscription_type = subscriptionType,
                    activation_date = activationDate,
                    expiry_date = expiryDate,
                    is_active = true,
                    email_verified = true,
                    is_trial = subscriptionType == "trial",
                    device_transfer_count = 0,
                    last_device_transfer_date = (DateTime?)null,
                    last_check_date = DateTime.UtcNow,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                    var json = JsonConvert.SerializeObject(subscriptionData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/create-subscription";
                    System.Diagnostics.Debug.WriteLine($"🔗 POST طلب إلى: {url}");
                    
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ تم إدراج الاشتراك بنجاح: {response.StatusCode}");
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ فشل إدراج الاشتراك: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"📋 تفاصيل الخطأ: {errorContent}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إدراج الاشتراك: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<bool> UpdateSubscriptionAsync(string subscriptionId, string email, string hardwareId, string subscriptionCode, string subscriptionType, DateTime activationDate, DateTime expiryDate)
        {
            try
            {


                System.Diagnostics.Debug.WriteLine($"🔄 جاري تحديث الاشتراك - النوع: {subscriptionType}, البريد: {email}");

                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                    var encodedEmail = System.Net.WebUtility.UrlEncode(email);

                    System.Diagnostics.Debug.WriteLine($"🔄 تحديث بيانات الاشتراك");
                    var subscriptionData = new
                    {
                        hardware_id = hardwareId,
                        subscription_code = subscriptionCode,
                        subscription_type = subscriptionType,
                        activation_date = activationDate,
                        expiry_date = expiryDate,
                        is_active = true,
                        email_verified = true,
                        is_trial = subscriptionType == "trial",
                        last_check_date = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow
                    };

                    var json = JsonConvert.SerializeObject(subscriptionData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/update-subscription?id={subscriptionId}";
                    System.Diagnostics.Debug.WriteLine($"🔗 PATCH طلب إلى: {url}");
                    
                    var response = await client.PatchAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ تم تحديث الاشتراك بنجاح: {response.StatusCode}");
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ فشل تحديث الاشتراك: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"📋 تفاصيل الخطأ: {errorContent}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحديث الاشتراك: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<bool> MarkTrialAsUsedAsync(string email)
        {
            try
            {


                var updateData = new
                {
                    is_trial = true,
                    updated_at = DateTime.UtcNow
                };

                using (var client = new HttpClient())
                {

                    
                    var json = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/get-subscription-by-email?email={email}";
                    var response = await client.PatchAsync(url, content);

                    System.Diagnostics.Debug.WriteLine($"✓ تم تحديث حالة التجربة: {response.StatusCode}");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تحديث حالة التجربة: {ex.Message}");
                return false;
            }
        }

        private async Task<string> VerifyTrialOtpAsync(string email, string otpCode)
        {
            try
            {


                using (var client = new HttpClient())
                {


                    var encodedEmail = System.Net.WebUtility.UrlEncode(email);
                    
                    System.Diagnostics.Debug.WriteLine($"🔍 التحقق من OTP: البريد={email}, الكود={otpCode}");
                    
                    // Check if OTP exists in macro_fort_subscriptions (trial subscription)
                    // OTP is stored there along with the subscription
                    var subscUrl = $"{RAILWAY_PROXY_URL}/check-trial-subscription-by-email?email={encodedEmail}";
                    var subscResponse = await client.GetAsync(subscUrl);

                    if (!subscResponse.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ فشل البحث عن الاشتراك - HTTP {(int)subscResponse.StatusCode}");
                        return null;
                    }

                    var subscContent = await subscResponse.Content.ReadAsStringAsync();
                    var subscriptions = JsonConvert.DeserializeObject<List<dynamic>>(subscContent);

                    if (subscriptions == null || subscriptions.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ لم يتم العثور على اشتراك تجربة للبريد");
                        return null;
                    }

                    // Verify OTP code and expiry
                    var subscription = subscriptions[0];
                    var storedOtp = subscription.otp_code?.ToString();
                    var otpExpiryStr = subscription.otp_expiry?.ToString();
                    
                    if (string.IsNullOrEmpty(storedOtp) || storedOtp != otpCode)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ كود OTP غير صحيح");
                        return null;
                    }

                    if (!string.IsNullOrEmpty(otpExpiryStr))
                    {
                        if (DateTime.TryParse(otpExpiryStr, out DateTime otpExpiry))
                        {
                            if (otpExpiry < DateTime.UtcNow)
                            {
                                System.Diagnostics.Debug.WriteLine("❌ كود OTP منتهي الصلاحية");
                                return null;
                            }
                        }
                    }

                    var hardwareId = subscription.hardware_id?.ToString();
                    System.Diagnostics.Debug.WriteLine($"✓ تم العثور على تجربة صحيحة - Hardware ID: {hardwareId}");
                    return hardwareId;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في التحقق من OTP: {ex.Message}");
                return null;
            }
        }

        private async Task<string> VerifyOtpInVerificationTableAsync(string email, string otpCode)
        {
            try
            {


                System.Diagnostics.Debug.WriteLine($"🔍 التحقق من OTP في جدول التحقق: البريد={email}, الكود={otpCode}");
                
                await System.Threading.Tasks.Task.Delay(500);
                
                var encodedEmail = System.Net.WebUtility.UrlEncode(email);
                var url = $"{RAILWAY_PROXY_URL}/verify-otp?email={encodedEmail}&code={otpCode}";
                
                var response = await RetryAsync(() => GetWithAuthAsync(url), maxRetries: 3);

                if (response == null || !response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ فشل البحث - HTTP {(int)(response?.StatusCode ?? System.Net.HttpStatusCode.BadRequest)}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var records = JsonConvert.DeserializeObject<List<dynamic>>(content);

                if (records == null || records.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ لم يتم العثور على رمز OTP");
                    return null;
                }

                var record = records[0];
                var expiresAtStr = record.expires_at?.ToString();
                
                if (!string.IsNullOrEmpty(expiresAtStr))
                {
                    if (DateTime.TryParse(expiresAtStr, out DateTime expiresAt))
                    {
                        if (expiresAt < DateTime.UtcNow)
                        {
                            System.Diagnostics.Debug.WriteLine("❌ كود OTP منتهي الصلاحية");
                            return null;
                        }
                    }
                }

                var hardwareId = record.hardware_id?.ToString();
                System.Diagnostics.Debug.WriteLine($"✓ تم التحقق من OTP بنجاح - Hardware ID: {hardwareId}");
                return hardwareId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في التحقق من OTP: {ex.Message}");
                return null;
            }
        }

        private async Task<System.Net.Http.HttpResponseMessage> GetWithAuthAsync(string url)
        {
            lock (_httpClient)
            {
                _httpClient.DefaultRequestHeaders.Clear();

            }
            return await _httpClient.GetAsync(url);
        }

        private async Task<T> RetryAsync<T>(Func<Task<T>> func, int maxRetries = 3) where T : System.Net.Http.HttpResponseMessage
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var result = await func();
                    
                    if ((int)result.StatusCode == 429)
                    {
                        if (i < maxRetries - 1)
                        {
                            var delayMs = (int)Math.Pow(2, i) * 1000;
                            System.Diagnostics.Debug.WriteLine($"⏱️ HTTP 429 - الانتظار {delayMs}ms قبل المحاولة {i + 2}...");
                            await System.Threading.Tasks.Task.Delay(delayMs);
                            continue;
                        }
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    if (i < maxRetries - 1)
                    {
                        var delayMs = (int)Math.Pow(2, i) * 500;
                        System.Diagnostics.Debug.WriteLine($"⚠️ خطأ في المحاولة {i + 1}: {ex.Message} - الانتظار {delayMs}ms...");
                        await System.Threading.Tasks.Task.Delay(delayMs);
                        continue;
                    }
                    throw;
                }
            }
            return null;
        }

        private async Task<bool> MarkVerificationOtpAsUsedAsync(string email, string otpCode)
        {
            try
            {


                using (var client = new HttpClient())
                {


                    var encodedEmail = System.Net.WebUtility.UrlEncode(email);
                    var encodedOtp = System.Net.WebUtility.UrlEncode(otpCode);
                    
                    var updateData = new
                    {
                        is_verified = true,
                        verified_at = DateTime.UtcNow
                    };

                    var json = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/mark-otp-used?email={encodedEmail}&code={encodedOtp}";
                    var response = await client.PatchAsync(url, content);

                    System.Diagnostics.Debug.WriteLine($"📝 تحديث حالة OTP: HTTP {(int)response.StatusCode}");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تحديث OTP: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> MarkTrialOtpAsUsedAsync(string hardwareId)
        {
            try
            {


                using (var client = new HttpClient())
                {


                    var encodedHardwareId = System.Net.WebUtility.UrlEncode(hardwareId);
                    var updateData = new
                    {
                        updated_at = DateTime.UtcNow
                    };

                    var json = JsonConvert.SerializeObject(updateData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/check-trial-by-hardware?hardware_id={encodedHardwareId}";
                    var response = await client.PatchAsync(url, content);

                    System.Diagnostics.Debug.WriteLine($"📝 مسح OTP من التجربة: HTTP {(int)response.StatusCode}");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تحديث OTP: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> VerifyOtpInDatabaseAsync(string email, string otpCode)
        {
            try
            {


                using (var client = new HttpClient())
                {

                    
                    var encodedEmail = System.Net.WebUtility.UrlEncode(email);
                    
                    // Update trial subscription after successful verification
                    var updateJson = JsonConvert.SerializeObject(new { updated_at = DateTime.UtcNow });
                    var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/check-trial-by-email?email={encodedEmail}";
                    var updateResponse = await client.PatchAsync(url, updateContent);
                    
                    return updateResponse.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تحديث OTP: {ex.Message}");
                return false;
            }
        }

        public async Task<MacroFortSubscriptionData> GetSubscriptionByHardwareIdAsync(string hardwareId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔗 طلب الاشتراك عبر Railway للـ hardware_id: {hardwareId}");
                
                using (var client = new HttpClient())
                {
                    var requestData = new { hardware_id = hardwareId };
                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/get-subscription-by-hardware";
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var subscription = JsonConvert.DeserializeObject<MacroFortSubscriptionData>(responseContent);
                        
                        if (subscription != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓ تم جلب الاشتراك من Railway بنجاح");
                            return subscription;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ لا توجد اشتراكات: {(int)response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في جلب الاشتراك عبر Railway: {ex.Message}");
            }

            return null;
        }

        public async Task<MacroFortSubscriptionData> GetSubscriptionByEmailAsync(string email)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔗 طلب الاشتراك عبر Railway للـ email: {email}");
                
                using (var client = new HttpClient())
                {
                    var requestData = new { email = email };
                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/get-subscription-by-email";
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"✓ استجابة ناجحة من Railway");
                        var subscription = JsonConvert.DeserializeObject<MacroFortSubscriptionData>(responseContent);
                        
                        if (subscription != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓ تم جلب الاشتراك من Railway بنجاح");
                            return subscription;
                        }
                        System.Diagnostics.Debug.WriteLine("⚠️ لا توجد نتائج للبريد");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ فشل الطلب: {(int)response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"💬 رسالة الخطأ: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في جلب الاشتراك: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"🔍 تفاصيل الخطأ: {ex.StackTrace}");
            }

            return null;
        }

        private async Task<bool> UpdateSubscriptionVerificationAsync(string email, string hardwareId, bool verified)
        {
            try
            {


                var updateData = new
                {
                    is_active = true,
                    email_verified = verified,
                    hardware_id = hardwareId,
                    last_check_date = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                using (var client = new HttpClient())
                {

                    
                    var json = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/get-subscription-by-email?email={email}";
                    var response = await client.PatchAsync(url, content);

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تحديث التحقق: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> InsertOtpCodeAsync(string email, string otp, DateTime otpExpiry)
        {
            try
            {


                var otpData = new
                {
                    email = email,
                    code = otp,
                    expires_at = otpExpiry,
                    is_used = false,
                    created_at = DateTime.UtcNow
                };

                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                    var json = JsonConvert.SerializeObject(otpData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/create-otp";
                    var response = await client.PostAsync(url, content);

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في إدراج OTP: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SaveOtpViaProxyAsync(string email, string otp, string hardwareId, DateTime otpExpiry)
        {
            try
            {


                var expiryUtc = otpExpiry.ToUniversalTime().ToString("O");
                
                var otpData = new
                {
                    email = email,
                    otp_code = otp,
                    hardware_id = hardwareId,
                    expires_at = expiryUtc
                };

                using (var client = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(otpData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/save-otp";
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"✓ تم حفظ OTP بنجاح عبر Proxy: {responseBody}");
                        return true;
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"✗ فشل حفظ OTP عبر Proxy: {response.StatusCode} - {errorBody}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في SaveOtpViaProxyAsync: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendOtpEmailAsync(string email, string otp, string subject)
        {
            try
            {
                var emailService = new EmailService();
                bool sent = await emailService.SendVerificationCodeAsync(email, otp);
                return sent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في إرسال البريد: {ex.Message}");
                return false;
            }
        }

        public async Task<MacroFortActivationResult> VerifyOtpAsync(string email, string otpCode)
        {
            try
            {
                var normalizedEmail = email.ToLower().Trim();
                
                var hardwareIdFromOtp = await VerifyOtpInVerificationTableAsync(normalizedEmail, otpCode);
                if (string.IsNullOrEmpty(hardwareIdFromOtp))
                {
                    System.Diagnostics.Debug.WriteLine("❌ فشل التحقق من OTP - الكود غير صحيح أو منتهي الصلاحية");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "كود التحقق غير صحيح أو منتهي الصلاحية",
                        ResultType = "invalid_otp"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"✓ تم التحقق من البريد {normalizedEmail} بنجاح عبر OTP");
                
                var markOtpUsedResult = await MarkVerificationOtpAsUsedAsync(normalizedEmail, otpCode);
                if (!markOtpUsedResult)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ فشل تحديث حالة OTP لكن سيُكمل التفعيل");
                }

                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = "تم التحقق من البريد بنجاح",
                    ResultType = "otp_verified"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في التحقق من OTP: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = "خطأ في التحقق",
                    ResultType = "verify_error"
                };
            }
        }

        public async Task<MacroFortActivationResult> RebindSubscriptionCodeAsync(string email, string hardwareId)
        {
            try
            {
                var otp = GenerateOtp();
                var otpExpiry = DateTime.UtcNow.AddMinutes(OTP_VALIDITY_MINUTES);
                var otpResult = await SaveOtpViaProxyAsync(email, otp, hardwareId, otpExpiry);
                if (!otpResult)
                {
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "فشل في إنشاء رمز التحقق",
                        ResultType = "otp_creation_failed"
                    };
                }

                var emailResult = await SendOtpEmailAsync(email, otp, "رمز إعادة ربط الاشتراك");
                if (!emailResult)
                {
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "فشل في إرسال رمز التحقق",
                        ResultType = "otp_send_failed"
                    };
                }

                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = "تم إرسال رمز التحقق بنجاح",
                    ResultType = "otp_sent"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في إعادة ربط الاشتراك: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = "خطأ في العملية",
                    ResultType = "rebind_error"
                };
            }
        }

        public async Task<MacroFortActivationResult> ConfirmRebindAsync(string email, string code, string otpCode, string newHardwareId)
        {
            try
            {
                var verifyResult = await VerifyOtpInDatabaseAsync(email, otpCode);
                if (!verifyResult)
                {
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "كود التحقق غير صحيح أو منتهي الصلاحية",
                        ResultType = "invalid_otp"
                    };
                }

                var subscription = await GetSubscriptionByEmailAsync(email);
                if (subscription == null)
                {
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لم يتم العثور على اشتراك",
                        ResultType = "subscription_not_found"
                    };
                }

                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = "تم تأكيد إعادة الربط بنجاح",
                    ResultType = "rebind_confirmed",
                    SubscriptionData = subscription
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تأكيد إعادة الربط: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = "خطأ في العملية",
                    ResultType = "confirm_rebind_error"
                };
            }
        }

        public async Task<MacroFortActivationResult> CheckActivationStatusAsync(string email)
        {
            try
            {
                var subscription = await GetSubscriptionByEmailAsync(email);
                if (subscription == null)
                {
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لم يتم العثور على اشتراك",
                        ResultType = "subscription_not_found"
                    };
                }

                if (!subscription.IsActive)
                {
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "الاشتراك غير نشط",
                        ResultType = "subscription_inactive"
                    };
                }

                var remainingDays = (subscription.ExpiryDate - DateTime.UtcNow).TotalDays;
                if (remainingDays <= 0)
                {
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "انتهت صلاحية الاشتراك",
                        ResultType = "subscription_expired"
                    };
                }

                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = "الاشتراك نشط",
                    ResultType = "subscription_active",
                    SubscriptionData = subscription,
                    ExpiryDate = subscription.ExpiryDate
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في التحقق من حالة الاشتراك: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = "خطأ في العملية",
                    ResultType = "check_status_error"
                };
            }
        }

        private async Task<(bool canProceed, string message, double? remainingMinutes)> CheckOtpSpamStatusAsync(string email)
        {
            try
            {
                var normalizedEmail = email.ToLower().Trim();


                using (var client = new HttpClient())
                {


                    var encodedEmail = System.Net.WebUtility.UrlEncode(normalizedEmail);
                    var url = $"{RAILWAY_PROXY_URL}/get-last-otp?email={encodedEmail}";
                    
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                        return (true, "", null);

                    var content = await response.Content.ReadAsStringAsync();
                    var records = JsonConvert.DeserializeObject<List<dynamic>>(content);

                    if (records == null || records.Count == 0)
                        return (true, "", null);

                    dynamic lastRecord = records[0];
                    
                    var throttleUntilStr = lastRecord.throttle_until?.ToString();
                    if (!string.IsNullOrEmpty(throttleUntilStr))
                    {
                        if (DateTime.TryParse(throttleUntilStr, out DateTime throttleUntil))
                        {
                            if (DateTime.UtcNow < throttleUntil)
                            {
                                var remainingMinutes = (throttleUntil - DateTime.UtcNow).TotalMinutes;
                                if (remainingMinutes > 0)
                                {
                                    return (false, $"لقد تجاوزت حد طلبات التحقق. حاول بعد {remainingMinutes:F0} دقيقة", remainingMinutes);
                                }
                            }
                        }
                    }

                    var lastOtpSentStr = lastRecord.last_otp_sent_at?.ToString();
                    if (!string.IsNullOrEmpty(lastOtpSentStr))
                    {
                        if (DateTime.TryParse(lastOtpSentStr, out DateTime lastOtpSent))
                        {
                            var secondsSinceLastRequest = DateTime.UtcNow.Subtract(lastOtpSent).TotalSeconds;
                            
                            if (secondsSinceLastRequest < 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ تحذير: last_otp_sent_at في المستقبل! الفرق: {secondsSinceLastRequest} ثانية");
                                secondsSinceLastRequest = MIN_OTP_REQUEST_INTERVAL_SECONDS;
                            }
                            
                            if (secondsSinceLastRequest < MIN_OTP_REQUEST_INTERVAL_SECONDS)
                            {
                                var remainingSeconds = MIN_OTP_REQUEST_INTERVAL_SECONDS - secondsSinceLastRequest;
                                return (false, $"انتظر {remainingSeconds:F0} ثانية قبل طلب جديد", null);
                            }
                        }
                    }

                    var otpRequestCount = lastRecord.otp_request_count ?? 0;
                    var createdAtStr = lastRecord.created_at?.ToString();
                    
                    if (!string.IsNullOrEmpty(createdAtStr))
                    {
                        var createdAt = DateTime.Parse(createdAtStr);
                        var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
                        
                        if (createdAt > cutoffTime && otpRequestCount >= MAX_OTP_REQUESTS_PER_10_MINUTES)
                        {
                            var throttleTime = DateTime.UtcNow.AddMinutes(THROTTLE_DURATION_MINUTES);
                            await UpdateSpamTrackingAsync(normalizedEmail, null, null, throttleTime);
                            return (false, $"لقد تجاوزت حد طلبات التحقق ({MAX_OTP_REQUESTS_PER_10_MINUTES} في 10 دقائق). حاول بعد {THROTTLE_DURATION_MINUTES} دقائق", (double)THROTTLE_DURATION_MINUTES);
                        }
                    }

                    return (true, "", null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في فحص السبام: {ex.Message}");
                return (true, "", null);
            }
        }

        private async Task<bool> RecordOtpRequestAsync(string email)
        {
            try
            {
                var normalizedEmail = email.ToLower().Trim();


                using (var client = new HttpClient())
                {


                    var encodedEmail = System.Net.WebUtility.UrlEncode(normalizedEmail);
                    var url = $"{RAILWAY_PROXY_URL}/get-last-otp?email={encodedEmail}";
                    
                    var getResponse = await client.GetAsync(url);
                    if (!getResponse.IsSuccessStatusCode)
                        return false;

                    var content = await getResponse.Content.ReadAsStringAsync();
                    var records = JsonConvert.DeserializeObject<List<dynamic>>(content);
                    
                    int newCount = 1;
                    if (records != null && records.Count > 0)
                    {
                        dynamic lastRecord = records[0];
                        var createdAtStr = lastRecord.created_at?.ToString();
                        if (!string.IsNullOrEmpty(createdAtStr))
                        {
                            var createdAt = DateTime.Parse(createdAtStr);
                            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
                            
                            if (createdAt > cutoffTime)
                            {
                                var currentCount = lastRecord.otp_request_count ?? 0;
                                newCount = currentCount + 1;
                            }
                        }
                    }

                    return await UpdateSpamTrackingAsync(normalizedEmail, DateTime.UtcNow, newCount, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تسجيل طلب OTP: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> UpdateSpamTrackingAsync(string email, DateTime? lastOtpSentAt, int? otpRequestCount, DateTime? throttleUntil)
        {
            try
            {
                var normalizedEmail = email.ToLower().Trim();


                var updateData = new Dictionary<string, object>();
                if (lastOtpSentAt.HasValue)
                    updateData["last_otp_sent_at"] = lastOtpSentAt.Value;
                if (otpRequestCount.HasValue)
                    updateData["otp_request_count"] = otpRequestCount.Value;
                if (throttleUntil.HasValue)
                {
                    updateData["is_throttled"] = true;
                    updateData["throttle_until"] = throttleUntil.Value;
                }

                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                    var encodedEmail = System.Net.WebUtility.UrlEncode(normalizedEmail);
                    var url = $"{RAILWAY_PROXY_URL}/clear-otp?email={encodedEmail}";

                    var json = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PatchAsync(url, content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تحديث سجل السبام: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// تسجيل محاولة التحقق من الجهاز في جدول السجل
        /// </summary>
        private async Task<bool> LogHardwareVerificationAsync(string subscriptionId, string email, string hardwareId, 
            string rawComponents, string result, string osVersion = null, string errorDetails = null)
        {
            try
            {


                System.Diagnostics.Debug.WriteLine($"📝 تسجيل محاولة التحقق من الجهاز - النتيجة: {result}");

                var logEntry = new
                {
                    subscription_id = string.IsNullOrEmpty(subscriptionId) ? (object)null : subscriptionId,
                    email = email,
                    hardware_id = hardwareId,
                    raw_components = string.IsNullOrEmpty(rawComponents) ? null : Newtonsoft.Json.Linq.JObject.Parse(rawComponents),
                    verification_result = result,
                    error_details = string.IsNullOrEmpty(errorDetails) ? null : new { message = errorDetails },
                    client_ip = await GetClientIpAsync(),
                    os_version = osVersion,
                    verified_at = DateTime.UtcNow
                };

                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                    var json = JsonConvert.SerializeObject(logEntry, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"{RAILWAY_PROXY_URL}/log-hardware-verification";
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ تم تسجيل محاولة التحقق بنجاح");
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"⚠️ فشل تسجيل محاولة التحقق: {response.StatusCode} - {errorContent}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في تسجيل محاولة التحقق: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// الحصول على عنوان IP للعميل
        /// </summary>
        private async Task<string> GetClientIpAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://api.ipify.org");
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ فشل الحصول على عنوان IP: {ex.Message}");
            }
            return "unknown";
        }

        /// <summary>
        /// الحصول على نسخة نظام التشغيل
        /// </summary>
        private string GetOsVersion()
        {
            try
            {
                var osVersion = System.Environment.OSVersion;
                return $"{osVersion.Platform} {osVersion.VersionString}";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// استرجاع الكود عبر Auth Proxy (/redeem-code endpoint)
        /// يقوم بـ: تحديث codes table + إنشاء/تحديث subscriptions table
        /// </summary>
        private async Task<MacroFortActivationResult> RedeemCodeViaAuthProxyAsync(string subscriptionCode, string email, string hardwareId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subscriptionCode) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(hardwareId))
                {
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "البيانات المطلوبة ناقصة",
                        ResultType = "invalid_input"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"🔄 استدعاء /redeem-code endpoint للكود: {subscriptionCode}");

                using (var client = new HttpClient())
                {
                    var requestData = new
                    {
                        code = subscriptionCode,
                        email = email,
                        hardware_id = hardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var authProxyUrl = "https://sr3h-auth-proxy-production.up.railway.app/redeem-code";
                    
                    System.Diagnostics.Debug.WriteLine($"🔗 POST طلب إلى: {authProxyUrl}");
                    
                    var response = await client.PostAsync(authProxyUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);

                        System.Diagnostics.Debug.WriteLine($"✓ تم استرجاع الكود بنجاح: {response.StatusCode}");

                        var subscriptionData = await GetSubscriptionByEmailAsync(email);
                        
                        if (subscriptionData != null)
                        {
                            return new MacroFortActivationResult
                            {
                                IsSuccess = true,
                                Message = "تم استرجاع الكود بنجاح",
                                ResultType = "activation_success",
                                ExpiryDate = subscriptionData.ExpiryDate,
                                SubscriptionData = subscriptionData
                            };
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("⚠️ تم الاسترجاع لكن لم يتم العثور على الاشتراك");
                            return new MacroFortActivationResult
                            {
                                IsSuccess = false,
                                Message = "فشل في جلب بيانات الاشتراك",
                                ResultType = "subscription_fetch_failed"
                            };
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ فشل الاسترجاع: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"📋 تفاصيل الخطأ: {errorContent}");

                        var errorData = JsonConvert.DeserializeObject<dynamic>(errorContent);
                        var errorMessage = errorData?.message?.ToString() ?? "فشل استرجاع الكود";

                        return new MacroFortActivationResult
                        {
                            IsSuccess = false,
                            Message = errorMessage,
                            ResultType = "redeem_failed"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في استدعاء /redeem-code: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ في الاتصال بخادم الاسترجاع: {ex.Message}",
                    ResultType = "connection_error"
                };
            }
        }
    }
}
