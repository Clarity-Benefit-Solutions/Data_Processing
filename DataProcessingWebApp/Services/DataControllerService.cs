using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;

namespace DataProcessingWebApp.Services
{

    public class DataControllerService : BackgroundService
    {
        private readonly ILogger Logger;
        private readonly string Id;
        public DataControllerService(ILogger logger, string id)
        {
            Id = id;
            Logger = logger;
        }


        private void HandleOnFileLogOperationCallback(List<string> listLogs, FileOperationLogParams logParams)
        {
            var logItem = new LogFields(
                DateTime.Now.ToString(CultureInfo.InvariantCulture),
                logParams.FileId,
                logParams.ProcessingTask,
                logParams.ProcessingTaskOutcome,
                logParams.OriginalFileName,
                logParams.ProcessingTaskOutcomeDetails
            ).ToString();


            listLogs.Add(logItem);
        }

#pragma warning disable CS1998
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
#pragma warning restore CS1998
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<string> listLogs = new List<string>();
                    DbUtils.eventOnLogFileOperationCallback += (sender, logParams) =>
                    {
                        HandleOnFileLogOperationCallback(listLogs, logParams);
                    };
                    Task task = null;
                    switch (Id?.ToString().ToLower())
                    {
                        case "processcobrafiles":
                            task = CobraDataProcessing.ProcessAll();
                            break;

                        case "processalegeusfiles":
                            task = AlegeusDataProcessing.ProcessAll();
                            break;

                        case "retrieveftperrorlogs":
                            task = AlegeusErrorLog.ProcessAll();
                            break;
                        default:
                            throw new Exception($"ERROR: {MethodBase.GetCurrentMethod()?.Name} : {Id} is not a valid operation");

                    }

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (task != null)
                    {
                        task.Wait(stoppingToken);
                    }

                    string[] arr = listLogs.ToArray();
                    string message = string.Join("\n", arr);
                    LogInfo(message);

                }
                catch (Exception ex)
                {
                    LogError(ex.ToString());
                    throw;
                }
            }
        }

        private void LogInfo(string message)
        {
            Logger.Log(LogLevel.Information, message);
        }
        private void LogError(string error)
        {
            Logger.Log(LogLevel.Error, error);
        }
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}