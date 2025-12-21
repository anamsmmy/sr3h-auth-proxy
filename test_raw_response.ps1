Write-Host "Testing raw response from /initiate-device-transfer" -ForegroundColor Cyan
Write-Host ""

$json = '{"email":"test@test.com","current_hardware_id":"hw-999"}'

try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json `
        -UseBasicParsing
    
    Write-Host "✅ SUCCESS" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)"
    Write-Host "Response content-type: $($response.Headers['Content-Type'])"
    Write-Host "Raw content: '$($response.Content)'"
} catch {
    Write-Host "❌ FAILED" -ForegroundColor Red
    
    $resp = $_.Exception.Response
    Write-Host "Status Code: $($resp.StatusCode.Value__)"
    Write-Host "Status Description: $($resp.StatusDescription)"
    Write-Host "Content Type: $($resp.Headers['Content-Type'])"
    Write-Host "Content Length: $($resp.ContentLength)"
    
    if ($resp.ContentLength -ne 0) {
        $stream = $resp.GetResponseStream()
        $reader = [System.IO.StreamReader]::new($stream)
        $content = $reader.ReadToEnd()
        Write-Host "Raw Content bytes: $([System.Text.Encoding]::UTF8.GetBytes($content) | foreach {$_.ToString('X2')}) -join ' '}"
        Write-Host "Raw Content: '$content'"
    }
}
