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

// Rate Limiting مشدد – 5 طلبات / 15 دقيقة
const authLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 5,
  message: 'عدد طلبات التحقق كبير، جرب لاحقاً',
  skipSuccessfulRequests: true,
  standardHeaders: true,
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

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/verify_authentication`,
      {
        user_email: email,
        user_hardware_id: hardware_id,
        verification_ip: req.ip
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`✅ تحقق ناجح لـ ${email} من IP: ${req.ip}`);
    res.json(response.data);
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

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/verify_authentication`,
      {
        user_email: email,
        user_hardware_id: hardware_id,
        verification_ip: req.ip
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`✔ تحقق دوري لـ ${email}`);
    res.json(response.data);
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

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/authenticate_user`,
      {
        user_email: email,
        user_hardware_id: hardware_id
      },
      {
        headers: {
          Authorization: `Bearer ${SUPABASE_KEY}`,
          apikey: SUPABASE_KEY,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log(`🔑 تفعيل جديد لـ ${email} على جهاز: ${hardware_id}`);
    res.json(response.data);
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

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/validate_subscription_code`,
      {
        p_code: code,
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

    console.log(`✅ تحقق من الكود: ${code} لـ ${email}`);
    res.json(response.data);
  } catch (error) {
    console.error('❌ خطأ في التحقق من الكود:', error.message);
    res.status(500).json({
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

    const response = await axios.post(
      `${SUPABASE_URL}/rest/v1/rpc/redeem_subscription_code`,
      {
        p_code: code,
        p_email: email,
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

    console.log(`✅ استرجاع الكود: ${code} لـ ${email}`);
    res.json(response.data);
  } catch (error) {
    console.error('❌ خطأ في استرجاع الكود:', error.message);
    res.status(500).json({
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
    console.error('❌ خطأ في توليد OTP:', error.message);
    res.status(500).json({
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
    console.error('❌ خطأ في التحقق من OTP:', error.message);
    res.status(500).json({
      success: false,
      message: 'خطأ في التحقق من OTP'
    });
  }
});

// POST /initiate-device-transfer - بدء عملية نقل الجهاز
app.post('/initiate-device-transfer', authLimiter, async (req, res) => {
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
        }
      }
    );

    console.log(`✅ بدء نقل الجهاز لـ ${email}`);
    res.json(response.data);
  } catch (error) {
    console.error('❌ خطأ في بدء نقل الجهاز:', error.message);
    res.status(500).json({
      success: false,
      message: 'خطأ في بدء نقل الجهاز'
    });
  }
});

// POST /complete-device-transfer - إكمال عملية نقل الجهاز
app.post('/complete-device-transfer', authLimiter, async (req, res) => {
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
    console.error('❌ خطأ في إكمال نقل الجهاز:', error.message);
    res.status(500).json({
      success: false,
      message: 'خطأ في إكمال نقل الجهاز'
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
// NEW ENDPOINTS TO ADD:
// ============================================================================

// ============================================================================
// 1. POST /validate-code - Validate subscription code without redeeming
// ============================================================================
app.post('/validate-code', async (req, res) => {
  try {
    const { code, email, hardware_id } = req.body;

    if (!code || !email || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: code أو email أو hardware_id'
      });
    }

    const { data, error } = await supabase.rpc('validate_subscription_code', {
      p_code: code,
      p_email: email
    });

    if (error) {
      console.error('RPC Error:', error);
      return res.status(500).json({
        success: false,
        message: error.message
      });
    }

    res.json(data);
  } catch (err) {
    console.error('Error:', err);
    res.status(500).json({
      success: false,
      message: err.message
    });
  }
});

// ============================================================================
// 2. POST /redeem-code - Redeem/use a subscription code
// ============================================================================
app.post('/redeem-code', async (req, res) => {
  try {
    const { code, email, hardware_id } = req.body;

    if (!code || !email || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: code أو email أو hardware_id'
      });
    }

    const { data, error } = await supabase.rpc('redeem_subscription_code', {
      p_code: code,
      p_email: email,
      p_hardware_id: hardware_id
    });

    if (error) {
      console.error('RPC Error:', error);
      return res.status(500).json({
        success: false,
        message: error.message
      });
    }

    res.json(data);
  } catch (err) {
    console.error('Error:', err);
    res.status(500).json({
      success: false,
      message: err.message
    });
  }
});

// ============================================================================
// 3. POST /generate-otp - Generate OTP for email verification
// ============================================================================
app.post('/generate-otp', async (req, res) => {
  try {
    const { email } = req.body;

    if (!email) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: email'
      });
    }

    const { data, error } = await supabase.rpc('generate_otp', {
      p_email: email
    });

    if (error) {
      console.error('RPC Error:', error);
      return res.status(500).json({
        success: false,
        message: error.message
      });
    }

    res.json(data);
  } catch (err) {
    console.error('Error:', err);
    res.status(500).json({
      success: false,
      message: err.message
    });
  }
});

