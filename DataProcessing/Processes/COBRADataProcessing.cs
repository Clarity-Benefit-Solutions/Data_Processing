using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreUtils;
using CoreUtils.Classes;

// ReSharper disable All

namespace DataProcessing
{
    [Guid("EAA4776C-45C3-4BC5-BC0B-4321F4C3C83F")]
    [ComVisible(true)]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class CobraDataProcessing
    {
        public static void MoveAndProcessCobraFtpFiles()
        {
            // init logParams
            MessageLogParams logParams = Vars.dbMessageLogParams;
            logParams.SetSubModuleStepAndCommand("MoveAndProcessCobraFtpFiles", "", "", "");

            //
            FileOperationLogParams fileLogParams = Vars.dbFileProcessingLogParams;

            // dbConn for logging
            DbConnection dbConn = fileLogParams.DbConnection;

            // MoveCobraFtpFiles
            MoveCobraFtpFiles(HeaderType.Own, dbConn, fileLogParams);

            // MoveCobraFtpFiles2
            MoveCobraFtpFiles2(HeaderType.Own, dbConn, fileLogParams);

            // PrepareCobraQbFtpFiles
            PrepareCobraQbFtpFiles(HeaderType.Own, dbConn, fileLogParams);

            // MoveCobraFtpFilesAfterPrepare
            MoveCobraFtpFilesAfterPrepare(HeaderType.Own, dbConn, fileLogParams);
        }

        public static void MoveCobraFtpFiles(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //MoveSourceFilesToCobraDirs
            MakeListOfCobraFtpSourceFolders(headerType, "*.*", dbConn, fileLogParams);

            //MoveSourceFilesToCobraDirs
            MoveSourceFilesToCobraDirs(headerType, "*.*", dbConn, fileLogParams);
        }


        protected static void MakeListOfCobraFtpSourceFolders(HeaderType headerType, string fileExt,
            DbConnection dbConn, FileOperationLogParams fileLogParams)

        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            fileLogParams.SetSourceFolderName(Vars.localFtpRoot);
            DbUtils.LogFileOperation(fileLogParams);
            //

            //1. truncate staging table
            string tableName = "[dbo].[processing_script_tbl]";
            DbUtils.TruncateTable(dbConn, tableName,
                fileLogParams?.GetMessageLogParams());

            FileUtils.IterateDirectory(
                Vars.localFtpRoot, DirectoryIterateType.Directories, false, fileExt,
                (srcDirPath, destFilePath, dummy2) =>
                {
                    //check file name and move to appropriate directory
                    DirectoryInfo dirInfo = new DirectoryInfo(srcDirPath);
                    srcDirPath = FileUtils.FixPath(srcDirPath);

                    if (Vars.cobraIgnoreFtpSourceDirs.Contains(srcDirPath, StringComparer.InvariantCultureIgnoreCase))
                    {
                        // ignore
                    }
                    else
                    {
                        //3. run script to fix data
                        string queryString =
                            $" insert into dbo.processing_script_tbl (folder_name, script_txt, script_purpose) " +
                            "\r\n" +
                            $" values ('{srcDirPath}', '', ''); " + "\r\n";

                        // run fix headers query
                        DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                            fileLogParams?.GetMessageLogParams());
                        //
                        fileLogParams.SetFileNames("", Path.GetFileName(srcDirPath), srcDirPath,
                            Path.GetFileName(destFilePath), destFilePath, "MoveCobraFtpFiles-ListFTPFolders",
                            "Success", $"Added Source Files Folder");
                        fileLogParams.SetSourceFolderName(srcDirPath);
                        DbUtils.LogFileOperation(fileLogParams);

                    }
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

        }


        protected static void MoveSourceFilesToCobraDirs(HeaderType headerType, string fileExt,
            DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //1. Clear all files in AutomatedHeaderV1_Files
            //echo y| del  G:\FTP\AutomatedHeaderV1_Files\*.*
            //
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            //2. Get list of folders for header from DB
            //decide table name
            string tableName = "[dbo].[processing_script_tbl]";

            //run query
            string queryString = $"Select * from {tableName} ;";
            DataTable folders =
                (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConn, queryString, null,
                    fileLogParams.DbMessageLogParams);


            //3. for each header folder, get file and move to header1 folder
            foreach (DataRow row in folders.Rows)
            {
                //Move / y "%_sourcepath%"  G:\FTP\AutomatedHeaderV1_Files
                string rowFolderName = row["folder_name"].ToString();
                string rowBenCode = "";
                string rowTemplateType = "";
                string rowIcType = "";
                string rowToFtp = "";

                // 
                FileOperationLogParams fileLogParams1 = Vars.dbFileProcessingLogParams;
                fileLogParams1.FolderName = rowFolderName;
                fileLogParams1.Bencode = rowBenCode;
                fileLogParams1.TemplateType = rowTemplateType;
                fileLogParams1.IcType = rowIcType;
                fileLogParams1.ToFtp = rowToFtp;


                if (!Utils.IsBlank(rowFolderName))
                {
                    // change from PROD source dir to Ctx source dir
                    rowFolderName = Vars.ConvertFilePathFromProdToCtx(rowFolderName);


                    FileUtils.IterateDirectory(
                        rowFolderName, DirectoryIterateType.Files, false, "*.*",
                        (srcFilePath, dummy1, dummy2) =>
                        {
                            //check file name and move to appropriate directory
                            FileInfo fileInfo = new FileInfo(srcFilePath);

                            Boolean processThisFile = false;
                            string destDirPath = "";

                            // mbi, txt, csv of type COBRA - move out of FTP folders so they will not be processed for Alegeus
                            if (fileInfo.Extension == ".mbi" || fileInfo.Extension == ".csv" ||
                                fileInfo.Extension == ".txt")

                            {
                                if (fileInfo.Name.IndexOf("VERSION", StringComparison.InvariantCulture) >= 0)
                                {
                                    processThisFile = true;
                                    destDirPath = $"{Vars.cobraImportHoldingRoot}";
                                }
                                else if (fileInfo.Name.IndexOf("QB", StringComparison.InvariantCulture) >= 0
                                         || fileInfo.Name.IndexOf("NPM", StringComparison.InvariantCulture) >= 0)
                                {
                                    processThisFile = true;
                                    destDirPath = $"{Vars.cobraImportRoot}";
                                }
                            }
                            // encrypted files
                            else if (fileInfo.Extension == ".pgp")
                            {
                                processThisFile = true;
                                destDirPath = $"{Vars.cobraImportHoldingDecryptRoot}";
                            }

                            if (processThisFile && !Utils.IsBlank(destDirPath))
                            {

                                // add uniqueId to file so we can track it across folders and operations
                                var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(headerType, srcFilePath, true,
                                    fileLogParams1);

                                //

                                fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), uniqueIdFilePath,
                                    Path.GetFileName(uniqueIdFilePath), "", $"AutomatedHeaders-{MethodBase.GetCurrentMethod()?.Name}",
                                    "Success", $"Processing File ${srcFilePath}");

                                DbUtils.LogFileOperation(fileLogParams);
                                //
                                string uniqueIdFileName = Path.GetFileName(uniqueIdFilePath);
                                string destFilePath = $"{destDirPath}/{uniqueIdFileName}";


                                FileUtils.MoveFile(
                                    uniqueIdFilePath, destFilePath,
                                    (srcFilePath2, dummy3, dummy4) =>
                                    {
                                        //
                                        fileLogParams.SetFileNames("", uniqueIdFileName, uniqueIdFilePath,
                                            Path.GetFileName(destFilePath), destFilePath,
                                            "MoveCobraFtpFiles-MoveFilesToCobraFolders", "Success",
                                            $"Moved file to COBRA folder");

                                        DbUtils.LogFileOperation(fileLogParams);
                                    },
                                    (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                                );
                            }
                        },
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );
                } //iterateDir
            } //foreach (DataRow row in folders.Rows)


            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

        } // sub

