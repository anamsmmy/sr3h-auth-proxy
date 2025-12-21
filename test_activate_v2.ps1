$body = @{
    email = "test@test.com"
    hardware_id = "hw-999"
} | ConvertTo-Json

Write-Host "Request Body: $body" -ForegroundColor Cyan

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
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorContent = $reader.ReadToEnd()
        Write-Host "Raw Error Response:" -ForegroundColor Yellow
        Write-Host $errorContent
    } else {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
