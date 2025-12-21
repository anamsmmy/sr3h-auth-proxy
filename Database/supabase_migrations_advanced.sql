-- Migration: Enhanced subscription management with advanced verification

-- ============================================================================
-- Update verify_authentication RPC to use macro_fort_subscriptions
-- ============================================================================

CREATE OR REPLACE FUNCTION verify_authentication(
  p_email TEXT,
  p_hardware_id TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_subscription RECORD;
  v_result JSONB;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  SELECT * INTO v_subscription
  FROM macro_fort_subscriptions
  WHERE email = p_email AND is_active = true
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'لم يتم العثور على اشتراك نشط',
      'subscription_type', NULL,
      'is_active', FALSE,
      'subscription_expired', FALSE,
      'email_verified', FALSE
    );
  END IF;

  -- Update verification tracking
  UPDATE macro_fort_subscriptions
  SET
    verification_count = verification_count + 1,
    last_verified_timestamp = v_current_timestamp,
    last_verification_ip = COALESCE(current_setting('app.client_ip', true), '')
  WHERE email = p_email;

  -- Check if subscription has expired
  IF v_subscription.expiry_date IS NOT NULL AND v_subscription.expiry_date < v_current_timestamp THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'انتهت صلاحية الاشتراك',
      'subscription_type', v_subscription.subscription_type,
      'expiry_date', v_subscription.expiry_date,
      'is_active', FALSE,
      'subscription_expired', TRUE,
      'email_verified', v_subscription.email_verified,
      'device_count', 1,
      'max_devices', 10,
      'is_trial', FALSE
    );
  END IF;

  -- Subscription is valid
  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم التحقق بنجاح',
    'subscription_type', v_subscription.subscription_type,
    'expiry_date', v_subscription.expiry_date,
    'is_active', TRUE,
    'subscription_expired', FALSE,
    'email_verified', v_subscription.email_verified,
    'device_count', 1,
    'max_devices', 3,
    'is_trial', FALSE
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- Update authenticate_user RPC to handle subscription code activation
-- ============================================================================

CREATE OR REPLACE FUNCTION authenticate_user(
  p_email TEXT,
  p_subscription_code TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_code_record RECORD;
  v_subscription RECORD;
  v_duration_days INT;
  v_expiry_date TIMESTAMP WITH TIME ZONE;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  -- Verify subscription code format and existence
  SELECT * INTO v_code_record
  FROM macro_fort_subscription_codes
  WHERE code = p_subscription_code
    AND email IS NULL
    AND used_date IS NULL
    AND (expiry_date IS NULL OR expiry_date > v_current_timestamp)
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'كود غير صحيح أو منتهي الصلاحية'
    );
  END IF;

  -- Check if user already has a subscription
  SELECT * INTO v_subscription
  FROM macro_fort_subscriptions
  WHERE email = p_email
  LIMIT 1;

  -- Calculate expiry date based on code duration
  v_duration_days := COALESCE(v_code_record.duration_days, 30);
  v_expiry_date := v_current_timestamp + (v_duration_days || ' days')::INTERVAL;

  IF FOUND THEN
    -- Update existing subscription (extend it)
    UPDATE macro_fort_subscriptions
    SET
      subscription_type = v_code_record.subscription_type,
      expiry_date = GREATEST(expiry_date, v_expiry_date),
      is_active = TRUE,
      updated_at = v_current_timestamp
    WHERE email = p_email;
  ELSE
    -- Create new subscription
    INSERT INTO macro_fort_subscriptions (
      email,
      subscription_type,
      expiry_date,
      is_active,
      email_verified,
      created_at,
      updated_at
    ) VALUES (
      p_email,
      v_code_record.subscription_type,
      v_expiry_date,
      TRUE,
      TRUE,
      v_current_timestamp,
      v_current_timestamp
    );
  END IF;

  -- Mark the code as used
  UPDATE macro_fort_subscription_codes
  SET used_date = v_current_timestamp
  WHERE code = p_subscription_code;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم تفعيل الاشتراك بنجاح',
    'subscription_type', v_code_record.subscription_type,
    'expiry_date', v_expiry_date,
    'is_active', TRUE
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- New RPC: Validate subscription code (without using it)
-- ============================================================================

