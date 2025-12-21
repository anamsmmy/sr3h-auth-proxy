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
    
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response:"
    $response.Content
} catch {
    Write-Host "Error Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error Content:"
    $errorResponse = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($errorResponse)
    $errorContent = $reader.ReadToEnd()
    Write-Host $errorContent -ForegroundColor Red
}
