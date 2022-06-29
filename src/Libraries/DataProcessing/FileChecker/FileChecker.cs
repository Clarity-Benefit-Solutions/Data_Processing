using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing.DataModels.DataProcessing;

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
        private static ExtendedCache _cache = new ExtendedCache(TimeSpan.FromHours(1), TimeSpan.FromHours(5), null);

        //
        private readonly DbConnection dbConnPortalWc;

        //
        public readonly FileCheckResults fileCheckResults = new FileCheckResults();
        public Boolean hasHeaderRow = false;
        public HeaderType headerType = HeaderType.NotApplicable;


        public FileChecker(string _srcFilePath, PlatformType _platformType, DbConnection _dbConn,
            FileOperationLogParams _fileLogParams, OnErrorCallback _onErrorCallback) : base()
        {
            this.SrcFilePath = _srcFilePath;
            this.OriginalSrcFilePath = _srcFilePath;
            this.PlatformType = _platformType;
            this.FileLogParams = _fileLogParams;
            this.DbConn = _dbConn;
            this.dbConnPortalWc = Vars.dbConnPortalWc;
            this.OnErrorCallback = _onErrorCallback;
        }

        private Vars Vars { get; } = new Vars();

        public string SrcFilePath { get; set; }
        public string OriginalSrcFilePath { get; set; }
        public PlatformType PlatformType { get; set; }
        public EdiFileFormat EdiFileFormat { get; set; }
        public FileOperationLogParams FileLogParams { get; set; }

        public DbConnection DbConn { get; set; }

        //
        public OnErrorCallback OnErrorCallback { get; }

        public void Dispose()
        {
            //
        }

        public void ClearCache()
        {
            if (_cache != null)
            {
            }

            _cache = new ExtendedCache(TimeSpan.FromHours(1), TimeSpan.FromHours(5), null);
        }

        #region CheckFile

        public OperationResult CheckFileAndProcess(FileCheckType fileCheckType, FileCheckProcessType fileCheckProcessType)
        {
            // check file
            OperationResultType resultType = CheckFile(fileCheckType);
            //
            OperationResult operationResult = null;

            try
            {
                // move source mbi file
                var fileName = $"{Path.GetFileNameWithoutExtension(this.SrcFilePath)}.mbi";
                var destFilePath = this.SrcFilePath;
                var queryStringOrgFile = "";
                string strCheckResults = "";

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
                                    destFilePath = $"{Vars.alegeusFilesTestPath}/{ fileName}";
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
                                    destFilePath = $"{Vars.cobraFilesTestPath}/{ fileName}";
                                }
                                else
                                {
                                    destFilePath = $"{Vars.cobraFilesPassedPath}/{fileName}";
                                }
                            }

                            // export all rows to file
                            queryStringOrgFile =
       $"exec [dbo].[proc_{PlatformType.ToDescription()}_ExportImportFile] '{Path.GetFileName(this.SrcFilePath)}', 'original_file', {this.FileLogParams.FileLogId}";
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
                            string originalFilePath = $"{destFilePath}-0-OriginalFile{ext}";
                            string passedLinesFilePath = $"{destFilePath}-1-PassedLines{ext}";
                            string rejectedLinesErrorFilePath = $"{destFilePath}-2-RejectedLines.err";
                            string rejectedLinesFilePath = $"{destFilePath}-3-RejectedLines{ext}";
                            string allLinesErrorFilePath = $"{destFilePath}-4-allLines.err";

                            // 2. export error file
                            ImpExpUtils.ExportSingleColumnFlatFile(originalFilePath, DbConn, queryStringOrgFile,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            // passed lines
                            var queryStringExpPassedLines =
                                $"exec [dbo].[proc_{PlatformType.ToDescription()}_ExportImportFile] '{srcFileName}', 'passed_lines', {this.FileLogParams.FileLogId}";

                            ImpExpUtils.ExportSingleColumnFlatFile(passedLinesFilePath, DbConn, queryStringExpPassedLines,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            // .err lines only errors
                            var queryStringExpErrFile =
                                $"exec [dbo].[proc_{PlatformType.ToDescription()}_ExportImportFile] '{srcFileName}', 'rejected_lines_with_errors', {this.FileLogParams.FileLogId}";

                            ImpExpUtils.ExportSingleColumnFlatFile(rejectedLinesErrorFilePath, DbConn, queryStringExpErrFile,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );

                            // rejected lines
                            var queryStringExpRejectedLines =
                                $"exec [dbo].[proc_{PlatformType.ToDescription()}_ExportImportFile] '{srcFileName}', 'rejected_lines', {this.FileLogParams.FileLogId}";

                            ImpExpUtils.ExportSingleColumnFlatFile(rejectedLinesFilePath, DbConn, queryStringExpRejectedLines,
                                "file_row", null, FileLogParams,
                                (directory, file, ex) => { DbUtils.LogError(directory, file, ex, FileLogParams); }
                            );


                            // entire file with errors
                            var queryStringExpAllLinesErrFile =
                                $"exec [dbo].[proc_{PlatformType.ToDescription()}_ExportImportFile] '{srcFileName}', 'all_lines_with_errors', {this.FileLogParams.FileLogId}";

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
                $"exec [dbo].[proc_{PlatformType.ToDescription()}_ExportImportFile] '{srcFileName}', 'all_lines_with_errors', {this.FileLogParams.FileLogId}";
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
            finally
            {
                if (operationResult == null)
                {
                    operationResult = new OperationResult(0, "400", "ERROR", "", "");
                }


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

        private OperationResultType CheckFile(FileCheckType fileCheckType)
        {
            if (this.PlatformType == PlatformType.Alegeus)
            {
                //
                Dictionary<EdiFileFormat, List<int>> fileFormats =
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
                // we can check the entire file directly
                EdiFileFormat fileFormat = EdiFileFormat.Unknown;
                //
                CheckCobraFile(fileFormat, this.OriginalSrcFilePath);
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


        #endregion



        #region CheckUtils


        private static readonly Regex regexInteger = new Regex("[^0-9]");
        private static readonly Regex regexDate = new Regex(@"[^a-zA-Z0-9\s:\-\//]");
        private static readonly Regex regexAlphaNumeric = new Regex(@"[^a-zA-Z0-9\s]");
        private static readonly Regex regexAlphaOnly = new Regex(@"[^a-zA-Z]");
        private static readonly Regex regexAlphaAndDashes = new Regex(@"[^a-zA-Z\-]");
        private static readonly Regex regexNumericAndDashes = new Regex(@"[^0-9\-]");
        private static readonly Regex regexDouble = new Regex("[^0-9.]");

        public string EnsureValueIsOfFormatAndMatchesRules(mbi_file_table_stage dataRow, TypedCsvColumn column,
            TypedCsvSchema mappings)
        {
            var orgValue = dataRow.ColumnValue(column.SourceColumn) ?? "";
            var value = orgValue;

            // always trim
            value = value?.Trim();

            //1. Check and fix format
            if (!Utils.IsBlank(value))
            {
                // fix value if possible
                switch (column.FormatType)
                {
                    case FormatType.Any:
                    case FormatType.String:
                        break;

                    case FormatType.Email:
                        if (!Utils.IsValidEmail(value))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a valid Email. {orgValue} is not valid");
                        }

                        break;
                    case FormatType.Zip:
                        value = regexInteger.Replace(value, String.Empty);
                        if (!Utils.IsBlank(value) && value.Length > column.MaxLength)
                        {
                            value = value.Substring(0, column.MaxLength);
                        }

                        break;
                    case FormatType.Phone:
                        value = regexInteger.Replace(value, String.Empty);
                        if (!Utils.IsBlank(value) && value.Length > column.MaxLength)
                        {
                            value = Utils.Right(value, column.MaxLength);
                        }

                        break;
                    case FormatType.AlphaNumeric:
                        // replace all non alphanumeric
                        value = regexAlphaNumeric.Replace(value, String.Empty);
                        break;

                    case FormatType.AlphaOnly:
                        // replace all non alphanumeric
                        value = regexAlphaOnly.Replace(value, String.Empty);
                        break;

                    case FormatType.FixedConstant:
                        // default to fixed value always!
                        value = column.FixedValue;
                        break;

                    case FormatType.AlphaAndDashes:
                        // replace all non alphanumeric
                        value = regexAlphaAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.NumbersAndDashes:
                        // replace all non alphanumeric
                        value = regexNumericAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.Integer:
                        // remove any non digits
                        value = regexInteger.Replace(value, String.Empty);
                        //
                        if (!Utils.IsInteger(value))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be numbers only. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.Double:
                        // remove any non digits and non . and non ,
                        value = regexDouble.Replace(value, String.Empty);
                        if (!Utils.IsDouble(value))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a Currency Value. {orgValue} is not valid");
                        }

                        // format as 0.00
                        var dblValue = Utils.ToDouble(value);
                        value = dblValue.ToString("0.00");

                        break;

                    case FormatType.IsoDate:
                        // remove any non digits
                        value = regexDate.Replace(value, String.Empty);
                        value = Utils.ToIsoDateString(Utils.ToDate(value));
                        if (!Utils.IsIsoDate(value, column.MaxLength > 0))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.IsoDateTime:
                        // remove any non digits
                        value = regexDate.Replace(value, String.Empty);
                        value = Utils.ToDateTimeString(Utils.ToDateTime(value));

                        if (!Utils.IsIsoDateTime(value, column.MaxLength > 0))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.YesNo:
                        if (!value.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("No", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. {orgValue} is not valid");
                        }

                        break;

                    case FormatType.TrueFalse:
                        if (!value.Equals("True", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("False", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either Yes or No. {orgValue} is not valid");
                        }

                        break;

                    default:
                        break;
                }
            }

            value = value?.Trim();

            //set default value
            if (Utils.IsBlank(value) && !Utils.IsBlank(column.DefaultValue))
            {
                value = column.DefaultValue;
            }

            // pad ssn to 9 digits with leading zeros
            if ((column.SourceColumn == "EmployeeSocialSecurityNumber" || column.SourceColumn == "EmployeeID"))

            {
                if (!Utils.IsBlank(value))
                {
                    value = regexAlphaNumeric.Replace(value, String.Empty);
                    if (value.Length < column.MinLength)
                    {
                        value = value.PadLeft(column.MinLength, '0');
                    }
                }
            }

            // set row column value to the fixed value if it has changed
            if (value != orgValue)
            {
                dataRow.SetColumnValue(column.SourceColumn, value);
                dataRow.data_row = GetDelimitedDataRow(dataRow, mappings);
            }

            // 2. check against GENERAL rules
            if (column.FixedValue != null && value != column.FixedValue &&
                !column.FixedValue.Split('|').Contains(value) && column.MinLength > 0)
            {
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must always be {column.FixedValue}. {orgValue} is not valid");
            }

            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. {orgValue} is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. {orgValue} is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number. {orgValue} is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. {orgValue} is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddAlegeusErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. {orgValue} is not valid");
                }
            }

            return value;
        }

        private string GetDelimitedDataRow(mbi_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            string value = "";

            foreach (TypedCsvColumn column in mappings)
            {
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    case "":
                    case "source_row_no":
                    case "error_row":
                    case "data_row":
                    case "res_file_name":
                    case "mbi_file_name":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }

                string fieldValue = dataRow.ColumnValue(column.SourceColumn);
                if (fieldValue?.IndexOf(",", StringComparison.InvariantCulture) > 0)
                {
                    fieldValue = $"\"{fieldValue}\"";
                }

                value += $",{fieldValue}";
            }

            // remove first char
            value = value.Substring(1);
            //
            return value;
        }

        #endregion
    }

}