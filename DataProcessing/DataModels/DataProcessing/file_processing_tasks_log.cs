//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataProcessing.DataModels.DataProcessing
{
    using System;
    using System.Collections.Generic;
    
    public partial class file_processing_tasks_log
    {
        public int fileLogTaskId { get; set; }
        public Nullable<int> fileLogId { get; set; }
        public string fileId { get; set; }
        public string processingTask { get; set; }
        public string processingTaskOutcome { get; set; }
        public string processingTaskOutcomeDetails { get; set; }
        public string originalFileName { get; set; }
        public string originalFullPath { get; set; }
        public string newFileName { get; set; }
        public string newFileFullPath { get; set; }
        public Nullable<System.DateTime> created_at { get; set; }
    }
}