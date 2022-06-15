use Alegeus_ErrorLog;

alter VIEW [dbo].[CRM_Listview]
    AS
        SELECT
            REPLACE( BENCODE , '"' , '' ) BENCODE
          , REPLACE( CRM , '"' , '' ) CRM
        FROM /* [Low_balance_reporting].[dbo].[CRM_List_contacts]*/
            dbo.CRM_List
go

drop table Alegeus_ErrorLog..CRM_List;
create table Alegeus_ErrorLog..CRM_List
(
[BENCODE]               [nvarchar](50) NULL,
[CRM]                   [nvarchar](max) NULL,
[CRM_email]             [nvarchar](max) NULL,
[emp_services]          [nvarchar](max) NULL,
[Primary_contact_name]  [nvarchar](max) NULL,
[Primary_contact_email] [nvarchar](max) NULL,
[client_start_date]     [nvarchar](50) NULL
)
go

