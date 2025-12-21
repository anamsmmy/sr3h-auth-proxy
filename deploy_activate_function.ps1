# Deploy activate_subscription RPC function to Supabase

$supabaseUrl = "https://fvayvetnlneekaqjkwjy.supabase.co"
$supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw"

# Read the SQL file
$sqlFile = "c:\SR3H_MACRO\Database\add_activate_subscription_rpc.sql"
$sql = Get-Content $sqlFile -Raw

Write-Host "SQL to execute:" -ForegroundColor Cyan
Write-Host $sql -ForegroundColor Gray
Write-Host ""
Write-Host "Note: Execute this SQL directly in Supabase Studio SQL Editor or use Supabase CLI" -ForegroundColor Yellow
Write-Host "URL: $supabaseUrl" -ForegroundColor Gray
