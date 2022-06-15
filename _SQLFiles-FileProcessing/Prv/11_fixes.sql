use Alegeus_ErrorLog;
go
alter procedure dbo.process_res_file_table_stage_import as
begin
    -- Script for SelectTopNRows command from SSMS
    declare @filename as varchar(200)
    
    /* get filename from import log*/
    set @filename = (
                        SELECT top 1
                            ltrim( rtrim( substring( error_row , charindex( ',' , ltrim( error_row ) ) + 1 ,
                                                     charindex( '.mbi' , error_row , 1 ) + 3 -
                                                     charindex( ',' , ltrim( error_row ) ) /*- 4*//*sumeet keep[ mbi extension*/ ) ) )
                        FROM
                            [Alegeus_ErrorLog].[dbo].[res_file_table_stage]
                        where
                            [error_row] like '%.mbi%'
    
                    );
    
    /* update filename for all rows*/
    if (@filename is not null and @filename <> '')
        begin
            update [Alegeus_ErrorLog].[dbo].[res_file_table_stage]
            set
                mbi_file_name = @filename
            FROM
                [Alegeus_ErrorLog].[dbo].[res_file_table_stage]
        end;
    
    /* update a*ny missing */
    update [Alegeus_ErrorLog].[dbo].[res_file_table_stage]
    set
        mbi_file_name = res_file_name
    where
        (mbi_file_name is null or mbi_file_name = '');
    
    /* clear error code from header row*/
    update dbo.res_file_table_stage
    set
        error_code=null,
        error_message = null
    where
        row_type = 'RA';
    
    /* parse error code and take error message from master */
    update [dbo].[res_file_table_stage]
    set
        error_code         = e.error_code,
        error_message_calc = e.user_desc
    FROM
        [dbo].[res_file_table_stage] as t
            join [dbo].[error_codes] as e
                 on e.error_code = t.error_code;
    
    /* ion case we did not import error message, set it from the calc one*/
    update [dbo].[res_file_table_stage]
    
    set
        error_message = error_message_calc
    where
         error_message is null
      or error_message = '';
    
    /* insert into main res table*/
    INSERT INTO [dbo].[res_file_table] (
                                       mbi_file_name,
                                       res_file_name,
                                       error_row,
                                       error_code,
                                       error_message,
                                       error_message_calc,
                                       row_num,
                                       row_type,
                                       AccountStatus,
                                       AccountTypeCode,
                                       AddressLine1,
                                       AddressLine2,
                                       BirthDate,
                                       City,
                                       Country,
                                       DeleteAccount,
                                       DependentID,
                                       DepositType,
                                       Division,
                                       EffectiveDate,
                                       EligibilityDate,
                                       Email,
                                       EmployeeDepositAmount,
                                       EmployeeID,
                                       EmployeePayPeriodElection,
                                       EmployeeSocialSecurityNumber,
                                       EmployeeStatus,
                                       EmployerDepositAmount,
                                       EmployerId,
                                       EmployerPayPeriodElection,
                                       FirstName,
                                       LastName,
                                       MiddleInitial,
                                       MobileNumber,
                                       OriginalPrefunded,
                                       Phone,
                                       PlanEndDate,
                                       PlanId,
                                       PlanStartDate,
                                       Relationship,
                                       State,
                                       TerminationDate,
                                       TpaId,
                                       Zip
    )
    
    SELECT
        mbi_file_name
      , res_file_name
      , error_row
      , error_code
      , error_message
      , error_message_calc
      , row_num
      , row_type
      , AccountStatus
      , AccountTypeCode
      , AddressLine1
      , AddressLine2
      , BirthDate
      , City
      , Country
      , DeleteAccount
      , DependentID
      , DepositType
      , Division
      , EffectiveDate
      , EligibilityDate
      , Email
      , EmployeeDepositAmount
      , EmployeeID
      , EmployeePayPeriodElection
      , EmployeeSocialSecurityNumber
      , EmployeeStatus
      , EmployerDepositAmount
      , EmployerId
      , EmployerPayPeriodElection
      , FirstName
      , LastName
      , MiddleInitial
      , MobileNumber
      , OriginalPrefunded
      , Phone
      , PlanEndDate
      , PlanId
      , PlanStartDate
      , Relationship
      , State
      , TerminationDate
      , TpaId
      , Zip
    FROM
        [Alegeus_ErrorLog].[dbo].[res_file_table_stage]
    
    --  truncate table [dbo].[res_file_table_stage];

end
go

alter procedure dbo.process_mbi_file_table_stage_import as
begin
    
    /* insert into main res table*/
    INSERT INTO [dbo].[mbi_file_table] (
                                       mbi_file_name,
                                       data_row,
                                       row_num,
                                       row_type,
                                       AccountStatus,
                                       AccountTypeCode,
                                       AddressLine1,
                                       AddressLine2,
                                       BirthDate,
                                       City,
                                       Country,
                                       DeleteAccount,
                                       DependentID,
                                       DepositType,
                                       Division,
                                       EffectiveDate,
                                       EligibilityDate,
                                       Email,
                                       EmployeeDepositAmount,
                                       EmployeeID,
                                       EmployeePayPeriodElection,
                                       EmployeeSocialSecurityNumber,
                                       EmployeeStatus,
                                       EmployerDepositAmount,
                                       EmployerId,
                                       EmployerPayPeriodElection,
                                       FirstName,
                                       LastName,
                                       MiddleInitial,
                                       MobileNumber,
                                       OriginalPrefunded,
                                       Phone,
                                       PlanEndDate,
                                       PlanId,
                                       PlanStartDate,
                                       Relationship,
                                       State,
                                       TerminationDate,
                                       TpaId,
                                       Zip
    )
    
    SELECT
        mbi_file_name
      , data_row
      , row_num
      , row_type
      , AccountStatus
      , AccountTypeCode
      , AddressLine1
      , AddressLine2
      , BirthDate
      , City
      , Country
      , DeleteAccount
      , DependentID
      , DepositType
      , Division
      , EffectiveDate
      , EligibilityDate
      , Email
      , EmployeeDepositAmount
      , EmployeeID
      , EmployeePayPeriodElection
      , EmployeeSocialSecurityNumber
      , EmployeeStatus
      , EmployerDepositAmount
      , EmployerId
      , EmployerPayPeriodElection
      , FirstName
      , LastName
      , MiddleInitial
      , MobileNumber
      , OriginalPrefunded
      , Phone
      , PlanEndDate
      , PlanId
      , PlanStartDate
      , Relationship
      , State
      , TerminationDate
      , TpaId
      , Zip
    FROM
        [Alegeus_ErrorLog].[dbo].[mbi_file_table_stage]
    
    --    truncate table [Alegeus_ErrorLog].[dbo].[mbi_file_table_stage];
