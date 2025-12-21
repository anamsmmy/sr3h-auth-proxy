const https = require('https');
const fs = require('fs');
const path = require('path');

const SUPABASE_URL = 'fvayvetnlneekaqjkwjy.supabase.co';
const SERVICE_ROLE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw';

const sqlFilePath = path.join(__dirname, 'Database', 'migration_fix_subscription_code_status.sql');

function readAndParseSQLFile() {
  try {
    const sqlContent = fs.readFileSync(sqlFilePath, 'utf8');
    
    const statements = [];
    let currentStatement = '';
    let inFunction = false;
    let dollarQuoteCount = 0;
    
    const lines = sqlContent.split('\n');
    
    for (const line of lines) {
      const trimmedLine = line.trim();
      
      // Skip empty lines and comments
      if (!trimmedLine || trimmedLine.startsWith('--')) {
        if (currentStatement) {
          currentStatement += '\n' + line;
        }
        continue;
      }
      
      // Track dollar quotes for function definitions
      const dollarMatches = (line.match(/\$\$/g) || []);
      if (dollarMatches.length > 0) {
        dollarQuoteCount += dollarMatches.length;
        inFunction = dollarQuoteCount % 2 === 1;
      }
      
      currentStatement += (currentStatement ? '\n' : '') + line;
      
      // Check if statement ends (semicolon outside of function)
      if (trimmedLine.endsWith(';') && !inFunction) {
        if (currentStatement.trim()) {
          statements.push(currentStatement);
        }
        currentStatement = '';
      }
    }
    
    // Add any remaining statement
    if (currentStatement.trim()) {
      statements.push(currentStatement);
    }
    
    return statements.filter(stmt => stmt.trim().length > 0);
  } catch (error) {
    console.error(`❌ Failed to read SQL file: ${error.message}`);
    process.exit(1);
  }
}

function makeRequest(sql) {
  return new Promise((resolve, reject) => {
    const query = sql.trim();
    if (!query) {
      resolve({ statusCode: 204, body: '{}' });
      return;
    }

    const postData = JSON.stringify({ query });

    const options = {
      hostname: SUPABASE_URL,
      path: '/rest/v1/rpc/execute_sql',
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
  console.log('🚀 Starting Subscription Code Status Fix Migration...\n');
  
  const statements = readAndParseSQLFile();
  console.log(`📋 Found ${statements.length} SQL statements to execute\n`);
  
  let successCount = 0;
  let errorCount = 0;

  for (let i = 0; i < statements.length; i++) {
    const stepNum = i + 1;
    const sql = statements[i];
    
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
          if (result.body) {
            const responseBody = JSON.parse(result.body);
            console.log(`📢 Error: ${responseBody.message || JSON.stringify(responseBody)}`);
          }
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
    console.log('\n📋 Alternatively, apply the migration manually in Supabase SQL Editor:');
    console.log(`    File: ${sqlFilePath}`);
  }
}

executeMigrations().catch(console.error);
