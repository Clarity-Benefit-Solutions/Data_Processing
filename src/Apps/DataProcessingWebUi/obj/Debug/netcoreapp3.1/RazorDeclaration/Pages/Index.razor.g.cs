#pragma checksum "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "dc68b4aa096378e9127efbf3f4e591f4e77aed42"
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
#nullable restore
#line 13 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using Syncfusion.Blazor;

#line default
#line hidden
#nullable disable
#nullable restore
#line 14 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
using Syncfusion.Blazor.Grids;

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
#line 113 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"

    private string _username = "dataprocessinguser";
    

#line default
#line hidden
#nullable disable
#nullable restore
#line 115 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
                                                 
    private string _password = "#gjgB0AXG^940";
    

#line default
#line hidden
#nullable disable
#nullable restore
#line 117 "C:\___Clarity\clarity_dev\r1_Data_Processing\src\Apps\DataProcessingWebUi\Pages\Index.razor"
                                                 

    SfGrid<LogFields> Grid { get; set; }
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


    public object SendRequest(string path, string arg)
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

            var converter = new ExpandoObjectConverter();
            dynamic taskJobDetails = JsonConvert.DeserializeObject<ExpandoObject>(content, converter);

            string jobId = (string)taskJobDetails.JobId;

            // get jobResult
            Boolean jobIsProcessing = true;

            // loop till job is processing - write to log : processing

            while (jobIsProcessing)
            {
                // get result of the job started by the request
                dynamic jobDetailsRequestResult = this.SendRequest("JobResults", $"{jobId}");
                var jobDetailsContent = jobDetailsRequestResult.Content.ReadAsStringAsync().Result;
                //
                dynamic jobDetails = JsonConvert.DeserializeObject<ExpandoObject>(jobDetailsContent, converter);
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
                            arg,
                            jobState,
                            "",
                            ""
                            );

                        this.Log(logItem);
                        //
                        Thread.Sleep(1000);
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

                        this.Log(logItem2);
                        result = jobDetailsRequestResult;
                        break;
                }

            }

        }
        return result;
    }

    void cmdCopyTestFiles()
    {
        this.Clear();

        this.Grid.Refresh();
        //
        dynamic result = this.SendRequest("StartJob", "copytestfiles");

    }

    void cmdProcessCobraFiles()
    {
        this.Clear();

        //
        dynamic result = this.SendRequest("StartJob", "processcobrafiles");
    }

    void cmdProcessAlegeusFiles()
    {
        this.Clear();

        //
        dynamic result = this.SendRequest("StartJob", "processalegeusfiles");
    }

    void cmdRetrieveFtpErrorLogs()
    {
        this.Clear();
        //
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
        this.Grid.Refresh();
        //
        this.needsRefresh = true;
        //
        StateHasChanged();
        //
        this.needsRefresh = false;
    }

    void Log(LogFields logItem)
    {
        this.Logs.Add(logItem);
        this.Grid.Refresh();

        if (!Utils.IsBlank(logItem.OutcomeDetails))
        {
            var converter = new ExpandoObjectConverter();
            OperationResult details = (OperationResult)Utils.DeserializeJson<OperationResult>(logItem.OutcomeDetails);
            if (!Utils.IsBlank(details.Error))
            {
                this.ResultTextAreaValue = details.Error;
            }
            else
            {
                this.ResultTextAreaValue = details.Details;
            }
        }
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
