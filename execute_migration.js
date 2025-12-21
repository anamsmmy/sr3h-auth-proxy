const https = require('https');
const querystring = require('querystring');

const SUPABASE_URL = 'fvayvetnlneekaqjkwjy.supabase.co';
const SERVICE_ROLE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw';

const sqlStatements = [
  // Step 1: Add duration_days column
  `ALTER TABLE macro_fort_subscription_codes 
   ADD COLUMN IF NOT EXISTS duration_days INTEGER DEFAULT 30;`,
  
  // Step 2: Add last_device_transfer_date column
  `ALTER TABLE macro_fort_subscriptions 
   ADD COLUMN IF NOT EXISTS last_device_transfer_date TIMESTAMP WITH TIME ZONE;`,
  
  // Step 3: Add device_transfers_30days column
  `ALTER TABLE macro_fort_subscriptions 
   ADD COLUMN IF NOT EXISTS device_transfers_30days INTEGER DEFAULT 0;`,
  
  // Step 4: Update complete_device_transfer RPC
  `CREATE OR REPLACE FUNCTION complete_device_transfer(
    p_email TEXT,
    p_new_hardware_id TEXT,
    p_transfer_token TEXT
  )
  RETURNS JSONB AS $$
  DECLARE
    v_token_record RECORD;
    v_subscription RECORD;
    v_current_timestamp TIMESTAMP WITH TIME ZONE;
    v_transfers_in_30days INTEGER;
  BEGIN
    v_current_timestamp := TIMEZONE('UTC', NOW());

    SELECT * INTO v_token_record
    FROM macro_fort_verification_codes
    WHERE email = p_email
      AND code = p_transfer_token
      AND code_type = 'TRANSFER'
      AND used_date IS NULL
      AND expiry_date > v_current_timestamp
    LIMIT 1;

    IF NOT FOUND THEN
      RETURN jsonb_build_object(
        'success', FALSE,
        'message', 'رمز نقل غير صحيح أو منتهي الصلاحية'
      );
    END IF;

    SELECT * INTO v_subscription
    FROM macro_fort_subscriptions
    WHERE email = p_email
    LIMIT 1;

    IF NOT FOUND THEN
      RETURN jsonb_build_object(
        'success', FALSE,
        'message', 'لم يتم العثور على اشتراك'
      );
    END IF;

    SELECT COUNT(*) INTO v_transfers_in_30days
    FROM macro_fort_subscriptions
    WHERE email = p_email
      AND last_device_transfer_date > (v_current_timestamp - INTERVAL '30 days');

    IF v_transfers_in_30days >= 10 THEN
      RETURN jsonb_build_object(
        'success', FALSE,
        'message', 'لقد تجاوزت الحد الأقصى لنقل الجهاز (10 نقلات كل 30 يوم)',
        'transfers_used', v_transfers_in_30days,
        'max_transfers_per_30days', 10
      );
    END IF;

    UPDATE macro_fort_verification_codes
    SET used_date = v_current_timestamp
    WHERE email = p_email AND code = p_transfer_token;

    UPDATE macro_fort_subscriptions
    SET
      device_transfer_count = COALESCE(device_transfer_count, 0) + 1,
      last_device_transfer_date = v_current_timestamp,
      device_transfers_30days = (
        SELECT COUNT(*) 
        FROM macro_fort_subscriptions 
        WHERE email = p_email 
          AND last_device_transfer_date > (v_current_timestamp - INTERVAL '30 days')
      ) + 1,
      updated_at = v_current_timestamp
    WHERE email = p_email;

    SELECT * INTO v_subscription
    FROM macro_fort_subscriptions
    WHERE email = p_email
    LIMIT 1;

    RETURN jsonb_build_object(
      'success', TRUE,
      'message', 'تم نقل الجهاز بنجاح',
      'subscription_type', v_subscription.subscription_type,
      'expiry_date', v_subscription.expiry_date,
      'device_transfer_count', COALESCE(v_subscription.device_transfer_count, 0) + 1,
      'transfers_used_in_30days', v_transfers_in_30days + 1,
      'max_transfers_per_30days', 10
    );
  END;
  $$ LANGUAGE plpgsql SECURITY DEFINER;`
];

function makeRequest(sql) {
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify({ query: sql });

    const options = {
      hostname: SUPABASE_URL,
      path: '/rest/v1/rpc/',
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${SERVICE_ROLE_KEY}`,
        'apikey': SERVICE_ROLE_KEY,
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(postData)
      },
      timeout: 30000
    };

    const req = https.request(options, (res) => {
      let data = '';

      res.on('data', (chunk) => {
        data += chunk;
      });

      res.on('end', () => {
        resolve({
          statusCode: res.statusCode,
          body: data
        });
      });
    });

    req.on('error', reject);
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('Request timeout'));
    });

    req.write(postData);
    req.end();
  });
}

async function executeMigrations() {
  console.log('🚀 Starting database migrations...\n');
  let successCount = 0;

  for (let i = 0; i < sqlStatements.length; i++) {
    const stepNum = i + 1;
    const sql = sqlStatements[i];
    
    console.log(`[${stepNum}/${sqlStatements.length}] Executing SQL statement...`);
    
    try {
      const result = await makeRequest(sql);
      
      if (result.statusCode === 200 || result.statusCode === 201) {
        console.log(`✅ Step ${stepNum} completed successfully\n`);
        successCount++;
      } else {
        console.log(`⚠️ Step ${stepNum} returned status ${result.statusCode}`);
        console.log(`Response: ${result.body}\n`);
      }
    } catch (error) {
      console.log(`❌ Step ${stepNum} failed: ${error.message}\n`);
    }
  }

  console.log(`\n${'='.repeat(50)}`);
  console.log(`✅ Migration Summary: ${successCount}/${sqlStatements.length} steps completed`);
  console.log(`${'='.repeat(50)}`);
}

executeMigrations().catch(console.error);
