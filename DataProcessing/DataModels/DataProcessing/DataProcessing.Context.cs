﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class Data_ProcessingEntities : DbContext
    {
        public Data_ProcessingEntities()
            : base("name=Data_ProcessingEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<dbo_error_log_results_workflow_local> dbo_error_log_results_workflow_local { get; set; }
        public virtual DbSet<mbi_file_table> mbi_file_table { get; set; }
        public virtual DbSet<mbi_file_table_stage> mbi_file_table_stage { get; set; }
        public virtual DbSet<res_file_table> res_file_table { get; set; }
        public virtual DbSet<res_file_table_stage> res_file_table_stage { get; set; }
        public virtual DbSet<alegeus_file_final> alegeus_file_final { get; set; }
        public virtual DbSet<alegeus_file_staging> alegeus_file_staging { get; set; }
        public virtual DbSet<app_settings> app_settings { get; set; }
        public virtual DbSet<Automated_Header_list> Automated_Header_list { get; set; }
        public virtual DbSet<dbo_error_log_results_workflow_local_AL> dbo_error_log_results_workflow_local_AL { get; set; }
        public virtual DbSet<dbo_error_log_results_workflow_localBU> dbo_error_log_results_workflow_localBU { get; set; }
        public virtual DbSet<dbo_tracked_errors_local> dbo_tracked_errors_local { get; set; }
        public virtual DbSet<file_processing_log> file_processing_log { get; set; }
        public virtual DbSet<file_processing_tasks_log> file_processing_tasks_log { get; set; }
        public virtual DbSet<message_log> message_log { get; set; }
        public virtual DbSet<QB_file_data_fixtbl> QB_file_data_fixtbl { get; set; }
        public virtual DbSet<vw_file_processing_log> vw_file_processing_log { get; set; }
    
        [DbFunction("Data_ProcessingEntities", "CsvSplit1")]
        public virtual IQueryable<CsvSplit1_Result> CsvSplit1(string delimited, string delimiter)
        {
            var delimitedParameter = delimited != null ?
                new ObjectParameter("delimited", delimited) :
                new ObjectParameter("delimited", typeof(string));
    
            var delimiterParameter = delimiter != null ?
                new ObjectParameter("delimiter", delimiter) :
                new ObjectParameter("delimiter", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<CsvSplit1_Result>("[Data_ProcessingEntities].[CsvSplit1](@delimited, @delimiter)", delimitedParameter, delimiterParameter);
        }
    
        [DbFunction("Data_ProcessingEntities", "SplitErrorRowStringToFields")]
        public virtual IQueryable<SplitErrorRowStringToFields_Result> SplitErrorRowStringToFields(string csvString)
        {
            var csvStringParameter = csvString != null ?
                new ObjectParameter("csvString", csvString) :
                new ObjectParameter("csvString", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<SplitErrorRowStringToFields_Result>("[Data_ProcessingEntities].[SplitErrorRowStringToFields](@csvString)", csvStringParameter);
        }
    
        [DbFunction("Data_ProcessingEntities", "SplitStringsOrdered")]
        public virtual IQueryable<SplitStringsOrdered_Result> SplitStringsOrdered(string list, string delimiter)
        {
            var listParameter = list != null ?
                new ObjectParameter("List", list) :
                new ObjectParameter("List", typeof(string));
    
            var delimiterParameter = delimiter != null ?
                new ObjectParameter("Delimiter", delimiter) :
                new ObjectParameter("Delimiter", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<SplitStringsOrdered_Result>("[Data_ProcessingEntities].[SplitStringsOrdered](@List, @Delimiter)", listParameter, delimiterParameter);
        }
    
        public virtual int Data_Processing_failure_alert()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("Data_Processing_failure_alert");
        }
    
        public virtual int Data_Processing_refreshsuccess_alert()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("Data_Processing_refreshsuccess_alert");
        }
    
        public virtual int Data_Processing_track_new_ftp_errors()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("Data_Processing_track_new_ftp_errors");
        }
    
        public virtual int Alegeus_Header_n_success()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("Alegeus_Header_n_success");
        }
    
        public virtual int build_auto_ftp_batfile()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("build_auto_ftp_batfile");
        }
    
        public virtual ObjectResult<Nullable<int>> check_imports_file()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Nullable<int>>("check_imports_file");
        }
    
        public virtual int Fix_COBRA_brokerage_Contacts_SSObollean()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("Fix_COBRA_brokerage_Contacts_SSObollean");
        }
    
        public virtual int Fix_COBRA_Client_Contacts_SSObollean()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("Fix_COBRA_Client_Contacts_SSObollean");
        }
    
        public virtual int Fix_COBRAQB_SSObollean()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("Fix_COBRAQB_SSObollean");
        }
    
        public virtual int Get_COBRA_QBs()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("Get_COBRA_QBs");
        }
    
        public virtual ObjectResult<Nullable<int>> insert_file_processing_log(string platform, ObjectParameter fileLogId, string fileId, string folderName, string templateType, string iCType, string toFTP, string bencode, string originalFileName, string originalFullPath, string originalFileUploadedOn, string newFileName, string newFileFullPath, ObjectParameter fileLogTaskId, string processingTask, string processingTaskOutcome, string processingTaskOutcomeDetails)
        {
            var platformParameter = platform != null ?
                new ObjectParameter("platform", platform) :
                new ObjectParameter("platform", typeof(string));
    
            var fileIdParameter = fileId != null ?
                new ObjectParameter("fileId", fileId) :
                new ObjectParameter("fileId", typeof(string));
    
            var folderNameParameter = folderName != null ?
                new ObjectParameter("folderName", folderName) :
                new ObjectParameter("folderName", typeof(string));
    
            var templateTypeParameter = templateType != null ?
                new ObjectParameter("templateType", templateType) :
                new ObjectParameter("templateType", typeof(string));
    
            var iCTypeParameter = iCType != null ?
                new ObjectParameter("ICType", iCType) :
                new ObjectParameter("ICType", typeof(string));
    
            var toFTPParameter = toFTP != null ?
                new ObjectParameter("toFTP", toFTP) :
                new ObjectParameter("toFTP", typeof(string));
    
            var bencodeParameter = bencode != null ?
                new ObjectParameter("bencode", bencode) :
                new ObjectParameter("bencode", typeof(string));
    
            var originalFileNameParameter = originalFileName != null ?
                new ObjectParameter("originalFileName", originalFileName) :
                new ObjectParameter("originalFileName", typeof(string));
    
            var originalFullPathParameter = originalFullPath != null ?
                new ObjectParameter("originalFullPath", originalFullPath) :
                new ObjectParameter("originalFullPath", typeof(string));
    
            var originalFileUploadedOnParameter = originalFileUploadedOn != null ?
                new ObjectParameter("originalFileUploadedOn", originalFileUploadedOn) :
                new ObjectParameter("originalFileUploadedOn", typeof(string));
    
            var newFileNameParameter = newFileName != null ?
                new ObjectParameter("newFileName", newFileName) :
                new ObjectParameter("newFileName", typeof(string));
    
            var newFileFullPathParameter = newFileFullPath != null ?
                new ObjectParameter("newFileFullPath", newFileFullPath) :
                new ObjectParameter("newFileFullPath", typeof(string));
    
            var processingTaskParameter = processingTask != null ?
                new ObjectParameter("processingTask", processingTask) :
                new ObjectParameter("processingTask", typeof(string));
    
            var processingTaskOutcomeParameter = processingTaskOutcome != null ?
                new ObjectParameter("processingTaskOutcome", processingTaskOutcome) :
                new ObjectParameter("processingTaskOutcome", typeof(string));
    
            var processingTaskOutcomeDetailsParameter = processingTaskOutcomeDetails != null ?
                new ObjectParameter("processingTaskOutcomeDetails", processingTaskOutcomeDetails) :
                new ObjectParameter("processingTaskOutcomeDetails", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Nullable<int>>("insert_file_processing_log", platformParameter, fileLogId, fileIdParameter, folderNameParameter, templateTypeParameter, iCTypeParameter, toFTPParameter, bencodeParameter, originalFileNameParameter, originalFullPathParameter, originalFileUploadedOnParameter, newFileNameParameter, newFileFullPathParameter, fileLogTaskId, processingTaskParameter, processingTaskOutcomeParameter, processingTaskOutcomeDetailsParameter);
        }
    
        public virtual int insert_to_auto_ftp_list()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("insert_to_auto_ftp_list");
        }
    
        public virtual int proc_alegeus_AlterHeaders2015()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("proc_alegeus_AlterHeaders2015");
        }
    
        public virtual int proc_alegeus_AlterHeaders2019()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("proc_alegeus_AlterHeaders2019");
        }
    
        public virtual int proc_alegeus_AlterHeadersNone()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("proc_alegeus_AlterHeadersNone");
        }
    
        public virtual int proc_alegeus_AlterHeadersOwn()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("proc_alegeus_AlterHeadersOwn");
        }
    
        public virtual int process_mbi_file_table_stage_import()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("process_mbi_file_table_stage_import");
        }
    
        public virtual int process_res_file_table_stage_import()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("process_res_file_table_stage_import");
        }
    
        public virtual int SplitAllErrorRowStringsToFields(string tableName)
        {
            var tableNameParameter = tableName != null ?
                new ObjectParameter("tableName", tableName) :
                new ObjectParameter("tableName", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("SplitAllErrorRowStringsToFields", tableNameParameter);
        }
    
        public virtual int zz_truncate_all()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("zz_truncate_all");
        }
    }
}
