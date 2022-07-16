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
    
    public partial class NPM
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public NPM()
        {
            this.NPMAccesses = new HashSet<NPMAccess>();
            this.NPMCommunications = new HashSet<NPMCommunication>();
            this.NPMHIPAACertDatas = new HashSet<NPMHIPAACertData>();
            this.NPMNotes = new HashSet<NPMNote>();
        }
    
        public int MemberID { get; set; }
        public int ClientDivisionID { get; set; }
        public bool Active { get; set; }
        public System.DateTime DateEntered { get; set; }
        public string MethodEntered { get; set; }
        public string EnteredByUser { get; set; }
        public string Salutation { get; set; }
        public string FirstName { get; set; }
        public string MiddleInitial { get; set; }
        public string LastName { get; set; }
        public string SSN { get; set; }
        public string IndividualID { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public bool UsesAndFamily { get; set; }
        public bool HasBecomeAQB { get; set; }
        public bool HasWaivedAllCoverage { get; set; }
        public Nullable<System.DateTime> HireDate { get; set; }
        public Nullable<bool> IsGRLetterSent { get; set; }
        public Nullable<System.DateTime> EnteredDateTime { get; set; }
        public Nullable<System.DateTime> LastModifiedDate { get; set; }
    
        public virtual ClientDivision ClientDivision { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<NPMAccess> NPMAccesses { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<NPMCommunication> NPMCommunications { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<NPMHIPAACertData> NPMHIPAACertDatas { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<NPMNote> NPMNotes { get; set; }
    }
}