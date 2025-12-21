-- ============================================================================
-- Create macro_fort_subscription_codes table
-- ============================================================================

CREATE TABLE IF NOT EXISTS macro_fort_subscription_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL,
    subscription_type TEXT NOT NULL DEFAULT 'premium',
    duration_days INTEGER NOT NULL DEFAULT 30,
    used_date TIMESTAMP WITH TIME ZONE NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expiry_date TIMESTAMP WITH TIME ZONE NULL
);

CREATE INDEX IF NOT EXISTS idx_subscription_codes_code ON macro_fort_subscription_codes(code);
CREATE INDEX IF NOT EXISTS idx_subscription_codes_email ON macro_fort_subscription_codes(email);
CREATE INDEX IF NOT EXISTS idx_subscription_codes_used ON macro_fort_subscription_codes(is_used);

-- ============================================================================
-- Drop and recreate macro_fort_verification_codes table
-- ============================================================================

DROP TABLE IF EXISTS macro_fort_verification_codes CASCADE;

CREATE TABLE macro_fort_verification_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email TEXT NOT NULL,
    
    -- OTP Verification fields
    otp_code TEXT,
    hardware_id TEXT,
    is_verified BOOLEAN DEFAULT FALSE,
    verified_at TIMESTAMP WITH TIME ZONE NULL,
    expires_at TIMESTAMP WITH TIME ZONE,
    
    -- Device Transfer fields
    code TEXT,
    code_type TEXT,
    used_date TIMESTAMP WITH TIME ZONE NULL,
    expiry_date TIMESTAMP WITH TIME ZONE,
    
    -- Spam Prevention fields
    last_otp_sent_at TIMESTAMP WITH TIME ZONE NULL,
    otp_request_count INTEGER DEFAULT 0,
    is_throttled BOOLEAN DEFAULT FALSE,
    throttle_until TIMESTAMP WITH TIME ZONE NULL,
    
    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes for OTP operations
CREATE INDEX idx_verification_codes_email ON macro_fort_verification_codes(email);
CREATE INDEX idx_verification_codes_otp ON macro_fort_verification_codes(otp_code);
CREATE INDEX idx_verification_codes_hardware ON macro_fort_verification_codes(hardware_id);

-- Indexes for Device Transfer operations
CREATE INDEX idx_verification_codes_code ON macro_fort_verification_codes(code);
CREATE INDEX idx_verification_codes_code_type ON macro_fort_verification_codes(code_type);

-- Index for expiration checks
CREATE INDEX idx_verification_codes_expires_at ON macro_fort_verification_codes(expires_at);
CREATE INDEX idx_verification_codes_expiry_date ON macro_fort_verification_codes(expiry_date);

-- ============================================================================
-- Ensure macro_fort_subscriptions has all required columns
-- ============================================================================

ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS subscription_code TEXT;
ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS email_verified BOOLEAN DEFAULT FALSE;
ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS device_count INTEGER DEFAULT 1;
ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS max_devices INTEGER DEFAULT 10;
ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS is_trial BOOLEAN DEFAULT FALSE;
ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS verification_count INTEGER DEFAULT 0;
ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS last_verified_timestamp TIMESTAMP WITH TIME ZONE;
ALTER TABLE macro_fort_subscriptions ADD COLUMN IF NOT EXISTS last_verification_ip TEXT;


