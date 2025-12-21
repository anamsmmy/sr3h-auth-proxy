# Database Schema Optimization - Complete Analysis

## 📊 Overview

This document details the database schema optimization based on the new security architecture implementing:
- **Server-centric hardware verification** (no local file caching)
- **In-memory session caching** (30-minute grace period)
- **Complete hardware fingerprinting** (disk, CPU, BIOS components)

---

## 🗑️ REMOVED FIELDS (Now Redundant)

### Table: `macro_fort_subscriptions`

| Field | Type | Why Removed | Migration |
|-------|------|------------|-----------|
| **`subscription_code`** | varchar | Redundant - use `macro_fort_subscription_codes` table relationship instead. ORM should join tables. | Foreign key to `macro_fort_subscription_codes(code)` |
| **`otp_code`** | varchar | Moved to `macro_fort_verification_codes` table. OTP is temporary and should be in verification table, not subscriptions. | Query `macro_fort_verification_codes` for OTP lookups |
| **`otp_expiry`** | timestamptz | Moved to `macro_fort_verification_codes` table with same reasoning. | Query `macro_fort_verification_codes.expires_at` |

**Impact**: These fields were stored in subscriptions for quick lookup, but caused:
- Data duplication (subscription_code also in codes table)
- Sync issues when OTP updates
- Confusion about which table is source of truth
- Unnecessarily wide rows

**Code Migration**: 
```csharp
// OLD (problematic):
var otp = subscription.otp_code;
var expiry = subscription.otp_expiry;

// NEW (correct):
var verification = await GetVerificationCodeAsync(email, orderId);
var otp = verification.OtpCode;
var expiry = verification.ExpiresAt;
```

---

### Table: `macro_fort_verification_codes`

| Field | Type | Why Removed | Replacement |
|-------|------|------------|------------|
| **`code`** | text | Duplicate of `otp_code` - redundant semantic names | Use `otp_code` consistently |
| **`expiry_date`** | timestamptz | Duplicate of `expires_at` - inconsistent naming | Use `expires_at` for consistency |
| **`code_type`** | text | Always "email_verification" - unnecessary column | Can be implicit based on table purpose |

**Impact**: Verification table is for OTP codes only. Having duplicate fields causes:
- Confusion about which field to update
- Potential sync issues
- Wasted storage

**Database cleanup**:
```sql
-- These are redundant, use otp_code and expires_at instead
ALTER TABLE macro_fort_verification_codes DROP COLUMN code;
ALTER TABLE macro_fort_verification_codes DROP COLUMN expiry_date;
-- code_type can remain but is always the same value
```

---

## ✨ ADDED FIELDS (New Security Architecture)

### Table: `macro_fort_subscriptions`

#### 1. **`hardware_verification_status`** (varchar, default: 'pending')
```sql
hardware_verification_status VARCHAR(20) 
CHECK (hardware_verification_status IN ('pending', 'verified', 'failed', 'mismatch'))
```

**Purpose**: Track the state of hardware verification for each subscription
- **pending**: Device has not been verified yet
- **verified**: Hardware ID matches server records
- **failed**: Last verification attempt failed
- **mismatch**: Hardware ID changed (different device detected)

**Use Cases**:
```csharp
// Check if device is verified before allowing access
if (subscription.HardwareVerificationStatus != "verified") {
    // Require server verification before granting license
}

// Detect device tampering
if (subscription.HardwareVerificationStatus == "mismatch") {
    // Device changed since last verified - suspicious
}
```

**Default Behavior**: All existing subscriptions set to 'verified' for backward compatibility

---

#### 2. **`last_hardware_verification_at`** (timestamptz, nullable)
```sql
last_hardware_verification_at TIMESTAMP WITH TIME ZONE
```

**Purpose**: Timestamp of the most recent successful hardware verification

**Use Cases**:
```csharp
// Check if device needs re-verification (e.g., after 7 days)
var timeSinceVerification = DateTime.UtcNow - subscription.LastHardwareVerificationAt;
if (timeSinceVerification > TimeSpan.FromDays(7)) {
    // Force re-verification
}

// Audit trail - when was this subscription last verified?
var lastCheck = subscription.LastHardwareVerificationAt;
```

**Auditing**: Used for compliance and security investigations

