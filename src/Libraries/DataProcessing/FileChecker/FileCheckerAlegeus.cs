﻿using System;
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
            var dataRows = dbDataProcessing.mbi_file_table_stage
                .OrderBy(dataRow => dataRow.source_row_no)
                .ToList();

            //check each dataRow
            int rowNo = 0;
            foreach (var dataRow in dataRows)
            {
                rowNo++;
                this.CheckAlegeusRowData(fileFormat, dataRow, mappings);
            }

            // save any changes
            dbDataProcessing.SaveChanges();
        }

        private void CheckAlegeusRowData(EdiFileFormat fileFormat, mbi_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            // don't check header dataRow
            if (dataRow.row_type == "IA" || dataRow.row_type == "RA")
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
                        hasError = this.CheckAlegeusTpaExists(dataRow, column, fileFormat);
                        break;

                    // ER ID
                    case "employerid":
                        //ER must exist before any Import files are sent
                        hasError = this.CheckAlegeusEmployerExists(dataRow, column, fileFormat);
                        break;

                    // EE ID
                    case "employeeid":
                        //ER must exist before any Import files are sent. But for IB files, employee need not exist - he is being added
                        hasError = this.CheckAlegeusEmployeeExists(dataRow, column, fileFormat);

                        break;
                    // plan related
                    case "planid":
                    case @"accounttypecode":
                        if (fileFormat == EdiFileFormat.AlegeusEnrollment)
                        {
                            hasError = this.CheckAlegeusEmployerPlanExists(dataRow, column, fileFormat);
                        }
                        else if (fileFormat == EdiFileFormat.AlegeusEmployeeDeposit)
                        {
                            hasError = this.CheckAlegeusEmployeePlanExists(dataRow, column, fileFormat);
                        }

                        break;

                    default:
                        break;
                }
            }
            // check for duplicate posting of the row
            hasError = CheckForDuplicateAlegeusPosting(dataRow, fileFormat);
        }

        private void AddAlegeusErrorForRow(mbi_file_table_stage dataRow, string errCode, string errMessage,
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


        public Boolean CheckAlegeusTpaExists(mbi_file_table_stage dataRow, TypedCsvColumn column, EdiFileFormat fileFormat)
        {
            var errorMessage = "";
            var cacheKey = $"{MethodBase.GetCurrentMethod()?.Name}-{dataRow.TpaId}";
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
                        if (Utils.IsBlank(dataRow.TpaId))
                        {
                            errorMessage = "TPA ID cannot be blank. It must always be BENEFL";
                        }
                        else if (dataRow.TpaId != "BENEFL")
                        {
                            errorMessage = $"TPA ID {dataRow.TpaId} is invalid. It must always be BENEFL";
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
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployerExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(dataRow.EmployerId))
                {
                    errorMessage = $"The Employer ID cannot be blank";
                }
                else
                {
                    DataTable dbResults = GetAllAlegeusEmployers();
                    // planid is not always present e.g. in deposit file
                    string filter = $"employer_id = '{dataRow.EmployerId}'";
                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        errorMessage = $"The Employer ID {dataRow.EmployerId} could not be found";
                    }
                    else
                    {
                        //DataRow dbData = dbRows[0];

                        //note: FileChecker: verify if employer status need to be checked
                        //string status = dbData["employer_status"]?.ToString();
                        //if (status != "Active" && status != "New")
                        //{
                        //  errorMessage =
                        //    $"The Employer ID {dataRow.EmployerId} has status {status} which is not valid";
                        //}
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployeeExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
            EdiFileFormat fileFormat = EdiFileFormat.Unknown)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-{dataRow.EmployeeID}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(dataRow.EmployerId))
                {
                    AddAlegeusErrorForRow(dataRow, "EmployerId", $"The Employer ID cannot be blank");
                }
                else if (Utils.IsBlank(dataRow.EmployeeID))
                {
                    AddAlegeusErrorForRow(dataRow, "EmployeeId", $"The Employee ID cannot be blank");
                }
                else
                {
                    DataTable dbResults = GetAllAlegeusEmployeesForEmployer(dataRow.EmployerId);
                    DataRow[] dbRows =
                        dbResults.Select(
                            $"employerid = '{dataRow.EmployerId}' and employeeid = '{dataRow.EmployeeID}'");
                    if (dbRows.Length == 0)
                    {
                        // for demographics file, the employee will not yet exist or the status may be changing (activating or terminating) - do not check
                        if (fileFormat == EdiFileFormat.AlegeusDemographics)
                        {
                            // as it is an demographics file, add this employee to the ER-EE table so a check for plan enrollemnt within same run or before reaggregation from Alegeus will suceed
                            DataRow newRow = dbResults.NewRow();
                            newRow["employerid"] = dataRow.EmployerId;
                            newRow["employeeid"] = dataRow.EmployeeID;
                            newRow["is_active"] = dataRow.EmployeeStatus == "2" ? 1 : 0;
                            dbResults.Rows.Add(newRow);

                            var cacheKey2 =
                                $"GetAllEmployeesForEmployer-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-AllEmployees";
                            _cache.Add(cacheKey2, dbResults);

                            //
                            return false;
                        }
                        else
                        {
                            errorMessage +=
                                $"The Employee ID {dataRow.EmployeeID} could not be found for Employer Id {dataRow.EmployerId}";
                        }
                    }
                    else
                    {
                        DataRow dbData = dbRows[0];
                        // if employee exists as per our data, that is fine
                        // do not check the file EmployeeStatus against what we havwe in the db
                        //float status = Utils.ToNumber(dbData["is_active"]?.ToString());
                        //if (status <= 0 && Utils.ToNumber(dataRow.EmployeeStatus) > 1)
                        //{
                        //  errorMessage +=
                        //    $"The Employee ID {dataRow.EmployeeID} has status {status} which is not valid";
                        //}
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }
        public Boolean CheckForDuplicateAlegeusPosting(mbi_file_table_stage dataRow,
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
                        $" TpaId='{dataRow.TpaId}'" +
                        $" and EmployerId='{dataRow.EmployerId}'" +
                        $" and EmployeeID='{dataRow.EmployeeID}'" +
                        $" and AccountTypeCode='{dataRow.AccountTypeCode}'" +
                        $" and PlanEndDate='{dataRow.PlanEndDate}'" +
                        $" and PlanStartDate='{dataRow.PlanStartDate}'" +
                        $" and EffectiveDate='{dataRow.EffectiveDate}'" +
                        $" and DepositType='{dataRow.DepositType}'" +
                        // todo: flag only if 
                        // check amounts also - deposit type ER / EE
                        // EmployeeDepositAmount ?? ??
                        // EmployerDepositAmount ?? ??
                        //$" and DepositType='{dataRow.DepositType}'" +
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
                this.AddAlegeusErrorForRow(dataRow, "DuplicatePosting", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployerPlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-{dataRow.AccountTypeCode}-{dataRow.PlanId}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(dataRow.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(dataRow.AccountTypeCode))
                {
                    errorMessage += $"The AccountTypeCode cannot be blank";
                    ;
                }
                else
                {
                    DataTable dbResults = GetAllAlegeusPlansForEmployer(dataRow.EmployerId);

                    // planid is not always present e.g. in deposit file
                    string filter = $"employer_id = '{dataRow.EmployerId}'";
                    if (!Utils.IsBlank(dataRow.AccountTypeCode))
                    {
                        filter += $" and account_type_code = '{dataRow.AccountTypeCode}' ";
                    }

                    if (!Utils.IsBlank(dataRow.PlanId))
                    {
                        filter += $" and plan_id = '{dataRow.PlanId}' ";
                    }

                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        errorMessage +=
                            $"The AccountTypeID {dataRow.AccountTypeCode}" +
                            (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                            $" could not be found for Employer Id {dataRow.EmployerId}";
                        ;
                    }
                    else
                    {
                        // 2022-07-05 - we need an exact match for start and end dates - loop all plans
                        List<DataRow> matchedRows = new List<DataRow>();
                        //
                        foreach (var dbData in dbRows)
                        {

                            DateTime actualPlanStartDate = (DateTime)dbData["planstart"];
                            DateTime actualPlanEndDate = (DateTime)dbData["planend"];

                            // exact start and end dates match
                            if (Utils.ToDate(dataRow.PlanStartDate) == Utils.ToDate(dataRow.PlanStartDate) &&
                                Utils.ToDate(dataRow.PlanEndDate) == Utils.ToDate(dataRow.PlanEndDate))
                            {
                                matchedRows.Add(dbData);
                            }
                        }

                        // if no exact match for start and end dates, throw error
                        if (matchedRows.Count == 0)
                        {
                            errorMessage +=
                                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                                     $" and Plan Start date {dataRow.PlanStartDate}" +
                                                     $" and Plan End date {dataRow.PlanEndDate}" +
                                                    $" could not be found for Employer Id {dataRow.EmployerId}";
                        }
                        else
                        {

                            // take first matched rows - should be only usually
                            var dbData = matchedRows.First();
                            //
                            DateTime actualPlanStartDate = (DateTime)dbData["planstart"];
                            DateTime actualPlanEndDate = (DateTime)dbData["planend"];

                            //check if end date is after startdate
                            if (!Utils.IsBlank(dataRow.PlanStartDate) && !Utils.IsBlank(dataRow.PlanEndDate) &&
                                Utils.ToDate(dataRow.PlanStartDate) > Utils.ToDate(dataRow.PlanEndDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                    $" Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate} for Employer Id {dataRow.EmployerId}";
                            }



                            //check effectivedate is within plan dates
                            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
                                actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate}";
                            }

                            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
                                actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
                            {
                                errorMessage =
                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.EffectiveDate}";
                                ;
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
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployeePlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-{dataRow.EmployeeID}-{dataRow.AccountTypeCode}-{dataRow.PlanId}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(dataRow.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(dataRow.EmployeeID))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(dataRow.AccountTypeCode))
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
                    //  var hasError = this.CheckEmployeeExists(dataRow, column, fileFormat);
                    //  //return hasError;
                    //}

                    DataTable dbResults = GetAllAlegeusEmployeePlansForEmployer(dataRow.EmployerId);

                    // planid is not always present e.g. in deposit file
                    string filter = $" employeeid = '{dataRow.EmployeeID}' ";
                    if (!Utils.IsBlank(dataRow.AccountTypeCode))
                    {
                        filter += $" and plancode = '{dataRow.AccountTypeCode}' ";
                    }

                    if (!Utils.IsBlank(dataRow.PlanId))
                    {
                        filter += $" and plandesc = '{dataRow.PlanId}' ";
                    }

                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        if (fileFormat == EdiFileFormat.AlegeusEnrollment)
                        {
                            // as it is an enrollment, enroll the EE in this plan demographics file, 
                            DataRow newRow = dbResults.NewRow();
                            newRow["employerid"] = dataRow.EmployerId;
                            newRow["employeeid"] = dataRow.EmployeeID;
                            newRow["plancode"] = dataRow.AccountTypeCode;
                            newRow["plandesc"] = dataRow.PlanId;
                            newRow["planstart"] = Utils.ToDateTime(dataRow.PlanStartDate);
                            newRow["planend"] = Utils.ToDateTime(dataRow.PlanEndDate);

                            dbResults.Rows.Add(newRow);

                            var cacheKey2 =
                                $"GetAllEmployeePlansForEmployer-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-AllEmployeePlans";
                            //
                            _cache.Add(cacheKey2, dbResults);

                            //
                            return false;
                        }

                        errorMessage +=
                            $"The AccountTypeID {dataRow.AccountTypeCode}" +
                            (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                            $" could not be found for Employee Id {dataRow.EmployeeID}";
                        ;
                    }
                    else
                    {
                        // 2022-07-05 - we need an exact match for start and end dates - loop all plans
                        List<DataRow> matchedRows = new List<DataRow>();
                        //
                        foreach (var dbData in dbRows)
                        {

                            DateTime actualPlanStartDate = (DateTime)dbData["planstart"];
                            DateTime actualPlanEndDate = (DateTime)dbData["planend"];
                            //DateTime? actualGracePeriodEndDate = Utils.ToDate(dbData["actualGracePeriodEndDate"]?.ToString());

                            // exact start and end dates match
                            if (Utils.ToDate(dataRow.PlanStartDate) == Utils.ToDate(dataRow.PlanStartDate) &&
                                Utils.ToDate(dataRow.PlanEndDate) == Utils.ToDate(dataRow.PlanEndDate))
                            {
                                matchedRows.Add(dbData);
                            }
                        }

                        // if no exact match for start and end dates, throw error
                        if (matchedRows.Count == 0)
                        {
                            errorMessage +=
                                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                                     $" and Plan Start date {dataRow.PlanStartDate}" +
                                                     $" and Plan End date {dataRow.PlanEndDate}" +
                                                    $" could not be found for Employee Id {dataRow.EmployeeID}";
                        }
                        else
                        {
                            // take first matched rows - should be only usually
                            var dbData = matchedRows.First();
                            //
                            DateTime actualPlanStartDate = (DateTime)dbData["planstart"];
                            DateTime actualPlanEndDate = (DateTime)dbData["planend"];

                            //check end date is after startdate
                            if (!Utils.IsBlank(dataRow.PlanStartDate) && !Utils.IsBlank(dataRow.PlanEndDate) &&
                                Utils.ToDate(dataRow.PlanStartDate) > Utils.ToDate(dataRow.PlanEndDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                    $" Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate} for Employee Id {dataRow.EmployeeID}";
                            }

                            // not needed - we match exact start and end dates
                            /*if (!Utils.IsBlank(dataRow.PlanStartDate) &&
                                actualPlanStartDate > Utils.ToDate(dataRow.PlanStartDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate} for Employee Id {dataRow.EmployeeID}";
                            }

                            if (!Utils.IsBlank(dataRow.PlanEndDate) &&
                                actualPlanEndDate < Utils.ToDate(dataRow.PlanEndDate)
                                && dataRow.PlanEndDate != "20991231")
                            {
                                errorMessage =
                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.PlanEndDate} for Employee Id {dataRow.EmployeeID}";
                                ;
                            }*/

                            //check effectivedate is within plan dates
                            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
                                actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate} for Employee Id {dataRow.EmployeeID}";
                            }

                            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
                                actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
                            {
                                errorMessage =
                                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
                                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
                                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.EffectiveDate} for Employee Id {dataRow.EmployeeID}";
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
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusDependentExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            // dependent plans are linked to the employee
            return CheckAlegeusEmployeeExists(dataRow, column, fileFormat);
        }

        public Boolean CheckAlegeusDependentPlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            // dependent plans are linked to the employee
            return CheckAlegeusEmployeePlanExists(dataRow, column, fileFormat);
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



        public string EnsureValueIsOfFormatAndMatchesRules(mbi_file_table_stage dataRow, TypedCsvColumn column,
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
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
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
                        value = Utils.regexInteger.Replace(value, String.Empty);
                        //
                        if (!Utils.IsInteger(value))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be numbers only. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.Double:
                        // remove any non digits and non . and non ,
                        value = Utils.regexDouble.Replace(value, String.Empty);
                        if (!Utils.IsDouble(value))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a Currency Value. '{orgValue}' is not valid");
                        }

                        // format as 0.00
                        var dblValue = Utils.ToDouble(value);
                        value = dblValue.ToString("0.00");

                        break;

                    case FormatType.IsoDate:
                        // remove any non digits
                        value = Utils.regexDate.Replace(value, String.Empty);
                        value = Utils.ToIsoDateString(Utils.ToDate(value));
                        if (!Utils.IsIsoDate(value, column.MaxLength > 0))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.IsoDateTime:
                        // remove any non digits
                        value = Utils.regexDate.Replace(value, String.Empty);
                        value = Utils.ToDateTimeString(Utils.ToDateTime(value));

                        if (!Utils.IsIsoDateTime(value, column.MaxLength > 0))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.YesNo:
                        if (!value.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("No", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.TrueFalse:
                        if (!value.Equals("True", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("False", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
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
            if ((column.SourceColumn == "EmployeeSocialSecurityNumber" || column.SourceColumn == "EmployeeID"))

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
                    this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
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
            
            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. '{orgValue}' is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. '{orgValue}' is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number. '{orgValue}' is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. '{orgValue}' is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. '{orgValue}' is not valid");
                }
            }

            return value;
        }

        private string GetDelimitedDataRow(mbi_file_table_stage dataRow, TypedCsvSchema mappings)
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
