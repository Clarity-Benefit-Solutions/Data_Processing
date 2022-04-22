using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.AspNet.FriendlyUrls;

namespace DataProcessingWebApp
{

    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            var settings = new FriendlyUrlSettings();
            settings.AutoRedirectMode = RedirectMode.Permanent;

            routes.EnableFriendlyUrls(settings);

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "DataProcessingCheckFileAlegeus", // Route name
                "DataProcessing/CheckFileAlegeus", // URL with parameters
                new {controller = "DataProcessing", action = "CheckFileAlegeus"} // Parameter defaults
            );
            routes.MapRoute(
                "DataProcessingCheckFileCobra", // Route name
                "DataProcessing/CheckFileCobra", // URL with parameters
                new {controller = "DataProcessing", action = "CheckFileCobra"} // Parameter defaults
            );

            routes.MapRoute(
                "DataProcessingStartJob", // Route name
                "DataProcessing/StartJob/{id}", // URL with parameters
                new {controller = "DataProcessing", action = "StartJob", id = ""} // Parameter defaults
            );

            routes.MapRoute(
                "DataProcessingJobResults", // Route name
                "DataProcessing/JobResults/{jobId}", // URL with parameters
                new {controller = "DataProcessing", action = "JobResults", jobId = ""} // Parameter defaults
            );
        }
    }

}