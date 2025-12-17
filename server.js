require('dotenv').config();
const express = require('express');
const axios = require('axios');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const morgan = require('morgan');

const app = express();

// ✅ مهم جداً على Railway / أي Proxy
// يسمح لـ express-rate-limit بالتعامل الصحيح مع X-Forwarded-For
app.set('trust proxy', 1);

// Security Middleware
app.use(helmet());
app.use(morgan('combined'));
app.use(express.json());

// CORS
app.use(
  cors({
    origin: process.env.ALLOWED_ORIGINS?.split(',') || '*',
    credentials: true
  })
);

// Rate Limiting عام – 30 طلب / 15 دقيقة
const limiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 30,
  message: 'عدد الطلبات كثير جداً، جرب لاحقاً',
  standardHeaders: true,
  legacyHeaders: false
});

// Rate Limiting مشدد – 20 طلبات / 15 دقيقة
const authLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 20,
  message: 'عدد طلبات التحقق كبير، جرب لاحقاً',
  skip: (req, res) => false,
  handler: (req, res) => {
    res.status(429).json({
      success: false,
      message: 'عدد طلبات التحقق كبير، جرب لاحقاً'
    });
  },
  standardHeaders: false,
  legacyHeaders: false
});

// تطبيق الـ limiter العام
app.use(limiter);

// Supabase
const SUPABASE_URL = process.env.SUPABASE_URL;
const SUPABASE_KEY = process.env.SUPABASE_SERVICE_ROLE_KEY;
const DATABASE_NAME = process.env.DATABASE_NAME || 'sr3h-users-auth';

if (!SUPABASE_URL || !SUPABASE_KEY) {
  console.error('❌ خطأ: SUPABASE_URL أو SUPABASE_SERVICE_ROLE_KEY مفقودة');
  process.exit(1);
}

// Health Check
app.get('/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// API Info
app.get('/', (req, res) => {
  res.json({
    service: 'SR3H Macro - Authentication Proxy',
    version: '2.0.0',
    endpoints: {
      '/health': 'GET - Health check',
      '/verify': 'POST - تحقق من الترخيص',
      '/verify-periodic': 'POST - تحقق دوري',
      '/activate': 'POST - تفعيل / ربط جهاز',
      '/validate-code': 'POST - التحقق من كود الاشتراك',
      '/redeem-code': 'POST - استهلاك كود الاشتراك',
      '/generate-otp': 'POST - توليد رمز البريد',
      '/verify-otp': 'POST - التحقق من رمز البريد',
      '/initiate-device-transfer': 'POST - بدء نقل الجهاز',
      '/complete-device-transfer': 'POST - إكمال نقل الجهاز'
    }
  });
});

// POST /verify
app.post('/verify', authLimiter, async (req, res) => {
  try {
    const { email, hardware_id } = req.body;

    if (!email || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مطلوبان البريد الإلكتروني و hardware_id'
      });
    }

    const response = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions?email=eq.${encodeURIComponent(email)}&hardware_id=eq.${encodeURIComponent(hardware_id)}&select=*`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    if (response.data && response.data.length > 0) {
      const subscription = response.data[0];
      console.log(`✅ تحقق ناجح لـ ${email} من IP: ${req.ip}`);
      return res.json({
        success: true,
        subscription_type: subscription.subscription_type,
        status: subscription.status,
        expiry_date: subscription.expiry_date,
        activated_date: subscription.activated_date,
        trial_days: subscription.trial_days
      });
    }

    return res.json({
      success: false,
      message: 'لم يتم العثور على اشتراك نشط'
    });
  } catch (error) {
    console.error('❌ خطأ في التحقق:', error.response?.data || error.message);
    res.status(500).json({
      success: false,
      message: 'خطأ من خادم التحقق'
    });
  }
});

// POST /verify-periodic
app.post('/verify-periodic', authLimiter, async (req, res) => {
  try {
    const { email, hardware_id } = req.body;

    if (!email || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'البيانات غير كاملة'
      });
    }

    const response = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions?email=eq.${encodeURIComponent(email)}&hardware_id=eq.${encodeURIComponent(hardware_id)}&select=*`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    if (response.data && response.data.length > 0) {
      const subscription = response.data[0];
      console.log(`✔ تحقق دوري لـ ${email}`);
      return res.json({
        success: true,
        subscription_type: subscription.subscription_type,
        status: subscription.status,
        expiry_date: subscription.expiry_date
      });
    }

    return res.json({
      success: false,
      message: 'لم يتم العثور على اشتراك'
    });
  } catch (error) {
    console.error('❌ خطأ في التحقق الدوري:', error.response?.data || error.message);
    res.status(500).json({
      success: false,
      message: 'فشل التحقق الدوري'
    });
  }
});

