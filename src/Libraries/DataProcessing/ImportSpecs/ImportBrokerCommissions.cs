using System;
using System.Data.Common;
using System.Reflection;
using CoreUtils;
using CoreUtils.Classes;

//using ETLBox.Connection;
//using ETLBox.DataFlow;
//using ETLBox.DataFlow.Connectors;

// ReSharper disable All

namespace DataProcessing
{
    public static partial class Import
    {
        public static TypedCsvSchema GetBrokerCommissionFileImportMappings(EdiRowFormat rowFormat, HeaderType headerType, Boolean forImport = true)
        {
            var mappings = new TypedCsvSchema();

            switch (rowFormat)
            {
                /////////////////////////////////////////////////////
                // IB, RB
                case EdiRowFormat.BrokerCommissionQBRawData:
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
                                 $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : rowFormat : {rowFormat.ToDescription()} is invalid";
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
            EdiRowFormat rowFormat = EdiRowFormat.BrokerCommissionQBRawData;

            // 2. import the file
            try
            {
                // check mappinsg and type opf file (Import or Result)

                var headerType = HeaderType.NotApplicable;
                TypedCsvSchema mappings = GetBrokerCommissionFileImportMappings(rowFormat, headerType);
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
                    onErrorCallback(srcFilePath, rowFormat.ToDescription(), ex);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}