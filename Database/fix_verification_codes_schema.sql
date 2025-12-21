-- Fix macro_fort_verification_codes schema to match RPC functions

-- Add missing columns
ALTER TABLE macro_fort_verification_codes ADD COLUMN IF NOT EXISTS code TEXT;
ALTER TABLE macro_fort_verification_codes ADD COLUMN IF NOT EXISTS code_type TEXT DEFAULT 'OTP';
ALTER TABLE macro_fort_verification_codes ADD COLUMN IF NOT EXISTS expiry_date TIMESTAMP WITH TIME ZONE;
ALTER TABLE macro_fort_verification_codes ADD COLUMN IF NOT EXISTS used_date TIMESTAMP WITH TIME ZONE;

-- Copy data from otp_code to code if code is NULL
UPDATE macro_fort_verification_codes 
SET code = otp_code 
WHERE code IS NULL AND otp_code IS NOT NULL;

-- Update expiry_date from expires_at if NULL
UPDATE macro_fort_verification_codes 
SET expiry_date = expires_at 
WHERE expiry_date IS NULL AND expires_at IS NOT NULL;

-- Update used_date from verified_at if NULL and is_verified is true
UPDATE macro_fort_verification_codes 
SET used_date = verified_at 
WHERE used_date IS NULL AND verified_at IS NOT NULL AND is_verified = TRUE;

-- Make code column NOT NULL where it has data
ALTER TABLE macro_fort_verification_codes ALTER COLUMN code SET NOT NULL;

-- Add index on code and code_type for faster lookups
CREATE INDEX IF NOT EXISTS idx_verification_codes_code ON macro_fort_verification_codes(code);
CREATE INDEX IF NOT EXISTS idx_verification_codes_code_type ON macro_fort_verification_codes(code_type);
CREATE INDEX IF NOT EXISTS idx_verification_codes_expiry ON macro_fort_verification_codes(expiry_date);
