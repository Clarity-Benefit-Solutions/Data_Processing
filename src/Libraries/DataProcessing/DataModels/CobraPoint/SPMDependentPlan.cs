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
    
    public partial class SPMDependentPlan
    {
        public int SPMDependentPlanID { get; set; }
        public int SPMDependentID { get; set; }
        public string InsuranceType { get; set; }
        public Nullable<System.DateTime> FirstDayOfCoverage { get; set; }
        public Nullable<System.DateTime> LastDayOfCoverage { get; set; }
        public string PlanName { get; set; }
        public System.DateTime StartDate { get; set; }
        public Nullable<System.DateTime> EndDate { get; set; }
        public string BundleName { get; set; }
        public int ClientPlanSPMID { get; set; }
    
        public virtual ClientPlanSPM ClientPlanSPM { get; set; }
        public virtual SPMDependent SPMDependent { get; set; }
    }
}
