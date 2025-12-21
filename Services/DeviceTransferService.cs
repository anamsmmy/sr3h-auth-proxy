using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MacroApp.Services
{
    public class TransferInitResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("transfer_token")]
        public string TransferToken { get; set; }

        [JsonProperty("expires_in_seconds")]
        public int? ExpiresInSeconds { get; set; }
    }

    public class TransferCompleteResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("subscription_type")]
        public string SubscriptionType { get; set; }

        [JsonProperty("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [JsonProperty("device_count")]
        public int? DeviceCount { get; set; }
    }

    public class DeviceTransferService
    {
        private readonly string _proxyUrl = "https://sr3h-auth-proxy-production.up.railway.app";
        private const int RequestTimeoutSeconds = 10;

        public async Task<TransferInitResponse> InitiateTransferAsync(string email, string currentHardwareId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/initiate-device-transfer";

                    var requestBody = new
                    {
                        email = email,
                        current_hardware_id = currentHardwareId
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 بدء عملية نقل الجهاز");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        return new TransferInitResponse
                        {
                            Success = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>(),
                            TransferToken = result["transfer_token"]?.Value<string>(),
                            ExpiresInSeconds = result["expires_in_seconds"]?.Value<int>()
                        };
                    }

                    return new TransferInitResponse
                    {
                        Success = false,
                        Message = $"فشل البدء ({(int)httpResponse.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في بدء النقل: {ex.Message}");
                return new TransferInitResponse
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }

        public async Task<TransferCompleteResponse> CompleteTransferAsync(string email, string newHardwareId, string transferToken)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                    var url = $"{_proxyUrl}/complete-device-transfer";

                    var requestBody = new
                    {
                        email = email,
                        new_hardware_id = newHardwareId,
                        transfer_token = transferToken
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"🔄 إتمام عملية نقل الجهاز");

                    var httpResponse = await client.PostAsync(url, content);
                    var resultContent = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(resultContent);
                        return new TransferCompleteResponse
                        {
                            Success = result["success"]?.Value<bool>() ?? false,
                            Message = result["message"]?.Value<string>(),
                            SubscriptionType = result["subscription_type"]?.Value<string>(),
                            ExpiryDate = result["expiry_date"]?.Value<DateTime?>(),
                            DeviceCount = result["device_count"]?.Value<int>()
                        };
                    }

                    return new TransferCompleteResponse
                    {
                        Success = false,
                        Message = $"فشل الإتمام ({(int)httpResponse.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إتمام النقل: {ex.Message}");
                return new TransferCompleteResponse
                {
                    Success = false,
                    Message = $"خطأ: {ex.Message}"
                };
            }
        }
    }
}
