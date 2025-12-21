# Schema Implementation Guide

## 📋 Implementation Checklist for New Database Fields

### Phase 1: ✅ Completed
- [x] Created migration SQL file (`migration_database_optimization.sql`)
- [x] Updated ORM Models to include new fields
- [x] Documented all changes in `DATABASE_SCHEMA_CHANGES.md`

### Phase 2: TODO - Code Updates Required

#### Step 1: Update `MacroFortActivationService.cs`

**Location**: `MacroFortActivationService.cs:96-178` (VerifyHardwareAsync method)

When verifying hardware, update subscription with new fields:

```csharp
// After successful hardware verification
var subscription = subscription_data;

// Update hardware verification fields
subscription.HardwareVerificationStatus = "verified";
subscription.LastHardwareVerificationAt = DateTime.UtcNow;
subscription.RawHardwareComponents = JObject.Parse(rawComponentsJson);

// If successful, set grace period
subscription.GracePeriodEnabled = true;
subscription.GracePeriodExpiresAt = DateTime.UtcNow.AddMinutes(30);

// Log verification attempt
await LogHardwareVerificationAsync(
    subscriptionId: subscription.Id,
    email: email,
    hardwareId: verificationResponse.HardwareId,
    rawComponents: rawComponentsJson,
    result: "success",
    osVersion: GetOsVersion()
);
```

---

#### Step 2: Update Queries in `MacroFortActivationService.cs`

**Before** (Using deprecated fields):
```sql
SELECT * FROM macro_fort_subscriptions 
WHERE email = @email 
  AND subscription_code = @code
  AND otp_code = @otp
```

**After** (Using normalized schema):
```sql
SELECT s.* FROM macro_fort_subscriptions s
WHERE s.email = @email 
  AND s.id IN (
    SELECT subscription_id 
    FROM macro_fort_subscription_codes 
    WHERE code = @code
  )
  AND EXISTS (
    SELECT 1 FROM macro_fort_verification_codes v
    WHERE v.email = s.email AND v.otp_code = @otp
  )
```

---

#### Step 3: Add Grace Period Management in `SessionActivationCache.cs`

```csharp
public class SessionActivationCache
{
    // Update cache with grace period info from subscription
    public static void SetCachedActivationWithGracePeriod(
        ActivationData data, 
        DateTime gracePeriodExpiresAt)
    {
        ActivationData = data;
        GracePeriodExpiresAt = gracePeriodExpiresAt;
        CachedAt = DateTime.UtcNow;
        
        // Timer already in place, just ensure it respects grace period
        StartGracePeriodTimer();
    }

    private static void StartGracePeriodTimer()
    {
        var remainingTime = GracePeriodExpiresAt - DateTime.UtcNow;
        if (remainingTime.TotalMilliseconds > 0)
        {
            _gracePeriodTimer = new Timer(
                _ => Clear(),
                null,
                remainingTime,
                Timeout.Infinite
            );
        }
    }
}
```

---

#### Step 4: Update `App.xaml.cs` to Log Verifications

```csharp
private async Task VerifyWithServerMandatoryAsync()
{
    var hardwareId = SafeHardwareIdService.GetFreshHardwareId();
    var osVersion = GetOsVersion(); // Implement this
    
    var result = await MacroFortActivationService.Instance.VerifyHardwareAsync(email);
    
    if (result.IsSuccess)
    {
        // Get subscription and update verification info
        var subscription = await GetSubscriptionFromServerAsync(email);
        
        subscription.HardwareVerificationStatus = "verified";
        subscription.LastHardwareVerificationAt = DateTime.UtcNow;
        subscription.GracePeriodEnabled = true;
        subscription.GracePeriodExpiresAt = DateTime.UtcNow.AddMinutes(30);
        
        // Store in cache
        SessionActivationCache.SetCachedActivation(subscription);
        
        // Log successful verification
        await LogVerificationAsync(
            subscriptionId: subscription.Id,
            email: email,
            hardwareId: result.HardwareId,
            result: "success",
            osVersion: osVersion
        );
    }
    else
    {
        // Log failed verification
        await LogVerificationAsync(
            subscriptionId: null,
            email: email,
            hardwareId: hardwareId,
            result: "failed",
            errorDetails: result.Message,
            osVersion: osVersion
        );
    }
}

private string GetOsVersion()
{
    var osVersion = System.Environment.OSVersion;
    return $"{osVersion.Platform} {osVersion.VersionString}";
}

private async Task LogVerificationAsync(
    string subscriptionId,
    string email,
    string hardwareId,
    string result,
    string errorDetails = null,
    string osVersion = null)
{
    try
    {
        var log = new
        {
            subscription_id = subscriptionId,
            email = email,
            hardware_id = hardwareId,
            verification_result = result,
            error_details = errorDetails,
            os_version = osVersion,
            client_ip = await GetClientIpAsync(),
            verified_at = DateTime.UtcNow
        };
        
        var json = JsonConvert.SerializeObject(log);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        await _httpClient.PostAsync(
            $"{_credentials.SupabaseUrl}/rest/v1/macro_fort_hardware_verification_log",
            content
        );
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Failed to log verification: {ex.Message}");
    }
}

private async Task<string> GetClientIpAsync()
{
    try
    {
        var response = await new HttpClient().GetAsync("https://api.ipify.org");
        return await response.Content.ReadAsStringAsync();
    }
    catch
    {
        return "unknown";
    }
}
```

