using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreUtils.Classes;
using Microsoft.SqlServer.Server;

// ReSharper disable All

namespace CoreUtils
{


    public class DbUtils
    {
        public delegate void OnLogOperationCallback(MessageLogParams logParams);

        
        public static event EventHandler<MessageLogParams> EventOnLogOperationCallback;

        public static void RaiseOnLogOperationCallback(MessageLogParams logParams)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<MessageLogParams> raiseEvent = EventOnLogOperationCallback;

            // Event will be null if there are no subscribers
            if (raiseEvent != null)
            {
                object obj = new Object();
                // Call to raise the event.
                raiseEvent(obj, logParams);
            }
        }

        public static event EventHandler<FileOperationLogParams> eventOnLogFileOperationCallback;

        public static void RaiseOnLogFileOperationCallback(FileOperationLogParams logParams)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<FileOperationLogParams> raiseEvent = eventOnLogFileOperationCallback;

            // Event will be null if there are no subscribers
            if (raiseEvent != null)
            {
                object obj = new Object();
                // Call to raise the event.
                raiseEvent(obj, logParams);
            }
        }

        public static void TruncateTable(DbConnection dbConn, string tableName, MessageLogParams logParams)
        {
            // validate
            if (dbConn is null)
            {
                string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : dbConn should be set";
                throw new Exception(message);
            }

            if (Utils.IsBlank(tableName))
            {
                string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : queryString should be set";
                throw new Exception(message);
            }

            // use dbQuery
            string queryString = $"TRUNCATE TABLE {Utils.DbQuote(tableName)} ; ";

            // do query
            DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                logParams?.SetStepAndCommand(MethodBase.GetCurrentMethod()?.Name, queryString));
        }


        public static Object DbQuery(DbOperation dbOperation, DbConnection dbConn, string queryString,
            DbParameters queryParams = null, MessageLogParams logParams = null, Boolean isSP = false,
            Boolean doNotLogOperationToDb = false)
        {
            // validate
            if (dbConn is null)
            {
                string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : dbConn.dbConnection should be set";
                throw new Exception(message);
            }

            if (Utils.IsBlank(queryString))
            {
                string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : queryString should be set";
                throw new Exception(message);
            }

            Object retVal = null;
            //
            DbCommand queryCommand = dbConn.CreateCommand();
            queryCommand.CommandTimeout = 0;
            if (isSP)
            {
                queryCommand.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                queryCommand.CommandType = CommandType.Text;
            }

            queryCommand.CommandText = queryString;

            // add params
            if (queryParams != null)
            {
                foreach (SqlParameter param in queryParams)
                {
                    queryCommand.Parameters.Add(param);
                }
            }

            // execute Query
            if (dbOperation == DbOperation.ExecuteReader)
            {
                using DbDataReader reader = queryCommand.ExecuteReader();


                // read data and return
                List<DataColumn> listCols = new List<DataColumn>();
                DataTable dt = new DataTable();
                DataTable schemaTable = reader.GetSchemaTable();
                if (schemaTable != null)
                {
                    foreach (DataRow drow in schemaTable.Rows)
                    {
                        string columnName = Convert.ToString(drow["ColumnName"]);
                        DataColumn column = new DataColumn(columnName, (Type)(drow["DataType"]));
                        column.Unique = (bool)drow["IsUnique"];
                        column.AllowDBNull = (bool)drow["AllowDBNull"];
                        column.AutoIncrement = (bool)drow["IsAutoIncrement"];
                        listCols.Add(column);
                        dt.Columns.Add(column);
                    }

                    // Read rows from DataReader and populate the DataTable 
                    while (reader.Read())
                    {
                        DataRow dataRow = dt.NewRow();
                        for (int i = 0; i < listCols.Count; i++)
                        {
                            dataRow[((DataColumn)listCols[i])] = reader[i];
                        }

                        dt.Rows.Add(dataRow);
                    }
                }

                //
                retVal = dt;
            }
            else if (dbOperation == DbOperation.ExecuteScalar)
            {
                retVal = queryCommand.ExecuteScalar();
            }
            else if (dbOperation == DbOperation.ExecuteNonQuery)
            {
                queryCommand.ExecuteNonQuery();
                retVal = 1;
            }
            else
            {
                string message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : DbOperation : {dbOperation} is Invalid";
                throw new Exception(message);
            }

            // logOperation
            if (!doNotLogOperationToDb && logParams != null)
            {
                logParams.Command = queryString;
                LogMessage(logParams);
            }

            // return values
            return retVal;
        }

        public static void LogMessage(MessageLogParams logParams)
        {
            // log Query
            if (logParams.DbConnection != null && !Utils.IsBlank(logParams.LogTableName))
            {
                string logQuery = $"INSERT INTO {logParams.LogTableName} " +
                                  $"                          (platform, module_name, submodule_name, step_type, step_name, command) " +
                                  $"                          values ('{Utils.DbQuote(logParams.Platform)}','{Utils.DbQuote(logParams.ModuleName)}', '{Utils.DbQuote(logParams.SubModuleName)}', '{Utils.DbQuote(logParams.StepType)}', '{Utils.DbQuote(logParams.StepName)}', '{Utils.DbQuote(logParams.Command)}')";

                // pass new dbLogParams() to ensure no recursion of logging!
                DbQuery(DbOperation.ExecuteNonQuery, logParams.DbConnection, logQuery, null, null);
            }

            // raise event
            RaiseOnLogOperationCallback(logParams);
        }

        public static void LogFileOperation(FileOperationLogParams fileLogParams)
        {
            if (fileLogParams.FileLogId == 0)
            {
                Debug.WriteLine($"FileLogID = 0 for {fileLogParams.OriginalFullPath} ");
            }

            fileLogParams.CalculateIds();
            // log Query
            if (fileLogParams.DbConnection != null && !Utils.IsBlank(fileLogParams.LogTableName))
            {
                //
                string logQuery = "";
                logQuery = $"dbo.insert_file_processing_log";
                //

                if (fileLogParams.FileLogId == 0 && Utils.IsBlank(fileLogParams.FileId))
                {
                    //  Debug.WriteLine("a");
                }

                DbParameters queryParams = new DbParameters();
                queryParams.Add(new SqlParameter("@platform", fileLogParams.Platform));
                queryParams.Add(new SqlParameter("@fileLogId", fileLogParams.FileLogId));
                queryParams.Add(new SqlParameter("@fileId", fileLogParams.FileId));
                queryParams.Add(new SqlParameter("@folderName", fileLogParams.FolderName));

                queryParams.Add(new SqlParameter("@templateType", fileLogParams.TemplateType));
                queryParams.Add(new SqlParameter("@IcType", fileLogParams.IcType));
                queryParams.Add(new SqlParameter("@toFTP", fileLogParams.ToFtp));

                queryParams.Add(new SqlParameter("@bencode", fileLogParams.Bencode));
                queryParams.Add(new SqlParameter("@originalFileName", fileLogParams.OriginalFileName));
                queryParams.Add(new SqlParameter("@originalFullPath", fileLogParams.OriginalFullPath));

                queryParams.Add(new SqlParameter("@originalFileUploadedOn", fileLogParams.OriginalFileUploadedOn));

                queryParams.Add(new SqlParameter("@newFileName", fileLogParams.NewFileName));
                queryParams.Add(new SqlParameter("@newFileFullPath", fileLogParams.NewFileFullPath));
                queryParams.Add(new SqlParameter("@fileLogTaskId", fileLogParams.FileLogTaskId));

                queryParams.Add(new SqlParameter("@processingTask", fileLogParams.ProcessingTask));
                queryParams.Add(new SqlParameter("@processingTaskOutcome", fileLogParams.ProcessingTaskOutcome));
                queryParams.Add(new SqlParameter("@processingTaskOutcomeDetails",
                    fileLogParams.ProcessingTaskOutcomeDetails));

                // pass new dbLogParams() to ensure no recursion of logging!
                DataTable dt = (DataTable)DbQuery(DbOperation.ExecuteReader, fileLogParams.DbConnection, logQuery,
                    queryParams, null, true);
            }

            // logDbOperation
            LogMessage(fileLogParams?.GetMessageLogParams());

            // raise event
            RaiseOnLogFileOperationCallback(fileLogParams);

            // clear fileids so we get from db for next file    
            fileLogParams.ReInitIds();
        }

        public static void LogError(string arg1, string arg2, Exception ex, FileOperationLogParams fileLogParams)
        {
            if (fileLogParams == null)
            {
                throw ex;
            }

            fileLogParams.SetTaskOutcome("ERROR",
                $"ERROR {fileLogParams.ProcessingTask}: {arg1} - {arg2} - {ex.ToString()}");
            //
            LogFileOperation(fileLogParams);
        }




        // return last fileLogId from file_processing_log for old or new filename equal to picked up filename so the file can be tracked across operations
        public static int GetFileOperationFileLogId(string srcFileName, FileOperationLogParams logParams)
        {
            string fileName = Path.GetFileNameWithoutExtension(srcFileName);

            if (!Utils.IsBlank(fileName))
            {
                string logQuery = $"select dbo.getFileLogId('{Utils.DbQuote(fileName)}');";

                // pass new dbLogParams() to ensure no recursion of logging!
                int fileLogId = (int)DbQuery(DbOperation.ExecuteScalar, logParams.DbConnection, logQuery, null, null,
                    false);
                return fileLogId;
            }
            else
            {
                return 0;
            }
        }


       
        public static string AddUniqueIdToFileAndLogToDb(string srcFilePath, Boolean fixFileNameLength, FileOperationLogParams fileLogParams)
        {
            // get filename without leading fileid

            // ignore some files
            if (FileUtils.IgnoreFile(srcFilePath))
            {
                return srcFilePath;
            }


            // if file has uniqueID and headerttype already, nothing to do
            if (!Utils.IsBlank(Utils.GetUniqueIdFromFileName(srcFilePath))
               /* && (GetHeaderTypeFromFileName(srcFilePath) == headerType || headerType == HeaderType.NotApplicable)*/)
            {
                return srcFilePath;
            }

            FileInfo srcFileInfo = new FileInfo(srcFilePath);
            string oldFileName = srcFileInfo.Name;

            //string newFileName = AddUniqueIdAndHeaderTypeToFileName(oldFileName, headerType);
            string newFileName = Utils.AddUniqueIdToFileName(oldFileName);

            // fix for alegeus - max 30 chars incvluding extension
            string newFileNameFixed = newFileName;
            if (fixFileNameLength)
            {
                newFileNameFixed =
                    $" {Utils.Left(Path.GetFileNameWithoutExtension(newFileName), Utils.MaxFilenameLengthFtp - 4)}{Path.GetExtension(newFileName)}";
            }

            //
            string newFileId = Utils.GetUniqueIdFromFileName(newFileName);

            //get full path of dest file with uniqueID
            string newFilePath = $"{srcFileInfo.Directory}/{newFileName}";

            // move prv file to new file with fileid prefixed so we can track it as it moves
            srcFilePath = FileUtils.FixPath(srcFilePath);
            newFilePath = FileUtils.FixPath(newFilePath);
            if (!srcFilePath.Equals(newFilePath))
            {
                srcFileInfo.MoveTo(newFilePath);

                //
                fileLogParams.SetFileNames(newFileId, oldFileName, srcFilePath, newFileName, newFilePath,
                    "New UniqueID created for Source File", "Success",
                    $"Set {newFileId} for Source File: {oldFileName} and copied to {newFilePath}");
                fileLogParams.setOriginalFileUploadedOn(srcFileInfo.CreationTime);

                // add to fileLog
                fileLogParams.ReInitIds();

                //
                LogFileOperation(fileLogParams);

                return newFilePath;
            }

            //
            return srcFilePath;

        }

        public class DbParameters : List<SqlParameter>
        {
        }
    }
}