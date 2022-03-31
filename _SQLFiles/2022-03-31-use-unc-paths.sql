use Data_Processing;
drop view Header_list_ALL
go

drop view Header_list_ALL_folders
go

drop view Header_list_Own
go

drop view Header_list_new
go

drop view Header_list_none
go

drop view Header_list_old
go



alter table Data_Processing..FTP_Source_Folders add environment varchar(50) default 'PROD';
update Data_Processing..FTP_Source_Folders
set
    FTP_Source_Folders.environment='PROD';

alter table FTP_Source_Folders alter column environment varchar(50) not null
go



UPDATE Data_Processing.dbo.FTP_Source_Folders SET environment = N'TEST' where so

alter table FTP_Source_Folders alter column Folder_name nvarchar(200) not null
go

create unique index [Automated_Header_list_Folder_name_uindex]
	on FTP_Source_Folders (Folder_name)
go

alter table FTP_Source_Folders
	add constraint [{schema}_Automated_Header_list_pk]
		primary key nonclustered (Folder_name)
go
INSERT INTO Data_Processing.dbo.FTP_Source_Folders (Folder_name,template_type,IC_type,to_FTP,BENCODE,environment)VALUES (N'_COBRA_SourceFiles',N'Old',null,N'2',null,N'TEST');

update  Data_Processing..FTP_Source_Folders set Folder_name = concat('\\Fs009\user_files_d\BENEFLEX\DEPTS\FTP\', Folder_name);
go
exec sp_rename 'Automated_Header_list', FTP_Source_Folders, 'OBJECT'
go

alter PROCEDURE [dbo].[insert_to_auto_ftp_list]

AS
BEGIN




INSERT INTO [dbo].[ftp_autofile_list]
           ([find_autoftp_file])
   SELECT distinct replace(replace(f.[folder_name],right(f.[folder_name],charindex('.',reverse(f.[folder_name]))),''),'G:\FTP\AutomatedHeaderv1_Files\','')
     FROM [dbo].[alegeus_file_final] as f
	 join [dbo].[FTP_Source_Folders] as l
	   on charindex(l.[BENCODE],[file_row]) > 1
    where file_row like '%,BEN%' and len(f.folder_name) > 3
	  and l.to_FTP = 1

END
go


