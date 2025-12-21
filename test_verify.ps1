$body = @{
    email = "test@test.com"
    hardware_id = "hw-123"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/verify" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "Status Code: $($response.StatusCode)"
    Write-Host "Response Body:"
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Response: $($_.Exception.Response.Content)"
    }
}
