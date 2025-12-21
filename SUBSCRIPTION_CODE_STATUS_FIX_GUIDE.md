# Subscription Code Status Fix - Implementation Guide

## Overview
This guide explains how to implement the subscription code status fix that removes the redundant 'active' state and keeps only three states: `unused`, `used`, and `expired`.

## Files Changed

### 1. Source Code Files (Already Updated)
- **migration_add_subscription_code_status.sql** - Updated to remove 'active' state
- **migration_update_code_status_functions.sql** - Updated function logic
- **migration_fix_subscription_code_status.sql** - NEW: Complete migration file

### 2. Changes Summary

#### Three States Only:
- `unused` (غير مستخدم) - Code has not been used yet
- `used` (مستخدم) - Code has been consumed/redeemed
- `expired` (منتهي) - Code has passed its expiry date

#### Updated Functions:
1. **mark_code_as_used()** - Removed 'active' from status check
2. **validate_subscription_code_status()** - Simplified validation logic
3. **redeem_subscription_code()** - Now uses status field properly
4. **authenticate_user()** - Removed 'active' from validation

## How to Apply the Migration

### Option 1: Using Supabase SQL Editor (Recommended)

1. Open Supabase Dashboard: https://app.supabase.com
2. Navigate to **SQL Editor**
3. Click **New Query**
4. Copy the entire contents of `Database/migration_fix_subscription_code_status.sql`
5. Paste into the SQL editor
6. Click **Run** to execute the migration
7. Verify all statements executed successfully

### Option 2: Using Node.js Script

```bash
cd c:\SR3H_MACRO
node apply_subscription_code_status_fix_v2.js
```

**Note:** This method requires a properly configured RPC endpoint and may need adjustments.

### Option 3: Manual Import via Supabase CLI

If you have Supabase CLI installed:

```bash
supabase db push --file Database/migration_fix_subscription_code_status.sql
```

## Verification Steps

After applying the migration, verify the changes:

1. **Check Table Constraint:**
   ```sql
   SELECT constraint_name, constraint_type 
   FROM information_schema.table_constraints 
   WHERE table_name = 'macro_fort_subscription_codes' 
     AND constraint_type = 'CHECK';
   ```

2. **Test Status Values:**
   ```sql
   -- List all unique status values
   SELECT DISTINCT status FROM macro_fort_subscription_codes;
   -- Should return: 'unused', 'used', 'expired' only
   ```

3. **Verify Function Signatures:**
   ```sql
   SELECT routine_name, routine_type 
   FROM information_schema.routines 
   WHERE routine_schema = 'public' 
     AND routine_name IN (
       'mark_code_as_used',
       'validate_subscription_code_status',
       'redeem_subscription_code',
       'authenticate_user'
     );
   ```

4. **Test Code Redemption:**
   - Create a test code with `status = 'unused'`
   - Verify it can be redeemed
   - After redemption, verify `status = 'used'`
   - Verify expired codes cannot be redeemed

## Testing Scenarios

### Scenario 1: Redeem Unused Code
```
Input: Code with status='unused'
Expected: Successfully redeemed → status='used'
```

### Scenario 2: Try to Redeem Used Code
```
Input: Code with status='used'
Expected: Rejection - "Code not found or already used"
```

### Scenario 3: Try to Redeem Expired Code
```
Input: Code with status='expired'
Expected: Rejection - "Code not found or already used"
```

### Scenario 4: Expired Code Auto-Detection
```
Input: Code with status='unused' but expiry_date in past
Expected: System auto-updates to status='expired'
```

## Rollback Plan

If issues occur, you can revert to the old state:

```sql
-- Restore constraint with 4 states
ALTER TABLE macro_fort_subscription_codes
DROP CONSTRAINT IF EXISTS macro_fort_subscription_codes_status_check;

ALTER TABLE macro_fort_subscription_codes
ADD CONSTRAINT macro_fort_subscription_codes_status_check 
CHECK (status IN ('unused', 'active', 'used', 'expired'));
```

## Migration Success Indicators

✅ All SQL statements executed without errors
✅ Table constraint accepts only 3 states
✅ All RPC functions updated and execute successfully
✅ Code redemption works with new status logic
✅ Existing codes maintain proper status values

## Support Files

- **Migration File:** `Database/migration_fix_subscription_code_status.sql`
- **Execute Script:** `apply_subscription_code_status_fix.js`
- **Execute Script V2:** `apply_subscription_code_status_fix_v2.js`

## Important Notes

1. **No Data Loss:** This migration only updates function logic, not data values
2. **Backwards Compatible:** Existing subscription codes retain their status values
3. **Safe to Apply:** Uses `CREATE OR REPLACE FUNCTION` for safe updates
4. **Database Mutations:** Only modifies RPC functions and constraints, no table data changes

---

**Last Updated:** 2025-12-20
**Status:** Ready for Implementation
