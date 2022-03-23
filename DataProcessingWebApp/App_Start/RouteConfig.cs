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
                "DataProcessing",                                           // Route name
                "DataProcessing/{id}",                            // URL with parameters
                new { controller = "DataProcessing", action = "Get", id="" }  // Parameter defaults
            );
          

        }
    }
}
