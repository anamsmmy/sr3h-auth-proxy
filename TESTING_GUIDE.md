# SR3H MACRO Endpoint Testing Guide

## Setup

### 1. Environment Variables
Ensure your Railway proxy server has these environment variables set:
```
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_SERVICE_ROLE_KEY=your-service-role-key
ALLOWED_ORIGINS=*
```

### 2. Test Data
Before running tests, create test data in your Supabase database:

```sql
-- Create test subscription
INSERT INTO macro_fort_subscriptions (email, hardware_id, subscription_type, expiry_date, is_active, email_verified)
VALUES ('test@test.com', 'hw-123', 'premium', NOW() + INTERVAL '30 days', true, true)
ON CONFLICT(email) DO UPDATE SET 
  expiry_date = NOW() + INTERVAL '30 days',
  is_active = true,
  subscription_type = 'premium';

-- Create test subscription code
INSERT INTO macro_fort_subscription_codes (code, subscription_type, status, email, hardware_id, expiry_date)
VALUES ('TEST123', 'premium', 'unused', 'test@test.com', 'hw-123', NOW() + INTERVAL '30 days')
ON CONFLICT(code) DO UPDATE SET 
  status = 'unused',
  expiry_date = NOW() + INTERVAL '30 days';
```

---

## Endpoint Tests

### Base URL
```
http://localhost:3000
```

### 1. Health Check
**Endpoint:** `GET /health`

```bash
curl -X GET http://localhost:3000/health
```

**Expected Response:**
```json
{
  "status": "ok",
  "timestamp": "2025-12-10T10:12:10.000Z"
}
```

---

### 2. API Info
**Endpoint:** `GET /`

```bash
curl -X GET http://localhost:3000/
```

**Expected Response:** List of all available endpoints

---

### 3. Verify Authentication
**Endpoint:** `POST /verify`

Tests if a user with an active subscription can verify.

```bash
curl -X POST http://localhost:3000/verify \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@test.com",
    "hardware_id": "hw-123"
  }'
```

**Expected Response (Success):**
```json
{
  "success": true,
  "message": "تم التحقق بنجاح",
  "subscription_type": "premium",
  "is_active": true,
  "subscription_expired": false,
  "email_verified": true
}
```

---

### 4. Periodic Verification
**Endpoint:** `POST /verify-periodic`

Same as verify but for periodic checks.

```bash
curl -X POST http://localhost:3000/verify-periodic \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@test.com",
    "hardware_id": "hw-123"
  }'
```

**Expected Response:** Similar to `/verify`

---

### 5. Activate Device
**Endpoint:** `POST /activate`

Activates/binds a device to a user account.

```bash
curl -X POST http://localhost:3000/activate \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@test.com",
    "hardware_id": "hw-123"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "تم ربط الجهاز بنجاح",
  "subscription_type": "premium",
  "is_active": true
}
```

---

### 6. Validate Subscription Code (WITHOUT Redeeming)
**Endpoint:** `POST /validate-code`

Check if a code is valid without consuming it.

```bash
curl -X POST http://localhost:3000/validate-code \
  -H "Content-Type: application/json" \
  -d '{
    "code": "TEST123",
    "email": "test@test.com",
    "hardware_id": "hw-123"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "كود صحيح",
  "subscription_type": "premium",
  "expiry_date": "2026-01-09T10:12:10.000Z"
}
```

---

### 7. Redeem Subscription Code
**Endpoint:** `POST /redeem-code`

Uses/consumes a subscription code and applies it to the account.

```bash
curl -X POST http://localhost:3000/redeem-code \
  -H "Content-Type: application/json" \
  -d '{
    "code": "TEST123",
    "email": "test@test.com",
    "hardware_id": "hw-123"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "تم استرجاع الكود بنجاح",
  "subscription_type": "premium",
  "new_expiry_date": "2026-01-09T10:12:10.000Z",
  "subscription_extended": true
}
```

---

### 8. Generate OTP (Email Verification Code)
**Endpoint:** `POST /generate-otp`

Generates a one-time password for email verification.

```bash
curl -X POST http://localhost:3000/generate-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@test.com"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "تم إرسال الرمز للبريد الإلكتروني",
  "otp_code": "123456",
  "expires_in_minutes": 10
}
```