// POST /activate
app.post('/activate', authLimiter, async (req, res) => {
  try {
    const { email, hardware_id } = req.body;

    if (!email || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'البيانات غير كاملة'
      });
    }

    const checkResponse = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions?email=eq.${encodeURIComponent(email)}&select=id`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    if (checkResponse.data && checkResponse.data.length > 0) {
      await axios.patch(
        `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions?email=eq.${encodeURIComponent(email)}`,
        {
          hardware_id: hardware_id,
          activated_date: new Date().toISOString(),
          status: 'active'
        },
        {
          headers: {
            Authorization: `Bearer ${SUPABASE_KEY}`,
            apikey: SUPABASE_KEY,
            'Content-Type': 'application/json'
          }
        }
      );
    } else {
      await axios.post(
        `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions`,
        {
          email: email,
          hardware_id: hardware_id,
          activated_date: new Date().toISOString(),
          status: 'active',
          subscription_type: 'trial',
          trial_days: 0
        },
        {
          headers: {
            Authorization: `Bearer ${SUPABASE_KEY}`,
            apikey: SUPABASE_KEY,
            'Content-Type': 'application/json'
          }
        }
      );
    }

    console.log(`🔑 تفعيل جديد لـ ${email} على جهاز: ${hardware_id}`);
    res.json({
      success: true,
      message: 'تم التفعيل بنجاح'
    });
  } catch (error) {
    console.error('❌ خطأ في التفعيل:', error.response?.data || error.message);
    res.status(500).json({
      success: false,
      message: 'فشل التفعيل'
    });
  }
});

// POST /validate-code - التحقق من كود الاشتراك (بدون استهلاكه)
app.post('/validate-code', authLimiter, async (req, res) => {
  try {
    const { code, email, hardware_id } = req.body;

    if (!code || !email || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: code أو email أو hardware_id'
      });
    }

    const response = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?code=eq.${encodeURIComponent(code)}&select=*`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    if (response.data && response.data.length > 0) {
      const codeRecord = response.data[0];
      
      if (codeRecord.status === 'used') {
        return res.status(400).json({
          success: false,
          message: 'الكود مستخدم بالفعل'
        });
      }

      if (codeRecord.expiry_date && new Date(codeRecord.expiry_date) < new Date()) {
        return res.status(400).json({
          success: false,
          message: 'الكود منتهي الصلاحية'
        });
      }

      console.log(`✅ تحقق من الكود: ${code} لـ ${email}`);
      return res.json({
        success: true,
        message: 'الكود صحيح',
        subscription_type: codeRecord.subscription_type,
        duration_days: codeRecord.duration_days
      });
    }

    return res.status(404).json({
      success: false,
      message: 'الكود غير موجود'
    });
  } catch (error) {
    console.error('❌ Validation error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في التحقق من الكود'
    });
  }
});

