using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CoreUtils;
using CoreUtils.Classes;
using Org.BouncyCastle.Crypto.Engines;
using Sylvan.Data.Csv;
//using ETLBox.Connection;
//using ETLBox.DataFlow;
//using ETLBox.DataFlow.Connectors;
using SylvanCsvDataReader = Sylvan.Data.Csv.CsvDataReader;

// ReSharper disable All


namespace DataProcessing
{

    public static class Import
    {

        #region Common

        public static readonly string AppendCommasToCsvLine = ",,,,,,,,,,,,,,,,,,,,";

        public static void ImportCrmListFileBulkCopy(DbConnection dbConn, string srcFilePath,
          Boolean hasHeaderRow, string tableName, FileOperationLogParams fileLogParams
          , OnErrorCallback onErrorCallback)
        {
            try
            {
                string fileName = Path.GetFileName(srcFilePath);
                fileLogParams?.SetFileNames(Utils.GetUniqueIdFromFileName(fileName), fileName, srcFilePath, tableName,
                    tableName, "ImportCrmListFileBulkCopy", $"Starting: Import {fileName}", "Starting");

                //
                TypedCsvSchema mappings = new TypedCsvSchema();
                //
                mappings.Add(new TypedCsvColumn("BENCODE", "BENCODE"));
                mappings.Add(new TypedCsvColumn("CRM", "CRM"));
                mappings.Add(new TypedCsvColumn("CRM_email", "CRM_email"));
                mappings.Add(new TypedCsvColumn("emp_services", "emp_services"));
                mappings.Add(new TypedCsvColumn("Primary_contact_name", "Primary_contact_name"));
                mappings.Add(new TypedCsvColumn("Primary_contact_email", "Primary_contact_email"));
                mappings.Add(new TypedCsvColumn("client_start_date", "client_start_date"));

                //
                ImpExpUtils.ImportCsvFileBulkCopy(dbConn, srcFilePath, hasHeaderRow, tableName, mappings,
                    fileLogParams,
                    (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                );
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(srcFilePath, tableName, ex);
                }
                else
                {
                    throw;
                }
            }
        }


        public static string PrefixLineWithEntireLineAndFileName(string srcFilePath, string orgSrcFilePath,
            FileOperationLogParams fileLogParams)
        {
            string fileName = Path.GetFileName(orgSrcFilePath);
            //FileLogParams?.SetFileNames(Utils.GetUniqueIdFromFileName(fileName), fileName, orgSrcFilePath, "", "", $"{ MethodBase.GetCurrentMethod()?.Name}", $"Adding Entire Line as Last Column", "Starting");

            //
            string tempFileName = Path.GetTempFileName();
            FileUtils.EnsurePathExists(tempFileName);
            //
            using var splitFileWriter = new StreamWriter(tempFileName, false);
            using (var inputFile = new StreamReader(srcFilePath))
            {
                string line;
                int rowNo = 0;
                while ((line = inputFile.ReadLine()) != null)
                {
                    rowNo++;
                    // for each header row, ensure enough columns are created as header columns are less than the data columns and then the csv data reader errors when asked for columns that are in the data schema but not in the header itself
                    if (line.Substring(0, 2) == "IA" || line.Substring(0, 2) == "RA")
                    {
                        line += AppendCommasToCsvLine;
                    }
                    else
                    {
                        // add extra cols as sometimes error message column is missing
                        line += AppendCommasToCsvLine;
                    }

                    // add src file path as res_file_name 
                    var newLine = $"{rowNo},{Utils.CsvQuote(line)},{fileName},{line}";
                    splitFileWriter.WriteLine(newLine);
                }
            }

            splitFileWriter.Close();

            return tempFileName;
        }

        public static string GetUniformNameForFile(PlatformType platformType, string srcFilePath)
        {
            string newPath = srcFilePath;
            var srcFileName = Path.GetFileName(srcFilePath);

            string testMarker = "";

            if (Utils.IsTestFile(srcFileName))
            {
                testMarker = $"TEST_";
            }

            string platformCode = "";
            if (platformType == PlatformType.Alegeus)
            {
                platformCode = "AL";
            }
            else if (platformType == PlatformType.Cobra)
            {
                platformCode = "CO";
            }
            else
            {
                platformCode = "UNKNOWN";
            }

            if (platformType == PlatformType.Cobra)
            {
                // we dont have bencodes! just return the filename
                newPath = $"{Path.GetDirectoryName(srcFilePath)}/{Utils.GetUniqueIdFromFileName(srcFileName)}--";
                newPath +=
                    $"{testMarker}{Utils.StripUniqueIdAndHeaderTypeFromFileName(Path.GetFileNameWithoutExtension(srcFileName))}_{platformCode}_{Utils.ToIsoDateString(DateTime.Now)}{Path.GetExtension(srcFilePath)}";
                newPath = FileUtils.FixPath(newPath);

                return newPath;
            }

            var useThisFilePath = srcFilePath;

            // convert excel files to csv to check
            if (FileUtils.IsExcelFile(srcFilePath))
            {
                var csvFilePath = Path.GetTempFileName() + ".csv";

                FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                    Import.GetPasswordsToOpenExcelFiles(srcFilePath),
                    null,
                    null);

                useThisFilePath = csvFilePath;
            }

            string BenCode = "UNKNOWN";
            string recType = "XX";

            var csvDataReaderOptions =
                new CsvDataReaderOptions
                {
                    // also take header row as  data in case there uis no file header
                    HasHeaders = false,
                };

            using var csv = SylvanCsvDataReader.Create(useThisFilePath, csvDataReaderOptions);
            // read till we match header type for line
            int rowNo = 0;
            while (csv.Read())
            {
                rowNo++;
                //
                // col1: Record Type
                var firstColValue = csv.GetString(0);
                if (!Utils.IsBlank(firstColValue) && firstColValue.Length == 2
                                                  && firstColValue.StartsWith("I",
                                                      StringComparison.InvariantCultureIgnoreCase)
                                                  && !firstColValue.Equals("IA",
                                                      StringComparison.InvariantCultureIgnoreCase)
                   )
                {
                    recType = firstColValue.Trim();

                    //
                    // col2: tpaid
                    var secondColValue = csv.GetString(1);

                    // col3: employerid
                    var thirdColValue = csv.GetString(2);
                    if (thirdColValue.StartsWith("BEN", StringComparison.InvariantCultureIgnoreCase))
                    {
                        BenCode = thirdColValue.Trim();
                    }

                    // exit when we have both
                    if (!Utils.IsBlank(BenCode) && !Utils.IsBlank(recType))
                    {
                        break;
                    }
                }
            }
            //
            // remove single quotes

            srcFileName = srcFileName.Replace("--", "_");
            srcFileName = srcFileName.Replace("-", "_");
            srcFileName = srcFileName.Replace(" ", "_");
            srcFileName = srcFileName.Replace("__", "_");
            srcFileName = srcFileName.Replace("'", "");
            srcFileName = srcFileName.Replace(",", "");
            srcFileName = srcFileName.Replace("\"", "");
            srcFileName = srcFileName.Trim();
            //
            /*newPath = $"{Path.GetDirectoryName(srcFilePath)}/{Utils.GetUniqueIdFromFileName(srcFileName)}--";*/
            newPath = $"{Path.GetDirectoryName(srcFilePath)}/";
            newPath +=
                $"{testMarker}{BenCode}_{recType}_{platformCode}_{Utils.ToIsoDateString(DateTime.Now)}{Path.GetExtension(srcFilePath)}";
            newPath = FileUtils.FixPath(newPath);

            return newPath;
        }

