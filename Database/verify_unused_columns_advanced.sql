-- ============================================
-- استعلامات متقدمة للتحقق من الحقول غير المستخدمة
-- ============================================

-- 12. تحليل رسومي للنسب المئوية للبيانات الفارغة
SELECT 'Completeness Analysis' as analysis_type,
       ROUND(100.0 * COUNT(NULLIF(verification_count, 0)) / COUNT(*), 2) || '%' as verification_count_filled,
       ROUND(100.0 * COUNT(NULLIF(last_verified_timestamp, NULL)) / COUNT(*), 2) || '%' as last_verified_filled,
       ROUND(100.0 * COUNT(NULLIF(last_verification_ip, NULL)) / COUNT(*), 2) || '%' as last_ip_filled,
       ROUND(100.0 * COUNT(NULLIF(device_transfers_30days, 0)) / COUNT(*), 2) || '%' as transfers_30days_filled,
       ROUND(100.0 * COUNT(NULLIF(order_number, NULL)) / COUNT(*), 2) || '%' as order_number_filled
FROM macro_fort_subscriptions;

-- 13. التحقق من كود الاشتراك مقابل حقول rebind
SELECT sc.code,
       sc.status,
       sc.rebind_attempts,
       sc.rebind_attempts_30days,
       sc.last_rebind_date,
       sc.activated_at,
       COUNT(DISTINCT s.id) as linked_subscriptions
FROM macro_fort_subscription_codes sc
LEFT JOIN macro_fort_subscriptions s ON sc.email = s.email AND sc.hardware_id = s.hardware_id
WHERE sc.status = 'used'
GROUP BY sc.id, sc.code, sc.status, sc.rebind_attempts, sc.rebind_attempts_30days, sc.last_rebind_date, sc.activated_at
HAVING COUNT(DISTINCT s.id) > 0
LIMIT 20;

-- 14. البحث عن أي اشتراكات تتجاوز حد نقل الجهاز
SELECT email,
       hardware_id,
       subscription_type,
       device_transfer_count,
       device_transfers_30days,
       last_device_transfer_date,
       updated_at
FROM macro_fort_subscriptions
WHERE device_transfer_count > 2 OR device_transfers_30days > 2
ORDER BY device_transfer_count DESC;

-- 15. التحقق من order_number - هل يكون للكود علاقة بـ order_number في الـ subscription
SELECT sc.order_number as code_order,
       s.order_number as subscription_order,
       COUNT(*) as count
FROM macro_fort_subscription_codes sc
INNER JOIN macro_fort_subscriptions s ON sc.email = s.email
WHERE sc.order_number IS NOT NULL OR s.order_number IS NOT NULL
GROUP BY sc.order_number, s.order_number;

-- 16. التحقق من verification_count مقابل عدد التحققات الفعلية
SELECT s.email,
       s.hardware_id,
       s.verification_count,
       s.last_verified_timestamp,
       s.last_verification_ip,
       COUNT(DISTINCT DATE(h.first_trial_started_at)) as trial_days,
       COUNT(*) as trial_records
FROM macro_fort_subscriptions s
LEFT JOIN macro_fort_trial_history h ON s.email = h.device_fingerprint_hash OR s.hardware_id = h.device_fingerprint_hash
WHERE s.verification_count > 0
GROUP BY s.id, s.email, s.hardware_id, s.verification_count, s.last_verified_timestamp, s.last_verification_ip
LIMIT 10;

-- 17. تحليل ملء البيانات - جدول macro_fort_verification_codes
SELECT 'macro_fort_verification_codes - Data Completeness' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(order_id, '')) as non_empty_order_ids,
       COUNT(NULLIF(order_id, NULL)) as non_null_order_ids,
       COUNT(NULLIF(is_used, false)) as used_codes,
       COUNT(NULLIF(is_throttled, false)) as throttled_codes
FROM macro_fort_verification_codes;

-- 18. البحث عن أي سجلات trial ولها notes
SELECT id,
       device_fingerprint_hash,
       trial_expires_at,
       notes,
       created_at
FROM macro_fort_trial_history
WHERE notes IS NOT NULL AND notes != ''
LIMIT 20;

-- 19. التحقق من تسلسل rebind - كم عدد المحاولات الحقيقية
SELECT code,
       subscription_type,
       status,
       rebind_attempts,
       rebind_attempts_30days,
       last_rebind_date,
       CASE 
         WHEN status = 'used' AND rebind_attempts > 0 THEN 'Used with rebind'
         WHEN status = 'used' AND rebind_attempts = 0 THEN 'Used no rebind'
         WHEN status = 'expired' AND rebind_attempts > 0 THEN 'Expired with rebind'
         ELSE status 
       END as status_with_rebind
FROM macro_fort_subscription_codes
WHERE rebind_attempts > 0 OR rebind_attempts_30days > 0
LIMIT 20;

