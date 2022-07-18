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

create or alter
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
alter table res_file_table
    drop constraint res_file_table_PK
go


go
drop index uk_name_row_num on res_file_table
go


-- auto-generated definition
create unique index uk_name_source_row_num
    on res_file_table (res_file_name, source_row_no)
go


create or
alter procedure dbo.process_cobra_file_table_stage_import as
begin
    
    update [dbo].[cobra_file_table_stage]
    set
        /* remove extra csv commas added to line */
        data_row = replace( data_row , ',,,,,,,,,,,,,,,,,,,,' , '' );
    
    /* upsert  into main res table*/
    MERGE dbo.cobra_file_table AS tgt
    USING (
              SELECT
                  cobra_file_name
                , data_row
                , row_num
                , row_type
                , source_row_no
                , check_type
                , error_code
                , error_message
                , error_message_calc
                , VersionNumber
                , ClientName
                , ClientDivisionName
                , Salutation
                , FirstName
                , MiddleInitial
                , LastName
                , SSN
                , IndividualID
                , Email
                , Phone
                , Phone2
                , Address1
                , Address2
                , City
                , StateOrProvince
                , PostalCode
                , Country
                , PremiumAddressSameAsPrimary
                , PremiumAddress1
                , PremiumAddress2
                , PremiumCity
                , PremiumStateOrProvince
                , PremiumPostalCode
                , PremiumCountry
                , Sex
                , DOB
                , TobaccoUse
                , EmployeeType
                , EmployeePayrollType
                , YearsOfService
                , PremiumCouponType
                , UsesHCTC
                , Active
                , AllowMemberSSO
                , BenefitGroup
                , AccountStructure
                , ClientSpecificData
                , SSOIdentifier
                , PlanCategory
                , EventType
                , EventDate
                , EnrollmentDate
                , EmployeeSSN
                , EmployeeName
                , SecondEventOriginalFDOC
                , DateSpecificRightsNoticeWasPrinted
                , PostmarkDateOfElection
                , IsPaidThroughLastDayOfCOBRA
                , NextPremiumOwedMonth
                , NextPremiumOwedYear
                , NextPremiumOwedAmountReceived
                , SendTakeoverLetter
                , IsConversionLetterSent
                , SendDODSubsidyExtension
                , PlanName
                , CoverageLevel
                , NumberOfUnit
                , StartDate
                , EndDate
                , FirstDayOfCOBRA
                , LastDayOfCOBRA
                , COBRADurationMonths
                , DaysToElect
                , DaysToMake1stPayment
                , DaysToMakeSubsequentPayments
                , ElectionPostmarkDate
                , LastDateRatesNotified
                , NumberOfUnits
                , SendPlanChangeLetterForLegacy
                , PlanBundleName
                , Relationship
                , AddressSameAsQB
                , IsQMCSO
                , UsesFDOC
                , NoteType
                , DateTime
                , NoteText
                , UserName
                , InsuranceType
                , SubsidyAmountType
                , Amount
                , SubsidyType
                , RatePeriodSubsidy
                , CASRINSERT
                , CTSRINSERT
                , MNLIFEINSERT
                , MNCONTINSERT
                , ORSRINSERT
                , TXSRINSERT
                , NYSRINSERT
                , VEBASRINSERT
                , ILSRINSERT
                , RISRINSERT
                , GASRINSERT
                , VASRINSERT
                , DisabilityApproved
                , PostmarkOfDisabilityExtension
                , DateDisabled
                , DenialReason
                , Rate
                , TermOrReinstate
                , EffectiveDate
                , Reason
                , LetterAttachmentName
                , QualifyingEventDate
                , UserDefinedFieldName
                , UserDefinedFieldValue
                , AddressSameAsSPM
                , BillingEndDate
                , BillingFrequency
                , BillingPeriodSeedDate
                , BillingStartDate
                , BillingType
                , FirstDayOfCoverage
                , FullName
                , HasWaivedAllCoverage
                , HireDate
                , InitialGracePeriodDate
                , InitialGracePeriodDays
                , Invalid
                , IsCOBRAEligible
                , IsCOBRAEligibleAtTermination
                , IsLegacy
                , LastDayOfCoverage
                , OriginalEnrollmentDate
                , SecondBillingPeriodSeedDate
                , SendGRNotice
                , SPMInitialGracePeriodOptionType
                , SPMSubsequentGracePeriodOptionType
                , SubsequentGracePeriodNrOfDays
                , UsesFamilyInAddress
                , UsesFirstDayOfCoverage
        
              from
                  dbo.cobra_file_table_stage
          ) as src
    ON (tgt.cobra_file_name = src.cobra_file_name and tgt.source_row_no = src.source_row_no)
    WHEN MATCHED THEN
        UPDATE
        SET
            cobra_file_name=src.cobra_file_name
          , data_row=src.data_row
          , row_num=src.row_num
          , row_type=src.row_type
          , source_row_no=src.source_row_no
          , check_type=src.check_type
          , error_code=src.error_code
          , error_message=src.error_message
          , error_message_calc=src.error_message_calc
          , VersionNumber=src.VersionNumber
          , ClientName=src.ClientName
          , ClientDivisionName=src.ClientDivisionName
          , Salutation=src.Salutation
          , FirstName=src.FirstName
          , MiddleInitial=src.MiddleInitial
          , LastName=src.LastName
          , SSN=src.SSN
          , IndividualID=src.IndividualID
          , Email=src.Email
          , Phone=src.Phone
          , Phone2=src.Phone2
          , Address1=src.Address1
          , Address2=src.Address2
          , City=src.City
          , StateOrProvince=src.StateOrProvince
          , PostalCode=src.PostalCode
          , Country=src.Country
          , PremiumAddressSameAsPrimary=src.PremiumAddressSameAsPrimary
          , PremiumAddress1=src.PremiumAddress1
          , PremiumAddress2=src.PremiumAddress2
          , PremiumCity=src.PremiumCity
          , PremiumStateOrProvince=src.PremiumStateOrProvince
          , PremiumPostalCode=src.PremiumPostalCode
          , PremiumCountry=src.PremiumCountry
          , Sex=src.Sex
          , DOB=src.DOB
          , TobaccoUse=src.TobaccoUse
          , EmployeeType=src.EmployeeType
          , EmployeePayrollType=src.EmployeePayrollType
          , YearsOfService=src.YearsOfService
          , PremiumCouponType=src.PremiumCouponType
          , UsesHCTC=src.UsesHCTC
          , Active=src.Active
          , AllowMemberSSO=src.AllowMemberSSO
          , BenefitGroup=src.BenefitGroup
          , AccountStructure=src.AccountStructure
          , ClientSpecificData=src.ClientSpecificData
          , SSOIdentifier=src.SSOIdentifier
          , PlanCategory=src.PlanCategory
          , EventType=src.EventType
          , EventDate=src.EventDate
          , EnrollmentDate=src.EnrollmentDate
          , EmployeeSSN=src.EmployeeSSN
          , EmployeeName=src.EmployeeName
          , SecondEventOriginalFDOC=src.SecondEventOriginalFDOC
          , DateSpecificRightsNoticeWasPrinted=src.DateSpecificRightsNoticeWasPrinted
          , PostmarkDateOfElection=src.PostmarkDateOfElection
          , IsPaidThroughLastDayOfCOBRA=src.IsPaidThroughLastDayOfCOBRA
          , NextPremiumOwedMonth=src.NextPremiumOwedMonth
          , NextPremiumOwedYear=src.NextPremiumOwedYear
          , NextPremiumOwedAmountReceived=src.NextPremiumOwedAmountReceived
          , SendTakeoverLetter=src.SendTakeoverLetter
          , IsConversionLetterSent=src.IsConversionLetterSent
          , SendDODSubsidyExtension=src.SendDODSubsidyExtension
          , PlanName=src.PlanName
          , CoverageLevel=src.CoverageLevel
          , NumberOfUnit=src.NumberOfUnit
          , StartDate=src.StartDate
          , EndDate=src.EndDate
          , FirstDayOfCOBRA=src.FirstDayOfCOBRA
          , LastDayOfCOBRA=src.LastDayOfCOBRA
          , COBRADurationMonths=src.COBRADurationMonths
          , DaysToElect=src.DaysToElect
          , DaysToMake1stPayment=src.DaysToMake1stPayment
          , DaysToMakeSubsequentPayments=src.DaysToMakeSubsequentPayments
          , ElectionPostmarkDate=src.ElectionPostmarkDate
          , LastDateRatesNotified=src.LastDateRatesNotified
          , NumberOfUnits=src.NumberOfUnits
          , SendPlanChangeLetterForLegacy=src.SendPlanChangeLetterForLegacy
          , PlanBundleName=src.PlanBundleName
          , Relationship=src.Relationship
          , AddressSameAsQB=src.AddressSameAsQB
          , IsQMCSO=src.IsQMCSO
          , UsesFDOC=src.UsesFDOC
          , NoteType=src.NoteType
          , DateTime=src.DateTime
          , NoteText=src.NoteText
          , UserName=src.UserName
          , InsuranceType=src.InsuranceType
          , SubsidyAmountType=src.SubsidyAmountType
          , Amount=src.Amount
          , SubsidyType=src.SubsidyType
          , RatePeriodSubsidy=src.RatePeriodSubsidy
          , CASRINSERT=src.CASRINSERT
          , CTSRINSERT=src.CTSRINSERT
          , MNLIFEINSERT=src.MNLIFEINSERT
          , MNCONTINSERT=src.MNCONTINSERT
          , ORSRINSERT=src.ORSRINSERT
          , TXSRINSERT=src.TXSRINSERT
          , NYSRINSERT=src.NYSRINSERT
          , VEBASRINSERT=src.VEBASRINSERT
          , ILSRINSERT=src.ILSRINSERT
          , RISRINSERT=src.RISRINSERT
          , GASRINSERT=src.GASRINSERT
          , VASRINSERT=src.VASRINSERT
          , DisabilityApproved=src.DisabilityApproved
          , PostmarkOfDisabilityExtension=src.PostmarkOfDisabilityExtension
          , DateDisabled=src.DateDisabled
          , DenialReason=src.DenialReason
          , Rate=src.Rate
          , TermOrReinstate=src.TermOrReinstate
          , EffectiveDate=src.EffectiveDate
          , Reason=src.Reason
          , LetterAttachmentName=src.LetterAttachmentName
          , QualifyingEventDate=src.QualifyingEventDate
          , UserDefinedFieldName=src.UserDefinedFieldName
          , UserDefinedFieldValue=src.UserDefinedFieldValue
          , AddressSameAsSPM=src.AddressSameAsSPM
          , BillingEndDate=src.BillingEndDate
          , BillingFrequency=src.BillingFrequency
          , BillingPeriodSeedDate=src.BillingPeriodSeedDate
          , BillingStartDate=src.BillingStartDate
          , BillingType=src.BillingType
          , FirstDayOfCoverage=src.FirstDayOfCoverage
          , FullName=src.FullName
          , HasWaivedAllCoverage=src.HasWaivedAllCoverage
          , HireDate=src.HireDate
          , InitialGracePeriodDate=src.InitialGracePeriodDate
          , InitialGracePeriodDays=src.InitialGracePeriodDays
          , Invalid=src.Invalid
          , IsCOBRAEligible=src.IsCOBRAEligible
          , IsCOBRAEligibleAtTermination=src.IsCOBRAEligibleAtTermination
          , IsLegacy=src.IsLegacy
          , LastDayOfCoverage=src.LastDayOfCoverage
          , OriginalEnrollmentDate=src.OriginalEnrollmentDate
          , SecondBillingPeriodSeedDate=src.SecondBillingPeriodSeedDate
          , SendGRNotice=src.SendGRNotice
          , SPMInitialGracePeriodOptionType=src.SPMInitialGracePeriodOptionType
          , SPMSubsequentGracePeriodOptionType=src.SPMSubsequentGracePeriodOptionType
          , SubsequentGracePeriodNrOfDays=src.SubsequentGracePeriodNrOfDays
          , UsesFamilyInAddress=src.UsesFamilyInAddress
          , UsesFirstDayOfCoverage=src.UsesFirstDayOfCoverage
    
    WHEN NOT MATCHED THEN
        INSERT (
                 cobra_file_name
               , data_row
               , row_num
               , row_type
               , source_row_no
               , check_type
               , error_code
               , error_message
               , error_message_calc
               , VersionNumber
               , ClientName
               , ClientDivisionName
               , Salutation
               , FirstName
               , MiddleInitial
               , LastName
               , SSN
               , IndividualID
               , Email
               , Phone
               , Phone2
               , Address1
               , Address2
               , City
               , StateOrProvince
               , PostalCode
               , Country
               , PremiumAddressSameAsPrimary
               , PremiumAddress1
               , PremiumAddress2
               , PremiumCity
               , PremiumStateOrProvince
               , PremiumPostalCode
               , PremiumCountry
               , Sex
               , DOB
               , TobaccoUse
               , EmployeeType
               , EmployeePayrollType
               , YearsOfService
               , PremiumCouponType
               , UsesHCTC
               , Active
               , AllowMemberSSO
               , BenefitGroup
               , AccountStructure
               , ClientSpecificData
               , SSOIdentifier
               , PlanCategory
               , EventType
               , EventDate
               , EnrollmentDate
               , EmployeeSSN
               , EmployeeName
               , SecondEventOriginalFDOC
               , DateSpecificRightsNoticeWasPrinted
               , PostmarkDateOfElection
               , IsPaidThroughLastDayOfCOBRA
               , NextPremiumOwedMonth
               , NextPremiumOwedYear
               , NextPremiumOwedAmountReceived
               , SendTakeoverLetter
               , IsConversionLetterSent
               , SendDODSubsidyExtension
               , PlanName
               , CoverageLevel
               , NumberOfUnit
               , StartDate
               , EndDate
               , FirstDayOfCOBRA
               , LastDayOfCOBRA
               , COBRADurationMonths
               , DaysToElect
               , DaysToMake1stPayment
               , DaysToMakeSubsequentPayments
               , ElectionPostmarkDate
               , LastDateRatesNotified
               , NumberOfUnits
               , SendPlanChangeLetterForLegacy
               , PlanBundleName
               , Relationship
               , AddressSameAsQB
               , IsQMCSO
               , UsesFDOC
               , NoteType
               , DateTime
               , NoteText
               , UserName
               , InsuranceType
               , SubsidyAmountType
               , Amount
               , SubsidyType
               , RatePeriodSubsidy
               , CASRINSERT
               , CTSRINSERT
               , MNLIFEINSERT
               , MNCONTINSERT
               , ORSRINSERT
               , TXSRINSERT
               , NYSRINSERT
               , VEBASRINSERT
               , ILSRINSERT
               , RISRINSERT
               , GASRINSERT
               , VASRINSERT
               , DisabilityApproved
               , PostmarkOfDisabilityExtension
               , DateDisabled
               , DenialReason
               , Rate
               , TermOrReinstate
               , EffectiveDate
               , Reason
               , LetterAttachmentName
               , QualifyingEventDate
               , UserDefinedFieldName
               , UserDefinedFieldValue
               , AddressSameAsSPM
               , BillingEndDate
               , BillingFrequency
               , BillingPeriodSeedDate
               , BillingStartDate
               , BillingType
               , FirstDayOfCoverage
               , FullName
               , HasWaivedAllCoverage
               , HireDate
               , InitialGracePeriodDate
               , InitialGracePeriodDays
               , Invalid
               , IsCOBRAEligible
               , IsCOBRAEligibleAtTermination
               , IsLegacy
               , LastDayOfCoverage
               , OriginalEnrollmentDate
               , SecondBillingPeriodSeedDate
               , SendGRNotice
               , SPMInitialGracePeriodOptionType
               , SPMSubsequentGracePeriodOptionType
               , SubsequentGracePeriodNrOfDays
               , UsesFamilyInAddress
               , UsesFirstDayOfCoverage
        
        )
        VALUES (
                   src.cobra_file_name
               ,   src.data_row
               ,   src.row_num
               ,   src.row_type
               ,   src.source_row_no
               ,   src.check_type
               ,   src.error_code
               ,   src.error_message
               ,   src.error_message_calc
               ,   src.VersionNumber
               ,   src.ClientName
               ,   src.ClientDivisionName
               ,   src.Salutation
               ,   src.FirstName
               ,   src.MiddleInitial
               ,   src.LastName
               ,   src.SSN
               ,   src.IndividualID
               ,   src.Email
               ,   src.Phone
               ,   src.Phone2
               ,   src.Address1
               ,   src.Address2
               ,   src.City
               ,   src.StateOrProvince
               ,   src.PostalCode
               ,   src.Country
               ,   src.PremiumAddressSameAsPrimary
               ,   src.PremiumAddress1
               ,   src.PremiumAddress2
               ,   src.PremiumCity
               ,   src.PremiumStateOrProvince
               ,   src.PremiumPostalCode
               ,   src.PremiumCountry
               ,   src.Sex
               ,   src.DOB
               ,   src.TobaccoUse
               ,   src.EmployeeType
               ,   src.EmployeePayrollType
               ,   src.YearsOfService
               ,   src.PremiumCouponType
               ,   src.UsesHCTC
               ,   src.Active
               ,   src.AllowMemberSSO
               ,   src.BenefitGroup
               ,   src.AccountStructure
               ,   src.ClientSpecificData
               ,   src.SSOIdentifier
               ,   src.PlanCategory
               ,   src.EventType
               ,   src.EventDate
               ,   src.EnrollmentDate
               ,   src.EmployeeSSN
               ,   src.EmployeeName
               ,   src.SecondEventOriginalFDOC
               ,   src.DateSpecificRightsNoticeWasPrinted
               ,   src.PostmarkDateOfElection
               ,   src.IsPaidThroughLastDayOfCOBRA
               ,   src.NextPremiumOwedMonth
               ,   src.NextPremiumOwedYear
               ,   src.NextPremiumOwedAmountReceived
               ,   src.SendTakeoverLetter
               ,   src.IsConversionLetterSent
               ,   src.SendDODSubsidyExtension
               ,   src.PlanName
               ,   src.CoverageLevel
               ,   src.NumberOfUnit
               ,   src.StartDate
               ,   src.EndDate
               ,   src.FirstDayOfCOBRA
               ,   src.LastDayOfCOBRA
               ,   src.COBRADurationMonths
               ,   src.DaysToElect
               ,   src.DaysToMake1stPayment
               ,   src.DaysToMakeSubsequentPayments
               ,   src.ElectionPostmarkDate
               ,   src.LastDateRatesNotified
               ,   src.NumberOfUnits
               ,   src.SendPlanChangeLetterForLegacy
               ,   src.PlanBundleName
               ,   src.Relationship
               ,   src.AddressSameAsQB
               ,   src.IsQMCSO
               ,   src.UsesFDOC
               ,   src.NoteType
               ,   src.DateTime
               ,   src.NoteText
               ,   src.UserName
               ,   src.InsuranceType
               ,   src.SubsidyAmountType
               ,   src.Amount
               ,   src.SubsidyType
               ,   src.RatePeriodSubsidy
               ,   src.CASRINSERT
               ,   src.CTSRINSERT
               ,   src.MNLIFEINSERT
               ,   src.MNCONTINSERT
               ,   src.ORSRINSERT
               ,   src.TXSRINSERT
               ,   src.NYSRINSERT
               ,   src.VEBASRINSERT
               ,   src.ILSRINSERT
               ,   src.RISRINSERT
               ,   src.GASRINSERT
               ,   src.VASRINSERT
               ,   src.DisabilityApproved
               ,   src.PostmarkOfDisabilityExtension
               ,   src.DateDisabled
               ,   src.DenialReason
               ,   src.Rate
               ,   src.TermOrReinstate
               ,   src.EffectiveDate
               ,   src.Reason
               ,   src.LetterAttachmentName
               ,   src.QualifyingEventDate
               ,   src.UserDefinedFieldName
               ,   src.UserDefinedFieldValue
               ,   src.AddressSameAsSPM
               ,   src.BillingEndDate
               ,   src.BillingFrequency
               ,   src.BillingPeriodSeedDate
               ,   src.BillingStartDate
               ,   src.BillingType
               ,   src.FirstDayOfCoverage
               ,   src.FullName
               ,   src.HasWaivedAllCoverage
               ,   src.HireDate
               ,   src.InitialGracePeriodDate
               ,   src.InitialGracePeriodDays
               ,   src.Invalid
               ,   src.IsCOBRAEligible
               ,   src.IsCOBRAEligibleAtTermination
               ,   src.IsLegacy
               ,   src.LastDayOfCoverage
               ,   src.OriginalEnrollmentDate
               ,   src.SecondBillingPeriodSeedDate
               ,   src.SendGRNotice
               ,   src.SPMInitialGracePeriodOptionType
               ,   src.SPMSubsequentGracePeriodOptionType
               ,   src.SubsequentGracePeriodNrOfDays
               ,   src.UsesFamilyInAddress
               ,   src.UsesFirstDayOfCoverage
               );
    
    --    truncate table [dbo].[cobra_file_table_stage];
