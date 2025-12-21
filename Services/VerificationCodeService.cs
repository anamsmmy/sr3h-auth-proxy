using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MacroApp.Services
{
    public class OtpResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("otp_code")]
        public string OtpCode { get; set; }

        [JsonProperty("expires_in_seconds")]
        public int? ExpiresInSeconds { get; set; }
    }

    public class OtpVerifyResponse
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
    }

    public class VerificationCodeResult
    {
        [JsonProperty("success")]
        public bool IsSuccess { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        public string ErrorMessage => Message;
    }

    public class VerificationCodeService
    {
        private readonly string _proxyUrl = "https://sr3h-auth-proxy-production.up.railway.app";
        private const int RequestTimeoutSeconds = 10;

        public async Task<VerificationCodeResult> ValidateCodeAsync(string email, string orderId, string code)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/validate-code";

                    var requestBody = new
                    {
                        code = code,
                        email = email,
                        hardware_id = orderId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 التحقق من الكود: {code}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        return new VerificationCodeResult
                        {
                            IsSuccess = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>()
                        };
                    }

                    return new VerificationCodeResult
                    {
                        IsSuccess = false,
                        Message = $"فشل التحقق ({(int)httpResponse.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التحقق من الكود: {ex.Message}");
                return new VerificationCodeResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }

        public async Task<VerificationCodeResult> GenerateAndSendCodeAsync(string email, string orderId = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/generate-otp";

                    var requestBody = new { email = email };
                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 توليد OTP للبريد: {email}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        return new VerificationCodeResult
                        {
                            IsSuccess = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>()
                        };
                    }

                    return new VerificationCodeResult
                    {
                        IsSuccess = false,
                        Message = $"فشل التوليد ({(int)httpResponse.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في توليد OTP: {ex.Message}");
                return new VerificationCodeResult
                {
                    IsSuccess = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }

        public async Task<OtpResponse> GenerateOtpAsync(string email)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/generate-otp";

                    var requestBody = new { email = email };
                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 توليد OTP للبريد: {email}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        return new OtpResponse
                        {
                            Success = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>(),
                            OtpCode = result["otp_code"]?.Value<string>(),
                            ExpiresInSeconds = result["expires_in_seconds"]?.Value<int>()
                        };
                    }

                    return new OtpResponse
                    {
                        Success = false,
                        Message = $"فشل توليد OTP ({(int)httpResponse.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في توليد OTP: {ex.Message}");
                return new OtpResponse
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }

        public async Task<OtpVerifyResponse> VerifyOtpAsync(string email, string otpCode, string hardwareId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/verify-otp";

                    var requestBody = new
                    {
                        email = email,
                        otp_code = otpCode,
                        hardware_id = hardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 التحقق من OTP");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        return new OtpVerifyResponse
                        {
                            Success = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>(),
                            SubscriptionType = result["subscription_type"]?.Value<string>(),
                            ExpiryDate = result["expiry_date"]?.Value<DateTime?>(),
                            IsActive = result["is_active"]?.Value<bool>() ?? false
                        };
                    }

                    return new OtpVerifyResponse
                    {
                        Success = false,
                        Message = $"فشل التحقق ({(int)httpResponse.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التحقق من OTP: {ex.Message}");
                return new OtpVerifyResponse
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }
    }
}
