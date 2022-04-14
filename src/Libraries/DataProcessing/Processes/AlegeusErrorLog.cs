﻿using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;

// ReSharper disable All

// ReSharper disable StringLiteralTypo

//todo: FTPErrors: how to avoid reprocessing the same result file over and over again: shall we move all files older than 2 days? does Alegeus ever move them?
namespace DataProcessing
{
    public class AlegeusErrorLog
    {
        private Vars Vars { get; } = new Vars();

        public static async Task ProcessAll()
        {
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    Thread.CurrentThread.Name = "DataProcessing";
                    AlegeusErrorLog DataProcessing = new AlegeusErrorLog();
                    DataProcessing.RetrieveErrorLogs();
                }
            );
        }
        public void USERVERYCAUTIOUSLY_ClearAllTables()
        {
            if (Vars.Environment != "TEST")
            {
                throw new Exception($"Clear All Tables is available only in TEST Environment");
            }
            var dbConn = Vars.dbConnDataProcessing;
            var fileLogParams = Vars.dbFileProcessingLogParams;

            var queryString = "exec [Data_Processing].dbo.zz_truncate_all;";

            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null, fileLogParams.DbMessageLogParams,
                false, false);
        }

        public void RetrieveErrorLogs()
        {
            // init logParams

            //
            var fileLogParams = Vars.dbFileProcessingLogParams;
            var dbConn = Vars.dbConnDataProcessing;
            var ftpConn = Vars.RemoteAlegeusFtpConnection;

            // 

            //DeleteStagingFiles
            DeleteStagingFiles(dbConn, fileLogParams);

            //GetFtpFilesFroMAlegeus
            GetFtpFilesFromAlegeus(ftpConn, fileLogParams);

            ////GetCrmList
            //Import.ImportCrmListFileBulkCopy(DbConn, $"{Vars.fromBoomiFtpRoot}/CRM_List.csv", false,
            //    "dbo.[CRM_List]", FileLogParams);

            //ImportAlegeusResultResFiles
            ImportAlegeusFiles(dbConn, fileLogParams);

            //TrackNewFtpFileErrors
            TrackAllNewFtpFileErrors(dbConn, fileLogParams);

            //MoveFilesToArchive
            MoveFilesToArchive(dbConn, fileLogParams);
        }

        protected void DeleteStagingFiles(DbConnection dbConn,
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
            FileUtils.DeleteFiles(new[] { Vars.AlegeusErrorLogMbiFilesRoot, Vars.AlegeusErrorLogResFilesRoot }
                , false
                , new[] { "*.mbi", "*.dne", "*.txt", "*.res" },
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "ErrorLog-DeleteStagingFiles", "Success",
                        "Delete File in Dir");
                    // do not log - gives too many lines
                    //DbUtils.LogFileOperation(FileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void GetFtpFilesFromAlegeus(SFtpConnection ftpConn,
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
                new string[] { Vars.remoteAlegeusFtpRootPath }, false,
                new string[] { "*.mbi", "*.dne" },
                Vars.AlegeusErrorLogMbiFilesRoot, "", "",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    var headerType = Import.GetAlegeusHeaderTypeFromFile(destFilePath);

                    // add uniqueId to file so we can track it across folders and operations
                    var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(destFilePath, false,
                        fileLogParams);

                    fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), uniqueIdFilePath,
                        Path.GetFileName(uniqueIdFilePath), "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", $"Got MBI/DNE File from FTP");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); throw ex; }
            );


            // download res dir files
            ftpConn.CopyOrMoveFiles(
                FtpFileOperation.DownloadAndDelete,
                new string[] { Vars.remoteAlegeusFtpRootPath }, false,
                new string[] { "*.res" },
                Vars.AlegeusErrorLogResFilesRoot, "", "",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add uniqueId to file so we can track it across folders and operations
                    var headerType = Import.GetAlegeusHeaderTypeFromFile(destFilePath);
                    var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(destFilePath, true,
                        fileLogParams);

                    fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), uniqueIdFilePath,
                        Path.GetFileName(uniqueIdFilePath), "",
                        $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", $"Got Res File from FTP");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); throw ex; }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void ImportAlegeusFiles(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);

            //
            FileUtils.IterateDirectory(
                new[] { Vars.AlegeusErrorLogResFilesRoot, Vars.AlegeusErrorLogMbiFilesRoot }, DirectoryIterateType.Files
                , false, new[] { "*.res", "*.dne", "*.txt", "*.mbi" },
                (srcFilePath, destFilePath, dummy2) =>
                {
                    //
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "ErrorLog-ImportAlegeusFiles", "Starting",
                        "Starting: Import Alegeus File");
                    DbUtils.LogFileOperation(fileLogParams);

                    //2. import file
                    Import.ImportAlegeusFile(dbConn, srcFilePath, false, fileLogParams,
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );

                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "ErrorLog-ImportAlegeusFiles", "Success",
                        "Completed: Import Alegeus File");
                    DbUtils.LogFileOperation(fileLogParams);
                }
                , null
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }


        protected void TrackAllNewFtpFileErrors(DbConnection dbConn,
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
                        exec [dbo].[Data_Processing_track_new_ftp_errors]
                            ";
            // run query
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                fileLogParams?.GetMessageLogParams());

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void MoveFilesToArchive(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //


            // move mbiFiles to Archive
            FileUtils.MoveFiles(
                Vars.AlegeusErrorLogMbiFilesRoot, false, "*.*",
                Vars.AlegeusErrorLogMbiFilesArchiveRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", "Moved File to Archive");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(FileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );
            // move resFiles to Archive
            FileUtils.MoveFiles(
                Vars.AlegeusErrorLogResFilesRoot, false, "*.*",
                Vars.AlegeusErrorLogResFilesArchiveRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", "Moved File to Archive");

                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(FileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        } // routine
    } // end class
} // end namespace