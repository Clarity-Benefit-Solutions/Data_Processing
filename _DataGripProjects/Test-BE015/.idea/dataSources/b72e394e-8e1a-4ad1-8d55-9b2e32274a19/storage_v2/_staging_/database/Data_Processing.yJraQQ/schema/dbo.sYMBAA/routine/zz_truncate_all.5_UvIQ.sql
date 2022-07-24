CREATE or alter procedure zz_truncate_all as
begin
    truncate table dbo.file_processing_log;
    
    truncate table dbo.file_processing_tasks_log;
    truncate table dbo.message_log;
    
    truncate table [dbo].[res_file_table];
    truncate table [dbo].[res_file_table_stage];
    truncate table [dbo].[mbi_file_table];
    truncate table [dbo].[mbi_file_table_stage];
    
    truncate table [dbo].[alegeus_file_final];
    
    truncate table [dbo].[cobra_res_file_table_stage];
    truncate table [dbo].[cobra_res_file_table];
    truncate table [dbo].[cobra_file_table];
    truncate table [dbo].[cobra_file_table_stage];
    
    truncate table dbo_tracked_errors_local;
    truncate table dbo_error_log_results_workflow_local;
end;
go

exec zz_truncate_all

