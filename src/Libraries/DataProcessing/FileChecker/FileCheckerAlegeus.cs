using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public Boolean CheckAlegeusDependentExists(mbi_file_table_stage fileRow, TypedCsvColumn column,
            EdiRowFormat rowFormat)
        {
            // dependent plans are linked to the employee
            return CheckAlegeusEmployeeExists(fileRow, column, rowFormat);
        }

        public Boolean CheckAlegeusDependentPlanExists(mbi_file_table_stage fileRow, TypedCsvColumn column,
            EdiRowFormat rowFormat)
        {
            // dependent plans are linked to the employee
            return CheckAlegeusEmployeePlanExists(fileRow, column, rowFormat);
        }

        public Boolean CheckAlegeusEmployeeExists(mbi_file_table_stage fileRow, TypedCsvColumn column,
            EdiRowFormat rowFormat = EdiRowFormat.Unknown)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{fileRow.EmployerId}-{fileRow.EmployeeID}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(fileRow.EmployerId))
                {
                    AddAlegeusErrorForRow(fileRow, "EmployerId", $"The Employer ID cannot be blank");
                }
                else if (Utils.IsBlank(fileRow.EmployeeID))
                {
                    AddAlegeusErrorForRow(fileRow, "EmployeeId", $"The Employee ID cannot be blank");
                }
                else
                {
                    DataTable dbResults = GetAllAlegeusEmployeesForEmployer(fileRow.EmployerId);
                    DataRow[] dbRows =
                        dbResults.Select(
                            $"employerid = '{fileRow.EmployerId}' and employeeid = '{fileRow.EmployeeID}'");
                    if (dbRows.Length == 0)
                    {
                        // for demographics file, the employee will not yet exist or the status may be changing (activating or terminating) - do not check
                        if (rowFormat == EdiRowFormat.AlegeusDemographics)
                        {
                            // as it is an demographics file, add this employee to the ER-EE table so a check for plan enrollemnt within same run or before reaggregation from Alegeus will suceed
                            DataRow newRow = dbResults.NewRow();
                            newRow["employerid"] = fileRow.EmployerId;
                            newRow["employeeid"] = fileRow.EmployeeID;
                            newRow["is_active"] = fileRow.EmployeeStatus == "2" ? 1 : 0;
                            dbResults.Rows.Add(newRow);

                            var cacheKey2 =
                                $"GetAllEmployeesForEmployer-{this.PlatformType.ToDescription()}-{fileRow.EmployerId}-AllEmployees";
                            _cache.Add(cacheKey2, dbResults);

                            //
                            return false;
                        }
                        else
                        {
                            errorMessage +=
                                $"The Employee ID '{fileRow.EmployeeID}' could not be found for Employer Id '{fileRow.EmployerId}'";
                        }
                    }
                    else
                    {
                        DataRow dbRow = dbRows[0];
                        // if employee exists as per our data, that is fine
                        // do not check the file EmployeeStatus against what we havwe in the db
                        //float status = Utils.ToNumber(dbRow["is_active"]?.ToString());
                        //if (status <= 0 && Utils.ToNumber(fileRow.EmployeeStatus) > 1)
                        //{
                        //  errorMessage +=
                        //    $"The Employee ID {fileRow.EmployeeID} has status {status} which is not valid";
                        //}
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployeePlanExists(mbi_file_table_stage fileRow, TypedCsvColumn column,
            EdiRowFormat rowFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{fileRow.EmployerId}-{fileRow.EmployeeID}-{fileRow.AccountTypeCode}-{fileRow.PlanId}-{fileRow.PlanStartDate}-{fileRow.PlanEndDate}-{fileRow.EffectiveDate}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(fileRow.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(fileRow.EmployeeID))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(fileRow.AccountTypeCode))
                {
                    errorMessage += $"The AccountTypeCode cannot be blank";
                    ;
                }
                else
                {
                    //// if we are enrolling an employee in a plan, only check if ER has this EE
                    //if (rowFormat == EdiFileFormat.AlegeusEnrollment)
                    //{
                    //  //as it is an enrollment file, check the EE exists and enroll in the plan
                    //  var hasError = this.CheckEmployeeExists(fileRow, column, rowFormat);
                    //  //return hasError;
                    //}

                    DataTable dbResults = GetAllAlegeusEmployeePlansForEmployer(fileRow.EmployerId);

                    // planid is not always present e.g. in deposit file
                    string filter = $" employeeid = '{fileRow.EmployeeID}' ";
                    if (!Utils.IsBlank(fileRow.AccountTypeCode))
                    {
                        filter += $" and plancode = '{fileRow.AccountTypeCode}' ";
                    }

                    if (!Utils.IsBlank(fileRow.PlanId))
                    {
                        filter += $" and plandesc = '{fileRow.PlanId}' ";
                    }

                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        if (rowFormat == EdiRowFormat.AlegeusEnrollment)
                        {
                            // as it is an enrollment, enroll the EE in this plan demographics file,
                            DataRow newRow = dbResults.NewRow();
                            newRow["employerid"] = fileRow.EmployerId;
                            newRow["employeeid"] = fileRow.EmployeeID;
                            newRow["plancode"] = fileRow.AccountTypeCode;
                            newRow["plandesc"] = fileRow.PlanId;
                            newRow["planstart"] = Utils.ToDateTime(fileRow.PlanStartDate);
                            newRow["planend"] = Utils.ToDateTime(fileRow.PlanEndDate);

                            dbResults.Rows.Add(newRow);

                            var cacheKey2 =
                                $"GetAllEmployeePlansForEmployer-{this.PlatformType.ToDescription()}-{fileRow.EmployerId}-AllEmployeePlans";
                            //
                            _cache.Add(cacheKey2, dbResults);

                            //
                            return false;
                        }

                        errorMessage +=
                            $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                            (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                            $" could not be found for Employee Id '{fileRow.EmployeeID}'";
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
                            if (dbRowPlanStartDate == Utils.ToDate(fileRow.PlanStartDate) &&
                                dbRowPlanEndDate == Utils.ToDate(fileRow.PlanEndDate))
                            {
                                matchedRows.Add(dbRow);
                            }
                        }

                        // if no exact match for start and end dates, throw error
                        if (matchedRows.Count == 0)
                        {
                            errorMessage +=
                                                    $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                                                    (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                                                     $" and Plan Start date '{fileRow.PlanStartDate}'" +
                                                     $" and Plan End date '{fileRow.PlanEndDate}'" +
                                                    $" could not be found for Employee Id '{fileRow.EmployeeID}'";
                        }
                        else
                        {
                            // take first matched rows - should be only usually
                            var dbRow = matchedRows.First();
                            //
                            DateTime dbRowPlanStartDate = (DateTime)dbRow["planstart"];
                            DateTime dbRowPlanEndDate = (DateTime)dbRow["planend"];

                            //check end date is after startdate
                            if (!Utils.IsBlank(fileRow.PlanStartDate) && !Utils.IsBlank(fileRow.PlanEndDate) &&
                                Utils.ToDate(fileRow.PlanStartDate) > Utils.ToDate(fileRow.PlanEndDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                                    (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                                    $" Start Date '{fileRow.PlanStartDate}' must be before the Plan End Date '{fileRow.PlanEndDate}' for Employee Id '{fileRow.EmployeeID}'";
                            }

                            //check effectivedate is within plan dates
                            if (!Utils.IsBlank(fileRow.EffectiveDate) &&
                                dbRowPlanStartDate > Utils.ToDate(fileRow.EffectiveDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                                    (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                                    $" starts only on '{Utils.ToDateString(dbRowPlanStartDate)}' and is not yet started on '{fileRow.EffectiveDate}' for Employee Id '{fileRow.EmployeeID}' ";
                            }

                            if (!Utils.IsBlank(fileRow.EffectiveDate) &&
                                dbRowPlanEndDate < Utils.ToDate(fileRow.EffectiveDate))
                            {
                                errorMessage =
                                    $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                                    (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                                    $" ended on '{Utils.ToDateString(dbRowPlanEndDate)}' and is no longer active on '{fileRow.EffectiveDate}' for Employee Id '{fileRow.EmployeeID}'";
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
                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployerExists(mbi_file_table_stage fileRow, TypedCsvColumn column,
            EdiRowFormat rowFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{fileRow.EmployerId}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(fileRow.EmployerId))
                {
                    errorMessage = $"The Employer ID cannot be blank";
                }
                else
                {
                    DataTable dbResults = GetAllAlegeusEmployers();
                    // planid is not always present e.g. in deposit file
                    string filter = $"employer_id = '{fileRow.EmployerId}'";
                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        errorMessage = $"The Employer ID '{fileRow.EmployerId}' could not be found";
                    }
                    else
                    {
                        //DataRow dbRow = dbRows[0];

                        //note: FileChecker: verify if employer status need to be checked
                        //string status = dbRow["employer_status"]?.ToString();
                        //if (status != "Active" && status != "New")
                        //{
                        //  errorMessage =
                        //    $"The Employer ID {fileRow.EmployerId} has status {status} which is not valid";
                        //}
                    }
                }

                //
                _cache.Add(cacheKey, errorMessage);
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusEmployerPlanExists(mbi_file_table_stage fileRow, TypedCsvColumn column,
            EdiRowFormat rowFormat)
        {
            var errorMessage = "";
            var cacheKey =
                $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{fileRow.EmployerId}-{fileRow.AccountTypeCode}-{fileRow.PlanId}-{fileRow.PlanStartDate}-{fileRow.PlanEndDate}-{fileRow.EffectiveDate}";
            if (_cache.ContainsKey(cacheKey))
            {
                errorMessage = _cache.Get(cacheKey)?.ToString();
            }
            else
            {
                // check DB
                if (Utils.IsBlank(fileRow.EmployerId))
                {
                    errorMessage += $"The Employer ID cannot be blank";
                    ;
                }
                else if (Utils.IsBlank(fileRow.AccountTypeCode))
                {
                    errorMessage += $"The AccountTypeCode cannot be blank";
                    ;
                }
                else
                {
                    //todo: check this logic
                    DataTable dbResults = GetAllAlegeusPlansForEmployer(fileRow.EmployerId);

                    // planid is not always present e.g. in deposit file
                    string filter = $"employer_id = '{fileRow.EmployerId}'";
                    if (!Utils.IsBlank(fileRow.AccountTypeCode))
                    {
                        filter += $" and account_type_code = '{fileRow.AccountTypeCode}' ";
                    }

                    if (!Utils.IsBlank(fileRow.PlanId))
                    {
                        filter += $" and plan_id = '{fileRow.PlanId}' ";
                    }

                    DataRow[] dbRows = dbResults.Select(filter);

                    if (dbRows.Length == 0)
                    {
                        errorMessage +=
                            $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                            (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                            $" could not be found for Employer Id '{fileRow.EmployerId}' ";
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
                            if (dbRowPlanStartDate == Utils.ToDate(fileRow.PlanStartDate) &&
                                dbRowPlanEndDate == Utils.ToDate(fileRow.PlanEndDate))
                            {
                                matchedRows.Add(dbRow);
                            }
                        }

                        // if no exact match for start and end dates, throw error
                        if (matchedRows.Count == 0)
                        {
                            errorMessage +=
                                                    $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                                                    (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                                                     $" and Plan Start date '{fileRow.PlanStartDate}'" +
                                                     $" and Plan End date '{fileRow.PlanEndDate}'" +
                                                    $" could not be found for Employer Id '{fileRow.EmployerId}'";
                        }
                        else
                        {
                            // take first matched rows - should be only usually
                            var dbRow = matchedRows.First();
                            //
                            DateTime dbRowPlanStartDate = (DateTime)dbRow["planstart"];
                            DateTime dbRowPlanEndDate = (DateTime)dbRow["planend"];

                            //check if end date is after startdate
                            if (!Utils.IsBlank(fileRow.PlanStartDate) && !Utils.IsBlank(fileRow.PlanEndDate) &&
                                Utils.ToDate(fileRow.PlanStartDate) > Utils.ToDate(fileRow.PlanEndDate))
                            {
                                errorMessage +=
                                    $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                                    (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                                    $" Start Date '{fileRow.PlanStartDate}' must be before the Plan End Date '{fileRow.PlanEndDate}' for Employer Id '{fileRow.EmployerId}'";
                            }

                            //check effectivedate is within plan dates
                            if (!Utils.IsBlank(fileRow.EffectiveDate))
                            {
                                if (dbRowPlanStartDate > Utils.ToDate(fileRow.EffectiveDate))
                                {
                                    errorMessage +=
                                        $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                                        (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                                        $" starts only on '{Utils.ToDateString(dbRowPlanStartDate)}' and is not yet started on '{fileRow.EffectiveDate}'";
                                }
                            }

                            if (!Utils.IsBlank(fileRow.EffectiveDate))
                            {
                                if (dbRowPlanEndDate < Utils.ToDate(fileRow.EffectiveDate))
                                {
                                    errorMessage =
                                        $"The AccountTypeID '{fileRow.AccountTypeCode}'" +
                                        (!Utils.IsBlank(fileRow.PlanId) ? $" and Plan ID '{fileRow.PlanId}'" : "") +
                                        $" ended on '{Utils.ToDateString(dbRowPlanEndDate)}' and is no longer active on '{fileRow.EffectiveDate}' ";
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
                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckAlegeusTpaExists(mbi_file_table_stage fileRow, TypedCsvColumn column, EdiRowFormat rowFormat)
        {
            var errorMessage = "";
            var cacheKey = $"{MethodBase.GetCurrentMethod()?.Name}-{fileRow.TpaId}";
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
                        if (Utils.IsBlank(fileRow.TpaId))
                        {
                            errorMessage = "TPA ID cannot be blank. It must always be BENEFL";
                        }
                        else if (fileRow.TpaId != "BENEFL")
                        {
                            errorMessage = $"TPA ID '{fileRow.TpaId}' is invalid. It must always be BENEFL";
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
                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckForDuplicateAlegeusPosting(mbi_file_table_stage fileRow,
                  EdiRowFormat rowFormat = EdiRowFormat.Unknown)
        {
            string errorMessage = "";

            //
            switch (rowFormat)
            {
                case EdiRowFormat.AlegeusEmployeeDeposit:
                    string queryString =
                        $"select * from  [mbi_file_table] " +
                        $" where " +
                        /* if already checked and posted - as we import it in mbi_file_table during create headers*/
                        $" check_type='PreCheck'" +
                        $" and TpaId='{fileRow.TpaId}'" +
                        $" and EmployerId='{fileRow.EmployerId}'" +
                        $" and EmployeeID='{fileRow.EmployeeID}'" +
                        $" and AccountTypeCode='{fileRow.AccountTypeCode}'" +
                        $" and PlanEndDate='{fileRow.PlanEndDate}'" +
                        $" and PlanStartDate='{fileRow.PlanStartDate}'" +
                        $" and EffectiveDate='{fileRow.EffectiveDate}'" +
                        $" and DepositType='{fileRow.DepositType}'" +
                        // todo: flag only if
                        // check amounts also - deposit type ER / EE
                        // EmployeeDepositAmount ?? ??
                        // EmployerDepositAmount ?? ??
                        //$" and DepositType='{fileRow.DepositType}'" +
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
                    errorMessage = $"Potential Duplicate Posting! Was probably posted earlier on '{Utils.ToIsoDateString(prvRow["CreatedAt"])}' as part of file  '{prvRow["mbi_file_name"]}'";
                    break;

                default:
                    break;
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddAlegeusErrorForRow(fileRow, "DuplicatePosting", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        private void AddAlegeusErrorForRow(mbi_file_table_stage fileRow, string errCode, string errMessage, Boolean isWarningOnly = false)
        {
            // add to fileRow so it will be saved back to DB for fileRow by fileRow data

            if (Utils.IsBlank(fileRow.error_code))
            {
                fileRow.error_code = errCode;
            }
            else
            {
                fileRow.error_code = ErrorSeparator + errCode;
            }

            if (Utils.IsBlank(fileRow.error_message))
            {
                fileRow.error_message = errMessage;
            }
            else
            {
                fileRow.error_message = ErrorSeparator + errMessage;
            }

            if (fileRow.error_code.StartsWith(ErrorSeparator))
            {
                fileRow.error_code = fileRow.error_code.Substring(1);
            }

            if (fileRow.error_message.StartsWith(ErrorSeparator))
            {
                fileRow.error_message = fileRow.error_message.Substring(1);
            }

            if (isWarningOnly)
            {
                fileRow.error_code = $"IRRELEVANT_LINE: {fileRow.error_code}";
                fileRow.error_message = $"IRRELEVANT_LINE: {fileRow.error_message}";
            }

            //
            int key = fileRow.source_row_no ?? 0;
            if (this.fileCheckResults.ContainsKey(key))
            {
                this.fileCheckResults[key] = $"{fileRow.source_row_no}: {fileRow.error_code} : {fileRow.error_message}";
            }
            else
            {
                this.fileCheckResults.Add(key, $"{fileRow.source_row_no}: {fileRow.error_code} : {fileRow.error_message}");
            }

        }

        private void CheckAlegeusFile(Dictionary<EdiRowFormat, List<int>> fileFormats)
        {
            // 2. import the file
            string fileName = Path.GetFileName(SrcFilePath) ?? string.Empty;
            FileLogParams?.SetFileNames(Utils.GetUniqueIdFromFileName(fileName), fileName, SrcFilePath, "", "",
                "CheckFile", $"Starting: Check {fileName}", "Starting");

            // split text fileinto multiple files
            Dictionary<EdiRowFormat, object[]> files = new Dictionary<EdiRowFormat, object[]>();

            //
            foreach (EdiRowFormat rowFormat in fileFormats.Keys)
            {
                // get temp file for each format
                string splitFileName = Path.GetTempFileName();
                FileUtils.EnsurePathExists(splitFileName);
                //
                var splitFileWriter = new StreamWriter(splitFileName, false);
                files.Add(rowFormat, new Object[] { splitFileWriter, splitFileName });
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

                    foreach (EdiRowFormat fileFormat2 in fileFormats.Keys)
                    {
                        if (
                            fileFormats[fileFormat2].Contains(rowNo)
                            || Utils.Left(line, 2) == "RA" || Utils.Left(line, 2) == "IA"
                        )
                        {
                            // get temp file for each format
                            var splitFileWriter = (StreamWriter)files[fileFormat2][0];

                            // if there is prvUnwrittenLine it was probably a header line - write to the file that
                            splitFileWriter.WriteLine($"{rowNo}{Import.SourceRowNoSeparator}{line}");
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

        private void CheckAlegeusFile(EdiRowFormat rowFormat, string currentFilePath)
        {
            string tableName = "";
            TypedCsvSchema mappings = new TypedCsvSchema();
            string postImportProc = "";

            // check mappings and type of file (Import or Result)
            Boolean isResultFile = Import.GetAlegeusFileFormatIsResultFile(rowFormat);
            if (isResultFile)
            {
                return;
            }

            // get header type from filename
            var headerType = Import.GetAlegeusHeaderTypeFromFile(currentFilePath, true);

            // get columns for file based on header type
            mappings = Import.GetAlegeusFileImportMappings(rowFormat, headerType);

            //
            tableName = "[dbo].[mbi_file_table_stage]";
            postImportProc = "[dbo].[process_mbi_file_table_stage_import]";

            //todo: need to check changed code
            Import.ImportAlegeusFile(rowFormat, DbConn, currentFilePath, OriginalSrcFilePath, this.hasHeaderRow, FileLogParams, null);

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
            CheckAlegeusFileData(rowFormat, mappings);

            // run post import proc to take data from stage into final table
            string queryString = $"exec {postImportProc};";
            //
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, DbConn, queryString, null,
                FileLogParams?.GetMessageLogParams()
            );
        }

        private void CheckAlegeusFileData(EdiRowFormat rowFormat, TypedCsvSchema mappings)
        {
            // ensure previously cached data is not used so
            // so create a new db context to ensure stale data will NOT be used
            var dbDataProcessing = Vars.dbCtxDataProcessingNew;

            // get all dbRows without caching
            var mbiRows = dbDataProcessing.mbi_file_table_stage
                .OrderBy(fileRow => fileRow.source_row_no)
                .ToList();

            //check each fileRow
            int rowNo = 0;
            foreach (var fileRow in mbiRows)
            {
                rowNo++;
                try
                {
                    // clear any previous values
                    fileRow.error_code = "";
                    fileRow.error_message = "";
                    //
                    this.CheckAlegeusRowData(fileRow, mappings);
                }
                catch (Exception ex)
                {
                    this.AddAlegeusErrorForRow(fileRow, "Error", ex.Message);
                }
            }

            // save any changes
            dbDataProcessing.SaveChanges();
        }

        private void CheckAlegeusRowData(mbi_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            
            var rowFormat = ImpExpUtils.GetAlegeusRowFormat(fileRow.row_type);
            switch (rowFormat)
            {
                // don't check header fileRow
                case EdiRowFormat.AlegeusHeader:
                case EdiRowFormat.AlegeusResultsHeader:
                    return;

                case EdiRowFormat.Unknown:
                    
                    this.AddAlegeusErrorForRow(fileRow, "Invalid Record Type", $"Record Type '{fileRow.row_type}' is invalid", 
                        Import.IsAlegeusIrrelevantLine(fileRow.data_row));
                    return;
            }

            Boolean lineHasError = false;

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
                var orgValue = fileRow.ColumnValue(column.SourceColumn) ?? "";

                // 1. valid Format and general rules check - save corrected value to row
                var formattedValue = EnsureValueIsOfFormatAndMatchesRules(fileRow, column, mappings);

            }

            if (fileRow.hasError)
            {
                return;
            }

            // then check data for each column
            foreach (var column in mappings.Columns)
            {
                // if previous column caused an error, skip other columns
                // if any column is not in a good format, skip further checking as therecould be unexpected errors
                if (lineHasError || fileRow.hasError)
                {
                    break;
                }
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
                        lineHasError = this.CheckAlegeusTpaExists(fileRow, column, rowFormat);
                        break;

                    // ER ID
                    case "employerid":
                        //ER must exist before any Import files are sent
                        lineHasError = this.CheckAlegeusEmployerExists(fileRow, column, rowFormat);
                        break;

                    // EE ID
                    case "employeeid":
                        //EE must exist before any Import files are sent. But for IB files, employee need not exist - he is being added
                        lineHasError = this.CheckAlegeusEmployeeExists(fileRow, column, rowFormat);
                        break;
                    // plan related
                    case "planid":
                    case @"accounttypecode":
                        if (rowFormat == EdiRowFormat.AlegeusEnrollment)
                        {
                            lineHasError = this.CheckAlegeusEmployerPlanExists(fileRow, column, rowFormat);
                        }
                        else if (rowFormat == EdiRowFormat.AlegeusEmployeeDeposit)
                        {
                            lineHasError = this.CheckAlegeusEmployeePlanExists(fileRow, column, rowFormat);
                        }
                        break;

                    default:
                        break;
                }
            }
            // if previous column caused an error, skip other columns
            // if any column is not in a good format, skip further checking as therecould be unexpected errors
            if (!lineHasError && Utils.IsBlank(fileRow.error_message))
            {
                lineHasError = CheckForDuplicateAlegeusPosting(fileRow, rowFormat);
            }
        }

        #endregion CheckAlegeusFile

        #region cacheAlegeusData

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
        }

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

        #endregion cacheAlegeusData

        #region CheckUtils

        public string EnsureValueIsOfFormatAndMatchesRules(mbi_file_table_stage fileRow, TypedCsvColumn column,
            TypedCsvSchema mappings)
        {
            var orgValue = fileRow.ColumnValue(column.SourceColumn) ?? "";
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
                            this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
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
                            this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
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
                            this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
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
                                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
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
                                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format YYYYMMDD HHMMSS. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD HHMMSS. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.YesNo:
                        if (!value.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("No", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.TrueFalse:
                        if (!value.Equals("True", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("False", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
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
                    this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                        $"{column.SourceColumn} must always be one of or ezxactly {column.FixedValue}. '{orgValue}' is not valid");
                }
            }

            // set row column value to the fixed value if it has changed
            if (value != orgValue)
            {
                fileRow.SetColumnValue(column.SourceColumn, value);
                fileRow.data_row = GetDelimitedDataRow(fileRow, mappings);
            }

            // 2. check against GENERAL rules

            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. '{orgValue}' is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                    $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. '{orgValue}' is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number. '{orgValue}' is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. '{orgValue}' is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddAlegeusErrorForRow(fileRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. '{orgValue}' is not valid");
                }
            }

            return value;
        }

        private string GetDelimitedDataRow(mbi_file_table_stage fileRow, TypedCsvSchema mappings)
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

                string fieldValue = fileRow.ColumnValue(column.SourceColumn);
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

        #endregion CheckUtils
    }
}