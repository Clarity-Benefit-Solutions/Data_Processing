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
    
    public partial class QBPremium
    {
        public int QBPremiumID { get; set; }
        public int MemberID { get; set; }
        public Nullable<System.DateTime> NextPaymentDueDate { get; set; }
        public Nullable<System.DateTime> GracePeriodEndDate { get; set; }
        public Nullable<decimal> AmountDue { get; set; }
        public Nullable<decimal> UnallocatedAmount { get; set; }
        public Nullable<decimal> MemberOwes { get; set; }
    
        public virtual QB QB { get; set; }
    }
}
