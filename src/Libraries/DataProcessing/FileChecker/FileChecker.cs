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
using DataProcessing;

// ReSharper disable All

// ReSharper disable once CheckNamespace
namespace DataProcessing
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class FileChecker : IDisposable
    {
        //
        public static readonly string ErrorSeparator = ";";

        //
        private static ExtendedCache _cache = new ExtendedCache(TimeSpan.FromHours(1), TimeSpan.FromHours(5), null);

        //
        private readonly DbConnection dbConnPortalWc;

        //
        public readonly FileCheckResults fileCheckResults = new FileCheckResults();
        public Boolean hasHeaderRow = false;
        public HeaderType headerType = HeaderType.NotApplicable;
        private Vars Vars { get; } = new Vars();


        public FileChecker(string _srcFilePath, PlatformType _platformType, DbConnection _dbConn,
            FileOperationLogParams _fileLogParams, OnErrorCallback _onErrorCallback) : base()
        {
            this.SrcFilePath = _srcFilePath;
            this.PlatformType = _platformType;
            this.FileLogParams = _fileLogParams;
            this.DbConn = _dbConn;
            this.dbConnPortalWc = Vars.dbConnPortalWc;
            this.OnErrorCallback = _onErrorCallback;
        }

        public string SrcFilePath { get; set; }
        public PlatformType PlatformType { get; set; }
        public EdiFileFormat EdiFileFormat { get; set; }
        public FileOperationLogParams FileLogParams { get; set; }

        public DbConnection DbConn { get; set; }

        //
        public OnErrorCallback OnErrorCallback { get; }

        public void Dispose()
        {
            //
        }

        #region CheckFile


        public OperationResult CheckFileAndProcess(FileCheckType fileCheckType,
            FileCheckProcessType fileCheckProcessType)
        {
            // check file
            OperationResultType resultType = CheckFile(fileCheckType);

            // move file
            var fileName = Path.GetFileName(this.SrcFilePath);
            var newFilePath = this.SrcFilePath;
            var newErrorFilePath = "";

            // act on resultType
            switch (resultType)
            {

                ///////////////////////////////////////
                case OperationResultType.Ok:
                    ///////////////////////////////////////
                    if (fileCheckProcessType == FileCheckProcessType.MoveToDestDirectories)
                    {
                        if (Utils.IsTestFile(this.SrcFilePath))
                        {
                            newFilePath = $"{Vars.alegeusFilesPreCheckTestRoot}/{fileName}";
                        }
                        else
                        {
                            newFilePath = $"{Vars.alegeusFilesPreCheckOKRoot}/{fileName}";
                        }

                        FileUtils.MoveFile(SrcFilePath, newFilePath, (srcFilePath2, destFilePath2, dummy2) =>
                            {
                                // add to fileLog
                                FileLogParams.SetFileNames("", fileName, SrcFilePath,
                                    Path.GetFileName(newFilePath), newFilePath,
                                    $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                    "Success", "PreCheck OK. Moved File to PreCheck OK Directory");
                                //
                                DbUtils.LogFileOperation(FileLogParams);
                            },
                            (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, FileLogParams); }
                        );
                    }
                    else if (fileCheckProcessType == FileCheckProcessType.ReturnResults)
                    {

                    }

                    // OK result
                    return new OperationResult(1, "200", "Completed", "", "");

                ///////////////////////////////////////
                case OperationResultType.CompleteFail:
                case OperationResultType.ProcessingError:
                case OperationResultType.PartialFail:
                default:
                    ///////////////////////////////////////

                    string srcFileName = Path.GetFileName(this.SrcFilePath);
                    //

                    if (fileCheckProcessType == FileCheckProcessType.MoveToDestDirectories)
                    {
                        if (Utils.IsTestFile(this.SrcFilePath))
                        {
                            newFilePath = $"{Vars.alegeusFilesPreCheckTestRoot}/{fileName}";
                        }
                        else
                        {
                            newFilePath = $"{Vars.alegeusFilesPreCheckFailRoot}/{fileName}";
                        }

                        FileUtils.MoveFile(SrcFilePath, newFilePath, (srcFilePath2, destFilePath2, dummy2) =>
                            {
                                // add to fileLog
                                FileLogParams.SetFileNames("", fileName, SrcFilePath,
                                    Path.GetFileName(newFilePath), newFilePath,
                                    $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                    "Fail", "PreCheck FAIL. Moved File to PreCheck FAIL Directory");
                                //
                                DbUtils.LogFileOperation(FileLogParams);
                            },
                            (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, FileLogParams); }
                        );

                        newErrorFilePath = $"{newFilePath}.err";
                    }
                    else if (fileCheckProcessType == FileCheckProcessType.ReturnResults)
                    {
                        newErrorFilePath = $"{SrcFilePath}.err";
                    }

                    // export error file
                    var outputTableName = "[dbo].[mbi_file_table]";
                    //
                    var queryStringExp =
                        $" select concat(data_row, ',', case when len(error_message) > 0 then concat( 'PreCheck Errors: ' , error_message ) else 'PreCheck: OK' end ) as file_row" +
                        $" from {outputTableName} " +
                        $" where mbi_file_name = '{srcFileName}'" +
                        $" order by mbi_file_table.source_row_no; ";

                    ImpExpUtils.ExportSingleColumnFlatFile(newErrorFilePath, DbConn, queryStringExp,
                        "file_row", null, FileLogParams,
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, FileLogParams); }
                    );

                    //
                    string strCheckResults = File.ReadAllText(newErrorFilePath);

                    // OK result
                    return new OperationResult(0, "300", "Completed", "", strCheckResults);
            }

        }
        //

        private OperationResultType CheckFile(FileCheckType fileCheckType)
        {
            //
            Dictionary<EdiFileFormat, List<int>> fileFormats =
                ImpExpUtils.GetAlegeusFileFormats(this.SrcFilePath, false, this.FileLogParams);

            CheckFile(fileFormats);

            var result = this.fileCheckResults.OperationResultType;

            return result;
        }

        private void CheckFile(Dictionary<EdiFileFormat, List<int>> fileFormats)
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
                CheckFile(fileFormat3, (string)files[fileFormat3][1]);
            }
        }

        private void CheckFile(EdiFileFormat fileFormat, string currentFilePath)
        {
            // check mappings and type of file (Import or Result)
            Boolean isResultFile = Import.GetAlegeusFileFormatIsResultFile(fileFormat);
            if (isResultFile)
            {
                return;
            }

            // get header type from filename
            var headerType = Import.GetAlegeusHeaderTypeFromFile(currentFilePath);

            // get columns for file based on header type
            TypedCsvSchema mappings = Import.GetAlegeusFileImportMappings(fileFormat, headerType);

            //
            string tableName = "[dbo].[mbi_file_table_stage]";

            // truncate staging table
            DbUtils.TruncateTable(DbConn, tableName,
                FileLogParams?.GetMessageLogParams());


            // import the file with bulk copy
            var newPath =
                Import.PrefixLineWithEntireLineAndFileName(currentFilePath, this.SrcFilePath, FileLogParams);

            // import into table so we can manipulate the file
            ImpExpUtils.ImportCsvFileBulkCopy(this.DbConn, newPath, this.hasHeaderRow, tableName,
                mappings, this.FileLogParams,
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, FileLogParams); }
            );

            // update check type for table
            string queryString1 = $" update {tableName} set " +
                                  $" /* set check type */check_type = 'PreCheck', " +
                                  $" /* remove extra csv commas added to line */ data_row = replace(data_row, ',,,,,,,,,,,,,,,,,,,,', '') " +
                                  $" where 1 = 1;";
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, DbConn, queryString1, null,
                FileLogParams?.GetMessageLogParams()
            );

            // check file data
            CheckFileData(fileFormat, mappings);

            // run post import proc to take data from stage into final table
            string postImportProc = "[dbo].[process_mbi_file_table_stage_import]";
            string queryString = $"exec {postImportProc};";
            //
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, DbConn, queryString, null,
                FileLogParams?.GetMessageLogParams()
            );
        }

        private void CheckFileData(EdiFileFormat fileFormat, TypedCsvSchema mappings)
        {
            // ensure previously cached data is not used so
            // so create a new db context to ensure stale data will NOT be used
            var dbErrorLog = Vars.dbCtxDataProcessingNew;

            // get all dbRows without caching
            var dataRows = dbErrorLog.mbi_file_table_stage
                .OrderBy(dataRow => dataRow.source_row_no)
                .ToList();

            //check each dataRow
            int rowNo = 0;
            foreach (var dataRow in dataRows)
            {
                rowNo++;
                this.CheckFileData(fileFormat, dataRow, mappings);
            }

            // save any changes
            dbErrorLog.SaveChanges();
        }


        private void CheckFileData(EdiFileFormat fileFormat, mbi_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            // don't check header dataRow
            if (dataRow.row_type == "IA" || dataRow.row_type == "RA")
            {
                return;
            }

            Boolean hasError;

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
                var formattedValue = EnsureValueIsOfFormatAndMatchesRules(dataRow, column);

                // 2. specific column checking against business rules & DB
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    // ER ID
                    case "tpaid":
                        hasError = this.CheckTpaExists(dataRow, column, fileFormat);
                        break;

                    // ER ID
                    case "employerid":
                        //ER must exist before any Import files are sent
                        hasError = this.CheckEmployerExists(dataRow, column, fileFormat);
                        break;

                    // EE ID
                    case "employeeid":
                        //ER must exist before any Import files are sent. But for IB files, employee need not exist - he is being added
                        hasError = this.CheckEmployeeExists(dataRow, column, fileFormat);

                        break;
                    // plan related
                    case "planid":
                    case @"accounttypecode":
                        if (fileFormat == EdiFileFormat.AlegeusEnrollment)
                        {
                            hasError = this.CheckEmployerPlanExists(dataRow, column, fileFormat);
                        }
                        else if (fileFormat == EdiFileFormat.AlegeusEmployeeDeposit)
                        {
                            hasError = this.CheckEmployeePlanExists(dataRow, column, fileFormat);
                        }

                        break;

                    default:
                        break;
                }

            }
        }

        private void AddErrorForRow(mbi_file_table_stage dataRow, string errCode, string errMessage,
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

        #endregion CheckFile


        #region checkData

        public Boolean CheckTpaExists(mbi_file_table_stage dataRow, TypedCsvColumn column, EdiFileFormat fileFormat)
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
                    if (Utils.IsBlank(dataRow.TpaId))
                    {
                        errorMessage = "TPA ID cannot be blank. It must always be BENEFL";
                    }
                    else if (dataRow.TpaId != "BENEFL")
                    {
                        errorMessage = $"TPA ID {dataRow.TpaId} is invalid. It must always be BENEFL";
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
                this.AddErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckEmployerExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
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
                    DataTable dbResults = GetAllEmployers();
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
                        //    errorMessage =
                        //        $"The Employer ID {dataRow.EmployerId} has status {status} which is not valid";
                        //}
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckEmployeeExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
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
                    throw new Exception($"The Employer ID cannot be blank");
                }
                else if (Utils.IsBlank(dataRow.EmployeeID))
                {
                    throw new Exception($"The Employee ID cannot be blank");
                }
                else
                {
                    DataTable dbResults = GetAllEmployeesForEmployer(dataRow.EmployerId);
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

                        errorMessage +=
                            $"The Employee ID {dataRow.EmployeeID} could not be found for Employer Id {dataRow.EmployerId}";
                        ;
                    }
                    else
                    {
                        DataRow dbData = dbRows[0];
                        float status = Utils.ToNumber(dbData["is_active"]?.ToString());
                        if (status <= 0 && Utils.ToNumber(dataRow.EmployeeStatus) > 1)
                        {
                            errorMessage +=
                                $"The Employee ID {dataRow.EmployeeID} has status {status} which is not valid";
                        }
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckEmployerPlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
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
                    DataTable dbResults = GetAllPlansForEmployer(dataRow.EmployerId);

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
                            $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} could not be found for Employer Id {dataRow.EmployerId}";
                        ;
                    }
                    else
                    {
                        DataRow dbData = dbRows[0];
                        DateTime actualPlanStartDate = (DateTime)dbData["plan_year_start_date"];
                        DateTime actualPlanEndDate = (DateTime)dbData["plan_year_end_date"];
                        //DateTime actualGracePeriodEndDate = (DateTime)dbData["grace_period_end_date"];

                        //check start and end dates 
                        if (!Utils.IsBlank(dataRow.PlanStartDate) && !Utils.IsBlank(dataRow.PlanEndDate) &&
                            Utils.ToDate(dataRow.PlanStartDate) > Utils.ToDate(dataRow.PlanEndDate))
                        {
                            errorMessage +=
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate}";
                        }

                        //check plan dates match Alegeus
                        if (!Utils.IsBlank(dataRow.PlanStartDate) &&
                            actualPlanStartDate > Utils.ToDate(dataRow.PlanStartDate))
                        {
                            errorMessage +=
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate}";
                        }

                        if (!Utils.IsBlank(dataRow.PlanEndDate) &&
                            actualPlanEndDate < Utils.ToDate(dataRow.PlanEndDate))
                        {
                            errorMessage =
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)} and is no linger active on {dataRow.PlanStartDate}";
                            ;
                        }

                        //check effectivedate is within plan dates
                        if (!Utils.IsBlank(dataRow.EffectiveDate) &&
                            actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
                        {
                            errorMessage +=
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate}";
                        }

                        if (!Utils.IsBlank(dataRow.EffectiveDate) &&
                            actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
                        {
                            errorMessage =
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on  {dataRow.EffectiveDate}";
                            ;
                        }

                    }

                    //
                    _cache.Add(cacheKey, errorMessage);
                }
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }

        }

        public Boolean CheckEmployeePlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
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
                    //    //as it is an enrollment file, check the EE exists and enroll in the plan
                    //    var hasError = this.CheckEmployeeExists(dataRow, column, fileFormat);
                    //    //return hasError;
                    //}

                    DataTable dbResults = GetAllEmployeePlansForEmployer(dataRow.EmployerId);

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
                            _cache.Add(cacheKey2, dbResults);

                            //
                            return false;
                        }

                        errorMessage +=
                            $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} could not be found for Employer Id {dataRow.EmployerId} and Employee Id {dataRow.EmployeeID}";
                        ;
                    }
                    else
                    {
                        DataRow dbData = dbRows[0];

                        // for demographics file, the employee will not yet exist or the status may be changing (activating or terminating) - do not check

                        DateTime actualPlanStartDate = (DateTime)dbData["planstart"];
                        DateTime actualPlanEndDate = (DateTime)dbData["planend"];
                        //DateTime? actualGracePeriodEndDate = Utils.ToDate(dbData["actualGracePeriodEndDate"]?.ToString());

                        //todo: we need to get all alegeus plans going back many years
                        //check start and end dates 
                        if (!Utils.IsBlank(dataRow.PlanStartDate) && !Utils.IsBlank(dataRow.PlanEndDate) &&
                            Utils.ToDate(dataRow.PlanStartDate) > Utils.ToDate(dataRow.PlanEndDate))
                        {
                            errorMessage +=
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate}";
                        }

                        //check plan dates match Alegeus
                        if (!Utils.IsBlank(dataRow.PlanStartDate) &&
                            actualPlanStartDate > Utils.ToDate(dataRow.PlanStartDate))
                        {
                            errorMessage +=
                                $"The AccountTypeID {dataRow.AccountTypeCode} and PlanID {dataRow.PlanId} and Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate}";
                        }

                        if (!Utils.IsBlank(dataRow.PlanEndDate) &&
                            actualPlanEndDate < Utils.ToDate(dataRow.PlanEndDate))
                        {
                            errorMessage =
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)} and is no linger active on {dataRow.PlanStartDate}";
                            ;
                        }

                        //check effectivedate is within plan dates
                        if (!Utils.IsBlank(dataRow.EffectiveDate) &&
                            actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
                        {
                            errorMessage +=
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate}";
                        }

                        if (!Utils.IsBlank(dataRow.EffectiveDate) &&
                            actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
                        {
                            errorMessage =
                                $"The AccountTypeID {dataRow.AccountTypeCode} and Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on  {dataRow.EffectiveDate}";
                            ;
                        }

                    }

                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckDependentExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            // dependent plans are linked to the employee
            return CheckEmployeeExists(dataRow, column, fileFormat);

        }

        public Boolean CheckDependentPlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column,
            EdiFileFormat fileFormat)
        {
            // dependent plans are linked to the employee
            return CheckEmployeePlanExists(dataRow, column, fileFormat);
        }

        #endregion checkData

        #region cacheEmployerData

        private DataTable GetAllEmployers()
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
        private DataTable GetAllEmployeesForEmployer(string employerId)
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
                        $" FROM wc.wc_participants  " +
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

        private DataTable GetAllPlansForEmployer(string employerId)
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
                    string queryString =
                            $"select employer_id, account_type_code, plan_id, min(plan_year_start_date) as plan_year_start_date, max(plan_year_end_date) as plan_year_end_date, max(grace_period_end_date) grace_period_end_date " +
                            $" from wc.wc_employer_plans " +
                            $" where employer_id = '{Utils.DbQuote(employerId)}' " +
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

        private DataTable GetAllEmployeePlansForEmployer(string employerId)
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
                    // get ALL plans
                    string queryString1 =
                            $" select employerid, employeeid, plancode, plandesc, min(planstart) as planstart, max(planend) as planend " +
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
        private static readonly Regex regexInteger = new Regex("[^0-9]");
        private static readonly Regex regexDate = new Regex(@"[^a-zA-Z0-9\s:\-\//]");
        private static readonly Regex regexAlphaNumeric = new Regex(@"[^a-zA-Z0-9\s]");
        private static readonly Regex regexAlphaOnly = new Regex(@"[^a-zA-Z]");
        private static readonly Regex regexAlphaAndDashes = new Regex(@"[^a-zA-Z\-]");
        private static readonly Regex regexNumericAndDashes = new Regex(@"[^0-9\-]");
        private static readonly Regex regexDouble = new Regex("[^0-9.]");

        public string EnsureValueIsOfFormatAndMatchesRules(mbi_file_table_stage dataRow, TypedCsvColumn column)
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
                            this.AddErrorForRow(dataRow, column.SourceColumn,
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
                            this.AddErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be numbers only. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.Double:
                        // remove any non digits and non . and non ,
                        value = regexDouble.Replace(value, String.Empty);
                        if (!Utils.IsDouble(value))
                        {
                            this.AddErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a Currency Value. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.IsoDate:
                        // remove any non digits
                        value = regexDate.Replace(value, String.Empty);
                        value = Utils.ToIsoDateString(Utils.ToDate(value));
                        if (!Utils.IsIsoDate(value, column.MaxLength > 0))
                        {
                            this.AddErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. {orgValue} is not valid");
                        }

                        break;


                    case FormatType.IsoDateTime:
                        // remove any non digits
                        value = regexDate.Replace(value, String.Empty);
                        value = Utils.ToDateTimeString(Utils.ToDateTime(value));

                        if (!Utils.IsIsoDateTime(value, column.MaxLength > 0))
                        {
                            this.AddErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. {orgValue} is not valid");
                        }

                        break;


                    case FormatType.YesNo:
                        if (!value.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("No", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.TrueFalse:
                        if (!value.Equals("True", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("False", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. {orgValue} is not valid");
                        }

                        break;


                    default:
                        break;
                }
            }

            // pad ssn to 9 digits with leading zeros
            if (column.SourceColumn == "EmployeeSocialSecurityNumber" && !Utils.IsBlank(value) && value.Length < column.MinLength)
            {
                value = value.PadLeft(column.MinLength, '0');
            }

            // set row column value to the fixed value if it has changed
            if (value != orgValue)
            {
                dataRow.SetColumnValue(column.SourceColumn, value);
            }


            // 2. check against GENERAL rules
            if (column.FixedValue != null && value != column.FixedValue && !column.FixedValue.Split('|').Contains(value) && column.MinLength > 0)
            {
                this.AddErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must always be {column.FixedValue}. {orgValue} is not valid");
            }


            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. {orgValue} is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddErrorForRow(dataRow, column.SourceColumn,
                $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. {orgValue} is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddErrorForRow(dataRow, column.SourceColumn, $"{column.SourceColumn} must be a number. {orgValue} is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. {orgValue} is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. {orgValue} is not valid");
                }
            }

            return value;

        }
        #endregion


    }
}