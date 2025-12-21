using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MacroApp.Services
{
    public class EmailService
    {
        private const string SmtpHost = "smtp.zoho.sa";
        private const int SmtpPort = 587;
        private const string SenderEmail = "noreply@sr3h.com";
        private const string SenderPassword = "VRp778pe4fNd";
        private const string SenderName = "SR3H MACRO";

        public async Task<bool> SendVerificationCodeAsync(string recipientEmail, string verificationCode)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpHost, SmtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(SenderEmail, SenderPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Timeout = 30000;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(SenderEmail, SenderName),
                        Subject = "كود التحقق - SR3H MACRO",
                        Body = GenerateEmailBody(verificationCode),
                        IsBodyHtml = true,
                        Priority = MailPriority.High
                    };

                    mailMessage.To.Add(recipientEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email sending error: {ex}");
                return false;
            }
        }

        private string GenerateEmailBody(string verificationCode)
        {
            return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 20px;
            direction: rtl;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #2196F3 0%, #1976D2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: bold;
        }}
        .content {{
            padding: 40px 30px;
            text-align: center;
        }}
        .verification-code {{
            background-color: #f8f9fa;
            border: 2px dashed #2196F3;
            border-radius: 8px;
            padding: 20px;
            margin: 30px 0;
            font-size: 36px;
            font-weight: bold;
            color: #2196F3;
            letter-spacing: 8px;
            font-family: 'Courier New', monospace;
        }}
        .message {{
            font-size: 16px;
            color: #333;
            line-height: 1.6;
            margin: 20px 0;
            text-align: right;
        }}
        .warning {{
            background-color: #fff3cd;
            border-right: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 5px;
            text-align: right;
        }}
        .warning p {{
            margin: 5px 0;
            color: #856404;
            font-size: 14px;
            text-align: right;
        }}
        .affiliate-section {{
            background-color: #f5f5f5;
            padding: 25px 30px;
            text-align: right;
            border-top: 1px solid #e0e0e0;
            border-bottom: 1px solid #e0e0e0;
        }}
        .affiliate-title {{
            font-size: 15px;
            color: #1565C0;
            margin-bottom: 20px;
            font-weight: bold;
            text-align: right;
        }}
        .affiliate-option {{
            background-color: white;
            padding: 15px;
            margin-bottom: 15px;
            border-radius: 5px;
            text-align: right;
            border-right: 4px solid #2196F3;
        }}
        .affiliate-option.best {{
            border-right-color: #FFB300;
            background-color: #fffbf0;
        }}
        .affiliate-badge {{
            display: inline-block;
            background-color: #FFB300;
            color: white;
            padding: 4px 10px;
            border-radius: 3px;
            font-size: 12px;
            font-weight: bold;
            margin-right: 8px;
            margin-left: 0;
        }}
        .affiliate-option-title {{
            font-size: 14px;
            font-weight: bold;
            color: #333;
            margin-bottom: 8px;
            text-align: right;
        }}
        .affiliate-option-desc {{
            font-size: 13px;
            color: #666;
            margin-bottom: 10px;
            text-align: right;
            line-height: 1.5;
        }}
        .affiliate-links {{
            display: flex;
            justify-content: flex-end;
            gap: 8px;
            flex-wrap: wrap;
        }}
        .affiliate-btn {{
            display: inline-block;
            padding: 8px 12px;
            margin: 3px 0;
            text-decoration: none;
            border-radius: 4px;
            font-size: 12px;
            font-weight: bold;
            color: #ffffff !important;
            text-align: center;
            transition: all 0.3s;
        }}
        .affiliate-btn-video {{
            background-color: #FF0000;
        }}
        .affiliate-btn-video:hover {{
            background-color: #CC0000;
        }}
        .affiliate-btn-join {{
            background-color: #2196F3;
        }}
        .affiliate-btn-join:hover {{
            background-color: #1976D2;
        }}
        .social-section {{
            background-color: #f0f8ff;
            padding: 25px 30px;
            text-align: center;
            border-top: 1px solid #e0e0e0;
        }}
        .social-title {{
            font-size: 14px;
            color: #333;
            margin-bottom: 15px;
            font-weight: bold;
        }}
        .social-links {{
            display: flex;
            justify-content: center;
            gap: 10px;
            flex-wrap: wrap;
        }}
        .social-btn {{
            display: inline-block;
            padding: 10px 15px;
            margin: 5px 3px;
            text-decoration: none;
            border-radius: 5px;
            font-size: 13px;
            font-weight: bold;
            color: #ffffff !important;
            text-align: center;
            transition: all 0.3s;
        }}
        .btn-website {{
            background-color: #2196F3;
        }}
        .btn-website:hover {{
            background-color: #1976D2;
        }}
        .btn-youtube {{
            background-color: #FF0000;
        }}
        .btn-youtube:hover {{
            background-color: #CC0000;
        }}
        .btn-tiktok {{
            background-color: #000000;
        }}
        .btn-tiktok:hover {{
            background-color: #333333;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            color: #666;
            font-size: 14px;
        }}
        .footer p {{
            margin: 5px 0;
        }}
        .footer a {{
            color: #2196F3;
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 SR3H MACRO</h1>
            <p style='margin: 10px 0 0 0; font-size: 16px;'>كود التحقق من التفعيل</p>
        </div>
        
        <div class='content'>
            <p class='message'>
                <span>مرحباً بك في </span><span style='direction: ltr; unicode-bidi: bidi-override;'>SR3H MACRO</span><span>!</span>
            </p>
            
            <p class='message'>
                لإتمام عملية التفعيل، يرجى إدخال كود التحقق التالي في التطبيق:
            </p>
            
            <div class='verification-code'>
                {verificationCode}
            </div>
            
            <div class='warning'>
                <p><strong>⚠️ تنبيهات مهمة:</strong></p>
                <p>• كود التحقق صالح لمدة 10 دقائق فقط</p>
                <p>• لا تشارك هذا الكود مع أي شخص</p>
                <p>• إذا لم تطلب هذا الكود، يرجى تجاهل هذه الرسالة</p>
            </div>
            
            <p class='message' style='margin-top: 30px; font-size: 14px; color: #666;'>
                إذا واجهت أي مشكلة، يرجى التواصل مع الدعم الفني
            </p>
        </div>
        
        <div class='affiliate-section'>
            <p class='affiliate-title'>التسويق بالعمولة – اربح من مشاركاتك</p>
            
            <div class='affiliate-option best'>
                <p class='affiliate-option-title'>
                    <span class='affiliate-badge'>الأفضل</span>شركاء سلة
                </p>
                <p class='affiliate-option-desc'>تحصل على عمولتك كاملة (30% = 30%)</p>
                <p class='affiliate-option-desc'>رابط تسويق خاص + تطبيق الكود تلقائياً للعميل</p>
                <div class='affiliate-links'>
                    <a href='https://youtu.be/uiAUnG4cRis' class='affiliate-btn affiliate-btn-video'>▶️ شاهد الشرح</a>
                    <a href='https://portal.salla.partners/affiliate/marketplaces/stores/54209750' class='affiliate-btn affiliate-btn-join'>🔗 الانضمام الآن</a>
                </div>
            </div>
            
            <div class='affiliate-option'>
                <p class='affiliate-option-title'>منصة كودماب</p>
                <p class='affiliate-option-desc'>عند تخصيص 30% للمسوق، يصل للمسوق 21%</p>
                <p class='affiliate-option-desc'>كودماب تخصم 30% من عمولة المسوق</p>
                <div class='affiliate-links'>
                    <a href='https://youtu.be/VzOXRS7f-Jc' class='affiliate-btn affiliate-btn-video'>▶️ شاهد الشرح</a>
                    <a href='https://codemap.me/m/coupon/3132' class='affiliate-btn affiliate-btn-join'>🔗 الانضمام الآن</a>
                </div>
            </div>
        </div>
        
        <div class='social-section'>
            <p class='social-title'>تابعنا على حساباتنا الرسمية</p>
            <div class='social-links'>
                <a href='https://sr3h.com/' class='social-btn btn-website'>🌐 منصة سرعة</a>
                <a href='https://www.youtube.com/@sr3hcom' class='social-btn btn-youtube'>📺 اليوتيوب</a>
                <a href='https://www.tiktok.com/@sr3hcom' class='social-btn btn-tiktok'>🎵 تيك توك</a>
            </div>
        </div>
        
        <div class='footer'>
            <p>SR3H MACRO - نظام الماكرو الاحترافي</p>
            <p><a href='https://sr3h.com/'>www.SR3H.com</a></p>
            <p style='margin-top: 10px; font-size: 12px; color: #999;'>
                هذه رسالة تلقائية، يرجى عدم الرد عليها
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
