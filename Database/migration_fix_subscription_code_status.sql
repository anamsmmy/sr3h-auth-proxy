-- ============================================================================
-- Migration: Fix Subscription Code Status to Use Only Three States
-- Purpose: Remove 'active' state, maintain only unused/used/expired
-- Issue: 'active' state was redundant and complicates validation logic
-- ============================================================================

-- ============================================================================
-- Step 1: Update CHECK constraint to remove 'active'
-- ============================================================================

ALTER TABLE macro_fort_subscription_codes
DROP CONSTRAINT IF EXISTS macro_fort_subscription_codes_status_check;

ALTER TABLE macro_fort_subscription_codes
ADD CONSTRAINT macro_fort_subscription_codes_status_check 
CHECK (status IN ('unused', 'used', 'expired'));

-- ============================================================================
-- Step 2: Update mark_code_as_used function
-- Remove 'active' from status check
-- ============================================================================

CREATE OR REPLACE FUNCTION public.mark_code_as_used(
  p_code TEXT,
  p_email TEXT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_catalog
AS $$
DECLARE
  v_current_timestamp TIMESTAMPTZ;
  v_affected_rows INT;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  UPDATE public.macro_fort_subscription_codes
  SET 
    used_date = v_current_timestamp,
    email = p_email,
    status = 'used'
  WHERE code = p_code AND status = 'unused';

  GET DIAGNOSTICS v_affected_rows = ROW_COUNT;

  IF v_affected_rows = 0 THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'Code not found or already used',
      'code', p_code
    );
  END IF;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'Code marked as used',
    'code', p_code,
    'email', p_email,
    'used_at', v_current_timestamp
  );
END;
$$;

-- ============================================================================
-- Step 3: Update validate_subscription_code_status function
-- Simplify validation logic for three states only
-- ============================================================================

CREATE OR REPLACE FUNCTION public.validate_subscription_code_status(
  p_code TEXT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_catalog
AS $$
DECLARE
  v_code_record RECORD;
  v_current_timestamp TIMESTAMPTZ;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  SELECT * INTO v_code_record
  FROM public.macro_fort_subscription_codes
  WHERE code = p_code;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'valid', FALSE,
      'status', 'not_found',
      'message', 'رمز غير موجود'
    );
  END IF;

  -- Update status if expired
  IF v_code_record.expiry_date < v_current_timestamp 
    AND v_code_record.status != 'expired' THEN
    UPDATE public.macro_fort_subscription_codes
    SET status = 'expired'
    WHERE code = p_code;
    v_code_record.status := 'expired';
  END IF;

  RETURN jsonb_build_object(
    'valid', v_code_record.status = 'unused',
    'status', v_code_record.status,
    'code', p_code,
    'subscription_type', v_code_record.subscription_type,
    'duration_days', v_code_record.duration_days,
    'created_at', v_code_record.created_at,
    'expiry_date', v_code_record.expiry_date,
    'used_date', v_code_record.used_date,
    'message', CASE 
      WHEN v_code_record.status = 'used' THEN 'تم استخدام الرمز من قبل'
      WHEN v_code_record.status = 'expired' THEN 'انتهت صلاحية الرمز'
      ELSE 'الرمز متاح للاستخدام'
    END
  );
END;
$$;

-- ============================================================================
-- Step 4: Update redeem_subscription_code function
-- Use status field instead of checking used_date and email fields
-- ============================================================================

