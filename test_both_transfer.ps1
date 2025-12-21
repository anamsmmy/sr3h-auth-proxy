Write-Host "=== Testing Both Transfer Endpoints ===" -ForegroundColor Cyan
Write-Host ""

$json = '{"email":"test@test.com","current_hardware_id":"hw-999"}'

# Test 1: Without rate limiter
Write-Host "1. Testing /test-transfer (NO rate limiter)" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:3000/test-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json
    
    Write-Host "✅ Status: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch {
    Write-Host "❌ Status: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = [System.IO.StreamReader]::new($stream)
    $content = $reader.ReadToEnd()
    Write-Host "Response: '$content'"
}

Write-Host ""
Write-Host "2. Testing /initiate-device-transfer (WITH rate limiter)" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json
    
    Write-Host "✅ Status: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch {
    Write-Host "❌ Status: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = [System.IO.StreamReader]::new($stream)
    $content = $reader.ReadToEnd()
    if ($content.Length -gt 0) {
        Write-Host "Response: '$content'"
    } else {
        Write-Host "Response: (empty body)"
    }
}
