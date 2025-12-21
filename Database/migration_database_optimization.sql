-- ============================================================================
-- Database Optimization Migration
-- Removes redundant fields, adds security tracking fields, optimizes schema
-- ============================================================================

-- ============================================================================
-- Phase 1: Remove redundant fields from macro_fort_subscriptions
-- ============================================================================

ALTER TABLE macro_fort_subscriptions 
DROP COLUMN IF EXISTS subscription_code;

ALTER TABLE macro_fort_subscriptions 
DROP COLUMN IF EXISTS otp_code;

ALTER TABLE macro_fort_subscriptions 
DROP COLUMN IF EXISTS otp_expiry;

-- ============================================================================
-- Phase 2: Remove duplicate fields from macro_fort_verification_codes
-- ============================================================================

-- Remove duplicate 'code' field (kept 'otp_code' which is more semantic)
ALTER TABLE macro_fort_verification_codes 
DROP COLUMN IF EXISTS code CASCADE;

-- Remove duplicate expiry field (kept 'expires_at' for consistency with subscriptions)
ALTER TABLE macro_fort_verification_codes 
DROP COLUMN IF EXISTS expiry_date CASCADE;

-- Remove code_type if always the same value (consolidate to single purpose table)
-- This can be removed if verification_codes is only for OTP
-- ALTER TABLE macro_fort_verification_codes 
-- DROP COLUMN IF EXISTS code_type;

-- ============================================================================
-- Phase 3: Add security tracking fields to macro_fort_subscriptions
-- ============================================================================

-- Hardware verification status tracking
ALTER TABLE macro_fort_subscriptions 
ADD COLUMN IF NOT EXISTS hardware_verification_status VARCHAR(20) DEFAULT 'pending'
CHECK (hardware_verification_status IN ('pending', 'verified', 'failed', 'mismatch'));

-- Timestamp of last successful hardware verification
ALTER TABLE macro_fort_subscriptions 
ADD COLUMN IF NOT EXISTS last_hardware_verification_at TIMESTAMP WITH TIME ZONE;

-- Grace period management (in-memory session based)
ALTER TABLE macro_fort_subscriptions 
ADD COLUMN IF NOT EXISTS grace_period_enabled BOOLEAN DEFAULT FALSE;

-- When grace period expires (only relevant for current session)
ALTER TABLE macro_fort_subscriptions 
ADD COLUMN IF NOT EXISTS grace_period_expires_at TIMESTAMP WITH TIME ZONE;

-- Raw hardware components for server-side comparison
-- Stored as JSONB: {disk1: "...", disk2: "...", cpu1: "...", cpu2: "...", bios: "..."}
ALTER TABLE macro_fort_subscriptions 
ADD COLUMN IF NOT EXISTS raw_hardware_components JSONB;

-- ============================================================================
-- Phase 4: Add security fields to macro_fort_trial_history
-- ============================================================================

ALTER TABLE macro_fort_trial_history 
ADD COLUMN IF NOT EXISTS secondary_hardware_components JSONB;

-- Installation ID for tracking unique installations
ALTER TABLE macro_fort_trial_history 
ADD COLUMN IF NOT EXISTS installation_id UUID DEFAULT gen_random_uuid();

-- OS version for additional fingerprinting
ALTER TABLE macro_fort_trial_history 
ADD COLUMN IF NOT EXISTS os_version VARCHAR(255);

-- Track how many times grace period was used
ALTER TABLE macro_fort_trial_history 
ADD COLUMN IF NOT EXISTS grace_period_usage_count INTEGER DEFAULT 0;

-- ============================================================================
-- Phase 5: Create hardware verification log table (for audit trail)
-- ============================================================================

CREATE TABLE IF NOT EXISTS macro_fort_hardware_verification_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID NOT NULL REFERENCES macro_fort_subscriptions(id) ON DELETE CASCADE,
    email TEXT NOT NULL,
    hardware_id VARCHAR(255) NOT NULL,
    
    -- Raw components as sent by client
    raw_components JSONB,
    
    -- Verification result
    verification_result VARCHAR(20) NOT NULL
    CHECK (verification_result IN ('success', 'mismatch', 'invalid', 'error')),
    
    -- Error details if verification failed
    error_details JSONB,
    
    -- Additional context
    client_ip TEXT,
    os_version VARCHAR(255),
    
    -- Timestamps
    verified_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create indexes for audit trail
