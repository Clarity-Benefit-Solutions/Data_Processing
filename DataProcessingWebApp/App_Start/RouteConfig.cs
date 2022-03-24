using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using DataProcessingWebApp.Controllers;
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
                "DataProcessingCheckFile",                                           // Route name
                "DataProcessing/CheckFile",                            // URL with parameters
                new { controller = "DataProcessing", action = "CheckFile" }  // Parameter defaults
            );

            routes.MapRoute(
                "DataProcessingStartJob",                                           // Route name
                "DataProcessing/StartJob/{id}",                            // URL with parameters
                new { controller = "DataProcessing", action = "StartJob", id = "" }  // Parameter defaults
            );

            routes.MapRoute(
                   "DataProcessingJobResults",                                           // Route name
                   "DataProcessing/JobResults/{jobId}",                            // URL with parameters
                   new { controller = "DataProcessing", action = "JobResults", jobId = "" }  // Parameter defaults
               );

        }
    }
}
