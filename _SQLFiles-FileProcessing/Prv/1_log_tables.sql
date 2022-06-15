use Alegeus_File_Processing;

drop table Alegeus_File_Processing.dbo.message_log;

create table Alegeus_File_Processing.dbo.message_log
(
log_id         int identity,
module_name    nvarchar(500),
submodule_name nvarchar(500),
step_type      nvarchar(500),
step_name      nvarchar(500),
command        nvarchar(max),
created_at     datetime default getdate( ),
platform       nvarchar(200)
)
go

drop table Alegeus_File_Processing.dbo.file_processing_log;
create table Alegeus_File_Processing.dbo.file_processing_log
(
fileLogId              int identity,
platform               nvarchar(50),
fileId                 nvarchar(10),
folderName             nvarchar(400),
templateType           nvarchar(50),
ICType                 nvarchar(50),
toFTP                  nvarchar(10),
bencode                varchar(50),
originalFileName       varchar(500),
originalFullPath       varchar(500),
newFileName            varchar(500),
newFileFullPath        varchar(500),
created_at             datetime default getdate( ),
originalFileUploadedOn datetime
)
go

create index fileId
    on file_processing_log (fileId)
go

create index folderName
    on file_processing_log (folderName)
go

create index templateType
    on file_processing_log (templateType)
go

create index bencode
    on file_processing_log (bencode)
go

create index originalFullPath
    on file_processing_log (originalFullPath)
go

create index newFileName
    on file_processing_log (newFileName)
go

create index newFileFullPath
    on file_processing_log (newFileFullPath)
go

drop table Alegeus_File_Processing.dbo.file_processing_tasks_log;
create table Alegeus_File_Processing.dbo.file_processing_tasks_log
(
	fileLogTaskId int identity,
	fileLogId int,
	fileId nvarchar(10),
	processingTask nvarchar(50),
	processingTaskOutcome nvarchar(20),
	processingTaskOutcomeDetails varchar(500),
	originalFileName nvarchar(500),
	originalFullPath varchar(500),
	newFileName nvarchar(500),
	newFileFullPath nvarchar(500),
	created_at datetime default getdate()
)
go

create index fileId
	on file_processing_tasks_log (fileId)
go

create index fileLogId
	on file_processing_tasks_log (fileLogId)
go

create index processingTask
	on file_processing_tasks_log (processingTask)
go

create index processingTaskOutcome
	on file_processing_tasks_log (processingTaskOutcome)
go

create index originalFullPath
	on file_processing_tasks_log (originalFullPath)
go

create index newFileName
	on file_processing_tasks_log (newFileName)
go

create index newFileFullPath
	on file_processing_tasks_log (newFileFullPath)
go