end
go

alter table cobra_file_table
    drop constraint cobra_file_table_PK
go
drop index uk_mbi_name_row_num on cobra_file_table
go

create unique index uk_name_source_row_num
    on cobra_file_table (cobra_file_name, source_row_no)
go
create   or alter
    procedure dbo.process_cobra_res_file_table_stage_import as
begin
    -- Script for SelectTopNRows command from SSMS
    declare @filename as varchar(200)
    
    /* get filename from import log*/
    set @filename = (
                        SELECT top 1
                            ltrim( rtrim( substring( error_row , charindex( ',' , ltrim( error_row ) ) + 1 ,
                                                     charindex( '.csv' , error_row , 1 ) + 3 -
                                                     charindex( ',' , ltrim( error_row ) ) /*- 4*//*sumeet keep[ mbi extension*/ ) ) )
                        FROM
                            [dbo].[cobra_res_file_table_stage]
                        where
                            [error_row] like '%.csv%'
                    );
    
    /* update filename for all rows*/
    if (@filename is not null and @filename <> '')
        begin
            update [dbo].[cobra_res_file_table_stage]
            set
                cobra_res_file_name = @filename,
                /* remove extra csv commas added to line */
                data_row            = replace( data_row , ',,,,,,,,,,,,,,,,,,,,' , '' ),
                error_row           = replace( error_row , ',,,,,,,,,,,,,,,,,,,,' , '' )
            FROM
                [dbo].[cobra_res_file_table_stage]
        end;
    
    /* update a*ny missing */
    update [dbo].[cobra_res_file_table_stage]
    set
        cobra_res_file_name = cobra_res_file_name
    where
        (cobra_res_file_name is null or cobra_res_file_name = '');
    
    /* clear error code from header row*/
    update dbo.cobra_res_file_table_stage
    set
        error_code=null,
        error_message = null
    where
        row_type = 'RA';
    
    /* parse error code and take error message from master */
    update [dbo].[cobra_res_file_table_stage]
    set
        error_code         = e.error_code,
        error_message_calc = e.user_desc
    FROM
        [dbo].[cobra_res_file_table_stage] as t
            join [dbo].[error_codes] as e
                 on e.error_code = t.error_code;
    
    /* ion case we did not import error message, set it from the calc one*/
    update [dbo].[cobra_res_file_table_stage]
    set
        error_message = error_message_calc
    where
         error_message is null
      or error_message = '';
    
    /* upsert  into main res table*/
    MERGE dbo.cobra_res_file_table AS tgt
    USING (
              SELECT
                  result_template
                , error_row
                , error_code
                , error_message
                , row_num
                , row_type
                , error_message_calc
                , cobra_res_file_name
                , data_row
                , source_row_no
                , check_type
                , VersionNumber
                , ClientName
                , ClientDivisionName
                , Salutation
                , FirstName
                , MiddleInitial
                , LastName
                , SSN
                , IndividualID
                , Email
                , Phone
                , Phone2
                , Address1
                , Address2
                , City
                , StateOrProvince
                , PostalCode
                , Country
                , PremiumAddressSameAsPrimary
                , PremiumAddress1
                , PremiumAddress2
                , PremiumCity
                , PremiumStateOrProvince
                , PremiumPostalCode
                , PremiumCountry
                , Sex
                , DOB
                , TobaccoUse
                , EmployeeType
                , EmployeePayrollType
                , YearsOfService
                , PremiumCouponType
                , UsesHCTC
                , Active
                , AllowMemberSSO
                , BenefitGroup
                , AccountStructure
                , ClientSpecificData
                , SSOIdentifier
                , PlanCategory
                , EventType
                , EventDate
                , EnrollmentDate
                , EmployeeSSN
                , EmployeeName
                , SecondEventOriginalFDOC
                , DateSpecificRightsNoticeWasPrinted
                , PostmarkDateOfElection
                , IsPaidThroughLastDayOfCOBRA
                , NextPremiumOwedMonth
                , NextPremiumOwedYear
                , NextPremiumOwedAmountReceived
                , SendTakeoverLetter
                , IsConversionLetterSent
                , SendDODSubsidyExtension
                , PlanName
                , CoverageLevel
                , NumberOfUnit
                , StartDate
                , EndDate
                , FirstDayOfCOBRA
                , LastDayOfCOBRA
                , COBRADurationMonths
                , DaysToElect
                , DaysToMake1stPayment
                , DaysToMakeSubsequentPayments
                , ElectionPostmarkDate
                , LastDateRatesNotified
                , NumberOfUnits
                , SendPlanChangeLetterForLegacy
                , PlanBundleName
                , Relationship
                , AddressSameAsQB
                , IsQMCSO
                , UsesFDOC
                , NoteType
                , DateTime
                , NoteText
                , UserName
                , InsuranceType
                , SubsidyAmountType
                , Amount
                , SubsidyType
                , RatePeriodSubsidy
                , CASRINSERT
                , CTSRINSERT
                , MNLIFEINSERT
                , MNCONTINSERT
                , ORSRINSERT
                , TXSRINSERT
                , NYSRINSERT
                , VEBASRINSERT
                , ILSRINSERT
                , RISRINSERT
                , GASRINSERT
                , VASRINSERT
                , DisabilityApproved
                , PostmarkOfDisabilityExtension
                , DateDisabled
                , DenialReason
                , Rate
                , TermOrReinstate
                , EffectiveDate
                , Reason
                , LetterAttachmentName
                , QualifyingEventDate
                , UserDefinedFieldName
                , UserDefinedFieldValue
                , AddressSameAsSPM
                , BillingEndDate
                , BillingFrequency
                , BillingPeriodSeedDate
                , BillingStartDate
                , BillingType
                , FirstDayOfCoverage
                , FullName
                , HasWaivedAllCoverage
                , HireDate
                , InitialGracePeriodDate
                , InitialGracePeriodDays
                , Invalid
                , IsCOBRAEligible
                , IsCOBRAEligibleAtTermination
                , IsLegacy
                , LastDayOfCoverage
                , OriginalEnrollmentDate
                , SecondBillingPeriodSeedDate
                , SendGRNotice
                , SPMInitialGracePeriodOptionType
                , SPMSubsequentGracePeriodOptionType
                , SubsequentGracePeriodNrOfDays
                , UsesFamilyInAddress
                , UsesFirstDayOfCoverage
        
              from
                  dbo.cobra_res_file_table_stage
          ) as src
    ON (tgt.cobra_res_file_name = src.cobra_res_file_name and tgt.source_row_no = src.source_row_no)
    WHEN MATCHED THEN
        UPDATE
        SET
            result_template=src.result_template
          , error_row=src.error_row
          , error_code=src.error_code
          , error_message=src.error_message
          , row_num=src.row_num
          , row_type=src.row_type
          , error_message_calc=src.error_message_calc
          , cobra_res_file_name=src.cobra_res_file_name
          , data_row=src.data_row
          , source_row_no=src.source_row_no
          , check_type=src.check_type
          , VersionNumber=src.VersionNumber
          , ClientName=src.ClientName
          , ClientDivisionName=src.ClientDivisionName
          , Salutation=src.Salutation
          , FirstName=src.FirstName
          , MiddleInitial=src.MiddleInitial
          , LastName=src.LastName
          , SSN=src.SSN
          , IndividualID=src.IndividualID
          , Email=src.Email
          , Phone=src.Phone
          , Phone2=src.Phone2
          , Address1=src.Address1
          , Address2=src.Address2
          , City=src.City
          , StateOrProvince=src.StateOrProvince
          , PostalCode=src.PostalCode
          , Country=src.Country
          , PremiumAddressSameAsPrimary=src.PremiumAddressSameAsPrimary
          , PremiumAddress1=src.PremiumAddress1
          , PremiumAddress2=src.PremiumAddress2
          , PremiumCity=src.PremiumCity
          , PremiumStateOrProvince=src.PremiumStateOrProvince
          , PremiumPostalCode=src.PremiumPostalCode
          , PremiumCountry=src.PremiumCountry
          , Sex=src.Sex
          , DOB=src.DOB
          , TobaccoUse=src.TobaccoUse
          , EmployeeType=src.EmployeeType
          , EmployeePayrollType=src.EmployeePayrollType
          , YearsOfService=src.YearsOfService
          , PremiumCouponType=src.PremiumCouponType
          , UsesHCTC=src.UsesHCTC
          , Active=src.Active
          , AllowMemberSSO=src.AllowMemberSSO
          , BenefitGroup=src.BenefitGroup
          , AccountStructure=src.AccountStructure
          , ClientSpecificData=src.ClientSpecificData
          , SSOIdentifier=src.SSOIdentifier
          , PlanCategory=src.PlanCategory
          , EventType=src.EventType
          , EventDate=src.EventDate
          , EnrollmentDate=src.EnrollmentDate
          , EmployeeSSN=src.EmployeeSSN
          , EmployeeName=src.EmployeeName
          , SecondEventOriginalFDOC=src.SecondEventOriginalFDOC
          , DateSpecificRightsNoticeWasPrinted=src.DateSpecificRightsNoticeWasPrinted
          , PostmarkDateOfElection=src.PostmarkDateOfElection
          , IsPaidThroughLastDayOfCOBRA=src.IsPaidThroughLastDayOfCOBRA
          , NextPremiumOwedMonth=src.NextPremiumOwedMonth
          , NextPremiumOwedYear=src.NextPremiumOwedYear
          , NextPremiumOwedAmountReceived=src.NextPremiumOwedAmountReceived
          , SendTakeoverLetter=src.SendTakeoverLetter
          , IsConversionLetterSent=src.IsConversionLetterSent
          , SendDODSubsidyExtension=src.SendDODSubsidyExtension
          , PlanName=src.PlanName
          , CoverageLevel=src.CoverageLevel
          , NumberOfUnit=src.NumberOfUnit
          , StartDate=src.StartDate
          , EndDate=src.EndDate
          , FirstDayOfCOBRA=src.FirstDayOfCOBRA
          , LastDayOfCOBRA=src.LastDayOfCOBRA
          , COBRADurationMonths=src.COBRADurationMonths
          , DaysToElect=src.DaysToElect
          , DaysToMake1stPayment=src.DaysToMake1stPayment
          , DaysToMakeSubsequentPayments=src.DaysToMakeSubsequentPayments
          , ElectionPostmarkDate=src.ElectionPostmarkDate
          , LastDateRatesNotified=src.LastDateRatesNotified
          , NumberOfUnits=src.NumberOfUnits
          , SendPlanChangeLetterForLegacy=src.SendPlanChangeLetterForLegacy
          , PlanBundleName=src.PlanBundleName
          , Relationship=src.Relationship
          , AddressSameAsQB=src.AddressSameAsQB
          , IsQMCSO=src.IsQMCSO
          , UsesFDOC=src.UsesFDOC
          , NoteType=src.NoteType
          , DateTime=src.DateTime
          , NoteText=src.NoteText
          , UserName=src.UserName
          , InsuranceType=src.InsuranceType
          , SubsidyAmountType=src.SubsidyAmountType
          , Amount=src.Amount
          , SubsidyType=src.SubsidyType
          , RatePeriodSubsidy=src.RatePeriodSubsidy
          , CASRINSERT=src.CASRINSERT
          , CTSRINSERT=src.CTSRINSERT
          , MNLIFEINSERT=src.MNLIFEINSERT
          , MNCONTINSERT=src.MNCONTINSERT
          , ORSRINSERT=src.ORSRINSERT
          , TXSRINSERT=src.TXSRINSERT
          , NYSRINSERT=src.NYSRINSERT
          , VEBASRINSERT=src.VEBASRINSERT
          , ILSRINSERT=src.ILSRINSERT
          , RISRINSERT=src.RISRINSERT
          , GASRINSERT=src.GASRINSERT
          , VASRINSERT=src.VASRINSERT
          , DisabilityApproved=src.DisabilityApproved
          , PostmarkOfDisabilityExtension=src.PostmarkOfDisabilityExtension
          , DateDisabled=src.DateDisabled
          , DenialReason=src.DenialReason
          , Rate=src.Rate
          , TermOrReinstate=src.TermOrReinstate
          , EffectiveDate=src.EffectiveDate
          , Reason=src.Reason
          , LetterAttachmentName=src.LetterAttachmentName
          , QualifyingEventDate=src.QualifyingEventDate
          , UserDefinedFieldName=src.UserDefinedFieldName
          , UserDefinedFieldValue=src.UserDefinedFieldValue
          , AddressSameAsSPM=src.AddressSameAsSPM
          , BillingEndDate=src.BillingEndDate
          , BillingFrequency=src.BillingFrequency
          , BillingPeriodSeedDate=src.BillingPeriodSeedDate
          , BillingStartDate=src.BillingStartDate
          , BillingType=src.BillingType
          , FirstDayOfCoverage=src.FirstDayOfCoverage
          , FullName=src.FullName
          , HasWaivedAllCoverage=src.HasWaivedAllCoverage
          , HireDate=src.HireDate
          , InitialGracePeriodDate=src.InitialGracePeriodDate
          , InitialGracePeriodDays=src.InitialGracePeriodDays
          , Invalid=src.Invalid
          , IsCOBRAEligible=src.IsCOBRAEligible
          , IsCOBRAEligibleAtTermination=src.IsCOBRAEligibleAtTermination
          , IsLegacy=src.IsLegacy
          , LastDayOfCoverage=src.LastDayOfCoverage
          , OriginalEnrollmentDate=src.OriginalEnrollmentDate
          , SecondBillingPeriodSeedDate=src.SecondBillingPeriodSeedDate
          , SendGRNotice=src.SendGRNotice
          , SPMInitialGracePeriodOptionType=src.SPMInitialGracePeriodOptionType
          , SPMSubsequentGracePeriodOptionType=src.SPMSubsequentGracePeriodOptionType
          , SubsequentGracePeriodNrOfDays=src.SubsequentGracePeriodNrOfDays
          , UsesFamilyInAddress=src.UsesFamilyInAddress
          , UsesFirstDayOfCoverage=src.UsesFirstDayOfCoverage
    
    WHEN NOT MATCHED THEN
        INSERT (
                 result_template
               , error_row
               , error_code
               , error_message
               , row_num
               , row_type
               , error_message_calc
               , cobra_res_file_name
               , data_row
               , source_row_no
               , check_type
               , VersionNumber
               , ClientName
               , ClientDivisionName
               , Salutation
               , FirstName
               , MiddleInitial
               , LastName
               , SSN
               , IndividualID
               , Email
               , Phone
               , Phone2
               , Address1
               , Address2
               , City
               , StateOrProvince
               , PostalCode
               , Country
               , PremiumAddressSameAsPrimary
               , PremiumAddress1
               , PremiumAddress2
               , PremiumCity
               , PremiumStateOrProvince
               , PremiumPostalCode
               , PremiumCountry
               , Sex
               , DOB
               , TobaccoUse
               , EmployeeType
               , EmployeePayrollType
               , YearsOfService
               , PremiumCouponType
               , UsesHCTC
               , Active
               , AllowMemberSSO
               , BenefitGroup
               , AccountStructure
               , ClientSpecificData
               , SSOIdentifier
               , PlanCategory
               , EventType
               , EventDate
               , EnrollmentDate
               , EmployeeSSN
               , EmployeeName
               , SecondEventOriginalFDOC
               , DateSpecificRightsNoticeWasPrinted
               , PostmarkDateOfElection
               , IsPaidThroughLastDayOfCOBRA
               , NextPremiumOwedMonth
               , NextPremiumOwedYear
               , NextPremiumOwedAmountReceived
               , SendTakeoverLetter
               , IsConversionLetterSent
               , SendDODSubsidyExtension
               , PlanName
               , CoverageLevel
               , NumberOfUnit
               , StartDate
               , EndDate
               , FirstDayOfCOBRA
               , LastDayOfCOBRA
               , COBRADurationMonths
               , DaysToElect
               , DaysToMake1stPayment
               , DaysToMakeSubsequentPayments
               , ElectionPostmarkDate
               , LastDateRatesNotified
               , NumberOfUnits
               , SendPlanChangeLetterForLegacy
               , PlanBundleName
               , Relationship
               , AddressSameAsQB
               , IsQMCSO
               , UsesFDOC
               , NoteType
               , DateTime
               , NoteText
               , UserName
               , InsuranceType
               , SubsidyAmountType
               , Amount
               , SubsidyType
               , RatePeriodSubsidy
               , CASRINSERT
               , CTSRINSERT
               , MNLIFEINSERT
               , MNCONTINSERT
               , ORSRINSERT
               , TXSRINSERT
               , NYSRINSERT
               , VEBASRINSERT
               , ILSRINSERT
               , RISRINSERT
               , GASRINSERT
               , VASRINSERT
               , DisabilityApproved
               , PostmarkOfDisabilityExtension
               , DateDisabled
               , DenialReason
               , Rate
               , TermOrReinstate
               , EffectiveDate
               , Reason
               , LetterAttachmentName
               , QualifyingEventDate
               , UserDefinedFieldName
               , UserDefinedFieldValue
               , AddressSameAsSPM
               , BillingEndDate
               , BillingFrequency
               , BillingPeriodSeedDate
               , BillingStartDate
               , BillingType
               , FirstDayOfCoverage
               , FullName
               , HasWaivedAllCoverage
               , HireDate
               , InitialGracePeriodDate
               , InitialGracePeriodDays
               , Invalid
               , IsCOBRAEligible
               , IsCOBRAEligibleAtTermination
               , IsLegacy
               , LastDayOfCoverage
               , OriginalEnrollmentDate
               , SecondBillingPeriodSeedDate
               , SendGRNotice
               , SPMInitialGracePeriodOptionType
               , SPMSubsequentGracePeriodOptionType
               , SubsequentGracePeriodNrOfDays
               , UsesFamilyInAddress
               , UsesFirstDayOfCoverage
        
        )
        VALUES (
                   src.result_template
               ,   src.error_row
               ,   src.error_code
               ,   src.error_message
               ,   src.row_num
               ,   src.row_type
               ,   src.error_message_calc
               ,   src.cobra_res_file_name
               ,   src.data_row
               ,   src.source_row_no
               ,   src.check_type
               ,   src.VersionNumber
               ,   src.ClientName
               ,   src.ClientDivisionName
               ,   src.Salutation
               ,   src.FirstName
               ,   src.MiddleInitial
               ,   src.LastName
               ,   src.SSN
               ,   src.IndividualID
               ,   src.Email
               ,   src.Phone
               ,   src.Phone2
               ,   src.Address1
               ,   src.Address2
               ,   src.City
               ,   src.StateOrProvince
               ,   src.PostalCode
               ,   src.Country
               ,   src.PremiumAddressSameAsPrimary
               ,   src.PremiumAddress1
               ,   src.PremiumAddress2
               ,   src.PremiumCity
               ,   src.PremiumStateOrProvince
               ,   src.PremiumPostalCode
               ,   src.PremiumCountry
               ,   src.Sex
               ,   src.DOB
               ,   src.TobaccoUse
               ,   src.EmployeeType
               ,   src.EmployeePayrollType
               ,   src.YearsOfService
               ,   src.PremiumCouponType
               ,   src.UsesHCTC
               ,   src.Active
               ,   src.AllowMemberSSO
               ,   src.BenefitGroup
               ,   src.AccountStructure
               ,   src.ClientSpecificData
               ,   src.SSOIdentifier
               ,   src.PlanCategory
               ,   src.EventType
               ,   src.EventDate
               ,   src.EnrollmentDate
               ,   src.EmployeeSSN
               ,   src.EmployeeName
               ,   src.SecondEventOriginalFDOC
               ,   src.DateSpecificRightsNoticeWasPrinted
               ,   src.PostmarkDateOfElection
               ,   src.IsPaidThroughLastDayOfCOBRA
               ,   src.NextPremiumOwedMonth
               ,   src.NextPremiumOwedYear
               ,   src.NextPremiumOwedAmountReceived
               ,   src.SendTakeoverLetter
               ,   src.IsConversionLetterSent
               ,   src.SendDODSubsidyExtension
               ,   src.PlanName
               ,   src.CoverageLevel
               ,   src.NumberOfUnit
               ,   src.StartDate
               ,   src.EndDate
               ,   src.FirstDayOfCOBRA
               ,   src.LastDayOfCOBRA
               ,   src.COBRADurationMonths
               ,   src.DaysToElect
               ,   src.DaysToMake1stPayment
               ,   src.DaysToMakeSubsequentPayments
               ,   src.ElectionPostmarkDate
               ,   src.LastDateRatesNotified
               ,   src.NumberOfUnits
               ,   src.SendPlanChangeLetterForLegacy
               ,   src.PlanBundleName
               ,   src.Relationship
               ,   src.AddressSameAsQB
               ,   src.IsQMCSO
               ,   src.UsesFDOC
               ,   src.NoteType
               ,   src.DateTime
               ,   src.NoteText
               ,   src.UserName
               ,   src.InsuranceType
               ,   src.SubsidyAmountType
               ,   src.Amount
               ,   src.SubsidyType
               ,   src.RatePeriodSubsidy
               ,   src.CASRINSERT
               ,   src.CTSRINSERT
               ,   src.MNLIFEINSERT
               ,   src.MNCONTINSERT
               ,   src.ORSRINSERT
               ,   src.TXSRINSERT
               ,   src.NYSRINSERT
               ,   src.VEBASRINSERT
               ,   src.ILSRINSERT
               ,   src.RISRINSERT
               ,   src.GASRINSERT
               ,   src.VASRINSERT
               ,   src.DisabilityApproved
               ,   src.PostmarkOfDisabilityExtension
               ,   src.DateDisabled
               ,   src.DenialReason
               ,   src.Rate
               ,   src.TermOrReinstate
               ,   src.EffectiveDate
               ,   src.Reason
               ,   src.LetterAttachmentName
               ,   src.QualifyingEventDate
               ,   src.UserDefinedFieldName
               ,   src.UserDefinedFieldValue
               ,   src.AddressSameAsSPM
               ,   src.BillingEndDate
               ,   src.BillingFrequency
               ,   src.BillingPeriodSeedDate
               ,   src.BillingStartDate
               ,   src.BillingType
               ,   src.FirstDayOfCoverage
               ,   src.FullName
               ,   src.HasWaivedAllCoverage
               ,   src.HireDate
               ,   src.InitialGracePeriodDate
               ,   src.InitialGracePeriodDays
               ,   src.Invalid
               ,   src.IsCOBRAEligible
               ,   src.IsCOBRAEligibleAtTermination
               ,   src.IsLegacy
               ,   src.LastDayOfCoverage
               ,   src.OriginalEnrollmentDate
               ,   src.SecondBillingPeriodSeedDate
               ,   src.SendGRNotice
               ,   src.SPMInitialGracePeriodOptionType
               ,   src.SPMSubsequentGracePeriodOptionType
               ,   src.SubsequentGracePeriodNrOfDays
               ,   src.UsesFamilyInAddress
               ,   src.UsesFirstDayOfCoverage
        
               );
    
    --  truncate table [dbo].[cobra_res_file_table_stage];

end
go

alter table cobra_res_file_table_stage
    drop constraint cobra_res_file_table_stage_PK
go


create unique index uk_name_source_row_num
    on cobra_res_file_table_stage (cobra_res_file_name, source_row_no)
go
