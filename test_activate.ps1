$body = @{
    email = "test@test.com"
    hardware_id = "hw-new-456"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/activate" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "Status Code: $($response.StatusCode)"
    Write-Host "Response Body:"
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch {
    Write-Host "Error Status Code: $($_.Exception.Response.StatusCode)"
    Write-Host "Error Message: $($_.Exception.Message)"
}
