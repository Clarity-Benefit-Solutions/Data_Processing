using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Sylvan.Data.Csv;
using static CoreUtils.DbUtils;
using SylvanCsvDataReader = Sylvan.Data.Csv.CsvDataReader;
using CsvHelperCsvDataReader = CsvHelper.CsvDataReader;

namespace CoreUtils.Classes
{

    public static class ImpExpUtils
    {
        private static readonly SqlBulkCopyOptions _defaultSqlBulkCopyOptions = SqlBulkCopyOptions.KeepNulls |
            SqlBulkCopyOptions.CheckConstraints |
            SqlBulkCopyOptions.FireTriggers |
            SqlBulkCopyOptions.TableLock;

        private static readonly Regex RegexCSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        public static Dictionary<EdiFileFormat, List<int>> GetAlegeusFileFormats(string srcFilePath, bool hasHeaders,
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
            Dictionary<EdiFileFormat, List<int>> fileFormats = new Dictionary<EdiFileFormat, List<int>>();

            const int rowsToRead = 9999;
            while (csv.Read())
            {
                rowNo++;
                if (rowNo <= rowsToRead)
                {
                    var firstColValue = csv.GetString(0);
                    var fileFormat = GetAlegeusRowFormat(firstColValue);

                    // don't take header and unknown as we will not send them across  or parse them
                    if (fileFormat != EdiFileFormat.Unknown && fileFormat != EdiFileFormat.AlegeusHeader &&
                        fileFormat != EdiFileFormat.AlegeusResultsHeader)
                    {
                        if (fileFormats.ContainsKey(fileFormat))
                        {
                            fileFormats[fileFormat].Add(rowNo);
                        }
                        else
                        {
                            fileFormats.Add(fileFormat, new List<int> {rowNo});
                        }
                    }
                }
            }

            //
            return fileFormats;
        }

        public static EdiFileFormat GetAlegeusRowFormat(object[] rowValues)
        {
            if (rowValues == null || rowValues.Length == 0)
            {
                return EdiFileFormat.Unknown;
            }

            var firstFieldValue = rowValues[0].ToString();
            return GetAlegeusRowFormat(firstFieldValue);
        }

        public static EdiFileFormat GetAlegeusRowFormat(string firstFieldValue)
        {
            if (!Utils.IsBlank(firstFieldValue) && firstFieldValue.Length == 2)
            {
                switch (firstFieldValue)
                {
                    case "IA":
                        return EdiFileFormat.AlegeusHeader;

                    case "IB":
                        return EdiFileFormat.AlegeusDemographics;

                    case "IC":
                        return EdiFileFormat.AlegeusEnrollment;

                    case "ID":
                        return EdiFileFormat.AlegeusDependentDemographics;

                    case "IE":
                        return EdiFileFormat.AlegeusDependentLink;

                    case "IF":
                        return EdiFileFormat.AlegeusCardCreation;

                    case "IG":
                        return EdiFileFormat.AlegeusEmployerDeposit;

                    case "IH":
                        return EdiFileFormat.AlegeusEmployeeDeposit;

                    case "II":
                        return EdiFileFormat.AlegeusEmployeeCardFees;

                    case "IJ":
                        return EdiFileFormat.AlegeusCardStatusChange;

                    case "IK":
                        return EdiFileFormat.AlegeusAdjudication;

                    case "IL":
                        return EdiFileFormat.AlegeusImportRecordForExport;

                    case "IM":
                        return EdiFileFormat.AlegeusCoverageMcc;

                    case "IN":
                        return EdiFileFormat.AlegeusCoverageOption;

                    case "IQ":
                        return EdiFileFormat.AlegeusNewEmployeeId;

                    case "IR":
                        return EdiFileFormat.AlegeusCoverageGeneralSetup;

                    case "IS":
                        return EdiFileFormat.AlegeusEmployerDemographics;

                    case "IT":
                        return EdiFileFormat.AlegeusEmployerLogicalAccount;

                    case "IU":
                        return EdiFileFormat.AlegeusEmployerStandardPlan;

                    case "IV":
                        return EdiFileFormat.AlegeusEmployerPhysicalAccount;

                    case "IW":
                        return EdiFileFormat.AlegeusEmployeeAutoReview;

                    case "IX":
                        return EdiFileFormat.AlegeusEmployerSplitPlan;

                    case "IZ":
                        return EdiFileFormat.AlegeusEmployeeHrInfo;

                    // results files
                    case "RA":
                        return EdiFileFormat.AlegeusResultsHeader;

                    case "RB":
                        return EdiFileFormat.AlegeusResultsDemographics;

                    case "RC":
                        return EdiFileFormat.AlegeusResultsEnrollment;

                    case "RD":
                        return EdiFileFormat.AlegeusResultsDependentDemographics;

                    case "RE":
                        return EdiFileFormat.AlegeusResultsDependentLink;

                    case "RF":
                        return EdiFileFormat.AlegeusResultsCardCreation;

                    case "RH":
                        return EdiFileFormat.AlegeusResultsEmployeeDeposit;

                    case "RI":
                        return EdiFileFormat.AlegeusResultsEmployeeCardFees;

                    case "RZ":
                        return EdiFileFormat.AlegeusResultsEmployeeHrInfo;
                }
            }

            return EdiFileFormat.Unknown;
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
                var dt = (DataTable) DbQuery(DbOperation.ExecuteReader, dbConn, queryString, queryParams,
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

        public static string[] GetCsvColumnsFromText(string text)
        {
            if (Utils.IsBlank(text))
            {
                return new string[] { };
            }

            var Fields = ImpExpUtils.RegexCSVParser.Split(text);
            return Fields;
        }

        public static void ImportCsvFile<T>(string filePath, DbConnection dbConn, string tableName,
            string[] contentsColNames, bool hasHeaders, FileOperationLogParams fileLogParams,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                //fileLogParams?.SetTaskOutcome("Starting", $"Starting: Import Into {tableName}");
                //LogFileOperation(fileLogParams);

                var theType = typeof(T);

                var insertColumnNames = string.Join(",", contentsColNames);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    NewLine = Environment.NewLine,
                    // in case HasHeaderRecord = false, the reader will use Index attributes of the class properties to match
                    HasHeaderRecord = hasHeaders,
                };

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, config);
                IEnumerable<T> records = csv.GetRecords<T>();

                foreach (var row in records)
                {
                    // get insertValues string
                    var insertValuesString = "";
                    foreach (var columnName in contentsColNames)
                    {
                        var value = theType.GetProperty(columnName)?.GetValue(row, null);
                        insertValuesString += $"'{Utils.DbQuote(value?.ToString())}',";
                    }

                    insertValuesString = insertValuesString.Substring(0, insertValuesString.Length - 1);

                    //
                    var query = $"INSERT INTO {tableName} " +
                                $" ({insertColumnNames}) " +
                                $" values ({insertValuesString})";

                    // pass new dbLogParams() to ensure no recursion of logging!
                    DbQuery(DbOperation.ExecuteNonQuery, dbConn, query, null,
                        fileLogParams?.GetMessageLogParams()
                        , doNotLogOperationToDb: true);
                }

                //// log
                //fileLogParams?.SetTaskOutcome("Success", $"Completed: Import Into {tableName}");
                //LogFileOperation(fileLogParams);
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
        } // routine

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
    } // class

} // ns