-- 20. البحث عن أي حقول مُحدثة مؤخراً (آخر 30 يوم)
SELECT 'Recent Updates Analysis' as check_name,
       COUNT(*) as recent_updates,
       COUNT(CASE WHEN verification_count > 0 THEN 1 END) as with_verification,
       COUNT(CASE WHEN device_transfers_30days > 0 THEN 1 END) as with_device_transfers,
       COUNT(CASE WHEN order_number IS NOT NULL THEN 1 END) as with_order_number,
       MAX(updated_at) as last_update
FROM macro_fort_subscriptions
WHERE updated_at >= NOW() - INTERVAL '30 days';

-- 21. تقرير شامل - اكتشاف الحقول الفعلية المستخدمة
SELECT 'Actual Usage Detection' as report_type,
       'Subscriptions with verification data' as metric,
       COUNT(*) as count
FROM macro_fort_subscriptions
WHERE verification_count > 0 OR last_verified_timestamp IS NOT NULL
UNION ALL
SELECT 'Actual Usage Detection',
       'Subscriptions with device_transfers_30days > 0',
       COUNT(*)
FROM macro_fort_subscriptions
WHERE device_transfers_30days > 0
UNION ALL
SELECT 'Actual Usage Detection',
       'Subscription codes with rebind attempts',
       COUNT(*)
FROM macro_fort_subscription_codes
WHERE rebind_attempts > 0 OR rebind_attempts_30days > 0
UNION ALL
SELECT 'Actual Usage Detection',
       'Trial records with notes',
       COUNT(*)
FROM macro_fort_trial_history
WHERE notes IS NOT NULL AND notes != ''
UNION ALL
SELECT 'Actual Usage Detection',
       'Verification codes with order_id',
       COUNT(*)
FROM macro_fort_verification_codes
WHERE order_id IS NOT NULL AND order_id != '';

-- 22. التحقق من اتساق البيانات - هل قيم rebind تتطابق مع last_rebind_date
SELECT code,
       rebind_attempts,
       rebind_attempts_30days,
       last_rebind_date,
       CASE 
         WHEN rebind_attempts > 0 AND last_rebind_date IS NULL THEN '⚠️ Inconsistent: attempts > 0 but no date'
         WHEN rebind_attempts = 0 AND last_rebind_date IS NOT NULL THEN '⚠️ Inconsistent: date exists but 0 attempts'
         WHEN rebind_attempts > 0 AND last_rebind_date IS NOT NULL THEN '✓ Consistent'
         ELSE '✓ Consistent'
       END as consistency_check
FROM macro_fort_subscription_codes
WHERE rebind_attempts > 0 OR rebind_attempts_30days > 0 OR last_rebind_date IS NOT NULL
LIMIT 20;

-- 23. النتيجة النهائية - جدول ملخص شامل
WITH verification_data AS (
  SELECT COUNT(*) as total_subs,
         COUNT(NULLIF(verification_count, 0)) as with_verification,
         COUNT(NULLIF(last_verified_timestamp, NULL)) as with_last_verified,
         COUNT(NULLIF(last_verification_ip, NULL)) as with_last_ip,
         COUNT(NULLIF(device_transfers_30days, 0)) as with_transfers,
         COUNT(NULLIF(order_number, NULL)) as with_order
  FROM macro_fort_subscriptions
),
rebind_data AS (
  SELECT COUNT(*) as total_codes,
         COUNT(NULLIF(rebind_attempts, 0)) as with_rebind_attempts,
         COUNT(NULLIF(rebind_attempts_30days, 0)) as with_rebind_30d,
         COUNT(NULLIF(last_rebind_date, NULL)) as with_rebind_date
  FROM macro_fort_subscription_codes
),
trial_data AS (
  SELECT COUNT(*) as total_trials,
         COUNT(NULLIF(notes, NULL)) as with_notes
  FROM macro_fort_trial_history
),
verification_codes_data AS (
  SELECT COUNT(*) as total_codes,
         COUNT(NULLIF(order_id, NULL)) as with_order_id,
         COUNT(NULLIF(code_type, NULL)) as with_code_type
  FROM macro_fort_verification_codes
)
SELECT 'FINAL SUMMARY REPORT' as report,
       'Subscriptions' as table_name,
       v.total_subs as total_records,
       v.with_verification as with_verification_count,
       v.with_last_verified as with_last_verified_ts,
       v.with_last_ip as with_last_ip,
       v.with_transfers as with_device_transfers_30d,
       v.with_order as with_order_number
FROM verification_data v
UNION ALL
SELECT 'FINAL SUMMARY REPORT',
       'Subscription Codes',
       r.total_codes,
       r.with_rebind_attempts,
       r.with_rebind_30d,
       r.with_rebind_date,
       NULL,
       NULL
FROM rebind_data r
UNION ALL
SELECT 'FINAL SUMMARY REPORT',
       'Trial History',
       t.total_trials,
       t.with_notes,
       NULL,
       NULL,
       NULL,
       NULL
FROM trial_data t
UNION ALL
SELECT 'FINAL SUMMARY REPORT',
       'Verification Codes',
       vc.total_codes,
       vc.with_order_id,
       vc.with_code_type,
       NULL,
       NULL,
       NULL
FROM verification_codes_data vc;
