create
 procedure dbo.process_res_file_table_stage_import as
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
                            [dbo].[res_file_table_stage]
                        where
                            [error_row] like '%.mbi%'
                    );
    
    /* update filename for all rows*/
    if (@filename is not null and @filename <> '')
        begin
            update [dbo].[res_file_table_stage]
            set
                mbi_file_name = @filename
        end;
    
    update [dbo].[res_file_table_stage]
    set
        /* remove extra csv commas added to line */
        data_row  = replace( data_row , ',,,,,,,,,,,,,,,,,,,,' , '' ),
        error_row = replace( error_row , ',,,,,,,,,,,,,,,,,,,,' , '' )
    
    /* save org_error_row*/
    update [dbo].[res_file_table_stage]
    set
        org_error_row = error_row
    where
        isnull( org_error_row , '' ) = '';
    
    update [dbo].[res_file_table_stage]
    set
        org_data_row = data_row
    where
        isnull( org_data_row , '' ) = '';
    
    /* update a*ny missing */
    update [dbo].[res_file_table_stage]
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
    
    /* upsert  into main res table*/
    MERGE dbo.res_file_table AS tgt
    USING (
              SELECT
                  AccountStatus
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
                , error_code
                , error_message
                , error_message_calc
                , error_row
                , org_error_row
                , org_data_row
                , FirstName
                , LastName
                , mbi_file_name
                , MiddleInitial
                , MobileNumber
                , OriginalPrefunded
                , Phone
                , PlanEndDate
                , PlanId
                , PlanStartDate
                , Relationship
                , res_file_name
                , result_template
                , row_num
                , row_type
                , State
                , TerminationDate
                , TpaId
                , Zip
                , source_row_no
                , check_type
                , Class
                , AlternateId
        
              from
                  dbo.res_file_table_stage
          ) as src
    ON (tgt.mbi_file_name = src.mbi_file_name and tgt.source_row_no = src.source_row_no)
    WHEN MATCHED THEN
        UPDATE
        SET
            AccountStatus=src.AccountStatus,
            AccountTypeCode=src.AccountTypeCode,
            AddressLine1=src.AddressLine1,
            AddressLine2=src.AddressLine2,
            BirthDate=src.BirthDate,
            City=src.City,
            Country=src.Country,
            DeleteAccount=src.DeleteAccount,
            DependentID=src.DependentID,
            DepositType=src.DepositType,
            Division=src.Division,
            EffectiveDate=src.EffectiveDate,
            EligibilityDate=src.EligibilityDate,
            Email=src.Email,
            EmployeeDepositAmount=src.EmployeeDepositAmount,
            EmployeeID=src.EmployeeID,
            EmployeePayPeriodElection=src.EmployeePayPeriodElection,
            EmployeeSocialSecurityNumber=src.EmployeeSocialSecurityNumber,
            EmployeeStatus=src.EmployeeStatus,
            EmployerDepositAmount=src.EmployerDepositAmount,
            EmployerId=src.EmployerId,
            EmployerPayPeriodElection=src.EmployerPayPeriodElection,
            error_code=src.error_code,
            error_message=src.error_message,
            error_message_calc=src.error_message_calc,
            error_row=src.error_row,
            org_error_row= src.org_error_row,
            org_data_row = src.org_data_row,
            FirstName=src.FirstName,
            LastName=src.LastName,
            mbi_file_name=src.mbi_file_name,
            MiddleInitial=src.MiddleInitial,
            MobileNumber=src.MobileNumber,
            OriginalPrefunded=src.OriginalPrefunded,
            Phone=src.Phone,
            PlanEndDate=src.PlanEndDate,
            PlanId=src.PlanId,
            PlanStartDate=src.PlanStartDate,
            Relationship=src.Relationship,
            res_file_name=src.res_file_name,
            result_template=src.result_template,
            row_num=src.row_num,
            row_type=src.row_type,
            State=src.State,
            TerminationDate=src.TerminationDate,
            TpaId=src.TpaId,
            Zip=src.Zip,
            source_row_no=src.source_row_no,
            check_type=src.check_type,
            Class= src.Class,
            AlternateId = src.AlternateId
    
    WHEN NOT MATCHED THEN
        INSERT (
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
               error_code,
               error_message,
               error_message_calc,
               error_row,
               org_error_row,
               org_data_row,
               FirstName,
               LastName,
               mbi_file_name,
               MiddleInitial,
               MobileNumber,
               OriginalPrefunded,
               Phone,
               PlanEndDate,
               PlanId,
               PlanStartDate,
               Relationship,
               res_file_name,
               result_template,
               row_num,
               row_type,
               State,
               TerminationDate,
               TpaId,
               Zip,
               source_row_no,
               check_type,
               Class,
               AlternateId
        )
        VALUES (
               src.AccountStatus,
               src.AccountTypeCode,
               src.AddressLine1,
               src.AddressLine2,
               src.BirthDate,
               src.City,
               src.Country,
               src.DeleteAccount,
               src.DependentID,
               src.DepositType,
               src.Division,
               src.EffectiveDate,
               src.EligibilityDate,
               src.Email,
               src.EmployeeDepositAmount,
               src.EmployeeID,
               src.EmployeePayPeriodElection,
               src.EmployeeSocialSecurityNumber,
               src.EmployeeStatus,
               src.EmployerDepositAmount,
               src.EmployerId,
               src.EmployerPayPeriodElection,
               src.error_code,
               src.error_message,
               src.error_message_calc,
               src.error_row,
               src.org_error_row,
               src.org_data_row,
               src.FirstName,
               src.LastName,
               src.mbi_file_name,
               src.MiddleInitial,
               src.MobileNumber,
               src.OriginalPrefunded,
               src.Phone,
               src.PlanEndDate,
               src.PlanId,
               src.PlanStartDate,
               src.Relationship,
               src.res_file_name,
               src.result_template,
               src.row_num,
               src.row_type,
               src.State,
               src.TerminationDate,
               src.TpaId,
               src.Zip,
               src.source_row_no,
               src.check_type,
               src.Class,
               src.AlternateId
               );
    
    --  truncate table [dbo].[res_file_table_stage];

end
go

