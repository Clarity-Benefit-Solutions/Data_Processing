use [Alegeus_File_Processing];


/*file_processing_log*/
select *
from
    Alegeus_File_Processing.dbo.file_processing_log
    -- where originalFileName is null or originalFileName = ''
order by
    fileLogId desc;
;
/**/
select *
from
    Alegeus_File_Processing.dbo.file_processing_tasks_log
where
     processingTask is null
  or processingTask = ''
order by
    file_processing_tasks_log.fileLogTaskId desc
  , fileLogId asc;
/**/
select *
from
    Alegeus_File_Processing.dbo.message_log;
/**/
