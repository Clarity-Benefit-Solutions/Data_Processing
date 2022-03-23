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

    public class Job
    {
        public Job(string jobName, string jobId, string jobState = "", string jobHistory = "", string jobResult = "")
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


        // GET api/<controller>/5
        public Job StartJob(string id)
        {
            try
            {
                //jobManager.Start<SampleJob>(x => x.RunAsync());
                // do job in background
                string jobId = BackgroundJob.Enqueue(() => DataProcessingJob.ProcessAsync(null, id));

                return new Job(id, $"[Job ID {jobId} Queued for {id}", "STARTED");
            }
            catch (Exception ex)
            {
                return new Job(id, $"[Job Could Not Be Queued as {ex.ToString()}", "FAILED");
            }
        }
        public Job GetJobResult(string jobId)
        {
            try
            {
                //jobManager.Start<SampleJob>(x => x.RunAsync());
                // do job in background
                //string jobId = BackgroundJob.Enqueue(() => DataProcessingJob.ProcessAsync(null, id));
                var jobData = JobStorage.Current.GetConnection().GetJobData(jobId);
                if (jobData == null || jobData.Job == null)
                {
                    return new Job("", jobId, "Not Found");
                }

                string history = "";
                string result = "";

                string id = (string)(jobData?.Job?.Args?.Last() ?? "");

                var hMonitoringApi = JobStorage.Current.GetMonitoringApi();
                var jobState = hMonitoringApi.JobDetails(jobId);
                if (jobState != null)
                {
                    var keyValuePairs = jobState.History.First()?.Data;
                    if (keyValuePairs != null)
                        history =
                            $"CreatedAt: {jobState.CreatedAt}, Last History: {String.Join(",", keyValuePairs)}";
                }

                return new Job(id, jobId, jobData.State, history, result);

            }
            catch (Exception ex)
            {
                return new Job("", $"[Job Id {jobId} Results Could Not Be Queried as {ex.ToString()}", "FAILED");
            }
        }



    }
}
