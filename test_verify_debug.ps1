Write-Host "Testing /verify endpoint (should work)" -ForegroundColor Cyan
Write-Host ""

$json = '{"email":"test@test.com","hardware_id":"hw-123"}'

Write-Host "Sending: $json" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:3000/verify" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json `
        -UseBasicParsing
    
    Write-Host "✅ SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch {
    Write-Host "❌ FAILED - Status: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    
    $streamReader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
    $content = $streamReader.ReadToEnd()
    Write-Host "Response: '$content'"
}
