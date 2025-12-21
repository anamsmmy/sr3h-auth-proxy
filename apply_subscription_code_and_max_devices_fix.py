import requests
import json
import sys

SUPABASE_URL = "https://fvayvetnlneekaqjkwjy.supabase.co"
SERVICE_ROLE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw"

headers = {
    "Authorization": f"Bearer {SERVICE_ROLE_KEY}",
    "apikey": SERVICE_ROLE_KEY,
    "Content-Type": "application/json",
}

print('🚀 تطبيق Migrations على Supabase...\n')
print('📋 التعديلات المطبقة:')
print('1. إضافة حقل subscription_code')
print('2. تحديث max_devices من 3 إلى 10\n')

success_count = 0
total_steps = 3

queries = [
    ("إضافة حقل subscription_code", 
     "ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS subscription_code TEXT;"),
    
    ("تحديث default value لـ max_devices",
     "ALTER TABLE macro_fort_subscriptions ALTER COLUMN max_devices SET DEFAULT 10;"),
    
    ("تحديث السجلات الموجودة",
     "UPDATE macro_fort_subscriptions SET max_devices = 10 WHERE max_devices = 3;")
]

for step_num, (description, query) in enumerate(queries, 1):
    print(f"[{step_num}/{total_steps}] {description}...")
    
    try:
        response = requests.post(
            f"{SUPABASE_URL}/rest/v1/rpc/",
            headers=headers,
            json={"query": query},
            timeout=30
        )
        
        if response.status_code in [200, 201]:
            print(f"✅ الخطوة {step_num} تمت بنجاح\n")
            success_count += 1
        else:
            print(f"⚠️ الخطوة {step_num} أرجعت status {response.status_code}")
            print(f"الرد: {response.text}\n")
    except Exception as e:
        print(f"❌ الخطوة {step_num} فشلت: {str(e)}\n")

print(f"\n{'='*60}")
print(f"✅ ملخص التطبيق: {success_count}/{total_steps} خطوات تمت بنجاح")
print(f"{'='*60}")

if success_count == total_steps:
    print('\n✨ جميع التعديلات طُبقت بنجاح على Supabase!')
else:
    print('\n⚠️ بعض الخطوات قد لم تنجح، يرجى مراجعة السجلات.')