        public static Dictionary<string, string> GetPasswordsToOpenExcelFiles(string srcFilePath)
        {
            Dictionary<string, string> passwords = new Dictionary<string, string>();
            passwords.Add("", "");
            //
            passwords.Add("Building Maintenance Service", "benflex");
            passwords.Add("LLC-Vornado Realty Trust", "benflex");
            passwords.Add("Southern Bank of Tennessee", "Christmas2021!");
            passwords.Add("Columbia Bank", "Winter2022!");
            passwords.Add("Instinet Group Inc", "benefits2022");
            passwords.Add("Trilogy Federal", "Trilogy2022");
            passwords.Add("LLCView Account Hierarchy", "Trilogy2022");
            passwords.Add("Standard New York Inc.", "Clarity_2022");
            passwords.Add("ESS Management Corp", "4817 -- EIN no.");
            passwords.Add("The Studio Museum in Harlem", "Clarity1!");
            passwords.Add("HR Acuity", "HRAHSA#1");
            passwords.Add("Population Council", "lhpclar");
            passwords.Add("Archwell Holdings", "Clarity1!");
            passwords.Add("National Capitol Contracting LLC", "Clarity1!");


            //ToDo: get list of password so that we can open password protected files
            return passwords;
        }
        public static void MoveFileToPlatformRejectsFolder(string srcFilePath, Exception ex)
        {
            MoveFileToPlatformRejectsFolder(srcFilePath, ex.ToString());
        }

        public static void MoveFileToPlatformRejectsFolder(string srcFilePath, string rejectMessage)
        {
            Vars Vars = new Vars();
            PlatformType platformType;
            try
            {
                platformType = Import.GetPlatformTypeForFile(srcFilePath);
            }
            catch (Exception ex)
            {
                // presume Alegeus
                platformType = PlatformType.Alegeus;
            }

            string destDir;
            switch (platformType)
            {
                case PlatformType.Alegeus:
                    destDir = Vars.alegeusFilesRejectsPath;
                    break;

                case PlatformType.Cobra:
                    destDir = Vars.cobraFilesRejectsPath;
                    break;

                default:
                    destDir = Vars.unknownFilesRejectsPath;
                    break;
            }

            MoveFileToPlatformRejectsFolder(srcFilePath, rejectMessage, destDir);
        }

        public static void MoveFileToPlatformRejectsFolder(string srcFilePath, string rejectMessage, string destFolder)
        {
            var file = srcFilePath;
            //
            if (Utils.IsBlank(destFolder))
            {
                destFolder = Path.GetDirectoryName(file);
            }

            string rejectFilePath = $"{destFolder}/{Path.GetFileName(file)}";
            FileUtils.MoveFile(file, rejectFilePath, null, null);

            /*export .err file */
            string errorFilePath = $"{destFolder}/{Path.GetFileName(file)}.error";
            FileUtils.WriteToFile(errorFilePath, rejectMessage, null);
        }

