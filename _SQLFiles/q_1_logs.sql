/*
truncate table Alegeus_File_Processing.dbo.file_processing_log;
truncate table Alegeus_File_Processing.dbo.file_processing_tasks_log;
truncate table Alegeus_File_Processing.dbo.message_log;
*/
/*
message_log
*/
select *
from
    Alegeus_File_Processing.dbo.message_log
order by
    message_log.log_id desc;/*

file_processing_log
*/
select *
from
    Alegeus_File_Processing.dbo.file_processing_log
order by
    fileLogId desc;
/*
file_processing_tasks_log
*/
select *
from
    Alegeus_File_Processing.dbo.file_processing_tasks_log
    where fileLogId=98
order by
    fileLogId desc
  , fileLogTaskId desc;

/*processing_script_tbl*/
select *
from
    Alegeus_File_Processing.dbo.processing_script_tbl;

/*CRM_List*/
select *
from
    Alegeus_ErrorLog.dbo.CRM_List;

UPDATE [Alegeus_ErrorLog].[dbo].[dbo_error_log_results_workflow_local]
set
    CRM = replace( c.crm , '\"' , '' )
FROM
    [Alegeus_ErrorLog].[dbo].[dbo_error_log_results_workflow_local] as l
        JOIN [Alegeus_ErrorLog].[dbo].[CRM_List] as c on l.BENCODE = c.bencode , '\"' , '' );
