using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using MacroApp.Models;
using System.Net.Http;

namespace MacroApp.Services
{
    public class AuthenticationService
    {
        private readonly string _authFilePath;
        private readonly HttpClient _httpClient;
        private readonly ConfigurationService _configService;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private readonly int _offlineGraceDays;

        public AuthenticationService()
        {
            _configService = ConfigurationService.Instance;
            
            _supabaseUrl = SecureSupabaseConfig.GetSupabaseUrl();
            _supabaseKey = SecureSupabaseConfig.GetSupabaseKey();
            
            _offlineGraceDays = _configService.Settings.Authentication.OfflineGraceDays;
            _authFilePath = _configService.GetAuthFilePath();
            
            if (!SecureSupabaseConfig.ValidateConnection())
            {
                throw new InvalidOperationException("فشل في تحميل إعدادات الاتصال الآمنة");
            }
            
            _httpClient = new HttpClient();
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var localAuth = LoadLocalAuthData();
                if (localAuth == null || !localAuth.IsAuthenticated)
                    return false;

                // Check if we're within offline grace period
                var daysSinceLastVerification = (DateTime.Now - localAuth.LastVerified).TotalDays;
                if (daysSinceLastVerification <= _offlineGraceDays)
                    return true;

                // Grace period expired, need to verify online
                return await VerifyOnlineAsync(localAuth.Email, localAuth.HardwareId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(string email, string subscriptionCode = null)
        {
            try
            {
                string hardwareId = HardwareIdService.GenerateHardwareId();
                
                // إذا لم يكن هناك كود، أنشئ اشتراك تجريبي
                if (string.IsNullOrWhiteSpace(subscriptionCode))
                {
                    return await CreateTrialSubscriptionAsync(email, hardwareId);
                }

                // إذا كان هناك كود، استرجع الاشتراك
                return await RedeemSubscriptionCodeAsync(email, subscriptionCode, hardwareId);
            }
            catch (Exception ex)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Message = $"خطأ في الاتصال: {ex.Message}"
                };
            }
        }

        private async Task<AuthenticationResponse> CreateTrialSubscriptionAsync(string email, string hardwareId)
        {
            try
            {
                var requestData = new
                {
                    p_email = email,
                    p_hardware_id = hardwareId
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_supabaseUrl}/rest/v1/rpc/create_trial_subscription", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var supabaseResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    
                    var authResponse = new AuthenticationResponse
                    {
                        Success = (bool)supabaseResponse.success,
                        Message = (string)supabaseResponse.message
                    };
                    
                    if (authResponse.Success)
                    {
                        var localAuth = new LocalAuthData
                        {
                            Email = email,
                            HardwareId = hardwareId,
                            LastVerified = DateTime.Now,
                            IsAuthenticated = true
                        };
                        SaveLocalAuthData(localAuth);
                    }
                    
                    return authResponse;
                }
                else
                {
                    return new AuthenticationResponse
                    {
                        Success = false,
                        Message = $"خطأ في إنشاء الاشتراك التجريبي: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Message = $"خطأ في إنشاء الاشتراك التجريبي: {ex.Message}"
                };
            }
        }

        private async Task<AuthenticationResponse> RedeemSubscriptionCodeAsync(string email, string subscriptionCode, string hardwareId)
        {
            try
            {
                var requestData = new
                {
                    p_code = subscriptionCode,
                    p_email = email,
                    p_hardware_id = hardwareId
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_supabaseUrl}/rest/v1/rpc/redeem_subscription_code", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var supabaseResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    
                    var authResponse = new AuthenticationResponse
                    {
                        Success = (bool)supabaseResponse.success,
                        Message = (string)supabaseResponse.message
                    };
                    
                    if (authResponse.Success)
                    {
                        var localAuth = new LocalAuthData
                        {
                            Email = email,
                            HardwareId = hardwareId,
                            LastVerified = DateTime.Now,
                            IsAuthenticated = true
                        };
                        SaveLocalAuthData(localAuth);
                    }
                    
                    return authResponse;
                }
                else
                {
                    return new AuthenticationResponse
                    {
                        Success = false,
                        Message = $"خطأ في استرجاع الكود: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Message = $"خطأ في استرجاع الكود: {ex.Message}"
                };
            }
        }

        public async Task<AuthenticationResponse> ReactivateSubscriptionAsync(string email, string orderId)
        {
            try
            {
                // إنشاء طلب إعادة التفعيل بالتنسيق المطلوب لـ Supabase RPC
                var requestData = new
                {
                    user_email = email,
                    user_order_id = orderId
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_supabaseUrl}/rest/v1/rpc/reactivate_subscription", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // تحليل الاستجابة من Supabase
                    var supabaseResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    
                    return new AuthenticationResponse
                    {
                        Success = (bool)supabaseResponse.success,
                        Message = (string)supabaseResponse.message
                    };
                }
                else
                {
                    return new AuthenticationResponse
                    {
                        Success = false,
                        Message = $"خطأ في الخادم: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Message = $"خطأ في الاتصال: {ex.Message}"
                };
            }
        }

        private async Task<bool> VerifyOnlineAsync(string email, string hardwareId)
        {
            try
            {
                // إنشاء طلب التحقق بالتنسيق المطلوب لـ Supabase RPC
                var requestData = new
                {
                    user_email = email,
                    user_hardware_id = hardwareId
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_supabaseUrl}/rest/v1/rpc/verify_authentication", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // تحليل الاستجابة من Supabase
                    var supabaseResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    bool success = (bool)supabaseResponse.success;

                    if (success)
                    {
                        // تحديث وقت آخر تحقق
                        var localAuth = new LocalAuthData
                        {
                            Email = email,
                            HardwareId = hardwareId,
                            LastVerified = DateTime.Now,
                            IsAuthenticated = true
                        };
                        SaveLocalAuthData(localAuth);
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private LocalAuthData LoadLocalAuthData()
        {
            try
            {
                if (!File.Exists(_authFilePath))
                    return null;

                var encryptedData = File.ReadAllBytes(_authFilePath);
                var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decryptedData);
                
                return JsonConvert.DeserializeObject<LocalAuthData>(json);
            }
            catch
            {
                return null;
            }
        }

        private void SaveLocalAuthData(LocalAuthData authData)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_authFilePath));
                
                var json = JsonConvert.SerializeObject(authData);
                var data = Encoding.UTF8.GetBytes(json);
                var encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                
                File.WriteAllBytes(_authFilePath, encryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save authentication data: {ex.Message}", ex);
            }
        }

        public void ClearAuthenticationData()
        {
            try
            {
                if (File.Exists(_authFilePath))
                    File.Delete(_authFilePath);
            }
            catch { }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}