// <auto-generated/>
#pragma warning disable 1591
#pragma warning disable 0414
#pragma warning disable 0649
#pragma warning disable 0169

namespace DataProcessingWebUi.Pages
{
    #line hidden
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
#nullable restore
#line 1 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using System.Net.Http;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using Microsoft.AspNetCore.Authorization;

#line default
#line hidden
#nullable disable
#nullable restore
#line 3 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using Microsoft.AspNetCore.Components.Authorization;

#line default
#line hidden
#nullable disable
#nullable restore
#line 4 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using Microsoft.AspNetCore.Components.Forms;

#line default
#line hidden
#nullable disable
#nullable restore
#line 5 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using Microsoft.AspNetCore.Components.Routing;

#line default
#line hidden
#nullable disable
#nullable restore
#line 6 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using Microsoft.AspNetCore.Components.Web;

#line default
#line hidden
#nullable disable
#nullable restore
#line 7 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using Microsoft.JSInterop;

#line default
#line hidden
#nullable disable
#nullable restore
#line 8 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using DataProcessingWebUi;

#line default
#line hidden
#nullable disable
#nullable restore
#line 9 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using DataProcessingWebUi.Shared;

#line default
#line hidden
#nullable disable
#nullable restore
#line 10 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using Radzen;

#line default
#line hidden
#nullable disable
#nullable restore
#line 11 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\_Imports.razor"
using Radzen.Blazor;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using Utils = CoreUtils.Classes.Utils;

#line default
#line hidden
#nullable disable
#nullable restore
#line 3 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using Newtonsoft.Json;

#line default
#line hidden
#nullable disable
#nullable restore
#line 4 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using Newtonsoft.Json.Converters;

#line default
#line hidden
#nullable disable
#nullable restore
#line 5 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using System.Web;

#line default
#line hidden
#nullable disable
#nullable restore
#line 6 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using System;

#line default
#line hidden
#nullable disable
#nullable restore
#line 7 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using System.Dynamic;

#line default
#line hidden
#nullable disable
#nullable restore
#line 8 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using System.Globalization;

#line default
#line hidden
#nullable disable
#nullable restore
#line 9 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using System.Net.Http.Headers;

#line default
#line hidden
#nullable disable
#nullable restore
#line 10 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using System.Threading;

#line default
#line hidden
#nullable disable
#nullable restore
#line 11 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using CoreUtils.Classes;

#line default
#line hidden
#nullable disable
#nullable restore
#line 12 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using Microsoft.VisualBasic.CompilerServices;

#line default
#line hidden
#nullable disable
    [Microsoft.AspNetCore.Components.RouteAttribute("/")]
    public partial class Index : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
        }
        #pragma warning restore 1998
#nullable restore
#line 123 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"

    private string _username = "dataprocessinguser";
    

#line default
#line hidden
#nullable disable
#nullable restore
#line 125 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
                                                 
    private string _password = "#gjgB0AXG^940";
    