// ============================================================================
// 4. POST /verify-otp - Verify OTP code for email verification
// ============================================================================
app.post('/verify-otp', async (req, res) => {
  try {
    const { email, otp_code, hardware_id } = req.body;

    if (!email || !otp_code || !hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: email أو otp_code أو hardware_id'
      });
    }

    const { data, error } = await supabase.rpc('verify_otp', {
      p_email: email,
      p_otp_code: otp_code,
      p_hardware_id: hardware_id
    });

    if (error) {
      console.error('RPC Error:', error);
      return res.status(500).json({
        success: false,
        message: error.message
      });
    }

    res.json(data);
  } catch (err) {
    console.error('Error:', err);
    res.status(500).json({
      success: false,
      message: err.message
    });
  }
});

// ============================================================================
// 5. POST /initiate-device-transfer - Start device transfer process
// ============================================================================
app.post('/initiate-device-transfer', async (req, res) => {
  try {
    const { email, current_hardware_id } = req.body;

    if (!email || !current_hardware_id) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: email أو current_hardware_id'
      });
    }

    const { data, error } = await supabase.rpc('initiate_device_transfer', {
      p_email: email,
      p_current_hardware_id: current_hardware_id
    });

    if (error) {
      console.error('RPC Error:', error);
      return res.status(500).json({
        success: false,
        message: error.message
      });
    }

    res.json(data);
  } catch (err) {
    console.error('Error:', err);
    res.status(500).json({
      success: false,
      message: err.message
    });
  }
});

// ============================================================================
// 6. POST /complete-device-transfer - Complete device transfer
// ============================================================================
app.post('/complete-device-transfer', async (req, res) => {
  try {
    const { email, new_hardware_id, transfer_token } = req.body;

    if (!email || !new_hardware_id || !transfer_token) {
      return res.status(400).json({
        success: false,
        message: 'مفقود: email أو new_hardware_id أو transfer_token'
      });
    }

    const { data, error } = await supabase.rpc('complete_device_transfer', {
      p_email: email,
      p_new_hardware_id: new_hardware_id,
      p_transfer_token: transfer_token
    });

    if (error) {
      console.error('RPC Error:', error);
      return res.status(500).json({
        success: false,
        message: error.message
      });
    }

    res.json(data);
  } catch (err) {
    console.error('Error:', err);
    res.status(500).json({
      success: false,
      message: err.message
    });
  }
});

// ============================================================================
// HEALTH CHECK ENDPOINT (already exists)
// ============================================================================
// GET / - Health check

// ============================================================================
// ENDPOINT SUMMARY
// ============================================================================
/*
Total Endpoints:
- GET  / (existing)
- POST /verify (existing)
- POST /verify-periodic (existing)
- POST /activate (existing)
- POST /validate-code (NEW)
- POST /redeem-code (NEW)
- POST /generate-otp (NEW)
- POST /verify-otp (NEW)
- POST /initiate-device-transfer (NEW)
- POST /complete-device-transfer (NEW)

All endpoints handle errors gracefully and return JSON responses.
*/

// ============================================================================
// IMPORTANT NOTES FOR DEPLOYMENT
// ============================================================================
/*
1. Add these endpoints AFTER existing endpoints in server.js
2. Ensure Supabase client is initialized at the top of server.js
3. All RPC functions should already exist from SQL migrations
4. Test each endpoint using Postman or curl before deploying

Example Postman requests:

POST /validate-code
{
  "code": "TESTCODE123",
  "email": "user@example.com",
  "hardware_id": "hw-id-123"
}

POST /redeem-code
{
  "code": "TESTCODE123",
  "email": "user@example.com",
  "hardware_id": "hw-id-123"
}

POST /generate-otp
{
  "email": "user@example.com"
}

POST /verify-otp
{
  "email": "user@example.com",
  "otp_code": "123456",
  "hardware_id": "hw-id-123"
}

POST /initiate-device-transfer
{
  "email": "user@example.com",
  "current_hardware_id": "hw-id-old"
}

POST /complete-device-transfer
{
  "email": "user@example.com",
  "new_hardware_id": "hw-id-new",
  "transfer_token": "token-from-initiate"
}
*/

app.listen(PORT, () => {
  console.log(`🚀 SR3H Authentication Proxy يعمل على PORT: ${PORT}`);
  console.log(`Environment: ${process.env.NODE_ENV || 'development'}`);
});

module.exports = app;
