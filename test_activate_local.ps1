$body = @{
    email = "test@test.com"
    hardware_id = "hw-999"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/activate" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "Status Code: $($response.StatusCode)"
    Write-Host "Response:"
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch {
    Write-Host "Error Status Code: $($_.Exception.Response.StatusCode)"
    Write-Host "Error Response:"
    try {
        $_.Exception.Response.Content.ToString() | ConvertFrom-Json | ConvertTo-Json
    } catch {
        $_.Exception.Message
    }
}
