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
    
    public partial class ClientSPMBillingFrequencyOption
    {
        public int ClientSPMBillingFrequencyOptionID { get; set; }
        public int ClientID { get; set; }
        public string BillingFrequency { get; set; }
        public int GracePeriodDays { get; set; }
        public int NoticeDaysPrior { get; set; }
        public Nullable<int> NumberOfCoupons { get; set; }
        public int SPMInitialGracePeriodDays { get; set; }
        public bool OverrideCustomerDefault { get; set; }
    
        public virtual Client Client { get; set; }
    }
}