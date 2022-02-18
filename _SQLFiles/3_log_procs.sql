use Alegeus_File_Processing;

alter table file_processing_log
    drop column originalFileUploadedOn
go
alter table file_processing_log
    add platform nvarchar(50) null
go
alter table dbo.message_log
    add platform nvarchar(50) null
go


alter table COBRA.dbo.QB_file_data_fixtbl
    add folder_name nvarchar(500) null
go
alter table COBRA.dbo.QB_file_data_fixtbl
    add row_num int identity
go


alter table file_processing_log
    add originalFileUploadedOn datetime null
go
alter procedure dbo.insert_file_processing_log(
                                              @platform nvarchar(50),
                                              @fileLogId int output,
                                              @fileId nvarchar(10),
                                              @folderName nvarchar(400),
                                              @templateType nvarchar(50),
                                              @ICType nvarchar(50),
                                              @toFTP nvarchar(10),
                                              @bencode varchar(50),
                                              @originalFileName nvarchar(200),
                                              @originalFullPath nvarchar(200),
                                              @originalFileUploadedOn nvarchar(50),
                                              @newFileName nvarchar(200),
                                              @newFileFullPath nvarchar(200),
                                              @fileLogTaskId int output,
                                              @processingTask nvarchar(50),
                                              @processingTaskOutcome nvarchar(20),
                                              @processingTaskOutcomeDetails nvarchar(500) ) as
begin
    
    SET NOCOUNT ON
    /* if fileLogId = 0 or processingTask = 'New', we will insert into main and detail tables
       else we will insert only into the detailed table
    */
    
    if @fileLogId is null or @fileLogId = 0 or @processingTask = 'New'
        begin
            set @fileLogId = 0;
            
            if rtrim( ltrim( @originalFileUploadedOn ) ) = ''
                begin
                    set @originalFileUploadedOn = null;
                end
            
            insert into dbo.file_processing_log (
                                                platform,
                                                fileId,
                                                folderName,
                                                templateType,
                                                ICType,
                                                toFTP,
                                                bencode,
                                                originalFileName,
                                                originalFullPath,
                                                originalFileUploadedOn,
                                                newFileName,
                                                newFileFullPath
            
            )
            values (
                   @platform,
                   @fileId,
                   @folderName,
                   @templateType,
                   @ICType,
                   @toFTP,
                   @bencode,
                   @originalFileName,
                   @originalFullPath,
                   @originalFileUploadedOn,
                   @newFileName,
                   @newFileFullPath
                   );
            
            /**/
            set @fileLogId = SCOPE_IDENTITY( );
        end
    
    /* no insert into header table, only into detail table*/
    insert into dbo.file_processing_tasks_log(
                                             fileLogId,
                                             fileId,
                                             processingTask,
                                             processingTaskOutcome,
                                             processingTaskOutcomeDetails,
                                             originalFileName,
                                             originalFullPath,
                                             newFileName,
                                             newFileFullPath
    )
    values (
           @fileLogId,
           @fileId,
           @processingTask,
           @processingTaskOutcome,
           @processingTaskOutcomeDetails,
           @originalFileName,
           @originalFullPath,
           @newFileName,
           @newFileFullPath
           );
    
    /* return the header PK */
    select
        @fileLogId;
end;
go



alter function dbo.getFileLogId(
    @filePathWithoutExtension varchar(200) ) returns int
    as
    begin
        declare @fileLogId int = null;
        
        /* take last id for filename (wihout path or extension*/
        select top 1
            @fileLogId = fileLogId
        from
            dbo.file_processing_log
        where
             originalFileName like concat( @filePathWithoutExtension , '%' )
          or newFileName like concat( @filePathWithoutExtension , '%' )
        order by
            fileLogId desc;
        
        if @fileLogId is not null and @fileLogId <> 0
            begin
                return @fileLogId;
            end
        else
            begin
                return 0;
            end
        
        return @fileLogId
    end;
select
    dbo.getFileLogId( 'EOP9 -- LittleBirdHR_20220127_IB' );
