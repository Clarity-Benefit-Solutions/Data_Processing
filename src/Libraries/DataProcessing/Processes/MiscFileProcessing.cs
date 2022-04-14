using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;

// ReSharper disable All

namespace DataProcessing
{


    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class MiscFileProcessing
    {
        private Vars Vars { get; } = new Vars();

        public static async Task ProcessAll()
        {
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    Thread.CurrentThread.Name = "ProcessMiscFiles";
                    MiscFileProcessing fileProcessing = new MiscFileProcessing();
                    fileProcessing.ProcessParticipantEnrollmentFilesInternal();
                }
            );
        }

        public static async Task ProcessParticipantEnrollmentFiles()
        {
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    Thread.CurrentThread.Name = "ProcessMiscFiles";
                    MiscFileProcessing fileProcessing = new MiscFileProcessing();
                    fileProcessing.ProcessParticipantEnrollmentFilesInternal();
                }
            );
        }

        public void ProcessParticipantEnrollmentFilesInternal()
        {
            // init logParams
            MessageLogParams logParams = Vars.dbMessageLogParams;
            logParams.SetSubModuleStepAndCommand("ProcessParticipantEnrollmentFiles", "", "", "");

            //
            FileOperationLogParams fileLogParams = Vars.dbFileProcessingLogParams;

            // DbConn for logging
            DbConnection dbConn = fileLogParams.DbConnection;

            //ftpConn
            var ftpConn = Vars.RemoteAlegeusFtpConnection;

            //ParticipantEnrollmentDeleteStagingFiles
            ParticipantEnrollmentDeleteStagingFiles(dbConn, fileLogParams);

            //ParticipantEnrollmentGetFtpFilesFromAlegeus
            ParticipantEnrollmentGetFtpFilesFromAlegeus(ftpConn, fileLogParams);

            //ParticipantEnrollmentDecryptFiles
            ParticipantEnrollmentDecryptFiles(ftpConn, fileLogParams);

            ////ParticipantEnrollmentImportFiles
            //ParticipantEnrollmentImportFiles(dbConn, fileLogParams);
        }

        protected void ParticipantEnrollmentDeleteStagingFiles(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {

            ////1. Clear all files in AutomatedHeaderV1_Files
            ////echo y| del  G:\FTP\AutomatedHeaderV1_Files\*.*
            ////

            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
            ////
            //FileUtils.DeleteFiles(new[] { Vars.AlegeusErrorLogMbiFilesRoot, Vars.AlegeusErrorLogResFilesRoot }
            //    , false
            //    , new[] { "*.mbi", "*.dne", "*.txt", "*.res" },
            //    (srcFilePath, destFilePath, dummy2) =>
            //    {
            //        // add to fileLog
            //        fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
            //            Path.GetFileName(destFilePath), destFilePath, "ErrorLog-DeleteStagingFiles", "Success",
            //            "Delete File in Dir");
            //        // do not log - gives too many lines
            //        //DbUtils.LogFileOperation(FileLogParams);
            //    },
            //    (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            //);

            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
        }


        protected void ParticipantEnrollmentGetFtpFilesFromAlegeus(SFtpConnection ftpConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            fileLogParams.SetSourceFolderName(Vars.remoteAlegeusFtpRootPath);
            //
            DbUtils.LogFileOperation(fileLogParams);
            string tempDownLoadPath = FileUtils.FixPath(Path.GetTempPath());

            // todo: delete the file after download?
            ftpConn.CopyOrMoveFiles(
                //FtpFileOperation.DownloadAndDelete,
                FtpFileOperation.Download,
                new string[] { Vars.remoteAlegeusFtpRootPath }, false,
                new string[] { "Enrolled_Participant_Report_*.csv.pgp" },
                tempDownLoadPath, "", "",
                (srcFilePath, destFilePath, fileContents) =>
                {

                    // move to final path
                    string downloadedFilePath =
                        $"{Vars.alegeusParticipantEnrollmentFilesDownloadPath}/{Path.GetFileName(destFilePath)}";
                    //
                    FileUtils.MoveFile(destFilePath, downloadedFilePath, null, null);

                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", $"Got Participant Enrollment File from FTP");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) =>
                {
                    DbUtils.LogError(arg1, arg2, ex, fileLogParams);
                    throw ex;
                }
            );


            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void ParticipantEnrollmentDecryptFiles(SFtpConnection ftpConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            fileLogParams.SetSourceFolderName(Vars.alegeusParticipantEnrollmentFilesDownloadPath);
            //
            DbUtils.LogFileOperation(fileLogParams);
            FileUtils.EnsurePathExists(Vars.alegeusParticipantEnrollmentFilesDownloadPath);

            //decrypt all files
            FileUtils.IterateDirectory(
                new string[] { Vars.alegeusParticipantEnrollmentFilesDownloadPath },
                DirectoryIterateType.Files,
                false,
                new string[] { "Enrolled_Participant_Report*.csv.pgp" },
                (srcFilePath, destFilePath, fileContents) =>
                {
                    FileUtils.EnsurePathExists(Vars.alegeusParticipantEnrollmentFilesDecryptedPath);
                    // just remove the last .pgp
                    destFilePath = $" {Vars.alegeusParticipantEnrollmentFilesDecryptedPath}/{Path.GetFileNameWithoutExtension(srcFilePath)}";
                    FileUtils.DeleteFileIfExists(destFilePath, null, null);
                    //
                    string privateKeyFileName = Utils.GetAppSetting("AlegeusPgpKey1Filepath");
                    string passPhrase = Utils.GetAppSetting("AlegeusPgpKey1Passphrase"); ;

                    // decrypt the file and copy decrypted file to destPath
                    PgpUtils.PgpDecryptFile(srcFilePath, destFilePath, privateKeyFileName, passPhrase,
                        (srcFilePath, destFilePath, dummy2) =>
                        {
                            // log
                            fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(destFilePath), destFilePath, $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", $"Decrypted Downloaded File");

                            DbUtils.LogFileOperation(fileLogParams);

                        },
                        (arg1, arg2, ex) =>
                        {
                            // log
                            fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(destFilePath), destFilePath, $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                                "ERROR", $"Error ion Decrypting Downloaded File");

                            DbUtils.LogFileOperation(fileLogParams);
                            DbUtils.LogError(arg1, arg2, ex, fileLogParams);
                        }
                    );

                },
                (arg1, arg2, ex) =>
                {
                    DbUtils.LogError(arg1, arg2, ex, fileLogParams);
                    throw ex;
                }
            );


            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void ParticipantEnrollmentImportFiles(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);

            ////
            //FileUtils.IterateDirectory(
            //    new[] { Vars.AlegeusErrorLogResFilesRoot, Vars.AlegeusErrorLogMbiFilesRoot }, DirectoryIterateType.Files
            //    , false, new[] { "*.res", "*.dne", "*.txt", "*.mbi" },
            //    (srcFilePath, destFilePath, dummy2) =>
            //    {
            //        //
            //        fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
            //            Path.GetFileName(destFilePath), destFilePath, "ErrorLog-ImportAlegeusFiles", "Starting",
            //            "Starting: Import Alegeus File");
            //        DbUtils.LogFileOperation(fileLogParams);

            //        //2. import file
            //        Import.ImportAlegeusFile(dbConn, srcFilePath, false, fileLogParams,
            //            (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            //        );

            //        fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
            //            Path.GetFileName(destFilePath), destFilePath, "ErrorLog-ImportAlegeusFiles", "Success",
            //            "Completed: Import Alegeus File");
            //        DbUtils.LogFileOperation(fileLogParams);
            //    }
            //    , null
            //);

            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
        }

    }
}
