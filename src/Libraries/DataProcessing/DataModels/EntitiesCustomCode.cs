using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

// ReSharper disable All

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
                    this.AccountStatus = (string) value;
                    break;
                case "accounttypecode":
                    this.AccountTypeCode = (string) value;
                    break;
                case "addressline1":
                    this.AddressLine1 = (string) value;
                    break;
                case "addressline2":
                    this.AddressLine2 = (string) value;
                    break;
                case "birthdate":
                    this.BirthDate = (string) value;
                    break;
                case "city":
                    this.City = (string) value;
                    break;
                case "country":
                    this.Country = (string) value;
                    break;
                case "data_row":
                    this.data_row = (string) value;
                    break;
                case "deleteaccount":
                    this.DeleteAccount = (string) value;
                    break;
                case "dependentid":
                    this.DependentID = (string) value;
                    break;
                case "deposittype":
                    this.DepositType = (string) value;
                    break;
                case "division":
                    this.Division = (string) value;
                    break;
                case "effectivedate":
                    this.EffectiveDate = (string) value;
                    break;
                case "eligibilitydate":
                    this.EligibilityDate = (string) value;
                    break;
                case "email":
                    this.Email = (string) value;
                    break;
                case "employeedepositamount":
                    this.EmployeeDepositAmount = (string) value;
                    break;
                case "employeeid":
                    this.EmployeeID = (string) value;
                    break;
                case "employeepayperiodelection":
                    this.EmployeePayPeriodElection = (string) value;
                    break;
                case "employeesocialsecuritynumber":
                    this.EmployeeSocialSecurityNumber = (string) value;
                    break;
                case "employeestatus":
                    this.EmployeeStatus = (string) value;
                    break;
                case "employerdepositamount":
                    this.EmployerDepositAmount = (string) value;
                    break;
                case "employerid":
                    this.EmployerId = (string) value;
                    break;
                case "employerpayperiodelection":
                    this.EmployerPayPeriodElection = (string) value;
                    break;
                case "error_code":
                    this.error_code = (string) value;
                    break;
                case "error_message":
                    this.error_message = (string) value;
                    break;
                case "error_message_calc":
                    this.error_message_calc = (string) value;
                    break;
                //case "error_row":  this.error_row  = (string) value; break;
                case "firstname":
                    this.FirstName = (string) value;
                    break;
                case "lastname":
                    this.LastName = (string) value;
                    break;
                case "mbi_file_name":
                    this.mbi_file_name = (string) value;
                    break;
                case "middleinitial":
                    this.MiddleInitial = (string) value;
                    break;
                case "mobilenumber":
                    this.MobileNumber = (string) value;
                    break;
                case "originalprefunded":
                    this.OriginalPrefunded = (string) value;
                    break;
                case "phone":
                    this.Phone = (string) value;
                    break;
                case "planenddate":
                    this.PlanEndDate = (string) value;
                    break;
                case "planid":
                    this.PlanId = (string) value;
                    break;
                case "planstartdate":
                    this.PlanStartDate = (string) value;
                    break;
                case "relationship":
                    this.Relationship = (string) value;
                    break;
                //case "res_file_name":  this.res_file_name  = (string) value; break;
                //case "result_template":  this.result_template  = (string) value; break;
                case "row_num":
                    this.row_num = Int32.Parse(value);
                    break;
                case "row_type":
                    this.row_type = (string) value;
                    break;
                case "source_row_no":
                    this.source_row_no = Int32.Parse(value);
                    break;
                case "state":
                    this.State = (string) value;
                    break;
                case "terminationdate":
                    this.TerminationDate = (string) value;
                    break;
                case "tpaid":
                    this.TpaId = (string) value;
                    break;
                case "zip":
                    this.Zip = (string) value;
                    break;
                //
                default:
                    string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : {colName} is Invalid";
                    throw new Exception(message);
            }
        }
    }

}