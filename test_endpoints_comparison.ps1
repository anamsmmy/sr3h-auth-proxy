Write-Host "=== Testing Multiple Endpoints ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Health (no auth limiter)
Write-Host "1. Testing /health (no auth limiter)" -ForegroundColor Yellow
try {
    $r = Invoke-WebRequest -Uri "http://localhost:3000/health" -Method GET
    Write-Host "✅ Status: $($r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Verify (with auth limiter, should work)
Write-Host "2. Testing /verify (with auth limiter)" -ForegroundColor Yellow
$json = '{"email":"test@test.com","hardware_id":"hw-123"}'
try {
    $r = Invoke-WebRequest -Uri "http://localhost:3000/verify" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json `
        -UseBasicParsing
    Write-Host "✅ Status: $($r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $content = $reader.ReadToEnd()
    if ($content) { Write-Host "   Response: $content" }
}

Write-Host ""

# Test 3: Initiate transfer (with auth limiter, failing)
Write-Host "3. Testing /initiate-device-transfer (with auth limiter)" -ForegroundColor Yellow
$json = '{"email":"test@test.com","current_hardware_id":"hw-999"}'
try {
    $r = Invoke-WebRequest -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json `
        -UseBasicParsing
    Write-Host "✅ Status: $($r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $content = $reader.ReadToEnd()
    if ($content) { Write-Host "   Response: $content" } else { Write-Host "   Response: (empty)" }
}
