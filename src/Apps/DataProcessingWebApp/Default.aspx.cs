using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

        private string SendRequest(string path, string arg)
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
                dynamic taskResult;
                if (path.ToLower() != "JobResults".ToLower())
                {
                    // parse job ID
                    var content = result.Content.ReadAsStringAsync().Result;
                    
                    dynamic jobDetails = responseTask.Result;
                    string jobId = jobDetails.jobId;

                    // get jobDetails
                    dynamic jobResult = this.SendRequest("JobResults", $"{jobId}");

                    // loop till job is processing - write to log : processing

                    // when job finished, log job result

                    taskResult = jobResult.Content;
                }
                else
                {
                    taskResult = result.Content;
                }

                return taskResult.ToString();
            }
            else //web api sent error response 
            {
                //log response status here..
                throw new Exception($"{result.ReasonPhrase} {result.RequestMessage}");

            }
        }

        protected void cmdCopyTestFiles_Click(object sender, EventArgs e)
        {
            dynamic result = this.SendRequest("StartJob", "copytestfiles");

        }
    }
}