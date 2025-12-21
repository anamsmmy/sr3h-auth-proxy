using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MacroApp.Services
{
    public class CodeValidationResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("subscription_type")]
        public string SubscriptionType { get; set; }

        [JsonProperty("duration_days")]
        public int? DurationDays { get; set; }

        [JsonProperty("expiry_date")]
        public DateTime? ExpiryDate { get; set; }
    }

    public class CodeCheckResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("subscription_type")]
        public string SubscriptionType { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }

        [JsonProperty("expiry_date")]
        public DateTime? ExpiryDate { get; set; }
    }

    public class SubscriptionCodeService
    {
        private readonly string _proxyUrl = "https://sr3h-auth-proxy-production.up.railway.app";
        private const int RequestTimeoutSeconds = 10;

        public async Task<CodeValidationResponse> ValidateSubscriptionCodeAsync(string code, string email, string hardwareId)
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
                        hardware_id = hardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 التحقق من الكود: {url}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"📡 Status: {(int)httpResponse.StatusCode}");

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        return new CodeValidationResponse
                        {
                            Success = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>(),
                            SubscriptionType = result["subscription_type"]?.Value<string>(),
                            DurationDays = result["duration_days"]?.Value<int>(),
                            ExpiryDate = result["expiry_date"]?.Value<DateTime?>()
                        };
                    }

                    return new CodeValidationResponse
                    {
                        Success = false,
                        Message = $"فشل التحقق ({(int)httpResponse.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التحقق من الكود: {ex.Message}");
                return new CodeValidationResponse
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }

        public async Task<CodeCheckResponse> CheckSubscriptionCodeAsync(string code)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/check-code";

                    var requestBody = new { code = code };
                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔍 التحقق من الكود: {code}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        var expiryDateStr = result["expiry_date"]?.Value<string>();
                        return new CodeCheckResponse
                        {
                            Success = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>(),
                            SubscriptionType = result["subscription_type"]?.Value<string>(),
                            Status = result["status"]?.Value<string>(),
                            Email = result["email"]?.Value<string>(),
                            HardwareId = result["hardware_id"]?.Value<string>(),
                            ExpiryDate = !string.IsNullOrEmpty(expiryDateStr) ? DateTime.Parse(expiryDateStr) : (DateTime?)null
                        };
                    }

                    return new CodeCheckResponse
                    {
                        Success = false,
                        Message = $"فشل التحقق ({(int)httpResponse.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التحقق من الكود: {ex.Message}");
                return new CodeCheckResponse
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }

        public async Task<bool> BindCodeToEmailAndDeviceAsync(string code, string email, string hardwareId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/bind-code";

                    var requestBody = new
                    {
                        code = code,
                        email = email,
                        hardware_id = hardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔗 ربط الكود: {code} مع {email} و {hardwareId}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        bool success = result["success"]?.Value<bool>() ?? false;
                        
                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine("✓ تم ربط الكود بنجاح");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ فشل الربط: {result["message"]}");
                        }
                        return success;
                    }

                    System.Diagnostics.Debug.WriteLine($"❌ فشل الربط - HTTP {(int)httpResponse.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في ربط الكود: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RedeemSubscriptionCodeAsync(string code, string email, string hardwareId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/redeem-code";

                    var requestBody = new
                    {
                        code = code,
                        email = email,
                        hardware_id = hardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 استرجاع الكود: {url}");
                    System.Diagnostics.Debug.WriteLine($"📤 البيانات: code={code}, email={email}, hardware_id={hardwareId}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"📊 كود الحالة: {(int)httpResponse.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"📋 محتوى الرد: {resultContent}");

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        try
                        {
                            var result = JObject.Parse(resultContent);
                            bool success = result["success"]?.Value<bool>() ?? false;
                            string message = result["message"]?.Value<string>() ?? "";
                            
                            if (success)
                            {
                                System.Diagnostics.Debug.WriteLine("✓ تم استرجاع الكود بنجاح");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ فشل الاسترجاع: {message}");
                            }
                            return success;
                        }
                        catch (Exception parseEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحليل رد الخادم: {parseEx.Message}");
                            return false;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ فشل الاسترجاع - HTTP {(int)httpResponse.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"📋 الخطأ: {resultContent}");
                        return false;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في الاتصال بالخادم: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"الـ URL: {_proxyUrl}/redeem-code");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في استرجاع الكود: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> MarkCodeAsUsedAsync(string code)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/mark-code-used";

                    var requestBody = new { code = code };
                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"✔️ تحديد الكود كمستخدم: {code}");

                    var httpResponse = await client.PostAsync(url, content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine("✓ تم تحديث حالة الكود بنجاح");
                        return true;
                    }

                    System.Diagnostics.Debug.WriteLine($"❌ فشل التحديث - HTTP {(int)httpResponse.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحديث حالة الكود: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceTransferAsync(string code, string newHardwareId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/update-device-transfer";

                    var requestBody = new
                    {
                        code = code,
                        new_hardware_id = newHardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"📱 تحديث نقل الجهاز: {code}");

                    var httpResponse = await client.PostAsync(url, content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine("✓ تم تحديث نقل الجهاز بنجاح");
                        return true;
                    }

                    System.Diagnostics.Debug.WriteLine($"❌ فشل التحديث - HTTP {(int)httpResponse.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحديث نقل الجهاز: {ex.Message}");
                return false;
            }
        }

        public class CodeDetails
        {
            public string Code { get; set; }
            public string SubscriptionType { get; set; }
            public bool IsUsed { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public string Email { get; set; }
            public string HardwareId { get; set; }
        }

        public async Task<CodeDetails> GetCodeDetailsAsync(string code)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/check-code";

                    var requestBody = new { code = code };
                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔍 فحص تفاصيل الكود: {code}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        bool success = result["success"]?.Value<bool>() ?? false;

                        if (success)
                        {
                            var expiryDateStr = result["expiry_date"]?.Value<string>();
                            return new CodeDetails
                            {
                                Code = code,
                                SubscriptionType = result["subscription_type"]?.Value<string>() ?? "month",
                                IsUsed = result["status"]?.Value<string>() == "used",
                                ExpiryDate = !string.IsNullOrEmpty(expiryDateStr) ? DateTime.Parse(expiryDateStr) : (DateTime?)null,
                                Email = result["email"]?.Value<string>(),
                                HardwareId = result["hardware_id"]?.Value<string>()
                            };
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"❌ فشل فحص الكود - HTTP {(int)httpResponse.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في فحص الكود: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SyncCodeStatusAsync(string code)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/sync-code-status";

                    var requestBody = new { code = code };
                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 مزامنة حالة الكود: {code}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        try
                        {
                            var result = JObject.Parse(resultContent);
                            bool success = result["success"]?.Value<bool>() ?? false;
                            string newStatus = result["status"]?.Value<string>();
                            
                            if (success)
                            {
                                System.Diagnostics.Debug.WriteLine($"✓ تم مزامنة حالة الكود: {code} → {newStatus}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ فشلت المزامنة: {result["message"]}");
                            }
                            return success;
                        }
                        catch (Exception parseEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحليل رد الخادم: {parseEx.Message}");
                            return false;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ فشلت المزامنة - HTTP {(int)httpResponse.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"📋 الخطأ: {resultContent}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في مزامنة حالة الكود: {ex.Message}");
                return false;
            }
        }

        public class CodeTransferVerification
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("subscription_type")]
            public string SubscriptionType { get; set; }

            [JsonProperty("current_hardware_id")]
            public string CurrentHardwareId { get; set; }

            [JsonProperty("can_transfer")]
            public bool CanTransfer { get; set; }

            [JsonProperty("transfers_remaining")]
            public int TransfersRemaining { get; set; }

            [JsonProperty("transfer_limit_reason")]
            public string TransferLimitReason { get; set; }
        }

        public async Task<CodeTransferVerification> VerifyCodeForTransferAsync(string code, string email)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/verify-code-for-transfer";

                    var requestBody = new
                    {
                        code = code,
                        email = email
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔍 التحقق من الكود لإعادة الربط: {code}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"📊 كود الحالة: {(int)httpResponse.StatusCode}");

                    try
                    {
                        var result = JObject.Parse(resultContent);
                        var verification = new CodeTransferVerification
                        {
                            Success = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>(),
                            Code = result["code"]?.Value<string>(),
                            SubscriptionType = result["subscription_type"]?.Value<string>(),
                            CurrentHardwareId = result["current_hardware_id"]?.Value<string>(),
                            CanTransfer = result["can_transfer"]?.Value<bool>() ?? false,
                            TransfersRemaining = result["transfers_remaining"]?.Value<int>() ?? 0,
                            TransferLimitReason = result["transfer_limit_reason"]?.Value<string>()
                        };

                        if (verification.Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓ الكود صحيح - متبقي: {verification.TransfersRemaining} نقلات");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ فشل التحقق: {verification.Message}");
                        }

                        return verification;
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحليل الرد: {parseEx.Message}");
                        return new CodeTransferVerification
                        {
                            Success = false,
                            Message = $"خطأ في تحليل الرد: {parseEx.Message}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التحقق من الكود: {ex.Message}");
                return new CodeTransferVerification
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }

        public class CompleteTransferResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("transfers_used")]
            public int TransfersUsed { get; set; }
        }

        public async Task<CompleteTransferResponse> CompleteDeviceTransferAsync(string code, string email, string newHardwareId, string oldHardwareId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/complete-device-transfer";

                    var requestBody = new
                    {
                        code = code,
                        email = email,
                        new_hardware_id = newHardwareId,
                        old_hardware_id = oldHardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"📱 إكمال نقل الجهاز: {code}");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"📊 كود الحالة: {(int)httpResponse.StatusCode}");

                    try
                    {
                        var result = JObject.Parse(resultContent);
                        var response = new CompleteTransferResponse
                        {
                            Success = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>(),
                            TransfersUsed = result["transfers_used"]?.Value<int>() ?? 0
                        };

                        if (response.Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓ تم نقل الجهاز بنجاح - عمليات مستخدمة: {response.TransfersUsed}/3");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ فشل نقل الجهاز: {response.Message}");
                        }

                        return response;
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحليل الرد: {parseEx.Message}");
                        return new CompleteTransferResponse
                        {
                            Success = false,
                            Message = $"خطأ في تحليل الرد: {parseEx.Message}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إكمال نقل الجهاز: {ex.Message}");
                return new CompleteTransferResponse
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }
    }
}
