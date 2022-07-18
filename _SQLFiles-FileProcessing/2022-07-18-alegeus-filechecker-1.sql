use Data_Processing;

create  or alter
 procedure dbo.process_mbi_file_table_stage_import as
begin
    
    update [dbo].[mbi_file_table_stage]
    set
        /* remove extra csv commas added to line */
        data_row = replace( data_row , ',,,,,,,,,,,,,,,,,,,,' , '' );
    
    /* ensure org data is saved before we fix the data given by client*/
    update [dbo].[mbi_file_table_stage]
    set
        /* remove extra csv commas added to line */
        org_data_row = data_row
    where
        isnull( org_data_row , '' ) = '';
    
    /* upsert  into main res table*/
    MERGE dbo.mbi_file_table AS tgt
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
                  --                 , error_code
                  --                 , error_message
                  --                 , error_message_calc
                , data_row
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
                  --                 , res_file_name
                  --                 , result_template
                , row_num
                , row_type
                , State
                , TerminationDate
                , TpaId
                , Zip
                , source_row_no
                , check_type
                , error_code
                , error_message
                , error_message_calc
                , Class
                , AlternateId
              from
                  dbo.mbi_file_table_stage
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
            --             error_code=src.error_code,
            --             error_message=src.error_message,
            --             error_message_calc=src.error_message_calc,
            data_row=src.data_row,
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
            --             res_file_name=src.res_file_name,
            --             result_template=src.result_template,
            row_num=src.row_num,
            row_type=src.row_type,
            State=src.State,
            TerminationDate=src.TerminationDate,
            TpaId=src.TpaId,
            Zip=src.Zip,
            source_row_no=src.source_row_no,
            check_type=src.check_type,
            error_code=src.error_code,
            error_message=src.error_message,
            error_message_calc=src.error_message_calc,
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
            --                error_code,
            --                error_message,
            --                error_message_calc,
               data_row,
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
            --                res_file_name,
            --                result_template,
               row_num,
               row_type,
               State,
               TerminationDate,
               TpaId,
               Zip,
               source_row_no,
               check_type,
               error_code,
               error_message,
               error_message_calc,
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
                   --                src.error_code,
                   --                src.error_message,
                   --                src.error_message_calc,
               src.data_row,
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
                   --                src.res_file_name,
                   --                src.result_template,
               src.row_num,
               src.row_type,
               src.State,
               src.TerminationDate,
               src.TpaId,
               src.Zip,
               src.source_row_no,
               src.check_type,
               src.error_code,
               src.error_message,
               src.error_message_calc,
               src.Class,
               src.AlternateId
               );
    
    --    truncate table [dbo].[mbi_file_table_stage];
end
go


alter table mbi_file_table
    drop constraint mbi_file_table_PK
go

create unique index mbi_file_table_UK
    on mbi_file_table (mbi_file_name, source_row_no)
go

drop index uk_mbi_name_row_num on mbi_file_table
go

