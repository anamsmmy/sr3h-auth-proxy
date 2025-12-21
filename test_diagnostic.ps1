Write-Host "=== DIAGNOSTIC TEST ===" -ForegroundColor Cyan
Write-Host ""

$json = '{"email":"test@test.com","current_hardware_id":"hw-999"}'

# Test 1: GET /health
Write-Host "1. GET /health" -ForegroundColor Yellow
try {
    $r = Invoke-WebRequest -Uri "http://localhost:3000/health" -Method GET
    Write-Host "   ✅ $($r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
}

# Test 2: POST /test-transfer (should work)
Write-Host "2. POST /test-transfer" -ForegroundColor Yellow
try {
    $r = Invoke-WebRequest -Uri "http://localhost:3000/test-transfer" `
        -Method POST -ContentType "application/json" -Body $json
    $data = $r.Content | ConvertFrom-Json
    if ($data.success -and $data.received.email) {
        Write-Host "   ✅ $($r.StatusCode) - Body received correctly" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ $($r.StatusCode) - Body not received" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
}

# Test 3: POST /initiate-device-transfer (currently failing)
Write-Host "3. POST /initiate-device-transfer" -ForegroundColor Yellow
try {
    $r = Invoke-WebRequest -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST -ContentType "application/json" -Body $json
    $data = $r.Content | ConvertFrom-Json
    if ($data.success) {
        Write-Host "   ✅ $($r.StatusCode) - Working!" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ $($r.StatusCode) - Not working: $($data.message)" -ForegroundColor Yellow
    }
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
    Write-Host "   ❌ $statusCode" -ForegroundColor Red
    try {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = [System.IO.StreamReader]::new($stream)
        $content = $reader.ReadToEnd()
        if ($content.Length -gt 0) {
            $data = $content | ConvertFrom-Json
            Write-Host "      Message: $($data.message)" -ForegroundColor Red
        } else {
            Write-Host "      (empty response body)" -ForegroundColor Red
        }
    } catch {
        Write-Host "      Could not parse response" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "- /test-transfer works ✅"
Write-Host "- /initiate-device-transfer does NOT work ❌"
Write-Host "- Both endpoints have identical code structure"
Write-Host "- Issue must be elsewhere"
