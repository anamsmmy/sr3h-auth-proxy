using System;
using Newtonsoft.Json;

namespace MacroApp.Models
{
    public class UserSubscription
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("subscription_start")]
        public DateTime SubscriptionStart { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("last_check")]
        public DateTime? LastCheck { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class AuthenticationRequest
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }
    }

    public class ReactivationRequest
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }
    }

    public class AuthenticationResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("user")]
        public UserSubscription User { get; set; }
    }

    public class LocalAuthData
    {
        public string Email { get; set; }
        public string HardwareId { get; set; }
        public DateTime LastVerified { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}