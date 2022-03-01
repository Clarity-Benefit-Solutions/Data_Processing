using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing.DataModels.AlegeusErrorLog;
using EtlUtilities;
using MySqlConnector;
using System.Runtime.Caching;

// ReSharper disable All

// ReSharper disable once CheckNamespace
namespace DataProcessing
{

    public class FileCheckResults : Dictionary<int, string>
    {
        internal Boolean markAsCompleteFail = false;

        public FileCheckResults() : base()
        {
        }

        public Boolean HasErrors
        {
            get { return this.Count > 0; }
        }

        public Boolean IsCompleteFail
        {
            get { return this.markAsCompleteFail; }
        }
    }

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
            this.srcFilePath = _srcFilePath;
            this.PlatformType = _platformType;
            this.fileLogParams = _fileLogParams;
            this.dbConn = _dbConn;
            this.dbConnPortalWc = Vars.dbConnPortalWc;
            this.OnErrorCallback = _onErrorCallback;
        }

        public string srcFilePath { get; set; }
        public PlatformType PlatformType { get; set; }
        public EdiFileFormat EdiFileFormat { get; set; }
        public FileOperationLogParams fileLogParams { get; set; }

        public DbConnection dbConn { get; set; }

        //
        public OnErrorCallback OnErrorCallback { get; }

        public void Dispose()
        {
            //
        }

        public OperationResult CheckFileAndMove(FileCheckType fileCheckType)
        {
            // check file
            OperationResult result = CheckFile(fileCheckType);

            // move file
            var fileName = Path.GetFileName(this.srcFilePath);
            var newFilePath = this.srcFilePath;
            var newErrorFilePath = "";

            // act on result
            switch (result)
            {

                ///////////////////////////////////////
                case OperationResult.Ok:
                    ///////////////////////////////////////
                    newFilePath = $"{Vars.alegeusFilesPreCheckOKRoot}/{fileName}";
                    FileUtils.MoveFile(srcFilePath, newFilePath, (srcFilePath2, destFilePath2, dummy2) =>
                    {
                        // add to fileLog
                        fileLogParams.SetFileNames("", fileName, srcFilePath,
                                Path.GetFileName(newFilePath), newFilePath,
                                $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", "PreCheck OK. Moved File to PreCheck OK Directory");
                        //
                        DbUtils.LogFileOperation(fileLogParams);
                    },
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );
                    break;

                ///////////////////////////////////////
                case OperationResult.CompleteFail:
                case OperationResult.ProcessingError:
                case OperationResult.PartialFail:
                    ///////////////////////////////////////

                    // todo: output file with errors with ext *.*.pre
                    string srcFileName = Path.GetFileName(this.srcFilePath);
                    ;
                    newFilePath = $"{Vars.alegeusFilesPreCheckFailRoot}/{fileName}";
                    newErrorFilePath = $"{newFilePath}.err";

                    // export error file
                    var outputTableName = "[dbo].[mbi_file_table]";
                    //
                    var queryStringExp = $" select concat(data_row, ',', case when len(error_message) > 0 then concat( 'PreCheck Errors: ' , error_message ) else 'PreCheck: OK' end ) as file_row" +
                                         $" from {outputTableName} " +
                                         $" where mbi_file_name = '{srcFileName}'" +
                                         $" order by mbi_file_table.source_row_no; ";

                    ImpExpUtils.ExportSingleColumnFlatFile(newErrorFilePath, dbConn, queryStringExp,
                        "file_row", null, fileLogParams,
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );

                    //
                    FileUtils.MoveFile(srcFilePath, newFilePath, (srcFilePath2, destFilePath2, dummy2) =>
                    {
                        // add to fileLog
                        fileLogParams.SetFileNames("", fileName, srcFilePath,
                                Path.GetFileName(newFilePath), newFilePath,
                                $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                "Fail", "PreCheck FAIL. Moved File to PreCheck FAIL Directory");
                        //
                        DbUtils.LogFileOperation(fileLogParams);
                    },
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );

                    break;
            }
            //
            return result;
        }
        private OperationResult CheckFile(FileCheckType fileCheckType)
        {
            //
            Dictionary<EdiFileFormat, List<int>> fileFormats =
                ImpExpUtils.GetAlegeusFileFormats(this.srcFilePath, false, this.fileLogParams);

            var result = OperationResult.Ok;

            // file may contain only a header...
            if (fileFormats.Count == 0)
            {
                this.fileCheckResults.Add(0, "File Is Empty");
                result = OperationResult.CompleteFail;
            }
            else
            {
                // check the file
                this.CheckFile(fileFormats);

                //
                if (this.fileCheckResults.IsCompleteFail)
                {
                    result = OperationResult.CompleteFail;
                }
                else if (this.fileCheckResults.HasErrors)
                {
                    result = OperationResult.PartialFail;
                }
                else
                {
                    result = OperationResult.Ok;
                }

            }

            return result;
        }

        private void CheckFile(Dictionary<EdiFileFormat, List<int>> fileFormats)
        {
            // 2. import the file
            string fileName = Path.GetFileName(srcFilePath) ?? string.Empty;
            fileLogParams?.SetFileNames(DbUtils.GetUniqueIdFromFileName(fileName), fileName, srcFilePath, "", "",
                "CheckFile", $"Starting: Check {fileName}", "Starting");

            // split text fileinto multiple files
            Dictionary<EdiFileFormat, Object[]> files = new Dictionary<EdiFileFormat, Object[]>();

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
            using (var inputFile = new StreamReader(this.srcFilePath))
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

            // get columns for file
            TypedCsvSchema mappings = Import.GetAlegeusFileImportMappings(fileFormat, true);

            //
            string tableName = "[dbo].[mbi_file_table_stage]";

            // truncate staging table
            DbUtils.TruncateTable(dbConn, tableName,
                fileLogParams?.GetMessageLogParams());


            // import the file with bulk copy
            var newPath =
                Import.PrefixLineWithEntireLineAndFileName(headerType, currentFilePath, this.srcFilePath, fileLogParams);

            // import into table so we can manipulate the file
            ImpExpUtils.ImportCsvFileBulkCopy(this.headerType, this.dbConn, newPath, this.hasHeaderRow, tableName,
                mappings, this.fileLogParams,
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            // update check type for table
            string queryString1 = $" update {tableName} set " +
                                  $" /* set check type */check_type = 'PreCheck', " +
                                  $" /* remove extra csv commas added to line */ data_row = replace(data_row, ',,,,,,,,,,,,,,,,,,,,', '') " +
                                  $" where 1 = 1;";
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString1, null,
                fileLogParams?.GetMessageLogParams()
            );

            // check file data
            CheckFileData(fileFormat, mappings);

            // run post import proc to take data from stage into final table
            string postImportProc = "[dbo].[process_mbi_file_table_stage_import]";
            string queryString = $"exec {postImportProc};";
            //
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                fileLogParams?.GetMessageLogParams()
            );
        }

        private void CheckFileData(EdiFileFormat fileFormat, TypedCsvSchema mappings)
        {
            // ensure previously cached data is not used so
            // so create a new db context to ensure stale data will NOT be used
            var dbErrorLog = Vars.dbCtxAlegeusErrorLogNew;
            // get all dbRows

            // ALTERNATE METHOD: refresh previous data - not working
            /*
                        //dbErrorLog.Entry(dbErrorLog.mbi_file_table_stage).Reload();
                        //((IObjectContextAdapter) dbErrorLog).ObjectContext.Refresh(RefreshMode.StoreWins, dbErrorLog.mbi_file_table_stage);
            */
            // get all data
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

            Boolean skipRestOfRow = false;

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

                // specific column checking against DB
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    // tpa ID
                    case "tpaid":
                        skipRestOfRow = this.CheckTpaExists(dataRow, column);
                        break;

                    // ER ID
                    case "employerid":
                        //ER must exist before any Import files are sent
                        skipRestOfRow = this.CheckEmployerExists(dataRow, column);
                        break;

                    // EE ID
                    case "employeeid":
                        //ER must exist before any Import files are sent. But for IB files, employee need not exist - he is being added
                        if (fileFormat != EdiFileFormat.AlegeusDemographics)
                        {
                            skipRestOfRow = this.CheckEmployeeExists(dataRow, column);
                        }

                        break;
                    // plan related
                    case "planid":
                        //case "planstartdate":
                        //case "planenddate":
                        // note: employee need not exist for IC files but employer must have a plan and EE must exist for the ER already via ann IB file load
                        skipRestOfRow = this.CheckEmployeeExists(dataRow, column);
                        if (!skipRestOfRow)
                        {
                            skipRestOfRow = this.CheckEmployerPlanExists(dataRow, column);
                        }

                        //if (!Utils.IsBlank(dataRow.DependentID))
                        //{
                        //    skipRestOfRow = this.CheckDependentPlanExists(dataRow, column);
                        //}
                        //else if (!Utils.IsBlank(dataRow.EmployeeID))
                        //{
                        //    skipRestOfRow = this.CheckEmployeePlanExists(dataRow, column);
                        //}
                        //else
                        //{
                        //    skipRestOfRow = this.CheckEmployerPlanExists(dataRow, column);
                        //}
                        break;

                    default:
                        break;
                }

                // skip checking other columns if a important column value is invalid
                if (skipRestOfRow)
                {
                    return;
                }

                // get value for the column
                var value = dataRow.ColumnValue(column.SourceColumn) ?? "";


                // check against column rules

                // format
                if (!Utils.IsValueOfFormat(value, column.FormatType))
                {
                    this.AddErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must formatted as {column.FormatType.ToDescription()}");
                }

                // minLength
                if (column.MinLength > 0 && value.Length < column.MinLength)
                {
                    this.AddErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must minimum {column.MinLength} characters long");
                }

                // maxLength
                if (column.MinLength > 0 && value.Length < column.MinLength)
                {
                    this.AddErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must minimum {column.MinLength} characters long");
                }

                // min/max value
                if (column.MinValue != 0 || column.MaxValue != 0)
                {
                    if (!Utils.IsNumeric(value))
                    {
                        this.AddErrorForRow(dataRow, column.SourceColumn, $"{column.SourceColumn} must be a number");
                    }

                    float numValue = Utils.ToNumber(value);
                    if (numValue < column.MinValue)
                    {
                        this.AddErrorForRow(dataRow, column.SourceColumn,
                            $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}");
                    }

                    if (numValue > column.MaxValue)
                    {
                        this.AddErrorForRow(dataRow, column.SourceColumn,
                            $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}");
                    }
                }
            }
        }

        private void AddErrorForRow(mbi_file_table_stage dataRow, string errCode, string errMessage, Boolean markAsCompleteFail = false)
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
                this.fileCheckResults.markAsCompleteFail = true;
            }
        }

        public Boolean CheckTpaExists(mbi_file_table_stage dataRow, TypedCsvColumn column)
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
                        fileLogParams?.GetMessageLogParams());

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

        public Boolean CheckEmployerExists(mbi_file_table_stage dataRow, TypedCsvColumn column)
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
                        DataRow dbData = dbRows[0];

                        //todo: does employer status need to be checked
                        string status = dbData["employer_status"]?.ToString();
                        if (status != "Active" && status != "New")
                        {
                            errorMessage =
                                $"The Employer ID {dataRow.EmployerId} has status {status} which is not valid";
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
                        fileLogParams?.GetMessageLogParams());

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

        }  // cache all EE for ER to reduce number of queries to database - each query for a single EE takes around 150 ms so we aree saving significant time esp for ER witjh many EE

        public Boolean CheckEmployeeExists(mbi_file_table_stage dataRow, TypedCsvColumn column)
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
                    DataRow[] dbRows = dbResults.Select($"employeeid = '{dataRow.EmployeeID}'");
                    if (dbRows.Length == 0)
                    {
                        errorMessage +=
                            $"The Employee ID {dataRow.EmployeeID} could not be found for Employer Id {dataRow.EmployerId}";
                        ;
                    }
                    else
                    {
                        // todo: we may be activating an employee - do not check?
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
                        fileLogParams?.GetMessageLogParams());

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

        public Boolean CheckEmployerPlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column)
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
                else if (Utils.IsBlank(dataRow.PlanId))
                {
                    errorMessage += $"The Plan ID cannot be blank";
                    ;
                }
                else
                {
                    DataTable dbResults = GetAllPlansForEmployer(dataRow.EmployerId);
                    // planid is not always present e.g. in deposit file
                    string filter = $"employer_id = '{dataRow.EmployerId}'";
                    if (!Utils.IsBlank(dataRow.AccountTypeCode))
                    {
                        filter += $"and account_type_code = '{dataRow.AccountTypeCode}' ";
                    }
                    if (!Utils.IsBlank(dataRow.PlanId))
                    {
                        filter += $"and plan_id = '{dataRow.PlanId}' ";
                    }
                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        errorMessage +=
                            $"The Plan ID {dataRow.PlanId} could not be found for Employer Id {dataRow.EmployerId}";
                        ;
                    }
                    else
                    {
                        DataRow dbData = dbRows[0];
                        DateTime actualPlanStartDate = (DateTime)dbData["plan_year_start_date"];
                        DateTime actualPlanEndDate = (DateTime)dbData["plan_year_end_date"];
                        DateTime actualGracePeriodEndDate = (DateTime)dbData["grace_period_end_date"];


                        //check plan dates match Alegeus
                        if (!Utils.IsBlank(dataRow.PlanStartDate) && actualPlanStartDate > Utils.ToDateTime(dataRow.PlanStartDate))
                        {
                            errorMessage +=
                                $"The Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate}";
                        }

                        if (!Utils.IsBlank(dataRow.PlanEndDate) && actualPlanEndDate < Utils.ToDateTime(dataRow.PlanEndDate))
                        {
                            errorMessage =
                                $"The Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)} and is no linger active on {dataRow.PlanStartDate}";
                            ;
                        }
                        //check effectivedate is within plan dates
                        if (!Utils.IsBlank(dataRow.EffectiveDate) && actualPlanStartDate > Utils.ToDateTime(dataRow.EffectiveDate))
                        {
                            errorMessage +=
                                $"The Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate}";
                        }

                        if (!Utils.IsBlank(dataRow.EffectiveDate) && actualPlanEndDate < Utils.ToDateTime(dataRow.EffectiveDate))
                        {
                            errorMessage =
                                $"The Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on  {dataRow.EffectiveDate}";
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
                    string queryString =
                        $" select employerid, employeeid, plancode, plandesc, min(planstart) as planstart, max(planend) as planend " +
                        $" from wc.wc_participant_plans " +
                        $" where employerid = '{Utils.DbQuote(employerId)}' " +
                        $" order by employerid, employeeid, plancode, plandesc"
                        ;
                    ;
                    //
                    dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        queryString, null,
                        fileLogParams?.GetMessageLogParams());

                    // create index on EmployeeID

                    DataColumn[] indices = new DataColumn[3];
                    indices[0] = (DataColumn)dbResults.Columns["employeeid"];
                    indices[1] = (DataColumn)dbResults.Columns["plancode"];
                    indices[2] = (DataColumn)dbResults.Columns["plandesc"];
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

        public Boolean CheckEmployeePlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column)
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
                else if (Utils.IsBlank(dataRow.PlanId))
                {
                    errorMessage += $"The Plan ID cannot be blank";
                    ;
                }
                else
                {
                    DataTable dbResults = GetAllEmployeePlansForEmployer(dataRow.EmployerId);

                    // planid is not always present e.g. in deposit file
                    string filter = $"employeeid = '{dataRow.EmployeeID}' ";
                    if (!Utils.IsBlank(dataRow.AccountTypeCode))
                    {
                        filter += $"and plancode = '{dataRow.AccountTypeCode}' ";
                    }
                    if (!Utils.IsBlank(dataRow.PlanId))
                    {
                        filter += $"and plandesc = '{dataRow.PlanId}' ";
                    }
                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        errorMessage +=
                            $"The Plan ID {dataRow.PlanId} could not be found for Employer Id {dataRow.EmployerId} and Employee Id {dataRow.EmployeeID}";
                        ;
                    }
                    else
                    {
                        DataRow dbData = dbRows[0];
                        DateTime actualPlanStartDate = (DateTime)dbData["planstart"];
                        DateTime actualPlanEndDate = (DateTime)dbData["planend"];
                        DateTime actualGracePeriodEndDate = Utils.ToDateTime(dbData["actualGracePeriodEndDate"]?.ToString());

                        //check plan dates match Alegeus
                        if (!Utils.IsBlank(dataRow.PlanStartDate) && actualPlanStartDate > Utils.ToDateTime(dataRow.PlanStartDate))
                        {
                            errorMessage +=
                                $"The Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate}";
                        }

                        if (!Utils.IsBlank(dataRow.PlanEndDate) && actualPlanEndDate < Utils.ToDateTime(dataRow.PlanEndDate))
                        {
                            errorMessage =
                                $"The Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)} and is no linger active on {dataRow.PlanStartDate}";
                            ;
                        }
                        //check effectivedate is within plan dates
                        if (!Utils.IsBlank(dataRow.EffectiveDate) && actualPlanStartDate > Utils.ToDateTime(dataRow.EffectiveDate))
                        {
                            errorMessage +=
                                $"The Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate}";
                        }

                        if (!Utils.IsBlank(dataRow.EffectiveDate) && actualPlanEndDate < Utils.ToDateTime(dataRow.EffectiveDate))
                        {
                            errorMessage =
                                $"The Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on  {dataRow.EffectiveDate}";
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

        public Boolean CheckDependentExists(mbi_file_table_stage dataRow, TypedCsvColumn column)
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
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(dataRow.EmployeeID))
                {
                    errorMessage += $"The Employee ID cannot be blank";
                    ;
                }
                else
                {
                    if (PlatformType == PlatformType.Alegeus)
                    {
                        //todo: add data in Portal for Dependents
                        //string queryString =
                        //    $"select employer_id, employee_id, is_active from wc.vw_wc_participants  " +
                        //    $" where employer_id = '{Utils.DbQuote(dataRow.EmployerId)}' " +
                        //    $" and employee_id = '{Utils.DbQuote(dataRow.EmployeeID)}' ";
                        ////
                        //DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        //    queryString, null,
                        //     fileLogParams?.GetMessageLogParams()));

                        //if (dbResults.Rows.Count == 0)
                        //{
                        //    errorMessage += $"The Employee ID {dataRow.EmployeeID} could not be found for Employer Id {dataRow.EmployerId}" ; ;
                        //}
                        //else
                        //{
                        //    DataRow dbData = dbResults.Rows[0];
                        //    float status = Utils.ToNumber(dbData["is_active"]?.ToString());
                        //    if (status <= 0)
                        //    {
                        //        errorMessage +=
                        //            $"The Employee ID {dataRow.EmployeeID} has status {status} which is not valid" ;
                        //    }
                        //}
                    }
                    else
                    {
                        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
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
        public Boolean CheckDependentPlanExists(mbi_file_table_stage dataRow, TypedCsvColumn column)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.EmployerId}-{dataRow.PlanId}";
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
                else if (Utils.IsBlank(dataRow.PlanId))
                {
                    errorMessage += $"The Plan ID cannot be blank";
                    ;
                }
                else
                {
                    if (PlatformType == PlatformType.Alegeus)
                    {
                        // todo: add data in Portal for dependent plans
                        //string queryString =
                        //    $" select employerid,employeeid, plancode, plandesc, planstart,planend from wc.wc_participant_plans " +
                        //    $" where employerid = '{Utils.DbQuote(dataRow.EmployerId)}' " +
                        //    $" where employeeid = '{Utils.DbQuote(dataRow.EmployeeID)}' " +
                        //    $" and (plancode = '{Utils.DbQuote(dataRow.PlanId)}' OR plandesc = '{Utils.DbQuote(dataRow.PlanId)}' )";
                        ////
                        //DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        //    queryString, null,
                        //     fileLogParams?.GetMessageLogParams());

                        //if (dbResults.Rows.Count == 0)
                        //{
                        //    errorMessage += $"The Plan ID {dataRow.PlanId} could not be found for Employer Id {dataRow.EmployerId} and Employee Id {dataRow.EmployeeID}" ; ;
                        //}
                        //else
                        //{
                        //    DataRow dbData = dbResults.Rows[0];
                        //    DateTime actualPlanStartDate = Utils.ToDateTime(dbData["planstart"]?.ToString());
                        //    DateTime actualPlanEndDate = Utils.ToDateTime(dbData["planend"]?.ToString());
                        //    //DateTime actualGracePeriodEndDate = Utils.ToDateTime(dbData["actualGracePeriodEndDate"]?.ToString());

                        //    //
                        //    if (actualPlanStartDate > DateTime.Now.Date)
                        //    {
                        //        errorMessage +=
                        //            $"The Plan ID {dataRow.PlanId} starts only on {Utils.ToDateString(actualPlanStartDate)}" ;
                        //    }
                        //    if (actualPlanEndDate < DateTime.Now.Date)
                        //    {
                        //        errorMessage =
                        //            $"The Plan ID {dataRow.PlanId} ended on {Utils.ToDateString(actualPlanEndDate)}" ; ;
                        //    }
                        //}
                    }
                    else
                    {
                        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
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
    }
}