---

#### Step 5: Update `BackgroundValidationScheduler.cs`

```csharp
public class BackgroundValidationScheduler
{
    public async Task UpdateVerificationDataAsync(MacroFortSubscription subscription)
    {
        try
        {
            // Update verification timestamp
            subscription.LastCheckDate = DateTime.UtcNow;
            
            // Check if verification is still valid
            if (subscription.LastHardwareVerificationAt.HasValue)
            {
                var timeSinceVerification = DateTime.UtcNow - subscription.LastHardwareVerificationAt;
                
                // If older than 7 days, mark for re-verification
                if (timeSinceVerification.TotalDays > 7)
                {
                    subscription.HardwareVerificationStatus = "pending";
                    subscription.LastHardwareVerificationAt = null;
                }
            }
            
            // Store updated data
            SessionActivationCache.SetCachedActivation(subscription);
            
            System.Diagnostics.Debug.WriteLine(
                $"✓ Verification status: {subscription.HardwareVerificationStatus}"
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating verification: {ex.Message}");
        }
    }
}
```

---

#### Step 6: Update Trial History Tracking

```csharp
// In StartTrialAsync or similar method
public async Task<MacroFortActivationResult> StartTrialAsync(string email)
{
    var result = await CheckTrialStatusAsync(email, hardwareId);
    
    if (result.IsSuccess)
    {
        // Get raw components for audit trail
        var rawComponents = SafeHardwareIdService.GetRawHardwareComponents();
        
        // Store trial with new fields
        var trialEntry = new
        {
            email = email,
            device_fingerprint_hash = hardwareId,
            first_trial_started_at = DateTime.UtcNow,
            trial_expires_at = DateTime.UtcNow.AddDays(7),
            trial_days = 7,
            installation_id = Guid.NewGuid(), // New unique ID
            os_version = GetOsVersion(),
            secondary_hardware_components = ExtractSecondaryComponents(rawComponents),
            grace_period_usage_count = 0
        };
        
        // Save to database...
    }
}

private JObject ExtractSecondaryComponents(string rawComponents)
{
    var components = JObject.Parse(rawComponents);
    return new JObject(
        new JProperty("disk2", components["disk2"]),
        new JProperty("cpu2", components["cpu2"])
    );
}
```

---

### Phase 3: TODO - Testing

#### Unit Tests to Add

```csharp
[TestClass]
public class HardwareVerificationTests
{
    [TestMethod]
    public async Task VerifyHardware_SetsVerificationStatus()
    {
        // Arrange
        var service = new MacroFortActivationService();
        var email = "test@example.com";
        
        // Act
        var result = await service.VerifyHardwareAsync(email);
        
        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("verified", result.HardwareVerificationStatus);
        Assert.IsNotNull(result.LastHardwareVerificationAt);
        Assert.IsTrue(result.GracePeriodEnabled);
    }

    [TestMethod]
    public async Task GracePeriod_ExpiresAfter30Minutes()
    {
        // Arrange
        var cache = new SessionActivationCache();
        var gracePeriodExpiry = DateTime.UtcNow.AddMinutes(30);
        
        // Act
        cache.SetCachedActivationWithGracePeriod(data, gracePeriodExpiry);
        
        // Assert
        Assert.IsTrue(cache.HasCachedActivation());
        
        // Wait 31 minutes
        await Task.Delay(TimeSpan.FromMinutes(31));
        
        // Assert
        Assert.IsFalse(cache.HasCachedActivation());
    }

    [TestMethod]
    public async Task VerificationLog_CreatedSuccessfully()
    {
        // Arrange
        var logService = new HardwareVerificationLogService();
        var log = new HardwareVerificationLog
        {
            Email = "test@example.com",
            HardwareId = "abc123",
            VerificationResult = "success"
        };
        
        // Act
        await logService.LogVerificationAsync(log);
        
        // Assert
        var savedLog = await logService.GetLastVerificationAsync("test@example.com");
        Assert.AreEqual("success", savedLog.VerificationResult);
    }
}
```

