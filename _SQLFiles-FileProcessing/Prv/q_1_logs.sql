use Data_Processing;
/*
truncate table file_processing_log;
truncate table file_processing_tasks_log;
truncate table message_log;
*/
/*
message_log
*/
select *
from
    message_log
order by
    message_log.log_id desc;

select *
from

order by
    log_id desc;

select *
from
    file_processing_log
order by
    fileLogId desc;
/*
file_processing_tasks_log
*/
select *
from
    file_processing_tasks_log
--     where (
-- --         fileLogId=118
--         processingTask like '%AutomatedHeaders-<PreCheckFilesAndProcess%'
--         )
order by
    fileLogTaskId desc;

/*processing_script_tbl*/
select *
from
    processing_script_tbl;

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
