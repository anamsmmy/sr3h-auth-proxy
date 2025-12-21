-- بيانات اختبار لنظام التفعيل الجديد
-- يجب تنفيذ supabase_setup.sql أولاً

-- إضافة مستخدمين للاختبار
INSERT INTO macro_subscriptions (
    email, 
    order_id, 
    is_active, 
    subscription_start,
    notes
) VALUES 
-- مستخدم نشط للاختبار العادي
(
    'test@sr3h.com', 
    'SR3H001', 
    true, 
    NOW(),
    'Test user - Active subscription'
),
-- مستخدم نشط آخر
(
    'demo@sr3h.com', 
    'SR3H002', 
    true, 
    NOW(),
    'Demo user - Active subscription'
),
-- مستخدم غير مفعل لاختبار الرفض
(
    'inactive@sr3h.com', 
    'SR3H003', 
    false, 
    NOW(),
    'Test user - Inactive subscription'
),
-- مستخدم للاختبار المتقدم
(
    'advanced@sr3h.com', 
    'SR3H004', 
    true, 
    NOW() - INTERVAL '10 days',
    'Advanced test user - 10 days old'
);

-- عرض البيانات المضافة
SELECT 
    id,
    email,
    hardware_id,
    is_active,
    subscription_start,
    order_id,
    last_check,
    notes
FROM macro_subscriptions
ORDER BY id;

-- اختبار دالة التفعيل
-- SELECT authenticate_user('test@sr3h.com', 'TEST_HARDWARE_ID_123');

-- اختبار دالة التحقق
-- SELECT verify_authentication('test@sr3h.com', 'TEST_HARDWARE_ID_123');

-- اختبار دالة إعادة التفعيل
-- SELECT reactivate_subscription('test@sr3h.com', 'SR3H001');