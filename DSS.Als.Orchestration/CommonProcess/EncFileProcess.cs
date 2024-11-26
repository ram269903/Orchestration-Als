using Serilog;
using System;

namespace DSS.Orchestration.CommonProcess
{
    public static class EncFileProcess 
    {
       
        public static bool EncryptAFile(string sourcefilepath, string destinationfilepath, string key)
        {
            try
            {
                Log.Information($"Encryption initiated for source file: {sourcefilepath} destination path: {destinationfilepath} key path: {key}");
                #region PGPEncryption
                Encrypt encryptAFP = new Encrypt();
                var pkr = encryptAFP.asciiPublicKeyToRing(key);
                Encrypt.EncryptFile(sourcefilepath, destinationfilepath, encryptAFP.getFirstPublicEncryptionKeyFromRing(pkr), true, true);
                return true;
                #endregion
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Error while file encryption.");
                return false;
            }
        }

    }
}
