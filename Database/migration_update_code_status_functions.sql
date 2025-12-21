-- Migration: Update subscription code functions to manage status field
-- Purpose: Ensure status is updated to 'used' when codes are consumed

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

INSERT INTO database_migrations (migration_name, migration_version, status, notes)
VALUES (
    'migration_update_code_status_functions',
    '1.0.0',
    'completed',
    'Updated RPC functions to properly manage subscription code status field'
)
ON CONFLICT (migration_name) DO UPDATE 
SET status = 'completed', applied_at = NOW();

SELECT 'Migration completed: Code Status Functions Updated' as status;
