using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;
using EtlUtilities;
using Hangfire;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.AspNet.SignalR;
using Microsoft.Extensions.Logging;

namespace DataProcessingWebApp.Jobs
{
    public class DataProcessingJob : IDisposable
    {

        public DataProcessingJob()
        {
        }

        private static void HandleOnFileLogOperationCallback(PerformContext context, List<string> listLogs, FileOperationLogParams logParams)
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

        public static async Task<string> StartJob(PerformContext context, string id)
        {
            List<string> listLogs = new List<string>();

            try
            {
                DbUtils.eventOnLogFileOperationCallback += (sender, logParams) =>
                {
                    HandleOnFileLogOperationCallback(context, listLogs, logParams);
                };

                switch (id.ToString().ToLower())
                {
                    case @"processcobrafiles":
                        await CobraDataProcessing.ProcessAll();
                        break;

                    case @"processalegeusfiles":
                        await AlegeusDataProcessing.ProcessAll();
                        break;

                    case @"retrieveftperrorlogs":
                        await AlegeusErrorLog.ProcessAll();
                        break;

                    case @"copytestfiles":
                        var directoryPath = Vars.GetProcessBaseDir();
                        Process.Start($"{directoryPath}/../__LocalTestDirsAndFiles/copy_Alegeus_mbi+res_to_export_ftp.bat");
                        Process.Start(
                            $"{directoryPath}/../__LocalTestDirsAndFiles/copy_Alegeus_source_files_to_import_ftp.bat");
                        Process.Start(
                            $"{directoryPath}/../__LocalTestDirsAndFiles/copy_COBRA_source_files_to_import_ftp.bat");

                        break;

                    default:
                        var message =
                            $"ERROR: DataProcessingJob:StartJob : {id} is not a valid operation";
                        throw new Exception(message);

                }
                //
                string logs = String.Join("\n", listLogs.ToArray());
                return new OperationResult(true, 200, "Completed", logs, "").ToString();

            }
            catch (Exception ex)
            {
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine(ex.ToString());
                context.SetTextColor(ConsoleTextColor.Black);

                //
                string logs = String.Join("\n", listLogs.ToArray());
                return new OperationResult(false, 400, "Error", logs, ex.ToString()).ToString();
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

                Vars vars = new Vars();

                PlatformType platformType = PlatformType.Unknown;
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
                srcFilePath = DbUtils.AddUniqueIdToFileAndLogToDb(srcFilePath, true, fileLogParams);

                // convert from xl is needed
                string fileName = Path.GetFileName(srcFilePath);
                string fileExt = Path.GetExtension(srcFilePath);
                if (fileExt == ".xlsx" || fileExt == ".xls")
                {
                    var csvFilePath = Path.GetTempFileName() + ".csv";
                    FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                        null,
                        null);

                    srcFilePath = csvFilePath;
                }

                // if COBRA file, treat as such!
                if (Import.IsCobraImportFile(srcFilePath))
                {
                    platformType = PlatformType.Cobra;
                }

                //  log operation
                fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                    Path.GetFileName(srcFilePath), srcFilePath,
                    $"DataProcessingWebApp-{MethodBase.GetCurrentMethod()?.Name}",
                    "Starting", "Starting PreCheck");
                DbUtils.LogFileOperation(fileLogParams);

                // init checker
                using FileChecker fileChecker = new FileChecker(
                    srcFilePath, platformType, vars.dbConnDataProcessing, fileLogParams,
                    (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); });

                // check file
                var results = fileChecker.CheckFileAndProcess(FileCheckType.AllData, FileCheckProcessType.ReturnResults);

                //
                //string logs = String.Join("\n", listLogs.ToArray());
                //return new OperationResult(true, 200, "Completed", logs, "").ToString();

                return results.ToString();

            }
            catch (Exception ex)
            {
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine(ex.ToString());
                context.SetTextColor(ConsoleTextColor.Black);

                //
                string logs = String.Join("\n", listLogs.ToArray());
                return new OperationResult(false, 400, "Error", logs, ex.ToString()).ToString();
            }
        }

        public void Dispose()
        {
            //_dbContext.Dispose();
        }


    }
}