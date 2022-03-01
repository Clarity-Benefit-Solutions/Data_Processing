use Alegeus_File_Processing;
go

create function fileLogHasError( @fileLogId int ) returns int
begin
    declare @cnt int = 0;
    select
        @cnt = count( * )
    from
        dbo.file_processing_tasks_log
    where
          fileLogId = @fileLogId
      and processingTaskOutcome = 'ERROR';
    
    return isnull( @cnt , 0 );
end
GO
alter view vw_file_processing_log
    as
        select
            fileLogId
          , platform
          , fileId
          , folderName
          , templateType
          , ICType
          , toFTP
          , bencode
          , originalFileName
          , originalFullPath
          , newFileName
          , newFileFullPath
          , created_at
          , originalFileUploadedOn
          , dbo.fileLogHasError( fileLogId ) ProcessingErrorCount
        from
            file_processing_log h;

select *
from
    vw_file_processing_log
where
    ErrorCount > 0;

alter table Alegeus_ErrorLog..mbi_file_table_stage
    add OngoingPrefunded nvarchar(50);
alter table Alegeus_ErrorLog..mbi_file_table
    add OngoingPrefunded nvarchar(50);

alter table Alegeus_ErrorLog..res_file_table_stage
    add OngoingPrefunded nvarchar(50);
alter table Alegeus_ErrorLog..res_file_table
    add OngoingPrefunded nvarchar(50);
