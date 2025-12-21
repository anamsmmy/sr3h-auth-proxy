# تحليل الحقول غير المستخدمة في قاعدة البيانات

## كيفية التنفيذ

انسخ الاستعلامات من `Database/verify_unused_columns.sql` والصقها في **SQL Editor** من لوحة Supabase مباشرة.

---

## الاستعلامات والتوقعات

### 1️⃣ **جدول `macro_fort_subscriptions` - حقول التحقق**

```sql
SELECT 'macro_fort_subscriptions - verification fields' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(verification_count, 0)) as non_zero_verification_count,
       COUNT(NULLIF(last_verified_timestamp, NULL)) as non_null_last_verified,
       COUNT(NULLIF(last_verification_ip, NULL)) as non_null_last_ip,
       MAX(verification_count) as max_verification_count,
       COUNT(DISTINCT last_verification_ip) as distinct_ips
FROM macro_fort_subscriptions;
```

**إذا كانت الحقول **مستخدمة** فعلياً:**
- ✅ `non_zero_verification_count` > 0 (على الأقل بعض السجلات)
- ✅ `non_null_last_verified` > 0
- ✅ `non_null_last_ip` > 0
- ✅ `max_verification_count` > 5 (عدة تحققات)

**إذا كانت **غير مستخدمة**:**
- ❌ جميع القيم = 0 أو NULL
- ❌ `distinct_ips` = 0

