using System.Security.Cryptography;
using System.Text;

namespace EliteJobs.App.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration configuration)
        {
            // Ключ должен быть 32 байта для AES-256
            var encryptionKey = configuration["Encryption:Key"]
                ?? Environment.GetEnvironmentVariable("Encryption__Key")
                ?? "DefaultDevKey1234567890123456789012"; // 32 символа для разработки

            _key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
            _iv = Encoding.UTF8.GetBytes(encryptionKey.PadRight(16).Substring(0, 16));
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(cipherBytes);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Если расшифровка не удалась, возвращаем зашифрованный текст как есть
                return "[Зашифрованное сообщение]";
            }
        }
    }
}