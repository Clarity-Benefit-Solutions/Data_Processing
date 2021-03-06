using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CoreUtils;
using CoreUtils.Classes;

namespace DataProcessing
{
    public class IncomingFileProcessing
    {
        private Vars Vars { get; } = new Vars();

        public static async Task CopyTestFiles(string ftpSubFolderPath = "")
        {
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    //
                    Vars.ftpSubFolderPath = ftpSubFolderPath;
                    //
                    // set process environment
                    Vars vars = new Vars();
                    var directoryPath = $"{vars.localFtpRoot}/..";
                    Process.Start(
                        $"{directoryPath}/copy_test_files.bat");
                }
            );
        }

        public static async Task ProcessAll(string ftpSubFolderPath = "")
        {
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    //Debug.Assert(false);

                    // set process environment
                    Vars.ftpSubFolderPath = ftpSubFolderPath;
                    //
                    Thread.CurrentThread.Name = "IncomingFileProcessing";
                    var fileProcessing = new IncomingFileProcessing();
                    fileProcessing.ProcessAllFiles();
                }
            );
        }

        public void ProcessAllFiles()
        {
            //
            var fileLogParams = this.Vars.dbFileProcessingLogParams;
            var dbConn = this.Vars.dbConnDataProcessing;

            //MoveIncomingFilesToProcessingDirs
            this.MoveIncomingFilesToProcessingDirs(dbConn, fileLogParams);

            //ProcessIncomingCobraFiles
            this.ProcessIncomingCobraFiles(dbConn, fileLogParams);

            //ProcessIncomingAlegeusFiles
            this.ProcessIncomingAlegeusFiles(dbConn, fileLogParams);
        }

        protected void AddAlegeusHeaderForAllFiles(DbConnection dbConn, FileOperationLogParams fileLogParams)

        {
            // Iterate all files in header dir
            FileUtils.IterateDirectory(this.Vars.alegeusFileHeadersRoot, DirectoryIterateType.Files, false,
                new string[] { "*.mbi", "*.csv", "*.txt", "*.xls", "*.xlsx" },
                (srcFilePath, destFilePath, dummy2) =>
                {
                    try
                    {
                        // 1. if excel file, convert to csv and delete it
                        if (FileUtils.IsExcelFile(srcFilePath))
                        {
                            var csvFilePath =
                                $"{Path.GetDirectoryName(srcFilePath)}/{Path.GetFileNameWithoutExtension(srcFilePath)}.csv";

                            FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                                Import.GetPasswordsToOpenExcelFiles(srcFilePath),
                                null,
                                null);

                            FileUtils.DeleteFile(srcFilePath, null, null);
                            // Log
                            fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(csvFilePath), csvFilePath,
                                $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", $"Converted Excel File to Csv");
                            DbUtils.LogFileOperation(fileLogParams);

                            //
                            srcFilePath = csvFilePath;
                        }

                        // 2. get header type for file
                        var headerType = Import.GetAlegeusHeaderTypeFromFile(srcFilePath, false);

                        //3. truncate staging table
                        var tableName = "[dbo].[alegeus_file_staging]";
                        DbUtils.TruncateTable(dbConn, tableName,
                            fileLogParams?.GetMessageLogParams());

                        //4. import file
                        //
                        ImpExpUtils.ImportSingleColumnFlatFile(dbConn, srcFilePath, srcFilePath, tableName,
                            "folder_name",
                            "data_row",
                            (filePath1, rowNo, line) =>
                            {
                                if (Utils.IsBlank(line))
                                {
                                    return false;
                                }

                                if (
                                    // skip if line is not of a import row Type
                                    !Import.IsAlegeusImportLine(line)
                                )
                                {
                                    return false;
                                }

                                return true;
                            },
                            fileLogParams,
                            (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                        );

                        // 5. create headers
                        string procName;
                        switch (headerType)
                        {
                            case HeaderType.Own:
                                procName = "dbo.[proc_alegeus_AlterHeadersOwn]";
                                break;

                            case HeaderType.Old:
                                procName = "dbo.[proc_alegeus_AlterHeaders2015]";
                                break;

                            case HeaderType.NoChange:
                                procName = "dbo.[proc_alegeus_AlterHeadersNone]";
                                break;

                            case HeaderType.New:
                                procName = "dbo.[proc_alegeus_AlterHeaders2019]";
                                break;
                            //todo: how to add headers for segmented funding?
                            case HeaderType.SegmentedFunding:
                                procName = "dbo.[proc_alegeus_AlterHeaders2019]";
                                break;

                            default:
                                var message =
                                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : headerType : {headerType} is invalid";
                                throw new Exception(message);
                        }

                        //
                        if (!Utils.IsBlank(procName))
                        {
                            var queryString = " ";
                            queryString += $" EXEC {procName};" + "\r\n";
                            // run fix headers query
                            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                                fileLogParams?.GetMessageLogParams());
                        }

                        //6. Export File with proper headers
                        var expFilePath =
                            $"{Path.GetDirectoryName(srcFilePath)}/{Path.GetFileNameWithoutExtension(srcFilePath)}.mbi";

                        // delete src file to avoid duplicates
                        FileUtils.DeleteFile(srcFilePath, null, null);

                        var outputTableName = "[dbo].[alegeus_file_final]";
                        var queryStringExp = $"Select * from {outputTableName} order by row_num asc";
                        ImpExpUtils.ExportSingleColumnFlatFile(expFilePath, dbConn, queryStringExp,
                            "file_row", null, fileLogParams,
                            (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                        );

                        // Log
                        fileLogParams?.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                            Path.GetFileName(expFilePath), expFilePath, "IncomingFileProcessing-AddHeaderToFile",
                            "Success",
                            "Added Header to File");
                        DbUtils.LogFileOperation(fileLogParams);

                        // 7. move file to PreCheck
                        string destPreCheckPath = $"{Vars.alegeusFilesToProcessPath}/{Path.GetFileNameWithoutExtension(expFilePath)}.mbi";
                        FileUtils.MoveFile(expFilePath, destPreCheckPath, null, null);

                        // add to fileLog
                        fileLogParams?.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                            Path.GetFileName(destPreCheckPath), destPreCheckPath,
                            "IncomingFileProcessing-MoveHeaderFileToPreCheckDir",
                            "Success", "Moved Header File to PreCheck Dir");
                        // do not log - gives too many lines
                        // DbUtils.LogFileOperation(FileLogParams);
                    }
                    catch (Exception ex2)
                    {
                        fileLogParams?.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                            Path.GetFileName(srcFilePath), srcFilePath,
                            $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                            "ERROR", ex2.ToString());
                        DbUtils.LogFileOperation(fileLogParams);

                        // reject file
                        var completeFilePath = srcFilePath;
                        Import.MoveFileToPlatformRejectsFolder(completeFilePath, ex2.ToString(), Vars.alegeusFilesRejectsPath);
                        ;
                    }
                },
                (directory, file, ex) => { Import.MoveFileToPlatformRejectsFolder(file, ex.ToString()); }
            );
        }

        protected void MoveIncomingFilesToProcessingDirs(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            ////
            //fileLogParams.SetFileNames("", "", "", "", "",
            //    $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);

            //1. Get list of folders for header from DB
            //decide table name
            var tableName = "dbo.[FTP_Source_Folders]";

            // run query - we take only by environment so we can test
            var queryString =
                $"Select * from {tableName} where environment = '{Vars.RunTimeEnvironment}' and is_active = 1  order by folder_name;";
            var dtHeaderFolders = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConn, queryString);

            //3. for each header folder, get file and move to header1 folder
            foreach (DataRow row in dtHeaderFolders.Rows)
            {
                var rowFolderName = row["folder_name"].ToString();
                var rowBenCode = row["BENCODE"].ToString();
                var rowTemplateType = row["template_type"].ToString();
                var rowIcType = row["IC_type"].ToString();
                var rowtoFtp = row["to_FTP"].ToString();

                // 3. for each source folder
                if (!Utils.IsBlank(rowFolderName))
                {
                    // change from PROD source dir to Ctx source dir
                    rowFolderName = this.Vars.ConvertFilePathFromProdToCtx(rowFolderName);

                    // we need to set these first before setting folderName
                    var fileLogParams1 = this.Vars.dbFileProcessingLogParams;

                    //fileLogParams.SetFileNames("", Path.GetFileName(rowFolderName), rowFolderName,
                    //    Path.GetFileName(rowFolderName), "",
                    //    $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                    //    "Starting", "Started Iterating Directory");
                    ////
                    //fileLogParams1.Bencode = rowBenCode;
                    //fileLogParams1.TemplateType = rowTemplateType;
                    //fileLogParams1.IcType = rowIcType;
                    //fileLogParams1.ToFtp = rowtoFtp;
                    //fileLogParams1.SetSourceFolderName(rowFolderName);
                    //DbUtils.LogFileOperation(fileLogParams);

                    ////
                    // 3a. set unique fileNames for each file in source folder and add to file Log and move to Holding Dir
                    FileUtils.IterateDirectory(
                        new string[] { rowFolderName }, DirectoryIterateType.Files, false, new string[] { "*.csv", "*.xlsx", "*.xls", "*.txt", "*.mbi" },
                        (srcFilePath, destFilePath, dummy2) =>
                        {
                            this.MoveIncomingFileToNextStepDir(srcFilePath, dbConn, fileLogParams);
                        },
                        (directory, file, ex) =>
                        {
                            if (!Utils.IsBlank(file))
                            {
                                Import.MoveFileToPlatformRejectsFolder(file, ex);
                            }
                            else
                            {
                                DbUtils.LogError(directory, file, ex, fileLogParams);
                            }
                        }
                    );
                }
            } // each dr
        }

        protected void MoveIncomingFileToNextStepDir(string srcFilePath, DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            var currentFilePath = srcFilePath;
            try
            {
                //1. fix path
                srcFilePath = FileUtils.FixPath(srcFilePath);

                //
                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                    Path.GetFileName(srcFilePath), srcFilePath,
                    $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                    "Success", "Found Source File");
                DbUtils.LogFileOperation(fileLogParams);

                // 2. archive source in source subfolder
                string srcArchiveDir = $"{Path.GetDirectoryName(srcFilePath)}/Archive/{Utils.ToIsoDateString(DateTime.Now)}";
                string srcArchivePath = $"{srcArchiveDir}/{Path.GetFileName(srcFilePath)}";

                // handle long paths
                if (srcArchivePath.Length >= 250)
                {
                    srcArchivePath = $"{Utils.Left(srcArchivePath, 250)}{Path.GetExtension(srcArchivePath)}";
                }
                //
                try
                {
                    FileUtils.CopyFile(srcFilePath, srcArchivePath, null, null);
                }
                catch (Exception ex)
                {
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(srcFilePath), srcFilePath,
                        $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                        "Error", $"Archiving File Error: {ex.Message}");

                    DbUtils.LogFileOperation(fileLogParams);
                }

                // 2B. Convert excel file to csv now itself so any password protected files that cannot be opened will be rejected. Also we can look inside the file easier
                // convert from xl is needed
                if (FileUtils.IsExcelFile(srcFilePath))
                {
                    var csvFilePath = $"{Path.GetDirectoryName(srcFilePath)}/{Path.GetFileNameWithoutExtension(srcFilePath)}.csv";
                    FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                        Import.GetPasswordsToOpenExcelFiles(srcFilePath),
                        null,
                        null);
                    FileUtils.DeleteFile(srcFilePath, null, null);
                    srcFilePath = csvFilePath;
                }

                var platformType = Import.GetPlatformTypeForFile(srcFilePath);

                // 4. make FilenameProperty uniform
                var uniformFilePath = Import.GetUniformNameForFile(platformType, srcFilePath);
                if (Path.GetFileName(srcFilePath) != Path.GetFileName(uniformFilePath))
                {
                    FileUtils.MoveFile(srcFilePath, uniformFilePath, null, null);
                    currentFilePath = uniformFilePath;
                }

                // 5. add uniqueId to file so we can track it across folders and operations
                var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(uniformFilePath, true, true, fileLogParams);
                currentFilePath = uniqueIdFilePath;

                // 6. Suffix uniquePath to Archived Source file so we can trace back from passed/reject file
                string srcArchiveCombinedPath = $"{srcArchivePath}---{Path.GetFileName(uniqueIdFilePath)}";
                FileUtils.MoveFile(srcArchivePath, srcArchiveCombinedPath, null, null);

                // 7. move source to platform holding dir
                string destDirHolding;
                switch (platformType)
                {
                    case PlatformType.Alegeus:
                        destDirHolding = Vars.alegeusFilesImportHoldingPath;
                        break;

                    case PlatformType.Cobra:
                        destDirHolding = Vars.cobraFilesImportHoldingPath;
                        break;

                    default:
                        destDirHolding = Vars.unknownFilesImportHoldingPath;
                        break;
                }

                // 8. copy source file to holding archive root
                string destPathHoldingArchive = $"{destDirHolding}/Archive/{Utils.ToIsoDateString(DateTime.Now)}/{Path.GetFileName(currentFilePath)}";
                FileUtils.CopyFile(currentFilePath, destPathHoldingArchive, null, null);

                // 9. move source file to holding root
                string destPathHolding = $"{destDirHolding}/{Path.GetFileName(currentFilePath)}";
                FileUtils.MoveFile(currentFilePath, destPathHolding, null, null);
                //
                currentFilePath = destPathHolding;

                // 10. log move to holding
                fileLogParams.SetFileNames("", Path.GetFileName(currentFilePath), currentFilePath,
                    Path.GetFileName(destPathHolding), destPathHolding,
                    $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                    "Success", "Moved Source File to Holding");
                DbUtils.LogFileOperation(fileLogParams);

                // rename txt and mbi files to csv
                if (FileUtils.IsEmptyFile(currentFilePath))
                {
                    string emptyFilePath;
                    if (platformType == PlatformType.Cobra)
                    {
                        emptyFilePath = $"{Vars.cobraFilesEmptyPath}/{Path.GetFileName(currentFilePath)}";
                    }
                    else if (platformType == PlatformType.Alegeus)
                    {
                        emptyFilePath = $"{Vars.alegeusFilesEmptyPath}/{Path.GetFileName(currentFilePath)}";
                    }
                    else
                    {
                        emptyFilePath = $"{Vars.unknownFilesEmptyPath}/{Path.GetFileName(currentFilePath)}";
                    }

                    FileUtils.MoveFile(currentFilePath, emptyFilePath,
                        (srcFilePath2, destFilePath2, dummy4) =>
                        {
                            //
                            fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                            Path.GetFileName(emptyFilePath), emptyFilePath,
                            "IncomingFileProcessing-MoveEmptyFiles", "Success",
                            $"Moved Empty file to Archive - Done folder");

                            DbUtils.LogFileOperation(fileLogParams);
                        },
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );

                    // don't process any more
                    return;
                }

                // only for alegeus move to headers root
                if (platformType == PlatformType.Alegeus)
                {
                    // 11. move Alegeus Files to headers Dir
                    string headerPath = $"{Vars.alegeusFileHeadersRoot}/{Path.GetFileName(destPathHolding)}";
                    FileUtils.MoveFile(destPathHolding, headerPath, null, null);
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(destPathHolding), destPathHolding,
                        Path.GetFileName(headerPath), headerPath, "IncomingFileProcessing-CopyToHeadersDir", "Success",
                        "Copied Alegeus File to headers Directory");
                }
            }
            catch (Exception ex2)
            {
                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                    Path.GetFileName(srcFilePath), srcFilePath,
                    $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                    "ERROR", ex2.ToString());
                DbUtils.LogFileOperation(fileLogParams);
                //
                var completeFilePath = currentFilePath;
                if (Path.GetFileName(srcFilePath) != Path.GetFileName(currentFilePath))
                {
                    completeFilePath =
                        $"{Path.GetDirectoryName(currentFilePath)}/{Path.GetFileName(srcFilePath)}---{Path.GetFileName(currentFilePath)}";
                    FileUtils.MoveFile(currentFilePath, completeFilePath, null, null);
                }

                Import.MoveFileToPlatformRejectsFolder(completeFilePath, ex2.ToString());
                ;
            }
        }

        protected void PreCheckAndProcessAlegeusFiles(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            ////
            //fileLogParams.SetFileNames("", "", "", "", "",
            //    $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.IterateDirectory(this.Vars.alegeusFilesToProcessPath, DirectoryIterateType.Files, false, "*.mbi",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // ensure file ext is mbi
                    if (Path.GetExtension(srcFilePath).ToLower() != ".mbi")
                    {
                        string mbiFilePath = $"{Path.GetDirectoryName(srcFilePath)}/{Path.GetFileNameWithoutExtension(srcFilePath)}.mbi";
                        if (Path.GetFileName(mbiFilePath) != Path.GetFileName(srcFilePath))
                        {
                            FileUtils.MoveFile(srcFilePath, mbiFilePath, null, null);
                            srcFilePath = mbiFilePath;
                        }
                    }
                    // check the file
                    using var fileChecker = new FileChecker(srcFilePath, PlatformType.Alegeus,
                        this.Vars.dbConnDataProcessing, fileLogParams,
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );

                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(srcFilePath), srcFilePath,
                        $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                        "Starting", "Starting PreCheck");
                    DbUtils.LogFileOperation(fileLogParams);

                    //
                    fileChecker.CheckFileAndProcess(FileCheckType.AllData, FileCheckProcessType.MoveToDestDirectories);
                },
                (directory, file, ex) => { Import.MoveFileToPlatformRejectsFolder(file, ex.ToString()); }
            );

            ////
            //fileLogParams.SetFileNames("", "", "", "", "",
            //    $"IncomingFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
        }

        protected void PreCheckAndProcessCobraFiles(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            ////
            //fileLogParams.SetFileNames("", "", "", "", "",
            //    $"CobraFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
            ////
            FileUtils.IterateDirectory(
                new string[] { Vars.cobraFilesImportHoldingPath, Vars.cobraFilesPreparedQbPath },
                DirectoryIterateType.Files,
                false,
                new string[] { "*.*" },
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // ensure file ext is csv
                    if (Path.GetExtension(srcFilePath).ToLower() != ".csv")
                    {
                        string csvFilePath = $"{Path.GetDirectoryName(srcFilePath)}/{Path.GetFileNameWithoutExtension(srcFilePath)}.csv";
                        if (Path.GetFileName(csvFilePath) != Path.GetFileName(srcFilePath))
                        {
                            FileUtils.MoveFile(srcFilePath, csvFilePath, null, null);
                            srcFilePath = csvFilePath;
                        }
                    }
                    // check the file
                    using var fileChecker = new FileChecker(srcFilePath, PlatformType.Cobra,
                        this.Vars.dbConnDataProcessing, fileLogParams,
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );

                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(srcFilePath), srcFilePath,
                        $"CobraFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                        "Starting", "Starting PreCheck");
                    DbUtils.LogFileOperation(fileLogParams);

                    //
                    fileChecker.CheckFileAndProcess(FileCheckType.AllData, FileCheckProcessType.MoveToDestDirectories);
                },
                (directory, file, ex) => { Import.MoveFileToPlatformRejectsFolder(file, ex.ToString()); }
            );

            ////
            //fileLogParams.SetFileNames("", "", "", "", "",
            //    $"CobraFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
        }

        protected void PrepareCobraQbFile(string srcFilePath, DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            // DB connection for COBRA specific
            DbConnection dbConnCobra = Vars.dbConnDataProcessing;

            //1. truncate staging table
            string tableName = @"[dbo].[QB_file_data_fixtbl]";
            DbUtils.TruncateTable(dbConnCobra, tableName,
                fileLogParams?.GetMessageLogParams());

            //2. import file
            string procName = @"dbo.[Fix_COBRAQB_SSObollean]";
            ImpExpUtils.ImportSingleColumnFlatFile(dbConnCobra, srcFilePath, srcFilePath, tableName,
                "folder_name",
                "QB_data",
                (filePath1, rowNo, line) => { return true; },
                fileLogParams,
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //3. run script to fix data
            string queryString = "";

            // fix header proc
            if (!Utils.IsBlank(procName))
            {
                queryString += $" EXEC {procName};" + "\r\n";
            }
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConnCobra, queryString, null, fileLogParams?.GetMessageLogParams());

            //4. Export File
            string expFilePath = FileUtils.GetDestFilePath(srcFilePath, "");
            //
            var outputTableName = tableName;
            string queryStringExp = $"Select * from {outputTableName} order by row_num asc";
            //
            ImpExpUtils.ExportSingleColumnFlatFile(expFilePath, dbConnCobra, queryStringExp,
        "QB_data", null, fileLogParams,
        (directory, file, ex) =>
        {
            DbUtils.LogError(directory, file, ex, fileLogParams);
        }
            );

            // add to fileLog
            fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
        Path.GetFileName(expFilePath), expFilePath, $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
        "Success", $"Prepared Flat File for COBRA"
            );
            DbUtils.LogFileOperation(fileLogParams);
        }

        protected void PrepareIncomingCobraQbFiles(DbConnection dbConn, FileOperationLogParams fileLogParams)

        {
            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////

            string fileExt = "*.csv";
            //
            FileUtils.IterateDirectory(
                new string[] { Vars.cobraFilesPreparedQbPath, Vars.cobraFilesDecryptPath },
                DirectoryIterateType.Files, false,
                new string[] { fileExt },
                (srcFilePath, destFilePath, dummy2) =>
                {
                    this.PrepareCobraQbFile(srcFilePath, dbConn, fileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            ); // IterateDir

            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////
        }

        protected void ProcessIncomingAlegeusFiles(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //AddAlegeusHeaderForAllFiles
            this.AddAlegeusHeaderForAllFiles(dbConn, fileLogParams);

            //PreCheckAndProcessAlegeusFiles
            this.PreCheckAndProcessAlegeusFiles(dbConn, fileLogParams);
        }

        protected void ProcessIncomingCobraFiles(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            // TriageIncomingCobraFiles
            this.TriageIncomingCobraFiles(dbConn, fileLogParams);

            // PrepareIncomingCobraQbFiles
            this.PrepareIncomingCobraQbFiles(dbConn, fileLogParams);

            // MoveIncomingCobraFilesToProcessingDir
            this.PreCheckAndProcessCobraFiles(dbConn, fileLogParams);
        }

        protected void TriageIncomingCobraFiles(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            ////
            //fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
            //    "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            //DbUtils.LogFileOperation(fileLogParams);
            ////

            // 1. txt -> csv, mbi -> csv
            FileUtils.IterateDirectory(
                Vars.cobraFilesImportHoldingPath, DirectoryIterateType.Files, false, "*.*",
                (srcFilePath, dummy1, dummy2) =>
                {
                    FileInfo fileInfo = new FileInfo(srcFilePath);
                    if (fileInfo.Extension == ".txt" || fileInfo.Extension == ".mbi")
                    {
                        string csvFilePath =
                            $"{fileInfo.Directory}/{Path.GetFileNameWithoutExtension(fileInfo.Name)}.csv";

                        FileUtils.MoveFile(srcFilePath, csvFilePath, null, null);
                        fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                            Path.GetFileName(csvFilePath), csvFilePath,
                            $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Success",
                            $"Renamed file to *.csv");
                        DbUtils.LogFileOperation(fileLogParams);
                        //
                        srcFilePath = csvFilePath;
                    }

                    // move qb csv file to PreparedQBRoot
                    if (Import.IsCobraImportQbFile(srcFilePath)
                    /*|| Import.IsCobraImportSpmFile(srcFilePath)*/
                    )
                    {
                        string destFilePath =
                            $"{Vars.cobraFilesPreparedQbPath}/{fileInfo.Name}";

                        //
                        FileUtils.MoveFile(srcFilePath, destFilePath, null, null);

                        fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                            Path.GetFileName(destFilePath), destFilePath,
                            "CobraProcessing-MoveQBCsvFilesToPreparedQB", "Success",
                            $"Moved QB File to MoveQBCsvFilesToPreparedQB folder");

                        DbUtils.LogFileOperation(fileLogParams);
                    }
                }
                ,
                 null
                ); // iterate
        } // routine

        // routine
    } // end class
} // end namespace