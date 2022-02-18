using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CoreUtils;
using CoreUtils.Classes;
using Google.Protobuf.WellKnownTypes;

namespace DataProcessing
{
    [Guid("EAA4976A-45C1-4AC5-BC0B-4321F4C3C83F")]
    [ComVisible(true)]
    public class AlegeusDataProcessing
    {
        public static void ProcessAllFiles()
        {
            // init logParams

            //
            var fileLogParams = Vars.dbFileProcessingLogParams;

            // dbConn
            var dbConn = Vars.dbConnAlegeusFileProcessing;

            // CreateHeaders
            CreateHeaders(HeaderType.Own, dbConn, fileLogParams);
            CreateHeaders(HeaderType.Old, dbConn, fileLogParams);
            CreateHeaders(HeaderType.NoChange, dbConn, fileLogParams);
            CreateHeaders(HeaderType.New, dbConn, fileLogParams);

            //PreCheckFilesAndProcess
            PreCheckFilesAndProcess(HeaderType.NotApplicable, dbConn, fileLogParams);
        }

        public static void CreateHeaders(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //MoveSourceFilesToCobraDirs
            MoveSourceFilesToHeaderDirs(headerType, dbConn, fileLogParams);

            //ConvertExcelFilesToCsv
            ConvertAllHeaderExcelFilesToCsv(headerType, dbConn, fileLogParams);

            //addHeaderToFiles
            if (headerType != HeaderType.Own)
            {
                AddHeaderToAllHeaderDirFiles(headerType, dbConn, fileLogParams);
            }

            //CopyHoldAllFilesToPreCheckDir
            CopyHoldAllFilesToPreCheckDir(headerType, dbConn, fileLogParams);

            //RenamePreCheckDirFilesToMbi
            MoveHeaderDirFilesToPreCheck(headerType, dbConn, fileLogParams);

            //RemoveDuplicateFilesInPreCheckDir
            RemoveDuplicateFilesInPreCheckDir(headerType, dbConn, fileLogParams);

        }

        protected static void MoveSourceFilesToHeaderDirs(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
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
                        "Delete File in Header Dir");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                () => { }
            );


