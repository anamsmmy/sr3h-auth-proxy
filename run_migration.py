#!/usr/bin/env python3
import requests
import json
import sys

# Supabase credentials
SUPABASE_URL = "https://fvayvetnlneekaqjkwjy.supabase.co"
SERVICE_ROLE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw"

headers = {
    "Authorization": f"Bearer {SERVICE_ROLE_KEY}",
    "apikey": SERVICE_ROLE_KEY,
    "Content-Type": "application/json",
}

# Step 1: Add duration_days to macro_fort_subscription_codes
print("Step 1: Adding duration_days column to macro_fort_subscription_codes...")
query1 = """
ALTER TABLE macro_fort_subscription_codes 
ADD COLUMN IF NOT EXISTS duration_days INTEGER DEFAULT 30;
"""

try:
    response = requests.post(
        f"{SUPABASE_URL}/rest/v1/",
        headers=headers,
        json={"query": query1}
    )
    if response.status_code in [200, 201]:
        print("✓ Column duration_days added successfully")
    else:
        print(f"✗ Error: {response.status_code} - {response.text}")
except Exception as e:
    print(f"✗ Connection error: {e}")
    sys.exit(1)

# Step 2: Add last_device_transfer_date
print("\nStep 2: Adding last_device_transfer_date column...")
query2 = """
ALTER TABLE macro_fort_subscriptions 
ADD COLUMN IF NOT EXISTS last_device_transfer_date TIMESTAMP WITH TIME ZONE;
"""

try:
    response = requests.post(
        f"{SUPABASE_URL}/rest/v1/",
        headers=headers,
        json={"query": query2}
    )
    if response.status_code in [200, 201]:
        print("✓ Column last_device_transfer_date added successfully")
    else:
        print(f"✗ Error: {response.status_code} - {response.text}")
except Exception as e:
    print(f"✗ Connection error: {e}")

# Step 3: Add device_transfers_30days
print("\nStep 3: Adding device_transfers_30days column...")
query3 = """
ALTER TABLE macro_fort_subscriptions 
ADD COLUMN IF NOT EXISTS device_transfers_30days INTEGER DEFAULT 0;
"""

try:
    response = requests.post(
        f"{SUPABASE_URL}/rest/v1/",
        headers=headers,
        json={"query": query3}
    )
    if response.status_code in [200, 201]:
        print("✓ Column device_transfers_30days added successfully")
    else:
        print(f"✗ Error: {response.status_code} - {response.text}")
except Exception as e:
    print(f"✗ Connection error: {e}")

print("\n✓ Migration completed successfully!")
