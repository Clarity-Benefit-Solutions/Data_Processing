//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataProcessing.DataModels.AlegeusErrorLog
{
    using System;
    using System.Collections.Generic;
    
    public partial class dbo_error_log_results_workflow_localBU
    {
        public Nullable<int> error_code { get; set; }
        public string error_row { get; set; }
        public string mbi_file_name { get; set; }
        public string error_message { get; set; }
        public Nullable<int> mbi_row_num { get; set; }
        public string mbi_line { get; set; }
        public bool CRM_Check { get; set; }
        public bool Proc_Check { get; set; }
        public string updated_by { get; set; }
        public Nullable<System.DateTime> updated_timestamp { get; set; }
        public string BENCODE { get; set; }
        public string CRM { get; set; }
        public string Notes { get; set; }
        public string Notes_CRM { get; set; }
        public string file_type { get; set; }
        public int IDfield { get; set; }
        public bool Archive_Check { get; set; }
    }
}