#line default
#line hidden
#nullable disable
#nullable restore
#line 127 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
                                                 

    RadzenDataGrid<LogFields> Grid { get; set; }
    public string ResultTextAreaValue { get; set; } = String.Empty;

    private Boolean needsRefresh = false;

    private string strBaseUrl = "http://be015:81";


    private List<LogFields> Logs = new List<LogFields> { };

    private void addAuthHeader(HttpClient client)
    {
        string creds = $"{_username}:{_password}";
        AuthenticationHeaderValue authHeaderValue = new AuthenticationHeaderValue("basic", creds);
        client.DefaultRequestHeaders.Authorization = authHeaderValue;
    }

    private void LogJobResult(string jobId)
    {

        // get result of the job started by the request
        dynamic jobDetailsRequestResult = this.SendRequest("JobResults", $"{jobId}");
        //
        var jobDetailsContent = jobDetailsRequestResult.Content.ReadAsStringAsync().Result;
        //
        JobDetails jobDetails = JsonConvert.DeserializeObject<JobDetails>(jobDetailsContent);
        //
        string jobState = jobDetails != null ? jobDetails.JobState : "error";

        // check job state
        switch (jobState.ToLower())
        {
            case @"processing":
            case @"started":
            case @"enqueued":
                var logItem = new LogFields(
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "",
                    jobDetails?.JobName,
                    jobState,
                    "",
                    ""
                    );

                this.Log(logItem);

                // schedule new check in N ms
                var timer = new System.Threading.Timer((_) =>
                {
                    InvokeAsync(async () =>
                    {
                        LogJobResult(jobId);
                    });
                }, null, 1000, 0);

                return;

            default:
                OperationResult jobDetailsOutcome = (OperationResult)Utils.DeserializeJson<OperationResult>(jobDetails?.JobResultDetails);

                if (jobDetailsOutcome.Code != "200")
                {
                    jobState = "ERROR";
                }

                var logItem2 = new LogFields(
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "",
                    jobDetails?.JobName,
                    jobState,
                    "",
                    jobDetailsOutcome.ToString()
                    );

                this.Log(logItem2);
                //
                return;
        }


    }

    public object SendRequest(string path, string arg)
    {
        try
        {

            StateHasChanged();

            HttpClient client = new HttpClient { BaseAddress = new Uri($"{strBaseUrl}"), Timeout = TimeSpan.FromSeconds(600) };

            // add auth header
            this.addAuthHeader(client);

            //HTTP GET Async
            var result = client.GetAsync($"/DataProcessing/{path}/{arg}").Result;

            if (!result.IsSuccessStatusCode)
            {
                //log response status here..
                throw new Exception($"{result.ReasonPhrase} {result.RequestMessage}");
            }

            // parse job ID
            var content = result.Content.ReadAsStringAsync().Result;

            //
            if (path.ToLower() != "JobResults".ToLower())
            {
                JobDetails taskJobDetails = JsonConvert.DeserializeObject<JobDetails>(content);

                string jobId = (string)taskJobDetails?.JobId;
                //
                LogJobResult(jobId);

            }
            return result;
        }
        catch (Exception ex)
        {
            var logItem2 = new LogFields(
                DateTime.Now.ToString(CultureInfo.InvariantCulture),
                "",
                arg,
                "ERROR",
                "",
                new OperationResult("0", "200", "ERROPR", "", ex.ToString()).ToString()
                );

            this.Log(logItem2);
            return null;
        }
    }

    void cmdCopyTestFiles()
    {
        dynamic result = this.SendRequest("StartJob", "copytestfiles");
    }

    void cmdProcessCobraFiles()
    {
        dynamic result = this.SendRequest("StartJob", "processcobrafiles");
    }

    void cmdProcessAlegeusFiles()
    {
        dynamic result = this.SendRequest("StartJob", "processalegeusfiles");
    }

    void cmdRetrieveFtpErrorLogs()
    {
        dynamic result = this.SendRequest("StartJob", "retrieveftperrorlogs");
    }

    void cmdOpenAccessDB()
    {
        this.Clear();

    }

    void cmdShowJobStatus()
    {
        jsRuntime.InvokeAsync<object>("open", $"{strBaseUrl}/hangfire/jobs/processing", "_blank");
    }

    void Clear()
    {
        this.Logs.Clear();
        this.ResultTextAreaValue = "";
        this.Grid.Reload();
        //
        this.needsRefresh = true;
        //
        StateHasChanged();
        //
        this.needsRefresh = false;
    }

    void Log(LogFields logItem)
    {

        if (!Utils.IsBlank(logItem.OutcomeDetails))
        {
            OperationResult details = (OperationResult)Utils.DeserializeJson<OperationResult>(logItem.OutcomeDetails);
            if (!Utils.IsBlank(details.Error))
            {
                this.ResultTextAreaValue = details.Error + "\n\n\n\n------------------------------------\n\n\n\n" + details.Details;
            }
            else
            {
                this.ResultTextAreaValue = details.Details;
            }
        }

        //
        logItem.OutcomeDetails = logItem.Status;
        this.Logs.Add(logItem);
        this.Grid.Reload();

        this.needsRefresh = true;
        StateHasChanged();

    }


#line default
#line hidden
#nullable disable
        [global::Microsoft.AspNetCore.Components.InjectAttribute] private IJSRuntime jsRuntime { get; set; }
    }
}
#pragma warning restore 1591
