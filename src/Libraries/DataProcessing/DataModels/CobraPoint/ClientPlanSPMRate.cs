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
    
    public partial class ClientPlanSPMRate
    {
        public int ClientPlanSPMRateID { get; set; }
        public int ClientPlanSPMID { get; set; }
        public System.DateTime EffectiveDate { get; set; }
        public Nullable<System.DateTime> BillingDate { get; set; }
        public Nullable<System.DateTime> RenewalDate { get; set; }
        public Nullable<System.DateTime> EndDate { get; set; }
        public string SPMCoverageLevel { get; set; }
        public Nullable<decimal> Rate { get; set; }
        public Nullable<decimal> RemittableAdminFeeFlatAmount { get; set; }
        public Nullable<decimal> RemittableAdminFeePercentOfPremium { get; set; }
        public Nullable<bool> IsRemittableAdminFeeFlatAmountProratable { get; set; }
        public Nullable<decimal> BookableAdminFeePercentOfPremium { get; set; }
        public Nullable<decimal> BookableAdminFeeFlatAmount { get; set; }
        public Nullable<bool> IsBookableAdminFeeFlatAmountProratable { get; set; }
        public string IndividualType { get; set; }
        public Nullable<int> MemberID { get; set; }
        public Nullable<bool> PlanRateHold { get; set; }
    
        public virtual ClientPlanSPM ClientPlanSPM { get; set; }
    }
}
