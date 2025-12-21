const https = require('https');

const SUPABASE_URL = 'fvayvetnlneekaqjkwjy.supabase.co';
const SERVICE_ROLE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw';

const queries = [
  `ALTER TABLE macro_fort_subscription_codes ADD COLUMN IF NOT EXISTS duration_days INTEGER DEFAULT 30;`,
  `ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS last_device_transfer_date TIMESTAMP WITH TIME ZONE;`,
  `ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS device_transfers_30days INTEGER DEFAULT 0;`
];

async function executeQuery(query) {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: SUPABASE_URL,
      path: '/rest/v1/rpc/exec_sql',
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${SERVICE_ROLE_KEY}`,
        'apikey': SERVICE_ROLE_KEY,
        'Content-Type': 'application/json'
      }
    };

    const req = https.request(options, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        console.log(`Status: ${res.statusCode}`);
        console.log(`Response: ${data}\n`);
        resolve(data);
      });
    });

    req.on('error', reject);
    req.write(JSON.stringify({ sql: query }));
    req.end();
  });
}

async function runMigrations() {
  console.log('Starting migrations...\n');
  
  for (let i = 0; i < queries.length; i++) {
    console.log(`Executing query ${i + 1}/${queries.length}...`);
    try {
      await executeQuery(queries[i]);
    } catch (error) {
      console.error(`Error: ${error}`);
    }
  }
  
  console.log('Migrations completed!');
}

runMigrations();
