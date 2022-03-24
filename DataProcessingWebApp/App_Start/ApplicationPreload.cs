using System.Configuration;
using System.Data.Entity;
using System.Web.Hosting;
using Hangfire;

namespace DataProcessingWebApp
{
    public class ApplicationPreload : IProcessHostPreloadClient
    {
        public void Preload(string[] parameters)
        {
            HangfireAspNet.Use(Startup.GetHangfireConfiguration);
        }
    }
}