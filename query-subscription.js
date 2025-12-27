const axios = require('axios');

const SUPABASE_URL = 'https://fvayvetnlneekaqjkwjy.supabase.co';
const SUPABASE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ2YXl2ZXRubG5lZWthcWprd2p5Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3MDI4MTkwMjksImV4cCI6MjAxODM5NTAyOX0.hW5iyRqwYIH8GZFVLPJVLEW4Aq5UBUz0v8OIjY7vIGE';

async function querySubscriptions() {
  try {
    console.log('📊 استعلام عن الاشتراكات للبريد: msmmy1@gmail.com\n');

    // Query macro_fort_subscriptions for the user
    const subResponse = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions?email=eq.msmmy1@gmail.com&select=*`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    console.log('✅ نتائج جدول macro_fort_subscriptions:');
    console.log(JSON.stringify(subResponse.data, null, 2));

    if (subResponse.data && subResponse.data.length > 0) {
      const subscription = subResponse.data[0];
      console.log('\n📋 تفاصيل الاشتراك:');
      console.log(`   ID: ${subscription.id}`);
      console.log(`   Email: ${subscription.email}`);
      console.log(`   Hardware ID: ${subscription.hardware_id}`);
      console.log(`   Subscription Code: ${subscription.subscription_code}`);
      console.log(`   Subscription Type: ${subscription.subscription_type}`);
      console.log(`   Status: ${subscription.is_active ? 'نشط' : 'غير نشط'}`);
      console.log(`   Max Devices: ${subscription.max_devices}`);
      console.log(`   Device Transfer Count: ${subscription.device_transfer_count}`);
      console.log(`   Expiry Date: ${subscription.expiry_date}`);
    } else {
      console.log('\n❌ لا توجد اشتراكات لهذا البريد الإلكتروني!');
    }

    // Also query the codes table
    console.log('\n\n📊 استعلام عن الأكواس للبريد: msmmy1@gmail.com\n');
    const codesResponse = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?email=eq.msmmy1@gmail.com&select=*`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    console.log('✅ نتائج جدول macro_fort_subscription_codes:');
    console.log(JSON.stringify(codesResponse.data, null, 2));

  } catch (error) {
    console.error('❌ خطأ في الاستعلام:', error.response?.data || error.message);
  }
}

querySubscriptions();
