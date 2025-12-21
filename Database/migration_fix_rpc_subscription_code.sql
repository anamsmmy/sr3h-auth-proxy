-- ============================================================================
-- Migration: Fix RPC Functions to Remove subscription_code References
-- Purpose: Update activate_trial and other functions after subscription_code removal
-- ============================================================================

-- ============================================================================
-- Update activate_trial RPC function
-- Removes all references to deprecated subscription_code column
-- ============================================================================

CREATE OR REPLACE FUNCTION public.activate_trial(
  p_device_fingerprint_hash TEXT,
  p_email TEXT,
  p_trial_days INT DEFAULT 7
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_catalog
AS $$
DECLARE
  v_trial_expires TIMESTAMPTZ;
  v_current_timestamp TIMESTAMPTZ;
  v_result JSONB;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());
  v_trial_expires := v_current_timestamp + (p_trial_days || ' days')::INTERVAL;

  -- First: Check eligibility
  SELECT public.check_trial_eligibility(p_device_fingerprint_hash) INTO v_result;
  
  IF (v_result->>'allowed')::BOOLEAN = FALSE THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', v_result->>'reason'
    );
  END IF;

  -- Try to insert trial history record
  BEGIN
    INSERT INTO public.macro_fort_trial_history (
      device_fingerprint_hash,
      first_trial_started_at,
      trial_expires_at,
      trial_days,
      notes
    ) VALUES (
      p_device_fingerprint_hash,
      v_current_timestamp,
      v_trial_expires,
      p_trial_days,
      'Trial activated via email: ' || p_email
    );
  EXCEPTION WHEN unique_violation THEN
    -- Record already exists - check if still valid
    SELECT public.check_trial_eligibility(p_device_fingerprint_hash) INTO v_result;
    IF (v_result->>'allowed')::BOOLEAN = FALSE THEN
      RETURN jsonb_build_object(
        'success', FALSE,
        'message', 'trial_already_used_on_device'
      );
    END IF;
  END;

  -- Insert subscription record (subscription_code removed)
  BEGIN
    INSERT INTO public.macro_fort_subscriptions (
      email,
      hardware_id,
      subscription_type,
      activation_date,
      expiry_date,
      is_active,
      email_verified,
      is_trial,
      created_at,
      updated_at
    ) VALUES (
      p_email,
      p_device_fingerprint_hash,
      'trial',
      v_current_timestamp,
      v_trial_expires,
      TRUE,
      TRUE,
      TRUE,
      v_current_timestamp,
      v_current_timestamp
    );
  EXCEPTION WHEN unique_violation THEN
    -- Update existing subscription if it's a trial
    UPDATE public.macro_fort_subscriptions
    SET email = p_email,
        expiry_date = v_trial_expires,
        is_active = TRUE,
        is_trial = TRUE,
        updated_at = v_current_timestamp
    WHERE hardware_id = p_device_fingerprint_hash AND is_trial = TRUE;
  END;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'trial_activated',
    'device_fingerprint', p_device_fingerprint_hash,
    'trial_expires_at', v_trial_expires,
    'trial_days', p_trial_days
  );
END;
$$;

-- ============================================================================
-- Update check_trial_eligibility RPC function (ensure consistency)
-- ============================================================================

CREATE OR REPLACE FUNCTION public.check_trial_eligibility(
  p_device_fingerprint_hash TEXT
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_catalog
AS $$
DECLARE
  v_trial_record RECORD;
  v_result JSONB;
  v_current_timestamp TIMESTAMPTZ;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  -- Check if device has already used trial
  SELECT * INTO v_trial_record
  FROM public.macro_fort_trial_history
  WHERE device_fingerprint_hash = p_device_fingerprint_hash
  LIMIT 1;

  IF FOUND THEN
    -- Device has used trial before - check if expired
    IF v_trial_record.trial_expires_at > v_current_timestamp THEN
      -- Trial still active - can reactivate on same device
      RETURN jsonb_build_object(
        'allowed', TRUE,
        'reason', 'trial_exists_not_expired',
        'trial_expires_at', v_trial_record.trial_expires_at
      );
    ELSE
      -- Trial has expired - permanent rejection
      RETURN jsonb_build_object(
        'allowed', FALSE,
        'reason', 'trial_already_used_on_device',
        'first_trial_at', v_trial_record.first_trial_started_at,
        'trial_expired_at', v_trial_record.trial_expires_at
      );
    END IF;
  END IF;

  -- No trial record found - device eligible for trial
  RETURN jsonb_build_object(
    'allowed', TRUE,
    'reason', 'trial_eligible'
  );
END;
$$;

-- ============================================================================
-- Grant permissions
-- ============================================================================

REVOKE ALL ON FUNCTION public.check_trial_eligibility(TEXT) FROM PUBLIC;
REVOKE ALL ON FUNCTION public.activate_trial(TEXT, TEXT, INT) FROM PUBLIC;

GRANT EXECUTE ON FUNCTION public.check_trial_eligibility(TEXT) TO authenticated;
GRANT EXECUTE ON FUNCTION public.activate_trial(TEXT, TEXT, INT) TO authenticated;

GRANT EXECUTE ON FUNCTION public.check_trial_eligibility(TEXT) TO service_role;
GRANT EXECUTE ON FUNCTION public.activate_trial(TEXT, TEXT, INT) TO service_role;

-- ============================================================================
-- Update migration tracking
-- ============================================================================

INSERT INTO database_migrations (migration_name, migration_version, status, notes)
VALUES (
    'migration_fix_rpc_subscription_code',
    '1.0.0',
    'completed',
    'Fixed RPC functions to remove deprecated subscription_code column references'
)
ON CONFLICT (migration_name) DO UPDATE 
SET status = 'completed', applied_at = NOW();

SELECT 'Migration completed: RPC functions fixed for subscription_code removal' as status;
