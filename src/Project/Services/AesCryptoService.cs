using System.Security.Cryptography;
using System.Text;

namespace TuringMachinesAPI.Services
{
    public class AesCryptoService : ICryptoService
    {
        private readonly string _key;
        private readonly string _salt;
        public AesCryptoService(IConfiguration configuration)
        {
            string? key = configuration["Crypto:Key"];
            string? salt = configuration["Crypto:Salt"];

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(salt))
            {
                throw new ArgumentNullException("Crypto key or salt is not configured properly.");
            }
            _key = key;
            _salt = salt;
        }

        string? ICryptoService.Encrypt(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                using var aes = Aes.Create();
                using var keyDerivation = new Rfc2898DeriveBytes(_key, Encoding.UTF8.GetBytes(_salt), 10000, HashAlgorithmName.SHA256);

                aes.Key = keyDerivation.GetBytes(32);
                aes.IV = keyDerivation.GetBytes(16);

                using var encryptor = aes.CreateEncryptor();
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(value);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error encrypting value.", ex);
            }
        }

        string? ICryptoService.Decrypt(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                using var aes = Aes.Create();
                using var keyDerivation = new Rfc2898DeriveBytes(_key, Encoding.UTF8.GetBytes(_salt), 10000, HashAlgorithmName.SHA256);

                aes.Key = keyDerivation.GetBytes(32);
                aes.IV = keyDerivation.GetBytes(16);

                var buffer = Convert.FromBase64String(value);

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error decrypting value.", ex);
            }
        }
    }
}
