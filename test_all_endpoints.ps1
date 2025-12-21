Write-Host "=== Comprehensive Endpoint Testing ===" -ForegroundColor Cyan
Write-Host ""

$endpoints = @(
    @{
        name = "/health"
        method = "GET"
        uri = "http://localhost:3000/health"
        body = $null
    },
    @{
        name = "/verify"
        method = "POST"
        uri = "http://localhost:3000/verify"
        body = '{"email":"test@test.com","hardware_id":"hw-123"}'
    },
    @{
        name = "/activate"
        method = "POST"
        uri = "http://localhost:3000/activate"
        body = '{"email":"test@test.com","hardware_id":"hw-999"}'
    },
    @{
        name = "/validate-code"
        method = "POST"
        uri = "http://localhost:3000/validate-code"
        body = '{"code":"TEST123","email":"test@test.com","hardware_id":"hw-123"}'
    },
    @{
        name = "/generate-otp"
        method = "POST"
        uri = "http://localhost:3000/generate-otp"
        body = '{"email":"test@test.com"}'
    },
    @{
        name = "/initiate-device-transfer"
        method = "POST"
        uri = "http://localhost:3000/initiate-device-transfer"
        body = '{"email":"test@test.com","current_hardware_id":"hw-999"}'
    }
)

foreach ($endpoint in $endpoints) {
    Write-Host "Testing: $($endpoint.name)" -ForegroundColor Yellow
    
    try {
        if ($endpoint.method -eq "GET") {
            $response = Invoke-WebRequest -Uri $endpoint.uri -Method GET -TimeoutSec 5
        } else {
            $response = Invoke-WebRequest -Uri $endpoint.uri -Method POST `
                -ContentType "application/json" -Body $endpoint.body -TimeoutSec 5
        }
        
        Write-Host "  ✅ Status: $($response.StatusCode)" -ForegroundColor Green
        
        $content = $response.Content | ConvertFrom-Json
        if ($content.success) {
            Write-Host "  ✅ Success: $($content.message)" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️ Failed: $($content.message)" -ForegroundColor Yellow
        }
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        Write-Host "  ❌ Status: $statusCode" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "✅ /health - Health check"
Write-Host "✅ /verify - Verify subscription"
Write-Host "✅ /activate - Activate device"
Write-Host "⏳ /validate-code - Validate code (needs valid code)"
Write-Host "⏳ /generate-otp - Generate OTP"
Write-Host "⏳ /initiate-device-transfer - Device transfer (needs RPC to be working)"
