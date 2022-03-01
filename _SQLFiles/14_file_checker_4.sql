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

select *
from
    Alegeus_ErrorLog..mbi_file_table_stage
where
    len( error_message ) > 0;

exec Alegeus_ErrorLog..process_mbi_file_table_stage_import;

select
    concat(
            data_row , ','
        , case
              when len( error_message ) > 0 then concat( 'ERRORS: ' , error_message )
              else ''
          end )
from
    Alegeus_ErrorLog..mbi_file_table
where
    mbi_file_name = '110--3--Wisenbaker_Election_01272022.mbi'
order by
    mbi_file_table.source_row_no;
