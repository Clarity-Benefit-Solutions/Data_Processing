﻿using System;
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

    public class AlegeusDataProcessing
    {
        private Vars Vars { get; } = new Vars();

        public static async Task ProcessAll()
        {
            // ensure cobra files are moved
            await CobraDataProcessing.ProcessAll();

            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    Thread.CurrentThread.Name = "ProcessAlegeusFiles";
                    var alegeusDataProcessing = new AlegeusDataProcessing();
                    alegeusDataProcessing.ProcessAllFiles();
                }
            );
        }

        public static async Task CopyTestFiles()
        {
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    var directoryPath = Vars.GetProcessBaseDir();
                    Process.Start(
                        $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_mbi+res_to_export_ftp.bat");
                    Process.Start(
                        $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_source_files_to_import_ftp.bat");
                    Process.Start(
                        $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_COBRA_source_files_to_import_ftp.bat");
                }
            );
        }

        public void ProcessAllFiles()
        {
            //
            var fileLogParams = this.Vars.dbFileProcessingLogParams;

            // DbConn
            var dbConn = this.Vars.dbConnDataProcessing;

            // CreateHeaders
            this.CreateHeaders(dbConn, fileLogParams);

            //PreCheckFilesAndProcess
            this.PreCheckFilesAndProcess(HeaderType.NotApplicable, dbConn, fileLogParams);
        }

        public void CreateHeaders(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //MoveSourceFilesToCobraDirs
            this.MoveSourceFilesToHeaderDirs(dbConn, fileLogParams);

            //ConvertExcelFilesToCsv
            this.ConvertAllHeaderExcelFilesToCsv(dbConn, fileLogParams);

            this.AddHeaderToAllHeaderDirFiles(dbConn, fileLogParams);

            ////CopyHoldAllFilesToPreCheckDir
            //CopyHoldAllFilesToPreCheckDir(dbConn, fileLogParams);

            //RenamePreCheckDirFilesToMbi
            this.MoveHeaderDirFilesToPreCheck(dbConn, fileLogParams);

            //RemoveDuplicateFilesInPreCheckDir
            this.RemoveDuplicateFilesInPreCheckDir(dbConn, fileLogParams);
        }

        protected void MoveSourceFilesToHeaderDirs(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);

            /*//1. Clear all files in AutomatedHeaderV1_Files
            //echo y| del  G:\FTP\AutomatedHeaderV1_Files\*.*
            //
            FileUtils.DeleteFiles(Vars.alegeusFileHeadersRoot, false, "*.*",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AlegeusFileProcessing-ClearAllFiles", "Success",
                        "Deleted File in Header Dir");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(FileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );*/

            //2. Get list of folders for header from DB
            //decide table name
            var tableName = "dbo.[FTP_Source_Folders]";

            // run query - we take only by environment so we can test 
            var queryString =
                $"Select * from {tableName} where environment = '{Vars.Environment}' and is_active = 1  order by folder_name;";
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

                    fileLogParams.SetFileNames("", Path.GetFileName(rowFolderName), rowFolderName,
                        Path.GetFileName(rowFolderName), "",
                        $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                        "Starting", "Started Iterating Directory");
                    //
                    fileLogParams1.Bencode = rowBenCode;
                    fileLogParams1.TemplateType = rowTemplateType;
                    fileLogParams1.IcType = rowIcType;
                    fileLogParams1.ToFtp = rowtoFtp;
                    fileLogParams1.SetSourceFolderName(rowFolderName);
                    DbUtils.LogFileOperation(fileLogParams);

                    //
                    // 3a. set unique fileNames for each file in source folder and add to file Log
                    FileUtils.IterateDirectory(
                        rowFolderName, DirectoryIterateType.Files, false, "*.*",
                        (srcFilePath, destFilePath, dummy2) =>
                        {
                            var currentFilePath = srcFilePath;
                            try
                            {
                                // fix path
                                srcFilePath = FileUtils.FixPath(srcFilePath);

                                // make FilenameProperty uniform
                                var uniformFilePath = Import.GetUniformNameForFile(PlatformType.Alegeus, srcFilePath);
                                if (Path.GetFileName(srcFilePath) != Path.GetFileName(uniformFilePath))
                                {
                                    FileUtils.MoveFile(srcFilePath, uniformFilePath, null, null);
                                    currentFilePath = uniformFilePath;
                                }
                                // check if we can get the header typ[e - if not it is an invalid file - do not rename

                                var headerType = Import.GetAlegeusHeaderTypeFromFile(currentFilePath);

                                // add uniqueId to file so we can track it across folders and operations
                                var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(uniformFilePath, true,
                                    fileLogParams1);
                                currentFilePath = uniqueIdFilePath;

                                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                    Path.GetFileName(uniqueIdFilePath), uniqueIdFilePath,
                                    $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                                    "Success", "Found Source File");
                                DbUtils.LogFileOperation(fileLogParams);
                            }
                            catch (Exception ex2)
                            {
                                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                    Path.GetFileName(srcFilePath), srcFilePath,
                                    $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
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

                                Import.MoveFileToAlegeusRejectsFolder(completeFilePath, ex2.ToString());
                                ;
                            }
                        },
                        (directory, file, ex) => { Import.MoveFileToAlegeusRejectsFolder(file, ex.ToString()); }
                    );

                    // 3b. move all source files (with new names) to Headers dir
                    FileUtils.MoveFiles(
                        rowFolderName, false, "*.*", this.Vars.alegeusFileHeadersRoot, "", "",
                        (srcFilePath, destFilePath, fileContents) =>
                        {
                            // add to fileLog
                            fileLogParams1.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(destFilePath), destFilePath, "AlegeusFileProcessing-MoveFilesToHeader",
                                "Success", "Copied Source File to Header Directory");
                            DbUtils.LogFileOperation(fileLogParams1);
                        },
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );
                }
            } // each dr

            //4. copy all header files to Archive root
            FileUtils.CopyFiles(this.Vars.alegeusFileHeadersRoot, false, "*.*", this.Vars.alegeusFileHeadersArchiveRoot,
                "", "",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AlegeusFileProcessing-CopyToArchive", "Success",
                        "Copied File to Archive Directory");

                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(FileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //5a. delete all remnant HoldAll files 
            FileUtils.DeleteFiles(this.Vars.alegeusFilesPreCheckHoldAllRoot, false, "*.*",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AlegeusFileProcessing-DeleteAllFilesInHoldAll",
                        "Success", "Deleted File In HoldAll Directory");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(FileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //5b: copy all header files to HoldAll
            FileUtils.CopyFiles(this.Vars.alegeusFileHeadersRoot, false, "*.*",
                this.Vars.alegeusFilesPreCheckHoldAllRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AlegeusFileProcessing-CopyFilesToHoldAll",
                        "Success",
                        "Copied File to HoldAll Directory");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(FileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void ConvertAllHeaderExcelFilesToCsv(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            // Log
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            // Iterate and convert all Excel files in fileProcessingHeadersRoot - no subDirs
            FileUtils.ConvertAllExcelFilesToCsv(this.Vars.alegeusFileHeadersRoot, false,
                this.Vars.alegeusFileHeadersRoot, dbConn,
                fileLogParams,
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // archive the source excel file
                    var file = srcFilePath;
                    var archiveFilePath = $"{Path.GetDirectoryName(file)}/Archive/{Path.GetFileName(file)}";
                    FileUtils.MoveFile(file, archiveFilePath, null, null);
                },
                (directory, file, ex) => { Import.MoveFileToAlegeusRejectsFolder(file, ex.ToString()); }
            );

            // Log
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        } //end method

        protected void AddHeaderToAllHeaderDirFiles(DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {
            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            this.AddHeaderToAllHeaderDirFilesForExt("*.mbi", dbConn, fileLogParams);
            this.AddHeaderToAllHeaderDirFilesForExt("*.csv", dbConn, fileLogParams);
            this.AddHeaderToAllHeaderDirFilesForExt("*.txt", dbConn, fileLogParams);
            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void AddHeaderToAllHeaderDirFilesForExt(string fileExt, DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {
            // Iterate all files in header dir
            FileUtils.IterateDirectory(this.Vars.alegeusFileHeadersRoot, DirectoryIterateType.Files, false, fileExt,
                (srcFilePath, destFilePath, dummy2) =>
                {
                    //
                    var headerType = Import.GetAlegeusHeaderTypeFromFile(srcFilePath);

                    //1. truncate staging table
                    var tableName = "[dbo].[alegeus_file_staging]";
                    DbUtils.TruncateTable(dbConn, tableName,
                        fileLogParams?.GetMessageLogParams());

                    //2. import file
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
                        default:
                            var message =
                                $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : headerType : {headerType} is invalid";
                            throw new Exception(message);
                    }

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
                                  //confirmed: with danielle - we skip all lines that do not start with valid record type? 
                                  /*rowNo == 1
                                  && */
                                  // skip if line is not of a import row Type
                                  !Import.IsAlegeusImportRecLine(line)
                            )
                            {
                                return false;
                            }

                            return true;
                        },
                        fileLogParams,
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );

                    //3. run script to fix data
                    // no need to fix - csv parser skips "
                    //var queryString = $" UPDATE {tableName} set folder_name = '' where folder_name is null; " + "\r\n" +
                    //                  $" UPDATE {tableName} set data_row = replace(data_row, '\"', ''); " + "\r\n";

                    var queryString = " ";

                    // fix header proc
                    if (!Utils.IsBlank(procName))
                    {
                        queryString += $" EXEC {procName};" + "\r\n";
                    }

                    // run fix headers query
                    DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                        fileLogParams?.GetMessageLogParams());

                    //4. Export File
                    var expFilePath = FileUtils.GetDestFilePath(srcFilePath, ".mbi");

                    var outputTableName = "[dbo].[alegeus_file_final]";
                    var queryStringExp = $"Select * from {outputTableName} order by row_num asc";
                    ImpExpUtils.ExportSingleColumnFlatFile(expFilePath, dbConn, queryStringExp,
                        "file_row", null, fileLogParams,
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );

                    // add to fileLog
                    fileLogParams?.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(expFilePath), expFilePath, "AlegeusFileProcessing-AddHeaderToFile", "Success",
                        "Added Header to File");
                    DbUtils.LogFileOperation(fileLogParams);

                    // archive source excel file
                    var file = srcFilePath;
                    var archiveFilePath = $"{Path.GetDirectoryName(file)}/Archive/{Path.GetFileName(file)}";
                    FileUtils.MoveFile(file, archiveFilePath, null, null);
                },
                (directory, file, ex) => { Import.MoveFileToAlegeusRejectsFolder(file, ex.ToString()); }
            );
        }

        protected void CopyHoldAllFilesToPreCheckDir(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.CopyFiles(this.Vars.alegeusFilesPreCheckHoldAllRoot, false, "*.*",
                this.Vars.alegeusFilesPreCheckRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath,
                        "AlegeusFileProcessing-CopyHoldAllFilesToProcessing",
                        "Success", "Copied HoldAll File to Processing");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(FileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void MoveHeaderDirFilesToPreCheck(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            //1. Copy / y G:\FTP\AutomatedHeaderV1_Files\*.* G:\FTP\AutomatedHeaderV1_Files\Archive
            //
            FileUtils.MoveFiles(
                new[] { this.Vars.alegeusFileHeadersRoot }, false, new[]
                {
                    /*"*.txt", "*.csv",*/ "*.mbi",
                }, this.Vars.alegeusFilesPreCheckRoot, "", ".mbi",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath,
                        "AlegeusFileProcessing-renameHeaderDirTxtFilesToMbi",
                        "Success", "Renamed txt file to mbi");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(FileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void RemoveDuplicateFilesInPreCheckDir(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //1. delete xls, xlsx, txt, csv for each mbi file found
            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.IterateDirectory(this.Vars.alegeusFilesPreCheckRoot, DirectoryIterateType.Files, false, "*.mbi",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // delete xls, xlsx, txt, csv
                    string[] extensionsToDelete = { ".xls", ".xlsx", ".txt", ".csv" };

                    foreach (var fileExt in extensionsToDelete)
                    {
                        var delFilePath = FileUtils.GetDestFilePath(srcFilePath, fileExt);
                        FileUtils.DeleteFileIfExists(delFilePath, null,
                            (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                        );
                    }
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void PreCheckFilesAndProcess(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.IterateDirectory(this.Vars.alegeusFilesPreCheckRoot, DirectoryIterateType.Files, false, "*.mbi",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // check the file 
                    using var fileChecker = new FileChecker(srcFilePath, PlatformType.Alegeus,
                        this.Vars.dbConnDataProcessing, fileLogParams,
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );

                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(srcFilePath), srcFilePath,
                        $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                        "Starting", "Starting PreCheck");
                    DbUtils.LogFileOperation(fileLogParams);

                    //
                    fileChecker.CheckFileAndProcess(FileCheckType.AllData, FileCheckProcessType.MoveToDestDirectories);
                },
                (directory, file, ex) => { Import.MoveFileToAlegeusRejectsFolder(file, ex.ToString()); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "",
                $"AlegeusFileProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }
    } // end class

} // end namespace