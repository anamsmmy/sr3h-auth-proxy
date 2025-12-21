-- ============================================
-- التحقق من الحقول غير المستخدمة في قاعدة البيانات
-- ============================================

-- 1. جدول macro_fort_subscriptions - التحقق من حقول التحقق
SELECT 'macro_fort_subscriptions - verification fields' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(verification_count, 0)) as non_zero_verification_count,
       COUNT(NULLIF(last_verified_timestamp, NULL)) as non_null_last_verified,
       COUNT(NULLIF(last_verification_ip, NULL)) as non_null_last_ip,
       MAX(verification_count) as max_verification_count,
       COUNT(DISTINCT last_verification_ip) as distinct_ips
FROM macro_fort_subscriptions;

-- 2. جدول macro_fort_subscriptions - التحقق من device_transfers_30days
SELECT 'macro_fort_subscriptions - device_transfers_30days' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(device_transfers_30days, 0)) as non_zero_transfers,
       MAX(device_transfers_30days) as max_transfers,
       AVG(device_transfers_30days) as avg_transfers,
       SUM(device_transfers_30days) as total_transfers
FROM macro_fort_subscriptions;

-- 3. جدول macro_fort_subscriptions - التحقق من order_number
SELECT 'macro_fort_subscriptions - order_number' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(order_number, '')) as non_empty_order_numbers,
       COUNT(NULLIF(order_number, NULL)) as non_null_order_numbers,
       COUNT(DISTINCT order_number) as distinct_order_numbers
FROM macro_fort_subscriptions
WHERE order_number IS NOT NULL;

-- 4. جدول macro_fort_subscription_codes - rebind حقول
SELECT 'macro_fort_subscription_codes - rebind fields' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(rebind_attempts, 0)) as non_zero_rebind_attempts,
       COUNT(NULLIF(rebind_attempts_30days, 0)) as non_zero_rebind_30days,
       COUNT(NULLIF(last_rebind_date, NULL)) as non_null_last_rebind,
       MAX(rebind_attempts) as max_rebind_attempts,
       MAX(rebind_attempts_30days) as max_rebind_30days
FROM macro_fort_subscription_codes;

-- 5. جدول macro_fort_subscription_codes - order_number
SELECT 'macro_fort_subscription_codes - order_number' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(order_number, '')) as non_empty_order_numbers,
       COUNT(NULLIF(order_number, NULL)) as non_null_order_numbers,
       COUNT(DISTINCT order_number) as distinct_order_numbers
FROM macro_fort_subscription_codes
WHERE order_number IS NOT NULL;

-- 6. جدول macro_fort_subscription_codes - تفصيل حالة الكود مع rebind
SELECT status,
       COUNT(*) as count,
       COUNT(NULLIF(rebind_attempts, 0)) as with_rebind_attempts,
       COUNT(NULLIF(last_rebind_date, NULL)) as with_rebind_date,
       MAX(rebind_attempts) as max_attempts
FROM macro_fort_subscription_codes
GROUP BY status;

-- 7. جدول macro_fort_verification_codes - order_id
SELECT 'macro_fort_verification_codes - order_id' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(order_id, '')) as non_empty_order_ids,
       COUNT(NULLIF(order_id, NULL)) as non_null_order_ids,
       COUNT(DISTINCT order_id) as distinct_order_ids
FROM macro_fort_verification_codes
WHERE order_id IS NOT NULL;

-- 8. جدول macro_fort_trial_history - notes
SELECT 'macro_fort_trial_history - notes' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(notes, '')) as non_empty_notes,
       COUNT(NULLIF(notes, NULL)) as non_null_notes,
       COUNT(DISTINCT notes) as distinct_notes
FROM macro_fort_trial_history
WHERE notes IS NOT NULL;

-- 9. التحقق الشامل من جميع الحقول - هل يوجد بيانات حديثة
SELECT 'Comprehensive Check - Recent Activity (Last 7 Days)' as check_name,
       COUNT(*) as total_recent,
       COUNT(NULLIF(verification_count, 0)) as with_verification,
       COUNT(NULLIF(device_transfers_30days, 0)) as with_device_transfers,
       COUNT(NULLIF(order_number, NULL)) as with_order_number
FROM macro_fort_subscriptions
WHERE updated_at >= NOW() - INTERVAL '7 days';

-- 10. التحقق من الارتباطات بين الجداول
SELECT 'Cross-table validation' as check_name,
       'subscription_codes to subscriptions' as relation,
       COUNT(*) as codes_with_active_sub
FROM macro_fort_subscription_codes sc
INNER JOIN macro_fort_subscriptions s ON sc.email = s.email AND sc.hardware_id = s.hardware_id
WHERE sc.status = 'used'
  AND s.subscription_code = sc.code;

-- 11. تقرير التكرار - هل order_number يُستخدم في المنطق
SELECT email, 
       subscription_type,
       COUNT(DISTINCT order_number) as distinct_orders
FROM macro_fort_subscriptions
GROUP BY email, subscription_type
HAVING COUNT(DISTINCT order_number) > 1;
