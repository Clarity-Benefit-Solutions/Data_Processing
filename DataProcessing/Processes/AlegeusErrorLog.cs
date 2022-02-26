﻿using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreUtils;
using CoreUtils.Classes;
using EtlUtilities;

// ReSharper disable All

// ReSharper disable StringLiteralTypo

//todo: how to avoid reimporting the same result file over and over again
namespace DataProcessing
{
    [Guid("EAFED76A-45C1-4AC5-BC0B-4321F4C3C83F")]
    [ComVisible(true)]
    public class AlegeusErrorLog
    {
        public static void USERVERYCAUTIOUSLY_ClearAllTables()
        {
            var dbConn = Vars.dbConnAlegeusErrorLog;
            var fileLogParams = Vars.dbFileProcessingLogParams;

            var queryString = "exec [Alegeus_ErrorLog].dbo.zz_truncate_all;";

            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null, fileLogParams.DbMessageLogParams,
                false, false);
        }

        public static void RetrieveErrorLogs()
        {
            // init logParams

            //
            var fileLogParams = Vars.dbFileProcessingLogParams;
            var dbConn = Vars.dbConnAlegeusErrorLog;
            var ftpConn = Vars.RemoteAlegeusFtpConnection;

            // 
            var headerType = HeaderType.NotApplicable;

            //DeleteStagingFiles
            DeleteStagingFiles(headerType, dbConn, fileLogParams);

            //GetFtpFilesFroMAlegeus
            GetFtpFilesFromAlegeus(headerType, ftpConn, fileLogParams);

            ////GetCrmList
            //Import.ImportCrmListFileBulkCopy(headerType, dbConn, $"{Vars.fromBoomiFtpRoot}/CRM_List.csv", false,
            //    "dbo.[CRM_List]", fileLogParams);

            //ImportAlegeusResultResFiles
            ImportAlegeusFiles(headerType, dbConn, fileLogParams);

            //TrackNewFtpFileErrors
            TrackAllNewFtpFileErrors(headerType, dbConn, fileLogParams);

            //MoveFilesToArchive
            MoveFilesToArchive(headerType, dbConn, fileLogParams);
        }

        protected static void DeleteStagingFiles(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //1. Clear all files in AutomatedHeaderV1_Files
            //echo y| del  G:\FTP\AutomatedHeaderV1_Files\*.*
            //

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.DeleteFiles(new[] { Vars.alegeusErrorLogMbiFilesRoot, Vars.alegeusErrorLogResFilesRoot }
                , false
                , new[] { "*.mbi", "*.dne", "*.txt", "*.res" },
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "ErrorLog-DeleteStagingFiles", "Success",
                        "Delete File in Dir");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) =>
                {
                    if (fileLogParams == null) { throw ex; } else 
                    {
                        fileLogParams.SetTaskOutcome("ERROR",
                            $"ERROR {fileLogParams.ProcessingTask}: {ex.ToString()}");
                        DbUtils.LogFileOperation(fileLogParams);
                    }
                }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected static void GetFtpFilesFromAlegeus(HeaderType headerType, SFtpConnection ftpConn,
            FileOperationLogParams fileLogParams)
        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            fileLogParams.SetSourceFolderName(Vars.remoteAlegeusFtpRootPath);
            //
            DbUtils.LogFileOperation(fileLogParams);

            // download mbi dir files
            ftpConn.CopyOrMoveFiles(
                FtpFileOperation.DownloadAndDelete,
                new string[] { Vars.remoteAlegeusFtpRootPath }, true,
                new string[] { "*.mbi", "*.dne" },
                Vars.alegeusErrorLogMbiFilesRoot, "", "",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add uniqueId to file so we can track it across folders and operations
                    var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(headerType, destFilePath, false,
                            fileLogParams);

                    fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), uniqueIdFilePath,
                        Path.GetFileName(uniqueIdFilePath), "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", $"Processing File ${srcFilePath}");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) =>
                {
                    if (fileLogParams == null) { throw ex; } else 
                    {
                        fileLogParams.SetTaskOutcome("ERROR",
                            $"ERROR {fileLogParams.ProcessingTask}: {ex.ToString()}");
                        DbUtils.LogFileOperation(fileLogParams);
                    }
                }
            );


            // download res dir files
            ftpConn.CopyOrMoveFiles(
                FtpFileOperation.DownloadAndDelete,
                new string[] { Vars.remoteAlegeusFtpRootPath }, true,
                new string[] { "*.res" },
                Vars.alegeusErrorLogResFilesRoot, "", "",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add uniqueId to file so we can track it across folders and operations
                    var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(headerType, destFilePath, true,
                          fileLogParams);

                    fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), uniqueIdFilePath,
                        Path.GetFileName(uniqueIdFilePath), "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", $"Processing File ${srcFilePath}");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) =>
                {
                    if (fileLogParams == null) { throw ex; } else 
                    {
                        fileLogParams.SetTaskOutcome("ERROR",
                            $"ERROR {fileLogParams.ProcessingTask}: {ex.ToString()}");
                        DbUtils.LogFileOperation(fileLogParams);
                    }
                }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected static void ImportAlegeusFiles(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);

            //
            FileUtils.IterateDirectory(
                new[] { Vars.alegeusErrorLogResFilesRoot, Vars.alegeusErrorLogMbiFilesRoot }, DirectoryIterateType.Files
                , false, new[] { "*.res", "*.dne", "*.txt", "*.mbi" },
                (srcFilePath, destFilePath, dummy2) =>
                {
                    //
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "ErrorLog-ImportAlegeusFiles", "Success",
                        "Importing Alegeus File");

                    //2. import file
                    Import.ImportAlegeusFile(headerType, dbConn, srcFilePath, false, fileLogParams,
                        (arg1, arg2, ex) =>
                        {
                            if (fileLogParams == null) { throw ex; } else 
                            {
                                fileLogParams.SetTaskOutcome("ERROR",
                                    $"ERROR {fileLogParams.ProcessingTask}: {ex.ToString()}");
                                DbUtils.LogFileOperation(fileLogParams);
                            }
                        }
                    );

                }
                , null
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }


        protected static void TrackAllNewFtpFileErrors(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);

            //
            string queryString = null;

            // 1: insert into dbo_tracked_errors_local
            queryString = @" 
                        exec [dbo].[alegeus_errorlog_track_new_ftp_errors]
                            ";
            // run query
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                fileLogParams?.GetMessageLogParams());

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected static void MoveFilesToArchive(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //


            // move mbiFiles to Archive
            FileUtils.MoveFiles(
                Vars.alegeusErrorLogMbiFilesRoot, false, "*.*",
                Vars.alegeusErrorLogMbiFilesArchiveRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", "Moved File to Archive");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) =>
                {
                    if (fileLogParams == null) { throw ex; } else 
                    {
                        fileLogParams.SetTaskOutcome("ERROR",
                            $"ERROR {fileLogParams.ProcessingTask}: {ex.ToString()}");
                        DbUtils.LogFileOperation(fileLogParams);
                    }
                }
            );
            // move resFiles to Archive
            FileUtils.MoveFiles(
                Vars.alegeusErrorLogResFilesRoot, false, "*.*",
                Vars.alegeusErrorLogResFilesArchiveRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", "Moved File to Archive");

                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) =>
                {
                    if (fileLogParams == null) { throw ex; } else 
                    {
                        fileLogParams.SetTaskOutcome("ERROR",
                            $"ERROR {fileLogParams.ProcessingTask}: {ex.ToString()}");
                        DbUtils.LogFileOperation(fileLogParams);
                    }
                }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        } // routine
    } // end class
} // end namespace