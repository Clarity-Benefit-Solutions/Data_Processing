using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/*
 * 
 * 
 * PGPEncryptDecrypt.EncryptFile(inputFileName, 
                              outputFileName,
                              recipientKeyFileName,
                              shouldArmor,
                              shouldCheckIntegrity);
Decrypt a file:



 * */
namespace CoreUtils.Classes
{

    public class PgpUtils
    {
        public static void PgpDecryptFile(string srcFilePath, string destFilePath, string privateKeyFileName, string passPhrase, SingleFileCallback fileCallback,
          OnErrorCallback onErrorCallback)
        {
            try
            {
                if (Utils.IsBlank(srcFilePath))
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : srcFilePath should be set";
                    throw new Exception(message);
                }
                if (Utils.IsBlank(destFilePath))
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : destFilePath should be set";
                    throw new Exception(message);
                }

                if (privateKeyFileName is null || privateKeyFileName.Length == 0)
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : privateKeyFileName should be set";
                    throw new Exception(message);
                }


                PGPEncryptDecrypt.Decrypt(srcFilePath, privateKeyFileName, passPhrase, destFilePath);

                // callback on complete
                fileCallback(srcFilePath, destFilePath, "");
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(srcFilePath, destFilePath, ex);
                else
                    throw;
            }
        }


        public static void PgpEncryptFile(string srcFilePath, string destFilePath, string recipientKeyFileName, bool shouldArmor, bool shouldCheckIntegrity, SingleFileCallback fileCallback,
          OnErrorCallback onErrorCallback)
        {
            try
            {
                if (Utils.IsBlank(srcFilePath))
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : srcFilePath should be set";
                    throw new Exception(message);
                }
                if (Utils.IsBlank(destFilePath))
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : destFilePath should be set";
                    throw new Exception(message);
                }

                if (recipientKeyFileName is null || recipientKeyFileName.Length == 0)
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : recipientKeyFileName should be set";
                    throw new Exception(message);
                }


                PGPEncryptDecrypt.EncryptFile(srcFilePath,
                                  destFilePath,
                                  recipientKeyFileName,
                                  shouldArmor,
                                  shouldCheckIntegrity);

                // callback on complete
                fileCallback(srcFilePath, destFilePath, "");
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(srcFilePath, destFilePath, ex);
                else
                    throw;
            }
        }

    }

}