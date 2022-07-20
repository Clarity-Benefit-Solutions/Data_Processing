using CoreUtils.Classes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

// ReSharper disable All
namespace DataProcessing.DataModels.CobraPoint
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class CobraPointEntities
    {
        public CobraPointEntities(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }
    }

    public partial class QB
    {
        public string SSNFormatted
        {
            get
            {
                return Utils.IsBlank(this.SSN) ? "" : Utils.FormatSsnWithDashes(this.SSN);
            }
        }
        public string SSNNumbersOnly
        {
            get
            {
                return Utils.IsBlank(this.SSN) ? "" : Utils.FormatSsnNumbersOnly(this.SSN);
            }
        }
    }
    public partial class NPM
    {
        public string SSNFormatted
        {
            get
            {
                return Utils.IsBlank(this.SSN) ? "" : Utils.FormatSsnWithDashes(this.SSN);
            }
        }
        public string SSNNumbersOnly
        {
            get
            {
                return Utils.IsBlank(this.SSN) ? "" : Utils.FormatSsnNumbersOnly(this.SSN);
            }
        }
    }

    public partial class SPM
    {
        public string SSNFormatted
        {
            get
            {
                return Utils.IsBlank(this.SSN) ? "" : Utils.FormatSsnWithDashes(this.SSN);
            }
        }
        public string SSNNumbersOnly
        {
            get
            {
                return Utils.IsBlank(this.SSN) ? "" : Utils.FormatSsnNumbersOnly(this.SSN);
            }
        }
    }

}

