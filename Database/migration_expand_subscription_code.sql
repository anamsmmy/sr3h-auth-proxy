-- ============================================================================
-- Migration: Expand subscription_code column (DEPRECATED)
-- Note: subscription_code column was removed from macro_fort_subscriptions in migration_database_optimization
-- This migration is no longer needed
-- ============================================================================

-- Expand the code column in macro_fort_subscription_codes table
-- This table still exists and is used for storing subscription activation codes
ALTER TABLE macro_fort_subscription_codes
ALTER COLUMN code TYPE TEXT;

-- Log the migration
SELECT 'Migration completed: code column in macro_fort_subscription_codes expanded to TEXT' as status;
