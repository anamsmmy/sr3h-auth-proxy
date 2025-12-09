require('dotenv').config();
const express = require('express');
const axios = require('axios');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const morgan = require('morgan');

const app = express();

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
    version: '1.0.0',
    endpoints: {
      '/health': 'GET - Health check',
      '/verify': 'POST - تحقق من الترخيص',
      '/verify-periodic': 'POST - تحقق دوري',
      '/activate': 'POST - تفعيل / ربط جهاز'
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
    console.error('❌ خطأ في التحقق:', error.message);
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
    console.error('❌ خطأ في التحقق الدوري:', error.message);
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
    console.error('❌ خطأ في التفعيل:', error.message);
    res.status(500).json({
      success: false,
      message: 'فشل التفعيل'
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
