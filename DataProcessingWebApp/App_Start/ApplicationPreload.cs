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
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<HighlighterDbContext, Configuration>());
            HangfireAspNet.Use(Startup.GetHangfireConfiguration);
        }
    }
}