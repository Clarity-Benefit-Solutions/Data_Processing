using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Sylvan.Data.Csv;
using static CoreUtils.DbUtils;
using SylvanCsvDataReader = Sylvan.Data.Csv.CsvDataReader;

namespace CoreUtils.Classes
{
    public static class ImpExpUtils
    {
        private static readonly SqlBulkCopyOptions _defaultSqlBulkCopyOptions = SqlBulkCopyOptions.KeepNulls |
            SqlBulkCopyOptions.CheckConstraints |
            SqlBulkCopyOptions.FireTriggers |
            SqlBulkCopyOptions.TableLock;

        private static readonly Regex RegexCSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        public static void ExportSingleColumnFlatFile(string filePath, DbConnection dbConn,
            string queryString, string contentsColName, DbParameters queryParams,
            FileOperationLogParams fileLogParams, OnErrorCallback onErrorCallback)
        {
            try
            {
                //
                FileUtils.EnsurePathExists(filePath);

                //fileLogParams?.SetTaskOutcome("Starting", $"Starting: Export using {queryString}");
                //LogFileOperation(fileLogParams);

                // query
                var dt = (DataTable)DbQuery(DbOperation.ExecuteReader, dbConn, queryString, queryParams,
                    fileLogParams?.GetMessageLogParams());
                //

                // loop thru rows
                //
                FileUtils.EnsurePathExists(filePath);
                using var writer = new StreamWriter(filePath, false);
                foreach (DataRow row in dt.Rows)
                {
                    var line = row[contentsColName].ToString();
                    writer.WriteLine(line);
                }

                // close
                if (writer != null)
                {
                    writer.Close();
                }

                //// log
                //fileLogParams?.SetTaskOutcome("Success", $"Completed: Export using {queryString}");
                //LogFileOperation(fileLogParams);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(filePath, queryString, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public static Dictionary<EdiRowFormat, List<int>> GetAlegeusFileFormats(string srcFilePath, bool hasHeaders,
                    FileOperationLogParams fileLogParams)
        {
            // open csvdatareader, read the file, and check the 2nd row
            var csvDataReaderOptions =
                new CsvDataReaderOptions
                {
                    HasHeaders = hasHeaders,
                };

            using var csv = SylvanCsvDataReader.Create(srcFilePath, csvDataReaderOptions);
            //
            var rowNo = 0;
            Dictionary<EdiRowFormat, List<int>> fileFormats = new Dictionary<EdiRowFormat, List<int>>();

            // note: the splitting of file is not working - just import as a single file format
            const int rowsToRead = 9999;
            while (csv.Read())
            {
                rowNo++;
                if (rowNo <= rowsToRead)
                {
                    var firstColValue = csv.GetString(0);
                    var rowFormat = GetAlegeusRowFormat(firstColValue);
                    // don't take header and unknown as we will not send them across  or parse them
                    if (
                        // sumeet: 2022-07-18-take invalid row types also as client may have made a typos
                        /*rowFormat != EdiFileFormat.Unknown &&*/
                        rowFormat != EdiRowFormat.AlegeusHeader &&
                        rowFormat != EdiRowFormat.AlegeusResultsHeader)
                    {
                        if (fileFormats.ContainsKey(rowFormat))
                        {
                            fileFormats[rowFormat].Add(rowNo);
                        }
                        else
                        {
                            fileFormats.Add(rowFormat, new List<int> { rowNo });
                        }
                    }
                }
            }

            //
            return fileFormats;
        }

        public static EdiRowFormat GetAlegeusRowFormat(object[] rowValues)
        {
            if (rowValues == null || rowValues.Length == 0)
            {
                return EdiRowFormat.Unknown;
            }

            var firstFieldValue = rowValues[0].ToString();
            return GetAlegeusRowFormat(firstFieldValue);
        }

        public static EdiRowFormat GetAlegeusRowFormat(string firstFieldValue)
        {
            if (!Utils.IsBlank(firstFieldValue) && firstFieldValue.Length == 2)
            {
                switch (firstFieldValue)
                {
                    case "IA":
                        return EdiRowFormat.AlegeusHeader;

                    case "IB":
                        return EdiRowFormat.AlegeusDemographics;

                    case "IC":
                        return EdiRowFormat.AlegeusEnrollment;

                    case "ID":
                        return EdiRowFormat.AlegeusDependentDemographics;

                    case "IE":
                        return EdiRowFormat.AlegeusDependentLink;

                    case "IF":
                        return EdiRowFormat.AlegeusCardCreation;

                    case "IG":
                        return EdiRowFormat.AlegeusEmployerDeposit;

                    case "IH":
                        return EdiRowFormat.AlegeusEmployeeDeposit;

                    case "II":
                        return EdiRowFormat.AlegeusEmployeeCardFees;

                    case "IJ":
                        return EdiRowFormat.AlegeusCardStatusChange;

                    case "IK":
                        return EdiRowFormat.AlegeusAdjudication;

                    case "IL":
                        return EdiRowFormat.AlegeusImportRecordForExport;

                    case "IM":
                        return EdiRowFormat.AlegeusCoverageMcc;

                    case "IN":
                        return EdiRowFormat.AlegeusCoverageOption;

                    case "IQ":
                        return EdiRowFormat.AlegeusNewEmployeeId;

                    case "IR":
                        return EdiRowFormat.AlegeusCoverageGeneralSetup;

                    case "IS":
                        return EdiRowFormat.AlegeusEmployerDemographics;

                    case "IT":
                        return EdiRowFormat.AlegeusEmployerLogicalAccount;

                    case "IU":
                        return EdiRowFormat.AlegeusEmployerStandardPlan;

                    case "IV":
                        return EdiRowFormat.AlegeusEmployerPhysicalAccount;

                    case "IW":
                        return EdiRowFormat.AlegeusEmployeeAutoReview;

                    case "IX":
                        return EdiRowFormat.AlegeusEmployerSplitPlan;

                    case "IZ":
                        return EdiRowFormat.AlegeusEmployeeHrInfo;

                    // results files
                    case "RA":
                        return EdiRowFormat.AlegeusResultsHeader;

                    case "RB":
                        return EdiRowFormat.AlegeusResultsDemographics;

                    case "RC":
                        return EdiRowFormat.AlegeusResultsEnrollment;

                    case "RD":
                        return EdiRowFormat.AlegeusResultsDependentDemographics;

                    case "RE":
                        return EdiRowFormat.AlegeusResultsDependentLink;

                    case "RF":
                        return EdiRowFormat.AlegeusResultsCardCreation;

                    case "RH":
                        return EdiRowFormat.AlegeusResultsEmployeeDeposit;

                    case "RI":
                        return EdiRowFormat.AlegeusResultsEmployeeCardFees;

                    case "RZ":
                        return EdiRowFormat.AlegeusResultsEmployeeHrInfo;
                }
            }

            return EdiRowFormat.Unknown;
        }

        public static string[] GetCsvColumnsFromText(string text)
        {
            if (Utils.IsBlank(text))
            {
                return new string[] { };
            }

            var Fields = ImpExpUtils.RegexCSVParser.Split(text);
            return Fields;
        }

        //imports passed csvfile into table line by line as per passed columns
        public static void ImportCsvFile(string filePath, DbConnection dbConn, string tableName,
            TypedCsvSchema mappings, bool hasHeaders, FileOperationLogParams fileLogParams,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                using (var inputFile = new StreamReader(filePath))
                {
                    int rowNo = 0;
                    string line;
                    while ((line = inputFile.ReadLine()) != null)
                    {
                        rowNo++;
                        if (rowNo == 1 && hasHeaders)
                        {
                            continue;
                        }

                        ImportCsvLine(line, dbConn, tableName, mappings, fileLogParams, onErrorCallback);
                    }
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(filePath, tableName, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public static void ImportCsvFileBulkCopy(DbConnection dbConn,
            string srcFilePath, bool hasHeaderRow, string tableName, TypedCsvSchema columnMappings,
            FileOperationLogParams fileLogParams, OnErrorCallback onErrorCallback)
        {
            try
            {
                //fileLogParams?.SetTaskOutcome("Starting", $"Starting: Import Into {tableName}");
                //LogFileOperation(fileLogParams);

                //
                // truncate table
                TruncateTable(dbConn, tableName,
                    fileLogParams?.GetMessageLogParams());

                if (columnMappings == null || columnMappings.Count == 0)
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : columnMappings should be set";
                    throw new Exception(message);
                }

                // Get the schema for the target table

                // init SqlBulkCopy
                using var bcp = new SqlBulkCopy(dbConn.ConnectionString, ImpExpUtils._defaultSqlBulkCopyOptions);

                // source columns for reading and mapping for writing
                List<TypedCsvColumn> listCols = new List<TypedCsvColumn>();
                foreach (var column in columnMappings.Columns)
                {
                    // for reading
                    listCols.Add(column);

                    //for writing to DB
                    if (!Utils.IsBlank(column.ColumnName))
                    {
                        bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column.SourceColumn,
                            column.DestinationColumn));
                    }
                    else
                    {
                        bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column.SourceOrdinal,
                            column.DestinationOrdinal));
                    }
                }

                // create csv reader options
                var csvDataReaderOptions =
                    new CsvDataReaderOptions
                    {
                        Schema = new CsvSchema(listCols),
                        HasHeaders = hasHeaderRow,
                    };

                // create the csv reader
                using var csv = SylvanCsvDataReader.Create(srcFilePath, csvDataReaderOptions);

                //
                bcp.BulkCopyTimeout = 0;
                bcp.DestinationTableName = tableName;
                bcp.BatchSize = 1000;

                // write all rows to server
                bcp.WriteToServer(csv);

                ////
                //fileLogParams?.SetTaskOutcome("Success", $"Completed: Import Into {tableName}");
                //LogFileOperation(fileLogParams);
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

        public static void ImportCsvFileBulkCopyAutoSchema(HeaderType headerType, DbConnection dbConn,
            string srcFilePath, bool hasHeaderRow,
            string tableName,
            FileOperationLogParams fileLogParams, OnErrorCallback onErrorCallback)
        {
            try
            {
                //fileLogParams?.SetTaskOutcome("Starting", $"Starting: Import Into {tableName}");
                //LogFileOperation(fileLogParams);

                //
                // truncate table
                TruncateTable(dbConn, tableName,
                    fileLogParams?.GetMessageLogParams());

                // Get the schema for the target table
                var conn = new SqlConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = $"select top 0 * from {tableName}";
                var reader = cmd.ExecuteReader();
                var schemaTable = reader.GetSchemaTable();
                reader.Close();

                List<TypedCsvColumn> listCols = new List<TypedCsvColumn>();

                if (schemaTable != null)
                {
                    foreach (DataRow row in schemaTable.Rows)
                    {
                        var columnName = Convert.ToString(row["ColumnName"]);
                        //
                        var column =
                            new TypedCsvColumn(columnName, columnName);

                        listCols.Add(column);
                    }
                }

                // create csv reader options
                var csvDataReaderOptions =
                    new CsvDataReaderOptions
                    {
                        Schema = new CsvSchema(listCols),
                        HasHeaders = hasHeaderRow,
                    };

                var csv = SylvanCsvDataReader.Create(srcFilePath, csvDataReaderOptions);

                //
                var bcp = new SqlBulkCopy(conn.ConnectionString, ImpExpUtils._defaultSqlBulkCopyOptions);
                //
                bcp.BulkCopyTimeout = 0;
                bcp.DestinationTableName = tableName;
                bcp.BatchSize = 10000;
                //
                bcp.WriteToServer(csv);

                ////
                //fileLogParams?.SetTaskOutcome("Success", $"Completed: Import Into {tableName}");
                //LogFileOperation(fileLogParams);
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

        //imports passed line into table as per passed columns
        public static void ImportCsvLine(string line, DbConnection dbConn, string tableName,
        TypedCsvSchema mappings, FileOperationLogParams fileLogParams,
        OnErrorCallback onErrorCallback)
        {
            // get insertValues string
            var insertValuesString = "";
            string[] columns = ImpExpUtils.GetCsvColumnsFromText(line);
            int colNo = -1;

            string insertColumnNames = "";
            foreach (var mapping in mappings.Columns)
            {
                insertColumnNames += $"{mapping.DestinationColumn},";
                //
                colNo++;
                string value = "";
                if (columns.Length > colNo)
                {
                    value = columns[colNo];
                }

                // trim starting and ending "
                if (Utils.Left(value, 1) == "\"")
                {
                    value = value.Substring(1);
                }
                if (Utils.Right(value, 1) == "\"")
                {
                    value = Utils.Left(value, value.Length - 1);
                }

                value = value.Replace("\"\"", "\"");

                //
                insertValuesString += $"'{Utils.DbQuote(value)}',";
            }

            insertValuesString = insertValuesString.Substring(0, insertValuesString.Length - 1);
            insertColumnNames = insertColumnNames.Substring(0, insertColumnNames.Length - 1);

            //
            var query = $"INSERT INTO {tableName} " +
                        $" ({insertColumnNames}) " +
                        $" values ({insertValuesString})";

            // pass new dbLogParams() to ensure no recursion of logging!
            DbQuery(DbOperation.ExecuteNonQuery, dbConn, query, null,
                fileLogParams?.GetMessageLogParams()
                , doNotLogOperationToDb: true);
        }

        public static void ImportSingleColumnFlatFile(DbConnection dbConn,
                                                    string srcFilePath, string srcFileName,
            string tableName, string fileColName, string contentsColName,
            ImportThisLineCallback importThisLineCallback,
            FileOperationLogParams fileLogParams,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                //fileLogParams?.SetTaskOutcome("Starting", $"Starting: Import using {tableName}");
                //LogFileOperation(fileLogParams);

                // read each line and insert
                using var inputFile = new StreamReader(srcFilePath);

                string line;
                var rowNo = 0;
                while ((line = inputFile.ReadLine()!) != null)
                {
                    rowNo++;
                    if (importThisLineCallback == null || importThisLineCallback(srcFilePath, rowNo, line))
                    {
                        // insert each line into table
                        var query = $"INSERT INTO {tableName} " +
                                    $" ({fileColName}, {contentsColName}) " +
                                    $" values ('{Utils.DbQuote(srcFileName)}', '{Utils.DbQuote(line)}')";

                        // pass new dbLogParams() to ensure no recursion of logging!
                        DbQuery(DbOperation.ExecuteNonQuery, dbConn, query, null,
                            fileLogParams?.GetMessageLogParams()
                            , doNotLogOperationToDb: true);
                    }
                }

                //fileLogParams?.SetTaskOutcome("Success", $"Completed: Import Into {tableName}");
                //LogFileOperation(fileLogParams);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(srcFilePath, srcFileName, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        // routine

        // routine
    } // class
} // ns