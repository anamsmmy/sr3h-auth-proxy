-- Fix column size for transfer tokens and OTP codes
-- The code column needs to hold MD5 hashes (32 chars) and OTP codes (6 chars)
-- Current: VARCHAR(6) - TOO SMALL
-- New: VARCHAR(255) - PROPER SIZE

ALTER TABLE macro_fort_verification_codes
ALTER COLUMN code TYPE VARCHAR(255);

-- Verify the change
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'macro_fort_verification_codes' AND column_name = 'code';
