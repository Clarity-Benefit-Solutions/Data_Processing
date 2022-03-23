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


namespace DataProcessingWebApp.Controllers
{
    public class DataProcessingController : Controller
    {

      

        // GET api/<controller>/5
        public string Get(string id)
        {
            try
            {
                //jobManager.Start<SampleJob>(x => x.RunAsync());
                // do job in background
                string jobId = BackgroundJob.Enqueue(() => DataProcessingJob.ProcessAsync(null, id));

                return $"Job ID {jobId} Queued for {id}";
            }
            catch (Exception ex)
            {
                return $"ERROR: Job Queuing Job for {id} as {ex.ToString()}";
            }
        }



    }
}
