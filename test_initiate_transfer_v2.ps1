$body = @{
    email = "test@test.com"
    current_hardware_id = "hw-999"
} | ConvertTo-Json

Write-Host "Testing /initiate-device-transfer" -ForegroundColor Cyan
Write-Host "Body: $body" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body
    
    Write-Host "✅ Success - Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response:"
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch [System.Net.WebException] {
    $ex = $_.Exception
    Write-Host "❌ Status Code: $($ex.Response.StatusCode)" -ForegroundColor Red
    
    if ($ex.Response) {
        $stream = $ex.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Response Body: '$errorBody'" -ForegroundColor Yellow
        
        if ($errorBody) {
            try {
                $errorJson = $errorBody | ConvertFrom-Json
                Write-Host "Parsed Error:"
                $errorJson | ConvertTo-Json
            } catch {
                Write-Host "Raw Error: $errorBody"
            }
        }
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