        public static PlatformType GetPlatformTypeForFile(string srcFilePath)
        {
            // convert excel file
            if (FileUtils.IsExcelFile(srcFilePath))
            {
                var csvFilePath = Path.GetTempFileName() + ".csv";

                FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                    Import.GetPasswordsToOpenExcelFiles(srcFilePath),
                    null,
                    null);

                srcFilePath = csvFilePath;
            }
            //
            if (IsCobraImportFile(srcFilePath))
            {
                return PlatformType.Cobra;
            }
            else if (IsAlegeusImportFile(srcFilePath))
            {
                return PlatformType.Alegeus;

            }
            else
            {
                // presume Alegeus
                return PlatformType.Alegeus;
            }
        }
        #endregion

        #region BrokerCommissions

        public static TypedCsvSchema GetBrokerCommissionFileImportMappings(EdiFileFormat fileFormat, HeaderType headerType, Boolean forImport = true)
        {
            var mappings = new TypedCsvSchema();

            switch (fileFormat)
            {
                /////////////////////////////////////////////////////
                // IB, RB 
                case EdiFileFormat.BrokerCommissionQBRawData:
                    // Type	Date	Num	Name	Memo	Agent	Qty	Sales Price	Amount	Open Balance

                    // for all
                    mappings.Add(new TypedCsvColumn("Type", "Type", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Date", "Date", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Num", "Num", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Name", "Name", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Memo", "Memo", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Agent", "Agent", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Qty", "Qty", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Sales Price", "Sales Price", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Amount", "Amount", FormatType.String, null, 0, 0, 0, 0, ""));
                    mappings.Add(new TypedCsvColumn("Open Balance", "Open Balance", FormatType.String, null, 0, 0, 0, 0, ""));

                    break;

                default:
                    var message =
                                 $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : fileFormat : {fileFormat.ToDescription()} is invalid";
                    throw new Exception(message);

            }

            // entrire line
            return mappings;
        }

        public static void ImportBrokerCommissionFile(DbConnection dbConn, string srcFilePath,
            Boolean hasHeaderRow, FileOperationLogParams fileLogParams
            , OnErrorCallback onErrorCallback)
        {
            //
            EdiFileFormat fileFormat = EdiFileFormat.BrokerCommissionQBRawData;


            // 2. import the file
            try
            {
                // check mappinsg and type opf file (Import or Result)

                var headerType = HeaderType.NotApplicable;
                TypedCsvSchema mappings = GetBrokerCommissionFileImportMappings(fileFormat, headerType);
                Boolean isResultFile = false;

                //
                //var newPath =PrefixLineWithEntireLineAndFileName(srcFilePath, orgSrcFilePath, fileLogParams);
                var newPath = srcFilePath;
                //
                string tableName = isResultFile ? "[dbo].Import_OCT" : "[dbo].Import_OCT";
                string postImportProc = isResultFile
                    ? ""
                    : "";

                // truncate staging table
                DbUtils.TruncateTable(dbConn, tableName,
                    fileLogParams?.GetMessageLogParams());

                // import the file with bulk copy
                ImpExpUtils.ImportCsvFileBulkCopy(dbConn, newPath, hasHeaderRow, tableName, mappings,
                    fileLogParams, onErrorCallback
                );

                if (!Utils.IsBlank(postImportProc))
                {
                    //
                    // run postimport query to take from staging to final table
                    string queryString = $"exec {postImportProc};";
                    DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                        fileLogParams?.GetMessageLogParams());
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(srcFilePath, fileFormat.ToDescription(), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        #endregion

        #region Alegeus

        private static readonly Regex regexALImportHeader = new Regex("IA,");
        private static readonly Regex regexALImportRecType = new Regex("I[B-Z],");
        private static readonly Regex regexALExportHeader = new Regex("RA,");
        private static readonly Regex regexALExportRecType = new Regex("R[B-Z],");

        public static TypedCsvSchema GetAlegeusFileImportMappings(EdiFileFormat fileFormat, HeaderType headerType,
            Boolean forImport = true)
        {
            var mappings = new TypedCsvSchema();

            Boolean isResultFile = GetAlegeusFileFormatIsResultFile(fileFormat);

            // add src file path as res_file_name and mbi_file_name
            if (forImport)
            {
                // tracks rowNo in submitted file as a single file can jhave than format lines
                mappings.Add(new TypedCsvColumn("source_row_no", "source_row_no", FormatType.String, null, 0, 0, 0, 0));
                //
                if (isResultFile)
                {
                    //
                    // add duplicated entire line as first column to ensure it is parsed correctly
                    mappings.Add(new TypedCsvColumn("error_row", "error_row", FormatType.String, null, 0, 0, 0, 0));
                    // source filename
                    mappings.Add(new TypedCsvColumn("res_file_name", "res_file_name", FormatType.String, null, 0, 0, 0,
                        0));
                }
                else
                {
                    // add duplicated entire line as first column to ensure it isd parsed correctly
                    mappings.Add(new TypedCsvColumn("data_row", "data_row", FormatType.String, null, 0, 0, 0, 0));
                    // source filename
                    mappings.Add(new TypedCsvColumn("mbi_file_name", "mbi_file_name", FormatType.String, null, 0, 0, 0,
                        0));
                }
            }

            // the row_type
            mappings.Add(new TypedCsvColumn("row_type", "row_type", FormatType.String, null, 0, 0, 0, 0));

            switch (fileFormat)
            {
                /////////////////////////////////////////////////////
                // IB, RB 
                case EdiFileFormat.AlegeusDemographics:
                case EdiFileFormat.AlegeusResultsDemographics:
                    //
                    if (fileFormat == EdiFileFormat.AlegeusDemographics)
                    {
                        // for all
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("EmployeeSocialSecurityNumber", "EmployeeSocialSecurityNumber",
                            FormatType.AlphaNumeric, null, 9, 9, 0, 0));
                        mappings.Add(new TypedCsvColumn("LastName", "LastName", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("FirstName", "FirstName", FormatType.AlphaOnly, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("MiddleInitial", "MiddleInitial", FormatType.String, null, 0, 0,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("BirthDate", "BirthDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("Phone", "Phone", FormatType.Phone, null, 0, 10, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine1", "AddressLine1", FormatType.AlphaNumeric, null,
                            0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine2", "AddressLine2", FormatType.AlphaNumeric, null,
                            0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("City", "City", FormatType.AlphaOnly, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("State", "State", FormatType.AlphaOnly, null, 2, 2, 0, 0));
                        mappings.Add(new TypedCsvColumn("Zip", "Zip", FormatType.Zip, null, 0, 5, 0, 0));
                        mappings.Add(new TypedCsvColumn("Country", "Country", FormatType.AlphaOnly, "US", 2, 2, 0, 0));
                        mappings.Add(new TypedCsvColumn("Email", "Email", FormatType.Email, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("MobileNumber", "MobileNumber", FormatType.Phone, null, 0, 10,
                            0, 0));
                        // Note: EmployeeStatus: must be populated
                        mappings.Add(new TypedCsvColumn("EmployeeStatus", "EmployeeStatus", FormatType.Integer, "2|5",
                            1, 1, 0,
                            0, "2"));

                        // for New & Segmented
                        mappings.Add(new TypedCsvColumn("EligibilityDate", "EligibilityDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("TerminationDate", "TerminationDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("AlternateId", "AlternateId", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("Division", "Division", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("Class", "Class", FormatType.String, null, 0, 0, 0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsDemographics)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                // IC, RC
                case EdiFileFormat.AlegeusEnrollment:
                case EdiFileFormat.AlegeusResultsEnrollment:

                    //
                    if (fileFormat == EdiFileFormat.AlegeusEnrollment)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumeric, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("AccountTypeCode", "AccountTypeCode", FormatType.AlphaNumeric,
                            null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));

                        // Note: dont default
                        mappings.Add(new TypedCsvColumn("AccountStatus", "AccountStatus", FormatType.Integer, "2|5", 1,
                            1, 0, 0 /*, "2"*/));
                        mappings.Add(new TypedCsvColumn("OriginalPrefunded", "OriginalPrefunded", FormatType.Double,
                            null, 0, 0,
                            0, 0));

                        // note: Old does not have this column
                        if (headerType != HeaderType.Old)
                        {
                            mappings.Add(new TypedCsvColumn("OngoingPrefunded", "OngoingPrefunded", FormatType.Double,
                                null, 0, 0,
                                0, 0));
                        }

                        mappings.Add(new TypedCsvColumn("EmployeePayPeriodElection",
                            "EmployeePayPeriodElection", FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerPayPeriodElection",
                            "EmployerPayPeriodElection", FormatType.Double, null, 0, 0, 0, 0));

                        mappings.Add(new TypedCsvColumn("EffectiveDate", "EffectiveDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("TerminationDate", "TerminationDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));

                        if (headerType == HeaderType.SegmentedFunding)
                        {
                            mappings.Add(new TypedCsvColumn("AccountSegmentId", "AccountSegmentId", FormatType.String,
                                null, 0, 0, 0,
                                0));
                        }
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsEnrollment)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumeric, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        //mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null,0, 0, 0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                // ID, RD
                case EdiFileFormat.AlegeusDependentDemographics:
                case EdiFileFormat.AlegeusResultsDependentDemographics:

                    //
                    if (fileFormat == EdiFileFormat.AlegeusDependentDemographics)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("LastName", "LastName", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("FirstName", "FirstName", FormatType.AlphaOnly, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("MiddleInitial", "MiddleInitial", FormatType.String, null, 0, 0,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("Phone", "Phone", FormatType.Phone, null, 0, 10, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine1", "AddressLine1", FormatType.AlphaNumeric, null,
                            0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine2", "AddressLine2", FormatType.AlphaNumeric, null,
                            0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("City", "City", FormatType.AlphaOnly, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("State", "State", FormatType.AlphaOnly, null, 2, 2, 0, 0));
                        mappings.Add(new TypedCsvColumn("Zip", "Zip", FormatType.Zip, null, 0, 5, 0, 0));
                        mappings.Add(new TypedCsvColumn("Country", "Country", FormatType.AlphaOnly, "US", 2, 2, 0, 0));
                        mappings.Add(new TypedCsvColumn("BirthDate", "BirthDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("Relationship", "Relationship", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsDependentDemographics)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));

                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                // IE, RE
                case EdiFileFormat.AlegeusDependentLink:
                case EdiFileFormat.AlegeusResultsDependentLink:

                    //
                    if (fileFormat == EdiFileFormat.AlegeusDependentLink)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("AccountTypeCode", "AccountTypeCode", FormatType.AlphaNumeric,
                            null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("DeleteAccount", "DeleteAccount", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsDependentLink)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumeric, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));

                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;
                /////////////////////////////////////////////////////
                // IF, RF
                case EdiFileFormat.AlegeusCardCreation:
                case EdiFileFormat.AlegeusResultsCardCreation:

                    //
                    if (fileFormat == EdiFileFormat.AlegeusCardCreation)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("IssueCard", "IssueCard", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine1",
                            "AddressLine1", FormatType.AlphaNumeric, null, 0, 0, 0, 0)); // Shipping Address Code
                        mappings.Add(new TypedCsvColumn("AddressLine2",
                            "AddressLine2", FormatType.AlphaNumeric, null, 0, 0, 0, 0)); // Shipping Method Code
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsDependentLink)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("IssueCard", "IssueCard", FormatType.String, null, 0, 0, 0, 0));

                        //?? todo: to verify next column mapping
                        mappings.Add(new TypedCsvColumn("AddressLine1", "AddressLine1", FormatType.AlphaNumeric, null,
                            0, 0, 0,
                            0)); // Shipping Address Code
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                //  IH, RH
                case EdiFileFormat.AlegeusEmployeeDeposit:
                case EdiFileFormat.AlegeusResultsEmployeeDeposit:
                    //
                    if (fileFormat == EdiFileFormat.AlegeusEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("AccountTypeCode", "AccountTypeCode", FormatType.AlphaNumeric,
                            null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        // Note: do NOT default deposit type 
                        mappings.Add(new TypedCsvColumn("DepositType", "DepositType", FormatType.Integer, "1", 1, 1, 0,
                            0 /*, "1"*/));
                        mappings.Add(new TypedCsvColumn("EmployeeDepositAmount", "EmployeeDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerDepositAmount", "EmployerDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EffectiveDate", "EffectiveDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        // ? planid??
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumeric, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DepositType", "DepositType", FormatType.Integer, "1", 1, 1, 0,
                            0, "1"));
                        mappings.Add(new TypedCsvColumn("EmployeeDepositAmount", "EmployeeDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerDepositAmount", "EmployerDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;
                /////////////////////////////////////////////////////
                // LATER: FileChecker: mapping for II,RI: take from FinanceApp?
                //  II, RI
                case EdiFileFormat.AlegeusEmployeeCardFees:
                case EdiFileFormat.AlegeusResultsEmployeeCardFees:
                    //
                    if (fileFormat == EdiFileFormat.AlegeusEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        // ? planid??
                        mappings.Add(new TypedCsvColumn("AccountTypeCode", "AccountTypeCode", FormatType.AlphaNumeric,
                            null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DepositType", "DepositType", FormatType.Integer, "1", 1, 1, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeDepositAmount", "EmployeeDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerDepositAmount", "EmployerDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EffectiveDate", "EffectiveDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        // ? planid??
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumeric, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DepositType", "DepositType", FormatType.Integer, "1", 1, 1, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeDepositAmount", "EmployeeDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerDepositAmount", "EmployerDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                // IZ, RZ
                case EdiFileFormat.AlegeusEmployeeHrInfo:
                case EdiFileFormat.AlegeusResultsEmployeeHrInfo:
                    //
                    if (fileFormat == EdiFileFormat.AlegeusEmployeeHrInfo)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("EligibilityDate", "EligibilityDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("TerminationDate", "TerminationDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("Division", "Division", FormatType.String, null, 0, 0, 0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;
            }

            // entrire line
            return mappings;
        }

        public static Boolean IsAlegeusImportRecLine(string text)
        {
            return regexALImportRecType.IsMatch(text?.Trim().Substring(0, 3));
        }
        public static Boolean IsAlegeusImportHeaderLine(string text)
        {
            return regexALImportHeader.IsMatch(text?.Trim().Substring(0, 3));
        }

        public static Boolean IsAlegeusExportRecLine(string text)
        {
            return regexALExportRecType.IsMatch(text?.Trim().Substring(0, 3));
        }
        public static Boolean IsAlegeusExportHeaderLine(string text)
        {
            return regexALExportHeader.IsMatch(text?.Trim().Substring(0, 3));
        }

        public static HeaderType GetAlegeusHeaderTypeFromFile(string srcFilePath)
        {
            var headerType = HeaderType.NotApplicable;
            // convert excel files to csv to check
            if (FileUtils.IsExcelFile(srcFilePath))
            {
                var csvFilePath = Path.GetTempFileName() + ".csv";

                FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                    Import.GetPasswordsToOpenExcelFiles(srcFilePath),
                    null,
                    null);

                srcFilePath = csvFilePath;
            }

            int rowNo = 0;
            using var inputFile = new StreamReader(srcFilePath);
            while (true)
            {
                string line = inputFile.ReadLine()!;
                rowNo++;

                if (line == null)
                {
                    if (headerType == HeaderType.NotApplicable)
                    {
                        string message =
                            $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Could Not Determine Header Type for  {srcFilePath}";
                        throw new IncorrectFileFormatException(message);
                    }

                    return headerType;
                }

                string[] columns = ImpExpUtils.GetCsvColumnsFromText(line);
                var columnCount = columns.Length;
                if (columnCount == 0)
                {
                    continue;
                }

                var expectedMappingColumnsCount = 0;
                var firstColValue = columns[0];
                var fileFormat = ImpExpUtils.GetAlegeusRowFormat(firstColValue);

                // we only care for Import Headers
                if (Import.GetAlegeusFileFormatIsResultFile(fileFormat))
                {
                    return HeaderType.New;
                }


                // get file format
                // note: we are also detecting header type from conettn for prev Own and NoChange header folders
                switch (fileFormat)
                {
                    // ignore unknown and header lines
                    case EdiFileFormat.Unknown:
                    case EdiFileFormat.AlegeusHeader:
                    case EdiFileFormat.AlegeusResultsHeader:
                        continue;

                    case EdiFileFormat.AlegeusDemographics:
                        switch (columnCount)
                        {
                            case 19:
                                headerType = HeaderType.Old;
                                expectedMappingColumnsCount = columnCount;
                                break;

                            case 24:
                                headerType = HeaderType.New;
                                ;
                                expectedMappingColumnsCount = columnCount;
                                break;
                                // can also be segmented header!
                        }

                        ;
                        break;

                    case EdiFileFormat.AlegeusEnrollment:
                        switch (columnCount)
                        {
                            case 14:
                                headerType = HeaderType.Old;
                                expectedMappingColumnsCount = columnCount;
                                break;

                            case 15:
                                headerType = HeaderType.New;
                                expectedMappingColumnsCount = columnCount;
                                break;

                            case 16:
                                headerType = HeaderType.SegmentedFunding;
                                expectedMappingColumnsCount = columnCount;
                                break;
                        }

                        ;
                        break;

                    case EdiFileFormat.AlegeusEmployeeDeposit:
                        switch (columnCount)
                        {
                            case 11:
                                headerType = HeaderType.Old;
                                expectedMappingColumnsCount = columnCount;
                                break;

                            default:
                                headerType = HeaderType.NotApplicable;
                                break;
                        }

                        ;
                        break;

                    default:
                        headerType = HeaderType.New;
                        break;
                }

                if (expectedMappingColumnsCount <= 0)
                {
                    // what mappings do we expect for this format
                    var mappings = GetAlegeusFileImportMappings(fileFormat, HeaderType.NotApplicable, true);

                    // check we have the exact columns that we expected
                    expectedMappingColumnsCount =
                        mappings.Count /*subtract the extra columns we add before iomport to csv files*/ - 3;
                    if (columnCount != expectedMappingColumnsCount)
                    {
                        string message =
                            $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source file with format {fileFormat.ToDescription()} has {columnCount} columns instead of the expected {expectedMappingColumnsCount}. Could Not Determine Header Type for {srcFilePath}";
                        throw new IncorrectFileFormatException(message);
                    }
                }

                return headerType;
            }
        }

        public static Boolean GetAlegeusFileFormatIsResultFile(EdiFileFormat fileFormat)
        {
            String fileFormatDesc = fileFormat.ToDescription();
            if (fileFormatDesc.IndexOf("Result", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        public static Boolean IsAlegeusImportFile(string srcFilePath)
        {
            using var inputFile = new StreamReader(srcFilePath);
            string line;
            while ((line = inputFile.ReadLine()!) != null)
            {
                if (IsAlegeusImportRecLine(line))
                {
                    return true;
                }
            }

            return false;
        }

        public static void ImportAlegeusFile(DbConnection dbConn, string srcFilePath,
                  Boolean hasHeaderRow, FileOperationLogParams fileLogParams
                  , OnErrorCallback onErrorCallback)
        {
            //
            Dictionary<EdiFileFormat, List<int>> fileFormats = ImpExpUtils.GetAlegeusFileFormats(
                srcFilePath, hasHeaderRow, fileLogParams
            );

            // file may contain only a header...
            if (fileFormats.Count == 0)
            {
                Boolean isResultFile = GetAlegeusFileFormatIsResultFile(fileFormats.Keys.First());

                // import as plain text file
                ImpExpUtils.ImportSingleColumnFlatFile(dbConn, srcFilePath, Path.GetFileName(srcFilePath),
                    isResultFile ? "res_file_table_stage" : "mbi_file_table_stage",
                    isResultFile ? "res_file_name" : "mbi_file_name",
                    isResultFile ? "error_row" : "data_row",
                    (filePath1, rowNo, line) =>
                    {
                        // we only import valid import lines 
                        Boolean import = true;
                        return import;
                    },
                    fileLogParams,
                    onErrorCallback
                );

                return;
            }

            // 2. import the file
            ImportAlegeusFile(fileFormats, dbConn, srcFilePath, hasHeaderRow, fileLogParams,
                onErrorCallback);
        }

        //doesnt work - mappings not clear
        public static void ImportAlegeusFile(Dictionary<EdiFileFormat, List<int>> fileFormats,
            DbConnection dbConn, string srcFilePath, Boolean hasHeaderRow, FileOperationLogParams fileLogParams
            , OnErrorCallback onErrorCallback)
        {
            try
            {
                string fileName = Path.GetFileName(srcFilePath);
                fileLogParams?.SetFileNames(Utils.GetUniqueIdFromFileName(fileName), fileName, srcFilePath, "", "",
                    "ImportAlegeusFile", $"Starting: Import {fileName}", "Starting");

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
                using (var inputFile = new StreamReader(srcFilePath))
                {
                    int rowNo = 0;
                    string line;
                    while ((line = inputFile.ReadLine()) != null)
                    {
                        rowNo++;

                        foreach (EdiFileFormat fileFormat2 in fileFormats.Keys)
                        {
                            if (fileFormats[fileFormat2].Contains(rowNo)
                                || (line?.Substring(0, 2) == "RA" || line?.Substring(0, 2) == "IA"))
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
                    ImportAlegeusFile(fileFormat3, dbConn, (string)files[fileFormat3][1], srcFilePath,
                        hasHeaderRow, fileLogParams, onErrorCallback);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(srcFilePath, fileFormats.Values.ToString(), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        private static void ImportAlegeusFile(EdiFileFormat fileFormat, DbConnection dbConn,
            string srcFilePath, string orgSrcFilePath, Boolean hasHeaderRow, FileOperationLogParams fileLogParams
            , OnErrorCallback onErrorCallback)
        {
            try
            {
                // check mappinsg and type opf file (Import or Result)

                var headerType = GetAlegeusHeaderTypeFromFile(srcFilePath);
                TypedCsvSchema mappings = GetAlegeusFileImportMappings(fileFormat, headerType);
                Boolean isResultFile = GetAlegeusFileFormatIsResultFile(fileFormat);

                //
                var newPath =
                    PrefixLineWithEntireLineAndFileName(srcFilePath, orgSrcFilePath, fileLogParams);
                //
                string tableName = isResultFile ? "[dbo].[res_file_table_stage]" : "[dbo].[mbi_file_table_stage]";
                string postImportProc = isResultFile
                    ? "dbo.process_res_file_table_stage_import"
                    : "dbo.process_mbi_file_table_stage_import";

                // truncate staging table
                DbUtils.TruncateTable(dbConn, tableName,
                    fileLogParams?.GetMessageLogParams());

                // import the file with bulk copy
                ImpExpUtils.ImportCsvFileBulkCopy(dbConn, newPath, hasHeaderRow, tableName, mappings,
                    fileLogParams, onErrorCallback
                );

                //
                // run postimport query to take from staging to final table
                string queryString = $"exec {postImportProc};";
                DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                    fileLogParams?.GetMessageLogParams());
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(orgSrcFilePath, fileFormat.ToDescription(), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        #endregion

        #region COBRA

        public static Boolean IsCobraImportFile(string srcFilePath)
        {
            // COBRA files, first line starts with [VERSION],
            string contents = FileUtils.GetFlatFileContents(srcFilePath, 1);

            if (contents.Contains("[VERSION],"))
            {
                return true;
            }


            return false;
        }
        public static Boolean IsCobraImportQbFile(string srcFilePath)
        {
            Boolean isCobraFile = IsCobraImportFile(srcFilePath);

            // toDo: how to distincguih QB and NPM files?
            return isCobraFile;
        }

        public static CobraFileTypeAndVersionNo GetCobraFileTypeAndVersionNoFromFile(string srcFilePath)
        {
            CobraFileTypeAndVersionNo fileTypeAndVersionNo = new CobraFileTypeAndVersionNo();

            // convert excel files to csv to check
            if (FileUtils.IsExcelFile(srcFilePath))
            {
                var csvFilePath = Path.GetTempFileName() + ".csv";

                FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                    Import.GetPasswordsToOpenExcelFiles(srcFilePath),
                    null,
                    null);

                srcFilePath = csvFilePath;
            }

            int rowNo = 0;
            string firstLine = null;
            string secondLine = null;

            using var inputFile = new StreamReader(srcFilePath);
            while (true)
            {
                string line = inputFile.ReadLine()!;
                rowNo++;

                // if we reached end of file, return what we got
                if (line == null)
                {
                    return fileTypeAndVersionNo;
                }

                string[] columns = ImpExpUtils.GetCsvColumnsFromText(line);
                var columnCount = columns.Length;
                if (columnCount == 0)
                {
                    continue;
                }

                // take first non blank line with 2 columns
                if (Utils.IsBlank(firstLine) && columnCount == 2 && columns[0].ToUpperInvariant() == "[VERSION]")
                {
                    fileTypeAndVersionNo.versionNo = columns[1];
                }

                // take first non blank line mathing basic entity type
                if (Utils.IsBlank(secondLine) && columns[0].ToUpperInvariant() == "[QB]")
                {
                    fileTypeAndVersionNo.fileType = CobraFileType.Qb;
                }
                else if (Utils.IsBlank(secondLine) && columns[0].ToUpperInvariant() == "[SPM]")
                {
                    fileTypeAndVersionNo.fileType = CobraFileType.Spm;
                }
                else if (Utils.IsBlank(secondLine) && columns[0].ToUpperInvariant() == "[NPM]")
                {
                    fileTypeAndVersionNo.fileType = CobraFileType.Npm;
                }

                if (!Utils.IsBlank(fileTypeAndVersionNo.versionNo) && fileTypeAndVersionNo.fileType != CobraFileType.Unknown)
                {
                    return fileTypeAndVersionNo;
                }
            }
        }

        public static TypedCsvSchema GetCobraFileImportMappings(CobraFileTypeAndVersionNo fileTypeAndVersionNo, string rowType, Boolean forImport = true)
        {
            var mappings = new TypedCsvSchema();

            Boolean isResultFile = false;// GetAlegeusFileFormatIsResultFile(fileFormat);

            // add src file path as res_file_name and mbi_file_name
            if (forImport)
            {
                // tracks rowNo in submitted file as a single file can jhave than format lines
                mappings.Add(new TypedCsvColumn("source_row_no", "source_row_no", FormatType.String, null, 0, 0, 0, 0));
                //
                if (isResultFile)
                {
                    //
                    // add duplicated entire line as first column to ensure it is parsed correctly
                    mappings.Add(new TypedCsvColumn("error_row", "error_row", FormatType.String, null, 0, 0, 0, 0));

                    // source filename
                    mappings.Add(new TypedCsvColumn("cobra_res_file_name", "cobra_res_file_name", FormatType.String, null, 0, 0, 0,
                        0));

                }
                else
                {
                    // add duplicated entire line as first column to ensure it isd parsed correctly
                    mappings.Add(new TypedCsvColumn("data_row", "data_row", FormatType.String, null, 0, 0, 0, 0));
                    // source filename
                    mappings.Add(new TypedCsvColumn("cobra_import_file_name", "cobra_import_file_name", FormatType.String, null, 0, 0, 0,
                        0));
                }
            }

            // the row_type
            mappings.Add(new TypedCsvColumn("row_type", "row_type", FormatType.String, null, 0, 0, 0, 0));

            switch (rowType)
            {
                case "[VERSION]":
                    mappings.Add(new CobraTypedCsvColumn("VersionNumber", FormatType.Integer, 0, 1, "Use “1.2” for this import specification"));
                    break;
                case "[QB]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("ClientName", FormatType.String, 100, 1, "The unique Client name assigned in COBRA & Direct Billing "));
                            mappings.Add(new CobraTypedCsvColumn("ClientDivisionName", FormatType.String, 50, 1, "The unique Client Division name assigned in COBRA & Direct Billing. If there are no Divisions, then use the ClientName."));
                            mappings.Add(new CobraTypedCsvColumn("Salutation", FormatType.String, 35, 0, "MR, MRS, MS, MISS, DR"));
                            mappings.Add(new CobraTypedCsvColumn("FirstName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("MiddleInitial", FormatType.String, 1, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("LastName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 1, "Social Security Number"));
                            mappings.Add(new CobraTypedCsvColumn("IndividualID", FormatType.String, 50, 0, "Optional, used to store Employee ID #s or any other type of secondary identification"));
                            mappings.Add(new CobraTypedCsvColumn("Email", FormatType.Email, 100, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone2", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address1", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address2", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("City", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("StateOrProvince", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("PostalCode", FormatType.String, 35, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Country", FormatType.String, 50, 0, "Leave blank if the QB resides in the USA"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumAddressSameAsPrimary", FormatType.CobraYesNo, 0, 1, "Always set to TRUE"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumAddress1", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumAddress2", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumCity", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumStateOrProvince", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumPostalCode", FormatType.String, 35, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumCountry", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("Sex", FormatType.String, 1, 1, "F, M, U", "F|M|U"));
                            mappings.Add(new CobraTypedCsvColumn("DOB", FormatType.CobraDate, 0, 1, "Date of Birth – needed for age based plans and also for the Medicare Letter"));
                            mappings.Add(new CobraTypedCsvColumn("TobaccoUse", FormatType.String, 35, 1, "YES, NO, UNKNOWN"));
                            mappings.Add(new CobraTypedCsvColumn("EmployeeType", FormatType.String, 35, 1, "FTE, PTE, H1B, CONSULTANT, SABBATICAL, PROBATIONARY, CONTINGENT, TELECOMMUTING, INTERN, GROUPLEADER, ASSOCIATE, PARTNER, UNKNOWN"));
                            mappings.Add(new CobraTypedCsvColumn("EmployeePayrollType", FormatType.String, 35, 1, "EXEMPT, NONEXEMPT, UNKNOWN"));
                            mappings.Add(new CobraTypedCsvColumn("YearsOfService", FormatType.Integer, 0, 0, "Not used currently and only informational"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumCouponType", FormatType.String, 35, 1, "PREMIUMNOTICE, COUPONBOOK, NONE"));
                            mappings.Add(new CobraTypedCsvColumn("UsesHCTC", FormatType.CobraYesNo, 0, 1, "TRUE if this QB uses the Health Care Tax Credit (HCTC) system"));
                            mappings.Add(new CobraTypedCsvColumn("Active", FormatType.CobraYesNo, 0, 1, "Should always be set to TRUE"));
                            mappings.Add(new CobraTypedCsvColumn("AllowMemberSSO", FormatType.CobraYesNo, 1, 1, "TRUE or FALSE"));
                            mappings.Add(new CobraTypedCsvColumn("BenefitGroup", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("AccountStructure", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("ClientSpecificData", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("SSOIdentifier", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("PlanCategory", FormatType.String, 0, 0, "Separate multiple entries with a comma (each individual entry cannot exceed 100 characters). Angle Brackets “< >” are not permitted."));
                            //
                            break;
                    }
                    break;
                case "[QBEVENT]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("EventType", FormatType.String, 35, 1, "DIVORCELEGASSEPARATION,DEATH,INELIGIBLEDEPENDENT,MEDICARE,TERMINATION,RETIREMENT,REDUCTIONINHOURS-STATUSCHANGE,REDUCTIONINFORCE,BANKRUPTCY,STATECONTINUATION,LOSSOFELIGIBILITY,REDUCTIONINHOURS-ENDOFLEAVE,WORKSTOPPAGE,USERRA-TERMINATION,USERRA-REDUCTIONINHOURS,INVOLUNTARYTERMINATION,TERMINATIONWITHSEVERANCE,RETIREEBANKRUPTCY"));
                            mappings.Add(new CobraTypedCsvColumn("EventDate", FormatType.CobraDate, 0, 0, "The qualifying event date that the event type occurred on. Do not adjust for plan benefit termination types, just use the actual date of the event."));
                            mappings.Add(new CobraTypedCsvColumn("EnrollmentDate", FormatType.CobraDate, 0, 1, "Original enrollment date of the member’s current medical plan - used for HIPAA certificate to show length of continuous coverage"));
                            mappings.Add(new CobraTypedCsvColumn("EmployeeSSN", FormatType.SSN, 9, 1, "The original employee’s SSN. Required if the event type is a dependent type event, such as DEATH, DIVORCELEGALSEPARATION, INELIGIBLEDEPENDENT or MEDICARE."));
                            mappings.Add(new CobraTypedCsvColumn("EmployeeName", FormatType.String, 100, 1, "The original employee’s name. Required if the event type is a dependent type event, such as DEATH, DIVORCELEGALSEPARATION, INELIGIBLEDEPENDENT or MEDICARE."));
                            mappings.Add(new CobraTypedCsvColumn("SecondEventOriginalFDOC", FormatType.CobraDate, 1, 1, "DEPRECATED – any value will be ignored"));
                            //
                            break;
                    }
                    break;
                //

                case "[QBLEGACY]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("DateSpecificRightsNoticeWasPrinted", FormatType.CobraDate, 0, 1, "The date that the original Specific Rights Notice was printed (or postmarked)"));
                            mappings.Add(new CobraTypedCsvColumn("PostmarkDateOfElection", FormatType.CobraDate, 0, 0, "If the QB has elected, then the date of that election (or the postmark of the election form receipt) should be entered as the postmark date of election form"));
                            mappings.Add(new CobraTypedCsvColumn("IsPaidThroughLastDayOfCOBRA", FormatType.CobraYesNo, 0, 1, "If the QB is paid all the way through the end of COBRA then set this to TRUE, otherwise FALSE"));
                            mappings.Add(new CobraTypedCsvColumn("NextPremiumOwedMonth", FormatType.Integer, 0, 1, "The month (1-12) of the next payment that is owed"));
                            mappings.Add(new CobraTypedCsvColumn("NextPremiumOwedYear", FormatType.Integer, 0, 1, "The year (for example 2009) of the next payment that is owed"));
                            mappings.Add(new CobraTypedCsvColumn("NextPremiumOwedAmountReceived", FormatType.CobraMoney, 0, 1, "Always set to 0"));
                            mappings.Add(new CobraTypedCsvColumn("SendTakeoverLetter", FormatType.CobraYesNo, 0, 1, "Set to TRUE if a Takeover Letter is to be sent to this legacy QB. Takeover Letters are typically sent out when a QB is switching from one TPA to another."));
                            mappings.Add(new CobraTypedCsvColumn("IsConversionLetterSent", FormatType.CobraYesNo, 0, 1, "Set to TRUE if this Legacy QB has already received a Conversion Letter. Conversion Letters are sent out 180 days before the end of COBRA explaining the QB’s right to convert their coverage."));
                            mappings.Add(new CobraTypedCsvColumn("SendDODSubsidyExtension", FormatType.CobraYesNo, 0, 1, "Deprecated – any value will be ignored, set to FALSE."));
                            //
                            break;
                    }
                    break;
                case "[QBPLANINITIAL]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("CoverageLevel", FormatType.String, 35, 1, "EE,EE+SPOUSE,EE+CHILD,EE+CHILDREN,EE+FAMILY,EE+1,EE+2,SPOUSEONLY,SPOUSE+CHILD,CHILDREN,EE+1Child,EE+2Children,EE+3Children,EE+4Children,EE+5orMoreChildren,EE+Spouse+1Child,EE+Spouse+2Children,EE+Spouse+3Children,EE+Spouse+4Children,EE+Spouse+5orMoreChildren,SPOUSE+1CHILD,SPOUSE+2CHILDREN,SPOUSE+3CHILDREN,SPOUSE+4CHILDREN,SPOUSE+5ORMORECHILDREN,EE+DOMESTICPARTNER,EE1UNDER19,EE+SPOUSE1UNDER19,EE+SPOUSE2UNDER19,EE+CHILDREN1UNDER19,EE+CHILDREN2UNDER19,EE+CHILDREN3UNDER19,EE+FAMILY1UNDER19,EE+FAMILY2UNDER19,EE+FAMILY3UNDER19"));
                            mappings.Add(new CobraTypedCsvColumn("NumberOfUnit", FormatType.CobraMoney, 0, 0, "Sets the # of units for this plan. Required if plan is units based (e.g. Life)."));
                            //
                            break;
                    }
                    break;
                case "[QBPLAN]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date that the QB will begin coverage on this plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 0, "Optional, the end date the QB will cease coverage on this plan. This should be set to the LDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("CoverageLevel", FormatType.String, 35, 1, "EE, EE+SPOUSE, EE+CHILD, EE+CHILDREN, EE+FAMILY, EE+1, EE+2, SPOUSEONLY, SPOUSE+CHILD, CHILDREN,EE + 1Child, EE + 2Children, EE + 3Children, EE + 4Children, EE + 5orMoreChildren, EE + Spouse + 1Child, EE + Spouse + 2Children, EE + Spouse + 3Children, EE + Spouse + 4Children, EE + Spouse + 5orMoreChildren, SPOUSE + 1CHILD, SPOUSE + 2CHILDREN, SPOUSE + 3CHILDREN,SPOUSE + 4CHILDREN, SPOUSE + 5ORMORECHILDREN"));
                            mappings.Add(new CobraTypedCsvColumn("FirstDayOfCOBRA", FormatType.CobraDate, 0, 0, "The First Day of COBRA. Unless you wish to override the calculated FDOC, this field should be left blank. If left blank the system will determine the FDOC based on the EventDate and the plan benefit termination type."));
                            mappings.Add(new CobraTypedCsvColumn("LastDayOfCOBRA", FormatType.CobraDate, 0, 0, "The Last Day of COBRA. This field should be left blank. The system will determine the LDOC based on the FDOC and COBRADurationMonths."));
                            mappings.Add(new CobraTypedCsvColumn("COBRADurationMonths", FormatType.Integer, 0, 0, "The number of months of COBRA is usually determined by the event type. This may be left blank (preferred) and the system will determine the correct number of months. It is typically 18 months, but can be extended on Dependent Event Types and USERRA Event Types."));
                            mappings.Add(new CobraTypedCsvColumn("DaysToElect", FormatType.Integer, 0, 0, "The number of days the QB has to elect coverage under COBRA. This may be left blank (preferred) and the system will determine the correct number of days. It is typically 60 days."));
                            mappings.Add(new CobraTypedCsvColumn("DaysToMake1stPayment", FormatType.Integer, 0, 0, "The number of days the QB has to make their 1st full payment under COBRA. This may be left blank (preferred) and the system will determine the correct number of days. It is typically 45 days."));
                            mappings.Add(new CobraTypedCsvColumn("DaysToMakeSubsequentPayments", FormatType.Integer, 0, 0, "The total number of days the member has to make subsequent payments after their initial payment. This may be left blank (preferred) and the system will determine the correct number of days. It is typically 45 days."));
                            mappings.Add(new CobraTypedCsvColumn("ElectionPostmarkDate", FormatType.CobraDate, 0, 0, "Always leave blank"));
                            mappings.Add(new CobraTypedCsvColumn("LastDateRatesNotified", FormatType.CobraDate, 0, 0, "Always leave blank"));
                            mappings.Add(new CobraTypedCsvColumn("NumberOfUnits", FormatType.CobraMoney, 0, 0, "Sets the # of units for this plan. Required if plan is units based (e.g. Life)."));
                            mappings.Add(new CobraTypedCsvColumn("SendPlanChangeLetterForLegacy", FormatType.CobraYesNo, 1, 1, "Set to TRUE to send a plan change letter after this record is entered."));
                            mappings.Add(new CobraTypedCsvColumn("PlanBundleName", FormatType.String, 50, 0, "Conditionally required for plans that are part of a bundle."));
                            //
                            break;
                    }
                    break;
                case "[QBDEPENDENT]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 0, "Social Security Number"));
                            mappings.Add(new CobraTypedCsvColumn("Relationship", FormatType.String, 35, 1, "SPOUSE, CHILD, DOMESTICPARTNER"));
                            mappings.Add(new CobraTypedCsvColumn("Salutation", FormatType.String, 35, 0, "MR, MRS, MS, MISS, DR"));
                            mappings.Add(new CobraTypedCsvColumn("FirstName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("MiddleInitial", FormatType.String, 1, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("LastName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Email", FormatType.Email, 100, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone2", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("AddressSameAsQB", FormatType.CobraYesNo, 1, 1, "Set to TRUE if the dependent’s address is the same as the QB’s address"));
                            mappings.Add(new CobraTypedCsvColumn("Address1", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address2", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("City", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("StateOrProvince", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("PostalCode", FormatType.String, 35, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Country", FormatType.String, 50, 0, "Leave empty if the dependent resides in the USA"));
                            mappings.Add(new CobraTypedCsvColumn("EnrollmentDate", FormatType.CobraDate, 0, 0, "Original enrollment date of the dependent’s medical plan - used for HIPAA certificate"));
                            mappings.Add(new CobraTypedCsvColumn("Sex", FormatType.String, 1, 0, "F, M, U (this is required if the dependent is on a sex based plan that sets rates based on the dependent’s sex)", "F|M|U"));
                            mappings.Add(new CobraTypedCsvColumn("DOB", FormatType.CobraDate, 0, 0, "Date of birth (this is required if the dependent is on an age based plan that sets rates based on the dependent’s age)"));
                            mappings.Add(new CobraTypedCsvColumn("IsQMCSO", FormatType.CobraYesNo, 1, 0, "TRUE if the dependent is under a Qualified Medical Child Support Order (QMCSO)"));
                            //
                            break;
                    }
                    break;
                case "[QBDEPENDENTPLANINITIAL]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            //
                            break;
                    }
                    break;
                case "[QBDEPENDENTPLAN]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date of the dependent on the plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 0, "The end date of the dependent on the plan. This should be set to the LDOC for the plan in the field above unless it is known that the dependent will be ending the plan before LDOC."));
                            mappings.Add(new CobraTypedCsvColumn("UsesFDOC", FormatType.CobraYesNo, 1, 0, "Set to TRUE if the dependent’s plan starts on the QB’s FDOC. Default value is TRUE."));
                            //
                            break;
                    }
                    break;
                case " [QBNOTE]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("NoteType", FormatType.String, 35, 1, "MANUAL, AUTONOTE"));
                            mappings.Add(new CobraTypedCsvColumn("DateTime", FormatType.CobraDateTime, 0, 1, "Date and time of the note"));
                            mappings.Add(new CobraTypedCsvColumn("NoteText", FormatType.String, 0, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("UserName", FormatType.String, 0, 0, "Always leave blank"));
                            //
                            break;
                    }
                    break;
                case "[QBSUBSIDYSCHEDULE]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("InsuranceType", FormatType.String, 35, 1, "MEDICAL, DENTAL, VISION, PHARMACY, FSA, HCRA, EAP, GAP, 401k, LIFE, NULIFE, MSA, PBA, HSA, NUOTHER1, NUOTHER2, GRPLIFE, NUGRPLIFE, VOLLIFE, NUVOLLIFE, CANCER, MERP, DEPLIFE1, DEPLIFE2, DEPLIFE3, NUDEPLIFE1, NUDEPLIFE2, NUDEPLIFE3, MEDSTURIDER1, MEDSTURIDER2, MEDSTURIDER3, LTD, AD&D, CHIROPRACTIC, VEBA, CUSTOMBILLING, LTDNONUNITBASED, LTDUNITBASED, STDNONUNITBASED, STDUNITBASED, CRITICALILLNESS, ACCIDENTNONUNITBASED, ACCIDENTUNITBASED, VOLUNTARYOTHER, UOTHER1, UOTHER2, UOTHER3"));
                            //
                            break;
                    }
                    break;
                case "[QBNOTE]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("SubsidyAmountType", FormatType.String, 35, 0, "?", "FLAT or PERCENTAGE is required if RatePeriodSubsidy is False"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "Start date of the subsidy"));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 1, "End date of the subsidy"));
                            mappings.Add(new CobraTypedCsvColumn("Amount", FormatType.CobraMoney, 0, 0, "Required if RatePeriodSubsidy is False. Amount if FLAT or Percentage if PERCENTAGE of the subsidy.For example, use “50” if it is a 50 % subsidy and the SubsidyAmountType is set to PERCENTAGE."));
                            mappings.Add(new CobraTypedCsvColumn("SubsidyType", FormatType.String, 35, 0, "EMPLOYER (defaults to EMPLOYER)"));
                            mappings.Add(new CobraTypedCsvColumn("RatePeriodSubsidy", FormatType.CobraYesNo, 0, 0, "True = '1', 'Y', 'YES', 'T' or 'TRUE'; False = '0', 'N', 'NO', 'F' or 'FALSE'; Blank value. If RatePeriodSubsidy = blank, True or False value will be applied after import by the system according to the existing logic. RatePeriodSubsidy is not allowed to import on Employer Portal. True or False value will be applied after import by the system according to the existing logic."));
                            //
                            break;
                    }
                    break;
                case "[QBSTATEINSERTS]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("CASRINSERT", FormatType.CobraYesNo, 0, 0, "California Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("CTSRINSERT", FormatType.CobraYesNo, 0, 0, "Connecticut Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("MNLIFEINSERT", FormatType.CobraYesNo, 0, 0, "Minnesota Life Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("MNCONTINSERT", FormatType.CobraYesNo, 0, 0, "Minnesota Continuation Specific Rights Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("ORSRINSERT", FormatType.CobraYesNo, 0, 0, "Oregon Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("TXSRINSERT", FormatType.CobraYesNo, 0, 0, "Texas Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("NY-SR INSERT", FormatType.CobraYesNo, 0, 0, "New York State Continuation. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("VEBASRINSERT", FormatType.CobraYesNo, 0, 0, "VEBA Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("ILSRINSERT", FormatType.CobraYesNo, 0, 0, "Illinois State Continuation. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("RISRINSERT", FormatType.CobraYesNo, 0, 0, "Rhode Island State Continuation. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("GASRINSERT", FormatType.CobraYesNo, 0, 0, "Georgia State Continuation. Default is FALSE. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("VASRINSERT", FormatType.CobraYesNo, 0, 0, "Commonwealth of VA Continuation. Default is FALSE."));
                            //
                            break;
                    }
                    break;
                case "[QBDISABILITYEXTENSION]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("DisabilityApproved", FormatType.CobraYesNo, 1, 1, "Set to TRUE if the Disability Extension is approved or FALSE if it is not"));
                            mappings.Add(new CobraTypedCsvColumn("PostmarkOfDisabilityExtension", FormatType.CobraDate, 0, 1, "Set to the postmark date that the Disability Extension when it was received"));
                            mappings.Add(new CobraTypedCsvColumn("DateDisabled", FormatType.CobraDate, 0, 1, "The date the member was disabled"));
                            mappings.Add(new CobraTypedCsvColumn("DenialReason", FormatType.String, 35, 0, "DISABILITYDATE, SUBMISSIONDATE – required if DisabilityApproved is FALSE"));
                            //
                            break;
                    }
                    break;
                case "[QBPLANMEMBERSPECIFICRATEINITIAL]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("Rate", FormatType.CobraMoney, 0, 1, "The amount of the member specific rate"));
                            //
                            break;
                    }
                    break;
                case "[QBPLANMEMBERSPECIFICRATE]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date of the dependent on the plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 1, "End date of the member specific rate"));
                            mappings.Add(new CobraTypedCsvColumn("Rate", FormatType.CobraMoney, 0, 1, "The amount of the member specific rate"));
                            //
                            break;
                    }
                    break;
                case "[QBPLANTERMREINSTATE]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("TermOrReinstate", FormatType.String, 20, 1, "TERMINATE or REINSTATE"));
                            mappings.Add(new CobraTypedCsvColumn("EffectiveDate", FormatType.CobraDate, 0, 1, "Effective date of the term or reinstate"));
                            mappings.Add(new CobraTypedCsvColumn("Reason", FormatType.String, 35, 1, "Reason for the termination or reinstatement"));
                            //
                            break;
                    }
                    break;
                case "[QBLETTERATTACHMENT]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("LetterAttachmentName", FormatType.String, 100, 1, "The unique name of letter attachment."));
                            //
                            break;
                    }
                    break;
                case "[QBLOOKUP]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("ClientName", FormatType.String, 100, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("QualifyingEventDate", FormatType.CobraDate, 0, 1, "N/A"));
                            //
                            break;
                    }
                    break;
                case "[MEMBERUSERDEFINEDFIELD]":
                    switch (fileTypeAndVersionNo.versionNo)
                    {
                        case "1.2":
                            mappings.Add(new CobraTypedCsvColumn("UserDefinedFieldName", FormatType.String, 0, 1, "The unique name of the user defined field."));
                            mappings.Add(new CobraTypedCsvColumn("UserDefinedFieldValue", FormatType.String, 2000, 0, "Any provided value, including blank, will be saved."));
                            //
                            break;
                    }
                    break;
                default:
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : fileFormat : {rowType} is invalid";
                    throw new Exception(message);
            }

            // entrire line
            return mappings;
        }

        public static string GetCobraRowFormatFromLine(string line)
        {
            if (Utils.IsBlank(line))
            {
                return "[UKNOWN]";
            }

            string[] columns = ImpExpUtils.GetCsvColumnsFromText(line);
            var columnCount = columns.Length;
            if (columnCount == 0)
            {
                return "[UKNOWN]";
            }

            var firstColValue = columns[0];

            //todo: improce this
            return firstColValue;

        }

        public static void ImportCobraFile(DbConnection dbConn, string srcFilePath,
          Boolean hasHeaderRow, FileOperationLogParams fileLogParams
          , OnErrorCallback onErrorCallback)
        {

            try
            {
                string fileName = Path.GetFileName(srcFilePath);
                fileLogParams?.SetFileNames(Utils.GetUniqueIdFromFileName(fileName), fileName, srcFilePath, "", "",
                    "ImportCobraFile", $"Starting: Import {fileName}", "Starting");

                // split text fileinto multiple files
                Dictionary<EdiFileFormat, Object[]> files = new Dictionary<EdiFileFormat, Object[]>();


                // open file for reading
                // read each line and insert
                using (var inputFile = new StreamReader(srcFilePath))
                {
                    int rowNo = 0;
                    string line;
                    while ((line = inputFile.ReadLine()) != null)
                    {
                        rowNo++;

                        var fileTypeAndVersionNo = GetCobraFileTypeAndVersionNoFromFile(srcFilePath);
                        string fileFormat = GetCobraRowFormatFromLine(line);
                        //
                        TypedCsvSchema mappings = GetCobraFileImportMappings(fileFormat, fileTypeAndVersionNo.fileType, fileTypeAndVersionNo.versionNo);
                        //
                        Boolean isResultFile = false; // GetCobraFileFormatIsResultFile(fileFormat);

                        //
                        string tableName = isResultFile ? "[dbo].[cobra_res_file_table_stage]" : "[dbo].[cobra_file_table_stage]";
                        string postImportProc = isResultFile
                            ? "dbo.process_cobra_res_file_table_stage_import"
                            : "dbo.process_cobra_file_table_stage_import";

                        // truncate staging table
                        DbUtils.TruncateTable(dbConn, tableName,
                            fileLogParams?.GetMessageLogParams());

                        // import the line with manual insert statement
                        ImpExpUtils.ImportCsvLine(line, dbConn, tableName, mappings, fileLogParams, onErrorCallback);

                        //
                        // run postimport query to take from staging to final table
                        string queryString = $"exec {postImportProc};";
                        DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                            fileLogParams?.GetMessageLogParams());
                    }
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(srcFilePath, "", ex);
                }
                else
                {
                    throw;
                }
            }
        }
        #endregion

    }
}