---

#### 3. **`grace_period_enabled`** (bool, default: false)
```sql
grace_period_enabled BOOLEAN DEFAULT FALSE
```

**Purpose**: Indicates if the subscription is currently in grace period (offline access)

**Why in Database**: 
- Session-based in-memory cache is lost on app restart
- Database record ensures consistency across sessions
- Server can log grace period usage for analytics

**Lifecycle**:
1. User successful verifies → grace period not enabled
2. After 30 minutes offline → grace period automatically expires (client-side)
3. User goes offline → grace period NOT active (requires server verification each session)
4. Database field helps audit "how many users were in grace period"

**Example**:
```csharp
// When activating session from cache
if (!SessionActivationCache.HasCachedActivation()) {
    if (subscription.GracePeriodEnabled) {
        // Device can work offline for remaining time
    } else {
        // Must contact server
    }
}
```

---

#### 4. **`grace_period_expires_at`** (timestamptz, nullable)
```sql
grace_period_expires_at TIMESTAMP WITH TIME ZONE
```

**Purpose**: When the grace period expires (calculated as: last_verification + 30 minutes)

**Use Cases**:
```csharp
// Server-side check: Is user still in grace period?
if (subscription.GracePeriodExpiresAt > DateTime.UtcNow) {
    // Grace period still active
}

// For audits: How long was user offline?
var gracePeriodDuration = subscription.GracePeriodExpiresAt - subscription.LastHardwareVerificationAt;
```

**Note**: Client doesn't trust this - it uses in-memory timer. Server uses for audit/analytics.

---

#### 5. **`raw_hardware_components`** (jsonb, nullable)
```sql
raw_hardware_components JSONB
-- Contains: { "disk1": "...", "disk2": "...", "cpu1": "...", "cpu2": "...", "bios": "..." }
```

**Purpose**: Store the raw hardware component data for server-side verification without re-querying client

**Structure**:
```json
{
  "disk1": "SATA_SN_1234567890",
  "disk2": "SSD_SN_0987654321",
  "cpu1": "0000651B6F2610",
  "cpu2": "0000651B6F2611",
  "bios": "DELL-12345"
}
```

**Use Cases**:
```csharp
// Server-side verification: Hash these components
var hardware_id = SHA256($"{disk1}|{disk2}|{cpu1}|{cpu2}|{bios}");

// Detect hardware changes
if (storedComponents.disk1 != newComponents.disk1) {
    // Primary disk changed - device tampering or legitimate upgrade?
}

// Gradual hardware changes detection
// If only secondary components changed but disk1 matches, allow with warning
```

**Security**: Stored server-side to prevent client-side tampering. Client only sends temporarily (during verification), server stores hashed version + raw for comparison.

---

### Table: `macro_fort_trial_history`

#### 1. **`secondary_hardware_components`** (jsonb, nullable)
```sql
secondary_hardware_components JSONB
-- {disk2, cpu2} separated from primary for change detection
```

**Purpose**: Trial-specific hardware tracking for secondary components

**Why Separate**: Allows tracking component changes over trial period

---

#### 2. **`installation_id`** (uuid, default: gen_random_uuid())
```sql
installation_id UUID DEFAULT gen_random_uuid()
```

**Purpose**: Unique identifier for each installation session

**Use Cases**:
```csharp
// Track if same person is running multiple trials on different devices
var uniqueTrialsPerUser = db.TrialHistory
    .Where(t => t.Email == email)
    .Select(t => t.InstallationId)
    .Distinct()
    .Count();

if (uniqueTrialsPerUser > 2) {
    // User is installing trial on multiple devices - suspicious
}
```

---

#### 3. **`os_version`** (varchar, nullable)
```sql
os_version VARCHAR(255)
```

**Purpose**: Operating system version for additional device fingerprinting

**Format**: "Windows 10 21H2", "Windows 11", etc.

**Use Cases**:
- Detect if same hardware running different OS (VM detection)
- Analytics: What OS versions use the application most?

---

#### 4. **`grace_period_usage_count`** (int4, default: 0)
```sql
grace_period_usage_count INTEGER DEFAULT 0
```

**Purpose**: How many times this trial used grace period offline access

