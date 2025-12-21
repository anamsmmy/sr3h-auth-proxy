-- ============================================================================
-- Migration: Create macro_fort_trial_history table (Permanent Trial Lock)
-- Purpose: Store trial history per device fingerprint - never delete/update
-- This is the SINGLE SOURCE OF TRUTH for preventing trial abuse
-- ============================================================================

CREATE TABLE IF NOT EXISTS macro_fort_trial_history (
    id BIGSERIAL PRIMARY KEY,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    device_fingerprint_hash TEXT NOT NULL UNIQUE,
    first_trial_started_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    trial_expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    trial_days INTEGER NOT NULL DEFAULT 7,
    
    notes TEXT
);

-- Create unique index to ensure one trial per device
CREATE UNIQUE INDEX IF NOT EXISTS uq_trial_history_device 
ON macro_fort_trial_history(device_fingerprint_hash);

-- Additional indexes for queries
CREATE INDEX IF NOT EXISTS idx_trial_history_created_at 
ON macro_fort_trial_history(created_at);

CREATE INDEX IF NOT EXISTS idx_trial_history_expires_at 
ON macro_fort_trial_history(trial_expires_at);

-- ============================================================================
-- Add device_fingerprint_hash column to macro_fort_subscriptions if missing
-- ============================================================================

ALTER TABLE macro_fort_subscriptions 
ADD COLUMN IF NOT EXISTS device_fingerprint_hash TEXT;

-- Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_subscriptions_device_fingerprint 
ON macro_fort_subscriptions(device_fingerprint_hash);

-- ============================================================================
-- Add status and other columns to macro_fort_subscription_codes if missing
-- ============================================================================

ALTER TABLE macro_fort_subscription_codes 
ADD COLUMN IF NOT EXISTS status TEXT DEFAULT 'unused';

ALTER TABLE macro_fort_subscription_codes 
ADD COLUMN IF NOT EXISTS device_fingerprint_hash TEXT;

ALTER TABLE macro_fort_subscription_codes 
ADD COLUMN IF NOT EXISTS rebind_attempts INTEGER DEFAULT 0;

ALTER TABLE macro_fort_subscription_codes 
ADD COLUMN IF NOT EXISTS rebind_attempts_30d INTEGER DEFAULT 0;

ALTER TABLE macro_fort_subscription_codes 
ADD COLUMN IF NOT EXISTS last_rebind_date TIMESTAMP WITH TIME ZONE;

ALTER TABLE macro_fort_subscription_codes 
ADD COLUMN IF NOT EXISTS activated_at TIMESTAMP WITH TIME ZONE;

-- Create unique index on subscription_code to prevent duplicates
CREATE UNIQUE INDEX IF NOT EXISTS uq_subscriptions_code 
ON macro_fort_subscription_codes(code);

-- Create index on status for queries
CREATE INDEX IF NOT EXISTS idx_subscription_codes_status 
ON macro_fort_subscription_codes(status);

-- Create index on device for lookups
CREATE INDEX IF NOT EXISTS idx_subscription_codes_device 
ON macro_fort_subscription_codes(device_fingerprint_hash);

-- ============================================================================
-- Add columns to macro_fort_verification_codes for OTP throttling
-- ============================================================================

ALTER TABLE macro_fort_verification_codes 
ADD COLUMN IF NOT EXISTS otp_request_count INTEGER DEFAULT 0;

ALTER TABLE macro_fort_verification_codes 
ADD COLUMN IF NOT EXISTS is_throttled BOOLEAN DEFAULT FALSE;

ALTER TABLE macro_fort_verification_codes 
ADD COLUMN IF NOT EXISTS throttle_until TIMESTAMP WITH TIME ZONE;

-- Create index for throttle lookups
CREATE INDEX IF NOT EXISTS idx_verification_codes_throttled 
ON macro_fort_verification_codes(is_throttled, throttle_until);

-- ============================================================================
-- Log completion
-- ============================================================================

SELECT 'Migration completed: macro_fort_trial_history and related columns created' AS status;
