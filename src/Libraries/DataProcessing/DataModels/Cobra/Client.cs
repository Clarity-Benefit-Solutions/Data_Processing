//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataProcessing.DataModels.Cobra
{
    using System;
    using System.Collections.Generic;
    
    public partial class Client
    {
        public int ClientID { get; set; }
        public string ClientName { get; set; }
        public int ClientGroupID { get; set; }
        public string DBAName { get; set; }
        public string EIN { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public Nullable<System.DateTime> BillingStartDate { get; set; }
        public Nullable<int> SendHIPAACertWithQBSRandQBTermination { get; set; }
        public Nullable<int> SendHIPAACertWithWelcomeLetter { get; set; }
        public Nullable<int> AllowClientPortalAccess { get; set; }
        public Nullable<int> AutomaticallyCreateQBSubsidyForInsignificantAmount { get; set; }
        public Nullable<int> IgnoreSPMPaymentGracePeriods { get; set; }
        public Nullable<bool> MigratedClient { get; set; }
        public string ClientPremiumNotice { get; set; }
        public string EmployeeCountRange { get; set; }
        public string WeeklyBillingFirstDayOfWeek { get; set; }
        public Nullable<bool> ClientRequiresAEI2009SubsidyEligibleAttestationToSubsidize { get; set; }
        public Nullable<bool> ClientAllowsAEI2009PlanEnrollmentOptions { get; set; }
        public Nullable<bool> ClientDoesOwnAEI2009SubsidyTakenReportPosting { get; set; }
        public string ClientAlternate { get; set; }
        public Nullable<int> AllowClientSSO { get; set; }
        public Nullable<int> SPMInitialGracePeriodDays { get; set; }
        public Nullable<decimal> SPMConvenienceFee { get; set; }
        public Nullable<decimal> SPMPartnerConvenienceFee { get; set; }
        public Nullable<decimal> QBConvenienceFee { get; set; }
        public Nullable<decimal> QBPartnerConvenienceFee { get; set; }
    }
}
