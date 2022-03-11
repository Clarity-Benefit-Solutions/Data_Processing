using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CoreUtils;
using CoreUtils.Classes;
using EtlUtilities;

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
                    AlegeusDataProcessing alegeusDataProcessing = new AlegeusDataProcessing();
                    alegeusDataProcessing.ProcessAllFiles();
                }
            );
        }

        public void ProcessAllFiles()
        {   
           


            //
            var fileLogParams = Vars.dbFileProcessingLogParams;

            // dbConn
            var dbConn = Vars.dbConnAlegeusFileProcessing;

            // CreateHeaders
            CreateHeaders(dbConn, fileLogParams);

            //PreCheckFilesAndProcess
            PreCheckFilesAndProcess(HeaderType.NotApplicable, dbConn, fileLogParams);
        }

        public void CreateHeaders(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //MoveSourceFilesToCobraDirs
            MoveSourceFilesToHeaderDirs(dbConn, fileLogParams);

            //ConvertExcelFilesToCsv
            ConvertAllHeaderExcelFilesToCsv(dbConn, fileLogParams);

            AddHeaderToAllHeaderDirFiles(dbConn, fileLogParams);

            //CopyHoldAllFilesToPreCheckDir
            CopyHoldAllFilesToPreCheckDir(dbConn, fileLogParams);

            //RenamePreCheckDirFilesToMbi
            MoveHeaderDirFilesToPreCheck(dbConn, fileLogParams);

            //RemoveDuplicateFilesInPreCheckDir
            RemoveDuplicateFilesInPreCheckDir(dbConn, fileLogParams);
        }

        protected void MoveSourceFilesToHeaderDirs(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);


            //1. Clear all files in AutomatedHeaderV1_Files
            //echo y| del  G:\FTP\AutomatedHeaderV1_Files\*.*
            //
            FileUtils.DeleteFiles(Vars.alegeusFileHeadersRoot, false, "*.*",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-ClearAllFiles", "Success",
                        "Deleted File in Header Dir");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );


            //2. Get list of folders for header from DB
            //decide table name
            var tableName = "dbo.[Header_list_ALL]";
            
            //run query
            var queryString = $"Select * from {tableName} order by template_type, folder_name;";
            var dtHeaderFolders = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConn, queryString, null);


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
                    // we need to set these first before setting folderName
                    var fileLogParams1 = Vars.dbFileProcessingLogParams;
                    fileLogParams1.SetFileNames("", rowFolderName, rowFolderName,
                        "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                        "Success", "Added Source Files Folder");
                    //
                    fileLogParams1.Bencode = rowBenCode;
                    fileLogParams1.TemplateType = rowTemplateType;
                    fileLogParams1.IcType = rowIcType;
                    fileLogParams1.ToFtp = rowtoFtp;
                    fileLogParams1.SetSourceFolderName(rowFolderName);

                    // do not log - gives too many lines
                    //DbUtils.LogFileOperation(fileLogParams);

                    //var headerType = HeaderType.NotApplicable;

                    //// get folder header type
                    //switch (rowTemplateType?.ToLower())
                    //{
                    //    case "new":
                    //        headerType = HeaderType.New;
                    //        break;
                    //    case "old":
                    //        headerType = HeaderType.Old;
                    //        break;
                    //    case "none":
                    //        headerType = HeaderType.NoChange;
                    //        break;
                    //    case "own":
                    //        headerType = HeaderType.Own;
                    //        break;
                    //    default:
                    //        headerType = HeaderType.NotApplicable;
                    //        break;
                    //}

                    // change from PROD source dir to Ctx source dir
                    rowFolderName = Vars.ConvertFilePathFromProdToCtx(rowFolderName);
                    //
                    // 3a. set unique fileNames for each file in source folder and add to file Log
                    FileUtils.IterateDirectory(
                        rowFolderName, DirectoryIterateType.Files, false, "*.*",
                        (srcFilePath, destFilePath, dummy2) =>
                        {
                            var headerType = Import.GetAlegeusHeaderTypeFromFile(srcFilePath);

                            // add uniqueId to file so we can track it across folders and operations
                            var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(headerType, srcFilePath, true,
                                fileLogParams1);

                            fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), uniqueIdFilePath,
                                Path.GetFileName(uniqueIdFilePath), "",
                                $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", $"Found Source File");
                            //DbUtils.LogFileOperation(fileLogParams);
                        },
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );

                    // 3b. move all source files (with new names) to Headers dir
                    FileUtils.MoveFiles(
                        rowFolderName, false, "*.*",
                        Vars.alegeusFileHeadersRoot, "", "",
                        (srcFilePath, destFilePath, fileContents) =>
                        {
                            // add to fileLog
                            fileLogParams1.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-MoveFilesToHeader",
                                "Success", "Copied Source File to Header Directory");
                            DbUtils.LogFileOperation(fileLogParams1);
                        },
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );
                }
            } // each dr


            //4. copy all header files to Archive root
            FileUtils.CopyFiles(
                Vars.alegeusFileHeadersRoot, false, "*.*",
                Vars.alegeusFileHeadersArchiveRoot, "", "",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-CopyToArchive", "Success",
                        "Copied File to Archive Directory");

                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );


            //5a. delete all remnant HoldAll files 
            FileUtils.DeleteFiles(
                Vars.alegeusFilesPreCheckHoldAllRoot, false, "*.*",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-DeleteAllFilesInHoldAll",
                        "Success", "Deleted File In HoldAll Directory");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //5b: copy all header files to HoldAll
            FileUtils.CopyFiles(
                Vars.alegeusFileHeadersRoot, false, "*.*",
                Vars.alegeusFilesPreCheckHoldAllRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-CopyFilesToHoldAll", "Success",
                        "Copied File to HoldAll Directory");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void ConvertAllHeaderExcelFilesToCsv(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            // Log
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            // Iterate and convert all Excel files in fileProcessingHeadersRoot - no subDirs
            FileUtils.ConvertAllExcelFilesToCsv(Vars.alegeusFileHeadersRoot, false, Vars.alegeusFileHeadersRoot, dbConn,
                fileLogParams,
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            // Log
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        } //end method

        protected void AddHeaderToAllHeaderDirFiles(DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            AddHeaderToAllHeaderDirFilesForExt("*.mbi", dbConn, fileLogParams);
            AddHeaderToAllHeaderDirFilesForExt("*.csv", dbConn, fileLogParams);
            AddHeaderToAllHeaderDirFilesForExt("*.txt", dbConn, fileLogParams);
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void AddHeaderToAllHeaderDirFilesForExt(string fileExt, DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {
            // Iterate all files in header dir
            FileUtils.IterateDirectory(
                Vars.alegeusFileHeadersRoot, DirectoryIterateType.Files, false, fileExt,
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

                    ImpExpUtils. ImportSingleColumnFlatFile( dbConn, srcFilePath, srcFilePath, tableName,
                        "folder_name",
                        "data_row", fileLogParams,
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );

                    //3. run script to fix data
                    // no need to fix - csv parser skips "
                    //var queryString = $" UPDATE {tableName} set folder_name = '' where folder_name is null; " + "\r\n" +
                    //                  $" UPDATE {tableName} set data_row = replace(data_row, '\"', ''); " + "\r\n";

                    var queryString = " ";

                    // fix header proc
                    if (!Utils.IsBlank(procName)) queryString += $" EXEC {procName};" + "\r\n";

                    // run fix headers query
                    DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                        fileLogParams?.GetMessageLogParams());

                    //4. Export File
                    var expFilePath = FileUtils.GetDestFilePath(srcFilePath, ".mbi");

                    var outputTableName = "[dbo].[alegeus_file_final]";
                    var queryStringExp = $"Select * from {outputTableName} order by row_num asc";
                    ImpExpUtils.ExportSingleColumnFlatFile(expFilePath, dbConn, queryStringExp,
                         "file_row", null, fileLogParams,
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );

                    // add to fileLog
                    fileLogParams?.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(expFilePath), expFilePath, "AutomatedHeaders-AddHeaderToFile", "Success",
                        "Added Header to File");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );
        }

        protected void CopyHoldAllFilesToPreCheckDir(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.CopyFiles(
                Vars.alegeusFilesPreCheckHoldAllRoot, false, "*.*",
                Vars.alegeusFilesPreCheckRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-CopyHoldAllFilesToProcessing",
                        "Success", "Copied HoldAll File to Processing");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void MoveHeaderDirFilesToPreCheck(DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            //1. Copy / y G:\FTP\AutomatedHeaderV1_Files\*.* G:\FTP\AutomatedHeaderV1_Files\Archive
            //
            FileUtils.MoveFiles(
                new[] { Vars.alegeusFileHeadersRoot }, false, new[] { "*.txt", "*.csv", "*.mbi" },
                Vars.alegeusFilesPreCheckRoot, "", ".mbi",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-renameHeaderDirTxtFilesToMbi",
                        "Success", "Renamed txt file to mbi");
                    // do not log - gives too many lines
                    // DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void RemoveDuplicateFilesInPreCheckDir(DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //1. delete xls, xlsx, txt, csv for each mbi file found
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.IterateDirectory(
                Vars.alegeusFilesPreCheckRoot, DirectoryIterateType.Files, false, "*.mbi",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // delete xls, xlsx, txt, csv
                    string[] extensionsToDelete = { ".xls", ".xlsx", ".txt", ".csv" };

                    foreach (var fileExt in extensionsToDelete)
                    {
                        var delFilePath = FileUtils.GetDestFilePath(srcFilePath, fileExt);
                        FileUtils.DeleteFileIfExists(delFilePath, null,
                            (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                        );
                    }
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void PreCheckFilesAndProcess(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.IterateDirectory(
                Vars.alegeusFilesPreCheckRoot, DirectoryIterateType.Files, false, "*.mbi",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // check the file 
                    using var fileChecker = new FileChecker(srcFilePath, PlatformType.Alegeus,
                        Vars.dbConnAlegeusErrorLog, fileLogParams,
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );

                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(srcFilePath), srcFilePath,
                        $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                        "Starting", "Starting PreCheck");
                    DbUtils.LogFileOperation(fileLogParams);
                    
                    //
                    fileChecker.CheckFileAndMove(FileCheckType.AllData);

                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }
    } // end class
} // end namespace