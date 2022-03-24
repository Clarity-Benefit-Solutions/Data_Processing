using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
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

    public class JobDetails
    {
        public JobDetails(string jobName, string jobId, string jobState = "", string jobHistory = "", string jobResult = "")
        {
            JobName = jobName;
            JobId = jobId;
            JobResult = jobResult;
            JobState = jobState;
            JobHistory = jobHistory;
        }
        public string JobName;
        public string JobId;
        public string JobState;
        public string JobHistory;
        public string JobResult;

        public override string ToString()
        {
            return $"JobName: {JobName}\n<br>JobId: {JobId}\n<br>JobState: {JobState}\n<br>JobHistory: {JobHistory}\n<br>JobResult: {JobResult}";
        }
    }
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


        public JobDetails CheckFile(HttpPostedFileBase file)
        {
            string id = "CheckFile";

            try
            {
                if (file == null || Utils.IsBlank(file.FileName))
                {
                    return new JobDetails(file != null ? file.FileName : "", "{id}", "Valid File MUST BE PASSED");
                }
                // do job in background
                string jobId = BackgroundJob.Enqueue(() => DataProcessingJob.CheckFile(null, file));
                //
                return new JobDetails(id, $"[JobDetails ID {jobId} Queued for {id} and File {file.FileName}", "STARTED");
            }
            catch (Exception ex)
            {
                return new JobDetails(id, $"[Job Could Not Be Queued as {ex.ToString()}", "FAILED");
            }
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
                if (jobState != null)
                {
                    var keyValuePairs = jobState.History.First()?.Data;
                    if (keyValuePairs != null)
                        history =
                            $"CreatedAt: {jobState.CreatedAt}, Last History: {String.Join(",", keyValuePairs)}";
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
