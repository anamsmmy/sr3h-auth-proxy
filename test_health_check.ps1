Write-Host "Testing /health endpoint" -ForegroundColor Cyan

try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/health" `
        -Method GET
    
    Write-Host "✅ Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response:"
    $response.Content
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Testing / endpoint" -ForegroundColor Cyan

try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/" `
        -Method GET
    
    Write-Host "✅ Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response (first 500 chars):"
    $response.Content.Substring(0, [System.Math]::Min(500, $response.Content.Length))
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
