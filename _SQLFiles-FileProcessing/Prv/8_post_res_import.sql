use Alegeus_ErrorLog;

alter procedure dbo.process_res_file_table_stage_import as
begin
    -- Script for SelectTopNRows command from SSMS
    declare @filename as varchar(200)
    
    /* get filename from import log*/
    set @filename = (
                        SELECT top 1
                            ltrim( rtrim( substring( error_row , charindex( ',' , ltrim( error_row ) ) + 1 ,
                                                     charindex( '.mbi' , error_row , 1 ) + 3 -
                                                     charindex( ',' , ltrim( error_row ) ) - 4 ) ) )
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

end
go
