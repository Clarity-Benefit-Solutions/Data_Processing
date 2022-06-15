using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CoreUtils;
using CoreUtils.Classes;

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

            //ParticipantEnrollmentCopyLastFileToStandardPath
            ParticipantEnrollmentCopyLastFileToStandardPath(ftpConn, fileLogParams);

        }

        protected void ParticipantEnrollmentDeleteStagingFiles(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            ////1. Clear all files in AutomatedHeaderV1_Files
            ////echo y| del G:\FTP\AutomatedHeaderV1_Files\*.*
            ////

            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
            //  "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
            ////
            //FileUtils.DeleteFiles(new[] { Vars.AlegeusErrorLogMbiFilesRoot, Vars.AlegeusErrorLogResFilesRoot }
            //  , false
            //  , new[] { "*.mbi", "*.dne", "*.txt", "*.res" },
            //  (srcFilePath, destFilePath, dummy2) =>
            //  {
            //    // add to fileLog
            //    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
            //      Path.GetFileName(destFilePath), destFilePath, "ErrorLog-DeleteStagingFiles", "Success",
            //      "Delete File in Dir");
            //    // do not log - gives too many lines
            //    //DbUtils.LogFileOperation(FileLogParams);
            //  },
            //  (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            //);

            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
            //  "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
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

            ftpConn.CopyOrMoveFiles(
                /* Note: 2022-04-27: Linda S verified files can be deleted from remote after it has been downloaded*/
                // todo: test download and delete
                //FtpFileOperation.Download,
                FtpFileOperation.DownloadAndDelete,
                new string[] { Vars.remoteAlegeusFtpRootPath }, false,
                new string[] { "Enrolled_Participant_Report_*.csv.pgp" },
                tempDownLoadPath, "", "",
                (srcFilePath, destFilePath, file, fileContents) =>
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
                (directory, file, ex) =>
                {
                    DbUtils.LogError(directory, file, ex, fileLogParams);
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
                    destFilePath =
                        $"{Vars.alegeusParticipantEnrollmentFilesDecryptedPath}/{Path.GetFileNameWithoutExtension(srcFilePath)}";
                    FileUtils.DeleteFileIfExists(destFilePath, null, null);
                    //
                    string privateKeyFileName = Utils.GetAppSetting("AlegeusPgpKey1Filepath");
                    string passPhrase = Utils.GetAppSetting("AlegeusPgpKey1Passphrase");
                    ;

                    // decrypt the file and copy decrypted file to destPath
                    PgpUtils.PgpDecryptFile(srcFilePath, destFilePath, privateKeyFileName, passPhrase,
                        (srcFilePath, destFilePath, dummy2) =>
                        {
                            // log
                            fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(destFilePath), destFilePath,
                                $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", $"Decrypted Downloaded Encrypted File");

                            DbUtils.LogFileOperation(fileLogParams);

                            // move processed encrypted file to archive
                            string archiveFilePath =
                                $"{Path.GetDirectoryName(srcFilePath)}/Archive/{Path.GetFileName(srcFilePath)}";
                            FileUtils.MoveFile(srcFilePath, archiveFilePath, null, null);
                            // log
                            fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(archiveFilePath), archiveFilePath,
                                $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", $"Archived Downloaded Encrypted File");

                            DbUtils.LogFileOperation(fileLogParams);
                        },
                        (directory, file, ex) =>
                        {
                            // log
                            fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(destFilePath), destFilePath,
                                $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                                "ERROR", $"Error ion Decrypting Downloaded File");

                            DbUtils.LogFileOperation(fileLogParams);
                            DbUtils.LogError(directory, file, ex, fileLogParams);
                        }
                    );
                },
                (directory, file, ex) =>
                {
                    DbUtils.LogError(directory, file, ex, fileLogParams);
                    throw ex;
                }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }
        protected void ParticipantEnrollmentCopyLastFileToStandardPath(SFtpConnection ftpConn,
         FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            fileLogParams.SetSourceFolderName(Vars.alegeusParticipantEnrollmentFilesDownloadPath);
            //
            DbUtils.LogFileOperation(fileLogParams);
            FileUtils.EnsurePathExists(Vars.alegeusParticipantEnrollmentFilesDownloadPath);

            //get list of all decrypted files
            List<string> files = FileUtils.GetListOfFiles(
                new string[] { Vars.alegeusParticipantEnrollmentFilesDecryptedPath },
                false,
                new string[] { "Enrolled_Participant_Report*.csv" }
                );

            // sort by name asc
            files.Sort((x, y) => string.Compare(x, y));

            // the last file will be the latest one
            var srcFilePath = files.Last();

            // 
            var destFilePath = $"{Path.GetDirectoryName(srcFilePath)}/EPR_ENROLLED_PARTICIPANT.csv";
            FileUtils.CopyFile(srcFilePath, destFilePath, null, null);

            // log
            fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                Path.GetFileName(destFilePath), destFilePath,
                $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Saved Decrypted File as Latest File");

            DbUtils.LogFileOperation(fileLogParams);
            //  // 
            destFilePath = $"\\\\fs009\\FTP-IT\\ToBoomi\\EPR_ENROLLED_PARTICIPANT.csv";
            FileUtils.CopyFile(srcFilePath, destFilePath, null, null);

            // log
            fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                Path.GetFileName(destFilePath), destFilePath,
                $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Copied Latest File To FTP IT");

            DbUtils.LogFileOperation(fileLogParams);
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
            //  "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);

            ////
            //FileUtils.IterateDirectory(
            //  new[] { Vars.AlegeusErrorLogResFilesRoot, Vars.AlegeusErrorLogMbiFilesRoot }, DirectoryIterateType.Files
            //  , false, new[] { "*.res", "*.dne", "*.txt", "*.mbi" },
            //  (srcFilePath, destFilePath, dummy2) =>
            //  {
            //    //
            //    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
            //      Path.GetFileName(destFilePath), destFilePath, "ErrorLog-ImportAlegeusFiles", "Starting",
            //      "Starting: Import Alegeus File");
            //    DbUtils.LogFileOperation(fileLogParams);

            //    //2. import file
            //    Import.ImportAlegeusFile(dbConn, srcFilePath, false, fileLogParams,
            //      (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            //    );

            //    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
            //      Path.GetFileName(destFilePath), destFilePath, "ErrorLog-ImportAlegeusFiles", "Success",
            //      "Completed: Import Alegeus File");
            //    DbUtils.LogFileOperation(fileLogParams);
            //  }
            //  , null
            //);

            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"ErrorLog-{MethodBase.GetCurrentMethod()?.Name}",
            //  "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
        }
    }

}