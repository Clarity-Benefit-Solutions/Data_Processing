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
    
    public partial class Broker
    {
        public int BrokerID { get; set; }
        public string BrokerName { get; set; }
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
        public Nullable<int> AllowBrokerPortalAccess { get; set; }
        public Nullable<int> Active { get; set; }
        public Nullable<int> AllowBrokerSSO { get; set; }
    }
}
