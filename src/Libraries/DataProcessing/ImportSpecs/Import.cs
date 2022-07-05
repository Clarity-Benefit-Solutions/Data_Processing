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

    public static partial class Import
    {

      
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
                newPath = $"{Path.GetDirectoryName(srcFilePath)}/";
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
    
    }
}