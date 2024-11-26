using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Common.Security
{
    public static class SecurityHelper
    {
        public static string ComputeSHA1Hash(string inputValue)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(inputValue);

            var hashString = string.Empty;

            using (SHA1Managed sha1 = new SHA1Managed())
            {
                hashString = Convert.ToBase64String(sha1.ComputeHash(bytes));
            }

            return hashString;
        }

        public static string GetSHA1FileHash(string filePath)
        {
            using (var sha256 = new SHA256CryptoServiceProvider())
                return GetHash(filePath, sha256);
        }

        public static string ShortGuid()
        {
            Random rn = new Random();
            string charsToUse = "AzByCxDwEvFuGtHsIrJqKpLoMnNmOlPkQjRiShTgUfVeWdXcYbZa1234567890";

            MatchEvaluator RandomChar = delegate (Match m)
            {
                return charsToUse[rn.Next(charsToUse.Length)].ToString();
            };

            return Regex.Replace("XXXXXXXXXXXXXXXXXXXX", "X", RandomChar);
        }

		public static string GeneratePassword(int size)
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[size];

            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }

            StringBuilder result = new StringBuilder(size);

            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }

            return result.ToString();
        }
		
        public static int GenerateRandomNumber(int minValue, int maxValue)
        {
            // We will make up an integer seed from 4 bytes of this array.
            byte[] randomBytes = new byte[4];

            // Generate 4 random bytes.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(randomBytes);

            // Convert four random bytes into a positive integer value.
            int seed = ((randomBytes[0] & 0x7f) << 24) |
                        (randomBytes[1] << 16) |
                        (randomBytes[2] << 8) |
                        (randomBytes[3]);

            // Now, this looks more like real randomization.
            Random random = new Random(seed);

            // Calculate a random number.
            return random.Next(minValue, maxValue + 1);
        }

        private static string GetHash(string filePath, HashAlgorithm hasher)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return GetHash(fs, hasher);
        }

        private static string GetHash(Stream s, HashAlgorithm hasher)
        {
            var hash = hasher.ComputeHash(s);
            var hashStr = Convert.ToBase64String(hash);
            return hashStr.TrimEnd('=');
        }

        public static string GetMacAddress()
        {
            string macAddresses = "";
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only consider Ethernet network interfaces, thereby ignoring any
                // loopback devices etc.
                if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue;
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    macAddresses += nic.GetPhysicalAddress().ToString();
                    break;
                }
            }
            return macAddresses;
        }

        public static string GetUniqueKey(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        public static string EncryptText(string encryptionKey, string dataToEncrypt)
        {
            var rijndaelKey = new RijndaelEnhanced(encryptionKey);
            var encryptedData = rijndaelKey.Encrypt(dataToEncrypt);

            var x = EmbedKey(encryptedData, encryptionKey);
            return x;
        }

        public static string DecryptText(string encryptedText)
        {
            var firstHalf = (encryptedText.Length - 25) / 2;
            var revEncData = encryptedText.Substring(0, firstHalf) + encryptedText.Substring(firstHalf + 25);
            var revKey = encryptedText.Substring(firstHalf, 25);

            var encryptedData = Reverse(revEncData);

            var rijndaelKey = new RijndaelEnhanced(Reverse(revKey));
            var decryptedData = rijndaelKey.Decrypt(encryptedData);

            return decryptedData;
        }

        private static string EmbedKey(string encryptedData, string privateKey)
        {
            var revkey = Reverse(privateKey);

            var revEncData = Reverse(encryptedData);

            var firstHalf = (int)Math.Round((double)(revEncData.Length) / 2);
            var secondHalf = revEncData.Length - firstHalf;

            var key = revEncData.Substring(0, firstHalf) + revkey + revEncData.Substring(firstHalf, secondHalf);

            return key;
        }

        private static string ExtractKey(string encryptedText, int keyLength)
        {
            var firstHalf = (encryptedText.Length - keyLength) / 2;
            var revKey = encryptedText.Substring(firstHalf, keyLength);

            return Reverse(revKey);
        }

        public static string ExtractData(string encryptedText, int keyLength)
        {
            var firstHalf = (encryptedText.Length - keyLength) / 2;
            var revEncData = encryptedText.Substring(0, firstHalf) + encryptedText.Substring(firstHalf + keyLength);

            return Reverse(revEncData);
        }

        static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private const int DerivationIterations = 1000;
        private const int saltBytes = 32; //  bytes
        private const int ivBytes = 16; // bytes

        public static string Encrypt(string plainText, string passPhrase)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            byte[] saltStringBytes = GenerateBitsOfRandomEntropy(32);
            byte[] ivStringBytes = GenerateBitsOfRandomEntropy(16);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                byte[] keyBytes = password.GetBytes(saltBytes);
                using (var symmetricKey = new AesCryptoServiceProvider())
                {
                    symmetricKey.BlockSize = 128;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();

                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                byte[] cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [16 bytes of IV] + [n bytes of CipherText]
            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            byte[] saltStringBytes = cipherTextBytesWithSaltAndIv.Take(saltBytes).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            byte[] ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(saltBytes).Take(ivBytes).ToArray();
            // Get the actual cipher text bytes by removing the first 48 bytes from the cipherText string.
            byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(saltBytes + ivBytes).Take(cipherTextBytesWithSaltAndIv.Length - (saltBytes + ivBytes)).ToArray();

            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                byte[] keyBytes = password.GetBytes(saltBytes);

                using (var symmetricKey = new AesCryptoServiceProvider())
                {
                    symmetricKey.BlockSize = 128;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    using (var memoryStream = new MemoryStream(cipherTextBytes))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        var plainTextBytes = new byte[cipherTextBytes.Length];
                        int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memoryStream.Close();
                        cryptoStream.Close();
                        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                    }
                }
            }
        }

        private static byte[] GenerateBitsOfRandomEntropy(int num)
        {
            var randomBytes = new byte[num]; // 32 Bytes will give us 256 bits.

            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }

            return randomBytes;
        }

        public static string EncryptWithEmbedKey(string encryptionKey, string dataToEncrypt) 
        {
            var encryptedData = Encrypt(dataToEncrypt, encryptionKey);
            return EmbedKey(encryptedData, encryptionKey);
        }

        public static string DecryptWithEmbedKey(string encryptedText, int keyLength) 
        {
            var key = ExtractKey(encryptedText, keyLength);
            var data = ExtractData(encryptedText, keyLength);

            return Decrypt(data, key);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return HttpUtility.UrlEncode(Convert.ToBase64String(plainTextBytes));
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(HttpUtility.UrlDecode(base64EncodedData));
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