        protected static void MoveCobraFtpFiles2(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //


            // 1. txt -> csv, mbi -> csv
            FileUtils.IterateDirectory(
                Vars.cobraImportHoldingRoot, DirectoryIterateType.Files, false, "*.*",
                (srcFilePath, dummy1, dummy2) =>
                {
                    //check file name and move to appropriate directory
                    FileInfo fileInfo = new FileInfo(srcFilePath);

                    // rename txt and mbi files to csv
                    if (fileInfo.Extension == ".txt" || fileInfo.Extension == ".mbi")
                    {
                        string destFilePath =
                            $"{fileInfo.Directory}/{Path.GetFileNameWithoutExtension(fileInfo.Name)}.csv";


                        FileUtils.MoveFile(srcFilePath, destFilePath,
                            (srcFilePath2, destFilePath2, dummy4) =>
                            {
                                //
                                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath2), srcFilePath2,
                                    Path.GetFileName(destFilePath2), destFilePath2,
                                    "MoveCobraFtpFiles2-RenameTxtAndMbiFiles", "Success",
                                    $"Renamed file to *.csv");

                                DbUtils.LogFileOperation(fileLogParams);
                            },
                            (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                        );
                    }
                }, null
            ); //iterateDir


            //2. move out blank files
            MoveCobraBlankFtpFilesToArchive(headerType, dbConn, fileLogParams);

