using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using Hangfire.Dashboard;
using DataProcessingWebApp;
using DataProcessingWebApp.Jobs;
using Hangfire;
using Hangfire.Common;
using Hangfire.Console;
using Hangfire.SqlServer;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using Serilog;
using Serilog.Exceptions;

[assembly: OwinStartup(typeof(Startup))]

namespace DataProcessingWebApp
{
    public class Startup
    {
        // keep hangfire logs for N days
        public class ProlongExpirationTimeAttribute : JobFilterAttribute, IApplyStateFilter
        {
            public void OnStateApplied(ApplyStateContext filterContext, IWriteOnlyTransaction transaction)
            {
                filterContext.JobExpirationTimeout = TimeSpan.FromDays(30);

            }
            public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                context.JobExpirationTimeout = TimeSpan.FromDays(30);
            }
        }


        public static IEnumerable<IDisposable> GetHangfireConfiguration()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("App", "DataProcessingWebApp")
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithExceptionDetails()
                .WriteTo.Seq("https://logs.hangfire.io", apiKey: ConfigurationManager.AppSettings["SeqApiKey"])
                .MinimumLevel.Verbose()
                .CreateLogger();

            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSerilogLogProvider()
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseConsole(new ConsoleOptions { FollowJobRetentionPolicy = true, TimestampColor = "Red" })
                .UseSqlServerStorage("HighlighterDb", new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                    EnableHeavyMigrations = true
                });

            ;
            GlobalJobFilters.Filters.Add(new ProlongExpirationTimeAttribute());

            yield return new BackgroundJobServer(new BackgroundJobServerOptions
            {
                WorkerCount = 4,
                StopTimeout = TimeSpan.FromSeconds(5)
            });
        }

        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();

            app.UseHangfireAspNet(GetHangfireConfiguration);
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                // ReSharper disable once UseArrayEmptyMethod
                Authorization = new IDashboardAuthorizationFilter[0]
            });

            //RecurringJob.AddOrUpdate<SnippetHighlighter>(
            //    "SnippetHighlighter.CleanUp",
            //    x => x.CleanUpAsync(),
            //    Cron.Daily);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfireServer();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      
    }
}