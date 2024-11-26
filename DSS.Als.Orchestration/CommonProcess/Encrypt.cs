using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Security;
using System;
using System.IO;

namespace DSS.Orchestration.CommonProcess
{
    public class Encrypt
    {
        public PgpPublicKeyRing asciiPublicKeyToRing(string ascfilein)
        {
            using Stream input = File.OpenRead(ascfilein);
            ArmoredInputStream inputStream = new ArmoredInputStream(input);
            PgpObjectFactory pgpObjectFactory = new PgpObjectFactory(inputStream);
            object obj = pgpObjectFactory.NextPgpObject();
            return obj as PgpPublicKeyRing;
        }

        public PgpPublicKey getFirstPublicEncryptionKeyFromRing(PgpPublicKeyRing pkr)
        {
            foreach (PgpPublicKey publicKey in pkr.GetPublicKeys())
            {
                if (publicKey.IsEncryptionKey)
                {
                    return publicKey;
                }
            }

            throw new ArgumentException("Can't find encryption key in key ring.");
        }

        public static void EncryptFile(string inputFile, string outputFile, PgpPublicKey encKey, bool armor, bool withIntegrityCheck)
        {
            using MemoryStream memoryStream = new MemoryStream();
            PgpCompressedDataGenerator pgpCompressedDataGenerator = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
            PgpUtilities.WriteFileToLiteralData(pgpCompressedDataGenerator.Open(memoryStream), 'b', new FileInfo(inputFile));
            pgpCompressedDataGenerator.Close();
            PgpEncryptedDataGenerator pgpEncryptedDataGenerator = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, withIntegrityCheck, new SecureRandom());
            pgpEncryptedDataGenerator.AddMethod(encKey);
            byte[] array = memoryStream.ToArray();
            using Stream stream = File.Create(outputFile);
            if (armor)
            {
                using ArmoredOutputStream outStr = new ArmoredOutputStream(stream);
                using Stream stream2 = pgpEncryptedDataGenerator.Open(outStr, array.Length);
                stream2.Write(array, 0, array.Length);
            }
            else
            {
                using Stream stream3 = pgpEncryptedDataGenerator.Open(stream, array.Length);
                stream3.Write(array, 0, array.Length);
            }
        }
    }
}
