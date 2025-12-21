const https = require('https');

const SUPABASE_URL = 'fvayvetnlneekaqjkwjy.supabase.co';
const SERVICE_ROLE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw';

const sqlStatements = [
  // Step 1: Add subscription_code column
  `ALTER TABLE macro_fort_subscriptions 
   ADD COLUMN IF NOT EXISTS subscription_code TEXT;`,
  
  // Step 2: Update max_devices default value from 3 to 10
  `ALTER TABLE macro_fort_subscriptions 
   ALTER COLUMN max_devices SET DEFAULT 10;`,
  
  // Step 3: Update existing records that have max_devices = 3 to 10
  `UPDATE macro_fort_subscriptions 
   SET max_devices = 10 
   WHERE max_devices = 3;`,
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
  console.log('🚀 تطبيق Migrations على Supabase...\n');
  console.log('📋 التعديلات المطبقة:');
  console.log('1. إضافة حقل subscription_code');
  console.log('2. تحديث max_devices من 3 إلى 10\n');
  
  let successCount = 0;

  for (let i = 0; i < sqlStatements.length; i++) {
    const stepNum = i + 1;
    const sql = sqlStatements[i];
    
    console.log(`[${stepNum}/${sqlStatements.length}] تنفيذ SQL statement...`);
    
    try {
      const result = await makeRequest(sql);
      
      if (result.statusCode === 200 || result.statusCode === 201) {
        console.log(`✅ الخطوة ${stepNum} تمت بنجاح\n`);
        successCount++;
      } else {
        console.log(`⚠️ الخطوة ${stepNum} أرجعت status ${result.statusCode}`);
        console.log(`الرد: ${result.body}\n`);
      }
    } catch (error) {
      console.log(`❌ الخطوة ${stepNum} فشلت: ${error.message}\n`);
    }
  }

  console.log(`\n${'='.repeat(60)}`);
  console.log(`✅ ملخص التطبيق: ${successCount}/${sqlStatements.length} خطوات تمت بنجاح`);
  console.log(`${'='.repeat(60)}`);
  
  if (successCount === sqlStatements.length) {
    console.log('\n✨ جميع التعديلات طُبقت بنجاح على Supabase!');
  } else {
    console.log('\n⚠️ بعض الخطوات قد لم تنجح، يرجى مراجعة السجلات.');
  }
}

executeMigrations().catch(console.error);
