-- ============================================================================
-- Migration: Add duration_days to codes table and device transfer limits
-- ============================================================================

-- ============================================================================
-- Step 1: Ensure duration_days exists in macro_fort_subscription_codes
-- ============================================================================

ALTER TABLE macro_fort_subscription_codes 
  ADD COLUMN IF NOT EXISTS duration_days INTEGER DEFAULT 30;

COMMENT ON COLUMN macro_fort_subscription_codes.duration_days IS 
  'Number of days subscription extends when this code is redeemed (30/180/365 for MONTH/SEMI/YEAR, NULL for LIFETIME)';

-- ============================================================================
-- Step 2: Add device transfer limit tracking to macro_fort_subscriptions
-- ============================================================================

ALTER TABLE macro_fort_subscriptions 
  ADD COLUMN IF NOT EXISTS last_device_transfer_date TIMESTAMP WITH TIME ZONE;

COMMENT ON COLUMN macro_fort_subscriptions.last_device_transfer_date IS 
  'Timestamp of the most recent device transfer';

ALTER TABLE macro_fort_subscriptions 
  ADD COLUMN IF NOT EXISTS device_transfers_30days INTEGER DEFAULT 0;

COMMENT ON COLUMN macro_fort_subscriptions.device_transfers_30days IS 
  'Number of device transfers in the last 30 days (limit: 10 per 30 days)';

-- ============================================================================
-- Step 3: Update complete_device_transfer RPC with transfer limit check
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
  v_transfers_in_30days INTEGER;
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

  -- Get current subscription and check transfer limits
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

  -- Count transfers in last 30 days
  SELECT COUNT(*) INTO v_transfers_in_30days
  FROM macro_fort_subscriptions
  WHERE email = p_email
    AND last_device_transfer_date > (v_current_timestamp - INTERVAL '30 days');

  -- Check if user exceeded 10 transfers per 30 days limit
  IF v_transfers_in_30days >= 10 THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'لقد تجاوزت الحد الأقصى لنقل الجهاز (10 نقلات كل 30 يوم)',
      'transfers_used', v_transfers_in_30days,
      'max_transfers_per_30days', 10
    );
  END IF;

  -- Mark token as used
  UPDATE macro_fort_verification_codes
  SET used_date = v_current_timestamp
  WHERE email = p_email AND code = p_transfer_token;

  -- Update subscription with transfer info
  UPDATE macro_fort_subscriptions
  SET
    device_transfer_count = COALESCE(device_transfer_count, 0) + 1,
    last_device_transfer_date = v_current_timestamp,
    device_transfers_30days = (
      SELECT COUNT(*) 
      FROM macro_fort_subscriptions 
      WHERE email = p_email 
        AND last_device_transfer_date > (v_current_timestamp - INTERVAL '30 days')
    ) + 1,
    updated_at = v_current_timestamp
  WHERE email = p_email;

  -- Get updated subscription info
  SELECT * INTO v_subscription
  FROM macro_fort_subscriptions
  WHERE email = p_email
  LIMIT 1;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم نقل الجهاز بنجاح',
    'subscription_type', v_subscription.subscription_type,
    'expiry_date', v_subscription.expiry_date,
    'device_transfer_count', COALESCE(v_subscription.device_transfer_count, 0) + 1,
    'transfers_used_in_30days', v_transfers_in_30days + 1,
    'max_transfers_per_30days', 10
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- Verify the changes
-- ============================================================================

SELECT 
  'macro_fort_subscription_codes' as table_name,
  COUNT(*) as column_count
FROM information_schema.columns 
WHERE table_name = 'macro_fort_subscription_codes';

SELECT 
  'macro_fort_subscriptions' as table_name,
  COUNT(*) as column_count
FROM information_schema.columns 
WHERE table_name = 'macro_fort_subscriptions';
