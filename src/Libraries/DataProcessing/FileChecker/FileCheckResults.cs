using System;
using System.Collections.Generic;
using CoreUtils.Classes;

namespace DataProcessing
{
    /// <inheritdoc />
    public class FileCheckResults : Dictionary<int, string>
    {
        internal Boolean MarkAsCompleteFail = false;

        public FileCheckResults() : base()
        {
        }

        public Boolean HasErrors
        {
            get { return this.Count > 0; }
        }

        public Boolean IsCompleteFail
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
                else if (this.HasErrors)
                {
                    return OperationResultType.PartialFail;
                }
                else
                {
                    return OperationResultType.Ok;
                }
            }
        }
    }
}