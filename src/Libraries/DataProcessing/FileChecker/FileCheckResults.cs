using System.Collections.Generic;
using CoreUtils.Classes;

namespace DataProcessing
{

    /// <inheritdoc />
    public class FileCheckResults : Dictionary<int, string>
    {
        internal bool MarkAsCompleteFail = false;

        public bool HasErrors => Count > 0;

        public bool IsCompleteFail => MarkAsCompleteFail;

        public OperationResultType OperationResultType
        {
            get
            {
                if (IsCompleteFail)
                    return OperationResultType.CompleteFail;
                if (HasErrors)
                    return OperationResultType.PartialFail;
                return OperationResultType.Ok;
            }
        }
    }

}