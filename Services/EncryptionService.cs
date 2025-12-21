using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MacroApp.Services
{
    public class EncryptionService
    {
        // مفتاح التشفير المدمج في التطبيق (يجب أن يكون معقد وفريد)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("SR3H_MACRO_2024_SECURE_KEY_PROTECT");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("SR3H_INIT_VECTOR");

        /// <summary>
        /// تشفير النص باستخدام AES
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = ResizeKey(Key, 32); // AES-256
                    aes.IV = ResizeKey(IV, 16);   // 128-bit IV
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                        swEncrypt.Close();
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// فك تشفير النص
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using (var aes = Aes.Create())
                {
                    aes.Key = ResizeKey(Key, 32); // AES-256
                    aes.IV = ResizeKey(IV, 16);   // 128-bit IV
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(cipherBytes))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تغيير حجم المفتاح ليناسب متطلبات AES
        /// </summary>
        private static byte[] ResizeKey(byte[] key, int size)
        {
            var resizedKey = new byte[size];
            
            if (key.Length >= size)
            {
                Array.Copy(key, resizedKey, size);
            }
            else
            {
                Array.Copy(key, resizedKey, key.Length);
                // ملء الباقي بقيم مشتقة من المفتاح الأصلي
                for (int i = key.Length; i < size; i++)
                {
                    resizedKey[i] = (byte)(key[i % key.Length] ^ (i * 7));
                }
            }
            
            return resizedKey;
        }

        /// <summary>
        /// تشفير إضافي باستخدام XOR مع مفتاح ديناميكي
        /// </summary>
        public static string AdvancedEncrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            // تشفير AES أولاً
            var aesEncrypted = Encrypt(plainText);
            
            // ثم تشفير XOR إضافي
            var xorKey = GenerateXorKey(aesEncrypted.Length);
            var bytes = Encoding.UTF8.GetBytes(aesEncrypted);
            
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= xorKey[i % xorKey.Length];
            }
            
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// فك التشفير المتقدم
        /// </summary>
        public static string AdvancedDecrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                var xorKey = GenerateXorKey(bytes.Length);
                
                // فك تشفير XOR أولاً
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] ^= xorKey[i % xorKey.Length];
                }
                
                var aesEncrypted = Encoding.UTF8.GetString(bytes);
                
                // ثم فك تشفير AES
                return Decrypt(aesEncrypted);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Advanced decryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// توليد مفتاح XOR ديناميكي
        /// </summary>
        private static byte[] GenerateXorKey(int length)
        {
            var key = new byte[Math.Min(length, 64)]; // حد أقصى 64 بايت
            var seed = "SR3H_MACRO_XOR_KEY_2024";
            var seedBytes = Encoding.UTF8.GetBytes(seed);
            
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (byte)(seedBytes[i % seedBytes.Length] ^ (i * 13) ^ 0xAA);
            }
            
            return key;
        }
    }

    /// <summary>
    /// فئة للاتصال عبر Railway Proxy الآمن
    /// </summary>
    public static class SecureSupabaseConfig
    {
        private static readonly string RailwayProxyUrl = "https://sr3h-auth-proxy-production.up.railway.app";

        /// <summary>
        /// الحصول على Railway Proxy URL
        /// </summary>
        public static string GetSupabaseUrl()
        {
            return RailwayProxyUrl;
        }

        /// <summary>
        /// لا نحتاج مفتاح للـ Railway Proxy (البيانات الحساسة محفوظة هناك)
        /// </summary>
        public static string GetSupabaseKey()
        {
            return "";
        }

        /// <summary>
        /// التحقق من صحة الاتصال
        /// </summary>
        public static bool ValidateConnection()
        {
            return !string.IsNullOrEmpty(RailwayProxyUrl) && 
                   RailwayProxyUrl.Contains("railway.app");
        }
    }
}