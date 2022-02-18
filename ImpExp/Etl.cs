using System.Data.Common;
using CoreUtils;
using CoreUtils.Classes;
using ETLBox.Connection;
using ETLBox.DataFlow;
using ETLBox.DataFlow.Connectors;

namespace ImportExport
{
   public static class Etl
    {

        public static void ImportCrmListManual(Utils.HeaderType headerType, DbConnection dbConn, string srcFilePath, string tableName, FileOperationLogParams fileLogParams)
        {
            // todo: need csv helper

            fileLogParams?.SetFileNames("", "", "", "", "", "ErrorLog-GetCrmList", "CRMList", "Starting: Get CRM List");
            DbUtils.LogFileOperation(fileLogParams);

            fileLogParams?.SetFileNames("", "", "", "", "", "ErrorLog-GetCrmList", "CRMList", "Get CRM List");

            //
            string[] columns =
            {
                "BENCODE",
                "CRM",
                "CRM_email",
                "emp_services",
                "Primary_contact_name",
                "Primary_contact_email",
                "client_start_date"
            };

            //
            ImpExpUtils.ImportCsvFile<CrmList>(srcFilePath, dbConn, tableName, columns, false, fileLogParams);

            //
            fileLogParams?.SetFileNames("", "", "", "", "", "ErrorLog-GetCrmList", "CRMList", "Completed: Get CRM List");
            DbUtils.LogFileOperation(fileLogParams);
        }

        public static void ImportCrmList(Utils.HeaderType headerType, DbConnection dbConn, string srcFilePath, string tableName, FileOperationLogParams fileLogParams)
        {

            fileLogParams?.SetFileNames("", "", "", "", "", "ErrorLog-GetCrmList", "CRMList", "Starting: Get CRM List");
            DbUtils.LogFileOperation(fileLogParams);

            //
           
            // truncate table
            DbUtils.TruncateTable(dbConn, tableName,
                fileLogParams?.DbMessageLogParams?.SetSubModuleStepAndCommand(fileLogParams.ProcessingTask,
                    "Truncate Table", fileLogParams.ProcessingTaskOutcomeDetails, fileLogParams.OriginalFullPath));

            // import csv file
            SqlConnectionManager conMan = new SqlConnectionManager(dbConn.ConnectionString);

            //
            CsvSource sourceCsv = new CsvSource(srcFilePath);
            DbDestination importDest = new DbDestination(conMan, tableName);
            sourceCsv.LinkTo(importDest);
            Network.Execute(sourceCsv);

            //
            fileLogParams?.SetFileNames("", "", "", "", "", "ErrorLog-GetCrmList", "CRMList", "Completed: Get CRM List");
            DbUtils.LogFileOperation(fileLogParams);
        }

    }
}
