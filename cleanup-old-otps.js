const https = require('https');

const SUPABASE_URL = 'fvayvetnlneekaqjkwjy.supabase.co';
const SERVICE_ROLE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MzQ1NTkxNywiZXhwIjoyMDY5MDMxOTE3fQ.nsJXBzMNAWkw7Rd2H389p71aDYlo_7OsD0gcw3w6UFw';
const EMAIL = 'msmmy1@gmail.com';

function deleteOldOtps() {
  return new Promise((resolve, reject) => {
    const encodedEmail = encodeURIComponent(EMAIL);
    const cutoffTime = new Date(Date.now() - 60 * 60 * 1000).toISOString();
    const encodedCutoff = encodeURIComponent(cutoffTime);
    
    const path = `/rest/v1/macro_fort_verification_codes?email=eq.${encodedEmail}&created_at=lt.${encodedCutoff}`;
    
    console.log(`🧹 حذف السجلات القديمة قبل: ${cutoffTime}`);
    console.log(`📧 البريد: ${EMAIL}`);
    
    const options = {
      hostname: SUPABASE_URL,
      path: path,
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${SERVICE_ROLE_KEY}`,
        'apikey': SERVICE_ROLE_KEY,
        'Content-Type': 'application/json',
        'Prefer': 'return=representation'
      }
    };

    const req = https.request(options, (res) => {
      let data = '';

      res.on('data', (chunk) => {
        data += chunk;
      });

      res.on('end', () => {
        if (res.statusCode === 200 || res.statusCode === 204) {
          console.log(`✅ تم حذف السجلات بنجاح`);
          console.log(`📊 رمز الاستجابة: ${res.statusCode}`);
          if (data) {
            const deleted = JSON.parse(data);
            console.log(`🗑️ عدد الصفوف المحذوفة: ${deleted.length}`);
          }
          resolve(true);
        } else {
          console.log(`❌ فشل الحذف - الرمز: ${res.statusCode}`);
          console.log(`📋 الرد: ${data}`);
          reject(new Error(`HTTP ${res.statusCode}`));
        }
      });
    });

    req.on('error', (error) => {
      console.error(`❌ خطأ في الطلب: ${error.message}`);
      reject(error);
    });

    req.end();
  });
}

deleteOldOtps()
  .then(() => {
    console.log('\n✨ اكتمل التنظيف بنجاح!');
    process.exit(0);
  })
  .catch((error) => {
    console.error('\n❌ فشل التنظيف:', error.message);
    process.exit(1);
  });