end
go


alter VIEW dbo.[error_log_results_withmbi_with_CRM]
    AS
        SELECT
            dbo.CRM_Listview.CRM
          , mbi_file_name
          , res_file_name
          , row_type
          , error_row
          , error_code
          , error_message
          , EmployerId
          , EmployeeID
          , DependentID
          , PlanId
          , error_row_num
          , mbi_row_num
          , mbi_line
        FROM
            dbo.error_log_results_withmbi
                LEFT OUTER JOIN
                dbo.CRM_Listview ON dbo.error_log_results_withmbi.EmployerId = dbo.CRM_Listview.BENCODE
go

alter table CRM_List
    alter column CRM nvarchar(100) null
alter table CRM_List
    alter column CRM_email nvarchar(500) null
alter table CRM_List
    alter column emp_services nvarchar(500) null
alter table CRM_List
    alter column Primary_contact_name nvarchar(500) null
alter table CRM_List
    alter column Primary_contact_email nvarchar(500) null

alter VIEW dbo.[CRM_Listview]
    AS
        SELECT distinct
            BENCODE
          , CRM
          , CRM_email
        FROM
            /*[Low_balance_reporting].dbo.[CRM_List_contacts]*/
            CRM_List
go

alter VIEW dbo.[error_log_results_withmbi_with_CRM]
    AS
        SELECT distinct
            dbo.CRM_Listview.CRM
          , dbo.CRM_Listview.CRM_email
          , mbi_file_name
          , res_file_name
          , row_type
          , error_row
          , error_code
          , error_message
          , EmployerId
          , EmployeeID
          , DependentID
          , PlanId
          , error_row_num
          , mbi_row_num
          , mbi_line
        FROM
            dbo.error_log_results_withmbi
                LEFT OUTER JOIN
                dbo.CRM_Listview ON dbo.error_log_results_withmbi.EmployerId = dbo.CRM_Listview.BENCODE
go

alter VIEW dbo.RESwithnoMBIrecords
    AS
        SELECT DISTINCT TOP (100) PERCENT
            r.mbi_file_name
        FROM
            dbo.res_file_table AS r
                LEFT OUTER JOIN
                dbo.mbi_file_table AS m ON m.mbi_file_name = r.mbi_file_name
        WHERE
              (m.mbi_file_name IS NULL)
          AND (r.mbi_file_name IS NOT NULL)
go

alter VIEW dbo.[mbi_no_errors]
    AS
        SELECT distinct
            m.mbi_file_name --, m.data_row AS mbi_line
        FROM
            dbo.mbi_file_table AS m
                left join (
                              select distinct
                                  mbi_file_name
                              from
                                  dbo.[res_file_table]
                          ) as r
                          on m.mbi_file_name = r.mbi_file_name
        WHERE
            --              m.row_type IN ('IH', 'IB', 'IC', 'IZ', 'II', 'ID') and
            r.mbi_file_name is null
go


use Alegeus_ErrorLog;

alter procedure zz_truncate_all as
begin
    truncate table Alegeus_File_Processing.dbo.file_processing_log;
    
    truncate table Alegeus_File_Processing.dbo.file_processing_tasks_log;
    truncate table Alegeus_File_Processing.dbo.message_log;
    
    truncate table [Alegeus_ErrorLog].[dbo].[res_file_table];
    truncate table [Alegeus_ErrorLog].[dbo].[res_file_table_stage];
    truncate table [Alegeus_ErrorLog].[dbo].[mbi_file_table];
    truncate table [Alegeus_ErrorLog].[dbo].[mbi_file_table_stage];
    
    truncate table [Alegeus_File_Processing].[dbo].[alegeus_file_final];
    
    truncate table [Alegeus_ErrorLog]..dbo_tracked_errors_local;
    truncate table [Alegeus_ErrorLog]..dbo_error_log_results_workflow_local;
end;


alter VIEW dbo.[error_log_results_withmbi]
    AS
        SELECT
            e.mbi_file_name
          , e.res_file_name
          , e.row_type
          , e.error_row
          , e.error_code
          , e.error_message
          , e.EmployerId
          , e.EmployeeID
          , e.DependentID
          , e.PlanId
          , e.row_num error_row_num
          , m.row_num mbi_row_num
          , m.data_row mbi_line
        FROM
            dbo.error_log_results
                AS e
                Left JOIN
                dbo.mbi_file_table AS m ON e.mbi_file_name = m.mbi_file_name
                    --                     AND (e.EmployerId = m.EmployerId and e.EmployeeID = m.EmployeeID and e.DependentID = m.DependentID)
                    and e.row_num = m.row_num
go
