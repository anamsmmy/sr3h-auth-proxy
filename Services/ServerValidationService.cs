using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MacroApp.Services
{
    public class ServerValidationResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("subscription_type")]
        public string SubscriptionType { get; set; }

        [JsonProperty("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("subscription_expired")]
        public bool SubscriptionExpired { get; set; }

        [JsonProperty("email_verified")]
        public bool? EmailVerified { get; set; }

        [JsonProperty("device_count")]
        public int? DeviceCount { get; set; }

        [JsonProperty("max_devices")]
        public int? MaxDevices { get; set; }

        [JsonProperty("is_trial")]
        public bool? IsTrial { get; set; }
    }

    public class ServerValidationService
    {
        private readonly MacroFortActivationService _activationService;

        // ✅ الاتصال عبر الخادم الوسيط فقط
        private readonly string _proxyUrl = "https://sr3h-auth-proxy-production.up.railway.app";
        private const int RequestTimeoutSeconds = 10;

        public ServerValidationService()
        {
            _activationService = MacroFortActivationService.Instance;
        }

        // ✅ النسخة الجديدة الكاملة للدالة
        public async Task<ServerValidationResponse> ValidateSubscriptionAsync(string email, string hardwareId)
        {
            const int maxRetries = 3;
            int attemptNumber = 0;

            while (attemptNumber < maxRetries)
            {
                attemptNumber++;
                try
                {
                    var response = new ServerValidationResponse();
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                        var url = $"{_proxyUrl}/verify";

                        var requestBody = new
                        {
                            email = email,
                            hardware_id = hardwareId
                        };

                        var json = JsonConvert.SerializeObject(requestBody);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        System.Diagnostics.Debug.WriteLine($"🔄 محاولة الاتصال ({attemptNumber}/{maxRetries}): {url}");

                        var httpResponse = await client.PostAsync(url, content);
                        var resultContent = await httpResponse.Content.ReadAsStringAsync();

                        System.Diagnostics.Debug.WriteLine($"📡 Status: {(int)httpResponse.StatusCode} {httpResponse.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"📝 Response Body: {resultContent}");

                        if (httpResponse.IsSuccessStatusCode)
                        {
                            var result = JObject.Parse(resultContent);

                            response.Success = result["success"]?.Value<bool>() ?? false;
                            response.Message = result["message"]?.Value<string>();
                            response.SubscriptionType = result["subscription_type"]?.Value<string>();
                            response.ExpiryDate = result["expiry_date"]?.Value<DateTime?>();
                            response.IsActive = result["is_active"]?.Value<bool>() ?? false;
                            response.SubscriptionExpired = result["subscription_expired"]?.Value<bool>() ?? false;
                            response.EmailVerified = result["email_verified"]?.Value<bool?>();
                            response.DeviceCount = result["device_count"]?.Value<int?>();
                            response.MaxDevices = result["max_devices"]?.Value<int?>();
                            response.IsTrial = result["is_trial"]?.Value<bool?>();

                            System.Diagnostics.Debug.WriteLine($"✓ تحقق من السيرفر: {response.Message}");
                            return response;
                        }
                        else if ((int)httpResponse.StatusCode >= 500)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ خطأ من الخادم ({httpResponse.StatusCode}). إعادة المحاولة...");
                            if (attemptNumber < maxRetries)
                            {
                                await System.Threading.Tasks.Task.Delay(1000 * attemptNumber);
                                continue;
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"✗ خطأ في الاتصال: {httpResponse.StatusCode}");
                        response.Success = false;
                        response.Message = $"فشل الاتصال بخادم التحقق ({(int)httpResponse.StatusCode})";
                        return response;
                    }
                }
                catch (TaskCanceledException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⏱️ انتهاء الوقت المسموح (Timeout): {ex.Message}");
                    if (attemptNumber < maxRetries)
                    {
                        await System.Threading.Tasks.Task.Delay(1000 * attemptNumber);
                        continue;
                    }
                    return new ServerValidationResponse
                    {
                        Success = false,
                        Message = "انتهاء الوقت المسموح للاتصال بالخادم"
                    };
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"🌐 خطأ في الاتصال (محاولة {attemptNumber}/{maxRetries}): {ex.Message}");
                    if (attemptNumber < maxRetries)
                    {
                        await System.Threading.Tasks.Task.Delay(1000 * attemptNumber);
                        continue;
                    }
                    return new ServerValidationResponse
                    {
                        Success = false,
                        Message = "لا يوجد اتصال بالإنترنت"
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ خطأ: {ex.GetType().Name} - {ex.Message}");
                    if (attemptNumber < maxRetries)
                    {
                        await System.Threading.Tasks.Task.Delay(1000 * attemptNumber);
                        continue;
                    }
                    return new ServerValidationResponse
                    {
                        Success = false,
                        Message = $"خطأ: {ex.Message}"
                    };
                }
            }

            return new ServerValidationResponse
            {
                Success = false,
                Message = "فشل التحقق بعد عدة محاولات"
            };
        }

        public bool IsValidationStale(DateTime? lastVerified)
        {
            if (lastVerified == null)
                return true;

            var hoursSinceVerification = (DateTime.UtcNow - lastVerified.Value).TotalHours;
            return hoursSinceVerification >= 24;
        }

        public async Task<ServerValidationResponse> PeriodicVerifyAsync(string email, string hardwareId)
        {
            try
            {
                var response = new ServerValidationResponse();
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/verify-periodic";

                    var requestBody = new
                    {
                        email = email,
                        hardware_id = hardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 التحقق الدوري: {url}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);

                        response.Success = result["success"]?.Value<bool>() ?? false;
                        response.Message = result["message"]?.Value<string>();
                        response.SubscriptionType = result["subscription_type"]?.Value<string>();
                        response.ExpiryDate = result["expiry_date"]?.Value<DateTime?>();
                        response.IsActive = result["is_active"]?.Value<bool>() ?? false;
                        response.SubscriptionExpired = result["subscription_expired"]?.Value<bool>() ?? false;
                        response.EmailVerified = result["email_verified"]?.Value<bool?>();
                        response.DeviceCount = result["device_count"]?.Value<int?>();
                        response.MaxDevices = result["max_devices"]?.Value<int?>();
                        response.IsTrial = result["is_trial"]?.Value<bool?>();

                        System.Diagnostics.Debug.WriteLine($"✓ التحقق الدوري: {response.Message}");
                        return response;
                    }

                    System.Diagnostics.Debug.WriteLine($"✗ فشل التحقق الدوري: {httpResponse.StatusCode}");
                    response.Success = false;
                    response.Message = $"فشل التحقق ({(int)httpResponse.StatusCode})";
                    return response;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التحقق الدوري: {ex.Message}");
                return new ServerValidationResponse
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }
    }
}