CREATE OR REPLACE FUNCTION public.redeem_subscription_code(
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

  -- Check if code exists and is valid (unused and not expired)
  SELECT * INTO v_code_record
  FROM public.macro_fort_subscription_codes
  WHERE code = p_code
    AND status = 'unused'
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
  FROM public.macro_fort_subscriptions
  WHERE email = p_email
  LIMIT 1;

  v_expiry_date := COALESCE(v_code_record.expiry_date, v_current_timestamp + INTERVAL '30 days');

  IF FOUND THEN
    UPDATE public.macro_fort_subscriptions
    SET
      subscription_type = v_code_record.subscription_type,
      expiry_date = GREATEST(expiry_date, v_expiry_date),
      is_active = TRUE,
      updated_at = v_current_timestamp
    WHERE email = p_email;
  ELSE
    INSERT INTO public.macro_fort_subscriptions (
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

  -- Mark code as used with status update
  UPDATE public.macro_fort_subscription_codes
  SET 
    used_date = v_current_timestamp,
    status = 'used',
    email = p_email
  WHERE code = p_code;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم استرجاع الكود بنجاح',
    'subscription_type', v_code_record.subscription_type,
    'expiry_date', v_expiry_date
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = public, pg_catalog;

-- ============================================================================
-- Step 5: Update authenticate_user function
-- Remove 'active' from status filter
-- ============================================================================

CREATE OR REPLACE FUNCTION public.authenticate_user(
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

  SELECT * INTO v_code_record
  FROM public.macro_fort_subscription_codes
  WHERE code = p_subscription_code
    AND status = 'unused'
    AND (expiry_date IS NULL OR expiry_date > v_current_timestamp)
  LIMIT 1;

  IF NOT FOUND THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', 'كود غير صحيح أو منتهي الصلاحية'
    );
  END IF;

  SELECT * INTO v_subscription
  FROM public.macro_fort_subscriptions
  WHERE email = p_email
  LIMIT 1;

  v_duration_days := COALESCE(v_code_record.duration_days, 30);
  v_expiry_date := v_current_timestamp + (v_duration_days || ' days')::INTERVAL;

  IF FOUND THEN
    UPDATE public.macro_fort_subscriptions
    SET
      subscription_type = v_code_record.subscription_type,
      expiry_date = GREATEST(expiry_date, v_expiry_date),
      is_active = TRUE,
      updated_at = v_current_timestamp
    WHERE email = p_email;
  ELSE
    INSERT INTO public.macro_fort_subscriptions (
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

  UPDATE public.macro_fort_subscription_codes
  SET 
    used_date = v_current_timestamp,
    status = 'used',
    email = p_email,
    updated_at = v_current_timestamp
  WHERE code = p_subscription_code;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'تم تفعيل الاشتراك بنجاح',
    'subscription_type', v_code_record.subscription_type,
    'expiry_date', v_expiry_date,
    'is_active', TRUE
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = public, pg_catalog;

-- ============================================================================
-- Step 6: Grant permissions
-- ============================================================================

GRANT EXECUTE ON FUNCTION public.mark_code_as_used(TEXT, TEXT) TO authenticated;
GRANT EXECUTE ON FUNCTION public.validate_subscription_code_status(TEXT) TO authenticated;
GRANT EXECUTE ON FUNCTION public.authenticate_user(TEXT, TEXT) TO authenticated;
GRANT EXECUTE ON FUNCTION public.redeem_subscription_code(TEXT, TEXT, TEXT) TO authenticated;

GRANT EXECUTE ON FUNCTION public.mark_code_as_used(TEXT, TEXT) TO service_role;
GRANT EXECUTE ON FUNCTION public.validate_subscription_code_status(TEXT) TO service_role;
GRANT EXECUTE ON FUNCTION public.authenticate_user(TEXT, TEXT) TO service_role;
GRANT EXECUTE ON FUNCTION public.redeem_subscription_code(TEXT, TEXT, TEXT) TO service_role;

-- ============================================================================
-- Step 7: Update migration tracking
-- ============================================================================

INSERT INTO database_migrations (migration_name, migration_version, status, notes)
VALUES (
    'migration_fix_subscription_code_status',
    '1.0.0',
    'completed',
    'Fixed subscription code status to use only three states: unused (غير مستخدم), used (مستخدم), expired (منتهي)'
)
ON CONFLICT (migration_name) DO UPDATE 
SET status = 'completed', applied_at = NOW();

SELECT 'Migration completed: Subscription Code Status Fixed (3 states only)' as status;
