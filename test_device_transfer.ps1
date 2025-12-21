Write-Host "=== Testing Device Transfer Flow ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Initiate device transfer
Write-Host "Step 1: Initiate Device Transfer" -ForegroundColor Yellow
$initiateBody = @{
    email = "test@test.com"
    current_hardware_id = "hw-999"
    new_hardware_id = "hw-555"
} | ConvertTo-Json

Write-Host "Request: $initiateBody" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/initiate-device-transfer" `
        -Method POST `
        -ContentType "application/json" `
        -Body $initiateBody `
        -ErrorAction Stop
    
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
    $responseData = $response.Content | ConvertFrom-Json
    Write-Host ($responseData | ConvertTo-Json) -ForegroundColor Green
    $transferToken = $responseData.transfer_token
    Write-Host ""
    
    # Step 2: Complete device transfer
    if ($transferToken) {
        Write-Host "Step 2: Complete Device Transfer" -ForegroundColor Yellow
        $completeBody = @{
            email = "test@test.com"
            new_hardware_id = "hw-555"
            transfer_token = $transferToken
        } | ConvertTo-Json
        
        Write-Host "Request: $completeBody" -ForegroundColor Gray
        
        $response2 = Invoke-WebRequest -Uri "http://localhost:3000/complete-device-transfer" `
            -Method POST `
            -ContentType "application/json" `
            -Body $completeBody `
            -ErrorAction Stop
        
        Write-Host "Status: $($response2.StatusCode)" -ForegroundColor Green
        Write-Host ($response2.Content | ConvertFrom-Json | ConvertTo-Json) -ForegroundColor Green
    }
} catch {
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorContent = $reader.ReadToEnd()
        Write-Host "Error: $errorContent"
    }
}