// POST /redeem-code - استهلاك كود الاشتراك
app.post('/redeem-code', authLimiter, async (req, res) => {
  try {
    const { code, email, hardware_id } = req.body;

    if (!code || !email || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: code أو email أو hardware_id'
      });
    }

    const checkResponse = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?code=eq.${encodeURIComponent(code)}&select=*`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    if (!checkResponse.data || checkResponse.data.length === 0) {
      return res.status(404).json({
        success: false,
        message: 'الكود غير موجود'
      });
    }

    const codeRecord = checkResponse.data[0];

    if (codeRecord.status === 'used') {
      return res.status(400).json({
        success: false,
        message: 'الكود مستخدم بالفعل'
      });
    }

    if (codeRecord.expiry_date && new Date(codeRecord.expiry_date) < new Date()) {
      return res.status(400).json({
        success: false,
        message: 'الكود منتهي الصلاحية'
      });
    }

    const expiryDate = new Date();
    expiryDate.setDate(expiryDate.getDate() + (codeRecord.duration_days || 0));

    await axios.patch(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?code=eq.${encodeURIComponent(code)}`,
      {
        status: 'used',
        email: email,
        hardware_id: hardware_id,
        used_date: new Date().toISOString()
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    const checkSubResponse = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions?email=eq.${encodeURIComponent(email)}&select=id,subscription_code`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    if (checkSubResponse.data && checkSubResponse.data.length > 0) {
      const subId = checkSubResponse.data[0].id;
      await axios.patch(
        `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions?id=eq.${subId}`,
        { subscription_code: null },
        {
          headers: {
            Authorization: `Bearer ${SUPABASE_KEY}`,
            apikey: SUPABASE_KEY,
            'Content-Type': 'application/json'
          }
        }
      );

      await axios.patch(
        `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions?email=eq.${encodeURIComponent(email)}`,
        {
          subscription_type: codeRecord.subscription_type,
          hardware_id: hardware_id,
          subscription_code: code,
          expiry_date: expiryDate.toISOString(),
          status: 'active',
          activated_date: new Date().toISOString()
        },
        {
          headers: {
            Authorization: `Bearer ${SUPABASE_KEY}`,
            apikey: SUPABASE_KEY,
            'Content-Type': 'application/json'
          }
        }
      );
    } else {
      await axios.post(
        `${SUPABASE_URL}/rest/v1/macro_fort_subscriptions`,
        {
          email: email,
          hardware_id: hardware_id,
          subscription_code: code,
          subscription_type: codeRecord.subscription_type,
          status: 'active',
          expiry_date: expiryDate.toISOString(),
          activated_date: new Date().toISOString(),
          trial_days: 0
        },
        {
          headers: {
            Authorization: `Bearer ${SUPABASE_KEY}`,
            apikey: SUPABASE_KEY,
            'Content-Type': 'application/json'
          }
        }
      );
    }

    console.log(`✅ استرجاع الكود: ${code} لـ ${email}`);
    res.json({
      success: true,
      message: 'تم استرجاع الكود بنجاح',
      subscription_type: codeRecord.subscription_type,
      expiry_date: expiryDate.toISOString()
    });
  } catch (error) {
    console.error('❌ Redeem error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في استرجاع الكود'
    });
  }
});

// POST /generate-otp - توليد رمز تحقق البريد الإلكتروني
app.post('/generate-otp', authLimiter, async (req, res) => {
  try {
    const { email } = req.body;

    if (!email) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: email'
      });
    }

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/generate_otp`,
      {
        p_email: email
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`✅ توليد OTP لـ ${email}`);
    res.json(response.data);
  } catch (error) {
    console.error('❌ OTP generation error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في توليد OTP'
    });
  }
});

// POST /verify-otp - التحقق من رمز البريد الإلكتروني
app.post('/verify-otp', authLimiter, async (req, res) => {
  try {
    const { email, otp_code, hardware_id } = req.body;

    if (!email || !otp_code || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: email أو otp_code أو hardware_id'
      });
    }

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/verify_otp`,
      {
        p_email: email,
        p_otp_code: otp_code,
        p_hardware_id: hardware_id
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`✅ تحقق من OTP لـ ${email}`);
    res.json(response.data);
  } catch (error) {
    console.error('❌ OTP verification error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في التحقق من OTP'
    });
  }
});

// POST /initiate-device-transfer - بدء عملية نقل الجهاز
app.post('/initiate-device-transfer', async (req, res) => {
  try {
    const { email, current_hardware_id } = req.body;

    if (!email || !current_hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: email أو current_hardware_id'
      });
    }

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/initiate_device_transfer`,
      {
        p_email: email,
        p_current_hardware_id: current_hardware_id
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        },
        timeout: 10000
      }
    );

    res.json(response.data);
  } catch (error) {
    console.error('❌ Device transfer initiation error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في بدء نقل الجهاز'
    });
  }
});

// POST /complete-device-transfer - إكمال عملية نقل الجهاز
app.post('/complete-device-transfer', async (req, res) => {
  try {
    const { email, new_hardware_id, transfer_token } = req.body;

    if (!email || !new_hardware_id || !transfer_token) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: email أو new_hardware_id أو transfer_token'
      });
    }

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/complete_device_transfer`,
      {
        p_email: email,
        p_new_hardware_id: new_hardware_id,
        p_transfer_token: transfer_token
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`✅ إكمال نقل الجهاز لـ ${email}`);
    res.json(response.data);
  } catch (error) {
    console.error('❌ Device transfer completion error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في إكمال نقل الجهاز'
    });
  }
});

// POST /check-code - التحقق من حالة الكود (جديد: يرجع status, email, hardware_id)
app.post('/check-code', authLimiter, async (req, res) => {
  try {
    const { code } = req.body;

    if (!code) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: code'
      });
    }

    const response = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?code=eq.${code}&select=*`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    if (response.data && response.data.length > 0) {
      const codeRecord = response.data[0];
      console.log(`✅ تحقق من الكود: ${code}`);
      return res.json({
        success: true,
        message: 'الكود موجود',
        subscription_type: codeRecord.subscription_type,
        status: codeRecord.status,
        email: codeRecord.email,
        hardware_id: codeRecord.hardware_id,
        expiry_date: codeRecord.expiry_date
      });
    }

    return res.json({
      success: false,
      message: 'الكود غير موجود'
    });
  } catch (error) {
    console.error('❌ Check code error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في التحقق من الكود'
    });
  }
});

