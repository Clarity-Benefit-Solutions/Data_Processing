using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace DataProcessingWebApp
{
    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Unrestricted)]
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //
            //DataProcessing.Vars.IsRunningAsWebApp = true;
            //DataProcessing.Vars.WebAppRootPath = System.Web.Hosting.HostingEnvironment.MapPath("~/"); ;
            //
            Debug.Assert(false);
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
