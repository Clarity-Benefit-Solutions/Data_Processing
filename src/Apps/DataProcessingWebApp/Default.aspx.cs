using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CoreUtils.Classes;
using Microsoft.Ajax.Utilities;

namespace DataProcessingWebApp
{
    public partial class _Default : Page
    {
        private string _username = Utils.GetAppSetting("BasicAuthUserName");
        string _password = Utils.GetAppSetting("BasicAuthPassword");
        //private BindingSource _bindingSource1 = new BindingSource();

        private List<LogFields> _logs = new List<LogFields> { };

        private void addAuthHeader(HttpClient client)
        {
            string creds = $"{_username}:{_password}";
            creds = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(creds));
            AuthenticationHeaderValue authHeaderValue = new AuthenticationHeaderValue("basic", creds);
            client.DefaultRequestHeaders.Authorization = authHeaderValue;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.listLogs.DataSource = this._logs;
            this.listLogs.DataBind();
        }

        private object SendRequest(string path, string arg)
        {
            using var client = new HttpClient();

            String strPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
            String strUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "");

            // set base
            client.BaseAddress = new Uri($"{strUrl}");

            // add auth header
            this.addAuthHeader(client);

            //HTTP GET Async
            var responseTask = client.GetAsync($"/DataProcessing/{path}/{arg}");

            // wait
            responseTask.Wait();

            var result = responseTask.Result;
            if (result.IsSuccessStatusCode)
            {
                // parse job ID
                var content = result.Content.ReadAsStringAsync().Result;

                //
                if (path.ToLower() != "JobResults".ToLower())
                {

                    JobDetails taskJobDetails = (JobDetails)Utils.DeserializeJson<JobDetails>(content);
                    string jobId = (string)taskJobDetails.JobId;

                    // get jobResult
                    Boolean jobIsProcessing = true;

                    // loop till job is processing - write to log : processing

                    while (jobIsProcessing)
                    {
                        // get result of the job started by the request
                        dynamic jobDetailsRequestResult = this.SendRequest("JobResults", $"{jobId}");
                        var jobDetailsContent = jobDetailsRequestResult.Content.ReadAsStringAsync().Result;
                        JobDetails jobDetails = (JobDetails)Utils.DeserializeJson<JobDetails>(jobDetailsContent);
                        //
                        string jobState = jobDetails != null ? jobDetails.JobState : "error";

                        // check job state

                        switch (jobState.ToLower())
                        {
                            case @"processing":
                            case @"started":
                                var logItem = new LogFields(
                                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                                    "",
                                    arg,
                                    jobState,
                                    "",
                                    jobState
                                );

                                this._logs.Add(logItem);
                                this.listLogs.DataBind();
                                Thread.Sleep(500);
                                break;

                            default:
                                jobIsProcessing = false;

                                var logItem2 = new LogFields(
                                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                                    "",
                                    arg,
                                    jobState,
                                    "",
                                    Utils.IsBlank(jobDetails?.JobErrorDetails) ? jobDetails?.JobResultDetails : jobDetails?.JobErrorDetails
                                );

                                this._logs.Add(logItem2);
                                this.listLogs.DataBind();
                                result = jobDetailsRequestResult;
                                break;
                        }

                    }

                    // when job finished, log job result

                    return result;
                }
                else
                {
                    return result;
                }

            }
            else //web api sent error response 
            {
                //log response status here..
                throw new Exception($"{result.ReasonPhrase} {result.RequestMessage}");
            }
        }

        protected void cmdCopyTestFiles_Click(object sender, EventArgs e)
        {
            this._logs.Clear();
            this.listLogs.DataBind();
            dynamic result = this.SendRequest("StartJob", "copytestfiles");

        }

        protected void cmdProcessCobraFiles_Click(object sender, EventArgs e)
        {
            this._logs.Clear();
            this.listLogs.DataBind();
            dynamic result = this.SendRequest("StartJob", "processcobrafiles");
        }

        protected void cmdProcessAlegeusFiles_Click(object sender, EventArgs e)
        {
            this._logs.Clear();
            this.listLogs.DataBind();
            dynamic result = this.SendRequest("StartJob", "processalegeusfiles");
        }

        protected void cmdRetrieveFtpErrorLogs_Click(object sender, EventArgs e)
        {
            this._logs.Clear();
            this.listLogs.DataBind();
            dynamic result = this.SendRequest("StartJob", "retrieveftperrorlogs");
        }

        protected void cmdOpenAccessDB_Click(object sender, EventArgs e)
        {
            this._logs.Clear();
            this.listLogs.DataBind();

            //
        }
    }
}