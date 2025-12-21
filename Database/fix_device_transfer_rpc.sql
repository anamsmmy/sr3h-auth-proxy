-- Fix initiate_device_transfer to use 'TRANSFER' instead of 'DEVICE_TRANSFER'
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

-- Fix complete_device_transfer to use 'TRANSFER' instead of 'DEVICE_TRANSFER'
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
      'message', 'رمز النقل غير صحيح أو انتهت صلاحيته'
    );
  END IF;

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

  UPDATE macro_fort_verification_codes
  SET used_date = v_current_timestamp
  WHERE id = v_token_record.id;

  UPDATE macro_fort_subscriptions
  SET
    hardware_id = p_new_hardware_id,
    is_active = TRUE,
    updated_at = v_current_timestamp
  WHERE email = p_email;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم نقل الجهاز بنجاح',
    'new_hardware_id', p_new_hardware_id
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
