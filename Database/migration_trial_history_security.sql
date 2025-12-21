-- ============================================================================
-- Migration: Create Trial History Table (Permanent Trial Record)
-- Purpose: One-time trial lock per device - prevents trial reuse indefinitely
-- Security: This is the SOURCE OF TRUTH for trial eligibility
-- ============================================================================

-- Create the permanent trial history table
CREATE TABLE IF NOT EXISTS macro_fort_trial_history (
  id BIGSERIAL PRIMARY KEY,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  
  device_fingerprint_hash TEXT NOT NULL UNIQUE,
  first_trial_started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  trial_expires_at TIMESTAMPTZ NOT NULL,
  trial_days INT NOT NULL DEFAULT 7,
  
  notes TEXT
);

-- Create unique index to enforce one trial per device
CREATE UNIQUE INDEX IF NOT EXISTS uq_trial_history_device
ON macro_fort_trial_history(device_fingerprint_hash);

-- Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_trial_history_expiry
ON macro_fort_trial_history(trial_expires_at);

-- ============================================================================
-- Update macro_fort_subscriptions table with required columns
-- ============================================================================

-- Add columns if they don't exist
ALTER TABLE macro_fort_subscriptions
ADD COLUMN IF NOT EXISTS is_trial BOOLEAN DEFAULT FALSE;

ALTER TABLE macro_fort_subscriptions
ADD COLUMN IF NOT EXISTS device_transfer_count INT DEFAULT 0;

ALTER TABLE macro_fort_subscriptions
ADD COLUMN IF NOT EXISTS last_device_transfer_date TIMESTAMPTZ;

ALTER TABLE macro_fort_subscriptions
ADD COLUMN IF NOT EXISTS otp_code VARCHAR(6);

ALTER TABLE macro_fort_subscriptions
ADD COLUMN IF NOT EXISTS otp_expiry TIMESTAMPTZ;

-- ============================================================================
-- Note: subscription_code was removed in migration_database_optimization
-- Device binding is now via hardware_id + is_trial flag
-- ============================================================================

-- ============================================================================
-- Create function to check trial eligibility
-- Returns: JSON with { allowed: boolean, reason: string }
-- ============================================================================

CREATE OR REPLACE FUNCTION check_trial_eligibility(
  p_device_fingerprint_hash TEXT
)
RETURNS JSONB AS $$
DECLARE
  v_trial_record RECORD;
  v_result JSONB;
  v_current_timestamp TIMESTAMPTZ;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());

  -- Check if device has already used trial
  SELECT * INTO v_trial_record
  FROM macro_fort_trial_history
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
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- Create function to activate trial
-- Must be called AFTER OTP verification
-- ============================================================================

CREATE OR REPLACE FUNCTION activate_trial(
  p_device_fingerprint_hash TEXT,
  p_email TEXT,
  p_trial_days INT DEFAULT 7
)
RETURNS JSONB AS $$
DECLARE
  v_trial_expires TIMESTAMPTZ;
  v_current_timestamp TIMESTAMPTZ;
  v_result JSONB;
BEGIN
  v_current_timestamp := TIMEZONE('UTC', NOW());
  v_trial_expires := v_current_timestamp + (p_trial_days || ' days')::INTERVAL;

  -- First: Check eligibility
  SELECT check_trial_eligibility(p_device_fingerprint_hash) INTO v_result;
  
  IF (v_result->>'allowed')::BOOLEAN = FALSE THEN
    RETURN jsonb_build_object(
      'success', FALSE,
      'message', v_result->>'reason'
    );
  END IF;

  -- Try to insert trial history record
  -- If it already exists (concurrent request), it will fail and we catch it
  BEGIN
    INSERT INTO macro_fort_trial_history (
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
    SELECT check_trial_eligibility(p_device_fingerprint_hash) INTO v_result;
    IF (v_result->>'allowed')::BOOLEAN = FALSE THEN
      RETURN jsonb_build_object(
        'success', FALSE,
        'message', 'trial_already_used_on_device'
      );
    END IF;
  END;

  -- Insert or update subscription
  INSERT INTO macro_fort_subscriptions (
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
  )
  ON CONFLICT(email) DO UPDATE SET
    hardware_id = EXCLUDED.hardware_id,
    subscription_type = EXCLUDED.subscription_type,
    expiry_date = EXCLUDED.expiry_date,
    is_active = EXCLUDED.is_active,
    is_trial = EXCLUDED.is_trial,
    updated_at = v_current_timestamp
  WHERE macro_fort_subscriptions.subscription_type = 'trial'
    AND macro_fort_subscriptions.expiry_date > v_current_timestamp;

  RETURN jsonb_build_object(
    'success', TRUE,
    'message', 'trial_activated',
    'device_fingerprint', p_device_fingerprint_hash,
    'trial_expires_at', v_trial_expires,
    'trial_days', p_trial_days
  );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- Grant permissions
-- ============================================================================

GRANT SELECT ON macro_fort_trial_history TO authenticated;
GRANT EXECUTE ON FUNCTION check_trial_eligibility TO authenticated;
GRANT EXECUTE ON FUNCTION activate_trial TO authenticated;

SELECT 'Migration completed: Trial History Security System' as status;
