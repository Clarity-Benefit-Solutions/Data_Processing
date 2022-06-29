using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing.DataModels.DataProcessing;

// ReSharper disable All

// ReSharper disable once CheckNamespace
namespace DataProcessing
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class FileChecker : IDisposable
    {
        #region CheckCobraFile

        private void CheckCobraFile(string currentFilePath)
        {
            string tableName = "";
            TypedCsvSchema mappings = new TypedCsvSchema();
            string postImportProc = "";

            // check mappings and type of file (Import or Result)
            Boolean isResultFile = Import.GetCobraFileFormatIsResultFile(currentFilePath);
            if (isResultFile)
            {
                return;
            }

            //
            tableName = "[dbo].[cobra_file_table_stage]";
            postImportProc = "[dbo].[process_cobra_file_table_stage_import]";
            //
            Import.ImportCobraFile(DbConn, currentFilePath, currentFilePath, this.hasHeaderRow, FileLogParams, null);

            //
            // update check type for table
            string queryString1 = $" update {tableName} set " +
                                  $" /* set check type */check_type = 'PreCheck', " +
                                  $" /* remove extra csv commas added to line */ data_row = replace(data_row, ',,,,,,,,,,,,,,,,,,,,', '') " +
                                  $" where 1 = 1;";
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, DbConn, queryString1, null,
                FileLogParams?.GetMessageLogParams()
            );

            // check file data from the table based on mappings
            CheckCobraFileData();

            // run post import proc to take data from stage into final table
            string queryString = $"exec {postImportProc};";
            //
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, DbConn, queryString, null,
                FileLogParams?.GetMessageLogParams()
            );
        }

        private void CheckCobraFileData()
        {
            // ensure previously cached data is not used so
            // so create a new db context to ensure stale data will NOT be used
            var dbDataProcessing = Vars.dbCtxDataProcessingNew;

            // get all dbRows without caching
            var dataRows = dbDataProcessing.cobra_file_table_stage
                .OrderBy(dataRow => dataRow.source_row_no)
                .ToList();

            var versionNo = Import.GetCobraFileVersionNoFromFile(SrcFilePath);

            //check each dataRow
            int rowNo = 0;
            foreach (var dataRow in dataRows)
            {
                // get row format for current lione as file can have multiple row types
                string rowType = dataRow.row_type;

                // get mappings for event type & version
                TypedCsvSchema mappings = Import.GetCobraFileImportMappings(rowType, versionNo);

                rowNo++;
                this.CheckCobraFileData(versionNo, dataRow, mappings);
            }

            // save any changes
            dbDataProcessing.SaveChanges();
        }


        private void CheckCobraFileData(string versionNo, cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            // don't check header dataRow
            if (dataRow.row_type == "")
            {
                return;
            }

            Boolean hasError;

            // first fix all columns
            foreach (var column in mappings.Columns)
            {
                // skip some columns
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    case "":
                    case "source_row_no":
                    case "error_row":
                    case "data_row":
                    case "res_file_name":
                    case "cobra_file_name":
                    case "row_type":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }

                // save Org Column value before changes
                var orgValue = dataRow.ColumnValue(column.SourceColumn) ?? "";

                // 1. valid Format and general rules check - save corrected value to row
                var formattedValue = EnsureValueIsOfFormatAndMatchesRules(dataRow, column, mappings);
            }

            // then check data for each column
            foreach (var column in mappings.Columns)
            {
                // skip some columns
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    case "":
                    case "source_row_no":
                    case "error_row":
                    case "data_row":
                    case "res_file_name":
                    case "cobra_file_name":
                    case "row_type":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }

                // 2. specific column checking against business rules & DB
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    // ER ID
                    case "tpaid":
                        hasError = this.CheckCobraTpaExists(dataRow, column, versionNo);
                        break;

                    // ER ID
                    case "employerid":
                        //ER must exist before any Import files are sent
                        hasError = this.CheckCobraEmployerExists(dataRow, column, versionNo);
                        break;

                    // EE ID
                    case "employeeid":
                        //ER must exist before any Import files are sent. But for IB files, employee need not exist - he is being added
                        hasError = this.CheckCobraEmployeeExists(dataRow, column, versionNo);

                        break;
                    // plan related
                    case "planid":
                    case @"accounttypecode":
                        //if (versionNo == string.CobraEnrollment)
                        //{
                        //    hasError = this.CheckCobraEmployerPlanExists(dataRow, column, versionNo);
                        //}
                        //else if (versionNo == string.CobraEmployeeDeposit)
                        //{
                        //    hasError = this.CheckCobraEmployeePlanExists(dataRow, column, versionNo);
                        //}

                        break;

                    default:
                        break;
                }
            }
            // check for duplicate posting of the row
            hasError = CheckForDuplicateCobraPosting(dataRow, versionNo);
        }

        private void AddCobraErrorForRow(cobra_file_table_stage dataRow, string errCode, string errMessage,
            Boolean markAsCompleteFail = false)
        {
            // add to dataRow so it will be saved back to DB for dataRow by dataRow data

            if (Utils.IsBlank(dataRow.error_code))
            {
                dataRow.error_code = errCode;
            }
            else
            {
                dataRow.error_code = ErrorSeparator + errCode;
            }

            if (Utils.IsBlank(dataRow.error_message))
            {
                dataRow.error_message = errMessage;
            }
            else
            {
                dataRow.error_message = ErrorSeparator + errMessage;
            }

            if (dataRow.error_code.StartsWith(ErrorSeparator))
            {
                dataRow.error_code = dataRow.error_code.Substring(1);
            }

            if (dataRow.error_message.StartsWith(ErrorSeparator))
            {
                dataRow.error_message = dataRow.error_message.Substring(1);
            }

            //
            int key = dataRow.source_row_no ?? 0;
            if (this.fileCheckResults.ContainsKey(key))
            {
                this.fileCheckResults[key] = $"{dataRow.source_row_no}: {errCode} : {errMessage}";
            }
            else
            {
                this.fileCheckResults.Add(key, $"{dataRow.source_row_no}: {errCode} : {errMessage}");
            }

            //
            if (markAsCompleteFail)
            {
                this.fileCheckResults.MarkAsCompleteFail = true;
            }
        }


        public Boolean CheckCobraTpaExists(cobra_file_table_stage dataRow, TypedCsvColumn column, string versionNo)
        {
            var errorMessage = "";
            //var cacheKey = $"{MethodBase.GetCurrentMethod()?.Name}-{dataRow.TpaId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    if (PlatformType == PlatformType.Cobra)
            //    {
            //        // check DB
            //        if (Utils.IsBlank(column.FixedValue))
            //        {
            //            if (Utils.IsBlank(dataRow.TpaId))
            //            {
            //                errorMessage = "TPA ID cannot be blank. It must always be BENEFL";
            //            }
            //            else if (dataRow.TpaId != "BENEFL")
            //            {
            //                errorMessage = $"TPA ID {dataRow.TpaId} is invalid. It must always be BENEFL";
            //            }
            //        }
            //    }
            //    else
            //    {
            //        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
            //    }

            //    //
            //    _cache.Add(cacheKey, errorMessage);
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckCobraEmployerExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            var errorMessage = "";
            //var cacheKey =
            //    $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    // check DB
            //    if (Utils.IsBlank(dataRow.EmployerId))
            //    {
            //        errorMessage = $"The Employer ID cannot be blank";
            //    }
            //    else
            //    {
            //        DataTable dbResults = GetAllCobraEmployers();
            //        // planid is not always present e.g. in deposit file
            //        string filter = $"employer_id = '{dataRow.EmployerId}'";
            //        DataRow[] dbRows = dbResults.Select(filter);

            //        if (dbRows.Length == 0)
            //        {
            //            errorMessage = $"The Employer ID {dataRow.EmployerId} could not be found";
            //        }
            //        else
            //        {
            //            //DataRow dbData = dbRows[0];

            //            //note: FileChecker: verify if employer status need to be checked
            //            //string status = dbData["employer_status"]?.ToString();
            //            //if (status != "Active" && status != "New")
            //            //{
            //            //  errorMessage =
            //            //    $"The Employer ID {dataRow.EmployerId} has status {status} which is not valid";
            //            //}
            //        }
            //    }

            //    //
            //    _cache.Add(cacheKey, errorMessage);
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckCobraEmployeeExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo )
        {
            var errorMessage = "";
            //var cacheKey =
            //    $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-{dataRow.EmployeeID}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    // check DB
            //    if (Utils.IsBlank(dataRow.EmployerId))
            //    {
            //        AddCobraErrorForRow(dataRow, "EmployerId", $"The Employer ID cannot be blank");
            //    }
            //    else if (Utils.IsBlank(dataRow.EmployeeID))
            //    {
            //        AddCobraErrorForRow(dataRow, "EmployeeId", $"The Employee ID cannot be blank");
            //    }
            //    else
            //    {
            //        DataTable dbResults = GetAllCobraEmployeesForEmployer(dataRow.EmployerId);
            //        DataRow[] dbRows =
            //            dbResults.Select(
            //                $"employerid = '{dataRow.EmployerId}' and employeeid = '{dataRow.EmployeeID}'");
            //        if (dbRows.Length == 0)
            //        {
            //            // for demographics file, the employee will not yet exist or the status may be changing (activating or terminating) - do not check
            //            if (true /*|| versionNo == string.CobraDemographics*/)
            //            {
            //                // as it is an demographics file, add this employee to the ER-EE table so a check for plan enrollemnt within same run or before reaggregation from Cobra will suceed
            //                DataRow newRow = dbResults.NewRow();
            //                newRow["employerid"] = dataRow.EmployerId;
            //                newRow["employeeid"] = dataRow.EmployeeID;
            //                newRow["is_active"] = dataRow.EmployeeStatus == "2" ? 1 : 0;
            //                dbResults.Rows.Add(newRow);

            //                var cacheKey2 =
            //                    $"GetAllEmployeesForEmployer-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-AllEmployees";
            //                _cache.Add(cacheKey2, dbResults);

            //                //
            //                return false;
            //            }
            //            else
            //            {
            //                errorMessage +=
            //                    $"The Employee ID {dataRow.EmployeeID} could not be found for Employer Id {dataRow.EmployerId}";
            //            }
            //        }
            //        else
            //        {
            //            DataRow dbData = dbRows[0];
            //            // if employee exists as per our data, that is fine
            //            // do not check the file EmployeeStatus against what we havwe in the db
            //            //float status = Utils.ToNumber(dbData["is_active"]?.ToString());
            //            //if (status <= 0 && Utils.ToNumber(dataRow.EmployeeStatus) > 1)
            //            //{
            //            //  errorMessage +=
            //            //    $"The Employee ID {dataRow.EmployeeID} has status {status} which is not valid";
            //            //}
            //        }
            //    }

            //    //
            //    _cache.Add(cacheKey, errorMessage);
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }
        public Boolean CheckForDuplicateCobraPosting(cobra_file_table_stage dataRow,
                  string versionNo )
        {
            string errorMessage = "";

            ////
            //switch (versionNo)
            //{
            //    default:
            //        string queryString =
            //            $"select * from  [cobra_file_table] " +
            //            $" where " +
            //            $" TpaId='{dataRow.TpaId}'" +
            //            $" and EmployerId='{dataRow.EmployerId}'" +
            //            $" and EmployeeID='{dataRow.EmployeeID}'" +
            //            $" and AccountTypeCode='{dataRow.AccountTypeCode}'" +
            //            $" and PlanEndDate='{dataRow.PlanEndDate}'" +
            //            $" and PlanStartDate='{dataRow.PlanStartDate}'" +
            //            $" and EffectiveDate='{dataRow.EffectiveDate}'" +
            //            $" and DepositType='{dataRow.DepositType}'" +
            //            $" and len(isnull(error_message, '')) = 0" +
            //            $" order by row_id desc, cobra_file_name, source_row_no ;";
            //        //
            //        DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, DbConn,
            //             queryString, null,
            //             FileLogParams?.GetMessageLogParams());
            //        //
            //        if (dbResults.Rows.Count == 0)
            //        {
            //            return false;
            //        }

            //        DataRow prvRow = dbResults.Rows[0];
            //        //
            //        errorMessage = $"Potential Duplicate Posting! Was probably posted earlier on {Utils.ToIsoDateString(prvRow["CreatedAt"])} as part of file  {prvRow["cobra_file_name"]}";
            //        break;
                
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, "DuplicatePosting", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckCobraEmployerPlanExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            var errorMessage = "";
            //var cacheKey =
            //    $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-{dataRow.AccountTypeCode}-{dataRow.PlanId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    // check DB
            //    if (Utils.IsBlank(dataRow.EmployerId))
            //    {
            //        errorMessage += $"The Employer ID cannot be blank";
            //        ;
            //    }
            //    else if (Utils.IsBlank(dataRow.AccountTypeCode))
            //    {
            //        errorMessage += $"The AccountTypeCode cannot be blank";
            //        ;
            //    }
            //    else
            //    {
            //        DataTable dbResults = GetAllCobraPlansForEmployer(dataRow.EmployerId);

            //        // planid is not always present e.g. in deposit file
            //        string filter = $"employer_id = '{dataRow.EmployerId}'";
            //        if (!Utils.IsBlank(dataRow.AccountTypeCode))
            //        {
            //            filter += $" and account_type_code = '{dataRow.AccountTypeCode}' ";
            //        }

            //        if (!Utils.IsBlank(dataRow.PlanId))
            //        {
            //            filter += $" and plan_id = '{dataRow.PlanId}' ";
            //        }

            //        DataRow[] dbRows = dbResults.Select(filter);

            //        if (dbRows.Length == 0)
            //        {
            //            errorMessage +=
            //                $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                $" could not be found for Employer Id {dataRow.EmployerId}";
            //            ;
            //        }
            //        else
            //        {
            //            DataRow dbData = dbRows[0];
            //            DateTime actualPlanStartDate = (DateTime)dbData["plan_year_start_date"];
            //            DateTime actualPlanEndDate = (DateTime)dbData["plan_year_end_date"];
            //            //DateTime actualGracePeriodEndDate = (DateTime)dbData["grace_period_end_date"];

            //            //check start and end dates 
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) && !Utils.IsBlank(dataRow.PlanEndDate) &&
            //                Utils.ToDate(dataRow.PlanStartDate) > Utils.ToDate(dataRow.PlanEndDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate}";
            //            }

            //            //check plan dates match Cobra
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.PlanStartDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate}";
            //            }

            //            if (!Utils.IsBlank(dataRow.PlanEndDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.PlanEndDate))
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.PlanStartDate}";
            //                ;
            //            }

            //            //check effectivedate is within plan dates
            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate}";
            //            }

            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.EffectiveDate}";
            //                ;
            //            }
            //        }

            //        //
            //        _cache.Add(cacheKey, errorMessage);
            //    }
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckCobraEmployeePlanExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            var errorMessage = "";
            //var cacheKey =
            //    $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-{dataRow.EmployeeID}-{dataRow.AccountTypeCode}-{dataRow.PlanId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    // check DB
            //    if (Utils.IsBlank(dataRow.EmployerId))
            //    {
            //        errorMessage += $"The Employer ID cannot be blank";
            //        ;
            //    }
            //    else if (Utils.IsBlank(dataRow.EmployeeID))
            //    {
            //        errorMessage += $"The Employer ID cannot be blank";
            //        ;
            //    }
            //    else if (Utils.IsBlank(dataRow.AccountTypeCode))
            //    {
            //        errorMessage += $"The AccountTypeCode cannot be blank";
            //        ;
            //    }
            //    else
            //    {
            //        //// if we are enrolling an employee in a plan, only check if ER has this EE
            //        //if (versionNo == string.CobraEnrollment)
            //        //{
            //        //  //as it is an enrollment file, check the EE exists and enroll in the plan
            //        //  var hasError = this.CheckEmployeeExists(dataRow, column, versionNo);
            //        //  //return hasError;
            //        //}

            //        DataTable dbResults = GetAllCobraEmployeePlansForEmployer(dataRow.EmployerId);

            //        // planid is not always present e.g. in deposit file
            //        string filter = $" employeeid = '{dataRow.EmployeeID}' ";
            //        if (!Utils.IsBlank(dataRow.AccountTypeCode))
            //        {
            //            filter += $" and plancode = '{dataRow.AccountTypeCode}' ";
            //        }

            //        if (!Utils.IsBlank(dataRow.PlanId))
            //        {
            //            filter += $" and plandesc = '{dataRow.PlanId}' ";
            //        }

            //        DataRow[] dbRows = dbResults.Select(filter);

            //        if (dbRows.Length == 0)
            //        {
            //            if (true  /*|| versionNo == string.CobraEnrollment*/)
            //            {
            //                // as it is an enrollment, enroll the EE in this plan demographics file, 
            //                DataRow newRow = dbResults.NewRow();
            //                newRow["employerid"] = dataRow.EmployerId;
            //                newRow["employeeid"] = dataRow.EmployeeID;
            //                newRow["plancode"] = dataRow.AccountTypeCode;
            //                newRow["plandesc"] = dataRow.PlanId;
            //                newRow["planstart"] = Utils.ToDateTime(dataRow.PlanStartDate);
            //                newRow["planend"] = Utils.ToDateTime(dataRow.PlanEndDate);

            //                dbResults.Rows.Add(newRow);

            //                var cacheKey2 =
            //                    $"GetAllEmployeePlansForEmployer-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-AllEmployeePlans";
            //                //
            //                _cache.Add(cacheKey2, dbResults);

            //                //
            //                return false;
            //            }

            //            errorMessage +=
            //                $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                $" could not be found for Employee Id {dataRow.EmployeeID}";
            //            ;
            //        }
            //        else
            //        {
            //            DataRow dbData = dbRows[0];

            //            // for demographics file, the employee will not yet exist or the status may be changing (activating or terminating) - do not check

            //            DateTime actualPlanStartDate = (DateTime)dbData["planstart"];
            //            DateTime actualPlanEndDate = (DateTime)dbData["planend"];
            //            //DateTime? actualGracePeriodEndDate = Utils.ToDate(dbData["actualGracePeriodEndDate"]?.ToString());

            //            //note: we need to ensure we got Cobra plans going back many years properly. we have data from 2004 onwards in the portal
            //            //check start and end dates 
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) && !Utils.IsBlank(dataRow.PlanEndDate) &&
            //                Utils.ToDate(dataRow.PlanStartDate) > Utils.ToDate(dataRow.PlanEndDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate} for Employee Id {dataRow.EmployeeID}";
            //            }

            //            //check plan dates match Cobra
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.PlanStartDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate} for Employee Id {dataRow.EmployeeID}";
            //            }

            //            if (!Utils.IsBlank(dataRow.PlanEndDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.PlanEndDate)
            //                && dataRow.PlanEndDate != "20991231")
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.PlanEndDate} for Employee Id {dataRow.EmployeeID}";
            //                ;
            //            }

            //            //check effectivedate is within plan dates
            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate} for Employee Id {dataRow.EmployeeID}";
            //            }

            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.EffectiveDate} for Employee Id {dataRow.EmployeeID}";
            //                ;
            //            }
            //        }
            //    }

            //    //
            //    _cache.Add(cacheKey, errorMessage);
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckCobraDependentExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            // dependent plans are linked to the employee
            return CheckCobraEmployeeExists(dataRow, column, versionNo);
        }

        public Boolean CheckCobraDependentPlanExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            // dependent plans are linked to the employee
            return CheckCobraEmployeePlanExists(dataRow, column, versionNo);
        }

        #endregion checkData

        #region cacheCobraData

        private DataTable GetAllCobraEmployers()
        {
            DataTable dbResults = new DataTable();
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-AllEmployers";
            if (_cache.ContainsKey(cacheKey))
            {
                dbResults = (DataTable)_cache.Get(cacheKey);
            }
            else
            {
                if (PlatformType == PlatformType.Cobra)
                {
                    string queryString =
                        $"select employer_id, employer_name, employer_status from wc.wc_employers " +
                        $" order by employer_id ;";
                    //
                    dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        queryString, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on EmployeeID

                    DataColumn[] indices = new DataColumn[1];
                    indices[0] = (DataColumn)dbResults.Columns["employerid"];
                    dbResults.PrimaryKey = indices;

                    //
                    _cache.Add(cacheKey, dbResults);
                }
                else
                {
                    throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                }
            }

            return dbResults;
        }

        // cache all EE for ER to reduce number of queries to database - each query for a single EE takes around 150 ms so we aree saving significant time esp for ER witjh many EE
        private DataTable GetAllCobraEmployeesForEmployer(string employerId)
        {
            DataTable dbResults = new DataTable();
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{employerId}-AllEmployees";
            if (_cache.ContainsKey(cacheKey))
            {
                dbResults = (DataTable)_cache.Get(cacheKey);
            }
            else
            {
                if (PlatformType == PlatformType.Cobra)
                {
                    string queryString =
                        $"SELECT employerid, employeeid, wc.wc_is_active_status(employeestatus, employeeid,employerid) is_active " +
                        $" FROM wc.wc_participants " +
                        $" where employerid = '{Utils.DbQuote(employerId)}' " +
                        $" ORDER by employeeid ";
                    //
                    dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        queryString, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on EmployeeID

                    DataColumn[] indices = new DataColumn[1];
                    indices[0] = (DataColumn)dbResults.Columns["employeeid"];
                    dbResults.PrimaryKey = indices;

                    //
                    _cache.Add(cacheKey, dbResults);
                }
                else
                {
                    throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                }
            }

            return dbResults;
        } // cache all EE for ER to reduce number of queries to database - each query for a single EE takes around 150 ms so we aree saving significant time esp for ER witjh many EE
        // cache all plans for ER to reduce number of queries to database - each query for a single plan takes around 150 ms so we aree saving significant time esp for ER witjh many EE

        private DataTable GetAllCobraPlansForEmployer(string employerId)
        {
            DataTable dbResults = new DataTable();
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{employerId}-AllEmployerPlans";
            if (_cache.ContainsKey(cacheKey))
            {
                dbResults = (DataTable)_cache.Get(cacheKey);
            }
            else
            {
                if (PlatformType == PlatformType.Cobra)
                {
                    // todo: we need check exactly check against each plan - min/max are not correct
                    string queryString =
                            $"select employer_id, account_type_code, plan_id, date(min(plan_year_start_date)) as plan_year_start_date, date(max(plan_year_end_date)) as plan_year_end_date /* , max(grace_period_end_date) grace_period_end_date*/ " +
                            $" from wc.vw_wc_employer_plans_combined " +
                            $" where employer_id = '{Utils.DbQuote(employerId)}' " +
                            $" group by employer_id, account_type_code, plan_id " +
                            $" order by employer_id, plan_id, account_type_code "
                        ;
                    //
                    dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        queryString, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on EmployeeID

                    DataColumn[] indices = new DataColumn[2];
                    indices[0] = (DataColumn)dbResults.Columns["account_type_code"];
                    indices[1] = (DataColumn)dbResults.Columns["plan_id"];
                    dbResults.PrimaryKey = indices;

                    //
                    _cache.Add(cacheKey, dbResults);
                }
                else
                {
                    throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                }
            }

            return dbResults;
        }

        private DataTable GetAllCobraEmployeePlansForEmployer(string employerId)
        {
            DataTable dbResults = new DataTable();
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{employerId}-AllEmployeePlans";
            if (_cache.ContainsKey(cacheKey))
            {
                dbResults = (DataTable)_cache.Get(cacheKey);
            }
            else
            {
                if (PlatformType == PlatformType.Cobra)
                {
                    // todo: we need check exactly check against each plan - min/max are not correct
                    // get ALL plans
                    string queryString1 =
                            $" select employerid, employeeid, plancode, plandesc, date(min(planstart)) as planstart, date(max(planend)) as planend " +
                            $" from wc.vw_wc_participant_plans_combined " +
                            $" where employerid = '{Utils.DbQuote(employerId)}' " +
                            $" group by employerid, employeeid, plancode, plandesc" +
                            $" order by employerid, employeeid, plancode, plandesc"
                        ;
                    ;
                    //

                    DataTable dt1 = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        queryString1, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on EmployeeID

                    DataColumn[] indices = new DataColumn[3];
                    indices[0] = (DataColumn)dt1.Columns["employeeid"];
                    indices[1] = (DataColumn)dt1.Columns["plancode"];
                    indices[2] = (DataColumn)dt1.Columns["plandesc"];
                    dt1.PrimaryKey = indices;

                    //
                    dbResults = dt1;
                    _cache.Add(cacheKey, dbResults);
                }
                else
                {
                    throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                }
            }

            return dbResults;
        }

        #endregion cacheEmployerData


        #region CheckUtils

        public string EnsureValueIsOfFormatAndMatchesRules(cobra_file_table_stage dataRow, TypedCsvColumn column,
            TypedCsvSchema mappings)
        {
            var orgValue = dataRow.ColumnValue(column.SourceColumn) ?? "";
            var value = orgValue;

            // always trim
            value = value?.Trim();

            //1. Check and fix format
            if (!Utils.IsBlank(value))
            {
                // fix value if possible
                switch (column.FormatType)
                {
                    case FormatType.Any:
                    case FormatType.String:
                        break;

                    case FormatType.Email:
                        if (!Utils.IsValidEmail(value))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a valid Email. {orgValue} is not valid");
                        }

                        break;
                    case FormatType.Zip:
                        value = regexInteger.Replace(value, String.Empty);
                        if (!Utils.IsBlank(value) && value.Length > column.MaxLength)
                        {
                            value = value.Substring(0, column.MaxLength);
                        }

                        break;
                    case FormatType.Phone:
                        value = regexInteger.Replace(value, String.Empty);
                        if (!Utils.IsBlank(value) && value.Length > column.MaxLength)
                        {
                            value = Utils.Right(value, column.MaxLength);
                        }

                        break;
                    case FormatType.AlphaNumeric:
                        // replace all non alphanumeric
                        value = regexAlphaNumeric.Replace(value, String.Empty);
                        break;

                    case FormatType.AlphaOnly:
                        // replace all non alphanumeric
                        value = regexAlphaOnly.Replace(value, String.Empty);
                        break;

                    case FormatType.FixedConstant:
                        // default to fixed value always!
                        value = column.FixedValue;
                        break;

                    case FormatType.AlphaAndDashes:
                        // replace all non alphanumeric
                        value = regexAlphaAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.NumbersAndDashes:
                        // replace all non alphanumeric
                        value = regexNumericAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.Integer:
                        // remove any non digits
                        value = regexInteger.Replace(value, String.Empty);
                        //
                        if (!Utils.IsInteger(value))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be numbers only. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.Double:
                        // remove any non digits and non . and non ,
                        value = regexDouble.Replace(value, String.Empty);
                        if (!Utils.IsDouble(value))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a Currency Value. {orgValue} is not valid");
                        }

                        // format as 0.00
                        var dblValue = Utils.ToDouble(value);
                        value = dblValue.ToString("0.00");

                        break;

                    case FormatType.IsoDate:
                        // remove any non digits
                        value = regexDate.Replace(value, String.Empty);
                        value = Utils.ToIsoDateString(Utils.ToDate(value));
                        if (!Utils.IsIsoDate(value, column.MaxLength > 0))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.IsoDateTime:
                        // remove any non digits
                        value = regexDate.Replace(value, String.Empty);
                        value = Utils.ToDateTimeString(Utils.ToDateTime(value));

                        if (!Utils.IsIsoDateTime(value, column.MaxLength > 0))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.YesNo:
                        if (!value.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("No", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.TrueFalse:
                        if (!value.Equals("True", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("False", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. {orgValue} is not valid");
                        }

                        break;

                    default:
                        break;
                }
            }

            value = value?.Trim();

            //set default value
            if (Utils.IsBlank(value) && !Utils.IsBlank(column.DefaultValue))
            {
                value = column.DefaultValue;
            }

            // pad ssn to 9 digits with leading zeros
            if ((column.SourceColumn == "EmployeeSocialSecurityNumber" || column.SourceColumn == "EmployeeID"))

            {
                if (!Utils.IsBlank(value))
                {
                    value = regexAlphaNumeric.Replace(value, String.Empty);
                    if (value.Length < column.MinLength)
                    {
                        value = value.PadLeft(column.MinLength, '0');
                    }
                }
            }

            // set row column value to the fixed value if it has changed
            if (value != orgValue)
            {
                dataRow.SetColumnValue(column.SourceColumn, value);
                dataRow.data_row = GetDelimitedDataRow(dataRow, mappings);
            }

            // 2. check against GENERAL rules
            if (column.FixedValue != null && value != column.FixedValue &&
                !column.FixedValue.Split('|').Contains(value) && column.MinLength > 0)
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must always be {column.FixedValue}. {orgValue} is not valid");
            }

            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. {orgValue} is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. {orgValue} is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number. {orgValue} is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. {orgValue} is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. {orgValue} is not valid");
                }
            }

            return value;
        }

        private string GetDelimitedDataRow(cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            string value = "";

            foreach (TypedCsvColumn column in mappings)
            {
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    case "":
                    case "source_row_no":
                    case "error_row":
                    case "data_row":
                    case "res_file_name":
                    case "cobra_file_name":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }

                string fieldValue = dataRow.ColumnValue(column.SourceColumn);
                if (fieldValue?.IndexOf(",", StringComparison.InvariantCulture) > 0)
                {
                    fieldValue = $"\"{fieldValue}\"";
                }

                value += $",{fieldValue}";
            }

            // remove first char
            value = value.Substring(1);
            //
            return value;
        }

        #endregion
    }
}
