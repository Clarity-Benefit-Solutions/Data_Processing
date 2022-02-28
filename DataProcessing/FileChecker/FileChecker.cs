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

// ReSharper disable All

// ReSharper disable once CheckNamespace
namespace DataProcessing
{
    public class FileCheckResults : Dictionary<int, string>
    {
        public FileCheckResults() : base()
        {
        }

        public Boolean Succcess
        {
            get { return this.Count == 0; }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class FileChecker : IDisposable
    {
        //
        private static Dictionary<string, string> cachedDBChecks = new Dictionary<string, string>();

        //
        private readonly MySqlConnection dbConnPortalWc;

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

        public void CheckFile(FileCheckType fileCheckType)
        {
            //
            Dictionary<EdiFileFormat, List<int>> fileFormats =
                ImpExpUtils.GetAlegeusFileFormats(this.srcFilePath, false, this.fileLogParams);

            // file may contain only a header...
            if (fileFormats.Count == 0)
            {
                this.fileCheckResults.Add(0, "File Is Empty");
                return;
            }
            else
            {
                this.CheckFile(fileFormats);
            }
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

                // import the file
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
                Import.PrefixLineWithEntireLineAndFileName(headerType, currentFilePath, this.srcFilePath,
                    fileLogParams);

            // import into table so we can manipulate the file
            ImpExpUtils.ImportCsvFileBulkCopy(this.headerType, this.dbConn, newPath, this.hasHeaderRow, tableName,
                mappings, this.fileLogParams,
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            // update check type for table
            string queryString1 = $"update {tableName} set check_type = 'PreCheck' where 1 = 1;";
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
            // get all rows

            // ALTERNATE METHOD: refresh previous data - not working
            /*
                        //dbErrorLog.Entry(dbErrorLog.mbi_file_table_stage).Reload();
                        //((IObjectContextAdapter) dbErrorLog).ObjectContext.Refresh(RefreshMode.StoreWins, dbErrorLog.mbi_file_table_stage);
            */
            // get all data
            var rows = dbErrorLog.mbi_file_table_stage
                .OrderBy(row => row.source_row_no)
                .ToList();

            //check each row
            int rowNo = 0;
            foreach (var row in rows)
            {
                rowNo++;
                this.CheckFileData(fileFormat, row, mappings);
            }

            // save any changes
            dbErrorLog.SaveChanges();
        }

        private void CheckFileData(EdiFileFormat fileFormat, mbi_file_table_stage row, TypedCsvSchema mappings)
        {
            // don't check header row
            if (row.row_type == "IA" || row.row_type == "RA")
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
                        skipRestOfRow = this.CheckTpaExists(row, column);
                        break;

                    // ER ID
                    case "employerid":
                        //ER must exist before any Import files are sent
                        skipRestOfRow = this.CheckEmployerExists(row, column);
                        break;

                    // EE ID
                    case "employeeid":
                        //ER must exist before any Import files are sent. But for IB files, employee need not exist - he is being added
                        if (fileFormat != EdiFileFormat.AlegeusDemographics)
                        {
                            skipRestOfRow = this.CheckEmployeeExists(row, column);
                        }

                        break;
                    // plan related
                    case "planid":
                        //case "planstartdate":
                        //case "planenddate":
                        // note: employee need not exist for IC files but employer must have a plan and EE must exist for the ER already via ann IB file load
                        skipRestOfRow = this.CheckEmployeeExists(row, column);
                        if (!skipRestOfRow)
                        {
                            skipRestOfRow = this.CheckEmployerPlanExists(row, column);
                        }

                        //if (!Utils.IsBlank(row.DependentID))
                        //{
                        //    skipRestOfRow = this.CheckDependentPlanExists(row, column);
                        //}
                        //else if (!Utils.IsBlank(row.EmployeeID))
                        //{
                        //    skipRestOfRow = this.CheckEmployeePlanExists(row, column);
                        //}
                        //else
                        //{
                        //    skipRestOfRow = this.CheckEmployerPlanExists(row, column);
                        //}
                        break;

                    default:
                        break;
                }

                // skip checking other columns if a important column value is invalid
                if (skipRestOfRow)
                {
                    continue;
                }

                // get value for the column
                var value = row.ColumnValue(column.SourceColumn) ?? "";


                // check against column rules

                // format
                if (!Utils.IsValueOfFormat(value, column.FormatType))
                {
                    this.AddErrorForRow(row, column.SourceColumn,
                        $"{column.SourceColumn} must formatted as {column.FormatType.ToDescription()}");
                }

                // minLength
                if (column.MinLength > 0 && value.Length < column.MinLength)
                {
                    this.AddErrorForRow(row, column.SourceColumn,
                        $"{column.SourceColumn} must minimum {column.MinLength} characters long");
                }

                // maxLength
                if (column.MinLength > 0 && value.Length < column.MinLength)
                {
                    this.AddErrorForRow(row, column.SourceColumn,
                        $"{column.SourceColumn} must minimum {column.MinLength} characters long");
                }

                // min/max value
                if (column.MinValue != 0 || column.MaxValue != 0)
                {
                    if (!Utils.IsNumeric(value))
                    {
                        this.AddErrorForRow(row, column.SourceColumn, $"{column.SourceColumn} must be a number");
                    }

                    float numValue = Utils.ToNumber(value);
                    if (numValue < column.MinValue)
                    {
                        this.AddErrorForRow(row, column.SourceColumn,
                            $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}");
                    }

                    if (numValue > column.MaxValue)
                    {
                        this.AddErrorForRow(row, column.SourceColumn,
                            $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}");
                    }
                }
            }
        }

        private void AddErrorForRow(mbi_file_table_stage row, string errCode,
            string errMessage)
        {
            // add to row so it will be saved back to DB for row by row data
            row.error_code = errCode + "\n";
            ;
            row.error_message += errMessage + "\n";
            //
            int key = row.source_row_no ?? 0;
            if (this.fileCheckResults.ContainsKey(key))
            {
                this.fileCheckResults[key] = $"{row.source_row_no}: {errCode} : {errMessage}";
            }
            else
            {
                this.fileCheckResults.Add(key, $"{row.source_row_no}: {errCode} : {errMessage}");
            }
        }

        public Boolean CheckTpaExists(mbi_file_table_stage row, TypedCsvColumn column)
        {
            var errorMessage = "";
            var cacheKey = $"{MethodBase.GetCurrentMethod()?.Name}-{row.TpaId}";
            if (cachedDBChecks.ContainsKey(cacheKey))
            {
                errorMessage = cachedDBChecks[cacheKey];
            }
            else
            {
                if (PlatformType == PlatformType.Alegeus)
                {
                    // check DB
                    if (Utils.IsBlank(row.TpaId))
                    {
                        errorMessage = "TPA ID cannot be blank. It must always be BENEFL";
                    }
                    else if (row.TpaId != "BENEFL")
                    {
                        errorMessage = $"TPA ID {row.TpaId} is invalid. It must always be BENEFL";
                    }
                }
                else
                {
                    throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                }

                //
                cachedDBChecks.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(row, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckEmployerExists(mbi_file_table_stage row, TypedCsvColumn column)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{row.EmployerId}";
            if (cachedDBChecks.ContainsKey(cacheKey))
            {
                errorMessage = cachedDBChecks[cacheKey];
            }
            else
            {
                // check DB
                if (Utils.IsBlank(row.EmployerId))
                {
                    errorMessage = $"The Employer ID cannot be blank";
                }
                else
                {
                    if (PlatformType == PlatformType.Alegeus)
                    {
                        string queryString =
                            $"select employer_id, employer_name, employer_status from wc.wc_employers " +
                            $" where employer_id = '{Utils.DbQuote(row.EmployerId)}' ";
                        //
                        DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                            queryString, null,
                            fileLogParams?.GetMessageLogParams()
                        );

                        if (dbResults.Rows.Count == 0)
                        {
                            errorMessage = $"The Employer ID {row.EmployerId} could not be found";
                        }
                        else
                        {
                            DataRow rec = dbResults.Rows[0];
                            //todo: does employer status need to be checked
                            string status = rec["employer_status"]?.ToString();
                            if (status != "Active" && status != "New")
                            {
                                errorMessage =
                                    $"The Employer ID {row.EmployerId} has status {status} which is not valid";
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                    }
                }

                //
                cachedDBChecks.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(row, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckEmployeeExists(mbi_file_table_stage row, TypedCsvColumn column)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{row.EmployerId}-{row.EmployeeID}";
            if (cachedDBChecks.ContainsKey(cacheKey))
            {
                errorMessage = cachedDBChecks[cacheKey];
            }
            else
            {
                // check DB
                if (Utils.IsBlank(row.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank" + "\n";
                    ;
                }
                else if (Utils.IsBlank(row.EmployeeID))
                {
                    errorMessage += $"The Employee ID cannot be blank" + "\n";
                    ;
                }
                else
                {
                    if (PlatformType == PlatformType.Alegeus)
                    {
                        string queryString =
                            $"select employerid, employeeid, is_active from wc.vw_wc_participants  " +
                            $" where employerid = '{Utils.DbQuote(row.EmployerId)}' " +
                            $" and employeeid = '{Utils.DbQuote(row.EmployeeID)}' ";
                        //
                        DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                            queryString, null,
                            fileLogParams?.GetMessageLogParams());

                        if (dbResults.Rows.Count == 0)
                        {
                            errorMessage +=
                                $"The Employee ID {row.EmployeeID} could not be found for Employer Id {row.EmployerId}" +
                                "\n";
                            ;
                        }
                        else
                        {
                            // todo: we may be activating an employee - do not check?
                            DataRow rec = dbResults.Rows[0];
                            float status = Utils.ToNumber(rec["is_active"]?.ToString());
                            if (status <= 0 && Utils.ToNumber(row.EmployeeStatus) > 1)
                            {
                                errorMessage +=
                                    $"The Employee ID {row.EmployeeID} has status {status} which is not valid" + "\n";
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                    }
                }

                //
                cachedDBChecks.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(row, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckDependentExists(mbi_file_table_stage row, TypedCsvColumn column)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{row.EmployerId}-{row.EmployeeID}";
            if (cachedDBChecks.ContainsKey(cacheKey))
            {
                errorMessage = cachedDBChecks[cacheKey];
            }
            else
            {
                // check DB
                if (Utils.IsBlank(row.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank" + "\n";
                    ;
                }
                else if (Utils.IsBlank(row.EmployeeID))
                {
                    errorMessage += $"The Employee ID cannot be blank" + "\n";
                    ;
                }
                else
                {
                    if (PlatformType == PlatformType.Alegeus)
                    {
                        //todo: add data in Portal for Dependents
                        //string queryString =
                        //    $"select employer_id, employee_id, is_active from wc.vw_wc_participants  " +
                        //    $" where employer_id = '{Utils.DbQuote(row.EmployerId)}' " +
                        //    $" and employee_id = '{Utils.DbQuote(row.EmployeeID)}' ";
                        ////
                        //DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        //    queryString, null,
                        //     fileLogParams?.GetMessageLogParams()));

                        //if (dbResults.Rows.Count == 0)
                        //{
                        //    errorMessage += $"The Employee ID {row.EmployeeID} could not be found for Employer Id {row.EmployerId}" + "\n"; ;
                        //}
                        //else
                        //{
                        //    DataRow rec = dbResults.Rows[0];
                        //    float status = Utils.ToNumber(rec["is_active"]?.ToString());
                        //    if (status <= 0)
                        //    {
                        //        errorMessage +=
                        //            $"The Employee ID {row.EmployeeID} has status {status} which is not valid" + "\n";
                        //    }
                        //}
                    }
                    else
                    {
                        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                    }
                }

                //
                cachedDBChecks.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(row, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckEmployerPlanExists(mbi_file_table_stage row, TypedCsvColumn column)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{row.EmployerId}-{row.PlanId}";
            if (cachedDBChecks.ContainsKey(cacheKey))
            {
                errorMessage = cachedDBChecks[cacheKey];
            }
            else
            {
                // check DB
                if (Utils.IsBlank(row.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank" + "\n";
                    ;
                }
                else if (Utils.IsBlank(row.PlanId))
                {
                    errorMessage += $"The Plan ID cannot be blank" + "\n";
                    ;
                }
                else
                {
                    if (PlatformType == PlatformType.Alegeus)
                    {
                        string queryString =
                            $"select employer_id, plan_id, min(plan_year_start_date) as plan_year_start_date, max(plan_year_end_date) as plan_year_end_date, max(grace_period_end_date) grace_period_end_date " +
                            $" from wc.wc_employer_plans " +
                            $" where employer_id = '{Utils.DbQuote(row.EmployerId)}' " +
                            $" and plan_id = '{Utils.DbQuote(row.PlanId)}'" +
                            $" and account_type_code = '{Utils.DbQuote(row.AccountTypeCode)}'" +
                            $" group by employer_id, plan_id, account_type_code " +
                            $" LIMIT 1 ";
                        //
                        DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                            queryString, null,
                            fileLogParams?.GetMessageLogParams());

                        if (dbResults.Rows.Count == 0)
                        {
                            errorMessage +=
                                $"The Plan ID {row.PlanId} could not be found for Employer Id {row.EmployerId}" + "\n";
                            ;
                        }
                        else
                        {
                            DataRow rec = dbResults.Rows[0];
                            DateTime plan_year_start_date = (DateTime)rec["plan_year_start_date"];
                            DateTime plan_year_end_date = (DateTime)rec["plan_year_end_date"];
                            DateTime grace_period_end_date = (DateTime)rec["grace_period_end_date"];

                            //todo: check plan dates match Alegeus
                            if (!Utils.IsBlank(row.PlanStartDate) && plan_year_start_date > DateTime.Now.Date)
                            {
                                errorMessage +=
                                    $"The Plan ID {row.PlanId} starts only on {Utils.ToDateString(plan_year_start_date)}" +
                                    "\n";
                            }

                            if (!Utils.IsBlank(row.PlanEndDate) && plan_year_end_date < DateTime.Now.Date)
                            {
                                errorMessage =
                                    $"The Plan ID {row.PlanId} ended on {Utils.ToDateString(plan_year_end_date)}" +
                                    "\n";
                                ;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                    }
                }

                //
                cachedDBChecks.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(row, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckEmployeePlanExists(mbi_file_table_stage row, TypedCsvColumn column)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{row.EmployerId}-{row.EmployeeID}-{row.PlanId}";
            if (cachedDBChecks.ContainsKey(cacheKey))
            {
                errorMessage = cachedDBChecks[cacheKey];
            }
            else
            {
                // check DB
                if (Utils.IsBlank(row.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank" + "\n";
                    ;
                }
                else if (Utils.IsBlank(row.EmployeeID))
                {
                    errorMessage += $"The Employer ID cannot be blank" + "\n";
                    ;
                }
                else if (Utils.IsBlank(row.PlanId))
                {
                    errorMessage += $"The Plan ID cannot be blank" + "\n";
                    ;
                }
                else
                {
                    if (PlatformType == PlatformType.Alegeus)
                    {
                        string queryString =
                            $" select employerid, employeeid, plancode, plandesc, min(planstart) as planstart, max(planend) as planend from wc.wc_participant_plans " +
                            $" where employerid = '{Utils.DbQuote(row.EmployerId)}' " +
                            $" and employeeid = '{Utils.DbQuote(row.EmployeeID)}' " +
                            /*$" and plandesc = '{Utils.DbQuote(row.PlanId)}'" +*/
                            $" and plancode = '{Utils.DbQuote(row.AccountTypeCode)}'" +
                            $" group by employerid, employeeid, plancode, plandesc" +
                            $" LIMIT 1 ";
                        //
                        DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                            queryString, null,
                            fileLogParams?.GetMessageLogParams());

                        if (dbResults.Rows.Count == 0)
                        {
                            errorMessage +=
                                $"The Plan ID {row.PlanId} could not be found for Employer Id {row.EmployerId} and Employee Id {row.EmployeeID}" +
                                "\n";
                            ;
                        }
                        else
                        {
                            DataRow rec = dbResults.Rows[0];
                            DateTime plan_year_start_date = (DateTime)rec["planstart"];
                            DateTime plan_year_end_date = (DateTime)rec["planend"];
                            //DateTime grace_period_end_date = Utils.ToDateTime(rec["grace_period_end_date"]?.ToString());

                            // todo: check depositdate are within the plan dates in Alegeus
                            // todo: check begin and end dates match those in Alegeus
                            if (plan_year_start_date > DateTime.Now.Date)
                            {
                                errorMessage +=
                                    $"The Plan ID {row.PlanId} starts only on {Utils.ToDateString(plan_year_start_date)}" +
                                    "\n";
                            }

                            if (plan_year_end_date < DateTime.Now.Date)
                            {
                                errorMessage =
                                    $"The Plan ID {row.PlanId} ended on {Utils.ToDateString(plan_year_end_date)}" +
                                    "\n";
                                ;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                    }
                }

                //
                cachedDBChecks.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(row, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckDependentPlanExists(mbi_file_table_stage row, TypedCsvColumn column)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{row.EmployerId}-{row.PlanId}";
            if (cachedDBChecks.ContainsKey(cacheKey))
            {
                errorMessage = cachedDBChecks[cacheKey];
            }
            else
            {
                // check DB
                if (Utils.IsBlank(row.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank" + "\n";
                    ;
                }
                else if (Utils.IsBlank(row.EmployeeID))
                {
                    errorMessage += $"The Employer ID cannot be blank" + "\n";
                    ;
                }
                else if (Utils.IsBlank(row.PlanId))
                {
                    errorMessage += $"The Plan ID cannot be blank" + "\n";
                    ;
                }
                else
                {
                    if (PlatformType == PlatformType.Alegeus)
                    {
                        // todo: add data in Portal for dependent plans
                        //string queryString =
                        //    $" select employerid,employeeid, plancode, plandesc, planstart,planend from wc.wc_participant_plans " +
                        //    $" where employerid = '{Utils.DbQuote(row.EmployerId)}' " +
                        //    $" where employeeid = '{Utils.DbQuote(row.EmployeeID)}' " +
                        //    $" and (plancode = '{Utils.DbQuote(row.PlanId)}' OR plandesc = '{Utils.DbQuote(row.PlanId)}' )";
                        ////
                        //DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnPortalWc,
                        //    queryString, null,
                        //     fileLogParams?.GetMessageLogParams());

                        //if (dbResults.Rows.Count == 0)
                        //{
                        //    errorMessage += $"The Plan ID {row.PlanId} could not be found for Employer Id {row.EmployerId} and Employee Id {row.EmployeeID}" + "\n"; ;
                        //}
                        //else
                        //{
                        //    DataRow rec = dbResults.Rows[0];
                        //    DateTime plan_year_start_date = Utils.ToDateTime(rec["planstart"]?.ToString());
                        //    DateTime plan_year_end_date = Utils.ToDateTime(rec["planend"]?.ToString());
                        //    //DateTime grace_period_end_date = Utils.ToDateTime(rec["grace_period_end_date"]?.ToString());

                        //    //
                        //    if (plan_year_start_date > DateTime.Now.Date)
                        //    {
                        //        errorMessage +=
                        //            $"The Plan ID {row.PlanId} starts only on {Utils.ToDateString(plan_year_start_date)}" + "\n";
                        //    }
                        //    if (plan_year_end_date < DateTime.Now.Date)
                        //    {
                        //        errorMessage =
                        //            $"The Plan ID {row.PlanId} ended on {Utils.ToDateString(plan_year_end_date)}" + "\n"; ;
                        //    }
                        //}
                    }
                    else
                    {
                        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
                    }
                }

                //
                cachedDBChecks.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddErrorForRow(row, column.SourceColumn, $"{errorMessage}");
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