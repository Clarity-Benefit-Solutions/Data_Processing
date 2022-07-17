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
        #region CheckAlegeusFile

        private void CheckAlegeusFile(Dictionary<EdiFileFormat, List<int>> fileFormats)
        {
            // 2. import the file
            string fileName = Path.GetFileName(SrcFilePath) ?? string.Empty;
            FileLogParams?.SetFileNames(Utils.GetUniqueIdFromFileName(fileName), fileName, SrcFilePath, "", "",
                "CheckFile", $"Starting: Check {fileName}", "Starting");

            // split text fileinto multiple files
            Dictionary<EdiFileFormat, object[]> files = new Dictionary<EdiFileFormat, object[]>();

            //
            foreach (EdiFileFormat fileFormat in fileFormats.Keys)
            {
                // get temp file for each format
                string splitFileName = Path.GetTempFileName();
                FileUtils.EnsurePathExists(splitFileName);
                //
                var splitFileWriter = new StreamWriter(splitFileName, false);
                files.Add(fileFormat, new Object[] { splitFileWriter, splitFileName });
            }

            // open file for reading
            // read each line and insert
            using (var inputFile = new StreamReader(this.SrcFilePath))
            {
                int rowNo = 0;
                string line;
                while ((line = inputFile.ReadLine()!) != null)
                {
                    rowNo++;

                    foreach (EdiFileFormat fileFormat2 in fileFormats.Keys)
                    {
                        if (
                            fileFormats[fileFormat2].Contains(rowNo)
                            || Utils.Left(line, 2) == "RA" || Utils.Left(line, 2) == "IA"
                        )
                        {
                            // get temp file for each format
                            var splitFileWriter = (StreamWriter)files[fileFormat2][0];
                            // if there is prvUnwrittenLine it was probably a header line - write to the file that 

                            splitFileWriter.WriteLine(line);
                            continue;
                        }
                    }

                    // go to next line if a line was written
                }
            }

            // close all files
            //
            foreach (var fileFormat3 in files.Keys)
            {
                // get temp file for each format
                var writer = (StreamWriter)files[fileFormat3][0];
                writer.Close();

                // import and check the file
                CheckAlegeusFile(fileFormat3, (string)files[fileFormat3][1]);
            }
        }

        private void CheckAlegeusFile(EdiFileFormat fileFormat, string currentFilePath)
        {
            string tableName = "";
            TypedCsvSchema mappings = new TypedCsvSchema();
            string postImportProc = "";

            // check mappings and type of file (Import or Result)
            Boolean isResultFile = Import.GetAlegeusFileFormatIsResultFile(fileFormat);
            if (isResultFile)
            {
                return;
            }

            // get header type from filename
            var headerType = Import.GetAlegeusHeaderTypeFromFile(currentFilePath);

            // get columns for file based on header type
            mappings = Import.GetAlegeusFileImportMappings(fileFormat, headerType);

            //
            tableName = "[dbo].[mbi_file_table_stage]";
            postImportProc = "[dbo].[process_mbi_file_table_stage_import]";

            //todo: need to check changed code
            Import.ImportAlegeusFile(fileFormat, DbConn, currentFilePath, OriginalSrcFilePath, this.hasHeaderRow, FileLogParams, null);

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
            CheckAlegeusFileData(fileFormat, mappings);

            // run post import proc to take data from stage into final table
            string queryString = $"exec {postImportProc};";
            //
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, DbConn, queryString, null,
                FileLogParams?.GetMessageLogParams()
            );
        }

        private void CheckAlegeusFileData(EdiFileFormat fileFormat, TypedCsvSchema mappings)
        {
            // ensure previously cached data is not used so
            // so create a new db context to ensure stale data will NOT be used
            var dbDataProcessing = Vars.dbCtxDataProcessingNew;

            // get all dbRows without caching
            var mbiRows = dbDataProcessing.mbi_file_table_stage
                .OrderBy(mbiRow => mbiRow.source_row_no)
                .ToList();

            //check each mbiRow
            int rowNo = 0;
            foreach (var mbiRow in mbiRows)
            {
                rowNo++;
                this.CheckAlegeusRowData(fileFormat, mbiRow, mappings);
            }

            // save any changes
            dbDataProcessing.SaveChanges();
        }

        private void CheckAlegeusRowData(EdiFileFormat fileFormat, mbi_file_table_stage mbiRow, TypedCsvSchema mappings)
        {
            // don't check header mbiRow
            if (mbiRow.row_type == "IA" || mbiRow.row_type == "RA")
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
                    case "mbi_file_name":
                    case "row_type":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }

                // save Org Column value before changes
                var orgValue = mbiRow.ColumnValue(column.SourceColumn) ?? "";

                // 1. valid Format and general rules check - save corrected value to row
                var formattedValue = EnsureValueIsOfFormatAndMatchesRules(mbiRow, column, mappings);
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
                    case "mbi_file_name":
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
                        hasError = this.CheckAlegeusTpaExists(mbiRow, column, fileFormat);
                        break;

                    // ER ID
                    case "employerid":
                        //ER must exist before any Import files are sent
                        hasError = this.CheckAlegeusEmployerExists(mbiRow, column, fileFormat);
                        break;

                    // EE ID
                    case "employeeid":
                        //ER must exist before any Import files are sent. But for IB files, employee need not exist - he is being added
                        hasError = this.CheckAlegeusEmployeeExists(mbiRow, column, fileFormat);

                        break;
                    // plan related
                    case "planid":
                    case @"accounttypecode":
                        if (fileFormat == EdiFileFormat.AlegeusEnrollment)
                        {
                            hasError = this.CheckAlegeusEmployerPlanExists(mbiRow, column, fileFormat);
                        }
                        else if (fileFormat == EdiFileFormat.AlegeusEmployeeDeposit)
                        {
                            hasError = this.CheckAlegeusEmployeePlanExists(mbiRow, column, fileFormat);
                        }

                        break;

                    default:
                        break;
                }
            }
            // check for duplicate posting of the row
            hasError = CheckForDuplicateAlegeusPosting(mbiRow, fileFormat);
        }

        private void AddAlegeusErrorForRow(mbi_file_table_stage mbiRow, string errCode, string errMessage,
            Boolean markAsCompleteFail = false)
        {
            // add to mbiRow so it will be saved back to DB for mbiRow by mbiRow data

            if (Utils.IsBlank(mbiRow.error_code))
            {
                mbiRow.error_code = errCode;
            }
            else
            {
                mbiRow.error_code = ErrorSeparator + errCode;
            }

            if (Utils.IsBlank(mbiRow.error_message))
            {
                mbiRow.error_message = errMessage;
            }
            else
            {
                mbiRow.error_message = ErrorSeparator + errMessage;
            }

            if (mbiRow.error_code.StartsWith(ErrorSeparator))
            {
                mbiRow.error_code = mbiRow.error_code.Substring(1);
            }

            if (mbiRow.error_message.StartsWith(ErrorSeparator))
            {
                mbiRow.error_message = mbiRow.error_message.Substring(1);
            }

            //
            int key = mbiRow.source_row_no ?? 0;
            if (this.fileCheckResults.ContainsKey(key))
            {
                this.fileCheckResults[key] = $"{mbiRow.source_row_no}: {errCode} : {errMessage}";
            }
            else
            {
                this.fileCheckResults.Add(key, $"{mbiRow.source_row_no}: {errCode} : {errMessage}");
            }

            //
            if (markAsCompleteFail)
            {
                this.fileCheckResults.MarkAsCompleteFail = true;
            }
        }


        public Boolean CheckAlegeusTpaExists(mbi_file_table_stage mbiRow, TypedCsvColumn column, EdiFileFormat fileFormat)
        {
            var errorMessage = "";
            var cacheKey = $"{MethodBase.GetCurrentMethod()?.Name}-{mbiRow.TpaId}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                if (PlatformType == PlatformType.Alegeus)
                {
                    // check DB
                    if (Utils.IsBlank(column.FixedValue))
                    {
                        if (Utils.IsBlank(mbiRow.TpaId))
                        {
                            errorMessage = "TPA ID cannot be blank. It must always be BENEFL";
                        }
                        else if (mbiRow.TpaId != "BENEFL")
                        {
                            errorMessage = $"TPA ID {mbiRow.TpaId} is invalid. It must always be BENEFL";
                        }
                    }
                }
                else
                {
                    throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployerExists(mbi_file_table_stage mbiRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{mbiRow.EmployerId}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(mbiRow.EmployerId))
                {
                    errorMessage = $"The Employer ID cannot be blank";
                }
                else
                {
                    DataTable dbResults = GetAllAlegeusEmployers();
                    // planid is not always present e.g. in deposit file
                    string filter = $"employer_id = '{mbiRow.EmployerId}'";
                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        errorMessage = $"The Employer ID {mbiRow.EmployerId} could not be found";
                    }
                    else
                    {
                        //DataRow dbRow = dbRows[0];

                        //note: FileChecker: verify if employer status need to be checked
                        //string status = dbRow["employer_status"]?.ToString();
                        //if (status != "Active" && status != "New")
                        //{
                        //  errorMessage =
                        //    $"The Employer ID {mbiRow.EmployerId} has status {status} which is not valid";
                        //}
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployeeExists(mbi_file_table_stage mbiRow, TypedCsvColumn column,
            EdiFileFormat fileFormat = EdiFileFormat.Unknown)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{mbiRow.EmployerId}-{mbiRow.EmployeeID}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(mbiRow.EmployerId))
                {
                    AddAlegeusErrorForRow(mbiRow, "EmployerId", $"The Employer ID cannot be blank");
                }
                else if (Utils.IsBlank(mbiRow.EmployeeID))
                {
                    AddAlegeusErrorForRow(mbiRow, "EmployeeId", $"The Employee ID cannot be blank");
                }
                else
                {
                    DataTable dbResults = GetAllAlegeusEmployeesForEmployer(mbiRow.EmployerId);
                    DataRow[] dbRows =
                        dbResults.Select(
                            $"employerid = '{mbiRow.EmployerId}' and employeeid = '{mbiRow.EmployeeID}'");
                    if (dbRows.Length == 0)
                    {
                        // for demographics file, the employee will not yet exist or the status may be changing (activating or terminating) - do not check
                        if (fileFormat == EdiFileFormat.AlegeusDemographics)
                        {
                            // as it is an demographics file, add this employee to the ER-EE table so a check for plan enrollemnt within same run or before reaggregation from Alegeus will suceed
                            DataRow newRow = dbResults.NewRow();
                            newRow["employerid"] = mbiRow.EmployerId;
                            newRow["employeeid"] = mbiRow.EmployeeID;
                            newRow["is_active"] = mbiRow.EmployeeStatus == "2" ? 1 : 0;
                            dbResults.Rows.Add(newRow);

                            var cacheKey2 =
                                $"GetAllEmployeesForEmployer-{this.PlatformType.ToDescription()}-{mbiRow.EmployerId}-AllEmployees";
                            _cache.Add(cacheKey2, dbResults);

                            //
                            return false;
                        }
                        else
                        {
                            errorMessage +=
                                $"The Employee ID {mbiRow.EmployeeID} could not be found for Employer Id {mbiRow.EmployerId}";
                        }
                    }
                    else
                    {
                        DataRow dbRow = dbRows[0];
                        // if employee exists as per our data, that is fine
                        // do not check the file EmployeeStatus against what we havwe in the db
                        //float status = Utils.ToNumber(dbRow["is_active"]?.ToString());
                        //if (status <= 0 && Utils.ToNumber(mbiRow.EmployeeStatus) > 1)
                        //{
                        //  errorMessage +=
                        //    $"The Employee ID {mbiRow.EmployeeID} has status {status} which is not valid";
                        //}
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }
        public Boolean CheckForDuplicateAlegeusPosting(mbi_file_table_stage mbiRow,
                  EdiFileFormat fileFormat = EdiFileFormat.Unknown)
        {


            string errorMessage = "";

            //
            switch (fileFormat)
            {
                case EdiFileFormat.AlegeusEmployeeDeposit:
                    string queryString =
                        $"select * from  [mbi_file_table] " +
                        $" where " +
                        $" TpaId='{mbiRow.TpaId}'" +
                        $" and EmployerId='{mbiRow.EmployerId}'" +
                        $" and EmployeeID='{mbiRow.EmployeeID}'" +
                        $" and AccountTypeCode='{mbiRow.AccountTypeCode}'" +
                        $" and PlanEndDate='{mbiRow.PlanEndDate}'" +
                        $" and PlanStartDate='{mbiRow.PlanStartDate}'" +
                        $" and EffectiveDate='{mbiRow.EffectiveDate}'" +
                        $" and DepositType='{mbiRow.DepositType}'" +
                        // todo: flag only if 
                        // check amounts also - deposit type ER / EE
                        // EmployeeDepositAmount ?? ??
                        // EmployerDepositAmount ?? ??
                        //$" and DepositType='{mbiRow.DepositType}'" +
                        $" and len(isnull(error_message, '')) = 0" +
                        $" order by row_id desc, mbi_file_name, source_row_no ;";
                    //
                    DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, DbConn,
                         queryString, null,
                         FileLogParams?.GetMessageLogParams());
                    //
                    if (dbResults.Rows.Count == 0)
                    {
                        return false;
                    }

                    DataRow prvRow = dbResults.Rows[0];
                    //
                    errorMessage = $"Potential Duplicate Posting! Was probably posted earlier on {Utils.ToIsoDateString(prvRow["CreatedAt"])} as part of file  {prvRow["mbi_file_name"]}";
                    break;
                default:
                    break;
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(mbiRow, "DuplicatePosting", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployerPlanExists(mbi_file_table_stage mbiRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{mbiRow.EmployerId}-{mbiRow.AccountTypeCode}-{mbiRow.PlanId}-{mbiRow.PlanStartDate}-{mbiRow.PlanEndDate}-{mbiRow.EffectiveDate}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(mbiRow.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(mbiRow.AccountTypeCode))
                {
                    errorMessage += $"The AccountTypeCode cannot be blank";
                    ;
                }
                else
                {
                    //todo: check this logic
                    DataTable dbResults = GetAllAlegeusPlansForEmployer(mbiRow.EmployerId);

                    // planid is not always present e.g. in deposit file
                    string filter = $"employer_id = '{mbiRow.EmployerId}'";
                    if (!Utils.IsBlank(mbiRow.AccountTypeCode))
                    {
                        filter += $" and account_type_code = '{mbiRow.AccountTypeCode}' ";
                    }

                    if (!Utils.IsBlank(mbiRow.PlanId))
                    {
                        filter += $" and plan_id = '{mbiRow.PlanId}' ";
                    }

                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        errorMessage +=
                            $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                            (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                            $" could not be found for Employer Id {mbiRow.EmployerId}";
                        ;
                    }
                    else
                    {
                        // 2022-07-05 - we need an exact match for start and end dates - loop all plans
                        List<DataRow> matchedRows = new List<DataRow>();
                        //
                        foreach (var dbRow in dbRows)
                        {

                            DateTime dbRowPlanStartDate = (DateTime)dbRow["planstart"];
                            DateTime dbRowPlanEndDate = (DateTime)dbRow["planend"];

                            // exact start and end dates match
                            if (dbRowPlanStartDate == Utils.ToDate(mbiRow.PlanStartDate) &&
                                dbRowPlanEndDate == Utils.ToDate(mbiRow.PlanEndDate))
                            {
                                matchedRows.Add(dbRow);
                            }
                        }

                        // if no exact match for start and end dates, throw error
                        if (matchedRows.Count == 0)
                        {
                            errorMessage +=
                                                    $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                                                    (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                                                     $" and Plan Start date {mbiRow.PlanStartDate}" +
                                                     $" and Plan End date {mbiRow.PlanEndDate}" +
                                                    $" could not be found for Employer Id {mbiRow.EmployerId}";
                        }
                        else
                        {

                            // take first matched rows - should be only usually
                            var dbRow = matchedRows.First();
                            //
                            DateTime dbRowPlanStartDate = (DateTime)dbRow["planstart"];
                            DateTime dbRowPlanEndDate = (DateTime)dbRow["planend"];

                            //check if end date is after startdate
                            if (!Utils.IsBlank(mbiRow.PlanStartDate) && !Utils.IsBlank(mbiRow.PlanEndDate) &&
                                Utils.ToDate(mbiRow.PlanStartDate) > Utils.ToDate(mbiRow.PlanEndDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                                    $" Start Date {mbiRow.PlanStartDate} must be before the Plan End Date {mbiRow.PlanEndDate} for Employer Id {mbiRow.EmployerId}";
                            }



                            //check effectivedate is within plan dates
                            if (!Utils.IsBlank(mbiRow.EffectiveDate))
                            {
                                if (dbRowPlanStartDate > Utils.ToDate(mbiRow.EffectiveDate))
                                {
                                    errorMessage +=
                                        $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                                        (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                                        $" starts only on {Utils.ToDateString(dbRowPlanStartDate)} and is not yet started on {mbiRow.EffectiveDate}";
                                }
                            }

                            if (!Utils.IsBlank(mbiRow.EffectiveDate))
                            {
                                if (dbRowPlanEndDate < Utils.ToDate(mbiRow.EffectiveDate))
                                {
                                    errorMessage =
                                        $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                                        (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                                        $" ended on {Utils.ToDateString(dbRowPlanEndDate)} and is no longer active on {mbiRow.EffectiveDate}";
                                    ;
                                }
                            }
                        } // matchedRows count
                    } // filter matches
                }  // key field checks

                //
                _cache.Add(cacheKey, errorMessage);
            } // cache key exists

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployeePlanExists(mbi_file_table_stage mbiRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{mbiRow.EmployerId}-{mbiRow.EmployeeID}-{mbiRow.AccountTypeCode}-{mbiRow.PlanId}-{mbiRow.PlanStartDate}-{mbiRow.PlanEndDate}-{mbiRow.EffectiveDate}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(mbiRow.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(mbiRow.EmployeeID))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(mbiRow.AccountTypeCode))
                {
                    errorMessage += $"The AccountTypeCode cannot be blank";
                    ;
                }
                else
                {
                    //// if we are enrolling an employee in a plan, only check if ER has this EE
                    //if (fileFormat == EdiFileFormat.AlegeusEnrollment)
                    //{
                    //  //as it is an enrollment file, check the EE exists and enroll in the plan
                    //  var hasError = this.CheckEmployeeExists(mbiRow, column, fileFormat);
                    //  //return hasError;
                    //}

                    DataTable dbResults = GetAllAlegeusEmployeePlansForEmployer(mbiRow.EmployerId);

                    // planid is not always present e.g. in deposit file
                    string filter = $" employeeid = '{mbiRow.EmployeeID}' ";
                    if (!Utils.IsBlank(mbiRow.AccountTypeCode))
                    {
                        filter += $" and plancode = '{mbiRow.AccountTypeCode}' ";
                    }

                    if (!Utils.IsBlank(mbiRow.PlanId))
                    {
                        filter += $" and plandesc = '{mbiRow.PlanId}' ";
                    }

                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        if (fileFormat == EdiFileFormat.AlegeusEnrollment)
                        {
                            // as it is an enrollment, enroll the EE in this plan demographics file, 
                            DataRow newRow = dbResults.NewRow();
                            newRow["employerid"] = mbiRow.EmployerId;
                            newRow["employeeid"] = mbiRow.EmployeeID;
                            newRow["plancode"] = mbiRow.AccountTypeCode;
                            newRow["plandesc"] = mbiRow.PlanId;
                            newRow["planstart"] = Utils.ToDateTime(mbiRow.PlanStartDate);
                            newRow["planend"] = Utils.ToDateTime(mbiRow.PlanEndDate);

                            dbResults.Rows.Add(newRow);

                            var cacheKey2 =
                                $"GetAllEmployeePlansForEmployer-{this.PlatformType.ToDescription()}-{mbiRow.EmployerId}-AllEmployeePlans";
                            //
                            _cache.Add(cacheKey2, dbResults);

                            //
                            return false;
                        }

                        errorMessage +=
                            $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                            (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                            $" could not be found for Employee Id {mbiRow.EmployeeID}";
                        ;
                    }
                    else
                    {
                        // 2022-07-05 - we need an exact match for start and end dates - loop all plans
                        List<DataRow> matchedRows = new List<DataRow>();
                        //
                        foreach (var dbRow in dbRows)
                        {

                            DateTime dbRowPlanStartDate = (DateTime)dbRow["planstart"];
                            DateTime dbRowPlanEndDate = (DateTime)dbRow["planend"];
                            //DateTime? actualGracePeriodEndDate = Utils.ToDate(dbRow["actualGracePeriodEndDate"]?.ToString());

                            // exact start and end dates match
                            if (dbRowPlanStartDate == Utils.ToDate(mbiRow.PlanStartDate) &&
                                dbRowPlanEndDate == Utils.ToDate(mbiRow.PlanEndDate))
                            {
                                matchedRows.Add(dbRow);
                            }
                        }

                        // if no exact match for start and end dates, throw error
                        if (matchedRows.Count == 0)
                        {
                            errorMessage +=
                                                    $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                                                    (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                                                     $" and Plan Start date {mbiRow.PlanStartDate}" +
                                                     $" and Plan End date {mbiRow.PlanEndDate}" +
                                                    $" could not be found for Employee Id {mbiRow.EmployeeID}";
                        }
                        else
                        {
                            // take first matched rows - should be only usually
                            var dbRow = matchedRows.First();
                            //
                            DateTime dbRowPlanStartDate = (DateTime)dbRow["planstart"];
                            DateTime dbRowPlanEndDate = (DateTime)dbRow["planend"];

                            //check end date is after startdate
                            if (!Utils.IsBlank(mbiRow.PlanStartDate) && !Utils.IsBlank(mbiRow.PlanEndDate) &&
                                Utils.ToDate(mbiRow.PlanStartDate) > Utils.ToDate(mbiRow.PlanEndDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                                    $" Start Date {mbiRow.PlanStartDate} must be before the Plan End Date {mbiRow.PlanEndDate} for Employee Id {mbiRow.EmployeeID}";
                            }

                            //check effectivedate is within plan dates
                            if (!Utils.IsBlank(mbiRow.EffectiveDate) &&
                                dbRowPlanStartDate > Utils.ToDate(mbiRow.EffectiveDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                                    $" starts only on {Utils.ToDateString(dbRowPlanStartDate)} and is not yet started on {mbiRow.EffectiveDate} for Employee Id {mbiRow.EmployeeID}";
                            }

                            if (!Utils.IsBlank(mbiRow.EffectiveDate) &&
                                dbRowPlanEndDate < Utils.ToDate(mbiRow.EffectiveDate))
                            {
                                errorMessage =
                                    $"The AccountTypeID {mbiRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(mbiRow.PlanId) ? $" and Plan ID {mbiRow.PlanId}" : "") +
                                    $" ended on {Utils.ToDateString(dbRowPlanEndDate)} and is no longer active on {mbiRow.EffectiveDate} for Employee Id {mbiRow.EmployeeID}";
                                ;
                            }
                        } // matchedRows count
                    } // filter matches
                }  // key field checks

                //
                _cache.Add(cacheKey, errorMessage);
            } // check key exists

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusDependentExists(mbi_file_table_stage mbiRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            // dependent plans are linked to the employee
            return CheckAlegeusEmployeeExists(mbiRow, column, fileFormat);
        }

        public Boolean CheckAlegeusDependentPlanExists(mbi_file_table_stage mbiRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            // dependent plans are linked to the employee
            return CheckAlegeusEmployeePlanExists(mbiRow, column, fileFormat);
        }

        #endregion checkData

        #region cacheAlegeusData

        private DataTable GetAllAlegeusEmployers()
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
                if (PlatformType == PlatformType.Alegeus)
                {
                    string queryString =
                        $"select employer_id, employer_name, employer_status from wc.wc_employers " +
                        $" order by employer_id ;";
                    //
                    dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        queryString, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on EmployeeID

                    //DataColumn[] indices = new DataColumn[1];
                    //indices[0] = (DataColumn)dbResults.Columns["employerid"];
                    //dbResults.PrimaryKey = indices;

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
        private DataTable GetAllAlegeusEmployeesForEmployer(string employerId)
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
                if (PlatformType == PlatformType.Alegeus)
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

                    //DataColumn[] indices = new DataColumn[1];
                    //indices[0] = (DataColumn)dbResults.Columns["employeeid"];
                    //dbResults.PrimaryKey = indices;

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

        private DataTable GetAllAlegeusPlansForEmployer(string employerId)
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
                if (PlatformType == PlatformType.Alegeus)
                {
                    // todo: we need check exactly check against each plan - min/max are not correct
                    string queryString =
                            $"select employer_id, account_type_code, plan_id, date(plan_year_start_date) as planstart, date(plan_year_end_date) as planend" +
                            $" from wc.vw_wc_employer_plans_combined " +
                            $" where employer_id = '{Utils.DbQuote(employerId)}' " +
                            $" order by employer_id, plan_id, account_type_code "
                        ;
                    //
                    dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        queryString, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on EmployeeID

                    //DataColumn[] indices = new DataColumn[2];
                    //indices[0] = (DataColumn)dbResults.Columns["account_type_code"];
                    //indices[1] = (DataColumn)dbResults.Columns["plan_id"];
                    //dbResults.PrimaryKey = indices;

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

        private DataTable GetAllAlegeusEmployeePlansForEmployer(string employerId)
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
                if (PlatformType == PlatformType.Alegeus)
                {
                    // todo: we need check exactly check against each plan - min/max are not correct
                    // get ALL plans
                    string queryString1 =
                            $" select employerid, employeeid, plancode, plandesc, date(planstart) as planstart, date(planend) as planend " +
                            $" from wc.vw_wc_participant_plans_combined " +
                            $" where employerid = '{Utils.DbQuote(employerId)}' " +
                            $" order by employerid, employeeid, plancode, plandesc"
                        ;
                    //
                    DataTable dt1 = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        queryString1, null,
                        FileLogParams?.GetMessageLogParams());

                    // create index on EmployeeID

                    //DataColumn[] indices = new DataColumn[3];
                    //indices[0] = (DataColumn)dt1.Columns["employeeid"];
                    //indices[1] = (DataColumn)dt1.Columns["plancode"];
                    //indices[2] = (DataColumn)dt1.Columns["plandesc"];
                    //dt1.PrimaryKey = indices;

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



        public string EnsureValueIsOfFormatAndMatchesRules(mbi_file_table_stage mbiRow, TypedCsvColumn column,
            TypedCsvSchema mappings)
        {
            var orgValue = mbiRow.ColumnValue(column.SourceColumn) ?? "";
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
                            this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a valid Email. '{orgValue}' is not valid");
                        }

                        break;
                    case FormatType.Zip:
                        value = Utils.regexInteger.Replace(value, String.Empty);
                        if (!Utils.IsBlank(value) && value.Length > column.MaxLength)
                        {
                            value = value.Substring(0, column.MaxLength);
                        }

                        break;
                    case FormatType.Phone:
                        value = Utils.regexInteger.Replace(value, String.Empty);
                        if (!Utils.IsBlank(value) && value.Length > column.MaxLength)
                        {
                            value = Utils.Right(value, column.MaxLength);
                        }

                        break;
                    case FormatType.AlphaNumeric:
                        // replace all non alphanumeric
                        value = Utils.regexAlphaNumeric.Replace(value, String.Empty);
                        break;

                    case FormatType.AlphaNumericAndDashes:
                        // replace all non alphanumeric
                        value = Utils.regexAlphaNumericAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.AlphaOnly:
                        // replace all non alphanumeric
                        value = Utils.regexAlphaOnly.Replace(value, String.Empty);
                        break;

                    case FormatType.FixedConstant:
                        // default to fixed value always!
                        value = column.FixedValue;
                        break;

                    case FormatType.AlphaAndDashes:
                        // replace all non alphanumeric
                        value = Utils.regexAlphaAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.NumbersAndDashes:
                        // replace all non alphanumeric
                        value = Utils.regexNumericAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.Integer:
                        // remove any non digits
                        //todo: Sumeet: dont replace any invalid characters - just trim spaces and commas
                        //value = Utils.regexInteger.Replace(value, String.Empty);
                        value = value.Replace(",", "").Replace(" ", "");
                        //
                        if (!Utils.IsInteger(value))
                        {
                            this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                $"{column.SourceColumn} must be numbers only. '{orgValue}' is not valid");
                        }
                        else
                        {
                            // format as 0
                            var intValue = Utils.ToInt(value);
                            value = intValue.ToString("0");
                        }

                        break;

                    case FormatType.Double:
                        // remove any non digits and non . and non ,
                        //todo: Sumeet: dont replace any invalid characters - just trim spaces and commas
                        //value = Utils.regexDouble.Replace(value, String.Empty);
                        value = value.Replace(",", "").Replace(" ", "");

                        if (!Utils.IsDouble(value))
                        {
                            this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a Currency Value. '{orgValue}' is not valid");
                        }
                        else
                        {

                            // format as 0.00
                            var dblValue = Utils.ToDouble(value);
                            value = dblValue.ToString("0.00");
                        }
                        break;

                    case FormatType.IsoDate:
                        // remove any non digits
                        value = Utils.regexDate.Replace(value, String.Empty);
                        try
                        {
                            value = Utils.ToIsoDateString(Utils.ToDate(value));
                            if (!Utils.IsIsoDate(value, column.MaxLength > 0))
                            {
                                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.IsoDateTime:
                        // remove any non digits
                        value = Utils.regexDate.Replace(value, String.Empty);
                        try
                        {
                            value = Utils.ToDateTimeString(Utils.ToDateTime(value));

                            if (!Utils.IsIsoDateTime(value, column.MaxLength > 0))
                            {
                                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format YYYYMMDD HHMMSS. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD HHMMSS. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.YesNo:
                        if (!value.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("No", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.TrueFalse:
                        if (!value.Equals("True", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("False", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. '{orgValue}' is not valid");
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
            if ((column.SourceColumn == "EmployeeSocialSecurityNumber" || column.SourceColumn == "EmployeeID") || column.FormatType == FormatType.SSN)

            {
                if (!Utils.IsBlank(value))
                {
                    value = Utils.regexAlphaNumeric.Replace(value, String.Empty);
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
                    this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                        $"{column.SourceColumn} must always be one of or ezxactly {column.FixedValue}. '{orgValue}' is not valid");
                }
            }

            // set row column value to the fixed value if it has changed
            if (value != orgValue)
            {
                mbiRow.SetColumnValue(column.SourceColumn, value);
                mbiRow.data_row = GetDelimitedDataRow(mbiRow, mappings);
            }

            // 2. check against GENERAL rules

            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. '{orgValue}' is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                    $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. '{orgValue}' is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number. '{orgValue}' is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. '{orgValue}' is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddAlegeusErrorForRow(mbiRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. '{orgValue}' is not valid");
                }
            }

            return value;
        }

        private string GetDelimitedDataRow(mbi_file_table_stage mbiRow, TypedCsvSchema mappings)
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
                    case "mbi_file_name":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }

                string fieldValue = mbiRow.ColumnValue(column.SourceColumn);
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
