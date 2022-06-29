use Data_Processing;
go
GO

ALTER TABLE cobra_res_file_table_STAGE
    ADD VersionNumber nvarchar(200) NULL;
go
CREATE INDEX VersionNumber ON cobra_res_file_table_STAGE (VersionNumber);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD ClientName nvarchar(200) NULL;
go
CREATE INDEX ClientName ON cobra_res_file_table_STAGE (ClientName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD ClientDivisionName nvarchar(200) NULL;
go
CREATE INDEX ClientDivisionName ON cobra_res_file_table_STAGE (ClientDivisionName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Salutation nvarchar(200) NULL;
go
CREATE INDEX Salutation ON cobra_res_file_table_STAGE (Salutation);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD FirstName nvarchar(200) NULL;
go
CREATE INDEX FirstName ON cobra_res_file_table_STAGE (FirstName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD MiddleInitial nvarchar(200) NULL;
go
CREATE INDEX MiddleInitial ON cobra_res_file_table_STAGE (MiddleInitial);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD LastName nvarchar(200) NULL;
go
CREATE INDEX LastName ON cobra_res_file_table_STAGE (LastName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SSN nvarchar(200) NULL;
go
CREATE INDEX SSN ON cobra_res_file_table_STAGE (SSN);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD IndividualID nvarchar(200) NULL;
go
CREATE INDEX IndividualID ON cobra_res_file_table_STAGE (IndividualID);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Email nvarchar(200) NULL;
go
CREATE INDEX Email ON cobra_res_file_table_STAGE (Email);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Phone nvarchar(200) NULL;
go
CREATE INDEX Phone ON cobra_res_file_table_STAGE (Phone);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Phone2 nvarchar(200) NULL;
go
CREATE INDEX Phone2 ON cobra_res_file_table_STAGE (Phone2);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Address1 nvarchar(200) NULL;
go
CREATE INDEX Address1 ON cobra_res_file_table_STAGE (Address1);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Address2 nvarchar(200) NULL;
go
CREATE INDEX Address2 ON cobra_res_file_table_STAGE (Address2);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD City nvarchar(200) NULL;
go
CREATE INDEX City ON cobra_res_file_table_STAGE (City);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD StateOrProvince nvarchar(200) NULL;
go
CREATE INDEX StateOrProvince ON cobra_res_file_table_STAGE (StateOrProvince);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PostalCode nvarchar(200) NULL;
go
CREATE INDEX PostalCode ON cobra_res_file_table_STAGE (PostalCode);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Country nvarchar(200) NULL;
go
CREATE INDEX Country ON cobra_res_file_table_STAGE (Country);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PremiumAddressSameAsPrimary nvarchar(200) NULL;
go
CREATE INDEX PremiumAddressSameAsPrimary ON cobra_res_file_table_STAGE (PremiumAddressSameAsPrimary);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PremiumAddress1 nvarchar(200) NULL;
go
CREATE INDEX PremiumAddress1 ON cobra_res_file_table_STAGE (PremiumAddress1);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PremiumAddress2 nvarchar(200) NULL;
go
CREATE INDEX PremiumAddress2 ON cobra_res_file_table_STAGE (PremiumAddress2);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PremiumCity nvarchar(200) NULL;
go
CREATE INDEX PremiumCity ON cobra_res_file_table_STAGE (PremiumCity);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PremiumStateOrProvince nvarchar(200) NULL;
go
CREATE INDEX PremiumStateOrProvince ON cobra_res_file_table_STAGE (PremiumStateOrProvince);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PremiumPostalCode nvarchar(200) NULL;
go
CREATE INDEX PremiumPostalCode ON cobra_res_file_table_STAGE (PremiumPostalCode);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PremiumCountry nvarchar(200) NULL;
go
CREATE INDEX PremiumCountry ON cobra_res_file_table_STAGE (PremiumCountry);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Sex nvarchar(200) NULL;
go
CREATE INDEX Sex ON cobra_res_file_table_STAGE (Sex);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DOB nvarchar(200) NULL;
go
CREATE INDEX DOB ON cobra_res_file_table_STAGE (DOB);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD TobaccoUse nvarchar(200) NULL;
go
CREATE INDEX TobaccoUse ON cobra_res_file_table_STAGE (TobaccoUse);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EmployeeType nvarchar(200) NULL;
go
CREATE INDEX EmployeeType ON cobra_res_file_table_STAGE (EmployeeType);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EmployeePayrollType nvarchar(200) NULL;
go
CREATE INDEX EmployeePayrollType ON cobra_res_file_table_STAGE (EmployeePayrollType);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD YearsOfService nvarchar(200) NULL;
go
CREATE INDEX YearsOfService ON cobra_res_file_table_STAGE (YearsOfService);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PremiumCouponType nvarchar(200) NULL;
go
CREATE INDEX PremiumCouponType ON cobra_res_file_table_STAGE (PremiumCouponType);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD UsesHCTC nvarchar(200) NULL;
go
CREATE INDEX UsesHCTC ON cobra_res_file_table_STAGE (UsesHCTC);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Active nvarchar(200) NULL;
go
CREATE INDEX Active ON cobra_res_file_table_STAGE (Active);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD AllowMemberSSO nvarchar(200) NULL;
go
CREATE INDEX AllowMemberSSO ON cobra_res_file_table_STAGE (AllowMemberSSO);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD BenefitGroup nvarchar(200) NULL;
go
CREATE INDEX BenefitGroup ON cobra_res_file_table_STAGE (BenefitGroup);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD AccountStructure nvarchar(200) NULL;
go
CREATE INDEX AccountStructure ON cobra_res_file_table_STAGE (AccountStructure);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD ClientSpecificData nvarchar(200) NULL;
go
CREATE INDEX ClientSpecificData ON cobra_res_file_table_STAGE (ClientSpecificData);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SSOIdentifier nvarchar(200) NULL;
go
CREATE INDEX SSOIdentifier ON cobra_res_file_table_STAGE (SSOIdentifier);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanCategory nvarchar(200) NULL;
go
CREATE INDEX PlanCategory ON cobra_res_file_table_STAGE (PlanCategory);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EventType nvarchar(200) NULL;
go
CREATE INDEX EventType ON cobra_res_file_table_STAGE (EventType);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EventDate nvarchar(200) NULL;
go
CREATE INDEX EventDate ON cobra_res_file_table_STAGE (EventDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EnrollmentDate nvarchar(200) NULL;
go
CREATE INDEX EnrollmentDate ON cobra_res_file_table_STAGE (EnrollmentDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EmployeeSSN nvarchar(200) NULL;
go
CREATE INDEX EmployeeSSN ON cobra_res_file_table_STAGE (EmployeeSSN);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EmployeeName nvarchar(200) NULL;
go
CREATE INDEX EmployeeName ON cobra_res_file_table_STAGE (EmployeeName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SecondEventOriginalFDOC nvarchar(200) NULL;
go
CREATE INDEX SecondEventOriginalFDOC ON cobra_res_file_table_STAGE (SecondEventOriginalFDOC);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DateSpecificRightsNoticeWasPrinted nvarchar(200) NULL;
go
CREATE INDEX DateSpecificRightsNoticeWasPrinted ON cobra_res_file_table_STAGE (DateSpecificRightsNoticeWasPrinted);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PostmarkDateOfElection nvarchar(200) NULL;
go
CREATE INDEX PostmarkDateOfElection ON cobra_res_file_table_STAGE (PostmarkDateOfElection);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD IsPaidThroughLastDayOfCOBRA nvarchar(200) NULL;
go
CREATE INDEX IsPaidThroughLastDayOfCOBRA ON cobra_res_file_table_STAGE (IsPaidThroughLastDayOfCOBRA);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD NextPremiumOwedMonth nvarchar(200) NULL;
go
CREATE INDEX NextPremiumOwedMonth ON cobra_res_file_table_STAGE (NextPremiumOwedMonth);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD NextPremiumOwedYear nvarchar(200) NULL;
go
CREATE INDEX NextPremiumOwedYear ON cobra_res_file_table_STAGE (NextPremiumOwedYear);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD NextPremiumOwedAmountReceived nvarchar(200) NULL;
go
CREATE INDEX NextPremiumOwedAmountReceived ON cobra_res_file_table_STAGE (NextPremiumOwedAmountReceived);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SendTakeoverLetter nvarchar(200) NULL;
go
CREATE INDEX SendTakeoverLetter ON cobra_res_file_table_STAGE (SendTakeoverLetter);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD IsConversionLetterSent nvarchar(200) NULL;
go
CREATE INDEX IsConversionLetterSent ON cobra_res_file_table_STAGE (IsConversionLetterSent);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SendDODSubsidyExtension nvarchar(200) NULL;
go
CREATE INDEX SendDODSubsidyExtension ON cobra_res_file_table_STAGE (SendDODSubsidyExtension);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanName nvarchar(200) NULL;
go
CREATE INDEX PlanName ON cobra_res_file_table_STAGE (PlanName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD CoverageLevel nvarchar(200) NULL;
go
CREATE INDEX CoverageLevel ON cobra_res_file_table_STAGE (CoverageLevel);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD NumberOfUnit nvarchar(200) NULL;
go
CREATE INDEX NumberOfUnit ON cobra_res_file_table_STAGE (NumberOfUnit);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanName nvarchar(200) NULL;
go
CREATE INDEX PlanName ON cobra_res_file_table_STAGE (PlanName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD StartDate nvarchar(200) NULL;
go
CREATE INDEX StartDate ON cobra_res_file_table_STAGE (StartDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EndDate nvarchar(200) NULL;
go
CREATE INDEX EndDate ON cobra_res_file_table_STAGE (EndDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD CoverageLevel nvarchar(200) NULL;
go
CREATE INDEX CoverageLevel ON cobra_res_file_table_STAGE (CoverageLevel);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD FirstDayOfCOBRA nvarchar(200) NULL;
go
CREATE INDEX FirstDayOfCOBRA ON cobra_res_file_table_STAGE (FirstDayOfCOBRA);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD LastDayOfCOBRA nvarchar(200) NULL;
go
CREATE INDEX LastDayOfCOBRA ON cobra_res_file_table_STAGE (LastDayOfCOBRA);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD COBRADurationMonths nvarchar(200) NULL;
go
CREATE INDEX COBRADurationMonths ON cobra_res_file_table_STAGE (COBRADurationMonths);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DaysToElect nvarchar(200) NULL;
go
CREATE INDEX DaysToElect ON cobra_res_file_table_STAGE (DaysToElect);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DaysToMake1stPayment nvarchar(200) NULL;
go
CREATE INDEX DaysToMake1stPayment ON cobra_res_file_table_STAGE (DaysToMake1stPayment);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DaysToMakeSubsequentPayments nvarchar(200) NULL;
go
CREATE INDEX DaysToMakeSubsequentPayments ON cobra_res_file_table_STAGE (DaysToMakeSubsequentPayments);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD ElectionPostmarkDate nvarchar(200) NULL;
go
CREATE INDEX ElectionPostmarkDate ON cobra_res_file_table_STAGE (ElectionPostmarkDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD LastDateRatesNotified nvarchar(200) NULL;
go
CREATE INDEX LastDateRatesNotified ON cobra_res_file_table_STAGE (LastDateRatesNotified);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD NumberOfUnits nvarchar(200) NULL;
go
CREATE INDEX NumberOfUnits ON cobra_res_file_table_STAGE (NumberOfUnits);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SendPlanChangeLetterForLegacy nvarchar(200) NULL;
go
CREATE INDEX SendPlanChangeLetterForLegacy ON cobra_res_file_table_STAGE (SendPlanChangeLetterForLegacy);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanBundleName nvarchar(200) NULL;
go
CREATE INDEX PlanBundleName ON cobra_res_file_table_STAGE (PlanBundleName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SSN nvarchar(200) NULL;
go
CREATE INDEX SSN ON cobra_res_file_table_STAGE (SSN);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Relationship nvarchar(200) NULL;
go
CREATE INDEX Relationship ON cobra_res_file_table_STAGE (Relationship);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Salutation nvarchar(200) NULL;
go
CREATE INDEX Salutation ON cobra_res_file_table_STAGE (Salutation);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD FirstName nvarchar(200) NULL;
go
CREATE INDEX FirstName ON cobra_res_file_table_STAGE (FirstName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD MiddleInitial nvarchar(200) NULL;
go
CREATE INDEX MiddleInitial ON cobra_res_file_table_STAGE (MiddleInitial);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD LastName nvarchar(200) NULL;
go
CREATE INDEX LastName ON cobra_res_file_table_STAGE (LastName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Email nvarchar(200) NULL;
go
CREATE INDEX Email ON cobra_res_file_table_STAGE (Email);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Phone nvarchar(200) NULL;
go
CREATE INDEX Phone ON cobra_res_file_table_STAGE (Phone);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Phone2 nvarchar(200) NULL;
go
CREATE INDEX Phone2 ON cobra_res_file_table_STAGE (Phone2);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD AddressSameAsQB nvarchar(200) NULL;
go
CREATE INDEX AddressSameAsQB ON cobra_res_file_table_STAGE (AddressSameAsQB);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Address1 nvarchar(200) NULL;
go
CREATE INDEX Address1 ON cobra_res_file_table_STAGE (Address1);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Address2 nvarchar(200) NULL;
go
CREATE INDEX Address2 ON cobra_res_file_table_STAGE (Address2);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD City nvarchar(200) NULL;
go
CREATE INDEX City ON cobra_res_file_table_STAGE (City);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD StateOrProvince nvarchar(200) NULL;
go
CREATE INDEX StateOrProvince ON cobra_res_file_table_STAGE (StateOrProvince);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PostalCode nvarchar(200) NULL;
go
CREATE INDEX PostalCode ON cobra_res_file_table_STAGE (PostalCode);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Country nvarchar(200) NULL;
go
CREATE INDEX Country ON cobra_res_file_table_STAGE (Country);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EnrollmentDate nvarchar(200) NULL;
go
CREATE INDEX EnrollmentDate ON cobra_res_file_table_STAGE (EnrollmentDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Sex nvarchar(200) NULL;
go
CREATE INDEX Sex ON cobra_res_file_table_STAGE (Sex);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DOB nvarchar(200) NULL;
go
CREATE INDEX DOB ON cobra_res_file_table_STAGE (DOB);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD IsQMCSO nvarchar(200) NULL;
go
CREATE INDEX IsQMCSO ON cobra_res_file_table_STAGE (IsQMCSO);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanName nvarchar(200) NULL;
go
CREATE INDEX PlanName ON cobra_res_file_table_STAGE (PlanName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanName nvarchar(200) NULL;
go
CREATE INDEX PlanName ON cobra_res_file_table_STAGE (PlanName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD StartDate nvarchar(200) NULL;
go
CREATE INDEX StartDate ON cobra_res_file_table_STAGE (StartDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EndDate nvarchar(200) NULL;
go
CREATE INDEX EndDate ON cobra_res_file_table_STAGE (EndDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD UsesFDOC nvarchar(200) NULL;
go
CREATE INDEX UsesFDOC ON cobra_res_file_table_STAGE (UsesFDOC);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD NoteType nvarchar(200) NULL;
go
CREATE INDEX NoteType ON cobra_res_file_table_STAGE (NoteType);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DateTime nvarchar(200) NULL;
go
CREATE INDEX DateTime ON cobra_res_file_table_STAGE (DateTime);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD NoteText nvarchar(200) NULL;
go
CREATE INDEX NoteText ON cobra_res_file_table_STAGE (NoteText);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD UserName nvarchar(200) NULL;
go
CREATE INDEX UserName ON cobra_res_file_table_STAGE (UserName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD InsuranceType nvarchar(200) NULL;
go
CREATE INDEX InsuranceType ON cobra_res_file_table_STAGE (InsuranceType);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SubsidyAmountType nvarchar(200) NULL;
go
CREATE INDEX SubsidyAmountType ON cobra_res_file_table_STAGE (SubsidyAmountType);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD StartDate nvarchar(200) NULL;
go
CREATE INDEX StartDate ON cobra_res_file_table_STAGE (StartDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EndDate nvarchar(200) NULL;
go
CREATE INDEX EndDate ON cobra_res_file_table_STAGE (EndDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Amount nvarchar(200) NULL;
go
CREATE INDEX Amount ON cobra_res_file_table_STAGE (Amount);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SubsidyType nvarchar(200) NULL;
go
CREATE INDEX SubsidyType ON cobra_res_file_table_STAGE (SubsidyType);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD RatePeriodSubsidy nvarchar(200) NULL;
go
CREATE INDEX RatePeriodSubsidy ON cobra_res_file_table_STAGE (RatePeriodSubsidy);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD CASRINSERT nvarchar(200) NULL;
go
CREATE INDEX CASRINSERT ON cobra_res_file_table_STAGE (CASRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD CTSRINSERT nvarchar(200) NULL;
go
CREATE INDEX CTSRINSERT ON cobra_res_file_table_STAGE (CTSRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD MNLIFEINSERT nvarchar(200) NULL;
go
CREATE INDEX MNLIFEINSERT ON cobra_res_file_table_STAGE (MNLIFEINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD MNCONTINSERT nvarchar(200) NULL;
go
CREATE INDEX MNCONTINSERT ON cobra_res_file_table_STAGE (MNCONTINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD ORSRINSERT nvarchar(200) NULL;
go
CREATE INDEX ORSRINSERT ON cobra_res_file_table_STAGE (ORSRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD TXSRINSERT nvarchar(200) NULL;
go
CREATE INDEX TXSRINSERT ON cobra_res_file_table_STAGE (TXSRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD NYSRINSERT nvarchar(200) NULL;
go
CREATE INDEX NYSRINSERT ON cobra_res_file_table_STAGE (NYSRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD VEBASRINSERT nvarchar(200) NULL;
go
CREATE INDEX VEBASRINSERT ON cobra_res_file_table_STAGE (VEBASRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD ILSRINSERT nvarchar(200) NULL;
go
CREATE INDEX ILSRINSERT ON cobra_res_file_table_STAGE (ILSRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD RISRINSERT nvarchar(200) NULL;
go
CREATE INDEX RISRINSERT ON cobra_res_file_table_STAGE (RISRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD GASRINSERT nvarchar(200) NULL;
go
CREATE INDEX GASRINSERT ON cobra_res_file_table_STAGE (GASRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD VASRINSERT nvarchar(200) NULL;
go
CREATE INDEX VASRINSERT ON cobra_res_file_table_STAGE (VASRINSERT);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DisabilityApproved nvarchar(200) NULL;
go
CREATE INDEX DisabilityApproved ON cobra_res_file_table_STAGE (DisabilityApproved);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PostmarkOfDisabilityExtension nvarchar(200) NULL;
go
CREATE INDEX PostmarkOfDisabilityExtension ON cobra_res_file_table_STAGE (PostmarkOfDisabilityExtension);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DateDisabled nvarchar(200) NULL;
go
CREATE INDEX DateDisabled ON cobra_res_file_table_STAGE (DateDisabled);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD DenialReason nvarchar(200) NULL;
go
CREATE INDEX DenialReason ON cobra_res_file_table_STAGE (DenialReason);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanName nvarchar(200) NULL;
go
CREATE INDEX PlanName ON cobra_res_file_table_STAGE (PlanName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Rate nvarchar(200) NULL;
go
CREATE INDEX Rate ON cobra_res_file_table_STAGE (Rate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanName nvarchar(200) NULL;
go
CREATE INDEX PlanName ON cobra_res_file_table_STAGE (PlanName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD StartDate nvarchar(200) NULL;
go
CREATE INDEX StartDate ON cobra_res_file_table_STAGE (StartDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EndDate nvarchar(200) NULL;
go
CREATE INDEX EndDate ON cobra_res_file_table_STAGE (EndDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Rate nvarchar(200) NULL;
go
CREATE INDEX Rate ON cobra_res_file_table_STAGE (Rate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD PlanName nvarchar(200) NULL;
go
CREATE INDEX PlanName ON cobra_res_file_table_STAGE (PlanName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD TermOrReinstate nvarchar(200) NULL;
go
CREATE INDEX TermOrReinstate ON cobra_res_file_table_STAGE (TermOrReinstate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD EffectiveDate nvarchar(200) NULL;
go
CREATE INDEX EffectiveDate ON cobra_res_file_table_STAGE (EffectiveDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD Reason nvarchar(200) NULL;
go
CREATE INDEX Reason ON cobra_res_file_table_STAGE (Reason);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD LetterAttachmentName nvarchar(200) NULL;
go
CREATE INDEX LetterAttachmentName ON cobra_res_file_table_STAGE (LetterAttachmentName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD ClientName nvarchar(200) NULL;
go
CREATE INDEX ClientName ON cobra_res_file_table_STAGE (ClientName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD SSN nvarchar(200) NULL;
go
CREATE INDEX SSN ON cobra_res_file_table_STAGE (SSN);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD QualifyingEventDate nvarchar(200) NULL;
go
CREATE INDEX QualifyingEventDate ON cobra_res_file_table_STAGE (QualifyingEventDate);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD UserDefinedFieldName nvarchar(200) NULL;
go
CREATE INDEX UserDefinedFieldName ON cobra_res_file_table_STAGE (UserDefinedFieldName);
go
ALTER TABLE cobra_res_file_table_STAGE
    ADD UserDefinedFieldValue nvarchar(200) NULL;
go
CREATE INDEX UserDefinedFieldValue ON cobra_res_file_table_STAGE (UserDefinedFieldValue);
go

exec sp_rename 'cobra_file_table.mbi_file_name', cobra_file_name, 'COLUMN'
go
exec sp_rename 'cobra_file_table_stage.mbi_file_name', cobra_file_name, 'COLUMN'
go

exec sp_rename 'cobra_res_file_table.res_file_name', cobra_res_file_name, 'COLUMN'
go
exec sp_rename 'cobra_res_file_table_stage.res_file_name', cobra_res_file_name, 'COLUMN'
go

