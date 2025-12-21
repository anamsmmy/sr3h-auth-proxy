Write-Host "Testing /initiate-device-transfer with full debug output" -ForegroundColor Cyan
Write-Host ""

$json = '{"email":"test@test.com","current_hardware_id":"hw-999"}'

Write-Host "Sending:"
Write-Host $json
Write-Host ""

try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $json `
        -UseBasicParsing `
        -TimeoutSec 10
    
    Write-Host "✅ SUCCESS" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)"
    Write-Host "Content:"
    $response.Content
} catch {
    Write-Host "❌ FAILED" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.Value__)"
    Write-Host "Status Description: $($_.Exception.Response.StatusDescription)"
    
    $streamReader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
    $content = $streamReader.ReadToEnd()
    $streamReader.Close()
    
    Write-Host "Response Length: $($content.Length)"
    Write-Host "Response Content:"
    Write-Host "'$content'"
    
    if ($content.Length -gt 0) {
        try {
            $json = $content | ConvertFrom-Json
            Write-Host "Parsed JSON:"
            $json | ConvertTo-Json
        } catch {
            Write-Host "Failed to parse as JSON"
        }
    }
}
