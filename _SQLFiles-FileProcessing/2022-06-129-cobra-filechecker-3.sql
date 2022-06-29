use Data_Processing;
go
create or
alter PROCEDURE [dbo].[proc_cobra_ExportImportFile](
                                                   @cobra_file_name nvarchar(2000),
                                                   @exportType nvarchar(50),
                                                   @batchId nvarchar(2000) )
AS
BEGIN
    
    declare @count int;
    declare @recordsSql nvarchar(max);
    declare @errorMsg nvarchar(2000);
    declare @headerSql nvarchar(max);
    declare @finalSql nvarchar(max);
    declare @countSql nvarchar(max);
    
    if (@exportType) = 'passed_lines'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    if (@exportType) = 'original_file'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        --                         ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    if (@exportType) = 'rejected_lines'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        ' and (len(isnull(error_message, ''''))) > 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    if (@exportType) = 'all_lines_with_errors'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(concat(data_row, '','', ' +
                        'case when len(error_message) > 0 then concat( ''PreCheck Errors: '' , error_message ) ' +
                        ' else ''PreCheck: OK'' end ) )) as file_row ' +
                        ', source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        --  ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    if (@exportType) = 'rejected_lines_with_errors'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(concat(data_row, '','', ' +
                        'case when len(error_message) > 0 then concat( ''PreCheck Errors: '' , error_message ) ' +
                        ' else ''PreCheck: OK'' end ) )) as file_row ' +
                        ', source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        ' and (len(isnull(error_message, ''''))) <> 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    
    if (isnull( @recordsSql , '' ) = '')
        begin
            set @errorMsg = 'Incorrect exportType: ' + @exporttype;
            throw 500001, @errorMsg, 1;
        end;
    
    /**/
    set @countSql = 'Select @cnt=count(*) from ( ' + @recordsSql + ') as T';
    EXECUTE sp_executesql @countSql , N'@cobra_file_name nvarchar(2000), @cnt int OUTPUT' ,
            @cobra_file_name = @cobra_file_name , @cnt = @count OUTPUT;
    /**/
    set @headerSql =
                'select top 1 ' +
                ' replace(concat(ltrim(rtrim(data_row)), '','', @batchId), ''XX'', @cnt) as file_row, source_row_no from [dbo].[cobra_file_table]  ' +
                ' where cobra_file_name = @cobra_file_name ' +
                ' and row_type = ''IA'' ';
    
    print @headerSql;
    set @finalSql = 'Select file_row, source_row_no from (' + @headerSql + ' UNION ALL ' + @recordsSql +
                    ') t2 order by source_row_no';
    
    EXECUTE sp_executesql @finalSql , N'@cobra_file_name nvarchar(2000), @batchId nvarchar(2000), @cnt int OUTPUT' ,
            @cobra_file_name = @cobra_file_name , @batchId = @batchId , @cnt = @count OUTPUT;
END
GO

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
              from
                  dbo.cobra_file_table_stage
          ) as src
    ON (tgt.cobra_file_name = src.cobra_file_name and tgt.row_num = src.row_num)
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
               );
    
    --    truncate table [dbo].[cobra_file_table_stage];
end
GO

create or
alter procedure dbo.process_cobra_res_file_table_stage_import as
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
              from
                  dbo.cobra_res_file_table_stage
          ) as src
    ON (tgt.cobra_res_file_name = src.cobra_res_file_name and tgt.row_num = src.row_num)
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
       
               );
    
    --  truncate table [dbo].[cobra_res_file_table_stage];

end
GO

