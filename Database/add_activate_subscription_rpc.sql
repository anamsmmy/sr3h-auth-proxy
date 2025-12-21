-- Add activate_subscription RPC function
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

GRANT EXECUTE ON FUNCTION activate_subscription(TEXT, TEXT) TO postgres, anon, authenticated, service_role;