CREATE OR REPLACE FUNCTION validate_subscription_code(
  p_code TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_code_record RECORD;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  SELECT * INTO v_code_record
  FROM macro_fort_subscription_codes
  WHERE code = p_code
    AND email IS NULL
    AND used_date IS NULL
    AND (expiry_date IS NULL OR expiry_date > v_current_timestamp)
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'كود غير صحيح أو منتهي الصلاحية'
    );
  END IF;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'كود صحيح',
    'subscription_type', v_code_record.subscription_type,
    'expiry_date', v_code_record.expiry_date
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- New RPC: Redeem subscription code
-- ============================================================================

CREATE OR REPLACE FUNCTION redeem_subscription_code(
  p_code TEXT,
  p_email TEXT,
  p_hardware_id TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_code_record RECORD;
  v_subscription RECORD;
  v_expiry_date TIMESTAMP WITH TIME ZONE;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  -- Check if code exists and is valid
  SELECT * INTO v_code_record
  FROM macro_fort_subscription_codes
  WHERE code = p_code
    AND email IS NULL
    AND used_date IS NULL
    AND (expiry_date IS NULL OR expiry_date > v_current_timestamp)
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'كود غير صحيح أو منتهي الصلاحية'
    );
  END IF;

  -- Check for existing subscription
  SELECT * INTO v_subscription
  FROM macro_fort_subscriptions
  WHERE email = p_email
  LIMIT 1;

  v_expiry_date := COALESCE(v_code_record.expiry_date, v_current_timestamp + INTERVAL '30 days');

  IF FOUND THEN
    UPDATE macro_fort_subscriptions
    SET
      subscription_type = v_code_record.subscription_type,
      expiry_date = GREATEST(expiry_date, v_expiry_date),
      is_active = TRUE,
      updated_at = v_current_timestamp
    WHERE email = p_email;
  ELSE
    INSERT INTO macro_fort_subscriptions (
      email,
      subscription_type,
      expiry_date,
      is_active,
      email_verified,
      created_at,
      updated_at
    ) VALUES (
      p_email,
      v_code_record.subscription_type,
      v_expiry_date,
      TRUE,
      TRUE,
      v_current_timestamp,
      v_current_timestamp
    );
  END IF;

  -- Mark code as used
  UPDATE macro_fort_subscription_codes
  SET used_date = v_current_timestamp
  WHERE code = p_code;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم استرجاع الكود بنجاح',
    'subscription_type', v_code_record.subscription_type,
    'expiry_date', v_expiry_date
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- New RPC: Generate OTP for email verification
-- ============================================================================

CREATE OR REPLACE FUNCTION generate_otp(
  p_email TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_otp_code TEXT;
  v_expiry TIMESTAMP WITH TIME ZONE;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());
  v_otp_code := LPAD((RANDOM() * 999999)::INT::TEXT, 6, '0');
  v_expiry := v_current_timestamp + INTERVAL '10 minutes';

  -- Store OTP in verification codes table
  INSERT INTO macro_fort_verification_codes (
    email,
    code,
    code_type,
    expiry_date,
    created_at
  ) VALUES (
    p_email,
    v_otp_code,
    'OTP',
    v_expiry,
    v_current_timestamp
  );

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم توليد OTP',
    'otp_code', v_otp_code,
    'expires_in_seconds', 600
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- New RPC: Verify OTP
-- ============================================================================

CREATE OR REPLACE FUNCTION verify_otp(
  p_email TEXT,
  p_otp_code TEXT,
  p_hardware_id TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_code_record RECORD;
  v_subscription RECORD;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  -- Check if OTP is valid
  SELECT * INTO v_code_record
  FROM macro_fort_verification_codes
  WHERE email = p_email
    AND code = p_otp_code
    AND code_type = 'OTP'
    AND used_date IS NULL
    AND expiry_date > v_current_timestamp
  ORDER BY created_at DESC
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'رمز OTP غير صحيح أو منتهي الصلاحية'
    );
  END IF;

  -- Mark OTP as used
  UPDATE macro_fort_verification_codes
  SET used_date = v_current_timestamp
  WHERE email = p_email AND code = p_otp_code;

  -- Update subscription email verification status
  UPDATE macro_fort_subscriptions
  SET
    email_verified = TRUE,
    updated_at = v_current_timestamp
  WHERE email = p_email;

  -- Get subscription info
  SELECT * INTO v_subscription
  FROM macro_fort_subscriptions
  WHERE email = p_email
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'لم يتم العثور على اشتراك'
    );
  END IF;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم التحقق من البريد الإلكتروني',
    'subscription_type', v_subscription.subscription_type,
    'expiry_date', v_subscription.expiry_date,
    'is_active', v_subscription.is_active
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- New RPC: Initiate device transfer
-- ============================================================================

