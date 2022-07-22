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
                new { controller = "DataProcessing", action = "CheckFileAlegeus" } // Parameter defaults
            );
            routes.MapRoute(
                "DataProcessingCheckFileCobra", // Route name
                "DataProcessing/CheckFileCobra", // URL with parameters
                new { controller = "DataProcessing", action = "CheckFileCobra" } // Parameter defaults
            );

            routes.MapRoute(
                "DataProcessingStartJob", // Route name
                "DataProcessing/StartJob/{id}/{ftpSubFolderPath}", // URL with parameters
                new { controller = "DataProcessing", action = "StartJob", id = "", ftpSubFolderPath = "" } // Parameter defaults
            );

            routes.MapRoute(
              "DataProcessingLocalFtpRoot", // Route name
              "DataProcessing/LocalFtpRoot/{ftpSubFolderPath}", // URL with parameters
              new { controller = "DataProcessing", action = "LocalFtpRoot", ftpSubFolderPath = "" } // Parameter defaults
          );

            routes.MapRoute(
                "DataProcessingJobResults", // Route name
                "DataProcessing/JobResults/{jobId}/{ftpSubFolderPath}", // URL with parameters
                new { controller = "DataProcessing", action = "JobResults", jobId = "", ftpSubFolderPath = "" } // Parameter defaults
            );
        }
    }
}