using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TechSolutions.Helpers
{
    public static class EncryptionHelper
    {
        /*Important Note about my choice here, in production the way to do this would be rotating hashed strings to ensure the security of the encryption on the system but for this assessment I just hardcoded my keys*/
        private static readonly string EncryptionKey = ConfigurationManager.AppSettings["EncryptionKey"];
        private static readonly string IV = ConfigurationManager.AppSettings["EncryptionIV"];

        #region String Encryption (for ID numbers, passport numbers, etc.)

        /// <summary>
        /// Encrypt a string value
        /// </summary>
        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                    aes.IV = Encoding.UTF8.GetBytes(IV);

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                            return Convert.ToBase64String(msEncrypt.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error encrypting data: " + ex.Message);
            }
        }

        /// <summary>
        /// Decrypt a string value
        /// </summary>
        public static string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                    aes.IV = Encoding.UTF8.GetBytes(IV);

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error decrypting data: " + ex.Message);
            }
        }

        #endregion

        #region File Encryption (for document uploads)

        /// <summary>
        /// Encrypt a file and save to disk
        /// </summary>
        public static void EncryptFile(string inputFilePath, string outputFilePath)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                    aes.IV = Encoding.UTF8.GetBytes(IV);

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (FileStream fsInput = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                    {
                        using (FileStream fsOutput = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                        {
                            using (CryptoStream cs = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write))
                            {
                                fsInput.CopyTo(cs);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error encrypting file: " + ex.Message);
            }
        }

        /// <summary>
        /// Decrypt a file and return as byte array
        /// </summary>
        public static byte[] DecryptFile(string encryptedFilePath)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                    aes.IV = Encoding.UTF8.GetBytes(IV);

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (FileStream fsInput = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read))
                    {
                        using (MemoryStream msOutput = new MemoryStream())
                        {
                            using (CryptoStream cs = new CryptoStream(fsInput, decryptor, CryptoStreamMode.Read))
                            {
                                cs.CopyTo(msOutput);
                            }
                            return msOutput.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error decrypting file: " + ex.Message);
            }
        }

        /// <summary>
        /// Decrypt file and save to output path
        /// </summary>
        public static void DecryptFileToPath(string encryptedFilePath, string outputFilePath)
        {
            try
            {
                byte[] decryptedBytes = DecryptFile(encryptedFilePath);
                System.IO.File.WriteAllBytes(outputFilePath, decryptedBytes);
            }
            catch (Exception ex)
            {
                throw new Exception("Error decrypting file to path: " + ex.Message);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Mask a sensitive string for display (show last 4 characters only)
        /// </summary>
        public static string MaskSensitiveData(string data, int visibleChars = 4)
        {
            if (string.IsNullOrEmpty(data))
                return "";

            if (data.Length <= visibleChars)
                return new string('*', data.Length);

            return new string('*', data.Length - visibleChars) + data.Substring(data.Length - visibleChars);
        }

        #endregion
    }
}