using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;
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

        public static async Task ProcessAsync(PerformContext context, string id)
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
                            $"ERROR: DataProcessingJob:ProcessAsync : {id} is not a valid operation";
                        throw new Exception(message);

                }

                //
                string[] arr = listLogs.ToArray();
                string strArr = String.Join("\n", arr);
                //context.Write(strArr);

            }
            catch (Exception ex)
            {
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine(ex.ToString());
                context.SetTextColor(ConsoleTextColor.Black);
                throw;
            }
        }

        public void Dispose()
        {
            //_dbContext.Dispose();
        }


    }
}