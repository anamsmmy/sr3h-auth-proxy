# SR3H MACRO - Railway Proxy Backend Specification

## 📋 Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Railway Proxy Endpoints](#railway-proxy-endpoints)
3. [Request/Response Formats](#requestresponse-formats)
4. [Error Handling](#error-handling)
5. [Rate Limiting Strategy](#rate-limiting-strategy)
6. [Database Schema](#database-schema)
7. [Subscription Code Flow](#subscription-code-flow)
8. [Hardware ID System](#hardware-id-system)

---

## 🏗️ Architecture Overview

### System Flow

```
┌─────────────────┐
│   WPF Client    │ (SR3H MACRO Application)
│   (.NET 9)      │
└────────┬────────┘
         │
         │ HTTPS
         ▼
┌─────────────────────────────────────┐
│  Railway Proxy Auth Service         │
│  (https://sr3h-auth-proxy-          │
│   production.up.railway.app)        │
└────────┬────────────────────────────┘
         │
         │ Database Calls
         ▼
┌─────────────────────────────────────┐
│     Supabase PostgreSQL             │
│  - macro_fort_subscriptions         │
│  - macro_fort_subscription_codes    │
│  - other tables                     │
└─────────────────────────────────────┘
```

### Key Design Principles
- **No direct client-to-Supabase**: All database operations go through Railway Proxy
- **Hardware fingerprinting**: Each subscription bound to specific device (hardware_id)
- **Code-based redemption**: Users redeem codes to activate subscriptions
- **Email verification**: Email is primary identifier alongside hardware_id
- **Session caching**: In-memory cache for offline operation during verification delays

---

## 🔌 Railway Proxy Endpoints

### 1. `/redeem-code` - Code Redemption
**Purpose**: User redeems a subscription code to activate access

**Endpoint**: `POST https://sr3h-auth-proxy-production.up.railway.app/redeem-code`

**Request**:
```json
{
  "code": "SUBSCRIPTION_CODE_STRING",
  "email": "user@example.com",
  "hardware_id": "HASHED_DEVICE_FINGERPRINT"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "تم استرجاع الكود بنجاح",
  "subscription": {
    "id": "uuid",
    "email": "user@example.com",
    "hardware_id": "HASHED_DEVICE_FINGERPRINT",
    "subscription_type": "monthly|yearly|trial",
    "activation_date": "2025-01-01T00:00:00Z",
    "expiry_date": "2025-02-01T00:00:00Z",
    "is_active": true,
    "email_verified": true,
    "last_check_date": "2025-01-01T00:00:00Z"
  }
}
```

**Response (Failure - 4xx/5xx)**:
```json
{
  "success": false,
  "message": "فشل استرجاع الكود - الكود غير صحيح"
}
```

**Implementation Details**:
- Called from: `MacroFortActivationService.RedeemCodeViaAuthProxyAsync()` (line 2232)
- After success: Wait 1 second before fetching subscription (line 2273)
- Then call: `GetSubscriptionByEmailWithRetryAsync()` with 3 retries, exponential backoff (500ms, 1s, 2s)
- Cache result immediately in `SessionActivationCache`

**Rate Limiting Notes**:
- High priority: Users actively redeeming codes expect fast response
- After redeem success → wait 1s before fetch subscription (railway rate limiting recovery)
- Recommended: 5-10 requests per minute per user

---

### 2. `/get-subscription-by-email` - Fetch Subscription by Email
**Purpose**: Get active subscription details using email address

**Endpoint**: `POST https://sr3h-auth-proxy-production.up.railway.app/get-subscription-by-email`

**Request**:
```json
{
  "email": "user@example.com"
}
```

**Response (Success - 200)**:
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "hardware_id": "HASHED_DEVICE_FINGERPRINT",
  "subscription_code": "CODE_STRING",
  "subscription_type": "monthly|yearly|trial",
  "activation_date": "2025-01-01T00:00:00Z",
  "expiry_date": "2025-02-01T00:00:00Z",
  "is_active": true,
  "email_verified": true,
  "last_check_date": "2025-01-01T00:00:00Z",
  "device_transfer_count": 2,
  "last_device_transfer_date": "2024-12-20T10:30:00Z"
}
```

**Response (No Subscription - 404)**:
```json
{
  "error": "Subscription not found"
}
```

**Implementation Details**:
- Called from: `MacroFortActivationService.GetSubscriptionByEmailAsync()` (line 1272)
- **Critical path** used in:
  - `GetSubscriptionByEmailWithRetryAsync()` - 3 retries with exponential backoff (line 1418)
  - `RedeemCodeViaAuthProxyAsync()` - after code redemption (line 2276)
  - `CheckActivationStatusAsync()` - during activation verification
- Timeout: 30 seconds
- Dates returned as ISO 8601 UTC

**Retry Logic** (in calling function):
```
Attempt 1: 500ms delay
Attempt 2: 1000ms delay (500ms × 2)
Attempt 3: 2000ms delay (1000ms × 2)
Total time: ~3.5 seconds maximum
```

**Rate Limiting Notes**:
- This is the most frequently called endpoint (on every app startup)
- Recommended: Limit to 10 requests per minute per email

---

### 3. `/get-subscription-by-hardware` - Fetch Subscription by Hardware ID
**Purpose**: Get active subscription using device fingerprint

**Endpoint**: `POST https://sr3h-auth-proxy-production.up.railway.app/get-subscription-by-hardware`

**Request**:
```json
{
  "hardware_id": "HASHED_DEVICE_FINGERPRINT"
}
```

**Response (Success - 200)**:
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "hardware_id": "HASHED_DEVICE_FINGERPRINT",
  "subscription_code": "CODE_STRING",
  "subscription_type": "monthly|yearly|trial",
  "activation_date": "2025-01-01T00:00:00Z",
  "expiry_date": "2025-02-01T00:00:00Z",
  "is_active": true,
  "email_verified": true,
  "last_check_date": "2025-01-01T00:00:00Z"
}
```

**Response (No Subscription - 404)**:
```json
{
  "error": "No subscription for this hardware"
}
```

**Implementation Details**:
- Called from: `MacroFortActivationService.GetSubscriptionByHardwareIdAsync()` (line 1232)
- **Critical path** on application startup via `App.xaml.cs`:
  - `CheckActivationAndProceedAsync()` calls `GetSubscriptionWithExponentialBackoffAsync()` (line 136)
  - 4 retries with exponential backoff: 500ms, 1s, 2s, 4s (App.xaml.cs line 79-122)
- Timeout: 30 seconds
- Primary lookup on startup (preferred over email-based lookup)

**Startup Retry Logic** (in App.xaml.cs):
```
Attempt 1: Immediate
Attempt 2: 500ms delay
Attempt 3: 1000ms delay (500ms × 2)
Attempt 4: 2000ms delay (1000ms × 2)
Attempt 5: 4000ms delay (2000ms × 2)
Total time: ~7.5 seconds maximum
```

**Rate Limiting Notes**:
- Very high volume: Every app startup makes this call
- Recommended: Limit to 20 requests per minute per hardware_id
- Consider: IP-based rate limiting to protect against brute force

---

### 4. `/initiate-device-transfer` - Start Device Transfer
**Purpose**: Initiate subscription transfer to new device

**Endpoint**: `POST https://sr3h-auth-proxy-production.up.railway.app/initiate-device-transfer`

**Request**:
```json
{
  "email": "user@example.com",
  "current_hardware_id": "OLD_DEVICE_FINGERPRINT"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "تم بدء نقل الجهاز بنجاح",
  "transfer_token": "TEMPORARY_TOKEN_STRING",
  "expires_in_seconds": 600
}
```

**Implementation Details**:
- Called from: `DeviceTransferService.InitiateTransferAsync()` (line 47)
- Token validity: Usually 10 minutes (600 seconds)
- Token is temporary and used in next step: `/complete-device-transfer`
- No automatic retry in current implementation
- Timeout: 10 seconds

**Related Code** (App.xaml.cs line 331):
```csharp
await InitiateDeviceTransferAsync(email, hardwareId);
```

---

### 5. `/complete-device-transfer` - Complete Device Transfer
**Purpose**: Finalize subscription transfer to new device using transfer token

**Endpoint**: `POST https://sr3h-auth-proxy-production.up.railway.app/complete-device-transfer`

**Request**:
```json
{
  "email": "user@example.com",
  "new_hardware_id": "NEW_DEVICE_FINGERPRINT",
  "transfer_token": "TEMPORARY_TOKEN_FROM_INITIATE"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "تم إكمال نقل الجهاز بنجاح",
  "subscription_type": "monthly|yearly|trial",
  "expiry_date": "2025-02-01T00:00:00Z",
  "device_count": 2
}
```

**Implementation Details**:
- Called from: `DeviceTransferService.CompleteTransferAsync()` (line 100)
- Must be called within token expiry window (600 seconds / 10 minutes)
- No automatic retry in current implementation
- Timeout: 10 seconds
- Updates subscription hardware_id binding and device_transfer_count

**Rate Limiting Notes**:
- Lower volume than other endpoints
- Recommended: 1-2 requests per minute per user (device transfers are rare)

---

### 6. `/save-otp` - Save One-Time Password
**Purpose**: Save OTP code on server for verification during device transfer

**Endpoint**: `POST https://sr3h-auth-proxy-production.up.railway.app/save-otp`

**Request**:
```json
{
  "email": "user@example.com",
  "otp_code": "NUMERIC_6_DIGIT_CODE",
  "hardware_id": "DEVICE_FINGERPRINT",
  "expires_at": "2025-01-01T10:30:00Z"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "تم حفظ الكود بنجاح"
}
```

**Implementation Details**:
- Called from: `MacroFortActivationService.SaveOtpViaProxyAsync()` (line 1557)
- OTP validity: Usually 10 minutes
- Email is sent separately via `EmailService.SendVerificationCodeAsync()` (line 1607)
- No automatic retry in current implementation
- Timeout: 30 seconds

---

## 📦 Request/Response Formats

### Date/Time Format
- **Standard**: ISO 8601 with UTC timezone
- **Format String**: `"yyyy-MM-ddTHH:mm:ss.fffK"` or `"O"` (Roundtrip format)
- **Examples**:
  - `"2025-01-01T00:00:00Z"`
  - `"2025-01-01T00:00:00.000Z"`
  - `"2025-01-01T12:30:45+00:00"`

### Client Implementation Details
- **Application enforces**: `CultureInfo.InvariantCulture` for all date formatting (prevents Islamic/Hijri calendar issues)
- **Date parser**: Uses `Newtonsoft.Json` with `DateTimeZoneHandling.Utc`
- **All dates stored internally**: UTC with `DateTimeKind.Utc`

### JSON Serialization Settings
```csharp
new Newtonsoft.Json.JsonSerializerSettings
{
    DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffK",
    DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc
}
```

---

## ⚠️ Error Handling

### HTTP Status Codes

| Code | Meaning | Client Action |
|------|---------|---------------|
| **200** | Success | Process response normally |
| **400** | Bad Request | Log error, show user "Invalid input" |
| **404** | Not Found | No subscription found, show license window |
| **429** | Too Many Requests | Retry with exponential backoff |
| **500** | Server Error | Retry, show user "Server temporarily unavailable" |
| **503** | Service Unavailable | Retry with longer delays |

### Client Error Handling Strategy

1. **Transient Errors** (429, 503, timeout):
   - Retry with exponential backoff
   - Max 3-4 attempts
   - Total delay: 3-7 seconds

2. **Permanent Errors** (400, 404, invalid input):
   - Do NOT retry
   - Show appropriate user message
   - Log for debugging

3. **Network Errors** (timeout, DNS, connection):
   - Retry with exponential backoff
   - Check internet connectivity
   - Show "No internet" message after retries exhaust

### Message Extraction
For Railway Proxy responses:
```json
{
  "success": false,
  "message": "الرسالة الموصوفة هنا"
}
```

Client extracts: `errorData?.message?.ToString() ?? "Default fallback message"`

---

## 🚦 Rate Limiting Strategy

### Problem Context
- Railway Proxy acts as gateway to Supabase
- Supabase has internal rate limiting (especially on quick successive calls)
- Issue: After `/redeem-code`, immediate `/get-subscription-by-email` causes 429 errors

### Current Client-Side Mitigation

#### 1. Post-Redemption Delay (RedeemCodeViaAuthProxyAsync)
```csharp
await Task.Delay(1000);  // 1 second wait
var subscriptionData = await GetSubscriptionByEmailWithRetryAsync(email);
```
- **Rationale**: Give Railway/Supabase time to process code redemption
- **Timing**: 1000ms should be sufficient for most cases

#### 2. Exponential Backoff on Fetch (GetSubscriptionByEmailWithRetryAsync)
```
Retry 1: 500ms delay
Retry 2: 1000ms delay (500ms × 2)
Retry 3: 2000ms delay (1000ms × 2)
```
- **Rationale**: If rate limited, space out retry attempts
- **Total time**: ~3.5 seconds maximum

#### 3. Startup Retry (GetSubscriptionWithExponentialBackoffAsync in App.xaml.cs)
```
Retry 1: Immediate
Retry 2: 500ms delay
Retry 3: 1000ms delay
Retry 4: 2000ms delay
Retry 5: 4000ms delay
```
- **Rationale**: App startup makes first call, could be rate limited
- **Total time**: ~7.5 seconds maximum

### Recommended Backend Configuration

#### Per-IP Rate Limiting (Railway Proxy)
- **Limit**: 20 requests per minute per IP address
- **Window**: Rolling 60-second window
- **Backoff**: Return `Retry-After: 2` header

#### Per-User Rate Limiting (Supabase)
- **Email**: 10 requests per minute per email
- **Hardware ID**: 20 requests per minute per hardware_id

#### Per-Endpoint Limits
| Endpoint | Limit | Window |
|----------|-------|--------|
| `/redeem-code` | 5/min per user | 60s |
| `/get-subscription-by-email` | 10/min per email | 60s |
| `/get-subscription-by-hardware` | 20/min per hardware_id | 60s |
| `/initiate-device-transfer` | 2/min per user | 60s |
| `/complete-device-transfer` | 2/min per user | 60s |
| `/save-otp` | 5/min per email | 60s |

### Monitoring & Alerts
- Monitor 429 rate limit responses
- Alert when >5% of requests return 429
- Log rate limiting patterns by time of day/user

---

## 🗄️ Database Schema

### Table: `macro_fort_subscriptions`

**Purpose**: Main subscription data, source of truth

```sql
CREATE TABLE macro_fort_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    hardware_id VARCHAR(255) NOT NULL,
    subscription_code VARCHAR(50),
    subscription_type VARCHAR(50) NOT NULL,  -- 'trial', 'monthly', 'yearly'
    activation_date TIMESTAMP WITH TIME ZONE NOT NULL,
    expiry_date TIMESTAMP WITH TIME ZONE NOT NULL,
    is_active BOOLEAN DEFAULT true,
    email_verified BOOLEAN DEFAULT false,
    last_check_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    device_transfer_count INTEGER DEFAULT 0,
    last_device_transfer_date TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_subscriptions_email ON macro_fort_subscriptions(email);
CREATE INDEX idx_subscriptions_hardware_id ON macro_fort_subscriptions(hardware_id);
CREATE INDEX idx_subscriptions_is_active ON macro_fort_subscriptions(is_active);
```

**Key Fields**:
- `hardware_id`: Device fingerprint, prevents multi-device sharing
- `subscription_type`: Determines expiry duration
- `is_active`: Soft-delete pattern
- `last_check_date`: Tracks last server verification
- `device_transfer_count`: Prevents abuse, limit ~10 per 30 days
- `last_device_transfer_date`: Rate limiting device transfers

---

### Table: `macro_fort_subscription_codes`

**Purpose**: Code inventory and redemption tracking

```sql
CREATE TABLE macro_fort_subscription_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    status VARCHAR(20) DEFAULT 'unused',  -- 'unused', 'used', 'expired'
    subscription_type VARCHAR(50) NOT NULL,  -- 'trial', 'monthly', 'yearly'
    used_by_email VARCHAR(255),
    redeemed_at TIMESTAMP WITH TIME ZONE,
    activated_at TIMESTAMP WITH TIME ZONE,
    expiry_date TIMESTAMP WITH TIME ZONE,  -- Code expiry, not subscription expiry
    hardware_id_bound_to VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_codes_code ON macro_fort_subscription_codes(code);
CREATE INDEX idx_codes_status ON macro_fort_subscription_codes(status);
CREATE INDEX idx_codes_used_by_email ON macro_fort_subscription_codes(used_by_email);
CREATE INDEX idx_codes_expiry_date ON macro_fort_subscription_codes(expiry_date);
```

**Key Fields**:
- `status`:
  - `'unused'`: Never redeemed
  - `'used'`: Redeemed successfully
  - `'expired'`: Code validity window passed
- `redeemed_at`: When code was successfully used
- `activated_at`: When subscription became active (from code)
- `expiry_date`: Code validity window (not subscription expiry)
- `used_by_email`: Email that redeemed this code
- `hardware_id_bound_to`: Hardware that originally redeemed code

**ℹ️ Important Note**:
- These fields can be populated by `/redeem-code` endpoint for auditing
- Application does NOT rely on these fields - gets subscription data from `macro_fort_subscriptions` instead
- Can be empty for legacy data

---

### Table: `otp_codes`

**Purpose**: One-time passwords for device transfer verification

```sql
CREATE TABLE otp_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL,
    otp_code VARCHAR(10) NOT NULL,
    hardware_id VARCHAR(255),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    verified BOOLEAN DEFAULT false,
    verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_otp_email_code ON otp_codes(email, otp_code);
CREATE INDEX idx_otp_expires_at ON otp_codes(expires_at);
```

---

## 🔄 Subscription Code Flow

### Activation Flow

```
User enters code in License Window
    │
    ▼
RedeemCodeViaAuthProxyAsync()
    │
    ├─ Validate input (code, email, hardware_id)
    │
    ├─ POST /redeem-code
    │   └─ Railway updates: macro_fort_subscriptions + macro_fort_subscription_codes
    │
    ├─ Wait 1 second (rate limiting recovery)
    │
    ├─ GetSubscriptionByEmailWithRetryAsync()
    │   └─ 3 retries: 500ms, 1s, 2s delays
    │
    ├─ On success: Cache in SessionActivationCache
    │
    ├─ Show main window
    │
    └─ Return success to UI
```

### Key Points
1. **Code validation happens on Railway** (not client-side)
2. **Email verification** usually not required for initial activation
3. **Subscription becomes active** immediately after code redemption
4. **SessionActivationCache** holds data in memory for duration of app session
5. **Next app launch** verifies subscription from database again

### Code Status Progression

```
Created (unused)
    ├─ Valid for: Configurable window (e.g., 30 days)
    │
    ├─ After: Automatically expired
    │
User redeems
    └─ Status: used
        └─ creates macro_fort_subscriptions record
```

---

## 🔐 Hardware ID System

### Purpose
- Prevent subscription sharing across multiple devices
- Bind subscription to specific device
- Allow controlled device transfers (limited to ~10 per 30 days)

### Hardware ID Calculation
Located in: `SafeHardwareIdService.cs`

**Components**:
1. CPU ID (from WMI)
2. Motherboard Serial (from WMI)
3. Physical Drive Serial (C: drive)
4. MAC Address (primary network adapter)
5. Windows ProductID

**Algorithm**:
```
raw_data = CPU + Motherboard + Drive + MAC + Windows ProductID
hardware_id = SHA256(raw_data).ToBase64()
```

**Characteristics**:
- Deterministic: Same components = Same hardware_id
- 64-character Base64 string
- Changes only if hardware changes significantly
- Never exposed in UI for security (removed in recent update)

### Security Considerations
- ✅ Never logged in full
- ✅ Only last 16 chars shown in logs (e.g., `...abc123xyz789`)
- ✅ Not exposed in UI windows
- ✅ Securely hashed before transmission
- ⚠️ Server should NOT expose algorithm to prevent crack developers from understanding it

### Device Transfer Process

```
User on Device A with active subscription
    │
    ├─ Device A: Initiates transfer (email + current_hardware_id)
    │   └─ POST /initiate-device-transfer
    │       └─ Returns: transfer_token (10-min validity)
    │
    ├─ User receives OTP via email
    │
    ├─ User on Device B: Enters transfer_token + OTP + email
    │   └─ POST /complete-device-transfer (with transfer_token)
    │       └─ Updates subscription.hardware_id to Device B
    │       └─ Increments device_transfer_count
    │
    └─ Device B: Now has active subscription (Device A revoked)
```

**Transfer Limits**:
- Max 10 transfers per 30 days (configurable)
- Prevents abuse by subscription sharing
- Resets monthly

---

## 📊 Monitoring & Logging

### What to Log
- All successful authentications (email, hardware_id masked)
- All failed authentications (reason for failure)
- Rate limit hits (429 responses)
- Transfer initiations and completions
- OTP generation and verification

### What NOT to Log
- Full hardware_id (log only last 16 chars)
- Full subscription codes (log only last 6 chars)
- User passwords
- OTP codes (except "sent successfully" notification)

### Recommended Metrics
- Success rate per endpoint
- Average response time per endpoint
- 429 rate limit frequency
- Device transfer frequency
- Code redemption success rate
- Subscription churn rate

---

## 🚀 Deployment Checklist

### Pre-Deployment
- [ ] All endpoints tested with Postman/curl
- [ ] Error responses properly formatted
- [ ] Rate limiting configured and tested
- [ ] Database indexes created
- [ ] Date/time formats verified (ISO 8601 UTC)
- [ ] SSL/TLS certificate valid
- [ ] CORS headers configured (if needed)

### Post-Deployment
- [ ] Monitor rate limit responses
- [ ] Monitor 429 error frequency
- [ ] Test all redemption paths
- [ ] Verify device transfer flow
- [ ] Test with various system clocks/timezones

---

## 📝 Version History

- **v1.0** (2025-01-01): Initial specification
- **Updates**: See CHANGELOG.md

---

## 🤝 Support

For issues or questions about this specification:
1. Check application debug logs (Output window in Visual Studio)
2. Enable detailed logging in MacroFortActivationService
3. Contact backend team with specific endpoint and error response

---

## 📚 Related Files

**Client-side implementation**:
- `Services/MacroFortActivationService.cs` - Core activation logic
- `Services/DeviceTransferService.cs` - Device transfer logic
- `App.xaml.cs` - Startup verification flow
- `Services/SafeHardwareIdService.cs` - Hardware ID generation
- `Services/SessionActivationCache.cs` - In-memory cache

**Configuration**:
- `Services/SecureSupabaseConfig.cs` - Supabase credentials
- `App.config` - Application settings
