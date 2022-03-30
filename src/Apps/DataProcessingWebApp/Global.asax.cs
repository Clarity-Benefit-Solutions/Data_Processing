using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Http;

namespace DataProcessingWebApp
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //ServicesConfig.ConfigureServices();
            //
            StackExchange.Profiling.MiniProfiler.StartNew("DataProcessingWebApp");
        }

        void Application_End(object sender, EventArgs e)
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