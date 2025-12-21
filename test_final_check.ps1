Write-Host "FINAL CHECK" -ForegroundColor Cyan
Write-Host ""

$json = '{"email":"test@test.com","current_hardware_id":"hw-999"}'

Write-Host "1. /test-plaintext" -ForegroundColor Yellow
curl.exe -s -X POST http://localhost:3000/test-plaintext
Write-Host ""

Write-Host "2. /test-transfer" -ForegroundColor Yellow
try {
    $r = Invoke-WebRequest -Uri "http://localhost:3000/test-transfer" -Method POST -ContentType "application/json" -Body $json
    Write-Host "Status: $($r.StatusCode)"
    $r.Content
} catch {
    Write-Host "Error: $($_.Exception.Response.StatusCode.Value__)"
}
Write-Host ""

Write-Host "3. /initiate-device-transfer" -ForegroundColor Yellow
try {
    $r = Invoke-WebRequest -Uri "http://localhost:3000/initiate-device-transfer" -Method POST -ContentType "application/json" -Body $json
    Write-Host "Status: $($r.StatusCode)"
    $r.Content
} catch {
    Write-Host "Error: $($_.Exception.Response.StatusCode.Value__)"
    # Try to get the body
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = [System.IO.StreamReader]::new($stream)
    $content = $reader.ReadToEnd()
    if ($content.Length -eq 0) {
        Write-Host "Body: (empty)"
    } else {
        Write-Host "Body: $content"
    }
}
