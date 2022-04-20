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
    public class CobraDataProcessing
    {
        private Vars Vars { get; } = new Vars();

        public static async Task ProcessAll()
        {
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    Thread.CurrentThread.Name = "ProcessCobraFiles";
                    CobraDataProcessing cobraProcessing = new CobraDataProcessing();
                    cobraProcessing.MoveAndProcessCobraFtpFiles();
                }
            );
        }

        public void MoveAndProcessCobraFtpFiles()
        {
            // init logParams
            MessageLogParams logParams = Vars.dbMessageLogParams;
            logParams.SetSubModuleStepAndCommand("MoveAndProcessCobraFtpFiles", "", "", "");

            //
            FileOperationLogParams fileLogParams = Vars.dbFileProcessingLogParams;

            // DbConn for logging
            DbConnection dbConn = fileLogParams.DbConnection;

            //MoveSourceFilesToCobraDirs
            //MakeListOfCobraFtpSourceFolders(HeaderType.NotApplicable, "*.*", dbConn, fileLogParams);

            //MoveSourceFilesToCobraDirs
            MoveSourceFilesToCobraDirs(HeaderType.NotApplicable, "*.*", dbConn, fileLogParams);

            // MoveCobraFtpFiles
            MoveCobraFtpFiles(HeaderType.NotApplicable, dbConn, fileLogParams);

            // PrepareCobraQbFtpFiles
            PrepareCobraQbFtpFiles(HeaderType.NotApplicable, dbConn, fileLogParams);

            // MoveCobraFtpFilesAfterPrepare
            MoveCobraFtpFilesAfterPrepare(HeaderType.NotApplicable, dbConn, fileLogParams);
        }


        protected void MakeListOfCobraFtpSourceFolders(HeaderType headerType, string fileExt,
            DbConnection dbConn, FileOperationLogParams fileLogParams)

        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
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
                            Path.GetFileName(destFilePath), destFilePath, "CobraProcessing-ListFTPFolders",
                            "Success", $"Added Source Files Folder");
                        fileLogParams.SetSourceFolderName(srcDirPath);

                        // do not log - gives too many lines
                        //DbUtils.LogFileOperation(FileLogParams);
                    }
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }


        protected void MoveSourceFilesToCobraDirs(HeaderType headerType, string fileExt,
            DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            //1. Clear all files in AutomatedHeaderV1_Files
            //echo y| del  G:\FTP\AutomatedHeaderV1_Files\*.*
            //
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //

            //2. Get list of folders for header from DB

            // sumeet: we will only iterate the folders from ther master table
            /*   string tableName = "[dbo].[processing_script_tbl]";
               //run query
               string queryString = $"Select * from {tableName} ;";
               DataTable folders =
                   (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConn, queryString, null,
                       fileLogParams.DbMessageLogParams);


               //3. for each header folder, get file and move to header1 folder
               foreach (DataRow row in folders.Rows)*/

            var tableName = "dbo.[FTP_Source_Folders]";

            // run query - we take only by environment so we can test 
            var queryString = $"Select * from {tableName} where environment = '{Vars.Environment}' and is_active = 1  order by folder_name;";
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

                // 3. for each source folder
                if (!Utils.IsBlank(rowFolderName))
                {
                    // change from PROD source dir to Ctx source dir
                    rowFolderName = Vars.ConvertFilePathFromProdToCtx(rowFolderName);

                    // we need to set these first before setting folderName
                    var fileLogParams1 = Vars.dbFileProcessingLogParams;

                    fileLogParams.SetFileNames("", Path.GetFileName(rowFolderName), rowFolderName,
                        Path.GetFileName(rowFolderName), "",
                        $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                        "Starting", $"Started Iterating Directory");
                    //
                    fileLogParams1.Bencode = rowBenCode;
                    fileLogParams1.TemplateType = rowTemplateType;
                    fileLogParams1.IcType = rowIcType;
                    fileLogParams1.ToFtp = rowtoFtp;
                    fileLogParams1.SetSourceFolderName(rowFolderName);
                    DbUtils.LogFileOperation(fileLogParams);

                    FileUtils.IterateDirectory(
                        rowFolderName, DirectoryIterateType.Files, false, "*.*",
                        (srcFilePath, dummy1, dummy2) =>
                        {
                            //check file name and move to appropriate directory
                            FileInfo fileInfo = new FileInfo(srcFilePath);

                            Boolean processThisFile = false;
                            string destDirPath = "";

                            // look inside file to dtermine if it is a COBRA file
                            if (fileInfo.Extension == ".pgp")
                            {
                                processThisFile = true;
                                destDirPath = $"{Vars.cobraImportHoldingDecryptRoot}";
                            }
                            else if (Import.IsCobraImportFile(srcFilePath))
                            {
                                processThisFile = true;
                                destDirPath = $"{Vars.cobraImportRoot}";
                            }

                            if (processThisFile && !Utils.IsBlank((destDirPath)))
                            {
                                // make FilenameProperty uniform
                                var uniformFilePath = Import.GetUniformNameForFile(PlatformType.Cobra, srcFilePath);
                                if (srcFilePath != uniformFilePath)
                                {
                                    FileUtils.MoveFile(srcFilePath, uniformFilePath, null, null);
                                }


                                // add uniqueId to file so we can track it across folders and operations
                                var uniqueIdFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(uniformFilePath,
                                    true,
                                    fileLogParams1);

                                //

                                fileLogParams.SetFileNames(srcFilePath, Path.GetFileName(srcFilePath), uniqueIdFilePath,
                                    Path.GetFileName(uniqueIdFilePath), "",
                                    $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                                    "Starting", $"Started Processing File");

                                //DbUtils.LogFileOperation(FileLogParams);
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
                                            "CobraProcessing-MoveFilesToCobraFolders", "Success",
                                            $"Moved file to COBRA folder");

                                        //DbUtils.LogFileOperation(FileLogParams);
                                    },
                                    (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                                );
                            }
                        },
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );
                } //iterateDir
            } //foreach (DataRow row in folders.Rows)


            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        } // sub

        protected void MoveCobraFtpFiles(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
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
                                    $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Success",
                                    $"Renamed file to *.csv");

                                DbUtils.LogFileOperation(fileLogParams);
                            },
                            (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                        );
                    }
                }, null
            ); //iterateDir


            //2. move out blank files
            MoveCobraBlankFtpFilesToArchive(headerType, dbConn, fileLogParams);

            // 3. move csv files for preparation of QB
            // from COBRA IMPORTS\Holding,ToDecrypt/*.* -> HOLDING\PreparedQB, COBRA IMPORTS\QB*.csv
            FileUtils.IterateDirectory(
                new string[] { Vars.cobraImportHoldingRoot, Vars.cobraImportHoldingDecryptRoot, Vars.cobraImportRoot },
                DirectoryIterateType.Files,
                false,
                // TODO: CobraFiles do we need to move *.* files from decrypt? not clear
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
                                    "CobraProcessing-MoveQBCsvFilesToPreparedQB", "Success",
                                    $"Moved QB File to MoveQBCsvFilesToPreparedQB folder");

                                DbUtils.LogFileOperation(fileLogParams);
                            },
                            (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                        );
                    }
                }, null
            ); //iterateDir


            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        } // routine

        protected void MoveCobraBlankFtpFilesToArchive(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)
        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
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
                    if (fileInfo.Length <= 30)
                    {
                        string destFilePath =
                            $"{Vars.cobraImportArchiveEmptyRoot}/{fileInfo.Name}";

                        FileUtils.MoveFile(srcFilePath, destFilePath,
                            (srcFilePath2, destFilePath2, dummy4) =>
                            {
                                //
                                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath2), srcFilePath2,
                                    Path.GetFileName(destFilePath2), destFilePath2,
                                    "CobraProcessing-MoveEmptyFiles", "Success",
                                    $"Moved Empty file to Archive - Done folder");

                                DbUtils.LogFileOperation(fileLogParams);
                            },
                            (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                        );
                    }
                }, null
            ); //iterateDir


            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void PrepareCobraQbFtpFiles(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //


            string fileExt = "*.csv";
            //
            FileUtils.IterateDirectory(
                Vars.cobraImportHoldingPreparedQbRoot, DirectoryIterateType.Files, false, fileExt,
                (srcFilePath, destFilePath, dummy2) =>
                {
                    // DB connecrtion for COBRA specific
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
                         (filePath1, rowNo, line) =>
                         {
                             return true;
                         },
                         fileLogParams,
                        (directory, file, ex) =>
                        {
                            DbUtils.LogError(directory, file, ex, fileLogParams);
                        }
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
                        "QB_data", null, fileLogParams,
                        (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                    );

                    // add to fileLog
                    fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                        Path.GetFileName(expFilePath), expFilePath,
                        $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}", "Success",
                        $"Prepared Flat File for COBRA");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        }

        protected void MoveCobraFtpFilesAfterPrepare(HeaderType headerType, DbConnection dbConn,
            FileOperationLogParams fileLogParams)

        {
            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Starting", $"Starting: {MethodBase.GetCurrentMethod()?.Name}");
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

                    if (Utils.IsTestFile(srcFilePath))
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
                                    "CobraProcessing-AfterPrepare",
                                    "Success", $"Moved File");
                                DbUtils.LogFileOperation(fileLogParams);
                            },
                            (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
                        );
                    }
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
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
                        Path.GetFileName(destFilePath), destFilePath, "CobraProcessing-AfterPrepare",
                        "Success", $"Moved File");
                    DbUtils.LogFileOperation(fileLogParams);
                },
                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); }
            );

            //3. move out blank files
            MoveCobraBlankFtpFilesToArchive(headerType, dbConn, fileLogParams);

            //
            fileLogParams.SetFileNames("", "", "", "", "", $"CobraProcessing-{MethodBase.GetCurrentMethod()?.Name}",
                "Success", $"Completed: {MethodBase.GetCurrentMethod()?.Name}");
            DbUtils.LogFileOperation(fileLogParams);
            //
        } // routine
    } // end class
} // end namespace