---

### 9. Verify OTP
**Endpoint:** `POST /verify-otp`

Verifies the OTP code sent to email.

```bash
curl -X POST http://localhost:3000/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@test.com",
    "otp_code": "123456",
    "hardware_id": "hw-123"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "تم التحقق من البريد بنجاح",
  "email_verified": true
}
```

---

### 10. Initiate Device Transfer
**Endpoint:** `POST /initiate-device-transfer`

Starts the process of transferring a subscription to a new device.

```bash
curl -X POST http://localhost:3000/initiate-device-transfer \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@test.com",
    "old_hardware_id": "hw-123",
    "new_hardware_id": "hw-456"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "تم بدء نقل الجهاز",
  "transfer_token": "unique-token-here",
  "expires_in_minutes": 30
}
```

---

### 11. Complete Device Transfer
**Endpoint:** `POST /complete-device-transfer`

Completes the device transfer process.

```bash
curl -X POST http://localhost:3000/complete-device-transfer \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@test.com",
    "new_hardware_id": "hw-456",
    "transfer_token": "unique-token-here"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "تم نقل الجهاز بنجاح",
  "new_hardware_id": "hw-456"
}
```

---

## Test Scenarios

### Scenario 1: New User Registration
1. Create new subscription in database
2. Test `/activate` endpoint
3. Test `/verify` endpoint

### Scenario 2: Subscription Code Redemption
1. Create subscription code in database
2. Test `/validate-code` endpoint (should succeed, code not consumed)
3. Test `/redeem-code` endpoint (consumes the code)
4. Test `/validate-code` again (should fail - code already used)

### Scenario 3: Email Verification
1. Test `/generate-otp` endpoint
2. Retrieve OTP from database or logs
3. Test `/verify-otp` endpoint with that OTP

### Scenario 4: Device Transfer
1. Test `/initiate-device-transfer` endpoint
2. Retrieve transfer token from database or response
3. Test `/complete-device-transfer` endpoint with that token

---

## PowerShell Testing Script

Create a file `test-endpoints.ps1`:

```powershell
# Configuration
$BaseUrl = "http://localhost:3000"
$Email = "test@test.com"
$HardwareId = "hw-123"
$Code = "TEST123"

# Function to make requests
function Test-Endpoint {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Body
    )
    
    $Url = "$BaseUrl$Endpoint"
    $JsonBody = $Body | ConvertTo-Json
    
    Write-Host "Testing: $Method $Endpoint" -ForegroundColor Cyan
    Write-Host "Body: $JsonBody" -ForegroundColor Gray
    
    try {
        $Response = Invoke-WebRequest -Uri $Url -Method $Method `
            -Headers @{"Content-Type"="application/json"} `
            -Body $JsonBody -UseBasicParsing
        
        Write-Host "Status: $($Response.StatusCode)" -ForegroundColor Green
        Write-Host "Response: $($Response.Content)" -ForegroundColor Green
    } catch {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Run tests
Test-Endpoint -Method "GET" -Endpoint "/health" -Body @{}
Test-Endpoint -Method "POST" -Endpoint "/verify" -Body @{
    email = $Email
    hardware_id = $HardwareId
}
Test-Endpoint -Method "POST" -Endpoint "/activate" -Body @{
    email = $Email
    hardware_id = $HardwareId
}
Test-Endpoint -Method "POST" -Endpoint "/validate-code" -Body @{
    code = $Code
    email = $Email
    hardware_id = $HardwareId
}
Test-Endpoint -Method "POST" -Endpoint "/redeem-code" -Body @{
    code = $Code
    email = $Email
    hardware_id = $HardwareId
}
```

Run it:
```powershell
.\test-endpoints.ps1
```

---

## Debugging Tips

1. **Check Server Logs**: Look at server console for detailed error messages
2. **Verify Database Data**: Check if test data exists in Supabase
3. **Check Rate Limiting**: 5 requests per 15 minutes for auth endpoints
4. **Validate JSON**: Ensure request bodies are valid JSON
5. **Check Environment Variables**: Verify SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY are set
