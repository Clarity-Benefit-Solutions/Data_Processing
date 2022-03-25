using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Mvc;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;
using DataProcessingWebApp.Jobs;
using Hangfire;
using Hangfire.Console;
using Hangfire.Storage.Monitoring;


namespace DataProcessingWebApp.Controllers
{


    public class DataProcessingController : Controller
    {
        public string Index()
        {
            try
            {
                return "DataProcessingController";
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // GET api/<controller>/5
        public JobDetails StartJob(string id)
        {
            try
            {
                if (Utils.IsBlank(id))
                {
                    return new JobDetails("", id, "Job ID MUST BE PASSED");
                }

                // do job in background
                string jobId = BackgroundJob.Enqueue(() => DataProcessingJob.StartJob(null, id));

                return new JobDetails(id, $"[JobDetails ID {jobId} Queued for {id}", "STARTED");
            }
            catch (Exception ex)
            {
                return new JobDetails(id, $"[Job Could Not Be Queued as {ex.ToString()}", "FAILED");
            }
        }


        public JobDetails CheckFileAlegeus(HttpPostedFileBase file)
        {
                return CheckFile(file, "alegeus");
         
        }
        public JobDetails CheckFile(HttpPostedFileBase file, string platform)
        {
            string id = "CheckFile: " + platform;

            try
            {
                if (file == null || Utils.IsBlank(file.FileName))
                {
                    return new JobDetails(file != null ? file.FileName : "", "{id}", "Valid File MUST BE PASSED");
                }

                // Get local temp file with UniqueID Added
                var srcFileName = file.FileName;
                var srcFilePath = FileUtils.FixPath($"{Path.GetTempPath()}/{srcFileName}");

                // save file to temp path
                file.SaveAs(srcFilePath);

                // do job in background
                string jobId = BackgroundJob.Enqueue(() => DataProcessingJob.CheckFile(null, srcFilePath, "Alegeus"));
                //
                return new JobDetails(id, $"[JobDetails ID {jobId} Queued for {id} and File {file.FileName}", "STARTED");
            }
            catch (Exception ex)
            {
                return new JobDetails(id, $"[Job Could Not Be Queued as {ex.ToString()}", "FAILED");
            }
        }
        public JobDetails CheckFileCobra(HttpPostedFileBase file)
        {
            return CheckFile(file, "cobra");
        }
        public JobDetails JobResults(string jobId)
        {
            try
            {
                if (Utils.IsBlank(jobId))
                {
                    return new JobDetails("", jobId, "Not Found");
                }

                var jobData = JobStorage.Current.GetConnection().GetJobData(jobId);
                if (jobData == null || jobData.Job == null)
                {
                    return new JobDetails("", jobId, "Not Found");
                }

                string history = "";
                string result = "";

                string jobKey = (string)(jobData?.Job?.Args?.Last() ?? "");

                var hMonitoringApi = JobStorage.Current.GetMonitoringApi();
                var jobState = hMonitoringApi.JobDetails(jobId);
                if (jobState != null )
                {
                    var historyData = jobState.History.First()?.Data;
                    if (historyData != null)
                    {
                        var jobResultKeyPair = historyData.Last();
                        if (jobResultKeyPair.Key == "Result")
                        {
                            result = jobResultKeyPair.Value;
                        }
                        history = $"CreatedAt: {jobState.CreatedAt}";
                    }
                }

                return new JobDetails(jobKey, jobId, jobData.State, history, result);

            }
            catch (Exception ex)
            {
                return new JobDetails("", $"[JobDetails jobId {jobId} Results Could Not Be Queried as {ex.ToString()}", "FAILED");
            }
        }



    }
}
