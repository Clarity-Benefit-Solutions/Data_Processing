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
    
    public partial class ClientAccess
    {
        public int ClientAccessID { get; set; }
        public int ClientID { get; set; }
        public System.DateTime AccessDateTime { get; set; }
        public string AccessType { get; set; }
        public System.Guid AccessUser { get; set; }
    
        public virtual AdminUser AdminUser { get; set; }
        public virtual Client Client { get; set; }
    }
}