            //2. Get list of folders for header from DB
            //decide table name
            string tableName;
            if (headerType == HeaderType.Own)
            {
                tableName = "dbo.[Header_list_Own]";
            }
            else if (headerType == HeaderType.Old)
            {
                tableName = "dbo.[Header_list_old]";
            }
            else if (headerType == HeaderType.NoChange)
            {
                tableName = "dbo.[Header_list_none]";
            }
            else if (headerType == HeaderType.New)
            {
                tableName = "dbo.[Header_list_new]";
            }
            else
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : headerType : {headerType} is invalid";
                throw new Exception(message);
            }

            //run query
            var queryString = $"Select * from {tableName} ;";
            var dtHeaderFolders = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConn, queryString, null);


            //3. for each header folder, get file and move to header1 folder
            foreach (DataRow row in dtHeaderFolders.Rows)
            {
                //Move / y "%_sourcepath%"  G:\FTP\AutomatedHeaderV1_Files
                var rowFolderName = row["folder_name"].ToString();
                var rowBenCode = row["BENCODE"].ToString();
                var rowTemplateType = row["template_type"].ToString();
                var rowIcType = row["IC_type"].ToString();
                var rowtoFtp = row["to_FTP"].ToString();

                // we need to set these first before setting folderName
                var fileLogParams1 = Vars.dbFileProcessingLogParams;
                fileLogParams1.SetFileNames("", rowFolderName, rowFolderName,
                    "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                    "Success", $"Added Source Files Folder");
                //
                fileLogParams1.Bencode = rowBenCode;
                fileLogParams1.TemplateType = rowTemplateType;
                fileLogParams1.IcType = rowIcType;
                fileLogParams1.ToFtp = rowtoFtp;
                fileLogParams1.SetSourceFolderName(rowFolderName);
                //
                DbUtils.LogFileOperation(fileLogParams);

                if (!Utils.IsBlank(rowFolderName))
                {
                    // change from PROD source dir to Ctx source dir
                    rowFolderName = Vars.ConvertFilePathFromProdToCtx(rowFolderName);

                    FileUtils.IterateDirectory(
                        rowFolderName, DirectoryIterateType.Files, false, "*.*",
                        (srcFilePath, destFilePath, dummy2) =>
                        {
                            // add uniqueId to file so we can track it across folders and operations
                            var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(headerType, srcFilePath, true,
                                fileLogParams1);

                            fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), uniqueIdFilePath,
                                Path.GetFileName(uniqueIdFilePath), "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", $"Processing File ${srcFilePath}");
                            DbUtils.LogFileOperation(fileLogParams);
                        },
                        () => { }
                    );

                    //move all files to Header1 Folder
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
                        () => { }
                    );
                }
            } // each dr


            //4. Copy / y G:\FTP\AutomatedHeaderV1_Files\*.* G:\FTP\AutomatedHeaderV1_Files\Archive
            //
            FileUtils.CopyFiles(
                Vars.alegeusFileHeadersRoot, false, "*.*",
                Vars.alegeusFileHeadersArchiveRoot, "", "",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-CopyToArchive", "Success",
                        "Copied File to Archive Directory");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                () => { }
            );


            //5. delete all HoldAll files and copy all header files
            //echo y|del  G:\FTP\To_Alegeus_FTP_Holding\HoldALL\*.*  
            //Copy / y G:\FTP\AutomatedHeaderV1_Files\*.* G:\FTP\To_Alegeus_FTP_Holding\HoldALL
            //
            //
            FileUtils.DeleteFiles(
                Vars.alegeusFilesPreCheckHoldAllRoot, false, "*.*",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-DeleteAllFilesInHoldAll",
                        "Success", "Deleted File In HoldAll Directory");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                () => { }
            );

            //5b: Copy all
            FileUtils.CopyFiles(
                Vars.alegeusFileHeadersRoot, false, "*.*",
                Vars.alegeusFilesPreCheckHoldAllRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-CopyFilesToHoldAll", "Success",
                        "Copied File to HoldAll Directory");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                () => { }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

        }

        protected static void ConvertAllHeaderExcelFilesToCsv(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            //1. Iterate and convert all Excel files in fileProcessingHeadersRoot - no subdirs
            //
            FileUtils.ConvertAllExcelFilesToCsv(Vars.alegeusFileHeadersRoot, false,
                Vars.alegeusFileHeadersRoot, dbConn, fileLogParams);

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

        } //end method

        protected static void AddHeaderToAllHeaderDirFiles(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            AddHeaderToAllHeaderDirFilesForExt(headerType, "*.mbi", dbConn, fileLogParams);
            AddHeaderToAllHeaderDirFilesForExt(headerType, "*.csv", dbConn, fileLogParams);
            AddHeaderToAllHeaderDirFilesForExt(headerType, "*.txt", dbConn, fileLogParams);
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected static void AddHeaderToAllHeaderDirFilesForExt(HeaderType headerType, string fileExt,
            DbConnection dbConn, FileOperationLogParams fileLogParams)

        {

            //
            FileUtils.IterateDirectory(
                Vars.alegeusFileHeadersRoot, DirectoryIterateType.Files, false, fileExt,
                (srcFilePath, destFilePath, dummy2) =>
                {
                    //1. truncate staging table
                    var tableName = "[dbo].[alegeus_file_staging]";
                    DbUtils.TruncateTable(dbConn, tableName,
                        fileLogParams?.DbMessageLogParams?.SetSubModuleStepAndCommand(fileLogParams.ProcessingTask,
                            "Truncate Table", fileLogParams.ProcessingTaskOutcomeDetails,
                            fileLogParams.OriginalFullPath));


                    //2. import file
                    string procName;
                    switch (headerType)
                    {
                        case HeaderType.Own:
                            procName = "";
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

                    ImpExpUtils.ImportSingleColumnFlatFile(headerType, dbConn, srcFilePath, srcFilePath, tableName, "folder_name",
                        "data_row", fileLogParams);

                    //3. run script to fix data
                    var queryString = $" UPDATE {tableName} set folder_name = '' where folder_name is null; " + "\r\n" +
                                      $" UPDATE {tableName} set data_row = replace(data_row, '\"', ''); " + "\r\n";

                    // fix header proc
                    if (!Utils.IsBlank(procName)) queryString += $" EXEC {procName};" + "\r\n";

                    // run fix headers query
                    DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                        fileLogParams?.DbMessageLogParams?.SetSubModuleStepAndCommand(fileLogParams.ProcessingTask,
                            fileLogParams.ProcessingTaskOutcomeDetails, fileLogParams.OriginalFullPath,
                            fileLogParams.NewFileFullPath));
                    //4. Export File
                    //( 'IC,','IB,','IZ,','IH,','ID,')
                    var expFilePath = FileUtils.GetDestFilePath(srcFilePath, ".mbi");

                    var outputTableName = "[dbo].[alegeus_file_final]";
                    var queryStringExp = $"Select * from {outputTableName} order by row_num asc";
                    ImpExpUtils.ExportSingleColumnFlatFile(expFilePath, dbConn, queryStringExp,
                        "folder_name", "file_row", null, fileLogParams);

                    // add to fileLog
                    fileLogParams?.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(expFilePath), expFilePath, "AutomatedHeaders-AddHeaderToFile", "Success",
                        "Added Header to File");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                () => { }
            );
        }

        protected static void CopyHoldAllFilesToPreCheckDir(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
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
                    DbUtils.LogFileOperation(fileLogParams);
                },
                () => { }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected static void MoveHeaderDirFilesToPreCheck(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            //1. Copy / y G:\FTP\AutomatedHeaderV1_Files\*.* G:\FTP\AutomatedHeaderV1_Files\Archive
            //
            FileUtils.MoveFiles(
                new string[] { Vars.alegeusFileHeadersRoot }, false, new string[] { "*.txt", "*.csv", "*.mbi" },
                Vars.alegeusFilesPreCheckRoot, "", ".mbi",
                (srcFilePath, destFilePath, fileContents) =>
                {
                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "AutomatedHeaders-renameHeaderDirTxtFilesToMbi",
                        "Success", "Renamed txt file to mbi");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                () => { }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected static void RemoveDuplicateFilesInPreCheckDir(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {

            //1. delete xls, xlsx, txt, csv for each mbi file found
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
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
                        FileUtils.DeleteFileIfExists(delFilePath, null);
                    }
                },
                () => { }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected static void PreCheckFilesAndProcess(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
            //
            FileUtils.IterateDirectory(
                Vars.alegeusFilesPreCheckRoot, DirectoryIterateType.Files, false, "*.mbi",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // check the file 
                    using var fileChecker = new FileChecker(srcFilePath, PlatformType.Alegeus, Vars.dbConnAlegeusErrorLog, fileLogParams);
                    fileChecker.CheckFile(FileCheckType.AllData);

                    // on success
                    if (fileChecker.fileCheckResults.Succcess)
                    {
                        var fileName = Path.GetFileName(srcFilePath);
                        var newFilePath = $"{Vars.alegeusFilesPreCheckOKRoot}/{fileName}";
                        FileUtils.MoveFile(srcFilePath, newFilePath, (srcFilePath2, destFilePath2, dummy2) =>
                        {
                            // add to fileLog
                            fileLogParams.SetFileNames("", fileName, srcFilePath,
                                Path.GetFileName(newFilePath), newFilePath, $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", "PreCheck OK. Moved File to PreCheck OK Directory");
                            //
                            DbUtils.LogFileOperation(fileLogParams);
                        });
                    }

                    // on failure
                    else
                    {
                        var fileName = Path.GetFileName(srcFilePath);
                        var newFilePath = $"{Vars.alegeusFilesPreCheckFailRoot}/{fileName}";
                        //
                        FileUtils.MoveFile(srcFilePath, newFilePath, (srcFilePath2, destFilePath2, dummy2) =>
                        {
                            // add to fileLog
                            fileLogParams.SetFileNames("", fileName, srcFilePath,
                                Path.GetFileName(newFilePath), newFilePath, $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                "Success", "PreeCheck FAIL. Moved File to PreCheck FAIL Directory");
                            //
                            DbUtils.LogFileOperation(fileLogParams);
                        });
                    }
                },
                () => { }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }
    } // end class
} // end namespace