// POST /bind-code - ربط الكود مع البريد والجهاز
app.post('/bind-code', authLimiter, async (req, res) => {
  try {
    const { code, email, hardware_id } = req.body;

    if (!code || !email || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: code أو email أو hardware_id'
      });
    }

    const updateData = {
      email: email,
      hardware_id: hardware_id
    };

    const response = await axios.patch(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?code=eq.${code}`,
      updateData,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`✅ ربط الكود: ${code} مع ${email}`);
    res.json({
      success: true,
      message: 'تم ربط الكود بنجاح'
    });
  } catch (error) {
    console.error('❌ Bind code error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في ربط الكود'
    });
  }
});

// POST /mark-code-used - تحديث حالة الكود إلى 'used'
app.post('/mark-code-used', authLimiter, async (req, res) => {
  try {
    const { code } = req.body;

    if (!code) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: code'
      });
    }

    const checkResponse = await axios.get(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?code=eq.${code}&select=code`,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY
        }
      }
    );

    if (!checkResponse.data || checkResponse.data.length === 0) {
      console.warn(`⚠️ الكود غير موجود: ${code}`);
      return res.status(404).json({
        success: false,
        message: 'الكود غير موجود'
      });
    }

    const updateResponse = await axios.patch(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?code=eq.${code}`,
      {
        status: 'used',
        used_date: new Date().toISOString()
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`✅ تحديد الكود كمستخدم: ${code}`);
    res.json({
      success: true,
      message: 'تم تحديث حالة الكود'
    });
  } catch (error) {
    console.error('❌ Mark code used error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في تحديث حالة الكود'
    });
  }
});

// POST /update-device-transfer - تحديث بيانات نقل الجهاز
app.post('/update-device-transfer', authLimiter, async (req, res) => {
  try {
    const { code, new_hardware_id } = req.body;

    if (!code || !new_hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: code أو new_hardware_id'
      });
    }

    const updateData = {
      hardware_id: new_hardware_id,
      device_transfer_count: 'device_transfer_count + 1',
      last_device_transfer_date: new Date().toISOString()
    };

    const response = await axios.patch(
      `${SUPABASE_URL}/rest/v1/macro_fort_subscription_codes?code=eq.${code}`,
      updateData,
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`✅ تحديث نقل الجهاز: ${code}`);
    res.json({
      success: true,
      message: 'تم تحديث بيانات نقل الجهاز'
    });
  } catch (error) {
    console.error('❌ Update device transfer error:', error.message);
    res.status(error.response?.status || 500).json({
      success: false,
      message: 'خطأ في تحديث بيانات نقل الجهاز'
    });
  }
});

// Generic proxy for /rest/* endpoints (Supabase REST API passthrough)
app.all('/rest/*', async (req, res) => {
  try {
    const path = req.path;
    const method = req.method;
    const query = req.url.includes('?') ? req.url.substring(req.url.indexOf('?')) : '';
    const fullUrl = `${SUPABASE_URL}${path}${query}`;
    
    const config = {
      method: method.toLowerCase(),
      url: fullUrl,
      headers: {
        Authorization: `Bearer ${SUPABASE_KEY}`,
        apikey: SUPABASE_KEY,
        'Content-Type': 'application/json'
      },
      validateStatus: () => true
    };

    if (['POST', 'PATCH', 'PUT'].includes(method)) {
      config.data = req.body;
    }

    console.log(`📡 Proxying ${method} ${path}`);
    const response = await axios(config);
    res.status(response.status).json(response.data);
  } catch (error) {
    console.error('❌ Proxy error:', error.message);
    res.status(500).json({
      success: false,
      message: 'Proxy error: ' + error.message
    });
  }
});

// 404
app.use((req, res) => {
  res.status(404).json({
    success: false,
    message: 'Endpoint غير موجود'
  });
});

// Error Handler
app.use((err, req, res, next) => {
  console.error('❌ خطأ عام في الخادم:', err.message);
  res.status(500).json({
    success: false,
    message: 'خطأ في الخادم'
  });
});

// Start Server
const PORT = process.env.PORT || 3000;

app.listen(PORT, () => {
  console.log(`🚀 SR3H Authentication Proxy يعمل على PORT: ${PORT}`);
  console.log(`Environment: ${process.env.NODE_ENV || 'development'}`);
});

module.exports = app;