            // 3. csv files from 
            // from COBRA IMPORTS\Holding,ToDecrypt/*.* -> HOLDING\PreparedQB, COBRA IMPORTS\QB*.csv
            FileUtils.IterateDirectory(
                new string[] { Vars.cobraImportHoldingRoot, Vars.cobraImportHoldingDecryptRoot, Vars.cobraImportRoot },
                DirectoryIterateType.Files,
                false,
                // TODO: do we need to move *.* files from decrypt? not clear
                new string[] { "*.csv" },
                (srcFilePath, dummy1, dummy2) =>
                {
                    //check file name and move to appropriate directory
                    FileInfo fileInfo = new FileInfo(srcFilePath);

                    // rename txt and mbi files to csv
                    if (fileInfo.Name.IndexOf("QB", StringComparison.InvariantCulture) >= 0)
                    {
                        string destFilePath =
                            $"{Vars.cobraImportHoldingPreparedQbRoot}/{fileInfo.Name}";

                        FileUtils.MoveFile(srcFilePath, destFilePath,
                            (srcFilePath2, destFilePath2, dummy4) =>
                            {
                                //
                                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath2), srcFilePath2,
                                    Path.GetFileName(destFilePath2), destFilePath2,
                                    "MoveCobraFtpFiles2-MoveQBCsvFilesToPreparedQB", "Success",
                                    $"Moved QB File to MoveQBCsvFilesToPreparedQB folder");

                                DbUtils.LogFileOperation(fileLogParams);
                            },
                            (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                        );
                    }
                }, null
            ); //iterateDir


            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

        } // routine

        protected static void MoveCobraBlankFtpFilesToArchive(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            // 1. empty files -> Archive - Empty
            // from COBRA IMPORTS, HOLDING, HOLDING\PreparedQB
            FileUtils.IterateDirectory(
                new string[]
                {
                    Vars.cobraImportHoldingRoot, Vars.cobraImportHoldingRoot, Vars.cobraImportHoldingPreparedQbRoot,
                    Vars.paylocityFtpRoot
                },
                DirectoryIterateType.Files,
                false,
                new string[] { "*.csv" },
                (srcFilePath, dummy1, dummy2) =>
                {
                    //check file name and move to appropriate directory
                    FileInfo fileInfo = new FileInfo(srcFilePath);

                    // rename txt and mbi files to csv
                    if (fileInfo.Length <= 10)
                    {
                        string destFilePath =
                            $"{Vars.cobraImportArchiveEmptyRoot}/{fileInfo.Name}";

                        FileUtils.MoveFile(srcFilePath, destFilePath,
                            (srcFilePath2, destFilePath2, dummy4) =>
                            {
                                //
                                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath2), srcFilePath2,
                                    Path.GetFileName(destFilePath2), destFilePath2,
                                    "MoveCobraFtpFiles2-MoveEmptyFiles", "Success",
                                    $"Moved Empty file to Archive - Done folder");

                                DbUtils.LogFileOperation(fileLogParams);
                            },
                            (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                        );
                    }
                }, null
            ); //iterateDir


            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

        }

        protected static void PrepareCobraQbFtpFiles(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //


            string fileExt = "*.csv";
            //
            FileUtils.IterateDirectory(
                Vars.cobraImportHoldingPreparedQbRoot, DirectoryIterateType.Files, false, fileExt,
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // DB connecrtion for COBRA specific
                    DbConnection dbConnCobra = Vars.dbConnCobraFileProcessing;

                    //1. truncate staging table
                    // ReSharper disable once StringLiteralTypo
                    string tableName = "[dbo].[QB_file_data_fixtbl]";
                    DbUtils.TruncateTable(dbConnCobra, tableName,
                        fileLogParams?.GetMessageLogParams());

                    //2. import file
                    string procName = "dbo.[Fix_COBRAQB_SSObollean]";
                    ImpExpUtils.ImportSingleColumnFlatFile(headerType, dbConnCobra, srcFilePath, srcFilePath, tableName, "folder_name",
                        "QB_data", fileLogParams,
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                    );

                    //3. run script to fix data
                    string queryString = "";

                    // fix header proc
                    if (!Utils.IsBlank(procName))
                    {
                        queryString += $" EXEC {procName};" + "\r\n";
                    }

                    // run fix headers query
                    DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConnCobra, queryString, null,
                        fileLogParams?.GetMessageLogParams());
                    //4. Export File

                    string expFilePath = FileUtils.GetDestFilePath(srcFilePath, "");
                    //
                    var outputTableName = tableName;
                    string queryStringExp = $"Select * from {outputTableName} order by row_num asc";
                    //
                    ImpExpUtils.ExportSingleColumnFlatFile(expFilePath, dbConnCobra, queryStringExp,
                        "folder_name", "QB_data", null, fileLogParams,
                        (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }

                    );

                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(expFilePath), expFilePath, $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Completed",
                        $"Prepared Flat File for COBRA");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

        }

        protected static void MoveCobraFtpFilesAfterPrepare(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            // 1. move test files
            // from COBRA IMPORTS, HOLDING, HOLDING\PreparedQB -> COBRA_testfiles
            FileUtils.IterateDirectory(
                new string[] { Vars.cobraImportRoot, Vars.cobraImportHoldingRoot, Vars.cobraImportHoldingPreparedQbRoot },
                DirectoryIterateType.Files,
                false,
                new string[] { "*.*" },
                (srcFilePath, destFilePath, dummy2) =>
                {
                    FileInfo fileInfo = new FileInfo(srcFilePath);

                    Boolean processThisFile = false;
                    string destDirPath = "";

                    if (fileInfo.Name.IndexOf("Test", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        processThisFile = true;
                        destDirPath = $"{Vars.cobraImportTestFilesRoot}";
                    }

                    if (processThisFile && !Utils.IsBlank(destDirPath))
                    {
                        string destFilePath2 = $"{destDirPath}/{fileInfo.Name}";
                        FileUtils.MoveFile(srcFilePath, destFilePath2,
                            (srcFilePath3, destFilePath3, dummy4) =>
                            {
                                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                    Path.GetFileName(destFilePath), destFilePath,
                                    "MoveCobraFtpFilesAfterPrepare",
                                    "Success", $"Moved File");
                                DbUtils.LogFileOperation(fileLogParams);
                            },
                            (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
                        );
                    }
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            // 2. move all holding and prepared files
            // from HOLDING, HOLDING\PreparedQB -> IMPORTS
            FileUtils.MoveFiles(
                new string[] { Vars.cobraImportHoldingRoot, Vars.cobraImportHoldingPreparedQbRoot },
                false,
                new string[] { "*.*" },
                Vars.cobraImportRoot, "", "",
                (srcFilePath, destFilePath, dummy2) =>
                {
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(destFilePath), destFilePath, "MoveCobraFtpFilesAfterPrepare",
                        "Success", $"Moved File");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); }
            );

            //3. move out blank files
            MoveCobraBlankFtpFilesToArchive(headerType, dbConn, fileLogParams);

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Completed", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

        } // routine
    } // end class
} // end namespace