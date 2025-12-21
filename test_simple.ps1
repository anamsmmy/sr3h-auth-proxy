Write-Host "Test 1: Simple test to verify body parsing" -ForegroundColor Cyan

$json = '{"email":"test@test.com","current_hardware_id":"hw-999"}'

Write-Host "JSON String: $json" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json `
        -UseBasicParsing
    
    Write-Host "✅ Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Body: $($response.Content)"
} catch {
    Write-Host "❌ Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    
    $resp = $_.Exception.Response
    if ($resp) {
        try {
            $content = $resp.GetResponseStream() | { $reader = New-Object System.IO.StreamReader($_); $reader.ReadToEnd() }
            Write-Host "Response Content: '$content'" -ForegroundColor Yellow
        } catch {
            Write-Host "Could not read response"
        }
    }
}
