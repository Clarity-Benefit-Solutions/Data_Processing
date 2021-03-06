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
    
    public partial class QBLegacy
    {
        public int QBLegacyID { get; set; }
        public int MemberID { get; set; }
        public Nullable<System.DateTime> DateSRPrinted { get; set; }
        public Nullable<bool> HasElected { get; set; }
        public Nullable<System.DateTime> PostmarkDateOfElection { get; set; }
        public Nullable<bool> IsPaidThroughLDOC { get; set; }
        public Nullable<System.DateTime> NextPremiumMonthOwed { get; set; }
        public Nullable<bool> DisabilityApproved { get; set; }
        public Nullable<System.DateTime> PostmarkOfDisabilityNotification { get; set; }
        public Nullable<System.DateTime> DateDisabled { get; set; }
        public Nullable<bool> ConversionLetterPreviouslySent { get; set; }
        public Nullable<bool> IsAEILegacy { get; set; }
        public Nullable<System.DateTime> DateAEINotificationPrinted { get; set; }
        public Nullable<System.DateTime> StartDateOfAEISubsidy { get; set; }
        public Nullable<bool> HasAEI20092ndElectionSRBeenPrinted { get; set; }
        public Nullable<System.DateTime> DateAEI20092ndElectionSRPrinted { get; set; }
        public Nullable<int> NumberOfDaysToElect2ndElection { get; set; }
        public Nullable<bool> HasAEI20092ndElectionBeenElected { get; set; }
        public Nullable<System.DateTime> PostmarkDateOfAEISubsidyWaiver { get; set; }
        public Nullable<System.DateTime> PostmarkDateOfAEISubsidyEligibleAttestation { get; set; }
    
        public virtual QB QB { get; set; }
    }
}
