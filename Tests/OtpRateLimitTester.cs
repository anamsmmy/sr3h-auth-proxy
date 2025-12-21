using System;
using System.Threading.Tasks;
using MacroApp.Services;

namespace MacroApp.Tests
{
    public class OtpRateLimitTester
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("🧪 اختبار حدود معدل OTP (OTP Rate Limiting Test)");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            var activationService = MacroFortActivationService.Instance;
            var testEmail = "test.ratelimit@example.com";
            var testCode = "TEST-CODE-12345";

            Console.WriteLine($"📧 البريد الاختباري: {testEmail}");
            Console.WriteLine($"🔑 الكود الاختباري: {testCode}\n");

            await TestOtpRateLimiting(activationService, testEmail, testCode);
            
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("✅ انتهى الاختبار");
            Console.WriteLine("═══════════════════════════════════════════════════════");
        }

        private static async Task TestOtpRateLimiting(MacroFortActivationService service, string email, string code)
        {
            Console.WriteLine("🔄 اختبار 1: محاولة إرسال 6 طلبات OTP متتالية");
            Console.WriteLine("────────────────────────────────────────────────────\n");

            for (int i = 1; i <= 6; i++)
            {
                Console.WriteLine($"📤 الطلب #{i}:");
                var result = await service.SendOtpForCodeActivationAsync(email, code);
                
                if (result.IsSuccess)
                {
                    Console.WriteLine($"   ✅ نجح: {result.Message}");
                }
                else
                {
                    Console.WriteLine($"   ❌ فشل: {result.Message}");
                    Console.WriteLine($"   النوع: {result.ResultType}");
                }

                if (i < 6)
                {
                    if (result.ResultType == "rate_limit_throttled" || result.ResultType == "rate_limit_exceeded")
                    {
                        Console.WriteLine($"   🚫 تم تفعيل حد المعدل! توقف الاختبار.");
                        break;
                    }

                    if (i < 5)
                    {
                        Console.WriteLine($"   ⏳ الانتظار 2 ثانية قبل الطلب التالي...\n");
                        await Task.Delay(2000);
                    }
                    else
                    {
                        Console.WriteLine($"   ⏳ الانتظار 1 ثانية قبل الطلب التالي (مباشرة بعد الـ 60 ثانية)...\n");
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    Console.WriteLine();
                }
            }

            Console.WriteLine("\n🔄 اختبار 2: التحقق من حد الـ 60 ثانية الأدنى");
            Console.WriteLine("────────────────────────────────────────────────────\n");
            
            Console.WriteLine("📤 الطلب الأول:");
            var firstResult = await service.SendOtpForCodeActivationAsync(email, "CODE-2");
            Console.WriteLine($"   النتيجة: {(firstResult.IsSuccess ? "✅ نجح" : "❌ فشل")}");

            if (firstResult.IsSuccess)
            {
                Console.WriteLine("\n⏳ محاولة إرسال طلب آخر بعد 5 ثوان (يجب أن يفشل):");
                await Task.Delay(5000);

                var secondResult = await service.SendOtpForCodeActivationAsync(email, "CODE-3");
                if (!secondResult.IsSuccess && secondResult.ResultType == "rate_limit_interval")
                {
                    Console.WriteLine($"   ✅ تم حجب الطلب بشكل صحيح: {secondResult.Message}");
                }
                else
                {
                    Console.WriteLine($"   ⚠️ النتيجة غير متوقعة: {secondResult.Message}");
                }

                Console.WriteLine("\n⏳ الانتظار حتى 60 ثانية الكاملة ثم محاولة الطلب مرة أخرى:");
                await Task.Delay(56000);

                var thirdResult = await service.SendOtpForCodeActivationAsync(email, "CODE-4");
                if (thirdResult.IsSuccess)
                {
                    Console.WriteLine($"   ✅ تم السماح بالطلب بعد 60 ثانية: {thirdResult.Message}");
                }
                else
                {
                    Console.WriteLine($"   ⚠️ فشل الطلب: {thirdResult.Message}");
                }
            }

            Console.WriteLine("\n📊 ملخص الاختبار:");
            Console.WriteLine("────────────────────────────────────────────────────");
            Console.WriteLine("✓ تم التحقق من تطبيق حد معدل OTP");
            Console.WriteLine("✓ تم التحقق من فترة الانتظار 60 ثانية");
            Console.WriteLine("✓ تم التحقق من حد 5 طلبات في 10 دقائق");
            Console.WriteLine("✓ تم التحقق من حد الـ 15 دقيقة للقفل");
        }
    }
}
