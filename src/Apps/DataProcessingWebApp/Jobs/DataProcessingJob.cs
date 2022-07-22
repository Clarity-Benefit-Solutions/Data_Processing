using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;
using Hangfire.Console;
using Hangfire.Server;

namespace DataProcessingWebApp.Jobs
{
    public class DataProcessingJob : IDisposable
    {
        public void Dispose()
        {
            //_dbContext.Dispose();
        }

        private static void HandleOnFileLogOperationCallback(PerformContext context, List<string> listLogs,
            FileOperationLogParams logParams)
        {
            var logItem = new LogFields(
                DateTime.Now.ToString(CultureInfo.InvariantCulture),
                logParams.FileId,
                logParams.ProcessingTask,
                logParams.ProcessingTaskOutcome,
                logParams.OriginalFileName,
                logParams.ProcessingTaskOutcomeDetails
            ).ToString();

            context.WriteLine(logItem);

            //var progress = progressBarFactory.Create("Test");
            //for (var i = 0; i < 100; i++)
            //{
            //    progress.SetValue(i + 1);
            //}

            listLogs.Add(logItem);
        }

        public static string localFtpRoot(string ftpSubFolderPath = "")
        {
            Vars.ftpSubFolderPath = ftpSubFolderPath;
            var vars = new Vars();
            return vars.localFtpRoot;
        }

        public static async Task<string> StartJob(PerformContext context, string id, string ftpSubFolderPath = "")
        {
            List<string> listLogs = new List<string>();

            try
            {
                DbUtils.eventOnLogFileOperationCallback += (sender, logParams) =>
                {
                    HandleOnFileLogOperationCallback(context, listLogs, logParams);
                };

                switch (id.ToLower())
                {
                    case @"processincomingfiles":
                        await IncomingFileProcessing.ProcessAll(ftpSubFolderPath);
                        break;

                    case @"retrieveftperrorlogs":
                        await AlegeusErrorLog.ProcessAll(ftpSubFolderPath);
                        break;

                    case @"copytestfiles":
                        var directoryPath = Vars.GetProcessBaseDir();
                        await IncomingFileProcessing.CopyTestFiles(ftpSubFolderPath);

                        break;

                    default:
                        var message =
                            $"ERROR: DataProcessingJob:StartJob : {id} is not a valid operation";
                        throw new Exception(message);
                }

                //
                var logs = string.Join("\n", listLogs.ToArray());
                return new OperationResult(1, "200", "Completed", logs).ToString();
            }
            catch (Exception ex)
            {
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine(ex.ToString());
                context.SetTextColor(ConsoleTextColor.Black);

                //
                var logs = string.Join("\n", listLogs.ToArray());
                return new OperationResult(0, "400", "Error", logs, ex.ToString()).ToString();
            }
        }

#pragma warning disable CS1998

        public static async Task<string> CheckFile(PerformContext context, string srcFilePath, string platform)
#pragma warning restore CS1998
        {
            List<string> listLogs = new List<string>();

            try
            {
                DbUtils.eventOnLogFileOperationCallback += (sender, logParams) =>
                {
                    HandleOnFileLogOperationCallback(context, listLogs, logParams);
                };

                var vars = new Vars();

                // presume Alegeus
                var platformType = PlatformType.Alegeus;
                if (platform?.ToLower() == "alegeus")
                {
                    platformType = PlatformType.Alegeus;
                }
                else if (platform?.ToLower() == "cobra")
                {
                    platformType = PlatformType.Cobra;
                }

                var fileLogParams = vars.dbFileProcessingLogParams;

                // Get local temp file with UniqueID Added
                srcFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(srcFilePath, true, true, fileLogParams);

                // convert from xl is needed
                if (FileUtils.IsExcelFile(srcFilePath))
                {
                    var csvFilePath = Path.GetTempFileName() + ".csv";
                    FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                        Import.GetPasswordsToOpenExcelFiles(srcFilePath),
                        null,
                        null);

                    srcFilePath = csvFilePath;
                }

                // if COBRA file, treat as such!
                if (Import.IsCobraImportFile(srcFilePath))
                {
                    platformType = PlatformType.Cobra;
                }

                ////  log operation
                //fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                //    Path.GetFileName(srcFilePath), srcFilePath,
                //    $"DataProcessingWebApp-{MethodBase.GetCurrentMethod()?.Name}",
                //    "Starting", "Starting PreCheck");
                //DbUtils.LogFileOperation(fileLogParams);

                // init checker
                using var fileChecker = new FileChecker(
                    srcFilePath, platformType, vars.dbConnDataProcessing, fileLogParams,
                    (directory, file, ex) => { DbUtils.LogError(directory, file, ex, fileLogParams); });

                // check file
                var results =
                    fileChecker.CheckFileAndProcess(FileCheckType.AllData, FileCheckProcessType.ReturnResults);

                // if success, save as result
                /*if (results.Success == "1")
                {*/
                return results.ToString();
                /*}
                else
                {
                    // throw error - will be saved as error
                    throw new Exception(results.ToString());
                }*/
            }
            catch (Exception ex)
            {
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine(ex.ToString());
                context.SetTextColor(ConsoleTextColor.Black);

                //
                var logs = string.Join("\n", listLogs.ToArray());
                return new OperationResult(0, "400", "Error", logs, ex.ToString()).ToString();
            }
        }
    }
}