namespace DataProcessing.DataModels.DataProcessing
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class Data_ProcessingEntities
    {
        public Data_ProcessingEntities(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }
    }


    public partial class res_file_table_stage
    {
        public string ColumnValue(string colName)
        {
            switch (colName.ToLowerInvariant())
            {
                case "accountstatus": return this.AccountStatus?.ToString();
                case "accounttypecode": return this.AccountTypeCode?.ToString();
                case "addressline1": return this.AddressLine1?.ToString();
                case "addressline2": return this.AddressLine2?.ToString();
                case "birthdate": return this.BirthDate?.ToString();
                case "city": return this.City?.ToString();
                case "country": return this.Country?.ToString();
                case "data_row": return this.data_row?.ToString();
                case "deleteaccount": return this.DeleteAccount?.ToString();
                case "dependentid": return this.DependentID?.ToString();
                case "deposittype": return this.DepositType?.ToString();
                case "division": return this.Division?.ToString();
                case "effectivedate": return this.EffectiveDate?.ToString();
                case "eligibilitydate": return this.EligibilityDate?.ToString();
                case "email": return this.Email?.ToString();
                case "employeedepositamount": return this.EmployeeDepositAmount?.ToString();
                case "employeeid": return this.EmployeeID?.ToString();
                case "employeepayperiodelection": return this.EmployeePayPeriodElection?.ToString();
                case "employeesocialsecuritynumber": return this.EmployeeSocialSecurityNumber?.ToString();
                case "employeestatus": return this.EmployeeStatus?.ToString();
                case "employerdepositamount": return this.EmployerDepositAmount?.ToString();
                case "employerid": return this.EmployerId?.ToString();
                case "employerpayperiodelection": return this.EmployerPayPeriodElection?.ToString();
                case "error_code": return this.error_code?.ToString();
                case "error_message": return this.error_message?.ToString();
                case "error_message_calc": return this.error_message_calc?.ToString();
                case "error_row": return this.error_row?.ToString();
                case "firstname": return this.FirstName?.ToString();
                case "lastname": return this.LastName?.ToString();
                case "mbi_file_name": return this.mbi_file_name?.ToString();
                case "middleinitial": return this.MiddleInitial?.ToString();
                case "mobilenumber": return this.MobileNumber?.ToString();
                case "originalprefunded": return this.OriginalPrefunded?.ToString();
                case "phone": return this.Phone?.ToString();
                case "planenddate": return this.PlanEndDate?.ToString();
                case "planid": return this.PlanId?.ToString();
                case "planstartdate": return this.PlanStartDate?.ToString();
                case "relationship": return this.Relationship?.ToString();
                case "res_file_name": return this.res_file_name?.ToString();
                case "result_template": return this.result_template?.ToString();
                case "row_num": return this.row_num.ToString();
                case "row_type": return this.row_type?.ToString();
                case "source_row_no": return this.source_row_no?.ToString();
                case "state": return this.State?.ToString();
                case "terminationdate": return this.TerminationDate?.ToString();
                case "tpaid": return this.TpaId?.ToString();
                case "zip": return this.Zip?.ToString();
                //
                default:
                    return "";
            }
        }
    }

    public partial class mbi_file_table_stage
    {
        public string ColumnValue(string colName)
        {
            switch (colName.ToLowerInvariant())
            {
                case "accountstatus": return this.AccountStatus?.ToString();
                case "accounttypecode": return this.AccountTypeCode?.ToString();
                case "addressline1": return this.AddressLine1?.ToString();
                case "addressline2": return this.AddressLine2?.ToString();
                case "birthdate": return this.BirthDate?.ToString();
                case "city": return this.City?.ToString();
                case "country": return this.Country?.ToString();
                case "data_row": return this.data_row?.ToString();
                case "deleteaccount": return this.DeleteAccount?.ToString();
                case "dependentid": return this.DependentID?.ToString();
                case "deposittype": return this.DepositType?.ToString();
                case "division": return this.Division?.ToString();
                case "effectivedate": return this.EffectiveDate?.ToString();
                case "eligibilitydate": return this.EligibilityDate?.ToString();
                case "email": return this.Email?.ToString();
                case "employeedepositamount": return this.EmployeeDepositAmount?.ToString();
                case "employeeid": return this.EmployeeID?.ToString();
                case "employeepayperiodelection": return this.EmployeePayPeriodElection?.ToString();
                case "employeesocialsecuritynumber": return this.EmployeeSocialSecurityNumber?.ToString();
                case "employeestatus": return this.EmployeeStatus?.ToString();
                case "employerdepositamount": return this.EmployerDepositAmount?.ToString();
                case "employerid": return this.EmployerId?.ToString();
                case "employerpayperiodelection": return this.EmployerPayPeriodElection?.ToString();
                case "error_code": return this.error_code?.ToString();
                case "error_message": return this.error_message?.ToString();
                case "error_message_calc": return this.error_message_calc?.ToString();
                //case "error_row": return this.error_row?.ToString();
                case "firstname": return this.FirstName?.ToString();
                case "lastname": return this.LastName?.ToString();
                case "mbi_file_name": return this.mbi_file_name?.ToString();
                case "middleinitial": return this.MiddleInitial?.ToString();
                case "mobilenumber": return this.MobileNumber?.ToString();
                case "originalprefunded": return this.OriginalPrefunded?.ToString();
                case "phone": return this.Phone?.ToString();
                case "planenddate": return this.PlanEndDate?.ToString();
                case "planid": return this.PlanId?.ToString();
                case "planstartdate": return this.PlanStartDate?.ToString();
                case "relationship": return this.Relationship?.ToString();
                //case "res_file_name": return this.res_file_name?.ToString();
                //case "result_template": return this.result_template?.ToString();
                case "row_num": return this.row_num.ToString();
                case "row_type": return this.row_type?.ToString();
                case "source_row_no": return this.source_row_no?.ToString();
                case "state": return this.State?.ToString();
                case "terminationdate": return this.TerminationDate?.ToString();
                case "tpaid": return this.TpaId?.ToString();
                case "zip": return this.Zip?.ToString();
                //
                default:
                    return "";
            }
        }

        public void SetColumnValue(string colName, string value)
        {
            switch (colName.ToLowerInvariant())
            {
                case "accountstatus":
                    this.AccountStatus = value;
                    break;
                case "accounttypecode":
                    this.AccountTypeCode = value;
                    break;
                case "addressline1":
                    this.AddressLine1 = value;
                    break;
                case "addressline2":
                    this.AddressLine2 = value;
                    break;
                case "birthdate":
                    this.BirthDate = value;
                    break;
                case "city":
                    this.City = value;
                    break;
                case "country":
                    this.Country = value;
                    break;
                case "data_row":
                    this.data_row = value;
                    break;
                case "deleteaccount":
                    this.DeleteAccount = value;
                    break;
                case "dependentid":
                    this.DependentID = value;
                    break;
                case "deposittype":
                    this.DepositType = value;
                    break;
                case "division":
                    this.Division = value;
                    break;
                case "effectivedate":
                    this.EffectiveDate = value;
                    break;
                case "eligibilitydate":
                    this.EligibilityDate = value;
                    break;
                case "email":
                    this.Email = value;
                    break;
                case "employeedepositamount":
                    this.EmployeeDepositAmount = value;
                    break;
                case "employeeid":
                    this.EmployeeID = value;
                    break;
                case "employeepayperiodelection":
                    this.EmployeePayPeriodElection = value;
                    break;
                case "employeesocialsecuritynumber":
                    this.EmployeeSocialSecurityNumber = value;
                    break;
                case "employeestatus":
                    this.EmployeeStatus = value;
                    break;
                case "employerdepositamount":
                    this.EmployerDepositAmount = value;
                    break;
                case "employerid":
                    this.EmployerId = value;
                    break;
                case "employerpayperiodelection":
                    this.EmployerPayPeriodElection = value;
                    break;
                case "error_code":
                    this.error_code = value;
                    break;
                case "error_message":
                    this.error_message = value;
                    break;
                case "error_message_calc":
                    this.error_message_calc = value;
                    break;
                //case "error_row":  this.error_row  =  value; break;
                case "firstname":
                    this.FirstName = value;
                    break;
                case "lastname":
                    this.LastName = value;
                    break;
                case "mbi_file_name":
                    this.mbi_file_name = value;
                    break;
                case "middleinitial":
                    this.MiddleInitial = value;
                    break;
                case "mobilenumber":
                    this.MobileNumber = value;
                    break;
                case "originalprefunded":
                    this.OriginalPrefunded = value;
                    break;
                case "phone":
                    this.Phone = value;
                    break;
                case "planenddate":
                    this.PlanEndDate = value;
                    break;
                case "planid":
                    this.PlanId = value;
                    break;
                case "planstartdate":
                    this.PlanStartDate = value;
                    break;
                case "relationship":
                    this.Relationship = value;
                    break;
                //case "res_file_name":  this.res_file_name  =  value; break;
                //case "result_template":  this.result_template  =  value; break;
                case "row_num":
                    this.row_num = Int32.Parse(value);
                    break;
                case "row_type":
                    this.row_type = value;
                    break;
                case "source_row_no":
                    this.source_row_no = Int32.Parse(value);
                    break;
                case "state":
                    this.State = value;
                    break;
                case "terminationdate":
                    this.TerminationDate = value;
                    break;
                case "tpaid":
                    this.TpaId = value;
                    break;
                case "zip":
                    this.Zip = value;
                    break;
                //
                default:
                    string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : {colName} is Invalid";
                    throw new Exception(message);
            }
        }
    }

    public partial class cobra_res_file_table_stage
    {
        public string ColumnValue(string colName)
        {
            switch (colName.ToLowerInvariant())
            {
                case "cobra_res_file_name": return this.cobra_res_file_name?.ToString();
                case "data_row": return this.data_row?.ToString();
                case "row_num": return this.row_num.ToString();
                case "row_type": return this.row_type?.ToString();
                case "source_row_no": return this.source_row_no?.ToString();
                case "check_type": return this.check_type?.ToString();
                case "error_code": return this.error_code?.ToString();
                case "error_message": return this.error_message?.ToString();
                case "error_message_calc": return this.error_message_calc?.ToString();
                case "versionnumber": return this.VersionNumber?.ToString();
                case "clientname": return this.ClientName?.ToString();
                case "clientdivisionname": return this.ClientDivisionName?.ToString();
                case "salutation": return this.Salutation?.ToString();
                case "firstname": return this.FirstName?.ToString();
                case "middleinitial": return this.MiddleInitial?.ToString();
                case "lastname": return this.LastName?.ToString();
                case "ssn": return this.SSN?.ToString();
                case "individualid": return this.IndividualID?.ToString();
                case "email": return this.Email?.ToString();
                case "phone": return this.Phone?.ToString();
                case "phone2": return this.Phone2?.ToString();
                case "address1": return this.Address1?.ToString();
                case "address2": return this.Address2?.ToString();
                case "city": return this.City?.ToString();
                case "stateorprovince": return this.StateOrProvince?.ToString();
                case "postalcode": return this.PostalCode?.ToString();
                case "country": return this.Country?.ToString();
                case "premiumaddresssameasprimary": return this.PremiumAddressSameAsPrimary?.ToString();
                case "premiumaddress1": return this.PremiumAddress1?.ToString();
                case "premiumaddress2": return this.PremiumAddress2?.ToString();
                case "premiumcity": return this.PremiumCity?.ToString();
                case "premiumstateorprovince": return this.PremiumStateOrProvince?.ToString();
                case "premiumpostalcode": return this.PremiumPostalCode?.ToString();
                case "premiumcountry": return this.PremiumCountry?.ToString();
                case "sex": return this.Sex?.ToString();
                case "dob": return this.DOB?.ToString();
                case "tobaccouse": return this.TobaccoUse?.ToString();
                case "employeetype": return this.EmployeeType?.ToString();
                case "employeepayrolltype": return this.EmployeePayrollType?.ToString();
                case "yearsofservice": return this.YearsOfService?.ToString();
                case "premiumcoupontype": return this.PremiumCouponType?.ToString();
                case "useshctc": return this.UsesHCTC?.ToString();
                case "active": return this.Active?.ToString();
                case "allowmembersso": return this.AllowMemberSSO?.ToString();
                case "benefitgroup": return this.BenefitGroup?.ToString();
                case "accountstructure": return this.AccountStructure?.ToString();
                case "clientspecificdata": return this.ClientSpecificData?.ToString();
                case "ssoidentifier": return this.SSOIdentifier?.ToString();
                case "plancategory": return this.PlanCategory?.ToString();
                case "eventtype": return this.EventType?.ToString();
                case "eventdate": return this.EventDate?.ToString();
                case "enrollmentdate": return this.EnrollmentDate?.ToString();
                case "employeessn": return this.EmployeeSSN?.ToString();
                case "employeename": return this.EmployeeName?.ToString();
                case "secondeventoriginalfdoc": return this.SecondEventOriginalFDOC?.ToString();
                case "datespecificrightsnoticewasprinted": return this.DateSpecificRightsNoticeWasPrinted?.ToString();
                case "postmarkdateofelection": return this.PostmarkDateOfElection?.ToString();
                case "ispaidthroughlastdayofcobra": return this.IsPaidThroughLastDayOfCOBRA?.ToString();
                case "nextpremiumowedmonth": return this.NextPremiumOwedMonth?.ToString();
                case "nextpremiumowedyear": return this.NextPremiumOwedYear?.ToString();
                case "nextpremiumowedamountreceived": return this.NextPremiumOwedAmountReceived?.ToString();
                case "sendtakeoverletter": return this.SendTakeoverLetter?.ToString();
                case "isconversionlettersent": return this.IsConversionLetterSent?.ToString();
                case "senddodsubsidyextension": return this.SendDODSubsidyExtension?.ToString();
                case "planname": return this.PlanName?.ToString();
                case "coveragelevel": return this.CoverageLevel?.ToString();
                case "numberofunit": return this.NumberOfUnit?.ToString();
                case "startdate": return this.StartDate?.ToString();
                case "enddate": return this.EndDate?.ToString();
                case "firstdayofcobra": return this.FirstDayOfCOBRA?.ToString();
                case "lastdayofcobra": return this.LastDayOfCOBRA?.ToString();
                case "cobradurationmonths": return this.COBRADurationMonths?.ToString();
                case "daystoelect": return this.DaysToElect?.ToString();
                case "daystomake1stpayment": return this.DaysToMake1stPayment?.ToString();
                case "daystomakesubsequentpayments": return this.DaysToMakeSubsequentPayments?.ToString();
                case "electionpostmarkdate": return this.ElectionPostmarkDate?.ToString();
                case "lastdateratesnotified": return this.LastDateRatesNotified?.ToString();
                case "numberofunits": return this.NumberOfUnits?.ToString();
                case "sendplanchangeletterforlegacy": return this.SendPlanChangeLetterForLegacy?.ToString();
                case "planbundlename": return this.PlanBundleName?.ToString();
                case "relationship": return this.Relationship?.ToString();
                case "addresssameasqb": return this.AddressSameAsQB?.ToString();
                case "isqmcso": return this.IsQMCSO?.ToString();
                case "usesfdoc": return this.UsesFDOC?.ToString();
                case "notetype": return this.NoteType?.ToString();
                case "datetime": return this.DateTime?.ToString();
                case "notetext": return this.NoteText?.ToString();
                case "username": return this.UserName?.ToString();
                case "insurancetype": return this.InsuranceType?.ToString();
                case "subsidyamounttype": return this.SubsidyAmountType?.ToString();
                case "amount": return this.Amount?.ToString();
                case "subsidytype": return this.SubsidyType?.ToString();
                case "rateperiodsubsidy": return this.RatePeriodSubsidy?.ToString();
                case "casrinsert": return this.CASRINSERT?.ToString();
                case "ctsrinsert": return this.CTSRINSERT?.ToString();
                case "mnlifeinsert": return this.MNLIFEINSERT?.ToString();
                case "mncontinsert": return this.MNCONTINSERT?.ToString();
                case "orsrinsert": return this.ORSRINSERT?.ToString();
                case "txsrinsert": return this.TXSRINSERT?.ToString();
                case "nysrinsert": return this.NYSRINSERT?.ToString();
                case "vebasrinsert": return this.VEBASRINSERT?.ToString();
                case "ilsrinsert": return this.ILSRINSERT?.ToString();
                case "risrinsert": return this.RISRINSERT?.ToString();
                case "gasrinsert": return this.GASRINSERT?.ToString();
                case "vasrinsert": return this.VASRINSERT?.ToString();
                case "disabilityapproved": return this.DisabilityApproved?.ToString();
                case "postmarkofdisabilityextension": return this.PostmarkOfDisabilityExtension?.ToString();
                case "datedisabled": return this.DateDisabled?.ToString();
                case "denialreason": return this.DenialReason?.ToString();
                case "rate": return this.Rate?.ToString();
                case "termorreinstate": return this.TermOrReinstate?.ToString();
                case "effectivedate": return this.EffectiveDate?.ToString();
                case "reason": return this.Reason?.ToString();
                case "letterattachmentname": return this.LetterAttachmentName?.ToString();
                case "qualifyingeventdate": return this.QualifyingEventDate?.ToString();
                case "userdefinedfieldname": return this.UserDefinedFieldName?.ToString();
                case "userdefinedfieldvalue": return this.UserDefinedFieldValue?.ToString();
                //
                default:
                    return "";
            }
        }
    }

    public partial class cobra_file_table_stage
    {
        public string entityType
        {
            get
            {
                if (Utils.IsBlank(this.row_type))
                {
                    return "";
                }
                return this.row_type.Replace("[", "").Replace("]", "");
            }
        }
        public string ColumnValue(string colName)
        {
            switch (colName.ToLowerInvariant())
            {
                case "cobra_file_name": return this.cobra_file_name?.ToString();
                case "data_row": return this.data_row?.ToString();
                case "row_num": return this.row_num.ToString();
                case "row_type": return this.row_type?.ToString();
                case "source_row_no": return this.source_row_no?.ToString();
                case "check_type": return this.check_type?.ToString();
                case "error_code": return this.error_code?.ToString();
                case "error_message": return this.error_message?.ToString();
                case "error_message_calc": return this.error_message_calc?.ToString();
                case "versionnumber": return this.VersionNumber?.ToString();
                case "clientname": return this.ClientName?.ToString();
                case "clientdivisionname": return this.ClientDivisionName?.ToString();
                case "salutation": return this.Salutation?.ToString();
                case "firstname": return this.FirstName?.ToString();
                case "middleinitial": return this.MiddleInitial?.ToString();
                case "lastname": return this.LastName?.ToString();
                case "ssn": return this.SSN?.ToString();
                case "individualid": return this.IndividualID?.ToString();
                case "email": return this.Email?.ToString();
                case "phone": return this.Phone?.ToString();
                case "phone2": return this.Phone2?.ToString();
                case "address1": return this.Address1?.ToString();
                case "address2": return this.Address2?.ToString();
                case "city": return this.City?.ToString();
                case "stateorprovince": return this.StateOrProvince?.ToString();
                case "postalcode": return this.PostalCode?.ToString();
                case "country": return this.Country?.ToString();
                case "premiumaddresssameasprimary": return this.PremiumAddressSameAsPrimary?.ToString();
                case "premiumaddress1": return this.PremiumAddress1?.ToString();
                case "premiumaddress2": return this.PremiumAddress2?.ToString();
                case "premiumcity": return this.PremiumCity?.ToString();
                case "premiumstateorprovince": return this.PremiumStateOrProvince?.ToString();
                case "premiumpostalcode": return this.PremiumPostalCode?.ToString();
                case "premiumcountry": return this.PremiumCountry?.ToString();
                case "sex": return this.Sex?.ToString();
                case "dob": return this.DOB?.ToString();
                case "tobaccouse": return this.TobaccoUse?.ToString();
                case "employeetype": return this.EmployeeType?.ToString();
                case "employeepayrolltype": return this.EmployeePayrollType?.ToString();
                case "yearsofservice": return this.YearsOfService?.ToString();
                case "premiumcoupontype": return this.PremiumCouponType?.ToString();
                case "useshctc": return this.UsesHCTC?.ToString();
                case "active": return this.Active?.ToString();
                case "allowmembersso": return this.AllowMemberSSO?.ToString();
                case "benefitgroup": return this.BenefitGroup?.ToString();
                case "accountstructure": return this.AccountStructure?.ToString();
                case "clientspecificdata": return this.ClientSpecificData?.ToString();
                case "ssoidentifier": return this.SSOIdentifier?.ToString();
                case "plancategory": return this.PlanCategory?.ToString();
                case "eventtype": return this.EventType?.ToString();
                case "eventdate": return this.EventDate?.ToString();
                case "enrollmentdate": return this.EnrollmentDate?.ToString();
                case "employeessn": return this.EmployeeSSN?.ToString();
                case "employeename": return this.EmployeeName?.ToString();
                case "secondeventoriginalfdoc": return this.SecondEventOriginalFDOC?.ToString();
                case "datespecificrightsnoticewasprinted": return this.DateSpecificRightsNoticeWasPrinted?.ToString();
                case "postmarkdateofelection": return this.PostmarkDateOfElection?.ToString();
                case "ispaidthroughlastdayofcobra": return this.IsPaidThroughLastDayOfCOBRA?.ToString();
                case "nextpremiumowedmonth": return this.NextPremiumOwedMonth?.ToString();
                case "nextpremiumowedyear": return this.NextPremiumOwedYear?.ToString();
                case "nextpremiumowedamountreceived": return this.NextPremiumOwedAmountReceived?.ToString();
                case "sendtakeoverletter": return this.SendTakeoverLetter?.ToString();
                case "isconversionlettersent": return this.IsConversionLetterSent?.ToString();
                case "senddodsubsidyextension": return this.SendDODSubsidyExtension?.ToString();
                case "planname": return this.PlanName?.ToString();
                case "coveragelevel": return this.CoverageLevel?.ToString();
                case "numberofunit": return this.NumberOfUnit?.ToString();
                case "startdate": return this.StartDate?.ToString();
                case "enddate": return this.EndDate?.ToString();
                case "firstdayofcobra": return this.FirstDayOfCOBRA?.ToString();
                case "lastdayofcobra": return this.LastDayOfCOBRA?.ToString();
                case "cobradurationmonths": return this.COBRADurationMonths?.ToString();
                case "daystoelect": return this.DaysToElect?.ToString();
                case "daystomake1stpayment": return this.DaysToMake1stPayment?.ToString();
                case "daystomakesubsequentpayments": return this.DaysToMakeSubsequentPayments?.ToString();
                case "electionpostmarkdate": return this.ElectionPostmarkDate?.ToString();
                case "lastdateratesnotified": return this.LastDateRatesNotified?.ToString();
                case "numberofunits": return this.NumberOfUnits?.ToString();
                case "sendplanchangeletterforlegacy": return this.SendPlanChangeLetterForLegacy?.ToString();
                case "planbundlename": return this.PlanBundleName?.ToString();
                case "relationship": return this.Relationship?.ToString();
                case "addresssameasqb": return this.AddressSameAsQB?.ToString();
                case "isqmcso": return this.IsQMCSO?.ToString();
                case "usesfdoc": return this.UsesFDOC?.ToString();
                case "notetype": return this.NoteType?.ToString();
                case "datetime": return this.DateTime?.ToString();
                case "notetext": return this.NoteText?.ToString();
                case "username": return this.UserName?.ToString();
                case "insurancetype": return this.InsuranceType?.ToString();
                case "subsidyamounttype": return this.SubsidyAmountType?.ToString();
                case "amount": return this.Amount?.ToString();
                case "subsidytype": return this.SubsidyType?.ToString();
                case "rateperiodsubsidy": return this.RatePeriodSubsidy?.ToString();
                case "casrinsert": return this.CASRINSERT?.ToString();
                case "ctsrinsert": return this.CTSRINSERT?.ToString();
                case "mnlifeinsert": return this.MNLIFEINSERT?.ToString();
                case "mncontinsert": return this.MNCONTINSERT?.ToString();
                case "orsrinsert": return this.ORSRINSERT?.ToString();
                case "txsrinsert": return this.TXSRINSERT?.ToString();
                case "nysrinsert": return this.NYSRINSERT?.ToString();
                case "vebasrinsert": return this.VEBASRINSERT?.ToString();
                case "ilsrinsert": return this.ILSRINSERT?.ToString();
                case "risrinsert": return this.RISRINSERT?.ToString();
                case "gasrinsert": return this.GASRINSERT?.ToString();
                case "vasrinsert": return this.VASRINSERT?.ToString();
                case "disabilityapproved": return this.DisabilityApproved?.ToString();
                case "postmarkofdisabilityextension": return this.PostmarkOfDisabilityExtension?.ToString();
                case "datedisabled": return this.DateDisabled?.ToString();
                case "denialreason": return this.DenialReason?.ToString();
                case "rate": return this.Rate?.ToString();
                case "termorreinstate": return this.TermOrReinstate?.ToString();
                case "effectivedate": return this.EffectiveDate?.ToString();
                case "reason": return this.Reason?.ToString();
                case "letterattachmentname": return this.LetterAttachmentName?.ToString();
                case "qualifyingeventdate": return this.QualifyingEventDate?.ToString();
                case "userdefinedfieldname": return this.UserDefinedFieldName?.ToString();
                case "userdefinedfieldvalue": return this.UserDefinedFieldValue?.ToString();
                //
                default:
                    return "";
            }
        }

        public void SetColumnValue(string colName, string value)
        {
            switch (colName.ToLowerInvariant())
            {
                case "cobra_file_name": this.cobra_file_name = value; break;
                case "data_row": this.data_row = value; break;
                case "row_num": this.row_num = Utils.ToInt(value); break;
                case "row_type": this.row_type = value; break;
                case "source_row_no": this.source_row_no = Utils.ToInt(value); break;
                case "check_type": this.check_type = value; break;
                case "error_code": this.error_code = value; break;
                case "error_message": this.error_message = value; break;
                case "error_message_calc": this.error_message_calc = value; break;
                case "versionnumber": this.VersionNumber = value; break;
                case "clientname": this.ClientName = value; break;
                case "clientdivisionname": this.ClientDivisionName = value; break;
                case "salutation": this.Salutation = value; break;
                case "firstname": this.FirstName = value; break;
                case "middleinitial": this.MiddleInitial = value; break;
                case "lastname": this.LastName = value; break;
                case "ssn": this.SSN = value; break;
                case "individualid": this.IndividualID = value; break;
                case "email": this.Email = value; break;
                case "phone": this.Phone = value; break;
                case "phone2": this.Phone2 = value; break;
                case "address1": this.Address1 = value; break;
                case "address2": this.Address2 = value; break;
                case "city": this.City = value; break;
                case "stateorprovince": this.StateOrProvince = value; break;
                case "postalcode": this.PostalCode = value; break;
                case "country": this.Country = value; break;
                case "premiumaddresssameasprimary": this.PremiumAddressSameAsPrimary = value; break;
                case "premiumaddress1": this.PremiumAddress1 = value; break;
                case "premiumaddress2": this.PremiumAddress2 = value; break;
                case "premiumcity": this.PremiumCity = value; break;
                case "premiumstateorprovince": this.PremiumStateOrProvince = value; break;
                case "premiumpostalcode": this.PremiumPostalCode = value; break;
                case "premiumcountry": this.PremiumCountry = value; break;
                case "sex": this.Sex = value; break;
                case "dob": this.DOB = value; break;
                case "tobaccouse": this.TobaccoUse = value; break;
                case "employeetype": this.EmployeeType = value; break;
                case "employeepayrolltype": this.EmployeePayrollType = value; break;
                case "yearsofservice": this.YearsOfService = value; break;
                case "premiumcoupontype": this.PremiumCouponType = value; break;
                case "useshctc": this.UsesHCTC = value; break;
                case "active": this.Active = value; break;
                case "allowmembersso": this.AllowMemberSSO = value; break;
                case "benefitgroup": this.BenefitGroup = value; break;
                case "accountstructure": this.AccountStructure = value; break;
                case "clientspecificdata": this.ClientSpecificData = value; break;
                case "ssoidentifier": this.SSOIdentifier = value; break;
                case "plancategory": this.PlanCategory = value; break;
                case "eventtype": this.EventType = value; break;
                case "eventdate": this.EventDate = value; break;
                case "enrollmentdate": this.EnrollmentDate = value; break;
                case "employeessn": this.EmployeeSSN = value; break;
                case "employeename": this.EmployeeName = value; break;
                case "secondeventoriginalfdoc": this.SecondEventOriginalFDOC = value; break;
                case "datespecificrightsnoticewasprinted": this.DateSpecificRightsNoticeWasPrinted = value; break;
                case "postmarkdateofelection": this.PostmarkDateOfElection = value; break;
                case "ispaidthroughlastdayofcobra": this.IsPaidThroughLastDayOfCOBRA = value; break;
                case "nextpremiumowedmonth": this.NextPremiumOwedMonth = value; break;
                case "nextpremiumowedyear": this.NextPremiumOwedYear = value; break;
                case "nextpremiumowedamountreceived": this.NextPremiumOwedAmountReceived = value; break;
                case "sendtakeoverletter": this.SendTakeoverLetter = value; break;
                case "isconversionlettersent": this.IsConversionLetterSent = value; break;
                case "senddodsubsidyextension": this.SendDODSubsidyExtension = value; break;
                case "planname": this.PlanName = value; break;
                case "coveragelevel": this.CoverageLevel = value; break;
                case "numberofunit": this.NumberOfUnit = value; break;
                case "startdate": this.StartDate = value; break;
                case "enddate": this.EndDate = value; break;
                case "firstdayofcobra": this.FirstDayOfCOBRA = value; break;
                case "lastdayofcobra": this.LastDayOfCOBRA = value; break;
                case "cobradurationmonths": this.COBRADurationMonths = value; break;
                case "daystoelect": this.DaysToElect = value; break;
                case "daystomake1stpayment": this.DaysToMake1stPayment = value; break;
                case "daystomakesubsequentpayments": this.DaysToMakeSubsequentPayments = value; break;
                case "electionpostmarkdate": this.ElectionPostmarkDate = value; break;
                case "lastdateratesnotified": this.LastDateRatesNotified = value; break;
                case "numberofunits": this.NumberOfUnits = value; break;
                case "sendplanchangeletterforlegacy": this.SendPlanChangeLetterForLegacy = value; break;
                case "planbundlename": this.PlanBundleName = value; break;
                case "relationship": this.Relationship = value; break;
                case "addresssameasqb": this.AddressSameAsQB = value; break;
                case "isqmcso": this.IsQMCSO = value; break;
                case "usesfdoc": this.UsesFDOC = value; break;
                case "notetype": this.NoteType = value; break;
                case "datetime": this.DateTime = value; break;
                case "notetext": this.NoteText = value; break;
                case "username": this.UserName = value; break;
                case "insurancetype": this.InsuranceType = value; break;
                case "subsidyamounttype": this.SubsidyAmountType = value; break;
                case "amount": this.Amount = value; break;
                case "subsidytype": this.SubsidyType = value; break;
                case "rateperiodsubsidy": this.RatePeriodSubsidy = value; break;
                case "casrinsert": this.CASRINSERT = value; break;
                case "ctsrinsert": this.CTSRINSERT = value; break;
                case "mnlifeinsert": this.MNLIFEINSERT = value; break;
                case "mncontinsert": this.MNCONTINSERT = value; break;
                case "orsrinsert": this.ORSRINSERT = value; break;
                case "txsrinsert": this.TXSRINSERT = value; break;
                case "nysrinsert": this.NYSRINSERT = value; break;
                case "vebasrinsert": this.VEBASRINSERT = value; break;
                case "ilsrinsert": this.ILSRINSERT = value; break;
                case "risrinsert": this.RISRINSERT = value; break;
                case "gasrinsert": this.GASRINSERT = value; break;
                case "vasrinsert": this.VASRINSERT = value; break;
                case "disabilityapproved": this.DisabilityApproved = value; break;
                case "postmarkofdisabilityextension": this.PostmarkOfDisabilityExtension = value; break;
                case "datedisabled": this.DateDisabled = value; break;
                case "denialreason": this.DenialReason = value; break;
                case "rate": this.Rate = value; break;
                case "termorreinstate": this.TermOrReinstate = value; break;
                case "effectivedate": this.EffectiveDate = value; break;
                case "reason": this.Reason = value; break;
                case "letterattachmentname": this.LetterAttachmentName = value; break;
                case "qualifyingeventdate": this.QualifyingEventDate = value; break;
                case "userdefinedfieldname": this.UserDefinedFieldName = value; break;
                case "userdefinedfieldvalue": this.UserDefinedFieldValue = value; break;
                //
                default:
                    string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : {colName} is Invalid";
                    throw new Exception(message);
            }
        }
    }

}