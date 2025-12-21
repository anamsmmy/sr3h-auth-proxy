Write-Host "Testing /initiate-device-transfer with raw JSON" -ForegroundColor Cyan
Write-Host ""

$json = '{"email":"test@test.com","current_hardware_id":"hw-999"}'

Write-Host "Sending JSON: $json" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json `
        -UseBasicParsing
    
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response:"
    $response.Content
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
    Write-Host "❌ Failed" -ForegroundColor Red
    Write-Host "Status Code: $statusCode"
    
    if ($_.Exception.Response.ContentLength -gt 0) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $content = $reader.ReadToEnd()
        Write-Host "Response: $content"
    } else {
        Write-Host "Response: (empty)"
    }
}
