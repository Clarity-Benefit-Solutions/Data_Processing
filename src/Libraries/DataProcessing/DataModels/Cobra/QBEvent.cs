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
    
    public partial class QBEvent
    {
        public int QBEventID { get; set; }
        public int MemberID { get; set; }
        public string Category { get; set; }
        public string EventType { get; set; }
        public System.DateTime EventDate { get; set; }
        public System.DateTime OriginalEnrollmentDate { get; set; }
        public string EmployeeNameIfDependentEvent { get; set; }
        public string EmployeeSSNIfDependentEvent { get; set; }
        public bool IsSecondEvent { get; set; }
        public Nullable<System.DateTime> OriginalFDOC { get; set; }
        public string AEIStatus { get; set; }
        public Nullable<bool> NewQBInvoluntaryTermFollowingReductionInHours { get; set; }
        public Nullable<System.DateTime> QEDateofRIH { get; set; }
        public Nullable<System.DateTime> LatestElectionPMD { get; set; }
    }
}