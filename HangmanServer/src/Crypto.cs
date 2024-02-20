using System;
using System.Security.Cryptography;
using System.Text;

namespace HangmanServer
{
    internal class Crypto
    {
        public static int ITERATIONS = 60000;

        static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static string Encrypt(string clearText, string encryptionKey, string salt)
        {
            try
            {
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, Encoding.UTF8.GetBytes(salt), ITERATIONS, HashAlgorithmName.SHA512);
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        clearText = Convert.ToBase64String(ms.ToArray());
                    }
                }
                return clearText;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine($"CryptographicException: {e.Message}");
                return "";
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"ArgumentException: {e.Message}");
                return "";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                return "";
            }
        }

        public static string Decrypt(string cipherText, string encryptionKey, string salt)
        {
            try
            {
                cipherText = cipherText.Replace(" ", "+");
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, Encoding.UTF8.GetBytes(salt), ITERATIONS, HashAlgorithmName.SHA512);
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
                return cipherText;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine($"CryptographicException: {e.Message}");
                return "";
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"ArgumentException: {e.Message}");
                return "";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                return "";
            }
        }
    }
}
