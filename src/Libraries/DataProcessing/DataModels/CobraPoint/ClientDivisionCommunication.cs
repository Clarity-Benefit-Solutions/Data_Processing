//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataProcessing.DataModels.CobraPoint
{
    using System;
    using System.Collections.Generic;
    
    public partial class ClientDivisionCommunication
    {
        public int ClientDivisionCommunicationID { get; set; }
        public int ClientDivisionContactID { get; set; }
        public Nullable<System.DateTime> DateTime { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public int PageCount { get; set; }
        public bool LetterSent { get; set; }
        public Nullable<int> LetterAttachmentID { get; set; }
    
        public virtual ClientDivisionContact ClientDivisionContact { get; set; }
        public virtual LetterAttachment LetterAttachment { get; set; }
    }
}