**Analytics**:
```sql
-- Find trials that heavily rely on grace period
SELECT email, grace_period_usage_count 
FROM macro_fort_trial_history 
WHERE grace_period_usage_count > 5;

-- These users need internet connectivity
```

---

## 📋 NEW TABLE: `macro_fort_hardware_verification_log`

Complete audit trail of all hardware verification attempts

```sql
CREATE TABLE macro_fort_hardware_verification_log (
    id UUID PRIMARY KEY,
    subscription_id UUID REFERENCES macro_fort_subscriptions(id),
    email TEXT NOT NULL,
    hardware_id VARCHAR(255) NOT NULL,
    
    raw_components JSONB,
    verification_result VARCHAR(20),  -- 'success', 'mismatch', 'invalid', 'error'
    error_details JSONB,
    
    client_ip TEXT,
    os_version VARCHAR(255),
    
    verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE
);
```

**Purpose**: 
- **Security Audit**: Track all verification attempts
- **Fraud Detection**: Multiple failed attempts from different IPs
- **Debugging**: Why did verification fail for a user?
- **Compliance**: Prove subscription validity at any point in time

**Examples**:
```sql
-- Find users with repeated verification failures
SELECT email, COUNT(*) as failures
FROM macro_fort_hardware_verification_log
WHERE verification_result = 'failed' 
  AND verified_at > NOW() - INTERVAL '7 days'
GROUP BY email
HAVING COUNT(*) > 5;

-- Track when hardware changed for a subscription
SELECT verified_at, hardware_id, verification_result
FROM macro_fort_hardware_verification_log
WHERE subscription_id = $1
ORDER BY verified_at DESC;

-- Detect distributed fraud attempts
SELECT email, COUNT(DISTINCT client_ip) as unique_ips
FROM macro_fort_hardware_verification_log
WHERE verification_result IN ('mismatch', 'invalid')
  AND verified_at > NOW() - INTERVAL '24 hours'
GROUP BY email
HAVING COUNT(DISTINCT client_ip) > 3;
```

---

## 📊 Schema Comparison Table

| Table | Removed Fields | Added Fields | New Rows | Status |
|-------|---|---|---|---|
| **macro_fort_subscriptions** | 3 | 5 | 0 | ✅ Optimized |
| **macro_fort_verification_codes** | 2 | 0 | 0 | ✅ Cleaned |
| **macro_fort_trial_history** | 0 | 4 | 0 | ✅ Enhanced |
| **macro_fort_hardware_verification_log** | - | - | ✨ New | ✅ Created |
| **macro_fort_subscription_codes** | 0 | 0 | 0 | ✅ Unchanged |

---

## 🔄 Migration Path

### Step 1: Backup existing data
```bash
# Export current data
pg_dump macro_fort_subscriptions > backup_subscriptions.sql
```

### Step 2: Apply migration
```bash
# Run the optimization migration
psql -f migration_database_optimization.sql
```

### Step 3: Update ORM Models
Update `MacroFortModels.cs` to reflect schema changes:
```csharp
// Remove from MacroFortSubscription class:
// - public string SubscriptionCode { get; set; }
// - public string OtpCode { get; set; }
// - public DateTime? OtpExpiry { get; set; }

// Add to MacroFortSubscription class:
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
```

### Step 4: Test queries
Verify application still works with new schema

### Step 5: Deploy
Push updates to production

---

## ✅ Verification Checklist

- [ ] Backup created
- [ ] Migration runs without errors
- [ ] New columns have proper indexes
- [ ] ORM models updated
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] No duplicate data remains
- [ ] Audit table receives verification logs
- [ ] Grace period fields update correctly

---

## 📞 Related Documentation

- `SessionActivationCache.cs` - In-memory grace period implementation
- `SafeHardwareIdService.cs` - Hardware component extraction
- `MacroFortActivationService.cs` - Server verification calls
- `VerifyHardwareAsync()` - Verification endpoint

---

## 🔐 Security Benefits

1. **No local file caching** → Eliminates file tampering risks
2. **Server-centric verification** → Single source of truth
3. **Complete audit trail** → Fraud detection capabilities
4. **Hardware binding** → Device-locked subscriptions
5. **Graceful offline support** → Works offline for 30 min max
6. **Automatic timeout** → No indefinite offline subscriptions

