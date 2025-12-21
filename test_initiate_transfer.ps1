$body = @{
    email = "test@test.com"
    current_hardware_id = "hw-999"
} | ConvertTo-Json

Write-Host "Testing /initiate-device-transfer" -ForegroundColor Cyan
Write-Host "Body: $body" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response:"
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch {
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorContent = $reader.ReadToEnd()
        Write-Host "Error: $errorContent"
    }
}
