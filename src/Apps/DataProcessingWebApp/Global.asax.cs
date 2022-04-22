using System;
using System.Web;
using System.Web.Http;
using System.Web.Optimization;
using System.Web.Routing;
using StackExchange.Profiling;

namespace DataProcessingWebApp
{

    public class Global : HttpApplication
    {
        private void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //ServicesConfig.ConfigureServices();
            //
            MiniProfiler.StartNew("DataProcessingWebApp");
        }

        private void Application_End(object sender, EventArgs e)
        {
            // Debug.Assert(true);
        }

        protected void Application_BeginRequest()
        {
        }

        protected void Application_EndRequest()
        {
        }
    }

}