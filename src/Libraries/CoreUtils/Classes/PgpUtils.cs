using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreUtils.Classes
{
  public  class PgpUtils
    {
        public static void PgpDecryptFile(string srcFilePath, string destFilePath, string[] keyDetails, SingleFileCallback fileCallback,
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

                if (keyDetails is null || keyDetails.Length == 0)
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : keyDetails should be set";
                    throw new Exception(message);
                }



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
