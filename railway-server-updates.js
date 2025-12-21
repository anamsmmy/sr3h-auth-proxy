// Updated Railway Proxy Server - server.js additions
// This file contains the additional endpoints to add to the existing server.js

// ============================================================================
// EXISTING ENDPOINTS (should already be in your server.js):
// ============================================================================
// POST /verify - calls verify_authentication RPC
// POST /verify-periodic - calls verify_authentication RPC
// POST /activate - calls authenticate_user RPC

// ============================================================================
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
