using System.Diagnostics.CodeAnalysis;

// ReSharper disable All

namespace DataProcessing.DataModels.AlegeusFileProcessing
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class Alegeus_File_ProcessingEntities
    {
        public Alegeus_File_ProcessingEntities(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }
    }
}

namespace DataProcessing.DataModels.AlegeusErrorLog
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class Alegeus_ErrorLogEntities
    {
        public Alegeus_ErrorLogEntities(string nameOrConnectionString)
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
    }
}

namespace DataProcessing.DataModels.COBRA
{
    // ReSharper disable once InconsistentNaming
    public partial class COBRAEntities
    {
        public COBRAEntities(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }
    }
}