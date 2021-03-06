using System.Collections.Generic;
using CoreUtils.Classes;

namespace DataProcessing
{
    /// <inheritdoc />
    public class FileCheckResults : Dictionary<int, string>
    {
        internal bool MarkAsCompleteFail = false;

        public bool HasErrors
        {
            get { return this.Count > 0; }
        }

        public bool IsCompleteFail
        {
            get { return this.MarkAsCompleteFail; }
        }

        public OperationResultType OperationResultType
        {
            get
            {
                if (this.IsCompleteFail)
                {
                    return OperationResultType.CompleteFail;
                }

                if (this.HasErrors)
                {
                    return OperationResultType.PartialFail;
                }

                return OperationResultType.Ok;
            }
        }
    }
}