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

                switch (dataRow.row_type)
                {
                    case "[QB]":
                        // 2. specific column checking against business rules & DB
                        switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                        {
                            case "clientname":
                            case "clientdivisionname":
                                hasError = this.CheckCobraClientAndDivisionExists(dataRow, column, versionNo);
                                break;

                            case "ssn":
                            case "individualid":
                                hasError = this.CheckCobraEEExists(dataRow, column, versionNo, "QB");
                                //todo: what to do if the QB alrteady exists in the DB?
                                break;

                            default:
                                break;
                        }
                        break;

                    case "[SPM]":
                        // 2. specific column checking against business rules & DB
                        switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                        {
                            case "clientname":
                            case "clientdivisionname":
                                hasError = this.CheckCobraClientAndDivisionExists(dataRow, column, versionNo);
                                break;

                            case "ssn":
                            case "individualid":
                                hasError = this.CheckCobraEEExists(dataRow, column, versionNo, "SPM");
                                //todo: what to do if the QB alrteady exists in the DB?
                                break;

                            default:
                                break;
                        }
                        break;

                    case "[NPM]":
                        // 2. specific column checking against business rules & DB
                        switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                        {
                            case "clientname":
                            case "clientdivisionname":
                                hasError = this.CheckCobraClientAndDivisionExists(dataRow, column, versionNo);
                                break;

                            case "ssn":
                            case "individualid":
                                hasError = this.CheckCobraEEExists(dataRow, column, versionNo, "SPM");
                                //todo: what to do if the QB alrteady exists in the DB?
                                break;

                            default:
                                break;
                        }
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

        public DataRow[] GetCobraClientAndDivisionRows(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            DataRow[] clientRows = null;
            // check DB
            if (Utils.IsBlank(dataRow.ClientName))
            {
                clientRows = null;
            }
            else
            {
                DataTable dbResults = GetAllCobraClientAndDivisions();
                // planid is not always present e.g. in deposit file
                string filter = $"ClientName = '{Utils.DbQuote(dataRow.ClientName)}' ";
                if (!Utils.IsBlank(dataRow.ClientDivisionName))
                {
                    filter += $" And DivisionName = '{Utils.DbQuote(dataRow.ClientDivisionName)}' ";
                }
                clientRows = dbResults.Select(filter);

            } // check client
              //

            return clientRows;
        }

        public Boolean CheckCobraClientAndDivisionExists(cobra_file_table_stage dataRow, TypedCsvColumn column, string versionNo)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-{dataRow.ClientDivisionName}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                DataRow[] dbRows = GetCobraClientAndDivisionRows(dataRow, column, versionNo);

                if (dbRows.Length == 0)
                {
                    errorMessage = $"The Client Name {dataRow.ClientName} and DivisionName {dataRow.ClientDivisionName} could not be found";
                }
                else
                {
                    //DataRow dbData = dbRows[0];

                    //note: FileChecker: verify if Client status need to be checked
                    //string status = dbData["Client_status"]?.ToString();
                    //if (status != "Active" && status != "New")
                    //{
                    //  errorMessage =
                    //    $"The Client Name {dataRow.ClientName} has status {status} which is not valid";
                    //}


                } // results.count
            } // check client

            //
            _cache.Add(cacheKey, errorMessage);

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

        public Boolean CheckCobraEEExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo, string entityType)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-{dataRow.row_type}-{dataRow.SSN}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(dataRow.ClientName))
                {
                    AddCobraErrorForRow(dataRow, "ClientName", $"The Client Name cannot be blank");
                }
                else if (Utils.IsBlank(dataRow.SSN))
                {
                    AddCobraErrorForRow(dataRow, "SSN", $"The SSN cannot be blank");
                }
                else
                {
                    DataTable dbResults = GetAllCobraEEForClient(dataRow, column, versionNo, entityType);
                    if (dbResults.Rows.Count > 0)
                    {
                        var filter = "";
                        filter += $"SSNFormatted = '{dataRow.SSN}'";

                        DataRow[] dbRows = dbResults.Select(filter);

                        if (dbRows.Length == 0)
                        {
                            // for demographics file, the employee will not yet exist or the status may be changing (activating or terminating) - do not check
                            if (true /*|| versionNo == string.CobraDemographics*/)
                            {
                                // as it is an demographics file, add this employee to the ER-EE table so a check for plan enrollemnt within same run or before reaggregation from Cobra will suceed
                                DataRow newRow = dbResults.NewRow();
                                newRow["ClientName"] = dataRow.ClientName;
                                newRow["SSN"] = dataRow.SSN;
                                newRow["is_active"] = dataRow.Active == "1" ? 1 : 0;
                                dbResults.Rows.Add(newRow);

                                var cacheKey2 =
                                    $"GetAllEmployeesForClient-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-AllEmployees";
                                _cache.Add(cacheKey2, dbResults);

                                //
                                return false;
                            }
                            else
                            {
                                errorMessage +=
                                    $"A {entityType} with SSN {dataRow.SSN} could not be found for Client Name {dataRow.ClientName} and DivisionName {dataRow.ClientDivisionName}";
                            }
                        }
                    }
                    else
                    {
                        errorMessage +=
                                   $"A {entityType} with SSN {dataRow.SSN} could not be found for Client Name {dataRow.ClientName} and DivisionName {dataRow.ClientDivisionName}";
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

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
                  string versionNo)
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
            //            $" and ClientName='{dataRow.ClientName}'" +
            //            $" and MemberId='{dataRow.MemberId}'" +
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

        public Boolean CheckCobraClientPlanExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            var errorMessage = "";
            //var cacheKey =
            //    $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-{dataRow.AccountTypeCode}-{dataRow.PlanId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    // check DB
            //    if (Utils.IsBlank(dataRow.ClientName))
            //    {
            //        errorMessage += $"The Client Name cannot be blank";
            //        ;
            //    }
            //    else if (Utils.IsBlank(dataRow.AccountTypeCode))
            //    {
            //        errorMessage += $"The AccountTypeCode cannot be blank";
            //        ;
            //    }
            //    else
            //    {
            //        DataTable dbResults = GetAllCobraPlansForClient(dataRow.ClientName);

            //        // planid is not always present e.g. in deposit file
            //        string filter = $"ClientName = '{dataRow.ClientName}'";
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
            //                $" could not be found for Client Name {dataRow.ClientName}";
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
            //    $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-{dataRow.MemberId}-{dataRow.AccountTypeCode}-{dataRow.PlanId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    // check DB
            //    if (Utils.IsBlank(dataRow.ClientName))
            //    {
            //        errorMessage += $"The Client Name cannot be blank";
            //        ;
            //    }
            //    else if (Utils.IsBlank(dataRow.MemberId))
            //    {
            //        errorMessage += $"The Client Name cannot be blank";
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

            //        DataTable dbResults = GetAllCobraEmployeePlansForClient(dataRow.ClientName);

            //        // planid is not always present e.g. in deposit file
            //        string filter = $" MemberId = '{dataRow.MemberId}' ";
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
            //                newRow["ClientName"] = dataRow.ClientName;
            //                newRow["MemberId"] = dataRow.MemberId;
            //                newRow["plancode"] = dataRow.AccountTypeCode;
            //                newRow["plandesc"] = dataRow.PlanId;
            //                newRow["planstart"] = Utils.ToDateTime(dataRow.PlanStartDate);
            //                newRow["planend"] = Utils.ToDateTime(dataRow.PlanEndDate);

            //                dbResults.Rows.Add(newRow);

            //                var cacheKey2 =
            //                    $"GetAllEmployeePlansForClient-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-AllEmployeePlans";
            //                //
            //                _cache.Add(cacheKey2, dbResults);

            //                //
            //                return false;
            //            }

            //            errorMessage +=
            //                $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                $" could not be found for Employee Id {dataRow.MemberId}";
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
            //                    $" Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate} for Employee Id {dataRow.MemberId}";
            //            }

            //            //check plan dates match Cobra
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.PlanStartDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate} for Employee Id {dataRow.MemberId}";
            //            }

            //            if (!Utils.IsBlank(dataRow.PlanEndDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.PlanEndDate)
            //                && dataRow.PlanEndDate != "20991231")
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.PlanEndDate} for Employee Id {dataRow.MemberId}";
            //                ;
            //            }

            //            //check effectivedate is within plan dates
            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate} for Employee Id {dataRow.MemberId}";
            //            }

            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.EffectiveDate} for Employee Id {dataRow.MemberId}";
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


        #endregion checkData

        #region cacheCobraData

        private DataTable GetAllCobraClientAndDivisions()
        {
            DataTable dbResults = new DataTable();
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-AllClients";
            if (_cache.ContainsKey(cacheKey))
            {
                dbResults = (DataTable)_cache.Get(cacheKey);
            }
            else
            {
                if (PlatformType == PlatformType.Cobra)
                {
                    string queryString =
                        $"select * from dbo.AllClientsAndDivisions  " +
                        $" order by ClientName, DivisionName ;";
                    //
                    dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnCobra,
                        queryString, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on MemberId

                    DataColumn[] indices = new DataColumn[1];
                    indices[0] = (DataColumn)dbResults.Columns["ClientID"];
                    indices[0] = (DataColumn)dbResults.Columns["DivisionID"];
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
        private DataTable GetAllCobraEEForClient(cobra_file_table_stage dataRow, TypedCsvColumn column, string versionNo, string entityType)
        {
            DataTable dbResults = new DataTable();
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-{dataRow.ClientDivisionName}-All{entityType}";
            if (_cache.ContainsKey(cacheKey))
            {
                dbResults = (DataTable)_cache.Get(cacheKey);
            }
            else
            {
                if (PlatformType == PlatformType.Cobra)
                {
                    // get client and division id
                    var clientId = "";
                    var divisionId = "";

                    DataRow[] clientRows = GetCobraClientAndDivisionRows(dataRow, column, versionNo);
                    if (clientRows != null && clientRows.Length > 0)
                    {
                        clientId = clientRows.First<DataRow>()["ClientID"].ToString();
                        divisionId = clientRows.First<DataRow>()["ClientDivisionID"].ToString();
                    }

                    if (Utils.IsBlank(clientId) || (!Utils.IsBlank(dataRow.ClientDivisionName) && Utils.IsBlank(divisionId)))
                    {
                        // no rows
                    }
                    else
                    {
                        var IndidiviualIDField = entityType == "QB" ? "IndividualIdentifier" : "IndividualID";
                        string queryString =
                            $"SELECT *, replace(SSN, '-', '') as SSNFormatted " +
                            $" FROM dbo.{entityType} " +
                            $" where ClientId = '{Utils.DbQuote(clientId)}' " +
                            $" ORDER by MemberId ";
                        //
                        dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnCobra,
                            queryString, null,
                            FileLogParams?.GetMessageLogParams());

                        // create index on MemberId

                        //DataColumn[] indices = new DataColumn[1];
                        //indices[0] = (DataColumn)dbResults.Columns["ClientId"];
                        //indices[1] = (DataColumn)dbResults.Columns["MemberId"];
                        //dbResults.PrimaryKey = indices;
                    }

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
        // cache all plans for ER to reduce number of queries to database - each query for a single plan takes around 150 ms so we aree saving significant time esp for ER witjh many EE

        private DataTable GetAllCobraPlansForClient(string ClientName)
        {
            DataTable dbResults = new DataTable();
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{ClientName}-AllClientPlans";
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
                            $"select ClientName, account_type_code, plan_id, date(min(plan_year_start_date)) as plan_year_start_date, date(max(plan_year_end_date)) as plan_year_end_date /* , max(grace_period_end_date) grace_period_end_date*/ " +
                            $" from wc.vw_wc_Client_plans_combined " +
                            $" where ClientName = '{Utils.DbQuote(ClientName)}' " +
                            $" group by ClientName, account_type_code, plan_id " +
                            $" order by ClientName, plan_id, account_type_code "
                        ;
                    //
                    dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnCobra,
                        queryString, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on MemberId

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

        private DataTable GetAllCobraEmployeePlansForClient(string ClientName)
        {
            DataTable dbResults = new DataTable();
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{ClientName}-AllEmployeePlans";
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
                            $" select ClientName, MemberId, plancode, plandesc, date(min(planstart)) as planstart, date(max(planend)) as planend " +
                            $" from wc.vw_wc_participant_plans_combined " +
                            $" where ClientName = '{Utils.DbQuote(ClientName)}' " +
                            $" group by ClientName, MemberId, plancode, plandesc" +
                            $" order by ClientName, MemberId, plancode, plandesc"
                        ;
                    ;
                    //

                    DataTable dt1 = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnCobra,
                        queryString1, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on MemberId

                    DataColumn[] indices = new DataColumn[3];
                    indices[0] = (DataColumn)dt1.Columns["MemberId"];
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

        #endregion cacheClientData


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
                                $"{column.SourceColumn} must be a valid Email. '{orgValue}' is not valid");
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

                    case FormatType.AlphaNumericAndDashes:
                        // replace all non alphanumeric
                        value = regexAlphaNumericAndDashes.Replace(value, String.Empty);
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
                                $"{column.SourceColumn} must be numbers only. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.Double:
                        // remove any non digits and non . and non ,
                        value = regexDouble.Replace(value, String.Empty);
                        if (!Utils.IsDouble(value))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a Currency Value. '{orgValue}' is not valid");
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
                                $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.IsoDateTime:
                        // remove any non digits
                        value = regexDate.Replace(value, String.Empty);
                        value = Utils.ToDateTimeString(Utils.ToDateTime(value));

                        if (!Utils.IsIsoDateTime(value, column.MaxLength > 0))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.CobraYesNo:
                    case FormatType.YesNo:
                    case FormatType.TrueFalse:
                        switch (value.ToUpperInvariant())
                        {
                            case "TRUE":
                            case "T":
                            case "YES":
                            case "1":
                                value = "1";
                                break;
                            case "NO":
                            case "FALSE":
                            case "F":
                            case "0":
                                value = "0";
                                break;
                        }

                        if (!value.Equals("1", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("0", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either 1/Yes/True or 0/No/False. '{orgValue}' is not valid");
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
            if ((column.SourceColumn == "EmployeeSocialSecurityNumber" || column.SourceColumn == "SSN" || column.SourceColumn == "MemberId" || column.FormatType == FormatType.SSN))

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

            if (!Utils.IsBlank(column.FixedValue) && value != column.FixedValue && column.MinLength > 0 && (!column.FixedValue.Split('|').Contains(value)))
            {
                if (column.FixedValue == "<BLANK>" & value != "")
                {
                    value = "";
                }
                else
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must always be one of or ezxactly {column.FixedValue}. '{orgValue}' is not valid");
                }
            }


            // set row column value to the fixed value if it has changed
            if (value != orgValue)
            {
                dataRow.SetColumnValue(column.SourceColumn, value);
                dataRow.data_row = GetDelimitedDataRow(dataRow, mappings);
            }

            // 2. check against GENERAL rules
            // 2b. check matches possible values

            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. '{orgValue}' is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. '{orgValue}' is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number. '{orgValue}' is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. '{orgValue}' is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. '{orgValue}' is not valid");
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
