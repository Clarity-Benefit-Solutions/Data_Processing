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
    
    public partial class ClientPlanQBRate
    {
        public int ClientPlanQBRateID { get; set; }
        public string RateType { get; set; }
        public int ClientPlanQBID { get; set; }
        public System.DateTime EffectiveDate { get; set; }
        public Nullable<System.DateTime> BillingDate { get; set; }
        public Nullable<System.DateTime> RenewalDate { get; set; }
        public Nullable<System.DateTime> EndDate { get; set; }
        public string QBPremiumAdminFee { get; set; }
        public string QBDisabilityFee { get; set; }
        public string QBCoverageLevel { get; set; }
        public Nullable<int> StartAge { get; set; }
        public Nullable<int> EndAge { get; set; }
        public Nullable<int> NumberOfChildren { get; set; }
        public string IndividualType { get; set; }
        public decimal Rate { get; set; }
        public Nullable<int> MemberID { get; set; }
        public Nullable<bool> PlanRateHold { get; set; }
    
        public virtual ClientPlanQB ClientPlanQB { get; set; }
    }
}
