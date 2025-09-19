using System.Buffers.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace Intellimix_Template.utils
{
  

    public static class  SimpleEncryption
    {
        public static string Decrypt(string encryptedText, string key="")
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }
            if (string.IsNullOrEmpty(key))
            {
                key = "your-encryption-key-32-char-long";
            }

            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            // 2. XOR with key
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                encryptedBytes[i] ^= keyBytes[i % keyBytes.Length];
            }

            // 3. Convert bytes to string (plaintext)
            return Encoding.UTF8.GetString(encryptedBytes);
        }

        public static string Encrypt(string plainText, string key = "")
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }
            if (string.IsNullOrEmpty(key))
            {
                key = "your-encryption-key-32-char-long";
            }
            // 1. Convert plaintext to bytes
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            // 2. XOR with key
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < plainBytes.Length; i++)
            {
                plainBytes[i] ^= keyBytes[i % keyBytes.Length];
            }
            // 3. Convert bytes to Base64 string
            return Convert.ToBase64String(plainBytes);
        }


    }

    public static class UtilityFunctions
    {
        public static bool CheckEmailid(string? email)
        {
            const string ValidEmailAddressPattern = "^[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,6}$";
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            var regex = new Regex(ValidEmailAddressPattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(email);

        }

        public static bool ValidatePassword(string? password)
        {
            const string Validpassword = "^(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$";
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var regex = new Regex(Validpassword, RegexOptions.IgnoreCase);
            return regex.IsMatch(password);

        }
    }


}
