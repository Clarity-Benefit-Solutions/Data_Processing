use Data_Processing;
go

drop table dbo.app_settings;
create table dbo.app_settings
(
	id int identity,
	environment nvarchar(50) default 'PROD',
	setting_name nvarchar(100),
	setting_value nvarchar(2000),
	created_at datetime default getdate(),
	is_active int default 0 not null
)
go

create unique index app_settings_uk
	on dbo.app_settings (environment, setting_name)
go

set identity_insert  dbo.app_settings ON;

INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (5, N'PROD', N'FtpPath', N'G:/FTP', N'2022-03-11 08:29:50.667', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (2, N'TEST', N'ProcessingPath', N'C:\___Clarity\clarity_dev\r1_Data_Processing\__LocalTestDirsAndFiles', N'2022-03-11 08:27:07.467', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (3, N'PROD', N'ProcessingPath', N'G:', N'2022-03-11 08:27:42.657', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (8, N'PROD', N'paylocityFtpPath', N'/Paylocity', N'2022-03-11 08:35:30.597', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (6, N'PROD', N'FtpItPath', N'G:/FTP-IT', N'2022-03-11 08:34:24.077', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (9, N'PROD', N'ToBoomiFtpItPath', N'/ToBoomi', N'2022-03-11 08:37:03.250', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (10, N'PROD', N'FromBoomiFtpItPath', N'/fromBoomi', N'2022-03-11 08:37:03.257', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (11, N'PROD', N'SalesForceCrmListPath', N'/fromBoomi/CRM_List.csv', N'2022-03-11 08:37:15.293', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (52, N'PROD', N'alegeusFilesImportHoldingPath', N'/ImportHolding/Alegeus', N'2022-03-11 08:39:04.290', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (13, N'PROD', N'cobraFilesImportHoldingPath', N'/ImportHolding/Cobra', N'2022-03-11 08:39:04.290', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (53, N'PROD', N'cobraFilesPassedPath', N'/Processed/Cobra/Passed', N'2022-03-11 08:40:22.520', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (15, N'PROD', N'cobraFilesPreparedQbPath', N'/Processed/Cobra/PreparedQb', N'2022-03-11 08:40:22.503', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (54, N'PROD', N'cobraFilesImportHoldingArchivePath', N'/ImportHolding/Cobra/Archive', N'2022-03-11 08:39:04.290', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (17, N'PROD', N'cobraFilesEmptyPath', N'/Processed/Cobra/Empty', N'2022-03-11 08:40:22.513', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (18, N'PROD', N'cobraFilesRejectsPath', N'/Processed/Cobra/Rejects', N'2022-03-11 08:40:22.520', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (19, N'PROD', N'cobraFilesDecryptPath', N'/Processed/Cobra/Decrypt', N'2022-03-11 08:40:22.523', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (55, N'PROD', N'cobraFilesTestPath', N'/Processed/Cobra/TestFiles', N'2022-03-11 08:40:22.513', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (21, N'PROD', N'cobraIgnoreFtpSourceDirsPaths', N'processing@claritybenefitsolutions,', N'2022-03-11 08:41:45.927', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (22, N'PROD', N'alegeusFileHeadersPath', N'/Processed/Alegeus/ForHeaders', N'2022-03-11 08:44:15.263', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (23, N'PROD', N'alegeusFilesImportHoldingArchivePath', N'/ImportHolding/Alegeus/Archive', N'2022-03-11 08:44:15.270', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (24, N'PROD', N'alegeusFilesToProcessPath', N'/Processed/Alegeus/ToProcess', N'2022-03-11 08:44:15.273', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (25, N'PROD', N'alegeusFilesPassedPath', N'/Processed/Alegeus/Passed', N'2022-03-11 08:44:15.280', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (56, N'PROD', N'cobraFilesToProcessPath', N'/Processed/Cobra/ToProcess', N'2022-03-11 08:44:15.273', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (62, N'PROD', N'unknownFilesImportHoldingPath', N'/ImportHolding/UnknownPlatform', N'2022-03-11 08:39:04.290', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (28, N'PROD', N'alegeusErrorLogMbiFilesPath', N'/AlegeusErrorLog/mbiFiles/Archive', N'2022-03-11 08:44:15.293', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (29, N'PROD', N'alegeusErrorLogMbiFilesArchivePath', N'/AlegeusErrorLog/mbiFiles', N'2022-03-11 08:44:15.300', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (30, N'PROD', N'alegeusErrorLogResFilesPath', N'/AlegeusErrorLog/resFiles/Archive', N'2022-03-11 08:44:15.303', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (31, N'PROD', N'alegeusErrorLogResFilesArchivePath', N'/AlegeusErrorLog/resFiles', N'2022-03-11 08:44:15.307', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (32, N'PROD', N'alegeusFilesRejectsPath', N'/Processed/Alegeus/Rejects', N'2022-03-11 08:44:32.107', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (40, N'TEST', N'localTestFilesPath', N'C:\___Clarity\clarity_dev\r1_Data_Processing\__LocalTestDirsAndFiles', N'2022-03-11 09:06:38.727', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (41, N'PROD', N'localTestFilesPath', N'G:/FTP/__LocalTestDirsAndFiles', N'2022-03-11 09:06:38.730', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (42, N'TEST', N'FtpItPath', N'C:\___Clarity\clarity_dev\r1_Data_Processing\__LocalTestDirsAndFiles/FTP-IT', N'2022-03-11 09:10:17.857', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (43, N'TEST', N'FtpPath', N'C:\___Clarity\clarity_dev\r1_Data_Processing\__LocalTestDirsAndFiles/FTP', N'2022-03-11 09:10:17.863', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (44, N'PROD', N'alegeusFilesTestPath', N'/Processed/Alegeus/Testfiles', N'2022-03-17 19:34:25.273', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (45, N'TEST', N'alegeusRemoteFTPPath', N'/C:/___Clarity/clarity_dev/r1_Data_Processing/__LocalTestDirsAndFiles/_local_FTP_Server_Server/Alegeus', N'2022-03-27 09:46:50.667', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (46, N'PROD', N'alegeusRemoteFTPPath', N'/benefledi', N'2022-03-27 09:46:50.680', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (47, N'TEST', N'cobraRemoteFTPPath', N'/C:/___Clarity/clarity_dev/r1_Data_Processing/__LocalTestDirsAndFiles/_local_FTP_Server_Server/Cobra', N'2022-03-27 09:50:23.107', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (48, N'PROD', N'cobraRemoteFTPPath', N'/', N'2022-03-27 09:50:32.620', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (49, N'PROD', N'alegeusParticipantEnrollmentFilesDownloadPath', N'/C:/___Clarity/clarity_dev/r1_Data_Processing/__LocalTestDirsAndFiles\EnrolledParticipantReport\Encrypted', N'2022-04-14 02:49:17.997', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (50, N'PROD', N'alegeusParticipantEnrollmentFilesDecryptedPath', N'/C:/___Clarity/clarity_dev/r1_Data_Processing/__LocalTestDirsAndFiles/Decrypted', N'2022-04-14 02:49:50.590', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (51, N'PROD', N'incomingFilesHoldingRoot', N'/IncomingFilesHolding', N'2022-03-11 08:44:15.283', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (57, N'PROD', N'alegeusFilesToReProcessPath', N'/Processed/Alegeus/ToReProcess', N'2022-03-11 08:44:15.273', 1);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (60, N'PROD', N'cobraFilesToReProcessPath', N'/Processed/Cobra/ToReProcess', N'2022-04-23 09:36:28.633', 0);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (61, N'PROD', N'alegeusFilesEmptyPath', N'/Processed/Alegeus/Empty', N'2022-04-23 09:38:49.010', 0);
INSERT INTO dbo.app_settings (id, environment, setting_name, setting_value, created_at, is_active) VALUES (63, N'PROD', N'unknownFilesRejectsPath', N'/Processed/UnknownPlatform/Rejects', N'2022-03-11 08:40:22.520', 1);

set identity_insert  dbo.app_settings OFF;


alter table dbo.mbi_file_table_stage add AccountSegmentId nvarchar(255) null;
alter table dbo.mbi_file_table add AccountSegmentId nvarchar(255) null;

alter table dbo.res_file_table_stage add AccountSegmentId nvarchar(255) null;
alter table dbo.res_file_table add AccountSegmentId nvarchar(255) null;

alter table dbo.alegeus_file_final add AccountSegmentId nvarchar(255) null;
alter table dbo.alegeus_file_staging add AccountSegmentId nvarchar(255) null;

