using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CoreUtils.Classes;
using DataProcessingWebApp.Jobs;
using Hangfire;

namespace DataProcessingWebApp.Controllers
{

    [System.Web.Http.Authorize]
    public class DataProcessingController : Controller
    {
        [System.Web.Http.Authorize]
        public string Index()
        {
            return "DataProcessingController";
        }

        // GET api/<controller>/5
        [System.Web.Http.Authorize]
        public JobDetails StartJob(string id, string ftpSubFolderPath)
        {
            try
            {
                if (Utils.IsBlank(id))
                {
                    return new JobDetails("", id, "", "Job ID MUST BE PASSED");
                }

                // do job in background
                var jobId = BackgroundJob.Enqueue(() => DataProcessingJob.StartJob(null, id, ftpSubFolderPath));

                return new JobDetails(id, jobId, $"[JobDetails ID {jobId} Queued for {id} and ftpSubFolderPath {ftpSubFolderPath}", "STARTED");
            }
            catch (Exception ex)
            {
                return new JobDetails(id, "", $"[Job {id} and ftpSubFolderPath {ftpSubFolderPath} Could Not Be Queued as {ex}", "FAILED");
            }
        }


        [System.Web.Http.Authorize]
        public JobDetails CheckFileAlegeus(HttpPostedFileBase file)
        {
            return this.CheckFile(file, "alegeus");
        }

        [System.Web.Http.Authorize]
        public string localFtpRoot(string ftpSubFolderPath)
        {
            return DataProcessingJob.localFtpRoot(ftpSubFolderPath);
        }

        [System.Web.Http.Authorize]
        public JobDetails CheckFile(HttpPostedFileBase file, string platform)
        {
            var id = "CheckFile: " + platform;

            try
            {
                if (file == null || Utils.IsBlank(file.FileName))
                {
                    return new JobDetails(file != null ? file.FileName : "", "", "{id}", "Valid File MUST BE PASSED");
                }

                // Get local temp file with UniqueID Added
                var srcFileName = file.FileName;
                var srcFilePath = FileUtils.FixPath($"{Path.GetTempPath()}/{srcFileName}");

                // save file to temp path
                file.SaveAs(srcFilePath);

                // do job in background
                var jobId = BackgroundJob.Enqueue(() => DataProcessingJob.CheckFile(null, srcFilePath, "Alegeus"));
                //
                return new JobDetails(id, jobId, jobId,
                    $"[JobDetails ID {jobId} Queued for {id} and File {file.FileName}", "STARTED");
            }
            catch (Exception ex)
            {
                return new JobDetails(id, $"[Job Could Not Be Queued as {ex}", "FAILED");
            }
        }

        [System.Web.Http.Authorize]
        public JobDetails CheckFileCobra(HttpPostedFileBase file)
        {
            return this.CheckFile(file, "cobra");
        }

        [System.Web.Http.Authorize]
        public JobDetails JobResults(string jobId, string ftpSubFolderPath)
        {
            try
            {
                if (Utils.IsBlank(jobId))
                {
                    return new JobDetails("", jobId, $"{jobId} Not Found", "Not Found");
                }

                var jobData = JobStorage.Current.GetConnection().GetJobData(jobId);
                if (jobData == null || jobData.Job == null)
                {
                    return new JobDetails("", jobId, $"{jobId} Not Found", "Not Found");
                }

                var history = "";
                var result = "";

                var jobKey = (string)(jobData?.Job?.Args?[1] ?? "");

                var hMonitoringApi = JobStorage.Current.GetMonitoringApi();
                var jobState = hMonitoringApi.JobDetails(jobId);
                if (jobState != null)
                {
                    IDictionary<string, string> historyData = jobState.History.First()?.Data;
                    if (historyData != null)
                    {
                        KeyValuePair<string, string> jobResultKeyPair = historyData.Last();
                        if (jobResultKeyPair.Key == "Result")
                        {
                            result = jobResultKeyPair.Value;
                        }

                        history = $"CreatedAt: {jobState.CreatedAt}";
                    }
                }

                return new JobDetails(jobKey, jobId, jobId, jobData.State, history, result);
            }
            catch (Exception ex)
            {
                return new JobDetails("", jobId, jobId,
                    $"[JobDetails jobId {jobId} Results Could Not Be Queried as {ex}", "FAILED", "FAILED", "FAILED");
            }
        }
    }

}