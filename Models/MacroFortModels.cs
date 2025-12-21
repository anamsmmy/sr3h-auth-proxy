using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MacroApp.Models
{
    public class MacroFortSubscription
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }

        [JsonProperty("subscription_type")]
        public string SubscriptionType { get; set; }

        [JsonProperty("activation_date")]
        public DateTime ActivationDate { get; set; }

        [JsonProperty("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonProperty("device_transfer_count")]
        public int DeviceTransferCount { get; set; }

        [JsonProperty("last_device_transfer_date")]
        public DateTime? LastDeviceTransferDate { get; set; }

        [JsonProperty("last_check_date")]
        public DateTime? LastCheckDate { get; set; }

        [JsonProperty("is_trial")]
        public bool IsTrial { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // ============================================================
        // New Security Fields for Hardware Verification & Grace Period
        // ============================================================

        [JsonProperty("hardware_verification_status")]
        public string HardwareVerificationStatus { get; set; }

        [JsonProperty("last_hardware_verification_at")]
        public DateTime? LastHardwareVerificationAt { get; set; }

        [JsonProperty("grace_period_enabled")]
        public bool GracePeriodEnabled { get; set; }

        [JsonProperty("grace_period_expires_at")]
        public DateTime? GracePeriodExpiresAt { get; set; }

        [JsonProperty("raw_hardware_components")]
        public JObject RawHardwareComponents { get; set; }
    }

    public class MacroFortSubscriptionCode
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("subscription_type")]
        public string SubscriptionType { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }

        [JsonProperty("activated_at")]
        public DateTime? ActivatedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("used_date")]
        public DateTime? UsedDate { get; set; }

        [JsonProperty("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [JsonProperty("duration_days")]
        public int? DurationDays { get; set; }
    }

    public class MacroFortVerificationCode
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("is_used")]
        public bool IsUsed { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonProperty("last_otp_sent_at")]
        public DateTime? LastOtpSentAt { get; set; }

        [JsonProperty("otp_request_count")]
        public int OtpRequestCount { get; set; }

        [JsonProperty("is_throttled")]
        public bool IsThrottled { get; set; }

        [JsonProperty("throttle_until")]
        public DateTime? ThrottleUntil { get; set; }

        [JsonProperty("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [JsonProperty("used_date")]
        public DateTime? UsedDate { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }

        [JsonProperty("code_type")]
        public string CodeType { get; set; }
    }

    public class MacroFortActivationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string ResultType { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string SubscriptionType { get; set; }
        public string Email { get; set; }
        public int RemainingDays { get; set; }
        public MacroFortSubscriptionData SubscriptionData { get; set; }
        public string WarningMessage { get; set; }
        public MacroFortSubscriptionData PreviousSubscription { get; set; }
    }

    public class MacroFortSubscriptionData
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string HardwareId { get; set; }
        public string SubscriptionCode { get; set; }
        public string SubscriptionType { get; set; }
        public DateTime ActivationDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime? LastCheckDate { get; set; }
        public int DeviceTransferCount { get; set; }
        public DateTime? LastDeviceTransferDate { get; set; }
        public int? MaxDevices { get; set; }

        // ✅ ALREADY ADDED
        public bool IsTrial { get; set; }
    }

    public class MacroFortLocalActivationData
    {
        public string Email { get; set; }
        public string HardwareId { get; set; }
        public string SubscriptionCode { get; set; }
        public string SubscriptionType { get; set; }
        public DateTime ActivationDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }

        // ✅ ADDED (for local persistence)
        public bool IsTrial { get; set; }

        public DateTime LastCheckDate { get; set; }
    }

    public class HardwareVerificationResponse
    {
        [JsonProperty("success")]
        public bool IsSuccess { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }

        [JsonProperty("is_matching")]
        public bool IsMatching { get; set; }

        [JsonProperty("device_name")]
        public string DeviceName { get; set; }

        [JsonProperty("verification_timestamp")]
        public DateTime? VerificationTimestamp { get; set; }
    }

    public class HardwareVerificationLog
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("subscription_id")]
        public string SubscriptionId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("hardware_id")]
        public string HardwareId { get; set; }

        [JsonProperty("raw_components")]
        public JObject RawComponents { get; set; }

        [JsonProperty("verification_result")]
        public string VerificationResult { get; set; }

        [JsonProperty("error_details")]
        public JObject ErrorDetails { get; set; }

        [JsonProperty("client_ip")]
        public string ClientIp { get; set; }

        [JsonProperty("os_version")]
        public string OsVersion { get; set; }

        [JsonProperty("verified_at")]
        public DateTime VerifiedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class TrialHistoryEntry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("device_fingerprint_hash")]
        public string DeviceFingerprintHash { get; set; }

        [JsonProperty("first_trial_started_at")]
        public DateTime FirstTrialStartedAt { get; set; }

        [JsonProperty("trial_expires_at")]
        public DateTime TrialExpiresAt { get; set; }

        [JsonProperty("trial_days")]
        public int TrialDays { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("secondary_hardware_components")]
        public JObject SecondaryHardwareComponents { get; set; }

        [JsonProperty("installation_id")]
        public string InstallationId { get; set; }

        [JsonProperty("os_version")]
        public string OsVersion { get; set; }

        [JsonProperty("grace_period_usage_count")]
        public int GracePeriodUsageCount { get; set; }
    }
}
