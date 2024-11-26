using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Common.Authentication
{
    public static class SecurityManager
    {
        public static string GenerateKey(int size, bool isHex = false)
        {

            char[] chars = isHex ? "abcdef1234567890".ToCharArray() : "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
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

        public static string GenerateHash(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            string hashStr = null;

            using (var hasher = new SHA384CryptoServiceProvider())
            {
                var hash = hasher.ComputeHash(bytes);
                hashStr = Convert.ToBase64String(hash).TrimEnd('=');
            }

            return hashStr;
        }

        public static string Encrypt(string plainText, string password, out string salt, out string iv)
        {
            salt = GenerateKey(32);
            iv = GenerateKey(32, true);

            using (var rijndael = RijndaelManagedInstance(password, salt, iv))
            {

                //iv = Encoding.ASCII.GetString(rijndael.IV, 0, rijndael.IV.Length);

                byte[] strText = new UTF8Encoding().GetBytes(plainText);
                ICryptoTransform transform = rijndael.CreateEncryptor();

                byte[] cipherText = transform.TransformFinalBlock(strText, 0, strText.Length);
                return Convert.ToBase64String(cipherText);
            }

        }

        public static byte[] HexStringToByteArray(string strHex)
        {
            var r = new byte[strHex.Length / 2];
            for (int i = 0; i <= strHex.Length - 1; i += 2)
            {
                r[i / 2] = Convert.ToByte(Convert.ToInt32(strHex.Substring(i, 2), 16));
            }
            return r;
        }

        public static string Decrypt(string encryptedText, string password, string salt, string iv)
        {
            using (var rijndael = RijndaelManagedInstance(password, salt, iv))
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                var decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
                byte[] originalBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                return Encoding.UTF8.GetString(originalBytes);
            }


        }

        private static RijndaelManaged RijndaelManagedInstance(string password, string salt, string iv = null)
        {
            var rijndaelInstance = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 128,
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC,
                Key = GenerateKey(password, salt, 1000)
            };

            if (string.IsNullOrEmpty(iv))
                rijndaelInstance.GenerateIV();
            else
                //rijndaelInstance.IV = Encoding.ASCII.GetBytes(iv);
                rijndaelInstance.IV = HexStringToByteArray(iv);

            return rijndaelInstance;
        }

        private static byte[] GenerateKey(string strPassword, string salt, int iterations)
        {
            var bSalt = Encoding.UTF8.GetBytes(salt);

            using (var rfc2898 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(strPassword), bSalt, iterations))
            {
                return rfc2898.GetBytes(128 / 8);
            }
        }

        public static string EncryptString(string text, string keyString)
        {
            var key = Encoding.UTF8.GetBytes(keyString);

            using (var aesAlg = Aes.Create())
            {
                using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(text);
                        }

                        var iv = aesAlg.IV;

                        var decryptedContent = msEncrypt.ToArray();

                        var result = new byte[iv.Length + decryptedContent.Length];

                        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                        Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

                        return Convert.ToBase64String(result);
                    }
                }
            }
        }

        public static string DecryptString(string cipherText, string keyString)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, fullCipher.Length - iv.Length);

            var key = Encoding.UTF8.GetBytes(keyString);

            using (var aesAlg = Aes.Create())
            {
                using (var decryptor = aesAlg.CreateDecryptor(key, iv))
                {
                    string result;
                    using (var msDecrypt = new MemoryStream(cipher))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                result = srDecrypt.ReadToEnd();
                            }
                        }
                    }

                    return result;
                }
            }
        }

        public static string GenerateToken(string jwtTokenSecret, int tokenExpiryMinutes, string nameIdentifier)
        {
            //var jwt = new JwtSecurityToken(issuer: "Blinkingcaret",
            //    audience: "Everyone",
            //    claims: claims, //the user's claims, for example new Claim[] { new Claim(ClaimTypes.Name, "The username"), //... 
            //    notBefore: DateTime.Now,
            //    expires: DateTime.Now.AddMinutes(5),
            //    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            //);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtTokenSecret));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, nameIdentifier)
                }),
                NotBefore = DateTime.Now,
                Expires = DateTime.Now.AddMinutes(tokenExpiryMinutes),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
