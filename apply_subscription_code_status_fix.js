const https = require('https');
const fs = require('fs');
const path = require('path');

const SUPABASE_URL = 'fvayvetnlneekaqjkwjy.supabase.co';
const SERVICE_ROLE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw';

const sqlFilePath = path.join(__dirname, 'Database', 'migration_fix_subscription_code_status.sql');

function readSQLFile() {
  try {
    const sqlContent = fs.readFileSync(sqlFilePath, 'utf8');
    
    const statements = sqlContent
      .split(';')
      .map(stmt => stmt.trim())
      .filter(stmt => stmt.length > 0 && !stmt.startsWith('--'));
    
    return statements;
  } catch (error) {
    console.error(`❌ Failed to read SQL file: ${error.message}`);
    process.exit(1);
  }
}

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
          body: data,
          headers: res.headers
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
  console.log('🚀 Starting Subscription Code Status Fix Migration...\n');
  
  const statements = readSQLFile();
  console.log(`📋 Found ${statements.length} SQL statements to execute\n`);
  
  let successCount = 0;
  let errorCount = 0;

  for (let i = 0; i < statements.length; i++) {
    const stepNum = i + 1;
    const sql = statements[i];
    
    if (sql.length === 0) continue;
    
    const shortSql = sql.substring(0, 80).replace(/\n/g, ' ') + (sql.length > 80 ? '...' : '');
    console.log(`[${stepNum}/${statements.length}] Executing: ${shortSql}`);
    
    try {
      const result = await makeRequest(sql);
      
      if (result.statusCode === 200 || result.statusCode === 201 || result.statusCode === 204) {
        console.log(`✅ Step ${stepNum} completed successfully`);
        successCount++;
      } else {
        console.log(`⚠️ Step ${stepNum} returned status ${result.statusCode}`);
        try {
          const responseBody = JSON.parse(result.body);
          console.log(`📢 Response: ${JSON.stringify(responseBody, null, 2)}`);
        } catch (e) {
          console.log(`📢 Response: ${result.body}`);
        }
        errorCount++;
      }
    } catch (error) {
      console.log(`❌ Step ${stepNum} failed: ${error.message}`);
      errorCount++;
    }
    
    console.log('');
    
    // Add small delay between requests
    await new Promise(resolve => setTimeout(resolve, 500));
  }

  console.log(`\n${'='.repeat(60)}`);
  console.log(`✅ Migration Summary:`);
  console.log(`   ✓ Successful: ${successCount}`);
  console.log(`   ✗ Failed: ${errorCount}`);
  console.log(`   Total: ${statements.length}`);
  console.log(`${'='.repeat(60)}`);
  
  if (errorCount === 0) {
    console.log('\n🎉 Subscription Code Status Fix Migration Completed Successfully!');
    console.log('\n📝 Changes Applied:');
    console.log('   ✓ Updated CHECK constraint to allow only 3 states (unused/used/expired)');
    console.log('   ✓ Updated mark_code_as_used function');
    console.log('   ✓ Updated validate_subscription_code_status function');
    console.log('   ✓ Updated redeem_subscription_code function');
    console.log('   ✓ Updated authenticate_user function');
  } else {
    console.log('\n⚠️ Some steps failed. Please review the output above.');
  }
}

executeMigrations().catch(console.error);
