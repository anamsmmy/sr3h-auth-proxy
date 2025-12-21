$headers = @{
    'Authorization' = 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3MDI4MTkwMjksImV4cCI6MjAxODM5NTAyOX0.hW5iyRqwYIH8GZFVLPJVLEW4Aq5UBUz0v8OIjY7vIGE'
    'apikey' = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3MDI4MTkwMjksImV4cCI6MjAxODM5NTAyOX0.hW5iyRqwYIH8GZFVLPJVLEW4Aq5UBUz0v8OIjY7vIGE'
}

Write-Host "📊 استعلام عن الاشتراكات للبريد: msmmy1@gmail.com" -ForegroundColor Cyan

$uri = 'https://fvayvetnlneekaqjkwjy.supabase.co/rest/v1/macro_fort_subscriptions?email=eq.msmmy1@gmail.com&select=*'
$response = Invoke-WebRequest -Uri $uri -Headers $headers -Method Get

$data = $response.Content | ConvertFrom-Json

Write-Host "`n✅ نتائج جدول macro_fort_subscriptions:" -ForegroundColor Green
Write-Host ($data | ConvertTo-Json -Depth 10)

if ($data -and $data.Count -gt 0) {
    $sub = $data[0]
    Write-Host "`n📋 تفاصيل الاشتراك:" -ForegroundColor Yellow
    Write-Host "   ID: $($sub.id)"
    Write-Host "   Email: $($sub.email)"
    Write-Host "   Hardware ID: $($sub.hardware_id)"
    Write-Host "   Subscription Code: $($sub.subscription_code)"
    Write-Host "   Subscription Type: $($sub.subscription_type)"
    Write-Host "   Status: $(if ($sub.is_active) { 'نشط' } else { 'غير نشط' })"
    Write-Host "   Max Devices: $($sub.max_devices)"
    Write-Host "   Device Transfer Count: $($sub.device_transfer_count)"
    Write-Host "   Expiry Date: $($sub.expiry_date)"
} else {
    Write-Host "`n❌ لا توجد اشتراكات لهذا البريد الإلكتروني!" -ForegroundColor Red
}

Write-Host "`n`n📊 استعلام عن الأكواس للبريد: msmmy1@gmail.com" -ForegroundColor Cyan

$uri2 = 'https://fvayvetnlneekaqjkwjy.supabase.co/rest/v1/macro_fort_subscription_codes?email=eq.msmmy1@gmail.com&select=*'
$response2 = Invoke-WebRequest -Uri $uri2 -Headers $headers -Method Get

$data2 = $response2.Content | ConvertFrom-Json

Write-Host "`n✅ نتائج جدول macro_fort_subscription_codes:" -ForegroundColor Green
Write-Host ($data2 | ConvertTo-Json -Depth 10)