**النتيجة المتوقعة من الكود الحالي:** ❌ **أصفار وNULL** (لأن الكود C# لا يكتب هذه الحقول)

---

### 2️⃣ **جدول `macro_fort_subscriptions` - `device_transfers_30d`**

```sql
SELECT 'macro_fort_subscriptions - device_transfers_30d' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(device_transfers_30d, 0)) as non_zero_transfers,
       MAX(device_transfers_30d) as max_transfers,
       AVG(device_transfers_30d) as avg_transfers,
       SUM(device_transfers_30d) as total_transfers
FROM macro_fort_subscriptions;
```

**إذا كانت **مستخدمة**:**
- ✅ `non_zero_transfers` > 0
- ✅ `max_transfers` >= 2 (إذا كان حد التحويل 2)
- ✅ `avg_transfers` > 0

**إذا كانت **غير مستخدمة**:**
- ❌ `non_zero_transfers` = 0
- ❌ `max_transfers` = 0

**النتيجة المتوقعة:** ❌ **أصفار فقط** (لا يوجد كود يعدل هذا الحقل)

---

### 3️⃣ **جدول `macro_fort_subscriptions` - `order_number`**

```sql
SELECT 'macro_fort_subscriptions - order_number' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(order_number, '')) as non_empty_order_numbers,
       COUNT(NULLIF(order_number, NULL)) as non_null_order_numbers,
       COUNT(DISTINCT order_number) as distinct_order_numbers
FROM macro_fort_subscriptions
WHERE order_number IS NOT NULL;
```

**إذا كانت **مستخدمة**:**
- ✅ `non_empty_order_numbers` > 0
- ✅ `distinct_order_numbers` >= عدد المستخدمين

**إذا كانت **غير مستخدمة**:**
- ❌ جميع القيم NULL أو فارغة

**النتيجة المتوقعة:** ❌ **NULL** (لم نعثر على استخدام في الكود)

---

### 4️⃣ **جدول `macro_fort_subscription_codes` - حقول `rebind_*`**

```sql
SELECT 'macro_fort_subscription_codes - rebind fields' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(rebind_attempts, 0)) as non_zero_rebind_attempts,
       COUNT(NULLIF(rebind_attempts_30days, 0)) as non_zero_rebind_30days,
       COUNT(NULLIF(last_rebind_date, NULL)) as non_null_last_rebind,
       MAX(rebind_attempts) as max_rebind_attempts,
       MAX(rebind_attempts_30days) as max_rebind_30days
FROM macro_fort_subscription_codes;
```

**إذا كانت **مستخدمة**:**
- ✅ `non_zero_rebind_attempts` > 0
- ✅ `non_zero_rebind_30days` > 0
- ✅ `non_null_last_rebind` > 0
- ✅ `max_rebind_attempts` >= 3 (حد إعادة الربط)

**إذا كانت **غير مستخدمة**:**
- ❌ جميع القيم = 0 أو NULL

**النتيجة المتوقعة:** ❌ **أصفار وNULL** (لا يوجد منطق في Server.js أو C#)

---

### 5️⃣ **جدول `macro_fort_subscription_codes` - `order_number`**

```sql
SELECT 'macro_fort_subscription_codes - order_number' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(order_number, '')) as non_empty_order_numbers,
       COUNT(NULLIF(order_number, NULL)) as non_null_order_numbers,
       COUNT(DISTINCT order_number) as distinct_order_numbers
FROM macro_fort_subscription_codes
WHERE order_number IS NOT NULL;
```

**النتيجة المتوقعة:** ❌ **NULL** (لا يوجد استخدام)

---

### 6️⃣ **جدول `macro_fort_verification_codes` - `order_id`**

```sql
SELECT 'macro_fort_verification_codes - order_id' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(order_id, '')) as non_empty_order_ids,
       COUNT(NULLIF(order_id, NULL)) as non_null_order_ids,
       COUNT(DISTINCT order_id) as distinct_order_ids
FROM macro_fort_verification_codes
WHERE order_id IS NOT NULL;
```

**النتيجة المتوقعة:** ❌ **NULL** (البحث أعطى 0 نتائج)

---

### 7️⃣ **جدول `macro_fort_trial_history` - `notes`**

```sql
SELECT 'macro_fort_trial_history - notes' as check_name,
       COUNT(*) as total_records,
       COUNT(NULLIF(notes, '')) as non_empty_notes,
       COUNT(NULLIF(notes, NULL)) as non_null_notes,
       COUNT(DISTINCT notes) as distinct_notes
FROM macro_fort_trial_history
WHERE notes IS NOT NULL;
```

**النتيجة المتوقعة:** ❌ **NULL** (لم نعثر على كود يكتب هذا الحقل)

---

## ملخص الاستنتاجات

| الحقل | الجدول | الحالة | الدليل |
|------|--------|--------|--------|
| `verification_count` | `macro_fort_subscriptions` | ❌ غير مستخدم | لا يوجد في Model C# |
| `last_verified_timestamp` | `macro_fort_subscriptions` | ❌ غير مستخدم | لا يوجد في Model C# |
| `last_verification_ip` | `macro_fort_subscriptions` | ❌ غير مستخدم | لا يوجد في Model C# |
| `device_transfers_30d` | `macro_fort_subscriptions` | ❌ غير مستخدم | لا يوجد في C# أو Server.js |
| `order_number` | `macro_fort_subscriptions` | ❌ غير مستخدم | موجود في Model لكن لا يُستخدم |
| `order_number` | `macro_fort_subscription_codes` | ❌ غير مستخدم | موجود في Model لكن لا يُستخدم |
| `rebind_attempts` | `macro_fort_subscription_codes` | ❌ غير مستخدم | موجود في Model لكن لا يُستخدم |
| `rebind_attempts_30days` | `macro_fort_subscription_codes` | ❌ غير مستخدم | موجود في Model لكن لا يُستخدم |
| `last_rebind_date` | `macro_fort_subscription_codes` | ❌ غير مستخدم | موجود في Model لكن لا يُستخدم |
| `order_id` | `macro_fort_verification_codes` | ❌ غير مستخدم | البحث أعطى 0 نتائج |
| `notes` | `macro_fort_trial_history` | ❌ غير مستخدم | البحث أعطى 0 نتائج |

---

## التوصيات

### **للتنظيف:**
1. حذف الحقول من Database إذا لم تكن مخطط لاستخدامها مستقبلاً
2. تنظيف Model من الخصائص غير المستخدمة
3. تحديث التعريفات في Server.js

### **للاستخدام (إذا كانت مخطط لها):**
1. تنفيذ المنطق في `MacroFortActivationService.cs` و `server.js`
2. إضافة القراءة والكتابة في RPC Functions
3. تحديث Models لاستخدامها فعلياً

---

## الخطوات التالية

1. **شغّل الاستعلامات** في SQL Editor من Supabase
2. **احفظ النتائج** (screenshot)
3. **قارن** مع التوقعات أعلاه
4. **اتخذ قرار**: حذف أم تنفيذ المنطق؟