---

#### Integration Tests

```csharp
[TestClass]
public class DatabaseIntegrationTests
{
    [TestMethod]
    public async Task Migration_CreatesNewColumns()
    {
        // Run migration
        // Verify columns exist
        var columns = await GetTableColumnsAsync("macro_fort_subscriptions");
        Assert.IsTrue(columns.Contains("hardware_verification_status"));
        Assert.IsTrue(columns.Contains("grace_period_expires_at"));
        Assert.IsTrue(columns.Contains("raw_hardware_components"));
    }

    [TestMethod]
    public async Task VerificationLog_TableExists()
    {
        // Verify new table was created
        var tables = await GetDatabaseTablesAsync();
        Assert.IsTrue(tables.Contains("macro_fort_hardware_verification_log"));
    }

    [TestMethod]
    public async Task OldFields_RemovedOrDeprecated()
    {
        // Verify old fields are gone
        var columns = await GetTableColumnsAsync("macro_fort_subscriptions");
        Assert.IsFalse(columns.Contains("subscription_code"));
        Assert.IsFalse(columns.Contains("otp_code"));
        Assert.IsFalse(columns.Contains("otp_expiry"));
    }
}
```

---

### Phase 4: TODO - Deployment

1. **Backup Production Database**
   ```bash
   pg_dump macro_fort > backup_$(date +%Y%m%d_%H%M%S).sql
   ```

2. **Apply Migration**
   ```bash
   psql < migration_database_optimization.sql
   ```

3. **Verify Migration**
   ```bash
   psql -c "\d macro_fort_subscriptions"
   psql -c "\d macro_fort_hardware_verification_log"
   ```

4. **Update Code** - Deploy new version with:
   - Updated `MacroFortModels.cs` ✅
   - Updated `MacroFortActivationService.cs` (TODO)
   - Updated `SessionActivationCache.cs` (TODO)
   - Updated `BackgroundValidationScheduler.cs` (TODO)

5. **Monitor**
   - Check application logs for errors
   - Verify verification logs are being created
   - Monitor grace period usage

---

## 📊 Query Examples

### Find subscriptions needing re-verification
```sql
SELECT email, last_hardware_verification_at, 
       NOW() - last_hardware_verification_at as days_since_verification
FROM macro_fort_subscriptions
WHERE hardware_verification_status = 'verified'
  AND last_hardware_verification_at < NOW() - INTERVAL '7 days'
ORDER BY last_hardware_verification_at DESC;
```

### Track grace period usage
```sql
SELECT email, SUM(grace_period_usage_count) as total_grace_periods,
       COUNT(*) as number_of_trials
FROM macro_fort_trial_history
GROUP BY email
HAVING SUM(grace_period_usage_count) > 3
ORDER BY total_grace_periods DESC;
```

### Detect suspicious activity
```sql
SELECT email, COUNT(*) as failed_attempts,
       COUNT(DISTINCT client_ip) as unique_ips,
       MAX(verified_at) as last_attempt
FROM macro_fort_hardware_verification_log
WHERE verification_result IN ('failed', 'mismatch')
  AND verified_at > NOW() - INTERVAL '24 hours'
GROUP BY email
HAVING COUNT(*) > 5 OR COUNT(DISTINCT client_ip) > 3;
```

### Hardware changes timeline
```sql
SELECT verified_at, 
       hardware_id,
       raw_components->>'disk1' as primary_disk,
       verification_result
FROM macro_fort_hardware_verification_log
WHERE email = 'user@example.com'
ORDER BY verified_at DESC;
```

---

## ✅ Success Criteria

- [ ] Migration applies without errors
- [ ] All new columns exist and have correct types
- [ ] Old columns are removed/deprecated
- [ ] Indexes are created
- [ ] Application builds without errors
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Grace period works (30 min timeout)
- [ ] Verification logs are created
- [ ] Hardware verification tracks correctly
- [ ] No data loss on existing subscriptions

