use Alegeus_File_Processing;

alter VIEW dbo.Header_list_ALL
    AS
        SELECT
            'G:\FTP\' + Folder_name folder_name
          , template_type
          , IC_type
          , BENCODE
          , to_FTP
        FROM
            dbo.Automated_Header_list
-- WHERE        (Folder_name LIKE '%hills%')
go

alter view [dbo].[Header_list_ALL_folders]
    AS
        SELECT
            'G:\FTP\' + Folder_name folder_name
          , template_type
          , IC_type
          , BENCODE
          , to_FTP
        FROM
            dbo.Automated_Header_list
go

alter view [dbo].[Header_list_Own]
    AS
        SELECT
            'G:\FTP\' + Folder_name folder_name
          , template_type
          , IC_type
          , BENCODE
          , to_FTP
        FROM
            dbo.Automated_Header_list
        WHERE
            (template_type = N'Own')

go

alter view [dbo].[Header_list_new]
    AS
        SELECT
            'G:\FTP\' + Folder_name folder_name
          , template_type
          , IC_type
          , BENCODE
          , to_FTP
        FROM
            dbo.Automated_Header_list
        WHERE
            (template_type = N'new')

go

alter view [dbo].[Header_list_none]
    AS
        SELECT
            'G:\FTP\' + Folder_name folder_name
          , template_type
          , IC_type
          , BENCODE
          , to_FTP
        FROM
            dbo.Automated_Header_list
        WHERE
            (template_type = N'none')

go

alter view dbo.Header_list_old
    AS
        SELECT
            'G:\FTP\' + Folder_name folder_name
          , template_type
          , IC_type
          , BENCODE
          , to_FTP
        FROM
            dbo.Automated_Header_list
        WHERE
            (template_type = N'Old')
go