CREATE OR REPLACE FUNCTION initiate_device_transfer(
  p_email TEXT,
  p_current_hardware_id TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_transfer_token TEXT;
  v_expiry TIMESTAMP WITH TIME ZONE;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());
  v_transfer_token := MD5(p_email || p_current_hardware_id || v_current_timestamp::TEXT);
  v_expiry := v_current_timestamp + INTERVAL '1 hour';

  -- Store transfer token
  INSERT INTO macro_fort_verification_codes (
    email,
    code,
    code_type,
    expiry_date,
    created_at
  ) VALUES (
    p_email,
    v_transfer_token,
    'TRANSFER',
    v_expiry,
    v_current_timestamp
  );

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم بدء عملية نقل الجهاز',
    'transfer_token', v_transfer_token,
    'expires_in_seconds', 3600
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- New RPC: Complete device transfer
-- ============================================================================

CREATE OR REPLACE FUNCTION complete_device_transfer(
  p_email TEXT,
  p_new_hardware_id TEXT,
  p_transfer_token TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_token_record RECORD;
  v_subscription RECORD;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  -- Verify transfer token
  SELECT * INTO v_token_record
  FROM macro_fort_verification_codes
  WHERE email = p_email
    AND code = p_transfer_token
    AND code_type = 'TRANSFER'
    AND used_date IS NULL
    AND expiry_date > v_current_timestamp
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'رمز نقل غير صحيح أو منتهي الصلاحية'
    );
  END IF;

  -- Mark token as used
  UPDATE macro_fort_verification_codes
  SET used_date = v_current_timestamp
  WHERE email = p_email AND code = p_transfer_token;

  -- Update subscription
  UPDATE macro_fort_subscriptions
  SET
    device_transfer_count = COALESCE(device_transfer_count, 0) + 1,
    updated_at = v_current_timestamp
  WHERE email = p_email;

  -- Get subscription info
  SELECT * INTO v_subscription
  FROM macro_fort_subscriptions
  WHERE email = p_email
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'لم يتم العثور على اشتراك'
    );
  END IF;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم نقل الجهاز بنجاح',
    'subscription_type', v_subscription.subscription_type,
    'expiry_date', v_subscription.expiry_date,
    'device_count', COALESCE(v_subscription.device_transfer_count, 0) + 1
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- Grant execute permissions to service role
-- ============================================================================
-- activate_subscription - Activate/bind a device to user subscription
-- ============================================================================

CREATE OR REPLACE FUNCTION activate_subscription(
  p_email TEXT,
  p_hardware_id TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_subscription RECORD;
  v_current_timestamp TIMESTAMP WITH TIME ZONE;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  SELECT * INTO v_subscription
  FROM macro_fort_subscriptions
  WHERE email = p_email
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'لم يتم العثور على اشتراك لهذا البريد الإلكتروني'
    );
  END IF;

  UPDATE macro_fort_subscriptions
  SET
    hardware_id = p_hardware_id,
    is_active = TRUE,
    updated_at = v_current_timestamp
  WHERE email = p_email;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم ربط الجهاز بنجاح',
    'subscription_type', v_subscription.subscription_type,
    'is_active', TRUE,
    'expiry_date', v_subscription.expiry_date,
    'hardware_id', p_hardware_id
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================

GRANT EXECUTE ON FUNCTION verify_authentication(TEXT, TEXT) TO postgres, anon, authenticated, service_role;
GRANT EXECUTE ON FUNCTION authenticate_user(TEXT, TEXT) TO postgres, anon, authenticated, service_role;
GRANT EXECUTE ON FUNCTION validate_subscription_code(TEXT) TO postgres, anon, authenticated, service_role;
GRANT EXECUTE ON FUNCTION redeem_subscription_code(TEXT, TEXT, TEXT) TO postgres, anon, authenticated, service_role;
GRANT EXECUTE ON FUNCTION generate_otp(TEXT) TO postgres, anon, authenticated, service_role;
GRANT EXECUTE ON FUNCTION verify_otp(TEXT, TEXT, TEXT) TO postgres, anon, authenticated, service_role;
GRANT EXECUTE ON FUNCTION initiate_device_transfer(TEXT, TEXT) TO postgres, anon, authenticated, service_role;
GRANT EXECUTE ON FUNCTION complete_device_transfer(TEXT, TEXT, TEXT) TO postgres, anon, authenticated, service_role;
GRANT EXECUTE ON FUNCTION activate_subscription(TEXT, TEXT) TO postgres, anon, authenticated, service_role;
