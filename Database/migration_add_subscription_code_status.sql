-- ============================================================================
-- Migration: Add status field to macro_fort_subscription_codes
-- Purpose: Track code lifecycle - unused (غير مستخدم), used (مستخدم), expired (منتهي)
-- ============================================================================

-- ============================================================================
-- Step 1: Add status column with check constraint
-- ============================================================================

ALTER TABLE macro_fort_subscription_codes
ADD COLUMN IF NOT EXISTS status VARCHAR(20) DEFAULT 'unused'
CHECK (status IN ('unused', 'used', 'expired'));

-- ============================================================================
-- Step 2: Update existing records based on used_date and expiry_date
-- ============================================================================

-- Mark codes that have been used
UPDATE macro_fort_subscription_codes
SET status = 'used'
WHERE used_date IS NOT NULL AND status = 'unused';

-- Mark expired codes
UPDATE macro_fort_subscription_codes
SET status = 'expired'
WHERE expiry_date IS NOT NULL 
  AND expiry_date < NOW() 
  AND status = 'unused';

-- ============================================================================
-- Step 3: Create indexes for status lookups
-- ============================================================================

CREATE INDEX IF NOT EXISTS idx_subscription_codes_status 
ON macro_fort_subscription_codes(status);

CREATE INDEX IF NOT EXISTS idx_subscription_codes_status_expiry 
ON macro_fort_subscription_codes(status, expiry_date);

-- ============================================================================
-- Step 4: Create function to update code status when used
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
-- Step 5: Create function to check code validity and status
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
-- Step 6: Grant permissions
-- ============================================================================

REVOKE ALL ON FUNCTION public.mark_code_as_used(TEXT, TEXT) FROM PUBLIC;
REVOKE ALL ON FUNCTION public.validate_subscription_code_status(TEXT) FROM PUBLIC;

GRANT EXECUTE ON FUNCTION public.mark_code_as_used(TEXT, TEXT) TO authenticated;
GRANT EXECUTE ON FUNCTION public.validate_subscription_code_status(TEXT) TO authenticated;

GRANT EXECUTE ON FUNCTION public.mark_code_as_used(TEXT, TEXT) TO service_role;
GRANT EXECUTE ON FUNCTION public.validate_subscription_code_status(TEXT) TO service_role;

-- ============================================================================
-- Step 7: Update migration tracking
-- ============================================================================

INSERT INTO database_migrations (migration_name, migration_version, status, notes)
VALUES (
    'migration_add_subscription_code_status',
    '1.0.0',
    'completed',
    'Added status column and management functions for subscription code lifecycle tracking'
)
ON CONFLICT (migration_name) DO UPDATE 
SET status = 'completed', applied_at = NOW();

SELECT 'Migration completed: Subscription Code Status Management' as status;
