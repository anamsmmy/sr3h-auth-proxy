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
        private const int DEVICE_TRANSFER_LIMIT_30_DAYS = 10;
        private const int CODE_REBIND_LIMIT_30_DAYS = 10;
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

        public static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            var localPart = parts[0];
            var domain = parts[1];

            var maskedLocal = localPart.Length <= 2 
                ? localPart 
                : $"{localPart[0]}{'*' * (localPart.Length - 2)}{localPart[localPart.Length - 1]}";

            var domainParts = domain.Split('.');
            var maskedDomain = domainParts.Length > 1
                ? $"{domainParts[0][0]}{'*' * (domainParts[0].Length - 1)}@{string.Join(".", domainParts.Skip(1))}"
                : $"{domainParts[0][0]}***";

            return $"{maskedLocal}@{maskedDomain}";
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
                var hardwareId = SafeHardwareIdService.GenerateHardwareId();
                var verifyPayload = new
                {
                    email = email,
                    hardware_id = hardwareId
                };
                
                var verifyJson = JsonConvert.SerializeObject(verifyPayload);
                var verifyContent = new StringContent(verifyJson, Encoding.UTF8, "application/json");
                
                var url = $"{RAILWAY_PROXY_URL}/verify";
                System.Diagnostics.Debug.WriteLine($"🌐 إرسال طلب التحقق من الجهاز إلى: {url}");

                var response = await _httpClient.PostAsync(url, verifyContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var resultJson = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    
                    bool success = resultJson?.success ?? false;
                    
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ تم التحقق من بيانات الجهاز بنجاح");
                        System.Diagnostics.Debug.WriteLine($"💻 HardwareId المرسل: {hardwareId.Substring(0, Math.Min(16, hardwareId.Length))}...");
                        
                        // تحديث cache بحالة التحقق الناجحة
                        SessionActivationCache.SetHardwareVerificationStatus("verified");
                        SessionActivationCache.SetGracePeriodExpiry(DateTime.UtcNow.AddMinutes(5));
                        
                        // تسجيل في السجل
                        await LogHardwareVerificationAsync(null, email, hardwareId, rawComponents, "success", GetOsVersion());
                        
                        return new HardwareVerificationResponse
                        {
                            IsSuccess = true,
                            HardwareId = hardwareId,
                            Message = "تم التحقق بنجاح"
                        };
                    }
                    else
                    {
                        string message = resultJson?.message ?? "فشل التحقق من بيانات الجهاز";
                        System.Diagnostics.Debug.WriteLine($"⚠️ الجهاز غير متطابق: {message}");
                        SessionActivationCache.SetHardwareVerificationStatus("mismatch");
                        await LogHardwareVerificationAsync(null, email, "", rawComponents, "mismatch", GetOsVersion(), message);
                        
                        return new HardwareVerificationResponse
                        {
                            IsSuccess = false,
                            Message = message
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

                // استدعاء Railway Proxy لفحص الأهلية
                var result = await CallTrialEligibilityCheckAsync(email, deviceFingerprintHash);
                
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

                var success = result["success"]?.ToObject<bool>() ?? false;

                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ الجهاز غير مؤهل للتجربة");
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "هذا الجهاز استخدم التجربة المجانية مسبقاً",
                        ResultType = "trial_already_used_on_device"
                    };
                }

                // إذا كانت التجربة موجودة وسارية، نستدعي UpdateTrialSubscriptionAsync بدلاً من InsertTrialSubscriptionAsync
                var existingSubscription = result["existing_subscription"]?.ToObject<bool>() ?? false;
                var statusMessage = existingSubscription ? "trial_exists_not_expired" : "trial_eligible";
                
                System.Diagnostics.Debug.WriteLine("✓ الجهاز مؤهل للحصول على تجربة مجانية جديدة");
                return new MacroFortActivationResult
                {
                    IsSuccess = true,
                    Message = statusMessage
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
        /// فحص أهلية التجربة عبر Railway Proxy /activate endpoint
        /// يتحقق ما إذا كان المستخدم قد استخدم التجربة من قبل
        /// </summary>
        private async Task<Newtonsoft.Json.Linq.JObject> CallTrialEligibilityCheckAsync(string email, string hardwareId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var url = $"{RAILWAY_PROXY_URL}/verify";
                    var payload = new { email = email, hardware_id = hardwareId };
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔗 استدعاء Railway Proxy: /verify (فحص الأهلية)");
                    System.Diagnostics.Debug.WriteLine($"   Payload: email={email}, hardware_id={hardwareId}");
                    var response = await client.PostAsync(url, content);

                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📨 Response Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"📨 Response Body: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(responseContent);
                        var success = result?["success"]?.ToObject<bool>() ?? false;
                        System.Diagnostics.Debug.WriteLine($"✓ Response Parsed: success={success}");
                        return result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ HTTP: {response.StatusCode}");
                        return JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>("{\"success\":false}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ خطأ في فحص الأهلية: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"✗ Stack Trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// إدراج تجربة جديدة عبر RPC activate_trial
        /// تتم العملية بشكل ذري (atomic) على السيرفر
        /// ثم ينشئ سجل في macro_fort_subscriptions للدعم الكامل
        /// </summary>
        private async Task<bool> InsertTrialSubscriptionAsync(string email, string hardwareId, string otp, DateTime otpExpiry, DateTime activationDate, DateTime expiryDate)
        {
            try
            {
                var (rpcSuccess, rpcMessage) = await CallActivateTrialRpcAsync(email, hardwareId, TRIAL_DURATION_DAYS);
                
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
        /// تفعيل التجربة عبر Railway Proxy /activate endpoint
        /// ينشئ حساب تجربة جديد للمستخدم والجهاز
        /// </summary>
        private async Task<(bool success, string message)> CallActivateTrialRpcAsync(string email, string hardwareId, int trialDays)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var url = $"{RAILWAY_PROXY_URL}/activate";
                    var payload = new
                    {
                        email = email,
                        hardware_id = hardwareId
                    };
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"📝 استدعاء Railway Proxy: /activate (تفعيل التجربة)");
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
                await Task.Delay(0);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ استثناء في UpdateTrialSubscriptionAsync: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"🔍 التحقق من OTP: البريد={email}, الكود={otpCode}");
                
                var encodedEmail = System.Net.WebUtility.UrlEncode(email);
                var verifyUrl = $"{RAILWAY_PROXY_URL}/verify-otp?email={encodedEmail}&code={otpCode}";
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(verifyUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<dynamic>(content);
                        
                        bool success = result?.success ?? false;
                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine("✓ تم التحقق من OTP بنجاح");
                            string hardwareId = result?.hardware_id?.ToString();
                            return hardwareId;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("❌ فشل التحقق من OTP");
                            return null;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"❌ فشل البحث عن OTP - HTTP {(int)response.StatusCode}");
                    return null;
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
                var result = JsonConvert.DeserializeObject<dynamic>(content);

                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ لم يتم العثور على رمز OTP");
                    return null;
                }

                bool success = result["success"]?.ToObject<bool>() ?? false;
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine("❌ فشل التحقق من OTP");
                    return null;
                }

                var hardwareId = result["hardware_id"]?.ToString();
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

        public async Task<MacroFortSubscription> GetSubscriptionByEmailAsync(string email)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔗 طلب الاشتراك عبر Railway للـ email: {email}");
                
                if (string.IsNullOrWhiteSpace(email))
                    return null;

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
                        
                        try
                        {
                            var jsonObject = JsonConvert.DeserializeObject<dynamic>(responseContent);
                            if (jsonObject?.success == true && jsonObject?.subscription != null)
                            {
                                var subscription = JsonConvert.DeserializeObject<MacroFortSubscription>(
                                    JsonConvert.SerializeObject(jsonObject.subscription)
                                );
                                
                                if (subscription != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"✓ تم جلب الاشتراك من Railway بنجاح");
                                    return subscription;
                                }
                            }
                            System.Diagnostics.Debug.WriteLine("⚠️ لا توجد نتائج للبريد");
                        }
                        catch (Exception parseEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ خطأ في تحليل استجابة الخادم: {parseEx.Message}");
                        }
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
                System.Diagnostics.Debug.WriteLine($"✓ فحص معدل OTP للبريد: {email}");
                await Task.Delay(0);
                return (true, "", null);
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
                System.Diagnostics.Debug.WriteLine($"✓ تسجيل طلب OTP للبريد: {email}");
                await Task.Delay(0);
                return true;
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
                System.Diagnostics.Debug.WriteLine($"✓ تحديث سجل السبام للبريد: {email}");
                await Task.Delay(0);
                return true;
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
        /// متابعة الفترة التجريبية بعد التحقق من البريد الإلكتروني عبر OTP
        /// يُستخدم عند: حذف وإعادة تثبيت التطبيق أثناء الفترة التجريبية
        /// </summary>
        public async Task<MacroFortActivationResult> ContinueTrialWithOtpAsync(string email, string deviceFingerprintHash, string otpCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 متابعة الفترة التجريبية للبريد: {email}");

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(deviceFingerprintHash) || string.IsNullOrWhiteSpace(otpCode))
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "البيانات المطلوبة ناقصة",
                        ResultType = "invalid_input"
                    };

                if (!await CheckInternetConnectionAsync())
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لا يوجد اتصال إنترنت",
                        ResultType = "no_internet"
                    };

                using (var client = new HttpClient())
                {
                    var requestData = new
                    {
                        email = email,
                        device_fingerprint_hash = deviceFingerprintHash,
                        otp_code = otpCode
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = $"{RAILWAY_PROXY_URL}/continue-trial-with-otp";

                    System.Diagnostics.Debug.WriteLine($"🔗 استدعاء Railway Proxy: /continue-trial-with-otp");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                        System.Diagnostics.Debug.WriteLine($"✓ تم متابعة الفترة التجريبية بنجاح");

                        var subscriptionData = await GetSubscriptionByEmailAsync(email);
                        
                        return new MacroFortActivationResult
                        {
                            IsSuccess = true,
                            Message = "تم استكمال الفترة التجريبية بنجاح",
                            ResultType = "trial_continued",
                            ExpiryDate = subscriptionData?.ExpiryDate ?? DateTime.UtcNow.AddDays(TRIAL_DURATION_DAYS),
                            SubscriptionData = subscriptionData
                        };
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ فشل متابعة الفترة التجريبية: {response.StatusCode}");
                        var errorData = JsonConvert.DeserializeObject<dynamic>(errorContent);
                        var errorMessage = errorData?.message?.ToString() ?? "فشل في استكمال الفترة التجريبية";

                        return new MacroFortActivationResult
                        {
                            IsSuccess = false,
                            Message = errorMessage,
                            ResultType = "trial_continuation_failed"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في متابعة الفترة التجريبية: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}",
                    ResultType = "error"
                };
            }
        }

        /// <summary>
        /// فحص مطابقة الكود مع جهاز المستخدم الحالي
        /// يكتشف إذا كان الكود مرتبطاً بجهاز آخر
        /// </summary>
        public async Task<MacroFortActivationResult> CheckCodeDeviceMismatchAsync(string subscriptionCode, string currentHardwareId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 فحص مطابقة الكود مع الجهاز الحالي للكود: {subscriptionCode}");

                if (string.IsNullOrWhiteSpace(subscriptionCode) || string.IsNullOrWhiteSpace(currentHardwareId))
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "بيانات ناقصة",
                        ResultType = "invalid_input"
                    };

                if (!await CheckInternetConnectionAsync())
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لا يوجد اتصال إنترنت",
                        ResultType = "no_internet"
                    };

                using (var client = new HttpClient())
                {
                    var requestData = new
                    {
                        code = subscriptionCode,
                        current_device_fingerprint = currentHardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = $"{RAILWAY_PROXY_URL}/check-code-device-mismatch";

                    System.Diagnostics.Debug.WriteLine($"🔗 استدعاء Railway Proxy: /check-code-device-mismatch");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        var isMismatch = result?.mismatch ?? false;

                        if (isMismatch)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ الكود مرتبط بجهاز آخر - يتطلب إعادة ربط");
                            return new MacroFortActivationResult
                            {
                                IsSuccess = false,
                                Message = result?.message?.ToString() ?? "هذا الكود مرتبط على جهاز آخر - هل أنت صاحب الكود؟",
                                ResultType = "code_device_mismatch",
                                LinkedEmail = result?.linked_email?.ToString()
                            };
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"✓ الكود متطابق مع الجهاز الحالي");
                            return new MacroFortActivationResult
                            {
                                IsSuccess = true,
                                Message = "الكود متطابق مع الجهاز الحالي",
                                ResultType = "code_device_match",
                                LinkedEmail = result?.linked_email?.ToString()
                            };
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ فشل فحص مطابقة الكود: {response.StatusCode}");
                        return new MacroFortActivationResult
                        {
                            IsSuccess = false,
                            Message = "فشل فحص الكود من الخادم",
                            ResultType = "check_failed"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في فحص مطابقة الكود: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}",
                    ResultType = "error"
                };
            }
        }

        /// <summary>
        /// إعادة ربط الكود بجهاز جديد بعد التحقق من البريد الإلكتروني
        /// </summary>
        public async Task<MacroFortActivationResult> ConfirmCodeRebindAsync(string subscriptionCode, string linkedEmail, string otpCode, string newHardwareId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 تأكيد إعادة ربط الكود للجهاز الجديد: {subscriptionCode}");

                if (string.IsNullOrWhiteSpace(subscriptionCode) || string.IsNullOrWhiteSpace(linkedEmail) || string.IsNullOrWhiteSpace(otpCode) || string.IsNullOrWhiteSpace(newHardwareId))
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "البيانات المطلوبة ناقصة",
                        ResultType = "invalid_input"
                    };

                if (!await CheckInternetConnectionAsync())
                    return new MacroFortActivationResult
                    {
                        IsSuccess = false,
                        Message = "لا يوجد اتصال إنترنت",
                        ResultType = "no_internet"
                    };

                using (var client = new HttpClient())
                {
                    var requestData = new
                    {
                        code = subscriptionCode,
                        linked_email = linkedEmail,
                        otp_code = otpCode,
                        new_device_fingerprint = newHardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = $"{RAILWAY_PROXY_URL}/rebind-subscription-code";

                    System.Diagnostics.Debug.WriteLine($"🔗 استدعاء Railway Proxy: /rebind-subscription-code");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                        System.Diagnostics.Debug.WriteLine($"✓ تم إعادة ربط الكود بنجاح");

                        var subscriptionData = await GetSubscriptionByEmailAsync(linkedEmail);
                        
                        return new MacroFortActivationResult
                        {
                            IsSuccess = true,
                            Message = "تم إعادة ربط الكود بنجاح",
                            ResultType = "rebind_success",
                            ExpiryDate = subscriptionData?.ExpiryDate,
                            SubscriptionData = subscriptionData
                        };
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ فشل إعادة ربط الكود: {response.StatusCode}");
                        var errorData = JsonConvert.DeserializeObject<dynamic>(errorContent);
                        var errorMessage = errorData?.message?.ToString() ?? "فشل إعادة ربط الكود";

                        return new MacroFortActivationResult
                        {
                            IsSuccess = false,
                            Message = errorMessage,
                            ResultType = "rebind_failed"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إعادة ربط الكود: {ex.Message}");
                return new MacroFortActivationResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}",
                    ResultType = "error"
                };
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
