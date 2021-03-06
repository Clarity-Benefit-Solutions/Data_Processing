using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing.DataModels.CobraPoint;

// ReSharper disable All

// ReSharper disable once CheckNamespace
namespace DataProcessing
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class FileChecker : IDisposable
    {
        //
        public static readonly string ErrorSeparator = ";";

        //
        public readonly FileCheckResults fileCheckResults = new FileCheckResults();

        public Boolean hasHeaderRow = false;

        public HeaderType headerType = HeaderType.NotApplicable;

        //
        private static ExtendedCache _cache = new ExtendedCache(TimeSpan.FromHours(1), TimeSpan.FromHours(5), null);

        private readonly DbConnection dbConnCobra;

        //
        private readonly DbConnection dbConnPortalWc;

        private readonly CobraPointEntities dbCtxCobra;

        public FileChecker(string _srcFilePath, PlatformType _platformType, DbConnection _dbConn,
            FileOperationLogParams _fileLogParams, OnErrorCallback _onErrorCallback) : base()
        {
            this.SrcFilePath = _srcFilePath;
            this.OriginalSrcFilePath = _srcFilePath;
            this.PlatformType = _platformType;
            this.FileLogParams = _fileLogParams;
            this.DbConn = _dbConn;
            this.dbConnPortalWc = Vars.dbConnPortalWc;
            this.dbConnCobra = Vars.dbConnCobra;
            this.dbCtxCobra = Vars.DbCtxCobraDefault;
            this.OnErrorCallback = _onErrorCallback;
        }

        public DbConnection DbConn { get; set; }
        public EdiRowFormat EdiFileFormat { get; set; }
        public FileOperationLogParams FileLogParams { get; set; }

        //
        public OnErrorCallback OnErrorCallback { get; }

        public string OriginalSrcFilePath { get; set; }
        public PlatformType PlatformType { get; set; }
        public string SrcFilePath { get; set; }
        private Vars Vars { get; } = new Vars();

        public void ClearCache()
        {
            if (_cache != null)
            {
            }

            _cache = new ExtendedCache(TimeSpan.FromHours(1), TimeSpan.FromHours(5), null);
        }

        public void Dispose()
        {
            //
        }

        #region CheckFile

        public OperationResult CheckFileAndProcess(FileCheckType fileCheckType, FileCheckProcessType fileCheckProcessType)
        {
            // A) check file format and dATA
            OperationResultType resultType = CheckFile(fileCheckType);

            //
            OperationResult operationResult = null;

            // B) return results based on filecheck results and fileCheckProcessType
            try
            {
                // move source mbi file
                var fileName = $"{Path.GetFileNameWithoutExtension(this.SrcFilePath)}.mbi";
                var destFilePath = this.SrcFilePath;
                var queryStringOrgFile = "";
                string strCheckResults = "";

                var platformName = "";
                switch (PlatformType)
                {
                    case PlatformType.Alegeus:
                        platformName = "Alegeus";
                        break;

                    case PlatformType.Cobra:
                        platformName = "Cobra";
                        break;

                    default:
                        platformName = "uUnknown";
                        break;
                }

                // act on resultType
                switch (resultType)
                {
                    ///////////////////////////////////////
                    case OperationResultType.Ok:
                        ///////////////////////////////////////
                        if (fileCheckProcessType == FileCheckProcessType.MoveToDestDirectories)
                        {
                            if (PlatformType == PlatformType.Alegeus)
                            {
                                if (Utils.IsTestFile(this.SrcFilePath))
                                {
                                    destFilePath = $"{Vars.alegeusFilesTestPath}/{fileName}";
                                }
                                else
                                {
                                    destFilePath = $"{Vars.alegeusFilesPassedPath}/{fileName}";
                                }
                            }
                            else if (PlatformType == PlatformType.Cobra)
                            {
                                if (Utils.IsTestFile(this.SrcFilePath))
                                {
                                    destFilePath = $"{Vars.cobraFilesTestPath}/{fileName}";
                                }
                                else
                                {
                                    destFilePath = $"{Vars.cobraFilesPassedPath}/{fileName}";
                                }
                            }

                            // export all rows to file
                            queryStringOrgFile =
       $"exec [dbo].[proc_{platformName}_ExportImportFile] '{Path.GetFileName(this.SrcFilePath)}', 'passed_lines', {this.FileLogParams.FileLogId}";

                            //
                            ImpExpUtils.ExportSingleColumnFlatFile(destFilePath, DbConn, queryStringOrgFile,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );
                        }
                        else if (fileCheckProcessType == FileCheckProcessType.ReturnResults)
                        {
                            // nothing to do
                        }

                        // delete src file
                        FileUtils.DeleteFile(SrcFilePath, null, null);

                        // OK result
                        strCheckResults = "";
                        operationResult = new OperationResult(1, "200", "Completed", strCheckResults, strCheckResults);
                        return operationResult;

                    ///////////////////////////////////////
                    case OperationResultType.CompleteFail:
                    case OperationResultType.ProcessingError:
                    case OperationResultType.PartialFail:
                    default:
                        ///////////////////////////////////////

                        var ext = "";
                        string srcFileName = Path.GetFileName(this.SrcFilePath);
                        //
                        if (fileCheckProcessType == FileCheckProcessType.MoveToDestDirectories)
                        {
                            if (PlatformType == PlatformType.Alegeus)
                            {
                                if (Utils.IsTestFile(this.SrcFilePath))
                                {
                                    destFilePath = $"{Vars.alegeusFilesTestPath}/{fileName}";
                                }
                                else
                                {
                                    destFilePath = $"{Vars.alegeusFilesRejectsPath}/{fileName}";
                                }
                                ext = ".mbi";
                            }
                            else if (PlatformType == PlatformType.Cobra)
                            {
                                if (Utils.IsTestFile(this.SrcFilePath))
                                {
                                    destFilePath = $"{Vars.cobraFilesTestPath}/{fileName}";
                                }
                                else
                                {
                                    destFilePath = $"{Vars.cobraFilesRejectsPath}/{fileName}";
                                }
                                ext = ".csv";
                            }
                            //
                            string originalFilePath = $"{destFilePath}-0-OriginalFile{ext}";
                            string passedLinesFilePath = $"{destFilePath}-1-PassedLines{ext}";
                            string rejectedLinesErrorFilePath = $"{destFilePath}-2-RejectedLines.err";
                            string rejectedLinesFilePath = $"{destFilePath}-3-RejectedLines{ext}";
                            string allLinesErrorFilePath = $"{destFilePath}-4-allLines.err";

                            // 2. export error file
                            queryStringOrgFile =
                             $"exec [dbo].[proc_{platformName}_ExportImportFile] '{srcFileName}', 'original_file', {this.FileLogParams.FileLogId}";

                            ImpExpUtils.ExportSingleColumnFlatFile(originalFilePath, DbConn, queryStringOrgFile,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            // 2a) passed lines
                            var queryStringExpPassedLines =
                                $"exec [dbo].[proc_{platformName}_ExportImportFile] '{srcFileName}', 'passed_lines', {this.FileLogParams.FileLogId}";

                            ImpExpUtils.ExportSingleColumnFlatFile(passedLinesFilePath, DbConn, queryStringExpPassedLines,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            // 2b) .err lines only errors
                            var queryStringExpErrFile =
                                $"exec [dbo].[proc_{platformName}_ExportImportFile] '{srcFileName}', 'rejected_lines_with_errors', {this.FileLogParams.FileLogId}";

                            ImpExpUtils.ExportSingleColumnFlatFile(rejectedLinesErrorFilePath, DbConn, queryStringExpErrFile,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            // 2c) rejected lines
                            var queryStringExpRejectedLines =
                                $"exec [dbo].[proc_{platformName}_ExportImportFile] '{srcFileName}', 'rejected_lines', {this.FileLogParams.FileLogId}";

                            ImpExpUtils.ExportSingleColumnFlatFile(rejectedLinesFilePath, DbConn, queryStringExpRejectedLines,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            // 2d) entire file with errors
                            var queryStringExpAllLinesErrFile =
                                $"exec [dbo].[proc_{platformName}_ExportImportFile] '{srcFileName}', 'all_lines_with_errors', {this.FileLogParams.FileLogId}";

                            ImpExpUtils.ExportSingleColumnFlatFile(allLinesErrorFilePath, DbConn, queryStringExpAllLinesErrFile,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            //
                            strCheckResults = File.ReadAllText(allLinesErrorFilePath);

                            // delete src file
                            FileUtils.DeleteFile(SrcFilePath, null, null);

                            // OK result
                            operationResult = new OperationResult(0, "300", "Failed", "", strCheckResults);
                            return operationResult;
                        }
                        else if (fileCheckProcessType == FileCheckProcessType.ReturnResults)
                        {
                            string allLinesErrorFilePath = $"{destFilePath}-4-allLines.err";
                            if (PlatformType == PlatformType.Alegeus)
                            {
                                if (Utils.IsTestFile(this.SrcFilePath))
                                {
                                    destFilePath = $"{Vars.alegeusFilesTestPath}/{fileName}";
                                }
                                else
                                {
                                    destFilePath = $"{Vars.alegeusFilesRejectsPath}/{fileName}";
                                }
                                ext = ".mbi";
                            }
                            else if (PlatformType == PlatformType.Cobra)
                            {
                                if (Utils.IsTestFile(this.SrcFilePath))
                                {
                                    destFilePath = $"{Vars.cobraFilesTestPath}/{fileName}";
                                }
                                else
                                {
                                    destFilePath = $"{Vars.cobraFilesRejectsPath}/{fileName}";
                                }
                                ext = ".csv";
                            }

                            // export entire file with errors
                            queryStringOrgFile =
                $"exec [dbo].[proc_{platformName}_ExportImportFile] '{srcFileName}', 'all_lines_with_errors', {this.FileLogParams.FileLogId}";
                            //
                            ImpExpUtils.ExportSingleColumnFlatFile(allLinesErrorFilePath, DbConn, queryStringOrgFile,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            //
                            strCheckResults = File.ReadAllText(allLinesErrorFilePath);

                            // OK result
                            operationResult = new OperationResult(0, "300", "Failed", "", strCheckResults);
                            return operationResult;
                        }
                        else
                        {
                            throw new Exception($"FileCheckProcessType: {fileCheckProcessType} is invalid");
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
            finally
            {
                if (operationResult == null)
                {
                    operationResult = new OperationResult(0, "400", "ERROR", "", "");
                }

                // log operation with outcome
                FileLogParams.SetFileNames("", Path.GetFileName(SrcFilePath), SrcFilePath,
                    Path.GetFileName(SrcFilePath), SrcFilePath,
                    $"FileChecker-{MethodBase.GetCurrentMethod()?.Name}",
                    "", "");

                switch (operationResult.Code)
                {
                    case "200":
                        FileLogParams.ProcessingTaskOutcome = "Passed";
                        FileLogParams.ProcessingTaskOutcomeDetails = "PreCheck File: Passed";
                        break;

                    case "300":
                        FileLogParams.ProcessingTaskOutcome = "Rejected";
                        FileLogParams.ProcessingTaskOutcomeDetails = "PreCheck File: Rejected";
                        break;

                    default:
                        FileLogParams.ProcessingTaskOutcome = "ERROR";
                        FileLogParams.ProcessingTaskOutcomeDetails = "PreCheck File: ERROR";
                        break;
                }

                DbUtils.LogFileOperation(FileLogParams);
            }
        }

        //

        //check file format, format data and check against db - save results to ErrorCode and ErrorMessage of each row
        private OperationResultType CheckFile(FileCheckType fileCheckType)
        {
            if (this.PlatformType == PlatformType.Alegeus)
            {
                //
                Dictionary<EdiRowFormat, List<int>> fileFormats =
                    ImpExpUtils.GetAlegeusFileFormats(this.SrcFilePath, false, this.FileLogParams);

                //
                CheckAlegeusFile(fileFormats);

                //
                var result = this.fileCheckResults.OperationResultType;

                //
                return result;
            }
            else if (this.PlatformType == PlatformType.Cobra)
            {
                //
                CheckCobraFile(this.OriginalSrcFilePath);
                //
                var result = this.fileCheckResults.OperationResultType;
                //
                return result;
            }
            else
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : PlatformType : {PlatformType} is invalid";
                throw new Exception(message);
            }
        }

        #endregion CheckFile
    }
}