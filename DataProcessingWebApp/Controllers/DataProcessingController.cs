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
using System.Web.Http;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;

namespace DataProcessingWebApp.Controllers
{
    public class DataProcessingController : ApiController
    {

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


        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public async Task<HttpResponseMessage> Get(string id)
        {
            List<string> listLogs = new List<string>();
            DbUtils.eventOnLogFileOperationCallback += (sender, logParams) =>
            {
                HandleOnFileLogOperationCallback(listLogs, logParams);
            };

            try
            {
                switch (id.ToString().ToLower())
                {
                    case "processcobrafiles":
                        await CobraDataProcessing.ProcessAll();
                        break;

                    case "processalegeusfiles":
                        await CobraDataProcessing.ProcessAll();
                        break;

                    case "retrieveftperrorlogs":
                        await CobraDataProcessing.ProcessAll();
                        break;
                    default:
                        var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : {id} is not a valid operation";
                        throw new Exception(message);

                }

                //
                string[] arr = listLogs.ToArray();
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, "value");
                response.Content = new StringContent(string.Join("\n", arr), Encoding.Unicode);
                response.Headers.CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromMinutes(0)
                };
                return response;
            }
            catch (Exception ex)
            {
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.InternalServerError, "");
                response.Content = new StringContent(ex.ToString(), Encoding.Unicode);
                response.Headers.CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromMinutes(0)
                };
                throw new HttpResponseException(response);
            }

        }

       
    }
}
