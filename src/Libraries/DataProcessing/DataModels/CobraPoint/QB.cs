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
    
    public partial class QB
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public QB()
        {
            this.QBAccesses = new HashSet<QBAccess>();
            this.QBACHes = new HashSet<QBACH>();
            this.QBAEIInformations = new HashSet<QBAEIInformation>();
            this.QBCommunications = new HashSet<QBCommunication>();
            this.QBDependents = new HashSet<QBDependent>();
            this.QBDisabilityInformations = new HashSet<QBDisabilityInformation>();
            this.QBEvents = new HashSet<QBEvent>();
            this.QBLegacies = new HashSet<QBLegacy>();
            this.QBNotes = new HashSet<QBNote>();
            this.QBPayments = new HashSet<QBPayment>();
            this.QBPremiums = new HashSet<QBPremium>();
            this.QBSubsidySchedules = new HashSet<QBSubsidySchedule>();
        }
    
        public int MemberID { get; set; }
        public int ClientID { get; set; }
        public int ClientDivisionID { get; set; }
        public string FirstName { get; set; }
        public string MiddleInitial { get; set; }
        public string LastName { get; set; }
        public string Salutation { get; set; }
        public string SSN { get; set; }
        public string IndividualIdentifier { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Gender { get; set; }
        public Nullable<System.DateTime> DOB { get; set; }
        public string TobaccoUse { get; set; }
        public string EmployeeType { get; set; }
        public string PayrollType { get; set; }
        public Nullable<int> YearsOfService { get; set; }
        public string PremiumCouponType { get; set; }
        public bool UsesHCTC { get; set; }
        public bool Active { get; set; }
        public Nullable<System.DateTime> EnteredDateTime { get; set; }
        public string EnteredByUser { get; set; }
        public string MethodEntered { get; set; }
        public Nullable<System.DateTime> PaidThroughDate { get; set; }
        public Nullable<System.DateTime> OnlineElectionProcessedDate { get; set; }
        public string BenefitGroup { get; set; }
        public string AccountStructure { get; set; }
        public string ClientCustomData { get; set; }
        public Nullable<bool> AllowSSO { get; set; }
        public string SSOIdentifier { get; set; }
        public Nullable<System.DateTime> LastModifiedDate { get; set; }
        public string PlanCategory { get; set; }
    
        public virtual Client Client { get; set; }
        public virtual ClientDivision ClientDivision { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBAccess> QBAccesses { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBACH> QBACHes { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBAEIInformation> QBAEIInformations { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBCommunication> QBCommunications { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBDependent> QBDependents { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBDisabilityInformation> QBDisabilityInformations { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBEvent> QBEvents { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBLegacy> QBLegacies { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBNote> QBNotes { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBPayment> QBPayments { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBPremium> QBPremiums { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QBSubsidySchedule> QBSubsidySchedules { get; set; }
    }
}