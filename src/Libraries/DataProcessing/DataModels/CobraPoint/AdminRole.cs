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
    
    public partial class AdminRole
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public AdminRole()
        {
            this.AdminUserRoles = new HashSet<AdminUserRole>();
        }
    
        public string AdminRoleID { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public Nullable<int> MemberCount { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AdminUserRole> AdminUserRoles { get; set; }
    }
}