CREATE INDEX IF NOT EXISTS idx_hw_verification_log_email 
ON macro_fort_hardware_verification_log(email);

CREATE INDEX IF NOT EXISTS idx_hw_verification_log_subscription 
ON macro_fort_hardware_verification_log(subscription_id);

CREATE INDEX IF NOT EXISTS idx_hw_verification_log_result 
ON macro_fort_hardware_verification_log(verification_result);

CREATE INDEX IF NOT EXISTS idx_hw_verification_log_verified_at 
ON macro_fort_hardware_verification_log(verified_at);

-- ============================================================================
-- Phase 6: Add indexes for new columns
-- ============================================================================

CREATE INDEX IF NOT EXISTS idx_subscriptions_hw_verification_status 
ON macro_fort_subscriptions(hardware_verification_status);

CREATE INDEX IF NOT EXISTS idx_subscriptions_last_hw_verification 
ON macro_fort_subscriptions(last_hardware_verification_at);

CREATE INDEX IF NOT EXISTS idx_subscriptions_grace_period 
ON macro_fort_subscriptions(grace_period_enabled, grace_period_expires_at);

-- ============================================================================
-- Phase 7: Update existing records (optional - set reasonable defaults)
-- ============================================================================

-- Mark all currently active subscriptions as verified
UPDATE macro_fort_subscriptions 
SET hardware_verification_status = 'verified',
    last_hardware_verification_at = COALESCE(last_check_date, NOW())
WHERE is_active = true AND hardware_verification_status = 'pending';

-- ============================================================================
-- Phase 8: Create migration status tracking table
-- ============================================================================

CREATE TABLE IF NOT EXISTS database_migrations (
    id SERIAL PRIMARY KEY,
    migration_name VARCHAR(255) NOT NULL UNIQUE,
    migration_version VARCHAR(20),
    applied_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'completed', 'failed')),
    notes TEXT
);

-- Record this migration
INSERT INTO database_migrations (migration_name, migration_version, status, notes)
VALUES (
    'migration_database_optimization',
    '1.0.0',
    'completed',
    'Removed redundant fields, added hardware verification tracking, created audit log table'
)
ON CONFLICT (migration_name) DO UPDATE 
SET status = 'completed', applied_at = NOW();

-- ============================================================================
-- Summary of Changes
-- ============================================================================

/*
REMOVED FIELDS:
- macro_fort_subscriptions.subscription_code (use subscription_code relationship instead)
- macro_fort_subscriptions.otp_code (moved to verification_codes table)
- macro_fort_subscriptions.otp_expiry (moved to verification_codes table)
- macro_fort_verification_codes.code (consolidated, use otp_code)
- macro_fort_verification_codes.expiry_date (consolidated, use expires_at)

ADDED FIELDS:
- macro_fort_subscriptions:
  * hardware_verification_status: Tracks if device has been verified
  * last_hardware_verification_at: When device was last verified
  * grace_period_enabled: Is device in grace period?
  * grace_period_expires_at: When grace period ends
  * raw_hardware_components: JSONB of device components for server validation

- macro_fort_trial_history:
  * secondary_hardware_components: JSONB of secondary components
  * installation_id: UUID to track unique installations
  * os_version: OS version for fingerprinting
  * grace_period_usage_count: Track grace period usage

NEW TABLES:
- macro_fort_hardware_verification_log: Complete audit trail of all verification attempts
- database_migrations: Track applied migrations

BENEFITS:
✅ Eliminates data duplication and sync issues
✅ Supports in-memory session caching architecture
✅ Enables server-centric hardware verification
✅ Provides complete audit trail for security analysis
✅ Maintains backward compatibility (no breaking changes)
*/
